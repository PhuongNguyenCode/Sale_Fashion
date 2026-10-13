using FashionStore.Data;
using FashionStore.Models;
using FashionStore.Services.Gateways;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Services
{
    /// <summary>
    /// Background service for payment-related periodic tasks
    /// </summary>
    public class PaymentBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentBackgroundService> _logger;

        public PaymentBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<PaymentBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaymentBackgroundService started");

            // Wait 5 seconds before first run to allow application to start
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            int cycleCount = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    cycleCount++;

                    // Poll Casso transactions every cycle (every 10 seconds)
                    // Fast exit if no pending payments exist
                    await PollCassoTransactionsAsync();

                    // Run expire payments every 5 minutes (30 cycles * 10s = 300s)
                    if (cycleCount % 30 == 0)
                    {
                        await ExpirePaymentsAsync();
                    }

                    // Run sync payment status every 15 minutes (90 cycles * 10s = 900s)
                    if (cycleCount % 90 == 0)
                    {
                        await SyncPaymentStatusAsync();
                    }

                    if (cycleCount >= 360)
                    {
                        cycleCount = 0;
                    }

                    // Wait 10 seconds before next cycle
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PaymentBackgroundService");
                    try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); } catch { break; }
                }
            }

            _logger.LogInformation("PaymentBackgroundService stopped");
        }

        /// <summary>
        /// Poll Casso API for new transactions and match with pending payments
        /// </summary>
        public async Task PollCassoTransactionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cassoService = scope.ServiceProvider.GetRequiredService<CassoApiService>();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();

            try
            {
                // Get pending Casso/BankTransfer payments (Pending or Processing)
                var pendingPayments = await context.Payments
                    .Include(p => p.Order)
                    .Where(p => (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing)
                        && (p.PaymentMethod == "Casso" || p.PaymentMethod == "BankTransfer")
                        && p.Order != null
                        && (!p.ExpiresAt.HasValue || p.ExpiresAt.Value > DateTime.Now))
                    .ToListAsync();

                if (!pendingPayments.Any())
                {
                    return;
                }

                _logger.LogInformation($"Found {pendingPayments.Count} pending bank/casso payments. Polling Casso API...");

                // Get recent transactions from Casso
                var transactions = await cassoService.GetRecentTransactionsAsync(50);

                if (!transactions.Any())
                {
                    return;
                }

                _logger.LogInformation($"Found {transactions.Count} recent transactions from Casso");

                // Match transactions with pending payments
                int matchedCount = 0;

                foreach (var payment in pendingPayments)
                {
                    var orderCode = payment.Order!.OrderCode;
                    var cleanOrderCode = orderCode.Replace("-", "").Replace(" ", "");

                    // Find matching transaction (flexible matching with/without hyphens or spaces)
                    var matchingTx = transactions.FirstOrDefault(tx =>
                    {
                        if (string.IsNullOrEmpty(tx.Description)) return false;
                        var cleanDesc = tx.Description.Replace("-", "").Replace(" ", "");
                        return (tx.Description.Contains(orderCode, StringComparison.OrdinalIgnoreCase) ||
                                cleanDesc.Contains(cleanOrderCode, StringComparison.OrdinalIgnoreCase)) &&
                               Math.Abs(tx.Amount - payment.Amount) < 1;
                    });

                    if (matchingTx != null)
                    {
                        _logger.LogInformation($"Found matching transaction for order {orderCode}: TxID={matchingTx.Tid}, Amount={matchingTx.Amount}");

                        // Update payment status
                        var updated = await paymentService.UpdatePaymentStatusAsync(
                            payment.PaymentId,
                            PaymentStatus.Paid,
                            matchingTx.Tid
                        );

                        if (updated)
                        {
                            // Record transaction
                            await paymentService.RecordTransactionAsync(
                                payment.PaymentId,
                                "Casso",
                                matchingTx.Tid ?? matchingTx.Id.ToString(),
                                matchingTx.Amount,
                                PaymentStatus.Paid,
                                new
                                {
                                    matchingTx.Id,
                                    matchingTx.Tid,
                                    matchingTx.Description,
                                    matchingTx.Amount,
                                    matchingTx.When,
                                    Source = "CassoApiPolling"
                                }
                            );

                            // Confirm order and deduct stock
                            await orderService.UpdateOrderStatusAsync(payment.Order.OrderId, OrderStatus.Confirmed);
                            payment.Order.PaymentStatus = PaymentStatus.Paid;
                            await context.SaveChangesAsync();
                            await stockService.DeductStockForOrderAsync(payment.Order.OrderId);

                            matchedCount++;
                            
                            // IMPORTANT: Log when polling confirms payment (indicates webhook may have failed)
                            _logger.LogWarning($"Payment {payment.PaymentId} confirmed via API POLLING for order {orderCode}. " +
                                             $"This indicates webhook may not have arrived. Check webhook configuration.");
                        }
                    }
                }

                _logger.LogInformation($"Casso polling completed: {matchedCount} payments matched out of {pendingPayments.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PollCassoTransactionsAsync");
            }
        }

        /// <summary>
        /// Find and expire old pending/processing payments
        /// </summary>
        public async Task ExpirePaymentsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            try
            {
                _logger.LogInformation("Running ExpirePaymentsAsync task");

                var expirationThreshold = DateTime.Now.AddMinutes(-15);

                // Find payments that should be expired
                var paymentsToExpire = await context.Payments
                    .Include(p => p.Order)
                    .Where(p => (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing)
                        && p.ExpiresAt.HasValue
                        && p.ExpiresAt.Value <= DateTime.Now
                        && p.PaymentMethod != "COD"
                        && p.PaymentMethod != "BankTransfer")
                    .ToListAsync();

                _logger.LogInformation($"Found {paymentsToExpire.Count} payments to expire");

                foreach (var payment in paymentsToExpire)
                {
                    payment.Status = PaymentStatus.Expired;
                    payment.UpdatedAt = DateTime.Now;

                    // Cancel associated order
                    if (payment.Order != null)
                    {
                        await orderService.UpdateOrderStatusAsync(payment.Order.OrderId, OrderStatus.Cancelled);
                        _logger.LogInformation($"Payment {payment.PaymentId} expired and order {payment.Order.OrderCode} cancelled");
                    }
                }

                if (paymentsToExpire.Any())
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Expired {paymentsToExpire.Count} payments successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExpirePaymentsAsync");
            }
        }

        /// <summary>
        /// Query payment gateways for pending/processing payments and sync status
        /// </summary>
        public async Task SyncPaymentStatusAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
            var gateways = scope.ServiceProvider.GetRequiredService<IEnumerable<IPaymentGateway>>();

            try
            {
                _logger.LogInformation("Running SyncPaymentStatusAsync task");

                // Get gateway dictionary
                var gatewayDict = gateways.ToDictionary(g => g.GatewayName, g => g, StringComparer.OrdinalIgnoreCase);

                // Find payments that need status sync (not expired, not COD/BankTransfer)
                var paymentsToSync = await context.Payments
                    .Include(p => p.Order)
                    .Where(p => (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing)
                        && p.PaymentMethod != "COD"
                        && p.PaymentMethod != "BankTransfer"
                        && p.TransactionId != null
                        && (!p.ExpiresAt.HasValue || p.ExpiresAt.Value > DateTime.Now))
                    .OrderBy(p => p.CreatedAt)
                    .Take(100) // Process maximum 100 payments per run
                    .ToListAsync();

                _logger.LogInformation($"Found {paymentsToSync.Count} payments to sync");

                foreach (var payment in paymentsToSync)
                {
                    try
                    {
                        var gatewayName = payment.PaymentGateway ?? payment.PaymentMethod;

                        if (!gatewayDict.TryGetValue(gatewayName, out var gateway))
                        {
                            _logger.LogWarning($"Gateway {gatewayName} not found for payment {payment.PaymentId}");
                            continue;
                        }

                        // Query gateway for payment status
                        var statusResult = await gateway.GetPaymentStatusAsync(payment.TransactionId!);

                        if (statusResult.Success && statusResult.Status != payment.Status)
                        {
                            _logger.LogInformation($"Payment {payment.PaymentId} status changed from {payment.Status} to {statusResult.Status}");

                            // Update payment status
                            var updated = await paymentService.UpdatePaymentStatusAsync(
                                payment.PaymentId,
                                statusResult.Status,
                                statusResult.TransactionId
                            );

                            if (updated)
                            {
                                // Record transaction
                                await paymentService.RecordTransactionAsync(
                                    payment.PaymentId,
                                    gatewayName,
                                    statusResult.TransactionId ?? payment.TransactionId!,
                                    statusResult.Amount ?? payment.Amount,
                                    statusResult.Status,
                                    statusResult
                                );

                                // If newly paid, confirm order and deduct stock
                                if (statusResult.Status == PaymentStatus.Paid && payment.Order != null)
                                {
                                    await orderService.UpdateOrderStatusAsync(payment.Order.OrderId, OrderStatus.Confirmed);
                                    payment.Order.PaymentStatus = PaymentStatus.Paid;
                                    await context.SaveChangesAsync();
                                    await stockService.DeductStockForOrderAsync(payment.Order.OrderId);
                                    _logger.LogInformation($"Order {payment.Order.OrderCode} confirmed and stock deducted after sync");
                                }
                                // If failed/cancelled, cancel order
                                else if ((statusResult.Status == PaymentStatus.Failed || statusResult.Status == PaymentStatus.Cancelled) && payment.Order != null)
                                {
                                    await orderService.UpdateOrderStatusAsync(payment.Order.OrderId, OrderStatus.Cancelled);
                                    payment.Order.PaymentStatus = statusResult.Status;
                                    await context.SaveChangesAsync();
                                    _logger.LogInformation($"Order {payment.Order.OrderCode} cancelled after payment failed in sync");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error syncing payment {payment.PaymentId}");
                        // Continue with next payment
                    }
                }

                _logger.LogInformation($"Synced {paymentsToSync.Count} payments successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SyncPaymentStatusAsync");
            }
        }
    }
}

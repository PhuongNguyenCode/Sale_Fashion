using FashionStore.Data;
using FashionStore.Models;
using FashionStore.Services.Gateways;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FashionStore.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly Dictionary<string, IPaymentGateway> _gateways;

        public PaymentService(
            ApplicationDbContext context,
            ILogger<PaymentService> logger,
            IEnumerable<IPaymentGateway> gateways)
        {
            _context = context;
            _logger = logger;
            _gateways = gateways.ToDictionary(g => g.GatewayName, g => g, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<Payment> CreatePaymentAsync(int orderId, string paymentMethod, decimal amount, string? gateway = null)
        {
            try
            {
                var payment = new Payment
                {
                    OrderId = orderId,
                    PaymentMethod = paymentMethod,
                    PaymentGateway = gateway,
                    Amount = amount,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTime.Now,
                    Version = 1
                };

                // Set expiration for gateway payments (15 minutes)
                if (gateway != null && !string.Equals(paymentMethod, "COD", StringComparison.OrdinalIgnoreCase) 
                    && !string.Equals(paymentMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase))
                {
                    payment.ExpiresAt = DateTime.Now.AddMinutes(15);
                }

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Payment created for order {orderId} with method {paymentMethod}");

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating payment for order {orderId}");
                throw;
            }
        }

        public async Task<PaymentGatewayResult> InitiateGatewayPaymentAsync(Payment payment, string returnUrl, string webhookUrl)
        {
            try
            {
                var gatewayName = payment.PaymentGateway ?? payment.PaymentMethod;

                if (!_gateways.TryGetValue(gatewayName, out var gateway))
                {
                    _logger.LogError($"Payment gateway {gatewayName} not found");
                    return new PaymentGatewayResult
                    {
                        Success = false,
                        ErrorMessage = $"Payment gateway {gatewayName} is not configured"
                    };
                }

                // Load order to get order code
                var order = await _context.Orders.FindAsync(payment.OrderId);
                if (order == null)
                {
                    return new PaymentGatewayResult
                    {
                        Success = false,
                        ErrorMessage = "Order not found"
                    };
                }

                var request = new PaymentRequest
                {
                    OrderCode = order.OrderCode,
                    Amount = payment.Amount,
                    Currency = "VND",
                    ReturnUrl = returnUrl,
                    WebhookUrl = webhookUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        ["PaymentId"] = payment.PaymentId.ToString(),
                        ["OrderId"] = payment.OrderId.ToString()
                    }
                };

                var result = await gateway.CreatePaymentAsync(request);

                // Update payment with transaction ID
                if (result.Success && !string.IsNullOrEmpty(result.TransactionId))
                {
                    payment.TransactionId = result.TransactionId;
                    payment.Status = PaymentStatus.Processing;
                    payment.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Record transaction
                    await RecordTransactionAsync(
                        payment.PaymentId,
                        gatewayName,
                        result.TransactionId,
                        payment.Amount,
                        PaymentStatus.Processing,
                        result
                    );
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initiating gateway payment for payment {payment.PaymentId}");
                return new PaymentGatewayResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to initiate payment: {ex.Message}"
                };
            }
        }

        public async Task<bool> UpdatePaymentStatusAsync(int paymentId, string status, string? transactionId = null)
        {
            const int maxRetries = 3;
            var attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    var payment = await _context.Payments.FindAsync(paymentId);
                    if (payment == null)
                    {
                        _logger.LogWarning($"Payment {paymentId} not found");
                        return false;
                    }

                    var oldStatus = payment.Status;
                    payment.Status = status;
                    payment.UpdatedAt = DateTime.Now;

                    if (!string.IsNullOrEmpty(transactionId))
                    {
                        payment.TransactionId = transactionId;
                    }

                    if (status == PaymentStatus.Paid)
                    {
                        payment.PaymentDate = DateTime.Now;
                    }

                    // Optimistic locking handled by EF Core (Version field)
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Payment {paymentId} status updated from {oldStatus} to {status}");

                    return true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    attempt++;
                    _logger.LogWarning($"Concurrency conflict updating payment {paymentId}, attempt {attempt}/{maxRetries}");

                    if (attempt >= maxRetries)
                    {
                        _logger.LogError(ex, $"Failed to update payment {paymentId} after {maxRetries} attempts");
                        return false;
                    }

                    // Reload entity and retry
                    await Task.Delay(100 * attempt); // Exponential backoff
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating payment {paymentId} status");
                    return false;
                }
            }

            return false;
        }

        public async Task<PaymentTransaction> RecordTransactionAsync(
            int paymentId,
            string gateway,
            string transactionId,
            decimal amount,
            string status,
            object? response = null)
        {
            try
            {
                var transaction = new PaymentTransaction
                {
                    PaymentId = paymentId,
                    Gateway = gateway,
                    ExternalTransactionId = transactionId,
                    Amount = amount,
                    Currency = "VND",
                    Status = status,
                    RawResponse = response != null ? JsonSerializer.Serialize(response) : null,
                    CreatedAt = DateTime.Now
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Transaction {transactionId} recorded for payment {paymentId}");

                return transaction;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_ExternalTransactionId") == true)
            {
                // Transaction already exists (idempotent)
                _logger.LogInformation($"Transaction {transactionId} already exists for payment {paymentId}");
                
                var existingTransaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.ExternalTransactionId == transactionId);

                return existingTransaction!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recording transaction {transactionId} for payment {paymentId}");
                throw;
            }
        }

        public async Task<RefundResult> ProcessRefundAsync(int paymentId, string reason)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment == null)
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = "Payment not found"
                    };
                }

                if (payment.Status != PaymentStatus.Paid)
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = "Payment must be in Paid status to refund"
                    };
                }

                var gatewayName = payment.PaymentGateway ?? payment.PaymentMethod;

                if (!_gateways.TryGetValue(gatewayName, out var gateway))
                {
                    _logger.LogWarning($"Gateway {gatewayName} not found for refund, marking as refunded locally");
                    
                    // For COD/BankTransfer, just update status locally
                    payment.Status = PaymentStatus.Refunded;
                    payment.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return new RefundResult
                    {
                        Success = true,
                        RefundId = $"LOCAL-{paymentId}",
                        RefundDate = DateTime.Now
                    };
                }

                // Process refund through gateway
                var transactionId = payment.TransactionId ?? "";
                var result = await gateway.RefundPaymentAsync(transactionId, payment.Amount, reason);

                if (result.Success)
                {
                    // Update payment status
                    payment.Status = PaymentStatus.Refunded;
                    payment.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Record refund transaction
                    if (!string.IsNullOrEmpty(result.RefundId))
                    {
                        await RecordTransactionAsync(
                            paymentId,
                            gatewayName,
                            result.RefundId,
                            payment.Amount,
                            PaymentStatus.Refunded,
                            result
                        );
                    }

                    _logger.LogInformation($"Refund processed for payment {paymentId}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing refund for payment {paymentId}");
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to process refund: {ex.Message}"
                };
            }
        }

        public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        public async Task<Payment?> GetPaymentByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
        }
    }
}

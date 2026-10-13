using FashionStore.Data;
using FashionStore.Models;
using FashionStore.Services.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace FashionStore.Services
{
    public class WebhookHandler : IWebhookHandler
    {
        private readonly ApplicationDbContext _context;
        private readonly ITransactionValidator _validator;
        private readonly IIdempotencyManager _idempotencyManager;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IStockService _stockService;
        private readonly ILogger<WebhookHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, IWebhookParser> _parsers;

        public WebhookHandler(
            ApplicationDbContext context,
            ITransactionValidator validator,
            IIdempotencyManager idempotencyManager,
            IPaymentService paymentService,
            IOrderService orderService,
            IStockService stockService,
            ILogger<WebhookHandler> logger,
            IConfiguration configuration,
            IEnumerable<IWebhookParser> parsers)
        {
            _context = context;
            _validator = validator;
            _idempotencyManager = idempotencyManager;
            _paymentService = paymentService;
            _orderService = orderService;
            _stockService = stockService;
            _logger = logger;
            _configuration = configuration;
            _parsers = parsers.ToDictionary(p => p.GatewayName, p => p, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> HandleWebhookAsync(string gateway, HttpRequest request)
        {
            try
            {
                string payload;
                using (var reader = new StreamReader(request.Body, Encoding.UTF8))
                {
                    payload = await reader.ReadToEndAsync();
                }

                _logger.LogInformation($"Webhook received from {gateway}: {payload}");

                if (!_parsers.TryGetValue(gateway, out var parser))
                {
                    _logger.LogError($"No parser found for gateway: {gateway}");
                    return new BadRequestObjectResult(new { error = "Unsupported gateway" });
                }

                WebhookPayload webhookPayload;
                try
                {
                    webhookPayload = parser.Parse(payload);
                    _logger.LogInformation($"Parsed webhook: {parser.PrettyPrint(webhookPayload)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to parse webhook from {gateway}");
                    return new BadRequestObjectResult(new { error = "Invalid webhook payload" });
                }

                // Xử lý request test/validate webhook (payOS/Casso gửi payload mẫu khi bạn lưu webhook URL)
                // PHẢI đặt TRƯỚC bước xác thực chữ ký và validate đơn hàng, vì payload test
                // không có đơn hàng thật để so khớp.
                if (webhookPayload.Status == "Ping")
                {
                    _logger.LogInformation($"Webhook validation ping từ {gateway} — trả về OK");
                    return new OkObjectResult(new { message = "Webhook endpoint is active" });
                }

                // Verify signature / secure token
                var signature = GetSignatureFromRequest(gateway, payload, request);

                var secretKey = GetSecretKey(gateway);
                if (string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogWarning($"No secret key configured for gateway {gateway}");
                    if (gateway.Equals("momo", StringComparison.OrdinalIgnoreCase))
                    {
                        return new StatusCodeResult(503);
                    }
                }
                else
                {
                    var isValidSignature = _validator.VerifySignature(gateway, payload, signature, secretKey);
                    if (!isValidSignature)
                    {
                        _logger.LogWarning($"Invalid signature for webhook from {gateway}");
                        return new BadRequestObjectResult(new { error = "Invalid signature" });
                    }
                }

                // An authorization notification is not a completed payment. Await
                // the final MoMo IPN so the same transId cannot block it as a duplicate.
                if (gateway.Equals("momo", StringComparison.OrdinalIgnoreCase) &&
                    (webhookPayload.Status == PaymentStatus.Processing ||
                     webhookPayload.Status == PaymentStatus.Pending))
                {
                    return new OkObjectResult(new { message = "MoMo payment is not final" });
                }

                var isProcessed = await _idempotencyManager.IsTransactionProcessedAsync(webhookPayload.TransactionId);
                if (isProcessed)
                {
                    _logger.LogInformation($"Transaction {webhookPayload.TransactionId} already processed (idempotent)");
                    return new OkObjectResult(new { message = "Webhook already processed" });
                }

                var validationResult = await _validator.ValidateWebhookAsync(webhookPayload);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning($"Webhook validation failed: {validationResult.ErrorMessage}");
                    return new BadRequestObjectResult(new { error = validationResult.ErrorMessage });
                }

                if (gateway.Equals("momo", StringComparison.OrdinalIgnoreCase) &&
                    (validationResult.Order!.OrderCode != webhookPayload.OrderCode ||
                     !string.Equals(validationResult.Payment!.PaymentGateway, "MoMo", StringComparison.OrdinalIgnoreCase)))
                {
                    return new BadRequestObjectResult(new { error = "MoMo order or gateway mismatch" });
                }

                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var order = validationResult.Order!;
                    var payment = validationResult.Payment!;

                    var statusUpdated = await _paymentService.UpdatePaymentStatusAsync(
                        payment.PaymentId,
                        webhookPayload.Status,
                        webhookPayload.TransactionId
                    );

                    if (!statusUpdated)
                    {
                        throw new Exception("Failed to update payment status");
                    }

                    await _paymentService.RecordTransactionAsync(
                        payment.PaymentId,
                        gateway,
                        webhookPayload.TransactionId,
                        webhookPayload.Amount,
                        webhookPayload.Status,
                        webhookPayload.RawResponse
                    );

                    if (webhookPayload.Status == PaymentStatus.Paid)
                    {
                        // The gateway has received the money. Record that fact even if
                        // fulfillment needs manual attention because stock is missing.
                        order.PaymentStatus = PaymentStatus.Paid;
                        await _context.SaveChangesAsync();

                        var stockDeducted = await _stockService.DeductStockForOrderAsync(order.OrderId);
                        if (stockDeducted)
                        {
                            if (!await _orderService.UpdateOrderStatusAsync(order.OrderId, OrderStatus.Confirmed))
                            {
                                throw new InvalidOperationException($"Could not confirm paid order {order.OrderCode}");
                            }

                            _logger.LogInformation("Order {OrderCode} confirmed via webhook and stock deducted", order.OrderCode);
                        }
                        else
                        {
                            _logger.LogCritical("Order {OrderCode} was paid but stock could not be deducted; manual fulfillment or refund is required", order.OrderCode);
                        }
                    }
                    else if (webhookPayload.Status == PaymentStatus.Failed || webhookPayload.Status == PaymentStatus.Cancelled)
                    {
                        await _orderService.UpdateOrderStatusAsync(order.OrderId, OrderStatus.Cancelled);

                        // Also update Order.PaymentStatus
                        order.PaymentStatus = webhookPayload.Status;
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Order {order.OrderCode} cancelled due to payment failure");
                    }
                    else if (webhookPayload.Status == PaymentStatus.Refunded)
                    {
                        await _stockService.RestoreStockForOrderAsync(order.OrderId);
                        await _orderService.UpdateOrderStatusAsync(order.OrderId, OrderStatus.Refunded);

                        // Also update Order.PaymentStatus
                        order.PaymentStatus = PaymentStatus.Refunded;
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Order {order.OrderCode} refunded and stock restored");
                    }

                    await _idempotencyManager.MarkTransactionAsProcessedAsync(webhookPayload.TransactionId, payment.PaymentId);

                    await dbTransaction.CommitAsync();

                    _logger.LogInformation($"Webhook processed successfully for transaction {webhookPayload.TransactionId}");

                    return new OkObjectResult(new { message = "Webhook processed successfully" });
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, $"Error processing webhook for transaction {webhookPayload.TransactionId}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling webhook from {gateway}");
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// payOS gửi chữ ký NẰM TRONG body JSON (field "signature"), không phải trong header.
        /// Các gateway khác (MoMo, Stripe...) dùng header như cũ.
        /// </summary>
        private string GetSignatureFromRequest(string gateway, string payload, HttpRequest request)
        {
            if (gateway.Equals("casso", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var doc = JsonDocument.Parse(payload);
                    if (doc.RootElement.TryGetProperty("signature", out var sigEl))
                    {
                        return sigEl.GetString() ?? "";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không đọc được field 'signature' trong payload payOS");
                }
                return "";
            }

            if (gateway.Equals("vnpay", StringComparison.OrdinalIgnoreCase))
            {
                // VNPay sends IPN as query string in the body
                // The signature is the vnp_SecureHash parameter
                try
                {
                    var queryParams = System.Web.HttpUtility.ParseQueryString(payload);
                    return queryParams["vnp_SecureHash"] ?? "";
                }
                catch
                {
                    return "";
                }
            }

            if (gateway.Equals("momo", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var doc = JsonDocument.Parse(payload);
                    return doc.RootElement.GetProperty("signature").GetString() ?? "";
                }
                catch
                {
                    return "";
                }
            }

            return request.Headers["Signature"].FirstOrDefault()
                ?? request.Headers["X-Signature"].FirstOrDefault()
                ?? request.Headers["Authorization"].FirstOrDefault()
                ?? "";
        }

        private string GetSecretKey(string gateway)
        {
            return gateway.ToLower() switch
            {
                "momo" => _configuration["PaymentGateways:MoMo:SecretKey"] ?? "",
                "vnpay" => _configuration["PaymentGateways:VNPay:HashSecret"] ?? "",
                "zalopay" => Environment.GetEnvironmentVariable("ZALOPAY_KEY2") ?? "",
                "stripe" => Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ?? "",
                "paypal" => Environment.GetEnvironmentVariable("PAYPAL_WEBHOOK_SECRET") ?? "",
                "casso" => _configuration["PaymentGateways:Casso:ChecksumKey"] ?? "",
                _ => ""
            };
        }
    }
}

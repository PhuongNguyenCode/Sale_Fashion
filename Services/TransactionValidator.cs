using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FashionStore.Data;
using FashionStore.Models;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Services
{
    public class TransactionValidator : ITransactionValidator
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransactionValidator> _logger;
        private readonly IConfiguration _configuration;

        public TransactionValidator(
            ApplicationDbContext context,
            ILogger<TransactionValidator> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public bool VerifySignature(string gateway, string payload, string signature, string secretKey)
        {
            try
            {
                if (gateway.Equals("casso", StringComparison.OrdinalIgnoreCase))
                {
                    return VerifyPayOSSignature(payload, signature, secretKey);
                }

                if (gateway.Equals("vnpay", StringComparison.OrdinalIgnoreCase))
                {
                    return VerifyVNPaySignature(payload, signature, secretKey);
                }

                if (gateway.Equals("momo", StringComparison.OrdinalIgnoreCase))
                {
                    return VerifyMoMoSignature(payload, signature, secretKey);
                }

                // Các gateway khác (MoMo, ZaloPay, Stripe, PayPal) dùng HMAC-SHA256 trên toàn bộ payload
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                string computedSignature = Convert.ToBase64String(hashBytes);

                var computedBytes = Encoding.UTF8.GetBytes(computedSignature);
                var receivedBytes = Encoding.UTF8.GetBytes(signature);

                if (computedBytes.Length != receivedBytes.Length)
                {
                    return false;
                }

                return CryptographicOperations.FixedTimeEquals(computedBytes, receivedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature for gateway {Gateway}", gateway);
                return false;
            }
        }

        private bool VerifyMoMoSignature(string payload, string signature, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(signature)) return false;

            using var doc = JsonDocument.Parse(payload);
            var data = doc.RootElement;
            string Field(string name) => data.TryGetProperty(name, out var value)
                ? value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.ToString()
                : "";

            var partnerCode = _configuration["PaymentGateways:MoMo:PartnerCode"];
            var accessKey = _configuration["PaymentGateways:MoMo:AccessKey"];
            if (string.IsNullOrWhiteSpace(partnerCode) || string.IsNullOrWhiteSpace(accessKey) ||
                !string.Equals(Field("partnerCode"), partnerCode, StringComparison.Ordinal))
            {
                return false;
            }

            var raw = $"accessKey={accessKey}&amount={Field("amount")}&extraData={Field("extraData")}" +
                $"&message={Field("message")}&orderId={Field("orderId")}&orderInfo={Field("orderInfo")}" +
                $"&orderType={Field("orderType")}&partnerCode={Field("partnerCode")}&payType={Field("payType")}" +
                $"&requestId={Field("requestId")}&responseTime={Field("responseTime")}" +
                $"&resultCode={Field("resultCode")}&transId={Field("transId")}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
            var receivedBytes = Encoding.UTF8.GetBytes(signature.ToLowerInvariant());
            var computedBytes = Encoding.UTF8.GetBytes(computed);
            return receivedBytes.Length == computedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(receivedBytes, computedBytes);
        }

        /// <summary>
        /// Xác thực chữ ký payOS theo đúng thuật toán chính thức:
        /// https://payos.vn/docs/tich-hop-webhook/kiem-tra-du-lieu-voi-signature/
        /// - Lấy object "data" trong payload
        /// - Sắp xếp các key theo alphabet
        /// - Ghép thành chuỗi key1=value1&key2=value2...
        /// - Tính HMAC-SHA256 với Checksum Key, so sánh với field "signature"
        /// </summary>
        private bool VerifyPayOSSignature(string payload, string signature, string checksumKey)
        {
            try
            {
                if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(checksumKey))
                {
                    return false;
                }

                using var doc = JsonDocument.Parse(payload);
                if (!doc.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning("payOS webhook payload không có object 'data' để xác thực chữ ký");
                    return false;
                }

                var pairs = new List<(string Key, string Value)>();
                foreach (var prop in dataElement.EnumerateObject())
                {
                    string value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        JsonValueKind.Number => prop.Value.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => "",
                        _ => prop.Value.GetRawText()
                    };
                    pairs.Add((prop.Name, value));
                }

                // Sắp xếp theo key alphabet (giống ksort của PHP)
                var dataStr = string.Join("&",
                    pairs.OrderBy(p => p.Key, StringComparer.Ordinal)
                         .Select(p => $"{p.Key}={p.Value}"));

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataStr));
                var computedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();

                var computedBytes = Encoding.UTF8.GetBytes(computedSignature);
                var receivedBytes = Encoding.UTF8.GetBytes(signature.ToLowerInvariant());

                if (computedBytes.Length != receivedBytes.Length)
                {
                    _logger.LogWarning("Độ dài chữ ký payOS không khớp — kiểm tra lại Checksum Key");
                    return false;
                }

                return CryptographicOperations.FixedTimeEquals(computedBytes, receivedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payOS signature");
                return false;
            }
        }

        private bool VerifyVNPaySignature(string queryString, string signature, string hashSecret)
        {
            try
            {
                if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(hashSecret))
                    return false;

                var segments = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);
                var sortedParams = new SortedList<string, string>(StringComparer.Ordinal);
                
                foreach (var segment in segments)
                {
                    var eqIndex = segment.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        var key = segment.Substring(0, eqIndex);
                        var value = segment.Substring(eqIndex + 1);
                        
                        if (!key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                            !key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                        {
                            sortedParams.Add(key, value);
                        }
                    }
                }

                var dataStr = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));

                using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hashSecret));
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataStr));
                var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                var computedBytes = Encoding.UTF8.GetBytes(computedHash);
                var receivedBytes = Encoding.UTF8.GetBytes(signature.ToLowerInvariant());

                if (computedBytes.Length != receivedBytes.Length)
                    return false;

                return CryptographicOperations.FixedTimeEquals(computedBytes, receivedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying VNPay signature");
                return false;
            }
        }

        public async Task<ValidationResult> ValidateWebhookAsync(WebhookPayload payload)
        {
            var result = new ValidationResult();

            try
            {
                Order? order = null;

                if (!string.IsNullOrEmpty(payload.OrderCode))
                {
                    // 1. Try exact match first
                    order = await _context.Orders
                        .Include(o => o.Payment)
                        .FirstOrDefaultAsync(o => o.OrderCode == payload.OrderCode);

                    // 2. If not found, try flexible digit matching
                    if (order == null)
                    {
                        var cleanPayloadCode = new string(payload.OrderCode.Where(char.IsDigit).ToArray());
                        if (!string.IsNullOrEmpty(cleanPayloadCode))
                        {
                            var pendingOrders = await _context.Orders
                                .Include(o => o.Payment)
                                .Where(o => o.PaymentStatus == PaymentStatus.Pending || o.PaymentStatus == PaymentStatus.Processing)
                                .ToListAsync();

                            order = pendingOrders.FirstOrDefault(o =>
                            {
                                var cleanDbCode = new string(o.OrderCode.Where(char.IsDigit).ToArray());
                                return !string.IsNullOrEmpty(cleanDbCode) &&
                                       (cleanDbCode.Contains(cleanPayloadCode) || cleanPayloadCode.Contains(cleanDbCode));
                            });
                        }
                    }
                }

                if (order == null)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Order not found: {payload.OrderCode}";
                    _logger.LogWarning("Webhook validation failed: Order not found {OrderCode}", payload.OrderCode);
                    return result;
                }

                if (order.Payment == null)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Payment not found for order: {payload.OrderCode}";
                    _logger.LogWarning("Webhook validation failed: Payment not found for order {OrderCode}", payload.OrderCode);
                    return result;
                }

                // A new transaction must never revive an order that has already
                // reached a terminal payment state. Repeated transaction IDs are
                // acknowledged by WebhookHandler before this validation.
                if (order.Payment.Status != PaymentStatus.Pending &&
                    order.Payment.Status != PaymentStatus.Processing)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Payment is no longer awaiting confirmation";
                    return result;
                }

                if (order.Payment.Amount != payload.Amount)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Amount mismatch. Expected: {order.Payment.Amount}, Received: {payload.Amount}";
                    _logger.LogWarning(
                        "Webhook validation failed: Amount mismatch for order {OrderCode}. Expected: {Expected}, Received: {Received}",
                        payload.OrderCode, order.Payment.Amount, payload.Amount);
                    return result;
                }

                result.IsValid = true;
                result.Order = order;
                result.Payment = order.Payment;

                _logger.LogInformation(
                    "Webhook validation passed for order {OrderCode}, transaction {TransactionId}",
                    payload.OrderCode, payload.TransactionId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating webhook for order {OrderCode}", payload.OrderCode);
                result.IsValid = false;
                result.ErrorMessage = "Internal validation error";
                return result;
            }
        }
    }
}

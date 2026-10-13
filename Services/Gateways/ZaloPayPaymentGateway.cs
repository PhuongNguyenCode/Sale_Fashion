using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FashionStore.Services.Gateways
{
    /// <summary>
    /// ZaloPay e-wallet payment gateway integration
    /// </summary>
    public class ZaloPayPaymentGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ZaloPayPaymentGateway> _logger;

        public string GatewayName => "ZaloPay";

        public ZaloPayPaymentGateway(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<ZaloPayPaymentGateway> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PaymentGatewayResult> CreatePaymentAsync(PaymentRequest request)
        {
            try
            {
                var appId = _configuration["Payment:ZaloPay:AppId"] ?? throw new InvalidOperationException("ZaloPay AppId not configured");
                var key1 = _configuration["Payment:ZaloPay:Key1"] ?? throw new InvalidOperationException("ZaloPay Key1 not configured");
                var apiUrl = _configuration["Payment:ZaloPay:ApiUrl"] ?? "https://sb-openapi.zalopay.vn/v2/create";

                var appTransId = $"{DateTime.Now:yyMMdd}_{Guid.NewGuid():N}";
                var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var amount = (long)request.Amount;
                var embedData = JsonSerializer.Serialize(new { redirecturl = request.ReturnUrl });
                var item = JsonSerializer.Serialize(new[] { new { itemid = request.OrderCode, itemname = $"Order {request.OrderCode}", itemprice = amount, itemquantity = 1 } });

                // Create MAC: data = appid|apptransid|appuser|amount|apptime|embeddata|item
                var data = $"{appId}|{appTransId}|FashionStore|{amount}|{appTime}|{embedData}|{item}";
                var mac = ComputeHmacSha256(data, key1);

                var payload = new Dictionary<string, string>
                {
                    ["app_id"] = appId,
                    ["app_trans_id"] = appTransId,
                    ["app_user"] = "FashionStore",
                    ["amount"] = amount.ToString(),
                    ["app_time"] = appTime.ToString(),
                    ["embed_data"] = embedData,
                    ["item"] = item,
                    ["description"] = $"Payment for order {request.OrderCode}",
                    ["bank_code"] = "",
                    ["mac"] = mac,
                    ["callback_url"] = request.WebhookUrl
                };

                var content = new FormUrlEncodedContent(payload);
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"ZaloPay API Response: {responseContent}");

                var result = JsonSerializer.Deserialize<ZaloPayResponse>(responseContent);

                if (result?.ReturnCode == 1)
                {
                    return new PaymentGatewayResult
                    {
                        Success = true,
                        PaymentUrl = result.OrderUrl,
                        TransactionId = appTransId,
                        ErrorMessage = null,
                        AdditionalInfo = new Dictionary<string, string>
                        {
                            ["ZpTransToken"] = result.ZpTransToken ?? ""
                        }
                    };
                }
                else
                {
                    return new PaymentGatewayResult
                    {
                        Success = false,
                        ErrorMessage = result?.ReturnMessage ?? "ZaloPay API error",
                        TransactionId = appTransId
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ZaloPay payment");
                return new PaymentGatewayResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create ZaloPay payment: {ex.Message}"
                };
            }
        }

        public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                var appId = _configuration["Payment:ZaloPay:AppId"] ?? throw new InvalidOperationException("ZaloPay AppId not configured");
                var key1 = _configuration["Payment:ZaloPay:Key1"] ?? throw new InvalidOperationException("ZaloPay Key1 not configured");
                var apiUrl = _configuration["Payment:ZaloPay:QueryApiUrl"] ?? "https://sb-openapi.zalopay.vn/v2/query";

                var appTransId = transactionId;
                var data = $"{appId}|{appTransId}|{key1}";
                var mac = ComputeHmacSha256(data, key1);

                var payload = new Dictionary<string, string>
                {
                    ["app_id"] = appId,
                    ["app_trans_id"] = appTransId,
                    ["mac"] = mac
                };

                var content = new FormUrlEncodedContent(payload);
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<ZaloPayStatusResponse>(responseContent);

                if (result?.ReturnCode == 1)
                {
                    return new PaymentStatusResult
                    {
                        Success = true,
                        Status = "Paid",
                        TransactionId = result.ZpTransId,
                        Amount = result.Amount
                    };
                }
                else if (result?.ReturnCode == 2)
                {
                    return new PaymentStatusResult
                    {
                        Success = true,
                        Status = "Failed",
                        ErrorMessage = result.ReturnMessage
                    };
                }
                else
                {
                    return new PaymentStatusResult
                    {
                        Success = true,
                        Status = "Processing",
                        ErrorMessage = result?.ReturnMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying ZaloPay payment status");
                return new PaymentStatusResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to query ZaloPay status: {ex.Message}"
                };
            }
        }

        public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
        {
            try
            {
                var appId = _configuration["Payment:ZaloPay:AppId"] ?? throw new InvalidOperationException("ZaloPay AppId not configured");
                var key1 = _configuration["Payment:ZaloPay:Key1"] ?? throw new InvalidOperationException("ZaloPay Key1 not configured");
                var apiUrl = _configuration["Payment:ZaloPay:RefundApiUrl"] ?? "https://sb-openapi.zalopay.vn/v2/refund";

                var mRefundId = $"{DateTime.Now:yyMMdd}_{Guid.NewGuid():N}";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var zpTransId = transactionId;
                var amountLong = (long)amount;

                var data = $"{appId}|{zpTransId}|{amountLong}|{reason}|{timestamp}";
                var mac = ComputeHmacSha256(data, key1);

                var payload = new Dictionary<string, string>
                {
                    ["app_id"] = appId,
                    ["zp_trans_id"] = zpTransId,
                    ["m_refund_id"] = mRefundId,
                    ["amount"] = amountLong.ToString(),
                    ["description"] = reason,
                    ["timestamp"] = timestamp.ToString(),
                    ["mac"] = mac
                };

                var content = new FormUrlEncodedContent(payload);
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<ZaloPayRefundResponse>(responseContent);

                if (result?.ReturnCode == 1)
                {
                    return new RefundResult
                    {
                        Success = true,
                        RefundId = result.RefundId,
                        RefundDate = DateTime.Now
                    };
                }
                else
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = result?.ReturnMessage ?? "ZaloPay refund failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ZaloPay refund");
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to process ZaloPay refund: {ex.Message}"
                };
            }
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                var key2 = _configuration["Payment:ZaloPay:Key2"] ?? throw new InvalidOperationException("ZaloPay Key2 not configured");

                // Parse payload to extract data field
                var webhookData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);
                if (webhookData == null || !webhookData.ContainsKey("data")) return false;

                var data = webhookData["data"].GetString();
                if (string.IsNullOrEmpty(data)) return false;

                // Compute MAC using Key2
                var computedMac = ComputeHmacSha256(data, key2);

                return computedMac.Equals(signature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying ZaloPay webhook signature");
                return false;
            }
        }

        private string ComputeHmacSha256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private class ZaloPayResponse
        {
            public int ReturnCode { get; set; }
            public string? ReturnMessage { get; set; }
            public string? OrderUrl { get; set; }
            public string? ZpTransToken { get; set; }
        }

        private class ZaloPayStatusResponse
        {
            public int ReturnCode { get; set; }
            public string? ReturnMessage { get; set; }
            public string? ZpTransId { get; set; }
            public decimal Amount { get; set; }
        }

        private class ZaloPayRefundResponse
        {
            public int ReturnCode { get; set; }
            public string? ReturnMessage { get; set; }
            public string? RefundId { get; set; }
        }
    }
}

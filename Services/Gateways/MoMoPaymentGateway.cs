using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FashionStore.Services.Gateways
{
    /// <summary>
    /// MoMo e-wallet payment gateway integration
    /// </summary>
    public class MoMoPaymentGateway : IPaymentGateway
    {
        private static readonly JsonSerializerOptions MoMoJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MoMoPaymentGateway> _logger;

        public string GatewayName => "MoMo";

        public MoMoPaymentGateway(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<MoMoPaymentGateway> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PaymentGatewayResult> CreatePaymentAsync(PaymentRequest request)
        {
            try
            {
                var partnerCode = _configuration["PaymentGateways:MoMo:PartnerCode"] ?? throw new InvalidOperationException("MoMo PartnerCode not configured");
                var accessKey = _configuration["PaymentGateways:MoMo:AccessKey"] ?? throw new InvalidOperationException("MoMo AccessKey not configured");
                var secretKey = _configuration["PaymentGateways:MoMo:SecretKey"] ?? throw new InvalidOperationException("MoMo SecretKey not configured");
                var apiUrl = _configuration["PaymentGateways:MoMo:ApiUrl"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";

                var requestId = Guid.NewGuid().ToString();
                var orderId = request.OrderCode;
                var amount = ((long)request.Amount).ToString();
                var orderInfo = $"Payment for order {request.OrderCode}";
                var redirectUrl = request.ReturnUrl;
                var ipnUrl = request.WebhookUrl;
                var requestType = "captureWallet";
                var extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request.Metadata)));

                // Create signature: accessKey=$accessKey&amount=$amount&extraData=$extraData&ipnUrl=$ipnUrl&orderId=$orderId&orderInfo=$orderInfo&partnerCode=$partnerCode&redirectUrl=$redirectUrl&requestId=$requestId&requestType=$requestType
                var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
                var signature = ComputeHmacSha256(rawSignature, secretKey);

                var payload = new
                {
                    partnerCode,
                    accessKey,
                    requestId,
                    amount,
                    orderId,
                    orderInfo,
                    redirectUrl,
                    ipnUrl,
                    requestType,
                    extraData,
                    signature,
                    lang = "vi"
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("MoMo create request failed with HTTP {StatusCode}", response.StatusCode);
                    return new PaymentGatewayResult { Success = false, ErrorMessage = "MoMo payment service unavailable" };
                }

                var result = JsonSerializer.Deserialize<MoMoResponse>(responseContent, MoMoJsonOptions);
                _logger.LogInformation("MoMo create resultCode={ResultCode}, orderId={OrderId}", result?.ResultCode, result?.OrderId);

                if (result?.ResultCode == 0 && !string.IsNullOrWhiteSpace(result.PayUrl) &&
                    result.OrderId == orderId && result.RequestId == requestId)
                {
                    return new PaymentGatewayResult
                    {
                        Success = true,
                        PaymentUrl = result.PayUrl,
                        // MoMo's query API identifies a payment by orderId.
                        TransactionId = orderId,
                        ErrorMessage = null
                    };
                }
                else
                {
                    return new PaymentGatewayResult
                    {
                        Success = false,
                        ErrorMessage = result?.Message ?? "MoMo did not return a valid payment URL"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MoMo payment");
                return new PaymentGatewayResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create MoMo payment: {ex.Message}"
                };
            }
        }

        public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                var partnerCode = _configuration["PaymentGateways:MoMo:PartnerCode"] ?? throw new InvalidOperationException("MoMo PartnerCode not configured");
                var accessKey = _configuration["PaymentGateways:MoMo:AccessKey"] ?? throw new InvalidOperationException("MoMo AccessKey not configured");
                var secretKey = _configuration["PaymentGateways:MoMo:SecretKey"] ?? throw new InvalidOperationException("MoMo SecretKey not configured");
                var apiUrl = _configuration["PaymentGateways:MoMo:QueryApiUrl"] ?? "https://test-payment.momo.vn/v2/gateway/api/query";

                var requestId = Guid.NewGuid().ToString();
                // Payment.TransactionId stores MoMo's orderId until the IPN is processed.
                var orderId = transactionId;

                // Create signature
                var rawSignature = $"accessKey={accessKey}&orderId={orderId}&partnerCode={partnerCode}&requestId={requestId}";
                var signature = ComputeHmacSha256(rawSignature, secretKey);

                var payload = new
                {
                    partnerCode,
                    accessKey,
                    requestId,
                    orderId,
                    signature,
                    lang = "vi"
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new PaymentStatusResult { Success = false, Status = "Pending", ErrorMessage = "MoMo query unavailable" };
                }

                var result = JsonSerializer.Deserialize<MoMoStatusResponse>(responseContent, MoMoJsonOptions);

                if (result?.ResultCode != null && result.OrderId == orderId)
                {
                    return new PaymentStatusResult
                    {
                        Success = true,
                        Status = MapMoMoStatus(result.ResultCode.Value),
                        TransactionId = result.TransId?.ToString() ?? orderId,
                        Amount = result.Amount
                    };
                }
                else
                {
                    return new PaymentStatusResult
                    {
                        Success = false,
                        Status = "Pending",
                        ErrorMessage = result?.Message
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying MoMo payment status");
                return new PaymentStatusResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to query MoMo status: {ex.Message}"
                };
            }
        }

        public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
        {
            try
            {
                var partnerCode = _configuration["PaymentGateways:MoMo:PartnerCode"] ?? throw new InvalidOperationException("MoMo PartnerCode not configured");
                var accessKey = _configuration["PaymentGateways:MoMo:AccessKey"] ?? throw new InvalidOperationException("MoMo AccessKey not configured");
                var secretKey = _configuration["PaymentGateways:MoMo:SecretKey"] ?? throw new InvalidOperationException("MoMo SecretKey not configured");
                var apiUrl = _configuration["PaymentGateways:MoMo:RefundApiUrl"] ?? "https://test-payment.momo.vn/v2/gateway/api/refund";

                var requestId = Guid.NewGuid().ToString();
                var orderId = transactionId;
                var transId = transactionId;
                var amountStr = ((long)amount).ToString();
                var description = reason;

                // Create signature
                var rawSignature = $"accessKey={accessKey}&amount={amountStr}&description={description}&orderId={orderId}&partnerCode={partnerCode}&requestId={requestId}&transId={transId}";
                var signature = ComputeHmacSha256(rawSignature, secretKey);

                var payload = new
                {
                    partnerCode,
                    accessKey,
                    requestId,
                    orderId,
                    transId,
                    amount = amountStr,
                    description,
                    signature,
                    lang = "vi"
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<MoMoRefundResponse>(responseContent);

                if (result?.ResultCode == 0)
                {
                    return new RefundResult
                    {
                        Success = true,
                        RefundId = result.TransId,
                        RefundDate = DateTime.Now
                    };
                }
                else
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = result?.Message ?? "MoMo refund failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo refund");
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to process MoMo refund: {ex.Message}"
                };
            }
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                var secretKey = _configuration["PaymentGateways:MoMo:SecretKey"] ?? throw new InvalidOperationException("MoMo SecretKey not configured");

                // Parse payload to extract fields for signature
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);
                if (data == null) return false;

                var accessKey = data.ContainsKey("accessKey") ? data["accessKey"].GetString() : "";
                var amount = data.ContainsKey("amount") ? data["amount"].ToString() : "";
                var extraData = data.ContainsKey("extraData") ? data["extraData"].GetString() : "";
                var message = data.ContainsKey("message") ? data["message"].GetString() : "";
                var orderId = data.ContainsKey("orderId") ? data["orderId"].GetString() : "";
                var orderInfo = data.ContainsKey("orderInfo") ? data["orderInfo"].GetString() : "";
                var orderType = data.ContainsKey("orderType") ? data["orderType"].GetString() : "";
                var partnerCode = data.ContainsKey("partnerCode") ? data["partnerCode"].GetString() : "";
                var payType = data.ContainsKey("payType") ? data["payType"].GetString() : "";
                var requestId = data.ContainsKey("requestId") ? data["requestId"].GetString() : "";
                var responseTime = data.ContainsKey("responseTime") ? data["responseTime"].ToString() : "";
                var resultCode = data.ContainsKey("resultCode") ? data["resultCode"].ToString() : "";
                var transId = data.ContainsKey("transId") ? data["transId"].ToString() : "";

                // Reconstruct signature string
                var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
                var computedSignature = ComputeHmacSha256(rawSignature, secretKey);

                return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying MoMo webhook signature");
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

        private string MapMoMoStatus(int resultCode)
        {
            return resultCode switch
            {
                0 => "Paid",
                1000 => "Pending",
                9000 => "Processing",
                _ => "Pending"
            };
        }

        private class MoMoResponse
        {
            public string? PartnerCode { get; set; }
            public string? RequestId { get; set; }
            public string? OrderId { get; set; }
            public long Amount { get; set; }
            public int? ResultCode { get; set; }
            public string? Message { get; set; }
            public string? PayUrl { get; set; }
        }

        private class MoMoStatusResponse
        {
            public string? OrderId { get; set; }
            public int? ResultCode { get; set; }
            public string? Message { get; set; }
            public long? TransId { get; set; }
            public decimal Amount { get; set; }
        }

        private class MoMoRefundResponse
        {
            public int ResultCode { get; set; }
            public string? Message { get; set; }
            public string? TransId { get; set; }
        }
    }
}

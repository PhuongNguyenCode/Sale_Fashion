using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FashionStore.Configuration;

namespace FashionStore.Services.Gateways
{
    /// <summary>
    /// Casso Payment Gateway - Auto bank transfer tracking with QR code
    /// Docs: https://docs.casso.vn/
    /// </summary>
    public class CassoPaymentGateway : IPaymentGateway
    {
        private readonly HttpClient _httpClient;
        private readonly CassoConfiguration _config;
        private readonly ILogger<CassoPaymentGateway> _logger;

        public string GatewayName => "Casso";

        public CassoPaymentGateway(
            HttpClient httpClient,
            IOptions<CassoConfiguration> config,
            ILogger<CassoPaymentGateway> logger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
        }

        public async Task<PaymentGatewayResult> CreatePaymentAsync(PaymentRequest request)
        {
            try
            {
                // Try creating official payOS payment request link if ClientId & ApiKey configured
                if (!string.IsNullOrEmpty(_config.ClientId) && !string.IsNullOrEmpty(_config.ApiKey))
                {
                    var payosResult = await CreatePayOSPaymentLinkAsync(request);
                    if (payosResult.Success)
                    {
                        return payosResult;
                    }
                    _logger.LogWarning($"Creating payOS payment link via API failed ({payosResult.ErrorMessage}), falling back to local VietQR");
                }

                // Fallback: Generate local VietQR code URL
                var qrCodeUrl = GenerateVietQRUrl(request);
                var transactionId = $"CASSO-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";

                _logger.LogInformation($"Created local Casso payment for order {request.OrderCode}");

                return new PaymentGatewayResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    QrCode = qrCodeUrl,
                    PaymentUrl = null,
                    AdditionalInfo = new Dictionary<string, string>
                    {
                        ["BankName"] = _config.BankName ?? "MB Bank",
                        ["AccountNumber"] = _config.AccountNumber ?? "0905313925",
                        ["AccountName"] = _config.AccountName ?? "NGUYEN PHUONG NGUYEN",
                        ["Branch"] = _config.Branch ?? "",
                        ["TransferContent"] = request.OrderCode,
                        ["Amount"] = request.Amount.ToString("N0"),
                        ["QRCode"] = qrCodeUrl
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Casso/payOS payment");
                return new PaymentGatewayResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create payment: {ex.Message}"
                };
            }
        }

        private async Task<PaymentGatewayResult> CreatePayOSPaymentLinkAsync(PaymentRequest request)
        {
            try
            {
                var apiUrl = (_config.ApiUrl ?? "https://api-merchant.payos.vn").TrimEnd('/');
                var url = $"{apiUrl}/v2/payment-requests";

                long payosOrderCode = GenerateNumericOrderCode(request.OrderCode);
                int amount = (int)Math.Round(request.Amount);

                string description = request.OrderCode;
                if (description.Length > 25)
                {
                    description = description.Substring(0, 25);
                }

                string returnUrl = request.ReturnUrl ?? "http://localhost:5244/payment/result";
                string cancelUrl = request.ReturnUrl ?? "http://localhost:5244/payment/result";

                var dataStr = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={payosOrderCode}&returnUrl={returnUrl}";

                string signature = "";
                if (!string.IsNullOrEmpty(_config.ChecksumKey))
                {
                    using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(_config.ChecksumKey));
                    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataStr));
                    signature = Convert.ToHexString(hashBytes).ToLowerInvariant();
                }

                var payloadObj = new
                {
                    orderCode = payosOrderCode,
                    amount = amount,
                    description = description,
                    cancelUrl = cancelUrl,
                    returnUrl = returnUrl,
                    signature = signature
                };

                var json = JsonSerializer.Serialize(payloadObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Headers.Add("x-client-id", _config.ClientId);
                httpRequest.Headers.Add("x-api-key", _config.ApiKey);
                httpRequest.Content = content;

                _logger.LogInformation($"Sending payOS CreatePaymentLink request for order {request.OrderCode} (payosOrderCode={payosOrderCode}, amount={amount})");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"payOS API error ({response.StatusCode}): {responseContent}");
                    return new PaymentGatewayResult { Success = false, ErrorMessage = $"payOS API Error: {response.StatusCode}" };
                }

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                var code = root.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : "";

                if (code != "00")
                {
                    var desc = root.TryGetProperty("desc", out var descEl) ? descEl.GetString() : "Unknown error";
                    _logger.LogError($"payOS create payment failed with code {code}: {desc}");
                    return new PaymentGatewayResult { Success = false, ErrorMessage = desc };
                }

                var data = root.GetProperty("data");
                var checkoutUrl = data.TryGetProperty("checkoutUrl", out var urlEl) ? urlEl.GetString() : null;
                var qrCode = data.TryGetProperty("qrCode", out var qrEl) ? qrEl.GetString() : null;
                var paymentLinkId = data.TryGetProperty("paymentLinkId", out var linkEl) ? linkEl.GetString() : payosOrderCode.ToString();

                _logger.LogInformation($"payOS Payment Link created successfully! linkId={paymentLinkId}, checkoutUrl={checkoutUrl}");

                var qrImageUrl = !string.IsNullOrEmpty(qrCode)
                    ? $"https://img.vietqr.io/image/{GetBankShortName(_config.BankName ?? "MB")}-{_config.AccountNumber}-compact2.jpg?amount={amount}&addInfo={Uri.EscapeDataString(description)}&accountName={Uri.EscapeDataString(_config.AccountName ?? "")}"
                    : GenerateVietQRUrl(request);

                return new PaymentGatewayResult
                {
                    Success = true,
                    TransactionId = paymentLinkId,
                    PaymentUrl = checkoutUrl,
                    QrCode = qrImageUrl,
                    AdditionalInfo = new Dictionary<string, string>
                    {
                        ["BankName"] = _config.BankName ?? "MB Bank",
                        ["AccountNumber"] = _config.AccountNumber ?? "0905313925",
                        ["AccountName"] = _config.AccountName ?? "NGUYEN PHUONG NGUYEN",
                        ["Branch"] = _config.Branch ?? "",
                        ["TransferContent"] = description,
                        ["Amount"] = amount.ToString("N0"),
                        ["QRCode"] = qrImageUrl,
                        ["CheckoutUrl"] = checkoutUrl ?? ""
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling payOS CreatePaymentLink API");
                return new PaymentGatewayResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private long GenerateNumericOrderCode(string orderCodeStr)
        {
            try
            {
                var digitsOnly = new string(orderCodeStr.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length > 12)
                {
                    digitsOnly = digitsOnly.Substring(digitsOnly.Length - 12);
                }
                if (long.TryParse(digitsOnly, out long result) && result > 0)
                {
                    return result;
                }
            }
            catch { }
            return DateTime.Now.Ticks % 900000000000000L + 100000000000000L;
        }

        public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.ApiKey))
                {
                    return new PaymentStatusResult
                    {
                        Success = false,
                        Status = "Pending",
                        ErrorMessage = "Casso API key not configured"
                    };
                }

                // Query Casso API for recent transactions
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_config.ApiUrl}/v2/transactions");
                request.Headers.Add("Authorization", $"Apikey {_config.ApiKey}");
                request.Headers.Add("secure-token", _config.SecureToken);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Casso API error: {content}");
                    return new PaymentStatusResult
                    {
                        Success = false,
                        Status = "Pending",
                        ErrorMessage = "Failed to query Casso API"
                    };
                }

                // Parse response and look for matching transaction
                // Note: In production, you should store the order code and match against description
                return new PaymentStatusResult
                {
                    Success = true,
                    Status = "Pending", // Will be updated by webhook
                    TransactionId = transactionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Casso payment status");
                return new PaymentStatusResult
                {
                    Success = false,
                    Status = "Pending",
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
        {
            // Bank transfer refunds are manual
            return Task.FromResult(new RefundResult
            {
                Success = true,
                RefundId = $"REF-{transactionId}",
                RefundDate = DateTime.Now,
                ErrorMessage = "Casso refunds must be processed manually by transferring funds back to customer"
            });
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.SecureToken))
                {
                    _logger.LogWarning("Casso secure token not configured");
                    return false;
                }

                // Casso uses secure-token in header for authentication
                // Signature verification is done in the webhook handler
                return !string.IsNullOrWhiteSpace(signature) && signature == _config.SecureToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Casso webhook signature");
                return false;
            }
        }

        private string GenerateVietQRUrl(PaymentRequest request)
        {
            // VietQR format
            var bankShortName = GetBankShortName(_config.BankName ?? "VCB");
            var accountNumber = _config.AccountNumber ?? "";
            var accountName = _config.AccountName ?? "";
            var amount = request.Amount;
            var description = request.OrderCode;

            // VietQR API: https://api.vietqr.io/
            return $"https://img.vietqr.io/image/{bankShortName}-{accountNumber}-compact2.jpg?amount={amount:F0}&addInfo={Uri.EscapeDataString(description)}&accountName={Uri.EscapeDataString(accountName)}";
        }

        private string GetBankShortName(string bankName)
        {
            // Map bank names to VietQR short codes
            var bankCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Vietcombank"] = "VCB",
                ["VCB"] = "VCB",
                ["Techcombank"] = "TCB",
                ["TCB"] = "TCB",
                ["BIDV"] = "BIDV",
                ["Agribank"] = "Agribank",
                ["VietinBank"] = "CTG",
                ["CTG"] = "CTG",
                ["MB Bank"] = "MB",
                ["MB"] = "MB",
                ["MBBank"] = "MB",
                ["ACB"] = "ACB",
                ["VPBank"] = "VPB",
                ["VPB"] = "VPB",
                ["Sacombank"] = "STB",
                ["STB"] = "STB",
                ["HDBank"] = "HDB",
                ["HDB"] = "HDB",
                ["VIB"] = "VIB",
                ["SHB"] = "SHB",
                ["TPBank"] = "TPB",
                ["TPB"] = "TPB"
            };

            return bankCodes.TryGetValue(bankName, out var code) ? code : "VCB";
        }
    }
}

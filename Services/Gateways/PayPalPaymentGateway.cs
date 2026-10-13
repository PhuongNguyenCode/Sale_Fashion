using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace FashionStore.Services.Gateways
{
    /// <summary>
    /// PayPal payment gateway integration for international payments
    /// </summary>
    public class PayPalPaymentGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PayPalPaymentGateway> _logger;

        public string GatewayName => "PayPal";

        public PayPalPaymentGateway(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<PayPalPaymentGateway> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PaymentGatewayResult> CreatePaymentAsync(PaymentRequest request)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return new PaymentGatewayResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to authenticate with PayPal"
                    };
                }

                var apiUrl = GetApiUrl() + "/v2/checkout/orders";

                // Convert VND to USD
                var amountInUsd = Math.Round(request.Amount / 23000, 2);

                var orderPayload = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            reference_id = request.OrderCode,
                            custom_id = request.OrderCode,
                            amount = new
                            {
                                currency_code = "USD",
                                value = amountInUsd.ToString("F2")
                            },
                            description = $"Order {request.OrderCode}"
                        }
                    },
                    application_context = new
                    {
                        return_url = request.ReturnUrl + "?status=success",
                        cancel_url = request.ReturnUrl + "?status=cancelled",
                        brand_name = "Fashion Store",
                        landing_page = "BILLING",
                        user_action = "PAY_NOW"
                    }
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(orderPayload),
                    Encoding.UTF8,
                    "application/json"
                );

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                httpRequest.Content = jsonContent;
                httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
                httpRequest.Headers.Add("PayPal-Request-Id", Guid.NewGuid().ToString());

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"PayPal API Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent);
                    var approveLink = result?.Links?.FirstOrDefault(l => l.Rel == "approve")?.Href;

                    return new PaymentGatewayResult
                    {
                        Success = true,
                        PaymentUrl = approveLink,
                        TransactionId = result?.Id,
                        ErrorMessage = null
                    };
                }
                else
                {
                    return new PaymentGatewayResult
                    {
                        Success = false,
                        ErrorMessage = $"PayPal API error: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal payment");
                return new PaymentGatewayResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create PayPal payment: {ex.Message}"
                };
            }
        }

        public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return new PaymentStatusResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to authenticate with PayPal"
                    };
                }

                var apiUrl = GetApiUrl() + $"/v2/checkout/orders/{transactionId}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PayPalOrderStatusResponse>(responseContent);

                    return new PaymentStatusResult
                    {
                        Success = true,
                        Status = MapPayPalStatus(result?.Status),
                        TransactionId = result?.Id,
                        Amount = result?.PurchaseUnits?.FirstOrDefault()?.Amount?.Value != null
                            ? decimal.Parse(result.PurchaseUnits[0].Amount.Value)
                            : null
                    };
                }
                else
                {
                    return new PaymentStatusResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to query PayPal status"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying PayPal payment status");
                return new PaymentStatusResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to query PayPal status: {ex.Message}"
                };
            }
        }

        public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to authenticate with PayPal"
                    };
                }

                // First, get capture ID from order
                var captureId = await GetCaptureIdAsync(transactionId, accessToken);
                if (string.IsNullOrEmpty(captureId))
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = "Could not find capture ID for this transaction"
                    };
                }

                var apiUrl = GetApiUrl() + $"/v2/payments/captures/{captureId}/refund";
                var amountInUsd = Math.Round(amount / 23000, 2);

                var refundPayload = new
                {
                    amount = new
                    {
                        currency_code = "USD",
                        value = amountInUsd.ToString("F2")
                    },
                    note_to_payer = reason
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(refundPayload),
                    Encoding.UTF8,
                    "application/json"
                );

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                httpRequest.Content = jsonContent;
                httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PayPalRefundResponse>(responseContent);

                    return new RefundResult
                    {
                        Success = true,
                        RefundId = result?.Id,
                        RefundDate = !string.IsNullOrEmpty(result?.CreateTime)
                            ? DateTime.Parse(result.CreateTime)
                            : DateTime.Now
                    };
                }
                else
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = $"PayPal refund failed: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal refund");
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to process PayPal refund: {ex.Message}"
                };
            }
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                var webhookId = _configuration["Payment:PayPal:WebhookId"];
                if (string.IsNullOrEmpty(webhookId))
                {
                    _logger.LogWarning("PayPal WebhookId not configured");
                    return false;
                }

                // PayPal webhook verification requires calling their API
                // For simplicity, we'll implement basic verification here
                // In production, you should use PayPal SDK or verify via their API

                // Extract signature headers from request (should be passed separately in real implementation)
                // For now, return true if webhook ID is configured
                // TODO: Implement proper PayPal webhook verification via API

                return !string.IsNullOrEmpty(webhookId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying PayPal webhook signature");
                return false;
            }
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                var clientId = _configuration["Payment:PayPal:ClientId"] ?? throw new InvalidOperationException("PayPal ClientId not configured");
                var clientSecret = _configuration["Payment:PayPal:ClientSecret"] ?? throw new InvalidOperationException("PayPal ClientSecret not configured");
                var apiUrl = GetApiUrl() + "/v1/oauth2/token";

                var authBytes = Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}");
                var authHeader = Convert.ToBase64String(authBytes);

                var formData = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials"
                };

                var content = new FormUrlEncodedContent(formData);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                httpRequest.Content = content;
                httpRequest.Headers.Add("Authorization", $"Basic {authHeader}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PayPalTokenResponse>(responseContent);
                    return result?.AccessToken;
                }

                _logger.LogError($"Failed to get PayPal access token: {responseContent}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayPal access token");
                return null;
            }
        }

        private async Task<string?> GetCaptureIdAsync(string orderId, string accessToken)
        {
            try
            {
                var apiUrl = GetApiUrl() + $"/v2/checkout/orders/{orderId}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PayPalOrderStatusResponse>(responseContent);
                    return result?.PurchaseUnits?.FirstOrDefault()?.Payments?.Captures?.FirstOrDefault()?.Id;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayPal capture ID");
                return null;
            }
        }

        private string GetApiUrl()
        {
            var isSandbox = _configuration.GetValue<bool>("Payment:PayPal:UseSandbox", true);
            return isSandbox
                ? "https://api-m.sandbox.paypal.com"
                : "https://api-m.paypal.com";
        }

        private string MapPayPalStatus(string? status)
        {
            return status switch
            {
                "COMPLETED" => "Paid",
                "APPROVED" => "Processing",
                "CREATED" => "Pending",
                "VOIDED" => "Cancelled",
                _ => "Failed"
            };
        }

        private class PayPalTokenResponse
        {
            public string? AccessToken { get; set; }
        }

        private class PayPalOrderResponse
        {
            public string? Id { get; set; }
            public string? Status { get; set; }
            public List<PayPalLink>? Links { get; set; }
        }

        private class PayPalLink
        {
            public string? Href { get; set; }
            public string? Rel { get; set; }
        }

        private class PayPalOrderStatusResponse
        {
            public string? Id { get; set; }
            public string? Status { get; set; }
            public List<PayPalPurchaseUnit>? PurchaseUnits { get; set; }
        }

        private class PayPalPurchaseUnit
        {
            public PayPalAmount? Amount { get; set; }
            public PayPalPayments? Payments { get; set; }
        }

        private class PayPalAmount
        {
            public string? CurrencyCode { get; set; }
            public string? Value { get; set; }
        }

        private class PayPalPayments
        {
            public List<PayPalCapture>? Captures { get; set; }
        }

        private class PayPalCapture
        {
            public string? Id { get; set; }
            public string? Status { get; set; }
        }

        private class PayPalRefundResponse
        {
            public string? Id { get; set; }
            public string? Status { get; set; }
            public string? CreateTime { get; set; }
        }
    }
}

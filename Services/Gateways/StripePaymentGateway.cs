using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace FashionStore.Services.Gateways
{
    /// <summary>
    /// Stripe payment gateway integration for international payments
    /// </summary>
    public class StripePaymentGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<StripePaymentGateway> _logger;

        public string GatewayName => "Stripe";

        public StripePaymentGateway(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<StripePaymentGateway> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PaymentGatewayResult> CreatePaymentAsync(PaymentRequest request)
        {
            try
            {
                var secretKey = _configuration["Payment:Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe SecretKey not configured");
                var apiUrl = "https://api.stripe.com/v1/checkout/sessions";

                // Convert VND to cents (Stripe uses smallest currency unit)
                var amountInCents = (long)(request.Amount / 23000 * 100); // Approximate VND to USD conversion

                // Build form data for Stripe API
                var formData = new Dictionary<string, string>
                {
                    ["mode"] = "payment",
                    ["success_url"] = request.ReturnUrl + "?session_id={CHECKOUT_SESSION_ID}",
                    ["cancel_url"] = request.ReturnUrl + "?cancelled=true",
                    ["line_items[0][price_data][currency]"] = "usd",
                    ["line_items[0][price_data][product_data][name]"] = $"Order {request.OrderCode}",
                    ["line_items[0][price_data][unit_amount]"] = amountInCents.ToString(),
                    ["line_items[0][quantity]"] = "1",
                    ["metadata[order_code]"] = request.OrderCode,
                    ["metadata[original_amount]"] = request.Amount.ToString(),
                    ["metadata[currency]"] = request.Currency
                };

                var content = new FormUrlEncodedContent(formData);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                httpRequest.Content = content;
                httpRequest.Headers.Add("Authorization", $"Bearer {secretKey}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Stripe API Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<StripeSessionResponse>(responseContent);

                    return new PaymentGatewayResult
                    {
                        Success = true,
                        PaymentUrl = result?.Url,
                        TransactionId = result?.Id,
                        ErrorMessage = null
                    };
                }
                else
                {
                    var error = JsonSerializer.Deserialize<StripeErrorResponse>(responseContent);
                    return new PaymentGatewayResult
                    {
                        Success = false,
                        ErrorMessage = error?.Error?.Message ?? "Stripe API error"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe payment");
                return new PaymentGatewayResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create Stripe payment: {ex.Message}"
                };
            }
        }

        public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                var secretKey = _configuration["Payment:Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe SecretKey not configured");
                var apiUrl = $"https://api.stripe.com/v1/checkout/sessions/{transactionId}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                httpRequest.Headers.Add("Authorization", $"Bearer {secretKey}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<StripeSessionStatusResponse>(responseContent);

                    return new PaymentStatusResult
                    {
                        Success = true,
                        Status = MapStripeStatus(result?.PaymentStatus),
                        TransactionId = result?.PaymentIntent,
                        Amount = result?.AmountTotal.HasValue == true ? result.AmountTotal.Value / 100m : null
                    };
                }
                else
                {
                    return new PaymentStatusResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to query Stripe status"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying Stripe payment status");
                return new PaymentStatusResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to query Stripe status: {ex.Message}"
                };
            }
        }

        public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
        {
            try
            {
                var secretKey = _configuration["Payment:Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe SecretKey not configured");
                var apiUrl = "https://api.stripe.com/v1/refunds";

                var amountInCents = (long)(amount / 23000 * 100); // Convert VND to USD cents

                var formData = new Dictionary<string, string>
                {
                    ["payment_intent"] = transactionId,
                    ["amount"] = amountInCents.ToString(),
                    ["reason"] = "requested_by_customer",
                    ["metadata[refund_reason]"] = reason
                };

                var content = new FormUrlEncodedContent(formData);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                httpRequest.Content = content;
                httpRequest.Headers.Add("Authorization", $"Bearer {secretKey}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<StripeRefundResponse>(responseContent);

                    return new RefundResult
                    {
                        Success = true,
                        RefundId = result?.Id,
                        RefundDate = result?.Created.HasValue == true 
                            ? DateTimeOffset.FromUnixTimeSeconds(result.Created.Value).DateTime 
                            : DateTime.Now
                    };
                }
                else
                {
                    var error = JsonSerializer.Deserialize<StripeErrorResponse>(responseContent);
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = error?.Error?.Message ?? "Stripe refund failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe refund");
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to process Stripe refund: {ex.Message}"
                };
            }
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                var webhookSecret = _configuration["Payment:Stripe:WebhookSecret"] ?? throw new InvalidOperationException("Stripe WebhookSecret not configured");

                // Stripe signature format: t=timestamp,v1=signature
                var signatureParts = signature.Split(',');
                var timestamp = "";
                var actualSignature = "";

                foreach (var part in signatureParts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        if (keyValue[0] == "t") timestamp = keyValue[1];
                        if (keyValue[0] == "v1") actualSignature = keyValue[1];
                    }
                }

                if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(actualSignature))
                    return false;

                // Compute expected signature
                var signedPayload = $"{timestamp}.{payload}";
                var expectedSignature = ComputeHmacSha256(signedPayload, webhookSecret);

                // Constant-time comparison
                return ConstantTimeEquals(expectedSignature, actualSignature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Stripe webhook signature");
                return false;
            }
        }

        private string ComputeHmacSha256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private bool ConstantTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;

            var result = 0;
            for (var i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }

        private string MapStripeStatus(string? status)
        {
            return status switch
            {
                "paid" => "Paid",
                "unpaid" => "Pending",
                "no_payment_required" => "Paid",
                _ => "Failed"
            };
        }

        private class StripeSessionResponse
        {
            public string? Id { get; set; }
            public string? Url { get; set; }
        }

        private class StripeSessionStatusResponse
        {
            public string? Id { get; set; }
            public string? PaymentStatus { get; set; }
            public string? PaymentIntent { get; set; }
            public long? AmountTotal { get; set; }
        }

        private class StripeRefundResponse
        {
            public string? Id { get; set; }
            public long? Created { get; set; }
            public string? Status { get; set; }
        }

        private class StripeErrorResponse
        {
            public StripeError? Error { get; set; }
        }

        private class StripeError
        {
            public string? Message { get; set; }
            public string? Type { get; set; }
        }
    }
}

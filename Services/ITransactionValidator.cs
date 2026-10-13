using FashionStore.Models;

namespace FashionStore.Services
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public Order? Order { get; set; }
        public Payment? Payment { get; set; }
    }

    public class WebhookPayload
    {
        public string TransactionId { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string Status { get; set; } = string.Empty;
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public object? RawResponse { get; set; }
    }

    public interface ITransactionValidator
    {
        /// <summary>
        /// Verify webhook signature using HMAC-SHA256
        /// </summary>
        /// <param name="gateway">Gateway name (MoMo, ZaloPay, Stripe, PayPal)</param>
        /// <param name="payload">Raw payload string</param>
        /// <param name="signature">Signature to verify</param>
        /// <param name="secretKey">Secret key for the gateway</param>
        /// <returns>True if signature is valid</returns>
        bool VerifySignature(string gateway, string payload, string signature, string secretKey);

        /// <summary>
        /// Validate webhook payload (amount, order existence, payment existence)
        /// </summary>
        /// <param name="payload">Webhook payload</param>
        /// <returns>Validation result with order and payment if found</returns>
        Task<ValidationResult> ValidateWebhookAsync(WebhookPayload payload);
    }
}

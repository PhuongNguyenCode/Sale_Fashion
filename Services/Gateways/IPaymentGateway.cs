namespace FashionStore.Services.Gateways
{
    public class PaymentRequest
    {
        public string OrderCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string ReturnUrl { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    public class PaymentGatewayResult
    {
        public bool Success { get; set; }
        public string? PaymentUrl { get; set; }
        public string? QrCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TransactionId { get; set; }
        public Dictionary<string, string> AdditionalInfo { get; set; } = new Dictionary<string, string>();
    }

    public class PaymentStatusResult
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public decimal? Amount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RefundResult
    {
        public bool Success { get; set; }
        public string? RefundId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? RefundDate { get; set; }
    }

    public interface IPaymentGateway
    {
        string GatewayName { get; }

        /// <summary>
        /// Create payment request with the gateway
        /// </summary>
        Task<PaymentGatewayResult> CreatePaymentAsync(PaymentRequest request);

        /// <summary>
        /// Query payment status from gateway
        /// </summary>
        Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId);

        /// <summary>
        /// Process refund through gateway
        /// </summary>
        Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason);

        /// <summary>
        /// Verify webhook signature from gateway
        /// </summary>
        bool VerifyWebhookSignature(string payload, string signature);
    }
}

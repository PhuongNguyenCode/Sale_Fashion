namespace FashionStore.Services.Gateways
{
    /// <summary>
    /// Cash on Delivery payment gateway - no external API calls
    /// </summary>
    public class CodPaymentGateway : IPaymentGateway
    {
        public string GatewayName => "COD";

        public Task<PaymentGatewayResult> CreatePaymentAsync(PaymentRequest request)
        {
            // COD requires no gateway interaction
            // Payment is confirmed manually when delivery is completed
            return Task.FromResult(new PaymentGatewayResult
            {
                Success = true,
                TransactionId = $"COD-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}",
                PaymentUrl = null, // No redirect needed
                ErrorMessage = null
            });
        }

        public Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId)
        {
            // COD payments are tracked internally, not via gateway
            return Task.FromResult(new PaymentStatusResult
            {
                Success = true,
                Status = "Pending", // Default status, manually updated
                TransactionId = transactionId
            });
        }

        public Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
        {
            // COD refunds are handled manually/offline
            return Task.FromResult(new RefundResult
            {
                Success = true,
                RefundId = $"REF-{transactionId}",
                RefundDate = DateTime.Now,
                ErrorMessage = null
            });
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            // COD has no webhooks
            return false;
        }
    }
}

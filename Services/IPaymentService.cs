using FashionStore.Models;
using FashionStore.Services.Gateways;

namespace FashionStore.Services
{
    public interface IPaymentService
    {
        /// <summary>
        /// Create a new payment record
        /// </summary>
        Task<Payment> CreatePaymentAsync(int orderId, string paymentMethod, decimal amount, string? gateway = null);

        /// <summary>
        /// Initiate payment with gateway and get payment URL
        /// </summary>
        Task<PaymentGatewayResult> InitiateGatewayPaymentAsync(Payment payment, string returnUrl, string webhookUrl);

        /// <summary>
        /// Update payment status with optimistic locking
        /// </summary>
        Task<bool> UpdatePaymentStatusAsync(int paymentId, string status, string? transactionId = null);

        /// <summary>
        /// Record a transaction from gateway
        /// </summary>
        Task<PaymentTransaction> RecordTransactionAsync(int paymentId, string gateway, string transactionId, decimal amount, string status, object? response = null);

        /// <summary>
        /// Process refund through gateway
        /// </summary>
        Task<RefundResult> ProcessRefundAsync(int paymentId, string reason);

        /// <summary>
        /// Get payment by ID
        /// </summary>
        Task<Payment?> GetPaymentByIdAsync(int paymentId);

        /// <summary>
        /// Get payment by order ID
        /// </summary>
        Task<Payment?> GetPaymentByOrderIdAsync(int orderId);
    }
}

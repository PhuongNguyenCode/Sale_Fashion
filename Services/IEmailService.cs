namespace FashionStore.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Send email notification
        /// </summary>
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);

        /// <summary>
        /// Send payment success email
        /// </summary>
        Task<bool> SendPaymentSuccessEmailAsync(string email, string orderCode, decimal amount, string paymentMethod);

        /// <summary>
        /// Send payment failure email
        /// </summary>
        Task<bool> SendPaymentFailureEmailAsync(string email, string orderCode, string reason);

        /// <summary>
        /// Send payment pending email (for bank transfer)
        /// </summary>
        Task<bool> SendPaymentPendingEmailAsync(string email, string orderCode, decimal amount, Dictionary<string, string>? bankInfo = null);
    }
}

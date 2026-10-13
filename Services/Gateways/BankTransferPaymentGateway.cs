using Microsoft.Extensions.Configuration;

namespace FashionStore.Services.Gateways
{
    /// <summary>
    /// Bank Transfer payment gateway - displays bank account information and QR code
    /// </summary>
    public class BankTransferPaymentGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;

        public string GatewayName => "BankTransfer";

        public BankTransferPaymentGateway(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<PaymentGatewayResult> CreatePaymentAsync(PaymentRequest request)
        {
            // Generate bank transfer information
            var bankName = _configuration["Payment:BankTransfer:BankName"] ?? "Vietcombank";
            var accountNumber = _configuration["Payment:BankTransfer:AccountNumber"] ?? "1234567890";
            var accountName = _configuration["Payment:BankTransfer:AccountName"] ?? "FASHION STORE";
            var branch = _configuration["Payment:BankTransfer:Branch"] ?? "HCMC Branch";

            // Generate QR code URL (using VietQR standard)
            var qrContent = $"BANK={bankName}&ACC={accountNumber}&NAME={accountName}&AMOUNT={request.Amount:F0}&DESC={request.OrderCode}";
            var qrCodeUrl = $"https://api.vietqr.io/image/{bankName}-{accountNumber}-compact2.jpg?amount={request.Amount:F0}&addInfo={request.OrderCode}";

            var transactionId = $"BANK-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";

            return Task.FromResult(new PaymentGatewayResult
            {
                Success = true,
                TransactionId = transactionId,
                QrCode = qrCodeUrl,
                PaymentUrl = null, // No redirect, display info on result page
                AdditionalInfo = new Dictionary<string, string>
                {
                    ["BankName"] = bankName,
                    ["AccountNumber"] = accountNumber,
                    ["AccountName"] = accountName,
                    ["Branch"] = branch,
                    ["TransferContent"] = request.OrderCode,
                    ["Amount"] = request.Amount.ToString("N0")
                }
            });
        }

        public Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId)
        {
            // Bank transfer payments must be confirmed manually by admin
            return Task.FromResult(new PaymentStatusResult
            {
                Success = true,
                Status = "Pending",
                TransactionId = transactionId
            });
        }

        public Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
        {
            // Bank transfer refunds are handled manually/offline
            return Task.FromResult(new RefundResult
            {
                Success = true,
                RefundId = $"REF-{transactionId}",
                RefundDate = DateTime.Now,
                ErrorMessage = "Bank transfer refunds must be processed manually by transferring funds back to customer"
            });
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            // Bank transfer has no automated webhooks
            return false;
        }
    }
}

using System.Net;
using System.Net.Mail;

namespace FashionStore.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
                var smtpUser = _configuration["Email:SmtpUser"];
                var smtpPassword = _configuration["Email:SmtpPassword"];
                var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
                var fromName = _configuration["Email:FromName"] ?? "Fashion Store";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("Email configuration is incomplete. Email not sent.");
                    return false;
                }

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail!, fromName);
                message.To.Add(to);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = isHtml;

                using var smtpClient = new SmtpClient(smtpHost, smtpPort);
                smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
                smtpClient.EnableSsl = true;

                await smtpClient.SendMailAsync(message);

                _logger.LogInformation($"Email sent successfully to {to}: {subject}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}: {subject}");
                return false;
            }
        }

        public async Task<bool> SendPaymentSuccessEmailAsync(string email, string orderCode, decimal amount, string paymentMethod)
        {
            var subject = $"Thanh toán thành công - Đơn hàng {orderCode}";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        .order-info {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #28a745; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>✓ Thanh toán thành công</h2>
                        </div>
                        <div class='content'>
                            <p>Xin chào,</p>
                            <p>Chúng tôi xác nhận đã nhận được thanh toán của bạn cho đơn hàng <strong>{orderCode}</strong>.</p>
                            
                            <div class='order-info'>
                                <p><strong>Mã đơn hàng:</strong> {orderCode}</p>
                                <p><strong>Số tiền:</strong> {amount:N0} VNĐ</p>
                                <p><strong>Phương thức:</strong> {paymentMethod}</p>
                                <p><strong>Trạng thái:</strong> <span style='color: #28a745;'>Đã thanh toán</span></p>
                            </div>

                            <p>Đơn hàng của bạn đang được xử lý và sẽ sớm được giao đến bạn.</p>
                            <p>Cảm ơn bạn đã mua sắm tại Fashion Store!</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 Fashion Store. All rights reserved.</p>
                            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(email, subject, body);
        }

        public async Task<bool> SendPaymentFailureEmailAsync(string email, string orderCode, string reason)
        {
            var subject = $"Thanh toán thất bại - Đơn hàng {orderCode}";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        .error-box {{ background-color: #fff3cd; padding: 15px; margin: 15px 0; border-left: 4px solid #dc3545; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>✗ Thanh toán thất bại</h2>
                        </div>
                        <div class='content'>
                            <p>Xin chào,</p>
                            <p>Rất tiếc, thanh toán cho đơn hàng <strong>{orderCode}</strong> của bạn không thành công.</p>
                            
                            <div class='error-box'>
                                <p><strong>Lý do:</strong> {reason}</p>
                            </div>

                            <p>Vui lòng thử lại hoặc chọn phương thức thanh toán khác.</p>
                            <p>Nếu bạn cần hỗ trợ, vui lòng liên hệ với chúng tôi.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 Fashion Store. All rights reserved.</p>
                            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(email, subject, body);
        }

        public async Task<bool> SendPaymentPendingEmailAsync(string email, string orderCode, decimal amount, Dictionary<string, string>? bankInfo = null)
        {
            var subject = $"Hướng dẫn thanh toán - Đơn hàng {orderCode}";
            
            var bankInfoHtml = "";
            if (bankInfo != null && bankInfo.Any())
            {
                bankInfoHtml = $@"
                    <div class='bank-info'>
                        <h3>Thông tin chuyển khoản:</h3>
                        <p><strong>Ngân hàng:</strong> {bankInfo.GetValueOrDefault("BankName", "")}</p>
                        <p><strong>Số tài khoản:</strong> {bankInfo.GetValueOrDefault("AccountNumber", "")}</p>
                        <p><strong>Tên tài khoản:</strong> {bankInfo.GetValueOrDefault("AccountName", "")}</p>
                        <p><strong>Chi nhánh:</strong> {bankInfo.GetValueOrDefault("Branch", "")}</p>
                        <p><strong>Số tiền:</strong> {amount:N0} VNĐ</p>
                        <p><strong>Nội dung chuyển khoản:</strong> {orderCode}</p>
                    </div>
                ";
            }

            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #ffc107; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        .bank-info {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #ffc107; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>⏳ Đang chờ thanh toán</h2>
                        </div>
                        <div class='content'>
                            <p>Xin chào,</p>
                            <p>Cảm ơn bạn đã đặt hàng tại Fashion Store. Đơn hàng <strong>{orderCode}</strong> của bạn đang chờ thanh toán.</p>
                            
                            {bankInfoHtml}

                            <p><strong>Lưu ý:</strong> Vui lòng chuyển khoản đúng số tiền và ghi đúng nội dung để đơn hàng được xác nhận nhanh chóng.</p>
                            <p>Sau khi chuyển khoản thành công, đơn hàng của bạn sẽ được xử lý trong vòng 24 giờ.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 Fashion Store. All rights reserved.</p>
                            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(email, subject, body);
        }
    }
}

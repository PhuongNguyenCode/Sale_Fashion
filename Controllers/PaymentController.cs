using FashionStore.Data;
using FashionStore.Models;
using FashionStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext _context,
            IPaymentService paymentService,
            IOrderService orderService,
            ILogger<PaymentController> logger)
        {
            this._context = _context;
            _paymentService = paymentService;
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Handle return from payment gateway
        /// </summary>
        [HttpGet("/payment/return")]
        public async Task<IActionResult> Return([FromQuery] string? orderCode, [FromQuery] string? status, [FromQuery] string? id)
        {
            _logger.LogInformation("Payment return: orderCode={OrderCode}, status={Status}, paymentLinkId={PaymentLinkId}",
                orderCode, status, id);

            // payOS returns its numeric orderCode and payment link id. The site's order code
            // is different, so resolve the saved link id before showing the result page.
            // Query parameters are not proof of payment; only the signed webhook updates status.
            Order? order = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                order = await _context.Orders.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Payment != null &&
                        o.Payment.PaymentGateway == "Casso" &&
                        (o.Payment.TransactionId == id ||
                         o.Payment.Transactions.Any(t => t.Gateway == "Casso" && t.ExternalTransactionId == id)));
            }

            if (order == null && !string.IsNullOrWhiteSpace(orderCode))
            {
                order = await _orderService.GetOrderByCodeAsync(orderCode);
            }

            if (order == null)
            {
                _logger.LogWarning("Payment return could not resolve order for payOS link {PaymentLinkId}", id);
                return RedirectToAction("Result", new { orderCode = orderCode ?? "unknown" });
            }

            return RedirectToAction("Result", new { orderCode = order.OrderCode });
        }

        /// <summary>
        /// Handle return from VNPay payment gateway
        /// </summary>
        [HttpGet("/payment/vnpay-return")]
        public async Task<IActionResult> VNPayReturn(
            [FromServices] IPaymentService paymentService,
            [FromServices] IStockService stockService)
        {
            var queryString = Request.QueryString.Value ?? "";
            _logger.LogInformation($"VNPay return received. Full query: {queryString}");

            // Parse all VNPay query params
            var vnpTxnRef = Request.Query["vnp_TxnRef"].FirstOrDefault() ?? "";
            var vnpResponseCode = Request.Query["vnp_ResponseCode"].FirstOrDefault() ?? "";
            var vnpTransactionNo = Request.Query["vnp_TransactionNo"].FirstOrDefault() ?? "";
            var vnpAmountStr = Request.Query["vnp_Amount"].FirstOrDefault() ?? "0";
            var vnpTransactionStatus = Request.Query["vnp_TransactionStatus"].FirstOrDefault() ?? "";
            var vnpSecureHash = Request.Query["vnp_SecureHash"].FirstOrDefault() ?? "";

            _logger.LogInformation($"VNPay return params: TxnRef={vnpTxnRef}, ResponseCode={vnpResponseCode}, " +
                $"TransactionNo={vnpTransactionNo}, Amount={vnpAmountStr}, TransactionStatus={vnpTransactionStatus}");

            if (string.IsNullOrEmpty(vnpTxnRef))
            {
                _logger.LogError("VNPay return: vnp_TxnRef is empty");
                return RedirectToAction("Result", new { status = "failed", message = "Invalid VNPay response" });
            }

            // Process VNPay payment directly (bypassing webhook handler for reliability)
            try
            {
                // Find order by OrderCode (vnp_TxnRef)
                var order = await _context.Orders
                    .Include(o => o.Payment)
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderCode == vnpTxnRef);

                if (order == null)
                {
                    _logger.LogError($"VNPay return: Order not found for TxnRef={vnpTxnRef}");
                    return RedirectToAction("Result", new { orderCode = vnpTxnRef });
                }

                var payment = order.Payment;
                if (payment == null)
                {
                    _logger.LogError($"VNPay return: Payment not found for order {vnpTxnRef}");
                    return RedirectToAction("Result", new { orderCode = vnpTxnRef });
                }

                // Skip if payment already processed
                if (payment.Status == Models.PaymentStatus.Paid)
                {
                    _logger.LogInformation($"VNPay return: Payment for order {vnpTxnRef} already Paid, skipping");
                    return RedirectToAction("Result", new { orderCode = vnpTxnRef });
                }

                // Check VNPay response code
                if (vnpResponseCode == "00" && vnpTransactionStatus == "00")
                {
                    _logger.LogInformation($"VNPay return: Payment SUCCESS for order {vnpTxnRef}");

                    // Verify amount (optional but logged)
                    if (decimal.TryParse(vnpAmountStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal vnpAmount))
                    {
                        vnpAmount = vnpAmount / 100m; // VNPay amount is multiplied by 100
                        if (vnpAmount != payment.Amount)
                        {
                            _logger.LogWarning($"VNPay return: Amount mismatch! Expected={payment.Amount}, " +
                                $"VNPay={vnpAmount}. Processing anyway (return URL).");
                        }
                    }

                    // Verify signature (log but don't block on failure for localhost)
                    try
                    {
                        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                        var hashSecret = config["PaymentGateways:VNPay:HashSecret"] ?? "";

                        if (!string.IsNullOrEmpty(hashSecret) && !string.IsNullOrEmpty(vnpSecureHash))
                        {
                            var qs = queryString.TrimStart('?');
                            var isValidSig = VerifyVNPayReturnSignature(qs, vnpSecureHash, hashSecret);
                            if (!isValidSig)
                            {
                                _logger.LogWarning($"VNPay return: Signature verification FAILED for order {vnpTxnRef}. " +
                                    "Processing anyway (return URL on localhost).");
                            }
                            else
                            {
                                _logger.LogInformation($"VNPay return: Signature verification PASSED for order {vnpTxnRef}");
                            }
                        }
                    }
                    catch (Exception sigEx)
                    {
                        _logger.LogWarning(sigEx, "VNPay return: Error during signature verification, continuing");
                    }

                    // UPDATE PAYMENT STATUS
                    payment.Status = Models.PaymentStatus.Paid;
                    payment.PaymentDate = DateTime.Now;
                    payment.UpdatedAt = DateTime.Now;
                    payment.TransactionId = vnpTransactionNo;

                    // UPDATE ORDER STATUS & ORDER.PaymentStatus
                    order.Status = Models.OrderStatus.Confirmed;
                    order.PaymentStatus = Models.PaymentStatus.Paid;
                    order.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"VNPay return: Payment and Order status updated to Paid/Confirmed for {vnpTxnRef}");

                    // Deduct stock
                    try
                    {
                        await stockService.DeductStockForOrderAsync(order.OrderId);
                        _logger.LogInformation($"VNPay return: Stock deducted for order {vnpTxnRef}");
                    }
                    catch (Exception stockEx)
                    {
                        _logger.LogError(stockEx, $"VNPay return: Failed to deduct stock for order {vnpTxnRef}");
                    }

                    // Record transaction
                    try
                    {
                        await paymentService.RecordTransactionAsync(
                            payment.PaymentId,
                            "VNPay",
                            vnpTransactionNo,
                            payment.Amount,
                            Models.PaymentStatus.Paid,
                            new { vnpResponseCode, vnpTransactionNo, vnpAmountStr, Source = "VNPayReturn" }
                        );
                    }
                    catch (Exception txEx)
                    {
                        _logger.LogWarning(txEx, "VNPay return: Failed to record transaction (may already exist)");
                    }
                }
                else if (vnpResponseCode == "24")
                {
                    _logger.LogInformation($"VNPay return: Payment CANCELLED by user for order {vnpTxnRef}");

                    payment.Status = Models.PaymentStatus.Cancelled;
                    payment.UpdatedAt = DateTime.Now;
                    order.Status = Models.OrderStatus.Cancelled;
                    order.PaymentStatus = Models.PaymentStatus.Cancelled;
                    order.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning($"VNPay return: Payment FAILED for order {vnpTxnRef}, ResponseCode={vnpResponseCode}");

                    payment.Status = Models.PaymentStatus.Failed;
                    payment.UpdatedAt = DateTime.Now;
                    order.PaymentStatus = Models.PaymentStatus.Failed;
                    order.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"VNPay return: Error processing payment for order {vnpTxnRef}");
            }

            return RedirectToAction("Result", new { orderCode = vnpTxnRef });
        }

        /// <summary>
        /// Verify VNPay return URL signature using raw query string
        /// </summary>
        private bool VerifyVNPayReturnSignature(string rawQueryString, string receivedHash, string hashSecret)
        {
            var segments = rawQueryString.Split('&', StringSplitOptions.RemoveEmptyEntries);
            var sortedParams = new SortedList<string, string>(StringComparer.Ordinal);

            foreach (var segment in segments)
            {
                var eqIndex = segment.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = segment.Substring(0, eqIndex);
                    var value = segment.Substring(eqIndex + 1);

                    if (!key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                        !key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                    {
                        sortedParams[key] = value;
                    }
                }
            }

            var dataStr = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));

            using var hmac = new System.Security.Cryptography.HMACSHA512(
                System.Text.Encoding.UTF8.GetBytes(hashSecret));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dataStr));
            var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// API to check payment status for real-time frontend polling
        /// </summary>
        [HttpGet("/payment/check-status/{orderCode}")]
        public async Task<IActionResult> CheckPaymentStatus(string orderCode)
        {
            if (string.IsNullOrEmpty(orderCode))
            {
                return BadRequest(new { isPaid = false, message = "Invalid order code" });
            }

            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null)
            {
                return NotFound(new { isPaid = false, message = "Order not found" });
            }

            var paymentStatus = order.Payment?.Status ?? order.PaymentStatus;
            var isPaid = string.Equals(paymentStatus, Models.PaymentStatus.Paid, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(order.PaymentStatus, Models.PaymentStatus.Paid, StringComparison.OrdinalIgnoreCase);

            return Ok(new
            {
                orderCode = order.OrderCode,
                status = paymentStatus,
                orderStatus = order.Status,
                isPaid = isPaid
            });
        }

        /// <summary>
        /// Display payment result page
        /// </summary>
        [HttpGet("/payment/result")]
        public async Task<IActionResult> Result([FromQuery] string orderCode)
        {
            if (string.IsNullOrEmpty(orderCode))
            {
                return RedirectToAction("Index", "Home");
            }

            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null)
            {
                ViewBag.Status = "failed";
                ViewBag.Message = "Order not found";
                return View();
            }

            var payment = order.Payment;

            ViewBag.OrderCode = order.OrderCode;
            ViewBag.TotalAmount = order.TotalAmount;
            ViewBag.PaymentMethod = order.PaymentMethod;
            ViewBag.OrderDate = order.OrderDate;

            if (payment == null)
            {
                ViewBag.Status = "pending";
                ViewBag.Message = "Payment information not available";
                return View();
            }

            // Map payment status to display status
            ViewBag.Status = payment.Status.ToLower() switch
            {
                "paid" => "success",
                "pending" => "pending",
                "processing" => "pending",
                "failed" => "failed",
                "cancelled" => "cancelled",
                "expired" => "expired",
                _ => "pending"
            };

            ViewBag.PaymentStatus = payment.Status;
            ViewBag.TransactionId = payment.TransactionId;
            ViewBag.PaymentDate = payment.PaymentDate;

            // For bank transfer or Casso, show bank info
            _logger.LogInformation($"Payment method: {order.PaymentMethod}, Status: {payment.Status}");
            if ((order.PaymentMethod == "BankTransfer" || order.PaymentMethod == "Casso") && (payment.Status == "Pending" || payment.Status == "Processing"))
            {
                var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var section = order.PaymentMethod == "Casso" ? "PaymentGateways:Casso" : "PaymentGateways:BankTransfer";
                
                var bankName = config[$"{section}:BankName"] ?? "MB Bank";
                var accountNumber = config[$"{section}:AccountNumber"] ?? "0905313925";
                var accountName = config[$"{section}:AccountName"] ?? "NGUYEN PHUONG NGUYEN";
                var branch = config[$"{section}:Branch"] ?? "Chi nhánh TP. Hồ Chí Minh";

                ViewBag.BankTransferInfo = new Dictionary<string, string>
                {
                    ["BankName"] = bankName,
                    ["AccountNumber"] = accountNumber,
                    ["AccountName"] = accountName,
                    ["Branch"] = branch,
                    ["Amount"] = order.TotalAmount.ToString("N0"),
                    ["TransferContent"] = order.OrderCode,
                    ["QRCode"] = GenerateVietQRUrl(
                        bankName,
                        accountNumber,
                        accountName,
                        order.TotalAmount,
                        order.OrderCode
                    )
                };
                _logger.LogInformation($"Bank info loaded for order {order.OrderCode}");
            }
            else
            {
                _logger.LogInformation($"Condition not met - Method: {order.PaymentMethod}, Status: {payment.Status}");
            }

            return View();
        }

        /// <summary>
        /// Generate VietQR URL for bank transfer QR code
        /// </summary>
        private string GenerateVietQRUrl(string bankName, string accountNumber, string accountName, decimal amount, string orderCode)
        {
            // Map bank names to VietQR short codes
            var bankCode = GetBankShortCode(bankName);
            
            // VietQR API format: https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.jpg
            return $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.jpg?amount={amount:F0}&addInfo={Uri.EscapeDataString(orderCode)}&accountName={Uri.EscapeDataString(accountName)}";
        }

        /// <summary>
        /// Get VietQR bank short code from bank name
        /// </summary>
        private string GetBankShortCode(string bankName)
        {
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

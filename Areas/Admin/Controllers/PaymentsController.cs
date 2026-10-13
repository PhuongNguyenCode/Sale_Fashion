using FashionStore.Data;
using FashionStore.Models;
using FashionStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IStockService _stockService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            ApplicationDbContext context,
            IPaymentService paymentService,
            IOrderService orderService,
            IStockService stockService,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _orderService = orderService;
            _stockService = stockService;
            _logger = logger;
        }

        // GET: Admin/Payments
        public async Task<IActionResult> Index(string? status, string? method, string? search, int page = 1)
        {
            const int pageSize = 20;

            var query = _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.User)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            // Filter by payment method
            if (!string.IsNullOrEmpty(method))
            {
                query = query.Where(p => p.PaymentMethod == method);
            }

            // Search by order code or transaction ID
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => 
                    p.Order!.OrderCode.Contains(search) || 
                    (p.TransactionId != null && p.TransactionId.Contains(search)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentMethod = method;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(payments);
        }

        // GET: Admin/Payments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.OrderDetails)
                .ThenInclude(od => od.Variant)
                .Include(p => p.Order)
                .ThenInclude(o => o!.User)
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Admin/Payments/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);

                if (payment == null)
                {
                    TempData["Error"] = "Payment not found";
                    return RedirectToAction(nameof(Index));
                }

                // Only allow confirming Bank Transfer payments
                if (payment.PaymentMethod != "BankTransfer")
                {
                    TempData["Error"] = "Only Bank Transfer payments can be manually confirmed";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    TempData["Error"] = "Payment is not in Pending status";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Update payment status to Paid
                var updated = await _paymentService.UpdatePaymentStatusAsync(id, PaymentStatus.Paid);

                if (updated)
                {
                    // Confirm order
                    await _orderService.UpdateOrderStatusAsync(payment.OrderId, OrderStatus.Confirmed);

                    // Deduct stock
                    await _stockService.DeductStockForOrderAsync(payment.OrderId);

                    // Record transaction
                    await _paymentService.RecordTransactionAsync(
                        id,
                        "BankTransfer",
                        $"MANUAL-{DateTime.Now:yyyyMMddHHmmss}",
                        payment.Amount,
                        PaymentStatus.Paid,
                        new { confirmedBy = User.Identity?.Name, confirmedAt = DateTime.Now }
                    );

                    TempData["Success"] = "Payment confirmed successfully";
                    _logger.LogInformation($"Payment {id} manually confirmed by {User.Identity?.Name}");
                }
                else
                {
                    TempData["Error"] = "Failed to confirm payment";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error confirming payment {id}");
                TempData["Error"] = "An error occurred while confirming payment";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Admin/Payments/Refund/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(int id, string reason)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);

                if (payment == null)
                {
                    TempData["Error"] = "Payment not found";
                    return RedirectToAction(nameof(Index));
                }

                if (payment.Status != PaymentStatus.Paid)
                {
                    TempData["Error"] = "Only paid payments can be refunded";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Process refund
                var result = await _paymentService.ProcessRefundAsync(id, reason ?? "Admin refund");

                if (result.Success)
                {
                    // Update order status to Refunded
                    await _orderService.UpdateOrderStatusAsync(payment.OrderId, OrderStatus.Refunded);

                    // Restore stock
                    await _stockService.RestoreStockForOrderAsync(payment.OrderId);

                    TempData["Success"] = "Refund processed successfully";
                    _logger.LogInformation($"Payment {id} refunded by {User.Identity?.Name}. Reason: {reason}");
                }
                else
                {
                    TempData["Error"] = $"Refund failed: {result.ErrorMessage}";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error refunding payment {id}");
                TempData["Error"] = "An error occurred while processing refund";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}

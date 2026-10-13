using FashionStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApplicationDbContext context, ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Danh sách đơn hàng của khách hàng
        /// </summary>
        [HttpGet]
        [Route("Account/Orders")]
        public async Task<IActionResult> Index(string? status, int page = 1)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            _logger.LogInformation($"OrderController.Index - UserIdClaim: {userIdClaim}");
            
            var userId = int.Parse(userIdClaim ?? "0");
            _logger.LogInformation($"OrderController.Index - UserId parsed: {userId}");
            
            const int pageSize = 10;

            var query = _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Variant).ThenInclude(v => v!.Product)
                .Where(o => o.UserId == userId)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var totalCount = await query.CountAsync();
            _logger.LogInformation($"OrderController.Index - Total orders found: {totalCount}");
            
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.Status = status;

            return View(orders);
        }

        /// <summary>
        /// Chi tiết đơn hàng
        /// </summary>
        [HttpGet]
        [Route("Account/Orders/{orderCode}")]
        public async Task<IActionResult> Detail(string orderCode)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Variant).ThenInclude(v => v!.Product)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            // Kiểm tra xem sản phẩm nào đã được đánh giá
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Variant?.Product != null)
                {
                    var hasReviewed = await _context.Reviews
                        .AnyAsync(r => r.UserId == userId && r.ProductId == detail.Variant.Product.ProductId);
                    
                    ViewData[$"HasReviewed_{detail.Variant.Product.ProductId}"] = hasReviewed;
                }
            }

            return View(order);
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string orderCode, string? reason)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Payment)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == userId);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index");
                }

                // Chỉ cho phép hủy đơn ở trạng thái Pending hoặc Confirmed
                if (order.Status != "Pending" && order.Status != "Confirmed")
                {
                    TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái hiện tại.";
                    return RedirectToAction("Detail", new { orderCode });
                }

                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.Now;
                order.Note = string.IsNullOrEmpty(order.Note) 
                    ? $"Khách hủy: {reason}" 
                    : $"{order.Note}. Khách hủy: {reason}";

                // Cập nhật payment status
                if (order.Payment != null && order.Payment.Status != "Paid")
                {
                    order.Payment.Status = "Cancelled";
                    order.Payment.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} cancelled order {orderCode}");

                TempData["Success"] = "Đã hủy đơn hàng thành công.";
                return RedirectToAction("Detail", new { orderCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order");
                TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return RedirectToAction("Detail", new { orderCode });
            }
        }
    }
}

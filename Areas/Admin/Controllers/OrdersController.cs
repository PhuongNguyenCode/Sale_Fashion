using FashionStore.Data;
using FashionStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? status, string? q, int page = 1)
        {
            page = Math.Max(1, page);
            int pageSize = 15;
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                string ql = q.ToLower();
                query = query.Where(o =>
                    o.OrderCode.ToLower().Contains(ql) ||
                    o.FullName.ToLower().Contains(ql) ||
                    o.Phone.ToLower().Contains(ql));
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            ViewBag.Search = q;
            ViewBag.Status = status;
            ViewBag.TotalCount = await query.CountAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(ViewBag.TotalCount / (double)pageSize);

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (OrderStatus.CanTransition(order.Status, newStatus))
            {
                order.Status = newStatus;
                order.UpdatedAt = DateTime.Now;

                if (newStatus == OrderStatus.Delivered)
                {
                    order.PaymentStatus = "Paid";
                    var payment = await _context.Payments.FirstOrDefaultAsync(item => item.OrderId == orderId);
                    if (payment != null)
                    {
                        payment.Status = "Paid";
                        payment.PaymentDate = DateTime.Now;
                    }
                }

                if (newStatus == OrderStatus.Cancelled)
                {
                    var details = await _context.OrderDetails
                        .Include(od => od.Variant)
                        .Where(od => od.OrderId == orderId)
                        .ToListAsync();

                    foreach (var detail in details)
                    {
                        if (detail.Variant != null)
                            detail.Variant.StockQuantity += detail.Quantity;
                    }

                    var payment = await _context.Payments.FirstOrDefaultAsync(item => item.OrderId == orderId);
                    if (payment != null) payment.Status = "Cancelled";
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật trạng thái thành công.";
            }
            else
            {
                TempData["Error"] = $"Không thể chuyển từ '{order.Status}' sang '{newStatus}'.";
            }

            return RedirectToAction("Detail", new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.PaymentStatus = PaymentStatus.Paid;
            order.Status = OrderStatus.Confirmed;
            order.UpdatedAt = DateTime.Now;

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment != null)
            {
                payment.Status = PaymentStatus.Paid;
                payment.PaymentDate = DateTime.Now;
                payment.UpdatedAt = DateTime.Now;
                if (string.IsNullOrEmpty(payment.TransactionId))
                {
                    payment.TransactionId = $"ADM-{DateTime.Now:yyyyMMddHHmmss}";
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Xác nhận thanh toán cho đơn hàng #{order.OrderCode} thành công!";

            return RedirectToAction("Detail", new { id = orderId });
        }
    }
}

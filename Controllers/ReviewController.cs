using FashionStore.Data;
using FashionStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(ApplicationDbContext context, ILogger<ReviewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Hiển thị form viết đánh giá cho sản phẩm
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(int productId, int? orderDetailId)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            // Kiểm tra sản phẩm có tồn tại không
            var product = await _context.Products
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra khách hàng đã mua sản phẩm này chưa
            var hasPurchased = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Variant)
                .AnyAsync(od => 
                    od.Order!.UserId == userId && 
                    od.Variant!.ProductId == productId &&
                    od.Order.Status == "Delivered"); // Chỉ cho phép đánh giá khi đã nhận hàng

            if (!hasPurchased)
            {
                TempData["Error"] = "Bạn chỉ có thể đánh giá sản phẩm đã mua và nhận hàng.";
                return RedirectToAction("Detail", "Product", new { slug = product.Slug });
            }

            // Kiểm tra đã đánh giá chưa
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => 
                    r.UserId == userId && 
                    r.ProductId == productId);

            if (existingReview != null)
            {
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này rồi.";
                return RedirectToAction("Detail", "Product", new { slug = product.Slug });
            }

            ViewBag.Product = product;
            ViewBag.OrderDetailId = orderDetailId;
            
            return View();
        }

        /// <summary>
        /// Xử lý submit đánh giá
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, int rating, string? comment, int? orderDetailId)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            try
            {
                // Validate
                if (rating < 1 || rating > 5)
                {
                    TempData["Error"] = "Đánh giá phải từ 1 đến 5 sao.";
                    return RedirectToAction("Create", new { productId, orderDetailId });
                }

                // Kiểm tra sản phẩm
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["Error"] = "Sản phẩm không tồn tại.";
                    return RedirectToAction("Index", "Home");
                }

                // Kiểm tra đã mua chưa
                var hasPurchased = await _context.OrderDetails
                    .Include(od => od.Order)
                    .Include(od => od.Variant)
                    .AnyAsync(od => 
                        od.Order!.UserId == userId && 
                        od.Variant!.ProductId == productId &&
                        od.Order.Status == "Delivered");

                if (!hasPurchased)
                {
                    TempData["Error"] = "Bạn chỉ có thể đánh giá sản phẩm đã mua và nhận hàng.";
                    return RedirectToAction("Detail", "Product", new { slug = product.Slug });
                }

                // Kiểm tra đã đánh giá chưa
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => 
                        r.UserId == userId && 
                        r.ProductId == productId);

                if (existingReview != null)
                {
                    TempData["Error"] = "Bạn đã đánh giá sản phẩm này rồi.";
                    return RedirectToAction("Detail", "Product", new { slug = product.Slug });
                }

                // Tạo review mới
                var review = new Review
                {
                    UserId = userId,
                    ProductId = productId,
                    OrderDetailId = orderDetailId,
                    Rating = rating,
                    Comment = comment?.Trim(),
                    IsApproved = false, // Chờ admin duyệt
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} created review for product {productId}");

                TempData["Success"] = "Cảm ơn bạn đã đánh giá! Đánh giá của bạn sẽ được hiển thị sau khi admin duyệt.";
                return RedirectToAction("Detail", "Product", new { slug = product.Slug });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return RedirectToAction("Create", new { productId, orderDetailId });
            }
        }

        /// <summary>
        /// Xóa đánh giá của chính mình
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            try
            {
                var review = await _context.Reviews
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == userId);

                if (review == null)
                {
                    TempData["Error"] = "Không tìm thấy đánh giá.";
                    return RedirectToAction("Index", "Home");
                }

                var productSlug = review.Product?.Slug;

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} deleted review {id}");

                TempData["Success"] = "Đã xóa đánh giá của bạn.";
                
                if (!string.IsNullOrEmpty(productSlug))
                {
                    return RedirectToAction("Detail", "Product", new { slug = productSlug });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review");
                TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Danh sách đánh giá của khách hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MyReviews(int page = 1)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            const int pageSize = 10;

            var query = _context.Reviews
                .Include(r => r.Product)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();
            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(reviews);
        }
    }
}

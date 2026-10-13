using FashionStore.Data;
using FashionStore.Models;
using FashionStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FashionStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string GuestCartSessionKey = "GuestCartSessionId";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper: Get or Create Cart
        private async Task<Cart> GetOrCreateCartAsync()
        {
            // Demo: Use a fixed Cart for testing if not logged in.
            // Ideally, use User.Identity.IsAuthenticated and UserId, or SessionId.
            int? userId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim, out int parsedId))
                {
                    userId = parsedId;
                }
            }

            Cart? cart = null;

            if (userId.HasValue)
            {
                cart = await _context.Carts
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Variant).ThenInclude(v => v!.Product)
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Variant).ThenInclude(v => v!.Color)
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Variant).ThenInclude(v => v!.Size)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);
            }
            else
            {
                var sessionId = HttpContext.Session.GetString(GuestCartSessionKey);
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = Guid.NewGuid().ToString("N");
                    HttpContext.Session.SetString(GuestCartSessionKey, sessionId);
                }
                cart = await _context.Carts
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Variant).ThenInclude(v => v!.Product)
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Variant).ThenInclude(v => v!.Color)
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Variant).ThenInclude(v => v!.Size)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);
            }

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    SessionId = userId.HasValue ? null : HttpContext.Session.GetString(GuestCartSessionKey),
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
                
            }

            return cart;
        }

        public async Task<IActionResult> Index()
        {
            var cart = await GetOrCreateCartAsync();
            var viewModel = new CartViewModel();

            if (cart != null && cart.CartItems.Any())
            {
                viewModel.Items = cart.CartItems.Select(ci => new CartItemViewModel
                {
                    CartItemId = ci.CartItemId,
                    VariantId = ci.VariantId,
                    ProductId = ci.Variant!.ProductId,
                    ProductName = ci.Variant.Product!.ProductName,
                    ProductImageUrl = ci.Variant.Product.MainImageUrl,
                    SizeName = ci.Variant.Size!.SizeName,
                    ColorName = ci.Variant.Color!.ColorName,
                    ColorCode = ci.Variant.Color.ColorCode ?? string.Empty,
                    UnitPrice = ci.UnitPrice,
                    Quantity = ci.Quantity,
                    StockQuantity = ci.Variant.StockQuantity,
                    Slug = ci.Variant.Product.Slug
                }).ToList();

                viewModel.SubTotal = cart.SubTotal;
                viewModel.TotalItems = cart.TotalItems;

                // Handle Coupon (Store in session for simplicity)
                var couponCode = HttpContext.Session.GetString("AppliedCoupon");
                if (!string.IsNullOrEmpty(couponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponCode == couponCode);
                    if (coupon != null && coupon.IsValid)
                    {
                        viewModel.CouponCode = couponCode;
                        viewModel.DiscountAmount = coupon.CalculateDiscount(viewModel.SubTotal);
                        viewModel.CouponMessage = $"Đã áp dụng mã {couponCode}";
                    }
                    else
                    {
                        HttpContext.Session.Remove("AppliedCoupon");
                        viewModel.CouponMessage = "Mã giảm giá không hợp lệ hoặc đã hết hạn.";
                    }
                }

                // Shipping logic (e.g. Free shipping over 300k)
                viewModel.ShippingFee = (viewModel.SubTotal - viewModel.DiscountAmount) >= 300000 ? 0 : 30000;
                viewModel.Total = viewModel.SubTotal - viewModel.DiscountAmount + viewModel.ShippingFee;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddToCartRequest request)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index", "Product");

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.VariantId == request.VariantId && v.IsActive);

            if (variant == null || variant.StockQuantity < request.Quantity)
            {
                TempData["Error"] = "Sản phẩm không tồn tại hoặc số lượng không đủ.";
                return RedirectToAction("Index", "Product");
            }

            var cart = await GetOrCreateCartAsync();
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.VariantId == request.VariantId);

            if (existingItem != null)
            {
                if (existingItem.Quantity + request.Quantity > variant.StockQuantity)
                {
                    TempData["Error"] = "Số lượng yêu cầu vượt quá tồn kho.";
                }
                else
                {
                    existingItem.Quantity += request.Quantity;
                    // Update price snapshot
                    existingItem.UnitPrice = variant.FinalPrice; 
                    TempData["Success"] = "Đã cập nhật giỏ hàng.";
                }
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    VariantId = request.VariantId,
                    Quantity = request.Quantity,
                    UnitPrice = variant.FinalPrice,
                    AddedAt = DateTime.Now
                });
                TempData["Success"] = "Đã thêm sản phẩm vào giỏ.";
            }

            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var cart = await GetOrCreateCartAsync();
            var item = cart.CartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                else
                {
                    if (quantity > item.Variant!.StockQuantity)
                    {
                        return Json(new { success = false, message = "Vượt quá số lượng tồn kho." });
                    }
                    item.Quantity = quantity;
                }
                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var cart = await GetOrCreateCartAsync();
            var item = cart.CartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);
            
            if (item != null)
            {
                _context.CartItems.Remove(item);
                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var cart = await GetOrCreateCartAsync();
            if (cart.CartItems.Any())
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyCoupon(string couponCode)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                TempData["Error"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToAction("Index");
            }

            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponCode == couponCode.ToUpper());
            if (coupon == null || !coupon.IsValid)
            {
                TempData["Error"] = "Mã giảm giá không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("Index");
            }

            var cart = await GetOrCreateCartAsync();
            if (cart.SubTotal < (coupon.MinOrderAmount ?? 0))
            {
                TempData["Error"] = $"Đơn hàng cần đạt tối thiểu {(coupon.MinOrderAmount ?? 0):N0}đ để áp dụng mã này.";
                return RedirectToAction("Index");
            }

            HttpContext.Session.SetString("AppliedCoupon", coupon.CouponCode);
            TempData["Success"] = "Áp dụng mã giảm giá thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove("AppliedCoupon");
            TempData["Success"] = "Đã bỏ mã giảm giá.";
            return RedirectToAction("Index");
        }
    }
}

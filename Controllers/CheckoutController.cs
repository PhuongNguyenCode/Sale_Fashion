using FashionStore.Data;
using FashionStore.Models;
using FashionStore.Services;
using FashionStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FashionStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICheckoutService _checkoutService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ApplicationDbContext context,
            ICheckoutService checkoutService,
            IConfiguration configuration,
            ILogger<CheckoutController> logger)
        {
            _context = context;
            _checkoutService = checkoutService;
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<Cart?> GetCurrentCartAsync()
        {
            int? userId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim, out int parsedId))
                {
                    userId = parsedId;
                }
            }

            if (userId.HasValue)
            {
                return await _context.Carts
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Variant).ThenInclude(v => v!.Product)
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Variant).ThenInclude(v => v!.Color)
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Variant).ThenInclude(v => v!.Size)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);
            }
            return null;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var cart = await GetCurrentCartAsync();
            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var viewModel = new CheckoutViewModel();
            
            // Pre-fill user data if logged in
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim, out int parsedId))
                {
                    var user = await _context.Users.FindAsync(parsedId);
                    if (user != null)
                    {
                        viewModel.FullName = user.FullName;
                        viewModel.Email = user.Email;
                        viewModel.Phone = user.Phone ?? "";
                        viewModel.Address = user.Address ?? "";
                        viewModel.City = user.City ?? "";
                    }
                }
            }

            // Populate Cart data
            viewModel.Cart.Items = cart.CartItems.Select(ci => new CartItemViewModel
            {
                CartItemId = ci.CartItemId,
                VariantId = ci.VariantId,
                ProductId = ci.Variant!.ProductId,
                ProductName = ci.Variant.Product!.ProductName,
                ProductImageUrl = ci.Variant.Product.MainImageUrl,
                SizeName = ci.Variant.Size!.SizeName,
                ColorName = ci.Variant.Color!.ColorName,
                UnitPrice = ci.UnitPrice, // We use snapshot price
                Quantity = ci.Quantity,
                StockQuantity = ci.Variant.StockQuantity
            }).ToList();

            viewModel.Cart.SubTotal = cart.SubTotal;
            viewModel.Cart.TotalItems = cart.TotalItems;

            var couponCode = HttpContext.Session.GetString("AppliedCoupon");
            Coupon? activeCoupon = null;
            if (!string.IsNullOrEmpty(couponCode))
            {
                activeCoupon = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponCode == couponCode);
                if (activeCoupon != null && activeCoupon.IsValid)
                {
                    viewModel.Cart.CouponCode = couponCode;
                    viewModel.Cart.DiscountAmount = activeCoupon.CalculateDiscount(viewModel.Cart.SubTotal);
                }
            }

            viewModel.Cart.ShippingFee = (viewModel.Cart.SubTotal - viewModel.Cart.DiscountAmount) >= 300000 ? 0 : 30000;
            viewModel.Cart.Total = viewModel.Cart.SubTotal - viewModel.Cart.DiscountAmount + viewModel.Cart.ShippingFee;

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            if (model.PaymentMethod is not ("COD" or "Casso" or "VNPay"))
            {
                TempData["Error"] = "Phương thức thanh toán không được hỗ trợ.";
                return RedirectToAction("Index");
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userIdParsed = int.TryParse(userId, out var uid) ? uid : (int?)null;

                // Get return and webhook URLs
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var publicBaseUrl = _configuration["Payment:PublicBaseUrl"];
                if (!string.IsNullOrWhiteSpace(publicBaseUrl))
                {
                    if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var publicUri) ||
                        publicUri.Scheme != Uri.UriSchemeHttps ||
                        !string.IsNullOrEmpty(publicUri.Query) || !string.IsNullOrEmpty(publicUri.Fragment))
                    {
                        throw new InvalidOperationException("Payment:PublicBaseUrl must be an HTTPS origin");
                    }
                    baseUrl = publicUri.GetLeftPart(UriPartial.Authority);
                }
                var returnUrl = model.PaymentMethod.ToLower() switch
                {
                    "vnpay" => $"{baseUrl}/payment/vnpay-return",
                    _ => $"{baseUrl}/payment/return"
                };

                // Set gateway-specific webhook/IPN URLs
                var webhookUrl = model.PaymentMethod.ToLower() switch
                {
                    "vnpay" => $"{baseUrl}/api/payment/ipn/vnpay",
                    _ => $"{baseUrl}/api/payment/webhook/{model.PaymentMethod.ToLower()}"
                };

                // Create checkout request
                var request = new CheckoutRequest
                {
                    UserId = userIdParsed,
                    SessionId = HttpContext.Session.Id,
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    Address = model.Address,
                    City = model.City,
                    Note = model.Note,
                    CouponCode = HttpContext.Session.GetString("AppliedCoupon"),
                    PaymentMethod = model.PaymentMethod,
                    PaymentGateway = model.PaymentMethod // For now, method = gateway
                };

                var result = await _checkoutService.PlaceOrderAsync(request, returnUrl, webhookUrl);

                if (!result.Success)
                {
                    TempData["Error"] = result.ErrorMessage;
                    return RedirectToAction("Index");
                }

                HttpContext.Session.Remove("AppliedCoupon");

                // For gateway payments, redirect to payment URL
                if (!string.IsNullOrEmpty(result.PaymentUrl))
                {
                    return Redirect(result.PaymentUrl);
                }

                // For COD/BankTransfer, go to success page
                TempData["OrderCode"] = result.OrderCode;
                TempData["OrderId"] = result.OrderId;
                TempData["PaymentId"] = result.PaymentId;

                // For bank transfer, store additional info
                if (result.AdditionalInfo.Any())
                {
                    TempData["BankTransferInfo"] = System.Text.Json.JsonSerializer.Serialize(result.AdditionalInfo);
                }

                return RedirectToAction("Result", "Payment", new { orderCode = result.OrderCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                TempData["Error"] = "Đã xảy ra lỗi trong quá trình đặt hàng. Vui lòng thử lại sau.";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Success()
        {
            if (TempData["OrderCode"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new OrderSuccessViewModel
            {
                OrderCode = TempData["OrderCode"]?.ToString() ?? "",
                TotalAmount = decimal.Parse(TempData["TotalAmount"]?.ToString() ?? "0"),
                PaymentMethod = TempData["PaymentMethod"]?.ToString() ?? ""
            };

            return View(model);
        }
    }
}

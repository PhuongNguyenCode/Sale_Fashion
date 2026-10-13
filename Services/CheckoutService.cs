using FashionStore.Data;
using FashionStore.Models;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IDiscountCalculator _discountCalculator;
        private readonly ILogger<CheckoutService> _logger;

        public CheckoutService(
            ApplicationDbContext context,
            IOrderService orderService,
            IPaymentService paymentService,
            IDiscountCalculator discountCalculator,
            ILogger<CheckoutService> logger)
        {
            _context = context;
            _orderService = orderService;
            _paymentService = paymentService;
            _discountCalculator = discountCalculator;
            _logger = logger;
        }

        public async Task<CheckoutPageViewModel?> GetCheckoutPageAsync(int? userId, string? sessionId)
        {
            try
            {
                // Get cart
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Variant)
                    .ThenInclude(v => v!.Product)
                    .ThenInclude(p => p!.Images)
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Variant)
                    .ThenInclude(v => v!.Size)
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Variant)
                    .ThenInclude(v => v!.Color)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => userId.HasValue ? c.UserId == userId : c.SessionId == sessionId);

                if (cart == null || !cart.CartItems.Any())
                {
                    return null;
                }

                // Validate cart
                var (isValid, errorMessage) = await ValidateCartAsync(userId, sessionId);
                if (!isValid)
                {
                    _logger.LogWarning($"Cart validation failed: {errorMessage}");
                    return null;
                }

                // Calculate totals
                var subtotal = cart.CartItems.Sum(ci => ci.SubTotal);
                var shippingFee = _discountCalculator.CalculateShippingFee(subtotal);

                var viewModel = new CheckoutPageViewModel
                {
                    CartId = cart.CartId,
                    SubTotal = subtotal,
                    DiscountAmount = 0,
                    ShippingFee = shippingFee,
                    TotalAmount = subtotal + shippingFee,
                    Items = cart.CartItems.Select(ci => new CheckoutItemViewModel
                    {
                        VariantId = ci.VariantId,
                        ProductName = ci.Variant?.Product?.ProductName ?? "Unknown",
                        SizeName = ci.Variant?.Size?.SizeName ?? "Unknown",
                        ColorName = ci.Variant?.Color?.ColorName ?? "Unknown",
                        ImageUrl = ci.Variant?.Product?.Images.FirstOrDefault(pi => pi.IsMain)?.ImageUrl,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.UnitPrice,
                        SubTotal = ci.SubTotal,
                        StockQuantity = ci.Variant?.StockQuantity ?? 0,
                        IsActive = ci.Variant?.IsActive ?? false
                    }).ToList(),
                    AvailablePaymentMethods = new List<string> { "COD", "Casso", "VNPay" }
                };

                // Pre-fill user information if authenticated
                if (cart.User != null)
                {
                    viewModel.UserFullName = cart.User.FullName;
                    viewModel.UserEmail = cart.User.Email;
                    viewModel.UserPhone = cart.User.Phone;
                    viewModel.UserAddress = cart.User.Address;
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checkout page");
                return null;
            }
        }

        public async Task<CheckoutResult> PlaceOrderAsync(CheckoutRequest request, string returnUrl, string webhookUrl)
        {
            if (request.PaymentMethod is not ("COD" or "Casso" or "VNPay") ||
                (request.PaymentGateway != null && request.PaymentGateway != request.PaymentMethod))
            {
                return new CheckoutResult
                {
                    Success = false,
                    ErrorMessage = "Phương thức thanh toán không được hỗ trợ."
                };
            }

            // Use database transaction to ensure order and payment are created atomically
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Get and validate cart
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Variant)
                    .ThenInclude(v => v!.Product)
                    .FirstOrDefaultAsync(c => request.UserId.HasValue 
                        ? c.UserId == request.UserId 
                        : c.SessionId == request.SessionId);

                if (cart == null || !cart.CartItems.Any())
                {
                    await dbTransaction.RollbackAsync();
                    return new CheckoutResult
                    {
                        Success = false,
                        ErrorMessage = "Cart is empty"
                    };
                }

                // Validate cart
                var (isValid, validationError) = await ValidateCartAsync(request.UserId, request.SessionId);
                if (!isValid)
                {
                    await dbTransaction.RollbackAsync();
                    return new CheckoutResult
                    {
                        Success = false,
                        ErrorMessage = validationError
                    };
                }

                // Calculate amounts (server-side recalculation for security)
                var subtotal = cart.CartItems.Sum(ci => ci.Quantity * ci.Variant!.FinalPrice);
                decimal discountAmount = 0;
                int? couponId = null;

                // Apply coupon if provided
                if (!string.IsNullOrWhiteSpace(request.CouponCode))
                {
                    var coupon = await _context.Coupons
                        .FirstOrDefaultAsync(c => c.CouponCode == request.CouponCode && c.IsActive);

                    if (coupon != null && coupon.IsValid)
                    {
                        discountAmount = _discountCalculator.CalculateDiscount(coupon, subtotal);
                        couponId = coupon.CouponId;
                    }
                    else
                    {
                        await dbTransaction.RollbackAsync();
                        return new CheckoutResult
                        {
                            Success = false,
                            ErrorMessage = "Invalid or expired coupon code"
                        };
                    }
                }

                var subtotalAfterDiscount = subtotal - discountAmount;
                var shippingFee = _discountCalculator.CalculateShippingFee(subtotalAfterDiscount);
                var totalAmount = subtotalAfterDiscount + shippingFee;

                // Create order (OrderService handles its own transaction)
                var orderRequest = new OrderCreationRequest
                {
                    UserId = request.UserId,
                    CouponId = couponId,
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Address = request.Address,
                    City = request.City,
                    Note = request.Note,
                    SubTotal = subtotal,
                    DiscountAmount = discountAmount,
                    ShippingFee = shippingFee,
                    TotalAmount = totalAmount,
                    PaymentMethod = request.PaymentMethod,
                    Items = cart.CartItems.Select(ci => new OrderItemRequest
                    {
                        VariantId = ci.VariantId,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.Variant!.FinalPrice
                    }).ToList()
                };

                var order = await _orderService.CreateOrderAsync(orderRequest);

                // Create payment (PaymentService handles its own transaction)
                var gateway = request.PaymentGateway ?? request.PaymentMethod;
                var payment = await _paymentService.CreatePaymentAsync(
                    order.OrderId,
                    request.PaymentMethod,
                    totalAmount,
                    gateway
                );

                // Initiate gateway payment if needed
                var gatewayResult = await _paymentService.InitiateGatewayPaymentAsync(payment, returnUrl, webhookUrl);
                
                // If gateway initiation failed, rollback everything
                if (!gatewayResult.Success)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError($"Gateway initiation failed for order {order.OrderCode}: {gatewayResult.ErrorMessage}");
                    return new CheckoutResult
                    {
                        Success = false,
                        ErrorMessage = gatewayResult.ErrorMessage ?? "Failed to initiate payment"
                    };
                }

                // Clear cart
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();
                
                // Commit transaction
                await dbTransaction.CommitAsync();

                _logger.LogInformation($"Order {order.OrderCode} placed successfully with transaction");

                var result = new CheckoutResult
                {
                    Success = true,
                    OrderCode = order.OrderCode,
                    OrderId = order.OrderId,
                    PaymentId = payment.PaymentId,
                    PaymentUrl = gatewayResult.PaymentUrl
                };

                // Add additional info for bank transfer
                if (gatewayResult.AdditionalInfo.Any())
                {
                    result.AdditionalInfo = gatewayResult.AdditionalInfo;
                }

                return result;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error placing order with transaction");
                return new CheckoutResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to place order: {ex.Message}"
                };
            }
        }

        public async Task<(bool isValid, string? errorMessage)> ValidateCartAsync(int? userId, string? sessionId)
        {
            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Variant)
                    .FirstOrDefaultAsync(c => userId.HasValue ? c.UserId == userId : c.SessionId == sessionId);

                if (cart == null)
                {
                    return (false, "Cart not found");
                }

                if (!cart.CartItems.Any())
                {
                    return (false, "Cart is empty");
                }

                foreach (var item in cart.CartItems)
                {
                    if (item.Variant == null)
                    {
                        return (false, $"Product variant not found for cart item {item.CartItemId}");
                    }

                    if (!item.Variant.IsActive)
                    {
                        return (false, $"Product variant {item.Variant.VariantId} is no longer available");
                    }

                    if (item.Variant.StockQuantity < item.Quantity)
                    {
                        return (false, $"Insufficient stock for variant {item.Variant.VariantId}. Available: {item.Variant.StockQuantity}, Required: {item.Quantity}");
                    }

                    // Check if price has changed
                    if (item.UnitPrice != item.Variant.FinalPrice)
                    {
                        // Update cart item price
                        item.UnitPrice = item.Variant.FinalPrice;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Price updated for cart item {item.CartItemId}: {item.UnitPrice} -> {item.Variant.FinalPrice}");
                    }
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cart");
                return (false, "Error validating cart");
            }
        }
    }
}

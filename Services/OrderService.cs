using FashionStore.Data;
using FashionStore.Models;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ApplicationDbContext context, ILogger<OrderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Order> CreateOrderAsync(OrderCreationRequest request)
        {
            // NOTE: Transaction is now managed by CheckoutService
            // Do not create nested transaction here
            try
            {
                // Generate unique order code
                var orderCode = GenerateOrderCode();
                
                // Ensure uniqueness (retry if collision)
                var attempts = 0;
                while (await _context.Orders.AnyAsync(o => o.OrderCode == orderCode) && attempts < 5)
                {
                    orderCode = GenerateOrderCode();
                    attempts++;
                }

                if (attempts >= 5)
                {
                    throw new InvalidOperationException("Failed to generate unique order code after 5 attempts");
                }

                // Create order
                var order = new Order
                {
                    UserId = request.UserId,
                    CouponId = request.CouponId,
                    OrderCode = orderCode,
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Address = request.Address,
                    City = request.City,
                    Note = request.Note,
                    SubTotal = request.SubTotal,
                    DiscountAmount = request.DiscountAmount,
                    ShippingFee = request.ShippingFee,
                    TotalAmount = request.TotalAmount,
                    Status = OrderStatus.Pending,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = "Pending",
                    OrderDate = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order details with product snapshot
                foreach (var item in request.Items)
                {
                    var variant = await _context.ProductVariants
                        .Include(v => v.Product)
                        .Include(v => v.Size)
                        .Include(v => v.Color)
                        .FirstOrDefaultAsync(v => v.VariantId == item.VariantId);

                    if (variant == null)
                    {
                        throw new InvalidOperationException($"Variant {item.VariantId} not found");
                    }

                    // Get product image
                    var productImage = await _context.ProductImages
                        .Where(pi => pi.ProductId == variant.ProductId && pi.IsMain)
                        .Select(pi => pi.ImageUrl)
                        .FirstOrDefaultAsync();

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        VariantId = item.VariantId,
                        ProductName = variant.Product?.ProductName ?? "Unknown Product",
                        SizeName = variant.Size?.SizeName ?? "Unknown",
                        ColorName = variant.Color?.ColorName ?? "Unknown",
                        ProductImageUrl = productImage,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        SubTotal = item.UnitPrice * item.Quantity
                    };

                    _context.OrderDetails.Add(orderDetail);
                }

                await _context.SaveChangesAsync();

                // Increment coupon usage if applicable
                if (request.CouponId.HasValue)
                {
                    var coupon = await _context.Coupons.FindAsync(request.CouponId.Value);
                    if (coupon != null)
                    {
                        coupon.UsedCount++;
                        await _context.SaveChangesAsync();
                    }
                }

                // NOTE: Transaction commit is now handled by CheckoutService
                // await transaction.CommitAsync(); // REMOVED

                _logger.LogInformation($"Order {orderCode} created successfully");

                // Load navigation properties
                await _context.Entry(order).Collection(o => o.OrderDetails).LoadAsync();

                return order;
            }
            catch (Exception ex)
            {
                // NOTE: Transaction rollback is now handled by CheckoutService
                // await transaction.RollbackAsync(); // REMOVED
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, string? updatedBy = null)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning($"Order {orderId} not found");
                    return false;
                }

                // Validate status transition
                if (!CanTransitionStatus(order.Status, newStatus))
                {
                    _logger.LogWarning($"Invalid status transition from {order.Status} to {newStatus} for order {orderId}");
                    return false;
                }

                var oldStatus = order.Status;
                order.Status = newStatus;
                order.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Order {order.OrderCode} status updated from {oldStatus} to {newStatus}" + 
                    (updatedBy != null ? $" by {updatedBy}" : ""));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order {orderId} status to {newStatus}");
                return false;
            }
        }

        public bool CanTransitionStatus(string currentStatus, string newStatus)
        {
            return OrderStatus.CanTransition(currentStatus, newStatus);
        }

        public string GenerateOrderCode()
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"ORD-{timestamp}-{random}";
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Variant)
                .Include(o => o.Payment)
                .Include(o => o.Coupon)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetOrderByCodeAsync(string orderCode)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Variant)
                .Include(o => o.Payment)
                .Include(o => o.Coupon)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        }
    }
}

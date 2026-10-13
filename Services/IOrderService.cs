using FashionStore.Models;

namespace FashionStore.Services
{
    public class OrderCreationRequest
    {
        public int? UserId { get; set; }
        public int? CouponId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Note { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "COD";
    }

    public class OrderItemRequest
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public interface IOrderService
    {
        /// <summary>
        /// Create a new order with order details
        /// </summary>
        Task<Order> CreateOrderAsync(OrderCreationRequest request);

        /// <summary>
        /// Update order status with transition validation
        /// </summary>
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, string? updatedBy = null);

        /// <summary>
        /// Check if status transition is valid
        /// </summary>
        bool CanTransitionStatus(string currentStatus, string newStatus);

        /// <summary>
        /// Generate unique order code in format ORD-YYMMDDHHMMSS-XXX
        /// </summary>
        string GenerateOrderCode();

        /// <summary>
        /// Get order by ID
        /// </summary>
        Task<Order?> GetOrderByIdAsync(int orderId);

        /// <summary>
        /// Get order by order code
        /// </summary>
        Task<Order?> GetOrderByCodeAsync(string orderCode);
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionStore.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int? UserId { get; set; }

        public int? CouponId { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [Required]
        [StringLength(30)]
        public string PaymentMethod { get; set; } = "COD";

        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual Payment? Payment { get; set; }
    }

    // Order Status Constants
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Processing = "Processing";
        public const string Shipping = "Shipping";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
        public const string Completed = "Completed";
        public const string Refunded = "Refunded";

        public static readonly string[] AllStatuses = 
        { 
            Pending, 
            Confirmed, 
            Processing, 
            Shipping, 
            Delivered, 
            Cancelled, 
            Completed, 
            Refunded 
        };

        // Valid state transitions
        private static readonly Dictionary<string, string[]> ValidTransitions = new()
        {
            [Pending] = new[] { Confirmed, Cancelled },
            [Confirmed] = new[] { Processing, Cancelled },
            [Processing] = new[] { Shipping, Cancelled },
            [Shipping] = new[] { Delivered },
            [Delivered] = new[] { Completed },
            [Completed] = Array.Empty<string>(),
            [Cancelled] = Array.Empty<string>(),
            [Refunded] = Array.Empty<string>()
        };

        // Check if transition is allowed
        public static bool CanTransition(string from, string to)
        {
            // Can refund from any status (as long as payment was paid)
            if (to == Refunded)
                return true;

            return ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
        }

        public static bool IsValidStatus(string status)
        {
            return AllStatuses.Contains(status);
        }
    }
}

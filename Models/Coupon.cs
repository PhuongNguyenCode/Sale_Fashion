using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionStore.Models
{
    [Table("Coupons")]
    public class Coupon
    {
        [Key]
        public int CouponId { get; set; }

        [Required]
        [StringLength(50)]
        public string CouponCode { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } = "Percent";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        [NotMapped]
        public bool IsValid => IsActive
            && (!StartDate.HasValue || DateTime.Now >= StartDate.Value)
            && (!EndDate.HasValue || DateTime.Today <= EndDate.Value.Date)
            && (!UsageLimit.HasValue || UsedCount < UsageLimit.Value);

        public decimal CalculateDiscount(decimal orderTotal)
        {
            if (!IsValid || orderTotal < (MinOrderAmount ?? 0)) return 0;
            decimal discount = DiscountType == "Percent" ? orderTotal * DiscountValue / 100 : DiscountValue;
            if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value) discount = MaxDiscountAmount.Value;
            return Math.Min(discount, orderTotal);
        }
    }
}

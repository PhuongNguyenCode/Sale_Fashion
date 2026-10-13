using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionStore.Models
{
    [Table("ProductVariants")]
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int SizeId { get; set; }

        [Required]
        public int ColorId { get; set; }

        [Required]
        public int StockQuantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalPrice { get; set; } = 0;

        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Navigation
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("SizeId")]
        public virtual Size? Size { get; set; }

        [ForeignKey("ColorId")]
        public virtual Color? Color { get; set; }

        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        // Computed
        [NotMapped]
        public decimal FinalPrice => (Product?.DisplayPrice ?? 0) + AdditionalPrice;
    }
}

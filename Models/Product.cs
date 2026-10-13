using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionStore.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        // FashionShop allows NULL, but the current administration screen requires a brand for each product.
        public int BrandId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }

        [StringLength(255)]
        public string? MainImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false;

        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }

        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

        // Computed properties
        [NotMapped]
        public decimal DisplayPrice => SalePrice ?? Price;

        [NotMapped]
        public int? DiscountPercent => SalePrice.HasValue && Price > 0
            ? (int)Math.Round((1 - SalePrice.Value / Price) * 100)
            : null;

        [NotMapped]
        public double AverageRating => Reviews.Any(r => r.IsApproved)
            ? Reviews.Where(r => r.IsApproved).Average(r => r.Rating)
            : 0;

        [NotMapped]
        public int ReviewCount => Reviews.Count(r => r.IsApproved);

        [NotMapped]
        public int TotalStock => Variants.Where(v => v.IsActive).Sum(v => v.StockQuantity);
    }
}

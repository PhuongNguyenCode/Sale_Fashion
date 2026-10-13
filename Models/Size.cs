using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionStore.Models
{
    [Table("Sizes")]
    public class Size
    {
        [Key]
        public int SizeId { get; set; }

        [Required]
        [StringLength(20)]
        public string SizeName { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        // Navigation
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    }
}

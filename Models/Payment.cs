using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionStore.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [StringLength(30)]
        public string PaymentMethod { get; set; } = "COD";

        [StringLength(50)]
        public string? PaymentGateway { get; set; }

        [StringLength(100)]
        public string? TransactionId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? PaymentDate { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [ConcurrencyCheck]
        public int? Version { get; set; } = 1;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public virtual ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    }
}

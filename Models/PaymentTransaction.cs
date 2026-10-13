using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionStore.Models
{
    [Table("PaymentTransactions")]
    public class PaymentTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int PaymentId { get; set; }

        [Required]
        [StringLength(50)]
        public string Gateway { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ExternalTransactionId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "VND";

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(50)]
        public string? ResponseCode { get; set; }

        [StringLength(500)]
        public string? ResponseMessage { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? RawResponse { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("PaymentId")]
        public virtual Payment? Payment { get; set; }
    }
}

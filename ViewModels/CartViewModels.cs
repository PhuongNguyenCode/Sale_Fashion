using FashionStore.Models;
using System.ComponentModel.DataAnnotations;

namespace FashionStore.ViewModels
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Total { get; set; }
        public string? CouponCode { get; set; }
        public string? CouponMessage { get; set; }
        public int TotalItems { get; set; }
    }

    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public string SizeName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string ColorCode { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }
        public decimal SubTotal => UnitPrice * Quantity;
        public string Slug { get; set; } = string.Empty;
    }

    public class AddToCartRequest
    {
        [Required]
        public int VariantId { get; set; }
        [Required]
        [Range(1, 99)]
        public int Quantity { get; set; } = 1;
    }

    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string Address { get; set; } = string.Empty;

        public string? City { get; set; }
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "COD";

        public CartViewModel Cart { get; set; } = new();
    }

    public class OrderSuccessViewModel
    {
        public string OrderCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }
}

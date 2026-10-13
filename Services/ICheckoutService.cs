namespace FashionStore.Services
{
    public class CheckoutPageViewModel
    {
        public int CartId { get; set; }
        public List<CheckoutItemViewModel> Items { get; set; } = new List<CheckoutItemViewModel>();
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CouponCode { get; set; }
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public string? UserAddress { get; set; }
        public List<string> AvailablePaymentMethods { get; set; } = new List<string>();
    }

    public class CheckoutItemViewModel
    {
        public int VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
    }

    public class CheckoutRequest
    {
        public int? UserId { get; set; }
        public string? SessionId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Note { get; set; }
        public string? CouponCode { get; set; }
        public string PaymentMethod { get; set; } = "COD";
        public string? PaymentGateway { get; set; }
    }

    public class CheckoutResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OrderCode { get; set; }
        public string? PaymentUrl { get; set; }
        public int? OrderId { get; set; }
        public int? PaymentId { get; set; }
        public Dictionary<string, string> AdditionalInfo { get; set; } = new Dictionary<string, string>();
    }

    public interface ICheckoutService
    {
        /// <summary>
        /// Get checkout page data with cart validation
        /// </summary>
        Task<CheckoutPageViewModel?> GetCheckoutPageAsync(int? userId, string? sessionId);

        /// <summary>
        /// Place order and create payment
        /// </summary>
        Task<CheckoutResult> PlaceOrderAsync(CheckoutRequest request, string returnUrl, string webhookUrl);

        /// <summary>
        /// Validate cart before checkout
        /// </summary>
        Task<(bool isValid, string? errorMessage)> ValidateCartAsync(int? userId, string? sessionId);
    }
}

using FashionStore.Models;

namespace FashionStore.Services
{
    public class DiscountCalculator : IDiscountCalculator
    {
        private const decimal FreeShippingThreshold = 300000m; // 300,000 VND
        private const decimal StandardShippingFee = 30000m;     // 30,000 VND

        public decimal CalculateDiscount(Coupon coupon, decimal subtotal)
        {
            // Validate coupon is active and not expired
            if (!coupon.IsValid)
                return 0;

            // Check minimum order amount requirement
            if (subtotal < (coupon.MinOrderAmount ?? 0))
                return 0;

            decimal discountAmount;

            // Calculate discount based on type
            if (coupon.DiscountType == "Percent")
            {
                // Percent discount: (subtotal * discountValue / 100)
                discountAmount = subtotal * coupon.DiscountValue / 100;

                // Cap at MaxDiscountAmount if specified
                if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
                {
                    discountAmount = coupon.MaxDiscountAmount.Value;
                }
            }
            else // Amount discount
            {
                discountAmount = coupon.DiscountValue;
            }

            // Ensure discount doesn't exceed subtotal
            return Math.Min(discountAmount, subtotal);
        }

        public decimal CalculateShippingFee(decimal subtotalAfterDiscount)
        {
            // Free shipping if order meets threshold
            if (subtotalAfterDiscount >= FreeShippingThreshold)
                return 0;

            return StandardShippingFee;
        }
    }
}

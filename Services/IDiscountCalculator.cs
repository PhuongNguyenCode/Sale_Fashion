using FashionStore.Models;

namespace FashionStore.Services
{
    public interface IDiscountCalculator
    {
        /// <summary>
        /// Calculate discount amount based on coupon rules and order subtotal
        /// </summary>
        /// <param name="coupon">The coupon to apply</param>
        /// <param name="subtotal">Order subtotal before discount</param>
        /// <returns>Calculated discount amount</returns>
        decimal CalculateDiscount(Coupon coupon, decimal subtotal);

        /// <summary>
        /// Calculate shipping fee based on order subtotal after discount
        /// Free shipping (0 VND) if subtotal >= 300,000 VND
        /// Otherwise 30,000 VND
        /// </summary>
        /// <param name="subtotalAfterDiscount">Subtotal after applying discount</param>
        /// <returns>Calculated shipping fee</returns>
        decimal CalculateShippingFee(decimal subtotalAfterDiscount);
    }
}

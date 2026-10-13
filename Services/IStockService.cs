namespace FashionStore.Services
{
    public interface IStockService
    {
        /// <summary>
        /// Deduct stock for all items in an order (idempotent)
        /// </summary>
        Task<bool> DeductStockForOrderAsync(int orderId);

        /// <summary>
        /// Restore stock when order is cancelled or refunded
        /// </summary>
        Task<bool> RestoreStockForOrderAsync(int orderId);

        /// <summary>
        /// Check if order can be fulfilled with current stock
        /// </summary>
        Task<bool> CanFulfillOrder(int orderId);

        /// <summary>
        /// Check if sufficient stock exists for a specific variant
        /// </summary>
        Task<bool> HasSufficientStock(int variantId, int quantity);
    }
}

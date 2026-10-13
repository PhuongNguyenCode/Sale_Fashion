namespace FashionStore.Services
{
    public interface IIdempotencyManager
    {
        /// <summary>
        /// Check if a transaction has already been processed
        /// </summary>
        /// <param name="transactionId">External transaction ID from payment gateway</param>
        /// <returns>True if transaction was already processed</returns>
        Task<bool> IsTransactionProcessedAsync(string transactionId);

        /// <summary>
        /// Mark a transaction as processed
        /// </summary>
        /// <param name="transactionId">External transaction ID</param>
        /// <param name="paymentId">Payment ID this transaction belongs to</param>
        Task MarkTransactionAsProcessedAsync(string transactionId, int paymentId);
    }
}

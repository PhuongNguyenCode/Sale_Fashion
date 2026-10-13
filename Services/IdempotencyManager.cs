using FashionStore.Data;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Services
{
    public class IdempotencyManager : IIdempotencyManager
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IdempotencyManager> _logger;

        public IdempotencyManager(
            ApplicationDbContext context,
            ILogger<IdempotencyManager> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsTransactionProcessedAsync(string transactionId)
        {
            try
            {
                // Check if transaction exists in PaymentTransactions table
                // Unique constraint on ExternalTransactionId ensures idempotency
                var exists = await _context.PaymentTransactions
                    .AnyAsync(pt => pt.ExternalTransactionId == transactionId);

                if (exists)
                {
                    _logger.LogInformation(
                        "Transaction {TransactionId} already processed (idempotent check)",
                        transactionId);
                }

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking transaction processed status for {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task MarkTransactionAsProcessedAsync(string transactionId, int paymentId)
        {
            try
            {
                // This method is typically called as part of webhook processing
                // The actual PaymentTransaction record should be created by PaymentService
                // This is just a placeholder for explicit marking if needed
                
                _logger.LogInformation(
                    "Transaction {TransactionId} marked as processed for Payment {PaymentId}",
                    transactionId, paymentId);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking transaction {TransactionId} as processed", transactionId);
                throw;
            }
        }
    }
}

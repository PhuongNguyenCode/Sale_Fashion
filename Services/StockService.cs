using FashionStore.Data;
using FashionStore.Models;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Services
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockService> _logger;

        // Track which orders have had stock deducted (in-memory for idempotency within app lifecycle)
        // For production, consider using a distributed cache or database flag
        private static readonly HashSet<int> _processedOrders = new HashSet<int>();
        private static readonly object _lock = new object();

        public StockService(ApplicationDbContext context, ILogger<StockService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> DeductStockForOrderAsync(int orderId)
        {
            var hasAmbientTransaction = _context.Database.CurrentTransaction != null;

            // The webhook's database transaction is the durable idempotency boundary.
            // Only use the in-memory guard for standalone stock updates.
            if (!hasAmbientTransaction)
            {
                lock (_lock)
                {
                    if (_processedOrders.Contains(orderId))
                    {
                        _logger.LogInformation("Stock already deducted for order {OrderId}, skipping", orderId);
                        return true;
                    }
                }
            }

            await using var ownedTransaction = hasAmbientTransaction
                ? null
                : await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    _logger.LogWarning($"Order {orderId} not found");
                    return false;
                }

                // Check if sufficient stock exists
                foreach (var detail in order.OrderDetails)
                {
                    if (detail.Variant == null)
                    {
                        _logger.LogError($"Variant {detail.VariantId} not found for order detail {detail.OrderDetailId}");
                        return false;
                    }

                    if (detail.Variant.StockQuantity < detail.Quantity)
                    {
                        _logger.LogWarning($"Insufficient stock for variant {detail.VariantId}: available={detail.Variant.StockQuantity}, required={detail.Quantity}");
                        return false;
                    }
                }

                // Deduct stock with row-level locking (UPDLOCK in SQL Server)
                foreach (var detail in order.OrderDetails)
                {
                    // Use raw SQL to ensure proper locking
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE ProductVariants WITH (UPDLOCK) SET StockQuantity = StockQuantity - {0} WHERE VariantId = {1}",
                        detail.Quantity,
                        detail.VariantId
                    );

                    _logger.LogInformation($"Deducted {detail.Quantity} units from variant {detail.VariantId} for order {order.OrderCode}");
                }

                if (ownedTransaction != null)
                {
                    await ownedTransaction.CommitAsync();
                    lock (_lock)
                    {
                        _processedOrders.Add(orderId);
                    }
                }

                _logger.LogInformation($"Stock deducted successfully for order {order.OrderCode}");

                return true;
            }
            catch (Exception ex)
            {
                if (ownedTransaction != null)
                {
                    await ownedTransaction.RollbackAsync();
                }
                _logger.LogError(ex, $"Error deducting stock for order {orderId}");
                if (hasAmbientTransaction)
                {
                    throw;
                }
                return false;
            }
        }

        public async Task<bool> RestoreStockForOrderAsync(int orderId)
        {
            var hasAmbientTransaction = _context.Database.CurrentTransaction != null;
            await using var ownedTransaction = hasAmbientTransaction
                ? null
                : await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    _logger.LogWarning($"Order {orderId} not found");
                    return false;
                }

                // Restore stock
                foreach (var detail in order.OrderDetails)
                {
                    if (detail.Variant == null)
                    {
                        _logger.LogWarning($"Variant {detail.VariantId} not found for order detail {detail.OrderDetailId}");
                        continue;
                    }

                    detail.Variant.StockQuantity += detail.Quantity;
                    _logger.LogInformation($"Restored {detail.Quantity} units to variant {detail.VariantId} for order {order.OrderCode}");
                }

                await _context.SaveChangesAsync();
                if (ownedTransaction != null)
                {
                    await ownedTransaction.CommitAsync();
                    lock (_lock)
                    {
                        _processedOrders.Remove(orderId);
                    }
                }

                _logger.LogInformation($"Stock restored successfully for order {order.OrderCode}");

                return true;
            }
            catch (Exception ex)
            {
                if (ownedTransaction != null)
                {
                    await ownedTransaction.RollbackAsync();
                }
                _logger.LogError(ex, $"Error restoring stock for order {orderId}");
                if (hasAmbientTransaction)
                {
                    throw;
                }
                return false;
            }
        }

        public async Task<bool> CanFulfillOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return false;
                }

                foreach (var detail in order.OrderDetails)
                {
                    if (detail.Variant == null || detail.Variant.StockQuantity < detail.Quantity)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if order {orderId} can be fulfilled");
                return false;
            }
        }

        public async Task<bool> HasSufficientStock(int variantId, int quantity)
        {
            try
            {
                var variant = await _context.ProductVariants.FindAsync(variantId);
                return variant != null && variant.StockQuantity >= quantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking stock for variant {variantId}");
                return false;
            }
        }
    }
}

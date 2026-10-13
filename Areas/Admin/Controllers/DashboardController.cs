using FashionStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionStore.Models;
using FashionStore.ViewModels;

namespace FashionStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var deliveredOrders = _context.Orders.Where(order => order.Status == OrderStatus.Delivered);
            var monthRevenue = await deliveredOrders.Where(order => order.OrderDate >= thisMonthStart)
                .SumAsync(order => (decimal?)order.TotalAmount) ?? 0;
            var previousMonthRevenue = await deliveredOrders
                .Where(order => order.OrderDate >= lastMonthStart && order.OrderDate < thisMonthStart)
                .SumAsync(order => (decimal?)order.TotalAmount) ?? 0;
            var last7Days = Enumerable.Range(0, 7).Select(i => today.AddDays(-6 + i)).ToList();

            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            var topProducts = await _context.OrderDetails
                .Where(detail => detail.Order!.Status == OrderStatus.Delivered)
                .GroupBy(detail => new { detail.Variant!.ProductId, detail.ProductName })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductName,
                    TotalSold = g.Sum(detail => detail.Quantity),
                    Revenue = g.Sum(detail => detail.SubTotal)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            var revenueByDay = await deliveredOrders
                .Where(order => order.OrderDate >= today.AddDays(-6))
                .GroupBy(order => order.OrderDate.Date)
                .Select(group => new { Date = group.Key, Total = group.Sum(order => order.TotalAmount) })
                .ToListAsync();

            var lowStockItems = await _context.Products
                .Where(product => product.IsActive)
                .Select(product => new DashboardLowStockProduct
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    StockQuantity = product.Variants.Where(variant => variant.IsActive).Sum(variant => (int?)variant.StockQuantity) ?? 0
                })
                .Where(product => product.StockQuantity <= 10)
                .OrderBy(product => product.StockQuantity)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalRevenue = await deliveredOrders.SumAsync(order => (decimal?)order.TotalAmount) ?? 0,
                MonthRevenue = monthRevenue,
                RevenueChangePercent = previousMonthRevenue == 0 ? 0 : Math.Round((monthRevenue - previousMonthRevenue) / previousMonthRevenue * 100, 1),
                TotalOrders = await _context.Orders.CountAsync(),
                MonthOrders = await _context.Orders.CountAsync(order => order.OrderDate >= thisMonthStart),
                TotalProducts = await _context.Products.CountAsync(product => product.IsActive),
                TotalCustomers = await _context.Users.CountAsync(user => user.Role != null && user.Role.RoleName == "Customer"),
                PendingOrders = await _context.Orders.CountAsync(order => order.Status == OrderStatus.Pending || order.Status == OrderStatus.Confirmed),
                ProcessingOrders = await _context.Orders.CountAsync(order => order.Status == OrderStatus.Processing),
                ShippingOrders = await _context.Orders.CountAsync(order => order.Status == OrderStatus.Shipping),
                LowStockProducts = lowStockItems.Count,
                PendingReviews = await _context.Reviews.CountAsync(review => !review.IsApproved),
                RecentOrders = recentOrders,
                TopProducts = topProducts.Select(product => new DashboardTopProduct { ProductId = product.ProductId, ProductName = product.ProductName, TotalSold = product.TotalSold, Revenue = product.Revenue }).ToList(),
                LowStockItems = lowStockItems,
                ChartLabels = last7Days.Select(day => day.ToString("dd/MM")).ToArray(),
                ChartData = last7Days.Select(day => revenueByDay.FirstOrDefault(item => item.Date == day)?.Total ?? 0).ToArray()
            };

            return View(model);
        }
    }
}

using FashionStore.Models;

namespace FashionStore.ViewModels;

public class AdminDashboardViewModel
{
    public decimal TotalRevenue { get; init; }
    public decimal MonthRevenue { get; init; }
    public decimal RevenueChangePercent { get; init; }
    public int TotalOrders { get; init; }
    public int MonthOrders { get; init; }
    public int TotalProducts { get; init; }
    public int TotalCustomers { get; init; }
    public int PendingOrders { get; init; }
    public int ProcessingOrders { get; init; }
    public int ShippingOrders { get; init; }
    public int LowStockProducts { get; init; }
    public int PendingReviews { get; init; }
    public List<Order> RecentOrders { get; init; } = [];
    public List<DashboardTopProduct> TopProducts { get; init; } = [];
    public List<DashboardLowStockProduct> LowStockItems { get; init; } = [];
    public string[] ChartLabels { get; init; } = [];
    public decimal[] ChartData { get; init; } = [];
}

public class DashboardTopProduct
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int TotalSold { get; init; }
    public decimal Revenue { get; init; }
}

public class DashboardLowStockProduct
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
}

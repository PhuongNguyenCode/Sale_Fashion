namespace FashionStore.ViewModels;

public class AdminSalesReportViewModel
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public int OrderCount { get; init; }
    public int DeliveredOrderCount { get; init; }
    public int CancelledOrderCount { get; init; }
    public decimal DeliveredRevenue { get; init; }
    public decimal AverageOrderValue { get; init; }
    public List<AdminDailyRevenue> DailyRevenue { get; init; } = [];
}

public class AdminDailyRevenue
{
    public DateTime Date { get; init; }
    public int Orders { get; init; }
    public decimal Revenue { get; init; }
}

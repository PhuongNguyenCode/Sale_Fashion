using System.Text;
using FashionStore.Data;
using FashionStore.Models;
using FashionStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReportsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
    {
        var (from, to) = NormalizeRange(fromDate, toDate);
        var orders = context.Orders.Where(order => order.OrderDate >= from && order.OrderDate < to.AddDays(1));
        var deliveredOrders = orders.Where(order => order.Status == OrderStatus.Delivered);
        var dailyTotals = await deliveredOrders
            .GroupBy(order => order.OrderDate.Date)
            .Select(group => new { Date = group.Key, Orders = group.Count(), Revenue = group.Sum(order => order.TotalAmount) })
            .ToListAsync();

        var allOrders = await orders.CountAsync();
        var deliveredCount = await deliveredOrders.CountAsync();
        var revenue = await deliveredOrders.SumAsync(order => (decimal?)order.TotalAmount) ?? 0;
        return View(new AdminSalesReportViewModel
        {
            FromDate = from, ToDate = to, OrderCount = allOrders, DeliveredOrderCount = deliveredCount,
            CancelledOrderCount = await orders.CountAsync(order => order.Status == OrderStatus.Cancelled),
            DeliveredRevenue = revenue, AverageOrderValue = deliveredCount == 0 ? 0 : Math.Round(revenue / deliveredCount, 0),
            DailyRevenue = Enumerable.Range(0, (to - from).Days + 1).Select(offset => from.AddDays(offset)).Select(day =>
            {
                var total = dailyTotals.FirstOrDefault(item => item.Date == day);
                return new AdminDailyRevenue { Date = day, Orders = total?.Orders ?? 0, Revenue = total?.Revenue ?? 0 };
            }).ToList()
        });
    }

    public async Task<FileResult> ExportOrders(DateTime? fromDate, DateTime? toDate)
    {
        var (from, to) = NormalizeRange(fromDate, toDate);
        var orders = await context.Orders.Where(order => order.OrderDate >= from && order.OrderDate < to.AddDays(1))
            .OrderByDescending(order => order.OrderDate)
            .Select(order => new { order.OrderCode, order.OrderDate, order.FullName, order.Phone, order.TotalAmount, order.Status, order.PaymentMethod, order.PaymentStatus })
            .ToListAsync();
        var csv = new StringBuilder("\uFEFFMã đơn,Ngày đặt,Khách hàng,Số điện thoại,Tổng tiền,Trạng thái,Phương thức TT,Trạng thái TT\r\n");
        foreach (var order in orders)
            csv.AppendLine(string.Join(',', Csv(order.OrderCode), Csv(order.OrderDate.ToString("dd/MM/yyyy HH:mm")), Csv(order.FullName), Csv(order.Phone), order.TotalAmount, Csv(order.Status), Csv(order.PaymentMethod), Csv(order.PaymentStatus)));
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"don-hang-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    private static (DateTime From, DateTime To) NormalizeRange(DateTime? fromDate, DateTime? toDate)
    {
        var to = (toDate ?? DateTime.Today).Date;
        var from = (fromDate ?? to.AddDays(-29)).Date;
        if (from > to) (from, to) = (to, from);
        if ((to - from).Days > 365) from = to.AddDays(-365);
        return (from, to);
    }

    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}

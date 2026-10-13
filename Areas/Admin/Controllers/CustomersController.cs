using FashionStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        const int pageSize = 15;
        page = Math.Max(1, page);

        var query = _context.Users
            .Include(user => user.Role)
            .Where(user => user.Role != null && user.Role.RoleName == "Customer");

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim().ToLower();
            query = query.Where(user => user.FullName.ToLower().Contains(keyword)
                || user.Email.ToLower().Contains(keyword)
                || (user.Phone != null && user.Phone.Contains(keyword)));
        }

        var totalCount = await query.CountAsync();
        ViewBag.Search = q;
        ViewBag.TotalCount = totalCount;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(await query
            .OrderByDescending(user => user.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var customer = await _context.Users.Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.UserId == id && user.Role != null && user.Role.RoleName == "Customer");
        if (customer == null) return NotFound();

        customer.IsActive = !customer.IsActive;
        customer.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["Success"] = customer.IsActive ? "Đã kích hoạt tài khoản khách hàng." : "Đã tạm khóa tài khoản khách hàng.";
        return RedirectToAction(nameof(Index));
    }
}

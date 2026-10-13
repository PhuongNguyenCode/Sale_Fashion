using FashionStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? status, string? q, int page = 1)
    {
        const int pageSize = 15;
        page = Math.Max(page, 1);
        var query = context.Reviews
            .Include(review => review.User)
            .Include(review => review.Product)
            .AsQueryable();

        if (status == "pending") query = query.Where(review => !review.IsApproved);
        if (status == "approved") query = query.Where(review => review.IsApproved);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim().ToLower();
            query = query.Where(review => review.Product!.ProductName.ToLower().Contains(keyword)
                || review.User!.FullName.ToLower().Contains(keyword)
                || (review.Comment != null && review.Comment.ToLower().Contains(keyword)));
        }

        var totalCount = await query.CountAsync();
        ViewBag.Status = status;
        ViewBag.Search = q;
        ViewBag.TotalCount = totalCount;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(await query.OrderByDescending(review => review.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleApproval(int id)
    {
        var review = await context.Reviews.FindAsync(id);
        if (review == null) return NotFound();

        review.IsApproved = !review.IsApproved;
        review.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        TempData["Success"] = review.IsApproved ? "Đã duyệt đánh giá." : "Đã ẩn đánh giá.";
        return RedirectToAction(nameof(Index));
    }
}

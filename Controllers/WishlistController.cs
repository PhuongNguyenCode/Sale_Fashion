using FashionStore.Data;
using FashionStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FashionStore.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly ApplicationDbContext _context;
    public WishlistController(ApplicationDbContext context) => _context = context;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var items = await _context.WishlistItems
            .Include(x => x.Product).ThenInclude(p => p!.Brand)
            .Where(x => x.Wishlist!.UserId == UserId && x.Product!.IsActive)
            .OrderByDescending(x => x.AddedAt)
            .ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
    {
        if (!await _context.Products.AnyAsync(p => p.ProductId == productId && p.IsActive)) return NotFound();
        var wishlist = await _context.Wishlists.Include(w => w.WishlistItems)
            .FirstOrDefaultAsync(w => w.UserId == UserId);
        if (wishlist == null)
        {
            wishlist = new Wishlist { UserId = UserId };
            _context.Wishlists.Add(wishlist);
        }
        else
        {
            var existing = wishlist.WishlistItems.FirstOrDefault(i => i.ProductId == productId);
            if (existing != null) _context.WishlistItems.Remove(existing);
            else wishlist.WishlistItems.Add(new WishlistItem { ProductId = productId });
        }
        await _context.SaveChangesAsync();
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }
}

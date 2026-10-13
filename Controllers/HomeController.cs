using FashionStore.Data;
using FashionStore.Models;
using FashionStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace FashionStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();
            var wishlistProductIds = await GetWishlistProductIdsAsync();

            // Lấy danh mục nổi bật (top 4)
            viewModel.FeaturedCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Take(4)
                .ToListAsync();

            // Lấy thương hiệu nổi bật
            viewModel.FeaturedBrands = await _context.Brands
                .Where(b => b.IsActive)
                .Take(6)
                .ToListAsync();

            // Helper để map Product sang ProductCardViewModel
            var mapToCard = (Product p) => new ProductCardViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Slug = p.Slug,
                MainImageUrl = p.MainImageUrl,
                Price = p.Price,
                SalePrice = p.SalePrice,
                BrandName = p.Brand?.BrandName ?? "",
                AverageRating = p.Reviews.Where(review => review.IsApproved).Select(review => (double?)review.Rating).Average() ?? 0,
                ReviewCount = p.Reviews.Count(review => review.IsApproved),
                IsInWishlist = wishlistProductIds.Contains(p.ProductId)
            };

            // Lấy sản phẩm mới (New Arrivals) - Sắp xếp theo CreatedAt giảm dần
            var newArrivals = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.ProductId)
                .Take(8)
                .ToListAsync();
            viewModel.NewArrivals = newArrivals.Select(mapToCard).ToList();
            var shownProductIds = newArrivals.Select(p => p.ProductId).ToHashSet();

            // Products ranked by views; avoid repeating items already shown above.
            var bestSellers = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive && p.IsFeatured && !shownProductIds.Contains(p.ProductId))
                .OrderByDescending(p => p.ViewCount)
                .Take(8)
                .ToListAsync();
            viewModel.BestSellers = bestSellers.Select(mapToCard).ToList();
            shownProductIds.UnionWith(bestSellers.Select(p => p.ProductId));

            // Keep the real highest discount for the campaign headline, even
            // when every discounted product is already shown in another section.
            var saleProducts = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive && p.SalePrice != null && p.Price > 0)
                .OrderByDescending(p => (p.Price - p.SalePrice) / p.Price)
                .Take(8)
                .ToListAsync();
            var saleCards = saleProducts.Select(mapToCard).ToList();
            viewModel.MaxSaleDiscountPercent = saleCards.Select(p => p.DiscountPercent ?? 0).DefaultIfEmpty(0).Max();
            viewModel.SaleProducts = saleCards
                .Where(p => !shownProductIds.Contains(p.ProductId))
                .Take(4)
                .ToList();

            return View(viewModel);
        }

        private async Task<HashSet<int>> GetWishlistProductIdsAsync()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return [];
            }

            return (await _context.WishlistItems
                .Where(item => item.Wishlist!.UserId == userId)
                .Select(item => item.ProductId)
                .ToListAsync())
                .ToHashSet();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

using FashionStore.Data;
using FashionStore.Models;
using FashionStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FashionStore.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            int? categoryId, 
            int? brandId, 
            int? sizeId, 
            int? colorId, 
            decimal? minPrice, 
            decimal? maxPrice, 
            string? q, 
            bool onSale = false,
            string sortBy = "newest",
            int page = 1)
        {
            page = Math.Max(1, page);
            int pageSize = 12;
            var viewModel = new ProductListViewModel
            {
                CategoryId = categoryId,
                BrandId = brandId,
                SizeId = sizeId,
                ColorId = colorId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                OnSale = onSale,
                SearchQuery = q,
                SortBy = sortBy,
                CurrentPage = page,
                PageSize = pageSize
            };

            // 1. Get filter lookup data
            viewModel.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
            viewModel.Brands = await _context.Brands.Where(b => b.IsActive).OrderBy(b => b.BrandName).ToListAsync();
            viewModel.Sizes = await _context.Sizes.OrderBy(s => s.DisplayOrder).ToListAsync();
            viewModel.Colors = await _context.Colors.OrderBy(c => c.ColorName).ToListAsync();

            var wishlistProductIds = await GetWishlistProductIdsAsync();

            if (categoryId.HasValue)
                viewModel.CategoryName = viewModel.Categories.FirstOrDefault(c => c.CategoryId == categoryId)?.CategoryName;
            if (brandId.HasValue)
                viewModel.BrandName = viewModel.Brands.FirstOrDefault(b => b.BrandId == brandId)?.BrandName;

            // 2. Build the query
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            // 3. Apply Filters
            if (!string.IsNullOrWhiteSpace(q))
            {
                string searchLower = q.Trim().ToLower();
                query = query.Where(p => 
                    p.ProductName.ToLower().Contains(searchLower) || 
                    p.Slug.ToLower().Contains(searchLower) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)) ||
                    (p.Brand != null && p.Brand.BrandName.ToLower().Contains(searchLower)));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);
            
            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId.Value);

            if (onSale)
                query = query.Where(p => p.SalePrice.HasValue && p.SalePrice.Value < p.Price);

            // Filter by Variant (Size/Color)
            if (sizeId.HasValue || colorId.HasValue)
            {
                query = query.Where(p => p.Variants.Any(v => 
                    v.IsActive && v.StockQuantity > 0 &&
                    (!sizeId.HasValue || v.SizeId == sizeId.Value) &&
                    (!colorId.HasValue || v.ColorId == colorId.Value)));
            }

            // Filter by Price (using computed price logic roughly in SQL or DB if possible)
            // Note: SalePrice is nullable. We check SalePrice if not null, else Price.
            if (minPrice.HasValue)
            {
                query = query.Where(p => (p.SalePrice != null ? p.SalePrice.Value : p.Price) >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => (p.SalePrice != null ? p.SalePrice.Value : p.Price) <= maxPrice.Value);
            }

            // 4. Sorting
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.SalePrice != null ? p.SalePrice : p.Price),
                "price_desc" => query.OrderByDescending(p => p.SalePrice != null ? p.SalePrice : p.Price),
                "best_selling" => query.OrderByDescending(p => p.ViewCount), // using ViewCount as proxy for now
                "rating" => query.OrderByDescending(p => p.Reviews.Any(r => r.IsApproved) ? p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) : 0),
                _ => query.OrderByDescending(p => p.CreatedAt) // newest default
            };

            // 5. Pagination
            viewModel.TotalItems = await query.CountAsync();
            viewModel.TotalPages = (int)Math.Ceiling(viewModel.TotalItems / (double)pageSize);

            // 6. Execute and project
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductCardViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    MainImageUrl = p.MainImageUrl,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    BrandName = p.Brand != null ? p.Brand.BrandName : "",
                    AverageRating = p.Reviews.Where(review => review.IsApproved).Select(review => (double?)review.Rating).Average() ?? 0,
                    ReviewCount = p.Reviews.Count(review => review.IsApproved)
                })
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsInWishlist = wishlistProductIds.Contains(product.ProductId);
            }

            viewModel.Products = products;

            return View(viewModel);
        }

        [Route("products/{slug}")]
        public async Task<IActionResult> Detail(string slug)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants).ThenInclude(v => v.Size)
                .Include(p => p.Variants).ThenInclude(v => v.Color)
                .Include(p => p.Reviews.Where(review => review.IsApproved)).ThenInclude(review => review.User)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            // Tăng ViewCount
            product.ViewCount++;
            await _context.SaveChangesAsync();

            var wishlistProductIds = await GetWishlistProductIdsAsync();

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                Images = product.Images.Where(image => !string.IsNullOrWhiteSpace(image.ImageUrl)).OrderBy(image => image.DisplayOrder).ToList(),
                Variants = product.Variants.Where(v => v.IsActive).ToList(),
                AvailableSizes = product.Variants.Where(v => v.IsActive && v.StockQuantity > 0 && v.Size != null).Select(v => v.Size!).DistinctBy(s => s.SizeId).OrderBy(s => s.DisplayOrder).ToList(),
                AvailableColors = product.Variants.Where(v => v.IsActive && v.StockQuantity > 0 && v.Color != null).Select(v => v.Color!).DistinctBy(c => c.ColorId).OrderBy(c => c.ColorName).ToList(),
                Reviews = product.Reviews.OrderByDescending(review => review.CreatedAt).ToList(),
                AverageRating = product.Reviews.Any() ? product.Reviews.Average(review => review.Rating) : 0,
                ReviewCount = product.Reviews.Count,
                RatingDistribution = Enumerable.Range(1, 5).ToDictionary(rating => rating, rating => product.Reviews.Count(review => review.Rating == rating)),
                IsInWishlist = wishlistProductIds.Contains(product.ProductId)
            };

            // Get related products
            viewModel.RelatedProducts = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .Take(4)
                .Select(p => new ProductCardViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    MainImageUrl = p.MainImageUrl,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    BrandName = p.Brand != null ? p.Brand.BrandName : "",
                    AverageRating = p.Reviews.Where(review => review.IsApproved).Select(review => (double?)review.Rating).Average() ?? 0,
                    ReviewCount = p.Reviews.Count(review => review.IsApproved)
                })
                .ToListAsync();

            foreach (var relatedProduct in viewModel.RelatedProducts)
            {
                relatedProduct.IsInWishlist = wishlistProductIds.Contains(relatedProduct.ProductId);
            }

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
    }
}

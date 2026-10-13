using FashionStore.Data;
using FashionStore.Models;
using FashionStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            page = Math.Max(1, page);
            int pageSize = 10;
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                string ql = q.ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(ql) || p.Slug.ToLower().Contains(ql));
            }

            ViewBag.Search = q;
            ViewBag.TotalCount = await query.CountAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(ViewBag.TotalCount / (double)pageSize);

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryId", "CategoryName");
            ViewBag.Brands = new SelectList(await _context.Brands.Where(b => b.IsActive).ToListAsync(), "BrandId", "BrandName");
            return View(new AdminProductViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminProductViewModel model)
        {
            NormalizeAndValidate(model);
            await ValidateProductReferencesAsync(model);
            ValidateImage(model.ImageFile);
            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    ProductName = model.ProductName,
                    Slug = model.Slug,
                    CategoryId = model.CategoryId,
                    BrandId = model.BrandId,
                    Description = model.Description,
                    Price = model.Price,
                    SalePrice = model.SalePrice,
                    IsActive = model.IsActive,
                    IsFeatured = model.IsFeatured,
                    CreatedAt = DateTime.Now
                };

                // Handle Image Upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                    product.MainImageUrl = "/images/products/" + uniqueFileName;
                }
                else
                {
                    product.MainImageUrl = "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?q=80&w=1000&auto=format&fit=crop"; // fallback
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã tạo sản phẩm. Hãy thêm biến thể size/màu và tồn kho để sản phẩm có thể bán.";
                return RedirectToAction(nameof(Variants), new { id = product.ProductId });
            }

            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryId", "CategoryName", model.CategoryId);
            ViewBag.Brands = new SelectList(await _context.Brands.Where(b => b.IsActive).ToListAsync(), "BrandId", "BrandName", model.BrandId);
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var model = new AdminProductViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Slug = product.Slug,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                Description = product.Description,
                Price = product.Price,
                SalePrice = product.SalePrice,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                MainImageUrl = product.MainImageUrl
            };

            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryId", "CategoryName", model.CategoryId);
            ViewBag.Brands = new SelectList(await _context.Brands.Where(b => b.IsActive).ToListAsync(), "BrandId", "BrandName", model.BrandId);
            
            return View(model);
        }

        public async Task<IActionResult> Variants(int id)
        {
            var product = await _context.Products
                .Include(product => product.Category)
                .Include(product => product.Brand)
                .FirstOrDefaultAsync(product => product.ProductId == id);
            if (product == null) return NotFound();

            return View(new AdminProductVariantsViewModel
            {
                Product = product,
                Variants = await _context.ProductVariants
                    .Where(variant => variant.ProductId == id)
                    .Include(variant => variant.Size)
                    .Include(variant => variant.Color)
                    .OrderBy(variant => variant.Size!.DisplayOrder)
                    .ThenBy(variant => variant.Color!.ColorName)
                    .ToListAsync(),
                Sizes = await _context.Sizes.OrderBy(size => size.DisplayOrder).ToListAsync(),
                Colors = await _context.Colors.OrderBy(color => color.ColorName).ToListAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariant(int productId, int sizeId, int colorId, int stockQuantity, decimal additionalPrice, string? sku)
        {
            if (stockQuantity < 0 || additionalPrice < 0)
            {
                TempData["Error"] = "Tồn kho và giá cộng thêm không được âm.";
                return RedirectToAction(nameof(Variants), new { id = productId });
            }

            var size = await _context.Sizes.FindAsync(sizeId);
            var color = await _context.Colors.FindAsync(colorId);
            if (await _context.Products.AnyAsync(product => product.ProductId == productId) && size != null && color != null)
            {
                var exists = await _context.ProductVariants.AnyAsync(variant => variant.ProductId == productId && variant.SizeId == sizeId && variant.ColorId == colorId);
                if (!exists)
                {
                    _context.ProductVariants.Add(new ProductVariant
                    {
                        ProductId = productId, SizeId = sizeId, ColorId = colorId, StockQuantity = stockQuantity,
                        AdditionalPrice = additionalPrice, SKU = string.IsNullOrWhiteSpace(sku) ? $"P{productId}-S{sizeId}-C{colorId}" : sku.Trim(), IsActive = true
                    });
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đã thêm biến thể sản phẩm.";
                    return RedirectToAction(nameof(Variants), new { id = productId });
                }
                TempData["Error"] = "Biến thể size và màu này đã tồn tại.";
            }
            else TempData["Error"] = "Dữ liệu biến thể không hợp lệ.";
            return RedirectToAction(nameof(Variants), new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVariant(int productId, int variantId, int stockQuantity, decimal additionalPrice, string? sku, bool isActive)
        {
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(item => item.VariantId == variantId && item.ProductId == productId);
            if (variant == null) return NotFound();
            if (stockQuantity < 0 || additionalPrice < 0)
            {
                TempData["Error"] = "Tồn kho và giá cộng thêm không được âm.";
                return RedirectToAction(nameof(Variants), new { id = productId });
            }

            variant.StockQuantity = stockQuantity;
            variant.AdditionalPrice = additionalPrice;
            variant.SKU = (sku ?? string.Empty).Trim();
            variant.IsActive = isActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật biến thể.";
            return RedirectToAction(nameof(Variants), new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminProductViewModel model)
        {
            NormalizeAndValidate(model);
            await ValidateProductReferencesAsync(model);
            ValidateImage(model.ImageFile);
            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null) return NotFound();

                product.ProductName = model.ProductName;
                product.Slug = model.Slug;
                product.CategoryId = model.CategoryId;
                product.BrandId = model.BrandId;
                product.Description = model.Description;
                product.Price = model.Price;
                product.SalePrice = model.SalePrice;
                product.IsActive = model.IsActive;
                product.IsFeatured = model.IsFeatured;
                product.UpdatedAt = DateTime.Now;

                // Handle Image Upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                    product.MainImageUrl = "/images/products/" + uniqueFileName;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryId", "CategoryName", model.CategoryId);
            ViewBag.Brands = new SelectList(await _context.Brands.Where(b => b.IsActive).ToListAsync(), "BrandId", "BrandName", model.BrandId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsActive = !product.IsActive;
            product.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["Success"] = product.IsActive ? "Đã hiển thị lại sản phẩm." : "Đã tạm ẩn sản phẩm.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePermanently(int id)
        {
            var product = await _context.Products
                .Include(item => item.Variants)
                .Include(item => item.Images)
                .Include(item => item.Reviews)
                .Include(item => item.WishlistItems)
                .FirstOrDefaultAsync(item => item.ProductId == id);
            if (product == null) return NotFound();

            var hasOrderHistory = await _context.OrderDetails.AnyAsync(detail => detail.Variant!.ProductId == id);
            if (hasOrderHistory)
            {
                TempData["Error"] = "Không thể xóa vĩnh viễn sản phẩm đã có trong đơn hàng. Hãy dùng Tạm ẩn để giữ lịch sử giao dịch.";
                return RedirectToAction(nameof(Index));
            }

            var cartItems = await _context.CartItems
                .Where(item => item.Variant!.ProductId == id)
                .ToListAsync();

            // These records have no financial-history value and must be removed before
            // deleting variants/product because their foreign keys are restrictive.
            _context.CartItems.RemoveRange(cartItems);
            _context.WishlistItems.RemoveRange(product.WishlistItems);
            _context.Reviews.RemoveRange(product.Reviews);
            _context.ProductImages.RemoveRange(product.Images);
            _context.ProductVariants.RemoveRange(product.Variants);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa vĩnh viễn sản phẩm và dữ liệu liên quan có thể xóa an toàn.";
            return RedirectToAction(nameof(Index));
        }

        private void NormalizeAndValidate(AdminProductViewModel model)
        {
            model.ProductName = model.ProductName.Trim();
            model.Slug = model.Slug.Trim().ToLowerInvariant();

            if (model.SalePrice.HasValue && model.SalePrice.Value >= model.Price)
                ModelState.AddModelError(nameof(model.SalePrice), "Giá khuyến mãi phải thấp hơn giá gốc.");
        }

        private async Task ValidateProductReferencesAsync(AdminProductViewModel model)
        {
            if (!await _context.Categories.AnyAsync(category => category.CategoryId == model.CategoryId && category.IsActive))
                ModelState.AddModelError(nameof(model.CategoryId), "Danh mục không tồn tại hoặc đã bị ẩn.");
            if (!await _context.Brands.AnyAsync(brand => brand.BrandId == model.BrandId && brand.IsActive))
                ModelState.AddModelError(nameof(model.BrandId), "Thương hiệu không tồn tại hoặc đã bị ẩn.");
            if (await _context.Products.AnyAsync(product => product.ProductId != model.ProductId && product.Slug == model.Slug))
                ModelState.AddModelError(nameof(model.Slug), "Slug này đã được sử dụng.");
        }

        private void ValidateImage(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension) || !imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(AdminProductViewModel.ImageFile), "Chỉ chấp nhận ảnh JPG, JPEG, PNG, WEBP hoặc GIF.");
            else if (imageFile.Length > 5 * 1024 * 1024)
                ModelState.AddModelError(nameof(AdminProductViewModel.ImageFile), "Dung lượng ảnh tối đa là 5 MB.");
        }
    }
}

using FashionStore.Data;
using FashionStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BrandsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BrandsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var brands = await _context.Brands
                .OrderBy(b => b.BrandName)
                .ToListAsync();
            return View(brands);
        }

        public IActionResult Create()
        {
            return View(new Brand { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand, IFormFile? imageFile)
        {
            Normalize(brand);
            await ValidateSlugAsync(brand);
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "brands");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    brand.LogoUrl = "/images/brands/" + uniqueFileName;
                }

                _context.Brands.Add(brand);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm thương hiệu thành công!";
                return RedirectToAction("Index");
            }
            return View(brand);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Brand model, IFormFile? imageFile)
        {
            Normalize(model);
            await ValidateSlugAsync(model);
            if (ModelState.IsValid)
            {
                var brand = await _context.Brands.FindAsync(model.BrandId);
                if (brand == null) return NotFound();

                brand.BrandName = model.BrandName;
                brand.Slug = model.Slug;
                brand.Description = model.Description;
                brand.IsActive = model.IsActive;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "brands");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    brand.LogoUrl = "/images/brands/" + uniqueFileName;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thương hiệu thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                bool hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
                if (hasProducts)
                {
                    TempData["Error"] = "Không thể xóa thương hiệu đã có sản phẩm. Hãy chọn trạng thái 'Ẩn' thay thế.";
                }
                else
                {
                    _context.Brands.Remove(brand);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Xóa thương hiệu thành công!";
                }
            }
            return RedirectToAction("Index");
        }

        private static void Normalize(Brand brand)
        {
            brand.BrandName = brand.BrandName.Trim();
            brand.Slug = brand.Slug.Trim().ToLowerInvariant();
        }

        private async Task ValidateSlugAsync(Brand brand)
        {
            if (await _context.Brands.AnyAsync(item => item.BrandId != brand.BrandId && item.Slug == brand.Slug))
                ModelState.AddModelError(nameof(brand.Slug), "Slug này đã được sử dụng.");
        }
    }
}

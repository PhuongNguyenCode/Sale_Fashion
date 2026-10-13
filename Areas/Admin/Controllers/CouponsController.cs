using FashionStore.Data;
using FashionStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponsController : Controller
{
    private readonly ApplicationDbContext _context;

    public CouponsController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
        => View(await _context.Coupons.OrderByDescending(coupon => coupon.CreatedAt).ToListAsync());

    public IActionResult Create() => View(new Coupon { IsActive = true, StartDate = DateTime.Today });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Coupon coupon)
    {
        NormalizeCoupon(coupon);
        await ValidateCouponAsync(coupon);
        if (!ModelState.IsValid) return View(coupon);

        coupon.CreatedAt = DateTime.Now;
        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã tạo mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        return coupon == null ? NotFound() : View(coupon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Coupon model)
    {
        var coupon = await _context.Coupons.FindAsync(model.CouponId);
        if (coupon == null) return NotFound();

        NormalizeCoupon(model);
        await ValidateCouponAsync(model);
        if (!ModelState.IsValid) return View(model);

        coupon.CouponCode = model.CouponCode;
        coupon.Description = model.Description;
        coupon.DiscountType = model.DiscountType;
        coupon.DiscountValue = model.DiscountValue;
        coupon.MinOrderAmount = model.MinOrderAmount;
        coupon.MaxDiscountAmount = model.MaxDiscountAmount;
        coupon.UsageLimit = model.UsageLimit;
        coupon.StartDate = model.StartDate;
        coupon.EndDate = model.EndDate;
        coupon.IsActive = model.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon == null) return NotFound();

        coupon.IsActive = !coupon.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = coupon.IsActive ? "Đã bật mã giảm giá." : "Đã tắt mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateCouponAsync(Coupon coupon)
    {
        if (coupon.DiscountType is not ("Percent" or "Amount"))
            ModelState.AddModelError(nameof(coupon.DiscountType), "Loại giảm giá không hợp lệ.");
        if (coupon.DiscountType == "Percent" && coupon.DiscountValue > 100)
            ModelState.AddModelError(nameof(coupon.DiscountValue), "Phần trăm giảm không được vượt quá 100%.");
        if (coupon.EndDate.HasValue && coupon.StartDate.HasValue && coupon.EndDate < coupon.StartDate)
            ModelState.AddModelError(nameof(coupon.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
        if (coupon.UsageLimit is < 0)
            ModelState.AddModelError(nameof(coupon.UsageLimit), "Giới hạn sử dụng không được âm.");
        if (await _context.Coupons.AnyAsync(item => item.CouponId != coupon.CouponId && item.CouponCode == coupon.CouponCode))
            ModelState.AddModelError(nameof(coupon.CouponCode), "Mã giảm giá này đã tồn tại.");
    }

    private static void NormalizeCoupon(Coupon coupon)
    {
        coupon.CouponCode = coupon.CouponCode.Trim().ToUpperInvariant();
        coupon.DiscountType = coupon.DiscountType.Trim();
    }
}

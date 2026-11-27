using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public CategoriesController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories.Include(c => c.Products).OrderBy(c => c.Name).ToListAsync();
        var brands = await _context.Brands.Include(b => b.Products).OrderBy(b => b.Name).ToListAsync();
        var units = await _context.Units.OrderBy(u => u.Name).ToListAsync();

        ViewBag.Brands = brands;
        ViewBag.Units = units;

        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string? code, string? description)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var category = new Category
        {
            BusinessId = businessId.Value,
            Name = name.Trim(),
            Code = string.IsNullOrWhiteSpace(code) ? name[..Math.Min(3, name.Length)].ToUpper() : code.Trim().ToUpper()
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category '{category.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBrand(string name)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var brand = new Brand
        {
            BusinessId = businessId.Value,
            Name = name.Trim()
        };

        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Brand '{brand.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUnit(string name, string shortCode, bool allowDecimals)
    {
        var unit = new Unit
        {
            Name = name.Trim(),
            ShortCode = shortCode.Trim(),
            AllowDecimals = allowDecimals
        };

        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Unit '{unit.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }
}

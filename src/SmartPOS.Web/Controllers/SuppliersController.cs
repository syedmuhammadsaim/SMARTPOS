using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Purchasing;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class SuppliersController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public SuppliersController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.LedgerEntries)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";

        return View(suppliers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? phone, string? email, string? address, string? contactPerson)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var supplier = new Supplier
        {
            BusinessId = businessId.Value,
            Name = name.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            ContactPerson = contactPerson?.Trim(),
            Status = "Active"
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Supplier '{supplier.Name}' added successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Ledger(Guid id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.LedgerEntries)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null) return NotFound();

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";

        return View(supplier);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class BranchesController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public BranchesController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var branches = await _context.Branches
            .Include(b => b.Counters)
            .OrderBy(b => b.Code)
            .ToListAsync();
        return View(branches);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string code, string name, string? address, string? contact, string? invoicePrefix)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var branch = new Branch
        {
            BusinessId = businessId.Value,
            Code = code.Trim().ToUpper(),
            Name = name.Trim(),
            Address = address?.Trim(),
            Contact = contact?.Trim(),
            InvoicePrefix = string.IsNullOrWhiteSpace(invoicePrefix) ? "INV" : invoicePrefix.Trim().ToUpper(),
            Status = "Active"
        };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        // Also create default warehouse for this branch
        var warehouse = new SmartPOS.Domain.Entities.Inventory.Warehouse
        {
            BusinessId = businessId.Value,
            BranchId = branch.Id,
            Code = $"WH-{branch.Code}",
            Name = $"{branch.Name} Store Room",
            Status = "Active"
        };
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Branch '{branch.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var branch = await _context.Branches.FindAsync(id);
        if (branch == null) return NotFound();

        branch.Status = branch.Status == "Active" ? "Inactive" : "Active";
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Branch '{branch.Name}' status updated to {branch.Status}.";
        return RedirectToAction(nameof(Index));
    }
}

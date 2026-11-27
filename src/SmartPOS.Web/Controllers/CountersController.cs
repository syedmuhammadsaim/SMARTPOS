using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class CountersController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public CountersController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var counters = await _context.Counters
            .Include(c => c.Branch)
            .OrderBy(c => c.Code)
            .ToListAsync();

        ViewBag.Branches = await _context.Branches.Where(b => b.Status == "Active").ToListAsync();
        return View(counters);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid branchId, string code, string name, string? printerName)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var counter = new Counter
        {
            BusinessId = businessId.Value,
            BranchId = branchId,
            Code = code.Trim().ToUpper(),
            Name = name.Trim(),
            PrinterName = string.IsNullOrWhiteSpace(printerName) ? "Default Thermal 80mm" : printerName.Trim(),
            Status = "Active"
        };

        _context.Counters.Add(counter);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Counter '{counter.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }
}

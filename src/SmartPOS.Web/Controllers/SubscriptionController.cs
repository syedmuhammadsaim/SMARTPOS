using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.SaaS;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class SubscriptionController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public SubscriptionController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var business = await _context.Businesses.FindAsync(businessId.Value);
        if (business == null) return NotFound();

        SubscriptionPlan? currentPlan = null;
        if (business.SubscriptionPlanId.HasValue)
        {
            currentPlan = await _context.SubscriptionPlans.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == business.SubscriptionPlanId.Value);
        }

        var availablePlans = await _context.SubscriptionPlans.IgnoreQueryFilters()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        // Calculate usage vs limits
        var branchCount = await _context.Branches.CountAsync();
        var counterCount = await _context.Counters.CountAsync();
        var productCount = await _context.Products.CountAsync();

        ViewBag.Business = business;
        ViewBag.CurrentPlan = currentPlan;
        ViewBag.BranchCount = branchCount;
        ViewBag.CounterCount = counterCount;
        ViewBag.ProductCount = productCount;

        var invoices = await _context.SubscriptionInvoices
            .Where(i => i.BusinessId == businessId.Value)
            .OrderByDescending(i => i.IssueDateUtc)
            .ToListAsync();

        ViewBag.Invoices = invoices;

        return View(availablePlans);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePlan(Guid planId)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var business = await _context.Businesses.FindAsync(businessId.Value);
        var plan = await _context.SubscriptionPlans.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == planId);
        if (business == null || plan == null) return NotFound();

        business.SubscriptionPlanId = plan.Id;
        business.SubscriptionExpiresAtUtc = DateTime.UtcNow.AddMonths(1);

        // Record invoice
        var invoiceNo = $"SUB-INV-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        _context.SubscriptionInvoices.Add(new SubscriptionInvoice
        {
            BusinessId = business.Id,
            InvoiceNo = invoiceNo,
            SubTotal = plan.MonthlyPrice,
            TaxAmount = 0m,
            TotalAmount = plan.MonthlyPrice,
            Currency = plan.Currency,
            Status = "Paid",
            IssueDateUtc = DateTime.UtcNow,
            DueDateUtc = DateTime.UtcNow.AddDays(7),
            PaidDateUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Subscription successfully switched to '{plan.Name}'.";
        return RedirectToAction(nameof(Index));
    }
}

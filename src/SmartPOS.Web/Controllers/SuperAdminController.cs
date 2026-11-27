using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Domain.Entities.Identity;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.SaaS;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Identity;
using SmartPOS.Web.Models;

namespace SmartPOS.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public SuperAdminController(SmartPOSDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        // Platform SuperAdmin accesses all records via IgnoreQueryFilters()
        var totalCompanies = await _context.Businesses.IgnoreQueryFilters().CountAsync();
        var activeCompanies = await _context.Businesses.IgnoreQueryFilters().CountAsync(b => b.Status == "Active");
        var suspendedCompanies = await _context.Businesses.IgnoreQueryFilters().CountAsync(b => b.Status == "Suspended");
        var totalUsers = await _userManager.Users.CountAsync();
        var totalBranches = await _context.Branches.IgnoreQueryFilters().CountAsync();
        var totalSales = await _context.POSOrders.IgnoreQueryFilters().SumAsync(o => (decimal?)o.Total) ?? 0m;

        // Calculate Monthly Recurring Revenue (MRR)
        var activeBizPlanIds = await _context.Businesses
            .IgnoreQueryFilters()
            .Where(b => b.Status == "Active" && b.SubscriptionPlanId.HasValue)
            .Select(b => b.SubscriptionPlanId!.Value)
            .ToListAsync();

        var plans = await _context.SubscriptionPlans.IgnoreQueryFilters().ToListAsync();
        var mrr = activeBizPlanIds
            .Select(id => plans.FirstOrDefault(p => p.Id == id)?.MonthlyPrice ?? 0m)
            .Sum();

        var recentCompanies = await _context.Businesses
            .IgnoreQueryFilters()
            .OrderByDescending(b => b.CreatedAtUtc)
            .Take(8)
            .ToListAsync();

        var recentLogs = await _context.AuditLogs
            .IgnoreQueryFilters()
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(10)
            .ToListAsync();

        var vm = new SuperAdminDashboardViewModel
        {
            TotalCompanies = totalCompanies,
            ActiveCompanies = activeCompanies,
            SuspendedCompanies = suspendedCompanies,
            TotalUsers = totalUsers,
            TotalBranches = totalBranches,
            TotalPlatformSales = totalSales,
            MonthlyRecurringRevenue = mrr,
            RecentCompanies = recentCompanies,
            Plans = plans,
            RecentAuditLogs = recentLogs
        };

        return View(vm);
    }

    public async Task<IActionResult> Companies(string? search, string? status)
    {
        var query = _context.Businesses.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b => b.Name.ToLower().Contains(term) || (b.Code != null && b.Code.ToLower().Contains(term)) || (b.Email != null && b.Email.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        var companies = await query
            .Include(b => b.Branches)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync();

        var plans = await _context.SubscriptionPlans.IgnoreQueryFilters().ToDictionaryAsync(p => p.Id, p => p.Name);
        ViewBag.Plans = plans;
        ViewBag.Search = search;
        ViewBag.Status = status;

        return View(companies);
    }

    public async Task<IActionResult> CompanyDetails(Guid id)
    {
        var company = await _context.Businesses
            .IgnoreQueryFilters()
            .Include(b => b.Branches)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (company == null)
            return NotFound();

        SubscriptionPlan? plan = null;
        if (company.SubscriptionPlanId.HasValue)
        {
            plan = await _context.SubscriptionPlans.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == company.SubscriptionPlanId.Value);
        }

        var branches = await _context.Branches.IgnoreQueryFilters().Where(b => b.BusinessId == id).ToListAsync();
        var branchIds = branches.Select(b => b.Id).ToList();
        var counters = await _context.Counters.IgnoreQueryFilters().Where(c => c.BusinessId == id).ToListAsync();
        var users = await _userManager.Users.Where(u => u.BusinessId == id).ToListAsync();

        var salesOrders = await _context.POSOrders.IgnoreQueryFilters().Where(o => o.BusinessId == id).ToListAsync();
        var totalSalesCount = salesOrders.Count;
        var totalSalesAmount = salesOrders.Sum(o => o.Total);
        var totalProductsCount = await _context.Products.IgnoreQueryFilters().CountAsync(p => p.BusinessId == id);

        var vm = new CompanyDetailsViewModel
        {
            Company = company,
            Plan = plan,
            Branches = branches,
            Counters = counters,
            Users = users,
            TotalSalesCount = totalSalesCount,
            TotalSalesAmount = totalSalesAmount,
            TotalProductsCount = totalProductsCount
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCompanyStatus(Guid id)
    {
        var company = await _context.Businesses.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == id);
        if (company == null) return NotFound();

        var oldStatus = company.Status;
        company.Status = company.Status == "Active" ? "Suspended" : "Active";
        company.UpdatedAtUtc = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            BusinessId = company.Id,
            Action = "SUPERADMIN_STATUS_CHANGE",
            Entity = "Business",
            EntityId = company.Id.ToString(),
            OldValues = oldStatus,
            NewValues = company.Status,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Company '{company.Name}' status changed to {company.Status}.";

        return RedirectToAction(nameof(CompanyDetails), new { id });
    }

    public async Task<IActionResult> Plans()
    {
        var plans = await _context.SubscriptionPlans.IgnoreQueryFilters().OrderBy(p => p.SortOrder).ToListAsync();
        return View(plans);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePlan(SubscriptionPlan plan)
    {
        if (plan.Id == Guid.Empty)
        {
            plan.Id = Guid.NewGuid();
            plan.CreatedAtUtc = DateTime.UtcNow;
            _context.SubscriptionPlans.Add(plan);
            TempData["SuccessMessage"] = $"Subscription plan '{plan.Name}' created successfully.";
        }
        else
        {
            var existing = await _context.SubscriptionPlans.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == plan.Id);
            if (existing != null)
            {
                existing.Name = plan.Name;
                existing.Code = plan.Code;
                existing.Description = plan.Description;
                existing.MonthlyPrice = plan.MonthlyPrice;
                existing.AnnualPrice = plan.AnnualPrice;
                existing.Currency = plan.Currency;
                existing.MaxBranches = plan.MaxBranches;
                existing.MaxCounters = plan.MaxCounters;
                existing.MaxUsers = plan.MaxUsers;
                existing.MaxProducts = plan.MaxProducts;
                existing.HasAdvancedReports = plan.HasAdvancedReports;
                existing.HasFbrIntegration = plan.HasFbrIntegration;
                existing.HasBarcodePrinting = plan.HasBarcodePrinting;
                existing.HasMultiCounter = plan.HasMultiCounter;
                existing.HasApiAccess = plan.HasApiAccess;
                existing.TrialDays = plan.TrialDays;
                existing.SortOrder = plan.SortOrder;
                existing.IsPopular = plan.IsPopular;
                existing.IsActive = plan.IsActive;
                existing.UpdatedAtUtc = DateTime.UtcNow;
                TempData["SuccessMessage"] = $"Subscription plan '{plan.Name}' updated successfully.";
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Plans));
    }

    public async Task<IActionResult> Settings()
    {
        var settings = await _context.PlatformSettings.IgnoreQueryFilters().ToListAsync();
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSetting(string key, string value, string category, string? description)
    {
        var setting = await _context.PlatformSettings.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            _context.PlatformSettings.Add(new PlatformSetting
            {
                Key = key,
                Value = value,
                Category = category,
                Description = description,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            setting.Value = value;
            setting.Category = category;
            setting.Description = description;
            setting.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Platform setting '{key}' saved successfully.";
        return RedirectToAction(nameof(Settings));
    }

    public async Task<IActionResult> AuditLogs()
    {
        var logs = await _context.AuditLogs
            .IgnoreQueryFilters()
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(100)
            .ToListAsync();

        return View(logs);
    }
}

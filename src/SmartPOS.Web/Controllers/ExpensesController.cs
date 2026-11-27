using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Finance;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class ExpensesController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ExpensesController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var expenses = await _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.Branch)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync();

        ViewBag.Categories = await _context.ExpenseCategories.Where(c => c.IsActive).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.Status == "Active").ToListAsync();

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";
        ViewBag.TotalExpenses = expenses.Sum(e => e.Amount);

        return View(expenses);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid categoryId, Guid branchId, decimal amount, string? description)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var expense = new Expense
        {
            BusinessId = businessId.Value,
            CategoryId = categoryId,
            BranchId = branchId,
            Amount = amount,
            PaymentMethod = "Cash",
            Description = description?.Trim(),
            Status = "Approved"
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Expense of PKR {amount:N2} recorded successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string? description)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var category = new ExpenseCategory
        {
            BusinessId = businessId.Value,
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true
        };

        _context.ExpenseCategories.Add(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Expense Category '{category.Name}' created.";
        return RedirectToAction(nameof(Index));
    }
}

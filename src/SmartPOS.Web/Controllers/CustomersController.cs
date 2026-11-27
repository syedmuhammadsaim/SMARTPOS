using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public CustomersController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Customers.Include(c => c.LedgerEntries).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) || (c.Phone != null && c.Phone.Contains(term)));
        }

        var customers = await query.OrderBy(c => c.Name).ToListAsync();

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";
        ViewBag.Search = search;
        ViewBag.TotalUdhaar = customers.Sum(c => c.CurrentBalance);

        return View(customers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? phone, string? email, string? address, decimal creditLimit)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var customer = new Customer
        {
            BusinessId = businessId.Value,
            Name = name.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            CreditLimit = creditLimit,
            CurrentBalance = 0m,
            Status = "Active"
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Customer '{customer.Name}' registered successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Ledger(Guid id)
    {
        var customer = await _context.Customers
            .Include(c => c.LedgerEntries)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null) return NotFound();

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";

        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceivePayment(Guid customerId, decimal amount, string? notes)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return NotFound();

        customer.CurrentBalance -= amount;

        // Add ledger credit payment entry
        _context.CustomerLedgers.Add(new CustomerLedger
        {
            BusinessId = businessId.Value,
            CustomerId = customerId,
            Date = DateTime.UtcNow,
            Type = "Payment",
            Credit = amount,
            Debit = 0m,
            RunningBalance = customer.CurrentBalance,
            Notes = notes?.Trim() ?? "Udhaar repayment"
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Payment of PKR {amount:N2} recorded for {customer.Name}.";
        return RedirectToAction(nameof(Ledger), new { id = customerId });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.POS.Interfaces;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.ShiftsAndSessions;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class ShiftsController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly IPOSService _posService;
    private readonly ITenantProvider _tenantProvider;

    public ShiftsController(
        SmartPOSDbContext context,
        IPOSService posService,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _posService = posService;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var sessions = await _context.CounterSessions
            .Include(s => s.Branch)
            .Include(s => s.Counter)
            .OrderByDescending(s => s.StartUtc)
            .Take(50)
            .ToListAsync();

        ViewBag.Counters = await _context.Counters.Include(c => c.Branch).Where(c => c.Status == "Active").ToListAsync();

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";

        return View(sessions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenShift(Guid counterId, decimal openingCash)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var counter = await _context.Counters.FindAsync(counterId);
        if (counter == null) return NotFound();

        // Check if there is already an open session for this counter
        var existingOpen = await _context.CounterSessions
            .FirstOrDefaultAsync(s => s.CounterId == counterId && s.Status == CounterSessionStatus.Open);

        if (existingOpen != null)
        {
            TempData["ErrorMessage"] = $"Counter '{counter.Name}' already has an active open session.";
            return RedirectToAction(nameof(Index));
        }

        var userId = _tenantProvider.CurrentUserId ?? Guid.NewGuid();

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.BranchId == counter.BranchId);
        if (employee == null)
        {
            employee = new Employee
            {
                BusinessId = businessId.Value,
                BranchId = counter.BranchId,
                EmployeeCode = "EMP-001",
                FirstName = "Cashier",
                LastName = "Staff",
                Status = "Active"
            };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.BranchId == counter.BranchId || s.BranchId == null);
        if (shift == null)
        {
            shift = new Shift
            {
                BusinessId = businessId.Value,
                BranchId = counter.BranchId,
                Name = "General Shift",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(22, 0, 0)
            };
            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();
        }

        var session = new CounterSession
        {
            BusinessId = businessId.Value,
            BranchId = counter.BranchId,
            CounterId = counter.Id,
            EmployeeId = employee.Id,
            ShiftId = shift.Id,
            BusinessDate = DateTime.UtcNow.Date,
            OpeningCash = openingCash,
            ExpectedCash = openingCash,
            ActualCash = 0m,
            Variance = 0m,
            StartUtc = DateTime.UtcNow,
            Status = CounterSessionStatus.Open
        };

        _context.CounterSessions.Add(session);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Shift session opened for Counter '{counter.Name}' with opening cash of PKR {openingCash:N2}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseShift(Guid sessionId, decimal actualCashCounted)
    {
        var userId = _tenantProvider.CurrentUserId ?? Guid.NewGuid();
        var report = await _posService.CloseSessionAndGenerateZReportAsync(sessionId, actualCashCounted, userId);

        TempData["SuccessMessage"] = $"Shift closed. Expected: PKR {report.ExpectedCash:N2}, Counted: PKR {report.ActualCash:N2}, Difference: PKR {report.Variance:+#,##0.00;-#,##0.00;0.00}.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Report(Guid sessionId)
    {
        var userId = _tenantProvider.CurrentUserId ?? Guid.NewGuid();
        var report = await _posService.GenerateXReportAsync(sessionId, userId);

        var session = await _context.CounterSessions
            .Include(s => s.Branch)
            .Include(s => s.Counter)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Business = business;
        ViewBag.Session = session;

        return View(report);
    }
}

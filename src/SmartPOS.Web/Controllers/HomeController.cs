using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Web.Models;

namespace SmartPOS.Web.Controllers;

public class HomeController : Controller
{
    private readonly SmartPOSDbContext _context;

    public HomeController(SmartPOSDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Index", "SuperAdmin");
            return RedirectToAction("Index", "Dashboard");
        }

        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        return View(plans);
    }

    public async Task<IActionResult> Pricing()
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        return View(plans);
    }

    public IActionResult Features()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SubmitContact(string name, string email, string phone, string message)
    {
        TempData["SuccessMessage"] = "Thank you! Your message has been received. Our enterprise support team will contact you shortly.";
        return RedirectToAction(nameof(Contact));
    }

    public IActionResult FAQ()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

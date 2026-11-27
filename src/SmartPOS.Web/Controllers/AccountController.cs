using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.System;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Identity;
using SmartPOS.Web.Models;

namespace SmartPOS.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SmartPOSDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        SmartPOSDbContext context,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Index", "SuperAdmin");
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid login credentials or account is suspended.");
            return View(model);
        }

        // Check company status if user is a tenant member
        if (user.BusinessId.HasValue)
        {
            var business = await _context.Businesses.FindAsync(user.BusinessId.Value);
            if (business != null && business.Status == "Suspended")
            {
                ModelState.AddModelError(string.Empty, "Your company account has been temporarily suspended. Please contact platform support.");
                return View(model);
            }
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName ?? model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in successfully.", model.Email);

            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                return RedirectToAction("Index", "SuperAdmin");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (await _userManager.IsInRoleAsync(user, "Cashier"))
            {
                return RedirectToAction("Index", "POS");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", model.Email);
            ModelState.AddModelError(string.Empty, "This account is temporarily locked due to multiple failed login attempts. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Register(string? plan = "TRIAL")
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var availablePlans = await _context.SubscriptionPlans.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToListAsync();
        ViewBag.AvailablePlans = availablePlans;

        return View(new RegisterCompanyViewModel
        {
            PlanCode = string.IsNullOrWhiteSpace(plan) ? "TRIAL" : plan.ToUpper()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterCompanyViewModel model)
    {
        var availablePlans = await _context.SubscriptionPlans.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToListAsync();
        ViewBag.AvailablePlans = availablePlans;

        if (!ModelState.IsValid)
            return View(model);

        // Check existing user
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "An account with this email address already exists.");
            return View(model);
        }

        // Use database transaction for multi-step tenant onboarding
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Code == model.PlanCode) 
                       ?? await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Code == "TRIAL");

            // 1. Create Tenant (Business)
            var business = new Business
            {
                Name = model.CompanyName.Trim(),
                Code = "CO-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                Address = model.Address?.Trim(),
                CurrencyCode = string.IsNullOrWhiteSpace(model.Currency) ? "PKR" : model.Currency,
                TimeZone = "Pakistan Standard Time",
                Status = "Active",
                SubscriptionPlanId = plan?.Id,
                SubscriptionExpiresAtUtc = DateTime.UtcNow.AddDays(plan?.TrialDays > 0 ? plan.TrialDays : 30)
            };
            _context.Businesses.Add(business);
            await _context.SaveChangesAsync();

            // 2. Create Default Branch
            var defaultBranch = new Branch
            {
                BusinessId = business.Id,
                Code = "BR-01",
                Name = $"{model.CompanyName.Trim()} - Main Branch",
                Address = model.Address?.Trim(),
                Contact = model.Phone.Trim(),
                Hours = "09:00 AM - 10:00 PM",
                Status = "Active",
                InvoicePrefix = "INV"
            };
            _context.Branches.Add(defaultBranch);
            await _context.SaveChangesAsync();

            // 3. Create Default Counter
            var defaultCounter = new Counter
            {
                BusinessId = business.Id,
                BranchId = defaultBranch.Id,
                Code = "CTR-01",
                Name = "POS Counter 01",
                Status = "Active",
                PrinterName = "Default Thermal 80mm"
            };
            _context.Counters.Add(defaultCounter);

            // 4. Create Default Warehouse
            var defaultWarehouse = new Warehouse
            {
                BusinessId = business.Id,
                BranchId = defaultBranch.Id,
                Code = "WH-01",
                Name = "Main Store Warehouse",
                Status = "Active"
            };
            _context.Warehouses.Add(defaultWarehouse);

            // 5. Create Default Customer (Walk-in)
            _context.Customers.Add(new Customer
            {
                BusinessId = business.Id,
                Name = "Walk-in Retail Customer",
                Phone = "0300-0000000",
                CurrentBalance = 0m,
                CreditLimit = 0m,
                Status = "Active"
            });

            // 6. Record Initial Subscription
            _context.Subscriptions.Add(new Subscription
            {
                BusinessId = business.Id,
                Plan = plan?.Name ?? "Free Trial",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(plan?.TrialDays > 0 ? plan.TrialDays : 30),
                MaxBranches = plan?.MaxBranches ?? 1,
                MaxCounters = plan?.MaxCounters ?? 2,
                Status = "Active"
            });

            await _context.SaveChangesAsync();

            // 7. Create Company Owner / Administrator User
            var adminUser = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                FullName = model.FullName.Trim(),
                PhoneNumber = model.Phone.Trim(),
                EmailConfirmed = true,
                BusinessId = business.Id,
                IsActive = true
            };

            var userResult = await _userManager.CreateAsync(adminUser, model.Password);
            if (!userResult.Succeeded)
            {
                await transaction.RollbackAsync();
                foreach (var err in userResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }

            // Assign Admin Role
            await _userManager.AddToRoleAsync(adminUser, "Admin");

            // Update Business with OwnerUserId
            business.OwnerUserId = adminUser.Id;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Company {CompanyName} successfully registered with Owner {Email}.", business.Name, adminUser.Email);

            // Sign in the new company administrator
            await _signInManager.SignInAsync(adminUser, isPersistent: false);

            return RedirectToAction("Index", "Dashboard", new { welcome = "true" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to register company for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred during company onboarding. Please try again.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

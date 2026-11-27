using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Identity;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.Pricing;
using SmartPOS.Domain.Entities.SaaS;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Identity;

namespace SmartPOS.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task EnsureDatabaseCreatedAndSeededAsync(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SmartPOSDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.EnsureCreatedAsync();
            await SeedSystemDefaultsAsync(context, roleManager, userManager);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SmartPOS DbInitializer] Database notice: {ex.Message}");
        }
    }
    public static async Task SeedSystemDefaultsAsync(
        SmartPOSDbContext context,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        // 1. Seed Permissions
        var defaultPermissions = new List<Permission>
        {
            new() { Code = "pos.billing", Module = "POS", Description = "Create sales and process checkouts" },
            new() { Code = "pos.void", Module = "POS", Description = "Void orders or lines (Manager Override eligible)" },
            new() { Code = "pos.discount.override", Module = "POS", Description = "Apply custom discounts beyond thresholds" },
            new() { Code = "pos.price.change", Module = "POS", Description = "Change product price at counter" },
            new() { Code = "pos.refund", Module = "POS", Description = "Process customer returns and refunds" },
            new() { Code = "pos.hold", Module = "POS", Description = "Park/Hold unfinished carts" },
            new() { Code = "pos.resume", Module = "POS", Description = "Resume parked carts" },
            new() { Code = "pos.report.x", Module = "POS", Description = "Generate and print mid-shift X report" },
            new() { Code = "pos.report.z", Module = "POS", Description = "Close session and print final Z report" },
            new() { Code = "stock.view", Module = "Inventory", Description = "View current stock on hand" },
            new() { Code = "stock.adjust", Module = "Inventory", Description = "Perform manual stock adjustments" },
            new() { Code = "stock.transfer", Module = "Inventory", Description = "Initiate and receive stock transfers" },
            new() { Code = "stock.count", Module = "Inventory", Description = "Perform and approve physical inventory count" },
            new() { Code = "purchase.order", Module = "Purchasing", Description = "Create purchase orders" },
            new() { Code = "purchase.approve", Module = "Purchasing", Description = "Approve supplier purchase orders" },
            new() { Code = "purchase.receive", Module = "Purchasing", Description = "Receive goods against purchase orders" },
            new() { Code = "purchase.return", Module = "Purchasing", Description = "Create and dispatch purchase returns to suppliers" },
            new() { Code = "customer.ledger.view", Module = "Customers", Description = "View customer balance and credit ledgers" },
            new() { Code = "customer.payment.receive", Module = "Customers", Description = "Receive Udhaar / credit payments" },
            new() { Code = "expense.create", Module = "Finance", Description = "Record till or branch expenses" },
            new() { Code = "expense.approve", Module = "Finance", Description = "Approve expense vouchers" },
            new() { Code = "dailyclosing.execute", Module = "Finance", Description = "Perform branch-level daily closing" },
            new() { Code = "report.view.cost", Module = "Reports", Description = "View product cost prices and margins" },
            new() { Code = "report.view.profit", Module = "Reports", Description = "View net profit and gross margin reports" },
            new() { Code = "settings.manage", Module = "Settings", Description = "Configure branches, FBR, and system parameters" }
        };

        foreach (var perm in defaultPermissions)
        {
            if (!await context.SystemPermissions.AnyAsync(p => p.Code == perm.Code))
            {
                context.SystemPermissions.Add(perm);
            }
        }
        await context.SaveChangesAsync();

        // 2. Seed Standard Roles
        string[] roles = 
        {
            "SuperAdmin",
            "Admin",
            "BranchManager",
            "Cashier",
            "InventoryManager",
            "StoreKeeper",
            "Accountant",
            "PurchaseManager"
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName, Description = $"System default role: {roleName}" });
            }
        }

        // 3. Seed Default Units
        if (!await context.Units.AnyAsync())
        {
            var units = new List<Unit>
            {
                new() { Name = "Piece", ShortCode = "Pcs", AllowDecimals = false },
                new() { Name = "Kilogram", ShortCode = "Kg", AllowDecimals = true },
                new() { Name = "Gram", ShortCode = "g", AllowDecimals = true },
                new() { Name = "Liter", ShortCode = "Ltr", AllowDecimals = true },
                new() { Name = "Milliliter", ShortCode = "ml", AllowDecimals = true },
                new() { Name = "Carton", ShortCode = "Ctn", AllowDecimals = false },
                new() { Name = "Dozen", ShortCode = "Dzn", AllowDecimals = false },
                new() { Name = "Pack", ShortCode = "Pak", AllowDecimals = false }
            };
            context.Units.AddRange(units);
            await context.SaveChangesAsync();
        }

        // 4. Seed Standard Payment Methods
        if (!await context.PaymentMethods.AnyAsync())
        {
            var paymentMethods = new List<PaymentMethod>
            {
                new() { Name = "Cash", Type = PaymentMethodType.Cash, RequiresReference = false },
                new() { Name = "Debit / Credit Card", Type = PaymentMethodType.Card, RequiresReference = true },
                new() { Name = "JazzCash", Type = PaymentMethodType.MobileWallet, RequiresReference = true },
                new() { Name = "Easypaisa", Type = PaymentMethodType.MobileWallet, RequiresReference = true },
                new() { Name = "Bank Transfer", Type = PaymentMethodType.BankTransfer, RequiresReference = true },
                new() { Name = "Customer Credit (Udhaar)", Type = PaymentMethodType.CustomerCredit, RequiresReference = false },
                new() { Name = "Store Credit", Type = PaymentMethodType.StoreCredit, RequiresReference = false }
            };
            context.PaymentMethods.AddRange(paymentMethods);
            await context.SaveChangesAsync();
        }

        // 5. Seed Standard Taxes
        if (!await context.Taxes.AnyAsync())
        {
            var taxes = new List<Tax>
            {
                new() { Name = "GST 18% Inclusive", Rate = 18.00m, IsInclusive = true, RoundingMode = TaxRoundingMode.RoundToNearestRupee, IsExempt = false },
                new() { Name = "GST 18% Exclusive", Rate = 18.00m, IsInclusive = false, RoundingMode = TaxRoundingMode.RoundToNearestRupee, IsExempt = false },
                new() { Name = "Tax Exempt (0%)", Rate = 0.00m, IsInclusive = false, RoundingMode = TaxRoundingMode.RoundToNearestRupee, IsExempt = true }
            };
            context.Taxes.AddRange(taxes);
            await context.SaveChangesAsync();
        }

        // 6. Seed SaaS Subscription Plans
        if (!await context.SubscriptionPlans.AnyAsync())
        {
            var plans = new List<SubscriptionPlan>
            {
                new()
                {
                    Name = "Free Trial",
                    Code = "TRIAL",
                    Description = "14-day fully featured trial for evaluating SMARTPOS ENTERPRISE.",
                    MonthlyPrice = 0m,
                    AnnualPrice = 0m,
                    Currency = "PKR",
                    MaxBranches = 1,
                    MaxCounters = 2,
                    MaxUsers = 3,
                    MaxProducts = 500,
                    HasAdvancedReports = true,
                    HasFbrIntegration = true,
                    HasBarcodePrinting = true,
                    HasMultiCounter = true,
                    HasApiAccess = false,
                    TrialDays = 14,
                    SortOrder = 1,
                    IsPopular = false
                },
                new()
                {
                    Name = "Starter Plan",
                    Code = "STARTER",
                    Description = "Essential POS and inventory control for single-store retail and quick-service shops.",
                    MonthlyPrice = 4500m,
                    AnnualPrice = 45000m,
                    Currency = "PKR",
                    MaxBranches = 1,
                    MaxCounters = 2,
                    MaxUsers = 5,
                    MaxProducts = 2500,
                    HasAdvancedReports = false,
                    HasFbrIntegration = false,
                    HasBarcodePrinting = true,
                    HasMultiCounter = true,
                    HasApiAccess = false,
                    TrialDays = 0,
                    SortOrder = 2,
                    IsPopular = false
                },
                new()
                {
                    Name = "Professional Plan",
                    Code = "PRO",
                    Description = "Advanced multi-counter, multi-branch operations with FBR tier integration.",
                    MonthlyPrice = 8500m,
                    AnnualPrice = 85000m,
                    Currency = "PKR",
                    MaxBranches = 3,
                    MaxCounters = 8,
                    MaxUsers = 15,
                    MaxProducts = 15000,
                    HasAdvancedReports = true,
                    HasFbrIntegration = true,
                    HasBarcodePrinting = true,
                    HasMultiCounter = true,
                    HasApiAccess = false,
                    TrialDays = 0,
                    SortOrder = 3,
                    IsPopular = true
                },
                new()
                {
                    Name = "Enterprise Plan",
                    Code = "ENTERPRISE",
                    Description = "Unlimited branches, complete API integration, dedicated account support and auditing.",
                    MonthlyPrice = 18000m,
                    AnnualPrice = 180000m,
                    Currency = "PKR",
                    MaxBranches = 25,
                    MaxCounters = 100,
                    MaxUsers = 100,
                    MaxProducts = 100000,
                    HasAdvancedReports = true,
                    HasFbrIntegration = true,
                    HasBarcodePrinting = true,
                    HasMultiCounter = true,
                    HasApiAccess = true,
                    TrialDays = 0,
                    SortOrder = 4,
                    IsPopular = false
                }
            };
            context.SubscriptionPlans.AddRange(plans);
            await context.SaveChangesAsync();
        }

        // 7. Seed Platform Settings
        if (!await context.PlatformSettings.AnyAsync())
        {
            var settings = new List<PlatformSetting>
            {
                new() { Key = "Platform:Name", Value = "SMARTPOS ENTERPRISE", Category = "General", Description = "Global SaaS brand name" },
                new() { Key = "Platform:SupportEmail", Value = "support@smartpos.com", Category = "General", Description = "Customer support email" },
                new() { Key = "Platform:Currency", Value = "PKR", Category = "Billing", Description = "Default platform billing currency" },
                new() { Key = "Platform:TrialDays", Value = "14", Category = "Billing", Description = "Default free trial duration" }
            };
            context.PlatformSettings.AddRange(settings);
            await context.SaveChangesAsync();
        }

        // 8. Seed SuperAdmin Platform User
        var superAdminEmail = "superadmin@smartpos.com";
        var existingSuperAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        if (existingSuperAdmin == null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FullName = "Platform Super Administrator",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(superAdmin, "SuperAdmin@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
            }
        }

        // 9. Seed Default Company / Tenant ("Apex Mart & Retail") if no business exists
        if (!await context.Businesses.AnyAsync())
        {
            var proPlan = await context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Code == "PRO");

            var defaultBiz = new Business
            {
                Name = "Apex Mart & Retail",
                Code = "APEX01",
                Email = "admin@apexmart.com",
                Phone = "+92 300 1234567",
                Address = "Plot 42, Commercial Avenue, DHA Phase 5, Lahore",
                CurrencyCode = "PKR",
                TimeZone = "Pakistan Standard Time",
                Status = "Active",
                TaxRegistrationNumber = "NTN-7362910-4",
                SubscriptionPlanId = proPlan?.Id,
                SubscriptionExpiresAtUtc = DateTime.UtcNow.AddMonths(12)
            };
            context.Businesses.Add(defaultBiz);
            await context.SaveChangesAsync();

            // Create Company Owner / Admin User
            var companyAdminEmail = "admin@apexmart.com";
            var companyAdmin = new ApplicationUser
            {
                UserName = companyAdminEmail,
                Email = companyAdminEmail,
                FullName = "Apex Store Manager",
                EmailConfirmed = true,
                BusinessId = defaultBiz.Id,
                IsActive = true
            };
            var adminRes = await userManager.CreateAsync(companyAdmin, "Admin@123!");
            if (adminRes.Succeeded)
            {
                await userManager.AddToRoleAsync(companyAdmin, "Admin");
                defaultBiz.OwnerUserId = companyAdmin.Id;
            }

            // Create Default Branch
            var mainBranch = new Branch
            {
                BusinessId = defaultBiz.Id,
                Code = "BR-MAIN",
                Name = "DHA Main Branch",
                Address = "Plot 42, Commercial Avenue, DHA Phase 5, Lahore",
                Contact = "+92 300 1234567",
                Hours = "09:00 AM - 11:00 PM",
                Status = "Active",
                InvoicePrefix = "APX"
            };
            context.Branches.Add(mainBranch);
            await context.SaveChangesAsync();

            // Create Default Counter
            var counter01 = new Counter
            {
                BusinessId = defaultBiz.Id,
                BranchId = mainBranch.Id,
                Code = "CTR-01",
                Name = "Main Checkout Counter 01",
                Status = "Active",
                PrinterName = "Default Thermal 80mm"
            };
            context.Counters.Add(counter01);

            // Create Default Warehouse
            var warehouse = new SmartPOS.Domain.Entities.Inventory.Warehouse
            {
                BusinessId = defaultBiz.Id,
                BranchId = mainBranch.Id,
                Code = "WH-MAIN",
                Name = "Main Store Warehouse",
                Status = "Active"
            };
            context.Warehouses.Add(warehouse);

            // Create Demo Categories & Products
            var catBeverages = new Category { BusinessId = defaultBiz.Id, Name = "Beverages", Code = "BEV" };
            var catSnacks = new Category { BusinessId = defaultBiz.Id, Name = "Snacks & Confectionery", Code = "SNK" };
            var catDairy = new Category { BusinessId = defaultBiz.Id, Name = "Dairy & Bakery", Code = "DRY" };
            context.Categories.AddRange(catBeverages, catSnacks, catDairy);
            await context.SaveChangesAsync();

            var brandNestle = new Brand { BusinessId = defaultBiz.Id, Name = "Nestle" };
            var brandLays = new Brand { BusinessId = defaultBiz.Id, Name = "Lays" };
            context.Brands.AddRange(brandNestle, brandLays);
            await context.SaveChangesAsync();

            var pcsUnit = await context.Units.FirstOrDefaultAsync(u => u.ShortCode == "Pcs");

            var prod1 = new Product
            {
                BusinessId = defaultBiz.Id,
                Name = "Mineral Water 1.5L",
                Sku = "WAT-1500",
                CategoryId = catBeverages.Id,
                BrandId = brandNestle.Id,
                UnitId = pcsUnit?.Id ?? Guid.Empty,
                PurchasePrice = 75.00m,
                SalePrice = 110.00m,
                MinStock = 20m,
                Status = "Active"
            };
            var prod2 = new Product
            {
                BusinessId = defaultBiz.Id,
                Name = "Classic Salted Chips 65g",
                Sku = "SNK-SALT-65",
                CategoryId = catSnacks.Id,
                BrandId = brandLays.Id,
                UnitId = pcsUnit?.Id ?? Guid.Empty,
                PurchasePrice = 60.00m,
                SalePrice = 90.00m,
                MinStock = 15m,
                Status = "Active"
            };
            var prod3 = new Product
            {
                BusinessId = defaultBiz.Id,
                Name = "Fresh Milk 1L Pack",
                Sku = "DRY-MLK-1000",
                CategoryId = catDairy.Id,
                BrandId = brandNestle.Id,
                UnitId = pcsUnit?.Id ?? Guid.Empty,
                PurchasePrice = 240.00m,
                SalePrice = 290.00m,
                MinStock = 10m,
                Status = "Active"
            };
            context.Products.AddRange(prod1, prod2, prod3);
            await context.SaveChangesAsync();

            // Seed Barcodes
            context.ProductBarcodes.AddRange(
                new ProductBarcode { BusinessId = defaultBiz.Id, ProductId = prod1.Id, Barcode = "8964000112233", IsPrimary = true },
                new ProductBarcode { BusinessId = defaultBiz.Id, ProductId = prod2.Id, Barcode = "8964000445566", IsPrimary = true },
                new ProductBarcode { BusinessId = defaultBiz.Id, ProductId = prod3.Id, Barcode = "8964000778899", IsPrimary = true }
            );

            // Seed Stock Balances
            context.StockBalances.AddRange(
                new SmartPOS.Domain.Entities.Inventory.StockBalance { BusinessId = defaultBiz.Id, WarehouseId = warehouse.Id, ProductId = prod1.Id, QtyOnHand = 150m },
                new SmartPOS.Domain.Entities.Inventory.StockBalance { BusinessId = defaultBiz.Id, WarehouseId = warehouse.Id, ProductId = prod2.Id, QtyOnHand = 85m },
                new SmartPOS.Domain.Entities.Inventory.StockBalance { BusinessId = defaultBiz.Id, WarehouseId = warehouse.Id, ProductId = prod3.Id, QtyOnHand = 40m }
            );

            // Seed a Walk-In Customer
            context.Customers.Add(new SmartPOS.Domain.Entities.Customers.Customer
            {
                BusinessId = defaultBiz.Id,
                Name = "Walk-in Retail Customer",
                Phone = "0300-0000000",
                CurrentBalance = 0m,
                CreditLimit = 0m,
                Status = "Active"
            });

            await context.SaveChangesAsync();
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Identity;
using SmartPOS.Domain.Entities.Pricing;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Identity;

namespace SmartPOS.Infrastructure.Data;

public static class DbInitializer
{
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
    }
}

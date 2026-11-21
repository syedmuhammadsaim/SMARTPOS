using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Common;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Domain.Entities.Finance;
using SmartPOS.Domain.Entities.Identity;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.Pricing;
using SmartPOS.Domain.Entities.PromotionsAndLoyalty;
using SmartPOS.Domain.Entities.Purchasing;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Entities.ShiftsAndSessions;
using SmartPOS.Domain.Entities.System;
using SmartPOS.Infrastructure.Identity;

namespace SmartPOS.Infrastructure.Data;

public class SmartPOSDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ITenantProvider? _tenantProvider;

    public SmartPOSDbContext(
        DbContextOptions<SmartPOSDbContext> options,
        ITenantProvider? tenantProvider = null) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    // Organization
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Counter> Counters => Set<Counter>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Employee> Employees => Set<Employee>();

    // Identity & Security
    public DbSet<Permission> SystemPermissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserBranchScope> UserBranchScopes => Set<UserBranchScope>();
    public DbSet<ManagerOverride> ManagerOverrides => Set<ManagerOverride>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Shifts & Sessions
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<EmployeeShift> EmployeeShifts => Set<EmployeeShift>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<CounterSession> CounterSessions => Set<CounterSession>();
    public DbSet<SessionCashMovement> SessionCashMovements => Set<SessionCashMovement>();
    public DbSet<CounterSessionReport> CounterSessionReports => Set<CounterSessionReport>();

    // Catalog & Pricing
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<ProductBundleItem> ProductBundleItems => Set<ProductBundleItem>();
    public DbSet<ProductPriceHistory> ProductPriceHistories => Set<ProductPriceHistory>();
    public DbSet<ProductUnitConversion> ProductUnitConversions => Set<ProductUnitConversion>();
    public DbSet<Tax> Taxes => Set<Tax>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();

    // Inventory & Warehouses
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountItem> StockCountItems => Set<StockCountItem>();

    // Purchasing & Suppliers
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<SupplierLedger> SupplierLedgers => Set<SupplierLedger>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();

    // Sales & Customers
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerLedger> CustomerLedgers => Set<CustomerLedger>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<POSOrder> POSOrders => Set<POSOrder>();
    public DbSet<POSOrderItem> POSOrderItems => Set<POSOrderItem>();
    public DbSet<POSOrderDiscount> POSOrderDiscounts => Set<POSOrderDiscount>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<HeldBill> HeldBills => Set<HeldBill>();
    public DbSet<HeldBillItem> HeldBillItems => Set<HeldBillItem>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleReturnItem> SaleReturnItems => Set<SaleReturnItem>();

    // Promotions, Gift Cards & Loyalty
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionRule> PromotionRules => Set<PromotionRule>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();
    public DbSet<GiftCardTransaction> GiftCardTransactions => Set<GiftCardTransaction>();
    public DbSet<MembershipTier> MembershipTiers => Set<MembershipTier>();
    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<LoyaltyRule> LoyaltyRules => Set<LoyaltyRule>();
    public DbSet<Reward> Rewards => Set<Reward>();

    // Finance
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<DailyClosing> DailyClosings => Set<DailyClosing>();

    // System, FBR & CRM
    public DbSet<FbrSetting> FbrSettings => Set<FbrSetting>();
    public DbSet<FbrInvoiceLog> FbrInvoiceLogs => Set<FbrInvoiceLog>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ReceiptTemplate> ReceiptTemplates => Set<ReceiptTemplate>();
    public DbSet<LabelTemplate> LabelTemplates => Set<LabelTemplate>();
    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();
    public DbSet<AppVersion> AppVersions => Set<AppVersion>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<LicenseKey> LicenseKeys => Set<LicenseKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // RolePermission Composite Key
        builder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Concurrency tokens
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType)
                    .Property<byte[]>("RowVersion")
                    .IsRowVersion();
            }

            // Decimal precision configuration
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    if (property.Name.Contains("Qty", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Contains("Quantity", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Contains("Factor", StringComparison.OrdinalIgnoreCase))
                    {
                        property.SetPrecision(18);
                        property.SetScale(3);
                    }
                    else
                    {
                        property.SetPrecision(18);
                        property.SetScale(2);
                    }
                }
            }

            // Multi-tenant Global Query Filter
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(SmartPOSDbContext)
                    .GetMethod(nameof(ConfigureTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { builder });
            }
        }

        // Unique indexes
        builder.Entity<POSOrder>()
            .HasIndex(o => o.ClientTransactionId)
            .IsUnique();

        builder.Entity<POSOrder>()
            .HasIndex(o => new { o.BusinessId, o.InvoiceNo })
            .IsUnique();

        builder.Entity<ProductBarcode>()
            .HasIndex(b => new { b.BusinessId, b.Barcode })
            .IsUnique();

        builder.Entity<Permission>()
            .HasIndex(p => p.Code)
            .IsUnique();

        builder.Entity<IdempotencyKey>()
            .HasIndex(k => k.Key)
            .IsUnique();

        builder.Entity<Coupon>()
            .HasIndex(c => new { c.BusinessId, c.Code })
            .IsUnique();

        builder.Entity<GiftCard>()
            .HasIndex(g => g.CardNo)
            .IsUnique();

        builder.Entity<ProductBatch>()
            .HasIndex(b => b.ExpiryDate);

        builder.Entity<StockBalance>()
            .HasIndex(s => new { s.ProductId, s.VariantId, s.WarehouseId, s.BatchId });

        // Prevent multiple cascade paths on SQL Server
        builder.Entity<ProductBundleItem>()
            .HasOne(b => b.ComponentProduct)
            .WithMany()
            .HasForeignKey(b => b.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductBundleItem>()
            .HasOne(b => b.BundleProduct)
            .WithMany()
            .HasForeignKey(b => b.BundleProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockTransfer>()
            .HasOne(m => m.ToWarehouse)
            .WithMany()
            .HasForeignKey(m => m.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockTransfer>()
            .HasOne(m => m.FromWarehouse)
            .WithMany()
            .HasForeignKey(m => m.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureTenantFilter<TEntity>(ModelBuilder builder) where TEntity : class, ITenantScoped
    {
        builder.Entity<TEntity>().HasQueryFilter(e => 
            _tenantProvider == null || 
            _tenantProvider.CurrentBusinessId == null || 
            e.BusinessId == _tenantProvider.CurrentBusinessId.Value);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Expenses.DTOs;
using SmartPOS.Application.Features.Promotions.DTOs;
using SmartPOS.Application.Features.Returns.DTOs;
using SmartPOS.Application.Features.Sequences.Interfaces;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Finance;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.PromotionsAndLoyalty;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Services;
using Xunit;

namespace SmartPOS.Tests;

public class ReturnsPromotionsExpensesTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _counterId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    private SmartPOSDbContext CreateDbContext(ITenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<SmartPOSDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new SmartPOSDbContext(options, tenantProvider);
    }

    private class MockTenantProvider : ITenantProvider
    {
        public Guid? CurrentBusinessId { get; }
        public Guid? CurrentBranchId { get; }
        public Guid? CurrentUserId { get; }

        public MockTenantProvider(Guid tenantId)
        {
            CurrentBusinessId = tenantId;
        }

        public void SetTenant(Guid businessId, Guid? branchId = null) { }
    }

    private class MockSequenceService : IDocumentSequenceService
    {
        public Task<string> GetNextNumberAsync(Guid businessId, Guid? branchId, Guid? counterId, string docType, string prefix)
        {
            return Task.FromResult($"{prefix}-{DateTime.UtcNow:yyyyMMdd}-001");
        }
    }

    [Fact]
    public async Task ProcessSaleReturn_ShouldRestockInventoryAndCreateCashRefund()
    {
        var tenantProvider = new MockTenantProvider(_tenantId);
        using var context = CreateDbContext(tenantProvider);
        var seqService = new MockSequenceService();
        var returnService = new ReturnService(context, tenantProvider, seqService);

        // Seed Warehouse & Product
        var warehouse = new Warehouse { Id = _warehouseId, BusinessId = _tenantId, BranchId = _branchId, Code = "WH-01", Name = "Main WH" };
        var product = new Product { Id = Guid.NewGuid(), BusinessId = _tenantId, Sku = "SKU-RET1", Name = "Polo Shirt", SalePrice = 1200m };
        var stock = new StockBalance { Id = Guid.NewGuid(), BusinessId = _tenantId, WarehouseId = _warehouseId, ProductId = product.Id, QtyOnHand = 10 };

        // Seed Original POS Order
        var order = new POSOrder
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            InvoiceNo = "INV-2026-0099",
            BranchId = _branchId,
            CounterId = _counterId,
            SessionId = _sessionId,
            Total = 2400m,
            Subtotal = 2400m,
            BusinessDate = DateTime.UtcNow,
            Items = new List<POSOrderItem>
            {
                new()
                {
                    BusinessId = _tenantId,
                    ProductId = product.Id,
                    NameSnapshot = product.Name,
                    PriceSnapshot = 1200m,
                    Qty = 2,
                    LineTotal = 2400m
                }
            }
        };

        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        context.StockBalances.Add(stock);
        context.POSOrders.Add(order);
        await context.SaveChangesAsync();

        // Action: Return 1 item with condition Restockable
        var returnReq = new CreateSaleReturnRequest(
            order.Id,
            _branchId,
            _counterId,
            _sessionId,
            "Cash",
            "Customer size exchange return",
            null,
            new List<ReturnItemRequest>
            {
                new(product.Id, null, null, 1, 1200m, "Restockable")
            }
        );

        var result = await returnService.ProcessSaleReturnAsync(returnReq);

        Assert.True(result.Success);
        Assert.Equal(1200m, result.RefundAmount);
        Assert.StartsWith("RET-", result.ReturnInvoiceNo);

        // Check stock incremented from 10 to 11
        var updatedStock = await context.StockBalances.FirstAsync(s => s.ProductId == product.Id);
        Assert.Equal(11, updatedStock.QtyOnHand);

        // Check StockTransaction created
        var stockTx = await context.StockTransactions.FirstAsync(t => t.ProductId == product.Id);
        Assert.Equal(StockTransactionType.SaleReturn, stockTx.Type);
        Assert.Equal(1, stockTx.Qty);

        // Check CashTransaction created for refund
        var cashTx = await context.CashTransactions.FirstAsync(c => c.SessionId == _sessionId);
        Assert.Equal("CashRefund", cashTx.Type);
        Assert.Equal(1200m, cashTx.Amount);
    }

    [Fact]
    public async Task GiftCard_IssueAndRedeem_ShouldUpdateBalanceAccurately()
    {
        var tenantProvider = new MockTenantProvider(_tenantId);
        using var context = CreateDbContext(tenantProvider);
        var service = new PromotionGiftCardService(context, tenantProvider);

        // 1. Issue card of Rs. 5000
        var issueReq = new IssueGiftCardRequest("GC-998877", 5000m, null, null);
        var issuedCard = await service.IssueGiftCardAsync(issueReq);

        Assert.Equal("GC-998877", issuedCard.CardNo);
        Assert.Equal(5000m, issuedCard.Balance);

        // 2. Inquire balance
        var balanceDto = await service.GetGiftCardBalanceAsync("GC-998877");
        Assert.NotNull(balanceDto);
        Assert.Equal(5000m, balanceDto.Balance);

        // 3. Redeem Rs. 1500
        var redeemResult = await service.RedeemGiftCardAsync(new RedeemGiftCardRequest("GC-998877", 1500m, Guid.NewGuid()));
        Assert.True(redeemResult.Success);
        Assert.Equal(1500m, redeemResult.RedeemedAmount);
        Assert.Equal(3500m, redeemResult.RemainingBalance);

        // 4. Try redeeming Rs. 4000 (exceeds balance)
        var failedRedeem = await service.RedeemGiftCardAsync(new RedeemGiftCardRequest("GC-998877", 4000m, Guid.NewGuid()));
        Assert.False(failedRedeem.Success);
        Assert.Contains("Insufficient balance", failedRedeem.Message);
    }

    [Fact]
    public async Task ValidateCoupon_ShouldApplyPercentageDiscount()
    {
        var tenantProvider = new MockTenantProvider(_tenantId);
        using var context = CreateDbContext(tenantProvider);
        var service = new PromotionGiftCardService(context, tenantProvider);

        var promo = new Promotion
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            Name = "Winter Sale 10%",
            Type = PromotionType.PercentageOff,
            Status = "Active",
            StartUtc = DateTime.UtcNow.AddDays(-1),
            EndUtc = DateTime.UtcNow.AddDays(10),
            Rules = new List<PromotionRule>
            {
                new()
                {
                    BusinessId = _tenantId,
                    MinAmount = 1000m,
                    DiscountValue = 10m // 10%
                }
            }
        };

        var coupon = new Coupon
        {
            BusinessId = _tenantId,
            Code = "WINTER10",
            PromotionId = promo.Id,
            Status = "Active",
            ExpiryDate = DateTime.UtcNow.AddDays(5),
            UsageLimit = 100,
            UsedCount = 0
        };

        context.Promotions.Add(promo);
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();

        // Validate on Rs. 3000 spend -> 10% of 3000 = Rs. 300 discount
        var response = await service.ValidateCouponAsync(new ValidateCouponRequest("WINTER10", 3000m, null, null));

        Assert.True(response.IsValid);
        Assert.Equal(300m, response.DiscountAmount);
    }

    [Fact]
    public async Task CreateExpense_PaidWithCash_ShouldRecordCashTransaction()
    {
        var tenantProvider = new MockTenantProvider(_tenantId);
        using var context = CreateDbContext(tenantProvider);
        var seqService = new MockSequenceService();
        var expenseService = new ExpenseService(context, tenantProvider, seqService);

        var category = await expenseService.CreateCategoryAsync("Refreshment", "Tea, milk, refreshments for office");

        var exp = await expenseService.CreateExpenseAsync(new CreateExpenseRequest(
            _branchId,
            _sessionId,
            category.Id,
            450m,
            "Cash",
            "Gulberg Bakery",
            "Evening snacks for staff",
            Guid.NewGuid()
        ));

        Assert.Equal(450m, exp.Amount);
        Assert.Equal("Refreshment", exp.CategoryName);
        Assert.StartsWith("EXP-", exp.ExpenseNo);

        // Verify till cash transaction is logged for session
        var tillTxs = await expenseService.GetSessionCashTransactionsAsync(_sessionId);
        Assert.Single(tillTxs);
        Assert.Equal("Expense", tillTxs[0].Type);
        Assert.Equal(450m, tillTxs[0].Amount);
    }
}

using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Returns;
using SmartPOS.Application.Features.Returns.DTOs;
using SmartPOS.Application.Features.Sequences.Interfaces;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Domain.Entities.Finance;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class ReturnService : IReturnService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDocumentSequenceService _sequenceService;

    public ReturnService(
        SmartPOSDbContext context,
        ITenantProvider tenantProvider,
        IDocumentSequenceService sequenceService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _sequenceService = sequenceService;
    }

    private Guid GetTenantId() => _tenantProvider.CurrentBusinessId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<OrderReturnEligibilityDto?> GetOrderForReturnAsync(string invoiceNo, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        var order = await _context.POSOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.BusinessId == tenantId && o.InvoiceNo == invoiceNo, ct);

        if (order == null) return null;

        // Fetch already returned items for this order
        var previousReturns = await _context.SaleReturnItems
            .Include(ri => ri.SaleReturn)
            .Where(ri => ri.SaleReturn != null && ri.SaleReturn.OriginalOrderId == order.Id && ri.BusinessId == tenantId)
            .ToListAsync(ct);

        var items = new List<OrderEligibleItemDto>();

        foreach (var item in order.Items)
        {
            var alreadyReturned = previousReturns
                .Where(r => r.ProductId == item.ProductId && r.VariantId == item.VariantId)
                .Sum(r => r.ReturnedQty);

            var eligible = Math.Max(0m, item.Qty - alreadyReturned);

            items.Add(new OrderEligibleItemDto(
                item.ProductId,
                item.VariantId,
                item.BatchId,
                item.NameSnapshot,
                null,
                item.Qty,
                alreadyReturned,
                eligible,
                item.PriceSnapshot
            ));
        }

        return new OrderReturnEligibilityDto(
            order.Id,
            order.InvoiceNo,
            order.BusinessDate,
            order.Total,
            items
        );
    }

    public async Task<SaleReturnResponse> ProcessSaleReturnAsync(CreateSaleReturnRequest request, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        var order = await _context.POSOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OriginalOrderId && o.BusinessId == tenantId, ct);

        if (order == null)
        {
            return new SaleReturnResponse(false, Guid.Empty, string.Empty, 0m, request.RefundMethod, "Original order not found.");
        }

        // Fetch primary warehouse for branch stock restock
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.BranchId == request.BranchId && w.BusinessId == tenantId, ct)
            ?? await _context.Warehouses.FirstOrDefaultAsync(w => w.BusinessId == tenantId, ct);

        decimal totalRefund = 0m;
        var returnInvoiceNo = await _sequenceService.GetNextNumberAsync(
            tenantId,
            request.BranchId,
            request.CounterId,
            "SaleReturn",
            "RET"
        );

        var saleReturn = new SaleReturn
        {
            BusinessId = tenantId,
            ReturnInvoiceNo = returnInvoiceNo,
            OriginalOrderId = order.Id,
            BranchId = request.BranchId,
            CounterId = request.CounterId,
            SessionId = request.SessionId,
            CustomerId = order.CustomerId,
            RefundMethod = request.RefundMethod,
            Reason = request.Reason,
            ManagerOverrideId = request.ManagerOverrideId
        };

        foreach (var reqItem in request.Items)
        {
            if (reqItem.Quantity <= 0) continue;

            var lineRefund = reqItem.Quantity * reqItem.UnitPrice;
            totalRefund += lineRefund;

            var returnItem = new SaleReturnItem
            {
                BusinessId = tenantId,
                SaleReturnId = saleReturn.Id,
                ProductId = reqItem.ProductId,
                VariantId = reqItem.VariantId,
                BatchId = reqItem.BatchId,
                ReturnedQty = reqItem.Quantity,
                UnitPrice = reqItem.UnitPrice,
                RefundAmount = lineRefund,
                Condition = reqItem.Condition
            };
            saleReturn.Items.Add(returnItem);

            // If item is restockable, return quantity back to warehouse inventory
            if (reqItem.Condition == "Restockable" && warehouse != null)
            {
                var stock = await _context.StockBalances.FirstOrDefaultAsync(s =>
                    s.BusinessId == tenantId &&
                    s.WarehouseId == warehouse.Id &&
                    s.ProductId == reqItem.ProductId &&
                    s.VariantId == reqItem.VariantId, ct);

                if (stock != null)
                {
                    stock.QtyOnHand += reqItem.Quantity;
                }
                else
                {
                    stock = new StockBalance
                    {
                        BusinessId = tenantId,
                        WarehouseId = warehouse.Id,
                        ProductId = reqItem.ProductId,
                        VariantId = reqItem.VariantId,
                        BatchId = reqItem.BatchId,
                        QtyOnHand = reqItem.Quantity
                    };
                    _context.StockBalances.Add(stock);
                }

                var stockTx = new StockTransaction
                {
                    BusinessId = tenantId,
                    WarehouseId = warehouse.Id,
                    ProductId = reqItem.ProductId,
                    VariantId = reqItem.VariantId,
                    BatchId = reqItem.BatchId,
                    Type = StockTransactionType.SaleReturn,
                    Qty = reqItem.Quantity,
                    ReferenceType = "SaleReturn",
                    ReferenceId = saleReturn.Id,
                    Note = $"Restocked from Return {returnInvoiceNo}"
                };
                _context.StockTransactions.Add(stockTx);
            }
        }

        saleReturn.RefundAmount = totalRefund;
        _context.SaleReturns.Add(saleReturn);

        // Till / Cash Drawer handling
        if (request.RefundMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
        {
            var cashTx = new CashTransaction
            {
                BusinessId = tenantId,
                BranchId = request.BranchId,
                SessionId = request.SessionId,
                Type = "CashRefund",
                Amount = totalRefund,
                Reason = $"Customer Return Refund: {returnInvoiceNo}",
                Reference = returnInvoiceNo,
                UserId = request.ManagerOverrideId ?? Guid.Empty
            };
            _context.CashTransactions.Add(cashTx);
        }
        else if (request.RefundMethod.Equals("CustomerLedger", StringComparison.OrdinalIgnoreCase) && order.CustomerId.HasValue)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.BusinessId == tenantId, ct);
            if (customer != null)
            {
                customer.CurrentBalance -= totalRefund;

                var ledger = new CustomerLedger
                {
                    BusinessId = tenantId,
                    CustomerId = customer.Id,
                    Type = "Return",
                    ReferenceId = saleReturn.Id,
                    Debit = 0m,
                    Credit = totalRefund,
                    RunningBalance = customer.CurrentBalance,
                    Notes = $"Refund credit for Return {returnInvoiceNo}",
                    Date = DateTime.UtcNow
                };
                _context.CustomerLedgers.Add(ledger);
            }
        }

        await _context.SaveChangesAsync(ct);

        return new SaleReturnResponse(
            true,
            saleReturn.Id,
            returnInvoiceNo,
            totalRefund,
            request.RefundMethod,
            "Sale return processed successfully."
        );
    }
}

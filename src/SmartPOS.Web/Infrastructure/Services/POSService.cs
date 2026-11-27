using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.POS.DTOs;
using SmartPOS.Application.Features.POS.Interfaces;
using SmartPOS.Application.Features.Sequences.Interfaces;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Entities.ShiftsAndSessions;
using SmartPOS.Domain.Enums;
using SmartPOS.Domain.Services;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class POSService : IPOSService
{
    private readonly SmartPOSDbContext _context;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ITenantProvider _tenantProvider;

    public POSService(
        SmartPOSDbContext context,
        IDocumentSequenceService sequenceService,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _sequenceService = sequenceService;
        _tenantProvider = tenantProvider;
    }

    public async Task<SaleOrderResponseDto> CreateSaleOrderAsync(CreateSaleOrderDto dto, Guid cashierId)
    {
        // 1. Idempotency Check: prevent duplicate orders if client retried with same ClientTransactionId
        var existingOrder = await _context.POSOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.ClientTransactionId == dto.ClientTransactionId);

        if (existingOrder != null)
        {
            return new SaleOrderResponseDto(
                Success: true,
                OrderId: existingOrder.Id,
                InvoiceNo: existingOrder.InvoiceNo,
                Subtotal: existingOrder.Subtotal,
                DiscountTotal: existingOrder.DiscountTotal,
                TaxTotal: existingOrder.TaxTotal,
                Total: existingOrder.Total,
                PaidAmount: existingOrder.PaidAmount,
                ChangeAmount: existingOrder.ChangeAmount,
                Status: existingOrder.Status,
                FbrInvoiceNo: existingOrder.FbrInvoiceNo,
                FbrQrCode: existingOrder.FbrQrCode,
                Message: "Order retrieved from previous submission (Idempotency)."
            );
        }

        // 2. Validate Session
        var session = await _context.CounterSessions
            .FirstOrDefaultAsync(s => s.Id == dto.SessionId && s.Status == CounterSessionStatus.Open);

        if (session == null)
        {
            return new SaleOrderResponseDto(false, null, null, 0, 0, 0, 0, 0, 0, OrderStatus.Cancelled, null, null, "Active cashier counter session is required to bill.");
        }

        var businessId = _tenantProvider.CurrentBusinessId ?? session.BusinessId;

        // 3. Resolve Warehouse for Branch
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.BranchId == dto.BranchId && w.Status == "Active");

        if (warehouse == null)
        {
            return new SaleOrderResponseDto(false, null, null, 0, 0, 0, 0, 0, 0, OrderStatus.Cancelled, null, null, "No active warehouse assigned to branch for inventory deduction.");
        }

        // 4. Begin DB Transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Generate Invoice No
            var branch = await _context.Branches.FindAsync(dto.BranchId);
            var prefix = branch?.InvoicePrefix ?? "INV";
            var invoiceNo = await _sequenceService.GetNextNumberAsync(businessId, dto.BranchId, dto.CounterId, "POSInvoice", prefix);

            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Include(p => p.Tax)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            decimal subtotal = 0m;
            decimal totalDiscount = 0m;
            decimal totalTax = 0m;
            var orderItems = new List<POSOrderItem>();

            foreach (var itemDto in dto.Items)
            {
                if (!products.TryGetValue(itemDto.ProductId, out var product))
                {
                    await transaction.RollbackAsync();
                    return new SaleOrderResponseDto(false, null, null, 0, 0, 0, 0, 0, 0, OrderStatus.Cancelled, null, null, $"Product {itemDto.ProductId} not found.");
                }

                // Authoritative Price check: use product SalePrice unless override provided
                var unitPrice = product.SalePrice;
                var taxRate = product.Tax?.Rate ?? 0m;
                var isTaxInclusive = product.Tax?.IsInclusive ?? true;
                var isTaxExempt = product.Tax?.IsExempt ?? false;

                var taxResult = TaxCalculationEngine.Calculate(
                    unitPrice: unitPrice,
                    quantity: itemDto.Qty,
                    taxRatePercentage: taxRate,
                    isInclusive: isTaxInclusive,
                    isExempt: isTaxExempt
                );

                var lineDiscount = itemDto.DiscountAmount;
                var lineTotal = taxResult.TotalAmount - lineDiscount;

                subtotal += taxResult.BaseAmount;
                totalTax += taxResult.TaxAmount;
                totalDiscount += lineDiscount;

                // Handle FEFO batch allocation if product has TrackBatch or TrackExpiry
                Guid? allocatedBatchId = itemDto.BatchId;
                if ((product.TrackBatch || product.TrackExpiry) && allocatedBatchId == null)
                {
                    var availableBatches = await _context.ProductBatches
                        .Where(b => b.ProductId == product.Id && b.WarehouseId == warehouse.Id && b.QtyOnHand > 0)
                        .Select(b => new AvailableBatch(b.Id, b.BatchNo, b.ExpiryDate, b.QtyOnHand, b.CostPrice))
                        .ToListAsync();

                    var fefoResult = BatchFEFOEngine.Allocate(availableBatches, itemDto.Qty, DateTime.UtcNow);
                    if (fefoResult.Allocations.Any())
                    {
                        allocatedBatchId = fefoResult.Allocations.First().BatchId;
                    }
                }

                orderItems.Add(new POSOrderItem
                {
                    BusinessId = businessId,
                    ProductId = product.Id,
                    VariantId = itemDto.VariantId,
                    BatchId = allocatedBatchId,
                    NameSnapshot = product.Name,
                    PriceSnapshot = unitPrice,
                    TaxSnapshot = taxResult.TaxAmount,
                    Qty = itemDto.Qty,
                    DiscountAmount = lineDiscount,
                    LineTotal = lineTotal
                });

                // Deduct stock balance
                var stockBalance = await _context.StockBalances
                    .FirstOrDefaultAsync(s => s.ProductId == product.Id && 
                                              s.WarehouseId == warehouse.Id && 
                                              s.VariantId == itemDto.VariantId && 
                                              s.BatchId == allocatedBatchId);

                if (stockBalance == null)
                {
                    stockBalance = new StockBalance
                    {
                        BusinessId = businessId,
                        ProductId = product.Id,
                        VariantId = itemDto.VariantId,
                        WarehouseId = warehouse.Id,
                        BatchId = allocatedBatchId,
                        QtyOnHand = -itemDto.Qty
                    };
                    _context.StockBalances.Add(stockBalance);
                }
                else
                {
                    stockBalance.QtyOnHand -= itemDto.Qty;
                }

                // If batch tracked, deduct from ProductBatch as well
                if (allocatedBatchId.HasValue)
                {
                    var batch = await _context.ProductBatches.FindAsync(allocatedBatchId.Value);
                    if (batch != null)
                    {
                        batch.QtyOnHand -= itemDto.Qty;
                    }
                }

                // Append StockTransaction (immutable stock ledger)
                _context.StockTransactions.Add(new StockTransaction
                {
                    BusinessId = businessId,
                    ProductId = product.Id,
                    VariantId = itemDto.VariantId,
                    WarehouseId = warehouse.Id,
                    BatchId = allocatedBatchId,
                    Type = StockTransactionType.POSSale,
                    Qty = -itemDto.Qty,
                    UnitCost = product.PurchasePrice,
                    ReferenceType = "POSOrder",
                    ReferenceId = Guid.Empty // will be set once order is created
                });
            }

            var total = subtotal + totalTax - totalDiscount;
            var paidAmount = dto.Payments.Sum(p => p.Amount);
            var changeAmount = paidAmount > total ? paidAmount - total : 0m;

            var order = new POSOrder
            {
                BusinessId = businessId,
                InvoiceNo = invoiceNo,
                BranchId = dto.BranchId,
                CounterId = dto.CounterId,
                SessionId = dto.SessionId,
                CustomerId = dto.CustomerId,
                CashierId = cashierId,
                BusinessDate = session.BusinessDate,
                Subtotal = subtotal,
                DiscountTotal = totalDiscount,
                TaxTotal = totalTax,
                Total = total,
                PaidAmount = paidAmount,
                ChangeAmount = changeAmount,
                Status = OrderStatus.Completed,
                ClientTransactionId = dto.ClientTransactionId,
                FbrStatus = FbrInvoiceStatus.NotApplicable
            };

            foreach (var item in orderItems)
            {
                item.POSOrderId = order.Id;
                order.Items.Add(item);
            }

            foreach (var payDto in dto.Payments)
            {
                order.Payments.Add(new Payment
                {
                    BusinessId = businessId,
                    POSOrderId = order.Id,
                    PaymentMethodId = payDto.PaymentMethodId,
                    Amount = payDto.Amount,
                    TenderedAmount = payDto.TenderedAmount,
                    ReferenceNo = payDto.ReferenceNo,
                    CardLastFour = payDto.CardLastFour
                });
            }

            _context.POSOrders.Add(order);

            // Handle Customer Udhaar if customer is present and payment method is CustomerCredit
            if (dto.CustomerId.HasValue)
            {
                var creditPayment = dto.Payments.FirstOrDefault(p => p.PaymentMethodId == Guid.Parse("00000000-0000-0000-0000-000000000007") || p.ReferenceNo == "Udhaar");
                if (creditPayment != null)
                {
                    var customer = await _context.Customers.FindAsync(dto.CustomerId.Value);
                    if (customer != null)
                    {
                        customer.CurrentBalance += creditPayment.Amount;

                        _context.CustomerLedgers.Add(new CustomerLedger
                        {
                            BusinessId = businessId,
                            CustomerId = customer.Id,
                            Date = DateTime.UtcNow,
                            Type = "Sale",
                            ReferenceId = order.Id,
                            Debit = creditPayment.Amount,
                            Credit = 0m,
                            RunningBalance = customer.CurrentBalance,
                            Notes = $"POS Invoice #{invoiceNo}"
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new SaleOrderResponseDto(
                Success: true,
                OrderId: order.Id,
                InvoiceNo: order.InvoiceNo,
                Subtotal: order.Subtotal,
                DiscountTotal: order.DiscountTotal,
                TaxTotal: order.TaxTotal,
                Total: order.Total,
                PaidAmount: order.PaidAmount,
                ChangeAmount: order.ChangeAmount,
                Status: order.Status,
                FbrInvoiceNo: order.FbrInvoiceNo,
                FbrQrCode: order.FbrQrCode,
                Message: "Order placed successfully."
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new SaleOrderResponseDto(false, null, null, 0, 0, 0, 0, 0, 0, OrderStatus.Cancelled, null, null, $"Billing error: {ex.Message}");
        }
    }

    public async Task<HeldBillSummaryDto> HoldBillAsync(HoldBillDto dto, Guid cashierId)
    {
        var businessId = _tenantProvider.CurrentBusinessId ?? Guid.Empty;
        var holdNo = $"HOLD-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var heldBill = new HeldBill
        {
            BusinessId = businessId,
            HoldNo = holdNo,
            BranchId = dto.BranchId,
            CounterId = dto.CounterId,
            CashierId = cashierId,
            CustomerId = dto.CustomerId,
            Note = dto.Note,
            Status = "Held"
        };

        foreach (var item in dto.Items)
        {
            heldBill.Items.Add(new HeldBillItem
            {
                BusinessId = businessId,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                Qty = item.Qty,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount
            });
        }

        _context.HeldBills.Add(heldBill);
        await _context.SaveChangesAsync();

        var estTotal = heldBill.Items.Sum(i => (i.UnitPrice * i.Qty) - i.DiscountAmount);

        return new HeldBillSummaryDto(
            HeldBillId: heldBill.Id,
            HoldNo: heldBill.HoldNo,
            CreatedAtUtc: heldBill.CreatedAtUtc,
            Note: heldBill.Note,
            ItemCount: heldBill.Items.Count,
            EstimatedTotal: estTotal
        );
    }

    public async Task<CreateSaleOrderDto?> ResumeHeldBillAsync(Guid heldBillId)
    {
        var heldBill = await _context.HeldBills
            .Include(h => h.Items)
            .FirstOrDefaultAsync(h => h.Id == heldBillId && h.Status == "Held");

        if (heldBill == null) return null;

        heldBill.Status = "Resumed";
        await _context.SaveChangesAsync();

        return new CreateSaleOrderDto(
            ClientTransactionId: Guid.NewGuid(),
            BranchId: heldBill.BranchId,
            CounterId: heldBill.CounterId,
            SessionId: Guid.Empty, // Will be attached to current cashier session
            CustomerId: heldBill.CustomerId,
            Items: heldBill.Items.Select(i => new SaleOrderItemDto(
                i.ProductId, i.VariantId, null, i.Qty, i.UnitPrice, i.DiscountAmount)).ToList(),
            Payments: new List<SaleOrderPaymentDto>()
        );
    }

    public async Task<CounterSessionReportDto> GenerateXReportAsync(Guid sessionId, Guid requestedBy)
    {
        return await BuildSessionReportAsync(sessionId, SessionReportType.XReport, 0m, requestedBy, false);
    }

    public async Task<CounterSessionReportDto> CloseSessionAndGenerateZReportAsync(Guid sessionId, decimal actualCashCounted, Guid closedBy)
    {
        return await BuildSessionReportAsync(sessionId, SessionReportType.ZReport, actualCashCounted, closedBy, true);
    }

    private async Task<CounterSessionReportDto> BuildSessionReportAsync(
        Guid sessionId,
        SessionReportType reportType,
        decimal actualCashCounted,
        Guid operatorId,
        bool lockSession)
    {
        var session = await _context.CounterSessions
            .Include(s => s.CashMovements)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            throw new InvalidOperationException("Counter session not found.");
        }

        var orders = await _context.POSOrders
            .Where(o => o.SessionId == sessionId && o.Status == OrderStatus.Completed)
            .Include(o => o.Payments)
            .ThenInclude(p => p.PaymentMethod)
            .ToListAsync();

        var paymentBreakdown = new Dictionary<string, decimal>();
        decimal cashSales = 0m;

        foreach (var order in orders)
        {
            foreach (var payment in order.Payments)
            {
                var methodName = payment.PaymentMethod?.Name ?? "Unknown";
                if (!paymentBreakdown.ContainsKey(methodName))
                    paymentBreakdown[methodName] = 0m;

                paymentBreakdown[methodName] += payment.Amount;

                if (payment.PaymentMethod?.Type == PaymentMethodType.Cash)
                {
                    cashSales += payment.Amount;
                }
            }
        }

        // Aggregate cash movements
        decimal cashReceived = session.CashMovements.Where(m => m.Type == "UdhaarReceived" || m.Type == "PaidIn").Sum(m => m.Amount);
        decimal cashRefunds = session.CashMovements.Where(m => m.Type == "Refund").Sum(m => m.Amount);
        decimal cashExpenses = session.CashMovements.Where(m => m.Type == "Expense").Sum(m => m.Amount);
        decimal cashPaidOut = session.CashMovements.Where(m => m.Type == "CashDrop" || m.Type == "PaidOut").Sum(m => m.Amount);

        var reconInput = new CashReconciliationInput(
            OpeningCash: session.OpeningCash,
            CashSales: cashSales,
            CashReceived: cashReceived,
            CashRefunds: cashRefunds,
            CashExpenses: cashExpenses,
            CashPaidOut: cashPaidOut,
            ActualCash: reportType == SessionReportType.ZReport ? actualCashCounted : session.OpeningCash + cashSales
        );

        var reconResult = CashReconciliationEngine.Calculate(reconInput);

        if (lockSession)
        {
            session.ExpectedCash = reconResult.ExpectedCash;
            session.ActualCash = actualCashCounted;
            session.Variance = reconResult.Variance;
            session.EndUtc = DateTime.UtcNow;
            session.Status = CounterSessionStatus.Closed;
        }

        var reportDto = new CounterSessionReportDto(
            SessionId: session.Id,
            ReportType: reportType,
            GeneratedAtUtc: DateTime.UtcNow,
            OpeningCash: session.OpeningCash,
            CashSales: cashSales,
            CashReceived: cashReceived,
            CashRefunds: cashRefunds,
            CashExpenses: cashExpenses,
            CashPaidOut: cashPaidOut,
            ExpectedCash: reconResult.ExpectedCash,
            ActualCash: reconInput.ActualCash,
            Variance: reconResult.Variance,
            TotalInvoices: orders.Count,
            TotalRevenue: orders.Sum(o => o.Total),
            PaymentMethodBreakdown: paymentBreakdown
        );

        // Store snapshot in CounterSessionReports table
        _context.CounterSessionReports.Add(new CounterSessionReport
        {
            BusinessId = session.BusinessId,
            SessionId = session.Id,
            Type = reportType,
            GeneratedAtUtc = reportDto.GeneratedAtUtc,
            GeneratedBy = operatorId,
            SnapshotJson = JsonSerializer.Serialize(reportDto),
            PrintCount = 1
        });

        await _context.SaveChangesAsync();
        return reportDto;
    }
}

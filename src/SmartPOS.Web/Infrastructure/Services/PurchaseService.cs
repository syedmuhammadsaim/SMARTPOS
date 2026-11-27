using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Purchasing;
using SmartPOS.Application.Features.Purchasing.DTOs;
using SmartPOS.Application.Features.Sequences.Interfaces;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Purchasing;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class PurchaseService : IPurchaseService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDocumentSequenceService _sequenceService;

    public PurchaseService(SmartPOSDbContext context, ITenantProvider tenantProvider, IDocumentSequenceService sequenceService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _sequenceService = sequenceService;
    }

    public async Task<List<SupplierDto>> GetAllSuppliersAsync(string? searchTerm = null, CancellationToken ct = default)
    {
        var query = _context.Suppliers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) ||
                                     s.Code.ToLower().Contains(term) ||
                                     (s.Phone != null && s.Phone.Contains(term)) ||
                                     (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(term)));
        }

        return await query.Select(s => new SupplierDto
        {
            Id = s.Id,
            SupplierCode = s.Code,
            CompanyName = s.Name,
            ContactPerson = s.ContactPerson ?? string.Empty,
            Phone = s.Phone ?? string.Empty,
            Email = s.Email,
            NTN = s.TaxNo,
            CurrentBalance = s.CurrentBalance,
            CreditDays = 30,
            IsActive = s.Status == "Active"
        }).ToListAsync(ct);
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s == null) return null;

        return new SupplierDto
        {
            Id = s.Id,
            SupplierCode = s.Code,
            CompanyName = s.Name,
            ContactPerson = s.ContactPerson ?? string.Empty,
            Phone = s.Phone ?? string.Empty,
            Email = s.Email,
            NTN = s.TaxNo,
            CurrentBalance = s.CurrentBalance,
            CreditDays = 30,
            IsActive = s.Status == "Active"
        };
    }

    public async Task<Guid> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.CurrentBusinessId ?? Guid.Empty;
        var supplierCode = await _sequenceService.GetNextNumberAsync(tenantId, null, null, "SUP", "SUP");

        var supplier = new Supplier
        {
            BusinessId = tenantId,
            Code = supplierCode,
            Name = request.CompanyName,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            TaxNo = request.NTN,
            CurrentBalance = 0m,
            Status = "Active"
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(ct);
        return supplier.Id;
    }

    public async Task<GoodsReceiptResponseDto> ProcessGoodsReceiptAsync(GoodsReceiptRequest request, string receivedBy, CancellationToken ct = default)
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, ct);
        if (supplier == null)
            throw new InvalidOperationException($"Supplier {request.SupplierId} not found.");

        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId, ct);
        if (warehouse == null)
            throw new InvalidOperationException($"Warehouse {request.WarehouseId} not found.");

        var tenantId = _tenantProvider.CurrentBusinessId ?? Guid.Empty;
        var grnNo = await _sequenceService.GetNextNumberAsync(tenantId, warehouse.BranchId, null, "GRN", "GRN");

        decimal totalAmount = 0m;

        var goodsReceipt = new GoodsReceipt
        {
            BusinessId = tenantId,
            ReceiptNo = grnNo,
            PurchaseOrderId = request.PurchaseOrderId,
            SupplierId = supplier.Id,
            WarehouseId = warehouse.Id,
            ReceiptDate = DateTime.UtcNow,
            SupplierInvoiceNo = request.SupplierInvoiceNo,
            Status = "Received"
        };
        _context.GoodsReceipts.Add(goodsReceipt);

        foreach (var item in request.Items)
        {
            var lineTotal = item.ReceivedQuantity * item.UnitCost;
            totalAmount += lineTotal;

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, ct);
            if (product == null) continue;

            // 1. GoodsReceiptItem
            var grItem = new GoodsReceiptItem
            {
                BusinessId = tenantId,
                GoodsReceiptId = goodsReceipt.Id,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                QtyReceived = item.ReceivedQuantity,
                UnitCost = item.UnitCost,
                LandedCost = item.UnitCost,
                BatchNo = item.BatchNumber,
                ExpiryDate = item.ExpiryDate
            };
            _context.GoodsReceiptItems.Add(grItem);

            // 2. Handle Batch (FEFO support)
            ProductBatch? batch = null;
            if (product.TrackBatch || product.TrackExpiry || !string.IsNullOrWhiteSpace(item.BatchNumber))
            {
                var batchNo = !string.IsNullOrWhiteSpace(item.BatchNumber)
                    ? item.BatchNumber
                    : $"B-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

                var expiry = item.ExpiryDate ?? DateTime.UtcNow.AddYears(1);

                batch = new ProductBatch
                {
                    BusinessId = tenantId,
                    ProductId = product.Id,
                    VariantId = item.VariantId,
                    WarehouseId = warehouse.Id,
                    BatchNo = batchNo,
                    ManufactureDate = item.ManufactureDate,
                    ExpiryDate = expiry,
                    CostPrice = item.UnitCost,
                    QtyReceived = item.ReceivedQuantity,
                    QtyOnHand = item.ReceivedQuantity,
                    GoodsReceiptItemId = grItem.Id,
                    Status = "Active"
                };
                _context.ProductBatches.Add(batch);
            }

            // 3. Update StockBalance
            var balance = await _context.StockBalances.FirstOrDefaultAsync(sb =>
                sb.WarehouseId == warehouse.Id &&
                sb.ProductId == product.Id &&
                sb.VariantId == item.VariantId &&
                sb.BatchId == (batch != null ? batch.Id : (Guid?)null), ct);

            if (balance == null)
            {
                balance = new StockBalance
                {
                    BusinessId = tenantId,
                    WarehouseId = warehouse.Id,
                    ProductId = product.Id,
                    VariantId = item.VariantId,
                    BatchId = batch?.Id,
                    QtyOnHand = item.ReceivedQuantity
                };
                _context.StockBalances.Add(balance);
            }
            else
            {
                balance.QtyOnHand += item.ReceivedQuantity;
            }

            // 4. Record Immutable StockTransaction
            var tx = new StockTransaction
            {
                BusinessId = tenantId,
                ProductId = product.Id,
                VariantId = item.VariantId,
                WarehouseId = warehouse.Id,
                BatchId = batch?.Id,
                Type = StockTransactionType.GoodsReceipt,
                Qty = item.ReceivedQuantity, // Positive for stock-in
                UnitCost = item.UnitCost,
                ReferenceType = "GoodsReceipt",
                ReferenceId = goodsReceipt.Id,
                Note = $"GRN #{grnNo} from {supplier.Name}"
            };
            _context.StockTransactions.Add(tx);

            // Update product purchase/sale price if requested
            if (item.SalePrice > 0)
            {
                product.SalePrice = item.SalePrice;
            }
            product.PurchasePrice = item.UnitCost;
        }

        goodsReceipt.TotalAmount = totalAmount;

        // 5. Update Supplier Ledger and Balance
        supplier.CurrentBalance += totalAmount;

        var ledgerEntry = new SupplierLedger
        {
            BusinessId = tenantId,
            SupplierId = supplier.Id,
            Date = DateTime.UtcNow,
            Type = "Invoice",
            ReferenceId = goodsReceipt.Id,
            Debit = 0m,
            Credit = totalAmount, // Increases liability
            RunningBalance = supplier.CurrentBalance,
            Notes = $"GRN #{grnNo} (Supplier Inv: {request.SupplierInvoiceNo})"
        };
        _context.SupplierLedgers.Add(ledgerEntry);

        await _context.SaveChangesAsync(ct);

        return new GoodsReceiptResponseDto
        {
            Id = goodsReceipt.Id,
            GRNNumber = grnNo,
            ReceivedDate = goodsReceipt.ReceiptDate,
            TotalAmount = totalAmount,
            ItemCount = request.Items.Count
        };
    }

    public async Task<string> RecordSupplierPaymentAsync(SupplierPaymentRequest request, string recordedBy, CancellationToken ct = default)
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, ct);
        if (supplier == null)
            throw new InvalidOperationException($"Supplier {request.SupplierId} not found.");

        var tenantId = _tenantProvider.CurrentBusinessId ?? Guid.Empty;
        var paymentNo = await _sequenceService.GetNextNumberAsync(tenantId, null, null, "SPAY", "SPAY");

        // Decrease supplier balance
        supplier.CurrentBalance -= request.Amount;

        var payment = new SupplierPayment
        {
            BusinessId = tenantId,
            PaymentNo = paymentNo,
            SupplierId = supplier.Id,
            Amount = request.Amount,
            PaymentMethodId = request.PaymentMethodId,
            InvoiceReference = request.ReferenceNo,
            PaymentDate = DateTime.UtcNow,
            Notes = request.Notes
        };
        _context.SupplierPayments.Add(payment);

        var ledgerEntry = new SupplierLedger
        {
            BusinessId = tenantId,
            SupplierId = supplier.Id,
            Date = DateTime.UtcNow,
            Type = "Payment",
            ReferenceId = payment.Id,
            Debit = request.Amount, // Decreases liability
            Credit = 0m,
            RunningBalance = supplier.CurrentBalance,
            Notes = $"Payment voucher #{paymentNo}. {request.Notes}".Trim()
        };
        _context.SupplierLedgers.Add(ledgerEntry);

        await _context.SaveChangesAsync(ct);
        return paymentNo;
    }

    public async Task<List<SupplierLedgerEntryDto>> GetSupplierLedgerAsync(Guid supplierId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var query = _context.SupplierLedgers
            .AsNoTracking()
            .Where(l => l.SupplierId == supplierId);

        if (fromDate.HasValue)
            query = query.Where(l => l.Date >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.Date <= toDate.Value);

        var list = await query
            .OrderBy(l => l.Date)
            .ThenBy(l => l.CreatedAtUtc)
            .ToListAsync(ct);

        return list.Select(l => new SupplierLedgerEntryDto
        {
            Id = l.Id,
            Date = l.Date,
            TransactionType = l.Type,
            DocumentNumber = l.ReferenceId.HasValue ? l.ReferenceId.Value.ToString()[..8] : string.Empty,
            Description = l.Notes,
            Debit = l.Debit,
            Credit = l.Credit,
            Balance = l.RunningBalance
        }).ToList();
    }
}

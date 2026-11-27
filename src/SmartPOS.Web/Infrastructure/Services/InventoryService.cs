using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Inventory;
using SmartPOS.Application.Features.Inventory.DTOs;
using SmartPOS.Application.Features.Sequences.Interfaces;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDocumentSequenceService _sequenceService;

    public InventoryService(SmartPOSDbContext context, ITenantProvider tenantProvider, IDocumentSequenceService sequenceService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _sequenceService = sequenceService;
    }

    public async Task<List<StockBalanceDto>> GetStockBalancesAsync(Guid? warehouseId = null, bool lowStockOnly = false, CancellationToken ct = default)
    {
        var query = _context.StockBalances
            .AsNoTracking()
            .Include(sb => sb.Product)
            .Include(sb => sb.Variant)
            .Include(sb => sb.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(sb => sb.WarehouseId == warehouseId.Value);

        var list = await query.ToListAsync(ct);

        var grouped = list
            .GroupBy(sb => new { sb.ProductId, sb.VariantId, sb.WarehouseId })
            .Select(g =>
            {
                var first = g.First();
                var totalQty = g.Sum(x => x.QtyOnHand);
                var product = first.Product;
                var variant = first.Variant;
                var warehouse = first.Warehouse;
                var alertQty = product?.MinStock ?? 5m;

                return new StockBalanceDto
                {
                    ProductId = first.ProductId,
                    SKU = variant?.VariantSku ?? product?.Sku ?? string.Empty,
                    ProductName = product?.Name ?? string.Empty,
                    VariantName = variant != null ? $"{variant.Size} {variant.Color}".Trim() : null,
                    WarehouseId = first.WarehouseId,
                    WarehouseName = warehouse?.Name ?? "Main Warehouse",
                    QuantityOnHand = totalQty,
                    QuantityReserved = 0m,
                    AverageCostPrice = product?.PurchasePrice ?? 0m,
                    AlertQuantity = alertQty
                };
            });

        if (lowStockOnly)
            grouped = grouped.Where(x => x.IsLowStock);

        return grouped.ToList();
    }

    public async Task<List<ExpiringStockAlertDto>> GetExpiringBatchesAsync(int daysThreshold = 60, Guid? warehouseId = null, CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysThreshold);

        var query = _context.ProductBatches
            .AsNoTracking()
            .Include(b => b.Product)
            .Include(b => b.Warehouse)
            .Where(b => b.QtyOnHand > 0 && b.ExpiryDate <= cutoffDate);

        if (warehouseId.HasValue)
            query = query.Where(b => b.WarehouseId == warehouseId.Value);

        return await query
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new ExpiringStockAlertDto
            {
                ProductId = b.ProductId,
                SKU = b.Product != null ? b.Product.Sku : string.Empty,
                ProductName = b.Product != null ? b.Product.Name : string.Empty,
                BatchNumber = b.BatchNo,
                ExpiryDate = b.ExpiryDate,
                Quantity = b.QtyOnHand,
                WarehouseName = b.Warehouse != null ? b.Warehouse.Name : "Main Warehouse"
            })
            .ToListAsync(ct);
    }

    public async Task<string> InitiateStockTransferAsync(CreateStockTransferRequest request, string initiatedBy, CancellationToken ct = default)
    {
        if (request.SourceWarehouseId == request.DestinationWarehouseId)
            throw new InvalidOperationException("Source and Destination warehouses cannot be identical.");

        var tenantId = _tenantProvider.CurrentBusinessId ?? Guid.Empty;
        var transferNo = await _sequenceService.GetNextNumberAsync(tenantId, null, null, "TRF", "TRF");

        var transfer = new StockTransfer
        {
            BusinessId = tenantId,
            TransferNo = transferNo,
            FromWarehouseId = request.SourceWarehouseId,
            ToWarehouseId = request.DestinationWarehouseId,
            Status = "Dispatched",
            Reason = request.Notes,
            DispatchedAtUtc = DateTime.UtcNow
        };
        _context.StockTransfers.Add(transfer);

        foreach (var item in request.Items)
        {
            var transferItem = new StockTransferItem
            {
                BusinessId = tenantId,
                StockTransferId = transfer.Id,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                BatchId = item.BatchId,
                Qty = item.Quantity,
                ReceivedQty = 0m
            };
            _context.StockTransferItems.Add(transferItem);

            // Deduct from Source Warehouse immediately upon dispatch
            var sourceBalance = await _context.StockBalances.FirstOrDefaultAsync(sb =>
                sb.WarehouseId == request.SourceWarehouseId &&
                sb.ProductId == item.ProductId &&
                sb.VariantId == item.VariantId &&
                sb.BatchId == item.BatchId, ct);

            if (sourceBalance != null)
            {
                sourceBalance.QtyOnHand -= item.Quantity;
            }

            // Deduct from Batch if specified
            if (item.BatchId.HasValue)
            {
                var batch = await _context.ProductBatches.FirstOrDefaultAsync(b => b.Id == item.BatchId.Value, ct);
                if (batch != null) batch.QtyOnHand -= item.Quantity;
            }

            // Record StockTransaction for outbound
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, ct);
            _context.StockTransactions.Add(new StockTransaction
            {
                BusinessId = tenantId,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                WarehouseId = request.SourceWarehouseId,
                BatchId = item.BatchId,
                Type = StockTransactionType.StockTransferOut,
                Qty = -item.Quantity,
                UnitCost = product?.PurchasePrice ?? 0m,
                ReferenceType = "StockTransfer",
                ReferenceId = transfer.Id,
                Note = $"Transfer #{transferNo} dispatched"
            });
        }

        await _context.SaveChangesAsync(ct);
        return transferNo;
    }

    public async Task<bool> ReceiveStockTransferAsync(Guid transferId, string receivedBy, CancellationToken ct = default)
    {
        var transfer = await _context.StockTransfers
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == transferId, ct);

        if (transfer == null || transfer.Status == "Received")
            return false;

        var tenantId = _tenantProvider.CurrentBusinessId ?? Guid.Empty;

        foreach (var item in transfer.Items)
        {
            item.ReceivedQty = item.Qty;

            // Credit Destination Warehouse
            var destBalance = await _context.StockBalances.FirstOrDefaultAsync(sb =>
                sb.WarehouseId == transfer.ToWarehouseId &&
                sb.ProductId == item.ProductId &&
                sb.VariantId == item.VariantId &&
                sb.BatchId == item.BatchId, ct);

            if (destBalance == null)
            {
                destBalance = new StockBalance
                {
                    BusinessId = tenantId,
                    WarehouseId = transfer.ToWarehouseId,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    BatchId = item.BatchId,
                    QtyOnHand = item.Qty
                };
                _context.StockBalances.Add(destBalance);
            }
            else
            {
                destBalance.QtyOnHand += item.Qty;
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, ct);
            _context.StockTransactions.Add(new StockTransaction
            {
                BusinessId = tenantId,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                WarehouseId = transfer.ToWarehouseId,
                BatchId = item.BatchId,
                Type = StockTransactionType.StockTransferIn,
                Qty = item.Qty,
                UnitCost = product?.PurchasePrice ?? 0m,
                ReferenceType = "StockTransfer",
                ReferenceId = transfer.Id,
                Note = $"Transfer #{transfer.TransferNo} received"
            });
        }

        transfer.Status = "Received";
        transfer.ReceivedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AdjustStockAsync(StockAdjustmentRequest request, string adjustedBy, CancellationToken ct = default)
    {
        var balance = await _context.StockBalances.FirstOrDefaultAsync(sb =>
            sb.WarehouseId == request.WarehouseId &&
            sb.ProductId == request.ProductId &&
            sb.VariantId == request.VariantId &&
            sb.BatchId == request.BatchId, ct);

        var currentQty = balance?.QtyOnHand ?? 0m;
        var variance = request.PhysicalQuantity - currentQty;

        if (variance == 0m)
            return true; // No adjustment needed

        var tenantId = _tenantProvider.CurrentBusinessId ?? Guid.Empty;

        if (balance == null)
        {
            balance = new StockBalance
            {
                BusinessId = tenantId,
                WarehouseId = request.WarehouseId,
                ProductId = request.ProductId,
                VariantId = request.VariantId,
                BatchId = request.BatchId,
                QtyOnHand = request.PhysicalQuantity
            };
            _context.StockBalances.Add(balance);
        }
        else
        {
            balance.QtyOnHand = request.PhysicalQuantity;
        }

        if (request.BatchId.HasValue)
        {
            var batch = await _context.ProductBatches.FirstOrDefaultAsync(b => b.Id == request.BatchId.Value, ct);
            if (batch != null) batch.QtyOnHand = request.PhysicalQuantity;
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);
        _context.StockTransactions.Add(new StockTransaction
        {
            BusinessId = tenantId,
            ProductId = request.ProductId,
            VariantId = request.VariantId,
            WarehouseId = request.WarehouseId,
            BatchId = request.BatchId,
            Type = StockTransactionType.PhysicalCountCorrection,
            Qty = variance,
            UnitCost = product?.PurchasePrice ?? 0m,
            ReferenceType = "CountAdjustment",
            ReferenceId = Guid.NewGuid(),
            Note = $"Correction from {currentQty} to {request.PhysicalQuantity}. Reason: {request.Reason}"
        });

        await _context.SaveChangesAsync(ct);
        return true;
    }
}

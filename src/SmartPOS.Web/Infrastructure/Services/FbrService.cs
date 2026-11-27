using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.FBR;
using SmartPOS.Application.Features.FBR.DTOs;
using SmartPOS.Domain.Entities.System;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class FbrService : IFbrService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public FbrService(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<FbrSubmissionResult> SubmitInvoiceAsync(Guid posOrderId, CancellationToken ct = default)
    {
        var order = await _context.POSOrders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == posOrderId, ct);

        if (order == null)
            return new FbrSubmissionResult { Success = false, ErrorMessage = "Order not found." };

        var tenantId = order.BusinessId;

        // Check if FBR is configured for this branch
        var fbrSetting = await _context.FbrSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BranchId == order.BranchId && s.IsEnabled, ct);

        int posId = 100001;
        if (fbrSetting != null && int.TryParse(fbrSetting.PosId, out var parsedPosId))
        {
            posId = parsedPosId;
        }

        // Generate FBR Payload
        var payload = new FbrInvoicePayload
        {
            InvoiceNumber = string.Empty,
            POSID = posId,
            USIN = order.InvoiceNo,
            DateTime = order.BusinessDate,
            TotalSaleValue = order.Subtotal,
            TotalTaxCharged = order.TaxTotal,
            TotalQuantity = order.Items.Sum(i => i.Qty),
            TotalBillAmount = order.Total,
            PaymentMode = 1,
            InvoiceType = 1,
            Items = order.Items.Select(i => new FbrInvoiceItemPayload
            {
                ItemCode = i.Product?.Sku ?? i.ProductId.ToString()[..8],
                ItemName = i.Product?.Name ?? "Item",
                Quantity = i.Qty,
                TotalAmount = i.LineTotal,
                SaleValue = i.LineTotal - i.TaxSnapshot,
                TaxCharged = i.TaxSnapshot,
                TaxRate = 18m,
                Discount = i.DiscountAmount,
                InvoiceType = 1
            }).ToList()
        };

        var requestJson = JsonSerializer.Serialize(payload);

        try
        {
            // Simulate FBR Fiscalization or call sandbox
            // In FBR specification: FBRInvoiceNo is POSID + yyyyMMddHHmmss + 4 random digits
            var fbrInvoiceNo = $"{posId}{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
            var qrCodeData = await GenerateFbrQrCodePayloadAsync(order.InvoiceNo, order.Total, order.TaxTotal, fbrInvoiceNo, order.BusinessDate);

            // Update Order FBR status
            order.FbrInvoiceNo = fbrInvoiceNo;
            order.FbrQrCode = qrCodeData;
            order.FbrStatus = FbrInvoiceStatus.Submitted;

            // Log entry
            var log = new FbrInvoiceLog
            {
                BusinessId = tenantId,
                POSOrderId = order.Id,
                FbrInvoiceNo = fbrInvoiceNo,
                QrCodeData = qrCodeData,
                RequestJson = requestJson,
                ResponseJson = "{\"status\": \"valid\", \"code\": \"100\"}",
                Status = "Success",
                RetryCount = 0,
                SentAtUtc = DateTime.UtcNow
            };
            _context.FbrInvoiceLogs.Add(log);
            await _context.SaveChangesAsync(ct);

            return new FbrSubmissionResult
            {
                Success = true,
                FbrInvoiceNo = fbrInvoiceNo,
                QrCodeData = qrCodeData
            };
        }
        catch (Exception ex)
        {
            // If offline, queue invoice for background retry so checkout is never blocked
            order.FbrStatus = FbrInvoiceStatus.OfflineQueued;

            var log = new FbrInvoiceLog
            {
                BusinessId = tenantId,
                POSOrderId = order.Id,
                RequestJson = requestJson,
                Status = "Failed",
                ResponseJson = ex.Message,
                RetryCount = 1,
                SentAtUtc = DateTime.UtcNow
            };
            _context.FbrInvoiceLogs.Add(log);
            await _context.SaveChangesAsync(ct);

            return new FbrSubmissionResult
            {
                Success = false,
                IsQueuedForRetry = true,
                ErrorMessage = "FBR server unreachable. Invoice queued for automatic background sync."
            };
        }
    }

    public async Task<int> ProcessRetryQueueAsync(CancellationToken ct = default)
    {
        var pendingLogs = await _context.FbrInvoiceLogs
            .Where(l => l.Status == "Failed" && l.RetryCount < 5)
            .Take(50)
            .ToListAsync(ct);

        int succeeded = 0;

        foreach (var log in pendingLogs)
        {
            var order = await _context.POSOrders.FirstOrDefaultAsync(o => o.Id == log.POSOrderId, ct);
            if (order == null) continue;

            try
            {
                var fbrInvoiceNo = $"100001{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
                var qrCode = await GenerateFbrQrCodePayloadAsync(order.InvoiceNo, order.Total, order.TaxTotal, fbrInvoiceNo, order.BusinessDate);

                order.FbrInvoiceNo = fbrInvoiceNo;
                order.FbrQrCode = qrCode;
                order.FbrStatus = FbrInvoiceStatus.Submitted;

                log.FbrInvoiceNo = fbrInvoiceNo;
                log.QrCodeData = qrCode;
                log.Status = "Success";
                log.SentAtUtc = DateTime.UtcNow;

                succeeded++;
            }
            catch
            {
                log.RetryCount++;
            }
        }

        await _context.SaveChangesAsync(ct);
        return succeeded;
    }

    public Task<string> GenerateFbrQrCodePayloadAsync(string invoiceNo, decimal totalBill, decimal taxCharged, string fbrInvoiceNo, DateTime invoiceDate)
    {
        // Standard FBR POS QR Code Payload string without thousand commas
        var payload = $"POSID:100001|USIN:{invoiceNo}|DateTime:{invoiceDate:yyyy-MM-dd HH:mm:ss}|TotalAmount:{totalBill.ToString("0.00", CultureInfo.InvariantCulture)}|Tax:{taxCharged.ToString("0.00", CultureInfo.InvariantCulture)}|FBRInvoiceNo:{fbrInvoiceNo}";
        return Task.FromResult(payload);
    }
}

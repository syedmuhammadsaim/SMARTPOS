using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Localization;
using SmartPOS.Application.Features.Localization.DTOs;
using SmartPOS.Domain.Entities.System;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class ReceiptTemplateService : IReceiptTemplateService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ReceiptTemplateService(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<ReceiptTemplateDto> GetTemplateForBranchAsync(Guid? branchId, CancellationToken ct = default)
    {
        var template = await _context.ReceiptTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.BranchId == branchId, ct);

        template ??= await _context.ReceiptTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.BranchId == null, ct);

        if (template == null)
        {
            return new ReceiptTemplateDto(
                Id: Guid.Empty,
                BranchId: branchId,
                Name: "Standard 80mm Bilingual",
                HeaderText: "SMARTPOS ENTERPRISE RETAIL",
                FooterText: "Thank you for shopping with us!",
                ShowQr: true,
                Language: "UrduEnglish",
                PaperWidthMm: 80,
                UrduGreeting: "تشریف لانے کا شکریہ - دوبارہ ضرور تشریف لائیں",
                UrduReturnPolicy: "خریدا ہوا مال 7 دن کے اندر رسید کے ہمراہ تبدیل ہو سکتا ہے۔"
            );
        }

        return new ReceiptTemplateDto(
            template.Id,
            template.BranchId,
            template.Name,
            template.HeaderText,
            template.FooterText,
            template.ShowQr,
            template.Language,
            template.PaperWidthMm,
            UrduGreeting: "تشریف لانے کا شکریہ - دوبارہ ضرور تشریف لائیں",
            UrduReturnPolicy: "خریدا ہوا مال 7 دن کے اندر رسید کے ہمراہ تبدیل ہو سکتا ہے۔"
        );
    }

    public async Task<ReceiptTemplateDto> SaveTemplateAsync(UpsertReceiptTemplateRequest request, CancellationToken ct = default)
    {
        var template = await _context.ReceiptTemplates
            .FirstOrDefaultAsync(t => t.BranchId == request.BranchId, ct);

        if (template == null)
        {
            template = new ReceiptTemplate
            {
                BusinessId = _tenantProvider.CurrentBusinessId ?? Guid.Empty,
                BranchId = request.BranchId,
                Name = request.Name,
                HeaderText = request.HeaderText,
                FooterText = request.FooterText,
                ShowQr = request.ShowQr,
                Language = request.Language,
                PaperWidthMm = request.PaperWidthMm
            };
            _context.ReceiptTemplates.Add(template);
        }
        else
        {
            template.Name = request.Name;
            template.HeaderText = request.HeaderText;
            template.FooterText = request.FooterText;
            template.ShowQr = request.ShowQr;
            template.Language = request.Language;
            template.PaperWidthMm = request.PaperWidthMm;
        }

        await _context.SaveChangesAsync(ct);

        return new ReceiptTemplateDto(
            template.Id,
            template.BranchId,
            template.Name,
            template.HeaderText,
            template.FooterText,
            template.ShowQr,
            template.Language,
            template.PaperWidthMm,
            request.UrduGreeting,
            request.UrduReturnPolicy
        );
    }

    public Task<string> FormatBilingualReceiptTextAsync(BilingualReceiptPayload p, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        var width = 42; // standard 80mm thermal 42-col width
        var separator = new string('-', width);
        var doubleSep = new string('=', width);

        // Header
        sb.AppendLine(Center(p.BusinessName, width));
        if (!string.IsNullOrEmpty(p.BranchName))
            sb.AppendLine(Center(p.BranchName, width));

        if (!string.IsNullOrEmpty(p.NtnNumber))
            sb.AppendLine(Center($"NTN: {p.NtnNumber}", width));
        if (!string.IsNullOrEmpty(p.StrnNumber))
            sb.AppendLine(Center($"STRN: {p.StrnNumber}", width));

        sb.AppendLine(doubleSep);
        sb.AppendLine($"Invoice : {p.InvoiceNo}");
        sb.AppendLine($"Date    : {p.Date:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Cashier : {p.CashierName} | Counter: {p.CounterName}");
        sb.AppendLine(separator);

        // Column Titles
        sb.AppendLine(string.Format("{0,-20} {1,5} {2,7} {3,8}", "Item / نام", "Qty", "Price", "Total"));
        sb.AppendLine(separator);

        // Items
        foreach (var it in p.Items)
        {
            var name = it.NameEnglish.Length > 20 ? it.NameEnglish[..20] : it.NameEnglish;
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-20} {1,5:0.##} {2,7:0.00} {3,8:0.00}", name, it.Qty, it.UnitPrice, it.LineTotal));
            
            // Bilingual Urdu Line
            if (!string.IsNullOrWhiteSpace(it.NameUrdu) && (p.Language == "Urdu" || p.Language == "UrduEnglish"))
            {
                sb.AppendLine($"  [{it.NameUrdu}]");
            }
        }

        sb.AppendLine(separator);

        // Totals
        sb.AppendLine(PadRightLeft("Subtotal:", $"Rs. {p.Subtotal:N2}", width));
        if (p.DiscountTotal > 0)
            sb.AppendLine(PadRightLeft("Discount:", $"-Rs. {p.DiscountTotal:N2}", width));
        if (p.TaxTotal > 0)
            sb.AppendLine(PadRightLeft("Tax / GST:", $"Rs. {p.TaxTotal:N2}", width));

        sb.AppendLine(doubleSep);
        sb.AppendLine(PadRightLeft("TOTAL / کُل رقم:", $"Rs. {p.GrandTotal:N2}", width));
        sb.AppendLine(doubleSep);

        sb.AppendLine(PadRightLeft("Cash Tendered:", $"Rs. {p.PaidAmount:N2}", width));
        sb.AppendLine(PadRightLeft("Change Return / بقایا:", $"Rs. {p.ChangeAmount:N2}", width));
        sb.AppendLine(separator);

        // FBR Details
        if (!string.IsNullOrEmpty(p.FbrInvoiceNo))
        {
            sb.AppendLine(Center("[ FBR FISCAL POS VERIFIED ]", width));
            sb.AppendLine(Center($"FBR Inv #: {p.FbrInvoiceNo}", width));
            if (!string.IsNullOrEmpty(p.FbrQrCode))
            {
                sb.AppendLine(Center($"FBR QR: {p.FbrQrCode[..Math.Min(35, p.FbrQrCode.Length)]}...", width));
            }
            sb.AppendLine(separator);
        }

        // Urdu Greeting & Policy
        if (!string.IsNullOrWhiteSpace(p.UrduGreeting))
        {
            sb.AppendLine(Center(p.UrduGreeting, width));
        }

        if (!string.IsNullOrWhiteSpace(p.UrduReturnPolicy))
        {
            sb.AppendLine(Center(p.UrduReturnPolicy, width));
        }

        sb.AppendLine(Center("*** Powered by SMARTPOS Enterprise ***", width));

        return Task.FromResult(sb.ToString());
    }

    private static string Center(string text, int width)
    {
        if (text.Length >= width) return text[..width];
        var left = (width - text.Length) / 2;
        return text.PadLeft(left + text.Length).PadRight(width);
    }

    private static string PadRightLeft(string left, string right, int width)
    {
        var spaces = Math.Max(1, width - (left.Length + right.Length));
        return left + new string(' ', spaces) + right;
    }
}

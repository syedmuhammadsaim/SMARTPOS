using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Feedback.DTOs;
using SmartPOS.Application.Features.Licensing.DTOs;
using SmartPOS.Application.Features.Localization.DTOs;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.System;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Services;
using Xunit;

namespace SmartPOS.Tests;

public class LicensingFeedbackLocalizationTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _counterId = Guid.NewGuid();

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

    [Fact]
    public async Task LicenseService_Validates_Active_GracePeriod_And_DeviceLimit()
    {
        var tenant = new MockTenantProvider(_tenantId);
        using var context = CreateDbContext(tenant);

        var business = new Business { Id = _tenantId, Name = "Al-Madina Cash & Carry" };
        context.Businesses.Add(business);

        // 1. Generate active license with 2 device limit
        var licenseService = new LicenseService(context, tenant);
        var key = await licenseService.GenerateLicenseKeyAsync(_tenantId, "Enterprise", deviceLimit: 2, validityDays: 30);

        // Validate active terminal 1
        var val1 = await licenseService.ActivateLicenseAsync(new ActivateLicenseRequest(
            _tenantId,
            key,
            DeviceFingerprint: "MAC-AA-BB-CC-11",
            MachineName: "POS-COUNTER-01",
            CounterId: _counterId
        ));
        Assert.True(val1.IsValid);
        Assert.Equal("Active", val1.Status);
        Assert.Equal(1, val1.ActiveDevicesCount);

        // Activate terminal 2
        var val2 = await licenseService.ActivateLicenseAsync(new ActivateLicenseRequest(
            _tenantId,
            key,
            DeviceFingerprint: "MAC-AA-BB-CC-22",
            MachineName: "POS-COUNTER-02",
            CounterId: Guid.NewGuid()
        ));
        Assert.True(val2.IsValid);
        Assert.Equal(2, val2.ActiveDevicesCount);

        // Attempt terminal 3 (exceeds limit 2)
        var val3 = await licenseService.ActivateLicenseAsync(new ActivateLicenseRequest(
            _tenantId,
            key,
            DeviceFingerprint: "MAC-AA-BB-CC-33",
            MachineName: "POS-COUNTER-03",
            CounterId: Guid.NewGuid()
        ));
        Assert.False(val3.IsValid);
        Assert.Equal("DeviceLimitExceeded", val3.Status);

        // 2. Test Grace Period (Expired 5 days ago <= 14 days grace)
        var graceKey = "SP-GRACE-KEY-001";
        context.LicenseKeys.Add(new LicenseKey
        {
            BusinessId = _tenantId,
            Key = graceKey,
            DeviceLimit = 5,
            ExpiresAt = DateTime.UtcNow.AddDays(-5)
        });
        await context.SaveChangesAsync();

        var graceResult = await licenseService.ValidateLicenseAsync(new LicenseValidationRequest(
            _tenantId,
            graceKey,
            DeviceFingerprint: "MAC-AA-BB-CC-11",
            MachineName: "POS-COUNTER-01"
        ));
        Assert.True(graceResult.IsValid);
        Assert.Equal("GracePeriod", graceResult.Status);

        // 3. Test Hard Expiration (Expired 20 days ago > 14 days grace)
        var expiredKey = "SP-EXPIRED-KEY-999";
        context.LicenseKeys.Add(new LicenseKey
        {
            BusinessId = _tenantId,
            Key = expiredKey,
            DeviceLimit = 5,
            ExpiresAt = DateTime.UtcNow.AddDays(-20)
        });
        await context.SaveChangesAsync();

        var expiredResult = await licenseService.ValidateLicenseAsync(new LicenseValidationRequest(
            _tenantId,
            expiredKey,
            DeviceFingerprint: "MAC-AA-BB-CC-11",
            MachineName: "POS-COUNTER-01"
        ));
        Assert.False(expiredResult.IsValid);
        Assert.Equal("Expired", expiredResult.Status);
    }

    [Fact]
    public async Task FeedbackService_Records_Feedback_And_Resolves_Complaints()
    {
        var tenant = new MockTenantProvider(_tenantId);
        using var context = CreateDbContext(tenant);

        var feedbackService = new FeedbackService(context, tenant);

        // 1. Record customer rating
        var fb = await feedbackService.RecordFeedbackAsync(new CreateFeedbackRequest(
            BranchId: _branchId,
            POSOrderId: Guid.NewGuid(),
            CustomerName: "Zubair Khan",
            CustomerPhone: "03211234567",
            RatingStars: 5,
            Comments: "Bohut behtareen service aur taiz billing!"
        ));
        Assert.Equal(5, fb.RatingStars);
        Assert.Equal("Zubair Khan", fb.CustomerName);

        // 2. Register complaint
        var managerId = Guid.NewGuid();
        var comp = await feedbackService.RegisterComplaintAsync(new CreateComplaintRequest(
            BranchId: _branchId,
            CustomerId: null,
            CustomerContact: "03009876543",
            Category: "PricingIssue",
            Description: "Rate on shelf was Rs. 200 but billed at Rs. 220.",
            AssignedToManagerId: managerId
        ));
        Assert.StartsWith("CMP-", comp.ComplaintNo);
        Assert.Equal("Pending", comp.Status);

        // 3. Resolve complaint
        var resolved = await feedbackService.ResolveComplaintAsync(new ResolveComplaintRequest(
            ComplaintId: comp.Id,
            ManagerId: managerId,
            ResolutionNotes: "Price tag corrected on shelf. Rs. 20 difference refunded to customer."
        ));
        Assert.Equal("Resolved", resolved.Status);
        Assert.NotNull(resolved.ResolvedAtUtc);
        Assert.Contains("refunded", resolved.ResolutionNotes);
    }

    [Fact]
    public async Task ReceiptTemplateService_Formats_Bilingual_Urdu_English_Receipt()
    {
        var tenant = new MockTenantProvider(_tenantId);
        using var context = CreateDbContext(tenant);

        var templateService = new ReceiptTemplateService(context, tenant);

        var payload = new BilingualReceiptPayload(
            BusinessName: "AL-FATAH DEPARTMENTAL STORE",
            BranchName: "Gulberg Branch Lahore",
            NtnNumber: "1234567-8",
            StrnNumber: "09-87-6543-210-99",
            InvoiceNo: "INV-20261121-0088",
            Date: DateTime.UtcNow,
            CashierName: "Ali Cashier",
            CounterName: "Counter 02",
            Items: new List<BilingualReceiptItemPayload>
            {
                new("Panadol Extra 500mg", "پیناڈول ایکسٹرا", 2m, 35m, 70m),
                new("Olpers Milk 1 Litre", "اولپرز دودھ ایک لٹر", 1m, 290m, 290m)
            },
            Subtotal: 360m,
            DiscountTotal: 10m,
            TaxTotal: 15m,
            GrandTotal: 365m,
            PaidAmount: 500m,
            ChangeAmount: 135m,
            FbrInvoiceNo: "FBR-9988776655",
            FbrQrCode: "POSID:1001|INVOICE:INV-0088|TOTAL:365.00",
            Language: "UrduEnglish",
            UrduGreeting: "تشریف لانے کا شکریہ - دوبارہ ضرور تشریف لائیں",
            UrduReturnPolicy: "خریدا ہوا مال 7 دن کے اندر رسید کے ہمراہ تبدیل ہو سکتا ہے۔"
        );

        var receiptText = await templateService.FormatBilingualReceiptTextAsync(payload);

        Assert.Contains("AL-FATAH DEPARTMENTAL STORE", receiptText);
        Assert.Contains("NTN: 1234567-8", receiptText);
        Assert.Contains("پیناڈول ایکسٹرا", receiptText);
        Assert.Contains("اولپرز دودھ ایک لٹر", receiptText);
        Assert.Contains("FBR-9988776655", receiptText);
        Assert.Contains("تشریف لانے کا شکریہ", receiptText);
        Assert.Contains("TOTAL / کُل رقم:", receiptText);
    }
}

using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Promotions;
using SmartPOS.Application.Features.Promotions.DTOs;
using SmartPOS.Domain.Entities.PromotionsAndLoyalty;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class PromotionGiftCardService : IPromotionGiftCardService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public PromotionGiftCardService(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    private Guid GetTenantId() => _tenantProvider.CurrentBusinessId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<GiftCardDto> IssueGiftCardAsync(IssueGiftCardRequest request, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        var existing = await _context.GiftCards
            .FirstOrDefaultAsync(g => g.BusinessId == tenantId && g.CardNo == request.CardNo, ct);

        if (existing != null)
        {
            throw new InvalidOperationException($"Gift Card with number '{request.CardNo}' already exists.");
        }

        var card = new GiftCard
        {
            BusinessId = tenantId,
            CardNo = request.CardNo,
            InitialAmount = request.Amount,
            Balance = request.Amount,
            CustomerId = request.CustomerId,
            IssuedDate = DateTime.UtcNow,
            ExpiryDate = request.ExpiryDate ?? DateTime.UtcNow.AddYears(1),
            Status = "Active"
        };

        var tx = new GiftCardTransaction
        {
            BusinessId = tenantId,
            GiftCardId = card.Id,
            Amount = request.Amount,
            Type = "Issue"
        };
        card.Transactions.Add(tx);

        _context.GiftCards.Add(card);
        await _context.SaveChangesAsync(ct);

        return new GiftCardDto(
            card.Id,
            card.CardNo,
            card.Balance,
            card.InitialAmount,
            card.IssuedDate,
            card.ExpiryDate,
            card.Status,
            null
        );
    }

    public async Task<GiftCardDto?> GetGiftCardBalanceAsync(string cardNo, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        var card = await _context.GiftCards
            .Include(g => g.Customer)
            .FirstOrDefaultAsync(g => g.BusinessId == tenantId && g.CardNo == cardNo, ct);

        if (card == null) return null;

        return new GiftCardDto(
            card.Id,
            card.CardNo,
            card.Balance,
            card.InitialAmount,
            card.IssuedDate,
            card.ExpiryDate,
            card.Status,
            card.Customer?.Name
        );
    }

    public async Task<RedeemGiftCardResponse> RedeemGiftCardAsync(RedeemGiftCardRequest request, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        var card = await _context.GiftCards
            .FirstOrDefaultAsync(g => g.BusinessId == tenantId && g.CardNo == request.CardNo, ct);

        if (card == null)
        {
            return new RedeemGiftCardResponse(false, request.CardNo, 0m, 0m, "Gift card not found.");
        }

        if (card.Status != "Active")
        {
            return new RedeemGiftCardResponse(false, request.CardNo, 0m, card.Balance, $"Gift card is {card.Status}.");
        }

        if (card.ExpiryDate.HasValue && card.ExpiryDate.Value < DateTime.UtcNow)
        {
            return new RedeemGiftCardResponse(false, request.CardNo, 0m, card.Balance, "Gift card has expired.");
        }

        if (request.AmountToRedeem <= 0)
        {
            return new RedeemGiftCardResponse(false, request.CardNo, 0m, card.Balance, "Invalid redemption amount.");
        }

        if (card.Balance < request.AmountToRedeem)
        {
            return new RedeemGiftCardResponse(false, request.CardNo, 0m, card.Balance, $"Insufficient balance. Available: Rs.{card.Balance:N2}");
        }

        card.Balance -= request.AmountToRedeem;
        if (card.Balance == 0)
        {
            card.Status = "Depleted";
        }

        var tx = new GiftCardTransaction
        {
            BusinessId = tenantId,
            GiftCardId = card.Id,
            POSOrderId = request.POSOrderId,
            Amount = request.AmountToRedeem,
            Type = "Redemption"
        };
        _context.GiftCardTransactions.Add(tx);

        await _context.SaveChangesAsync(ct);

        return new RedeemGiftCardResponse(
            true,
            card.CardNo,
            request.AmountToRedeem,
            card.Balance,
            "Gift card redeemed successfully."
        );
    }

    public async Task<ValidateCouponResponse> ValidateCouponAsync(ValidateCouponRequest request, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        var coupon = await _context.Coupons
            .Include(c => c.Promotion)
                .ThenInclude(p => p!.Rules)
            .FirstOrDefaultAsync(c => c.BusinessId == tenantId && c.Code == request.Code, ct);

        if (coupon == null)
        {
            return new ValidateCouponResponse(false, request.Code, 0m, "Coupon code not found.", null, null);
        }

        if (coupon.Status != "Active")
        {
            return new ValidateCouponResponse(false, request.Code, 0m, $"Coupon is {coupon.Status}.", coupon.PromotionId, coupon.Id);
        }

        if (coupon.ExpiryDate < DateTime.UtcNow)
        {
            return new ValidateCouponResponse(false, request.Code, 0m, "Coupon has expired.", coupon.PromotionId, coupon.Id);
        }

        if (coupon.UsedCount >= coupon.UsageLimit)
        {
            return new ValidateCouponResponse(false, request.Code, 0m, "Coupon usage limit reached.", coupon.PromotionId, coupon.Id);
        }

        var promo = coupon.Promotion;
        if (promo == null || promo.Status != "Active")
        {
            return new ValidateCouponResponse(false, request.Code, 0m, "Attached promotion is inactive.", null, coupon.Id);
        }

        var rule = promo.Rules.FirstOrDefault();
        if (rule == null)
        {
            return new ValidateCouponResponse(false, request.Code, 0m, "No promotion rules found.", promo.Id, coupon.Id);
        }

        if (request.OrderTotal < rule.MinAmount)
        {
            return new ValidateCouponResponse(false, request.Code, 0m, $"Minimum order amount of Rs.{rule.MinAmount:N2} required.", promo.Id, coupon.Id);
        }

        decimal discount = 0m;
        if (promo.Type == PromotionType.PercentageOff)
        {
            discount = Math.Round(request.OrderTotal * (rule.DiscountValue / 100m), 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            // Flat amount discount
            discount = Math.Min(rule.DiscountValue, request.OrderTotal);
        }

        return new ValidateCouponResponse(
            true,
            coupon.Code,
            discount,
            $"Valid coupon: {promo.Name}",
            promo.Id,
            coupon.Id
        );
    }
}

using SmartPOS.Application.Features.Promotions.DTOs;

namespace SmartPOS.Application.Features.Promotions;

public interface IPromotionGiftCardService
{
    // Gift Card Operations
    Task<GiftCardDto> IssueGiftCardAsync(IssueGiftCardRequest request, CancellationToken ct = default);
    Task<GiftCardDto?> GetGiftCardBalanceAsync(string cardNo, CancellationToken ct = default);
    Task<RedeemGiftCardResponse> RedeemGiftCardAsync(RedeemGiftCardRequest request, CancellationToken ct = default);

    // Coupon & Promotion Operations
    Task<ValidateCouponResponse> ValidateCouponAsync(ValidateCouponRequest request, CancellationToken ct = default);
}

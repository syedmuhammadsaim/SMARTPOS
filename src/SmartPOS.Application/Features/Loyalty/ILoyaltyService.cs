using SmartPOS.Application.Features.Loyalty.DTOs;
using SmartPOS.Domain.Entities.PromotionsAndLoyalty;

namespace SmartPOS.Application.Features.Loyalty;

public interface ILoyaltyService
{
    Task<CustomerLoyaltyProfileDto> GetCustomerLoyaltyProfileAsync(Guid customerId, CancellationToken ct = default);
    Task<decimal> EarnPointsForOrderAsync(EarnPointsRequestDto request, CancellationToken ct = default);
    Task<LoyaltyRedemptionResultDto> RedeemPointsAsync(RedeemPointsRequestDto request, CancellationToken ct = default);
    Task<List<MembershipTier>> GetMembershipTiersAsync(CancellationToken ct = default);
}

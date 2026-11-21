namespace SmartPOS.Application.Features.Loyalty.DTOs;

public class CustomerLoyaltyProfileDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string TierName { get; set; } = "Bronze";
    public decimal PointsBalance { get; set; }
    public decimal LifetimePoints { get; set; }
    public decimal EarnMultiplier { get; set; } = 1.0m;
    public decimal DiscountPercent { get; set; }
    public string? NextTierName { get; set; }
    public decimal PointsNeededForNextTier { get; set; }
    public decimal EquivalentDiscountRupees { get; set; }
}

public class EarnPointsRequestDto
{
    public Guid CustomerId { get; set; }
    public Guid POSOrderId { get; set; }
    public decimal AmountPaid { get; set; }
}

public class RedeemPointsRequestDto
{
    public Guid CustomerId { get; set; }
    public decimal PointsToRedeem { get; set; }
    public Guid? POSOrderId { get; set; }
}

public class LoyaltyRedemptionResultDto
{
    public bool Success { get; set; }
    public decimal RedeemedPoints { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RemainingPoints { get; set; }
    public string Message { get; set; } = string.Empty;
}

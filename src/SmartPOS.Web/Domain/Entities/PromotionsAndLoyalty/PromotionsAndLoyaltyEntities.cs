using SmartPOS.Domain.Common;
using SmartPOS.Domain.Enums;
using SmartPOS.Domain.Entities.Customers;

namespace SmartPOS.Domain.Entities.PromotionsAndLoyalty;

public class Promotion : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public PromotionType Type { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? DaysOfWeek { get; set; } // "1,2,3,4,5"
    public TimeSpan? StartTime { get; set; } // For happy hour
    public TimeSpan? EndTime { get; set; }
    public Guid? BranchScopeId { get; set; }
    public int Priority { get; set; } = 1;
    public bool IsStackable { get; set; }
    public string Status { get; set; } = "Active";

    public ICollection<PromotionRule> Rules { get; set; } = new List<PromotionRule>();
}

public class PromotionRule : BaseTenantEntity
{
    public Guid PromotionId { get; set; }
    public string AppliesTo { get; set; } = "All"; // Product, Category, Brand, All
    public Guid? TargetId { get; set; }
    public decimal MinQty { get; set; } = 1m;
    public decimal MinAmount { get; set; }
    public decimal DiscountValue { get; set; } // Percent or fixed amount
    public Guid? FreeProductId { get; set; }
    public decimal FreeQty { get; set; }

    public Promotion? Promotion { get; set; }
}

public class Coupon : BaseTenantEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid PromotionId { get; set; }
    public int UsageLimit { get; set; } = 100;
    public int UsedCount { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = "Active";

    public Promotion? Promotion { get; set; }
}

public class GiftCard : BaseTenantEntity
{
    public string CardNo { get; set; } = string.Empty;
    public decimal InitialAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public Guid? CustomerId { get; set; }
    public string Status { get; set; } = "Active";

    public Customer? Customer { get; set; }
    public ICollection<GiftCardTransaction> Transactions { get; set; } = new List<GiftCardTransaction>();
}

public class GiftCardTransaction : BaseTenantEntity
{
    public Guid GiftCardId { get; set; }
    public Guid? POSOrderId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = "Redemption"; // Issue, Redemption, Topup, Refund

    public GiftCard? GiftCard { get; set; }
}

public class MembershipTier : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty; // Bronze, Silver, Gold, Platinum
    public decimal MinPoints { get; set; }
    public decimal EarnMultiplier { get; set; } = 1.0m;
    public decimal DiscountPercent { get; set; }
}

public class LoyaltyAccount : BaseTenantEntity
{
    public Guid CustomerId { get; set; }
    public Guid? TierId { get; set; }
    public decimal PointsBalance { get; set; }
    public decimal LifetimePoints { get; set; }

    public Customer? Customer { get; set; }
    public MembershipTier? Tier { get; set; }
    public ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
}

public class LoyaltyTransaction : BaseTenantEntity
{
    public Guid LoyaltyAccountId { get; set; }
    public Guid? POSOrderId { get; set; }
    public string Type { get; set; } = "Earned"; // Earned, Redeemed, Expired, Reversed, ManualAdjustment
    public decimal Points { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Reason { get; set; }
    public Guid? ApprovedBy { get; set; }

    public LoyaltyAccount? LoyaltyAccount { get; set; }
}

public class LoyaltyRule : BaseTenantEntity
{
    public decimal EarnPerAmount { get; set; } = 100m; // 1 point per Rs. 100
    public decimal PointValue { get; set; } = 1m;     // 1 point = Rs. 1
    public decimal MinRedeemPoints { get; set; } = 50m;
    public int ExpiryDays { get; set; } = 365;
}

public class Reward : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal PointsRequired { get; set; }
    public string RewardType { get; set; } = "Discount"; // Discount, FreeProduct, Voucher
    public decimal Value { get; set; }
    public string Status { get; set; } = "Active";
}

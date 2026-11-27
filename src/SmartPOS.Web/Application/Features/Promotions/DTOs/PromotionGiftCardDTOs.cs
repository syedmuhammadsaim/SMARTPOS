using SmartPOS.Domain.Enums;

namespace SmartPOS.Application.Features.Promotions.DTOs;

public record IssueGiftCardRequest(
    string CardNo,
    decimal Amount,
    Guid? CustomerId,
    DateTime? ExpiryDate
);

public record GiftCardDto(
    Guid Id,
    string CardNo,
    decimal Balance,
    decimal InitialAmount,
    DateTime IssuedDate,
    DateTime? ExpiryDate,
    string Status,
    string? CustomerName
);

public record RedeemGiftCardRequest(
    string CardNo,
    decimal AmountToRedeem,
    Guid POSOrderId
);

public record RedeemGiftCardResponse(
    bool Success,
    string CardNo,
    decimal RedeemedAmount,
    decimal RemainingBalance,
    string Message
);

public record ValidateCouponRequest(
    string Code,
    decimal OrderTotal,
    Guid? BranchId,
    Guid? CustomerId
);

public record ValidateCouponResponse(
    bool IsValid,
    string Code,
    decimal DiscountAmount,
    string? Reason,
    Guid? PromotionId,
    Guid? CouponId
);

public record CreateCouponRequest(
    string Code,
    Guid PromotionId,
    int UsageLimit,
    DateTime ExpiryDate
);

using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Loyalty;
using SmartPOS.Application.Features.Loyalty.DTOs;
using SmartPOS.Domain.Entities.PromotionsAndLoyalty;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class LoyaltyService : ILoyaltyService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public LoyaltyService(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<CustomerLoyaltyProfileDto> GetCustomerLoyaltyProfileAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { customerId }, ct);
        if (customer == null)
            throw new KeyNotFoundException($"Customer {customerId} not found.");

        var account = await _context.LoyaltyAccounts
            .Include(a => a.Tier)
            .FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);

        var tiers = await _context.MembershipTiers
            .OrderBy(t => t.MinPoints)
            .ToListAsync(ct);

        var tier = account?.Tier;
        if (tier == null && tiers.Count > 0)
        {
            tier = tiers[0];
        }

        var lifetimePoints = account?.LifetimePoints ?? 0m;
        var currentPoints = account?.PointsBalance ?? 0m;

        // Find next tier
        var nextTier = tiers.FirstOrDefault(t => t.MinPoints > lifetimePoints);
        var pointsNeeded = nextTier != null ? Math.Max(0m, nextTier.MinPoints - lifetimePoints) : 0m;

        var rule = await _context.LoyaltyRules.FirstOrDefaultAsync(ct) ?? new LoyaltyRule();

        return new CustomerLoyaltyProfileDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            TierName = tier?.Name ?? "Standard",
            PointsBalance = currentPoints,
            LifetimePoints = lifetimePoints,
            EarnMultiplier = tier?.EarnMultiplier ?? 1.0m,
            DiscountPercent = tier?.DiscountPercent ?? 0m,
            NextTierName = nextTier?.Name,
            PointsNeededForNextTier = pointsNeeded,
            EquivalentDiscountRupees = currentPoints * rule.PointValue
        };
    }

    public async Task<decimal> EarnPointsForOrderAsync(EarnPointsRequestDto request, CancellationToken ct = default)
    {
        if (request.AmountPaid <= 0) return 0m;

        var rule = await _context.LoyaltyRules.FirstOrDefaultAsync(ct) ?? new LoyaltyRule
        {
            EarnPerAmount = 100m,
            PointValue = 1m,
            MinRedeemPoints = 50m
        };

        var account = await _context.LoyaltyAccounts
            .Include(a => a.Tier)
            .FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, ct);

        var tiers = await _context.MembershipTiers.OrderBy(t => t.MinPoints).ToListAsync(ct);

        if (account == null)
        {
            account = new LoyaltyAccount
            {
                BusinessId = _tenantProvider.CurrentBusinessId ?? Guid.Empty,
                CustomerId = request.CustomerId,
                PointsBalance = 0m,
                LifetimePoints = 0m,
                TierId = tiers.FirstOrDefault()?.Id
            };
            _context.LoyaltyAccounts.Add(account);
        }

        var multiplier = account.Tier?.EarnMultiplier ?? 1.0m;
        var basePoints = Math.Floor(request.AmountPaid / (rule.EarnPerAmount > 0 ? rule.EarnPerAmount : 100m));
        var pointsEarned = Math.Round(basePoints * multiplier, 2);

        if (pointsEarned <= 0) return 0m;

        account.PointsBalance += pointsEarned;
        account.LifetimePoints += pointsEarned;

        // Auto-upgrade tier progression based on LifetimePoints
        var eligibleTier = tiers
            .Where(t => account.LifetimePoints >= t.MinPoints)
            .OrderByDescending(t => t.MinPoints)
            .FirstOrDefault();

        if (eligibleTier != null && account.TierId != eligibleTier.Id)
        {
            account.TierId = eligibleTier.Id;
            account.Tier = eligibleTier;
        }

        var transaction = new LoyaltyTransaction
        {
            BusinessId = _tenantProvider.CurrentBusinessId ?? Guid.Empty,
            LoyaltyAccountId = account.Id,
            POSOrderId = request.POSOrderId,
            Type = "Earned",
            Points = pointsEarned,
            ExpiryDate = DateTime.UtcNow.AddDays(rule.ExpiryDays > 0 ? rule.ExpiryDays : 365),
            Reason = $"Points earned on Order {request.POSOrderId}"
        };

        _context.LoyaltyTransactions.Add(transaction);
        await _context.SaveChangesAsync(ct);

        return pointsEarned;
    }

    public async Task<LoyaltyRedemptionResultDto> RedeemPointsAsync(RedeemPointsRequestDto request, CancellationToken ct = default)
    {
        if (request.PointsToRedeem <= 0)
        {
            return new LoyaltyRedemptionResultDto
            {
                Success = false,
                Message = "Points to redeem must be greater than zero."
            };
        }

        var rule = await _context.LoyaltyRules.FirstOrDefaultAsync(ct) ?? new LoyaltyRule
        {
            EarnPerAmount = 100m,
            PointValue = 1m,
            MinRedeemPoints = 50m
        };

        if (request.PointsToRedeem < rule.MinRedeemPoints)
        {
            return new LoyaltyRedemptionResultDto
            {
                Success = false,
                Message = $"Minimum {rule.MinRedeemPoints} points required for redemption."
            };
        }

        var account = await _context.LoyaltyAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, ct);

        if (account == null || account.PointsBalance < request.PointsToRedeem)
        {
            return new LoyaltyRedemptionResultDto
            {
                Success = false,
                Message = $"Insufficient loyalty points. Balance is {account?.PointsBalance ?? 0}."
            };
        }

        var discount = request.PointsToRedeem * rule.PointValue;
        account.PointsBalance -= request.PointsToRedeem;

        var transaction = new LoyaltyTransaction
        {
            BusinessId = _tenantProvider.CurrentBusinessId ?? Guid.Empty,
            LoyaltyAccountId = account.Id,
            POSOrderId = request.POSOrderId,
            Type = "Redeemed",
            Points = -request.PointsToRedeem,
            Reason = $"Points redeemed for discount of Rs. {discount:N2}"
        };

        _context.LoyaltyTransactions.Add(transaction);
        await _context.SaveChangesAsync(ct);

        return new LoyaltyRedemptionResultDto
        {
            Success = true,
            RedeemedPoints = request.PointsToRedeem,
            DiscountAmount = discount,
            RemainingPoints = account.PointsBalance,
            Message = $"Successfully redeemed {request.PointsToRedeem} points for Rs. {discount:N2} discount."
        };
    }

    public async Task<List<MembershipTier>> GetMembershipTiersAsync(CancellationToken ct = default)
    {
        return await _context.MembershipTiers
            .AsNoTracking()
            .OrderBy(t => t.MinPoints)
            .ToListAsync(ct);
    }
}

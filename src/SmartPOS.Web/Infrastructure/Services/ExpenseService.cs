using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Expenses;
using SmartPOS.Application.Features.Expenses.DTOs;
using SmartPOS.Application.Features.Sequences.Interfaces;
using SmartPOS.Domain.Entities.Finance;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class ExpenseService : IExpenseService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDocumentSequenceService _sequenceService;

    public ExpenseService(
        SmartPOSDbContext context,
        ITenantProvider tenantProvider,
        IDocumentSequenceService sequenceService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _sequenceService = sequenceService;
    }

    private Guid GetTenantId() => _tenantProvider.CurrentBusinessId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        var category = await _context.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.BusinessId == tenantId, ct);

        var categoryName = category?.Name ?? "General Expense";

        var expenseNo = await _sequenceService.GetNextNumberAsync(
            tenantId,
            request.BranchId,
            Guid.Empty,
            "Expense",
            "EXP"
        );

        var expense = new Expense
        {
            BusinessId = tenantId,
            ExpenseNo = expenseNo,
            BranchId = request.BranchId,
            SessionId = request.SessionId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Payee = request.Payee,
            Description = request.Description,
            Status = "Approved",
            ApprovedBy = request.ApprovedBy
        };

        _context.Expenses.Add(expense);

        // If paid with cash directly from till, record cash drawer transaction to reconcile X/Z report
        if (request.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase) && request.SessionId.HasValue)
        {
            var cashTx = new CashTransaction
            {
                BusinessId = tenantId,
                BranchId = request.BranchId,
                SessionId = request.SessionId,
                Type = "Expense",
                Amount = request.Amount,
                Reason = $"Till Expense: {expenseNo} - {request.Payee}",
                Reference = expenseNo,
                Category = categoryName,
                UserId = request.ApprovedBy ?? Guid.Empty
            };
            _context.CashTransactions.Add(cashTx);
        }

        await _context.SaveChangesAsync(ct);

        return new ExpenseDto(
            expense.Id,
            expense.ExpenseNo,
            expense.Amount,
            expense.PaymentMethod,
            expense.Payee,
            expense.Description,
            expense.Status,
            categoryName,
            expense.CreatedAtUtc
        );
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync(Guid? branchId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        var query = _context.Expenses
            .Include(e => e.Category)
            .Where(e => e.BusinessId == tenantId);

        if (branchId.HasValue)
            query = query.Where(e => e.BranchId == branchId.Value);

        if (fromDate.HasValue)
            query = query.Where(e => e.CreatedAtUtc >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.CreatedAtUtc <= toDate.Value);

        return await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => new ExpenseDto(
                e.Id,
                e.ExpenseNo,
                e.Amount,
                e.PaymentMethod,
                e.Payee,
                e.Description,
                e.Status,
                e.Category != null ? e.Category.Name : "General",
                e.CreatedAtUtc
            ))
            .ToListAsync(ct);
    }

    public async Task<List<ExpenseCategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        return await _context.ExpenseCategories
            .Where(c => c.BusinessId == tenantId)
            .OrderBy(c => c.Name)
            .Select(c => new ExpenseCategoryDto(c.Id, c.Name, c.Description))
            .ToListAsync(ct);
    }

    public async Task<ExpenseCategoryDto> CreateCategoryAsync(string name, string? description, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        var category = new ExpenseCategory
        {
            BusinessId = tenantId,
            Name = name,
            Description = description
        };

        _context.ExpenseCategories.Add(category);
        await _context.SaveChangesAsync(ct);

        return new ExpenseCategoryDto(category.Id, category.Name, category.Description);
    }

    public async Task<CashTransactionDto> RecordCashFloatAsync(CreateCashFloatRequest request, Guid userId, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        var cashTx = new CashTransaction
        {
            BusinessId = tenantId,
            BranchId = request.BranchId,
            SessionId = request.SessionId,
            Type = request.Type,
            Amount = request.Amount,
            Reason = request.Reason,
            Reference = request.Reference,
            UserId = userId
        };

        _context.CashTransactions.Add(cashTx);
        await _context.SaveChangesAsync(ct);

        return new CashTransactionDto(
            cashTx.Id,
            cashTx.Type,
            cashTx.Amount,
            cashTx.Reason,
            cashTx.Reference,
            cashTx.CreatedAtUtc
        );
    }

    public async Task<List<CashTransactionDto>> GetSessionCashTransactionsAsync(Guid sessionId, CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        return await _context.CashTransactions
            .Where(t => t.BusinessId == tenantId && t.SessionId == sessionId)
            .OrderBy(t => t.CreatedAtUtc)
            .Select(t => new CashTransactionDto(
                t.Id,
                t.Type,
                t.Amount,
                t.Reason,
                t.Reference,
                t.CreatedAtUtc
            ))
            .ToListAsync(ct);
    }
}

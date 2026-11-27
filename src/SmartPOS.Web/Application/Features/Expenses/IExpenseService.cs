using SmartPOS.Application.Features.Expenses.DTOs;

namespace SmartPOS.Application.Features.Expenses;

public interface IExpenseService
{
    Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default);
    Task<List<ExpenseDto>> GetExpensesAsync(Guid? branchId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
    Task<List<ExpenseCategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<ExpenseCategoryDto> CreateCategoryAsync(string name, string? description, CancellationToken ct = default);

    // Cash Float / Cash Drawer Till movements
    Task<CashTransactionDto> RecordCashFloatAsync(CreateCashFloatRequest request, Guid userId, CancellationToken ct = default);
    Task<List<CashTransactionDto>> GetSessionCashTransactionsAsync(Guid sessionId, CancellationToken ct = default);
}

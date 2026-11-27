namespace SmartPOS.Application.Features.Expenses.DTOs;

public record CreateExpenseRequest(
    Guid BranchId,
    Guid? SessionId,
    Guid CategoryId,
    decimal Amount,
    string PaymentMethod, // "Cash", "BankTransfer"
    string Payee,
    string? Description,
    Guid? ApprovedBy
);

public record ExpenseDto(
    Guid Id,
    string ExpenseNo,
    decimal Amount,
    string PaymentMethod,
    string Payee,
    string? Description,
    string Status,
    string CategoryName,
    DateTime CreatedAtUtc
);

public record CreateCashFloatRequest(
    Guid BranchId,
    Guid? SessionId,
    decimal Amount,
    string Type, // "TillFloat", "CashIn", "CashOut", "BankDeposit"
    string Reason,
    string? Reference
);

public record CashTransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    string Reason,
    string? Reference,
    DateTime CreatedAtUtc
);

public record ExpenseCategoryDto(
    Guid Id,
    string Name,
    string? Description
);

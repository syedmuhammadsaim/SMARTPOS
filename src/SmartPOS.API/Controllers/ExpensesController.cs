using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Expenses;
using SmartPOS.Application.Features.Expenses.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        var expense = await _expenseService.CreateExpenseAsync(request, ct);
        return Ok(expense);
    }

    [HttpGet]
    public async Task<IActionResult> GetExpenses([FromQuery] Guid? branchId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
    {
        var list = await _expenseService.GetExpensesAsync(branchId, fromDate, toDate, ct);
        return Ok(list);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await _expenseService.GetCategoriesAsync(ct);
        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var cat = await _expenseService.CreateCategoryAsync(request.Name, request.Description, ct);
        return Ok(cat);
    }

    [HttpPost("cash-float")]
    public async Task<IActionResult> RecordCashFloat([FromBody] CreateCashFloatRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var tx = await _expenseService.RecordCashFloatAsync(request, userId, ct);
        return Ok(tx);
    }

    [HttpGet("sessions/{sessionId}/cash-transactions")]
    public async Task<IActionResult> GetSessionCashTransactions(Guid sessionId, CancellationToken ct)
    {
        var transactions = await _expenseService.GetSessionCashTransactionsAsync(sessionId, ct);
        return Ok(transactions);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public record CreateCategoryRequest(string Name, string? Description);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Customers;
using SmartPOS.Application.Features.Customers.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        var customers = await _customerService.GetAllCustomersAsync(search, ct);
        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken ct)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id, ct);
        if (customer == null)
            return NotFound(new { message = $"Customer {id} not found." });

        return Ok(customer);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var id = await _customerService.CreateCustomerAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpGet("{id:guid}/ledger")]
    public async Task<ActionResult<List<CustomerLedgerEntryDto>>> GetLedger(
        Guid id,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        var entries = await _customerService.GetCustomerLedgerAsync(id, fromDate, toDate, ct);
        return Ok(entries);
    }

    [HttpPost("{id:guid}/payments")]
    [Authorize]
    public async Task<ActionResult> RecordPayment(Guid id, [FromBody] CustomerPaymentRequest request, CancellationToken ct)
    {
        if (id != request.CustomerId)
            return BadRequest("Customer ID mismatch.");

        var user = User.Identity?.Name ?? "Cashier";
        var receiptNo = await _customerService.RecordCustomerPaymentAsync(request, user, ct);
        return Ok(new { receiptNumber = receiptNo, message = "Customer payment recorded successfully." });
    }

    [HttpGet("ageing-report")]
    [Authorize]
    public async Task<ActionResult<List<CustomerAgeingReportDto>>> GetAgeingReport(CancellationToken ct)
    {
        var report = await _customerService.GetCustomerAgeingReportAsync(ct);
        return Ok(report);
    }
}

using SmartPOS.Application.Features.Customers.DTOs;

namespace SmartPOS.Application.Features.Customers;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllCustomersAsync(string? searchTerm = null, CancellationToken ct = default);
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<List<CustomerLedgerEntryDto>> GetCustomerLedgerAsync(Guid customerId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    Task<string> RecordCustomerPaymentAsync(CustomerPaymentRequest request, string recordedBy, CancellationToken ct = default);
    Task<List<CustomerAgeingReportDto>> GetCustomerAgeingReportAsync(CancellationToken ct = default);
}

using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Customers;
using SmartPOS.Application.Features.Customers.DTOs;
using SmartPOS.Application.Features.Sequences.Interfaces;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Domain.Entities.ShiftsAndSessions;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDocumentSequenceService _sequenceService;

    public CustomerService(SmartPOSDbContext context, ITenantProvider tenantProvider, IDocumentSequenceService sequenceService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _sequenceService = sequenceService;
    }

    public async Task<List<CustomerDto>> GetAllCustomersAsync(string? searchTerm = null, CancellationToken ct = default)
    {
        var query = _context.Customers
            .AsNoTracking()
            .Include(c => c.CustomerGroup)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) ||
                                     (c.Phone != null && c.Phone.Contains(term)) ||
                                     (c.Cnic != null && c.Cnic.Contains(term)));
        }

        var list = await query.ToListAsync(ct);

        return list.Select(c => new CustomerDto
        {
            Id = c.Id,
            CustomerCode = c.Id.ToString()[..8].ToUpper(),
            Name = c.Name,
            Phone = c.Phone,
            Email = c.Email,
            CNIC = c.Cnic,
            NTN = c.TaxRegistrationNo,
            CreditLimit = c.CreditLimit,
            CurrentBalance = c.CurrentBalance,
            CreditDaysLimit = 30,
            IsCreditAllowed = !c.IsCreditBlocked,
            CustomerGroupId = c.CustomerGroupId,
            CustomerGroupName = c.CustomerGroup != null ? c.CustomerGroup.Name : null,
            IsActive = c.Status == "Active"
        }).ToList();
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _context.Customers
            .AsNoTracking()
            .Include(c => c.CustomerGroup)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (c == null) return null;

        return new CustomerDto
        {
            Id = c.Id,
            CustomerCode = c.Id.ToString()[..8].ToUpper(),
            Name = c.Name,
            Phone = c.Phone,
            Email = c.Email,
            CNIC = c.Cnic,
            NTN = c.TaxRegistrationNo,
            CreditLimit = c.CreditLimit,
            CurrentBalance = c.CurrentBalance,
            CreditDaysLimit = 30,
            IsCreditAllowed = !c.IsCreditBlocked,
            CustomerGroupId = c.CustomerGroupId,
            CustomerGroupName = c.CustomerGroup != null ? c.CustomerGroup.Name : null,
            IsActive = c.Status == "Active"
        };
    }

    public async Task<Guid> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = new Customer
        {
            BusinessId = _tenantProvider.CurrentBusinessId ?? Guid.Empty,
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            Cnic = request.CNIC,
            TaxRegistrationNo = request.NTN,
            CreditLimit = request.CreditLimit,
            CurrentBalance = 0m,
            IsCreditBlocked = !request.IsCreditAllowed,
            CustomerGroupId = request.CustomerGroupId,
            Status = "Active"
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(ct);
        return customer.Id;
    }

    public async Task<List<CustomerLedgerEntryDto>> GetCustomerLedgerAsync(Guid customerId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var query = _context.CustomerLedgers
            .AsNoTracking()
            .Where(l => l.CustomerId == customerId);

        if (fromDate.HasValue)
            query = query.Where(l => l.Date >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.Date <= toDate.Value);

        var list = await query
            .OrderBy(l => l.Date)
            .ThenBy(l => l.CreatedAtUtc)
            .ToListAsync(ct);

        return list.Select(l => new CustomerLedgerEntryDto
        {
            Id = l.Id,
            Date = l.Date,
            TransactionType = l.Type,
            DocumentNumber = l.ReferenceId.HasValue ? l.ReferenceId.Value.ToString()[..8] : string.Empty,
            Description = l.Notes,
            Debit = l.Debit,
            Credit = l.Credit,
            Balance = l.RunningBalance
        }).ToList();
    }

    public async Task<string> RecordCustomerPaymentAsync(CustomerPaymentRequest request, string recordedBy, CancellationToken ct = default)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);
        if (customer == null)
            throw new InvalidOperationException($"Customer {request.CustomerId} not found.");

        var tenantId = _tenantProvider.CurrentBusinessId ?? Guid.Empty;
        var receiptNo = await _sequenceService.GetNextNumberAsync(tenantId, request.BranchId, null, "CPAY", "CPAY");

        // Customer ledger balance update
        var newBalance = customer.CurrentBalance - request.Amount;
        customer.CurrentBalance = newBalance;

        var payment = new CustomerPayment
        {
            BusinessId = tenantId,
            ReceiptNo = receiptNo,
            CustomerId = customer.Id,
            BranchId = request.BranchId ?? Guid.Empty,
            SessionId = request.CounterSessionId,
            Amount = request.Amount,
            PaymentMethodId = request.PaymentMethodId,
            ReceivedBy = Guid.Empty,
            ReceivedAtUtc = DateTime.UtcNow,
            Notes = request.Notes
        };
        _context.CustomerPayments.Add(payment);

        var ledgerEntry = new CustomerLedger
        {
            BusinessId = tenantId,
            CustomerId = customer.Id,
            Date = DateTime.UtcNow,
            Type = "Payment",
            ReferenceId = payment.Id,
            Debit = 0m,
            Credit = request.Amount,
            RunningBalance = newBalance,
            Notes = $"Payment receipt #{receiptNo}. {request.Notes}".Trim()
        };
        _context.CustomerLedgers.Add(ledgerEntry);

        // If received during an active cashier session, record cash movement as UdhaarReceived
        if (request.CounterSessionId.HasValue)
        {
            var movement = new SessionCashMovement
            {
                BusinessId = tenantId,
                SessionId = request.CounterSessionId.Value,
                Type = "UdhaarReceived",
                Amount = request.Amount,
                Reason = $"Customer payment for {customer.Name}",
                Reference = receiptNo
            };
            _context.SessionCashMovements.Add(movement);
        }

        await _context.SaveChangesAsync(ct);
        return receiptNo;
    }

    public async Task<List<CustomerAgeingReportDto>> GetCustomerAgeingReportAsync(CancellationToken ct = default)
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.CurrentBalance > 0)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var result = new List<CustomerAgeingReportDto>();

        foreach (var c in customers)
        {
            // Fetch uncredited sales or ledger debits
            var debits = await _context.CustomerLedgers
                .AsNoTracking()
                .Where(l => l.CustomerId == c.Id && l.Debit > 0)
                .OrderByDescending(l => l.Date)
                .ToListAsync(ct);

            decimal current_0_30 = 0m;
            decimal overdue_31_60 = 0m;
            decimal overdue_61_90 = 0m;
            decimal overdue_90Plus = 0m;

            var remainingBalanceToAllocate = c.CurrentBalance;

            foreach (var d in debits)
            {
                if (remainingBalanceToAllocate <= 0) break;

                var amountToBucket = Math.Min(remainingBalanceToAllocate, d.Debit);
                var days = (now - d.Date).TotalDays;

                if (days <= 30) current_0_30 += amountToBucket;
                else if (days <= 60) overdue_31_60 += amountToBucket;
                else if (days <= 90) overdue_61_90 += amountToBucket;
                else overdue_90Plus += amountToBucket;

                remainingBalanceToAllocate -= amountToBucket;
            }

            if (remainingBalanceToAllocate > 0)
            {
                overdue_90Plus += remainingBalanceToAllocate;
            }

            result.Add(new CustomerAgeingReportDto
            {
                CustomerId = c.Id,
                CustomerName = c.Name,
                Phone = c.Phone,
                TotalOutstanding = c.CurrentBalance,
                Current_0_30 = current_0_30,
                Overdue_31_60 = overdue_31_60,
                Overdue_61_90 = overdue_61_90,
                Overdue_90Plus = overdue_90Plus
            });
        }

        return result;
    }
}

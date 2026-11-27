using SmartPOS.Domain.Common;
using SmartPOS.Domain.Enums;
using SmartPOS.Domain.Entities.Organization;

namespace SmartPOS.Domain.Entities.ShiftsAndSessions;

public class Shift : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GracePeriodMinutes { get; set; } = 15;
    public bool IsOvernight { get; set; }
    public Guid? BranchId { get; set; }
}

public class EmployeeShift : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }
    public Guid ShiftId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public Employee? Employee { get; set; }
    public Shift? Shift { get; set; }
}

public class Attendance : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime BusinessDate { get; set; }
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public int LateMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string Status { get; set; } = "Present";
    public string? CorrectionReason { get; set; }
    public Guid? CorrectedBy { get; set; }

    public Employee? Employee { get; set; }
}

public class CounterSession : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CounterId { get; set; }
    public Guid ShiftId { get; set; }
    public DateTime BusinessDate { get; set; }
    public DateTime StartUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndUtc { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal Variance { get; set; }
    public CounterSessionStatus Status { get; set; } = CounterSessionStatus.Open;

    public Employee? Employee { get; set; }
    public Branch? Branch { get; set; }
    public Counter? Counter { get; set; }
    public Shift? Shift { get; set; }

    public ICollection<SessionCashMovement> CashMovements { get; set; } = new List<SessionCashMovement>();
    public ICollection<CounterSessionReport> Reports { get; set; } = new List<CounterSessionReport>();
}

public class SessionCashMovement : BaseTenantEntity
{
    public Guid SessionId { get; set; }
    public string Type { get; set; } = string.Empty; // CashDrop, SafeDeposit, Expense, PaidIn, PaidOut, UdhaarReceived
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Reference { get; set; }

    public CounterSession? Session { get; set; }
}

public class CounterSessionReport : BaseTenantEntity
{
    public Guid SessionId { get; set; }
    public SessionReportType Type { get; set; } // XReport, ZReport
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid GeneratedBy { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public int PrintCount { get; set; } = 1;

    public CounterSession? Session { get; set; }
}

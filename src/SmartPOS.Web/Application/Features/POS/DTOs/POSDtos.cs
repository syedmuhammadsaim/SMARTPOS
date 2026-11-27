using SmartPOS.Domain.Enums;

namespace SmartPOS.Application.Features.POS.DTOs;

public record SaleOrderItemDto(
    Guid ProductId,
    Guid? VariantId,
    Guid? BatchId,
    decimal Qty,
    decimal UnitPrice,
    decimal DiscountAmount);

public record SaleOrderPaymentDto(
    Guid PaymentMethodId,
    decimal Amount,
    decimal TenderedAmount,
    string? ReferenceNo = null,
    string? CardLastFour = null);

public record CreateSaleOrderDto(
    Guid ClientTransactionId,
    Guid BranchId,
    Guid CounterId,
    Guid SessionId,
    Guid? CustomerId,
    List<SaleOrderItemDto> Items,
    List<SaleOrderPaymentDto> Payments,
    string? CouponCode = null);

public record SaleOrderResponseDto(
    bool Success,
    Guid? OrderId,
    string? InvoiceNo,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal Total,
    decimal PaidAmount,
    decimal ChangeAmount,
    OrderStatus Status,
    string? FbrInvoiceNo,
    string? FbrQrCode,
    string? Message);

public record HoldBillDto(
    Guid BranchId,
    Guid CounterId,
    Guid? CustomerId,
    string? Note,
    List<SaleOrderItemDto> Items);

public record HeldBillSummaryDto(
    Guid HeldBillId,
    string HoldNo,
    DateTime CreatedAtUtc,
    string? Note,
    int ItemCount,
    decimal EstimatedTotal);

public record CounterSessionReportDto(
    Guid SessionId,
    SessionReportType ReportType,
    DateTime GeneratedAtUtc,
    decimal OpeningCash,
    decimal CashSales,
    decimal CashReceived,
    decimal CashRefunds,
    decimal CashExpenses,
    decimal CashPaidOut,
    decimal ExpectedCash,
    decimal ActualCash,
    decimal Variance,
    int TotalInvoices,
    decimal TotalRevenue,
    Dictionary<string, decimal> PaymentMethodBreakdown);

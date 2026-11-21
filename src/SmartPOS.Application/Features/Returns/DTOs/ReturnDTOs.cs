namespace SmartPOS.Application.Features.Returns.DTOs;

public record CreateSaleReturnRequest(
    Guid OriginalOrderId,
    Guid BranchId,
    Guid CounterId,
    Guid SessionId,
    string RefundMethod, // "Cash", "StoreCredit", "CustomerLedger"
    string Reason,
    Guid? ManagerOverrideId,
    List<ReturnItemRequest> Items
);

public record ReturnItemRequest(
    Guid ProductId,
    Guid? VariantId,
    Guid? BatchId,
    decimal Quantity,
    decimal UnitPrice,
    string Condition // "Restockable", "Damaged", "Expired"
);

public record SaleReturnResponse(
    bool Success,
    Guid ReturnId,
    string ReturnInvoiceNo,
    decimal RefundAmount,
    string RefundMethod,
    string Message
);

public record OrderReturnEligibilityDto(
    Guid OrderId,
    string InvoiceNo,
    DateTime OrderDate,
    decimal TotalAmount,
    List<OrderEligibleItemDto> Items
);

public record OrderEligibleItemDto(
    Guid ProductId,
    Guid? VariantId,
    Guid? BatchId,
    string ProductName,
    string? VariantName,
    decimal OriginalQty,
    decimal AlreadyReturnedQty,
    decimal EligibleQty,
    decimal UnitPrice
);

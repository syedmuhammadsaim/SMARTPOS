using SmartPOS.Domain.Common;
using SmartPOS.Domain.Enums;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.ShiftsAndSessions;

namespace SmartPOS.Domain.Entities.Sales;

public class PaymentMethod : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty; // Cash, Card, JazzCash, Easypaisa, BankTransfer, Udhaar
    public PaymentMethodType Type { get; set; }
    public bool RequiresReference { get; set; }
}

public class POSOrder : BaseTenantEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid CounterId { get; set; }
    public Guid SessionId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid CashierId { get; set; }
    public DateTime BusinessDate { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Completed;

    // Blueprint v2 additions
    public Guid ClientTransactionId { get; set; } // Unique client-generated GUID preventing double-billing on retry
    public string? FbrInvoiceNo { get; set; }
    public FbrInvoiceStatus FbrStatus { get; set; } = FbrInvoiceStatus.NotApplicable;
    public string? FbrQrCode { get; set; }

    public Branch? Branch { get; set; }
    public Counter? Counter { get; set; }
    public CounterSession? Session { get; set; }
    public Customer? Customer { get; set; }

    public ICollection<POSOrderItem> Items { get; set; } = new List<POSOrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<POSOrderDiscount> Discounts { get; set; } = new List<POSOrderDiscount>();
}

public class POSOrderItem : BaseTenantEntity
{
    public Guid POSOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }

    // Snapshots: Name, Price, Tax must NEVER change once billed
    public string NameSnapshot { get; set; } = string.Empty;
    public decimal PriceSnapshot { get; set; }
    public decimal TaxSnapshot { get; set; }
    public decimal Qty { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }

    public POSOrder? POSOrder { get; set; }
    public Product? Product { get; set; }
}

public class POSOrderDiscount : BaseTenantEntity
{
    public Guid POSOrderId { get; set; }
    public Guid? POSOrderItemId { get; set; }
    public Guid? PromotionId { get; set; }
    public Guid? CouponId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;

    public POSOrder? POSOrder { get; set; }
}

public class Payment : BaseTenantEntity
{
    public Guid POSOrderId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public decimal TenderedAmount { get; set; }
    public string? ReferenceNo { get; set; } // Card Auth code, JazzCash Transaction ID
    public string? CardLastFour { get; set; }

    public POSOrder? POSOrder { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
}

public class HeldBill : BaseTenantEntity
{
    public string HoldNo { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid CounterId { get; set; }
    public Guid CashierId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "Held"; // Held, Resumed, Expired, Voided

    public ICollection<HeldBillItem> Items { get; set; } = new List<HeldBillItem>();
}

public class HeldBillItem : BaseTenantEntity
{
    public Guid HeldBillId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }

    public HeldBill? HeldBill { get; set; }
    public Product? Product { get; set; }
}

public class SaleReturn : BaseTenantEntity
{
    public string ReturnInvoiceNo { get; set; } = string.Empty;
    public Guid OriginalOrderId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CounterId { get; set; }
    public Guid SessionId { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal RefundAmount { get; set; }
    public string RefundMethod { get; set; } = "Cash"; // Cash, StoreCredit, CustomerLedger
    public string Reason { get; set; } = string.Empty;
    public Guid? ManagerOverrideId { get; set; }

    public POSOrder? OriginalOrder { get; set; }
    public ICollection<SaleReturnItem> Items { get; set; } = new List<SaleReturnItem>();
}

public class SaleReturnItem : BaseTenantEntity
{
    public Guid SaleReturnId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal ReturnedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }
    public string Condition { get; set; } = "Restockable"; // Restockable, Damaged, Expired

    public SaleReturn? SaleReturn { get; set; }
    public Product? Product { get; set; }
}

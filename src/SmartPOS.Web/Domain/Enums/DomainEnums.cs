namespace SmartPOS.Domain.Enums;

public enum ProductType
{
    Standard = 1,
    Weighted = 2,
    VariablePrice = 3,
    Service = 4,
    BundleCombo = 5
}

public enum TaxRoundingMode
{
    RoundToNearestRupee = 1,
    HalfUp = 2,
    HalfEven = 3
}

public enum SessionReportType
{
    XReport = 1,
    ZReport = 2
}

public enum CounterSessionStatus
{
    Open = 1,
    Closed = 2,
    Reconciled = 3
}

public enum StockTransactionType
{
    OpeningStock = 1,
    GoodsReceipt = 2,
    POSSale = 3,
    SaleReturn = 4,
    StockTransferOut = 5,
    StockTransferIn = 6,
    DamageAdjustment = 7,
    ExpiryWriteOff = 8,
    PhysicalCountCorrection = 9,
    PurchaseReturn = 10
}

public enum PaymentMethodType
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    MobileWallet = 4,
    StoreCredit = 5,
    SplitPayment = 6,
    CustomerCredit = 7
}

public enum PromotionType
{
    PercentageOff = 1,
    FixedOff = 2,
    BuyXGetY = 3,
    BundlePrice = 4,
    HappyHour = 5,
    CategoryOffer = 6,
    BrandOffer = 7,
    MinimumPurchase = 8
}

public enum OrderStatus
{
    Completed = 1,
    Held = 2,
    Cancelled = 3,
    Refunded = 4,
    PartiallyRefunded = 5
}

public enum FbrInvoiceStatus
{
    NotApplicable = 0,
    Pending = 1,
    Submitted = 2,
    Failed = 3,
    OfflineQueued = 4
}

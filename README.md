# SMARTPOS ENTERPRISE (Version 2)
### Complete Retail Management. One Powerful System.

Centralized retail POS and business management for single or multi-branch, multi-warehouse, and multi-counter retail setups. Tailored for supermarkets, departmental stores, pharmacies (batch/expiry), apparel/footwear (variants), wholesalers, and multi-branch retailers.

---

## 🏗️ Architecture & Technology Stack

| Tier | Component | Technology | Description |
| :--- | :--- | :--- | :--- |
| **Desktop POS** | `SmartPOS.Desktop` | WPF + C# (.NET 10) | Cashier billing, barcode scanning, thermal printing (80mm/58mm), Urdu RTL |
| **Web Admin** | `SmartPOS.Web` | ASP.NET Core MVC (.NET 10) | Multi-branch admin, reports, inventory, ledgers, approvals |
| **Backend API** | `SmartPOS.API` | ASP.NET Core Web API (.NET 10) | Versioned REST API (`/api/v1`), Idempotency, SignalR real-time hubs |
| **Core Domain** | `SmartPOS.Domain` | .NET 10 Class Library | Clean domain models, business calculation engines, money formulas |
| **Application** | `SmartPOS.Application` | .NET 10 Class Library | DTOs, service interfaces, validation, business orchestration |
| **Data & Infra** | `SmartPOS.Infrastructure` | EF Core 10 + Identity | SQL Server, multi-tenant global filters (`BusinessId`), FBR/SMS/WhatsApp |
| **Tests** | `SmartPOS.Tests` | xUnit (.NET 10) | Unit & integration tests for money-critical logic (Cash, Tax, FEFO) |

> ⚠️ **Architecture Constraint**: The WPF desktop application communicates with the backend exclusively via the Web API. Direct database connections to SQL Server from the desktop client are strictly disallowed.

---

## 🧮 Money-Critical Domain Engines

- **Cash Reconciliation (X/Z Reports)**:
  $$\text{Expected Cash} = \text{Opening} + \text{Cash Sales} + \text{Cash Received (Udhaar)} - \text{Cash Refunds} - \text{Cash Expenses} - \text{Cash Paid Out}$$
- **Tax Calculation Engine**: Inclusive/exclusive GST rates, tax exemption, rounding modes.
- **FEFO Batch Allocation**: Earliest Expiry First Out allocation preventing the sale of expired items.
- **Append-Only Ledgers**: `CustomerLedger`, `SupplierLedger`, and `StockTransactions` are immutable audit trails.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server 2022+ or SQL Server Express / LocalDB

### Build & Test
```powershell
# Restore & build solution
dotnet build SmartPOS.slnx

# Run automated tests
dotnet test tests/SmartPOS.Tests/SmartPOS.Tests.csproj
```

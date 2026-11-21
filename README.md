<p align="center">
  <img src="assets/logo.png" alt="SMARTPOS" width="100%" style="max-width: 850px; border-radius: 16px; box-shadow: 0 10px 30px rgba(0,0,0,0.5);" />
</p>

<h1 align="center">SMARTPOS</h1>

<p align="center">
  <strong>Next-Generation Enterprise Retail Management &amp; High-Volume POS Platform</strong><br>
  <em>Engineered for Supermarkets, Department Stores, Wholesalers, Pharmacies &amp; Multi-Branch Outlets</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Version-1.0.0-6366F1?style=for-the-badge&logo=semver&logoColor=white" alt="Version 1.0.0" />
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C# 13" />
  <img src="https://img.shields.io/badge/Architecture-Clean%20%2F%20DDD-06B6D4?style=for-the-badge" alt="Clean Architecture" />
  <img src="https://img.shields.io/badge/Tests-28%20Passed-10B981?style=for-the-badge&logo=checkmarx&logoColor=white" alt="28 Tests Passed" />
  <img src="https://img.shields.io/badge/FBR-Tier--1%20Compliant-F59E0B?style=for-the-badge" alt="FBR Compliant" />
</p>

---

## 📌 Executive Summary

**SMARTPOS** (Version 1.0) is a centralized, high-volume retail operating system designed for single and multi-branch retail enterprises. It brings together sub-second desktop billing (WPF), a real-time web administration portal (ASP.NET Core MVC), versioned micro-APIs (ASP.NET Core Web API), and an immutable financial calculation engine.

Designed from the ground up to prevent revenue leakage, enforce perpetual inventory control with batch FEFO tracking, and ensure strict compliance with fiscal requirements (including FBR digital invoice verification with cryptographic QR codes).

---

## 🏛️ System Architecture

SMARTPOS follows **Clean Architecture & Domain-Driven Design (DDD)** principles with strict layer isolation:

```
┌────────────────────────────────────────────────────────┐
│                   PRESENTATION TIER                    │
├────────────────────────────┬───────────────────────────┤
│    SmartPOS.Desktop (WPF)  │   SmartPOS.Web (ASP.NET)  │
│   Cashier Billing Client   │   Executive Web Portal    │
└─────────────┬──────────────┴─────────────┬─────────────┘
              │ (Strictly HTTP/REST)       │ (Internal Services)
              ▼                            │
┌──────────────────────────────────────────▼─────────────┐
│                    API GATEWAY TIER                    │
│     SmartPOS.API (ASP.NET Core 10 Web API / REST)      │
│      JWT Auth • Idempotency • SignalR Hubs • Swagger   │
└─────────────────────────────┬──────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────┐
│                   APPLICATION TIER                     │
│         SmartPOS.Application (.NET 10 Library)         │
│     Use Cases • DTOs • Interfaces • Business Rules     │
└─────────────────────────────┬──────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────┐
│                      DOMAIN TIER                       │
│           SmartPOS.Domain (.NET 10 Library)            │
│  Entities • Value Objects • Money Formulas • FEFO Core │
└─────────────────────────────┬──────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────┐
│                 INFRASTRUCTURE TIER                    │
│       SmartPOS.Infrastructure (EF Core 10 + Identity)   │
│   SQL Server • Multi-Tenant Filter • Hardware Drivers  │
└────────────────────────────────────────────────────────┘
```

> ⚠️ **Architecture Constraint**: The WPF desktop application communicates with the backend exclusively via the REST Web API. Direct database connections to SQL Server from cashier endpoints are strictly disallowed to guarantee tenant isolation, security, and idempotency.

---

## 🧩 Solution Projects

| Project | Target | Description |
| :--- | :--- | :--- |
| [`src/SmartPOS.Domain`](file:///c:/Users/Saim/Desktop/SMARTPOS/src/SmartPOS.Domain) | .NET 10 | Enterprise domain entities, money calculation formulas, and value objects. |
| [`src/SmartPOS.Application`](file:///c:/Users/Saim/Desktop/SMARTPOS/src/SmartPOS.Application) | .NET 10 | CQRS service contracts, DTOs, interfaces, and business workflows. |
| [`src/SmartPOS.Infrastructure`](file:///c:/Users/Saim/Desktop/SMARTPOS/src/SmartPOS.Infrastructure) | .NET 10 | EF Core database mappings, SQL Server persistence, multi-tenant filters, and background services. |
| [`src/SmartPOS.API`](file:///c:/Users/Saim/Desktop/SMARTPOS/src/SmartPOS.API) | .NET 10 Web API | High-throughput REST API with JWT security, role-based access, and SignalR live hubs. |
| [`src/SmartPOS.Desktop`](file:///c:/Users/Saim/Desktop/SMARTPOS/src/SmartPOS.Desktop) | .NET 10 WPF | Lightning-fast cashier workstation with barcode scanners, weigh scales, and thermal receipt printing. |
| [`src/SmartPOS.Web`](file:///c:/Users/Saim/Desktop/SMARTPOS/src/SmartPOS.Web) | .NET 10 MVC | Executive command center with dark-canvas glassmorphic UI and Chart.js analytics. |
| [`tests/SmartPOS.Tests`](file:///c:/Users/Saim/Desktop/SMARTPOS/tests/SmartPOS.Tests) | .NET 10 xUnit | Automated test suite verifying financial ledgers, tax calculation, and licensing logic. |

---

## ⚡ Core Modules & Capabilities

### 1. High-Speed Cashier Checkout (Desktop POS)
- **Sub-Second Barcode Scanning:** GS1, EAN-13, Code-128, and custom in-store label scanning.
- **Weigh-Scale Price Embedded Barcodes:** Auto-extraction of weight/price from prefix prefixes (`20`, `21`, `99`).
- **Cart Management:** Hold & recall cart sessions, price overrides with manager approval PIN, and item/bill discounts.
- **Multi-Tender Payments:** Split payments across Cash, 1Link/PayPak cards, digital wallets, and customer credit (Udhaar).
- **Shift Reconciliation (X & Z Reports):** Blind cash counts, over/short tracking, float tracking, and end-of-shift audits.
- **Refunds & Sale Returns Dialog:** Lookup historical invoices, tag return condition (`Restockable`, `Damaged`, `Expired`), and issue cash/credit refunds.

### 2. Multi-Tier Inventory & FEFO Batch Engine
- **First-Expiry-First-Out (FEFO):** Automatic allocation of earliest expiring batch numbers to prevent expired stock loss.
- **Stock Movements & Transfers:** Inter-branch and warehouse stock transfers with dispatch, transit, and receipt confirmation.
- **Stock Adjustments & Write-Offs:** Shrinkage, damage, expiry write-offs, and cycle audit reconciliations.
- **Automated Smart Stock Alerts:** Minimum reorder safety alerts, 30/60/90 days expiry horizons, and dead-stock capital tracking.

### 3. Customer Udhaar & Credit Risk Management
- **Customer Ledgers:** Append-only credit ledgers tracking every sale, partial payment, and balance adjustment.
- **Ageing Schedule Analysis:** Multi-bucket overdue analysis (0-30 days, 31-60 days, 61-90 days, 90+ days overdue).
- **Credit Limit & Policy Controls:** Configurable credit amount ceilings and credit day limits with automated freeze on defaults.

### 4. Fiscal Compliance & Billing
- **FBR Tier-1 POS Integration:** Cryptographic digital invoice validation, NTN/STRN handling, and verification QR codes.
- **Bilingual Thermal Receipts:** ESC/POS printing for 80mm and 58mm thermal printers with English and Urdu Nastaliq headings and greetings.

### 5. SaaS Licensing & Hardware Security
- **Machine Fingerprint Binding:** Cryptographic device node keys tied to motherboards, CPU IDs, and MAC addresses.
- **Terminal & Branch Quotas:** Enforce licensed counter limits per retail outlet.
- **Offline Grace Period:** 14-day offline fail-safe operational window for network outage resiliency.

### 6. Executive Reports & Financial Engine
- **Profit & Loss (P&L):** Real-time COGS (Cost of Goods Sold), operating expenses, gross margins, and net profitability.
- **Cashier Leaderboard:** Track cashier checkout speed, transaction volumes, discounts granted, and average basket sizes.
- **Visual Analytics:** Modern Chart.js 7-day revenue trend curves and departmental category share breakdowns.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server 2022+ or SQL Server Express / LocalDB
- Visual Studio 2026 / Visual Studio Code / JetBrains Rider

### 1. Clone & Build
```powershell
# Clone the repository
git clone https://github.com/syedmuhammadsaim/SMARTPOS.git
cd SMARTPOS

# Build entire solution
dotnet build SmartPOS.slnx
```

### 2. Run Automated Test Suite
```powershell
# Run all unit and integration tests
dotnet test tests/SmartPOS.Tests/SmartPOS.Tests.csproj
```
*Result: 28/28 tests passing (`100% Passed`)*

### 3. Launch Applications
```powershell
# Start the Backend REST API
dotnet run --project src/SmartPOS.API/SmartPOS.API.csproj

# Start the Executive Web Admin Portal
dotnet run --project src/SmartPOS.Web/SmartPOS.Web.csproj

# Start the Cashier Desktop Client
dotnet run --project src/SmartPOS.Desktop/SmartPOS.Desktop.csproj
```

---

## 🛡️ License
Proprietary & Enterprise Commercial License &bull; Copyright &copy; 2026 **SMARTPOS**. All Rights Reserved.

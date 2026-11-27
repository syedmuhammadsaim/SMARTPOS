<p align="center">
  <img src="assets/logo.png" alt="SMARTPOS ENTERPRISE" width="100%" style="max-width: 850px; border-radius: 16px; box-shadow: 0 10px 30px rgba(0,0,0,0.5);" />
</p>

<h1 align="center">SMARTPOS ENTERPRISE</h1>

<p align="center">
  <strong>Multi-Tenant SaaS Point of Sale &amp; Business Management Web Platform</strong><br>
  <em>"One Platform. Multiple Businesses. Complete Business Control."</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Version-2.0.0--Enterprise-6366F1?style=for-the-badge&logo=semver&logoColor=white" alt="Version 2.0.0" />
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C# 13" />
  <img src="https://img.shields.io/badge/Architecture-Single%20Web%20Project-06B6D4?style=for-the-badge&logo=dotnet&logoColor=white" alt="Single Web Project" />
  <img src="https://img.shields.io/badge/Multi--Tenant-SaaS%20Logical%20Isolation-10B981?style=for-the-badge&logo=securityscorecard&logoColor=white" alt="Multi-Tenant" />
  <img src="https://img.shields.io/badge/Database-SQL%20Server%20Enterprise-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/Build-0%20Errors%20Passing-10B981?style=for-the-badge&logo=checkmarx&logoColor=white" alt="Build Passing" />
</p>

---

## 📌 Executive Summary

**SMARTPOS ENTERPRISE** is a commercial-grade, multi-tenant SaaS Point of Sale (POS) and business management web application. Multiple independent companies can register, select subscription plans, operate multiple branches and checkout counters, manage inventory and warehouses, process sales, and track financial performance through a single centralized web application.

The entire solution is consolidated into a **single self-contained ASP.NET Core 10 Web application project folder** (`src/SmartPOS.Web`), eliminating redundant desktop applications, extra class library layers, and fragmented dependencies.

---

## 📂 Project Directory Structure

The entire platform is organized inside **[`src/SmartPOS.Web`](file:///c:/Users/Saim/Desktop/SMARTPOS%20ENTERPRISE/src/SmartPOS.Web)**:

```
SmartPOS ENTERPRISE/
├── .git/
├── assets/
│   └── logo.png
├── src/
│   └── SmartPOS.Web/                      <-- All-in-one standalone Web Application
│       ├── Application/                   <-- DTOs & Service Contracts
│       │   ├── Common/                    <-- Interfaces (ITenantProvider)
│       │   └── Features/                  <-- POS, Alerts, Auth, Inventory, Reports DTOs
│       ├── Controllers/                   <-- MVC Action Controllers
│       │   ├── AccountController.cs       <-- Login, Register, SaaS Onboarding
│       │   ├── BranchesController.cs      <-- Multi-Branch Management
│       │   ├── CategoriesController.cs    <-- Categories, Brands, Units
│       │   ├── CountersController.cs      <-- POS Cashier Counters
│       │   ├── CustomersController.cs     <-- Customer Profiles & Udhaar Ledgers
│       │   ├── DashboardController.cs     <-- Executive Metrics & Chart.js Visuals
│       │   ├── ExpensesController.cs      <-- Till & Branch Operational Expenses
│       │   ├── HomeController.cs          <-- Marketing Landing Pages
│       │   ├── InventoryController.cs     <-- Stock Balances, Adjustments, Transfers
│       │   ├── POSController.cs           <-- Fast Web POS Billing & Checkout
│       │   ├── ProductsController.cs      <-- Products & Code128 Barcode Printing
│       │   ├── PurchasesController.cs     <-- POs, Goods Receipt, Supplier Ledgers
│       │   ├── SalesController.cs         <-- Invoices History & Refunds
│       │   ├── ShiftsController.cs        <-- Till Opening, Blind Close, X/Z Reports
│       │   ├── SubscriptionController.cs  <-- Tenant Plan Upgrades & Billing
│       │   ├── SuperAdminController.cs    <-- Platform Super Admin Dashboard
│       │   └── SuppliersController.cs     <-- Supplier Directory & Ledger Accounts
│       ├── Domain/                        <-- Core Business Entities & Enums
│       │   ├── Common/                    <-- BaseEntity, BaseTenantEntity, ITenantScoped
│       │   ├── Entities/                  <-- Catalog, Sales, Inventory, SaaS, Finance
│       │   └── Enums/                     <-- OrderStatus, TransactionTypes, Enums
│       ├── Hubs/                          <-- SignalR Real-Time Notification Hubs
│       ├── Infrastructure/                <-- Data Persistence & Security
│       │   ├── Data/                      <-- SmartPOSDbContext & DbInitializer
│       │   ├── Identity/                  <-- Identity User & Claims Principal Factory
│       │   ├── Migrations/                <-- EF Core Code First Migrations
│       │   └── Services/                  <-- POS, Sequences, TenantProvider, Auth
│       ├── Models/                        <-- ViewModels for Auth & Dashboard
│       ├── Views/                         <-- Razor Views (Dark Canvas Bootstrap 5 Theme)
│       │   ├── Account/                   <-- Login, Register, AccessDenied
│       │   ├── Dashboard/                 <-- Executive Visual Analytics
│       │   ├── Home/                      <-- Landing, Features, Pricing, About, FAQ
│       │   ├── POS/                       <-- Web POS Register & 80mm Thermal Receipt
│       │   ├── Products/                  <-- Catalog & Barcode Label Sheets
│       │   ├── Shifts/                    <-- Till Shifts & X/Z Printed Reports
│       │   ├── SuperAdmin/                <-- Platform-wide Companies & Plans
│       │   └── Shared/                    <-- _Layout.cshtml (Sidebar navigation)
│       ├── wwwroot/                       <-- Static CSS, JS, Vendor Libraries
│       ├── appsettings.json               <-- Connection strings & App Configuration
│       ├── Program.cs                     <-- Web Application Host & Middleware Pipeline
│       └── SmartPOS.Web.csproj            <-- Clean self-contained project file
├── README.md                              <-- Project Documentation
└── SmartPOS.slnx                          <-- Visual Studio Solution File
```

---

## 🏛️ Multi-Tenant Architecture & Security

```
┌────────────────────────────────────────────────────────────────────────┐
│                        WEB PRESENTATION LAYER                          │
│     ASP.NET Core 10 MVC • Bootstrap 5 Dark Canvas • Chart.js UI        │
├───────────────────────────────────┬────────────────────────────────────┤
│   Marketing & SaaS Onboarding     │    Cashier Browser-Based POS       │
│   Company Admin Dashboard         │    Super Admin Platform Console    │
└─────────────────┬─────────────────┴──────────────────┬─────────────────┘
                  │                                    │
                  ▼                                    ▼
┌───────────────────────────────────┐┌───────────────────────────────────┐
│     AUTHENTICATION & CLAIMS       ││     TENANT CONTEXT RESOLUTION     │
│   ASP.NET Core Identity (Roles)   ││   ITenantProvider (Claims-bound)  │
│   BusinessId & FullName Claims    ││   Zero-Trust Header/Query Guards  │
└─────────────────┬─────────────────┘└─────────────────┬─────────────────┘
                  │                                    │
                  ▼                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        DATA ACCESS LAYER (ORM)                         │
│        Entity Framework Core 10 • SmartPOSDbContext                    │
│    Global Query Filters (e.BusinessId == _tenantProvider.BusinessId)   │
│    Restrict Delete Behaviors • Transactional Multi-Entity Saves        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                         SQL SERVER DATABASE                            │
│    Single Shared Database • Strict Logical TenantId Partitioning       │
│    InitialSaaSEnterpriseDb Migration • Complete Relational Schemas     │
└────────────────────────────────────────────────────────────────────────┘
```

### 🔒 Enterprise SaaS Tenant Isolation
- **Claims-Verified Tenant Context**: The tenant ID is resolved exclusively from the authenticated user's verified claims (`BusinessId`). Query strings or form posts cannot manipulate tenant boundaries.
- **Global EF Core Query Filters**: Every tenant-owned entity implements `ITenantScoped`. All database queries automatically filter by `BusinessId`.
- **SuperAdmin Governed Bypass**: Platform super-administrators have an audited, controlled capability to inspect tenants for support and billing administration without compromising logical boundary integrity.

---

## ⚡ Core Web Modules

| Module | Web Route | Description |
| :--- | :--- | :--- |
| **Public Landing** | `/` | Marketing hero, key features showcase, pricing tiers, and contact form. |
| **SaaS Onboarding** | `/Account/Register` | Atomic self-service tenant registration with default branch, counter, and warehouse creation. |
| **Super Admin Console** | `/SuperAdmin` | Platform-wide company management, subscription tiers, platform settings, and audit logs. |
| **Executive Dashboard** | `/Dashboard` | Real-time financial metrics (revenue, COGS, gross margin, expenses, net profit) with Chart.js trends. |
| **Fast Web POS** | `/POS` | Sub-second barcode scanner listener, dynamic cart, split payments, hold/resume sales, and 80mm thermal receipts. |
| **Product Catalog** | `/Products` | Product inventory, SKUs, brand/category associations, and printable Code128 barcode sheets. |
| **Stock & Warehouses** | `/Inventory` | Multi-warehouse stock balances, inter-warehouse transfers, and auditable shrinkage/damage adjustments. |
| **Till Shifts & Audits** | `/Shifts` | Opening float registration, blind cash counts, and printable mid-shift (X) & closing (Z) reports. |
| **Purchasing & POs** | `/Purchases` | Supplier purchase orders with automatic stock updating into warehouses upon receipt. |
| **Supplier Ledgers** | `/Suppliers` | Double-entry payables ledger tracking orders, payments, and running balances. |
| **Customer Udhaar** | `/Customers` | Customer credit limits and chronological receivables ledger. |
| **Expense Logs** | `/Expenses` | Daily operational and petty cash expense tracking per branch. |
| **Subscription Portal** | `/Subscription` | Active plan status, renewal management, branch quotas, and plan upgrades. |

---

## 🔐 Default Credentials (Pre-Seeded)

The database automatically seeds the following credentials upon first run:

| Role | Email | Password | Access Level |
| :--- | :--- | :--- | :--- |
| **SaaS Super Admin** | `superadmin@smartpos.com` | `SuperAdmin@123!` | Global Platform Command Center (`/SuperAdmin`) |
| **Company Admin** | `admin@apexmart.com` | `Admin@123!` | Apex Mart & Retail Operations (`/Dashboard`, `/POS`) |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server 2022+ or SQL Server Express / LocalDB (`localhost`)
- Visual Studio 2022 / Visual Studio Code / JetBrains Rider

### 1. Database Connection Configuration
The application connects to SQL Server using the connection string in `src/SmartPOS.Web/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=SmartPosEnterpriseDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

> **Note:** Database migrations and pre-seeded sample data are applied automatically when the application starts up!

### 2. Build & Launch Web Application
From the repository root:
```powershell
# Build the application
dotnet build

# Launch the Web Application
dotnet run --project src/SmartPOS.Web/SmartPOS.Web.csproj
```

Or change directory directly into the web project folder:
```powershell
cd src/SmartPOS.Web
dotnet run
```

### 3. Open in Browser
Once running, open your browser and navigate to:
- **`https://localhost:7196`** or **`http://localhost:5242`**

---

## 🛡️ License
Proprietary & Enterprise Commercial License &bull; Copyright &copy; 2026 **SMARTPOS ENTERPRISE**. All Rights Reserved.

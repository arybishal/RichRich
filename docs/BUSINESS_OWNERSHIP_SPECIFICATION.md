# RichRich: Business Ownership & Commercial Enterprise Specification

## 1. Executive Summary & Objective

This document specifies the commercial business ownership framework implemented in **RichRich Milestone 4**. The business ownership system enables players to transition from salaried employment into entrepreneurship, acquire commercial enterprises, manage facilities and staffing, upgrade operational capacity, and build an enterprise portfolio with real valuation tied to their personal balance sheet.

In accordance with core project rules:
- **No Tap-to-Earn:** Revenue is strictly generated through business operations, inventory sales, and client contract fulfillment.
- **Clean Architecture:** All business rules exist in pure C# domain entities (`Business`, `ServiceBay`, `StoreProduct`, `ClientProject`, `Employee`, `BusinessService`) with zero Unity engine dependencies.
- **Zero-Sum Ledger Attribution:** Capital investments, revenue payouts, parts expenses, COGS, and payroll flow through `FinancialService` and `FinancialLedger`.

---

## 2. Business Catalog & Acquisition Economics

| Enterprise ID | Name | Category | Base Acquisition Cost | Setup & Registration | Initial Operating Overhead | Initial Reputation | Initial Valuation |
|---|---|---|---|---|---|---|---|
| `auto_repair` | **Auto Repair Workshop** | Automotive | $5,499.00 | $500.00 | $35.00/day | 60/100 | $5,499.00 |
| `convenience_store` | **Corner Convenience Store** | Retail | $12,500.00 | $1,200.00 | $45.00/day | 60/100 | $12,500.00 |
| `digital_agency` | **Apex Digital Marketing Agency** | Digital Services | $25,000.00 | $2,500.00 | $85.00/day | 70/100 | $25,000.00 |

### 2.1 Acquisition Logic
1. **Preconditions:** Player liquid cash balance in `Wallet` must be $\ge (\text{BaseCost} + \text{SetupCost})$.
2. **Transaction Execution:**
   - Cash deducted: $\text{BaseCost} + \text{SetupCost}$ categorized as `TransactionCategory.AssetPurchase`.
   - `Business.Acquire()` marks ownership active, resets condition to 100%, and opens the facility.
   - `BalanceSheet.NonCashAssets` is credited with the initial asset valuation.
   - **Net Worth Invariance:** At the exact instant of purchase, Net Worth change is exactly **$0.00** (liquid cash converted to commercial asset value).

### 2.2 Facility Upgrades & Scaling
- **Upgrade Cost Formula:**
  $$\text{UpgradeCost} = \text{BaseCost} \times 1.25^{(\text{Level} - 1)}$$
- **Benefits:**
  - `auto_repair`: Unlocks additional physical service bays (Level 1 = 1 bay, Level 2 = 2 bays, etc.) and qualifies for complex mechanical work (diagnostics at Level 2, powertrain rebuilds at Level 3).
  - `convenience_store`: Increases customer foot traffic demand by +5 units/day per level.
  - `digital_agency`: Increases client contract eligibility and expands billable campaign tiering.
  - General: +5 reputation per upgrade level.

### 2.3 Valuation & Liquidation (Selling)
- **Asset Valuation Formula:**
  $$\text{Valuation} = \text{BaseCost} \times [1.0 + 0.5 \times (\text{Level} - 1)] \times \text{clamp}\left(\frac{\text{Reputation}}{100}, 0.25, 1.25\right) \times \text{clamp}\left(\frac{\text{Condition}}{100}, 0.5, 1.0\right)$$
- **Sale Execution:**
  - Converts entire `Valuation` into liquid cash credited to `Wallet` with `TransactionCategory.AssetSale`.
  - Removes enterprise from owned portfolio.
  - Automatically dismisses active employees and clears pending orders.
  - Reduces `BalanceSheet.NonCashAssets` accordingly.

---

## 3. Human Resources & Staffing Model

Commercial enterprises can recruit specialized staff from the candidate pool to automate operations, scale output, and free player bandwidth.

### 3.1 Employee Roster & Roles

| Role | Enterprise | Base Monthly Salary | Productivity | Operational Contribution |
|---|---|---|---|---|
| `ApprenticeMechanic` | Auto Repair | $1,800.00/mo | 1.0x | +180 repair minutes contributed daily per bay |
| `SeniorTechnician` | Auto Repair | $3,200.00/mo | 1.4x | +250 repair minutes contributed daily, reduces parts wastage |
| `StoreClerk` | Convenience Store | $1,600.00/mo | 1.0x | +8 customer transactions accommodated daily |
| `InventoryManager` | Convenience Store | $2,400.00/mo | 1.3x | +15 customer transactions accommodated daily |
| `JuniorMarketer` | Digital Agency | $2,600.00/mo | 1.1x | +8 billable contract hours contributed daily |
| `SeniorStrategist` | Digital Agency | $4,500.00/mo | 1.5x | +16 billable contract hours contributed daily |

### 3.2 Scheduled Monthly Payroll
- Scheduled on **Day 30** of each simulation calendar month.
- Iterates over all active hired employees in `OwnedBusinesses`.
- Deducts monthly salary via `FinancialService.TryRecordExpense(salary, TransactionCategory.BusinessExpense, "Employee Payroll", businessId)`.
- Records salary in `BusinessPAndL.PayrollExpenses`.
- If liquid cash is insufficient, loan liability or delinquency is triggered.

---

## 4. Operational Controls & Status

- **Open/Close Toggle:** The player can temporarily shutter an enterprise during cash flow crunches or renovations. When closed, daily operating overhead is minimized, but no customer demand or incoming contracts are processed.
- **Reputation (0–100):** Driven by on-time project deliveries, successful repair jobs, and adequate stock availability. Lower reputation diminishes customer volume and enterprise asset valuation.
- **Physical Condition (0–100):** Degrades slowly with operations; maintainable through workshop equipment servicing.

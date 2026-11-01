# RichRich: Implementation Gap & Capability Matrix

This document tracks all features and subsystems across the transition from the Phase 2 tap-to-earn prototype into the full Life & Wealth Simulation.

---

## 1. Subsystem Capability Ledger

| Subsystem | Feature / Requirement | Status | Verification Reference | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **Core Architecture** | Integer Minor-Unit Math | **Implemented** | `Currency.cs`, 4 tests | `long` minor units eliminate floating-point drift |
| **Core Architecture** | Versioned Atomic Persistence | **Implemented** | `SaveDataService.cs`, 7 tests | `.tmp` $\rightarrow$ `.json` + `.backup.json` with corruption recovery |
| **Core Architecture** | Assembly Definition Isolation | **Implemented** | `RichRich.Domain.asmdef` | Enforces `noEngineReferences: true` |
| **Simulation Time** | Central Deterministic Calendar Clock | **Implemented** | `SimulationClock.cs`, 2 tests | Converts real sec $\rightarrow$ game min; handles fast-forward / sleep |
| **Simulation Time** | Anti-Duplication Cycle Protection | **Implemented** | `SimulationClock.cs`, `LivingExpenseService.cs` | Recurring payments occur strictly once per month |
| **Simulation Time** | Offline Time Fast-Forward Sync | *Partially Implemented* | `LifeSimulationCoordinator.cs` | Basic minute restoration complete; background catch-up in progress |
| **Financial Engine** | Central Double-Entry Service | **Implemented** | `FinancialService.cs`, 3 tests | All balance mutations audited through central service |
| **Financial Engine** | Full Transaction Ledger | **Implemented** | `FinancialLedger.cs`, 2 tests | Auditable history with income/expense aggregations |
| **Financial Engine** | Balance Sheet & Net Worth | **Implemented** | `BalanceSheet.cs`, 2 tests | Computes Cash + Assets - Debt with negative net worth formatting |
| **Living Expenses** | Configurable Recurring Expenses | **Implemented** | `LivingExpenseService.cs`, 2 tests | Rent, Food, Transport, Utilities, Internet, Personal ($1,320/mo) |
| **Living Expenses** | Lifestyle Tiers (Frugal/Luxury) | *Designed* | `docs/LIFE_SIMULATION_DESIGN.md` | Configurable amounts implemented; preset UI selector planned |
| **Education & Skills** | Course Catalog & Tuition System | **Implemented** | `EducationService.cs`, 2 tests | Free digital course, IT Cert ($500), Sales Bootcamp ($250) |
| **Education & Skills** | Daily Study Hours Accumulation | **Implemented** | `EducationService.cs`, 1 test | 2 hours/day study progression to completion |
| **Education & Skills** | Skill & Qualification Awards | **Implemented** | `EducationService.cs`, 1 test | Awards credentials and increments Technical/Sales skills |
| **Education & Skills** | University Degree Programs | *Designed* | `docs/LIFE_SIMULATION_DESIGN.md` | Bachelor of CS, Diploma of Business planned for Milestone 3 |
| **Career & Jobs** | Entry-Level Job Listings | **Implemented** | `CareerService.cs`, 2 tests | Retail Assistant, Delivery Courier, Junior IT Helpdesk |
| **Career & Jobs** | Qualification & Skill Requirements | **Implemented** | `CareerService.cs`, 1 test | Validates prerequisites before allowing job hiring |
| **Career & Jobs** | Scheduled Monthly Salary Payday | **Implemented** | `CareerService.cs`, 1 test | Day 30 automatic paycheck deposit via FinancialService |
| **Career & Jobs** | Full 20+ Career Promotion Ladder | *Designed* | `docs/LIFE_SIMULATION_DESIGN.md` | Mid-level, Senior, Executive ladders planned for Milestone 3 |
| **Loans & Debt** | Responsible Loan Products ($5K / $10K)| **Implemented** | `LoanService.cs`, 3 tests | 12% APR, 12-month amortization term |
| **Loans & Debt** | Debt Underwriting & Eligibility Checks | **Implemented** | `LoanService.cs`, 1 test | Credit score $\ge 580$, DTI $\le 50\%$, max 2 active loans |
| **Loans & Debt** | Monthly Installment Split (P & I) | **Implemented** | `LoanModel.cs`, 2 tests | Splits into liability reduction and interest expense |
| **Loans & Debt** | Early Repayment in Full | **Implemented** | `LoanService.cs`, 1 test | Repays principal early with zero prepayment penalty |
| **Loans & Debt** | Delinquency & Missed Payment Penalties | **Implemented** | `LoanService.cs`, 1 test | Late fee ($25), credit score penalty (-25 pts) |
| **Financial Health** | 6-State Health Evaluator | **Implemented** | `FinancialHealthService.cs`, 2 tests | Healthy, Stressed, Crisis, Insolvent, Bankrupt, Restructuring |
| **Financial Health** | Recovery Action Recommendations | **Implemented** | `FinancialHealthService.cs`, 1 test | Dynamic advice generation based on diagnostic conditions |
| **UI Presentation** | Life Simulation HUD Controller | **Implemented** | `LifeSimulationHUDController.cs`, 1 test | Cash, Net Worth, Date, Cash Flow, Status, Speed controls |
| **UI Presentation** | Unity Life Simulation HUD View | **Implemented** | `MainHUDView.cs` | Binds controllers to Unity text labels and speed buttons |
| **UI Deactivation** | Tap-to-Earn Removal from Main HUD | **Implemented** | `MainHUDView.cs` | Large clicker button and tap multipliers removed |
| **Businesses** | Starter Business (Auto Repair Shop) | **Implemented** | `Business.cs`, `BusinessTaskController.cs` | Retained from Phase 2 for commercial business pathway |
| **Businesses** | Full 14 Business Tiers | *Designed* | `docs/IMPLEMENTATION_ROADMAP.md` | Store to Energy Company planned for Milestone 4 |
| **Investments** | Stock Market & Parody Equities | *Designed* | `docs/IMPLEMENTATION_ROADMAP.md` | Geometric Brownian motion engine planned for Milestone 5 |
| **Investments** | Real Estate Catalog & Mortgages | *Designed* | `docs/IMPLEMENTATION_ROADMAP.md` | Residential rental properties planned for Milestone 5 |
| **Device Validation** | Physical Android Device Testing | **Deferred / Blocked**| None (Environment constraint) | No physical device connected to ADB; Unity Editor not installed |

---

## 2. Status Definitions
- **Implemented:** Code is written, integrated, and verified by passing automated unit/integration tests in CI test harness.
- **Partially Implemented:** Functional core is written and operational, but certain advanced configurations or visual representations are slated for subsequent milestones.
- **Designed but not Implemented:** Full architectural and mathematical specifications are documented, ready for execution in upcoming sprint milestones.
- **Blocked / Deferred:** Development is paused due to missing external hardware (e.g. connected physical Android phone) or host environment software (e.g. Unity Editor binary).

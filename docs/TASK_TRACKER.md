# RichRich: Task Tracker & Sprint Progress

## Target Specifications
- **Product Name:** RichRich
- **Package ID:** `com.original.richrich`
- **Platform:** Android (Portrait, Min SDK 28, Target SDK 35, ARM64)
- **Engine Baseline:** Unity 6 LTS (URP, IL2CPP)
- **Architecture:** Clean Architecture (Domain Models, Services, Persistence, UI Presentation)
- **Persistence:** Local Dual-File Atomic JSON (`.json` + `.backup.json`)
- **Backend / Auth:** None (100% Offline Single-Player)

---

## Milestone: Transition to Life & Wealth Simulation

| Task ID | Task Description | Scope / Subsystems | Status | Deliverables |
| :--- | :--- | :--- | :--- | :--- |
| **LIFE-01** | Configurable Starting Conditions | Hard ($1K), Standard ($3K), Comfortable ($5K) | **Completed** | `GameDifficulty.cs`, `GameStartConfig.cs`, unit tests |
| **LIFE-02** | Simulation Clock & Calendar | Minute/Hour/Day/Month tick, 1 real sec = 1 game min, anti-duplication, sleep/fast-forward | **Completed** | `SimulationClock.cs`, `GameDateTime.cs`, unit tests |
| **LIFE-03** | Central Double-Entry Financial Service & Ledger | Balance sheet, transaction ledger, audited mutations, zero UI balance tampering | **Completed** | `FinancialService.cs`, `FinancialLedger.cs`, `BalanceSheet.cs`, unit tests |
| **LIFE-04** | Recurring Living Expenses | Rent, Food, Transport, Utilities, Internet, Personal ($1,320/mo), scheduled due date pay | **Completed** | `LivingExpenseService.cs`, `RecurringExpense.cs`, unit tests |
| **LIFE-05** | Education & Skill Progression | Courses catalog, tuition deduction, daily study hours, qualification & skill awards | **Completed** | `EducationService.cs`, `EducationModel.cs`, unit tests |
| **LIFE-06** | Career & Scheduled Salary | Retail Assistant, Courier, IT Helpdesk; requirements, hiring, Day 30 paychecks | **Completed** | `CareerService.cs`, `JobModel.cs`, unit tests |
| **LIFE-07** | Responsible Borrowing & Credit Engine | $5K / $10K loans, 12% APR amortization, DTI/credit score checks, late fee penalties | **Completed** | `LoanService.cs`, `LoanModel.cs`, unit tests |
| **LIFE-08** | 6-State Financial Health Evaluator | Healthy, Stressed, Crisis, Insolvent, Bankrupt, Restructuring; recovery recommendations | **Completed** | `FinancialHealthService.cs`, `FinancialHealthState.cs`, unit tests |
| **LIFE-09** | Deactivate Tap-to-Earn from Main HUD | Replaced tap button and multipliers with Cash, Net Worth, Date, Cash Flow, Status | **Completed** | `LifeSimulationHUDController.cs`, `MainHUDView.cs` |
| **LIFE-10** | Schema Version 2 Atomic Save | Persist simulation minutes, job, education, loans, credit score, difficulty | **Completed** | `SaveData.cs`, `LifeSimulationCoordinator.cs`, unit tests |
| **LIFE-11** | Comprehensive Automated Test Suite | 35 targeted test suites across all simulation subsystems (62 total tests) | **Completed** | `LifeSimulationTests.cs`, `FinancialIntegrationAuditTests.cs` (62/62 passing) |
| **LIFE-12** | Complete System Documentation | Life design doc, financial system spec, implementation gap matrix | **Completed** | `docs/LIFE_SIMULATION_DESIGN.md`, `docs/FINANCIAL_SYSTEM_SPECIFICATION.md`, `docs/IMPLEMENTATION_GAP_REPORT.md` |
| **LIFE-13** | Financial Simulation Integration Audit | Pre-Milestone 3 integration audit covering 12 scenarios & 8 architectural checks | **Completed** | `docs/FINANCIAL_INTEGRATION_AUDIT.md` (62/62 passing) |

---

## Milestone 3: Career, Education & Lifestyle Progression

| Task ID | Task Description | Scope / Subsystems | Status | Deliverables |
| :--- | :--- | :--- | :--- | :--- |
| **M3-01** | Interactive Job Board & Career Ladders | Multi-tier Retail (4 tiers), Tech (6 tiers), Business (5 tiers) ladders, vacancy browsing, hiring contracts, promotions, tenure | **Completed** | `JobModel.cs`, `CareerService.cs`, `JobBoardController.cs`, `JobBoardView.cs`, unit tests |
| **M3-02** | Higher Education & University Degrees | Diploma in Business Admin ($3,500), Bachelor of Computer Science ($12,000), tuition deduction, skill point rewards | **Completed** | `EducationModel.cs`, `EducationService.cs`, `EducationCatalogueController.cs`, `EducationCatalogueView.cs`, unit tests |
| **M3-03** | Study Paces & Concurrent Work-Study | Part-Time (2h/day) vs Full-Time (6h/day) study modes, simultaneous employment and degree study on daily clock tick | **Completed** | `EducationModel.cs`, `EducationService.cs`, unit tests |
| **M3-04** | Housing Accommodations System | 5 tiers (Shared Room $300, Studio $600, 1-Bed $1,100, Luxury $2,500, Penthouse $5,500), comfort ratings, move-in deposits, landlord verification | **Completed** | `HousingModel.cs`, `HousingService.cs`, `HousingCatalogueController.cs`, `HousingCatalogueView.cs`, unit tests |
| **M3-05** | Safe Rent & Anti-Duplication | Mid-month moves update rent for next calendar month without duplicate charges; Day 1 moves deduct new rate | **Completed** | `HousingService.cs`, `LivingExpenseService.cs`, unit tests |
| **M3-06** | Energy, Fatigue, Work Shifts & Rest | Energy pool (0-100), work shift & study energy consumption, fatigue states, rest action (+20), overnight housing recovery | **Completed** | `LifestyleEnergyModel.cs`, `LifestyleService.cs`, `LifestyleStatusController.cs`, `LifestyleStatusView.cs`, unit tests |
| **M3-07** | Schema Version 3 Atomic Persistence | Save/reload housing leases, deposits, energy, study pace, track experiences; backward-compatible migration from Schema 1 & 2 | **Completed** | `SaveData.cs`, `LifeSimulationCoordinator.cs`, unit tests |
| **M3-08** | Milestone 3 Automated Test Suite | 25 new NUnit tests covering all M3 acceptance criteria (86 total passing tests) | **Completed** | `Milestone3Tests.cs`, `PresentationControllerTests.cs` (86/86 passing) |
| **M3-09** | System Documentation & Milestone Report | Career, education, housing specifications, Milestone 3 report | **Completed** | `docs/CAREER_SYSTEM_SPECIFICATION.md`, `docs/EDUCATION_SYSTEM_SPECIFICATION.md`, `docs/HOUSING_LIFESTYLE_SPECIFICATION.md`, `MILESTONE_3_REPORT.md` |

---

## Detailed Acceptance Criteria Verification
- [x] Wallet currency operates strictly in integer minor-units (`long` cents).
- [x] Configurable starting cash: Hard ($1,000), Standard ($3,000), Comfortable ($5,000).
- [x] Repetitive tap-to-earn controls removed from main gameplay interface.
- [x] Simulation clock converts real time to game calendar (1 real sec = 1 game min).
- [x] Overdue scheduled events and recurring payments processed exactly once without duplicates.
- [x] Every financial transaction recorded with ID, amount, category, type, date, and description.
- [x] Loan disbursement increases Cash and Liabilities equally with zero net worth change.
- [x] Loan amortization splits monthly payment into principal reduction and interest expense.
- [x] Education progression accumulates daily study hours and awards credentials/skills upon completion.
- [x] Jobs validate qualifications and disburse salary on scheduled paydays.
- [x] Negative cash flow creates financial pressure without immediate bankruptcy.
- [x] Financial health evaluator classifies Healthy, Stressed, Crisis, Insolvent, and Bankrupt states.
- [x] Dual-file atomic JSON persistence preserves all simulation subsystems across app restarts.
- [x] Interactive Job Board allows browsing vacancies, filtering tracks, and checking qualifications.
- [x] Career ladders support promotion eligibility based on skills, tenure, and credentials.
- [x] Higher education degrees (Business Diploma, CS Degree) deduct tuition and award credentials.
- [x] Part-time and full-time study options operate concurrently with full-time employment.
- [x] Housing upgrades (Shared, Studio, 1-Bed, Luxury, Penthouse) deduct deposits and update rent safely.
- [x] Mid-month housing moves prevent duplicate rent deductions.
- [x] Player energy system balances work shifts, study hours, rest breaks, and housing sleep recovery.
- [x] Schema v3 persistence migrates older saves with safe defaults.
- [x] 86 automated tests passing cleanly in .NET 8 CLI harness (100% pass rate).
- [x] Financial Integration Audit completed and documented in `docs/FINANCIAL_INTEGRATION_AUDIT.md`.
- [x] Milestone 3 Completion Report documented in `MILESTONE_3_REPORT.md`.
- [ ] Validated on real Android hardware via ADB (Deferred: No physical ADB device connected).

# RichRich: Life & Wealth Simulation

**RichRich** is an original Android life, career, and wealth simulation game. The player begins with modest starting capital and must build long-term financial independence through education and skill acquisition, employment and career promotions, living expense budgeting, responsible borrowing, and business ownership.

Designed with clean-room engineering, 100% offline single-player persistence, and zero login barriers. Repetitive tap-to-earn clicker mechanics have been removed in favor of a deterministic simulation clock and meaningful economic decisions.

---

## Technical Specifications
- **Package ID:** `com.original.richrich`
- **Platform:** Android (Portrait, Min SDK 28, Target SDK 35, ARM64)
- **Engine Baseline:** Unity 6 LTS (URP, IL2CPP)
- **Architecture:** Clean Architecture with decoupled Domain, Application, and Presentation layers
- **Persistence:** Local Dual-File Atomic JSON (`richrich_save.json` + `richrich_save.backup.json`)
- **Backend / Authentication:** None (100% Offline Single-Player)

---

## Directory Structure

```
RichyRich/
├── MILESTONE_3_REPORT.md                  # Milestone 3 completion report and verification
├── docs/                                  # Architectural specifications and reports
│   ├── CAREER_SYSTEM_SPECIFICATION.md     # Multi-tier career ladders and promotion rules
│   ├── EDUCATION_SYSTEM_SPECIFICATION.md  # University degree programs and study pacing
│   ├── HOUSING_LIFESTYLE_SPECIFICATION.md # Housing tiers, deposits, energy and fatigue
│   ├── LIFE_SIMULATION_DESIGN.md          # Complete life & career progression design
│   ├── FINANCIAL_SYSTEM_SPECIFICATION.md  # Double-entry ledger, math, and balance sheet rules
│   ├── FINANCIAL_INTEGRATION_AUDIT.md     # Pre-Milestone 3 Integration Audit Report
│   ├── IMPLEMENTATION_GAP_REPORT.md       # Subsystem capability ledger and status matrix
│   ├── TECHNICAL_ARCHITECTURE.md          # Clean Architecture and domain service topology
│   ├── GAME_ECONOMY.md                    # Economic models, living expenses, and salaries
│   ├── TASK_TRACKER.md                    # Sprint task and acceptance criteria tracker
│   ├── PHASE_2_REPORT.md                  # Phase 2 vertical slice completion report
│   ├── GAMEPLAY_MECHANICS.md              # Gameplay loop and systems specification
│   ├── SCREEN_INVENTORY.md                # UI screen catalog and hierarchy
│   ├── UI_UX_SPECIFICATION.md             # Color tokens, layout, and typography
│   └── IMPLEMENTATION_ROADMAP.md          # Multi-milestone production roadmap
├── RichRich_Unity/                         # Unity 6 project root
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   └── MainGameplay.unity         # Core vertical slice scene
│   │   └── Scripts/
│   │       ├── Domain/                    # Pure C# domain models and services
│   │       │   ├── Models/                # Currency, BalanceSheet, GameDateTime, Jobs, Housing, etc.
│   │       │   ├── Persistence/           # SaveData (Schema v3), SaveDataService
│   │       │   ├── Services/              # Clock, FinancialService, Career, Education, Housing, Lifestyle
│   │       │   └── RichRich.Domain.asmdef # Enforces noEngineReferences: true
│   │       └── Presentation/              # Presentation layer
│   │           ├── Controllers/           # JobBoard, Education, Housing, Lifestyle, HUD, Business
│   │           ├── Views/                 # Unity MonoBehaviours (JobBoardView, HousingView, MainHUDView)
│   │           └── RichRich.Presentation.asmdef
│   ├── Packages/                          # Unity package manifest
│   └── ProjectSettings/                   # Unity project configuration and Android settings
└── tests/
    └── RichRich.Tests/                    # .NET 8 automated test suite (86 NUnit tests)
        ├── Milestone3Tests.cs             # 21 tests: Career ladders, Degrees, Housing, Energy, Schema v3
        ├── FinancialIntegrationAuditTests.cs # 20 tests: 12 Scenarios & 8 Stress Checks
        ├── LifeSimulationTests.cs         # 16 tests: Clock, Career, Expenses, Loans, Health, Saves
        ├── PresentationControllerTests.cs # 10 tests: View-model controllers and lifecycle tests
        ├── EconomyTests.cs                # 12 tests: Currency math, Wallet, and Business mechanics
        └── PersistenceTests.cs            # 7 tests: Dual-file atomic writes and corruption recovery
```

---

## Running Automated Tests

The domain models, business logic, persistence engine, and presentation controllers are completely decoupled from `UnityEngine.dll`, enabling instantaneous execution in headless CI/CD environments.

To execute the complete 86-test suite:

```powershell
# Set .NET SDK path if not in global PATH
$env:PATH = "$env:USERPROFILE\.dotnet;" + $env:PATH
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"

# Run all 86 NUnit unit and integration tests
dotnet test tests/RichRich.Tests/RichRich.Tests.csproj
```

**Test Coverage Highlights:**
1. **Starting Cash Selection:** Verifies Hard ($1,000), Standard ($3,000), and Comfortable ($5,000) presets.
2. **Simulation Clock Progression:** Verifies minute/hour/day conversions, configurable speed, and pause.
3. **Daily & Monthly Transitions:** Verifies sequential chronological firing across month boundaries.
4. **Recurring Living Expenses:** Verifies Day 1 deductions across 6 standard living expense categories.
5. **Financial Ledger Accuracy:** Audits double-entry records, income, expenses, and cash flow.
6. **Net Worth Calculation:** Accurately evaluates Cash + Assets - Liabilities, including negative net worth.
7. **Negative Cash Flow:** Verifies that operational deficits trigger warnings without instant bankruptcy.
8. **Loan Accounting:** Verifies loan disbursement increases Cash and Liabilities with 0 net worth impact.
9. **Loan Amortization:** Calculates exact 12% APR monthly installments, splitting principal and interest.
10. **Loan Repayments:** Verifies scheduled installments and early full payoff reduce liabilities.
11. **Anti-Duplication:** Guarantees recurring payments trigger strictly once per calendar cycle.
12. **Education Progression:** Verifies course enrollment, daily study hours, and credential awards.
13. **Employment & Salary:** Verifies qualification validation, hiring, and Day 30 salary deposits.
14. **Financial Health Evaluation:** Evaluates Healthy, Stressed, Crisis, Insolvent, and Bankrupt states.
15. **Save & Reload Consistency:** Verifies roundtrip persistence of clock, career, loans, and education.
16. **Life Simulation HUD Controller:** Validates presentation view-model formatting and clock speed controls.

---

## Unity Editor Setup & Build Instructions

1. Open **Unity Hub**.
2. Click **Add project from disk** and select `RichyRich/RichRich_Unity`.
3. Open with **Unity 6 LTS** (or compatible 6000.x editor version).
4. Select `File -> Open Scene -> Assets/Scenes/MainGameplay.unity`.
5. In `File -> Build Settings`:
   - Target Platform: **Android**
   - Texture Compression: **ASTC**
   - Run Device: Select connected Android device (via ADB)
6. Click **Build and Run** to produce and install `RichRich.apk`.

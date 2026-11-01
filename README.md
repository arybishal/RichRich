# RichRich: Life & Wealth Simulation

**RichRich** is an original Android life, career, and wealth simulation game. The player begins with modest starting capital and must build long-term financial independence through education and skill acquisition, employment and career promotions, living expense budgeting, responsible borrowing, and business ownership.

Designed with clean-room engineering, 100% offline single-player persistence, and zero login barriers. Repetitive tap-to-earn clicker mechanics have been removed in favor of a deterministic simulation clock and meaningful economic decisions.

---

## Technical Specifications
- **Package ID:** `com.bishal.richrich`
- **Platform:** Android (Portrait, Min SDK 28, Target SDK 35, ARM64)
- **Engine Baseline:** Unity 6 LTS (URP, IL2CPP)
- **Architecture:** Clean Architecture with decoupled Domain, Application, and Presentation layers
- **Persistence:** Local Dual-File Atomic JSON (`richrich_save.json` + `richrich_save.backup.json`)
- **Backend / Authentication:** None (100% Offline Single-Player)

---

## Directory Structure

```
RichyRich/
├── MILESTONE_4_REPORT.md                  # Milestone 4 completion report and verification
├── MILESTONE_3_REPORT.md                  # Milestone 3 completion report and verification
├── docs/                                  # Architectural specifications and reports
│   ├── BUSINESS_OWNERSHIP_SPECIFICATION.md  # Commercial enterprise ownership & staffing
│   ├── BUSINESS_ACCOUNTING_SPECIFICATION.md # P&L, COGS, operating overhead, commercial credit
│   ├── BUSINESS_SIMULATION_SPECIFICATION.md # Auto repair, convenience store, digital agency
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
│   │       │   ├── Models/                # Currency, BalanceSheet, GameDateTime, Business, Employee, etc.
│   │       │   ├── Persistence/           # SaveData (Schema v4), SaveDataService
│   │       │   ├── Services/              # Clock, FinancialService, BusinessService, Career, Loan, etc.
│   │       │   └── RichRich.Domain.asmdef # Enforces noEngineReferences: true
│   │       └── Presentation/              # Presentation layer
│   │           ├── Controllers/           # Business, JobBoard, Education, Housing, Lifestyle, HUD
│   │           ├── Views/                 # Unity MonoBehaviours (MarketplaceView, DashboardView, StoreView, etc.)
│   │           └── RichRich.Presentation.asmdef
│   ├── Packages/                          # Unity package manifest
│   └── ProjectSettings/                   # Unity project configuration and Android settings
└── tests/
    └── RichRich.Tests/                    # .NET 8 automated test suite (102 NUnit tests)
        ├── Milestone4Tests.cs             # 16 tests: Enterprise ownership, Auto Repair, Retail, Agency, Staff, Credit
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

To execute the complete 102-test suite:

```powershell
# Set .NET SDK path if not in global PATH
$env:PATH = "$env:USERPROFILE\.dotnet;" + $env:PATH
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"

# Run all 102 NUnit unit and integration tests
dotnet test tests/RichRich.Tests/RichRich.Tests.csproj
```

**Test Coverage Highlights:**
1. **Commercial Enterprise Ownership:** Browsing, acquiring, operating, and liquidating Auto Repair, Convenience Store, and Digital Agency.
2. **Auto Repair Workshop:** Service bay management, vehicle repair progression, parts cost deduction, and delivery payouts.
3. **Convenience Store Retail:** Wholesale ordering, price markup adjustment, daily customer traffic, COGS, gross margins, and stockout penalties.
4. **Digital Marketing Agency:** Client contracts, skill requirements, billable labor contribution, and deadline enforcement.
5. **Staffing & Payroll:** Employee recruitment across 6 industry roles, productivity scaling, and scheduled Day 30 monthly payroll.
6. **Commercial Credit & Loans:** Starter and expansion loans, underwriting on business revenue, and early debt payoffs.
7. **Consolidated P&L Accounting:** Isolated tracking of Gross Revenue, COGS, Operating Overhead, and Staff Payroll.
8. **Schema Version 4 Persistence:** Atomic JSON save/load roundtrips and backward-compatible migration from Schemas 1, 2, and 3.
9. **Interactive Job Board & Career Ladders:** Multi-tier career progression across Retail, Technology, and Business tracks.
10. **Higher Education:** Degree programs (Business Administration Diploma, Computer Science Bachelor) and study pacing.
11. **Housing Tiers & Leases:** Five accommodation tiers, deposit deductions, comfort ratings, and move-in verification.
12. **Energy & Fatigue System:** Daily work and study energy depletion, fatigue levels, and overnight recovery.
13. **Financial Ledger & Health:** Double-entry ledger records, solvency states (Healthy, Stressed, Crisis, Insolvent, Bankrupt).
14. **Starting Cash Presets:** Hard ($1,000), Standard ($3,000), and Comfortable ($5,000) presets.

---

## Android Build & Release Pipeline

RichRich includes an automated, headless build script located at `RichRich_Unity/Assets/Scripts/Editor/RichRichBuilder.cs`.

### Build Specifications
- **Package Identifier:** `com.bishal.richrich`
- **Application Version:** `0.1.0` (Bundle Version Code: `1`)
- **Target Platform:** Android (Portrait)
- **Scripting Backend:** IL2CPP (ARM64 `arm64-v8a`)
- **API Target:** Min SDK 28 (Android 9.0 Pie), Target SDK 35 (Android 15)

### One-Line Headless Release Build Command
```powershell
$unityExe = "C:\Program Files\Unity\Hub\Editor\6000.0.35f1\Editor\Unity.exe"
& $unityExe -batchmode -quit `
  -projectPath "D:\Webapp\RichyRich\RichRich_Unity" `
  -executeMethod RichRich.Editor.RichRichBuilder.BuildAndroidRelease `
  -buildOutput "D:\Webapp\RichyRich\Builds\Android\RichRich-v0.1.0-android-arm64.apk" `
  -logFile "D:\Webapp\RichyRich\Builds\Android\build.log"
```

### Connected Physical Device Deployment (via ADB)
```powershell
# 1. Install release APK
adb -s 3B15B800NRT00000 install -r "Builds/Android/RichRich-v0.1.0-android-arm64.apk"

# 2. Launch main activity
adb -s 3B15B800NRT00000 shell monkey -p com.bishal.richrich -c android.intent.category.LAUNCHER 1

# 3. Stream real-time Android logcat
adb -s 3B15B800NRT00000 logcat -s Unity:V RichRich:V AndroidRuntime:E
```

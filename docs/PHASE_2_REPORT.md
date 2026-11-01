# RichRich — Phase 2: Foundation & Core Vertical Slice Report

## Executive Summary

Phase 2 of **RichRich** (`com.original.richrich`) has been successfully executed. Building upon the extensive Phase 1 reconnaissance and clean-room specifications, the foundational gameplay vertical slice, clean domain architecture, atomic dual-file persistence engine, and presentation controllers have been implemented and rigorously verified with an automated NUnit test suite (26/26 tests passing).

The game is designed as a 100% offline-first, single-player wealth simulation with zero authentication barriers, zero third-party monetization traps, and zero stolen or decompiled code.

---

## 1. Architectural Overview & Component Structure

RichRich follows **Clean Architecture** principles with strict dependency boundaries:

```
┌────────────────────────────────────────────────────────┐
│                   Unity UI Views                       │
│ (GameBootstrapper, MainHUDView, BusinessTaskView, etc.)│
└───────────────────────────┬────────────────────────────┘
                            │ binds to
┌───────────────────────────▼────────────────────────────┐
│              Presentation Controllers                  │
│(MainHUDController, BusinessViewController, TaskCtrl)   │
└───────────────────────────┬────────────────────────────┘
                            │ coordinates
┌───────────────────────────▼────────────────────────────┐
│                Domain Application Services             │
│   (EconomyService, OfflineIncomeCalculator, Session)   │
└─────────────┬────────────────────────────┬─────────────┘
              │ uses                       │ persists
┌─────────────▼─────────────┐ ┌────────────▼─────────────┐
│       Domain Models       │ │    Persistence Layer     │
│ (Currency, Wallet,        │ │ (SaveData, SaveDataSvc,  │
│  Business, BusinessTask)  │ │  Atomic .tmp -> .backup) │
└───────────────────────────┘ └──────────────────────────┘
```

### Key Subsystems:
1. **Integer Minor-Unit Currency Engine (`Currency.cs` & `Wallet.cs`):**
   - Eliminates floating-point rounding errors and drift by storing balances in exact `long` minor units (cents).
   - Safe arithmetic operators (`+`, `-`, `*`, `/`) with underflow protection and standard financial abbreviations (`$4.00`, `$1,250.50`, `$1.25M`, `$4.88B`).
2. **Phase 1 Grounded Economy (`EconomyConfig.cs` & `EconomyService.cs`):**
   - Active tap income: Grounded at **$4.00 / tap** (400 cents).
   - Tap Multiplier Booster: **6x boost ($24.00 / tap)** for **30 seconds**.
   - Level 1 Milestone Target: **$1,000.00** (100,000 cents) scaling by 2.5x per tier.
   - Passive cashflow ticker crediting hourly yields smoothly per second.
3. **Playable Business Mechanic (`Business.cs`, `BusinessTask.cs`, `BusinessTaskController.cs`):**
   - Starter business: **Auto Repair Workshop** (Base purchase cost: $5,499.00; Base yield: $120.00/hr).
   - Upgrade scaling: Cost multiplier 1.25x per level; +25% income yield per level.
   - Interactive operational task: **Vehicle Diagnostic & Service** (5.0s duration, $150.00 reward).
   - Task state machine: `Idle` -> `Running` (interpolated timer & progress) -> `Completed` -> `ClaimReward` (guaranteed single payout; rejects duplicate claims).
4. **Crash-Resilient Dual-File Persistence (`SaveDataService.cs`):**
   - Writes new save to temporary file (`.tmp`).
   - Copies existing valid save to `.backup.json`.
   - Atomically replaces `.json` with `.tmp`.
   - If `.json` is ever corrupted or interrupted mid-write, the engine automatically falls back to `.backup.json`.
   - If both are corrupted, safely resets to initialized default state.
5. **Offline Earnings Engine (`OfflineIncomeCalculator.cs`):**
   - Calculates yield generated while application is closed.
   - Enforces a hard **3-hour cap** (10,800 seconds).
   - Anti-tamper clock detection: Rollbacks result in 0 income awarded.
6. **Presentation View-Model Controllers (`RichRich.Presentation.Controllers`):**
   - Decoupled from `UnityEngine.MonoBehaviour` so that entire UI state flows, button validations, formatting, and interactions can be tested in automated CI test harnesses.
7. **Unity Views (`RichRich.Presentation.Views`):**
   - `GameBootstrapper`: Scene lifecycle manager and auto-saver.
   - `MainHUDView`: Credit card balance, tap button, progress bar, booster banner.
   - `BusinessShopView`: Card layout, acquire and upgrade buttons.
   - `BusinessTaskView`: Operational task slider, countdown timer, and claim button.
   - `SettingsModalView`: Audio, haptics, version info, and save reset.
   - `OfflineEarningsModalView`: Welcome back popup with duration and earnings summary.
   - `FloatingTapText`: Organic floating number feedback when tapping.

---

## 2. Automated Test Verification Matrix

All tests run via `.NET 8` test harness (`RichRich.Tests.csproj`):

| Test Suite | Test Name | Target Behavior | Result |
| :--- | :--- | :--- | :--- |
| **EconomyTests** | `Currency_Arithmetic_PerformsExactIntegerMath` | Integer cents math with exact addition & subtraction | **PASSED** |
| | `Currency_Underflow_ThrowsInvalidOperationException` | Prevents negative currency values via subtraction | **PASSED** |
| | `Currency_NegativeCreation_ThrowsArgumentOutOfRangeException` | Rejects negative currency creation | **PASSED** |
| | `Currency_Formatting_FormatsExpectedAbbreviations` | Formats currency up to Billions ($1.25M, $4.88B) | **PASSED** |
| | `Wallet_TrySpend_RejectsInsufficientFunds` | Refuses purchases exceeding current balance | **PASSED** |
| | `Wallet_TrySpend_DeductsCorrectBalance` | Deducts cost and preserves lifetime earned | **PASSED** |
| | `Business_Upgrade_IncreasesCostAndIncome` | Validates Lv. 1->2->3 cost and +25% income yield | **PASSED** |
| | `BusinessTask_LifecycleAndRewardPrevention` | Verifies progress, claim payout, and duplicate claim rejection | **PASSED** |
| | `BusinessTask_Cancellation_ResetsRunningState` | Cancels running task and resets progress | **PASSED** |
| | `EconomyService_Tap_GeneratesGroundedIncomeAndMultipliers` | Verifies $4.00 tap and 6x booster ($24.00) expiration | **PASSED** |
| | `EconomyService_PassiveIncome_TicksCorrectly` | Verifies per-second passive income crediting | **PASSED** |
| | `EconomyService_LevelUp_AdvancesWhenTargetReached` | Verifies Level 1 ($1,000.00) target milestone advancement | **PASSED** |
| **PersistenceTests** | `SaveDataService_SaveAndLoad_PreservesAllFields` | Dual-file serialization preserves balance, level, and settings | **PASSED** |
| | `SaveDataService_PrimaryCorrupted_RecoversFromBackup` | Simulates primary file corruption and restores backup | **PASSED** |
| | `SaveDataService_BothCorrupted_ReturnsSafeDefaultState` | Corrupts both files and returns clean initialized state | **PASSED** |
| | `OfflineIncome_NormalDuration_ComputesExpectedYield` | Normal offline period earns exact hourly rate | **PASSED** |
| | `OfflineIncome_ExceedsCap_EnforcesExact3HourLimit` | Enforces exact 3-hour offline ceiling | **PASSED** |
| | `OfflineIncome_ClockRollback_DetectsTamperAndAwardsZero` | Detects backward time manipulation and denies earnings | **PASSED** |
| **PresentationControllerTests**| `BusinessTaskController_CannotStartIfBusinessNotOwned` | Disables task until Auto Repair Shop is acquired | **PASSED** |
| | `BusinessTaskController_Lifecycle_StartProgressClaim` | Verifies UI state, timer countdown, and reward claim | **PASSED** |
| | `BusinessTaskController_Cancel_ResetsToIdle` | Aborts task and restores button state | **PASSED** |
| | `MainHUDController_TracksBalanceAndTapping` | Verifies HUD text, tap handling, and booster banner | **PASSED** |
| | `BusinessViewController_HandlesAcquireAndUpgrades` | Validates afford checks, acquisition, and level upgrades | **PASSED** |
| | `OfflineEarningsModalController_CalculatesAndClaimsIncome`| Verifies popup display, time duration formatting, and claim | **PASSED** |
| | `GameSessionCoordinator_CompleteGameplayFlow_PersistsAndRestores` | Full integration test: Tap -> Buy Business -> Run Task -> Earn Passive -> Save -> Offline 2h -> Restore -> Claim | **PASSED** |

**Summary:** 26 Tests Run, 26 Passed, 0 Failed, 0 Skipped (Duration: 111 ms).

---

## 3. Environment & Hardware Verification Notes

1. **Test Runner:** Executed using `.NET 8.0.25` runtime located at `$env:USERPROFILE\.dotnet`.
2. **Unity Environment:** Unity 6 LTS scaffolded with Universal Render Pipeline (URP), IL2CPP configuration, and portrait orientation. Assembly definitions (`RichRich.Domain.asmdef` with `noEngineReferences: true`, and `RichRich.Presentation.asmdef`) isolate the domain logic.
3. **ADB / Physical Device Validation Status:**
   - Command `adb devices` was executed and reported 0 active devices attached during this development session.
   - In accordance with the project instructions ("Do not claim device validation unless executed"), physical Android on-device APK testing is marked deferred pending connection of the physical device and local Unity Editor batchmode installation.

---

## 4. Next Steps (Milestone 2 Roadmap)
- Expand business catalog from starter Auto Repair to full 14 business tiers (Store to Energy Company).
- Implement Business Mergers module with multi-asset prerequisites.
- Integrate Financial Markets simulation (Stocks & Crypto) with price tick oscillators.

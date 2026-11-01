# Technical Architecture Specification

## 1. Engine Evaluation & Technology Stack Rationale

To deliver a comparable, high-fidelity wealth simulation with equivalent UI performance, smooth financial charting, and instant responsiveness, three candidate engines were evaluated:

| Criterion | Unity 6 (Recommended) | Godot 4 (.NET / C#) | Flutter / React Native |
| :--- | :--- | :--- | :--- |
| **Parity with Reference** | **100% Exact match** (Reference uses Unity `6000.2.14f1`) | ~80% (requires porting shaders/plugins) | ~60% (struggles with 60/120fps animated particle/particle systems and heavy vector rendering) |
| **Financial Charting** | **Native XCharts integration** (proven in reference) | Custom line rendering required | High-quality web/flutter charts available |
| **Tweening & Animation** | **Native DOTween** (zero allocation, fluid 120fps) | Tween node | Reanimated / Flutter AnimationControllers |
| **Dependency Injection** | **Extenject / Zenject** | Custom DI container | Provider / Riverpod / Redux |
| **Build Pipeline & Profiling** | Mature Android IL2CPP + Google Frame Pacing (Swappy) | Good Android export | Native bridge overhead |

### Recommended Stack: Unity 6 LTS
- **Engine Version:** Unity `6000.0.x` or `6000.2.x` LTS.
- **Scripting Backend:** IL2CPP, Target Architecture: `ARM64` (`arm64-v8a`).
- **Target OS:** Android 9.0+ (API Level 28+) through Android 15 (API Level 35).
- **Core Libraries:**
  - `Zenject` (Extenject) for Dependency Injection
  - `R3` (Cysharp Reactive Extensions) for reactive event-driven models
  - `DOTween Pro` for tactile animations and rolling numbers
  - `XCharts` for financial chart rendering
  - `Newtonsoft.Json` for schema-safe JSON serialization
  - `TextMeshPro` for crisp resolution-independent typography

---

## 2. Architectural Design Pattern (Clean Architecture + Zenject)

The game follows a strict **Clean Architecture / Domain-Driven Design (DDD)** pattern with `RichRich.Domain.asmdef` compiled with `"noEngineReferences": true`:

```
┌────────────────────────────────────────────────────────┐
│                   Presentation Layer                   │
│ (GameBootstrapper, MainHUDView, LifeSimulationHUDView) │
└───────────────────────────▲────────────────────────────┘
                            │ (Events & Data Bindings)
┌───────────────────────────┴────────────────────────────┐
│                    Controller Layer                    │
│(LifeSimulationHUDController, BusinessViewController)   │
└───────────────────────────▲────────────────────────────┘
                            │ (Coordinates)
┌───────────────────────────┴────────────────────────────┐
│                Domain Application Services             │
│   (FinancialService, LivingExpenseService, Career,     │
│    EducationService, LoanService, HealthService)       │
└─────────────┬────────────────────────────┬─────────────┘
              │ uses                       │ persists
┌─────────────▼─────────────┐ ┌────────────▼─────────────┐
│       Domain Models       │ │    Persistence Layer     │
│ (Currency, BalanceSheet,  │ │ (SaveData, SaveDataSvc,  │
│  GameDateTime, Jobs, Loans│ │  Atomic .tmp -> .backup) │
└───────────────────────────┘ └──────────────────────────┘
```

### 2.1 Domain Application Services
1. **SimulationClock (`SimulationClock.cs`):** Central calendar timekeeper converting real time into simulation minutes, hours, days, and months with anti-duplication guards.
2. **FinancialService (`FinancialService.cs`):** Central double-entry transaction engine enforcing balance sheet integrity (Cash, Assets, Liabilities, Net Worth).
3. **FinancialLedger (`FinancialLedger.cs`):** Auditable ledger tracking all historical transactions, cashflow totals, and expense category distributions.
4. **LivingExpenseService (`LivingExpenseService.cs`):** Manages recurring baseline survival expenses (Rent, Food, Transport, Utilities, Internet, Personal) with scheduled due date deductions.
5. **HousingService (`HousingService.cs`):** Manages 5 accommodation tiers, move-in deposits, landlord eligibility verification, and safe updates to recurring accommodation rent.
6. **CareerService (`CareerService.cs`):** Manages 3 career ladders (Retail, Tech, Business) with 15+ tiers, qualification prerequisites, job tenure, promotions, and scheduled monthly salary disbursements.
7. **EducationService (`EducationService.cs`):** Manages workshops, professional certificates, and higher education degree programs (Business Diploma, CS Degree), tuition deductions, study pacing (Part-Time/Full-Time), qualifications, and skill acquisition.
8. **LifestyleService (`LifestyleService.cs`):** Manages player energy pool (0-100), work shift & study energy demands, fatigue levels, rest breaks, and housing-linked overnight sleep recovery.
9. **LoanService (`LoanService.cs`):** Underwrites loans ($5K / $10K), enforces 12% APR amortization, deducts scheduled installments, calculates principal/interest splits, and manages credit scoring.
10. **FinancialHealthService (`FinancialHealthService.cs`):** Evaluates player solvency across 6 formal states (Healthy, Stressed, Crisis, Insolvent, Bankrupt, Restructuring), integrating commercial business profits into projected cash flow.
11. **BusinessService (`BusinessService.cs`):** Master commercial enterprise engine managing enterprise catalogs, acquisitions, service bays, retail inventory, client contracts, staff recruitment, scheduled payroll, and facility valuations.
12. **LifeSimulationCoordinator (`LifeSimulationCoordinator.cs`):** Central master orchestrator tying all services, clock progression, and persistence into a cohesive headless simulation.

### 2.2 Presentation & View-Model Controllers
1. **BusinessMarketplaceController (`BusinessMarketplaceController.cs`):** View-model controller for browsing commercial opportunities, inspecting acquisition costs, and establishing commercial ventures.
2. **BusinessDashboardController (`BusinessDashboardController.cs`):** View-model controller for portfolio summary, enterprise performance, facility upgrades, and business liquidations.
3. **AutoRepairController (`AutoRepairController.cs`):** View-model controller for workshop service bay allocation, repair queue, vehicle intake, and delivery payouts.
4. **ConvenienceStoreController (`ConvenienceStoreController.cs`):** View-model controller for inventory stock lines, wholesale re-orders, customer pricing, and retail sales metrics.
5. **DigitalAgencyController (`DigitalAgencyController.cs`):** View-model controller for corporate client contracts, labor hour contributions, deadlines, and project payouts.
6. **BusinessEmployeeController (`BusinessEmployeeController.cs`):** View-model controller for candidate recruitment, staff rosters, productivity boosts, and monthly payroll.
7. **BusinessFinanceController (`BusinessFinanceController.cs`):** View-model controller for commercial loans, isolated business P&L accounting, and debt servicing.
8. **JobBoardController (`JobBoardController.cs`):** Decoupled presentation controller for track filtering, job inspection, applications, promotion tracking, and career profiles.
9. **EducationCatalogueController (`EducationCatalogueController.cs`):** Presentation controller for programme filtering, pace toggles, tuition checks, and live degree progression.
10. **HousingCatalogueController (`HousingCatalogueController.cs`):** Presentation controller for browsing 5 accommodation tiers, lease signing, and comfort comparisons.
11. **LifestyleStatusController (`LifestyleStatusController.cs`):** Presentation controller for energy meters, fatigue indicators, rest actions, and sleep transitions.
12. **LifeSimulationHUDController (`LifeSimulationHUDController.cs`):** View-model controller for core HUD metrics, time controls, and live status badges.

---

## 3. Local-First Persistence Engine

One of the key discoveries from our live reconnaissance of `/sdcard/Android/data/.../files/` is the **Modular Dual-Save System**.

Instead of a monolithic save file that can corrupt and wipe the entire player state, each subsystem persists independently with an atomic backup.

### 3.1 Persistence Contract
```csharp
public interface ISaveDataService
{
    T LoadData<T>(string saveKey) where T : new();
    void SaveData<T>(string saveKey, T data);
    void DeleteData(string saveKey);
}
```

### 3.2 Dual-File Atomic Save Implementation
```csharp
public class SaveDataService : ISaveDataService
{
    private readonly string _basePath;

    public SaveDataService()
    {
        _basePath = Application.persistentDataPath;
    }

    public void SaveData<T>(string saveKey, T data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        string mainPath = Path.Combine(_basePath, $"{saveKey}.json");
        string backupPath = Path.Combine(_basePath, $"{saveKey}.backup.json");

        // 1. Write to temporary file
        string tempPath = Path.Combine(_basePath, $"{saveKey}.tmp");
        File.WriteAllText(tempPath, json);

        // 2. Rotate existing main to backup
        if (File.Exists(mainPath))
        {
            File.Copy(mainPath, backupPath, overwrite: true);
        }

        // 3. Atomically move temp to main
        File.Move(tempPath, mainPath, overwrite: true);
    }

    public T LoadData<T>(string saveKey) where T : new()
    {
        string mainPath = Path.Combine(_basePath, $"{saveKey}.json");
        string backupPath = Path.Combine(_basePath, $"{saveKey}.backup.json");

        // Try main save
        if (File.Exists(mainPath))
        {
            try
            {
                string json = File.ReadAllText(mainPath);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Main save '{saveKey}' corrupted. Falling back to backup. Error: {ex.Message}");
            }
        }

        // Fallback to backup
        if (File.Exists(backupPath))
        {
            try
            {
                string json = File.ReadAllText(backupPath);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Both main and backup save '{saveKey}' corrupted: {ex.Message}");
            }
        }

        // Return clean default state
        return new T();
    }
}
```

---

## 4. Domain Data Schemas

Directly synthesized from verified device filesystem state:

### 4.1 Wallet & Currency Model (`PlayerBalance.json`)
```csharp
[Serializable]
public class PlayerBalanceData
{
    public double Balance { get; set; } = 0.0;
    public double MaxBalance { get; set; } = 0.0;
    public int Tickets { get; set; } = 3;
    public string LastOfflineIncomeTransactionId { get; set; } = "";
}
```

### 4.2 Tap & Progression Model (`IncomeData.json`)
```csharp
[Serializable]
public class IncomeData
{
    public int CurrentLevel { get; set; } = 1;
    public double TotalEarning { get; set; } = 0.0;
    public string IncomeVariantId { get; set; } = "15_base";
}
```

### 4.3 Stock Market Model (`StocksData.json`)
```csharp
[Serializable]
public class StockItemData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Capitalization { get; set; }
    public long Amount { get; set; }
    public double Divestments { get; set; }
    public double Price { get; set; }
    public List<double> PricesForTwoHour { get; set; } = new();
}

[Serializable]
public class StocksRootData
{
    public List<StockItemData> Stocks { get; set; } = new();
}
```

### 4.4 Business Inventory Model (`PurchasedBusinesses.json`)
```csharp
[Serializable]
public class BusinessInstanceData
{
    public string BusinessId { get; set; }
    public int Level { get; set; } = 1;
    public double BaseHourlyIncome { get; set; }
    public double BonusHourlyIncome { get; set; }
    public int CompletedOperations { get; set; } = 0; // For surgeries, repairs, IT projects
    public int FleetSize { get; set; } = 0; // For taxis and delivery
}
```

---

## 5. Offline Integrity & Time Synchronization

To preserve game balance without requiring an online authentication server:
1. **Monotonic Clocks:** Use `System.Diagnostics.Stopwatch.GetTimestamp()` and `Time.realtimeSinceStartup` during active play to prevent system clock tampering while the game is running.
2. **Offline Elapsed Calculation:**
   $$\Delta t = \min(t_{\text{device\_now}} - t_{\text{last\_save}}, \text{MAX\_OFFLINE\_SECONDS})$$
3. **Anti-Clock Manipulation Safeguard:** If $t_{\text{device\_now}} < t_{\text{last\_save}}$ (player rolled device clock backwards), reject time addition and flag safe reset to prevent backward-time exploits.
4. **Zero Remote Dependencies:** Progression, business yields, and markets calculate entirely on-device, satisfying the strict restriction: **No login, no signup, no required backend**.

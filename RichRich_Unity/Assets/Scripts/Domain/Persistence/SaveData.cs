using System;
using System.Collections.Generic;
using RichRich.Domain.Models;

namespace RichRich.Domain.Persistence
{
    [Serializable]
    public class LoanSaveSnapshot
    {
        public string LoanId { get; set; }
        public string ProductId { get; set; }
        public long RemainingPrincipalCents { get; set; }
        public int MonthsRemaining { get; set; }
        public int PaymentsMade { get; set; }
        public int MissedPaymentsCount { get; set; }
        public long OverdueAmountCents { get; set; }
        public int DueDayOfMonth { get; set; }
        public int LastPaidMonth { get; set; }
    }

    [Serializable]
    public class BusinessSaveSnapshot
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public bool IsOwned { get; set; }
        public int Level { get; set; }
        public int Reputation { get; set; }
        public double Condition { get; set; }
        public bool IsOpen { get; set; }
        public int CompletedTasks { get; set; }
    }

    [Serializable]
    public class EmployeeSaveSnapshot
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BusinessId { get; set; }
        public string Role { get; set; }
        public long MonthlySalaryCents { get; set; }
        public double Productivity { get; set; }
        public int HiredMonth { get; set; }
    }

    [Serializable]
    public class StoreProductSaveSnapshot
    {
        public string ProductId { get; set; }
        public int StockOnHand { get; set; }
        public long SellingPriceCents { get; set; }
        public int TotalUnitsSold { get; set; }
        public int MissedSalesDueToStockout { get; set; }
    }

    [Serializable]
    public class AgencyProjectSaveSnapshot
    {
        public string ProjectId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public long ContractValueCents { get; set; }
        public double RequiredHours { get; set; }
        public double CompletedHours { get; set; }
        public int DeadlineDay { get; set; }
        public int RequiredReputation { get; set; }
        public bool IsAccepted { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsExpired { get; set; }
    }

    [Serializable]
    public class AutoRepairJobSaveSnapshot
    {
        public string JobId { get; set; }
        public string VehicleModel { get; set; }
        public string Category { get; set; }
        public long RevenueCents { get; set; }
        public long PartsCostCents { get; set; }
        public int DurationMinutes { get; set; }
        public int ElapsedMinutes { get; set; }
        public bool IsCompleted { get; set; }
        public int AssignedBayIndex { get; set; }
    }

    /// <summary>
    /// Versioned, portable save schema for RichRich Life & Wealth Simulation.
    /// Schema Version 4 supports Entrepreneurship & Business Ownership (Auto Repair, Convenience Store, Digital Agency, Employees, Inventory, Contracts).
    /// Backward-compatible with Schema 1, 2, and 3.
    /// Serializes to clean human-readable JSON with zero personal data.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int SchemaVersion { get; set; } = 4;
        public long CreatedAtUnix { get; set; }
        public long LastSavedUnix { get; set; }

        // Simulation Clock & Calendar
        public string Difficulty { get; set; } = GameDifficulty.Standard.ToString();
        public long SimulationMinutes { get; set; } = GameDateTime.DefaultStart.TotalMinutes;
        public long LastProcessedSimulationMinute { get; set; } = GameDateTime.DefaultStart.TotalMinutes;

        // Financial Ledger & Wallet
        public long BalanceCents { get; set; }
        public long LifetimeEarnedCents { get; set; }
        public long OverdueLiabilitiesCents { get; set; }
        public int CreditScore { get; set; } = 650;
        public List<LoanSaveSnapshot> ActiveLoans { get; set; } = new List<LoanSaveSnapshot>();

        // Career & Employment
        public string CurrentJobId { get; set; }
        public int TotalMonthsWorked { get; set; }
        public int CurrentJobTenureMonths { get; set; }
        public int LastSalaryPaidMonth { get; set; }
        public int RetailExperienceMonths { get; set; }
        public int TechExperienceMonths { get; set; }
        public int BusinessExperienceMonths { get; set; }

        // Living Expenses & Housing
        public int LastExpensePaidMonth { get; set; }
        public string CurrentHousingTierId { get; set; } = "housing_studio";
        public long HousingDepositPaidCents { get; set; } = 60000L;

        // Education & Qualifications
        public string ActiveCourseId { get; set; }
        public double ActiveCourseHoursCompleted { get; set; }
        public string StudyPace { get; set; } = "PartTime";
        public List<string> EarnedQualifications { get; set; } = new List<string>();

        // Lifestyle & Energy
        public int CurrentEnergy { get; set; } = 100;

        // Entrepreneurship & Business Ownership (Schema 4)
        public List<BusinessSaveSnapshot> Businesses { get; set; } = new List<BusinessSaveSnapshot>();
        public List<EmployeeSaveSnapshot> Employees { get; set; } = new List<EmployeeSaveSnapshot>();
        public List<StoreProductSaveSnapshot> StoreInventory { get; set; } = new List<StoreProductSaveSnapshot>();
        public List<AgencyProjectSaveSnapshot> AgencyProjects { get; set; } = new List<AgencyProjectSaveSnapshot>();
        public List<AutoRepairJobSaveSnapshot> ActiveRepairJobs { get; set; } = new List<AutoRepairJobSaveSnapshot>();

        // Legacy / Schema 1 Fields (Preserved for backwards compatibility)
        public int CurrentLevel { get; set; } = 1;
        public int Tickets { get; set; } = EconomyConfig.StartingTickets;
        public string BusinessId { get; set; } = "auto_repair";
        public bool IsBusinessOwned { get; set; } = false;
        public int BusinessLevel { get; set; } = 1;
        public int BusinessTasksCompleted { get; set; } = 0;

        // Settings
        public bool HapticsEnabled { get; set; } = true;
        public bool SoundEnabled { get; set; } = true;

        public static SaveData CreateDefault(long currentUnix = 0, GameDifficulty difficulty = GameDifficulty.Standard)
        {
            Currency startCash = GameStartConfig.GetStartingCash(difficulty);
            return new SaveData
            {
                SchemaVersion = 4,
                CreatedAtUnix = currentUnix,
                LastSavedUnix = currentUnix,
                Difficulty = difficulty.ToString(),
                SimulationMinutes = GameDateTime.DefaultStart.TotalMinutes,
                LastProcessedSimulationMinute = GameDateTime.DefaultStart.TotalMinutes,
                BalanceCents = startCash.MinorUnits,
                LifetimeEarnedCents = startCash.MinorUnits,
                OverdueLiabilitiesCents = 0L,
                CreditScore = 650,
                ActiveLoans = new List<LoanSaveSnapshot>(),
                Businesses = new List<BusinessSaveSnapshot>(),
                Employees = new List<EmployeeSaveSnapshot>(),
                StoreInventory = new List<StoreProductSaveSnapshot>(),
                AgencyProjects = new List<AgencyProjectSaveSnapshot>(),
                ActiveRepairJobs = new List<AutoRepairJobSaveSnapshot>(),
                CurrentJobId = null,
                TotalMonthsWorked = 0,
                CurrentJobTenureMonths = 0,
                LastSalaryPaidMonth = 0,
                RetailExperienceMonths = 0,
                TechExperienceMonths = 0,
                BusinessExperienceMonths = 0,
                LastExpensePaidMonth = 0,
                CurrentHousingTierId = "housing_studio",
                HousingDepositPaidCents = 60000L,
                ActiveCourseId = null,
                ActiveCourseHoursCompleted = 0.0,
                StudyPace = "PartTime",
                EarnedQualifications = new List<string>(),
                CurrentEnergy = 100,
                CurrentLevel = 1,
                Tickets = EconomyConfig.StartingTickets,
                BusinessId = "auto_repair",
                IsBusinessOwned = false,
                BusinessLevel = 1,
                BusinessTasksCompleted = 0,
                HapticsEnabled = true,
                SoundEnabled = true
            };
        }
    }
}

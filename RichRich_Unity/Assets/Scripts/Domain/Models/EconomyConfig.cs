namespace RichRich.Domain.Models
{
    /// <summary>
    /// Economy configuration constants.
    /// Grounded directly in Phase 1 reconnaissance findings.
    /// </summary>
    public static class EconomyConfig
    {
        // VERIFIED: Level 1 base tap = $4.00 (400 cents)
        public static readonly Currency BaseTapIncome = Currency.FromCents(400);

        // VERIFIED: Level 1 target milestone = $1,000.00 (100,000 cents)
        public static readonly Currency Level1TargetBalance = Currency.FromCents(100_000);

        // INFERRED: Level milestone scaling factor
        public const double LevelTargetMultiplier = 2.5;

        // Starter business constants
        public const string StarterBusinessId = "auto_repair";
        public const string StarterBusinessName = "Auto Repair Workshop";

        // VERIFIED: Starter business cost grounded in lowest tier business ($5,499.00)
        public static readonly Currency StarterBusinessBaseCost = Currency.FromCents(549_900);

        // INFERRED: Base hourly passive yield for starter business ($120.00/hr)
        public static readonly Currency StarterBusinessBaseHourlyYield = Currency.FromCents(12_000);

        // INFERRED: Upgrade cost escalation multiplier per level
        public const double UpgradeCostMultiplier = 1.25;

        // INFERRED: Upgrade yield increment (+25% base income per level)
        public const double UpgradeYieldBonusPercent = 0.25;

        // VERIFIED: Tap multiplier booster (6x multiplier)
        public const double TapMultiplierFactor = 6.0;

        // VERIFIED: Booster duration (30 seconds)
        public const double TapMultiplierDurationSeconds = 30.0;

        // VERIFIED: Max offline income window = 3 hours (10,800 seconds)
        public const int MaxOfflineIncomeSeconds = 3 * 3600;

        // VERIFIED: Starter ad-skip tickets
        public const int StartingTickets = 3;

        // Operational Task (Autorepair diagnostic / service)
        public const string DefaultTaskId = "task_car_service";
        public const string DefaultTaskName = "Vehicle Diagnostic & Service";
        public const double DefaultTaskDurationSeconds = 5.0;
        public static readonly Currency DefaultTaskReward = Currency.FromCents(15_000); // $150.00
    }
}

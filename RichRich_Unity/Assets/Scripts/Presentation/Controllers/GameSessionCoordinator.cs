using System;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Pure C# Session Coordinator that integrates the entire game state:
    /// Persistence loading/saving, offline catch-up, domain services, and all presentation controllers.
    /// Fully testable in headless NUnit test harness without UnityEngine dependencies.
    /// </summary>
    public class GameSessionCoordinator
    {
        public SaveDataService SaveService { get; }
        public ITimeProvider TimeProvider { get; }

        public Wallet Wallet { get; private set; }
        public Business AutoRepairShop { get; private set; }
        public BusinessTask DiagnosticsTask { get; private set; }
        public EconomyService Economy { get; private set; }

        public MainHUDController HUDController { get; private set; }
        public BusinessViewController BusinessController { get; private set; }
        public BusinessTaskController TaskController { get; private set; }
        public SettingsModalController SettingsController { get; private set; }
        public OfflineEarningsModalController OfflineModalController { get; private set; }

        private double _autoSaveTimer;
        private const double AutoSaveIntervalSeconds = 30.0;

        public event Action OnStateSaved;

        public GameSessionCoordinator(SaveDataService saveService, ITimeProvider timeProvider = null)
        {
            SaveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            TimeProvider = timeProvider ?? new SystemTimeProvider();
        }

        public void InitializeSession()
        {
            // 1. Load persisted data or fallback to defaults
            SaveData save = SaveService.Load();

            Wallet = new Wallet(
                Currency.FromCents(save.BalanceCents),
                Currency.FromCents(save.LifetimeEarnedCents)
            );

            AutoRepairShop = new Business(
                save.BusinessId ?? EconomyConfig.StarterBusinessId,
                EconomyConfig.StarterBusinessName,
                EconomyConfig.StarterBusinessBaseCost,
                EconomyConfig.StarterBusinessBaseHourlyYield,
                save.IsBusinessOwned,
                save.BusinessLevel
            );

            DiagnosticsTask = new BusinessTask(
                EconomyConfig.DefaultTaskId,
                EconomyConfig.DefaultTaskName,
                EconomyConfig.DefaultTaskDurationSeconds,
                EconomyConfig.DefaultTaskReward
            );

            Economy = new EconomyService(
                Wallet,
                AutoRepairShop,
                DiagnosticsTask,
                TimeProvider,
                save.CurrentLevel
            );

            SettingsController = new SettingsModalController(
                save.SoundEnabled,
                save.HapticsEnabled
            );

            // 2. Offline income evaluation
            long nowUnix = TimeProvider.CurrentUnixSeconds;
            if (AutoRepairShop.IsOwned && save.LastSavedUnix > 0)
            {
                var offlineYield = AutoRepairShop.GetHourlyIncome();
                var offlineResult = OfflineIncomeCalculator.Calculate(save.LastSavedUnix, nowUnix, offlineYield);
                OfflineModalController = new OfflineEarningsModalController(offlineResult, Wallet);
            }
            else
            {
                OfflineModalController = new OfflineEarningsModalController(null, Wallet);
            }

            // 3. Presentation View-Model Controllers
            HUDController = new MainHUDController(Economy);
            BusinessController = new BusinessViewController(AutoRepairShop, Wallet, Economy);
            TaskController = new BusinessTaskController(AutoRepairShop, DiagnosticsTask, Wallet);

            _autoSaveTimer = 0.0;
        }

        public void Tick(double deltaSeconds)
        {
            if (deltaSeconds <= 0) return;

            HUDController?.Tick(deltaSeconds);
            TaskController?.Tick(deltaSeconds);

            _autoSaveTimer += deltaSeconds;
            if (_autoSaveTimer >= AutoSaveIntervalSeconds)
            {
                _autoSaveTimer = 0.0;
                SaveCurrentState();
            }
        }

        public SaveData SaveCurrentState()
        {
            var save = new SaveData
            {
                SchemaVersion = 1,
                LastSavedUnix = TimeProvider.CurrentUnixSeconds,
                BalanceCents = Wallet.Balance.MinorUnits,
                LifetimeEarnedCents = Wallet.LifetimeEarned.MinorUnits,
                CurrentLevel = Economy.CurrentLevel,
                Tickets = EconomyConfig.StartingTickets,
                BusinessId = AutoRepairShop.Id,
                IsBusinessOwned = AutoRepairShop.IsOwned,
                BusinessLevel = AutoRepairShop.Level,
                BusinessTasksCompleted = 0,
                HapticsEnabled = SettingsController.HapticsEnabled,
                SoundEnabled = SettingsController.SoundEnabled
            };

            SaveService.Save(save);
            OnStateSaved?.Invoke();
            return save;
        }
    }
}

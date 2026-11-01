using System;
using UnityEngine;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;
using RichRich.Domain.Services;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity lifecycle entry-point and scene bootstrapper for the Life & Wealth simulation.
    /// Wires together the persistent life simulation coordinator, controllers, and UI views.
    /// Handles frame updates, application pause/resume, and crash-resilient auto-saving.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private MainHUDView _hudView;
        [SerializeField] private BusinessShopView _shopView;
        [SerializeField] private BusinessTaskView _taskView;
        [SerializeField] private SettingsModalView _settingsView;

        [Header("Life & Career Progression Views")]
        [SerializeField] private JobBoardView _jobBoardView;
        [SerializeField] private EducationCatalogueView _educationView;
        [SerializeField] private HousingCatalogueView _housingView;
        [SerializeField] private LifestyleStatusView _lifestyleView;

        [Header("Commercial Enterprise Views")]
        [SerializeField] private BusinessMarketplaceView _marketplaceView;
        [SerializeField] private BusinessDashboardView _dashboardView;
        [SerializeField] private AutoRepairView _autoRepairView;
        [SerializeField] private ConvenienceStoreView _convenienceStoreView;
        [SerializeField] private DigitalAgencyView _digitalAgencyView;
        [SerializeField] private BusinessEmployeeView _employeeView;
        [SerializeField] private BusinessFinanceView _financeView;

        public LifeSimulationCoordinator LifeCoordinator { get; private set; }
        public LifeSimulationHUDController LifeHUDController { get; private set; }
        public GameSessionCoordinator LegacyCoordinator { get; private set; }

        // Controllers
        public JobBoardController JobBoardController { get; private set; }
        public EducationCatalogueController EducationController { get; private set; }
        public HousingCatalogueController HousingController { get; private set; }
        public LifestyleStatusController LifestyleController { get; private set; }
        public BusinessMarketplaceController MarketplaceController { get; private set; }
        public BusinessDashboardController DashboardController { get; private set; }
        public AutoRepairController AutoRepairController { get; private set; }
        public ConvenienceStoreController ConvenienceStoreController { get; private set; }
        public DigitalAgencyController DigitalAgencyController { get; private set; }
        public BusinessEmployeeController EmployeeController { get; private set; }
        public BusinessFinanceController FinanceController { get; private set; }

        private void Awake()
        {
            // Configure Android target framerate and screen sleep
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Initialize Persistence with platform persistent data path
            string saveDir = Application.persistentDataPath;
            var saveService = new SaveDataService(saveDir);

            // Initialize Life & Wealth Simulation Master Coordinator
            LifeCoordinator = new LifeSimulationCoordinator(saveService);
            LifeCoordinator.InitializeSession(GameDifficulty.Standard);

            // Life Simulation HUD Controller
            LifeHUDController = new LifeSimulationHUDController(LifeCoordinator);

            // Bind Views to Life Simulation Controllers
            if (_hudView != null)
            {
                _hudView.Bind(LifeHUDController);
            }

            // Life & Career Progression Subsystems
            JobBoardController = new JobBoardController(LifeCoordinator);
            if (_jobBoardView != null) _jobBoardView.Bind(JobBoardController);

            EducationController = new EducationCatalogueController(LifeCoordinator);
            if (_educationView != null) _educationView.Bind(EducationController);

            HousingController = new HousingCatalogueController(LifeCoordinator);
            if (_housingView != null) _housingView.Bind(HousingController);

            LifestyleController = new LifestyleStatusController(LifeCoordinator);
            if (_lifestyleView != null) _lifestyleView.Bind(LifestyleController);

            // Commercial Business Subsystems
            MarketplaceController = new BusinessMarketplaceController(LifeCoordinator);
            if (_marketplaceView != null) _marketplaceView.Bind(MarketplaceController);

            DashboardController = new BusinessDashboardController(LifeCoordinator);
            if (_dashboardView != null) _dashboardView.Bind(DashboardController);

            AutoRepairController = new AutoRepairController(LifeCoordinator);
            if (_autoRepairView != null) _autoRepairView.Bind(AutoRepairController);

            ConvenienceStoreController = new ConvenienceStoreController(LifeCoordinator);
            if (_convenienceStoreView != null) _convenienceStoreView.Bind(ConvenienceStoreController);

            DigitalAgencyController = new DigitalAgencyController(LifeCoordinator);
            if (_digitalAgencyView != null) _digitalAgencyView.Bind(DigitalAgencyController);

            EmployeeController = new BusinessEmployeeController(LifeCoordinator);
            if (_employeeView != null) _employeeView.Bind(EmployeeController);

            FinanceController = new BusinessFinanceController(LifeCoordinator);
            if (_financeView != null) _financeView.Bind(FinanceController);

            // Legacy business / shop views wired through session coordinator if present in scene
            var timeProvider = new SystemTimeProvider();
            LegacyCoordinator = new GameSessionCoordinator(saveService, timeProvider);
            LegacyCoordinator.InitializeSession();

            if (_shopView != null)
            {
                _shopView.Bind(LegacyCoordinator.BusinessController);
            }

            if (_taskView != null)
            {
                _taskView.Bind(LegacyCoordinator.TaskController);
            }

            if (_settingsView != null)
            {
                _settingsView.Bind(LegacyCoordinator.SettingsController, saveService);
            }
        }

        private void Update()
        {
            LifeCoordinator?.Tick(Time.deltaTime);
            LegacyCoordinator?.Tick(Time.deltaTime);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                LifeCoordinator?.SaveCurrentState();
                LegacyCoordinator?.SaveCurrentState();
            }
        }

        private void OnApplicationQuit()
        {
            LifeCoordinator?.SaveCurrentState();
            LegacyCoordinator?.SaveCurrentState();
        }
    }
}

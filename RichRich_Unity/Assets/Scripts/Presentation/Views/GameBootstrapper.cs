using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;
using RichRich.Domain.Services;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity lifecycle entry-point and scene bootstrapper for the Life & Wealth simulation.
    /// Wires together the persistent life simulation coordinator, controllers, and UI views.
    /// Features self-healing fallback UI construction and startup diagnostic recovery
    /// to guarantee the game never stalls on an unexplained black screen.
    /// Handles frame updates, application pause/resume, and crash-resilient auto-saving.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        public static GameBootstrapper Instance { get; private set; }

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

        [Header("Real Estate & Property Portfolio")]
        [SerializeField] private RealEstateView _realEstateView;

        [Header("Financial Markets & Equities")]
        [SerializeField] private StockMarketView _stockMarketView;

        [Header("Life Progression & Goals")]
        [SerializeField] private LifeProgressionView _goalsView;

        [Header("3D City Environment")]
        [SerializeField] private CityInteractionController _cityController;
        [SerializeField] private Button _cityOpenButton;

        public LifeSimulationCoordinator LifeCoordinator { get; private set; }
        public LifeSimulationHUDController LifeHUDController { get; private set; }
        public GameSessionCoordinator LegacyCoordinator { get; private set; }

        public bool IsInitialized { get; private set; }
        public Exception StartupError { get; private set; }

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
        public RealEstateController RealEstateController { get; private set; }
        public StockMarketController StockMarketController { get; private set; }
        public LifeProgressionController GoalsController { get; private set; }

        private SaveDataService _saveService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Configure Android target framerate and screen sleep
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            try
            {
                InitializeGame();
                IsInitialized = true;
                Debug.Log("[GameBootstrapper] RichRich Life & Enterprise Simulation successfully initialized!");
            }
            catch (Exception ex)
            {
                StartupError = ex;
                Debug.LogError($"[GameBootstrapper] FATAL STARTUP EXCEPTION: {ex}");
                ShowStartupRecoveryScreen(ex);
            }
        }

        private void InitializeGame()
        {
            // 1. Initialize Persistence with platform persistent data path
            string saveDir = Application.persistentDataPath;
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir))
            {
                try
                {
                    if (!string.IsNullOrEmpty(saveDir)) Directory.CreateDirectory(saveDir);
                }
                catch
                {
                    saveDir = Application.temporaryCachePath;
                }
            }
            _saveService = new SaveDataService(saveDir);

            // 2. Initialize Life & Wealth Simulation Master Coordinator
            LifeCoordinator = new LifeSimulationCoordinator(_saveService);
            try
            {
                LifeCoordinator.InitializeSession(GameDifficulty.Standard);
            }
            catch (Exception saveEx)
            {
                Debug.LogWarning($"[GameBootstrapper] Corrupted or invalid save detected, falling back to fresh start: {saveEx.Message}");
                // Initialize clean session if save load failed
                LifeCoordinator = new LifeSimulationCoordinator(new SaveDataService(saveDir));
                LifeCoordinator.InitializeSession(GameDifficulty.Standard);
            }

            // 3. Life Simulation HUD Controller
            LifeHUDController = new LifeSimulationHUDController(LifeCoordinator);

            // 4. Ensure UI is available; if views were missing from scene, construct runtime fallback
            if (_hudView == null)
            {
                Debug.LogWarning("[GameBootstrapper] _hudView is missing from scene. Constructing runtime self-healing UI...");
                EnsureRuntimeUI();
            }

            // 5. Bind Views to Life Simulation Controllers safely
            SafeBind(() =>
            {
                if (_hudView != null) _hudView.Bind(LifeHUDController);
            }, "MainHUDView");

            // Life & Career Progression Subsystems
            SafeBind(() =>
            {
                JobBoardController = new JobBoardController(LifeCoordinator);
                if (_jobBoardView != null) _jobBoardView.Bind(JobBoardController);
            }, "JobBoard");

            SafeBind(() =>
            {
                EducationController = new EducationCatalogueController(LifeCoordinator);
                if (_educationView != null) _educationView.Bind(EducationController);
            }, "Education");

            SafeBind(() =>
            {
                HousingController = new HousingCatalogueController(LifeCoordinator);
                if (_housingView != null) _housingView.Bind(HousingController);
            }, "Housing");

            SafeBind(() =>
            {
                LifestyleController = new LifestyleStatusController(LifeCoordinator);
                if (_lifestyleView != null) _lifestyleView.Bind(LifestyleController);
            }, "Lifestyle");

            // Commercial Business Subsystems
            SafeBind(() =>
            {
                MarketplaceController = new BusinessMarketplaceController(LifeCoordinator);
                if (_marketplaceView != null) _marketplaceView.Bind(MarketplaceController);
            }, "Marketplace");

            SafeBind(() =>
            {
                DashboardController = new BusinessDashboardController(LifeCoordinator);
                if (_dashboardView != null) _dashboardView.Bind(DashboardController);
            }, "Dashboard");

            SafeBind(() =>
            {
                AutoRepairController = new AutoRepairController(LifeCoordinator);
                if (_autoRepairView != null) _autoRepairView.Bind(AutoRepairController);
            }, "AutoRepair");

            SafeBind(() =>
            {
                ConvenienceStoreController = new ConvenienceStoreController(LifeCoordinator);
                if (_convenienceStoreView != null) _convenienceStoreView.Bind(ConvenienceStoreController);
            }, "ConvenienceStore");

            SafeBind(() =>
            {
                DigitalAgencyController = new DigitalAgencyController(LifeCoordinator);
                if (_digitalAgencyView != null) _digitalAgencyView.Bind(DigitalAgencyController);
            }, "DigitalAgency");

            SafeBind(() =>
            {
                EmployeeController = new BusinessEmployeeController(LifeCoordinator);
                if (_employeeView != null) _employeeView.Bind(EmployeeController);
            }, "Employee");

            SafeBind(() =>
            {
                FinanceController = new BusinessFinanceController(LifeCoordinator);
                if (_financeView != null) _financeView.Bind(FinanceController);
            }, "Finance");

            SafeBind(() =>
            {
                RealEstateController = new RealEstateController(LifeCoordinator);
                if (_realEstateView != null) _realEstateView.Bind(RealEstateController);
            }, "RealEstate");

            SafeBind(() =>
            {
                StockMarketController = new StockMarketController(LifeCoordinator);
                if (_stockMarketView != null) _stockMarketView.Bind(StockMarketController);
            }, "StockMarket");

            SafeBind(() =>
            {
                GoalsController = new LifeProgressionController(LifeCoordinator);
                if (_goalsView != null) _goalsView.Bind(GoalsController);
            }, "LifeProgression");

            SafeBind(() =>
            {
                if (_cityController == null)
                {
                    _cityController = FindObjectOfType<CityInteractionController>();
                }
                if (_cityController != null && _cityOpenButton != null)
                {
                    _cityOpenButton.onClick.AddListener(() => _cityController.SetCityExploreMode(true));
                }
            }, "CityEnvironment");

            // Legacy views (only wired if specifically present, without overwriting Schema 4 save)
            if (_shopView != null || _taskView != null)
            {
                SafeBind(() =>
                {
                    var timeProvider = new SystemTimeProvider();
                    LegacyCoordinator = new GameSessionCoordinator(_saveService, timeProvider);
                    LegacyCoordinator.InitializeSession();

                    if (_shopView != null) _shopView.Bind(LegacyCoordinator.BusinessController);
                    if (_taskView != null) _taskView.Bind(LegacyCoordinator.TaskController);
                }, "LegacyViews");
            }

            // Settings Modal
            SafeBind(() =>
            {
                if (_settingsView != null)
                {
                    var settingsController = new SettingsModalController();
                    _settingsView.Bind(settingsController, _saveService);
                }
            }, "SettingsModal");

            // Floating Currency Feedback Event Binding
            SafeBind(() =>
            {
                if (LifeCoordinator.FinancialService != null)
                {
                    LifeCoordinator.FinancialService.OnTransactionExecuted += tx =>
                    {
                        if (FloatingCurrencyFeedback.Instance != null)
                        {
                            if (tx.Type == TransactionType.Income || tx.Type == TransactionType.LiabilityDraw || tx.Type == TransactionType.AssetLiquidation)
                            {
                                FloatingCurrencyFeedback.Instance.ShowIncome(tx.Amount, tx.Description);
                            }
                            else
                            {
                                FloatingCurrencyFeedback.Instance.ShowExpense(tx.Amount, tx.Description);
                            }
                        }
                    };
                }
            }, "CurrencyFeedback");
        }

        private void SafeBind(Action action, string subsystemName)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrapper] Error binding {subsystemName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Self-healing fallback: Dynamically constructs camera, Canvas, EventSystem,
        /// and financial HUD if the scene was loaded without pre-authored UI.
        /// </summary>
        private void EnsureRuntimeUI()
        {
            // 1. Ensure Camera exists
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.transform.position = new Vector3(0, 0, -10f);
                mainCam = camGo.AddComponent<Camera>();
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 1.0f);
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5.0f;
            }

            // 2. Ensure EventSystem exists
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventGo = new GameObject("EventSystem");
                eventGo.AddComponent<EventSystem>();
                eventGo.AddComponent<StandaloneInputModule>();
            }

            // 3. Ensure Canvas exists
            var canvas = FindObjectOfType<Canvas>();
            GameObject canvasGo;
            if (canvas == null)
            {
                canvasGo = new GameObject("Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasGo = canvas.gameObject;
            }

            // 3.5 Ensure ModalManager exists
            if (FindObjectOfType<ModalManager>() == null)
            {
                var modalMgrGo = new GameObject("ModalManager");
                modalMgrGo.transform.SetParent(canvasGo.transform, false);
                modalMgrGo.AddComponent<ModalManager>();
            }

            // 4. Construct Runtime Fallback HUD
            var fallbackGo = new GameObject("RuntimeFallbackHUD", typeof(RectTransform));
            fallbackGo.transform.SetParent(canvasGo.transform, false);
            var rt = fallbackGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(30, 60);
            rt.offsetMax = new Vector2(-30, -60);

            _hudView = fallbackGo.AddComponent<MainHUDView>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Title
            var title = CreateRuntimeText("Title", fallbackGo, "RichRich: Life Simulation", 36, new Color(0.98f, 0.75f, 0.18f), font, TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = new Vector2(0, 0.92f);
            title.rectTransform.anchorMax = new Vector2(1, 1f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            // Cash Display
            var cashText = CreateRuntimeText("CashText", fallbackGo, "$3,000.00", 54, new Color(0.18f, 0.85f, 0.45f), font, TextAnchor.MiddleLeft);
            cashText.rectTransform.anchorMin = new Vector2(0, 0.82f);
            cashText.rectTransform.anchorMax = new Vector2(1, 0.92f);
            cashText.rectTransform.offsetMin = Vector2.zero;
            cashText.rectTransform.offsetMax = Vector2.zero;

            // Net Worth
            var netWorthText = CreateRuntimeText("NetWorthText", fallbackGo, "Net Worth: $3,000.00", 28, Color.white, font, TextAnchor.MiddleLeft);
            netWorthText.rectTransform.anchorMin = new Vector2(0, 0.75f);
            netWorthText.rectTransform.anchorMax = new Vector2(1, 0.82f);
            netWorthText.rectTransform.offsetMin = Vector2.zero;
            netWorthText.rectTransform.offsetMax = Vector2.zero;

            // Health Status
            var healthBadge = CreateRuntimeText("HealthBadge", fallbackGo, "Status: Solvent", 26, new Color(0.40f, 0.92f, 0.60f), font, TextAnchor.MiddleLeft);
            healthBadge.rectTransform.anchorMin = new Vector2(0, 0.68f);
            healthBadge.rectTransform.anchorMax = new Vector2(1, 0.75f);
            healthBadge.rectTransform.offsetMin = Vector2.zero;
            healthBadge.rectTransform.offsetMax = Vector2.zero;

            // Date
            var dateText = CreateRuntimeText("DateText", fallbackGo, "Year 1, Month 1 - Day 1", 26, new Color(0.70f, 0.78f, 0.90f), font, TextAnchor.MiddleLeft);
            dateText.rectTransform.anchorMin = new Vector2(0, 0.60f);
            dateText.rectTransform.anchorMax = new Vector2(1, 0.68f);
            dateText.rectTransform.offsetMin = Vector2.zero;
            dateText.rectTransform.offsetMax = Vector2.zero;

            // Speed
            var speedText = CreateRuntimeText("SpeedText", fallbackGo, "1x (Normal)", 24, new Color(0.35f, 0.80f, 0.98f), font, TextAnchor.MiddleLeft);
            speedText.rectTransform.anchorMin = new Vector2(0, 0.54f);
            speedText.rectTransform.anchorMax = new Vector2(1, 0.60f);
            speedText.rectTransform.offsetMin = Vector2.zero;
            speedText.rectTransform.offsetMax = Vector2.zero;

            // Career / Study / Housing summary
            var careerText = CreateRuntimeText("CareerText", fallbackGo, "Career: Unemployed", 24, Color.white, font, TextAnchor.MiddleLeft);
            careerText.rectTransform.anchorMin = new Vector2(0, 0.46f);
            careerText.rectTransform.anchorMax = new Vector2(1, 0.54f);
            careerText.rectTransform.offsetMin = Vector2.zero;
            careerText.rectTransform.offsetMax = Vector2.zero;

            var eduText = CreateRuntimeText("EduText", fallbackGo, "Study: None", 24, new Color(0.80f, 0.85f, 0.95f), font, TextAnchor.MiddleLeft);
            eduText.rectTransform.anchorMin = new Vector2(0, 0.38f);
            eduText.rectTransform.anchorMax = new Vector2(1, 0.46f);
            eduText.rectTransform.offsetMin = Vector2.zero;
            eduText.rectTransform.offsetMax = Vector2.zero;

            var housingText = CreateRuntimeText("HousingText", fallbackGo, "Housing: Homeless", 24, new Color(0.95f, 0.80f, 0.35f), font, TextAnchor.MiddleLeft);
            housingText.rectTransform.anchorMin = new Vector2(0, 0.30f);
            housingText.rectTransform.anchorMax = new Vector2(1, 0.38f);
            housingText.rectTransform.offsetMin = Vector2.zero;
            housingText.rectTransform.offsetMax = Vector2.zero;

            var energyText = CreateRuntimeText("EnergyText", fallbackGo, "Energy: 100/100", 24, new Color(0.35f, 0.80f, 0.98f), font, TextAnchor.MiddleLeft);
            energyText.rectTransform.anchorMin = new Vector2(0, 0.22f);
            energyText.rectTransform.anchorMax = new Vector2(1, 0.30f);
            energyText.rectTransform.offsetMin = Vector2.zero;
            energyText.rectTransform.offsetMax = Vector2.zero;

            var fatigueText = CreateRuntimeText("FatigueText", fallbackGo, "Fatigue: Well-Rested", 24, new Color(0.40f, 0.90f, 0.60f), font, TextAnchor.MiddleLeft);
            fatigueText.rectTransform.anchorMin = new Vector2(0, 0.14f);
            fatigueText.rectTransform.anchorMax = new Vector2(1, 0.22f);
            fatigueText.rectTransform.offsetMin = Vector2.zero;
            fatigueText.rectTransform.offsetMax = Vector2.zero;

            // Wire fields via reflection
            SetField(_hudView, "_cashBalanceText", cashText);
            SetField(_hudView, "_netWorthText", netWorthText);
            SetField(_hudView, "_healthBadgeText", healthBadge);
            SetField(_hudView, "_simulationDateText", dateText);
            SetField(_hudView, "_clockSpeedText", speedText);
            SetField(_hudView, "_careerStatusText", careerText);
            SetField(_hudView, "_educationStatusText", eduText);
            SetField(_hudView, "_housingStatusText", housingText);
            SetField(_hudView, "_energyStatusText", energyText);
            SetField(_hudView, "_fatigueStatusText", fatigueText);
        }

        private Text CreateRuntimeText(string name, GameObject parent, string content, int fontSize, Color color, Font font, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent.transform, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.font = font;
            text.alignment = align;
            return text;
        }

        /// <summary>
        /// Displays an in-game recovery dialog when an unhandled startup exception occurs,
        /// ensuring the player is never left staring at an unexplained black screen.
        /// </summary>
        private void ShowStartupRecoveryScreen(Exception ex)
        {
            // Ensure Camera exists
            if (Camera.main == null)
            {
                var camGo = new GameObject("RecoveryCamera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.05f, 0.05f, 1.0f);
            }

            // Ensure EventSystem
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventGo = new GameObject("RecoveryEventSystem");
                eventGo.AddComponent<EventSystem>();
                eventGo.AddComponent<StandaloneInputModule>();
            }

            // Create Emergency Overlay Canvas
            var canvasGo = new GameObject("StartupRecoveryCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Dark background panel
            var panelGo = new GameObject("RecoveryPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelImg = panelGo.GetComponent<Image>();
            panelImg.color = new Color(0.08f, 0.02f, 0.02f, 0.98f);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Error Header
            var header = CreateRuntimeText("Header", panelGo, "RichRich: Startup Notice", 38, new Color(0.95f, 0.35f, 0.35f), font, TextAnchor.MiddleCenter);
            header.fontStyle = FontStyle.Bold;
            header.rectTransform.anchorMin = new Vector2(0.05f, 0.85f);
            header.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            header.rectTransform.offsetMin = Vector2.zero;
            header.rectTransform.offsetMax = Vector2.zero;

            // Explanation
            var explanation = CreateRuntimeText("Explanation", panelGo,
                "The simulation encountered an initialization error during cold boot.\nYour player data is safe. You can retry startup or reset save data below.",
                24, Color.white, font, TextAnchor.MiddleCenter);
            explanation.rectTransform.anchorMin = new Vector2(0.05f, 0.72f);
            explanation.rectTransform.anchorMax = new Vector2(0.95f, 0.84f);
            explanation.rectTransform.offsetMin = Vector2.zero;
            explanation.rectTransform.offsetMax = Vector2.zero;

            // Error Details Card
            var details = CreateRuntimeText("Details", panelGo,
                $"Exception: {ex.GetType().Name}\n{ex.Message}\n\nStack:\n{ex.StackTrace}",
                20, new Color(0.85f, 0.85f, 0.85f), font, TextAnchor.UpperLeft);
            details.rectTransform.anchorMin = new Vector2(0.08f, 0.30f);
            details.rectTransform.anchorMax = new Vector2(0.92f, 0.70f);
            details.rectTransform.offsetMin = Vector2.zero;
            details.rectTransform.offsetMax = Vector2.zero;

            // Reset & Restart Button
            var resetBtnGo = new GameObject("ResetBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            resetBtnGo.transform.SetParent(panelGo.transform, false);
            resetBtnGo.GetComponent<Image>().color = new Color(0.75f, 0.20f, 0.20f);
            var resetRt = resetBtnGo.GetComponent<RectTransform>();
            resetRt.anchorMin = new Vector2(0.15f, 0.16f);
            resetRt.anchorMax = new Vector2(0.85f, 0.25f);
            resetRt.offsetMin = Vector2.zero;
            resetRt.offsetMax = Vector2.zero;

            var resetBtnText = CreateRuntimeText("BtnText", resetBtnGo, "Reset Save Data & Reload", 28, Color.white, font, TextAnchor.MiddleCenter);
            resetBtnText.fontStyle = FontStyle.Bold;
            resetBtnText.rectTransform.anchorMin = Vector2.zero;
            resetBtnText.rectTransform.anchorMax = Vector2.one;
            resetBtnText.rectTransform.offsetMin = Vector2.zero;
            resetBtnText.rectTransform.offsetMax = Vector2.zero;

            var resetBtn = resetBtnGo.GetComponent<Button>();
            resetBtn.onClick.AddListener(() =>
            {
                try
                {
                    _saveService?.DeleteAll();
                }
                catch { }
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });

            // Retry Button
            var retryBtnGo = new GameObject("RetryBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            retryBtnGo.transform.SetParent(panelGo.transform, false);
            retryBtnGo.GetComponent<Image>().color = new Color(0.20f, 0.45f, 0.75f);
            var retryRt = retryBtnGo.GetComponent<RectTransform>();
            retryRt.anchorMin = new Vector2(0.15f, 0.05f);
            retryRt.anchorMax = new Vector2(0.85f, 0.14f);
            retryRt.offsetMin = Vector2.zero;
            retryRt.offsetMax = Vector2.zero;

            var retryBtnText = CreateRuntimeText("BtnText", retryBtnGo, "Retry Normal Startup", 28, Color.white, font, TextAnchor.MiddleCenter);
            retryBtnText.fontStyle = FontStyle.Bold;
            retryBtnText.rectTransform.anchorMin = Vector2.zero;
            retryBtnText.rectTransform.anchorMax = Vector2.one;
            retryBtnText.rectTransform.offsetMin = Vector2.zero;
            retryBtnText.rectTransform.offsetMax = Vector2.zero;

            var retryBtn = retryBtnGo.GetComponent<Button>();
            retryBtn.onClick.AddListener(() =>
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var f = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            f?.SetValue(target, value);
        }

        private void Update()
        {
            if (!IsInitialized) return;
            LifeCoordinator?.Tick(Time.deltaTime);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && IsInitialized)
            {
                LifeCoordinator?.SaveCurrentState();
            }
        }

        private void OnApplicationQuit()
        {
            if (IsInitialized)
            {
                LifeCoordinator?.SaveCurrentState();
            }
        }
    }
}

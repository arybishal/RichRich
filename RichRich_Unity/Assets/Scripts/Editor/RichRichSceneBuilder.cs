using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RichRich.Presentation.Views;

namespace RichRich.Editor
{
    /// <summary>
    /// Deterministic Editor utility for authoring and validating the RichRich MainGameplay scene.
    /// Constructs Main Camera, EventSystem, Screen-Space Canvas, MainHUDView, modal dialogs,
    /// and binds the GameBootstrapper with complete serialized reference wiring.
    /// Prevents black-screen runtime failures caused by empty scenes or missing UI hierarchies.
    /// </summary>
    public static class RichRichSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/MainGameplay.unity";

        private static DefaultControls.Resources _defaultResources;
        private static Font _defaultFont;

        [MenuItem("RichRich/Rebuild MainGameplay Scene")]
        public static void BuildMainScene()
        {
            Debug.Log("[RichRichSceneBuilder] Generating complete MainGameplay scene architecture...");

            // Ensure parent directory exists
            string sceneDir = Path.GetDirectoryName(ScenePath);
            if (!string.IsNullOrEmpty(sceneDir) && !Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }

            // Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Initialize UI Resources
            _defaultResources = new DefaultControls.Resources();
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_defaultFont == null)
            {
                _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // 2. Main Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10f);
            var camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 1.0f); // #0A0F1A Dark Slate / FinTech background
            camera.orthographic = true;
            camera.orthographicSize = 5.0f;
            camera.depth = -1;
            camera.cullingMask = ~0; // Everything

            // 3. EventSystem
            var eventGo = new GameObject("EventSystem");
            eventGo.AddComponent<EventSystem>();
            eventGo.AddComponent<StandaloneInputModule>();

            // 4. Screen-Space Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // 9:16 Portrait baseline
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // 5. Safe Area Container
            var safeAreaGo = CreateUIObject("SafeArea", canvasGo);
            StretchFull(safeAreaGo);
            var safeRect = safeAreaGo.GetComponent<RectTransform>();
            safeRect.offsetMin = new Vector2(24, 48); // Left, Bottom
            safeRect.offsetMax = new Vector2(-24, -48); // Right, Top

            // 6. Main HUD View
            var hudGo = CreateUIObject("MainHUD", safeAreaGo);
            StretchFull(hudGo);
            var hudView = hudGo.AddComponent<MainHUDView>();

            // Scrollable Content Container for HUD
            var scrollRoot = CreateUIObject("ScrollContent", hudGo);
            StretchFull(scrollRoot);

            // --- HUD Top Header Bar ---
            var headerGo = CreatePanel("HeaderBar", scrollRoot, new Color(0.08f, 0.12f, 0.20f, 0.95f));
            SetRect(headerGo, 0, 1, 1, 1, 0, -140, 0, 0);

            var titleText = CreateText("AppTitle", headerGo, "RichRich: Life & Enterprise", 36, new Color(0.98f, 0.75f, 0.18f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(titleText.gameObject, 0, 0.5f, 0.6f, 1, 20, 0, 0, 0);

            var dateText = CreateText("DateText", headerGo, "Year 1, Month 1 - Day 1", 26, new Color(0.70f, 0.78f, 0.90f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(dateText.gameObject, 0, 0, 0.6f, 0.5f, 20, 0, 0, 0);

            var speedText = CreateText("SpeedText", headerGo, "1x (Normal)", 24, new Color(0.35f, 0.80f, 0.98f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(speedText.gameObject, 0.6f, 0.5f, 1, 1, 0, 0, -20, 0);

            // Clock Controls
            var pauseBtn = CreateButton("PauseBtn", headerGo, "Pause", new Color(0.20f, 0.28f, 0.40f), Color.white);
            SetRect(pauseBtn.gameObject, 0.60f, 0.05f, 0.72f, 0.45f, 0, 0, -4, 0);

            var normBtn = CreateButton("NormalSpeedBtn", headerGo, "1x", new Color(0.20f, 0.28f, 0.40f), Color.white);
            SetRect(normBtn.gameObject, 0.73f, 0.05f, 0.81f, 0.45f, 0, 0, -4, 0);

            var fastBtn = CreateButton("FastSpeedBtn", headerGo, "5x", new Color(0.20f, 0.28f, 0.40f), Color.white);
            SetRect(fastBtn.gameObject, 0.82f, 0.05f, 0.90f, 0.45f, 0, 0, -4, 0);

            var sleepBtn = CreateButton("SleepBtn", headerGo, "Sleep", new Color(0.35f, 0.25f, 0.55f), Color.white);
            SetRect(sleepBtn.gameObject, 0.91f, 0.05f, 0.99f, 0.45f, 0, 0, -4, 0);

            // --- HUD Financial Metrics Card ---
            var financeCard = CreatePanel("FinanceCard", scrollRoot, new Color(0.09f, 0.14f, 0.24f, 0.90f));
            SetRect(financeCard, 0, 1, 1, 1, 0, -360, 0, -155);

            var cashLabel = CreateText("CashLabel", financeCard, "Liquid Cash Balance", 24, new Color(0.58f, 0.65f, 0.78f), FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(cashLabel.gameObject, 0, 0.70f, 0.5f, 1, 24, 0, 0, -12);

            var cashText = CreateText("CashText", financeCard, "$3,000.00", 52, new Color(0.18f, 0.85f, 0.45f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(cashText.gameObject, 0, 0.35f, 0.6f, 0.75f, 24, 0, 0, 0);

            var netWorthText = CreateText("NetWorthText", financeCard, "Net Worth: $3,000.00", 28, Color.white, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(netWorthText.gameObject, 0.5f, 0.55f, 1, 0.85f, 0, 0, -24, 0);

            var healthBadge = CreateText("HealthBadge", financeCard, "Status: Solvent", 26, new Color(0.40f, 0.92f, 0.60f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(healthBadge.gameObject, 0.5f, 0.35f, 1, 0.55f, 0, 0, -24, 0);

            var incomeText = CreateText("IncomeText", financeCard, "+$0.00/mo", 24, new Color(0.30f, 0.85f, 0.50f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(incomeText.gameObject, 0, 0.05f, 0.33f, 0.32f, 24, 0, 0, 0);

            var expenseText = CreateText("ExpenseText", financeCard, "-$0.00/mo", 24, new Color(0.95f, 0.40f, 0.40f), FontStyle.Normal, TextAnchor.MiddleCenter);
            SetRect(expenseText.gameObject, 0.33f, 0.05f, 0.66f, 0.32f, 0, 0, 0, 0);

            var flowText = CreateText("FlowText", financeCard, "Cash Flow: +$0.00/mo", 24, new Color(0.40f, 0.75f, 0.98f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(flowText.gameObject, 0.66f, 0.05f, 1, 0.32f, 0, 0, -24, 0);

            // --- HUD Lifestyle & Career Status Card ---
            var statusCard = CreatePanel("StatusCard", scrollRoot, new Color(0.09f, 0.14f, 0.24f, 0.90f));
            SetRect(statusCard, 0, 1, 1, 1, 0, -560, 0, -375);

            var careerText = CreateText("CareerText", statusCard, "Career: Unemployed", 26, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(careerText.gameObject, 0, 0.68f, 0.5f, 0.98f, 24, 0, 0, 0);

            var eduText = CreateText("EducationText", statusCard, "Study: None", 26, new Color(0.80f, 0.85f, 0.95f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(eduText.gameObject, 0, 0.36f, 0.5f, 0.66f, 24, 0, 0, 0);

            var housingText = CreateText("HousingText", statusCard, "Housing: Homeless", 26, new Color(0.95f, 0.80f, 0.35f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(housingText.gameObject, 0, 0.04f, 0.5f, 0.34f, 24, 0, 0, 0);

            var energyText = CreateText("EnergyText", statusCard, "Energy: 100/100", 26, new Color(0.35f, 0.80f, 0.98f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(energyText.gameObject, 0.5f, 0.52f, 1, 0.98f, 0, 0, -24, 0);

            var fatigueText = CreateText("FatigueText", statusCard, "Fatigue: Well-Rested", 26, new Color(0.40f, 0.90f, 0.60f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(fatigueText.gameObject, 0.5f, 0.04f, 1, 0.50f, 0, 0, -24, 0);

            // --- Navigation Buttons Panel (Grid of Subsystem Modals) ---
            var navPanel = CreatePanel("NavGridPanel", scrollRoot, new Color(0.07f, 0.10f, 0.18f, 0.85f));
            SetRect(navPanel, 0, 0, 1, 1, 0, 0, 0, -575);

            var gridTitle = CreateText("GridTitle", navPanel, "Simulated Subsystems & Enterprise Portals", 26, new Color(0.60f, 0.70f, 0.85f), FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(gridTitle.gameObject, 0, 1, 1, 1, 24, -40, -24, -10);

            // Navigation Action Buttons
            var jobBtn = CreateNavButton("NavJobBtn", navPanel, "Career Board", 0, 1, 0.48f, 1, 10, -110, -5, -55);
            var eduBtn = CreateNavButton("NavEduBtn", navPanel, "Education & Degrees", 0.52f, 1, 1, 1, 5, -110, -10, -55);

            var houseBtn = CreateNavButton("NavHousingBtn", navPanel, "Housing & Leases", 0, 1, 0.48f, 1, 10, -175, -5, -120);
            var lifeBtn = CreateNavButton("NavLifeBtn", navPanel, "Lifestyle & Rest", 0.52f, 1, 1, 1, 5, -175, -10, -120);

            var mktBtn = CreateNavButton("NavMarketBtn", navPanel, "Enterprise Market", 0, 1, 0.48f, 1, 10, -240, -5, -185);
            var dashBtn = CreateNavButton("NavDashBtn", navPanel, "Business Dashboard", 0.52f, 1, 1, 1, 5, -240, -10, -185);

            var autoBtn = CreateNavButton("NavAutoBtn", navPanel, "Auto Repair Workshop", 0, 1, 0.48f, 1, 10, -305, -5, -250);
            var storeBtn = CreateNavButton("NavStoreBtn", navPanel, "Corner Convenience", 0.52f, 1, 1, 1, 5, -305, -10, -250);

            var agencyBtn = CreateNavButton("NavAgencyBtn", navPanel, "Digital Agency", 0, 1, 0.48f, 1, 10, -370, -5, -315);
            var staffBtn = CreateNavButton("NavStaffBtn", navPanel, "Recruitment & HR", 0.52f, 1, 1, 1, 5, -370, -10, -315);

            var finBtn = CreateNavButton("NavFinanceBtn", navPanel, "Commercial Debt & P&L", 0, 1, 0.48f, 1, 10, -435, -5, -380);
            var setBtn = CreateNavButton("NavSettingsBtn", navPanel, "Settings & Persistence", 0.52f, 1, 1, 1, 5, -435, -10, -380);

            // Wire MainHUDView serialized fields
            SetPrivateField(hudView, "_cashBalanceText", cashText);
            SetPrivateField(hudView, "_netWorthText", netWorthText);
            SetPrivateField(hudView, "_monthlyIncomeText", incomeText);
            SetPrivateField(hudView, "_monthlyExpensesText", expenseText);
            SetPrivateField(hudView, "_netCashFlowText", flowText);
            SetPrivateField(hudView, "_healthBadgeText", healthBadge);
            SetPrivateField(hudView, "_simulationDateText", dateText);
            SetPrivateField(hudView, "_clockSpeedText", speedText);
            SetPrivateField(hudView, "_pauseToggleButton", pauseBtn);
            SetPrivateField(hudView, "_normalSpeedButton", normBtn);
            SetPrivateField(hudView, "_fastSpeedButton", fastBtn);
            SetPrivateField(hudView, "_sleepButton", sleepBtn);
            SetPrivateField(hudView, "_careerStatusText", careerText);
            SetPrivateField(hudView, "_educationStatusText", eduText);
            SetPrivateField(hudView, "_housingStatusText", housingText);
            SetPrivateField(hudView, "_energyStatusText", energyText);
            SetPrivateField(hudView, "_fatigueStatusText", fatigueText);

            // 7. Modals Root Container
            var modalsRoot = CreateUIObject("Modals", canvasGo);
            StretchFull(modalsRoot);

            // --- 7.1 Job Board Modal ---
            var jobModal = CreateModalSheet("JobBoardModal", modalsRoot, "Career Board & Vacancies", out var jobCloseBtn);
            var jobView = jobModal.AddComponent<JobBoardView>();
            BuildJobBoardUI(jobModal, jobView);

            // --- 7.2 Education Catalogue Modal ---
            var eduModal = CreateModalSheet("EducationModal", modalsRoot, "Higher Education & Degrees", out var eduCloseBtn);
            var eduView = eduModal.AddComponent<EducationCatalogueView>();
            BuildEducationUI(eduModal, eduView);

            // --- 7.3 Housing Catalogue Modal ---
            var housingModal = CreateModalSheet("HousingModal", modalsRoot, "Housing & Accommodation Leases", out var housingCloseBtn);
            var housingView = housingModal.AddComponent<HousingCatalogueView>();
            BuildHousingUI(housingModal, housingView);

            // --- 7.4 Lifestyle Status Modal ---
            var lifeModal = CreateModalSheet("LifestyleModal", modalsRoot, "Lifestyle & Energy Recovery", out var lifeCloseBtn);
            var lifeView = lifeModal.AddComponent<LifestyleStatusView>();
            BuildLifestyleUI(lifeModal, lifeView);

            // --- 7.5 Commercial Marketplace Modal ---
            var mktModal = CreateModalSheet("MarketplaceModal", modalsRoot, "Commercial Business Marketplace", out var mktCloseBtn);
            var mktView = mktModal.AddComponent<BusinessMarketplaceView>();
            BuildMarketplaceUI(mktModal, mktView);

            // --- 7.6 Business Dashboard Modal ---
            var dashModal = CreateModalSheet("DashboardModal", modalsRoot, "Commercial Enterprise Dashboard", out var dashCloseBtn);
            var dashView = dashModal.AddComponent<BusinessDashboardView>();
            BuildDashboardUI(dashModal, dashView);

            // --- 7.7 Auto Repair Workshop Modal ---
            var autoModal = CreateModalSheet("AutoRepairModal", modalsRoot, "Auto Repair Workshop Service Bays", out var autoCloseBtn);
            var autoView = autoModal.AddComponent<AutoRepairView>();
            BuildAutoRepairUI(autoModal, autoView);

            // --- 7.8 Convenience Store Modal ---
            var storeModal = CreateModalSheet("ConvenienceStoreModal", modalsRoot, "Corner Convenience Store Retail", out var storeCloseBtn);
            var storeView = storeModal.AddComponent<ConvenienceStoreView>();
            BuildConvenienceStoreUI(storeModal, storeView);

            // --- 7.9 Digital Agency Modal ---
            var agencyModal = CreateModalSheet("DigitalAgencyModal", modalsRoot, "Digital Marketing Agency Contracts", out var agencyCloseBtn);
            var agencyView = agencyModal.AddComponent<DigitalAgencyView>();
            BuildDigitalAgencyUI(agencyModal, agencyView);

            // --- 7.10 Staffing & Recruitment Modal ---
            var staffModal = CreateModalSheet("StaffModal", modalsRoot, "Recruitment & Staffing Management", out var staffCloseBtn);
            var staffView = staffModal.AddComponent<BusinessEmployeeView>();
            BuildEmployeeUI(staffModal, staffView);

            // --- 7.11 Business Finance & Debt Modal ---
            var finModal = CreateModalSheet("FinanceModal", modalsRoot, "Enterprise Finance & Commercial Credit", out var finCloseBtn);
            var finView = finModal.AddComponent<BusinessFinanceView>();
            BuildFinanceUI(finModal, finView);

            // --- 7.12 Settings Modal ---
            var setModal = CreateModalSheet("SettingsModal", modalsRoot, "Settings & Persistence", out var setCloseBtn);
            var setView = setModal.AddComponent<SettingsModalView>();
            BuildSettingsUI(setModal, setView, setCloseBtn);

            // Wire HUD Nav buttons to Modal roots
            SetPrivateField(hudView, "_jobBoardOpenButton", jobBtn);
            SetPrivateField(hudView, "_jobBoardModalRoot", jobModal);

            SetPrivateField(hudView, "_educationOpenButton", eduBtn);
            SetPrivateField(hudView, "_educationModalRoot", eduModal);

            SetPrivateField(hudView, "_housingOpenButton", houseBtn);
            SetPrivateField(hudView, "_housingModalRoot", housingModal);

            SetPrivateField(hudView, "_lifestyleOpenButton", lifeBtn);
            SetPrivateField(hudView, "_lifestyleModalRoot", lifeModal);

            SetPrivateField(hudView, "_businessMarketplaceOpenButton", mktBtn);
            SetPrivateField(hudView, "_businessMarketplaceModalRoot", mktModal);

            SetPrivateField(hudView, "_businessDashboardOpenButton", dashBtn);
            SetPrivateField(hudView, "_businessDashboardModalRoot", dashModal);

            SetPrivateField(hudView, "_autoRepairOpenButton", autoBtn);
            SetPrivateField(hudView, "_autoRepairModalRoot", autoModal);

            SetPrivateField(hudView, "_convenienceStoreOpenButton", storeBtn);
            SetPrivateField(hudView, "_convenienceStoreModalRoot", storeModal);

            SetPrivateField(hudView, "_digitalAgencyOpenButton", agencyBtn);
            SetPrivateField(hudView, "_digitalAgencyModalRoot", agencyModal);

            SetPrivateField(hudView, "_businessEmployeeOpenButton", staffBtn);
            SetPrivateField(hudView, "_businessEmployeeModalRoot", staffModal);

            SetPrivateField(hudView, "_businessFinanceOpenButton", finBtn);
            SetPrivateField(hudView, "_businessFinanceModalRoot", finModal);

            SetPrivateField(hudView, "_settingsOpenButton", setBtn);
            SetPrivateField(hudView, "_settingsModalRoot", setModal);

            // 8. Attach and Wire GameBootstrapper
            var bootstrapperGo = new GameObject("GameBootstrapper");
            var bootstrapper = bootstrapperGo.AddComponent<GameBootstrapper>();

            SetPrivateField(bootstrapper, "_hudView", hudView);
            SetPrivateField(bootstrapper, "_jobBoardView", jobView);
            SetPrivateField(bootstrapper, "_educationView", eduView);
            SetPrivateField(bootstrapper, "_housingView", housingView);
            SetPrivateField(bootstrapper, "_lifestyleView", lifeView);
            SetPrivateField(bootstrapper, "_marketplaceView", mktView);
            SetPrivateField(bootstrapper, "_dashboardView", dashView);
            SetPrivateField(bootstrapper, "_autoRepairView", autoView);
            SetPrivateField(bootstrapper, "_convenienceStoreView", storeView);
            SetPrivateField(bootstrapper, "_digitalAgencyView", agencyView);
            SetPrivateField(bootstrapper, "_employeeView", staffView);
            SetPrivateField(bootstrapper, "_financeView", finView);
            SetPrivateField(bootstrapper, "_settingsView", setView);

            // Mark objects dirty for serializer
            EditorUtility.SetDirty(bootstrapper);
            EditorUtility.SetDirty(hudView);
            EditorUtility.SetDirty(jobView);
            EditorUtility.SetDirty(eduView);
            EditorUtility.SetDirty(housingView);
            EditorUtility.SetDirty(lifeView);
            EditorUtility.SetDirty(mktView);
            EditorUtility.SetDirty(dashView);
            EditorUtility.SetDirty(autoView);
            EditorUtility.SetDirty(storeView);
            EditorUtility.SetDirty(agencyView);
            EditorUtility.SetDirty(staffView);
            EditorUtility.SetDirty(finView);
            EditorUtility.SetDirty(setView);

            // Save Scene
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (saved)
            {
                Debug.Log($"[RichRichSceneBuilder] SUCCESS! MainGameplay scene saved cleanly at '{ScenePath}'.");
            }
            else
            {
                Debug.LogError($"[RichRichSceneBuilder] FAILED to save scene at '{ScenePath}'.");
            }
        }

        #region Modal Builders

        private static void BuildJobBoardUI(GameObject root, JobBoardView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var roleText = CreateText("RoleText", content, "Role: Unemployed", 26, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(roleText.gameObject, 0, 0.88f, 0.5f, 0.96f, 20, 0, 0, 0);

            var compText = CreateText("CompText", content, "Company: None", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(compText.gameObject, 0, 0.80f, 0.5f, 0.88f, 20, 0, 0, 0);

            var salaryText = CreateText("SalaryText", content, "Salary: $0.00/mo", 24, new Color(0.3f, 0.85f, 0.45f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(salaryText.gameObject, 0, 0.72f, 0.5f, 0.80f, 20, 0, 0, 0);

            var tenureText = CreateText("TenureText", content, "Tenure: 0 mo | Total: 0 mo", 22, new Color(0.6f, 0.65f, 0.75f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(tenureText.gameObject, 0.5f, 0.88f, 1, 0.96f, 0, 0, -20, 0);

            var trackExpText = CreateText("TrackExpText", content, "Retail: 0 mo | Tech: 0 mo | Biz: 0 mo", 22, new Color(0.6f, 0.65f, 0.75f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(trackExpText.gameObject, 0.5f, 0.78f, 1, 0.86f, 0, 0, -20, 0);

            // Filter Buttons
            var fltAll = CreateButton("FltAll", content, "All", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(fltAll.gameObject, 0.05f, 0.62f, 0.25f, 0.70f, 0, 0, 0, 0);
            var fltRet = CreateButton("FltRet", content, "Retail", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(fltRet.gameObject, 0.28f, 0.62f, 0.48f, 0.70f, 0, 0, 0, 0);
            var fltTech = CreateButton("FltTech", content, "Tech", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(fltTech.gameObject, 0.52f, 0.62f, 0.72f, 0.70f, 0, 0, 0, 0);
            var fltBiz = CreateButton("FltBiz", content, "Business", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(fltBiz.gameObject, 0.75f, 0.62f, 0.95f, 0.70f, 0, 0, 0, 0);

            // Selected Job Card
            var selTitle = CreateText("SelTitle", content, "Sales Associate", 30, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(selTitle.gameObject, 0, 0.52f, 0.6f, 0.60f, 20, 0, 0, 0);
            var selComp = CreateText("SelComp", content, "Apex Retail Group", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(selComp.gameObject, 0, 0.44f, 0.6f, 0.52f, 20, 0, 0, 0);
            var selSal = CreateText("SelSalary", content, "$2,200.00/mo", 28, new Color(0.2f, 0.85f, 0.45f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(selSal.gameObject, 0.6f, 0.52f, 1, 0.60f, 0, 0, -20, 0);
            var selHours = CreateText("SelHours", content, "40 hrs/week (Tier 1)", 22, new Color(0.6f, 0.65f, 0.75f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(selHours.gameObject, 0.6f, 0.44f, 1, 0.52f, 0, 0, -20, 0);
            var selReq = CreateText("SelReq", content, "No degree required | 0 mo experience", 22, new Color(0.8f, 0.85f, 0.9f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(selReq.gameObject, 0, 0.36f, 1, 0.44f, 20, 0, -20, 0);
            var selDesc = CreateText("SelDesc", content, "Assist store visitors and handle cashier duties.", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(selDesc.gameObject, 0, 0.24f, 1, 0.36f, 20, 0, -20, 0);

            // Actions
            var applyBtn = CreateButton("ApplyBtn", content, "Submit Application", new Color(0.12f, 0.65f, 0.35f), Color.white);
            SetRect(applyBtn.gameObject, 0.05f, 0.12f, 0.48f, 0.22f, 0, 0, 0, 0);
            var promoBtn = CreateButton("PromoBtn", content, "Apply Promotion", new Color(0.18f, 0.45f, 0.75f), Color.white);
            SetRect(promoBtn.gameObject, 0.52f, 0.12f, 0.95f, 0.22f, 0, 0, 0, 0);
            var resignBtn = CreateButton("ResignBtn", content, "Resign Job", new Color(0.75f, 0.25f, 0.25f), Color.white);
            SetRect(resignBtn.gameObject, 0.05f, 0.03f, 0.35f, 0.10f, 0, 0, 0, 0);

            var feedback = CreateText("Feedback", content, "", 22, new Color(0.95f, 0.85f, 0.4f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(feedback.gameObject, 0.38f, 0.03f, 0.95f, 0.10f, 0, 0, 0, 0);

            SetPrivateField(view, "_currentRoleText", roleText);
            SetPrivateField(view, "_currentCompanyText", compText);
            SetPrivateField(view, "_currentSalaryText", salaryText);
            SetPrivateField(view, "_careerTenureText", tenureText);
            SetPrivateField(view, "_trackExperienceSummaryText", trackExpText);
            SetPrivateField(view, "_filterAllButton", fltAll);
            SetPrivateField(view, "_filterRetailButton", fltRet);
            SetPrivateField(view, "_filterTechButton", fltTech);
            SetPrivateField(view, "_filterBusinessButton", fltBiz);
            SetPrivateField(view, "_selectedJobTitleText", selTitle);
            SetPrivateField(view, "_selectedJobCompanyText", selComp);
            SetPrivateField(view, "_selectedJobSalaryText", selSal);
            SetPrivateField(view, "_selectedJobHoursText", selHours);
            SetPrivateField(view, "_selectedJobRequirementsText", selReq);
            SetPrivateField(view, "_selectedJobDescriptionText", selDesc);
            SetPrivateField(view, "_applyButton", applyBtn);
            SetPrivateField(view, "_promotionButton", promoBtn);
            SetPrivateField(view, "_resignButton", resignBtn);
            SetPrivateField(view, "_applicationFeedbackText", feedback);
        }

        private static void BuildEducationUI(GameObject root, EducationCatalogueView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var courseText = CreateText("ActiveCourse", content, "Active Study: None", 28, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(courseText.gameObject, 0, 0.85f, 0.6f, 0.95f, 20, 0, 0, 0);

            var pctText = CreateText("ProgressPct", content, "0%", 28, new Color(0.35f, 0.8f, 0.98f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(pctText.gameObject, 0.6f, 0.85f, 1, 0.95f, 0, 0, -20, 0);

            var sliderGo = DefaultControls.CreateSlider(_defaultResources);
            sliderGo.transform.SetParent(content.transform, false);
            var slider = sliderGo.GetComponent<Slider>();
            SetRect(sliderGo, 0.05f, 0.78f, 0.95f, 0.84f, 0, 0, 0, 0);

            var hoursText = CreateText("HoursText", content, "Hours: 0.0 / 0.0 hrs", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(hoursText.gameObject, 0, 0.70f, 0.5f, 0.78f, 20, 0, 0, 0);

            var paceText = CreateText("PaceText", content, "Pace: PartTime", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(paceText.gameObject, 0.5f, 0.70f, 1, 0.78f, 0, 0, -20, 0);

            var ptBtn = CreateButton("PartTimeBtn", content, "Part-Time", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(ptBtn.gameObject, 0.1f, 0.60f, 0.45f, 0.68f, 0, 0, 0, 0);
            var ftBtn = CreateButton("FullTimeBtn", content, "Full-Time", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(ftBtn.gameObject, 0.55f, 0.60f, 0.9f, 0.68f, 0, 0, 0, 0);

            var selTitle = CreateText("SelTitle", content, "Business Administration Diploma", 28, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(selTitle.gameObject, 0, 0.48f, 1, 0.58f, 20, 0, -20, 0);
            var selTuition = CreateText("SelTuition", content, "Tuition: $4,500.00", 24, new Color(0.2f, 0.85f, 0.45f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(selTuition.gameObject, 0, 0.40f, 0.5f, 0.48f, 20, 0, 0, 0);
            var selDur = CreateText("SelDuration", content, "Duration: 6 Months (360 hrs)", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(selDur.gameObject, 0.5f, 0.40f, 1, 0.48f, 0, 0, -20, 0);
            var selPre = CreateText("SelPre", content, "Prerequisites: High School Diploma", 22, new Color(0.8f, 0.85f, 0.9f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(selPre.gameObject, 0, 0.32f, 1, 0.40f, 20, 0, -20, 0);
            var selDesc = CreateText("SelDesc", content, "Fundamental business management, financial accounting, and operations.", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(selDesc.gameObject, 0, 0.20f, 1, 0.32f, 20, 0, -20, 0);

            var enrollBtn = CreateButton("EnrollBtn", content, "Enroll in Program", new Color(0.12f, 0.65f, 0.35f), Color.white);
            SetRect(enrollBtn.gameObject, 0.1f, 0.08f, 0.5f, 0.18f, 0, 0, 0, 0);
            var dropBtn = CreateButton("DropBtn", content, "Drop Course", new Color(0.75f, 0.25f, 0.25f), Color.white);
            SetRect(dropBtn.gameObject, 0.55f, 0.08f, 0.9f, 0.18f, 0, 0, 0, 0);

            var feedback = CreateText("Feedback", content, "", 22, new Color(0.95f, 0.85f, 0.4f), FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(feedback.gameObject, 0, 0.01f, 1, 0.07f, 20, 0, -20, 0);

            SetPrivateField(view, "_activeCourseTitleText", courseText);
            SetPrivateField(view, "_activeProgressPercentText", pctText);
            SetPrivateField(view, "_activeHoursText", hoursText);
            SetPrivateField(view, "_activeProgressBar", slider);
            SetPrivateField(view, "_currentPaceText", paceText);
            SetPrivateField(view, "_partTimeButton", ptBtn);
            SetPrivateField(view, "_fullTimeButton", ftBtn);
            SetPrivateField(view, "_selectedCourseTitleText", selTitle);
            SetPrivateField(view, "_selectedTuitionText", selTuition);
            SetPrivateField(view, "_selectedDurationText", selDur);
            SetPrivateField(view, "_selectedPrerequisitesText", selPre);
            SetPrivateField(view, "_selectedDescriptionText", selDesc);
            SetPrivateField(view, "_enrollButton", enrollBtn);
            SetPrivateField(view, "_dropButton", dropBtn);
            SetPrivateField(view, "_feedbackText", feedback);
        }

        private static void BuildHousingUI(GameObject root, HousingCatalogueView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var currName = CreateText("CurrHousing", content, "Current: Studio Apartment", 28, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(currName.gameObject, 0, 0.88f, 0.6f, 0.96f, 20, 0, 0, 0);
            var currRent = CreateText("CurrRent", content, "Rent: $600.00/mo", 24, new Color(0.95f, 0.4f, 0.4f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(currRent.gameObject, 0.6f, 0.88f, 1, 0.96f, 0, 0, -20, 0);
            var currDep = CreateText("CurrDeposit", content, "Deposit: $600.00", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(currDep.gameObject, 0, 0.80f, 0.5f, 0.88f, 20, 0, 0, 0);
            var currComfort = CreateText("CurrComfort", content, "Comfort: 50/100", 22, new Color(0.95f, 0.8f, 0.2f), FontStyle.Normal, TextAnchor.MiddleCenter);
            SetRect(currComfort.gameObject, 0.35f, 0.80f, 0.65f, 0.88f, 0, 0, 0, 0);
            var currRec = CreateText("CurrRec", content, "+65 Energy/night", 22, new Color(0.35f, 0.8f, 0.98f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(currRec.gameObject, 0.65f, 0.80f, 1, 0.88f, 0, 0, -20, 0);

            var selTier = CreateText("SelTier", content, "One-Bedroom Apartment", 30, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(selTier.gameObject, 0, 0.65f, 1, 0.75f, 20, 0, -20, 0);
            var selRent = CreateText("SelRent", content, "Rent: $1,200.00/mo", 26, new Color(0.95f, 0.4f, 0.4f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(selRent.gameObject, 0, 0.57f, 0.5f, 0.65f, 20, 0, 0, 0);
            var selDep = CreateText("SelDep", content, "Move-in Deposit: $1,200.00", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(selDep.gameObject, 0.5f, 0.57f, 1, 0.65f, 0, 0, -20, 0);
            var selComfort = CreateText("SelComfort", content, "Comfort Rating: 70/100", 24, new Color(0.95f, 0.8f, 0.2f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(selComfort.gameObject, 0, 0.49f, 0.5f, 0.57f, 20, 0, 0, 0);
            var selRec = CreateText("SelRec", content, "+80 Energy/night", 24, new Color(0.35f, 0.8f, 0.98f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(selRec.gameObject, 0.5f, 0.49f, 1, 0.57f, 0, 0, -20, 0);
            var selReq = CreateText("SelReq", content, "Tenant Requirements: 2.5x Monthly Income", 22, new Color(0.8f, 0.85f, 0.9f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(selReq.gameObject, 0, 0.41f, 1, 0.49f, 20, 0, -20, 0);
            var selDesc = CreateText("SelDesc", content, "Spacious modern accommodation with quiet surroundings and high-speed internet.", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(selDesc.gameObject, 0, 0.27f, 1, 0.41f, 20, 0, -20, 0);

            var moveBtn = CreateButton("MoveBtn", content, "Sign Lease & Move In", new Color(0.12f, 0.65f, 0.35f), Color.white);
            SetRect(moveBtn.gameObject, 0.15f, 0.12f, 0.85f, 0.22f, 0, 0, 0, 0);
            var feedback = CreateText("Feedback", content, "", 22, new Color(0.95f, 0.85f, 0.4f), FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(feedback.gameObject, 0, 0.02f, 1, 0.10f, 20, 0, -20, 0);

            SetPrivateField(view, "_currentHousingNameText", currName);
            SetPrivateField(view, "_currentRentText", currRent);
            SetPrivateField(view, "_currentDepositText", currDep);
            SetPrivateField(view, "_currentComfortText", currComfort);
            SetPrivateField(view, "_currentRecoveryText", currRec);
            SetPrivateField(view, "_selectedTierNameText", selTier);
            SetPrivateField(view, "_selectedRentText", selRent);
            SetPrivateField(view, "_selectedDepositText", selDep);
            SetPrivateField(view, "_selectedComfortText", selComfort);
            SetPrivateField(view, "_selectedRecoveryText", selRec);
            SetPrivateField(view, "_selectedRequirementsText", selReq);
            SetPrivateField(view, "_selectedDescriptionText", selDesc);
            SetPrivateField(view, "_moveToSelectedButton", moveBtn);
            SetPrivateField(view, "_housingFeedbackText", feedback);
        }

        private static void BuildLifestyleUI(GameObject root, LifestyleStatusView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var sliderGo = DefaultControls.CreateSlider(_defaultResources);
            sliderGo.transform.SetParent(content.transform, false);
            var slider = sliderGo.GetComponent<Slider>();
            SetRect(sliderGo, 0.05f, 0.80f, 0.95f, 0.88f, 0, 0, 0, 0);

            var energyVal = CreateText("EnergyVal", content, "100 / 100 Energy", 28, new Color(0.35f, 0.8f, 0.98f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(energyVal.gameObject, 0, 0.70f, 0.5f, 0.80f, 20, 0, 0, 0);
            var fatigueBadge = CreateText("FatigueBadge", content, "Status: Well-Rested", 28, new Color(0.4f, 0.9f, 0.6f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(fatigueBadge.gameObject, 0.5f, 0.70f, 1, 0.80f, 0, 0, -20, 0);

            var houseTier = CreateText("HouseTier", content, "Residence: Studio Apartment (Comfort 50/100)", 26, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(houseTier.gameObject, 0, 0.55f, 1, 0.65f, 20, 0, -20, 0);
            var overnight = CreateText("Overnight", content, "+65 energy / night", 26, new Color(0.35f, 0.8f, 0.98f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(overnight.gameObject, 0, 0.45f, 1, 0.55f, 20, 0, -20, 0);

            var schedule = CreateText("Schedule", content, "Work: 0 hrs/wk | Study: 0 hrs/wk | Free: 168 hrs/wk", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(schedule.gameObject, 0, 0.32f, 1, 0.42f, 20, 0, -20, 0);

            var restBtn = CreateButton("RestBtn", content, "Take Rest Break (+20 Energy)", new Color(0.2f, 0.55f, 0.75f), Color.white);
            SetRect(restBtn.gameObject, 0.1f, 0.18f, 0.9f, 0.28f, 0, 0, 0, 0);
            var sleepBtn = CreateButton("SleepBtn", content, "Sleep to Morning (+Full Overnight)", new Color(0.35f, 0.25f, 0.55f), Color.white);
            SetRect(sleepBtn.gameObject, 0.1f, 0.05f, 0.9f, 0.15f, 0, 0, 0, 0);

            SetPrivateField(view, "_energyBar", slider);
            SetPrivateField(view, "_energyValueText", energyVal);
            SetPrivateField(view, "_fatigueBadgeText", fatigueBadge);
            SetPrivateField(view, "_housingTierText", houseTier);
            SetPrivateField(view, "_overnightRecoveryText", overnight);
            SetPrivateField(view, "_weeklyScheduleText", schedule);
            SetPrivateField(view, "_restBreakButton", restBtn);
            SetPrivateField(view, "_sleepToMorningButton", sleepBtn);
        }

        private static void BuildMarketplaceUI(GameObject root, BusinessMarketplaceView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var cashText = CreateText("AvailableCash", content, "Liquid Cash: $3,000.00", 28, new Color(0.2f, 0.85f, 0.45f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(cashText.gameObject, 0, 0.88f, 0.6f, 0.96f, 20, 0, 0, 0);
            var statusText = CreateText("MarketStatus", content, "3 commercial opportunities available.", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(statusText.gameObject, 0.6f, 0.88f, 1, 0.96f, 0, 0, -20, 0);

            var btnAuto = CreateButton("SelectAuto", content, "Auto Repair ($5,999)", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(btnAuto.gameObject, 0.05f, 0.78f, 0.33f, 0.86f, 0, 0, 0, 0);
            var btnStore = CreateButton("SelectStore", content, "Corner Store ($13,700)", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(btnStore.gameObject, 0.36f, 0.78f, 0.64f, 0.86f, 0, 0, 0, 0);
            var btnAgency = CreateButton("SelectAgency", content, "Digital Agency ($27,500)", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(btnAgency.gameObject, 0.67f, 0.78f, 0.95f, 0.86f, 0, 0, 0, 0);

            var nameText = CreateText("BizName", content, "Auto Repair Workshop", 32, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(nameText.gameObject, 0, 0.65f, 1, 0.75f, 20, 0, -20, 0);
            var catText = CreateText("BizCat", content, "Industry: Automotive Services", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(catText.gameObject, 0, 0.58f, 0.5f, 0.65f, 20, 0, 0, 0);
            var baseCost = CreateText("BaseCost", content, "Base Acquisition: $5,499.00", 24, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(baseCost.gameObject, 0, 0.50f, 0.5f, 0.58f, 20, 0, 0, 0);
            var setupCost = CreateText("SetupCost", content, "Registration & Setup: $500.00", 24, Color.white, FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(setupCost.gameObject, 0.5f, 0.50f, 1, 0.58f, 0, 0, -20, 0);
            var totalCost = CreateText("TotalCost", content, "Total Capital Required: $5,999.00", 26, new Color(0.95f, 0.4f, 0.4f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(totalCost.gameObject, 0, 0.42f, 1, 0.50f, 20, 0, -20, 0);
            var overhead = CreateText("Overhead", content, "Daily Overhead: $120.00/day", 24, new Color(0.85f, 0.5f, 0.5f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(overhead.gameObject, 0, 0.34f, 0.5f, 0.42f, 20, 0, 0, 0);
            var yieldText = CreateText("Yield", content, "Starting Base Yield: $120.00/hr", 24, new Color(0.2f, 0.85f, 0.45f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(yieldText.gameObject, 0.5f, 0.34f, 1, 0.42f, 0, 0, -20, 0);
            var desc = CreateText("Desc", content, "Full service garage for vehicle diagnostics, tune-ups, and component overhauls.", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(desc.gameObject, 0, 0.20f, 1, 0.34f, 20, 0, -20, 0);

            var purchaseBtn = CreateButton("PurchaseBtn", content, "Acquire Enterprise", new Color(0.12f, 0.65f, 0.35f), Color.white);
            SetRect(purchaseBtn.gameObject, 0.15f, 0.08f, 0.85f, 0.18f, 0, 0, 0, 0);
            var feedback = CreateText("Feedback", content, "", 22, new Color(0.95f, 0.85f, 0.4f), FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(feedback.gameObject, 0, 0.01f, 1, 0.07f, 20, 0, -20, 0);

            SetPrivateField(view, "_availableCashText", cashText);
            SetPrivateField(view, "_marketStatusText", statusText);
            SetPrivateField(view, "_selectAutoRepairButton", btnAuto);
            SetPrivateField(view, "_selectStoreButton", btnStore);
            SetPrivateField(view, "_selectAgencyButton", btnAgency);
            SetPrivateField(view, "_businessNameText", nameText);
            SetPrivateField(view, "_businessCategoryText", catText);
            SetPrivateField(view, "_purchaseCostText", baseCost);
            SetPrivateField(view, "_setupCostText", setupCost);
            SetPrivateField(view, "_totalAcquisitionCostText", totalCost);
            SetPrivateField(view, "_dailyOverheadText", overhead);
            SetPrivateField(view, "_initialYieldText", yieldText);
            SetPrivateField(view, "_descriptionText", desc);
            SetPrivateField(view, "_purchaseButton", purchaseBtn);
            SetPrivateField(view, "_feedbackText", feedback);
        }

        private static void BuildDashboardUI(GameObject root, BusinessDashboardView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var valText = CreateText("TotalVal", content, "Portfolio Valuation: $0.00", 26, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(valText.gameObject, 0, 0.88f, 0.6f, 0.96f, 20, 0, 0, 0);
            var countText = CreateText("CountText", content, "Enterprises: 0", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(countText.gameObject, 0.6f, 0.88f, 1, 0.96f, 0, 0, -20, 0);
            var profText = CreateText("NetProf", content, "Monthly Net Profit: +$0.00", 26, new Color(0.2f, 0.85f, 0.45f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(profText.gameObject, 0, 0.80f, 1, 0.88f, 20, 0, -20, 0);

            var nameText = CreateText("BizName", content, "No Businesses Owned", 30, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(nameText.gameObject, 0, 0.65f, 0.6f, 0.75f, 20, 0, 0, 0);
            var statusText = CreateText("BizStatus", content, "STATUS: CLOSED", 26, new Color(0.9f, 0.4f, 0.4f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(statusText.gameObject, 0.6f, 0.65f, 1, 0.75f, 0, 0, -20, 0);

            var lvlText = CreateText("LvlText", content, "Facility Level 1", 24, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(lvlText.gameObject, 0, 0.55f, 0.33f, 0.63f, 20, 0, 0, 0);
            var repText = CreateText("RepText", content, "Reputation: 50/100", 24, new Color(0.35f, 0.8f, 0.98f), FontStyle.Normal, TextAnchor.MiddleCenter);
            SetRect(repText.gameObject, 0.33f, 0.55f, 0.66f, 0.63f, 0, 0, 0, 0);
            var condText = CreateText("CondText", content, "Condition: 100%", 24, new Color(0.4f, 0.9f, 0.6f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(condText.gameObject, 0.66f, 0.55f, 1, 0.63f, 0, 0, -20, 0);

            var assetVal = CreateText("AssetVal", content, "Asset Valuation: $0.00", 24, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(assetVal.gameObject, 0, 0.45f, 0.5f, 0.53f, 20, 0, 0, 0);
            var upgCost = CreateText("UpgCost", content, "Upgrade Cost: $0.00", 24, new Color(0.95f, 0.8f, 0.2f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(upgCost.gameObject, 0.5f, 0.45f, 1, 0.53f, 0, 0, -20, 0);
            var liqVal = CreateText("LiqVal", content, "Liquidation Payout: $0.00", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(liqVal.gameObject, 0, 0.37f, 1, 0.45f, 20, 0, -20, 0);

            var toggleBtn = CreateButton("ToggleOpenBtn", content, "Open for Business", new Color(0.18f, 0.45f, 0.75f), Color.white);
            var toggleText = toggleBtn.GetComponentInChildren<Text>();
            SetRect(toggleBtn.gameObject, 0.1f, 0.22f, 0.9f, 0.32f, 0, 0, 0, 0);

            var upgBtn = CreateButton("UpgBtn", content, "Upgrade Facility", new Color(0.12f, 0.65f, 0.35f), Color.white);
            SetRect(upgBtn.gameObject, 0.1f, 0.10f, 0.48f, 0.20f, 0, 0, 0, 0);

            var sellBtn = CreateButton("SellBtn", content, "Liquidate Business", new Color(0.75f, 0.25f, 0.25f), Color.white);
            SetRect(sellBtn.gameObject, 0.52f, 0.10f, 0.9f, 0.20f, 0, 0, 0, 0);

            var feedback = CreateText("Feedback", content, "", 22, new Color(0.95f, 0.85f, 0.4f), FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(feedback.gameObject, 0, 0.01f, 1, 0.08f, 20, 0, -20, 0);

            SetPrivateField(view, "_totalPortfolioValuationText", valText);
            SetPrivateField(view, "_totalMonthlyNetProfitText", profText);
            SetPrivateField(view, "_ownedBusinessCountText", countText);
            SetPrivateField(view, "_businessNameText", nameText);
            SetPrivateField(view, "_businessLevelText", lvlText);
            SetPrivateField(view, "_businessReputationText", repText);
            SetPrivateField(view, "_businessConditionText", condText);
            SetPrivateField(view, "_businessStatusText", statusText);
            SetPrivateField(view, "_businessValuationText", assetVal);
            SetPrivateField(view, "_upgradeCostText", upgCost);
            SetPrivateField(view, "_liquidationValueText", liqVal);
            SetPrivateField(view, "_toggleOpenButton", toggleBtn);
            SetPrivateField(view, "_toggleOpenButtonText", toggleText);
            SetPrivateField(view, "_upgradeButton", upgBtn);
            SetPrivateField(view, "_sellButton", sellBtn);
            SetPrivateField(view, "_feedbackText", feedback);
        }

        private static void BuildAutoRepairUI(GameObject root, AutoRepairView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var title = CreateText("Title", content, "Auto Repair Workshop", 28, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.gameObject, 0, 0.88f, 0.5f, 0.96f, 20, 0, 0, 0);
            var status = CreateText("Status", content, "OPERATIONAL", 26, new Color(0.4f, 0.9f, 0.6f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(status.gameObject, 0.5f, 0.88f, 1, 0.96f, 0, 0, -20, 0);
            var baySum = CreateText("BaySummary", content, "Service Bays: 1 / 1 Available", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(baySum.gameObject, 0, 0.80f, 1, 0.88f, 20, 0, -20, 0);

            // Bay 1
            var bay1Veh = CreateText("Bay1Veh", content, "Bay 1: Empty", 26, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(bay1Veh.gameObject, 0, 0.66f, 0.6f, 0.74f, 20, 0, 0, 0);
            var bay1Claim = CreateButton("Bay1Claim", content, "Claim Payout", new Color(0.12f, 0.65f, 0.35f), Color.white);
            SetRect(bay1Claim.gameObject, 0.65f, 0.66f, 0.95f, 0.74f, 0, 0, 0, 0);
            var sliderGo = DefaultControls.CreateSlider(_defaultResources);
            sliderGo.transform.SetParent(content.transform, false);
            var slider = sliderGo.GetComponent<Slider>();
            SetRect(sliderGo, 0.05f, 0.58f, 0.95f, 0.64f, 0, 0, 0, 0);

            // Job 1
            var job1Title = CreateText("Job1Title", content, "Customer: Sedan Brake Replacement", 26, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(job1Title.gameObject, 0, 0.42f, 1, 0.50f, 20, 0, -20, 0);
            var job1Cat = CreateText("Job1Cat", content, "Brake Systems (45 mins)", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(job1Cat.gameObject, 0, 0.34f, 0.5f, 0.42f, 20, 0, 0, 0);
            var job1Fin = CreateText("Job1Fin", content, "Revenue: $380.00 | Parts: $110.00", 24, new Color(0.2f, 0.85f, 0.45f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(job1Fin.gameObject, 0, 0.26f, 1, 0.34f, 20, 0, -20, 0);

            var assignBtn = CreateButton("AssignBay1Btn", content, "Assign to Bay 1", new Color(0.18f, 0.45f, 0.75f), Color.white);
            SetRect(assignBtn.gameObject, 0.15f, 0.12f, 0.85f, 0.22f, 0, 0, 0, 0);
            var feedback = CreateText("Feedback", content, "", 22, new Color(0.95f, 0.85f, 0.4f), FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(feedback.gameObject, 0, 0.02f, 1, 0.10f, 20, 0, -20, 0);

            SetPrivateField(view, "_workshopTitleText", title);
            SetPrivateField(view, "_workshopStatusText", status);
            SetPrivateField(view, "_baySummaryText", baySum);
            SetPrivateField(view, "_bay1VehicleText", bay1Veh);
            SetPrivateField(view, "_bay1ProgressSlider", slider);
            SetPrivateField(view, "_bay1ClaimButton", bay1Claim);
            SetPrivateField(view, "_job1TitleText", job1Title);
            SetPrivateField(view, "_job1CategoryText", job1Cat);
            SetPrivateField(view, "_job1FinancialsText", job1Fin);
            SetPrivateField(view, "_job1AssignBay1Button", assignBtn);
            SetPrivateField(view, "_feedbackText", feedback);
        }

        private static void BuildConvenienceStoreUI(GameObject root, ConvenienceStoreView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var title = CreateText("Title", content, "Corner Convenience Store", 28, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.gameObject, 0, 0.88f, 0.6f, 0.96f, 20, 0, 0, 0);
            var rep = CreateText("Rep", content, "Reputation: 50/100", 24, new Color(0.35f, 0.8f, 0.98f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(rep.gameObject, 0.6f, 0.88f, 1, 0.96f, 0, 0, -20, 0);
            var status = CreateText("Status", content, "Foot Traffic: Moderate", 24, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(status.gameObject, 0, 0.80f, 1, 0.88f, 20, 0, -20, 0);

            var prodName = CreateText("ProdName", content, "Inventory Line: Beverages & Snacks", 28, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(prodName.gameObject, 0, 0.66f, 1, 0.74f, 20, 0, -20, 0);
            var prodWholesale = CreateText("Wholesale", content, "Wholesale: $1.20/unit", 24, new Color(0.95f, 0.5f, 0.5f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(prodWholesale.gameObject, 0, 0.58f, 0.5f, 0.66f, 20, 0, 0, 0);
            var prodPrice = CreateText("Price", content, "Retail Price: $2.50/unit", 24, new Color(0.2f, 0.85f, 0.45f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(prodPrice.gameObject, 0.5f, 0.58f, 1, 0.66f, 0, 0, -20, 0);
            var prodStock = CreateText("Stock", content, "Stock on Hand: 150 units", 24, new Color(0.35f, 0.8f, 0.98f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(prodStock.gameObject, 0, 0.50f, 0.5f, 0.58f, 20, 0, 0, 0);
            var prodMetrics = CreateText("Metrics", content, "Gross Margin: 52%", 24, Color.white, FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(prodMetrics.gameObject, 0.5f, 0.50f, 1, 0.58f, 0, 0, -20, 0);

            var restockSm = CreateButton("RestockSm", content, "Restock +50 Units", new Color(0.18f, 0.45f, 0.75f), Color.white);
            SetRect(restockSm.gameObject, 0.05f, 0.36f, 0.48f, 0.46f, 0, 0, 0, 0);
            var restockBulk = CreateButton("RestockBulk", content, "Bulk Restock +200", new Color(0.12f, 0.65f, 0.35f), Color.white);
            SetRect(restockBulk.gameObject, 0.52f, 0.36f, 0.95f, 0.46f, 0, 0, 0, 0);

            var priceInc = CreateButton("PriceInc", content, "Price +$0.25", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(priceInc.gameObject, 0.05f, 0.24f, 0.48f, 0.34f, 0, 0, 0, 0);
            var priceDec = CreateButton("PriceDec", content, "Price -$0.25", new Color(0.2f, 0.28f, 0.4f), Color.white);
            SetRect(priceDec.gameObject, 0.52f, 0.24f, 0.95f, 0.34f, 0, 0, 0, 0);

            var report = CreateText("Report", content, "Yesterday: 84 units sold | Revenue: $210.00 | COGS: $100.80", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(report.gameObject, 0, 0.12f, 1, 0.22f, 20, 0, -20, 0);
            var feedback = CreateText("Feedback", content, "", 22, new Color(0.95f, 0.85f, 0.4f), FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(feedback.gameObject, 0, 0.02f, 1, 0.10f, 20, 0, -20, 0);

            SetPrivateField(view, "_storeTitleText", title);
            SetPrivateField(view, "_storeReputationText", rep);
            SetPrivateField(view, "_storeStatusText", status);
            SetPrivateField(view, "_productNameText", prodName);
            SetPrivateField(view, "_productWholesaleCostText", prodWholesale);
            SetPrivateField(view, "_productSellingPriceText", prodPrice);
            SetPrivateField(view, "_productStockText", prodStock);
            SetPrivateField(view, "_productSalesMetricsText", prodMetrics);
            SetPrivateField(view, "_restockSmallButton", restockSm);
            SetPrivateField(view, "_restockBulkButton", restockBulk);
            SetPrivateField(view, "_priceIncreaseButton", priceInc);
            SetPrivateField(view, "_priceDecreaseButton", priceDec);
            SetPrivateField(view, "_salesReportText", report);
            SetPrivateField(view, "_feedbackText", feedback);
        }

        private static void BuildDigitalAgencyUI(GameObject root, DigitalAgencyView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var title = CreateText("Title", content, "Apex Digital Marketing Agency", 28, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.gameObject, 0, 0.88f, 0.6f, 0.96f, 20, 0, 0, 0);
            var rep = CreateText("Rep", content, "Reputation: 50/100", 24, new Color(0.35f, 0.8f, 0.98f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(rep.gameObject, 0.6f, 0.88f, 1, 0.96f, 0, 0, -20, 0);
            var status = CreateText("Status", content, "Retainer Capacity: 1 / 2 Clients", 24, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(status.gameObject, 0, 0.80f, 1, 0.88f, 20, 0, -20, 0);

            var client = CreateText("Client", content, "Client: TechStart SEO & Ad Strategy", 28, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(client.gameObject, 0, 0.66f, 1, 0.74f, 20, 0, -20, 0);
            var cat = CreateText("Cat", content, "Category: Performance Marketing", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(cat.gameObject, 0, 0.58f, 0.5f, 0.66f, 20, 0, 0, 0);
            var payout = CreateText("Payout", content, "Contract Payout: $4,200.00", 26, new Color(0.2f, 0.85f, 0.45f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(payout.gameObject, 0.5f, 0.58f, 1, 0.66f, 0, 0, -20, 0);

            var hours = CreateText("Hours", content, "Hours: 0.0 / 40.0 hrs", 24, new Color(0.35f, 0.8f, 0.98f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(hours.gameObject, 0, 0.50f, 0.5f, 0.58f, 20, 0, 0, 0);
            var deadline = CreateText("Deadline", content, "Deadline: Day 25 (14 days left)", 24, new Color(0.95f, 0.8f, 0.2f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(deadline.gameObject, 0.5f, 0.50f, 1, 0.58f, 0, 0, -20, 0);

            var sliderGo = DefaultControls.CreateSlider(_defaultResources);
            sliderGo.transform.SetParent(content.transform, false);
            var slider = sliderGo.GetComponent<Slider>();
            SetRect(sliderGo, 0.05f, 0.42f, 0.95f, 0.48f, 0, 0, 0, 0);

            var acceptBtn = CreateButton("AcceptBtn", content, "Accept Contract", new Color(0.18f, 0.45f, 0.75f), Color.white);
            SetRect(acceptBtn.gameObject, 0.05f, 0.24f, 0.48f, 0.34f, 0, 0, 0, 0);
            var workBtn = CreateButton("WorkBtn", content, "Contribute 4 Hours", new Color(0.12f, 0.65f, 0.35f), Color.white);
            SetRect(workBtn.gameObject, 0.52f, 0.24f, 0.95f, 0.34f, 0, 0, 0, 0);

            var claimBtn = CreateButton("ClaimBtn", content, "Claim Completed Payout", new Color(0.95f, 0.65f, 0.15f), Color.white);
            SetRect(claimBtn.gameObject, 0.15f, 0.12f, 0.85f, 0.22f, 0, 0, 0, 0);

            var feedback = CreateText("Feedback", content, "", 22, new Color(0.95f, 0.85f, 0.4f), FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(feedback.gameObject, 0, 0.02f, 1, 0.10f, 20, 0, -20, 0);

            SetPrivateField(view, "_agencyTitleText", title);
            SetPrivateField(view, "_agencyReputationText", rep);
            SetPrivateField(view, "_agencyStatusText", status);
            SetPrivateField(view, "_clientNameText", client);
            SetPrivateField(view, "_campaignCategoryText", cat);
            SetPrivateField(view, "_contractPayoutText", payout);
            SetPrivateField(view, "_hoursProgressText", hours);
            SetPrivateField(view, "_contractProgressSlider", slider);
            SetPrivateField(view, "_deadlineDaysText", deadline);
            SetPrivateField(view, "_acceptContractButton", acceptBtn);
            SetPrivateField(view, "_contribute4HoursButton", workBtn);
            SetPrivateField(view, "_claimPayoutButton", claimBtn);
            SetPrivateField(view, "_feedbackText", feedback);
        }

        private static void BuildEmployeeUI(GameObject root, BusinessEmployeeView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var payroll = CreateText("Payroll", content, "Total Monthly Payroll: $0.00/mo", 26, new Color(0.95f, 0.4f, 0.4f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(payroll.gameObject, 0, 0.88f, 0.65f, 0.96f, 20, 0, 0, 0);
            var staffCount = CreateText("StaffCount", content, "Staff: 0 Hired", 24, Color.white, FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(staffCount.gameObject, 0.65f, 0.88f, 1, 0.96f, 0, 0, -20, 0);

            // Hired Employee Card
            var emp1Name = CreateText("Emp1Name", content, "No Staff Currently Hired", 26, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(emp1Name.gameObject, 0, 0.70f, 0.6f, 0.78f, 20, 0, 0, 0);
            var emp1Dismiss = CreateButton("Emp1Dismiss", content, "Dismiss", new Color(0.75f, 0.25f, 0.25f), Color.white);
            SetRect(emp1Dismiss.gameObject, 0.70f, 0.70f, 0.95f, 0.78f, 0, 0, 0, 0);
            var emp1Role = CreateText("Emp1Role", content, "Role: None", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(emp1Role.gameObject, 0, 0.62f, 0.5f, 0.70f, 20, 0, 0, 0);
            var emp1Sal = CreateText("Emp1Sal", content, "Salary: $0.00/mo", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(emp1Sal.gameObject, 0.5f, 0.62f, 1, 0.70f, 0, 0, -20, 0);

            // Candidate Card
            var candTitle = CreateText("CandTitle", content, "Available Recruitment Candidates", 26, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(candTitle.gameObject, 0, 0.48f, 1, 0.56f, 20, 0, -20, 0);
            var cand1Name = CreateText("Cand1Name", content, "Liam Vance", 26, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(cand1Name.gameObject, 0, 0.38f, 0.6f, 0.46f, 20, 0, 0, 0);
            var cand1Hire = CreateButton("Cand1Hire", content, "Hire Staff", new Color(0.12f, 0.65f, 0.35f), Color.white);
            SetRect(cand1Hire.gameObject, 0.70f, 0.38f, 0.95f, 0.46f, 0, 0, 0, 0);
            var cand1Role = CreateText("Cand1Role", content, "Role: Master Technician (Auto Repair)", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(cand1Role.gameObject, 0, 0.30f, 0.6f, 0.38f, 20, 0, 0, 0);
            var cand1Sal = CreateText("Cand1Sal", content, "Salary: $3,200.00/mo", 22, new Color(0.95f, 0.5f, 0.5f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(cand1Sal.gameObject, 0.6f, 0.30f, 1, 0.38f, 0, 0, -20, 0);
            var cand1Prod = CreateText("Cand1Prod", content, "Productivity Boost: +1.25x Bay Speed", 22, new Color(0.35f, 0.8f, 0.98f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(cand1Prod.gameObject, 0, 0.22f, 1, 0.30f, 20, 0, -20, 0);

            var feedback = CreateText("Feedback", content, "", 22, new Color(0.95f, 0.85f, 0.4f), FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(feedback.gameObject, 0, 0.05f, 1, 0.15f, 20, 0, -20, 0);

            SetPrivateField(view, "_totalPayrollText", payroll);
            SetPrivateField(view, "_staffCountText", staffCount);
            SetPrivateField(view, "_employee1NameText", emp1Name);
            SetPrivateField(view, "_employee1RoleText", emp1Role);
            SetPrivateField(view, "_employee1SalaryText", emp1Sal);
            SetPrivateField(view, "_employee1DismissButton", emp1Dismiss);
            SetPrivateField(view, "_candidate1NameText", cand1Name);
            SetPrivateField(view, "_candidate1RoleText", cand1Role);
            SetPrivateField(view, "_candidate1SalaryText", cand1Sal);
            SetPrivateField(view, "_candidate1ProductivityText", cand1Prod);
            SetPrivateField(view, "_candidate1HireButton", cand1Hire);
            SetPrivateField(view, "_feedbackText", feedback);
        }

        private static void BuildFinanceUI(GameObject root, BusinessFinanceView view)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var rev = CreateText("Revenue", content, "Revenue: $0.00", 24, new Color(0.2f, 0.85f, 0.45f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(rev.gameObject, 0, 0.88f, 0.5f, 0.96f, 20, 0, 0, 0);
            var exp = CreateText("Expenses", content, "Overhead: $0.00", 24, new Color(0.95f, 0.4f, 0.4f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(exp.gameObject, 0.5f, 0.88f, 1, 0.96f, 0, 0, -20, 0);

            var cogs = CreateText("COGS", content, "COGS: $0.00", 24, new Color(0.95f, 0.5f, 0.5f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(cogs.gameObject, 0, 0.80f, 0.5f, 0.88f, 20, 0, 0, 0);
            var payroll = CreateText("Payroll", content, "Payroll: $0.00", 24, new Color(0.95f, 0.5f, 0.5f), FontStyle.Normal, TextAnchor.MiddleRight);
            SetRect(payroll.gameObject, 0.5f, 0.80f, 1, 0.88f, 0, 0, -20, 0);

            var netProfit = CreateText("NetProfit", content, "Operating Net Profit: $0.00", 26, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(netProfit.gameObject, 0, 0.70f, 0.6f, 0.78f, 20, 0, 0, 0);
            var val = CreateText("Valuation", content, "Assets: $0.00", 26, new Color(0.35f, 0.8f, 0.98f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(val.gameObject, 0.6f, 0.70f, 1, 0.78f, 0, 0, -20, 0);

            // Commercial Credit
            var loanTitle = CreateText("LoanTitle", content, "Commercial Equipment Facility", 26, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(loanTitle.gameObject, 0, 0.54f, 0.65f, 0.62f, 20, 0, 0, 0);
            var loanApply = CreateButton("LoanApply", content, "Disburse $15K", new Color(0.18f, 0.45f, 0.75f), Color.white);
            SetRect(loanApply.gameObject, 0.68f, 0.54f, 0.95f, 0.62f, 0, 0, 0, 0);
            var loanTerms = CreateText("LoanTerms", content, "Principal: $15,000.00 | Rate: 8.5% APR | Term: 24 Months", 22, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(loanTerms.gameObject, 0, 0.46f, 1, 0.54f, 20, 0, -20, 0);

            // Active Debt
            var debtSum = CreateText("DebtSummary", content, "Active Debt: $0.00 across 0 facilities", 26, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(debtSum.gameObject, 0, 0.28f, 0.65f, 0.36f, 20, 0, 0, 0);
            var repayBtn = CreateButton("RepayEarlyBtn", content, "Settle Early", new Color(0.12f, 0.65f, 0.35f), Color.white);
            SetRect(repayBtn.gameObject, 0.68f, 0.28f, 0.95f, 0.36f, 0, 0, 0, 0);

            var feedback = CreateText("Feedback", content, "", 22, new Color(0.95f, 0.85f, 0.4f), FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(feedback.gameObject, 0, 0.10f, 1, 0.20f, 20, 0, -20, 0);

            SetPrivateField(view, "_totalRevenueText", rev);
            SetPrivateField(view, "_totalOperatingExpensesText", exp);
            SetPrivateField(view, "_totalCOGSText", cogs);
            SetPrivateField(view, "_totalPayrollText", payroll);
            SetPrivateField(view, "_netOperatingProfitText", netProfit);
            SetPrivateField(view, "_balanceSheetValuationText", val);
            SetPrivateField(view, "_loan1TitleText", loanTitle);
            SetPrivateField(view, "_loan1TermsText", loanTerms);
            SetPrivateField(view, "_loan1ApplyButton", loanApply);
            SetPrivateField(view, "_activeDebtSummaryText", debtSum);
            SetPrivateField(view, "_repayEarlyButton", repayBtn);
            SetPrivateField(view, "_feedbackText", feedback);
        }

        private static void BuildSettingsUI(GameObject root, SettingsModalView view, Button closeBtn)
        {
            var content = root.transform.Find("Dialog/Content").gameObject;

            var sndToggleGo = DefaultControls.CreateToggle(_defaultResources);
            sndToggleGo.transform.SetParent(content.transform, false);
            var sndToggle = sndToggleGo.GetComponent<Toggle>();
            sndToggle.GetComponentInChildren<Text>().text = "Audio & Sound Effects";
            sndToggle.GetComponentInChildren<Text>().fontSize = 26;
            sndToggle.GetComponentInChildren<Text>().color = Color.white;
            SetRect(sndToggleGo, 0.1f, 0.75f, 0.9f, 0.85f, 0, 0, 0, 0);

            var hapticsToggleGo = DefaultControls.CreateToggle(_defaultResources);
            hapticsToggleGo.transform.SetParent(content.transform, false);
            var hapticsToggle = hapticsToggleGo.GetComponent<Toggle>();
            hapticsToggle.GetComponentInChildren<Text>().text = "Tactile Haptics Feedback";
            hapticsToggle.GetComponentInChildren<Text>().fontSize = 26;
            hapticsToggle.GetComponentInChildren<Text>().color = Color.white;
            SetRect(hapticsToggleGo, 0.1f, 0.62f, 0.9f, 0.72f, 0, 0, 0, 0);

            var ver = CreateText("Version", content, "Version: 0.1.1", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(ver.gameObject, 0, 0.48f, 1, 0.56f, 20, 0, -20, 0);
            var pkg = CreateText("Pkg", content, "Package: com.bishal.richrich", 24, new Color(0.7f, 0.75f, 0.85f), FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(pkg.gameObject, 0, 0.40f, 1, 0.48f, 20, 0, -20, 0);

            var resetBtn = CreateButton("ResetBtn", content, "Reset Save Data", new Color(0.75f, 0.25f, 0.25f), Color.white);
            SetRect(resetBtn.gameObject, 0.15f, 0.20f, 0.85f, 0.30f, 0, 0, 0, 0);

            SetPrivateField(view, "_soundToggle", sndToggle);
            SetPrivateField(view, "_hapticsToggle", hapticsToggle);
            SetPrivateField(view, "_versionText", ver);
            SetPrivateField(view, "_packageIdText", pkg);
            SetPrivateField(view, "_resetSaveButton", resetBtn);
            SetPrivateField(view, "_closeButton", closeBtn);
        }

        #endregion

        #region UI Construction Helpers

        private static GameObject CreateUIObject(string name, GameObject parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static GameObject CreatePanel(string name, GameObject parent, Color color)
        {
            var panelGo = DefaultControls.CreatePanel(_defaultResources);
            panelGo.name = name;
            panelGo.transform.SetParent(parent.transform, false);
            var img = panelGo.GetComponent<Image>();
            img.color = color;
            return panelGo;
        }

        private static Text CreateText(string name, GameObject parent, string text, int fontSize, Color color, FontStyle style, TextAnchor align)
        {
            var textGo = DefaultControls.CreateText(_defaultResources);
            textGo.name = name;
            textGo.transform.SetParent(parent.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text = text;
            t.font = _defaultFont;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            return t;
        }

        private static Button CreateButton(string name, GameObject parent, string label, Color bgColor, Color textColor)
        {
            var btnGo = DefaultControls.CreateButton(_defaultResources);
            btnGo.name = name;
            btnGo.transform.SetParent(parent.transform, false);
            var img = btnGo.GetComponent<Image>();
            img.color = bgColor;
            var t = btnGo.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.text = label;
                t.font = _defaultFont;
                t.color = textColor;
                t.fontSize = 24;
            }
            return btnGo.GetComponent<Button>();
        }

        private static Button CreateNavButton(string name, GameObject parent, string label, float xMin, float yMin, float xMax, float yMax, float offXMin, float offYMin, float offXMax, float offYMax)
        {
            var btn = CreateButton(name, parent, label, new Color(0.14f, 0.20f, 0.32f), Color.white);
            SetRect(btn.gameObject, xMin, yMin, xMax, yMax, offXMin, offYMin, offXMax, offYMax);
            return btn;
        }

        private static GameObject CreateModalSheet(string name, GameObject parent, string title, out Button closeButton)
        {
            var modalRoot = CreateUIObject(name, parent);
            StretchFull(modalRoot);
            modalRoot.SetActive(false); // Inactive initially

            // Dim Backdrop
            var backdrop = CreatePanel("Backdrop", modalRoot, new Color(0.02f, 0.04f, 0.08f, 0.92f));
            StretchFull(backdrop);

            // Dialog Window
            var dialog = CreatePanel("Dialog", modalRoot, new Color(0.09f, 0.13f, 0.22f, 1.0f));
            SetRect(dialog, 0.03f, 0.05f, 0.97f, 0.95f, 0, 0, 0, 0);

            // Dialog Header
            var header = CreatePanel("Header", dialog, new Color(0.12f, 0.18f, 0.30f, 1.0f));
            SetRect(header, 0, 1, 1, 1, 0, -90, 0, 0);

            var titleText = CreateText("Title", header, title, 32, new Color(0.95f, 0.8f, 0.2f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(titleText.gameObject, 0, 0, 0.8f, 1, 24, 0, 0, 0);

            closeButton = CreateButton("CloseBtn", header, "✕", new Color(0.75f, 0.25f, 0.25f), Color.white);
            SetRect(closeButton.gameObject, 0.85f, 0.15f, 0.98f, 0.85f, 0, 0, 0, 0);

            // Wire close button to dismiss dialog
            var capturedModal = modalRoot;
            closeButton.onClick.AddListener(() => capturedModal.SetActive(false));

            // Content Panel
            var content = CreateUIObject("Content", dialog);
            SetRect(content, 0, 0, 1, 1, 0, 0, 0, -95);

            return modalRoot;
        }

        private static void StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetRect(GameObject go, float axMin, float ayMin, float axMax, float ayMax, float offXMin, float offYMin, float offXMax, float offYMax)
        {
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(axMin, ayMin);
            rt.anchorMax = new Vector2(axMax, ayMax);
            rt.offsetMin = new Vector2(offXMin, offYMin);
            rt.offsetMax = new Vector2(offXMax, offYMax);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var type = target.GetType();
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[RichRichSceneBuilder] Field '{fieldName}' not found on type '{type.Name}'.");
            }
        }

        #endregion
    }
}

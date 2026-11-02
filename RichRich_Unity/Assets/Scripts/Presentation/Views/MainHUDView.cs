using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the Life & Wealth simulation HUD.
    /// Replaces tap-to-earn controls with financial health metrics, calendar clock,
    /// employment status, housing, energy, monthly cashflow, and speed controls.
    /// </summary>
    public class MainHUDView : MonoBehaviour
    {
        [Header("Financial Metrics")]
        [SerializeField] private Text _cashBalanceText;
        [SerializeField] private Text _netWorthText;
        [SerializeField] private Text _monthlyIncomeText;
        [SerializeField] private Text _monthlyExpensesText;
        [SerializeField] private Text _netCashFlowText;
        [SerializeField] private Text _healthBadgeText;

        [Header("Simulation Calendar & Clock")]
        [SerializeField] private Text _simulationDateText;
        [SerializeField] private Text _clockSpeedText;
        [SerializeField] private Button _pauseToggleButton;
        [SerializeField] private Button _normalSpeedButton;
        [SerializeField] private Button _fastSpeedButton;
        [SerializeField] private Button _sleepButton;

        [Header("Career, Housing & Lifestyle Summary")]
        [SerializeField] private Text _careerStatusText;
        [SerializeField] private Text _educationStatusText;
        [SerializeField] private Text _housingStatusText;
        [SerializeField] private Text _energyStatusText;
        [SerializeField] private Text _fatigueStatusText;

        [Header("v0.3.0 Bottom Navigation Dock")]
        [SerializeField] private Button _dockMetropolisButton;
        [SerializeField] private Button _dockCareerButton;
        [SerializeField] private Button _dockEnterpriseButton;
        [SerializeField] private Button _dockWealthButton;

        [Header("v0.3.0 Category Portal Modals")]
        [SerializeField] private GameObject _careerPortalModalRoot;
        [SerializeField] private GameObject _enterprisePortalModalRoot;
        [SerializeField] private GameObject _wealthPortalModalRoot;

        [Header("Navigation Buttons & Modals")]
        [SerializeField] private Button _jobBoardOpenButton;
        [SerializeField] private GameObject _jobBoardModalRoot;

        [SerializeField] private Button _educationOpenButton;
        [SerializeField] private GameObject _educationModalRoot;

        [SerializeField] private Button _housingOpenButton;
        [SerializeField] private GameObject _housingModalRoot;

        [SerializeField] private Button _lifestyleOpenButton;
        [SerializeField] private GameObject _lifestyleModalRoot;

        [Header("Commercial Business Navigation & Modals")]
        [SerializeField] private Button _businessMarketplaceOpenButton;
        [SerializeField] private GameObject _businessMarketplaceModalRoot;

        [SerializeField] private Button _businessDashboardOpenButton;
        [SerializeField] private GameObject _businessDashboardModalRoot;

        [SerializeField] private Button _autoRepairOpenButton;
        [SerializeField] private GameObject _autoRepairModalRoot;

        [SerializeField] private Button _convenienceStoreOpenButton;
        [SerializeField] private GameObject _convenienceStoreModalRoot;

        [SerializeField] private Button _digitalAgencyOpenButton;
        [SerializeField] private GameObject _digitalAgencyModalRoot;

        [SerializeField] private Button _businessEmployeeOpenButton;
        [SerializeField] private GameObject _businessEmployeeModalRoot;

        [SerializeField] private Button _businessFinanceOpenButton;
        [SerializeField] private GameObject _businessFinanceModalRoot;

        [Header("Real Estate & Properties")]
        [SerializeField] private Button _realEstateOpenButton;
        [SerializeField] private GameObject _realEstateModalRoot;

        [Header("Financial Markets & Equities")]
        [SerializeField] private Button _stockMarketOpenButton;
        [SerializeField] private GameObject _stockMarketModalRoot;

        [Header("Life Progression & Goals")]
        [SerializeField] private Button _goalsOpenButton;
        [SerializeField] private GameObject _goalsModalRoot;

        [Header("Settings")]
        [SerializeField] private Button _settingsOpenButton;
        [SerializeField] private GameObject _settingsModalRoot;

        private LifeSimulationHUDController _controller;

        public void Bind(LifeSimulationHUDController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnHUDUpdated += RefreshUI;

            if (_pauseToggleButton != null)
            {
                _pauseToggleButton.onClick.AddListener(() => _controller.TogglePause());
            }

            if (_normalSpeedButton != null)
            {
                _normalSpeedButton.onClick.AddListener(() => _controller.SetSpeed(1.0f));
            }

            if (_fastSpeedButton != null)
            {
                _fastSpeedButton.onClick.AddListener(() => _controller.SetSpeed(5.0f));
            }

            if (_sleepButton != null)
            {
                _sleepButton.onClick.AddListener(() => _controller.SleepToNextMorning());
            }

            if (_dockMetropolisButton != null)
            {
                _dockMetropolisButton.onClick.AddListener(() =>
                {
                    if (ModalManager.Instance != null)
                    {
                        ModalManager.Instance.CloseAll();
                    }
                    var city = FindObjectOfType<CityInteractionController>();
                    if (city != null)
                    {
                        city.SetCityExploreMode(true);
                    }
                });
            }

            if (_dockCareerButton != null && _careerPortalModalRoot != null)
            {
                _dockCareerButton.onClick.AddListener(() => OpenRootModal(_careerPortalModalRoot));
            }

            if (_dockEnterpriseButton != null && _enterprisePortalModalRoot != null)
            {
                _dockEnterpriseButton.onClick.AddListener(() => OpenRootModal(_enterprisePortalModalRoot));
            }

            if (_dockWealthButton != null && _wealthPortalModalRoot != null)
            {
                _dockWealthButton.onClick.AddListener(() => OpenRootModal(_wealthPortalModalRoot));
            }

            if (_jobBoardOpenButton != null && _jobBoardModalRoot != null)
            {
                _jobBoardOpenButton.onClick.AddListener(() => OpenSubsystemModal(_jobBoardModalRoot));
            }

            if (_educationOpenButton != null && _educationModalRoot != null)
            {
                _educationOpenButton.onClick.AddListener(() => OpenSubsystemModal(_educationModalRoot));
            }

            if (_housingOpenButton != null && _housingModalRoot != null)
            {
                _housingOpenButton.onClick.AddListener(() => OpenSubsystemModal(_housingModalRoot));
            }

            if (_lifestyleOpenButton != null && _lifestyleModalRoot != null)
            {
                _lifestyleOpenButton.onClick.AddListener(() => OpenSubsystemModal(_lifestyleModalRoot));
            }

            if (_businessMarketplaceOpenButton != null && _businessMarketplaceModalRoot != null)
            {
                _businessMarketplaceOpenButton.onClick.AddListener(() => OpenSubsystemModal(_businessMarketplaceModalRoot));
            }

            if (_businessDashboardOpenButton != null && _businessDashboardModalRoot != null)
            {
                _businessDashboardOpenButton.onClick.AddListener(() => OpenSubsystemModal(_businessDashboardModalRoot));
            }

            if (_autoRepairOpenButton != null && _autoRepairModalRoot != null)
            {
                _autoRepairOpenButton.onClick.AddListener(() => OpenSubsystemModal(_autoRepairModalRoot));
            }

            if (_convenienceStoreOpenButton != null && _convenienceStoreModalRoot != null)
            {
                _convenienceStoreOpenButton.onClick.AddListener(() => OpenSubsystemModal(_convenienceStoreModalRoot));
            }

            if (_digitalAgencyOpenButton != null && _digitalAgencyModalRoot != null)
            {
                _digitalAgencyOpenButton.onClick.AddListener(() => OpenSubsystemModal(_digitalAgencyModalRoot));
            }

            if (_businessEmployeeOpenButton != null && _businessEmployeeModalRoot != null)
            {
                _businessEmployeeOpenButton.onClick.AddListener(() => OpenSubsystemModal(_businessEmployeeModalRoot));
            }

            if (_businessFinanceOpenButton != null && _businessFinanceModalRoot != null)
            {
                _businessFinanceOpenButton.onClick.AddListener(() => OpenSubsystemModal(_businessFinanceModalRoot));
            }

            if (_realEstateOpenButton != null && _realEstateModalRoot != null)
            {
                _realEstateOpenButton.onClick.AddListener(() => OpenSubsystemModal(_realEstateModalRoot));
            }

            if (_stockMarketOpenButton != null && _stockMarketModalRoot != null)
            {
                _stockMarketOpenButton.onClick.AddListener(() => OpenSubsystemModal(_stockMarketModalRoot));
            }

            if (_goalsOpenButton != null && _goalsModalRoot != null)
            {
                _goalsOpenButton.onClick.AddListener(() => OpenSubsystemModal(_goalsModalRoot));
            }

            if (_settingsOpenButton != null && _settingsModalRoot != null)
            {
                _settingsOpenButton.onClick.AddListener(() => OpenSubsystemModal(_settingsModalRoot));
            }

            RefreshUI();
        }

        private void OpenSubsystemModal(GameObject modalRoot)
        {
            if (modalRoot == null) return;
            if (ModalManager.Instance != null)
            {
                if (ModalManager.Instance.HasActiveModals)
                {
                    ModalManager.Instance.PushChild(modalRoot);
                }
                else
                {
                    ModalManager.Instance.OpenRoot(modalRoot);
                }
            }
            else
            {
                modalRoot.SetActive(true);
            }
        }

        private void OpenRootModal(GameObject modalRoot)
        {
            if (modalRoot == null) return;
            if (ModalManager.Instance != null)
            {
                ModalManager.Instance.OpenRoot(modalRoot);
            }
            else
            {
                modalRoot.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnHUDUpdated -= RefreshUI;
            }
        }

        public void RefreshUI()
        {
            if (_controller == null) return;

            if (_cashBalanceText != null) _cashBalanceText.text = _controller.CashBalanceText;
            if (_netWorthText != null) _netWorthText.text = $"Net Worth: {_controller.NetWorthText}";
            if (_monthlyIncomeText != null) _monthlyIncomeText.text = _controller.MonthlyIncomeText;
            if (_monthlyExpensesText != null) _monthlyExpensesText.text = _controller.MonthlyExpensesText;
            if (_netCashFlowText != null) _netCashFlowText.text = $"Cash Flow: {_controller.NetMonthlyCashFlowText}";
            if (_healthBadgeText != null) _healthBadgeText.text = $"Status: {_controller.HealthBadgeText}";

            if (_simulationDateText != null) _simulationDateText.text = _controller.SimulationDateLongText;
            if (_clockSpeedText != null) _clockSpeedText.text = _controller.ClockSpeedText;

            if (_careerStatusText != null) _careerStatusText.text = $"Career: {_controller.CareerStatusText}";
            if (_educationStatusText != null) _educationStatusText.text = $"Study: {_controller.EducationStatusText}";
            if (_housingStatusText != null) _housingStatusText.text = $"Housing: {_controller.HousingStatusText}";
            if (_energyStatusText != null) _energyStatusText.text = $"Energy: {_controller.EnergyStatusText}";
            if (_fatigueStatusText != null) _fatigueStatusText.text = $"Fatigue: {_controller.FatigueStatusText}";
        }
    }
}

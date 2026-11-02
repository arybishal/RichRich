using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the Real Estate & Property Investment Portfolio.
    /// Manages residential and commercial property acquisition, mortgage amortization,
    /// tenant management, condition renovation, and property liquidation.
    /// Follows premium dark FinTech styling.
    /// </summary>
    public class RealEstateView : MonoBehaviour
    {
        [Header("Portfolio Summary")]
        [SerializeField] private Text _totalValuationText;
        [SerializeField] private Text _totalMortgageText;
        [SerializeField] private Text _totalEquityText;
        [SerializeField] private Text _monthlyCashFlowText;
        [SerializeField] private Text _liquidCashText;

        [Header("Property Selection Buttons")]
        [SerializeField] private Button _selectStarterButton;
        [SerializeField] private Button _selectDuplexButton;
        [SerializeField] private Button _selectRetailButton;
        [SerializeField] private Button _selectLuxuryButton;
        [SerializeField] private Button _selectPlazaButton;

        [Header("Selected Property Details")]
        [SerializeField] private Text _propertyNameText;
        [SerializeField] private Text _propertyTypeText;
        [SerializeField] private Text _ownershipStatusText;
        [SerializeField] private Text _valuationText;
        [SerializeField] private Text _conditionText;
        [SerializeField] private Text _purchasePriceText;
        [SerializeField] private Text _downPaymentText;
        [SerializeField] private Text _rentalIncomeText;
        [SerializeField] private Text _maintenanceCostText;
        [SerializeField] private Text _mortgageStatusText;

        [Header("Action Buttons")]
        [SerializeField] private Button _buyCashButton;
        [SerializeField] private Button _buyMortgageButton;
        [SerializeField] private Button _signTenantButton;
        [SerializeField] private Button _evictTenantButton;
        [SerializeField] private Button _renovateButton;
        [SerializeField] private Button _sellButton;
        [SerializeField] private Text _feedbackText;

        private RealEstateController _controller;

        public void Bind(RealEstateController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnRealEstateUpdated += RefreshUI;
            _controller.OnActionFeedback += (msg, isError) =>
            {
                if (_feedbackText != null)
                {
                    _feedbackText.text = msg;
                    _feedbackText.color = isError
                        ? new Color(0.95f, 0.35f, 0.35f)
                        : new Color(0.35f, 0.85f, 0.50f);
                }
            };

            // Wire selection buttons
            if (_selectStarterButton != null) _selectStarterButton.onClick.AddListener(() => _controller.SelectProperty("prop_starter_home"));
            if (_selectDuplexButton != null) _selectDuplexButton.onClick.AddListener(() => _controller.SelectProperty("prop_urban_duplex"));
            if (_selectRetailButton != null) _selectRetailButton.onClick.AddListener(() => _controller.SelectProperty("prop_retail_strip"));
            if (_selectLuxuryButton != null) _selectLuxuryButton.onClick.AddListener(() => _controller.SelectProperty("prop_luxury_tower"));
            if (_selectPlazaButton != null) _selectPlazaButton.onClick.AddListener(() => _controller.SelectProperty("prop_commercial_plaza"));

            // Wire action buttons
            if (_buyCashButton != null) _buyCashButton.onClick.AddListener(() => _controller.TryBuySelected(withMortgage: false, out _));
            if (_buyMortgageButton != null) _buyMortgageButton.onClick.AddListener(() => _controller.TryBuySelected(withMortgage: true, out _));
            if (_signTenantButton != null) _signTenantButton.onClick.AddListener(() => _controller.TrySignTenantSelected(out _));
            if (_evictTenantButton != null) _evictTenantButton.onClick.AddListener(() => _controller.TryEvictTenantSelected(out _));
            if (_renovateButton != null) _renovateButton.onClick.AddListener(() => _controller.TryRenovateSelected(out _));
            if (_sellButton != null) _sellButton.onClick.AddListener(() => _controller.TrySellSelected(out _, out _));

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_controller == null) return;

            // 1. Portfolio Header
            if (_totalValuationText != null) _totalValuationText.text = $"Total Valuation: {_controller.TotalValuation}";
            if (_totalMortgageText != null) _totalMortgageText.text = $"Mortgage Debt: {_controller.TotalMortgages}";
            if (_totalEquityText != null) _totalEquityText.text = $"Net Real Estate Equity: {_controller.TotalEquity}";
            if (_liquidCashText != null) _liquidCashText.text = $"Liquid Cash: {_controller.PlayerCash}";

            if (_monthlyCashFlowText != null)
            {
                long flowCents = _controller.MonthlyNetCashFlowCents;
                string sign = flowCents >= 0 ? "+" : "-";
                Currency flowAbs = Currency.FromCents(Math.Abs(flowCents));
                _monthlyCashFlowText.text = $"Net Monthly Yield: {sign}{flowAbs}/mo (Rent: {_controller.MonthlyRentalIncome} | Debt: {_controller.MonthlyDebtService} | Maint: {_controller.MonthlyMaintenance})";
                _monthlyCashFlowText.color = flowCents >= 0 ? new Color(0.35f, 0.85f, 0.50f) : new Color(0.95f, 0.35f, 0.35f);
            }

            // 2. Selected Property Details
            var prop = _controller.SelectedProperty;
            if (prop == null)
            {
                if (_propertyNameText != null) _propertyNameText.text = "No Property Selected";
                return;
            }

            if (_propertyNameText != null) _propertyNameText.text = prop.Name;
            if (_propertyTypeText != null) _propertyTypeText.text = $"Category: {prop.Type}";
            if (_ownershipStatusText != null)
            {
                _ownershipStatusText.text = prop.IsOwned ? "Status: [OWNED IN PORTFOLIO]" : "Status: [FOR SALE ON MARKET]";
                _ownershipStatusText.color = prop.IsOwned ? new Color(0.35f, 0.85f, 0.50f) : new Color(0.95f, 0.75f, 0.25f);
            }

            if (_valuationText != null) _valuationText.text = $"Current Valuation: {prop.CurrentValuation}";
            if (_conditionText != null)
            {
                _conditionText.text = $"Building Condition: {prop.ConditionFormatted}";
                _conditionText.color = prop.Condition >= 75 ? new Color(0.35f, 0.85f, 0.50f) : (prop.Condition >= 45 ? new Color(0.95f, 0.75f, 0.25f) : new Color(0.95f, 0.35f, 0.35f));
            }

            if (_purchasePriceText != null) _purchasePriceText.text = $"Price: {prop.PurchasePrice}";
            if (_downPaymentText != null) _downPaymentText.text = $"Down Payment ({prop.DownPaymentPercent:P0}): {prop.DownPaymentAmount}";

            if (_rentalIncomeText != null)
            {
                if (prop.IsRented)
                {
                    _rentalIncomeText.text = $"Tenant: Active (${prop.TenantMonthlyRent}/mo)";
                    _rentalIncomeText.color = new Color(0.35f, 0.85f, 0.50f);
                }
                else
                {
                    _rentalIncomeText.text = $"Tenant: Vacant (Market: {prop.BaseMonthlyRent}/mo)";
                    _rentalIncomeText.color = new Color(0.70f, 0.75f, 0.85f);
                }
            }

            if (_maintenanceCostText != null) _maintenanceCostText.text = $"Routine Maintenance: {prop.MonthlyMaintenanceCost}/mo";

            if (_mortgageStatusText != null)
            {
                if (prop.Mortgage != null && !prop.Mortgage.IsPaidOff)
                {
                    _mortgageStatusText.text = $"Mortgage: {prop.Mortgage.RemainingPrincipal} balance ({prop.Mortgage.MonthlyPayment}/mo, {prop.Mortgage.MonthsRemaining} mo left)";
                    _mortgageStatusText.color = new Color(0.95f, 0.65f, 0.35f);
                }
                else
                {
                    _mortgageStatusText.text = "Mortgage: None (Free & Clear)";
                    _mortgageStatusText.color = new Color(0.35f, 0.85f, 0.50f);
                }
            }

            // 3. Action button interactivity
            if (_buyCashButton != null)
            {
                _buyCashButton.gameObject.SetActive(!prop.IsOwned);
                _buyCashButton.interactable = !prop.IsOwned && _controller.CanAffordCash(prop);
            }

            if (_buyMortgageButton != null)
            {
                _buyMortgageButton.gameObject.SetActive(!prop.IsOwned);
                _buyMortgageButton.interactable = !prop.IsOwned && _controller.CanAffordMortgage(prop);
            }

            if (_signTenantButton != null)
            {
                _signTenantButton.gameObject.SetActive(prop.IsOwned && !prop.IsRented);
            }

            if (_evictTenantButton != null)
            {
                _evictTenantButton.gameObject.SetActive(prop.IsOwned && prop.IsRented);
            }

            if (_renovateButton != null)
            {
                _renovateButton.gameObject.SetActive(prop.IsOwned);
                Currency renoCost = Currency.FromCents(prop.MonthlyMaintenanceCost.MinorUnits * 3L);
                _renovateButton.interactable = prop.IsOwned && prop.Condition < 95.0 && _controller.PlayerCash >= renoCost;
            }

            if (_sellButton != null)
            {
                _sellButton.gameObject.SetActive(prop.IsOwned);
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the Commercial Business Dashboard & Portfolio Management.
    /// Manages enterprise operations, facility upgrades, liquidation/sales, and performance tracking.
    /// Follows premium dark FinTech design direction.
    /// </summary>
    public class BusinessDashboardView : MonoBehaviour
    {
        [Header("Portfolio Summary")]
        [SerializeField] private Text _totalPortfolioValuationText;
        [SerializeField] private Text _totalMonthlyNetProfitText;
        [SerializeField] private Text _ownedBusinessCountText;

        [Header("Selected Business Details")]
        [SerializeField] private Text _businessNameText;
        [SerializeField] private Text _businessLevelText;
        [SerializeField] private Text _businessReputationText;
        [SerializeField] private Text _businessConditionText;
        [SerializeField] private Text _businessStatusText;
        [SerializeField] private Text _businessValuationText;
        [SerializeField] private Text _upgradeCostText;
        [SerializeField] private Text _liquidationValueText;

        [Header("Actions")]
        [SerializeField] private Button _toggleOpenButton;
        [SerializeField] private Text _toggleOpenButtonText;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _sellButton;
        [SerializeField] private Text _feedbackText;

        private BusinessDashboardController _controller;

        public void Bind(BusinessDashboardController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnDashboardUpdated += RefreshUI;
            _controller.OnBusinessUpgraded += OnUpgraded;
            _controller.OnBusinessSold += OnSold;
            _controller.OnActionFailed += OnFailed;

            if (_toggleOpenButton != null)
                _toggleOpenButton.onClick.AddListener(() => _controller.ToggleOpen());
            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(() => _controller.TryUpgradeSelected(out _));
            if (_sellButton != null)
                _sellButton.onClick.AddListener(() => _controller.TrySellSelected(out _, out _));

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_controller == null) return;

            if (_totalPortfolioValuationText != null)
                _totalPortfolioValuationText.text = $"Portfolio Valuation: {_controller.TotalPortfolioValuation}";
            if (_totalMonthlyNetProfitText != null)
                _totalMonthlyNetProfitText.text = $"Monthly Net Profit: {_controller.TotalMonthlyNetProfit}";
            if (_ownedBusinessCountText != null)
                _ownedBusinessCountText.text = $"Enterprises: {_controller.OwnedBusinesses.Count}";

            var sel = _controller.SelectedBusiness;
            if (sel != null)
            {
                if (_businessNameText != null) _businessNameText.text = sel.Name;
                if (_businessLevelText != null) _businessLevelText.text = $"Facility Level {sel.Level}";
                if (_businessReputationText != null) _businessReputationText.text = $"Reputation: {sel.Reputation}/100";
                if (_businessConditionText != null) _businessConditionText.text = $"Condition: {sel.Condition}%";
                if (_businessStatusText != null) _businessStatusText.text = sel.IsOpen ? "STATUS: OPEN" : "STATUS: CLOSED";
                if (_businessValuationText != null) _businessValuationText.text = $"Asset Valuation: {sel.CalculateValuation()}";
                if (_upgradeCostText != null) _upgradeCostText.text = $"Upgrade Cost: {sel.GetUpgradeCost()}";
                if (_liquidationValueText != null) _liquidationValueText.text = $"Liquidation Payout: {sel.CalculateValuation()}";

                if (_toggleOpenButtonText != null)
                {
                    _toggleOpenButtonText.text = sel.IsOpen ? "Close Enterprise" : "Open for Business";
                }
            }
            else
            {
                if (_businessNameText != null) _businessNameText.text = "No Businesses Owned";
            }
        }

        private void OnUpgraded(Business biz)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"{biz.Name} upgraded to Level {biz.Level}!";
        }

        private void OnSold(Business biz, Currency proceeds)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Liquidated {biz.Name} for {proceeds} cash.";
        }

        private void OnFailed(string error)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Operation failed: {error}";
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnDashboardUpdated -= RefreshUI;
                _controller.OnBusinessUpgraded -= OnUpgraded;
                _controller.OnBusinessSold -= OnSold;
                _controller.OnActionFailed -= OnFailed;
            }
        }
    }
}

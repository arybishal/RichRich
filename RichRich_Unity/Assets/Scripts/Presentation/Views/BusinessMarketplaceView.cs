using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the Commercial Business Marketplace.
    /// Manages enterprise selection, inspecting acquisition costs, and establishing commercial ventures.
    /// Follows premium dark FinTech design direction.
    /// </summary>
    public class BusinessMarketplaceView : MonoBehaviour
    {
        [Header("Overview")]
        [SerializeField] private Text _availableCashText;
        [SerializeField] private Text _marketStatusText;

        [Header("Selected Business Details")]
        [SerializeField] private Text _businessNameText;
        [SerializeField] private Text _businessCategoryText;
        [SerializeField] private Text _purchaseCostText;
        [SerializeField] private Text _setupCostText;
        [SerializeField] private Text _totalAcquisitionCostText;
        [SerializeField] private Text _dailyOverheadText;
        [SerializeField] private Text _initialYieldText;
        [SerializeField] private Text _descriptionText;

        [Header("Actions")]
        [SerializeField] private Button _selectAutoRepairButton;
        [SerializeField] private Button _selectStoreButton;
        [SerializeField] private Button _selectAgencyButton;
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private Text _feedbackText;

        private BusinessMarketplaceController _controller;

        public void Bind(BusinessMarketplaceController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnMarketplaceUpdated += RefreshUI;
            _controller.OnBusinessPurchased += OnPurchased;
            _controller.OnPurchaseFailed += OnFailed;

            if (_selectAutoRepairButton != null)
                _selectAutoRepairButton.onClick.AddListener(() => _controller.SelectBusiness("auto_repair"));
            if (_selectStoreButton != null)
                _selectStoreButton.onClick.AddListener(() => _controller.SelectBusiness("convenience_store"));
            if (_selectAgencyButton != null)
                _selectAgencyButton.onClick.AddListener(() => _controller.SelectBusiness("digital_agency"));

            if (_purchaseButton != null)
            {
                _purchaseButton.onClick.AddListener(() =>
                {
                    _controller.TryPurchaseSelected(out _);
                });
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_controller == null) return;

            if (_availableCashText != null)
            {
                _availableCashText.text = $"Liquid Cash: {_controller.PlayerCash}";
            }

            if (_marketStatusText != null)
            {
                _marketStatusText.text = $"{_controller.UnownedBusinesses.Count} commercial opportunities available.";
            }

            var sel = _controller.SelectedBusiness;
            if (sel != null)
            {
                if (_businessNameText != null) _businessNameText.text = sel.Name;
                if (_businessCategoryText != null) _businessCategoryText.text = $"Industry: {sel.Category}";
                if (_purchaseCostText != null) _purchaseCostText.text = $"Base Acquisition: {sel.BaseCost}";
                if (_setupCostText != null) _setupCostText.text = $"Registration & Setup: {sel.SetupCost}";
                if (_totalAcquisitionCostText != null) _totalAcquisitionCostText.text = $"Total Capital Required: {sel.GetPurchaseCost()}";
                if (_dailyOverheadText != null) _dailyOverheadText.text = $"Daily Overhead: {sel.DailyOperatingExpenses}/day";
                if (_initialYieldText != null) _initialYieldText.text = $"Starting Base Yield: {sel.BaseHourlyYield}/hr";
                if (_descriptionText != null) _descriptionText.text = sel.Description;

                if (_purchaseButton != null)
                {
                    _purchaseButton.interactable = _controller.CanAffordSelected() && !sel.IsOwned;
                }
            }
        }

        private void OnPurchased(Business biz)
        {
            if (_feedbackText != null)
            {
                _feedbackText.text = $"Successfully acquired {biz.Name}! Ready for operations.";
            }
        }

        private void OnFailed(string error)
        {
            if (_feedbackText != null)
            {
                _feedbackText.text = $"Acquisition failed: {error}";
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnMarketplaceUpdated -= RefreshUI;
                _controller.OnBusinessPurchased -= OnPurchased;
                _controller.OnPurchaseFailed -= OnFailed;
            }
        }
    }
}

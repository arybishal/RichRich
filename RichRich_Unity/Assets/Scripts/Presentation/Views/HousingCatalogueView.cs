using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for Housing and Lifestyle Accommodations.
    /// Manages browsing 5 tiers, reviewing comfort/energy recovery, and move-in leases.
    /// </summary>
    public class HousingCatalogueView : MonoBehaviour
    {
        [Header("Current Residence")]
        [SerializeField] private Text _currentHousingNameText;
        [SerializeField] private Text _currentRentText;
        [SerializeField] private Text _currentDepositText;
        [SerializeField] private Text _currentComfortText;
        [SerializeField] private Text _currentRecoveryText;

        [Header("Selected Tier Details")]
        [SerializeField] private Text _selectedTierNameText;
        [SerializeField] private Text _selectedRentText;
        [SerializeField] private Text _selectedDepositText;
        [SerializeField] private Text _selectedComfortText;
        [SerializeField] private Text _selectedRecoveryText;
        [SerializeField] private Text _selectedRequirementsText;
        [SerializeField] private Text _selectedDescriptionText;

        [Header("Actions")]
        [SerializeField] private Button _moveToSelectedButton;
        [SerializeField] private Text _housingFeedbackText;

        private HousingCatalogueController _controller;

        public void Bind(HousingCatalogueController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnHousingUpdated += RefreshUI;

            if (_moveToSelectedButton != null)
            {
                _moveToSelectedButton.onClick.AddListener(() =>
                {
                    bool ok = _controller.MoveToSelected(out string err);
                    if (_housingFeedbackText != null)
                    {
                        _housingFeedbackText.text = ok ? "Lease signed! Moved successfully." : err;
                    }
                });
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnHousingUpdated -= RefreshUI;
            }
        }

        public void RefreshUI()
        {
            if (_controller == null) return;

            // Current
            if (_currentHousingNameText != null) _currentHousingNameText.text = _controller.CurrentHousingName;
            if (_currentRentText != null) _currentRentText.text = _controller.CurrentRentText;
            if (_currentDepositText != null) _currentDepositText.text = _controller.CurrentDepositPaidText;
            if (_currentComfortText != null) _currentComfortText.text = $"Comfort: {_controller.CurrentComfortRating}/100";
            if (_currentRecoveryText != null) _currentRecoveryText.text = $"+{_controller.CurrentEnergyRecovery} energy/night";

            // Selected
            var selected = _controller.SelectedTier;
            if (selected != null)
            {
                if (_selectedTierNameText != null) _selectedTierNameText.text = selected.Name;
                if (_selectedRentText != null) _selectedRentText.text = $"{selected.MonthlyRent}/mo";
                if (_selectedDepositText != null) _selectedDepositText.text = $"{selected.DepositCost} (Deposit)";
                if (_selectedComfortText != null) _selectedComfortText.text = $"Comfort: {selected.ComfortRating}/100";
                if (_selectedRecoveryText != null) _selectedRecoveryText.text = $"+{selected.EnergyRecoveryPerNight} energy/night";
                if (_selectedDescriptionText != null) _selectedDescriptionText.text = selected.Description;

                if (_selectedRequirementsText != null)
                {
                    string incomeReq = selected.MinIncomeRequirement.MinorUnits > 0
                        ? $"Min Income: {selected.MinIncomeRequirement}/mo"
                        : "Min Income: None";
                    _selectedRequirementsText.text = incomeReq;
                }

                if (_moveToSelectedButton != null)
                {
                    bool isCurrent = _controller.IsSelectedCurrent;
                    bool canMove = _controller.CanMoveToSelected(out string reason);

                    _moveToSelectedButton.interactable = !isCurrent && canMove;
                    if (isCurrent && _housingFeedbackText != null)
                    {
                        _housingFeedbackText.text = "Current Residence";
                    }
                    else if (!canMove && _housingFeedbackText != null)
                    {
                        _housingFeedbackText.text = reason;
                    }
                }
            }
        }
    }
}

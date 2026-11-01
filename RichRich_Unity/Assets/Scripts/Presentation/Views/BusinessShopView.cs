using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the Auto Repair Business card, purchase, and upgrade mechanics.
    /// </summary>
    public class BusinessShopView : MonoBehaviour
    {
        [Header("Card Labels")]
        [SerializeField] private Text _businessNameText;
        [SerializeField] private Text _levelBadgeText;
        [SerializeField] private Text _hourlyIncomeText;
        [SerializeField] private Text _upgradeBenefitText;

        [Header("Action Controls")]
        [SerializeField] private Button _actionButton;
        [SerializeField] private Text _actionButtonText;
        [SerializeField] private Text _errorMessageText;

        private BusinessViewController _controller;

        public void Bind(BusinessViewController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnBusinessViewUpdated += RefreshUI;
            _controller.OnActionFailed += ShowError;

            if (_actionButton != null)
            {
                _actionButton.onClick.AddListener(OnActionButtonClicked);
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnBusinessViewUpdated -= RefreshUI;
                _controller.OnActionFailed -= ShowError;
            }
        }

        public void RefreshUI()
        {
            if (_controller == null) return;

            if (_businessNameText != null) _businessNameText.text = _controller.BusinessName;
            if (_levelBadgeText != null) _levelBadgeText.text = _controller.LevelBadgeText;
            if (_hourlyIncomeText != null) _hourlyIncomeText.text = _controller.HourlyIncomeText;
            if (_upgradeBenefitText != null) _upgradeBenefitText.text = _controller.UpgradeBenefitDescription;

            if (_actionButtonText != null) _actionButtonText.text = _controller.ActionButtonText;
            if (_actionButton != null) _actionButton.interactable = _controller.CanAffordAction;

            if (_errorMessageText != null) _errorMessageText.text = string.Empty;
        }

        private void OnActionButtonClicked()
        {
            if (_controller == null) return;
            if (!_controller.TryExecuteAction(out string error))
            {
                ShowError(error);
            }
        }

        private void ShowError(string error)
        {
            if (_errorMessageText != null)
            {
                _errorMessageText.text = error;
            }
        }
    }
}

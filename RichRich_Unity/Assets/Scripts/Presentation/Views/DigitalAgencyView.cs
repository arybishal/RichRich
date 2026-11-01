using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the Digital Marketing Agency service operations.
    /// Manages corporate client contracts, skill requirement verification, project hours contribution,
    /// and campaign milestone payouts.
    /// Follows premium dark FinTech design direction.
    /// </summary>
    public class DigitalAgencyView : MonoBehaviour
    {
        [Header("Agency Header")]
        [SerializeField] private Text _agencyTitleText;
        [SerializeField] private Text _agencyReputationText;
        [SerializeField] private Text _agencyStatusText;

        [Header("Selected Contract Details")]
        [SerializeField] private Text _clientNameText;
        [SerializeField] private Text _campaignCategoryText;
        [SerializeField] private Text _contractPayoutText;
        [SerializeField] private Text _hoursProgressText;
        [SerializeField] private Slider _contractProgressSlider;
        [SerializeField] private Text _deadlineDaysText;

        [Header("Actions")]
        [SerializeField] private Button _acceptContractButton;
        [SerializeField] private Button _contribute4HoursButton;
        [SerializeField] private Button _claimPayoutButton;
        [SerializeField] private Text _feedbackText;

        private DigitalAgencyController _controller;

        public void Bind(DigitalAgencyController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnAgencyUpdated += RefreshUI;
            _controller.OnProjectCompleted += OnProjectDone;
            _controller.OnActionFailed += OnFailed;

            if (_acceptContractButton != null)
            {
                _acceptContractButton.onClick.AddListener(() =>
                {
                    if (_controller.SelectedProject != null)
                        _controller.TryAcceptProject(_controller.SelectedProject.ProjectId, out _);
                });
            }

            if (_contribute4HoursButton != null)
            {
                _contribute4HoursButton.onClick.AddListener(() =>
                {
                    if (_controller.SelectedProject != null)
                        _controller.TryWorkOnProject(_controller.SelectedProject.ProjectId, 4, out _);
                });
            }

            if (_claimPayoutButton != null)
            {
                _claimPayoutButton.onClick.AddListener(() =>
                {
                    if (_controller.SelectedProject != null)
                        _controller.TryClaimProjectPayout(_controller.SelectedProject.ProjectId, out _, out _);
                });
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_controller == null) return;

            if (_agencyTitleText != null)
                _agencyTitleText.text = $"Apex Digital Agency (Level {_controller.AgencyLevel})";
            if (_agencyReputationText != null)
                _agencyReputationText.text = $"Client Trust / Reputation: {_controller.Reputation}/100";
            if (_agencyStatusText != null)
                _agencyStatusText.text = _controller.IsOpen ? "STATUS: ACTIVE" : "STATUS: CLOSED";

            var proj = _controller.SelectedProject;
            if (proj != null)
            {
                if (_clientNameText != null) _clientNameText.text = $"Client: {proj.ClientName}";
                if (_campaignCategoryText != null) _campaignCategoryText.text = $"Service: {proj.Category}";
                if (_contractPayoutText != null) _contractPayoutText.text = $"Contract Payout: {proj.ContractPayment}";
                if (_hoursProgressText != null) _hoursProgressText.text = $"Work Completed: {proj.CompletedHours} / {proj.TotalHoursRequired} hrs";
                if (_contractProgressSlider != null) _contractProgressSlider.value = proj.ProgressNormalized;
                if (_deadlineDaysText != null) _deadlineDaysText.text = $"Days Remaining: {proj.DaysRemaining} days";

                if (_acceptContractButton != null) _acceptContractButton.interactable = !proj.IsAccepted && !proj.IsCompleted;
                if (_contribute4HoursButton != null) _contribute4HoursButton.interactable = proj.IsAccepted && !proj.IsCompleted;
                if (_claimPayoutButton != null) _claimPayoutButton.interactable = proj.IsCompleted;
            }
        }

        private void OnProjectDone(ClientProject proj)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Project '{proj.ClientName}' completed! Payout ready for collection.";
        }

        private void OnFailed(string error)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Agency action failed: {error}";
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnAgencyUpdated -= RefreshUI;
                _controller.OnProjectCompleted -= OnProjectDone;
                _controller.OnActionFailed -= OnFailed;
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the Auto Repair Workshop operations.
    /// Manages service bay allocation, repair task progression, and payout collection.
    /// Follows premium dark FinTech design direction.
    /// </summary>
    public class AutoRepairView : MonoBehaviour
    {
        [Header("Workshop Header")]
        [SerializeField] private Text _workshopTitleText;
        [SerializeField] private Text _workshopStatusText;
        [SerializeField] private Text _baySummaryText;

        [Header("Active Service Bay 1")]
        [SerializeField] private Text _bay1VehicleText;
        [SerializeField] private Slider _bay1ProgressSlider;
        [SerializeField] private Button _bay1ClaimButton;

        [Header("Incoming Repair Queue")]
        [SerializeField] private Text _job1TitleText;
        [SerializeField] private Text _job1CategoryText;
        [SerializeField] private Text _job1FinancialsText;
        [SerializeField] private Button _job1AssignBay1Button;

        [Header("Feedback")]
        [SerializeField] private Text _feedbackText;

        private AutoRepairController _controller;

        public void Bind(AutoRepairController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnWorkshopUpdated += RefreshUI;
            _controller.OnJobCompleted += OnJobDone;
            _controller.OnActionFailed += OnFailed;

            if (_bay1ClaimButton != null)
            {
                _bay1ClaimButton.onClick.AddListener(() =>
                {
                    _controller.TryDeliverRepairedVehicle(1, out _, out _);
                });
            }

            if (_job1AssignBay1Button != null)
            {
                _job1AssignBay1Button.onClick.AddListener(() =>
                {
                    var job = _controller.AvailableJobs.Count > 0 ? _controller.AvailableJobs[0] : null;
                    if (job != null)
                    {
                        _controller.TryAssignJobToBay(job.JobId, 1, out _);
                    }
                });
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_controller == null) return;

            if (_workshopTitleText != null)
                _workshopTitleText.text = $"Auto Repair Workshop (Level {_controller.WorkshopLevel})";
            if (_workshopStatusText != null)
                _workshopStatusText.text = _controller.IsOpen ? "STATUS: OPERATIONAL" : "STATUS: CLOSED";

            var bays = _controller.ServiceBays;
            if (_baySummaryText != null)
            {
                int occupied = 0;
                for (int i = 0; i < bays.Count; i++) if (bays[i].IsOccupied) occupied++;
                _baySummaryText.text = $"Bays: {occupied} / {bays.Count} Occupied";
            }

            if (bays.Count > 0)
            {
                var bay1 = bays[0];
                if (bay1.IsOccupied && bay1.CurrentJob != null)
                {
                    if (_bay1VehicleText != null)
                        _bay1VehicleText.text = $"{bay1.CurrentJob.VehicleModel} ({bay1.CurrentJob.Category})";
                    if (_bay1ProgressSlider != null)
                        _bay1ProgressSlider.value = bay1.CurrentJob.ProgressNormalized;
                    if (_bay1ClaimButton != null)
                        _bay1ClaimButton.interactable = bay1.CurrentJob.IsCompleted;
                }
                else
                {
                    if (_bay1VehicleText != null) _bay1VehicleText.text = "Service Bay 1: Empty";
                    if (_bay1ProgressSlider != null) _bay1ProgressSlider.value = 0f;
                    if (_bay1ClaimButton != null) _bay1ClaimButton.interactable = false;
                }
            }

            var jobs = _controller.AvailableJobs;
            if (jobs.Count > 0)
            {
                var j1 = jobs[0];
                if (_job1TitleText != null) _job1TitleText.text = $"{j1.VehicleModel} - {j1.CustomerName}";
                if (_job1CategoryText != null) _job1CategoryText.text = $"Category: {j1.Category} (Min Level {j1.RequiredLevel})";
                if (_job1FinancialsText != null) _job1FinancialsText.text = $"Parts: {j1.PartsCost} | Payout: {j1.CustomerPayment}";
            }
            else
            {
                if (_job1TitleText != null) _job1TitleText.text = "No repair jobs in queue.";
            }
        }

        private void OnJobDone(VehicleRepairJob job)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Repair completed for {job.VehicleModel}! Ready for delivery.";
        }

        private void OnFailed(string error)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Action failed: {error}";
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnWorkshopUpdated -= RefreshUI;
                _controller.OnJobCompleted -= OnJobDone;
                _controller.OnActionFailed -= OnFailed;
            }
        }
    }
}

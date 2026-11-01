using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the interactive business operational task:
    /// e.g. "Vehicle Diagnostic & Service" mini-task.
    /// Real-time progress bar, timer display, claim button, and abort functionality.
    /// </summary>
    public class BusinessTaskView : MonoBehaviour
    {
        [Header("Task Info")]
        [SerializeField] private Text _taskTitleText;
        [SerializeField] private Text _taskRewardText;
        [SerializeField] private Slider _taskProgressSlider;
        [SerializeField] private Text _timerText;

        [Header("Action Buttons")]
        [SerializeField] private Button _primaryActionButton;
        [SerializeField] private Text _primaryActionText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Text _statusMessageText;

        private BusinessTaskController _controller;

        public void Bind(BusinessTaskController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnTaskUIUpdated += RefreshUI;
            _controller.OnRewardClaimed += HandleRewardClaimed;
            _controller.OnTaskError += ShowError;

            if (_primaryActionButton != null)
            {
                _primaryActionButton.onClick.AddListener(OnPrimaryActionClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnTaskUIUpdated -= RefreshUI;
                _controller.OnRewardClaimed -= HandleRewardClaimed;
                _controller.OnTaskError -= ShowError;
            }
        }

        private void Update()
        {
            // Smooth progress interpolation while task is running
            if (_controller != null && _controller.State == BusinessTaskState.Running)
            {
                if (_taskProgressSlider != null)
                {
                    _taskProgressSlider.value = _controller.Progress;
                }
                if (_timerText != null)
                {
                    _timerText.text = _controller.TimeRemainingFormatted;
                }
            }
        }

        public void RefreshUI()
        {
            if (_controller == null) return;

            if (_taskTitleText != null) _taskTitleText.text = _controller.TaskName;
            if (_taskRewardText != null) _taskRewardText.text = $"Payout: {_controller.RewardFormatted}";
            if (_taskProgressSlider != null) _taskProgressSlider.value = _controller.Progress;
            if (_timerText != null) _timerText.text = _controller.TimeRemainingFormatted;

            if (_primaryActionText != null)
            {
                _primaryActionText.text = _controller.StatusButtonText;
            }

            if (_primaryActionButton != null)
            {
                _primaryActionButton.interactable = _controller.CanStartTask || _controller.CanClaimReward;
            }

            if (_cancelButton != null)
            {
                _cancelButton.gameObject.SetActive(_controller.CanCancelTask);
            }
        }

        private void OnPrimaryActionClicked()
        {
            if (_controller == null) return;

            if (_controller.CanStartTask)
            {
                if (_controller.TryStartTask(out string error))
                {
                    ShowMessage("Diagnostics running...");
                }
                else
                {
                    ShowError(error);
                }
            }
            else if (_controller.CanClaimReward)
            {
                if (_controller.TryClaimReward(out Currency reward, out string error))
                {
                    ShowMessage($"Collected +{reward}!");
                }
                else
                {
                    ShowError(error);
                }
            }
        }

        private void OnCancelClicked()
        {
            if (_controller == null) return;
            if (_controller.TryCancelTask(out string error))
            {
                ShowMessage("Task aborted.");
            }
            else
            {
                ShowError(error);
            }
        }

        private void HandleRewardClaimed(Currency reward)
        {
            ShowMessage($"Task Complete: +{reward} credited to Wallet!");
        }

        private void ShowMessage(string message)
        {
            if (_statusMessageText != null)
            {
                _statusMessageText.text = message;
            }
        }

        private void ShowError(string error)
        {
            if (_statusMessageText != null)
            {
                _statusMessageText.text = $"<color=red>{error}</color>";
            }
        }
    }
}

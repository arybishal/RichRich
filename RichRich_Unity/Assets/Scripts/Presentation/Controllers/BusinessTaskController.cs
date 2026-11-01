using System;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation controller managing the playable business operational mini-task:
    /// e.g. "Engine Diagnostics" in the Auto Repair workshop.
    /// Handles task triggers, real-time progress interpolation, button state, and reward dispatching.
    /// Pure C# view-model controller with zero MonoBehaviour coupling.
    /// </summary>
    public class BusinessTaskController
    {
        public Business Business { get; }
        public BusinessTask Task { get; }
        public Wallet Wallet { get; }

        public string TaskName => Task.Name;
        public BusinessTaskState State => Task.State;
        public float Progress => Task.ProgressNormalized;
        public double DurationSeconds => Task.DurationSeconds;
        public double ElapsedSeconds => Task.ElapsedSeconds;
        public double TimeRemainingSeconds => Math.Max(0.0, Task.DurationSeconds - Task.ElapsedSeconds);

        public string RewardFormatted => Task.Reward.ToString();
        public string TimeRemainingFormatted => $"{TimeRemainingSeconds:F1}s";

        public bool CanStartTask => Business.IsOwned && Task.State == BusinessTaskState.Idle;
        public bool CanClaimReward => Task.State == BusinessTaskState.Completed && !Task.IsRewardClaimed;
        public bool CanCancelTask => Task.State == BusinessTaskState.Running;

        public string StatusButtonText
        {
            get
            {
                if (!Business.IsOwned)
                {
                    return "Requires Auto Repair Shop";
                }

                return Task.State switch
                {
                    BusinessTaskState.Idle => $"Start Diagnostics ({RewardFormatted})",
                    BusinessTaskState.Running => $"Servicing... ({TimeRemainingFormatted})",
                    BusinessTaskState.Completed => $"Claim Reward ({RewardFormatted})",
                    BusinessTaskState.Cancelled => "Task Cancelled - Retry",
                    _ => "Ready"
                };
            }
        }

        public event Action OnTaskUIUpdated;
        public event Action<Currency> OnRewardClaimed;
        public event Action<string> OnTaskError;

        public BusinessTaskController(Business business, BusinessTask task, Wallet wallet)
        {
            Business = business ?? throw new ArgumentNullException(nameof(business));
            Task = task ?? throw new ArgumentNullException(nameof(task));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));

            Task.OnTaskUpdated += _ => OnTaskUIUpdated?.Invoke();
            Task.OnTaskCompleted += _ => OnTaskUIUpdated?.Invoke();
            Business.OnBusinessUpdated += _ => OnTaskUIUpdated?.Invoke();
        }

        public bool TryStartTask(out string error)
        {
            if (!Business.IsOwned)
            {
                error = $"Cannot start task: You must first purchase '{Business.Name}'.";
                OnTaskError?.Invoke(error);
                return false;
            }

            if (Task.State == BusinessTaskState.Running)
            {
                error = "Task is already actively running.";
                OnTaskError?.Invoke(error);
                return false;
            }

            Task.Start();
            error = null;
            OnTaskUIUpdated?.Invoke();
            return true;
        }

        public bool TryClaimReward(out Currency rewardGranted, out string error)
        {
            if (!Task.TryClaimReward(Wallet, out rewardGranted, out error))
            {
                OnTaskError?.Invoke(error);
                return false;
            }

            OnRewardClaimed?.Invoke(rewardGranted);
            OnTaskUIUpdated?.Invoke();
            return true;
        }

        public bool TryCancelTask(out string error)
        {
            if (Task.State != BusinessTaskState.Running)
            {
                error = "Cannot cancel: Task is not currently running.";
                OnTaskError?.Invoke(error);
                return false;
            }

            Task.Cancel();
            error = null;
            OnTaskUIUpdated?.Invoke();
            return true;
        }

        public void Tick(double deltaSeconds)
        {
            if (Task.State == BusinessTaskState.Running)
            {
                Task.Tick(deltaSeconds);
            }
        }
    }
}

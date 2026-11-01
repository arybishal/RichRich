using System;

namespace RichRich.Domain.Models
{
    public enum BusinessTaskState
    {
        Idle,
        Running,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Interactive operational mini-task within a business (e.g. Car Diagnostics / Repair).
    /// Tracks execution lifecycle, progress feedback, reward claiming, and duplicate prevention.
    /// </summary>
    public class BusinessTask
    {
        public string Id { get; }
        public string Name { get; }
        public double DurationSeconds { get; }
        public Currency Reward { get; }
        public BusinessTaskState State { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public bool IsRewardClaimed { get; private set; }

        public float ProgressNormalized => (float)Math.Clamp(ElapsedSeconds / DurationSeconds, 0.0, 1.0);

        public event Action<BusinessTask> OnTaskUpdated;
        public event Action<BusinessTask> OnTaskCompleted;

        public BusinessTask(string id, string name, double durationSeconds, Currency reward)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DurationSeconds = durationSeconds > 0 ? durationSeconds : throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            Reward = reward;
            State = BusinessTaskState.Idle;
            ElapsedSeconds = 0.0;
            IsRewardClaimed = false;
        }

        public void Start()
        {
            if (State == BusinessTaskState.Running)
            {
                throw new InvalidOperationException("Task is already in progress.");
            }

            State = BusinessTaskState.Running;
            ElapsedSeconds = 0.0;
            IsRewardClaimed = false;
            OnTaskUpdated?.Invoke(this);
        }

        public void Tick(double deltaSeconds)
        {
            if (State != BusinessTaskState.Running) return;

            ElapsedSeconds += deltaSeconds;
            if (ElapsedSeconds >= DurationSeconds)
            {
                ElapsedSeconds = DurationSeconds;
                State = BusinessTaskState.Completed;
                OnTaskCompleted?.Invoke(this);
            }
            OnTaskUpdated?.Invoke(this);
        }

        public void Cancel()
        {
            if (State != BusinessTaskState.Running) return;

            State = BusinessTaskState.Cancelled;
            ElapsedSeconds = 0.0;
            OnTaskUpdated?.Invoke(this);
        }

        public bool TryClaimReward(Wallet wallet, out Currency rewardGranted, out string error)
        {
            if (State != BusinessTaskState.Completed)
            {
                rewardGranted = Currency.Zero;
                error = "Cannot claim reward: Task is not yet completed.";
                return false;
            }

            if (IsRewardClaimed)
            {
                rewardGranted = Currency.Zero;
                error = "Reward has already been claimed for this task run.";
                return false;
            }

            IsRewardClaimed = true;
            rewardGranted = Reward;
            error = null;
            wallet.Add(Reward);
            State = BusinessTaskState.Idle;
            OnTaskUpdated?.Invoke(this);
            return true;
        }
    }
}

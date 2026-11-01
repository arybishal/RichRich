using System;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Core application service coordinating the game economy:
    /// Active tapping, level milestones, business purchases, upgrades, passive income, and tasks.
    /// Pure C# design with zero MonoBehaviour coupling.
    /// </summary>
    public class EconomyService
    {
        public Wallet Wallet { get; }
        public Business Business { get; }
        public BusinessTask CurrentTask { get; }
        public ITimeProvider TimeProvider { get; }

        public int CurrentLevel { get; private set; }
        public Currency LevelTargetBalance { get; private set; }
        public bool IsBoosterActive => _boosterRemainingSeconds > 0;
        public double BoosterRemainingSeconds => _boosterRemainingSeconds;

        private double _boosterRemainingSeconds;
        private double _passiveAccumulatorSeconds;

        public event Action<Currency> OnTapped;
        public event Action<int, Currency> OnLevelUp;
        public event Action<bool, double> OnBoosterStateChanged;
        public event Action<Currency> OnPassiveIncomeCredited;

        public EconomyService(
            Wallet wallet,
            Business business,
            BusinessTask task,
            ITimeProvider timeProvider,
            int currentLevel = 1)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Business = business ?? throw new ArgumentNullException(nameof(business));
            CurrentTask = task ?? throw new ArgumentNullException(nameof(task));
            TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

            CurrentLevel = Math.Max(1, currentLevel);
            RecalculateLevelTarget();
        }

        public Currency GetCurrentTapYield()
        {
            Currency yieldPerTap = EconomyConfig.BaseTapIncome;
            if (IsBoosterActive)
            {
                yieldPerTap = yieldPerTap * EconomyConfig.TapMultiplierFactor;
            }
            return yieldPerTap;
        }

        public Currency Tap()
        {
            Currency earned = GetCurrentTapYield();
            Wallet.Add(earned);
            OnTapped?.Invoke(earned);

            CheckLevelUp();
            return earned;
        }

        public void ActivateTapBooster(double durationSeconds = EconomyConfig.TapMultiplierDurationSeconds)
        {
            _boosterRemainingSeconds = durationSeconds;
            OnBoosterStateChanged?.Invoke(true, _boosterRemainingSeconds);
        }

        public bool TryBuyStarterBusiness(out string error)
        {
            if (Business.IsOwned)
            {
                error = $"Business '{Business.Name}' is already owned.";
                return false;
            }

            Currency cost = Business.GetPurchaseCost();
            if (!Wallet.TrySpend(cost, out error))
            {
                return false;
            }

            Business.Acquire();
            error = null;
            return true;
        }

        public bool TryUpgradeBusiness(out string error)
        {
            if (!Business.IsOwned)
            {
                error = $"Cannot upgrade unowned business '{Business.Name}'.";
                return false;
            }

            Currency cost = Business.GetUpgradeCost();
            if (!Wallet.TrySpend(cost, out error))
            {
                return false;
            }

            Business.Upgrade();
            error = null;
            return true;
        }

        public Currency TotalPassiveHourlyIncome => Business.GetHourlyIncome();

        public void Tick(double deltaSeconds)
        {
            if (deltaSeconds <= 0) return;

            // 1. Tick Multiplier Booster
            if (_boosterRemainingSeconds > 0)
            {
                _boosterRemainingSeconds -= deltaSeconds;
                if (_boosterRemainingSeconds <= 0)
                {
                    _boosterRemainingSeconds = 0;
                    OnBoosterStateChanged?.Invoke(false, 0);
                }
            }

            // 2. Tick Passive Income Accumulation
            if (Business.IsOwned)
            {
                _passiveAccumulatorSeconds += deltaSeconds;
                // Credit every second to maintain smooth rolling feedback
                if (_passiveAccumulatorSeconds >= 1.0)
                {
                    double hoursElapsed = _passiveAccumulatorSeconds / 3600.0;
                    Currency hourly = Business.GetHourlyIncome();
                    Currency passiveEarned = hourly * hoursElapsed;

                    if (passiveEarned.MinorUnits > 0)
                    {
                        Wallet.Add(passiveEarned);
                        OnPassiveIncomeCredited?.Invoke(passiveEarned);
                    }
                    _passiveAccumulatorSeconds = 0.0;
                }
            }

            // 3. Tick Active Operational Task
            if (CurrentTask.State == BusinessTaskState.Running)
            {
                CurrentTask.Tick(deltaSeconds);
            }

            CheckLevelUp();
        }

        public float GetLevelProgressNormalized()
        {
            if (LevelTargetBalance.MinorUnits == 0) return 1.0f;
            double progress = (double)Wallet.LifetimeEarned.MinorUnits / LevelTargetBalance.MinorUnits;
            return (float)Math.Clamp(progress, 0.0, 1.0);
        }

        private void CheckLevelUp()
        {
            if (Wallet.LifetimeEarned >= LevelTargetBalance)
            {
                CurrentLevel++;
                RecalculateLevelTarget();
                OnLevelUp?.Invoke(CurrentLevel, LevelTargetBalance);
            }
        }

        private void RecalculateLevelTarget()
        {
            double scale = Math.Pow(EconomyConfig.LevelTargetMultiplier, CurrentLevel - 1);
            LevelTargetBalance = EconomyConfig.Level1TargetBalance * scale;
        }
    }
}

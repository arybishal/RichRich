using System;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation controller for the top HUD, credit card balance, tap mechanic, and level progression.
    /// Manages formatting, tap booster countdowns, and reactive UI state updates.
    /// Pure C# view-model controller with zero MonoBehaviour coupling.
    /// </summary>
    public class MainHUDController
    {
        public EconomyService Economy { get; }
        public Wallet Wallet => Economy.Wallet;

        public string BalanceText => Wallet.Balance.ToString();
        public string LifetimeEarnedText => Wallet.LifetimeEarned.ToString();
        public string PassiveRateText => $"+{Economy.TotalPassiveHourlyIncome}/hr";
        public string LevelBadgeText => $"Level {Economy.CurrentLevel}";
        public string TapYieldText => $"+{Economy.GetCurrentTapYield()}";
        public float LevelProgressNormalized => Economy.GetLevelProgressNormalized();
        public string LevelProgressRatioText => $"{Wallet.LifetimeEarned} / {Economy.LevelTargetBalance}";

        public bool IsBoosterActive => Economy.IsBoosterActive;
        public double BoosterRemainingSeconds => Economy.BoosterRemainingSeconds;
        public string BoosterStatusText => IsBoosterActive
            ? $"6x BOOST ACTIVE ({BoosterRemainingSeconds:F0}s)"
            : "Activate 6x Boost (30s)";

        public event Action OnHUDUpdated;
        public event Action<Currency> OnTapFeedback;
        public event Action<int, Currency> OnLevelUpFeedback;

        public MainHUDController(EconomyService economy)
        {
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));

            Wallet.OnBalanceChanged += _ => OnHUDUpdated?.Invoke();
            Economy.OnTapped += earned =>
            {
                OnTapFeedback?.Invoke(earned);
                OnHUDUpdated?.Invoke();
            };
            Economy.OnLevelUp += (lvl, target) =>
            {
                OnLevelUpFeedback?.Invoke(lvl, target);
                OnHUDUpdated?.Invoke();
            };
            Economy.OnBoosterStateChanged += (_, _) => OnHUDUpdated?.Invoke();
            Economy.OnPassiveIncomeCredited += _ => OnHUDUpdated?.Invoke();
        }

        public Currency HandleTap()
        {
            return Economy.Tap();
        }

        public void ActivateBooster()
        {
            Economy.ActivateTapBooster();
            OnHUDUpdated?.Invoke();
        }

        public void Tick(double deltaSeconds)
        {
            Economy.Tick(deltaSeconds);
        }
    }
}

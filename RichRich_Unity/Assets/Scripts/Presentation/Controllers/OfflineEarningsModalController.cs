using System;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation controller for the Welcome Back / Offline Earnings popup modal.
    /// Pure C# view-model controller with zero MonoBehaviour coupling.
    /// </summary>
    public class OfflineEarningsModalController
    {
        public OfflineIncomeResult? Result { get; }
        public Wallet Wallet { get; }

        public bool HasClaimableEarnings => Result.HasValue && Result.Value.AmountEarned.MinorUnits > 0;
        public Currency AmountEarned => Result?.AmountEarned ?? Currency.Zero;
        public string AmountEarnedText => AmountEarned.ToString();
        public long SecondsAway => Result?.SecondsClaimed ?? 0L;

        public string DurationAwayFormatted
        {
            get
            {
                if (SecondsAway <= 0) return "0s";
                var ts = TimeSpan.FromSeconds(SecondsAway);
                if (ts.TotalHours >= 1.0)
                {
                    return $"{(int)ts.TotalHours}h {ts.Minutes}m";
                }
                return $"{ts.Minutes}m {ts.Seconds}s";
            }
        }

        public string SubtitleText => Result.HasValue && Result.Value.WasCapped
            ? $"Away for {DurationAwayFormatted} (offline income capped at 3h)"
            : $"Away for {DurationAwayFormatted}";

        public bool IsClaimed { get; private set; }

        public event Action<Currency> OnEarningsClaimed;

        public OfflineEarningsModalController(OfflineIncomeResult? result, Wallet wallet)
        {
            Result = result;
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            IsClaimed = false;
        }

        public bool TryClaim(out Currency claimed, out string error)
        {
            if (IsClaimed)
            {
                claimed = Currency.Zero;
                error = "Offline earnings have already been claimed.";
                return false;
            }

            if (!HasClaimableEarnings)
            {
                claimed = Currency.Zero;
                error = "No offline earnings to claim.";
                return false;
            }

            IsClaimed = true;
            claimed = AmountEarned;
            error = null;

            Wallet.Add(claimed);
            OnEarningsClaimed?.Invoke(claimed);
            return true;
        }
    }
}

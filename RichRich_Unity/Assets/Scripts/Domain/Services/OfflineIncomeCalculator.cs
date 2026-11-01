using System;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    public readonly struct OfflineIncomeResult
    {
        public Currency AmountEarned { get; }
        public long SecondsClaimed { get; }
        public bool WasCapped { get; }
        public bool IsClockTampered { get; }

        public OfflineIncomeResult(Currency amountEarned, long secondsClaimed, bool wasCapped, bool isClockTampered)
        {
            AmountEarned = amountEarned;
            SecondsClaimed = secondsClaimed;
            WasCapped = wasCapped;
            IsClockTampered = isClockTampered;
        }
    }

    /// <summary>
    /// Computes offline catch-up earnings upon game startup.
    /// Enforces the 3-hour cap and protects against backward clock manipulation.
    /// </summary>
    public static class OfflineIncomeCalculator
    {
        public static OfflineIncomeResult Calculate(
            long lastSavedUnix,
            long currentUnix,
            Currency hourlyIncome,
            int maxOfflineSeconds = EconomyConfig.MaxOfflineIncomeSeconds)
        {
            if (lastSavedUnix <= 0 || hourlyIncome.MinorUnits == 0)
            {
                return new OfflineIncomeResult(Currency.Zero, 0L, false, false);
            }

            // Anti-Tamper: Clock rolled backward
            if (currentUnix < lastSavedUnix)
            {
                return new OfflineIncomeResult(Currency.Zero, 0L, false, true);
            }

            long elapsedSeconds = currentUnix - lastSavedUnix;
            bool wasCapped = elapsedSeconds > maxOfflineSeconds;
            long creditedSeconds = Math.Min(elapsedSeconds, maxOfflineSeconds);

            double hoursElapsed = (double)creditedSeconds / 3600.0;
            Currency earned = hourlyIncome * hoursElapsed;

            return new OfflineIncomeResult(earned, creditedSeconds, wasCapped, false);
        }
    }
}

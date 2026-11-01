using System;

namespace RichRich.Domain.Models
{
    /// <summary>
    /// Financial balance sheet snapshot tracking Cash, Assets, Liabilities, and Net Worth.
    /// </summary>
    public class BalanceSheet
    {
        public Currency Cash { get; private set; }
        public Currency NonCashAssets { get; private set; }
        public Currency LoanLiabilities { get; private set; }
        public Currency OverdueLiabilities { get; private set; }

        public Currency TotalAssets => Cash + NonCashAssets;
        public Currency TotalLiabilities => LoanLiabilities + OverdueLiabilities;

        public long NetWorthCents => (long)TotalAssets.MinorUnits - (long)TotalLiabilities.MinorUnits;
        public bool IsNegativeNetWorth => NetWorthCents < 0;

        public string NetWorthFormatted
        {
            get
            {
                long cents = NetWorthCents;
                if (cents >= 0)
                {
                    return Currency.FromCents(cents).ToString();
                }
                else
                {
                    return "-" + Currency.FromCents(-cents).ToString();
                }
            }
        }

        public BalanceSheet(Currency cash, Currency nonCashAssets, Currency loanLiabilities, Currency overdueLiabilities)
        {
            Cash = cash;
            NonCashAssets = nonCashAssets;
            LoanLiabilities = loanLiabilities;
            OverdueLiabilities = overdueLiabilities;
        }

        public BalanceSheet() : this(Currency.Zero, Currency.Zero, Currency.Zero, Currency.Zero)
        {
        }

        public void UpdateCash(Currency cash) => Cash = cash;
        public void UpdateNonCashAssets(Currency assets) => NonCashAssets = assets;
        public void UpdateLoanLiabilities(Currency loans) => LoanLiabilities = loans;
        public void UpdateOverdueLiabilities(Currency overdue) => OverdueLiabilities = overdue;
    }
}

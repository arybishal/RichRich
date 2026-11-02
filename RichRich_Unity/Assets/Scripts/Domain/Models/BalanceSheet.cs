using System;

namespace RichRich.Domain.Models
{
    /// <summary>
    /// Financial balance sheet snapshot tracking Cash, Real Estate, Businesses, Stocks,
    /// Personal Loans, Mortgages, Overdue Liabilities, and Total Net Worth.
    /// Preserves full backward-compatibility with existing financial services.
    /// </summary>
    public class BalanceSheet
    {
        public Currency Cash { get; private set; }

        // Asset Sub-accounts
        public Currency BusinessAssets { get; private set; }
        public Currency RealEstateAssets { get; private set; }
        public Currency PortfolioAssets { get; private set; }

        public Currency NonCashAssets { get; private set; }

        // Liability Sub-accounts
        public Currency LoanLiabilities { get; private set; }
        public Currency MortgageLiabilities { get; private set; }
        public Currency OverdueLiabilities { get; private set; }

        public Currency TotalAssets => Cash + NonCashAssets;
        public Currency TotalLiabilities => LoanLiabilities + MortgageLiabilities + OverdueLiabilities;

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
            BusinessAssets = nonCashAssets;
            RealEstateAssets = Currency.Zero;
            PortfolioAssets = Currency.Zero;

            LoanLiabilities = loanLiabilities;
            MortgageLiabilities = Currency.Zero;
            OverdueLiabilities = overdueLiabilities;
        }

        public BalanceSheet() : this(Currency.Zero, Currency.Zero, Currency.Zero, Currency.Zero)
        {
        }

        public void UpdateCash(Currency cash) => Cash = cash;

        public void UpdateNonCashAssets(Currency totalAssets)
        {
            BusinessAssets = totalAssets;
            RecalculateNonCashAssets();
        }

        public void UpdateBusinessAssets(Currency businessAssets)
        {
            BusinessAssets = businessAssets;
            RecalculateNonCashAssets();
        }

        public void UpdateRealEstateAssets(Currency realEstateAssets)
        {
            RealEstateAssets = realEstateAssets;
            RecalculateNonCashAssets();
        }

        public void UpdatePortfolioAssets(Currency portfolioAssets)
        {
            PortfolioAssets = portfolioAssets;
            RecalculateNonCashAssets();
        }

        private void RecalculateNonCashAssets()
        {
            NonCashAssets = BusinessAssets + RealEstateAssets + PortfolioAssets;
        }

        public void UpdateLoanLiabilities(Currency loans) => LoanLiabilities = loans;
        public void UpdateMortgageLiabilities(Currency mortgages) => MortgageLiabilities = mortgages;
        public void UpdateOverdueLiabilities(Currency overdue) => OverdueLiabilities = overdue;
    }
}

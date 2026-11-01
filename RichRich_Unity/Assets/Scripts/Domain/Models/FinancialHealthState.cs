namespace RichRich.Domain.Models
{
    /// <summary>
    /// Financial health state classification for the player's economic standing.
    /// </summary>
    public enum FinancialHealthState
    {
        /// <summary>Adequate cash buffer (>= 1 mo expenses), positive cash flow, all obligations current.</summary>
        Healthy,

        /// <summary>Negative cash flow or low cash buffer (< 1 mo expenses), but obligations current.</summary>
        FinanciallyStressed,

        /// <summary>Cash insufficient for immediate upcoming payments or active missed obligations.</summary>
        CashCrisis,

        /// <summary>Total liabilities exceed total assets (negative net worth) with persistent deficits.</summary>
        Insolvent,

        /// <summary>Critical default: 2+ missed payments or overdue debts with severe insolvency.</summary>
        Bankrupt,

        /// <summary>Active workout or repayment plan negotiated under distress.</summary>
        Restructuring
    }

    public class FinancialHealthReport
    {
        public FinancialHealthState State { get; }
        public string PrimaryReason { get; }
        public string Recommendation { get; }

        public FinancialHealthReport(FinancialHealthState state, string primaryReason, string recommendation)
        {
            State = state;
            PrimaryReason = primaryReason;
            Recommendation = recommendation;
        }

        public override string ToString() => $"[{State}] {PrimaryReason} -> Advice: {Recommendation}";
    }
}

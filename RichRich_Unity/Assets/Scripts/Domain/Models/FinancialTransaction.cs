using System;

namespace RichRich.Domain.Models
{
    public enum TransactionType
    {
        Income,
        Expense,
        LiabilityDraw,
        LiabilityRepay,
        AssetAcquisition,
        AssetLiquidation
    }

    public enum TransactionCategory
    {
        StartingCapital,
        EmploymentSalary,
        LivingAccommodation,
        LivingFood,
        LivingTransport,
        LivingUtilities,
        LivingInternet,
        LivingPersonal,
        EducationTuition,
        LoanDisbursement,
        LoanInstallmentPrincipal,
        LoanInstallmentInterest,
        LatePenaltyFee,
        BusinessRevenue,
        BusinessExpense,
        AssetPurchase,
        AssetSale,
        Miscellaneous
    }

    /// <summary>
    /// Immutable record of a financial transaction.
    /// Forms the auditable entry in the player's financial ledger.
    /// </summary>
    public class FinancialTransaction
    {
        public string Id { get; }
        public Currency Amount { get; }
        public TransactionType Type { get; }
        public TransactionCategory Category { get; }
        public GameDateTime Date { get; }
        public string Description { get; }
        public string RelatedEntityId { get; }

        public FinancialTransaction(
            string id,
            Currency amount,
            TransactionType type,
            TransactionCategory category,
            GameDateTime date,
            string description,
            string relatedEntityId = null)
        {
            Id = id ?? Guid.NewGuid().ToString("N");
            Amount = amount;
            Type = type;
            Category = category;
            Date = date;
            Description = description ?? string.Empty;
            RelatedEntityId = relatedEntityId;
        }

        public override string ToString()
        {
            string sign = (Type == TransactionType.Income || Type == TransactionType.LiabilityDraw) ? "+" : "-";
            return $"[{Date}] {Category}: {sign}{Amount} ({Description})";
        }
    }
}

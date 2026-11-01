using System;

namespace RichRich.Domain.Models
{
    /// <summary>
    /// Configurable loan product definition offered to the player.
    /// </summary>
    public class LoanProduct
    {
        public string Id { get; }
        public string Name { get; }
        public Currency Principal { get; }
        public double AnnualInterestRate { get; } // e.g. 0.12 for 12%
        public int TermMonths { get; }            // e.g. 12 months
        public Currency RequiredMinimumMonthlyIncome { get; }

        public LoanProduct(
            string id,
            string name,
            Currency principal,
            double annualInterestRate = 0.12,
            int termMonths = 12,
            Currency? requiredMinimumIncome = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Principal = principal;
            AnnualInterestRate = annualInterestRate;
            TermMonths = Math.Max(1, termMonths);
            RequiredMinimumMonthlyIncome = requiredMinimumIncome ?? Currency.FromDollars(800.00);
        }

        public Currency CalculateMonthlyInstallment()
        {
            double monthlyRate = AnnualInterestRate / 12.0;
            if (monthlyRate <= 0.0)
            {
                return Currency.FromCents(Principal.MinorUnits / TermMonths);
            }

            double factor = Math.Pow(1.0 + monthlyRate, TermMonths);
            double installmentDollars = Principal.ToDollars() * (monthlyRate * factor) / (factor - 1.0);
            return Currency.FromDollars(installmentDollars);
        }
    }

    /// <summary>
    /// An active personal loan held by the player.
    /// Tracks outstanding principal, amortization, payment history, and delinquency.
    /// </summary>
    public class ActiveLoan
    {
        public string LoanId { get; }
        public LoanProduct Product { get; }
        public GameDateTime DisbursedDate { get; }
        public Currency InitialPrincipal => Product.Principal;
        public Currency RemainingPrincipal { get; private set; }
        public Currency MonthlyInstallment { get; }
        public int DueDayOfMonth { get; }

        public int MonthsRemaining { get; private set; }
        public int PaymentsMade { get; private set; }
        public int MissedPaymentsCount { get; private set; }
        public Currency OverdueAmount { get; private set; }
        public int LastPaidMonth { get; private set; }

        public bool IsFullyPaid => RemainingPrincipal.MinorUnits == 0 && OverdueAmount.MinorUnits == 0;
        public bool IsDelinquent => MissedPaymentsCount > 0;

        public static readonly Currency LateFeePenalty = Currency.FromDollars(25.00);

        public ActiveLoan(
            string loanId,
            LoanProduct product,
            GameDateTime disbursedDate,
            int dueDayOfMonth = 1,
            Currency? remainingPrincipal = null,
            int? monthsRemaining = null,
            int paymentsMade = 0,
            int missedPaymentsCount = 0,
            Currency? overdueAmount = null,
            int lastPaidMonth = 0)
        {
            LoanId = loanId ?? Guid.NewGuid().ToString("N");
            Product = product ?? throw new ArgumentNullException(nameof(product));
            DisbursedDate = disbursedDate;
            DueDayOfMonth = Math.Clamp(dueDayOfMonth, 1, 30);

            MonthlyInstallment = product.CalculateMonthlyInstallment();
            RemainingPrincipal = remainingPrincipal ?? product.Principal;
            MonthsRemaining = monthsRemaining ?? product.TermMonths;
            PaymentsMade = paymentsMade;
            MissedPaymentsCount = missedPaymentsCount;
            OverdueAmount = overdueAmount ?? Currency.Zero;
            LastPaidMonth = lastPaidMonth;
        }

        /// <summary>
        /// Computes the interest and principal components of the upcoming monthly installment.
        /// </summary>
        public (Currency InterestPortion, Currency PrincipalPortion) CalculateInstallmentSplit()
        {
            if (RemainingPrincipal.MinorUnits == 0)
            {
                return (Currency.Zero, Currency.Zero);
            }

            double monthlyRate = Product.AnnualInterestRate / 12.0;
            double interestDollars = RemainingPrincipal.ToDollars() * monthlyRate;
            Currency interest = Currency.FromDollars(interestDollars);

            // In final month or rounding, principal portion cannot exceed remaining principal
            Currency principalPortion = MonthlyInstallment > interest
                ? MonthlyInstallment - interest
                : Currency.Zero;

            if (principalPortion > RemainingPrincipal)
            {
                principalPortion = RemainingPrincipal;
            }

            return (interest, principalPortion);
        }

        public void ApplyScheduledPayment(Currency principalPaid, int monthPaid = 0)
        {
            RemainingPrincipal -= principalPaid;
            PaymentsMade++;
            MonthsRemaining = Math.Max(0, MonthsRemaining - 1);
            LastPaidMonth = monthPaid;

            if (RemainingPrincipal.MinorUnits == 0)
            {
                MonthsRemaining = 0;
            }
        }

        public void ApplyMissedPayment()
        {
            MissedPaymentsCount++;
            OverdueAmount += MonthlyInstallment + LateFeePenalty;
        }

        public void SettleOverdueDebt(Currency amountSettled)
        {
            if (amountSettled >= OverdueAmount)
            {
                OverdueAmount = Currency.Zero;
                MissedPaymentsCount = 0;
            }
            else
            {
                OverdueAmount -= amountSettled;
            }
        }

        public void RepayEarlyInFull()
        {
            RemainingPrincipal = Currency.Zero;
            MonthsRemaining = 0;
            OverdueAmount = Currency.Zero;
        }
    }
}

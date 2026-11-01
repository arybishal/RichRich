using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Service managing loan products, credit scoring, loan underwriting, amortization,
    /// scheduled monthly installment servicing, missed payment penalties, and early debt payoffs.
    /// </summary>
    public class LoanService
    {
        private readonly List<LoanProduct> _catalog = new List<LoanProduct>();
        private readonly List<ActiveLoan> _activeLoans = new List<ActiveLoan>();

        public IReadOnlyList<LoanProduct> Catalog => _catalog;
        public IReadOnlyList<ActiveLoan> ActiveLoans => _activeLoans;

        public FinancialService FinancialService { get; }
        public CareerService CareerService { get; }
        public SimulationClock Clock { get; }

        public int CreditScore { get; private set; } = 650; // Standard baseline credit score (300-850)

        public Currency TotalOutstandingPrincipal
        {
            get
            {
                long sum = _activeLoans.Sum(l => (long)l.RemainingPrincipal.MinorUnits);
                return Currency.FromCents(sum);
            }
        }

        public Currency TotalMonthlyInstallments
        {
            get
            {
                long sum = _activeLoans.Where(l => !l.IsFullyPaid).Sum(l => (long)l.MonthlyInstallment.MinorUnits);
                return Currency.FromCents(sum);
            }
        }

        public event Action<ActiveLoan> OnLoanDisbursed;
        public event Action<ActiveLoan, FinancialTransaction, FinancialTransaction> OnLoanInstallmentPaid;
        public event Action<ActiveLoan, string> OnLoanPaymentMissed;
        public event Action<ActiveLoan> OnLoanSettledInFull;

        public LoanService(
            FinancialService financialService,
            CareerService careerService,
            SimulationClock clock,
            IEnumerable<LoanProduct> initialCatalog = null)
        {
            FinancialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
            CareerService = careerService ?? throw new ArgumentNullException(nameof(careerService));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));

            if (initialCatalog != null)
            {
                _catalog.AddRange(initialCatalog);
            }
            else
            {
                _catalog.AddRange(GetDefaultCatalog());
            }

            Clock.OnDayPassed += CheckScheduledInstallments;
        }

        public static List<LoanProduct> GetDefaultCatalog()
        {
            return new List<LoanProduct>
            {
                new LoanProduct(
                    "loan_personal_5k",
                    "Starter Personal Loan ($5,000)",
                    Currency.FromDollars(5000.00),
                    annualInterestRate: 0.12,
                    termMonths: 12,
                    requiredMinimumIncome: Currency.FromDollars(1200.00)
                ),
                new LoanProduct(
                    "loan_growth_10k",
                    "Capital Growth Loan ($10,000)",
                    Currency.FromDollars(10000.00),
                    annualInterestRate: 0.12,
                    termMonths: 12,
                    requiredMinimumIncome: Currency.FromDollars(2200.00)
                )
            };
        }

        public void AddRestoredLoan(ActiveLoan loan)
        {
            if (loan == null) throw new ArgumentNullException(nameof(loan));
            _activeLoans.Add(loan);
            FinancialService.BalanceSheet.UpdateLoanLiabilities(FinancialService.BalanceSheet.LoanLiabilities + loan.RemainingPrincipal);
        }

        public bool CheckEligibility(string productId, out string rejectionReason)
        {
            var product = _catalog.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                rejectionReason = "Loan product not found in catalog.";
                return false;
            }

            // Limit active loans to prevent infinite credit exploitation
            if (_activeLoans.Count(l => !l.IsFullyPaid) >= 2)
            {
                rejectionReason = "Maximum limit of 2 concurrent active loans reached. Settle existing debt first.";
                return false;
            }

            // Credit score requirement
            if (CreditScore < 580)
            {
                rejectionReason = $"Credit score {CreditScore} is below minimum threshold (580) for loan approval.";
                return false;
            }

            // Income / Debt-to-income check
            Currency currentIncome = CareerService.CurrentMonthlySalary;
            if (currentIncome < product.RequiredMinimumMonthlyIncome)
            {
                rejectionReason = $"Monthly income of {currentIncome} does not meet required minimum of {product.RequiredMinimumMonthlyIncome}.";
                return false;
            }

            Currency projectedMonthlyDebt = TotalMonthlyInstallments + product.CalculateMonthlyInstallment();
            if (projectedMonthlyDebt.MinorUnits * 2 > currentIncome.MinorUnits) // Debt > 50% income
            {
                rejectionReason = "Debt-to-Income ratio exceeds safe borrowing limit (50%).";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        public bool TryTakeLoan(string productId, out ActiveLoan loan, out string error)
        {
            loan = null;
            if (!CheckEligibility(productId, out error))
            {
                return false;
            }

            var product = _catalog.First(p => p.Id == productId);
            loan = new ActiveLoan(
                Guid.NewGuid().ToString("N"),
                product,
                Clock.CurrentDate,
                dueDayOfMonth: Clock.CurrentDate.Day
            );

            _activeLoans.Add(loan);

            // Disburse funds via FinancialService (Cash increases, Liabilities increase, Net worth = 0 change)
            FinancialService.DisburseLoan(loan);

            OnLoanDisbursed?.Invoke(loan);
            error = null;
            return true;
        }

        public bool TryRepayLoanEarly(string loanId, out string error)
        {
            var loan = _activeLoans.FirstOrDefault(l => l.LoanId == loanId);
            if (loan == null)
            {
                error = "Loan not found.";
                return false;
            }

            if (loan.IsFullyPaid)
            {
                error = "Loan is already fully settled.";
                return false;
            }

            if (!FinancialService.TryRepayLoanInFull(loan, out _, out error))
            {
                return false;
            }

            CreditScore = Math.Min(850, CreditScore + 15);
            OnLoanSettledInFull?.Invoke(loan);
            return true;
        }

        private void CheckScheduledInstallments(GameDateTime currentDate)
        {
            foreach (var loan in _activeLoans.Where(l => !l.IsFullyPaid).ToList())
            {
                if (loan.DueDayOfMonth == currentDate.Day && loan.LastPaidMonth != currentDate.Month)
                {
                    bool success = FinancialService.TryProcessLoanInstallment(
                        loan,
                        out var principalTx,
                        out var interestTx,
                        out string error
                    );

                    if (success)
                    {
                        CreditScore = Math.Min(850, CreditScore + 3);
                        OnLoanInstallmentPaid?.Invoke(loan, principalTx, interestTx);

                        if (loan.IsFullyPaid)
                        {
                            CreditScore = Math.Min(850, CreditScore + 10);
                            OnLoanSettledInFull?.Invoke(loan);
                        }
                    }
                    else
                    {
                        loan.ApplyMissedPayment();
                        FinancialService.AddOverdueLiability(
                            loan.MonthlyInstallment + ActiveLoan.LateFeePenalty,
                            TransactionCategory.LatePenaltyFee,
                            $"Missed Loan Payment & Late Fee: {loan.Product.Name}"
                        );
                        CreditScore = Math.Max(300, CreditScore - 25);
                        OnLoanPaymentMissed?.Invoke(loan, error);
                    }
                }
            }
        }
    }
}

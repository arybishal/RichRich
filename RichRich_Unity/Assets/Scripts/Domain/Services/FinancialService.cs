using System;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Central financial authority executing all monetary operations.
    /// Strictly maintains double-entry discipline:
    /// - Disbursing a loan increases Cash and Liabilities equally (0 net-worth change).
    /// - Repaying principal decreases Cash and Liabilities equally.
    /// - Paying interest decreases Cash and records an expense.
    /// - Prevents direct tampering with player balances.
    /// </summary>
    public class FinancialService
    {
        public Wallet Wallet { get; }
        public FinancialLedger Ledger { get; }
        public BalanceSheet BalanceSheet { get; }
        public SimulationClock Clock { get; }

        public Currency CashBalance => Wallet.Balance;
        public long NetWorthCents => BalanceSheet.NetWorthCents;
        public string NetWorthFormatted => BalanceSheet.NetWorthFormatted;

        public event Action<FinancialTransaction> OnTransactionExecuted;
        public event Action OnBalanceSheetUpdated;

        public FinancialService(
            Wallet wallet,
            FinancialLedger ledger,
            SimulationClock clock,
            BalanceSheet balanceSheet = null)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            BalanceSheet = balanceSheet ?? new BalanceSheet();

            // Synchronize balance sheet with wallet
            BalanceSheet.UpdateCash(Wallet.Balance);
            Wallet.OnBalanceChanged += balance => BalanceSheet.UpdateCash(balance);
        }

        public FinancialTransaction RecordIncome(
            Currency amount,
            TransactionCategory category,
            string description,
            string relatedEntityId = null)
        {
            if (amount.MinorUnits == 0) return null;

            Wallet.Add(amount);
            BalanceSheet.UpdateCash(Wallet.Balance);

            var tx = new FinancialTransaction(
                Guid.NewGuid().ToString("N"),
                amount,
                TransactionType.Income,
                category,
                Clock.CurrentDate,
                description,
                relatedEntityId
            );

            Ledger.Record(tx);
            OnTransactionExecuted?.Invoke(tx);
            OnBalanceSheetUpdated?.Invoke();
            return tx;
        }

        public bool TryRecordExpense(
            Currency amount,
            TransactionCategory category,
            string description,
            out FinancialTransaction transaction,
            out string error,
            string relatedEntityId = null)
        {
            if (amount.MinorUnits == 0)
            {
                transaction = null;
                error = null;
                return true;
            }

            if (!Wallet.TrySpend(amount, out error))
            {
                transaction = null;
                return false;
            }

            BalanceSheet.UpdateCash(Wallet.Balance);

            transaction = new FinancialTransaction(
                Guid.NewGuid().ToString("N"),
                amount,
                TransactionType.Expense,
                category,
                Clock.CurrentDate,
                description,
                relatedEntityId
            );

            Ledger.Record(transaction);
            OnTransactionExecuted?.Invoke(transaction);
            OnBalanceSheetUpdated?.Invoke();
            error = null;
            return true;
        }

        /// <summary>
        /// Disburses a loan: Increases cash and loan liabilities by equal amounts.
        /// Net worth remains identical.
        /// </summary>
        public FinancialTransaction DisburseLoan(ActiveLoan loan, string description = "Loan Disbursement")
        {
            if (loan == null) throw new ArgumentNullException(nameof(loan));

            Wallet.Add(loan.InitialPrincipal);
            Currency updatedLiabilities = BalanceSheet.LoanLiabilities + loan.InitialPrincipal;
            BalanceSheet.UpdateCash(Wallet.Balance);
            BalanceSheet.UpdateLoanLiabilities(updatedLiabilities);

            var tx = new FinancialTransaction(
                Guid.NewGuid().ToString("N"),
                loan.InitialPrincipal,
                TransactionType.LiabilityDraw,
                TransactionCategory.LoanDisbursement,
                Clock.CurrentDate,
                $"{description}: {loan.Product.Name}",
                loan.LoanId
            );

            Ledger.Record(tx);
            OnTransactionExecuted?.Invoke(tx);
            OnBalanceSheetUpdated?.Invoke();
            return tx;
        }

        /// <summary>
        /// Processes a loan installment payment, splitting into principal reduction and interest expense.
        /// </summary>
        public bool TryProcessLoanInstallment(
            ActiveLoan loan,
            out FinancialTransaction principalTx,
            out FinancialTransaction interestTx,
            out string error)
        {
            principalTx = null;
            interestTx = null;

            if (loan == null) throw new ArgumentNullException(nameof(loan));

            var (interestPortion, principalPortion) = loan.CalculateInstallmentSplit();
            Currency totalPayment = interestPortion + principalPortion;

            if (!Wallet.CanAfford(totalPayment))
            {
                error = $"Insufficient funds for monthly loan installment of {totalPayment}. Available: {Wallet.Balance}.";
                return false;
            }

            // 1. Deduct interest (Expense)
            if (interestPortion.MinorUnits > 0)
            {
                Wallet.TrySpend(interestPortion, out _);
                interestTx = new FinancialTransaction(
                    Guid.NewGuid().ToString("N"),
                    interestPortion,
                    TransactionType.Expense,
                    TransactionCategory.LoanInstallmentInterest,
                    Clock.CurrentDate,
                    $"Monthly Interest on {loan.Product.Name}",
                    loan.LoanId
                );
                Ledger.Record(interestTx);
            }

            // 2. Deduct principal (Liability reduction)
            if (principalPortion.MinorUnits > 0)
            {
                Wallet.TrySpend(principalPortion, out _);
                loan.ApplyScheduledPayment(principalPortion, Clock.CurrentDate.Month);

                // Reduce balance sheet liability
                Currency newLiabilities = BalanceSheet.LoanLiabilities > principalPortion
                    ? BalanceSheet.LoanLiabilities - principalPortion
                    : Currency.Zero;
                BalanceSheet.UpdateLoanLiabilities(newLiabilities);

                principalTx = new FinancialTransaction(
                    Guid.NewGuid().ToString("N"),
                    principalPortion,
                    TransactionType.LiabilityRepay,
                    TransactionCategory.LoanInstallmentPrincipal,
                    Clock.CurrentDate,
                    $"Monthly Principal on {loan.Product.Name}",
                    loan.LoanId
                );
                Ledger.Record(principalTx);
            }

            BalanceSheet.UpdateCash(Wallet.Balance);
            OnTransactionExecuted?.Invoke(principalTx ?? interestTx);
            OnBalanceSheetUpdated?.Invoke();
            error = null;
            return true;
        }

        /// <summary>
        /// Repays an entire remaining loan principal early.
        /// </summary>
        public bool TryRepayLoanInFull(ActiveLoan loan, out FinancialTransaction tx, out string error)
        {
            tx = null;
            if (loan == null) throw new ArgumentNullException(nameof(loan));

            Currency principal = loan.RemainingPrincipal;
            if (principal.MinorUnits == 0)
            {
                error = "Loan principal is already zero.";
                return false;
            }

            if (!Wallet.TrySpend(principal, out error))
            {
                return false;
            }

            loan.RepayEarlyInFull();
            Currency newLiabilities = BalanceSheet.LoanLiabilities > principal
                ? BalanceSheet.LoanLiabilities - principal
                : Currency.Zero;
            BalanceSheet.UpdateLoanLiabilities(newLiabilities);
            BalanceSheet.UpdateCash(Wallet.Balance);

            tx = new FinancialTransaction(
                Guid.NewGuid().ToString("N"),
                principal,
                TransactionType.LiabilityRepay,
                TransactionCategory.LoanInstallmentPrincipal,
                Clock.CurrentDate,
                $"Early Full Repayment: {loan.Product.Name}",
                loan.LoanId
            );

            Ledger.Record(tx);
            OnTransactionExecuted?.Invoke(tx);
            OnBalanceSheetUpdated?.Invoke();
            return true;
        }

        public void AddOverdueLiability(Currency amount, TransactionCategory category, string description)
        {
            BalanceSheet.UpdateOverdueLiabilities(BalanceSheet.OverdueLiabilities + amount);
            OnBalanceSheetUpdated?.Invoke();
        }

        public bool TrySettleOverdueDebt(Currency amount, out string error)
        {
            if (!Wallet.TrySpend(amount, out error))
            {
                return false;
            }

            Currency remaining = BalanceSheet.OverdueLiabilities > amount
                ? BalanceSheet.OverdueLiabilities - amount
                : Currency.Zero;
            BalanceSheet.UpdateOverdueLiabilities(remaining);
            BalanceSheet.UpdateCash(Wallet.Balance);

            var tx = new FinancialTransaction(
                Guid.NewGuid().ToString("N"),
                amount,
                TransactionType.Expense,
                TransactionCategory.LatePenaltyFee,
                Clock.CurrentDate,
                "Settled Overdue Debt"
            );
            Ledger.Record(tx);
            OnTransactionExecuted?.Invoke(tx);
            OnBalanceSheetUpdated?.Invoke();
            return true;
        }

        public void UpdateNonCashAssets(Currency totalAssets)
        {
            BalanceSheet.UpdateNonCashAssets(totalAssets);
            OnBalanceSheetUpdated?.Invoke();
        }
    }
}

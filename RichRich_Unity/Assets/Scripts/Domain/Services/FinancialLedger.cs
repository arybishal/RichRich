using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Audit ledger storing the complete history of financial transactions.
    /// Provides analytics, categorization, and monthly cashflow aggregations.
    /// </summary>
    public class FinancialLedger
    {
        private readonly List<FinancialTransaction> _transactions = new List<FinancialTransaction>();

        public IReadOnlyList<FinancialTransaction> Transactions => _transactions;

        public event Action<FinancialTransaction> OnTransactionRecorded;

        public FinancialLedger(IEnumerable<FinancialTransaction> initialHistory = null)
        {
            if (initialHistory != null)
            {
                _transactions.AddRange(initialHistory);
            }
        }

        public void Record(FinancialTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            _transactions.Add(transaction);
            OnTransactionRecorded?.Invoke(transaction);
        }

        public Currency GetTotalIncome(GameDateTime startDate, GameDateTime endDate)
        {
            long cents = _transactions
                .Where(t => t.Date >= startDate && t.Date <= endDate && t.Type == TransactionType.Income)
                .Sum(t => (long)t.Amount.MinorUnits);

            return Currency.FromCents(cents);
        }

        public Currency GetTotalExpenses(GameDateTime startDate, GameDateTime endDate)
        {
            long cents = _transactions
                .Where(t => t.Date >= startDate && t.Date <= endDate &&
                           (t.Type == TransactionType.Expense || t.Category == TransactionCategory.LoanInstallmentInterest))
                .Sum(t => (long)t.Amount.MinorUnits);

            return Currency.FromCents(cents);
        }

        public long GetNetCashFlowCents(GameDateTime startDate, GameDateTime endDate)
        {
            Currency income = GetTotalIncome(startDate, endDate);
            Currency expenses = GetTotalExpenses(startDate, endDate);
            return (long)income.MinorUnits - (long)expenses.MinorUnits;
        }

        public (Currency Income, Currency Expenses, long NetCents) GetMonthlySummary(int year, int month)
        {
            var start = new GameDateTime(year, month, 1, 0, 0);
            var end = new GameDateTime(year, month, GameDateTime.DaysPerMonth, 23, 59);

            Currency inc = GetTotalIncome(start, end);
            Currency exp = GetTotalExpenses(start, end);
            long net = (long)inc.MinorUnits - (long)exp.MinorUnits;

            return (inc, exp, net);
        }

        public IReadOnlyDictionary<TransactionCategory, Currency> GetExpenseBreakdown(GameDateTime startDate, GameDateTime endDate)
        {
            var breakdown = new Dictionary<TransactionCategory, long>();

            foreach (var tx in _transactions.Where(t => t.Date >= startDate && t.Date <= endDate &&
                                                      (t.Type == TransactionType.Expense || t.Category == TransactionCategory.LoanInstallmentInterest)))
            {
                if (!breakdown.ContainsKey(tx.Category))
                {
                    breakdown[tx.Category] = 0;
                }
                breakdown[tx.Category] += tx.Amount.MinorUnits;
            }

            return breakdown.ToDictionary(k => k.Key, v => Currency.FromCents(v.Value));
        }
    }
}

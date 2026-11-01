using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Service managing player recurring living expenses:
    /// Accommodation, Food, Transportation, Utilities, Internet, and Personal needs.
    /// Deducts payments on scheduled due dates, detects shortfalls, and flags overdue liabilities.
    /// </summary>
    public class LivingExpenseService
    {
        private readonly List<RecurringExpense> _expenses = new List<RecurringExpense>();

        public IReadOnlyList<RecurringExpense> Expenses => _expenses;
        public FinancialService FinancialService { get; }
        public SimulationClock Clock { get; }

        public Currency TotalMonthlyBudget
        {
            get
            {
                long sum = _expenses.Sum(e => (long)e.MonthlyAmount.MinorUnits);
                return Currency.FromCents(sum);
            }
        }

        public event Action<RecurringExpense, FinancialTransaction> OnExpensePaid;
        public event Action<RecurringExpense, string> OnExpenseFailed;

        public LivingExpenseService(
            FinancialService financialService,
            SimulationClock clock,
            IEnumerable<RecurringExpense> customExpenses = null)
        {
            FinancialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));

            if (customExpenses != null)
            {
                _expenses.AddRange(customExpenses);
            }
            else
            {
                _expenses.AddRange(GetDefaultExpenses());
            }

            Clock.OnDayPassed += CheckScheduledExpenses;
        }

        public static List<RecurringExpense> GetDefaultExpenses()
        {
            return new List<RecurringExpense>
            {
                new RecurringExpense("exp_housing", "Apartment Rent", TransactionCategory.LivingAccommodation, Currency.FromDollars(600.00), dueDayOfMonth: 1),
                new RecurringExpense("exp_food", "Groceries & Food", TransactionCategory.LivingFood, Currency.FromDollars(300.00), dueDayOfMonth: 1),
                new RecurringExpense("exp_transit", "Public Transit Pass", TransactionCategory.LivingTransport, Currency.FromDollars(100.00), dueDayOfMonth: 1),
                new RecurringExpense("exp_utilities", "Utilities (Power & Water)", TransactionCategory.LivingUtilities, Currency.FromDollars(120.00), dueDayOfMonth: 1),
                new RecurringExpense("exp_internet", "Mobile & Internet Plan", TransactionCategory.LivingInternet, Currency.FromDollars(50.00), dueDayOfMonth: 1),
                new RecurringExpense("exp_personal", "Personal & Household", TransactionCategory.LivingPersonal, Currency.FromDollars(150.00), dueDayOfMonth: 1)
            };
        }

        public void CheckScheduledExpenses(GameDateTime currentDate)
        {
            int currentDay = currentDate.Day;
            int currentMonth = currentDate.Month;
            int currentYear = currentDate.Year;

            foreach (var expense in _expenses)
            {
                if (expense.DueDayOfMonth == currentDay)
                {
                    // Check if already paid this calendar month
                    if (expense.LastPaidDate.HasValue &&
                        expense.LastPaidDate.Value.Month == currentMonth &&
                        expense.LastPaidDate.Value.Year == currentYear)
                    {
                        continue;
                    }

                    // Attempt deduction
                    bool success = FinancialService.TryRecordExpense(
                        expense.MonthlyAmount,
                        expense.Category,
                        $"Monthly {expense.Name}",
                        out var tx,
                        out string error,
                        expense.Id
                    );

                    if (success)
                    {
                        expense.MarkPaid(currentDate);
                        OnExpensePaid?.Invoke(expense, tx);
                    }
                    else
                    {
                        expense.MarkOverdue();
                        FinancialService.AddOverdueLiability(expense.MonthlyAmount, expense.Category, $"Overdue {expense.Name}");
                        OnExpenseFailed?.Invoke(expense, error);
                    }
                }
            }
        }

        public void AddExpense(RecurringExpense expense)
        {
            if (expense == null) throw new ArgumentNullException(nameof(expense));
            _expenses.Add(expense);
        }

        public bool RemoveExpense(string expenseId)
        {
            int idx = _expenses.FindIndex(e => e.Id == expenseId);
            if (idx >= 0)
            {
                _expenses.RemoveAt(idx);
                return true;
            }
            return false;
        }
    }
}

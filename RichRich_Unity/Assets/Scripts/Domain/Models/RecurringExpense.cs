using System;

namespace RichRich.Domain.Models
{
    /// <summary>
    /// Configuration and state for a recurring living expense.
    /// </summary>
    public class RecurringExpense
    {
        public string Id { get; }
        public string Name { get; }
        public TransactionCategory Category { get; }
        public Currency MonthlyAmount { get; private set; }
        public int DueDayOfMonth { get; }
        public GameDateTime? LastPaidDate { get; private set; }
        public bool IsOverdue { get; private set; }

        public RecurringExpense(
            string id,
            string name,
            TransactionCategory category,
            Currency monthlyAmount,
            int dueDayOfMonth = 1,
            GameDateTime? lastPaidDate = null,
            bool isOverdue = false)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Category = category;
            MonthlyAmount = monthlyAmount;
            DueDayOfMonth = Math.Clamp(dueDayOfMonth, 1, 30);
            LastPaidDate = lastPaidDate;
            IsOverdue = isOverdue;
        }

        public void MarkPaid(GameDateTime date)
        {
            LastPaidDate = date;
            IsOverdue = false;
        }

        public void MarkOverdue()
        {
            IsOverdue = true;
        }

        public void SetMonthlyAmount(Currency newAmount)
        {
            MonthlyAmount = newAmount;
        }
    }
}

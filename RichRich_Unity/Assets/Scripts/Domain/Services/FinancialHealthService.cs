using System;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Service continuously evaluating the player's financial condition, cash flow solvency,
    /// debt burden, and default status across 6 discrete states.
    /// </summary>
    public class FinancialHealthService
    {
        public FinancialService FinancialService { get; }
        public LivingExpenseService ExpenseService { get; }
        public CareerService CareerService { get; }
        public LoanService LoanService { get; }

        public FinancialHealthService(
            FinancialService financialService,
            LivingExpenseService expenseService,
            CareerService careerService,
            LoanService loanService)
        {
            FinancialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
            ExpenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
            CareerService = careerService ?? throw new ArgumentNullException(nameof(careerService));
            LoanService = loanService ?? throw new ArgumentNullException(nameof(loanService));
        }

        public Currency ProjectedMonthlyIncome => CareerService.CurrentMonthlySalary;
        public Currency ProjectedMonthlyExpenses => ExpenseService.TotalMonthlyBudget + LoanService.TotalMonthlyInstallments;
        public long ProjectedNetMonthlyCashFlowCents => (long)ProjectedMonthlyIncome.MinorUnits - (long)ProjectedMonthlyExpenses.MinorUnits;
        public bool HasNegativeCashFlow => ProjectedNetMonthlyCashFlowCents < 0;

        public FinancialHealthReport EvaluateStatus()
        {
            var sheet = FinancialService.BalanceSheet;
            Currency cash = sheet.Cash;
            Currency monthlyExpenses = ProjectedMonthlyExpenses;
            Currency overdue = sheet.OverdueLiabilities;
            int totalMissedPayments = LoanService.ActiveLoans.Sum(l => l.MissedPaymentsCount);

            // 1. Bankrupt condition: Delinquent default (2+ missed payments or severe overdue debt) with negative net worth and no cash
            if (totalMissedPayments >= 2 && sheet.IsNegativeNetWorth && cash < overdue)
            {
                return new FinancialHealthReport(
                    FinancialHealthState.Bankrupt,
                    $"Critical Default: {totalMissedPayments} missed payments with negative net worth ({sheet.NetWorthFormatted}) and insufficient cash to cure delinquency.",
                    "File for restructuring, liquidate non-essential assets, or seek emergency community relief."
                );
            }

            // 2. Insolvent condition: Liabilities exceed assets with persistent negative cash flow
            if (sheet.IsNegativeNetWorth && HasNegativeCashFlow)
            {
                return new FinancialHealthReport(
                    FinancialHealthState.Insolvent,
                    $"Insolvent: Total debt ({sheet.TotalLiabilities}) exceeds total assets ({sheet.TotalAssets}) with monthly deficit.",
                    "Prioritize aggressive debt paydown, seek higher-paying employment, and cut living expenses."
                );
            }

            // 3. Cash Crisis: Active overdue liabilities or cash is less than upcoming monthly expenses
            if (overdue.MinorUnits > 0 || (monthlyExpenses.MinorUnits > 0 && cash.MinorUnits < (monthlyExpenses.MinorUnits / 4))) // Less than 1 week cash
            {
                return new FinancialHealthReport(
                    FinancialHealthState.CashCrisis,
                    overdue.MinorUnits > 0
                        ? $"Cash Crisis: Overdue obligations of {overdue} require immediate settlement."
                        : $"Cash Crisis: Available cash ({cash}) is critically low compared to upcoming obligations ({monthlyExpenses}).",
                    "Take extra courier or retail shifts immediately to prevent default penalties."
                );
            }

            // 4. Financially Stressed: Negative cash flow or cash buffer is less than 1 full month of expenses
            if (HasNegativeCashFlow || cash < monthlyExpenses)
            {
                return new FinancialHealthReport(
                    FinancialHealthState.FinanciallyStressed,
                    HasNegativeCashFlow
                        ? $"Financially Stressed: Monthly spending ({monthlyExpenses}) exceeds monthly income ({ProjectedMonthlyIncome})."
                        : $"Financially Stressed: Emergency cash cushion ({cash}) is less than one month's expenses ({monthlyExpenses}).",
                    "Improve income through skills certification or downsize accommodation."
                );
            }

            // 5. Healthy: Positive cash flow, >= 1 month emergency buffer, zero overdue liabilities
            return new FinancialHealthReport(
                FinancialHealthState.Healthy,
                $"Financially Healthy: Positive net cash flow (+{Currency.FromCents(ProjectedNetMonthlyCashFlowCents)}/mo) with {cash} liquid reserves.",
                "Maintain steady savings, invest in professional career certificates, and build wealth."
            );
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for Business Finance, Accounting & Commercial Credit.
    /// Manages enterprise P&L review, commercial equipment and expansion loans, early debt settlements,
    /// and business balance sheet tracking.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class BusinessFinanceController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public BusinessService BusinessService => Coordinator.BusinessService;
        public LoanService LoanService => Coordinator.LoanService;
        public FinancialService FinancialService => Coordinator.FinancialService;

        public IReadOnlyList<LoanProduct> CommercialLoanProducts => LoanService.Catalog.Where(p => p.IsBusinessLoan).ToList();
        public IReadOnlyList<ActiveLoan> ActiveCommercialLoans => LoanService.ActiveLoans.Where(l => l.Product.IsBusinessLoan && !l.IsFullyPaid).ToList();

        public Currency TotalBusinessValuation => BusinessService.TotalBusinessValuation;
        public Currency TotalBusinessRevenue => BusinessService.OwnedBusinesses.Aggregate(Currency.Zero, (acc, b) => acc + b.PAndL.TotalRevenue);
        public Currency TotalOperatingExpenses => BusinessService.OwnedBusinesses.Aggregate(Currency.Zero, (acc, b) => acc + b.PAndL.OperatingExpenses);
        public Currency TotalCOGS => BusinessService.OwnedBusinesses.Aggregate(Currency.Zero, (acc, b) => acc + b.PAndL.CostOfGoodsSold);
        public Currency TotalPayroll => BusinessService.OwnedBusinesses.Aggregate(Currency.Zero, (acc, b) => acc + b.PAndL.EmployeePayroll);
        public Currency TotalNetProfit => BusinessService.ProjectedMonthlyNetProfit;

        public string LastErrorMessage { get; private set; }

        public event Action OnFinanceUpdated;
        public event Action<ActiveLoan> OnLoanDisbursed;
        public event Action<ActiveLoan> OnLoanSettled;
        public event Action<string> OnActionFailed;

        public BusinessFinanceController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.LoanService.OnLoanDisbursed += loan =>
            {
                OnLoanDisbursed?.Invoke(loan);
                OnFinanceUpdated?.Invoke();
            };

            Coordinator.LoanService.OnLoanSettledInFull += loan =>
            {
                OnLoanSettled?.Invoke(loan);
                OnFinanceUpdated?.Invoke();
            };
        }

        public bool CheckLoanEligibility(string productId, out string rejectionReason)
        {
            return LoanService.CheckEligibility(productId, out rejectionReason);
        }

        public bool TryApplyForCommercialLoan(string productId, out ActiveLoan loan, out string error)
        {
            bool success = LoanService.TryTakeLoan(productId, out loan, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            OnFinanceUpdated?.Invoke();
            return true;
        }

        public bool TryRepayLoanEarly(string loanId, out string error)
        {
            bool success = LoanService.TryRepayLoanEarly(loanId, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            OnFinanceUpdated?.Invoke();
            return true;
        }

        public void Refresh()
        {
            OnFinanceUpdated?.Invoke();
        }
    }
}

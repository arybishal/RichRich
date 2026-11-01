using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for Business Finance, Accounting & Commercial Credit.
    /// Manages enterprise P&L review, commercial equipment and expansion loans, early debt settlements,
    /// and business balance sheet tracking.
    /// Follows premium dark FinTech design direction.
    /// </summary>
    public class BusinessFinanceView : MonoBehaviour
    {
        [Header("Consolidated Commercial P&L")]
        [SerializeField] private Text _totalRevenueText;
        [SerializeField] private Text _totalOperatingExpensesText;
        [SerializeField] private Text _totalCOGSText;
        [SerializeField] private Text _totalPayrollText;
        [SerializeField] private Text _netOperatingProfitText;
        [SerializeField] private Text _balanceSheetValuationText;

        [Header("Commercial Financing & Credit")]
        [SerializeField] private Text _loan1TitleText;
        [SerializeField] private Text _loan1TermsText;
        [SerializeField] private Button _loan1ApplyButton;

        [Header("Active Commercial Debt")]
        [SerializeField] private Text _activeDebtSummaryText;
        [SerializeField] private Button _repayEarlyButton;

        [Header("Feedback")]
        [SerializeField] private Text _feedbackText;

        private BusinessFinanceController _controller;

        public void Bind(BusinessFinanceController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnFinanceUpdated += RefreshUI;
            _controller.OnLoanDisbursed += OnDisbursed;
            _controller.OnLoanSettled += OnSettled;
            _controller.OnActionFailed += OnFailed;

            if (_loan1ApplyButton != null)
            {
                _loan1ApplyButton.onClick.AddListener(() =>
                {
                    var loans = _controller.CommercialLoanProducts;
                    if (loans.Count > 0)
                    {
                        _controller.TryApplyForCommercialLoan(loans[0].Id, out _, out _);
                    }
                });
            }

            if (_repayEarlyButton != null)
            {
                _repayEarlyButton.onClick.AddListener(() =>
                {
                    var active = _controller.ActiveCommercialLoans;
                    if (active.Count > 0)
                    {
                        _controller.TryRepayLoanEarly(active[0].LoanId, out _);
                    }
                });
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_controller == null) return;

            if (_totalRevenueText != null)
                _totalRevenueText.text = $"Total Revenue: {_controller.TotalBusinessRevenue}";
            if (_totalOperatingExpensesText != null)
                _totalOperatingExpensesText.text = $"Operating Overhead: {_controller.TotalOperatingExpenses}";
            if (_totalCOGSText != null)
                _totalCOGSText.text = $"Cost of Goods Sold (COGS): {_controller.TotalCOGS}";
            if (_totalPayrollText != null)
                _totalPayrollText.text = $"Staff Payroll: {_controller.TotalPayroll}";
            if (_netOperatingProfitText != null)
                _netOperatingProfitText.text = $"Net Commercial Profit: {_controller.TotalNetProfit}";
            if (_balanceSheetValuationText != null)
                _balanceSheetValuationText.text = $"Total Enterprise Assets: {_controller.TotalBusinessValuation}";

            var products = _controller.CommercialLoanProducts;
            if (products.Count > 0)
            {
                var l1 = products[0];
                if (_loan1TitleText != null) _loan1TitleText.text = l1.Name;
                if (_loan1TermsText != null)
                    _loan1TermsText.text = $"Principal: {l1.Principal} | Rate: {l1.AnnualInterestRate:P0} | Term: {l1.TermMonths} mo | Installment: {l1.CalculateMonthlyInstallment()}/mo";
                if (_loan1ApplyButton != null)
                    _loan1ApplyButton.interactable = _controller.CheckLoanEligibility(l1.Id, out _);
            }
            else
            {
                if (_loan1TitleText != null) _loan1TitleText.text = "No commercial credit products available.";
                if (_loan1TermsText != null) _loan1TermsText.text = string.Empty;
                if (_loan1ApplyButton != null) _loan1ApplyButton.interactable = false;
            }

            var activeDebt = _controller.ActiveCommercialLoans;
            if (activeDebt.Count > 0)
            {
                var ad = activeDebt[0];
                if (_activeDebtSummaryText != null)
                    _activeDebtSummaryText.text = $"Active Loan: {ad.Product.Name} | Remaining: {ad.RemainingPrincipal} | Due: {ad.MonthlyInstallment}/mo";
                if (_repayEarlyButton != null) _repayEarlyButton.interactable = true;
            }
            else
            {
                if (_activeDebtSummaryText != null) _activeDebtSummaryText.text = "Zero outstanding commercial liabilities.";
                if (_repayEarlyButton != null) _repayEarlyButton.interactable = false;
            }
        }

        private void OnDisbursed(ActiveLoan loan)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Disbursed {loan.Product.Name}. Funds credited to business capital.";
        }

        private void OnSettled(ActiveLoan loan)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Commercial loan {loan.Product.Name} settled in full!";
        }

        private void OnFailed(string error)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Credit action failed: {error}";
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnFinanceUpdated -= RefreshUI;
                _controller.OnLoanDisbursed -= OnDisbursed;
                _controller.OnLoanSettled -= OnSettled;
                _controller.OnActionFailed -= OnFailed;
            }
        }
    }
}

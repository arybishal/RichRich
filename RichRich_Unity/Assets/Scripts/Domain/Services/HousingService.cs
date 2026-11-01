using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Service managing player housing, accommodation upgrades, lease verification,
    /// move-in deposits, and safe updates to recurring rent obligations.
    /// Pure C# domain service with zero MonoBehaviour dependencies.
    /// </summary>
    public class HousingService
    {
        private readonly List<HousingTier> _catalog = new List<HousingTier>();

        public IReadOnlyList<HousingTier> Catalog => _catalog;
        public HousingTier CurrentHousing { get; private set; }
        public Currency HousingDepositPaid { get; private set; }

        public FinancialService FinancialService { get; }
        public LivingExpenseService ExpenseService { get; }
        public CareerService CareerService { get; }
        public SimulationClock Clock { get; }

        public event Action<HousingTier> OnHousingChanged;

        public HousingService(
            FinancialService financialService,
            LivingExpenseService expenseService,
            CareerService careerService,
            SimulationClock clock,
            IEnumerable<HousingTier> customTiers = null)
        {
            FinancialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
            ExpenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
            CareerService = careerService ?? throw new ArgumentNullException(nameof(careerService));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));

            if (customTiers != null)
            {
                _catalog.AddRange(customTiers);
            }
            else
            {
                _catalog.AddRange(HousingTier.GetDefaultTiers());
            }

            // Default initial housing is Studio Apartment ($600/mo)
            CurrentHousing = _catalog.FirstOrDefault(h => h.Id == "housing_studio") ?? _catalog.First();
            HousingDepositPaid = CurrentHousing.DepositCost;
        }

        public bool CanMoveTo(string tierId, out string reason)
        {
            var target = _catalog.FirstOrDefault(h => h.Id == tierId);
            if (target == null)
            {
                reason = $"Housing tier with ID '{tierId}' was not found.";
                return false;
            }

            if (target.Id == CurrentHousing.Id)
            {
                reason = $"You already reside in {target.Name}.";
                return false;
            }

            // Check cash for deposit
            if (target.DepositCost.MinorUnits > FinancialService.Wallet.Balance.MinorUnits)
            {
                reason = $"Insufficient cash for move-in deposit: Requires {target.DepositCost}, current cash is {FinancialService.Wallet.Balance}.";
                return false;
            }

            // Landlord income / savings check
            if (target.MinIncomeRequirement.MinorUnits > 0)
            {
                Currency salary = CareerService.CurrentMonthlySalary;
                bool meetsIncome = salary.MinorUnits >= target.MinIncomeRequirement.MinorUnits;
                bool meetsSavings = target.MinSavingsRequirement.MinorUnits > 0 &&
                                    FinancialService.Wallet.Balance.MinorUnits >= target.MinSavingsRequirement.MinorUnits;

                if (!meetsIncome && !meetsSavings)
                {
                    string reqText = target.MinSavingsRequirement.MinorUnits > 0
                        ? $"monthly salary of at least {target.MinIncomeRequirement} OR savings of {target.MinSavingsRequirement}"
                        : $"monthly salary of at least {target.MinIncomeRequirement}";

                    reason = $"Landlord qualification requirement not met: Requires {reqText} (current salary: {salary}, savings: {FinancialService.Wallet.Balance}).";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        public bool TryMoveTo(string tierId, out string error)
        {
            if (!CanMoveTo(tierId, out error))
            {
                return false;
            }

            var target = _catalog.First(h => h.Id == tierId);

            // Deduct move-in deposit / moving fee
            if (target.DepositCost.MinorUnits > 0)
            {
                bool paid = FinancialService.TryRecordExpense(
                    target.DepositCost,
                    TransactionCategory.LivingAccommodation,
                    $"Move-in Deposit: {target.Name}",
                    out _,
                    out error,
                    target.Id
                );

                if (!paid)
                {
                    return false;
                }
            }

            // Update recurring rent in LivingExpenseService
            // Anti-duplication guarantee: Updating the monthly amount does not clear LastPaidDate,
            // so moving mid-month does not cause a second rent charge for the current month.
            var rentExpense = ExpenseService.Expenses.FirstOrDefault(e => e.Category == TransactionCategory.LivingAccommodation);
            if (rentExpense != null)
            {
                rentExpense.SetMonthlyAmount(target.MonthlyRent);
            }

            CurrentHousing = target;
            HousingDepositPaid = target.DepositCost;
            error = null;
            OnHousingChanged?.Invoke(CurrentHousing);
            return true;
        }

        public void RestoreSavedHousing(string tierId, Currency depositPaid)
        {
            var target = _catalog.FirstOrDefault(h => h.Id == tierId);
            if (target != null)
            {
                CurrentHousing = target;
                HousingDepositPaid = depositPaid;

                var rentExpense = ExpenseService.Expenses.FirstOrDefault(e => e.Category == TransactionCategory.LivingAccommodation);
                if (rentExpense != null)
                {
                    rentExpense.SetMonthlyAmount(target.MonthlyRent);
                }
            }
        }
    }
}

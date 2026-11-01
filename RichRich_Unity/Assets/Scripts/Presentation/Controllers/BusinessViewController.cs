using System;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation controller for the Business view / card:
    /// Displays ownership state, level, income yield, purchase/upgrade costs, and affordability validation.
    /// Pure C# view-model controller with zero MonoBehaviour coupling.
    /// </summary>
    public class BusinessViewController
    {
        public Business Business { get; }
        public Wallet Wallet { get; }
        public EconomyService Economy { get; }

        public string BusinessId => Business.Id;
        public string BusinessName => Business.Name;
        public bool IsOwned => Business.IsOwned;
        public int Level => Business.Level;

        public string LevelBadgeText => IsOwned ? $"Level {Level}" : "Available to Acquire";
        public string HourlyIncomeText => $"+{Business.GetHourlyIncome()}/hr";

        public Currency NextActionCost => IsOwned ? Business.GetUpgradeCost() : Business.GetPurchaseCost();
        public string NextActionCostText => NextActionCost.ToString();
        public bool CanAffordAction => Wallet.CanAfford(NextActionCost);

        public string ActionButtonText
        {
            get
            {
                if (!IsOwned)
                {
                    return $"Acquire ({NextActionCostText})";
                }
                return $"Upgrade to Lv. {Level + 1} ({NextActionCostText})";
            }
        }

        public string UpgradeBenefitDescription
        {
            get
            {
                if (!IsOwned)
                {
                    return $"Base Yield: +{Business.BaseHourlyYield}/hr";
                }
                // Each upgrade adds 25% of base yield
                Currency nextHourly = Business.BaseHourlyYield * (1.0 + (Level * EconomyConfig.UpgradeYieldBonusPercent));
                Currency diff = nextHourly - Business.GetHourlyIncome();
                return $"+25% Income (+{diff}/hr)";
            }
        }

        public event Action OnBusinessViewUpdated;
        public event Action<Business> OnBusinessUpgraded;
        public event Action<Business> OnBusinessAcquired;
        public event Action<string> OnActionFailed;

        public BusinessViewController(Business business, Wallet wallet, EconomyService economy)
        {
            Business = business ?? throw new ArgumentNullException(nameof(business));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));

            Business.OnBusinessUpdated += _ => OnBusinessViewUpdated?.Invoke();
            Wallet.OnBalanceChanged += _ => OnBusinessViewUpdated?.Invoke();
        }

        public bool TryExecuteAction(out string error)
        {
            if (!IsOwned)
            {
                if (!Economy.TryBuyStarterBusiness(out error))
                {
                    OnActionFailed?.Invoke(error);
                    return false;
                }

                error = null;
                OnBusinessAcquired?.Invoke(Business);
                OnBusinessViewUpdated?.Invoke();
                return true;
            }
            else
            {
                if (!Economy.TryUpgradeBusiness(out error))
                {
                    OnActionFailed?.Invoke(error);
                    return false;
                }

                error = null;
                OnBusinessUpgraded?.Invoke(Business);
                OnBusinessViewUpdated?.Invoke();
                return true;
            }
        }
    }
}

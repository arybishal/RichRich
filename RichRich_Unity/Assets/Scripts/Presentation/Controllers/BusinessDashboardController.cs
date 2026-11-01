using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for the Commercial Business Dashboard & Portfolio Management.
    /// Manages enterprise operations, facility upgrades, liquidation/sales, and performance tracking.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class BusinessDashboardController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public BusinessService BusinessService => Coordinator.BusinessService;

        public Business SelectedBusiness { get; private set; }
        public string LastErrorMessage { get; private set; }

        public IReadOnlyList<Business> OwnedBusinesses => BusinessService.OwnedBusinesses;
        public bool HasOwnedBusinesses => OwnedBusinesses.Count > 0;

        public Currency TotalPortfolioValuation => BusinessService.TotalBusinessValuation;
        public Currency TotalMonthlyNetProfit => BusinessService.ProjectedMonthlyNetProfit;

        public event Action OnDashboardUpdated;
        public event Action<Business> OnBusinessUpgraded;
        public event Action<Business, Currency> OnBusinessSold;
        public event Action<string> OnActionFailed;

        public BusinessDashboardController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.BusinessService.OnBusinessPurchased += biz =>
            {
                if (SelectedBusiness == null) SelectedBusiness = biz;
                OnDashboardUpdated?.Invoke();
            };

            Coordinator.BusinessService.OnBusinessSold += (biz, proceeds) =>
            {
                if (SelectedBusiness?.Id == biz.Id)
                {
                    SelectedBusiness = OwnedBusinesses.FirstOrDefault();
                }
                OnBusinessSold?.Invoke(biz, proceeds);
                OnDashboardUpdated?.Invoke();
            };

            Coordinator.BusinessService.OnBusinessUpgraded += biz =>
            {
                OnBusinessUpgraded?.Invoke(biz);
                OnDashboardUpdated?.Invoke();
            };

            SelectedBusiness = OwnedBusinesses.FirstOrDefault();
        }

        public void SelectBusiness(string businessId)
        {
            var biz = OwnedBusinesses.FirstOrDefault(b => b.Id == businessId);
            if (biz != null)
            {
                SelectedBusiness = biz;
                LastErrorMessage = null;
                OnDashboardUpdated?.Invoke();
            }
        }

        public void ToggleOpen()
        {
            if (SelectedBusiness != null)
            {
                BusinessService.ToggleOpen(SelectedBusiness.Id, !SelectedBusiness.IsOpen);
                OnDashboardUpdated?.Invoke();
            }
        }

        public bool TryUpgradeSelected(out string error)
        {
            if (SelectedBusiness == null)
            {
                error = "No business selected.";
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            bool success = BusinessService.TryUpgrade(SelectedBusiness.Id, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            OnDashboardUpdated?.Invoke();
            return true;
        }

        public bool TrySellSelected(out Currency proceeds, out string error)
        {
            proceeds = Currency.Zero;
            if (SelectedBusiness == null)
            {
                error = "No business selected.";
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            bool success = BusinessService.TrySell(SelectedBusiness.Id, out proceeds, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            SelectedBusiness = OwnedBusinesses.FirstOrDefault();
            OnDashboardUpdated?.Invoke();
            return true;
        }
    }
}

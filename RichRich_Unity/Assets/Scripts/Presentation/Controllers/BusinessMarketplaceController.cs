using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for the Commercial Business Marketplace.
    /// Manages browsing enterprise opportunities, inspecting startup and registration costs,
    /// and executing business acquisitions.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class BusinessMarketplaceController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public BusinessService BusinessService => Coordinator.BusinessService;

        public Business SelectedBusiness { get; private set; }
        public string LastErrorMessage { get; private set; }

        public IReadOnlyList<Business> AvailableBusinesses => BusinessService.Catalog;
        public IReadOnlyList<Business> UnownedBusinesses => BusinessService.Catalog.Where(b => !b.IsOwned).ToList();

        public Currency PlayerCash => Coordinator.Wallet.Balance;

        public event Action OnMarketplaceUpdated;
        public event Action<Business> OnBusinessPurchased;
        public event Action<string> OnPurchaseFailed;

        public BusinessMarketplaceController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.BusinessService.OnBusinessPurchased += biz =>
            {
                OnMarketplaceUpdated?.Invoke();
                OnBusinessPurchased?.Invoke(biz);
            };

            Coordinator.BusinessService.OnBusinessSold += (_, _) => OnMarketplaceUpdated?.Invoke();

            SelectedBusiness = UnownedBusinesses.FirstOrDefault() ?? BusinessService.Catalog.FirstOrDefault();
        }

        public void SelectBusiness(string businessId)
        {
            var biz = BusinessService.GetBusiness(businessId);
            if (biz != null)
            {
                SelectedBusiness = biz;
                LastErrorMessage = null;
                OnMarketplaceUpdated?.Invoke();
            }
        }

        public bool CanAffordSelected()
        {
            if (SelectedBusiness == null || SelectedBusiness.IsOwned) return false;
            return PlayerCash.MinorUnits >= SelectedBusiness.GetPurchaseCost().MinorUnits;
        }

        public bool TryPurchaseSelected(out string error)
        {
            if (SelectedBusiness == null)
            {
                error = "No business selected.";
                LastErrorMessage = error;
                OnPurchaseFailed?.Invoke(error);
                return false;
            }

            bool success = BusinessService.TryPurchase(SelectedBusiness.Id, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnPurchaseFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            SelectedBusiness = UnownedBusinesses.FirstOrDefault();
            OnMarketplaceUpdated?.Invoke();
            return true;
        }
    }
}

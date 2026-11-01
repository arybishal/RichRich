using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for the Convenience Store retail operations.
    /// Manages stock inventory, wholesale restock purchasing, customer pricing strategy,
    /// and gross margin performance tracking.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class ConvenienceStoreController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public BusinessService BusinessService => Coordinator.BusinessService;

        public Business Store => BusinessService.GetBusiness("convenience_store");
        public bool IsOwned => Store != null && Store.IsOwned;
        public bool IsOpen => Store != null && Store.IsOpen;
        public int StoreLevel => Store != null ? Store.Level : 1;
        public int Reputation => Store != null ? Store.Reputation : 60;

        public IReadOnlyList<StoreProduct> Products => BusinessService.ConvenienceStoreCatalog;
        public StoreProduct SelectedProduct { get; private set; }

        public StoreDailySalesResult LastDailySalesResult { get; private set; }
        public string LastErrorMessage { get; private set; }

        public event Action OnStoreUpdated;
        public event Action<StoreDailySalesResult> OnSalesProcessed;
        public event Action<string> OnActionFailed;

        public ConvenienceStoreController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.BusinessService.OnStoreDailySalesProcessed += sales =>
            {
                LastDailySalesResult = sales;
                OnSalesProcessed?.Invoke(sales);
                OnStoreUpdated?.Invoke();
            };

            SelectedProduct = Products.FirstOrDefault();
        }

        public void SelectProduct(string productId)
        {
            SelectedProduct = Products.FirstOrDefault(p => p.Id == productId);
            LastErrorMessage = null;
            OnStoreUpdated?.Invoke();
        }

        public bool TryOrderStock(string productId, int units, out Currency totalCost, out string error)
        {
            totalCost = Currency.Zero;
            bool success = BusinessService.OrderWholesaleStock(productId, units, out totalCost, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            OnStoreUpdated?.Invoke();
            return true;
        }

        public void SetProductPrice(string productId, Currency newPrice)
        {
            BusinessService.SetProductSellingPrice(productId, newPrice);
            OnStoreUpdated?.Invoke();
        }
    }
}

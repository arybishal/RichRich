using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for Property and Real Estate Investment.
    /// Manages property marketplace browsing, mortgage financing vs cash acquisitions,
    /// tenant management, condition renovation, and property liquidation.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class RealEstateController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public RealEstateService RealEstateService => Coordinator.RealEstateService;

        public RealEstateProperty SelectedProperty { get; private set; }
        public string LastErrorMessage { get; private set; }
        public string LastSuccessMessage { get; private set; }

        public IReadOnlyList<RealEstateProperty> AllProperties => RealEstateService.AllProperties;
        public IReadOnlyList<RealEstateProperty> OwnedProperties => RealEstateService.OwnedProperties;

        public Currency PlayerCash => Coordinator.Wallet.Balance;

        public Currency TotalValuation => RealEstateService.TotalPropertyValuation;
        public Currency TotalMortgages => RealEstateService.TotalMortgageLiabilities;
        public Currency TotalEquity => RealEstateService.TotalEquity;
        public Currency MonthlyRentalIncome => RealEstateService.TotalMonthlyRentalIncome;
        public Currency MonthlyDebtService => RealEstateService.TotalMonthlyDebtService;
        public Currency MonthlyMaintenance => RealEstateService.TotalMonthlyMaintenance;
        public long MonthlyNetCashFlowCents => RealEstateService.TotalMonthlyNetCashFlowCents;

        public event Action OnRealEstateUpdated;
        public event Action<RealEstateProperty> OnPropertyAcquired;
        public event Action<RealEstateProperty, Currency> OnPropertySold;
        public event Action<string, bool> OnActionFeedback;

        public RealEstateController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.RealEstateService.OnPropertyAcquired += prop =>
            {
                OnPropertyAcquired?.Invoke(prop);
                OnRealEstateUpdated?.Invoke();
            };

            Coordinator.RealEstateService.OnPropertySold += (prop, netProceeds) =>
            {
                OnPropertySold?.Invoke(prop, netProceeds);
                OnRealEstateUpdated?.Invoke();
            };

            Coordinator.RealEstateService.OnPortfolioUpdated += () =>
            {
                OnRealEstateUpdated?.Invoke();
            };

            SelectedProperty = AllProperties.FirstOrDefault();
        }

        public void SelectProperty(string propertyId)
        {
            var prop = RealEstateService.GetProperty(propertyId);
            if (prop != null)
            {
                SelectedProperty = prop;
                LastErrorMessage = null;
                LastSuccessMessage = null;
                OnRealEstateUpdated?.Invoke();
            }
        }

        public bool CanAffordCash(RealEstateProperty prop)
        {
            if (prop == null || prop.IsOwned) return false;
            return PlayerCash >= prop.PurchasePrice;
        }

        public bool CanAffordMortgage(RealEstateProperty prop)
        {
            if (prop == null || prop.IsOwned) return false;
            return PlayerCash >= prop.DownPaymentAmount;
        }

        public bool TryBuySelected(bool withMortgage, out string error)
        {
            if (SelectedProperty == null)
            {
                error = "No property selected.";
                SetFeedback(error, isError: true);
                return false;
            }

            bool success = RealEstateService.BuyProperty(SelectedProperty.Id, withMortgage, out error);
            if (success)
            {
                string msg = withMortgage
                    ? $"Acquired {SelectedProperty.Name} with mortgage financing (Down payment: {SelectedProperty.DownPaymentAmount})."
                    : $"Acquired {SelectedProperty.Name} outright for cash ({SelectedProperty.PurchasePrice}).";
                SetFeedback(msg, isError: false);
            }
            else
            {
                SetFeedback(error, isError: true);
            }

            return success;
        }

        public bool TrySellSelected(out Currency netProceeds, out string error)
        {
            netProceeds = Currency.Zero;
            if (SelectedProperty == null || !SelectedProperty.IsOwned)
            {
                error = "Selected property is not owned in your portfolio.";
                SetFeedback(error, isError: true);
                return false;
            }

            string name = SelectedProperty.Name;
            bool success = RealEstateService.SellProperty(SelectedProperty.Id, out netProceeds, out error);
            if (success)
            {
                SetFeedback($"Liquidated {name} for net proceeds of {netProceeds} (settling any outstanding mortgage).", isError: false);
            }
            else
            {
                SetFeedback(error, isError: true);
            }

            return success;
        }

        public bool TrySignTenantSelected(out string error)
        {
            if (SelectedProperty == null || !SelectedProperty.IsOwned)
            {
                error = "Selected property is not owned.";
                SetFeedback(error, isError: true);
                return false;
            }

            if (SelectedProperty.IsRented)
            {
                error = "A tenant lease is already active.";
                SetFeedback(error, isError: true);
                return false;
            }

            bool success = RealEstateService.SignTenant(SelectedProperty.Id, SelectedProperty.BaseMonthlyRent, out error);
            if (success)
            {
                SetFeedback($"Signed commercial tenant at {SelectedProperty.TenantMonthlyRent}/mo for {SelectedProperty.Name}.", isError: false);
            }
            else
            {
                SetFeedback(error, isError: true);
            }

            return success;
        }

        public bool TryEvictTenantSelected(out string error)
        {
            if (SelectedProperty == null || !SelectedProperty.IsOwned)
            {
                error = "Selected property is not owned.";
                SetFeedback(error, isError: true);
                return false;
            }

            if (!SelectedProperty.IsRented)
            {
                error = "No active tenant lease to terminate.";
                SetFeedback(error, isError: true);
                return false;
            }

            bool success = RealEstateService.EvictTenant(SelectedProperty.Id, out error);
            if (success)
            {
                SetFeedback($"Terminated lease for {SelectedProperty.Name}.", isError: false);
            }
            else
            {
                SetFeedback(error, isError: true);
            }

            return success;
        }

        public bool TryRenovateSelected(out string error)
        {
            if (SelectedProperty == null || !SelectedProperty.IsOwned)
            {
                error = "Selected property is not owned.";
                SetFeedback(error, isError: true);
                return false;
            }

            if (SelectedProperty.Condition >= 95.0)
            {
                error = "Property is already in pristine condition (>95%).";
                SetFeedback(error, isError: true);
                return false;
            }

            bool success = RealEstateService.RenovateProperty(SelectedProperty.Id, out error);
            if (success)
            {
                SetFeedback($"Completed comprehensive renovation for {SelectedProperty.Name}. Condition restored.", isError: false);
            }
            else
            {
                SetFeedback(error, isError: true);
            }

            return success;
        }

        private void SetFeedback(string message, bool isError)
        {
            if (isError)
            {
                LastErrorMessage = message;
                LastSuccessMessage = null;
            }
            else
            {
                LastSuccessMessage = message;
                LastErrorMessage = null;
            }

            OnActionFeedback?.Invoke(message, isError);
            OnRealEstateUpdated?.Invoke();
        }
    }
}

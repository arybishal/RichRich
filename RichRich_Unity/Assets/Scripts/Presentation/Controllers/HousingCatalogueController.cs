using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for the Housing Catalogue and Upgrade Screen.
    /// Manages browsing 5 accommodation tiers, verifying tenant qualification, move-in deposits,
    /// and comparing comfort and energy recovery rates.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class HousingCatalogueController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public HousingService HousingService => Coordinator.HousingService;

        public IReadOnlyList<HousingTier> Catalog => HousingService.Catalog;
        public HousingTier CurrentHousing => HousingService.CurrentHousing;
        public HousingTier SelectedTier { get; private set; }

        public string CurrentHousingName => CurrentHousing.Name;
        public string CurrentRentText => $"{CurrentHousing.MonthlyRent}/mo";
        public string CurrentDepositPaidText => $"{HousingService.HousingDepositPaid}";
        public int CurrentComfortRating => CurrentHousing.ComfortRating;
        public int CurrentEnergyRecovery => CurrentHousing.EnergyRecoveryPerNight;

        public bool IsSelectedCurrent => SelectedTier != null && SelectedTier.Id == CurrentHousing.Id;

        public event Action OnHousingUpdated;

        public HousingCatalogueController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.HousingService.OnHousingChanged += _ => OnHousingUpdated?.Invoke();

            SelectedTier = CurrentHousing;
        }

        public void SelectTier(string tierId)
        {
            SelectedTier = Catalog.FirstOrDefault(h => h.Id == tierId) ?? CurrentHousing;
            OnHousingUpdated?.Invoke();
        }

        public bool CanMoveToSelected(out string reason)
        {
            if (SelectedTier == null)
            {
                reason = "No housing tier selected.";
                return false;
            }

            return HousingService.CanMoveTo(SelectedTier.Id, out reason);
        }

        public bool MoveToSelected(out string error)
        {
            if (SelectedTier == null)
            {
                error = "No housing tier selected.";
                return false;
            }

            bool success = HousingService.TryMoveTo(SelectedTier.Id, out error);
            OnHousingUpdated?.Invoke();
            return success;
        }
    }
}

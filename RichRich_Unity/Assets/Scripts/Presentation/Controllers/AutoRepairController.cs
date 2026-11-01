using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for the Auto Repair Workshop operations.
    /// Manages service bay allocation, vehicle intake, repair task progression, and payout collection.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class AutoRepairController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public BusinessService BusinessService => Coordinator.BusinessService;

        public Business Workshop => BusinessService.GetBusiness("auto_repair");
        public bool IsOwned => Workshop != null && Workshop.IsOwned;
        public bool IsOpen => Workshop != null && Workshop.IsOpen;
        public int WorkshopLevel => Workshop != null ? Workshop.Level : 1;
        public int Reputation => Workshop != null ? Workshop.Reputation : 50;

        public IReadOnlyList<ServiceBay> ServiceBays => BusinessService.AutoRepairBays;
        public IReadOnlyList<VehicleRepairJob> AvailableJobs => BusinessService.AvailableRepairJobs;

        public string LastErrorMessage { get; private set; }

        public event Action OnWorkshopUpdated;
        public event Action<VehicleRepairJob> OnJobCompleted;
        public event Action<string> OnActionFailed;

        public AutoRepairController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.BusinessService.OnRepairJobCompleted += job =>
            {
                OnJobCompleted?.Invoke(job);
                OnWorkshopUpdated?.Invoke();
            };

            Coordinator.BusinessService.OnBusinessUpgraded += biz =>
            {
                if (biz.Id == "auto_repair") OnWorkshopUpdated?.Invoke();
            };
        }

        public bool TryAssignJobToBay(string jobId, int bayId, out string error)
        {
            bool success = BusinessService.TryStartRepairJob(jobId, bayId, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            OnWorkshopUpdated?.Invoke();
            return true;
        }

        public bool TryDeliverRepairedVehicle(int bayId, out Currency payout, out string error)
        {
            payout = Currency.Zero;
            bool success = BusinessService.TryClaimCompletedRepair(bayId, out payout, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            OnWorkshopUpdated?.Invoke();
            return true;
        }

        public void Refresh()
        {
            OnWorkshopUpdated?.Invoke();
        }
    }
}

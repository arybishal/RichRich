using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for the Digital Marketing Agency service operations.
    /// Manages corporate client contracts, skill requirement verification, project hours contribution,
    /// and campaign milestone payouts.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class DigitalAgencyController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public BusinessService BusinessService => Coordinator.BusinessService;

        public Business Agency => BusinessService.GetBusiness("digital_agency");
        public bool IsOwned => Agency != null && Agency.IsOwned;
        public bool IsOpen => Agency != null && Agency.IsOpen;
        public int AgencyLevel => Agency != null ? Agency.Level : 1;
        public int Reputation => Agency != null ? Agency.Reputation : 70;

        public IReadOnlyList<ClientProject> AvailableProjects => BusinessService.AgencyProjects.Where(p => !p.IsAccepted && !p.IsCompleted && !p.IsFailed).ToList();
        public IReadOnlyList<ClientProject> ActiveProjects => BusinessService.AgencyProjects.Where(p => p.IsAccepted && !p.IsCompleted && !p.IsFailed).ToList();
        public IReadOnlyList<ClientProject> CompletedProjects => BusinessService.AgencyProjects.Where(p => p.IsCompleted).ToList();

        public ClientProject SelectedProject { get; private set; }
        public string LastErrorMessage { get; private set; }

        public event Action OnAgencyUpdated;
        public event Action<ClientProject> OnProjectCompleted;
        public event Action<string> OnActionFailed;

        public DigitalAgencyController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.BusinessService.OnAgencyProjectCompleted += proj =>
            {
                OnProjectCompleted?.Invoke(proj);
                OnAgencyUpdated?.Invoke();
            };

            SelectedProject = AvailableProjects.FirstOrDefault() ?? ActiveProjects.FirstOrDefault();
        }

        public void SelectProject(string projectId)
        {
            SelectedProject = BusinessService.AgencyProjects.FirstOrDefault(p => p.ProjectId == projectId);
            LastErrorMessage = null;
            OnAgencyUpdated?.Invoke();
        }

        public bool TryAcceptProject(string projectId, out string error)
        {
            bool success = BusinessService.TryAcceptClientProject(projectId, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            OnAgencyUpdated?.Invoke();
            return true;
        }

        public bool TryWorkOnProject(string projectId, int hours, out string error)
        {
            var project = BusinessService.AgencyProjects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null || !project.IsAccepted || project.IsCompleted)
            {
                error = "Active project not found.";
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            // Energy cost for owner working: 5 energy per hour
            int energyCost = hours * 5;
            if (Coordinator.LifestyleService.CurrentEnergy < energyCost)
            {
                error = $"Insufficient energy ({Coordinator.LifestyleService.CurrentEnergy} / {energyCost}) to work {hours} hours.";
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            Coordinator.LifestyleService.ConsumeEnergy(energyCost);
            BusinessService.ContributeAgencyWorkHours(projectId, hours);

            LastErrorMessage = null;
            OnAgencyUpdated?.Invoke();
            error = null;
            return true;
        }

        public bool TryClaimProjectPayout(string projectId, out Currency payout, out string error)
        {
            payout = Currency.Zero;
            bool success = BusinessService.ClaimCompletedProject(projectId, out payout, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            OnAgencyUpdated?.Invoke();
            return true;
        }
    }
}

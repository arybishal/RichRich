using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for the Interactive Job Board and Career Profile.
    /// Manages track filtering, vacancy selection, requirement inspection, job applications,
    /// promotions, and resignations.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class JobBoardController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public CareerService CareerService => Coordinator.CareerService;

        public CareerTrack? FilterTrack { get; private set; } = null;
        public JobListing SelectedJob { get; private set; }
        public JobApplicationResult LastApplicationResult { get; private set; }

        public IReadOnlyList<JobListing> FilteredJobs
        {
            get
            {
                if (FilterTrack.HasValue)
                {
                    return CareerService.GetJobsByTrack(FilterTrack.Value);
                }
                return CareerService.AvailableJobs;
            }
        }

        public bool IsEmployed => CareerService.IsEmployed;
        public string CurrentJobTitle => IsEmployed ? CareerService.CurrentEmployment.Job.Title : "Unemployed";
        public string CurrentCompany => IsEmployed ? CareerService.CurrentEmployment.Job.Company : "None";
        public string CurrentSalaryFormatted => IsEmployed ? $"{CareerService.CurrentMonthlySalary}/mo" : "$0.00/mo";
        public int TotalMonthsWorked => IsEmployed ? CareerService.CurrentEmployment.TotalMonthsWorked : 0;
        public int CurrentTenureMonths => IsEmployed ? CareerService.CurrentEmployment.CurrentJobTenureMonths : 0;
        public string CareerTrackText => IsEmployed ? CareerService.CurrentEmployment.Job.Track.ToString() : "General";

        public string RetailExperienceText => $"{CareerService.GetTrackExperienceMonths(CareerTrack.Retail)} months";
        public string TechExperienceText => $"{CareerService.GetTrackExperienceMonths(CareerTrack.Technology)} months";
        public string BusinessExperienceText => $"{CareerService.GetTrackExperienceMonths(CareerTrack.Business)} months";

        public event Action OnJobBoardUpdated;
        public event Action<JobApplicationResult> OnApplicationProcessed;

        public JobBoardController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.CareerService.OnHired += _ => OnJobBoardUpdated?.Invoke();
            Coordinator.CareerService.OnResigned += _ => OnJobBoardUpdated?.Invoke();
            Coordinator.CareerService.OnPromoted += (_, _) => OnJobBoardUpdated?.Invoke();

            SelectedJob = FilteredJobs.FirstOrDefault();
        }

        public void SetTrackFilter(CareerTrack? track)
        {
            FilterTrack = track;
            SelectedJob = FilteredJobs.FirstOrDefault();
            OnJobBoardUpdated?.Invoke();
        }

        public void SelectJob(string jobId)
        {
            SelectedJob = CareerService.AvailableJobs.FirstOrDefault(j => j.Id == jobId);
            OnJobBoardUpdated?.Invoke();
        }

        public bool CanApplySelected(out string reason)
        {
            if (SelectedJob == null)
            {
                reason = "No job selected.";
                return false;
            }

            return CareerService.CanApply(SelectedJob.Id, out reason);
        }

        public JobApplicationResult ApplyForSelected()
        {
            if (SelectedJob == null)
            {
                var err = JobApplicationResult.Rejected(JobApplicationStatus.PositionNotFound, "No job selected.", null);
                LastApplicationResult = err;
                OnApplicationProcessed?.Invoke(err);
                return err;
            }

            var result = CareerService.SubmitApplication(SelectedJob.Id);
            LastApplicationResult = result;
            OnApplicationProcessed?.Invoke(result);
            OnJobBoardUpdated?.Invoke();
            return result;
        }

        public bool TryApplyForPromotion(out string message)
        {
            bool success = CareerService.TryApplyForPromotion(out message);
            OnJobBoardUpdated?.Invoke();
            return success;
        }

        public bool CheckPromotionAvailability(out string promotionDetails)
        {
            bool eligible = CareerService.GetNextPromotion(out var nextJob, out bool canPromote, out string feedback);
            if (nextJob != null)
            {
                promotionDetails = $"{nextJob.Title} at {nextJob.Company} ({nextJob.MonthlySalary}/mo). {feedback}";
                return canPromote;
            }

            promotionDetails = feedback;
            return false;
        }

        public void ResignCurrentJob()
        {
            CareerService.ResignCurrentJob();
            OnJobBoardUpdated?.Invoke();
        }
    }
}

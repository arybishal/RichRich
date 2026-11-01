using System;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for the Player Lifestyle Status and Energy system.
    /// Manages fatigue indicators, rest actions, sleep transitions, and weekly time allocation.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class LifestyleStatusController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public LifestyleService LifestyleService => Coordinator.LifestyleService;

        public int CurrentEnergy => LifestyleService.CurrentEnergy;
        public int MaxEnergy => LifestyleService.MaxEnergy;
        public float EnergyNormalized => (float)CurrentEnergy / MaxEnergy;
        public string EnergyBarText => $"{CurrentEnergy} / {MaxEnergy}";

        public FatigueLevel FatigueState => LifestyleService.FatigueState;
        public string FatigueBadgeText => FatigueState.ToString();

        public string HousingNameText => Coordinator.HousingService.CurrentHousing.Name;
        public int HousingComfortRating => Coordinator.HousingService.CurrentHousing.ComfortRating;
        public int OvernightRecoveryAmount => Coordinator.HousingService.CurrentHousing.EnergyRecoveryPerNight;

        public int WeeklyWorkHours => Coordinator.CareerService.IsEmployed ? Coordinator.CareerService.CurrentEmployment.Job.WeeklyHours : 0;
        public int WeeklyStudyHours
        {
            get
            {
                if (Coordinator.EducationService.ActiveEnrollment == null || Coordinator.EducationService.ActiveEnrollment.IsCompleted)
                    return 0;

                return Coordinator.EducationService.CurrentStudyPace == StudyPace.FullTime ? 30 : 10;
            }
        }

        public string ScheduleSummaryText => $"Work: {WeeklyWorkHours}h/wk | Study: {WeeklyStudyHours}h/wk";

        public event Action OnLifestyleUpdated;

        public LifestyleStatusController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.LifestyleService.OnEnergyChanged += (_, _) => OnLifestyleUpdated?.Invoke();
            Coordinator.LifestyleService.OnFatigueChanged += _ => OnLifestyleUpdated?.Invoke();
            Coordinator.HousingService.OnHousingChanged += _ => OnLifestyleUpdated?.Invoke();
            Coordinator.CareerService.OnHired += _ => OnLifestyleUpdated?.Invoke();
            Coordinator.CareerService.OnResigned += _ => OnLifestyleUpdated?.Invoke();
            Coordinator.EducationService.OnEnrolled += _ => OnLifestyleUpdated?.Invoke();
            Coordinator.EducationService.OnCourseCompleted += _ => OnLifestyleUpdated?.Invoke();
        }

        public void TakeRest()
        {
            LifestyleService.TakeRest(20);
            OnLifestyleUpdated?.Invoke();
        }

        public void SleepToMorning()
        {
            Coordinator.Clock.FastForwardToNextMorning();
            OnLifestyleUpdated?.Invoke();
        }
    }
}

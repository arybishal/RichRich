using System;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Service managing player energy, work shift demands, study fatigue, rest, and sleep recovery.
    /// Balances professional and academic ambition against living standards and exhaustion.
    /// Pure C# domain service with zero MonoBehaviour dependencies.
    /// </summary>
    public class LifestyleService
    {
        public const int DefaultMaxEnergy = 100;

        public int CurrentEnergy { get; private set; }
        public int MaxEnergy { get; }

        public SimulationClock Clock { get; }
        public CareerService CareerService { get; }
        public EducationService EducationService { get; }
        public HousingService HousingService { get; }

        public FatigueLevel FatigueState
        {
            get
            {
                if (CurrentEnergy >= 70) return FatigueLevel.WellRested;
                if (CurrentEnergy >= 30) return FatigueLevel.Normal;
                if (CurrentEnergy >= 10) return FatigueLevel.Fatigued;
                return FatigueLevel.Exhausted;
            }
        }

        public event Action<int, int> OnEnergyChanged;
        public event Action<FatigueLevel> OnFatigueChanged;

        public LifestyleService(
            SimulationClock clock,
            CareerService careerService,
            EducationService educationService,
            HousingService housingService,
            int initialEnergy = DefaultMaxEnergy,
            int maxEnergy = DefaultMaxEnergy)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            CareerService = careerService ?? throw new ArgumentNullException(nameof(careerService));
            EducationService = educationService ?? throw new ArgumentNullException(nameof(educationService));
            HousingService = housingService ?? throw new ArgumentNullException(nameof(housingService));

            MaxEnergy = Math.Max(10, maxEnergy);
            CurrentEnergy = Math.Clamp(initialEnergy, 0, MaxEnergy);

            Clock.OnDayPassed += ProcessDailyEnergyCycle;
        }

        public void ConsumeEnergy(int amount)
        {
            if (amount <= 0) return;

            var priorState = FatigueState;
            CurrentEnergy = Math.Clamp(CurrentEnergy - amount, 0, MaxEnergy);
            OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);

            if (FatigueState != priorState)
            {
                OnFatigueChanged?.Invoke(FatigueState);
            }
        }

        public void RestoreEnergy(int amount)
        {
            if (amount <= 0) return;

            var priorState = FatigueState;
            CurrentEnergy = Math.Clamp(CurrentEnergy + amount, 0, MaxEnergy);
            OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);

            if (FatigueState != priorState)
            {
                OnFatigueChanged?.Invoke(FatigueState);
            }
        }

        public void TakeRest(int restAmount = 20)
        {
            RestoreEnergy(restAmount);
        }

        public void ProcessSleepRecovery()
        {
            int recovery = HousingService.CurrentHousing.EnergyRecoveryPerNight;
            RestoreEnergy(recovery);
        }

        private void ProcessDailyEnergyCycle(GameDateTime currentDate)
        {
            // 1. Work Shift Consumption (Weekdays: 5 out of 7 days)
            int dayOfWeekMod = (currentDate.Day % 7);
            bool isWorkDay = dayOfWeekMod != 6 && dayOfWeekMod != 0; // Days 1-5 work, 6-7 weekend

            if (isWorkDay && CareerService.IsEmployed)
            {
                int weeklyHours = CareerService.CurrentEmployment.Job.WeeklyHours;
                // E.g. 40 hrs -> 25 energy; 45 hrs -> 30 energy; 50 hrs -> 35 energy
                int workEnergyCost = (int)Math.Round((weeklyHours / 40.0) * 25.0);
                ConsumeEnergy(workEnergyCost);
            }

            // 2. Study Consumption
            if (EducationService.ActiveEnrollment != null && !EducationService.ActiveEnrollment.IsCompleted)
            {
                int studyCost = EducationService.CurrentStudyPace == StudyPace.FullTime ? 30 : 10;
                ConsumeEnergy(studyCost);
            }

            // 3. Overnight Sleep Recovery
            ProcessSleepRecovery();
        }

        public LifestyleStats GetStats()
        {
            int workHours = CareerService.IsEmployed ? CareerService.CurrentEmployment.Job.WeeklyHours : 0;
            int studyHours = 0;
            if (EducationService.ActiveEnrollment != null && !EducationService.ActiveEnrollment.IsCompleted)
            {
                studyHours = EducationService.CurrentStudyPace == StudyPace.FullTime ? 30 : 10;
            }

            return new LifestyleStats(
                CurrentEnergy,
                MaxEnergy,
                FatigueState,
                HousingService.CurrentHousing.ComfortRating,
                workHours,
                studyHours
            );
        }

        public void RestoreSavedEnergy(int energy)
        {
            CurrentEnergy = Math.Clamp(energy, 0, MaxEnergy);
            OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
        }
    }
}

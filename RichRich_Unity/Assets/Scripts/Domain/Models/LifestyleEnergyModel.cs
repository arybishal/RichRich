using System;

namespace RichRich.Domain.Models
{
    public enum FatigueLevel
    {
        WellRested,
        Normal,
        Fatigued,
        Exhausted
    }

    /// <summary>
    /// Snapshot summary of player lifestyle metrics.
    /// </summary>
    public class LifestyleStats
    {
        public int CurrentEnergy { get; }
        public int MaxEnergy { get; }
        public FatigueLevel FatigueState { get; }
        public int HousingComfortRating { get; }
        public int WeeklyWorkHours { get; }
        public int WeeklyStudyHours { get; }

        public LifestyleStats(
            int currentEnergy,
            int maxEnergy,
            FatigueLevel fatigueState,
            int housingComfortRating,
            int weeklyWorkHours,
            int weeklyStudyHours)
        {
            CurrentEnergy = currentEnergy;
            MaxEnergy = maxEnergy;
            FatigueState = fatigueState;
            HousingComfortRating = housingComfortRating;
            WeeklyWorkHours = weeklyWorkHours;
            WeeklyStudyHours = weeklyStudyHours;
        }
    }
}

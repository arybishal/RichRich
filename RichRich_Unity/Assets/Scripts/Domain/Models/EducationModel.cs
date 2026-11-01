using System;
using System.Collections.Generic;

namespace RichRich.Domain.Models
{
    public enum SkillType
    {
        Technical,
        Communication,
        Management,
        Sales,
        Analytics
    }

    public enum EducationType
    {
        ShortCourse,
        ProfessionalCertificate,
        Degree
    }

    public enum StudyPace
    {
        PartTime = 2,  // 2 hours/day
        FullTime = 6   // 6 hours/day
    }

    /// <summary>
    /// Educational course or degree program specification.
    /// </summary>
    public class EducationCourse
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public EducationType Type { get; }
        public Currency TuitionCost { get; }
        public int TotalStudyHoursRequired { get; }
        public int RecommendedWeeklyHours { get; }
        public string PrerequisiteQualification { get; }
        public string GrantedQualification { get; }
        public IReadOnlyDictionary<SkillType, int> SkillRewards { get; }

        public EducationCourse(
            string id,
            string title,
            string description,
            Currency tuitionCost,
            int totalStudyHoursRequired,
            int recommendedWeeklyHours,
            string grantedQualification = null,
            string prerequisiteQualification = null,
            Dictionary<SkillType, int> skillRewards = null,
            EducationType type = EducationType.ShortCourse)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? string.Empty;
            TuitionCost = tuitionCost;
            TotalStudyHoursRequired = Math.Max(1, totalStudyHoursRequired);
            RecommendedWeeklyHours = Math.Max(1, recommendedWeeklyHours);
            GrantedQualification = grantedQualification;
            PrerequisiteQualification = prerequisiteQualification;
            SkillRewards = skillRewards ?? new Dictionary<SkillType, int>();
            Type = type;
        }
    }

    /// <summary>
    /// Player's active or completed course enrollment.
    /// </summary>
    public class EducationEnrollment
    {
        public EducationCourse Course { get; }
        public GameDateTime EnrolledDate { get; }
        public double HoursCompleted { get; private set; }
        public StudyPace Pace { get; private set; }
        public bool IsCompleted => HoursCompleted >= Course.TotalStudyHoursRequired;
        public GameDateTime? CompletedDate { get; private set; }

        public float ProgressNormalized => (float)Math.Clamp(HoursCompleted / Course.TotalStudyHoursRequired, 0.0, 1.0);

        public EducationEnrollment(
            EducationCourse course,
            GameDateTime enrolledDate,
            double hoursCompleted = 0.0,
            StudyPace pace = StudyPace.PartTime)
        {
            Course = course ?? throw new ArgumentNullException(nameof(course));
            EnrolledDate = enrolledDate;
            HoursCompleted = Math.Max(0.0, hoursCompleted);
            Pace = pace;
        }

        public void SetPace(StudyPace pace)
        {
            Pace = pace;
        }

        public bool AddStudyHours(double hours, GameDateTime currentDate)
        {
            if (IsCompleted || hours <= 0) return false;

            HoursCompleted += hours;
            if (HoursCompleted >= Course.TotalStudyHoursRequired)
            {
                HoursCompleted = Course.TotalStudyHoursRequired;
                CompletedDate = currentDate;
                return true; // Just completed
            }

            return false;
        }
    }
}

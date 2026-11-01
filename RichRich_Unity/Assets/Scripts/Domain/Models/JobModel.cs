using System;
using System.Collections.Generic;

namespace RichRich.Domain.Models
{
    public enum CareerTrack
    {
        General,
        Retail,
        Technology,
        Business
    }

    public enum JobApplicationStatus
    {
        Offered,
        RejectedMissingQualification,
        RejectedInsufficientSkills,
        RejectedInsufficientExperience,
        RejectedTenureNotMet,
        PositionNotFound
    }

    public class JobApplicationResult
    {
        public bool Success { get; }
        public JobApplicationStatus Status { get; }
        public string Message { get; }
        public JobListing Job { get; }

        public JobApplicationResult(bool success, JobApplicationStatus status, string message, JobListing job)
        {
            Success = success;
            Status = status;
            Message = message ?? string.Empty;
            Job = job;
        }

        public static JobApplicationResult Accepted(JobListing job)
        {
            return new JobApplicationResult(true, JobApplicationStatus.Offered, $"Congratulations! You have been hired as {job.Title} at {job.Company}.", job);
        }

        public static JobApplicationResult Rejected(JobApplicationStatus status, string reason, JobListing job)
        {
            return new JobApplicationResult(false, status, reason, job);
        }
    }

    /// <summary>
    /// Employment vacancy / job specification within a career ladder.
    /// </summary>
    public class JobListing
    {
        public string Id { get; }
        public string Title { get; }
        public string Company { get; }
        public Currency MonthlySalary { get; }
        public int WeeklyHours { get; }
        public int PayDayOfMonth { get; }
        public string RequiredQualification { get; }
        public IReadOnlyDictionary<SkillType, int> RequiredSkills { get; }
        public int MinExperienceMonths { get; }
        public CareerTrack Track { get; }
        public int Tier { get; }
        public int RequiredTrackExperienceMonths { get; }
        public int RequiredTenureMonths { get; }
        public string Description { get; }

        public JobListing(
            string id,
            string title,
            string company,
            Currency monthlySalary,
            int weeklyHours = 40,
            int payDayOfMonth = 30,
            string requiredQualification = null,
            Dictionary<SkillType, int> requiredSkills = null,
            int minExperienceMonths = 0,
            CareerTrack track = CareerTrack.General,
            int tier = 1,
            int requiredTrackExperienceMonths = 0,
            int requiredTenureMonths = 0,
            string description = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Company = company ?? throw new ArgumentNullException(nameof(company));
            MonthlySalary = monthlySalary;
            WeeklyHours = weeklyHours;
            PayDayOfMonth = Math.Clamp(payDayOfMonth, 1, 30);
            RequiredQualification = requiredQualification;
            RequiredSkills = requiredSkills ?? new Dictionary<SkillType, int>();
            MinExperienceMonths = Math.Max(0, minExperienceMonths);
            Track = track;
            Tier = Math.Max(1, tier);
            RequiredTrackExperienceMonths = Math.Max(0, requiredTrackExperienceMonths);
            RequiredTenureMonths = Math.Max(0, requiredTenureMonths);
            Description = description ?? $"{title} role at {company}.";
        }
    }

    /// <summary>
    /// Active employment record for the player.
    /// </summary>
    public class EmploymentRecord
    {
        public JobListing Job { get; }
        public GameDateTime HiredDate { get; }
        public GameDateTime? LastPaidDate { get; private set; }
        public int TotalMonthsWorked { get; private set; }
        public int CurrentJobTenureMonths { get; private set; }
        public bool IsActive { get; private set; }
        public double PerformanceRating { get; private set; }

        public EmploymentRecord(
            JobListing job,
            GameDateTime hiredDate,
            GameDateTime? lastPaidDate = null,
            int totalMonthsWorked = 0,
            int currentJobTenureMonths = 0,
            double performanceRating = 1.0)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            HiredDate = hiredDate;
            LastPaidDate = lastPaidDate;
            TotalMonthsWorked = Math.Max(0, totalMonthsWorked);
            CurrentJobTenureMonths = Math.Max(0, currentJobTenureMonths);
            IsActive = true;
            PerformanceRating = Math.Clamp(performanceRating, 0.0, 1.0);
        }

        public void RecordSalaryPaid(GameDateTime payDate)
        {
            LastPaidDate = payDate;
            TotalMonthsWorked++;
            CurrentJobTenureMonths++;
        }

        public void SetPerformanceRating(double rating)
        {
            PerformanceRating = Math.Clamp(rating, 0.0, 1.0);
        }

        public void Resign(GameDateTime date)
        {
            IsActive = false;
        }
    }
}

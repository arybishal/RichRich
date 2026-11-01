using System;

namespace RichRich.Domain.Models
{
    public enum AgencyProjectCategory
    {
        SocialMedia,
        SEO,
        WebDevelopment,
        AdCampaign
    }

    /// <summary>
    /// Service contract project in the Digital Marketing Agency.
    /// Requires labor hours, skill requirements, and adherence to client deadlines.
    /// </summary>
    public class ClientProject
    {
        public string ProjectId { get; }
        public string ClientName { get; }
        public AgencyProjectCategory Category { get; }
        public int TotalHoursRequired { get; }
        public int CompletedHours { get; private set; }
        public int DeadlineDays { get; }
        public int DaysElapsed { get; private set; }
        public Currency ContractPayment { get; }
        public int ReputationGain { get; }
        public SkillType? RequiredSkill { get; }
        public int RequiredSkillLevel { get; }

        public bool IsAccepted { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsFailed { get; private set; }

        public float ProgressNormalized => (float)Math.Clamp((double)CompletedHours / TotalHoursRequired, 0.0, 1.0);
        public int DaysRemaining => Math.Max(0, DeadlineDays - DaysElapsed);

        public ClientProject(
            string projectId,
            string clientName,
            AgencyProjectCategory category,
            int totalHoursRequired,
            int deadlineDays,
            Currency contractPayment,
            int reputationGain = 5,
            SkillType? requiredSkill = null,
            int requiredSkillLevel = 0,
            int completedHours = 0,
            int daysElapsed = 0,
            bool isAccepted = false,
            bool isCompleted = false,
            bool isFailed = false)
        {
            ProjectId = projectId ?? Guid.NewGuid().ToString("N");
            ClientName = clientName ?? "Corporate Client";
            Category = category;
            TotalHoursRequired = Math.Max(1, totalHoursRequired);
            DeadlineDays = Math.Max(1, deadlineDays);
            ContractPayment = contractPayment;
            ReputationGain = reputationGain;
            RequiredSkill = requiredSkill;
            RequiredSkillLevel = Math.Max(0, requiredSkillLevel);
            CompletedHours = Math.Clamp(completedHours, 0, TotalHoursRequired);
            DaysElapsed = Math.Max(0, daysElapsed);
            IsAccepted = isAccepted;
            IsCompleted = isCompleted;
            IsFailed = isFailed;
        }

        public void Accept()
        {
            IsAccepted = true;
            IsCompleted = false;
            IsFailed = false;
        }

        public bool ContributeWorkHours(int hours)
        {
            if (!IsAccepted || IsCompleted || IsFailed || hours <= 0) return false;

            CompletedHours += hours;
            if (CompletedHours >= TotalHoursRequired)
            {
                CompletedHours = TotalHoursRequired;
                IsCompleted = true;
                return true;
            }
            return false;
        }

        public bool AdvanceDay()
        {
            if (!IsAccepted || IsCompleted || IsFailed) return false;

            DaysElapsed++;
            if (DaysElapsed > DeadlineDays)
            {
                IsFailed = true;
                return true; // Just missed deadline
            }
            return false;
        }
    }
}

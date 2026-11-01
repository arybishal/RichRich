using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Service managing job listings, career ladders, applications, promotions, work experience,
    /// and scheduled salary payouts.
    /// Pure C# domain service with zero MonoBehaviour dependencies.
    /// </summary>
    public class CareerService
    {
        private readonly List<JobListing> _availableJobs = new List<JobListing>();
        private readonly Dictionary<CareerTrack, int> _trackExperienceMonths = new Dictionary<CareerTrack, int>();

        public IReadOnlyList<JobListing> AvailableJobs => _availableJobs;
        public EmploymentRecord CurrentEmployment { get; private set; }
        public bool IsEmployed => CurrentEmployment != null && CurrentEmployment.IsActive;

        public FinancialService FinancialService { get; }
        public EducationService EducationService { get; }
        public SimulationClock Clock { get; }

        public Currency CurrentMonthlySalary => IsEmployed ? CurrentEmployment.Job.MonthlySalary : Currency.Zero;

        public event Action<EmploymentRecord> OnHired;
        public event Action<EmploymentRecord, FinancialTransaction> OnSalaryPaid;
        public event Action<EmploymentRecord> OnResigned;
        public event Action<EmploymentRecord, JobListing> OnPromoted;

        public CareerService(
            FinancialService financialService,
            EducationService educationService,
            SimulationClock clock,
            IEnumerable<JobListing> initialJobs = null)
        {
            FinancialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
            EducationService = educationService ?? throw new ArgumentNullException(nameof(educationService));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));

            foreach (CareerTrack track in Enum.GetValues(typeof(CareerTrack)))
            {
                _trackExperienceMonths[track] = 0;
            }

            if (initialJobs != null)
            {
                _availableJobs.AddRange(initialJobs);
            }
            else
            {
                _availableJobs.AddRange(GetDefaultJobs());
            }

            Clock.OnDayPassed += CheckSalaryPayout;
        }

        public static List<JobListing> GetDefaultJobs()
        {
            return new List<JobListing>
            {
                // ==================== RETAIL TRACK ====================
                new JobListing(
                    "job_retail_assistant",
                    "Retail Assistant",
                    "QuickMart Supermarkets",
                    Currency.FromDollars(1600.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    track: CareerTrack.Retail,
                    tier: 1,
                    description: "Greet customers, handle register transactions, and maintain store aisles."
                ),
                new JobListing(
                    "job_sales_assistant",
                    "Sales Assistant",
                    "Metro Direct Stores",
                    Currency.FromDollars(2000.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    track: CareerTrack.Retail,
                    tier: 1,
                    description: "Engage retail shoppers, advise on product selection, and process counter sales."
                ),
                new JobListing(
                    "job_retail_senior",
                    "Senior Retail Assistant",
                    "QuickMart Supermarkets",
                    Currency.FromDollars(2300.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Sales, 15 }, { SkillType.Communication, 10 } },
                    minExperienceMonths: 6,
                    track: CareerTrack.Retail,
                    tier: 2,
                    requiredTrackExperienceMonths: 6,
                    description: "Lead checkout operations, resolve escalated customer queries, and train new staff."
                ),
                new JobListing(
                    "job_retail_supervisor",
                    "Shift Supervisor",
                    "QuickMart Supermarkets",
                    Currency.FromDollars(3200.00),
                    weeklyHours: 42,
                    payDayOfMonth: 30,
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Management, 15 }, { SkillType.Sales, 25 } },
                    minExperienceMonths: 12,
                    track: CareerTrack.Retail,
                    tier: 3,
                    requiredTrackExperienceMonths: 12,
                    requiredTenureMonths: 6,
                    description: "Oversee daily store shift workflows, inventory receipts, and team scheduling."
                ),
                new JobListing(
                    "job_retail_store_manager",
                    "Store Manager",
                    "QuickMart Supermarkets",
                    Currency.FromDollars(4600.00),
                    weeklyHours: 45,
                    payDayOfMonth: 30,
                    requiredQualification: "qual_retail_sales",
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Management, 30 }, { SkillType.Analytics, 20 } },
                    minExperienceMonths: 24,
                    track: CareerTrack.Retail,
                    tier: 4,
                    requiredTrackExperienceMonths: 20,
                    requiredTenureMonths: 6,
                    description: "Total P&L responsibility for supermarket branch, staffing, and revenue targets."
                ),

                // ==================== TECHNOLOGY TRACK ====================
                new JobListing(
                    "job_it_helpdesk",
                    "Junior IT Helpdesk Technician",
                    "Apex Enterprise Solutions",
                    Currency.FromDollars(2800.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    requiredQualification: "qual_it_support_cert",
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Technical, 20 } },
                    track: CareerTrack.Technology,
                    tier: 1,
                    description: "Diagnose user hardware/software tickets, reset credentials, and assist workstation setup."
                ),
                new JobListing(
                    "job_it_specialist",
                    "IT Support Specialist",
                    "Apex Enterprise Solutions",
                    Currency.FromDollars(3600.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    requiredQualification: "qual_it_support_cert",
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Technical, 35 }, { SkillType.Communication, 15 } },
                    minExperienceMonths: 6,
                    track: CareerTrack.Technology,
                    tier: 2,
                    requiredTrackExperienceMonths: 6,
                    description: "Maintain network routers, manage cloud user directories, and administer company systems."
                ),
                new JobListing(
                    "job_junior_dev",
                    "Junior Software Developer",
                    "Nova Byte Dynamics",
                    Currency.FromDollars(4800.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Technical, 50 }, { SkillType.Analytics, 25 } },
                    track: CareerTrack.Technology,
                    tier: 3,
                    requiredTrackExperienceMonths: 6,
                    description: "Implement API endpoints, write automated tests, and fix bug tickets under senior review."
                ),
                new JobListing(
                    "job_software_dev",
                    "Software Developer",
                    "Nova Byte Dynamics",
                    Currency.FromDollars(6800.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Technical, 70 }, { SkillType.Analytics, 40 } },
                    minExperienceMonths: 18,
                    track: CareerTrack.Technology,
                    tier: 4,
                    requiredTrackExperienceMonths: 18,
                    requiredTenureMonths: 6,
                    description: "Architect microservices, design relational schemas, and build resilient core platforms."
                ),
                new JobListing(
                    "job_senior_dev",
                    "Senior Software Developer",
                    "Nova Byte Dynamics",
                    Currency.FromDollars(9200.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Technical, 85 }, { SkillType.Analytics, 60 }, { SkillType.Management, 20 } },
                    minExperienceMonths: 36,
                    track: CareerTrack.Technology,
                    tier: 5,
                    requiredTrackExperienceMonths: 30,
                    requiredTenureMonths: 6,
                    description: "Lead technical architecture, mentor engineering teams, and drive scalability across systems."
                ),
                new JobListing(
                    "job_tech_manager",
                    "Technology Manager",
                    "Nova Byte Dynamics",
                    Currency.FromDollars(13500.00),
                    weeklyHours: 45,
                    payDayOfMonth: 30,
                    requiredQualification: "qual_cs_degree",
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Technical, 80 }, { SkillType.Management, 50 }, { SkillType.Analytics, 50 } },
                    minExperienceMonths: 48,
                    track: CareerTrack.Technology,
                    tier: 6,
                    requiredTrackExperienceMonths: 40,
                    requiredTenureMonths: 6,
                    description: "Direct engineering department, manage multimillion-dollar tech budgets and hiring."
                ),

                // ==================== BUSINESS TRACK ====================
                new JobListing(
                    "job_office_assistant",
                    "Office Assistant",
                    "Beacon Corporate Services",
                    Currency.FromDollars(1900.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    track: CareerTrack.Business,
                    tier: 1,
                    description: "Sort corporate mail, coordinate meeting schedules, and maintain front-desk documentation."
                ),
                new JobListing(
                    "job_admin_officer",
                    "Administrative Officer",
                    "Beacon Corporate Services",
                    Currency.FromDollars(2700.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Communication, 20 }, { SkillType.Analytics, 10 } },
                    minExperienceMonths: 6,
                    track: CareerTrack.Business,
                    tier: 2,
                    requiredTrackExperienceMonths: 6,
                    description: "Manage client contracts, compile executive expense reports, and audit filing procedures."
                ),
                new JobListing(
                    "job_biz_executive",
                    "Business Operations Executive",
                    "Vanguard Holdings",
                    Currency.FromDollars(4200.00),
                    weeklyHours: 42,
                    payDayOfMonth: 30,
                    requiredQualification: "qual_business_diploma",
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Management, 25 }, { SkillType.Analytics, 25 } },
                    minExperienceMonths: 12,
                    track: CareerTrack.Business,
                    tier: 3,
                    requiredTrackExperienceMonths: 12,
                    description: "Optimize corporate procurement pipelines, analyze operational margins, and negotiate vendor deals."
                ),
                new JobListing(
                    "job_biz_manager",
                    "Business Operations Manager",
                    "Vanguard Holdings",
                    Currency.FromDollars(6500.00),
                    weeklyHours: 45,
                    payDayOfMonth: 30,
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Management, 45 }, { SkillType.Analytics, 40 } },
                    minExperienceMonths: 24,
                    track: CareerTrack.Business,
                    tier: 4,
                    requiredTrackExperienceMonths: 24,
                    requiredTenureMonths: 6,
                    description: "Lead operational teams across subsidiary offices, implement KPIs, and report to board executives."
                ),
                new JobListing(
                    "job_consultant",
                    "Enterprise Consultant",
                    "Global Advisory Partners",
                    Currency.FromDollars(10500.00),
                    weeklyHours: 50,
                    payDayOfMonth: 30,
                    requiredQualification: "qual_business_diploma",
                    requiredSkills: new Dictionary<SkillType, int> { { SkillType.Management, 60 }, { SkillType.Communication, 50 }, { SkillType.Analytics, 50 } },
                    minExperienceMonths: 36,
                    track: CareerTrack.Business,
                    tier: 5,
                    requiredTrackExperienceMonths: 30,
                    requiredTenureMonths: 6,
                    description: "Advise Fortune 500 leadership on corporate restructuring, mergers, and financial optimization."
                ),

                // ==================== GENERAL TRACK ====================
                new JobListing(
                    "job_delivery_worker",
                    "Delivery Courier",
                    "SwiftDrop Logistics",
                    Currency.FromDollars(1800.00),
                    weeklyHours: 40,
                    payDayOfMonth: 30,
                    track: CareerTrack.General,
                    tier: 1,
                    description: "Deliver packages across metropolitan zones, navigate traffic, and ensure on-time drops."
                )
            };
        }

        public IReadOnlyList<JobListing> GetJobsByTrack(CareerTrack track)
        {
            return _availableJobs.Where(j => j.Track == track).ToList();
        }

        public int GetTrackExperienceMonths(CareerTrack track)
        {
            return _trackExperienceMonths.TryGetValue(track, out int months) ? months : 0;
        }

        public void SetTrackExperience(CareerTrack track, int months)
        {
            _trackExperienceMonths[track] = Math.Max(0, months);
        }

        public bool CanApply(string jobId, out string reason)
        {
            return CanApply(jobId, out _, out reason);
        }

        public bool CanApply(string jobId, out JobApplicationStatus status, out string reason)
        {
            var job = _availableJobs.FirstOrDefault(j => j.Id == jobId);
            if (job == null)
            {
                status = JobApplicationStatus.PositionNotFound;
                reason = "Job not found in available listings.";
                return false;
            }

            // Check qualification
            if (!string.IsNullOrEmpty(job.RequiredQualification) &&
                !EducationService.HasQualification(job.RequiredQualification))
            {
                status = JobApplicationStatus.RejectedMissingQualification;
                reason = $"Missing required qualification: {job.RequiredQualification}";
                return false;
            }

            // Check skills
            foreach (var req in job.RequiredSkills)
            {
                int currentLevel = EducationService.GetSkillLevel(req.Key);
                if (currentLevel < req.Value)
                {
                    status = JobApplicationStatus.RejectedInsufficientSkills;
                    reason = $"Insufficient {req.Key} skill: Requires level {req.Value}, current is {currentLevel}.";
                    return false;
                }
            }

            // Check total overall experience
            int totalExp = CurrentEmployment != null ? CurrentEmployment.TotalMonthsWorked : 0;
            // Also take into account the sum of track experiences
            int totalTrackExp = _trackExperienceMonths.Values.Sum();
            int effectiveExp = Math.Max(totalExp, totalTrackExp);

            if (job.MinExperienceMonths > 0 && effectiveExp < job.MinExperienceMonths)
            {
                status = JobApplicationStatus.RejectedInsufficientExperience;
                reason = $"Insufficient total experience: Requires {job.MinExperienceMonths} months, current is {effectiveExp} months.";
                return false;
            }

            // Check track-specific experience
            if (job.RequiredTrackExperienceMonths > 0)
            {
                int trackExp = GetTrackExperienceMonths(job.Track);
                if (trackExp < job.RequiredTrackExperienceMonths)
                {
                    status = JobApplicationStatus.RejectedInsufficientExperience;
                    reason = $"Insufficient {job.Track} track experience: Requires {job.RequiredTrackExperienceMonths} months, current is {trackExp} months.";
                    return false;
                }
            }

            // Check minimum tenure in current position for higher tier internal promotions within the same track
            if (IsEmployed && job.Track == CurrentEmployment.Job.Track && job.Tier > CurrentEmployment.Job.Tier)
            {
                if (job.RequiredTenureMonths > 0 && CurrentEmployment.CurrentJobTenureMonths < job.RequiredTenureMonths)
                {
                    status = JobApplicationStatus.RejectedTenureNotMet;
                    reason = $"Requires at least {job.RequiredTenureMonths} months tenure in current position (currently {CurrentEmployment.CurrentJobTenureMonths} months).";
                    return false;
                }
            }

            status = JobApplicationStatus.Offered;
            reason = null;
            return true;
        }

        public JobApplicationResult SubmitApplication(string jobId)
        {
            var job = _availableJobs.FirstOrDefault(j => j.Id == jobId);
            if (job == null)
            {
                return JobApplicationResult.Rejected(JobApplicationStatus.PositionNotFound, "Job not found in available listings.", null);
            }

            if (!CanApply(jobId, out var status, out string reason))
            {
                return JobApplicationResult.Rejected(status, reason, job);
            }

            bool wasEmployed = IsEmployed;
            GameDateTime? priorLastPaid = null;
            int priorMonths = 0;
            bool isPromotion = false;

            if (wasEmployed)
            {
                priorLastPaid = CurrentEmployment.LastPaidDate;
                priorMonths = CurrentEmployment.TotalMonthsWorked;
                isPromotion = CurrentEmployment.Job.Track == job.Track && job.Tier > CurrentEmployment.Job.Tier;

                CurrentEmployment.Resign(Clock.CurrentDate);
                OnResigned?.Invoke(CurrentEmployment);
            }

            CurrentEmployment = new EmploymentRecord(job, Clock.CurrentDate, lastPaidDate: priorLastPaid, totalMonthsWorked: priorMonths);

            if (isPromotion)
            {
                OnPromoted?.Invoke(CurrentEmployment, job);
            }
            else
            {
                OnHired?.Invoke(CurrentEmployment);
            }

            return JobApplicationResult.Accepted(job);
        }

        public bool TryApply(string jobId, out string error)
        {
            var result = SubmitApplication(jobId);
            if (result.Success)
            {
                error = null;
                return true;
            }

            error = result.Message;
            return false;
        }

        public bool GetNextPromotion(out JobListing nextJob, out bool isEligible, out string feedback)
        {
            nextJob = null;
            isEligible = false;
            feedback = null;

            if (!IsEmployed)
            {
                feedback = "Must be employed to seek an internal promotion.";
                return false;
            }

            var current = CurrentEmployment.Job;
            nextJob = _availableJobs
                .Where(j => j.Track == current.Track && j.Tier == current.Tier + 1)
                .OrderBy(j => j.MonthlySalary.MinorUnits)
                .FirstOrDefault();

            if (nextJob == null)
            {
                feedback = $"You have reached the pinnacle of the {current.Track} career ladder ({current.Title}).";
                return false;
            }

            isEligible = CanApply(nextJob.Id, out feedback);
            if (isEligible)
            {
                feedback = $"Eligible for promotion to {nextJob.Title} ({nextJob.MonthlySalary}/mo)!";
            }
            return true;
        }

        public bool TryApplyForPromotion(out string error)
        {
            if (!GetNextPromotion(out var nextJob, out bool isEligible, out string feedback))
            {
                error = feedback;
                return false;
            }

            if (!isEligible)
            {
                error = feedback;
                return false;
            }

            return TryApply(nextJob.Id, out error);
        }

        public void ResignCurrentJob()
        {
            if (IsEmployed)
            {
                CurrentEmployment.Resign(Clock.CurrentDate);
                OnResigned?.Invoke(CurrentEmployment);
                CurrentEmployment = null;
            }
        }

        private void CheckSalaryPayout(GameDateTime currentDate)
        {
            if (!IsEmployed) return;

            var job = CurrentEmployment.Job;
            if (currentDate.Day == job.PayDayOfMonth)
            {
                // Check if already paid this month
                if (CurrentEmployment.LastPaidDate.HasValue &&
                    CurrentEmployment.LastPaidDate.Value.Month == currentDate.Month &&
                    CurrentEmployment.LastPaidDate.Value.Year == currentDate.Year)
                {
                    return;
                }

                // Credit salary via FinancialService
                var tx = FinancialService.RecordIncome(
                    job.MonthlySalary,
                    TransactionCategory.EmploymentSalary,
                    $"Monthly Salary: {job.Title} at {job.Company}",
                    job.Id
                );

                CurrentEmployment.RecordSalaryPaid(currentDate);

                // Accumulate track experience
                _trackExperienceMonths[job.Track] = GetTrackExperienceMonths(job.Track) + 1;

                OnSalaryPaid?.Invoke(CurrentEmployment, tx);
            }
        }
    }
}

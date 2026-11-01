using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Service managing education, training courses, higher education degree programs,
    /// qualifications, and skill acquisition.
    /// Pure C# domain service with zero MonoBehaviour dependencies.
    /// </summary>
    public class EducationService
    {
        private readonly List<EducationCourse> _catalog = new List<EducationCourse>();
        private readonly HashSet<string> _earnedQualifications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<SkillType, int> _skills = new Dictionary<SkillType, int>();

        public IReadOnlyList<EducationCourse> Catalog => _catalog;
        public IReadOnlyCollection<string> EarnedQualifications => _earnedQualifications;
        public EducationEnrollment ActiveEnrollment { get; private set; }

        public FinancialService FinancialService { get; }
        public SimulationClock Clock { get; }

        public StudyPace CurrentStudyPace => ActiveEnrollment?.Pace ?? StudyPace.PartTime;

        public event Action<EducationCourse> OnEnrolled;
        public event Action<EducationEnrollment> OnCourseProgressed;
        public event Action<EducationEnrollment> OnCourseCompleted;

        public EducationService(
            FinancialService financialService,
            SimulationClock clock,
            IEnumerable<EducationCourse> initialCatalog = null)
        {
            FinancialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));

            // Initialize default skill values
            foreach (SkillType skill in Enum.GetValues(typeof(SkillType)))
            {
                _skills[skill] = 0;
            }

            if (initialCatalog != null)
            {
                _catalog.AddRange(initialCatalog);
            }
            else
            {
                _catalog.AddRange(GetDefaultCatalog());
            }

            Clock.OnDayPassed += _ => ProcessDailyStudy();
        }

        public static List<EducationCourse> GetDefaultCatalog()
        {
            return new List<EducationCourse>
            {
                // ==================== SHORT WORKSHOPS ====================
                new EducationCourse(
                    "course_free_digital",
                    "Foundations of Digital Skills",
                    "Free online computer and productivity workshop covering email, spreadsheets, and cloud storage.",
                    Currency.Zero,
                    totalStudyHoursRequired: 20,
                    recommendedWeeklyHours: 10,
                    grantedQualification: "qual_digital_basics",
                    skillRewards: new Dictionary<SkillType, int> { { SkillType.Technical, 10 } },
                    type: EducationType.ShortCourse
                ),
                new EducationCourse(
                    "course_retail_sales_bootcamp",
                    "Sales & Customer Engagement Bootcamp",
                    "Essential customer service, negotiation, and retail point-of-sale operations.",
                    Currency.FromDollars(250.00),
                    totalStudyHoursRequired: 30,
                    recommendedWeeklyHours: 10,
                    grantedQualification: "qual_retail_sales",
                    skillRewards: new Dictionary<SkillType, int> { { SkillType.Communication, 15 }, { SkillType.Sales, 20 } },
                    type: EducationType.ShortCourse
                ),

                // ==================== PROFESSIONAL CERTIFICATES ====================
                new EducationCourse(
                    "course_it_support_cert",
                    "IT Support Professional Certificate",
                    "Comprehensive technical troubleshooting, network configuration, hardware assembly, and operating systems.",
                    Currency.FromDollars(500.00),
                    totalStudyHoursRequired: 60,
                    recommendedWeeklyHours: 15,
                    grantedQualification: "qual_it_support_cert",
                    prerequisiteQualification: "qual_digital_basics",
                    skillRewards: new Dictionary<SkillType, int> { { SkillType.Technical, 25 }, { SkillType.Communication, 10 } },
                    type: EducationType.ProfessionalCertificate
                ),

                // ==================== HIGHER EDUCATION DEGREES ====================
                new EducationCourse(
                    "degree_business_diploma",
                    "Diploma in Business Administration",
                    "Accredited multi-month program covering operational management, corporate finance, accounting, and strategic leadership.",
                    Currency.FromDollars(3500.00),
                    totalStudyHoursRequired: 180,
                    recommendedWeeklyHours: 15,
                    grantedQualification: "qual_business_diploma",
                    skillRewards: new Dictionary<SkillType, int>
                    {
                        { SkillType.Management, 35 },
                        { SkillType.Analytics, 25 },
                        { SkillType.Communication, 15 }
                    },
                    type: EducationType.Degree
                ),
                new EducationCourse(
                    "degree_computer_science",
                    "Bachelor of Computer Science",
                    "Four-year equivalent degree curriculum in data structures, algorithms, distributed systems, and software engineering.",
                    Currency.FromDollars(12000.00),
                    totalStudyHoursRequired: 480,
                    recommendedWeeklyHours: 20,
                    grantedQualification: "qual_cs_degree",
                    prerequisiteQualification: "qual_digital_basics",
                    skillRewards: new Dictionary<SkillType, int>
                    {
                        { SkillType.Technical, 70 },
                        { SkillType.Analytics, 50 },
                        { SkillType.Management, 15 }
                    },
                    type: EducationType.Degree
                )
            };
        }

        public IReadOnlyList<EducationCourse> GetCoursesByType(EducationType type)
        {
            return _catalog.Where(c => c.Type == type).ToList();
        }

        public int GetSkillLevel(SkillType skill) => _skills.TryGetValue(skill, out int val) ? val : 0;
        
        public void SetSkillLevel(SkillType skill, int level)
        {
            _skills[skill] = Math.Max(0, level);
        }

        public bool HasQualification(string qual) => !string.IsNullOrEmpty(qual) && _earnedQualifications.Contains(qual);

        public void AddQualification(string qual)
        {
            if (!string.IsNullOrEmpty(qual))
            {
                _earnedQualifications.Add(qual);
            }
        }

        public bool CanEnroll(string courseId, out string reason)
        {
            if (ActiveEnrollment != null && !ActiveEnrollment.IsCompleted)
            {
                reason = $"Already enrolled in '{ActiveEnrollment.Course.Title}'. Complete or drop it first.";
                return false;
            }

            var course = _catalog.FirstOrDefault(c => c.Id == courseId);
            if (course == null)
            {
                reason = $"Course with ID '{courseId}' was not found in catalog.";
                return false;
            }

            if (HasQualification(course.GrantedQualification))
            {
                reason = $"You have already graduated and earned '{course.Title}'.";
                return false;
            }

            if (!string.IsNullOrEmpty(course.PrerequisiteQualification) && !HasQualification(course.PrerequisiteQualification))
            {
                reason = $"Prerequisite '{course.PrerequisiteQualification}' not met.";
                return false;
            }

            if (course.TuitionCost.MinorUnits > FinancialService.Wallet.Balance.MinorUnits)
            {
                reason = $"Insufficient funds for tuition: Requires {course.TuitionCost}, current cash is {FinancialService.Wallet.Balance}.";
                return false;
            }

            reason = null;
            return true;
        }

        public bool TryEnroll(string courseId, out string error)
        {
            return TryEnroll(courseId, StudyPace.PartTime, out error);
        }

        public bool TryEnroll(string courseId, StudyPace pace, out string error)
        {
            if (!CanEnroll(courseId, out error))
            {
                return false;
            }

            var course = _catalog.First(c => c.Id == courseId);

            // Tuition deduction
            if (course.TuitionCost.MinorUnits > 0)
            {
                bool paid = FinancialService.TryRecordExpense(
                    course.TuitionCost,
                    TransactionCategory.EducationTuition,
                    $"Tuition: {course.Title}",
                    out _,
                    out error,
                    course.Id
                );

                if (!paid)
                {
                    return false;
                }
            }

            ActiveEnrollment = new EducationEnrollment(course, Clock.CurrentDate, pace: pace);
            error = null;
            OnEnrolled?.Invoke(course);
            return true;
        }

        public void SetStudyPace(StudyPace pace)
        {
            if (ActiveEnrollment != null)
            {
                ActiveEnrollment.SetPace(pace);
            }
        }

        public void DropActiveEnrollment()
        {
            ActiveEnrollment = null;
        }

        public void ProcessDailyStudy(double? explicitHours = null)
        {
            if (ActiveEnrollment == null || ActiveEnrollment.IsCompleted) return;

            double hours = explicitHours ?? (double)ActiveEnrollment.Pace;
            bool justFinished = ActiveEnrollment.AddStudyHours(hours, Clock.CurrentDate);
            OnCourseProgressed?.Invoke(ActiveEnrollment);

            if (justFinished)
            {
                // Award qualification
                if (!string.IsNullOrEmpty(ActiveEnrollment.Course.GrantedQualification))
                {
                    _earnedQualifications.Add(ActiveEnrollment.Course.GrantedQualification);
                }

                // Award skills
                foreach (var reward in ActiveEnrollment.Course.SkillRewards)
                {
                    _skills[reward.Key] = GetSkillLevel(reward.Key) + reward.Value;
                }

                OnCourseCompleted?.Invoke(ActiveEnrollment);
            }
        }
    }
}

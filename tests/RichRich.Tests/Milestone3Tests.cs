using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;
using RichRich.Domain.Services;
using RichRich.Presentation.Controllers;

namespace RichRich.Tests
{
    [TestFixture]
    public class Milestone3Tests
    {
        private string _testDir;
        private SaveDataService _saveService;
        private LifeSimulationCoordinator _coordinator;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "RichRich_M3_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            _saveService = new SaveDataService(_testDir);
            _coordinator = new LifeSimulationCoordinator(_saveService);
            _coordinator.InitializeSession(GameDifficulty.Standard); // Starts with $3,000.00
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }

        #region 1. Interactive Job Board & Career Ladders

        [Test]
        public void JobBoard_Filtering_ReturnsExpectedTracks()
        {
            var controller = new JobBoardController(_coordinator);

            // Default: All jobs
            Assert.That(controller.FilteredJobs.Count, Is.GreaterThanOrEqualTo(10));

            // Retail filter
            controller.SetTrackFilter(CareerTrack.Retail);
            Assert.That(controller.FilteredJobs.All(j => j.Track == CareerTrack.Retail), Is.True);
            Assert.That(controller.FilteredJobs.Any(j => j.Id == "job_retail_assistant"), Is.True);
            Assert.That(controller.FilteredJobs.Any(j => j.Id == "job_retail_store_manager"), Is.True);

            // Tech filter
            controller.SetTrackFilter(CareerTrack.Technology);
            Assert.That(controller.FilteredJobs.All(j => j.Track == CareerTrack.Technology), Is.True);
            Assert.That(controller.FilteredJobs.Any(j => j.Id == "job_it_helpdesk"), Is.True);
            Assert.That(controller.FilteredJobs.Any(j => j.Id == "job_tech_manager"), Is.True);

            // Business filter
            controller.SetTrackFilter(CareerTrack.Business);
            Assert.That(controller.FilteredJobs.All(j => j.Track == CareerTrack.Business), Is.True);
            Assert.That(controller.FilteredJobs.Any(j => j.Id == "job_office_assistant"), Is.True);
            Assert.That(controller.FilteredJobs.Any(j => j.Id == "job_consultant"), Is.True);
        }

        [Test]
        public void JobApplication_MissingPrerequisites_ReturnsRejected()
        {
            var controller = new JobBoardController(_coordinator);

            // Attempt to apply for Technology Manager (Tier 6) with zero experience and no degree
            controller.SelectJob("job_tech_manager");
            bool canApply = controller.CanApplySelected(out string reason);

            Assert.That(canApply, Is.False);
            Assert.That(reason, Does.Contain("Missing required qualification: qual_cs_degree"));

            var result = controller.ApplyForSelected();
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(JobApplicationStatus.RejectedMissingQualification));
            Assert.That(_coordinator.CareerService.IsEmployed, Is.False);
        }

        [Test]
        public void JobApplication_MeetsRequirements_ReturnsAccepted()
        {
            var controller = new JobBoardController(_coordinator);

            controller.SelectJob("job_retail_assistant");
            bool canApply = controller.CanApplySelected(out string reason);
            Assert.That(canApply, Is.True);
            Assert.That(reason, Is.Null);

            var result = controller.ApplyForSelected();
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(JobApplicationStatus.Offered));
            Assert.That(_coordinator.CareerService.IsEmployed, Is.True);
            Assert.That(_coordinator.CareerService.CurrentEmployment.Job.Id, Is.EqualTo("job_retail_assistant"));
            Assert.That(_coordinator.CareerService.CurrentMonthlySalary, Is.EqualTo(Currency.FromDollars(1600.00)));
        }

        [Test]
        public void JobSwitching_PreservesWorkTenureAndPreventsDuplicateSalary()
        {
            // Employ in retail
            _coordinator.CareerService.TryApply("job_retail_assistant", out _);

            // Advance to payday (Day 30)
            _coordinator.FastForwardDays(29);
            Assert.That(_coordinator.Clock.CurrentDate.Day, Is.EqualTo(30));

            long cashAfterRetailSalary = _coordinator.Wallet.Balance.MinorUnits;

            // Switch to courier on the same day
            bool switched = _coordinator.CareerService.TryApply("job_delivery_worker", out string err);
            Assert.That(switched, Is.True, err);
            Assert.That(_coordinator.CareerService.CurrentEmployment.Job.Id, Is.EqualTo("job_delivery_worker"));

            // Total months worked carries over
            Assert.That(_coordinator.CareerService.CurrentEmployment.TotalMonthsWorked, Is.EqualTo(1));

            // Verify cash did not receive second salary
            Assert.That(_coordinator.Wallet.Balance.MinorUnits, Is.EqualTo(cashAfterRetailSalary));
        }

        [Test]
        public void CareerProgression_AccumulatesTrackExperienceAndUnlocksPromotion()
        {
            var career = _coordinator.CareerService;

            // Start as Retail Assistant (Tier 1)
            career.TryApply("job_retail_assistant", out _);
            Assert.That(career.CurrentEmployment.Job.Tier, Is.EqualTo(1));

            // Give required skills for Senior Retail Assistant (Sales >= 15, Comm >= 10)
            _coordinator.EducationService.SetSkillLevel(SkillType.Sales, 20);
            _coordinator.EducationService.SetSkillLevel(SkillType.Communication, 15);

            // Cannot promote yet (0 retail experience months)
            bool eligible = career.GetNextPromotion(out var nextJob, out bool canPromote, out string feedback);
            Assert.That(eligible, Is.True);
            Assert.That(nextJob.Id, Is.EqualTo("job_retail_senior"));
            Assert.That(canPromote, Is.False);
            Assert.That(feedback, Does.Contain("Insufficient total experience"));

            // Simulate working 6 months in retail
            for (int i = 0; i < 6; i++)
            {
                _coordinator.FastForwardDays(30);
            }

            Assert.That(career.GetTrackExperienceMonths(CareerTrack.Retail), Is.EqualTo(6));

            // Now promotion should be eligible
            eligible = career.GetNextPromotion(out nextJob, out canPromote, out feedback);
            Assert.That(eligible, Is.True);
            Assert.That(canPromote, Is.True);

            // Apply for promotion
            bool promoted = career.TryApplyForPromotion(out string promoErr);
            Assert.That(promoted, Is.True, promoErr);
            Assert.That(career.CurrentEmployment.Job.Id, Is.EqualTo("job_retail_senior"));
            Assert.That(career.CurrentEmployment.Job.Tier, Is.EqualTo(2));
            Assert.That(career.CurrentMonthlySalary, Is.EqualTo(Currency.FromDollars(2300.00)));
        }

        [Test]
        public void CareerProgression_PinnacleRole_DetectsMaxTier()
        {
            var career = _coordinator.CareerService;

            // Direct set to Technology Manager (Tier 6)
            _coordinator.EducationService.AddQualification("qual_cs_degree");
            _coordinator.EducationService.SetSkillLevel(SkillType.Technical, 90);
            _coordinator.EducationService.SetSkillLevel(SkillType.Management, 60);
            _coordinator.EducationService.SetSkillLevel(SkillType.Analytics, 60);
            career.SetTrackExperience(CareerTrack.Technology, 50);

            career.TryApply("job_tech_manager", out _);
            Assert.That(career.CurrentEmployment.Job.Tier, Is.EqualTo(6));

            bool hasNext = career.GetNextPromotion(out var nextJob, out bool canPromote, out string feedback);
            Assert.That(hasNext, Is.False);
            Assert.That(nextJob, Is.Null);
            Assert.That(feedback, Does.Contain("pinnacle of the Technology career ladder"));
        }

        #endregion

        #region 2. Higher Education & University Degrees

        [Test]
        public void HigherEducation_UniversityEnrolment_DeductsTuitionFromCash()
        {
            // Give player sufficient funds for Diploma of Business ($3,500)
            _coordinator.Wallet.Add(Currency.FromDollars(2000.00));
            Currency cashBefore = _coordinator.Wallet.Balance;

            bool enrolled = _coordinator.EducationService.TryEnroll("degree_business_diploma", StudyPace.PartTime, out string err);
            Assert.That(enrolled, Is.True, err);

            var active = _coordinator.EducationService.ActiveEnrollment;
            Assert.That(active, Is.Not.Null);
            Assert.That(active.Course.Id, Is.EqualTo("degree_business_diploma"));
            Assert.That(active.Course.Type, Is.EqualTo(EducationType.Degree));
            Assert.That(active.Pace, Is.EqualTo(StudyPace.PartTime));

            // Tuition deduction verified
            Currency expectedCash = cashBefore - Currency.FromDollars(3500.00);
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(expectedCash));

            // Ledger record verified
            var tx = _coordinator.Ledger.Transactions.Last();
            Assert.That(tx.Category, Is.EqualTo(TransactionCategory.EducationTuition));
            Assert.That(tx.Amount, Is.EqualTo(Currency.FromDollars(3500.00)));
        }

        [Test]
        public void HigherEducation_InsufficientTuition_RejectsEnrolment()
        {
            // Satisfy prerequisite but have insufficient tuition ($12,000 required vs $3,000 cash)
            _coordinator.EducationService.AddQualification("qual_digital_basics");

            bool enrolled = _coordinator.EducationService.TryEnroll("degree_computer_science", out string err);
            Assert.That(enrolled, Is.False);
            Assert.That(err, Does.Contain("Insufficient funds for tuition"));
            Assert.That(_coordinator.EducationService.ActiveEnrollment, Is.Null);
        }

        [Test]
        public void HigherEducation_StudyProgression_PartTimeVsFullTime()
        {
            // Enroll in free course (20 hours total)
            _coordinator.EducationService.TryEnroll("course_free_digital", StudyPace.PartTime, out _);
            Assert.That(_coordinator.EducationService.ActiveEnrollment.HoursCompleted, Is.EqualTo(0.0));

            // Advance 1 day on PartTime (2 hrs/day)
            _coordinator.FastForwardDays(1);
            Assert.That(_coordinator.EducationService.ActiveEnrollment.HoursCompleted, Is.EqualTo(2.0));

            // Switch to FullTime (6 hrs/day)
            _coordinator.EducationService.SetStudyPace(StudyPace.FullTime);
            _coordinator.FastForwardDays(1);
            Assert.That(_coordinator.EducationService.ActiveEnrollment.HoursCompleted, Is.EqualTo(8.0)); // 2 + 6
        }

        [Test]
        public void HigherEducation_DegreeCompletion_AwardsCredentialAndSkillPoints()
        {
            // Give tuition
            _coordinator.Wallet.Add(Currency.FromDollars(5000.00));

            // Enroll in Diploma of Business Administration (180 hrs)
            _coordinator.EducationService.TryEnroll("degree_business_diploma", StudyPace.FullTime, out _);

            int initMgmt = _coordinator.EducationService.GetSkillLevel(SkillType.Management);
            int initAnalytics = _coordinator.EducationService.GetSkillLevel(SkillType.Analytics);

            // Advance 30 days at FullTime (6 hrs/day * 30 days = 180 hours)
            _coordinator.FastForwardDays(30);

            Assert.That(_coordinator.EducationService.ActiveEnrollment.IsCompleted, Is.True);
            Assert.That(_coordinator.EducationService.HasQualification("qual_business_diploma"), Is.True);
            Assert.That(_coordinator.EducationService.GetSkillLevel(SkillType.Management), Is.EqualTo(initMgmt + 35));
            Assert.That(_coordinator.EducationService.GetSkillLevel(SkillType.Analytics), Is.EqualTo(initAnalytics + 25));
        }

        [Test]
        public void WorkAndStudy_SimultaneousProgression_BothSucceed()
        {
            // Hire into delivery courier ($1,800/mo)
            _coordinator.CareerService.TryApply("job_delivery_worker", out _);

            // Enroll in digital skills course
            _coordinator.EducationService.TryEnroll("course_free_digital", StudyPace.PartTime, out _);

            // Advance 10 days (20 hours study)
            _coordinator.FastForwardDays(10);

            // Course completed
            Assert.That(_coordinator.EducationService.ActiveEnrollment.IsCompleted, Is.True);
            Assert.That(_coordinator.EducationService.HasQualification("qual_digital_basics"), Is.True);

            // Advance to Day 30
            _coordinator.FastForwardDays(19);
            Assert.That(_coordinator.Clock.CurrentDate.Day, Is.EqualTo(30));

            // Salary received
            Assert.That(_coordinator.CareerService.CurrentEmployment.TotalMonthsWorked, Is.EqualTo(1));
            Assert.That(_coordinator.CareerService.CurrentEmployment.LastPaidDate.HasValue, Is.True);
        }

        #endregion

        #region 3. Housing and Lifestyle Upgrades

        [Test]
        public void Housing_CatalogBrowse_ContainsAllFiveTiers()
        {
            var housing = _coordinator.HousingService;
            Assert.That(housing.Catalog.Count, Is.EqualTo(5));

            Assert.That(housing.Catalog[0].Id, Is.EqualTo("housing_shared_room"));
            Assert.That(housing.Catalog[0].MonthlyRent, Is.EqualTo(Currency.FromDollars(300.00)));

            Assert.That(housing.Catalog[1].Id, Is.EqualTo("housing_studio"));
            Assert.That(housing.Catalog[1].MonthlyRent, Is.EqualTo(Currency.FromDollars(600.00)));

            Assert.That(housing.Catalog[2].Id, Is.EqualTo("housing_one_bedroom"));
            Assert.That(housing.Catalog[2].MonthlyRent, Is.EqualTo(Currency.FromDollars(1100.00)));

            Assert.That(housing.Catalog[3].Id, Is.EqualTo("housing_luxury"));
            Assert.That(housing.Catalog[3].MonthlyRent, Is.EqualTo(Currency.FromDollars(2500.00)));

            Assert.That(housing.Catalog[4].Id, Is.EqualTo("housing_penthouse"));
            Assert.That(housing.Catalog[4].MonthlyRent, Is.EqualTo(Currency.FromDollars(5500.00)));
        }

        [Test]
        public void Housing_MoveToNewTier_DeductsDepositAndUpdatesMonthlyRent()
        {
            var housing = _coordinator.HousingService;
            var rentExpense = _coordinator.ExpenseService.Expenses.First(e => e.Category == TransactionCategory.LivingAccommodation);

            // Default is Studio ($600/mo)
            Assert.That(housing.CurrentHousing.Id, Is.EqualTo("housing_studio"));
            Assert.That(rentExpense.MonthlyAmount, Is.EqualTo(Currency.FromDollars(600.00)));

            // Downsize to Shared Room ($300/mo, deposit $300)
            Currency cashBefore = _coordinator.Wallet.Balance;
            bool moved = housing.TryMoveTo("housing_shared_room", out string err);
            Assert.That(moved, Is.True, err);

            Assert.That(housing.CurrentHousing.Id, Is.EqualTo("housing_shared_room"));
            Assert.That(housing.HousingDepositPaid, Is.EqualTo(Currency.FromDollars(300.00)));

            // Deposit deducted
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBefore - Currency.FromDollars(300.00)));

            // Rent updated in LivingExpenseService
            Assert.That(rentExpense.MonthlyAmount, Is.EqualTo(Currency.FromDollars(300.00)));
        }

        [Test]
        public void Housing_MidMonthMove_PreventsDuplicateRentDeduction()
        {
            var housing = _coordinator.HousingService;
            var expenseService = _coordinator.ExpenseService;

            // Month 1 Day 1: Studio rent ($600) was already deducted
            expenseService.CheckScheduledExpenses(_coordinator.Clock.CurrentDate);
            var rentExp = expenseService.Expenses.First(e => e.Category == TransactionCategory.LivingAccommodation);
            Assert.That(rentExp.LastPaidDate.HasValue, Is.True);
            Assert.That(rentExp.LastPaidDate.Value.Month, Is.EqualTo(1));

            long cashAfterRent = _coordinator.Wallet.Balance.MinorUnits;

            // Move on Day 15 to One-Bedroom (rent $1,100, deposit $1,100)
            _coordinator.FastForwardDays(14);
            Assert.That(_coordinator.Clock.CurrentDate.Day, Is.EqualTo(15));

            // Qualify with IT job ($2,800/mo salary > $2,500 req)
            _coordinator.EducationService.AddQualification("qual_it_support_cert");
            _coordinator.EducationService.SetSkillLevel(SkillType.Technical, 25);
            _coordinator.CareerService.TryApply("job_it_helpdesk", out _);
            _coordinator.Wallet.Add(Currency.FromDollars(5000.00));

            Currency preMoveCash = _coordinator.Wallet.Balance;
            bool moved = housing.TryMoveTo("housing_one_bedroom", out string err);
            Assert.That(moved, Is.True, err);

            // Deducted deposit ($1,100), but NOT new rent ($1,100)
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(preMoveCash - Currency.FromDollars(1100.00)));

            // Advance through rest of Month 1 (Day 16 to Day 30): Zero additional rent deductions
            _coordinator.FastForwardDays(15);
            Assert.That(_coordinator.Clock.CurrentDate.Day, Is.EqualTo(30));

            // Advance to Month 2 Day 1: One-Bedroom rent ($1,100) deducted once!
            _coordinator.FastForwardDays(1);
            Assert.That(_coordinator.Clock.CurrentDate.Day, Is.EqualTo(1));
            Assert.That(_coordinator.Clock.CurrentDate.Month, Is.EqualTo(2));

            // Total accommodation rent charges in ledger: exactly 2 entries (Month 1 Studio $600 + Month 2 1-Bed $1,100)
            var rentTxs = _coordinator.Ledger.Transactions
                .Where(t => t.Category == TransactionCategory.LivingAccommodation && t.Description.Contains("Monthly Apartment Rent"))
                .ToList();

            Assert.That(rentTxs.Count, Is.EqualTo(2));
            Assert.That(rentTxs[0].Amount, Is.EqualTo(Currency.FromDollars(600.00)));
            Assert.That(rentTxs[1].Amount, Is.EqualTo(Currency.FromDollars(1100.00)));
        }

        [Test]
        public void Housing_LuxuryIncomeVerification_EnforcesTenantStandards()
        {
            var housing = _coordinator.HousingService;

            // Attempt to move to Luxury Apartment ($6,000/mo min salary req) while unemployed with only $3,000 cash
            bool canMove = housing.CanMoveTo("housing_luxury", out string reason);
            Assert.That(canMove, Is.False);
            Assert.That(reason, Does.Contain("Landlord qualification requirement not met"));

            // Give player $30,000 in savings (satisfies min savings requirement of $25,000)
            _coordinator.Wallet.Add(Currency.FromDollars(30000.00));
            canMove = housing.CanMoveTo("housing_luxury", out reason);
            Assert.That(canMove, Is.True, reason);

            bool moved = housing.TryMoveTo("housing_luxury", out string moveErr);
            Assert.That(moved, Is.True, moveErr);
            Assert.That(housing.CurrentHousing.Id, Is.EqualTo("housing_luxury"));
            Assert.That(housing.CurrentHousing.ComfortRating, Is.EqualTo(90));
        }

        #endregion

        #region 4. Energy, Fatigue and Rest

        [Test]
        public void LifestyleEnergy_WorkAndStudy_ConsumesDailyEnergy()
        {
            var lifestyle = _coordinator.LifestyleService;
            Assert.That(lifestyle.CurrentEnergy, Is.EqualTo(100));

            // Employ in 40h job (consumes 25 energy)
            _coordinator.CareerService.TryApply("job_delivery_worker", out _);

            // Manual consumption test
            lifestyle.ConsumeEnergy(25);
            Assert.That(lifestyle.CurrentEnergy, Is.EqualTo(75));
            Assert.That(lifestyle.FatigueState, Is.EqualTo(FatigueLevel.WellRested));

            // Additional study consumption (30 energy)
            lifestyle.ConsumeEnergy(30);
            Assert.That(lifestyle.CurrentEnergy, Is.EqualTo(45));
            Assert.That(lifestyle.FatigueState, Is.EqualTo(FatigueLevel.Normal));
        }

        [Test]
        public void LifestyleEnergy_FatigueStates_TransitionCleanly()
        {
            var lifestyle = _coordinator.LifestyleService;

            lifestyle.RestoreSavedEnergy(80);
            Assert.That(lifestyle.FatigueState, Is.EqualTo(FatigueLevel.WellRested));

            lifestyle.RestoreSavedEnergy(50);
            Assert.That(lifestyle.FatigueState, Is.EqualTo(FatigueLevel.Normal));

            lifestyle.RestoreSavedEnergy(25);
            Assert.That(lifestyle.FatigueState, Is.EqualTo(FatigueLevel.Fatigued));

            lifestyle.RestoreSavedEnergy(5);
            Assert.That(lifestyle.FatigueState, Is.EqualTo(FatigueLevel.Exhausted));
        }

        [Test]
        public void LifestyleEnergy_RestAction_RestoresEnergy()
        {
            var lifestyle = _coordinator.LifestyleService;
            lifestyle.RestoreSavedEnergy(30);

            lifestyle.TakeRest(20);
            Assert.That(lifestyle.CurrentEnergy, Is.EqualTo(50));
        }

        [Test]
        public void LifestyleEnergy_HousingComfort_InfluencesOvernightRecovery()
        {
            var lifestyle = _coordinator.LifestyleService;
            var housing = _coordinator.HousingService;

            // Shared room (+50 recovery)
            housing.TryMoveTo("housing_shared_room", out _);
            lifestyle.RestoreSavedEnergy(10);
            lifestyle.ProcessSleepRecovery();
            Assert.That(lifestyle.CurrentEnergy, Is.EqualTo(60)); // 10 + 50

            // Luxury room (+100 recovery)
            _coordinator.Wallet.Add(Currency.FromDollars(30000.00));
            housing.TryMoveTo("housing_luxury", out _);
            lifestyle.RestoreSavedEnergy(10);
            lifestyle.ProcessSleepRecovery();
            Assert.That(lifestyle.CurrentEnergy, Is.EqualTo(100)); // 10 + 100 capped at 100
        }

        #endregion

        #region 5. Persistence & Schema Migration

        [Test]
        public void Persistence_SchemaV3_SavesAndRestoresAllNewSystems()
        {
            // Setup rich state
            _coordinator.Wallet.Add(Currency.FromDollars(10000.00));
            _coordinator.CareerService.TryApply("job_delivery_worker", out _);
            _coordinator.CareerService.SetTrackExperience(CareerTrack.Retail, 12);
            _coordinator.CareerService.SetTrackExperience(CareerTrack.Technology, 6);
            _coordinator.CareerService.SetTrackExperience(CareerTrack.Business, 3);

            _coordinator.HousingService.TryMoveTo("housing_one_bedroom", out _);
            _coordinator.EducationService.TryEnroll("degree_business_diploma", StudyPace.FullTime, out _);
            _coordinator.EducationService.ActiveEnrollment.AddStudyHours(45.0, _coordinator.Clock.CurrentDate);
            _coordinator.LifestyleService.RestoreSavedEnergy(65);

            // Save state
            var saved = _coordinator.SaveCurrentState();
            Assert.That(saved.SchemaVersion, Is.EqualTo(3));
            Assert.That(saved.CurrentHousingTierId, Is.EqualTo("housing_one_bedroom"));
            Assert.That(saved.StudyPace, Is.EqualTo("FullTime"));
            Assert.That(saved.RetailExperienceMonths, Is.EqualTo(12));
            Assert.That(saved.TechExperienceMonths, Is.EqualTo(6));
            Assert.That(saved.BusinessExperienceMonths, Is.EqualTo(3));
            Assert.That(saved.CurrentEnergy, Is.EqualTo(65));

            // Reload into fresh coordinator
            var reloadedCoordinator = new LifeSimulationCoordinator(_saveService);
            reloadedCoordinator.InitializeSession();

            Assert.That(reloadedCoordinator.HousingService.CurrentHousing.Id, Is.EqualTo("housing_one_bedroom"));
            Assert.That(reloadedCoordinator.EducationService.ActiveEnrollment.Course.Id, Is.EqualTo("degree_business_diploma"));
            Assert.That(reloadedCoordinator.EducationService.CurrentStudyPace, Is.EqualTo(StudyPace.FullTime));
            Assert.That(reloadedCoordinator.EducationService.ActiveEnrollment.HoursCompleted, Is.EqualTo(45.0));
            Assert.That(reloadedCoordinator.CareerService.GetTrackExperienceMonths(CareerTrack.Retail), Is.EqualTo(12));
            Assert.That(reloadedCoordinator.CareerService.GetTrackExperienceMonths(CareerTrack.Technology), Is.EqualTo(6));
            Assert.That(reloadedCoordinator.CareerService.GetTrackExperienceMonths(CareerTrack.Business), Is.EqualTo(3));
            Assert.That(reloadedCoordinator.LifestyleService.CurrentEnergy, Is.EqualTo(65));
        }

        [Test]
        public void Persistence_SchemaV2_MigrationToV3_SetsSafeDefaults()
        {
            // Create legacy schema v2 save data
            var legacyV2 = new SaveData
            {
                SchemaVersion = 2,
                CreatedAtUnix = 1700000000L,
                LastSavedUnix = 1700000100L,
                Difficulty = "Standard",
                BalanceCents = 250000L, // $2,500.00
                SimulationMinutes = 1440L,
                LastProcessedSimulationMinute = 1440L,
                CurrentJobId = "job_retail_assistant",
                TotalMonthsWorked = 2,
                // Omitted M3 fields (null/default)
                CurrentHousingTierId = null,
                HousingDepositPaidCents = 0L,
                CurrentEnergy = 0,
                StudyPace = null
            };

            _saveService.Save(legacyV2);

            // Initialize coordinator from legacy save
            var migrated = new LifeSimulationCoordinator(_saveService);
            migrated.InitializeSession();

            // Defaults correctly applied
            Assert.That(migrated.HousingService.CurrentHousing.Id, Is.EqualTo("housing_studio"));
            Assert.That(migrated.LifestyleService.CurrentEnergy, Is.EqualTo(100));
            Assert.That(migrated.EducationService.CurrentStudyPace, Is.EqualTo(StudyPace.PartTime));
            Assert.That(migrated.CareerService.CurrentEmployment.Job.Id, Is.EqualTo("job_retail_assistant"));
        }

        #endregion
    }
}

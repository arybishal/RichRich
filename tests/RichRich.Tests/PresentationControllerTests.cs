using System;
using System.IO;
using NUnit.Framework;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;
using RichRich.Domain.Services;
using RichRich.Presentation.Controllers;

namespace RichRich.Tests
{
    [TestFixture]
    public class PresentationControllerTests
    {
        private string _testDir;
        private SaveDataService _saveService;
        private MockTimeProvider _timeProvider;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "RichRich_PresTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            _saveService = new SaveDataService(_testDir);
            _timeProvider = new MockTimeProvider(new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }

        [Test]
        public void BusinessTaskController_CannotStartIfBusinessNotOwned()
        {
            var biz = new Business("auto_repair", "Auto Repair", Currency.FromDollars(5499.00), Currency.FromDollars(120.00), isOwned: false);
            var task = new BusinessTask("task1", "Engine Diagnostics", 5.0, Currency.FromDollars(150.00));
            var wallet = new Wallet();
            var controller = new BusinessTaskController(biz, task, wallet);

            Assert.That(controller.CanStartTask, Is.False);
            Assert.That(controller.StatusButtonText, Does.Contain("Requires Auto Repair Shop"));

            bool started = controller.TryStartTask(out string error);
            Assert.That(started, Is.False);
            Assert.That(error, Does.Contain("must first purchase"));
        }

        [Test]
        public void BusinessTaskController_Lifecycle_StartProgressClaim()
        {
            var biz = new Business("auto_repair", "Auto Repair", Currency.FromDollars(5499.00), Currency.FromDollars(120.00), isOwned: true);
            var task = new BusinessTask("task1", "Engine Diagnostics", 4.0, Currency.FromDollars(150.00));
            var wallet = new Wallet();
            var controller = new BusinessTaskController(biz, task, wallet);

            Assert.That(controller.CanStartTask, Is.True);
            Assert.That(controller.StatusButtonText, Is.EqualTo("Start Diagnostics ($150.00)"));

            // 1. Start task
            bool started = controller.TryStartTask(out string error);
            Assert.That(started, Is.True);
            Assert.That(controller.State, Is.EqualTo(BusinessTaskState.Running));
            Assert.That(controller.CanCancelTask, Is.True);

            // 2. Tick half way
            controller.Tick(2.0);
            Assert.That(controller.Progress, Is.EqualTo(0.5f));
            Assert.That(controller.TimeRemainingSeconds, Is.EqualTo(2.0));
            Assert.That(controller.TimeRemainingFormatted, Is.EqualTo("2.0s"));
            Assert.That(controller.StatusButtonText, Does.Contain("Servicing... (2.0s)"));

            // 3. Complete task
            controller.Tick(2.0);
            Assert.That(controller.Progress, Is.EqualTo(1.0f));
            Assert.That(controller.CanClaimReward, Is.True);
            Assert.That(controller.StatusButtonText, Is.EqualTo("Claim Reward ($150.00)"));

            // 4. Claim reward
            bool claimed = controller.TryClaimReward(out Currency reward, out string claimError);
            Assert.That(claimed, Is.True);
            Assert.That(reward, Is.EqualTo(Currency.FromDollars(150.00)));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(150.00)));
            Assert.That(controller.State, Is.EqualTo(BusinessTaskState.Idle));

            // 5. Subsequent claim must fail
            bool doubleClaim = controller.TryClaimReward(out _, out string secondError);
            Assert.That(doubleClaim, Is.False);
        }

        [Test]
        public void BusinessTaskController_Cancel_ResetsToIdle()
        {
            var biz = new Business("auto_repair", "Auto Repair", Currency.FromDollars(5499.00), Currency.FromDollars(120.00), isOwned: true);
            var task = new BusinessTask("task1", "Engine Diagnostics", 4.0, Currency.FromDollars(150.00));
            var wallet = new Wallet();
            var controller = new BusinessTaskController(biz, task, wallet);

            controller.TryStartTask(out _);
            controller.Tick(1.5);
            Assert.That(controller.State, Is.EqualTo(BusinessTaskState.Running));

            bool cancelled = controller.TryCancelTask(out string error);
            Assert.That(cancelled, Is.True);
            Assert.That(controller.State, Is.EqualTo(BusinessTaskState.Cancelled));
            Assert.That(controller.Progress, Is.EqualTo(0.0f));
        }

        [Test]
        public void MainHUDController_TracksBalanceAndTapping()
        {
            var wallet = new Wallet();
            var biz = new Business("auto_repair", "Auto Repair", EconomyConfig.StarterBusinessBaseCost, EconomyConfig.StarterBusinessBaseHourlyYield);
            var task = new BusinessTask("task1", "Task", 5.0, Currency.FromDollars(50.00));
            var economy = new EconomyService(wallet, biz, task, _timeProvider);
            var hud = new MainHUDController(economy);

            Assert.That(hud.BalanceText, Is.EqualTo("$0.00"));
            Assert.That(hud.PassiveRateText, Is.EqualTo("+$0.00/hr"));
            Assert.That(hud.LevelBadgeText, Is.EqualTo("Level 1"));
            Assert.That(hud.TapYieldText, Is.EqualTo("+$4.00"));

            // Tap
            var earned = hud.HandleTap();
            Assert.That(earned, Is.EqualTo(Currency.FromDollars(4.00)));
            Assert.That(hud.BalanceText, Is.EqualTo("$4.00"));

            // Booster
            hud.ActivateBooster();
            Assert.That(hud.IsBoosterActive, Is.True);
            Assert.That(hud.TapYieldText, Is.EqualTo("+$24.00"));
            Assert.That(hud.BoosterStatusText, Does.Contain("6x BOOST ACTIVE"));

            // Tap with booster
            var boostedEarned = hud.HandleTap();
            Assert.That(boostedEarned, Is.EqualTo(Currency.FromDollars(24.00)));
            Assert.That(hud.BalanceText, Is.EqualTo("$28.00"));
        }

        [Test]
        public void BusinessViewController_HandlesAcquireAndUpgrades()
        {
            var wallet = new Wallet();
            var biz = new Business(EconomyConfig.StarterBusinessId, EconomyConfig.StarterBusinessName, Currency.FromDollars(100.00), Currency.FromDollars(10.00));
            var task = new BusinessTask("task1", "Task", 5.0, Currency.FromDollars(10.00));
            var economy = new EconomyService(wallet, biz, task, _timeProvider);
            var controller = new BusinessViewController(biz, wallet, economy);

            Assert.That(controller.IsOwned, Is.False);
            Assert.That(controller.CanAffordAction, Is.False);
            Assert.That(controller.ActionButtonText, Is.EqualTo("Acquire ($100.00)"));

            // Attempt acquire with no money
            bool failedAcquire = controller.TryExecuteAction(out string err);
            Assert.That(failedAcquire, Is.False);
            Assert.That(err, Does.Contain("Insufficient funds"));

            // Add money and acquire
            wallet.Add(Currency.FromDollars(100.00));
            Assert.That(controller.CanAffordAction, Is.True);

            bool successAcquire = controller.TryExecuteAction(out string err2);
            Assert.That(successAcquire, Is.True);
            Assert.That(controller.IsOwned, Is.True);
            Assert.That(controller.Level, Is.EqualTo(1));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.Zero));

            // Upgrade cost Lv1 -> Lv2 = $100.00
            Assert.That(controller.ActionButtonText, Is.EqualTo("Upgrade to Lv. 2 ($100.00)"));
            Assert.That(controller.CanAffordAction, Is.False);

            // Add $100.00 and upgrade
            wallet.Add(Currency.FromDollars(100.00));
            Assert.That(controller.CanAffordAction, Is.True);
            bool successUpgrade = controller.TryExecuteAction(out _);
            Assert.That(successUpgrade, Is.True);
            Assert.That(controller.Level, Is.EqualTo(2));
            Assert.That(controller.HourlyIncomeText, Is.EqualTo("+$12.50/hr")); // $10 base + 25% = $12.50
        }

        [Test]
        public void OfflineEarningsModalController_CalculatesAndClaimsIncome()
        {
            var wallet = new Wallet();
            var result = new OfflineIncomeResult(Currency.FromDollars(240.00), 7200L, wasCapped: false, isClockTampered: false);
            var controller = new OfflineEarningsModalController(result, wallet);

            Assert.That(controller.HasClaimableEarnings, Is.True);
            Assert.That(controller.AmountEarnedText, Is.EqualTo("$240.00"));
            Assert.That(controller.DurationAwayFormatted, Is.EqualTo("2h 0m"));
            Assert.That(controller.SubtitleText, Is.EqualTo("Away for 2h 0m"));

            bool claimed = controller.TryClaim(out Currency earned, out string error);
            Assert.That(claimed, Is.True);
            Assert.That(earned, Is.EqualTo(Currency.FromDollars(240.00)));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(240.00)));

            // Duplicate claim rejected
            bool doubleClaim = controller.TryClaim(out _, out string error2);
            Assert.That(doubleClaim, Is.False);
            Assert.That(error2, Does.Contain("already been claimed"));
        }

        [Test]
        public void GameSessionCoordinator_CompleteGameplayFlow_PersistsAndRestores()
        {
            // 1. Initial Session: fresh startup
            var coordinator1 = new GameSessionCoordinator(_saveService, _timeProvider);
            coordinator1.InitializeSession();

            Assert.That(coordinator1.Wallet.Balance, Is.EqualTo(GameStartConfig.GetStartingCash(GameDifficulty.Standard)));
            Assert.That(coordinator1.AutoRepairShop.IsOwned, Is.False);
            Assert.That(coordinator1.HUDController.LevelBadgeText, Is.EqualTo("Level 1"));

            // 2. Tap to earn money ($400.00)
            for (int i = 0; i < 100; i++)
            {
                coordinator1.HUDController.HandleTap();
            }
            Assert.That(coordinator1.Wallet.Balance, Is.EqualTo(Currency.FromDollars(3400.00)));

            // Add remaining funds to acquire starter auto repair shop ($5,499.00)
            coordinator1.Wallet.Add(Currency.FromDollars(2099.00));
            Assert.That(coordinator1.Wallet.Balance, Is.EqualTo(Currency.FromDollars(5499.00)));

            bool acquired = coordinator1.BusinessController.TryExecuteAction(out string acqErr);
            Assert.That(acquired, Is.True, acqErr);
            Assert.That(coordinator1.AutoRepairShop.IsOwned, Is.True);
            Assert.That(coordinator1.AutoRepairShop.Level, Is.EqualTo(1));

            // 3. Run operational task
            bool startedTask = coordinator1.TaskController.TryStartTask(out string taskErr);
            Assert.That(startedTask, Is.True, taskErr);

            // Tick task 5 seconds to complete
            coordinator1.Tick(5.0);
            Assert.That(coordinator1.TaskController.CanClaimReward, Is.True);

            bool claimedTask = coordinator1.TaskController.TryClaimReward(out Currency taskReward, out _);
            Assert.That(claimedTask, Is.True);
            Assert.That(taskReward, Is.EqualTo(EconomyConfig.DefaultTaskReward)); // $150.00

            // 4. Save state
            coordinator1.SaveCurrentState();
            Assert.That(File.Exists(_saveService.PrimaryPath), Is.True);

            // 5. Advance time by 2 hours (7200s) to simulate offline absence
            _timeProvider.Advance(TimeSpan.FromHours(2));

            // 6. Launch new session: simulate reopening the app
            var coordinator2 = new GameSessionCoordinator(_saveService, _timeProvider);
            coordinator2.InitializeSession();

            // Verify state restoration ($150 task reward + $0.17 passive income earned during 5s of tick)
            Assert.That(coordinator2.AutoRepairShop.IsOwned, Is.True);
            Assert.That(coordinator2.AutoRepairShop.Level, Is.EqualTo(1));
            Assert.That(coordinator2.Wallet.Balance, Is.EqualTo(Currency.FromCents(15017)));

            // Verify offline earnings popup is ready with 2 hours of $120.00/hr = $240.00
            Assert.That(coordinator2.OfflineModalController.HasClaimableEarnings, Is.True);
            Assert.That(coordinator2.OfflineModalController.AmountEarned, Is.EqualTo(Currency.FromDollars(240.00)));
            Assert.That(coordinator2.OfflineModalController.DurationAwayFormatted, Is.EqualTo("2h 0m"));

            // Claim offline earnings
            bool claimedOffline = coordinator2.OfflineModalController.TryClaim(out Currency offlineEarned, out _);
            Assert.That(claimedOffline, Is.True);
            Assert.That(offlineEarned, Is.EqualTo(Currency.FromDollars(240.00)));
            Assert.That(coordinator2.Wallet.Balance, Is.EqualTo(Currency.FromCents(39017))); // $150.17 + $240.00 = $390.17
        }

        [Test]
        public void JobBoardController_FiltersAndApplications_WorkInteractively()
        {
            var simCoord = new LifeSimulationCoordinator(_saveService);
            simCoord.InitializeSession(GameDifficulty.Standard);
            var controller = new JobBoardController(simCoord);

            Assert.That(controller.IsEmployed, Is.False);
            Assert.That(controller.CurrentJobTitle, Is.EqualTo("Unemployed"));

            // Select Retail Assistant
            controller.SelectJob("job_retail_assistant");
            Assert.That(controller.SelectedJob.Id, Is.EqualTo("job_retail_assistant"));
            Assert.That(controller.CanApplySelected(out _), Is.True);

            // Apply
            var res = controller.ApplyForSelected();
            Assert.That(res.Success, Is.True);
            Assert.That(controller.IsEmployed, Is.True);
            Assert.That(controller.CurrentJobTitle, Is.EqualTo("Retail Assistant"));
            Assert.That(controller.CurrentSalaryFormatted, Is.EqualTo("$1,600.00/mo"));

            // Resign
            controller.ResignCurrentJob();
            Assert.That(controller.IsEmployed, Is.False);
            Assert.That(controller.CurrentJobTitle, Is.EqualTo("Unemployed"));
        }

        [Test]
        public void EducationCatalogueController_CourseSelectionAndPaceToggle_WorkInteractively()
        {
            var simCoord = new LifeSimulationCoordinator(_saveService);
            simCoord.InitializeSession(GameDifficulty.Standard);
            var controller = new EducationCatalogueController(simCoord);

            Assert.That(controller.HasActiveEnrollment, Is.False);

            // Filter to Degrees
            controller.SetTypeFilter(EducationType.Degree);
            Assert.That(controller.FilteredCourses.All(c => c.Type == EducationType.Degree), Is.True);

            // Select Diploma in Business Administration
            controller.SelectCourse("degree_business_diploma");
            Assert.That(controller.SelectedCourse.Id, Is.EqualTo("degree_business_diploma"));

            // Toggle Pace
            controller.SetStudyPace(StudyPace.FullTime);
            Assert.That(controller.SelectedPace, Is.EqualTo(StudyPace.FullTime));

            // Enroll (player starts with $3,000, tuition is $3,500)
            bool canEnroll = controller.CanEnrollSelected(out string reason);
            Assert.That(canEnroll, Is.False);
            Assert.That(reason, Does.Contain("Insufficient funds for tuition"));

            // Add cash
            simCoord.Wallet.Add(Currency.FromDollars(1000.00));
            Assert.That(controller.CanEnrollSelected(out _), Is.True);

            bool enrolled = controller.EnrollSelected(out string err);
            Assert.That(enrolled, Is.True, err);
            Assert.That(controller.HasActiveEnrollment, Is.True);
            Assert.That(controller.ActiveCourseTitle, Is.EqualTo("Diploma in Business Administration"));
            Assert.That(controller.ActiveStudyPaceText, Does.Contain("Full-Time"));
        }

        [Test]
        public void HousingCatalogueController_BrowsingAndLeaseSign_UpdatesPresentation()
        {
            var simCoord = new LifeSimulationCoordinator(_saveService);
            simCoord.InitializeSession(GameDifficulty.Standard);
            var controller = new HousingCatalogueController(simCoord);

            Assert.That(controller.CurrentHousingName, Is.EqualTo("Studio Apartment"));
            Assert.That(controller.CurrentRentText, Is.EqualTo("$600.00/mo"));

            // Select Shared Room ($300/mo)
            controller.SelectTier("housing_shared_room");
            Assert.That(controller.IsSelectedCurrent, Is.False);
            Assert.That(controller.CanMoveToSelected(out _), Is.True);

            bool moved = controller.MoveToSelected(out string err);
            Assert.That(moved, Is.True, err);
            Assert.That(controller.CurrentHousingName, Is.EqualTo("Shared Room"));
            Assert.That(controller.CurrentRentText, Is.EqualTo("$300.00/mo"));
            Assert.That(controller.CurrentComfortRating, Is.EqualTo(20));
        }

        [Test]
        public void LifestyleStatusController_RestBreakAndSleep_RefreshesViewModel()
        {
            var simCoord = new LifeSimulationCoordinator(_saveService);
            simCoord.InitializeSession(GameDifficulty.Standard);
            var controller = new LifestyleStatusController(simCoord);

            Assert.That(controller.CurrentEnergy, Is.EqualTo(100));
            Assert.That(controller.FatigueState, Is.EqualTo(FatigueLevel.WellRested));

            // Consume energy
            simCoord.LifestyleService.ConsumeEnergy(60);
            Assert.That(controller.CurrentEnergy, Is.EqualTo(40));
            Assert.That(controller.FatigueState, Is.EqualTo(FatigueLevel.Normal));

            // Rest break (+20)
            controller.TakeRest();
            Assert.That(controller.CurrentEnergy, Is.EqualTo(60));

            // Sleep to morning (+70 in Studio)
            controller.SleepToMorning();
            Assert.That(controller.CurrentEnergy, Is.EqualTo(100));
            Assert.That(controller.FatigueState, Is.EqualTo(FatigueLevel.WellRested));
        }
    }
}

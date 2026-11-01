using System;
using NUnit.Framework;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Tests
{
    [TestFixture]
    public class EconomyTests
    {
        [Test]
        public void Currency_Arithmetic_PerformsExactIntegerMath()
        {
            var fourDollars = Currency.FromDollars(4.00);
            var eightDollars = Currency.FromCents(800);

            var total = fourDollars + eightDollars;
            Assert.That(total.MinorUnits, Is.EqualTo(1200));
            Assert.That(total.ToDollars(), Is.EqualTo(12.00));

            var diff = eightDollars - fourDollars;
            Assert.That(diff.MinorUnits, Is.EqualTo(400));
        }

        [Test]
        public void Currency_Underflow_ThrowsInvalidOperationException()
        {
            var smaller = Currency.FromCents(100);
            var larger = Currency.FromCents(200);

            Assert.Throws<InvalidOperationException>(() =>
            {
                var result = smaller - larger;
            });
        }

        [Test]
        public void Currency_NegativeCreation_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Currency.FromCents(-50));
            Assert.Throws<ArgumentOutOfRangeException>(() => Currency.FromDollars(-1.50));
        }

        [Test]
        public void Currency_Formatting_FormatsExpectedAbbreviations()
        {
            Assert.That(Currency.FromDollars(4.00).ToString(), Is.EqualTo("$4.00"));
            Assert.That(Currency.FromDollars(1250.50).ToString(), Is.EqualTo("$1,250.50"));
            Assert.That(Currency.FromDollars(1_250_000.00).ToString(), Is.EqualTo("$1.25M"));
            Assert.That(Currency.FromDollars(4_880_000_000.00).ToString(), Is.EqualTo("$4.88B"));
        }

        [Test]
        public void Wallet_TrySpend_RejectsInsufficientFunds()
        {
            var wallet = new Wallet(Currency.FromDollars(10.00), Currency.FromDollars(10.00));
            var expensiveItem = Currency.FromDollars(15.00);

            bool success = wallet.TrySpend(expensiveItem, out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("Insufficient funds"));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(10.00)));
        }

        [Test]
        public void Wallet_TrySpend_DeductsCorrectBalance()
        {
            var wallet = new Wallet(Currency.FromDollars(50.00), Currency.FromDollars(50.00));
            var cost = Currency.FromDollars(20.00);

            bool success = wallet.TrySpend(cost, out string error);

            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(30.00)));
            Assert.That(wallet.LifetimeEarned, Is.EqualTo(Currency.FromDollars(50.00)));
        }

        [Test]
        public void Business_Upgrade_IncreasesCostAndIncome()
        {
            var biz = new Business("auto_repair", "Auto Repair", Currency.FromDollars(5499.00), Currency.FromDollars(120.00));
            Assert.That(biz.IsOwned, Is.False);
            Assert.That(biz.GetHourlyIncome(), Is.EqualTo(Currency.Zero));

            biz.Acquire();
            Assert.That(biz.IsOwned, Is.True);
            Assert.That(biz.Level, Is.EqualTo(1));
            Assert.That(biz.GetHourlyIncome(), Is.EqualTo(Currency.FromDollars(120.00)));

            var level2Cost = biz.GetUpgradeCost();
            Assert.That(level2Cost.ToDollars(), Is.EqualTo(5499.00)); // First upgrade cost is base cost

            biz.Upgrade();
            Assert.That(biz.Level, Is.EqualTo(2));
            // +25% income = $120 * 1.25 = $150.00/hr
            Assert.That(biz.GetHourlyIncome().ToDollars(), Is.EqualTo(150.00));

            var level3Cost = biz.GetUpgradeCost();
            // Level 2 -> 3 cost = $5,499 * 1.25 = $6,873.75
            Assert.That(level3Cost.ToDollars(), Is.EqualTo(6873.75));
        }

        [Test]
        public void BusinessTask_LifecycleAndRewardPrevention()
        {
            var wallet = new Wallet();
            var task = new BusinessTask("oil_change", "Engine Diagnostics", 5.0, Currency.FromDollars(150.00));

            Assert.That(task.State, Is.EqualTo(BusinessTaskState.Idle));
            Assert.That(task.ProgressNormalized, Is.EqualTo(0f));

            // Cannot claim before starting
            bool claimBeforeStart = task.TryClaimReward(wallet, out _, out string err1);
            Assert.That(claimBeforeStart, Is.False);

            task.Start();
            Assert.That(task.State, Is.EqualTo(BusinessTaskState.Running));

            task.Tick(2.5);
            Assert.That(task.ProgressNormalized, Is.EqualTo(0.5f));
            Assert.That(task.State, Is.EqualTo(BusinessTaskState.Running));

            // Complete task
            task.Tick(2.5);
            Assert.That(task.ProgressNormalized, Is.EqualTo(1.0f));
            Assert.That(task.State, Is.EqualTo(BusinessTaskState.Completed));

            // First claim succeeds
            bool firstClaim = task.TryClaimReward(wallet, out Currency reward, out string err2);
            Assert.That(firstClaim, Is.True);
            Assert.That(reward, Is.EqualTo(Currency.FromDollars(150.00)));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(150.00)));

            // Duplicate claim must be prevented
            bool duplicateClaim = task.TryClaimReward(wallet, out _, out string err3);
            Assert.That(duplicateClaim, Is.False);
            Assert.That(err3, Does.Contain("Cannot claim reward"));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(150.00)));
        }

        [Test]
        public void BusinessTask_Cancellation_ResetsRunningState()
        {
            var task = new BusinessTask("tire_rotation", "Tire Alignment", 4.0, Currency.FromDollars(80.00));
            task.Start();
            task.Tick(2.0);
            Assert.That(task.ProgressNormalized, Is.EqualTo(0.5f));

            task.Cancel();
            Assert.That(task.State, Is.EqualTo(BusinessTaskState.Cancelled));
            Assert.That(task.ProgressNormalized, Is.EqualTo(0.0f));
        }

        [Test]
        public void EconomyService_Tap_GeneratesGroundedIncomeAndMultipliers()
        {
            var wallet = new Wallet();
            var biz = new Business("auto_repair", "Auto Repair", EconomyConfig.StarterBusinessBaseCost, EconomyConfig.StarterBusinessBaseHourlyYield);
            var task = new BusinessTask(EconomyConfig.DefaultTaskId, EconomyConfig.DefaultTaskName, EconomyConfig.DefaultTaskDurationSeconds, EconomyConfig.DefaultTaskReward);
            var timeProvider = new MockTimeProvider(DateTime.UtcNow);
            var service = new EconomyService(wallet, biz, task, timeProvider);

            // Base tap = $4.00
            var tap1 = service.Tap();
            Assert.That(tap1, Is.EqualTo(Currency.FromDollars(4.00)));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(4.00)));

            // Activate 6x multiplier booster
            service.ActivateTapBooster(30.0);
            Assert.That(service.IsBoosterActive, Is.True);

            // Boosted tap = $4.00 * 6 = $24.00 (Verified Phase 1 figure)
            var tap2 = service.Tap();
            Assert.That(tap2, Is.EqualTo(Currency.FromDollars(24.00)));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(28.00)));

            // Tick booster past expiration
            service.Tick(30.1);
            Assert.That(service.IsBoosterActive, Is.False);

            // Back to $4.00
            var tap3 = service.Tap();
            Assert.That(tap3, Is.EqualTo(Currency.FromDollars(4.00)));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(32.00)));
        }

        [Test]
        public void EconomyService_PassiveIncome_TicksCorrectly()
        {
            var wallet = new Wallet();
            var biz = new Business("auto_repair", "Auto Repair", Currency.FromDollars(5499.00), Currency.FromDollars(3600.00)); // $3600/hr = $1.00/sec
            var task = new BusinessTask("task1", "Task", 10.0, Currency.FromDollars(50.00));
            var service = new EconomyService(wallet, biz, task, new MockTimeProvider(DateTime.UtcNow));

            biz.Acquire();

            // Tick 5 seconds -> should earn 5 * $1.00 = $5.00
            for (int i = 0; i < 5; i++)
            {
                service.Tick(1.0);
            }

            Assert.That(wallet.Balance.ToDollars(), Is.EqualTo(5.00));
        }

        [Test]
        public void EconomyService_LevelUp_AdvancesWhenTargetReached()
        {
            var wallet = new Wallet();
            var biz = new Business("auto", "Auto", Currency.FromDollars(100), Currency.FromDollars(10));
            var task = new BusinessTask("task", "Task", 5, Currency.FromDollars(10));
            var service = new EconomyService(wallet, biz, task, new MockTimeProvider(DateTime.UtcNow));

            Assert.That(service.CurrentLevel, Is.EqualTo(1));
            Assert.That(service.LevelTargetBalance, Is.EqualTo(EconomyConfig.Level1TargetBalance)); // $1,000.00

            // Add $1,000.00
            wallet.Add(Currency.FromDollars(1000.00));
            service.Tick(0.1);

            Assert.That(service.CurrentLevel, Is.EqualTo(2));
            // Level 2 target = $1000 * 2.5 = $2,500.00
            Assert.That(service.LevelTargetBalance, Is.EqualTo(Currency.FromDollars(2500.00)));
        }
    }
}

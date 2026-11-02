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
    public class LifeProgressionTests
    {
        private string _testDir;
        private SaveDataService _saveService;
        private LifeSimulationCoordinator _coordinator;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "RichRich_GoalsTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            _saveService = new SaveDataService(_testDir);
            _coordinator = new LifeSimulationCoordinator(_saveService);
            _coordinator.InitializeSession(GameDifficulty.Standard);
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
        public void LifeProgression_Initialization_HasDefaultGoalsAndUnachievedStatus()
        {
            var service = _coordinator.LifeProgressionService;
            Assert.That(service, Is.Not.Null);
            Assert.That(service.Goals.Count, Is.EqualTo(7));
            Assert.That(service.CompletedGoals.Count, Is.EqualTo(0));
            Assert.That(service.ActiveGoals.Count, Is.EqualTo(7));
            Assert.That(service.IsFireAchieved, Is.False);
        }

        [Test]
        public void LifeProgression_NetWorthGoal_CompletesAndDepositsCashRewardViaLedger()
        {
            var service = _coordinator.LifeProgressionService;
            var wallet = _coordinator.Wallet;
            long startingCents = wallet.Balance.MinorUnits;

            // Deposit $12,000 to surpass $10,000 net worth threshold
            wallet.Add(Currency.FromDollars(12000.00));
            _coordinator.FinancialService.BalanceSheet.UpdateCash(wallet.Balance);

            service.EvaluateGoals();

            var goal = service.Goals.FirstOrDefault(g => g.Id == "goal_first_capital");
            Assert.That(goal, Is.Not.Null);
            Assert.That(goal.IsCompleted, Is.True);
            Assert.That(goal.ProgressFraction, Is.EqualTo(1.0));

            // $1,000 achievement award deposited
            long expectedBalance = startingCents + 1200000L + 100000L;
            Assert.That(wallet.Balance.MinorUnits, Is.EqualTo(expectedBalance));

            // Verify financial ledger audit
            var tx = _coordinator.Ledger.Transactions.FirstOrDefault(t => t.RelatedEntityId == "goal_first_capital");
            Assert.That(tx, Is.Not.Null);
            Assert.That(tx.Amount.MinorUnits, Is.EqualTo(100000L));
            Assert.That(tx.Type, Is.EqualTo(TransactionType.Income));
        }

        [Test]
        public void LifeProgression_PropertyInvestorGoal_CompletesUponPropertyAcquisition()
        {
            var service = _coordinator.LifeProgressionService;
            _coordinator.Wallet.Add(Currency.FromDollars(100000.00));

            bool bought = _coordinator.RealEstateService.BuyProperty("prop_starter_home", withMortgage: false, out string err);
            Assert.That(bought, Is.True, err);

            service.EvaluateGoals();

            var goal = service.Goals.FirstOrDefault(g => g.Id == "goal_landlord");
            Assert.That(goal, Is.Not.Null);
            Assert.That(goal.IsCompleted, Is.True);
        }

        [Test]
        public void LifeProgression_FIRERatio_CalculatesRentAndDividendsAgainstLivingExpenses()
        {
            var service = _coordinator.LifeProgressionService;

            // Baseline: 0 passive income
            Assert.That(service.PassiveIncomeRatio, Is.EqualTo(0.0));
            Assert.That(service.IsFireAchieved, Is.False);

            // Buy property generating rental income and sign tenant
            _coordinator.Wallet.Add(Currency.FromDollars(500000.00));
            bool bought = _coordinator.RealEstateService.BuyProperty("prop_retail_strip", withMortgage: false, out _);
            Assert.That(bought, Is.True);
            bool signed = _coordinator.RealEstateService.SignTenant("prop_retail_strip", Currency.FromDollars(3900.00), out _);
            Assert.That(signed, Is.True);

            double ratio = service.PassiveIncomeRatio;
            Assert.That(ratio, Is.GreaterThan(0.0));

            if (service.RealEstateService.TotalMonthlyRentalIncome.MinorUnits >= service.ExpenseService.TotalMonthlyBudget.MinorUnits)
            {
                Assert.That(service.IsFireAchieved, Is.True);
            }
        }

        [Test]
        public void LifeProgression_DynamicEvent_ResolvesChoiceWithCashFlowAndLedgerEntry()
        {
            var service = _coordinator.LifeProgressionService;
            _coordinator.Wallet.Add(Currency.FromDollars(10000.00));

            // Trigger random event
            service.TriggerRandomEvent();
            Assert.That(service.CurrentPendingEvent, Is.Not.Null);

            var currentEvent = service.CurrentPendingEvent;
            var choice = currentEvent.Choices.First();

            long preBalance = _coordinator.Wallet.Balance.MinorUnits;
            bool resolved = service.ResolveCurrentEventChoice(choice.Id, out string msg, out string err);

            Assert.That(resolved, Is.True, err);
            Assert.That(service.CurrentPendingEvent, Is.Null);
            Assert.That(msg, Is.Not.Empty);

            if (choice.CashCost.MinorUnits > 0)
            {
                var expenseTx = _coordinator.Ledger.Transactions.FirstOrDefault(t => t.RelatedEntityId == currentEvent.Id && t.Type == TransactionType.Expense);
                Assert.That(expenseTx, Is.Not.Null);
            }
        }

        [Test]
        public void LifeProgression_Persistence_Schema7_SerializesAndRestoresMilestoneProgress()
        {
            var service = _coordinator.LifeProgressionService;
            _coordinator.Wallet.Add(Currency.FromDollars(20000.00));
            _coordinator.FinancialService.BalanceSheet.UpdateCash(_coordinator.Wallet.Balance);
            service.EvaluateGoals();

            var completedGoal = service.Goals.First(g => g.Id == "goal_first_capital");
            Assert.That(completedGoal.IsCompleted, Is.True);

            // Save state
            var saved = _coordinator.SaveCurrentState();
            Assert.That(saved.SchemaVersion, Is.GreaterThanOrEqualTo(7));
            Assert.That(saved.Goals, Is.Not.Null);
            Assert.That(saved.Goals.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(saved.Goals.Any(g => g.GoalId == "goal_first_capital" && g.IsCompleted), Is.True);

            _saveService.Save(saved);

            // Reload in fresh coordinator
            var reloaded = new LifeSimulationCoordinator(_saveService);
            reloaded.InitializeSession();

            var restoredGoal = reloaded.LifeProgressionService.Goals.FirstOrDefault(g => g.Id == "goal_first_capital");
            Assert.That(restoredGoal, Is.Not.Null);
            Assert.That(restoredGoal.IsCompleted, Is.True);
            Assert.That(reloaded.LifeProgressionService.CompletedGoals.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void LifeProgressionController_ExposesFormattedMetricsAndChoiceResolution()
        {
            var controller = new LifeProgressionController(_coordinator);
            Assert.That(controller.Goals.Count, Is.EqualTo(7));
            Assert.That(controller.FireProgressText, Does.Contain("% FIRE"));
            Assert.That(controller.GoalsAchievedSummary, Does.Contain("/7 Milestones Unlocked"));

            _coordinator.Wallet.Add(Currency.FromDollars(20000.00));
            _coordinator.LifeProgressionService.TriggerRandomEvent();
            Assert.That(controller.HasPendingEvent, Is.True);
            Assert.That(controller.CurrentPendingEvent, Is.Not.Null);

            var firstChoice = controller.CurrentPendingEvent.Choices.First();
            bool result = controller.ChooseEventOption(firstChoice.Id, out string outcome, out string err);
            Assert.That(result, Is.True, err);
            Assert.That(controller.HasPendingEvent, Is.False);
        }
    }
}

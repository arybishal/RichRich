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
    public class LifeSimulationTests
    {
        private string _testDir;
        private SaveDataService _saveService;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "RichRich_LifeSimTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            _saveService = new SaveDataService(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }

        #region 1. Starting Cash Selection
        [Test]
        public void StartingCash_Selection_MatchesDifficultyOptions()
        {
            var hardConfig = new GameStartConfig(GameDifficulty.Hard);
            var standardConfig = new GameStartConfig(GameDifficulty.Standard);
            var comfortableConfig = new GameStartConfig(GameDifficulty.Comfortable);

            Assert.That(hardConfig.StartingCash, Is.EqualTo(Currency.FromDollars(1000.00)));
            Assert.That(hardConfig.StartingCash.MinorUnits, Is.EqualTo(100_000L));

            Assert.That(standardConfig.StartingCash, Is.EqualTo(Currency.FromDollars(3000.00)));
            Assert.That(standardConfig.StartingCash.MinorUnits, Is.EqualTo(300_000L));

            Assert.That(comfortableConfig.StartingCash, Is.EqualTo(Currency.FromDollars(5000.00)));
            Assert.That(comfortableConfig.StartingCash.MinorUnits, Is.EqualTo(500_000L));

            // Default difficulty must be Standard
            Assert.That(GameStartConfig.Default.Difficulty, Is.EqualTo(GameDifficulty.Standard));
            Assert.That(GameStartConfig.Default.StartingCash, Is.EqualTo(Currency.FromDollars(3000.00)));
        }
        #endregion

        #region 2. Simulation Clock Progression
        [Test]
        public void SimulationClock_Progression_AdvancesMinutesHoursAndDays()
        {
            var clock = new SimulationClock(new GameDateTime(1, 1, 1, 8, 0));
            Assert.That(clock.CurrentDate.Hour, Is.EqualTo(8));
            Assert.That(clock.CurrentDate.Minute, Is.EqualTo(0));

            // Tick 60 real seconds at 1.0x speed -> advances 60 simulation minutes (1 hour)
            clock.Tick(60.0);
            Assert.That(clock.CurrentDate.Hour, Is.EqualTo(9));
            Assert.That(clock.CurrentDate.Minute, Is.EqualTo(0));

            // Speed up to 4.0x: 15 real seconds -> advances 60 simulation minutes (1 hour)
            clock.SpeedMultiplier = 4.0f;
            clock.Tick(15.0);
            Assert.That(clock.CurrentDate.Hour, Is.EqualTo(10));

            // Paused clock: 100 real seconds -> no advancement
            clock.SpeedMultiplier = 0.0f;
            Assert.That(clock.IsPaused, Is.True);
            clock.Tick(100.0);
            Assert.That(clock.CurrentDate.Hour, Is.EqualTo(10));
        }
        #endregion

        #region 3. Daily & Monthly Event Processing
        [Test]
        public void DailyAndMonthly_EventProcessing_FiresChronologicalTriggersSequentially()
        {
            var clock = new SimulationClock(new GameDateTime(1, 1, 1, 0, 0));
            int daysCount = 0;
            int monthsCount = 0;

            clock.OnDayPassed += _ => daysCount++;
            clock.OnMonthPassed += _ => monthsCount++;

            // Advance 35 days (crosses from Month 1 into Month 2)
            clock.AdvanceDays(35);

            Assert.That(daysCount, Is.EqualTo(35));
            Assert.That(monthsCount, Is.EqualTo(1));
            Assert.That(clock.CurrentDate.Month, Is.EqualTo(2));
            Assert.That(clock.CurrentDate.Day, Is.EqualTo(6));
        }
        #endregion

        #region 4. Recurring Expenses
        [Test]
        public void RecurringExpenses_DeductsOnScheduledDueDates()
        {
            var clock = new SimulationClock(new GameDateTime(1, 1, 1, 0, 0));
            var wallet = new Wallet(Currency.FromDollars(3000.00), Currency.FromDollars(3000.00));
            var ledger = new FinancialLedger();
            var finService = new FinancialService(wallet, ledger, clock);
            var expenseService = new LivingExpenseService(finService, clock);

            // Default monthly budget = $600 + $300 + $100 + $120 + $50 + $150 = $1,320.00
            Assert.That(expenseService.TotalMonthlyBudget, Is.EqualTo(Currency.FromDollars(1320.00)));

            // Trigger Day 1 deduction
            expenseService.CheckScheduledExpenses(clock.CurrentDate);

            // Cash reduced by $1,320: $3,000 - $1,320 = $1,680.00
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(1680.00)));
            Assert.That(ledger.Transactions.Count, Is.EqualTo(6));
        }
        #endregion

        #region 5. Financial Ledger Accuracy
        [Test]
        public void FinancialLedger_Accuracy_TracksAllCategoriesAndIncomeExpenseTotals()
        {
            var ledger = new FinancialLedger();
            var date = new GameDateTime(1, 1, 5, 12, 0);

            ledger.Record(new FinancialTransaction("tx1", Currency.FromDollars(1600.00), TransactionType.Income, TransactionCategory.EmploymentSalary, date, "Salary"));
            ledger.Record(new FinancialTransaction("tx2", Currency.FromDollars(600.00), TransactionType.Expense, TransactionCategory.LivingAccommodation, date, "Rent"));
            ledger.Record(new FinancialTransaction("tx3", Currency.FromDollars(300.00), TransactionType.Expense, TransactionCategory.LivingFood, date, "Groceries"));

            var start = new GameDateTime(1, 1, 1, 0, 0);
            var end = new GameDateTime(1, 1, 30, 23, 59);

            Assert.That(ledger.GetTotalIncome(start, end), Is.EqualTo(Currency.FromDollars(1600.00)));
            Assert.That(ledger.GetTotalExpenses(start, end), Is.EqualTo(Currency.FromDollars(900.00)));
            Assert.That(ledger.GetNetCashFlowCents(start, end), Is.EqualTo(70000L)); // +$700.00

            var breakdown = ledger.GetExpenseBreakdown(start, end);
            Assert.That(breakdown[TransactionCategory.LivingAccommodation], Is.EqualTo(Currency.FromDollars(600.00)));
            Assert.That(breakdown[TransactionCategory.LivingFood], Is.EqualTo(Currency.FromDollars(300.00)));
        }
        #endregion

        #region 6. Net-Worth Calculation
        [Test]
        public void NetWorth_Calculation_EvaluatesAssetsMinusLiabilitiesAccurately()
        {
            var sheet = new BalanceSheet();
            sheet.UpdateCash(Currency.FromDollars(3000.00));
            sheet.UpdateNonCashAssets(Currency.FromDollars(2000.00));
            sheet.UpdateLoanLiabilities(Currency.FromDollars(4000.00));

            // Total Assets = $5,000. Liabilities = $4,000. Net Worth = +$1,000.00
            Assert.That(sheet.TotalAssets, Is.EqualTo(Currency.FromDollars(5000.00)));
            Assert.That(sheet.TotalLiabilities, Is.EqualTo(Currency.FromDollars(4000.00)));
            Assert.That(sheet.NetWorthCents, Is.EqualTo(100000L));
            Assert.That(sheet.IsNegativeNetWorth, Is.False);
            Assert.That(sheet.NetWorthFormatted, Is.EqualTo("$1,000.00"));

            // Increase liabilities to $7,000 -> Net Worth = -$2,000.00
            sheet.UpdateLoanLiabilities(Currency.FromDollars(7000.00));
            Assert.That(sheet.NetWorthCents, Is.EqualTo(-200000L));
            Assert.That(sheet.IsNegativeNetWorth, Is.True);
            Assert.That(sheet.NetWorthFormatted, Is.EqualTo("-$2,000.00"));
        }
        #endregion

        #region 7. Negative Cash Flow
        [Test]
        public void NegativeCashFlow_DetectsDeficitAndCreatesFinancialPressureWithoutImmediateBankruptcy()
        {
            var clock = new SimulationClock();
            var wallet = new Wallet(Currency.FromDollars(5000.00), Currency.FromDollars(5000.00));
            var finService = new FinancialService(wallet, new FinancialLedger(), clock);
            var expenseService = new LivingExpenseService(finService, clock); // $1,320/mo expenses
            var educationService = new EducationService(finService, clock);
            var careerService = new CareerService(finService, educationService, clock); // Unemployed: $0/mo income
            var loanService = new LoanService(finService, careerService, clock);
            var healthService = new FinancialHealthService(finService, expenseService, careerService, loanService);

            Assert.That(healthService.ProjectedMonthlyIncome, Is.EqualTo(Currency.Zero));
            Assert.That(healthService.ProjectedMonthlyExpenses, Is.EqualTo(Currency.FromDollars(1320.00)));
            Assert.That(healthService.HasNegativeCashFlow, Is.True);
            Assert.That(healthService.ProjectedNetMonthlyCashFlowCents, Is.EqualTo(-132000L));

            // Should be FinanciallyStressed, NOT Bankrupt! (Player has $5,000 cash cushion)
            var report = healthService.EvaluateStatus();
            Assert.That(report.State, Is.EqualTo(FinancialHealthState.FinanciallyStressed));
            Assert.That(report.Recommendation, Does.Contain("Improve income"));
        }
        #endregion

        #region 8. Loan Principal & Liability Accounting
        [Test]
        public void LoanPrincipal_AndLiabilityAccounting_DisbursementDoesNotIncreaseNetWorth()
        {
            var clock = new SimulationClock();
            var wallet = new Wallet(Currency.FromDollars(3000.00), Currency.FromDollars(3000.00));
            var ledger = new FinancialLedger();
            var finService = new FinancialService(wallet, ledger, clock);
            var careerService = new CareerService(finService, new EducationService(finService, clock), clock);
            var loanService = new LoanService(finService, careerService, clock);

            long initialNetWorth = finService.NetWorthCents;
            Assert.That(initialNetWorth, Is.EqualTo(300000L)); // $3,000.00

            // Take $5,000 Personal Loan
            // Give player employment first so eligibility passes
            careerService.TryApply("job_delivery_worker", out _);
            bool success = loanService.TryTakeLoan("loan_personal_5k", out var activeLoan, out string err);
            Assert.That(success, Is.True, err);

            // Cash increases to $8,000
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(8000.00)));
            // Liabilities increase to $5,000
            Assert.That(finService.BalanceSheet.LoanLiabilities, Is.EqualTo(Currency.FromDollars(5000.00)));
            // Net worth must remain EXACTLY identical ($3,000.00)
            Assert.That(finService.NetWorthCents, Is.EqualTo(initialNetWorth));
            Assert.That(finService.BalanceSheet.NetWorthFormatted, Is.EqualTo("$3,000.00"));
        }
        #endregion

        #region 9. Loan Interest & Instalment Calculations
        [Test]
        public void LoanInterest_AndInstalmentCalculations_ComputesExactMonthlyAmortization()
        {
            var product = new LoanProduct("loan5k", "Personal 5K", Currency.FromDollars(5000.00), annualInterestRate: 0.12, termMonths: 12);
            Currency installment = product.CalculateMonthlyInstallment();

            // Monthly amortization formula produces ~$444.24
            Assert.That(installment.ToDollars(), Is.EqualTo(444.24));

            var activeLoan = new ActiveLoan("loan1", product, new GameDateTime(1, 1, 1, 0, 0));
            var (interest, principal) = activeLoan.CalculateInstallmentSplit();

            // Month 1: 1% of $5,000 = $50.00 interest. Principal reduction = $444.24 - $50.00 = $394.24
            Assert.That(interest.ToDollars(), Is.EqualTo(50.00));
            Assert.That(principal.ToDollars(), Is.EqualTo(394.24));
        }
        #endregion

        #region 10. Loan Repayment
        [Test]
        public void LoanRepayment_ScheduledAndEarlyPayoff_ReducesLiabilities()
        {
            var clock = new SimulationClock(new GameDateTime(1, 1, 1, 0, 0));
            var wallet = new Wallet(Currency.FromDollars(10000.00), Currency.FromDollars(10000.00));
            var ledger = new FinancialLedger();
            var finService = new FinancialService(wallet, ledger, clock);
            var careerService = new CareerService(finService, new EducationService(finService, clock), clock);
            careerService.TryApply("job_delivery_worker", out _);
            var loanService = new LoanService(finService, careerService, clock);

            loanService.TryTakeLoan("loan_personal_5k", out var activeLoan, out _);
            Assert.That(activeLoan.RemainingPrincipal, Is.EqualTo(Currency.FromDollars(5000.00)));

            // 1. Advance to installment due day (Day 1 of next month)
            clock.AdvanceDays(30);

            // Principal should have decreased by $394.24 ($5,000 - $394.24 = $4,605.76)
            Assert.That(activeLoan.RemainingPrincipal.ToDollars(), Is.EqualTo(4605.76));
            Assert.That(activeLoan.PaymentsMade, Is.EqualTo(1));

            // 2. Early payoff in full
            bool repaid = loanService.TryRepayLoanEarly(activeLoan.LoanId, out string err);
            Assert.That(repaid, Is.True, err);
            Assert.That(activeLoan.IsFullyPaid, Is.True);
            Assert.That(activeLoan.RemainingPrincipal, Is.EqualTo(Currency.Zero));
            Assert.That(finService.BalanceSheet.LoanLiabilities, Is.EqualTo(Currency.Zero));
        }
        #endregion

        #region 11. Duplicate Payment Prevention
        [Test]
        public void DuplicatePayment_Prevention_GuaranteesSingleDeductionPerCalendarCycle()
        {
            var clock = new SimulationClock(new GameDateTime(1, 1, 1, 0, 0));
            var wallet = new Wallet(Currency.FromDollars(3000.00), Currency.FromDollars(3000.00));
            var ledger = new FinancialLedger();
            var finService = new FinancialService(wallet, ledger, clock);
            var expenseService = new LivingExpenseService(finService, clock);

            // Day 1 triggered: Rent and living expenses paid ($1,320)
            expenseService.CheckScheduledExpenses(clock.CurrentDate);
            Assert.That(ledger.Transactions.Count, Is.EqualTo(6));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(1680.00)));

            // Ticking 60 real seconds (1 simulation hour) on Day 1 MUST NOT duplicate payment
            clock.Tick(60.0);
            expenseService.CheckScheduledExpenses(clock.CurrentDate);
            Assert.That(ledger.Transactions.Count, Is.EqualTo(6));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(1680.00)));

            // Advance through rest of month (Day 2..30)
            clock.AdvanceDays(28);
            Assert.That(ledger.Transactions.Count, Is.EqualTo(6));

            // Advance into Month 2 Day 1: New month triggers exactly one more set of 6 deductions
            clock.AdvanceDays(2);
            Assert.That(clock.CurrentDate.Month, Is.EqualTo(2));
            Assert.That(clock.CurrentDate.Day, Is.EqualTo(1));
            Assert.That(ledger.Transactions.Count, Is.EqualTo(12));
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(360.00)));
        }
        #endregion

        #region 12. Education Progress
        [Test]
        public void EducationProgress_EnrollsDeductsTuitionAndAwardsQualificationUponCompletion()
        {
            var clock = new SimulationClock();
            var wallet = new Wallet(Currency.FromDollars(2000.00), Currency.FromDollars(2000.00));
            var finService = new FinancialService(wallet, new FinancialLedger(), clock);
            var educationService = new EducationService(finService, clock);

            // Enroll in free digital basics
            bool freeEnrolled = educationService.TryEnroll("course_free_digital", out string err1);
            Assert.That(freeEnrolled, Is.True, err1);
            Assert.That(educationService.ActiveEnrollment.IsCompleted, Is.False);

            // Study 2 hours per day for 10 days = 20 hours
            clock.AdvanceDays(10);
            Assert.That(educationService.ActiveEnrollment.IsCompleted, Is.True);
            Assert.That(educationService.HasQualification("qual_digital_basics"), Is.True);
            Assert.That(educationService.GetSkillLevel(SkillType.Technical), Is.EqualTo(10));

            // Now enroll in IT Support Certificate ($500.00 tuition)
            bool itEnrolled = educationService.TryEnroll("course_it_support_cert", out string err2);
            Assert.That(itEnrolled, Is.True, err2);
            // $2,000 - $500 = $1,500
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(1500.00)));

            // Complete IT course (60 hours -> 30 days of 2h/day study)
            clock.AdvanceDays(30);
            Assert.That(educationService.HasQualification("qual_it_support_cert"), Is.True);
            Assert.That(educationService.GetSkillLevel(SkillType.Technical), Is.EqualTo(35)); // 10 + 25 = 35
        }
        #endregion

        #region 13. Job Salary Income
        [Test]
        public void JobSalary_Income_HiresQualifiedApplicantAndCreditsMonthlySalary()
        {
            var clock = new SimulationClock(new GameDateTime(1, 1, 1, 0, 0));
            var wallet = new Wallet(Currency.FromDollars(1000.00), Currency.FromDollars(1000.00));
            var finService = new FinancialService(wallet, new FinancialLedger(), clock);
            var eduService = new EducationService(finService, clock);
            var careerService = new CareerService(finService, eduService, clock);

            // Cannot apply to IT Helpdesk without qualification
            bool itApplyFailed = careerService.TryApply("job_it_helpdesk", out string err);
            Assert.That(itApplyFailed, Is.False);
            Assert.That(err, Does.Contain("Missing required qualification"));

            // Apply to Retail Assistant ($1,600/mo, no prereqs)
            bool hired = careerService.TryApply("job_retail_assistant", out string hireErr);
            Assert.That(hired, Is.True, hireErr);
            Assert.That(careerService.IsEmployed, Is.True);

            // Advance to payday (Day 30)
            clock.AdvanceDays(29);
            Assert.That(clock.CurrentDate.Day, Is.EqualTo(30));

            // Salary credited: $1,000 + $1,600 = $2,600.00
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(2600.00)));
            Assert.That(careerService.CurrentEmployment.TotalMonthsWorked, Is.EqualTo(1));
        }
        #endregion

        #region 14. Financial Health State Transitions
        [Test]
        public void FinancialHealth_StateTransitions_EvaluatesAllSixConditions()
        {
            var clock = new SimulationClock();
            var wallet = new Wallet(Currency.FromDollars(5000.00), Currency.FromDollars(5000.00));
            var finService = new FinancialService(wallet, new FinancialLedger(), clock);
            var expenseService = new LivingExpenseService(finService, clock);
            var eduService = new EducationService(finService, clock);
            var careerService = new CareerService(finService, eduService, clock);
            careerService.TryApply("job_it_helpdesk", out _); // $2,800/mo salary (with prereqs bypassed in unit test if applied directly)

            // Let's hire retail assistant ($1,600/mo) vs $1,320 expenses = +$280 net cash flow
            careerService.TryApply("job_retail_assistant", out _);
            var loanService = new LoanService(finService, careerService, clock);
            var health = new FinancialHealthService(finService, expenseService, careerService, loanService);

            // 1. Healthy: Positive cash flow (+$280/mo), $5,000 cash reserves (>= 1 mo expenses)
            Assert.That(health.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.Healthy));

            // 2. Financially Stressed: Resign job -> $0 income, -$1,320 cash flow deficit
            careerService.ResignCurrentJob();
            Assert.That(health.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.FinanciallyStressed));

            // 3. Cash Crisis: Drain cash below 1 week expenses ($330)
            wallet.TrySpend(Currency.FromDollars(4800.00), out _); // Balance $200
            Assert.That(wallet.Balance, Is.EqualTo(Currency.FromDollars(200.00)));
            Assert.That(health.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.CashCrisis));

            // 4. Insolvent: Liabilities ($8,000) exceed Assets ($200) with monthly deficit
            finService.BalanceSheet.UpdateLoanLiabilities(Currency.FromDollars(8000.00));
            Assert.That(finService.BalanceSheet.IsNegativeNetWorth, Is.True);
            Assert.That(health.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.Insolvent));

            // 5. Bankrupt: Insolvent with 2+ missed payments and cash unable to clear overdue debt
            var activeLoan = new ActiveLoan("loan_test", loanService.Catalog[0], clock.CurrentDate);
            loanService.AddRestoredLoan(activeLoan);
            activeLoan.ApplyMissedPayment();
            activeLoan.ApplyMissedPayment();
            finService.AddOverdueLiability(Currency.FromDollars(1000.00), TransactionCategory.LatePenaltyFee, "Default");
            Assert.That(health.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.Bankrupt));
        }
        #endregion

        #region 15. Save and Reload Consistency
        [Test]
        public void SaveAndReload_Consistency_RestoresAllSimulationSubsystems()
        {
            // 1. Session A: Setup life simulation
            var coordinatorA = new LifeSimulationCoordinator(_saveService);
            coordinatorA.InitializeSession(GameDifficulty.Standard); // $3,000 starting cash

            Assert.That(coordinatorA.Wallet.Balance, Is.EqualTo(Currency.FromDollars(3000.00)));

            // Apply for job
            coordinatorA.CareerService.TryApply("job_delivery_worker", out _);
            // Enroll in digital course
            coordinatorA.EducationService.TryEnroll("course_free_digital", out _);
            // Take $5,000 loan
            coordinatorA.LoanService.TryTakeLoan("loan_personal_5k", out var loanA, out _);

            // Advance simulation 15 days
            coordinatorA.FastForwardDays(15);
            Assert.That(coordinatorA.Clock.CurrentDate.Day, Is.EqualTo(16));

            // Save state
            coordinatorA.SaveCurrentState();
            Assert.That(File.Exists(_saveService.PrimaryPath), Is.True);

            // 2. Session B: Restore session from disk
            var coordinatorB = new LifeSimulationCoordinator(_saveService);
            coordinatorB.InitializeSession();

            // Verify clock restored to exact minute
            Assert.That(coordinatorB.Clock.CurrentDate.TotalMinutes, Is.EqualTo(coordinatorA.Clock.CurrentDate.TotalMinutes));
            Assert.That(coordinatorB.Clock.CurrentDate.Day, Is.EqualTo(16));

            // Verify career restored
            Assert.That(coordinatorB.CareerService.IsEmployed, Is.True);
            Assert.That(coordinatorB.CareerService.CurrentEmployment.Job.Id, Is.EqualTo("job_delivery_worker"));

            // Verify education restored and digital course completed (15 days * 2h/day = 30h > 20h)
            Assert.That(coordinatorB.EducationService.HasQualification("qual_digital_basics"), Is.True);

            // Verify loan restored
            Assert.That(coordinatorB.LoanService.ActiveLoans.Count, Is.EqualTo(1));
            var loanB = coordinatorB.LoanService.ActiveLoans[0];
            Assert.That(loanB.Product.Id, Is.EqualTo("loan_personal_5k"));
            Assert.That(loanB.RemainingPrincipal, Is.EqualTo(loanA.RemainingPrincipal));

            // Verify cash & net worth matching
            Assert.That(coordinatorB.Wallet.Balance, Is.EqualTo(coordinatorA.Wallet.Balance));
            Assert.That(coordinatorB.BalanceSheet.NetWorthCents, Is.EqualTo(coordinatorA.BalanceSheet.NetWorthCents));
        }
        #endregion

        #region 16. Life Simulation HUD Controller
        [Test]
        public void LifeSimulationHUDController_FormatsAllFinancialAndCalendarMetrics()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard); // $3,000 cash

            var hud = new LifeSimulationHUDController(coordinator);

            Assert.That(hud.CashBalanceText, Is.EqualTo("$3,000.00"));
            Assert.That(hud.NetWorthText, Is.EqualTo("$3,000.00"));
            Assert.That(hud.IsNetWorthNegative, Is.False);
            Assert.That(hud.CareerStatusText, Is.EqualTo("Unemployed"));
            Assert.That(hud.HealthBadgeText, Is.EqualTo("FinanciallyStressed")); // Unemployed = deficit

            // Hire for job
            coordinator.CareerService.TryApply("job_delivery_worker", out _);
            Assert.That(hud.CareerStatusText, Does.Contain("Delivery Courier ($1,800.00/mo)"));
            Assert.That(hud.MonthlyIncomeText, Is.EqualTo("+$1,800.00/mo"));
            Assert.That(hud.MonthlyExpensesText, Is.EqualTo("-$1,320.00/mo"));
            Assert.That(hud.NetMonthlyCashFlowText, Is.EqualTo("+$480.00/mo"));
            Assert.That(hud.HealthBadgeText, Is.EqualTo("Healthy"));

            // Clock actions
            Assert.That(hud.ClockSpeedText, Is.EqualTo("1x Speed"));
            hud.TogglePause();
            Assert.That(hud.ClockSpeedText, Is.EqualTo("PAUSED"));
            hud.TogglePause();
            Assert.That(hud.ClockSpeedText, Is.EqualTo("1x Speed"));

            // Sleep to next morning (from Day 1 08:00 to Day 2 08:00)
            hud.SleepToNextMorning();
            Assert.That(coordinator.Clock.CurrentDate.Day, Is.EqualTo(2));
            Assert.That(coordinator.Clock.CurrentDate.Hour, Is.EqualTo(8));
        }
        #endregion
    }
}

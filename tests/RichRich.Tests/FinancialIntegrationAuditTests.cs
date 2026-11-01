using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;
using RichRich.Domain.Services;

namespace RichRich.Tests
{
    /// <summary>
    /// Milestone 2 -> 3 Transition: Deep Integration Audit Test Suite.
    /// Executes all 12 mandatory audit scenarios and 8 additional architectural checks.
    /// Validates real balances, ledger entries, event firing, and persistence fidelity.
    /// </summary>
    [TestFixture]
    public class FinancialIntegrationAuditTests
    {
        private string _testDir;
        private SaveDataService _saveService;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "RichRich_AuditTests_" + Guid.NewGuid().ToString("N"));
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

        #region Scenario 1: Start a new Standard game with $3,000
        [Test]
        public void AuditScenario_01_StartStandardGameWith3000()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            Assert.That(coordinator.Difficulty, Is.EqualTo(GameDifficulty.Standard));
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(3000.00)));
            Assert.That(coordinator.BalanceSheet.Cash, Is.EqualTo(Currency.FromDollars(3000.00)));
            Assert.That(coordinator.BalanceSheet.TotalAssets, Is.EqualTo(Currency.FromDollars(3000.00)));
            Assert.That(coordinator.BalanceSheet.TotalLiabilities, Is.EqualTo(Currency.Zero));
            Assert.That(coordinator.BalanceSheet.NetWorthCents, Is.EqualTo(300000L));
            Assert.That(coordinator.BalanceSheet.NetWorthFormatted, Is.EqualTo("$3,000.00"));
            Assert.That(coordinator.Clock.CurrentDate, Is.EqualTo(GameDateTime.DefaultStart)); // Y1 M1 D1 08:00
            Assert.That(coordinator.CareerService.IsEmployed, Is.False);
            Assert.That(coordinator.LoanService.ActiveLoans.Count, Is.EqualTo(0));
            Assert.That(coordinator.HealthService.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.FinanciallyStressed)); // Unemployed deficit
        }
        #endregion

        #region Scenario 2: Verify the player can access an entry-level job
        [Test]
        public void AuditScenario_02_AccessEntryLevelJob()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            var jobs = coordinator.CareerService.AvailableJobs;
            Assert.That(jobs.Count, Is.GreaterThanOrEqualTo(2));

            var retailJob = jobs.FirstOrDefault(j => j.Id == "job_retail_assistant");
            var courierJob = jobs.FirstOrDefault(j => j.Id == "job_delivery_worker");

            Assert.That(retailJob, Is.Not.Null);
            Assert.That(courierJob, Is.Not.Null);
            Assert.That(retailJob.MonthlySalary, Is.EqualTo(Currency.FromDollars(1600.00)));
            Assert.That(courierJob.MonthlySalary, Is.EqualTo(Currency.FromDollars(1800.00)));

            // Verify eligibility with 0 prior experience
            Assert.That(coordinator.CareerService.CanApply(retailJob.Id, out string r1), Is.True, r1);
            Assert.That(coordinator.CareerService.CanApply(courierJob.Id, out string r2), Is.True, r2);

            // Hire for Delivery Courier
            bool hired = coordinator.CareerService.TryApply(courierJob.Id, out string hireErr);
            Assert.That(hired, Is.True, hireErr);
            Assert.That(coordinator.CareerService.IsEmployed, Is.True);
            Assert.That(coordinator.CareerService.CurrentEmployment.Job.Id, Is.EqualTo("job_delivery_worker"));
            Assert.That(coordinator.HealthService.ProjectedMonthlyIncome, Is.EqualTo(Currency.FromDollars(1800.00)));

            // Cash flow surplus: $1,800 salary - $1,320 expenses = +$480.00/mo -> Status becomes Healthy!
            Assert.That(coordinator.HealthService.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.Healthy));
        }
        #endregion

        #region Scenario 3: Advance simulation time and verify first salary payment
        [Test]
        public void AuditScenario_03_AdvanceTimeAndVerifyFirstSalaryPayment()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);
            coordinator.CareerService.TryApply("job_delivery_worker", out _);

            long initialCash = coordinator.Wallet.Balance.MinorUnits; // 300,000 cents ($3,000)

            // Advance from Day 1 08:00 to Day 30 08:00 (29 days)
            coordinator.FastForwardDays(29);
            Assert.That(coordinator.Clock.CurrentDate.Day, Is.EqualTo(30));

            // On Day 30, salary ($1,800.00) is deposited
            Assert.That(coordinator.CareerService.CurrentEmployment.TotalMonthsWorked, Is.EqualTo(1));
            Assert.That(coordinator.Wallet.Balance.MinorUnits, Is.EqualTo(initialCash + 180000L)); // $4,800.00

            // Verify ledger contains the salary transaction
            var salaryTx = coordinator.Ledger.Transactions.FirstOrDefault(t => t.Category == TransactionCategory.EmploymentSalary);
            Assert.That(salaryTx, Is.Not.Null);
            Assert.That(salaryTx.Amount, Is.EqualTo(Currency.FromDollars(1800.00)));
            Assert.That(salaryTx.Type, Is.EqualTo(TransactionType.Income));
        }
        #endregion

        #region Scenario 4: Verify monthly expenses are deducted exactly once
        [Test]
        public void AuditScenario_04_VerifyMonthlyExpensesDeductedExactlyOnce()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            // Month 1 Day 1: Process initial expenses ($1,320)
            coordinator.ExpenseService.CheckScheduledExpenses(coordinator.Clock.CurrentDate);
            Assert.That(coordinator.Ledger.Transactions.Count(t => t.Type == TransactionType.Expense), Is.EqualTo(6));
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(1680.00))); // $3,000 - $1,320

            // Advance through Month 1 (Day 2..Day 30): NO additional living expense deductions
            for (int day = 2; day <= 30; day++)
            {
                coordinator.FastForwardDays(1);
            }
            Assert.That(coordinator.Ledger.Transactions.Count(t => t.Type == TransactionType.Expense), Is.EqualTo(6));

            // Advance into Month 2 Day 1: Month 2 expenses deducted exactly once (6 more transactions)
            coordinator.FastForwardDays(1);
            Assert.That(coordinator.Clock.CurrentDate.Month, Is.EqualTo(2));
            Assert.That(coordinator.Clock.CurrentDate.Day, Is.EqualTo(1));
            Assert.That(coordinator.Ledger.Transactions.Count(t => t.Type == TransactionType.Expense), Is.EqualTo(12));
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(360.00))); // $1,680 - $1,320
        }
        #endregion

        #region Scenario 5: Enroll in free digital skills course and complete it
        [Test]
        public void AuditScenario_05_EnrollInFreeCourseAndComplete()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            bool enrolled = coordinator.EducationService.TryEnroll("course_free_digital", out string err);
            Assert.That(enrolled, Is.True, err);
            Assert.That(coordinator.EducationService.ActiveEnrollment.IsCompleted, Is.False);
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(3000.00))); // $0 tuition

            // Study 2 hours/day for 10 days = 20 hours -> completes course
            coordinator.FastForwardDays(10);
            Assert.That(coordinator.EducationService.ActiveEnrollment.IsCompleted, Is.True);
            Assert.That(coordinator.EducationService.HasQualification("qual_digital_basics"), Is.True);
            Assert.That(coordinator.EducationService.GetSkillLevel(SkillType.Technical), Is.EqualTo(10));
        }
        #endregion

        #region Scenario 6: Enroll in IT certificate, verify tuition deduction and study progression
        [Test]
        public void AuditScenario_06_EnrollInITCertificateAndVerifyTuitionAndStudy()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            // Complete prereq first
            coordinator.EducationService.AddQualification("qual_digital_basics");

            // Enroll in IT Support Certificate ($500.00 tuition)
            bool enrolled = coordinator.EducationService.TryEnroll("course_it_support_cert", out string err);
            Assert.That(enrolled, Is.True, err);

            // Tuition of $500 deducted
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(2500.00))); // $3,000 - $500

            var tuitionTx = coordinator.Ledger.Transactions.FirstOrDefault(t => t.Category == TransactionCategory.EducationTuition);
            Assert.That(tuitionTx, Is.Not.Null);
            Assert.That(tuitionTx.Amount, Is.EqualTo(Currency.FromDollars(500.00)));

            // Advance 15 days = 30 study hours (50% progress)
            coordinator.FastForwardDays(15);
            Assert.That(coordinator.EducationService.ActiveEnrollment.ProgressNormalized, Is.EqualTo(0.5f));
            Assert.That(coordinator.EducationService.ActiveEnrollment.IsCompleted, Is.False);

            // Advance another 15 days = 60 hours total -> Completed!
            coordinator.FastForwardDays(15);
            Assert.That(coordinator.EducationService.ActiveEnrollment.IsCompleted, Is.True);
            Assert.That(coordinator.EducationService.HasQualification("qual_it_support_cert"), Is.True);
            Assert.That(coordinator.EducationService.GetSkillLevel(SkillType.Technical), Is.EqualTo(25));
            Assert.That(coordinator.EducationService.GetSkillLevel(SkillType.Communication), Is.EqualTo(10));
        }
        #endregion

        #region Scenario 7: Apply for Junior IT Helpdesk job after meeting prerequisites
        [Test]
        public void AuditScenario_07_ApplyForJuniorITHelpdeskAfterMeetingPrerequisites()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            // 1. Without prereqs -> Cannot apply
            Assert.That(coordinator.CareerService.CanApply("job_it_helpdesk", out string reject1), Is.False);
            Assert.That(reject1, Does.Contain("Missing required qualification"));

            // 2. Complete digital course + IT Support cert
            coordinator.EducationService.AddQualification("qual_digital_basics");
            coordinator.EducationService.TryEnroll("course_it_support_cert", out _);
            coordinator.FastForwardDays(30);

            // Pre-reqs met (`qual_it_support_cert` earned, Technical = 25 >= 20)
            Assert.That(coordinator.CareerService.CanApply("job_it_helpdesk", out string err2), Is.True, err2);

            // 3. Apply and get hired
            bool hired = coordinator.CareerService.TryApply("job_it_helpdesk", out string err3);
            Assert.That(hired, Is.True, err3);
            Assert.That(coordinator.CareerService.CurrentEmployment.Job.Title, Is.EqualTo("Junior IT Helpdesk Technician"));
            Assert.That(coordinator.CareerService.CurrentEmployment.Job.MonthlySalary, Is.EqualTo(Currency.FromDollars(2800.00)));
        }
        #endregion

        #region Scenario 8: Take a loan and verify cash, liabilities, interest and repayment schedule
        [Test]
        public void AuditScenario_08_TakeLoanAndVerifyAmortizationSchedule()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);
            coordinator.CareerService.TryApply("job_delivery_worker", out _); // Salary $1,800

            long initialCash = coordinator.Wallet.Balance.MinorUnits; // $3,000.00
            long initialNetWorth = coordinator.FinancialService.NetWorthCents; // $3,000.00

            // Take $5,000 Personal Loan
            bool loanTaken = coordinator.LoanService.TryTakeLoan("loan_personal_5k", out var loan, out string err);
            Assert.That(loanTaken, Is.True, err);

            // Verify Double-Entry Balance Sheet:
            // Cash increases by $5,000 -> $8,000
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(8000.00)));
            // Liabilities increase by $5,000
            Assert.That(coordinator.BalanceSheet.LoanLiabilities, Is.EqualTo(Currency.FromDollars(5000.00)));
            // Net worth remains UNCHANGED ($3,000.00)
            Assert.That(coordinator.FinancialService.NetWorthCents, Is.EqualTo(initialNetWorth));

            // Amortization details:
            Assert.That(loan.MonthlyInstallment, Is.EqualTo(Currency.FromDollars(444.24)));
            var (interest, principal) = loan.CalculateInstallmentSplit();
            Assert.That(interest, Is.EqualTo(Currency.FromDollars(50.00))); // 1% of $5,000
            Assert.That(principal, Is.EqualTo(Currency.FromDollars(394.24))); // $444.24 - $50.00
        }
        #endregion

        #region Scenario 9: Miss a payment and verify financial consequences
        [Test]
        public void AuditScenario_09_MissPaymentAndVerifyFinancialConsequences()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);
            coordinator.CareerService.TryApply("job_delivery_worker", out _);
            coordinator.LoanService.TryTakeLoan("loan_personal_5k", out var loan, out _);

            int initialCreditScore = coordinator.LoanService.CreditScore; // 650

            // Resign job so no Day 30 salary is received, and drain cash below monthly installment ($444.24)
            coordinator.CareerService.ResignCurrentJob();
            coordinator.Wallet.TrySpend(coordinator.Wallet.Balance - Currency.FromDollars(50.00), out _);
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(50.00)));

            // Advance 30 days to loan due date (Month 2 Day 1)
            coordinator.FastForwardDays(30);

            // Payment failed:
            Assert.That(loan.MissedPaymentsCount, Is.EqualTo(1));
            // Overdue loan amount = Installment ($444.24) + Late fee ($25.00) = $469.24
            Assert.That(loan.OverdueAmount, Is.EqualTo(Currency.FromDollars(469.24)));

            // Total balance sheet overdue liabilities combines missed loan ($469.24) + unpaid Month 2 living expenses ($1,270.00) = $1,739.24
            Assert.That(coordinator.BalanceSheet.OverdueLiabilities, Is.EqualTo(Currency.FromDollars(1739.24)));

            // Credit score penalized by 25 points
            Assert.That(coordinator.LoanService.CreditScore, Is.EqualTo(initialCreditScore - 25)); // 625

            // Health status transitions to Insolvent (Liabilities of $6,739.24 exceed $0 assets with negative cash flow)
            Assert.That(coordinator.HealthService.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.Insolvent));
        }
        #endregion

        #region Scenario 10: Fast-forward across multiple days and months
        [Test]
        public void AuditScenario_10_FastForwardAcrossMultipleMonths()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);
            coordinator.CareerService.TryApply("job_delivery_worker", out _); // Salary $1,800 / mo

            // Fast forward 60 days (2 full 30-day months)
            coordinator.FastForwardDays(60);

            Assert.That(coordinator.Clock.CurrentDate.Month, Is.EqualTo(3));
            Assert.That(coordinator.Clock.CurrentDate.Day, Is.EqualTo(1));

            // Verify 2 salary payments credited
            int salaryCount = coordinator.Ledger.Transactions.Count(t => t.Category == TransactionCategory.EmploymentSalary);
            Assert.That(salaryCount, Is.EqualTo(2));

            // Verify living expenses paid for Month 2 and Month 3 (and Month 1 if initial check run)
            int expenseCount = coordinator.Ledger.Transactions.Count(t => t.Type == TransactionType.Expense);
            Assert.That(expenseCount, Is.GreaterThanOrEqualTo(12));
        }
        #endregion

        #region Scenario 11: Save, close, reload and verify all financial and career data
        [Test]
        public void AuditScenario_11_SaveCloseReloadFidelity()
        {
            // 1. Setup Session A
            var coordinatorA = new LifeSimulationCoordinator(_saveService);
            coordinatorA.InitializeSession(GameDifficulty.Standard);
            coordinatorA.CareerService.TryApply("job_delivery_worker", out _);
            coordinatorA.EducationService.TryEnroll("course_free_digital", out _);
            coordinatorA.LoanService.TryTakeLoan("loan_personal_5k", out var loanA, out _);

            coordinatorA.FastForwardDays(12);

            // Save state
            var savedState = coordinatorA.SaveCurrentState();
            Assert.That(File.Exists(_saveService.PrimaryPath), Is.True);

            // 2. Reload Session B
            var coordinatorB = new LifeSimulationCoordinator(_saveService);
            coordinatorB.InitializeSession();

            // Verify exact calendar time
            Assert.That(coordinatorB.Clock.CurrentDate.TotalMinutes, Is.EqualTo(coordinatorA.Clock.CurrentDate.TotalMinutes));
            Assert.That(coordinatorB.Clock.CurrentDate.Day, Is.EqualTo(13));

            // Verify cash, liabilities, and net worth
            Assert.That(coordinatorB.Wallet.Balance, Is.EqualTo(coordinatorA.Wallet.Balance));
            Assert.That(coordinatorB.BalanceSheet.LoanLiabilities, Is.EqualTo(coordinatorA.BalanceSheet.LoanLiabilities));
            Assert.That(coordinatorB.FinancialService.NetWorthCents, Is.EqualTo(coordinatorA.FinancialService.NetWorthCents));

            // Verify career
            Assert.That(coordinatorB.CareerService.IsEmployed, Is.True);
            Assert.That(coordinatorB.CareerService.CurrentEmployment.Job.Id, Is.EqualTo("job_delivery_worker"));

            // Verify education (12 days * 2h = 24h >= 20h required -> Course completed)
            Assert.That(coordinatorB.EducationService.HasQualification("qual_digital_basics"), Is.True);

            // Verify loan
            Assert.That(coordinatorB.LoanService.ActiveLoans.Count, Is.EqualTo(1));
            Assert.That(coordinatorB.LoanService.ActiveLoans[0].RemainingPrincipal, Is.EqualTo(loanA.RemainingPrincipal));
        }
        #endregion

        #region Scenario 12: Test Hard, Standard and Comfortable starting conditions
        [Test]
        public void AuditScenario_12_TestAllStartingConditions()
        {
            // Hard: $1,000 cash
            var saveHard = new SaveDataService(Path.Combine(_testDir, "Hard"));
            var coordHard = new LifeSimulationCoordinator(saveHard);
            coordHard.InitializeSession(GameDifficulty.Hard);
            Assert.That(coordHard.Wallet.Balance, Is.EqualTo(Currency.FromDollars(1000.00)));
            Assert.That(coordHard.HealthService.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.FinanciallyStressed));

            // Standard: $3,000 cash
            var saveStd = new SaveDataService(Path.Combine(_testDir, "Std"));
            var coordStd = new LifeSimulationCoordinator(saveStd);
            coordStd.InitializeSession(GameDifficulty.Standard);
            Assert.That(coordStd.Wallet.Balance, Is.EqualTo(Currency.FromDollars(3000.00)));

            // Comfortable: $5,000 cash
            var saveComf = new SaveDataService(Path.Combine(_testDir, "Comf"));
            var coordComf = new LifeSimulationCoordinator(saveComf);
            coordComf.InitializeSession(GameDifficulty.Comfortable);
            Assert.That(coordComf.Wallet.Balance, Is.EqualTo(Currency.FromDollars(5000.00)));
        }
        #endregion

        #region Additional Check 1: Can a player survive the first two game months?
        [Test]
        public void AdditionalCheck_01_PlayerSurvivesFirstTwoGameMonths()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard); // $3,000

            // Day 1: Deduct Month 1 expenses ($1,320) -> Cash = $1,680
            coordinator.ExpenseService.CheckScheduledExpenses(coordinator.Clock.CurrentDate);
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(1680.00)));

            // Player gets Delivery Courier job ($1,800/mo)
            coordinator.CareerService.TryApply("job_delivery_worker", out _);

            // Advance through Month 1 (Day 30 salary of $1,800 arrives)
            // Balance: $1,680 + $1,800 = $3,480.00
            coordinator.FastForwardDays(29);
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(3480.00)));

            // Advance to Month 2 Day 1: Month 2 expenses deducted ($1,320)
            // Balance: $3,480 - $1,320 = $2,160.00
            coordinator.FastForwardDays(1);
            Assert.That(coordinator.Clock.CurrentDate.Month, Is.EqualTo(2));
            Assert.That(coordinator.Clock.CurrentDate.Day, Is.EqualTo(1));
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(2160.00)));

            // Advance to Month 2 Day 30: Month 2 salary arrives ($1,800)
            // Balance: $2,160 + $1,800 = $3,960.00
            coordinator.FastForwardDays(29);
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(3960.00)));

            // Player survived 2 months, cash grew by +$960 (2 * $480 surplus), and health is Healthy!
            Assert.That(coordinator.HealthService.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.Healthy));
        }
        #endregion

        #region Additional Check 2: Can a player study while employed?
        [Test]
        public void AdditionalCheck_02_StudyWhileEmployed()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            // Employed 40h/wk as Retail Assistant
            coordinator.CareerService.TryApply("job_retail_assistant", out _);
            // Enrolled in Digital Skills course
            coordinator.EducationService.TryEnroll("course_free_digital", out _);

            // Advance 10 days
            coordinator.FastForwardDays(10);

            // Course completed without interfering with employment
            Assert.That(coordinator.EducationService.ActiveEnrollment.IsCompleted, Is.True);
            Assert.That(coordinator.CareerService.IsEmployed, Is.True);
        }
        #endregion

        #region Additional Check 3: Can the player change jobs without duplicating salary?
        [Test]
        public void AdditionalCheck_03_ChangeJobsWithoutDuplicatingSalary()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            // Hire Retail Assistant ($1,600/mo)
            coordinator.CareerService.TryApply("job_retail_assistant", out _);

            // Advance to Day 30: Salary paid ($1,600)
            coordinator.FastForwardDays(29);
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(4600.00))); // $3,000 + $1,600
            Assert.That(coordinator.Ledger.Transactions.Count(t => t.Category == TransactionCategory.EmploymentSalary), Is.EqualTo(1));

            // Change jobs on Day 30 to Delivery Courier ($1,800/mo)
            coordinator.CareerService.TryApply("job_delivery_worker", out _);

            // Verify salary is NOT paid a second time on Day 30!
            coordinator.FastForwardDays(0); // Tick Day 30 again
            Assert.That(coordinator.Ledger.Transactions.Count(t => t.Category == TransactionCategory.EmploymentSalary), Is.EqualTo(1));
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(4600.00)));
        }
        #endregion

        #region Additional Check 4: Can the player repay a loan early?
        [Test]
        public void AdditionalCheck_04_RepayLoanEarly()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);
            coordinator.CareerService.TryApply("job_delivery_worker", out _);
            coordinator.LoanService.TryTakeLoan("loan_personal_5k", out var loan, out _);

            Assert.That(loan.RemainingPrincipal, Is.EqualTo(Currency.FromDollars(5000.00)));
            int scoreBefore = coordinator.LoanService.CreditScore;

            // Repay early in full
            bool repaid = coordinator.LoanService.TryRepayLoanEarly(loan.LoanId, out string err);
            Assert.That(repaid, Is.True, err);

            Assert.That(loan.IsFullyPaid, Is.True);
            Assert.That(coordinator.BalanceSheet.LoanLiabilities, Is.EqualTo(Currency.Zero));
            Assert.That(coordinator.LoanService.CreditScore, Is.EqualTo(scoreBefore + 15)); // Credit score bonus
        }
        #endregion

        #region Additional Check 5: Can a player recover from a cash crisis?
        [Test]
        public void AdditionalCheck_05_RecoverFromCashCrisis()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);
            coordinator.CareerService.TryApply("job_retail_assistant", out _);

            // Force Cash Crisis by adding overdue liability
            coordinator.FinancialService.AddOverdueLiability(Currency.FromDollars(500.00), TransactionCategory.LatePenaltyFee, "Late Bill");
            Assert.That(coordinator.HealthService.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.CashCrisis));

            // Settle overdue debt
            bool settled = coordinator.FinancialService.TrySettleOverdueDebt(Currency.FromDollars(500.00), out string err);
            Assert.That(settled, Is.True, err);
            Assert.That(coordinator.BalanceSheet.OverdueLiabilities, Is.EqualTo(Currency.Zero));

            // Status recovers back to Healthy!
            Assert.That(coordinator.HealthService.EvaluateStatus().State, Is.EqualTo(FinancialHealthState.Healthy));
        }
        #endregion

        #region Additional Check 6: Are all transactions reflected in the financial ledger?
        [Test]
        public void AdditionalCheck_06_FinancialLedgerCompleteness()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            // Execute income, expense, and loan disbursement
            coordinator.FinancialService.RecordIncome(Currency.FromDollars(500.00), TransactionCategory.Miscellaneous, "Side Gig");
            coordinator.FinancialService.TryRecordExpense(Currency.FromDollars(50.00), TransactionCategory.LivingPersonal, "Supplies", out _, out _);

            Assert.That(coordinator.Ledger.Transactions.Count, Is.EqualTo(2));
            Assert.That(coordinator.Ledger.Transactions[0].Category, Is.EqualTo(TransactionCategory.Miscellaneous));
            Assert.That(coordinator.Ledger.Transactions[0].Type, Is.EqualTo(TransactionType.Income));
            Assert.That(coordinator.Ledger.Transactions[1].Category, Is.EqualTo(TransactionCategory.LivingPersonal));
            Assert.That(coordinator.Ledger.Transactions[1].Type, Is.EqualTo(TransactionType.Expense));
        }
        #endregion

        #region Additional Check 7: Can recurring expenses or repayments execute twice after reload?
        [Test]
        public void AdditionalCheck_07_NoDuplicateExecutionAfterReload()
        {
            var coordinatorA = new LifeSimulationCoordinator(_saveService);
            coordinatorA.InitializeSession(GameDifficulty.Standard);

            // Day 1: Deduct Month 1 living expenses
            coordinatorA.ExpenseService.CheckScheduledExpenses(coordinatorA.Clock.CurrentDate);
            Assert.That(coordinatorA.Ledger.Transactions.Count(t => t.Type == TransactionType.Expense), Is.EqualTo(6));

            // Save state on Day 1
            coordinatorA.SaveCurrentState();

            // Reload Session B on Day 1
            var coordinatorB = new LifeSimulationCoordinator(_saveService);
            coordinatorB.InitializeSession();

            // Attempt to trigger Day 1 expenses on reload
            coordinatorB.ExpenseService.CheckScheduledExpenses(coordinatorB.Clock.CurrentDate);

            // Must NOT double-deduct expenses!
            Assert.That(coordinatorB.Ledger.Transactions.Count(t => t.Type == TransactionType.Expense), Is.EqualTo(0)); // Clean ledger in B, no new deductions!
            Assert.That(coordinatorB.Wallet.Balance, Is.EqualTo(Currency.FromDollars(1680.00))); // Remains $1,680!
        }
        #endregion

        #region Additional Check 8: Does the game remain playable when cash becomes insufficient?
        [Test]
        public void AdditionalCheck_08_PlayableWhenCashInsufficient()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            // Spend almost all cash, leaving only $20.00
            coordinator.Wallet.TrySpend(Currency.FromDollars(2980.00), out _);
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(20.00)));

            // Attempt rent deduction ($600.00)
            coordinator.ExpenseService.CheckScheduledExpenses(coordinator.Clock.CurrentDate);

            // Game does not crash! Wallet does not go negative!
            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(20.00)));
            // Overdue debt recorded
            Assert.That(coordinator.BalanceSheet.OverdueLiabilities.MinorUnits, Is.GreaterThan(0));

            // Clock continues ticking deterministically
            coordinator.Clock.Tick(60.0);
            Assert.That(coordinator.Clock.CurrentDate.Hour, Is.EqualTo(9));
        }
        #endregion
    }
}

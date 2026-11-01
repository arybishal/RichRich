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
    public class Milestone4Tests
    {
        private string _tempDir;
        private SaveDataService _saveService;
        private LifeSimulationCoordinator _coordinator;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "RichRich_M4_Tests_" + Guid.NewGuid().ToString("N"));
            _saveService = new SaveDataService(_tempDir);
            _coordinator = new LifeSimulationCoordinator(_saveService);
            _coordinator.InitializeSession(GameDifficulty.Standard); // Starts with $3,000 cash
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }

        #region 1. Business Ownership & Valuation Tests

        [Test]
        public void BusinessMarketplace_Catalog_HasThreeDistinctCommercialEnterprises()
        {
            var catalog = _coordinator.BusinessService.Catalog;
            Assert.That(catalog.Count, Is.EqualTo(3));
            Assert.That(catalog.Any(b => b.Id == "auto_repair"), Is.True);
            Assert.That(catalog.Any(b => b.Id == "convenience_store"), Is.True);
            Assert.That(catalog.Any(b => b.Id == "digital_agency"), Is.True);
        }

        [Test]
        public void BusinessMarketplace_InsufficientFunds_RejectsAcquisitionSafely()
        {
            // Auto repair costs $5,499 base + $500 setup = $5,999. Starting cash is $3,000.
            var biz = _coordinator.BusinessService.GetBusiness("auto_repair");
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(3000.00)));

            bool success = _coordinator.BusinessService.TryPurchase("auto_repair", out string error);
            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("Insufficient funds"));
            Assert.That(biz.IsOwned, Is.False);
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(Currency.FromDollars(3000.00)));
            Assert.That(_coordinator.BalanceSheet.NonCashAssets, Is.EqualTo(Currency.Zero));
        }

        [Test]
        public void BusinessMarketplace_SufficientFunds_AcquiresBusinessAndUpdatesBalanceSheet()
        {
            // Inject capital to afford Auto Repair ($5,999 total)
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(10000.00), TransactionCategory.StartingCapital, "Investor Grant");
            var cashBefore = _coordinator.Wallet.Balance;

            bool success = _coordinator.BusinessService.TryPurchase("auto_repair", out string error);
            Assert.That(success, Is.True, error);

            var biz = _coordinator.BusinessService.GetBusiness("auto_repair");
            Assert.That(biz.IsOwned, Is.True);
            Assert.That(biz.IsOpen, Is.True);
            Assert.That(biz.Level, Is.EqualTo(1));

            // Verify cash deducted
            Currency expectedCost = Currency.FromDollars(5999.00);
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBefore - expectedCost));

            // Verify Balance Sheet: NonCashAssets holds the business valuation
            Currency valuation = biz.CalculateValuation();
            Assert.That(valuation.MinorUnits, Is.GreaterThan(0));
            Assert.That(_coordinator.BalanceSheet.NonCashAssets, Is.EqualTo(valuation));
        }

        [Test]
        public void BusinessDashboard_Upgrade_IncreasesFacilityLevelAndCapacity()
        {
            // Acquire Auto Repair
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(15000.00), TransactionCategory.StartingCapital, "Capital Injection");
            _coordinator.BusinessService.TryPurchase("auto_repair", out _);
            var biz = _coordinator.BusinessService.GetBusiness("auto_repair");

            Assert.That(biz.Level, Is.EqualTo(1));
            Assert.That(_coordinator.BusinessService.AutoRepairBays.Count, Is.EqualTo(1));

            // Upgrade to Level 2
            bool upgraded = _coordinator.BusinessService.TryUpgrade("auto_repair", out string error);
            Assert.That(upgraded, Is.True, error);
            Assert.That(biz.Level, Is.EqualTo(2));
            Assert.That(biz.Reputation, Is.EqualTo(65)); // 60 initial + 5 upgrade
            Assert.That(_coordinator.BusinessService.AutoRepairBays.Count, Is.EqualTo(2)); // Service bay added
        }

        [Test]
        public void BusinessDashboard_Sell_LiquidatesEnterpriseAndConvertsValuationToCash()
        {
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(15000.00), TransactionCategory.StartingCapital, "Capital Injection");
            _coordinator.BusinessService.TryPurchase("auto_repair", out _);
            var biz = _coordinator.BusinessService.GetBusiness("auto_repair");

            Currency valuation = biz.CalculateValuation();
            var cashBeforeSale = _coordinator.Wallet.Balance;

            bool sold = _coordinator.BusinessService.TrySell("auto_repair", out Currency proceeds, out string error);
            Assert.That(sold, Is.True, error);
            Assert.That(proceeds, Is.EqualTo(valuation));
            Assert.That(biz.IsOwned, Is.False);
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBeforeSale + proceeds));
            Assert.That(_coordinator.BalanceSheet.NonCashAssets, Is.EqualTo(Currency.Zero));
        }

        #endregion

        #region 2. Auto Repair Workshop Mechanics

        [Test]
        public void AutoRepair_JobPrerequisites_EnforcesFacilityLevel()
        {
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(10000.00), TransactionCategory.StartingCapital, "Capital Injection");
            _coordinator.BusinessService.TryPurchase("auto_repair", out _);

            // job_engine_1 requires Level 3. Workshop is Level 1.
            bool started = _coordinator.BusinessService.TryStartRepairJob("job_engine_1", 1, out string error);
            Assert.That(started, Is.False);
            Assert.That(error, Does.Contain("Level 3 required"));
        }

        [Test]
        public void AutoRepair_RepairLifecycle_AssignsAdvancesAndCollectsPayment()
        {
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(10000.00), TransactionCategory.StartingCapital, "Capital Injection");
            _coordinator.BusinessService.TryPurchase("auto_repair", out _);

            // Assign oil change job ($25 parts, $90 payout, 60 minutes) to Bay 1
            bool started = _coordinator.BusinessService.TryStartRepairJob("job_oil_1", 1, out string error);
            Assert.That(started, Is.True, error);

            var bay = _coordinator.BusinessService.AutoRepairBays.First(b => b.BayId == 1);
            Assert.That(bay.IsOccupied, Is.True);
            Assert.That(bay.CurrentJob.IsCompleted, Is.False);

            // Cannot claim before repair finishes
            bool earlyClaim = _coordinator.BusinessService.TryClaimCompletedRepair(1, out _, out string earlyErr);
            Assert.That(earlyClaim, Is.False);
            Assert.That(earlyErr, Does.Contain("not completed"));

            // Advance simulation clock by 1 hour (60 minutes) to finish repair
            _coordinator.Clock.AdvanceMinutes(60);
            Assert.That(bay.CurrentJob.IsCompleted, Is.True);

            // Claim vehicle delivery payout
            var cashBefore = _coordinator.Wallet.Balance;
            bool claimed = _coordinator.BusinessService.TryClaimCompletedRepair(1, out Currency payout, out string claimErr);
            Assert.That(claimed, Is.True, claimErr);
            Assert.That(payout, Is.EqualTo(Currency.FromDollars(90.00)));
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBefore + payout));
            Assert.That(bay.IsOccupied, Is.False); // Bay freed
        }

        #endregion

        #region 3. Convenience Store Retail Mechanics

        [Test]
        public void ConvenienceStore_WholesaleOrdering_DeductsCostAndIncreasesStock()
        {
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(20000.00), TransactionCategory.StartingCapital, "Capital Injection");
            _coordinator.BusinessService.TryPurchase("convenience_store", out _);

            var beverages = _coordinator.BusinessService.ConvenienceStoreCatalog.First(p => p.Id == "inv_beverages");
            int initialStock = beverages.StockOnHand;
            var cashBefore = _coordinator.Wallet.Balance;

            // Order 30 units at $1.20 wholesale = $36.00
            bool ordered = _coordinator.BusinessService.OrderWholesaleStock("inv_beverages", 30, out Currency cost, out string error);
            Assert.That(ordered, Is.True, error);
            Assert.That(cost, Is.EqualTo(Currency.FromDollars(36.00)));
            Assert.That(beverages.StockOnHand, Is.EqualTo(initialStock + 30));
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBefore - cost));
        }

        [Test]
        public void ConvenienceStore_PriceAdjustment_RejectsNegativeOrZeroPrice()
        {
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(20000.00), TransactionCategory.StartingCapital, "Capital Injection");
            _coordinator.BusinessService.TryPurchase("convenience_store", out _);

            Assert.Throws<ArgumentException>(() =>
            {
                _coordinator.BusinessService.SetProductSellingPrice("inv_beverages", Currency.Zero);
            });

            _coordinator.BusinessService.SetProductSellingPrice("inv_beverages", Currency.FromDollars(3.50));
            var prod = _coordinator.BusinessService.ConvenienceStoreCatalog.First(p => p.Id == "inv_beverages");
            Assert.That(prod.SellingPrice, Is.EqualTo(Currency.FromDollars(3.50)));
        }

        [Test]
        public void ConvenienceStore_DailySalesSimulation_CalculatesRevenueCOGSAndGrossProfit()
        {
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(20000.00), TransactionCategory.StartingCapital, "Capital Injection");
            _coordinator.BusinessService.TryPurchase("convenience_store", out _);

            var store = _coordinator.BusinessService.GetBusiness("convenience_store");
            var cashBefore = _coordinator.Wallet.Balance;

            // Advance by 1 day to trigger store retail sales and daily overhead
            _coordinator.Clock.AdvanceDays(1);

            // Store earned revenue and recorded P&L
            Assert.That(store.PAndL.TotalRevenue.MinorUnits, Is.GreaterThan(0));
            Assert.That(store.PAndL.CostOfGoodsSold.MinorUnits, Is.GreaterThan(0));
            Assert.That(store.PAndL.GrossProfit.MinorUnits, Is.GreaterThan(0));
        }

        #endregion

        #region 4. Digital Marketing Agency Mechanics

        [Test]
        public void DigitalAgency_ContractAcceptanceAndHoursContribution()
        {
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(35000.00), TransactionCategory.StartingCapital, "Capital Injection");
            _coordinator.BusinessService.TryPurchase("digital_agency", out _);

            // Accept social media project (20 hours required, $1,500 contract payout)
            bool accepted = _coordinator.BusinessService.TryAcceptClientProject("proj_social_1", out string error);
            Assert.That(accepted, Is.True, error);

            var project = _coordinator.BusinessService.AgencyProjects.First(p => p.ProjectId == "proj_social_1");
            Assert.That(project.IsAccepted, Is.True);
            Assert.That(project.CompletedHours, Is.EqualTo(0));

            // Contribute 10 hours
            _coordinator.BusinessService.ContributeAgencyWorkHours("proj_social_1", 10);
            Assert.That(project.CompletedHours, Is.EqualTo(10));
            Assert.That(project.ProgressNormalized, Is.EqualTo(0.5f));
            Assert.That(project.IsCompleted, Is.False);

            // Contribute remaining 10 hours
            _coordinator.BusinessService.ContributeAgencyWorkHours("proj_social_1", 10);
            Assert.That(project.CompletedHours, Is.EqualTo(20));
            Assert.That(project.IsCompleted, Is.True);

            // Claim completed payout
            var cashBefore = _coordinator.Wallet.Balance;
            bool claimed = _coordinator.BusinessService.ClaimCompletedProject("proj_social_1", out Currency payout, out string claimErr);
            Assert.That(claimed, Is.True, claimErr);
            Assert.That(payout, Is.EqualTo(Currency.FromDollars(1500.00)));
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBefore + payout));
        }

        #endregion

        #region 5. Employee Staffing & Payroll Operations

        [Test]
        public void EmployeeStaffing_HireAndScheduledMonthlyPayroll()
        {
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(20000.00), TransactionCategory.StartingCapital, "Capital Injection");
            _coordinator.BusinessService.TryPurchase("auto_repair", out _);

            var biz = _coordinator.BusinessService.GetBusiness("auto_repair");
            Assert.That(biz.EmployeeCount, Is.EqualTo(0));

            // Hire apprentice mechanic ($1,800/mo salary)
            bool hired = _coordinator.BusinessService.TryHireEmployee("auto_repair", "emp_mech_1", out string error);
            Assert.That(hired, Is.True, error);
            Assert.That(biz.EmployeeCount, Is.EqualTo(1));
            Assert.That(biz.MonthlyPayroll, Is.EqualTo(Currency.FromDollars(1800.00)));

            // Advance to day 30 to trigger payroll execution
            int daysUntil30 = 30 - _coordinator.Clock.CurrentDate.Day;
            if (daysUntil30 > 0)
            {
                _coordinator.Clock.AdvanceDays(daysUntil30);
            }

            // Verify payroll was deducted and recorded in P&L
            Assert.That(biz.PAndL.EmployeePayroll, Is.EqualTo(Currency.FromDollars(1800.00)));

            // Dismiss employee
            bool dismissed = _coordinator.BusinessService.TryDismissEmployee("auto_repair", "emp_mech_1", out string dismissErr);
            Assert.That(dismissed, Is.True, dismissErr);
            Assert.That(biz.EmployeeCount, Is.EqualTo(0));
            Assert.That(biz.MonthlyPayroll, Is.EqualTo(Currency.Zero));
        }

        #endregion

        #region 6. Commercial Loans & Credit

        [Test]
        public void CommercialLoans_UnderwritingAndDisbursement()
        {
            // Starter commercial loan requires at least one owned business
            bool eligibleBeforeOwnership = _coordinator.LoanService.CheckEligibility("loan_business_starter", out string reason);
            Assert.That(eligibleBeforeOwnership, Is.False);
            Assert.That(reason, Does.Contain("require at least one active operating business"));

            // Acquire business
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(15000.00), TransactionCategory.StartingCapital, "Grant");
            _coordinator.BusinessService.TryPurchase("auto_repair", out _);

            // Set career salary to qualify minimum income ($2,100/mo)
            _coordinator.CareerService.TryApply("job_sales_assistant", out _);

            bool eligibleAfter = _coordinator.LoanService.CheckEligibility("loan_business_starter", out string rejectReason);
            Assert.That(eligibleAfter, Is.True, rejectReason);

            // Take $15,000 commercial loan
            var cashBefore = _coordinator.Wallet.Balance;
            bool disbursed = _coordinator.LoanService.TryTakeLoan("loan_business_starter", out ActiveLoan loan, out string loanErr);
            Assert.That(disbursed, Is.True, loanErr);
            Assert.That(loan.InitialPrincipal, Is.EqualTo(Currency.FromDollars(15000.00)));
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBefore + Currency.FromDollars(15000.00)));
            Assert.That(_coordinator.BalanceSheet.LoanLiabilities, Is.EqualTo(Currency.FromDollars(15000.00)));

            // Early full repayment
            bool repaid = _coordinator.LoanService.TryRepayLoanEarly(loan.LoanId, out string repayErr);
            Assert.That(repaid, Is.True, repayErr);
            Assert.That(loan.IsFullyPaid, Is.True);
            Assert.That(_coordinator.BalanceSheet.LoanLiabilities, Is.EqualTo(Currency.Zero));
        }

        #endregion

        #region 7. Presentation Controllers Integration

        [Test]
        public void PresentationControllers_EndToEndWorkflow()
        {
            var marketCtrl = new BusinessMarketplaceController(_coordinator);
            var dashCtrl = new BusinessDashboardController(_coordinator);
            var autoCtrl = new AutoRepairController(_coordinator);
            var storeCtrl = new ConvenienceStoreController(_coordinator);
            var agencyCtrl = new DigitalAgencyController(_coordinator);
            var empCtrl = new BusinessEmployeeController(_coordinator);
            var finCtrl = new BusinessFinanceController(_coordinator);

            // Provide capital
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(30000.00), TransactionCategory.StartingCapital, "Angel Investment");

            // 1. Marketplace purchase
            marketCtrl.SelectBusiness("auto_repair");
            bool purchased = marketCtrl.TryPurchaseSelected(out string mErr);
            Assert.That(purchased, Is.True, mErr);

            // 2. Dashboard verification
            dashCtrl.SelectBusiness("auto_repair");
            Assert.That(dashCtrl.HasOwnedBusinesses, Is.True);
            Assert.That(dashCtrl.SelectedBusiness.Id, Is.EqualTo("auto_repair"));

            // 3. Auto Repair Workshop
            Assert.That(autoCtrl.IsOwned, Is.True);
            bool jobAssigned = autoCtrl.TryAssignJobToBay("job_oil_1", 1, out string aErr);
            Assert.That(jobAssigned, Is.True, aErr);

            // 4. Staffing
            empCtrl.SelectBusiness("auto_repair");
            bool hired = empCtrl.TryHireCandidate("emp_mech_1", out string hErr);
            Assert.That(hired, Is.True, hErr);

            // 5. Finance
            finCtrl.Refresh();
            Assert.That(finCtrl.TotalBusinessValuation.MinorUnits, Is.GreaterThan(0));
        }

        #endregion

        #region 8. Schema Version 4 Persistence & Migration

        [Test]
        public void Persistence_SchemaV4_SavesAndRestoresAllCommercialSystems()
        {
            _coordinator.FinancialService.RecordIncome(Currency.FromDollars(50000.00), TransactionCategory.StartingCapital, "Venture Capital");

            // Acquire Auto Repair & Convenience Store
            _coordinator.BusinessService.TryPurchase("auto_repair", out _);
            _coordinator.BusinessService.TryUpgrade("auto_repair", out _); // Level 2
            _coordinator.BusinessService.TryPurchase("convenience_store", out _);

            // Hire an employee
            _coordinator.BusinessService.TryHireEmployee("auto_repair", "emp_mech_1", out _);

            // Stock inventory
            _coordinator.BusinessService.OrderWholesaleStock("inv_beverages", 40, out _, out _);

            // Save state
            var saved = _coordinator.SaveCurrentState();
            Assert.That(saved.SchemaVersion, Is.EqualTo(4));
            Assert.That(saved.Businesses.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(saved.Employees.Count, Is.EqualTo(1));
            Assert.That(saved.StoreInventory.Count, Is.GreaterThan(0));

            // Reload fresh coordinator
            var reloaded = new LifeSimulationCoordinator(_saveService);
            reloaded.InitializeSession();

            var autoBiz = reloaded.BusinessService.GetBusiness("auto_repair");
            Assert.That(autoBiz.IsOwned, Is.True);
            Assert.That(autoBiz.Level, Is.EqualTo(2));
            Assert.That(autoBiz.Employees.Count, Is.EqualTo(1));
            Assert.That(autoBiz.Employees[0].Name, Is.EqualTo("Trevor Knox"));

            var storeBiz = reloaded.BusinessService.GetBusiness("convenience_store");
            Assert.That(storeBiz.IsOwned, Is.True);

            var beverages = reloaded.BusinessService.ConvenienceStoreCatalog.First(p => p.Id == "inv_beverages");
            Assert.That(beverages.StockOnHand, Is.EqualTo(90)); // 50 initial + 40 stocked
        }

        [Test]
        public void Persistence_Migration_SmoothlyLoadsLegacySchema3Save()
        {
            // Create legacy Schema 3 save file directly
            var legacySave = new SaveData
            {
                SchemaVersion = 3,
                CreatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Difficulty = GameDifficulty.Comfortable.ToString(),
                SimulationMinutes = GameDateTime.DefaultStart.TotalMinutes,
                BalanceCents = 800000L, // $8,000
                LifetimeEarnedCents = 800000L,
                IsBusinessOwned = true,
                BusinessLevel = 2,
                BusinessTasksCompleted = 5
            };

            _saveService.Save(legacySave);

            // Load into coordinator
            var reloaded = new LifeSimulationCoordinator(_saveService);
            reloaded.InitializeSession();

            var autoBiz = reloaded.BusinessService.GetBusiness("auto_repair");
            Assert.That(autoBiz.IsOwned, Is.True);
            Assert.That(autoBiz.Level, Is.EqualTo(2));
            Assert.That(autoBiz.CompletedTasksCount, Is.EqualTo(5));
            Assert.That(reloaded.BalanceSheet.NonCashAssets.MinorUnits, Is.GreaterThan(0));
        }

        #endregion
    }
}

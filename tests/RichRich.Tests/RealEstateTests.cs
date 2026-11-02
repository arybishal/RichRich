using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;
using RichRich.Domain.Services;

namespace RichRich.Tests
{
    [TestFixture]
    public class RealEstateTests
    {
        private string _testDir;
        private SaveDataService _saveService;
        private LifeSimulationCoordinator _coordinator;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "RichRichRealEstateTests_" + Guid.NewGuid().ToString("N"));
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
                try { Directory.Delete(_testDir, true); } catch { }
            }
        }

        [Test]
        public void Marketplace_Contains_Default_Five_Properties()
        {
            var props = _coordinator.RealEstateService.AllProperties;
            Assert.That(props.Count, Is.EqualTo(5));

            var starter = _coordinator.RealEstateService.GetProperty("prop_starter_home");
            Assert.That(starter, Is.Not.Null);
            Assert.That(starter.Type, Is.EqualTo(PropertyType.ResidentialStarter));
            Assert.That(starter.PurchasePrice, Is.EqualTo(Currency.FromDollars(65000)));

            var plaza = _coordinator.RealEstateService.GetProperty("prop_commercial_plaza");
            Assert.That(plaza, Is.Not.Null);
            Assert.That(plaza.Type, Is.EqualTo(PropertyType.CommercialPlaza));
            Assert.That(plaza.PurchasePrice, Is.EqualTo(Currency.FromDollars(2200000)));
        }

        [Test]
        public void BuyProperty_OutrightCash_DeductsFunds_UpdatesBalanceSheet_RecordsTransaction()
        {
            // Give player sufficient capital
            _coordinator.Wallet.Add(Currency.FromDollars(100000.00));
            Currency initialCash = _coordinator.Wallet.Balance;

            var prop = _coordinator.RealEstateService.GetProperty("prop_starter_home");
            bool success = _coordinator.RealEstateService.BuyProperty(prop.Id, withMortgage: false, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(prop.IsOwned, Is.True);
            Assert.That(prop.Mortgage, Is.Null);
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(initialCash - prop.PurchasePrice));

            // Verify Balance Sheet
            Assert.That(_coordinator.BalanceSheet.RealEstateAssets, Is.EqualTo(prop.CurrentValuation));
            Assert.That(_coordinator.BalanceSheet.MortgageLiabilities, Is.EqualTo(Currency.Zero));

            // Verify Ledger
            var tx = _coordinator.Ledger.Transactions.LastOrDefault(t => t.RelatedEntityId == prop.Id);
            Assert.That(tx, Is.Not.Null);
            Assert.That(tx.Amount, Is.EqualTo(prop.PurchasePrice));
            Assert.That(tx.Type, Is.EqualTo(TransactionType.AssetAcquisition));
        }

        [Test]
        public void BuyProperty_WithMortgage_RequiresOnlyDownPayment_CreatesMortgageLiability()
        {
            // Give player down payment funds
            _coordinator.Wallet.Add(Currency.FromDollars(20000.00));
            var prop = _coordinator.RealEstateService.GetProperty("prop_starter_home");
            Currency downPayment = prop.DownPaymentAmount;

            bool success = _coordinator.RealEstateService.BuyProperty(prop.Id, withMortgage: true, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(prop.IsOwned, Is.True);
            Assert.That(prop.Mortgage, Is.Not.Null);
            Assert.That(prop.Mortgage.RemainingPrincipal, Is.EqualTo(prop.PurchasePrice - downPayment));

            // Verify Balance Sheet
            Assert.That(_coordinator.BalanceSheet.RealEstateAssets, Is.EqualTo(prop.CurrentValuation));
            Assert.That(_coordinator.BalanceSheet.MortgageLiabilities, Is.EqualTo(prop.Mortgage.RemainingPrincipal));
        }

        [Test]
        public void Mortgage_MonthlyAmortization_ReducesPrincipal_RecordsSplit()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(50000.00));
            var prop = _coordinator.RealEstateService.GetProperty("prop_starter_home");
            _coordinator.RealEstateService.BuyProperty(prop.Id, withMortgage: true, out _);

            var mortgage = prop.Mortgage;
            Currency initialPrincipal = mortgage.RemainingPrincipal;
            Currency payment = mortgage.MonthlyPayment;

            mortgage.ProcessMonthlyPayment(out Currency principalPart, out Currency interestPart);

            Assert.That(principalPart + interestPart, Is.EqualTo(payment));
            Assert.That(mortgage.RemainingPrincipal, Is.EqualTo(initialPrincipal - principalPart));
            Assert.That(mortgage.PaymentsMade, Is.EqualTo(1));
            Assert.That(mortgage.MonthsRemaining, Is.EqualTo(mortgage.TermMonths - 1));
        }

        [Test]
        public void SignTenant_CollectsRent_OnMonthlyCycle()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(80000.00));
            var prop = _coordinator.RealEstateService.GetProperty("prop_starter_home");
            _coordinator.RealEstateService.BuyProperty(prop.Id, withMortgage: false, out _);

            bool signed = _coordinator.RealEstateService.SignTenant(prop.Id, prop.BaseMonthlyRent, out string error);
            Assert.That(signed, Is.True, error);
            Assert.That(prop.IsRented, Is.True);

            Currency cashBefore = _coordinator.Wallet.Balance;

            // Trigger monthly cycle
            _coordinator.RealEstateService.ProcessMonthlyCycle(1.0);

            // Net change: Rent collected - routine maintenance
            Currency expectedNetChange = prop.TenantMonthlyRent - prop.MonthlyMaintenanceCost;
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBefore + expectedNetChange));
        }

        [Test]
        public void Renovation_RestoresCondition_CostsThreeMonthsMaintenance()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(80000.00));
            var prop = _coordinator.RealEstateService.GetProperty("prop_starter_home");
            _coordinator.RealEstateService.BuyProperty(prop.Id, withMortgage: false, out _);

            prop.DegradeCondition(40.0);
            double degradedCondition = prop.Condition;

            Currency cost = Currency.FromCents(prop.MonthlyMaintenanceCost.MinorUnits * 3L);
            Currency cashBefore = _coordinator.Wallet.Balance;

            bool renovated = _coordinator.RealEstateService.RenovateProperty(prop.Id, out string error);

            Assert.That(renovated, Is.True, error);
            Assert.That(prop.Condition, Is.GreaterThan(degradedCondition));
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBefore - cost));
        }

        [Test]
        public void LiquidateProperty_PaysOffMortgage_DeductsAgentFee_DepositsNetProceeds()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(50000.00));
            var prop = _coordinator.RealEstateService.GetProperty("prop_starter_home");
            _coordinator.RealEstateService.BuyProperty(prop.Id, withMortgage: true, out _);

            Currency cashBefore = _coordinator.Wallet.Balance;
            long valuation = prop.CurrentValuation.MinorUnits;
            long agentFee = (long)(valuation * 0.05);
            long mortgageBalance = prop.Mortgage.RemainingPrincipal.MinorUnits;
            long expectedNetProceeds = valuation - agentFee - mortgageBalance;

            bool sold = _coordinator.RealEstateService.SellProperty(prop.Id, out Currency proceeds, out string error);

            Assert.That(sold, Is.True, error);
            Assert.That(prop.IsOwned, Is.False);
            Assert.That(proceeds.MinorUnits, Is.EqualTo(expectedNetProceeds));
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBefore + proceeds));
            Assert.That(_coordinator.BalanceSheet.RealEstateAssets, Is.EqualTo(Currency.Zero));
            Assert.That(_coordinator.BalanceSheet.MortgageLiabilities, Is.EqualTo(Currency.Zero));
        }

        [Test]
        public void Persistence_Schema5_SerializesAndRestoresRealEstatePortfolio()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(100000.00));
            var starter = _coordinator.RealEstateService.GetProperty("prop_starter_home");
            _coordinator.RealEstateService.BuyProperty(starter.Id, withMortgage: true, out _);
            _coordinator.RealEstateService.SignTenant(starter.Id, starter.BaseMonthlyRent, out _);

            // Save state
            var saved = _coordinator.SaveCurrentState();
            Assert.That(saved.SchemaVersion, Is.EqualTo(5));
            Assert.That(saved.Properties, Is.Not.Null);
            Assert.That(saved.Properties.Count, Is.EqualTo(5));
            Assert.That(saved.Properties.Count(p => p.IsOwned), Is.EqualTo(1));

            var savedProp = saved.Properties.First(p => p.IsOwned);
            Assert.That(savedProp.Id, Is.EqualTo(starter.Id));
            Assert.That(savedProp.IsOwned, Is.True);
            Assert.That(savedProp.IsRented, Is.True);
            Assert.That(savedProp.Mortgage, Is.Not.Null);

            _saveService.Save(saved);

            // Reload state in a new coordinator
            var reloaded = new LifeSimulationCoordinator(_saveService);
            reloaded.InitializeSession();

            var restoredStarter = reloaded.RealEstateService.GetProperty("prop_starter_home");
            Assert.That(restoredStarter, Is.Not.Null);
            Assert.That(restoredStarter.IsOwned, Is.True);
            Assert.That(restoredStarter.IsRented, Is.True);
            Assert.That(restoredStarter.Mortgage, Is.Not.Null);
            Assert.That(restoredStarter.Mortgage.RemainingPrincipal, Is.EqualTo(starter.Mortgage.RemainingPrincipal));

            Assert.That(reloaded.BalanceSheet.RealEstateAssets, Is.EqualTo(restoredStarter.CurrentValuation));
            Assert.That(reloaded.BalanceSheet.MortgageLiabilities, Is.EqualTo(restoredStarter.Mortgage.RemainingPrincipal));
        }

        [Test]
        public void FinancialHealth_IncludesRealEstateCashFlow_InProjectedIncomeAndExpenses()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(80000.00));
            var prop = _coordinator.RealEstateService.GetProperty("prop_starter_home");

            Currency baseIncome = _coordinator.HealthService.ProjectedMonthlyIncome;
            Currency baseExpenses = _coordinator.HealthService.ProjectedMonthlyExpenses;

            _coordinator.RealEstateService.BuyProperty(prop.Id, withMortgage: true, out _);
            _coordinator.RealEstateService.SignTenant(prop.Id, prop.BaseMonthlyRent, out _);

            Currency newIncome = _coordinator.HealthService.ProjectedMonthlyIncome;
            Currency newExpenses = _coordinator.HealthService.ProjectedMonthlyExpenses;

            Assert.That(newIncome, Is.EqualTo(baseIncome + prop.TenantMonthlyRent));
            Assert.That(newExpenses, Is.EqualTo(baseExpenses + prop.Mortgage.MonthlyPayment + prop.MonthlyMaintenanceCost));
        }
    }
}

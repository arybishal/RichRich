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
    public class StockMarketTests
    {
        private string _testDir;
        private SaveDataService _saveService;
        private LifeSimulationCoordinator _coordinator;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "RichRichStockTests_" + Guid.NewGuid().ToString("N"));
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
        public void Exchange_HasDefaultSixEquities_WithValidAttributes()
        {
            var equities = _coordinator.StockMarketService.Equities;
            Assert.That(equities.Count, Is.EqualTo(6));

            var nvst = _coordinator.StockMarketService.GetStock("NVST");
            Assert.That(nvst, Is.Not.Null);
            Assert.That(nvst.Sector, Is.EqualTo(StockSector.Technology));
            Assert.That(nvst.CurrentPrice, Is.EqualTo(Currency.FromDollars(142.50)));

            var glbk = _coordinator.StockMarketService.GetStock("GLBK");
            Assert.That(glbk, Is.Not.Null);
            Assert.That(glbk.Sector, Is.EqualTo(StockSector.FinancialBanking));
            Assert.That(glbk.AnnualDividendYield, Is.GreaterThan(0.04));
        }

        [Test]
        public void BuyStock_SufficientCash_DeductsTotalWithCommission_AddsPosition_RecordsLedger()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(10000.00));
            Currency cashBefore = _coordinator.Wallet.Balance;

            var stock = _coordinator.StockMarketService.GetStock("NVST");
            int shares = 10;
            _coordinator.StockMarketService.CalculateTradeCost("NVST", shares, out var gross, out var comm, out var total);

            bool bought = _coordinator.StockMarketService.BuyStock("NVST", shares, out string error);

            Assert.That(bought, Is.True, error);
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBefore - total));

            var pos = _coordinator.StockMarketService.GetPosition("NVST");
            Assert.That(pos.SharesOwned, Is.EqualTo(shares));
            Assert.That(pos.TotalInvested, Is.EqualTo(total));

            // Verify BalanceSheet PortfolioAssets
            Currency expectedValuation = pos.CalculateCurrentValue(stock.CurrentPrice);
            Assert.That(_coordinator.BalanceSheet.PortfolioAssets, Is.EqualTo(expectedValuation));

            // Verify Ledger Audit Entry
            var tx = _coordinator.Ledger.Transactions.LastOrDefault(t => t.RelatedEntityId == "NVST");
            Assert.That(tx, Is.Not.Null);
            Assert.That(tx.Type, Is.EqualTo(TransactionType.AssetAcquisition));
            Assert.That(tx.Category, Is.EqualTo(TransactionCategory.AssetPurchase));
            Assert.That(tx.Amount, Is.EqualTo(total));
        }

        [Test]
        public void BuyStock_InsufficientFunds_FailsGracefullyWithErrorMessage()
        {
            // Drain wallet to $0
            _coordinator.Wallet.TrySpend(_coordinator.Wallet.Balance, out _);

            bool bought = _coordinator.StockMarketService.BuyStock("NVST", 10, out string error);

            Assert.That(bought, Is.False);
            Assert.That(error, Does.Contain("Insufficient"));
        }

        [Test]
        public void SellStock_SufficientShares_CalculatesNetProceeds_DepositsCash_RecordsLedger()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(10000.00));
            _coordinator.StockMarketService.BuyStock("NVST", 20, out _);

            Currency cashBeforeSale = _coordinator.Wallet.Balance;
            int sellQty = 10;
            _coordinator.StockMarketService.CalculateTradeProceeds("NVST", sellQty, out var gross, out var comm, out var expectedNet);

            bool sold = _coordinator.StockMarketService.SellStock("NVST", sellQty, out Currency proceeds, out string error);

            Assert.That(sold, Is.True, error);
            Assert.That(proceeds, Is.EqualTo(expectedNet));
            Assert.That(_coordinator.Wallet.Balance, Is.EqualTo(cashBeforeSale + expectedNet));

            var pos = _coordinator.StockMarketService.GetPosition("NVST");
            Assert.That(pos.SharesOwned, Is.EqualTo(10));

            // Verify Ledger
            var tx = _coordinator.Ledger.Transactions.LastOrDefault(t => t.RelatedEntityId == "NVST");
            Assert.That(tx, Is.Not.Null);
            Assert.That(tx.Type, Is.EqualTo(TransactionType.AssetLiquidation));
            Assert.That(tx.Category, Is.EqualTo(TransactionCategory.AssetSale));
            Assert.That(tx.Amount, Is.EqualTo(proceeds));
        }

        [Test]
        public void SellStock_MoreSharesThanOwned_FailsGracefully()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(5000.00));
            _coordinator.StockMarketService.BuyStock("NVST", 5, out _);

            bool sold = _coordinator.StockMarketService.SellStock("NVST", 10, out _, out string error);

            Assert.That(sold, Is.False);
            Assert.That(error, Does.Contain("Insufficient shares"));
        }

        [Test]
        public void AdvanceMarketDay_UpdatesPricesAndHistory_MaintainsSixtyDayWindow()
        {
            var stock = _coordinator.StockMarketService.GetStock("NVST");
            int initialHistoryCount = stock.PriceHistory.Count;

            _coordinator.StockMarketService.AdvanceMarketDay();

            Assert.That(stock.PriceHistory.Count, Is.EqualTo(initialHistoryCount + 1));
            Assert.That(stock.PreviousDayPrice, Is.Not.EqualTo(Currency.Zero));

            // Advance 70 days to test cap
            for (int i = 0; i < 70; i++)
            {
                _coordinator.StockMarketService.AdvanceMarketDay();
            }

            Assert.That(stock.PriceHistory.Count, Is.LessThanOrEqualTo(60));
        }

        [Test]
        public void DistributeMonthlyDividends_PaysProportionalDividends_RecordsLedger()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(50000.00));
            // Buy GLBK (Global Reserve Bancorp - 4.5% dividend yield)
            _coordinator.StockMarketService.BuyStock("GLBK", 100, out _);

            Currency cashBefore = _coordinator.Wallet.Balance;

            _coordinator.StockMarketService.DistributeMonthlyDividends();

            Assert.That(_coordinator.Wallet.Balance, Is.GreaterThan(cashBefore));

            var pos = _coordinator.StockMarketService.GetPosition("GLBK");
            Assert.That(pos.TotalDividendsEarned.MinorUnits, Is.GreaterThan(0));

            var divTx = _coordinator.Ledger.Transactions.LastOrDefault(t => t.RelatedEntityId == "GLBK" && t.Type == TransactionType.Income);
            Assert.That(divTx, Is.Not.Null);
            Assert.That(divTx.Description, Does.Contain("dividend"));
        }

        [Test]
        public void Persistence_Schema6_SerializesAndRestoresStockPricesAndPortfolio()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(20000.00));
            _coordinator.StockMarketService.BuyStock("NVST", 15, out _);
            _coordinator.StockMarketService.BuyStock("APXR", 25, out _);

            _coordinator.StockMarketService.AdvanceMarketDay();

            // Save state
            var saved = _coordinator.SaveCurrentState();
            Assert.That(saved.SchemaVersion, Is.GreaterThanOrEqualTo(6));
            Assert.That(saved.StockHoldings, Is.Not.Null);
            Assert.That(saved.StockHoldings.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(saved.StockPrices.Count, Is.EqualTo(6));

            _saveService.Save(saved);

            // Reload into fresh coordinator
            var reloaded = new LifeSimulationCoordinator(_saveService);
            reloaded.InitializeSession();

            var restoredNvst = reloaded.StockMarketService.GetPosition("NVST");
            Assert.That(restoredNvst.SharesOwned, Is.EqualTo(15));

            var restoredApxr = reloaded.StockMarketService.GetPosition("APXR");
            Assert.That(restoredApxr.SharesOwned, Is.EqualTo(25));

            Assert.That(reloaded.BalanceSheet.PortfolioAssets, Is.EqualTo(reloaded.StockMarketService.TotalPortfolioValuation));
        }

        [Test]
        public void StockMarketController_OrderSizing_MaxCalculations_WorkAccurately()
        {
            _coordinator.Wallet.Add(Currency.FromDollars(1000.00));
            var controller = new StockMarketController(_coordinator);

            controller.SelectStock("NVST");
            controller.SetMaxAffordableShares();

            Assert.That(controller.TradeQuantity, Is.GreaterThan(0));
            controller.GetTradeCostPreview(out _, out _, out var total);
            Assert.That(total.MinorUnits, Is.LessThanOrEqualTo(_coordinator.Wallet.Balance.MinorUnits));

            // Execute purchase
            controller.TryBuy(out _);

            // Test sell max
            controller.SetMaxSellableShares();
            Assert.That(controller.TradeQuantity, Is.EqualTo(controller.SelectedPosition.SharesOwned));
        }
    }
}

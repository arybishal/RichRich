using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Service managing simulated equity securities, offline stock market dynamics,
    /// order execution (brokerage commissions), portfolio positions, and dividend distribution.
    /// Synchronizes portfolio valuation directly with the player's Balance Sheet and Financial Ledger.
    /// </summary>
    public class StockMarketService
    {
        private readonly List<StockInstrument> _equities = new List<StockInstrument>();
        private readonly Dictionary<string, PortfolioPosition> _positions = new Dictionary<string, PortfolioPosition>();
        private readonly Random _rng;

        public Wallet Wallet { get; }
        public FinancialLedger Ledger { get; }
        public FinancialService FinancialService { get; }
        public SimulationClock Clock { get; }

        public MarketSentiment CurrentSentiment { get; private set; } = MarketSentiment.Bullish;
        public int SentimentDaysRemaining { get; private set; } = 30;

        public IReadOnlyList<StockInstrument> Equities => _equities.AsReadOnly();
        public IReadOnlyList<PortfolioPosition> Positions => _positions.Values.Where(p => p.SharesOwned > 0).ToList().AsReadOnly();

        public Currency TotalPortfolioValuation
        {
            get
            {
                long totalCents = 0L;
                foreach (var pos in _positions.Values)
                {
                    if (pos.SharesOwned <= 0) continue;
                    var stock = GetStock(pos.Ticker);
                    if (stock != null)
                    {
                        totalCents += pos.CalculateCurrentValue(stock.CurrentPrice).MinorUnits;
                    }
                }
                return Currency.FromCents(totalCents);
            }
        }

        public Currency TotalInvested
        {
            get
            {
                long totalCents = _positions.Values.Where(p => p.SharesOwned > 0).Sum(p => (long)p.TotalInvested.MinorUnits);
                return Currency.FromCents(totalCents);
            }
        }

        public long TotalUnrealizedGainLossCents => (long)TotalPortfolioValuation.MinorUnits - (long)TotalInvested.MinorUnits;

        public Currency TotalDividendsEarned
        {
            get
            {
                long totalCents = _positions.Values.Sum(p => (long)p.TotalDividendsEarned.MinorUnits);
                return Currency.FromCents(totalCents);
            }
        }

        public event Action OnMarketPriceUpdated;
        public event Action<StockInstrument, int, Currency> OnStockPurchased;
        public event Action<StockInstrument, int, Currency> OnStockSold;
        public event Action<Currency> OnDividendsPaid;
        public event Action OnPortfolioUpdated;

        public StockMarketService(
            Wallet wallet,
            FinancialLedger ledger,
            FinancialService financialService,
            SimulationClock clock = null,
            IEnumerable<StockInstrument> equities = null,
            int? rngSeed = null)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            FinancialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
            Clock = clock;
            _rng = rngSeed.HasValue ? new Random(rngSeed.Value) : new Random();

            if (equities != null)
            {
                _equities.AddRange(equities);
            }
            else
            {
                _equities.AddRange(StockInstrument.GetDefaultEquities());
            }

            foreach (var stock in _equities)
            {
                _positions[stock.Ticker] = new PortfolioPosition(stock.Ticker);
            }

            if (Clock != null)
            {
                Clock.OnDayPassed += _ => AdvanceMarketDay();
                Clock.OnMonthPassed += _ => DistributeMonthlyDividends();
            }

            UpdateBalanceSheet();
        }

        public StockInstrument GetStock(string ticker)
        {
            return _equities.FirstOrDefault(s => string.Equals(s.Ticker, ticker, StringComparison.OrdinalIgnoreCase));
        }

        public PortfolioPosition GetPosition(string ticker)
        {
            if (_positions.TryGetValue(ticker, out var pos))
            {
                return pos;
            }
            pos = new PortfolioPosition(ticker);
            _positions[ticker] = pos;
            return pos;
        }

        public void CalculateTradeCost(string ticker, int shares, out Currency grossCost, out Currency commission, out Currency totalCost)
        {
            var stock = GetStock(ticker);
            if (stock == null || shares <= 0)
            {
                grossCost = Currency.Zero;
                commission = Currency.Zero;
                totalCost = Currency.Zero;
                return;
            }

            long grossCents = (long)shares * stock.CurrentPrice.MinorUnits;
            // 0.5% brokerage fee, min $1.00
            long commCents = Math.Max(100L, (long)(grossCents * 0.005));

            grossCost = Currency.FromCents(grossCents);
            commission = Currency.FromCents(commCents);
            totalCost = Currency.FromCents(grossCents + commCents);
        }

        public void CalculateTradeProceeds(string ticker, int shares, out Currency grossProceeds, out Currency commission, out Currency netProceeds)
        {
            var stock = GetStock(ticker);
            if (stock == null || shares <= 0)
            {
                grossProceeds = Currency.Zero;
                commission = Currency.Zero;
                netProceeds = Currency.Zero;
                return;
            }

            long grossCents = (long)shares * stock.CurrentPrice.MinorUnits;
            // 0.5% brokerage fee, min $1.00
            long commCents = Math.Max(100L, (long)(grossCents * 0.005));
            long netCents = Math.Max(0L, grossCents - commCents);

            grossProceeds = Currency.FromCents(grossCents);
            commission = Currency.FromCents(commCents);
            netProceeds = Currency.FromCents(netCents);
        }

        public bool BuyStock(string ticker, int shares, out string error)
        {
            if (shares <= 0)
            {
                error = "Order quantity must be at least 1 share.";
                return false;
            }

            var stock = GetStock(ticker);
            if (stock == null)
            {
                error = $"Stock ticker {ticker} not found on exchange.";
                return false;
            }

            CalculateTradeCost(ticker, shares, out _, out _, out var totalCost);

            if (!Wallet.TrySpend(totalCost, out string spendError))
            {
                error = spendError ?? $"Insufficient liquid funds: Requires {totalCost}.";
                return false;
            }

            var pos = GetPosition(ticker);
            pos.AddShares(shares, totalCost);

            var currentDate = Clock?.CurrentDate ?? new GameDateTime(2026, 1, 1);
            Ledger.Record(new FinancialTransaction(
                Guid.NewGuid().ToString("N"),
                totalCost,
                TransactionType.AssetAcquisition,
                TransactionCategory.AssetPurchase,
                currentDate,
                $"Market buy: {shares} shares of {stock.Ticker} ({stock.Name}) @ {stock.CurrentPrice}",
                stock.Ticker
            ));

            UpdateBalanceSheet();
            error = null;
            OnStockPurchased?.Invoke(stock, shares, totalCost);
            OnPortfolioUpdated?.Invoke();
            return true;
        }

        public bool SellStock(string ticker, int shares, out Currency netProceeds, out string error)
        {
            netProceeds = Currency.Zero;
            if (shares <= 0)
            {
                error = "Order quantity must be at least 1 share.";
                return false;
            }

            var stock = GetStock(ticker);
            if (stock == null)
            {
                error = $"Stock ticker {ticker} not found.";
                return false;
            }

            var pos = GetPosition(ticker);
            if (pos.SharesOwned < shares)
            {
                error = $"Insufficient shares: You own {pos.SharesOwned} shares of {ticker}.";
                return false;
            }

            CalculateTradeProceeds(ticker, shares, out _, out _, out netProceeds);

            pos.RemoveShares(shares, out var costBasisDeducted);
            Wallet.Add(netProceeds);

            var currentDate = Clock?.CurrentDate ?? new GameDateTime(2026, 1, 1);
            Ledger.Record(new FinancialTransaction(
                Guid.NewGuid().ToString("N"),
                netProceeds,
                TransactionType.AssetLiquidation,
                TransactionCategory.AssetSale,
                currentDate,
                $"Market sell: {shares} shares of {stock.Ticker} (Cost basis: {costBasisDeducted}, Net: {netProceeds})",
                stock.Ticker
            ));

            UpdateBalanceSheet();
            error = null;
            OnStockSold?.Invoke(stock, shares, netProceeds);
            OnPortfolioUpdated?.Invoke();
            return true;
        }

        public void AdvanceMarketDay()
        {
            SentimentDaysRemaining--;
            if (SentimentDaysRemaining <= 0)
            {
                CycleSentiment();
            }

            double multiplier = GetSentimentMultiplier();
            foreach (var stock in _equities)
            {
                stock.AdvanceDay(multiplier, _rng);
            }

            UpdateBalanceSheet();
            OnMarketPriceUpdated?.Invoke();
            OnPortfolioUpdated?.Invoke();
        }

        public void DistributeMonthlyDividends()
        {
            long totalPayoutCents = 0L;
            var currentDate = Clock?.CurrentDate ?? new GameDateTime(2026, 1, 1);

            foreach (var pos in _positions.Values)
            {
                if (pos.SharesOwned <= 0) continue;
                var stock = GetStock(pos.Ticker);
                if (stock == null || stock.AnnualDividendYield <= 0) continue;

                // Monthly dividend = (Shares * Price * AnnualYield) / 12
                double annualDivPerShare = stock.CurrentPrice.ToDollars() * stock.AnnualDividendYield;
                double monthlyDivPerShare = annualDivPerShare / 12.0;
                long totalStockDivCents = (long)Math.Round(monthlyDivPerShare * pos.SharesOwned * 100.0);

                if (totalStockDivCents > 0)
                {
                    Currency divPayout = Currency.FromCents(totalStockDivCents);
                    Wallet.Add(divPayout);
                    pos.RecordDividend(divPayout);
                    totalPayoutCents += totalStockDivCents;

                    Ledger.Record(new FinancialTransaction(
                        Guid.NewGuid().ToString("N"),
                        divPayout,
                        TransactionType.Income,
                        TransactionCategory.Miscellaneous,
                        currentDate,
                        $"Monthly dividend distribution from {stock.Ticker} ({pos.SharesOwned} shares)",
                        stock.Ticker
                    ));
                }
            }

            if (totalPayoutCents > 0)
            {
                OnDividendsPaid?.Invoke(Currency.FromCents(totalPayoutCents));
                OnPortfolioUpdated?.Invoke();
            }
        }

        public void UpdateBalanceSheet()
        {
            FinancialService?.BalanceSheet?.UpdatePortfolioAssets(TotalPortfolioValuation);
        }

        private void CycleSentiment()
        {
            int r = _rng.Next(0, 4);
            CurrentSentiment = (MarketSentiment)r;
            SentimentDaysRemaining = _rng.Next(20, 60);
        }

        private double GetSentimentMultiplier()
        {
            return CurrentSentiment switch
            {
                MarketSentiment.Bullish => 1.05,
                MarketSentiment.Bearish => 0.95,
                MarketSentiment.Volatile => 1.00,
                _ => 1.00
            };
        }

        #region State Restoration

        public void RestorePosition(string ticker, int shares, Currency totalInvested, Currency totalDividends)
        {
            var pos = GetPosition(ticker);
            pos.AddShares(shares, totalInvested);
            pos.RecordDividend(totalDividends);
            UpdateBalanceSheet();
        }

        public void RestoreSentiment(MarketSentiment sentiment, int daysRemaining)
        {
            CurrentSentiment = sentiment;
            SentimentDaysRemaining = daysRemaining;
        }

        #endregion
    }
}

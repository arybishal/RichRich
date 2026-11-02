using System;
using System.Collections.Generic;
using System.Linq;

namespace RichRich.Domain.Models
{
    public enum StockSector
    {
        Technology,
        Healthcare,
        CleanEnergy,
        ConsumerRetail,
        FinancialBanking,
        HeavyIndustry
    }

    public enum MarketSentiment
    {
        Bullish,
        Bearish,
        Stagnant,
        Volatile
    }

    /// <summary>
    /// Represents a publicly traded corporate equity security.
    /// Manages pricing, historical trend tracking, dividend yields, and market volatility.
    /// </summary>
    public class StockInstrument
    {
        public string Ticker { get; }
        public string Name { get; }
        public StockSector Sector { get; }
        public string Description { get; }

        public Currency CurrentPrice { get; private set; }
        public Currency PreviousDayPrice { get; private set; }
        public Currency DayHigh { get; private set; }
        public Currency DayLow { get; private set; }
        public Currency FiftyTwoWeekHigh { get; private set; }
        public Currency FiftyTwoWeekLow { get; private set; }

        public double AnnualDividendYield { get; } // e.g. 0.035 for 3.5%
        public double BaseVolatility { get; }      // Daily standard deviation

        private readonly List<Currency> _priceHistory = new List<Currency>();
        public IReadOnlyList<Currency> PriceHistory => _priceHistory.AsReadOnly();

        public double DailyPriceChangePercent
        {
            get
            {
                if (PreviousDayPrice.MinorUnits == 0) return 0.0;
                long diff = (long)CurrentPrice.MinorUnits - (long)PreviousDayPrice.MinorUnits;
                return (double)diff / PreviousDayPrice.MinorUnits;
            }
        }

        public string DailyChangeFormatted
        {
            get
            {
                double pct = DailyPriceChangePercent * 100.0;
                string sign = pct >= 0 ? "+" : "";
                return $"{sign}{pct:F2}%";
            }
        }

        public StockInstrument(
            string ticker,
            string name,
            StockSector sector,
            string description,
            Currency initialPrice,
            double annualDividendYield = 0.025,
            double baseVolatility = 0.03,
            IEnumerable<Currency> initialHistory = null)
        {
            Ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Sector = sector;
            Description = description ?? "";
            CurrentPrice = initialPrice;
            PreviousDayPrice = initialPrice;
            DayHigh = initialPrice;
            DayLow = initialPrice;
            FiftyTwoWeekHigh = initialPrice;
            FiftyTwoWeekLow = initialPrice;
            AnnualDividendYield = Math.Max(0.0, annualDividendYield);
            BaseVolatility = Math.Clamp(baseVolatility, 0.01, 0.20);

            if (initialHistory != null)
            {
                _priceHistory.AddRange(initialHistory);
            }
            if (_priceHistory.Count == 0)
            {
                _priceHistory.Add(initialPrice);
            }
        }

        public void AdvanceDay(double marketSentimentMultiplier, Random rng)
        {
            PreviousDayPrice = CurrentPrice;

            // Box-Muller normal distribution sample
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            // Drift based on sector and market sentiment (-0.01 to +0.02)
            double drift = (marketSentimentMultiplier - 1.0) * 0.015;
            double pctChange = drift + (randStdNormal * BaseVolatility);

            // Cap extreme single-day swings (-15% to +20%)
            pctChange = Math.Clamp(pctChange, -0.15, 0.20);

            long currentCents = CurrentPrice.MinorUnits;
            long deltaCents = (long)Math.Round(currentCents * pctChange);
            long newCents = Math.Max(100L, currentCents + deltaCents); // Floor at $1.00

            CurrentPrice = Currency.FromCents(newCents);

            // Update high / low
            DayHigh = CurrentPrice > PreviousDayPrice ? CurrentPrice : PreviousDayPrice;
            DayLow = CurrentPrice < PreviousDayPrice ? CurrentPrice : PreviousDayPrice;

            if (CurrentPrice > FiftyTwoWeekHigh) FiftyTwoWeekHigh = CurrentPrice;
            if (CurrentPrice < FiftyTwoWeekLow) FiftyTwoWeekLow = CurrentPrice;

            _priceHistory.Add(CurrentPrice);
            if (_priceHistory.Count > 60)
            {
                _priceHistory.RemoveAt(0); // Maintain rolling 60-day window
            }
        }

        public void SetRestoredState(Currency currentPrice, Currency prevPrice, IEnumerable<Currency> history)
        {
            CurrentPrice = currentPrice;
            PreviousDayPrice = prevPrice;
            _priceHistory.Clear();
            if (history != null)
            {
                _priceHistory.AddRange(history);
            }
            if (_priceHistory.Count == 0)
            {
                _priceHistory.Add(currentPrice);
            }
        }

        public static List<StockInstrument> GetDefaultEquities()
        {
            return new List<StockInstrument>
            {
                new StockInstrument("NVST", "Novus Technologies", StockSector.Technology, "Semiconductors, cloud infrastructure, and AI hardware.", Currency.FromDollars(142.50), annualDividendYield: 0.008, baseVolatility: 0.045),
                new StockInstrument("BIOC", "BioCure Therapeutics", StockSector.Healthcare, "Clinical oncology, therapeutics, and medical devices.", Currency.FromDollars(88.20), annualDividendYield: 0.015, baseVolatility: 0.040),
                new StockInstrument("SLRS", "Solis Renewable Energy", StockSector.CleanEnergy, "Utility-scale solar, wind farms, and battery grid storage.", Currency.FromDollars(46.75), annualDividendYield: 0.022, baseVolatility: 0.050),
                new StockInstrument("APXR", "Apex Global Retail", StockSector.ConsumerRetail, "Supermarket chains, e-commerce, and logistics fulfillment.", Currency.FromDollars(62.30), annualDividendYield: 0.038, baseVolatility: 0.025),
                new StockInstrument("GLBK", "Global Reserve Bancorp", StockSector.FinancialBanking, "Commercial depository banking, underwriting, and mortgages.", Currency.FromDollars(115.00), annualDividendYield: 0.045, baseVolatility: 0.028),
                new StockInstrument("TITN", "Titan Heavy Industries", StockSector.HeavyIndustry, "Aerospace components, rail machinery, and heavy equipment.", Currency.FromDollars(74.60), annualDividendYield: 0.032, baseVolatility: 0.035)
            };
        }
    }

    /// <summary>
    /// Represents the player's ownership holding in an equity instrument.
    /// Tracks cost basis, total invested, market valuation, and cumulative dividends.
    /// </summary>
    public class PortfolioPosition
    {
        public string Ticker { get; }
        public int SharesOwned { get; private set; }
        public Currency TotalInvested { get; private set; }
        public Currency TotalDividendsEarned { get; private set; }

        public Currency AverageCostBasis => SharesOwned > 0 ? Currency.FromCents(TotalInvested.MinorUnits / SharesOwned) : Currency.Zero;

        public PortfolioPosition(string ticker, int sharesOwned = 0, Currency? totalInvested = null, Currency? totalDividends = null)
        {
            Ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
            SharesOwned = Math.Max(0, sharesOwned);
            TotalInvested = totalInvested ?? Currency.Zero;
            TotalDividendsEarned = totalDividends ?? Currency.Zero;
        }

        public void AddShares(int shares, Currency totalCost)
        {
            if (shares <= 0) return;
            SharesOwned += shares;
            TotalInvested += totalCost;
        }

        public Currency RemoveShares(int shares, out Currency costBasisDeducted)
        {
            if (shares <= 0 || shares > SharesOwned)
            {
                throw new InvalidOperationException($"Cannot sell {shares} shares; only {SharesOwned} owned.");
            }

            long avgBasisCents = TotalInvested.MinorUnits / SharesOwned;
            costBasisDeducted = Currency.FromCents(avgBasisCents * shares);

            SharesOwned -= shares;
            TotalInvested = SharesOwned > 0 ? TotalInvested - costBasisDeducted : Currency.Zero;

            return costBasisDeducted;
        }

        public void RecordDividend(Currency payout)
        {
            if (payout.MinorUnits > 0)
            {
                TotalDividendsEarned += payout;
            }
        }

        public Currency CalculateCurrentValue(Currency currentPrice)
        {
            return Currency.FromCents((long)SharesOwned * currentPrice.MinorUnits);
        }

        public long CalculateUnrealizedGainLossCents(Currency currentPrice)
        {
            long currentVal = (long)SharesOwned * currentPrice.MinorUnits;
            return currentVal - (long)TotalInvested.MinorUnits;
        }

        public double CalculateReturnPercentage(Currency currentPrice)
        {
            if (TotalInvested.MinorUnits == 0) return 0.0;
            long gainLoss = CalculateUnrealizedGainLossCents(currentPrice);
            return (double)gainLoss / TotalInvested.MinorUnits;
        }
    }
}

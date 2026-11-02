using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Pure C# presentation controller for the simulated Financial Markets and Equities Exchange.
    /// Manages stock quotes, trade execution (market buy/sell orders), order size sizing,
    /// dividend yield inspection, and portfolio performance tracking.
    /// Zero MonoBehaviour dependencies for complete automated testing.
    /// </summary>
    public class StockMarketController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public StockMarketService StockService => Coordinator.StockMarketService;

        public StockInstrument SelectedStock { get; private set; }
        public int TradeQuantity { get; private set; } = 10;
        public string LastErrorMessage { get; private set; }
        public string LastSuccessMessage { get; private set; }

        public IReadOnlyList<StockInstrument> Equities => StockService.Equities;
        public IReadOnlyList<PortfolioPosition> Positions => StockService.Positions;

        public Currency PlayerCash => Coordinator.Wallet.Balance;

        public Currency TotalPortfolioValuation => StockService.TotalPortfolioValuation;
        public Currency TotalInvested => StockService.TotalInvested;
        public long TotalUnrealizedGainLossCents => StockService.TotalUnrealizedGainLossCents;
        public Currency TotalDividendsEarned => StockService.TotalDividendsEarned;
        public MarketSentiment CurrentSentiment => StockService.CurrentSentiment;

        public PortfolioPosition SelectedPosition => SelectedStock != null ? StockService.GetPosition(SelectedStock.Ticker) : null;

        public event Action OnMarketUpdated;
        public event Action<string, bool> OnActionFeedback;

        public StockMarketController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.StockMarketService.OnMarketPriceUpdated += () => OnMarketUpdated?.Invoke();
            Coordinator.StockMarketService.OnPortfolioUpdated += () => OnMarketUpdated?.Invoke();
            Coordinator.StockMarketService.OnDividendsPaid += payout =>
            {
                SetFeedback($"Received {payout} in monthly corporate dividend distributions.", isError: false);
            };

            SelectedStock = Equities.FirstOrDefault();
        }

        public void SelectStock(string ticker)
        {
            var stock = StockService.GetStock(ticker);
            if (stock != null)
            {
                SelectedStock = stock;
                LastErrorMessage = null;
                LastSuccessMessage = null;
                OnMarketUpdated?.Invoke();
            }
        }

        public void SetTradeQuantity(int quantity)
        {
            TradeQuantity = Math.Max(1, quantity);
            OnMarketUpdated?.Invoke();
        }

        public void AdjustTradeQuantity(int delta)
        {
            SetTradeQuantity(TradeQuantity + delta);
        }

        public void SetMaxAffordableShares()
        {
            if (SelectedStock == null || SelectedStock.CurrentPrice.MinorUnits == 0) return;

            long cashCents = PlayerCash.MinorUnits;
            long priceCents = SelectedStock.CurrentPrice.MinorUnits;
            // Accounting for ~0.5% commission
            long approxPriceWithComm = (long)(priceCents * 1.005);
            if (approxPriceWithComm <= 0) return;

            int maxShares = (int)(cashCents / approxPriceWithComm);
            SetTradeQuantity(Math.Max(1, maxShares));
        }

        public void SetMaxSellableShares()
        {
            if (SelectedPosition != null && SelectedPosition.SharesOwned > 0)
            {
                SetTradeQuantity(SelectedPosition.SharesOwned);
            }
        }

        public void GetTradeCostPreview(out Currency gross, out Currency commission, out Currency total)
        {
            if (SelectedStock == null)
            {
                gross = Currency.Zero;
                commission = Currency.Zero;
                total = Currency.Zero;
                return;
            }
            StockService.CalculateTradeCost(SelectedStock.Ticker, TradeQuantity, out gross, out commission, out total);
        }

        public void GetTradeProceedsPreview(out Currency gross, out Currency commission, out Currency net)
        {
            if (SelectedStock == null)
            {
                gross = Currency.Zero;
                commission = Currency.Zero;
                net = Currency.Zero;
                return;
            }
            StockService.CalculateTradeProceeds(SelectedStock.Ticker, TradeQuantity, out gross, out commission, out net);
        }

        public bool TryBuy(out string error)
        {
            if (SelectedStock == null)
            {
                error = "No stock selected.";
                SetFeedback(error, isError: true);
                return false;
            }

            bool success = StockService.BuyStock(SelectedStock.Ticker, TradeQuantity, out error);
            if (success)
            {
                SetFeedback($"Successfully bought {TradeQuantity} shares of {SelectedStock.Ticker} ({SelectedStock.Name}) at {SelectedStock.CurrentPrice}/share.", isError: false);
            }
            else
            {
                SetFeedback(error, isError: true);
            }

            return success;
        }

        public bool TrySell(out Currency netProceeds, out string error)
        {
            netProceeds = Currency.Zero;
            if (SelectedStock == null)
            {
                error = "No stock selected.";
                SetFeedback(error, isError: true);
                return false;
            }

            string ticker = SelectedStock.Ticker;
            bool success = StockService.SellStock(ticker, TradeQuantity, out netProceeds, out error);
            if (success)
            {
                SetFeedback($"Successfully sold {TradeQuantity} shares of {ticker} for net proceeds of {netProceeds}.", isError: false);
            }
            else
            {
                SetFeedback(error, isError: true);
            }

            return success;
        }

        private void SetFeedback(string message, bool isError)
        {
            if (isError)
            {
                LastErrorMessage = message;
                LastSuccessMessage = null;
            }
            else
            {
                LastSuccessMessage = message;
                LastErrorMessage = null;
            }

            OnActionFeedback?.Invoke(message, isError);
            OnMarketUpdated?.Invoke();
        }
    }
}

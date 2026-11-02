using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the simulated Financial Markets and Equities Exchange.
    /// Manages stock quotes, trade execution (market buy/sell orders), order size controls,
    /// dividend yield inspection, and portfolio performance tracking.
    /// Follows dark FinTech visual styling.
    /// </summary>
    public class StockMarketView : MonoBehaviour
    {
        [Header("Portfolio & Market Header")]
        [SerializeField] private Text _totalValuationText;
        [SerializeField] private Text _totalGainLossText;
        [SerializeField] private Text _sentimentText;
        [SerializeField] private Text _liquidCashText;

        [Header("Stock Selector Buttons")]
        [SerializeField] private Button _selectNVSTButton;
        [SerializeField] private Button _selectBIOCButton;
        [SerializeField] private Button _selectSLRSButton;
        [SerializeField] private Button _selectAPXRButton;
        [SerializeField] private Button _selectGLBKButton;
        [SerializeField] private Button _selectTITNButton;

        [Header("Selected Stock Details")]
        [SerializeField] private Text _stockTitleText;
        [SerializeField] private Text _sectorText;
        [SerializeField] private Text _currentPriceText;
        [SerializeField] private Text _dayChangeText;
        [SerializeField] private Text _dividendYieldText;
        [SerializeField] private Text _historyRangeText;
        [SerializeField] private Text _holdingSharesText;
        [SerializeField] private Text _holdingReturnText;

        [Header("Trading Controls")]
        [SerializeField] private Text _quantityText;
        [SerializeField] private Button _minus1Button;
        [SerializeField] private Button _plus1Button;
        [SerializeField] private Button _plus10Button;
        [SerializeField] private Button _maxAffordButton;
        [SerializeField] private Button _maxSellButton;
        [SerializeField] private Text _orderCostPreviewText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _sellButton;
        [SerializeField] private Text _feedbackText;

        private StockMarketController _controller;

        public void Bind(StockMarketController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnMarketUpdated += RefreshUI;
            _controller.OnActionFeedback += (msg, isError) =>
            {
                if (_feedbackText != null)
                {
                    _feedbackText.text = msg;
                    _feedbackText.color = isError
                        ? new Color(0.95f, 0.35f, 0.35f)
                        : new Color(0.35f, 0.85f, 0.50f);
                }
            };

            // Wire selection buttons
            if (_selectNVSTButton != null) _selectNVSTButton.onClick.AddListener(() => _controller.SelectStock("NVST"));
            if (_selectBIOCButton != null) _selectBIOCButton.onClick.AddListener(() => _controller.SelectStock("BIOC"));
            if (_selectSLRSButton != null) _selectSLRSButton.onClick.AddListener(() => _controller.SelectStock("SLRS"));
            if (_selectAPXRButton != null) _selectAPXRButton.onClick.AddListener(() => _controller.SelectStock("APXR"));
            if (_selectGLBKButton != null) _selectGLBKButton.onClick.AddListener(() => _controller.SelectStock("GLBK"));
            if (_selectTITNButton != null) _selectTITNButton.onClick.AddListener(() => _controller.SelectStock("TITN"));

            // Wire quantity adjustment buttons
            if (_minus1Button != null) _minus1Button.onClick.AddListener(() => _controller.AdjustTradeQuantity(-1));
            if (_plus1Button != null) _plus1Button.onClick.AddListener(() => _controller.AdjustTradeQuantity(1));
            if (_plus10Button != null) _plus10Button.onClick.AddListener(() => _controller.AdjustTradeQuantity(10));
            if (_maxAffordButton != null) _maxAffordButton.onClick.AddListener(() => _controller.SetMaxAffordableShares());
            if (_maxSellButton != null) _maxSellButton.onClick.AddListener(() => _controller.SetMaxSellableShares());

            // Wire order execution buttons
            if (_buyButton != null) _buyButton.onClick.AddListener(() => _controller.TryBuy(out _));
            if (_sellButton != null) _sellButton.onClick.AddListener(() => _controller.TrySell(out _, out _));

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_controller == null) return;

            // 1. Portfolio Header
            if (_totalValuationText != null) _totalValuationText.text = $"Portfolio Value: {_controller.TotalPortfolioValuation}";
            if (_liquidCashText != null) _liquidCashText.text = $"Liquid Cash: {_controller.PlayerCash}";
            if (_sentimentText != null)
            {
                _sentimentText.text = $"Market Sentiment: {_controller.CurrentSentiment}";
                _sentimentText.color = _controller.CurrentSentiment switch
                {
                    MarketSentiment.Bullish => new Color(0.35f, 0.85f, 0.50f),
                    MarketSentiment.Bearish => new Color(0.95f, 0.40f, 0.40f),
                    MarketSentiment.Volatile => new Color(0.95f, 0.75f, 0.25f),
                    _ => new Color(0.70f, 0.75f, 0.85f)
                };
            }

            if (_totalGainLossText != null)
            {
                long gainLoss = _controller.TotalUnrealizedGainLossCents;
                string sign = gainLoss >= 0 ? "+" : "-";
                Currency absVal = Currency.FromCents(Math.Abs(gainLoss));
                double totalInvestedCents = _controller.TotalInvested.MinorUnits;
                double pct = totalInvestedCents > 0 ? ((double)gainLoss / totalInvestedCents) * 100.0 : 0.0;

                _totalGainLossText.text = $"Unrealized Gain: {sign}{absVal} ({sign}{pct:F1}%) | Divs: {_controller.TotalDividendsEarned}";
                _totalGainLossText.color = gainLoss >= 0 ? new Color(0.35f, 0.85f, 0.50f) : new Color(0.95f, 0.40f, 0.40f);
            }

            // 2. Selected Stock Details
            var stock = _controller.SelectedStock;
            if (stock == null) return;

            if (_stockTitleText != null) _stockTitleText.text = $"{stock.Ticker} - {stock.Name}";
            if (_sectorText != null) _sectorText.text = $"Sector: {stock.Sector}";
            if (_currentPriceText != null) _currentPriceText.text = $"Price: {stock.CurrentPrice}";

            if (_dayChangeText != null)
            {
                _dayChangeText.text = $"Day Change: {stock.DailyChangeFormatted}";
                _dayChangeText.color = stock.DailyPriceChangePercent >= 0 ? new Color(0.35f, 0.85f, 0.50f) : new Color(0.95f, 0.40f, 0.40f);
            }

            if (_dividendYieldText != null)
            {
                _dividendYieldText.text = $"Dividend Yield: {stock.AnnualDividendYield:P1} APR";
            }

            if (_historyRangeText != null)
            {
                _historyRangeText.text = $"52-Wk Range: {stock.FiftyTwoWeekLow} - {stock.FiftyTwoWeekHigh}";
            }

            // 3. Player Position in Selected Stock
            var pos = _controller.SelectedPosition;
            int shares = pos?.SharesOwned ?? 0;
            if (_holdingSharesText != null)
            {
                if (shares > 0)
                {
                    Currency val = pos.CalculateCurrentValue(stock.CurrentPrice);
                    _holdingSharesText.text = $"Holdings: {shares} shares ({val})";
                    _holdingSharesText.color = new Color(0.35f, 0.85f, 0.50f);
                }
                else
                {
                    _holdingSharesText.text = "Holdings: 0 shares (No position)";
                    _holdingSharesText.color = new Color(0.70f, 0.75f, 0.85f);
                }
            }

            if (_holdingReturnText != null)
            {
                if (shares > 0)
                {
                    long gain = pos.CalculateUnrealizedGainLossCents(stock.CurrentPrice);
                    string sign = gain >= 0 ? "+" : "-";
                    Currency absGain = Currency.FromCents(Math.Abs(gain));
                    double pct = pos.CalculateReturnPercentage(stock.CurrentPrice) * 100.0;
                    _holdingReturnText.text = $"Avg Cost: {pos.AverageCostBasis} | Return: {sign}{absGain} ({sign}{pct:F1}%)";
                    _holdingReturnText.color = gain >= 0 ? new Color(0.35f, 0.85f, 0.50f) : new Color(0.95f, 0.40f, 0.40f);
                }
                else
                {
                    _holdingReturnText.text = "Avg Cost: $0.00 | Return: 0.0%";
                    _holdingReturnText.color = new Color(0.70f, 0.75f, 0.85f);
                }
            }

            // 4. Order Quantity & Cost Preview
            int qty = _controller.TradeQuantity;
            if (_quantityText != null) _quantityText.text = $"Order Qty: {qty} shares";

            _controller.GetTradeCostPreview(out var grossCost, out var commission, out var totalCost);
            _controller.GetTradeProceedsPreview(out _, out _, out var netProceeds);

            if (_orderCostPreviewText != null)
            {
                _orderCostPreviewText.text = $"Buy Est: {totalCost} (Fee: {commission}) | Sell Est: {netProceeds}";
            }

            // 5. Button Interactivity
            if (_buyButton != null)
            {
                _buyButton.interactable = _controller.PlayerCash >= totalCost && qty > 0;
            }

            if (_sellButton != null)
            {
                _sellButton.interactable = shares >= qty && qty > 0;
            }
        }
    }
}

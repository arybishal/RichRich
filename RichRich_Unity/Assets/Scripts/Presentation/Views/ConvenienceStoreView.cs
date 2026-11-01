using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the Convenience Store retail operations.
    /// Manages stock inventory, wholesale restock purchasing, customer pricing strategy,
    /// and gross margin performance tracking.
    /// Follows premium dark FinTech design direction.
    /// </summary>
    public class ConvenienceStoreView : MonoBehaviour
    {
        [Header("Store Header")]
        [SerializeField] private Text _storeTitleText;
        [SerializeField] private Text _storeReputationText;
        [SerializeField] private Text _storeStatusText;

        [Header("Inventory Management")]
        [SerializeField] private Text _productNameText;
        [SerializeField] private Text _productWholesaleCostText;
        [SerializeField] private Text _productSellingPriceText;
        [SerializeField] private Text _productStockText;
        [SerializeField] private Text _productSalesMetricsText;

        [Header("Actions")]
        [SerializeField] private Button _restockSmallButton;
        [SerializeField] private Button _restockBulkButton;
        [SerializeField] private Button _priceIncreaseButton;
        [SerializeField] private Button _priceDecreaseButton;

        [Header("Daily Sales Report")]
        [SerializeField] private Text _salesReportText;
        [SerializeField] private Text _feedbackText;

        private ConvenienceStoreController _controller;

        public void Bind(ConvenienceStoreController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnStoreUpdated += RefreshUI;
            _controller.OnSalesProcessed += OnDailySales;
            _controller.OnActionFailed += OnFailed;

            if (_restockSmallButton != null)
            {
                _restockSmallButton.onClick.AddListener(() =>
                {
                    if (_controller.SelectedProduct != null)
                        _controller.TryOrderStock(_controller.SelectedProduct.Id, 20, out _, out _);
                });
            }

            if (_restockBulkButton != null)
            {
                _restockBulkButton.onClick.AddListener(() =>
                {
                    if (_controller.SelectedProduct != null)
                        _controller.TryOrderStock(_controller.SelectedProduct.Id, 50, out _, out _);
                });
            }

            if (_priceIncreaseButton != null)
            {
                _priceIncreaseButton.onClick.AddListener(() =>
                {
                    if (_controller.SelectedProduct != null)
                    {
                        var newPrice = _controller.SelectedProduct.SellingPrice + Currency.FromDollars(0.50);
                        _controller.SetProductPrice(_controller.SelectedProduct.Id, newPrice);
                    }
                });
            }

            if (_priceDecreaseButton != null)
            {
                _priceDecreaseButton.onClick.AddListener(() =>
                {
                    if (_controller.SelectedProduct != null && _controller.SelectedProduct.SellingPrice.ToDollars() > 1.00)
                    {
                        var newPrice = _controller.SelectedProduct.SellingPrice - Currency.FromDollars(0.50);
                        _controller.SetProductPrice(_controller.SelectedProduct.Id, newPrice);
                    }
                });
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_controller == null) return;

            if (_storeTitleText != null)
                _storeTitleText.text = $"Corner Convenience Store (Level {_controller.StoreLevel})";
            if (_storeReputationText != null)
                _storeReputationText.text = $"Foot Traffic / Reputation: {_controller.Reputation}/100";
            if (_storeStatusText != null)
                _storeStatusText.text = _controller.IsOpen ? "STATUS: OPEN" : "STATUS: CLOSED";

            var prod = _controller.SelectedProduct;
            if (prod != null)
            {
                if (_productNameText != null) _productNameText.text = prod.Name;
                if (_productWholesaleCostText != null) _productWholesaleCostText.text = $"Wholesale Unit Cost: {prod.WholesaleCost}";
                if (_productSellingPriceText != null) _productSellingPriceText.text = $"Retail Selling Price: {prod.SellingPrice}";
                if (_productStockText != null) _productStockText.text = $"In Stock: {prod.StockOnHand} / {prod.MaxCapacity} units";
                if (_productSalesMetricsText != null) _productSalesMetricsText.text = $"Total Sold: {prod.UnitsSoldTotal} | Missed Demand: {prod.MissedSalesTotal}";
            }
        }

        private void OnDailySales(StoreDailySalesResult sales)
        {
            if (_salesReportText != null)
            {
                _salesReportText.text = $"Yesterday's Sales: {sales.UnitsSold} units | Revenue: {sales.Revenue} | Gross Profit: {sales.GrossProfit}";
            }
        }

        private void OnFailed(string error)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Inventory action failed: {error}";
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnStoreUpdated -= RefreshUI;
                _controller.OnSalesProcessed -= OnDailySales;
                _controller.OnActionFailed -= OnFailed;
            }
        }
    }
}

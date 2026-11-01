using System;

namespace RichRich.Domain.Models
{
    /// <summary>
    /// Retail inventory product in the Convenience Store.
    /// Tracks wholesale acquisition cost, retail selling price, stock quantities, and sales metrics.
    /// </summary>
    public class StoreProduct
    {
        public string Id { get; }
        public string Name { get; }
        public Currency WholesaleCost { get; }
        public Currency SellingPrice { get; private set; }
        public int StockOnHand { get; private set; }
        public int MaxCapacity { get; }
        public int UnitsSoldTotal { get; private set; }
        public int MissedSalesTotal { get; private set; }

        public StoreProduct(
            string id,
            string name,
            Currency wholesaleCost,
            Currency sellingPrice,
            int stockOnHand = 0,
            int maxCapacity = 200,
            int unitsSoldTotal = 0,
            int missedSalesTotal = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            WholesaleCost = wholesaleCost;
            SellingPrice = sellingPrice;
            StockOnHand = Math.Clamp(stockOnHand, 0, maxCapacity);
            MaxCapacity = Math.Max(10, maxCapacity);
            UnitsSoldTotal = Math.Max(0, unitsSoldTotal);
            MissedSalesTotal = Math.Max(0, missedSalesTotal);
        }

        public void SetPrice(Currency newPrice)
        {
            if (newPrice.MinorUnits <= 0)
            {
                throw new ArgumentException("Selling price must be greater than zero.", nameof(newPrice));
            }
            SellingPrice = newPrice;
        }

        public void RestoreInventory(int stockOnHand, Currency sellingPrice, int unitsSoldTotal, int missedSalesTotal)
        {
            StockOnHand = Math.Clamp(stockOnHand, 0, MaxCapacity);
            if (sellingPrice.MinorUnits > 0)
            {
                SellingPrice = sellingPrice;
            }
            UnitsSoldTotal = Math.Max(0, unitsSoldTotal);
            MissedSalesTotal = Math.Max(0, missedSalesTotal);
        }

        public int Restock(int units)
        {
            if (units <= 0) return 0;
            int room = MaxCapacity - StockOnHand;
            int added = Math.Min(units, room);
            StockOnHand += added;
            return added;
        }

        public StoreDailySalesResult ProcessCustomerDemand(int demand)
        {
            if (demand <= 0)
            {
                return new StoreDailySalesResult(0, Currency.Zero, Currency.Zero, Currency.Zero, 0);
            }

            int unitsSold = Math.Min(demand, StockOnHand);
            int missedSales = demand - unitsSold;

            StockOnHand -= unitsSold;
            UnitsSoldTotal += unitsSold;
            MissedSalesTotal += missedSales;

            Currency revenue = SellingPrice * unitsSold;
            Currency cogs = WholesaleCost * unitsSold;
            Currency grossProfit = revenue - cogs;

            return new StoreDailySalesResult(unitsSold, revenue, cogs, grossProfit, missedSales);
        }
    }

    /// <summary>
    /// Result record of daily retail sales for a store product.
    /// </summary>
    public class StoreDailySalesResult
    {
        public int UnitsSold { get; }
        public Currency Revenue { get; }
        public Currency CostOfGoodsSold { get; }
        public Currency GrossProfit { get; }
        public int MissedSales { get; }

        public StoreDailySalesResult(int unitsSold, Currency revenue, Currency cogs, Currency grossProfit, int missedSales)
        {
            UnitsSold = unitsSold;
            Revenue = revenue;
            CostOfGoodsSold = cogs;
            GrossProfit = grossProfit;
            MissedSales = missedSales;
        }
    }
}

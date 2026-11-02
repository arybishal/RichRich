using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Domain service managing residential and commercial property investments,
    /// mortgage financing, rental management, maintenance, and asset valuation.
    /// Fully integrated with the central FinancialLedger and BalanceSheet.
    /// </summary>
    public class RealEstateService
    {
        private readonly List<RealEstateProperty> _properties = new List<RealEstateProperty>();

        public Wallet Wallet { get; }
        public FinancialLedger Ledger { get; }
        public FinancialService FinancialService { get; }
        public SimulationClock Clock { get; }

        public IReadOnlyList<RealEstateProperty> AllProperties => _properties.AsReadOnly();
        public IReadOnlyList<RealEstateProperty> OwnedProperties => _properties.Where(p => p.IsOwned).ToList().AsReadOnly();

        public Currency TotalPropertyValuation => Currency.FromCents(OwnedProperties.Sum(p => p.CurrentValuation.MinorUnits));
        public Currency TotalMortgageLiabilities => Currency.FromCents(OwnedProperties.Where(p => p.Mortgage != null && !p.Mortgage.IsPaidOff).Sum(p => p.Mortgage.RemainingPrincipal.MinorUnits));
        public Currency TotalEquity => Currency.FromCents(OwnedProperties.Sum(p => p.Equity.MinorUnits));
        public Currency TotalMonthlyRentalIncome => Currency.FromCents(OwnedProperties.Where(p => p.IsRented).Sum(p => p.TenantMonthlyRent.MinorUnits));
        public Currency TotalMonthlyDebtService => Currency.FromCents(OwnedProperties.Where(p => p.Mortgage != null && !p.Mortgage.IsPaidOff).Sum(p => p.Mortgage.MonthlyPayment.MinorUnits));
        public Currency TotalMonthlyMaintenance => Currency.FromCents(OwnedProperties.Sum(p => p.MonthlyMaintenanceCost.MinorUnits));
        public Currency TotalMonthlyNetCashFlow
        {
            get
            {
                long income = TotalMonthlyRentalIncome.MinorUnits;
                long obligations = TotalMonthlyDebtService.MinorUnits + TotalMonthlyMaintenance.MinorUnits;
                long net = income - obligations;
                return net >= 0 ? Currency.FromCents(net) : Currency.Zero;
            }
        }
        public long TotalMonthlyNetCashFlowCents => (long)TotalMonthlyRentalIncome.MinorUnits - (long)TotalMonthlyDebtService.MinorUnits - (long)TotalMonthlyMaintenance.MinorUnits;

        public event Action<RealEstateProperty> OnPropertyAcquired;
        public event Action<RealEstateProperty, Currency> OnPropertySold;
        public event Action OnPortfolioUpdated;

        public RealEstateService(
            Wallet wallet,
            FinancialLedger ledger,
            FinancialService financialService,
            SimulationClock clock = null,
            IEnumerable<RealEstateProperty> properties = null)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            FinancialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
            Clock = clock;

            if (Clock != null)
            {
                Clock.OnMonthPassed += _ => ProcessMonthlyCycle(1.0);
            }

            if (properties != null)
            {
                _properties.AddRange(properties);
            }
            else
            {
                _properties.AddRange(RealEstateProperty.GetDefaultMarketplace());
            }

            UpdateBalanceSheet();
        }

        public RealEstateProperty GetProperty(string id)
        {
            return _properties.FirstOrDefault(p => p.Id == id);
        }

        public bool BuyProperty(string propertyId, bool withMortgage, out string error)
        {
            var property = GetProperty(propertyId);
            if (property == null)
            {
                error = "Property listing not found.";
                return false;
            }

            if (property.IsOwned)
            {
                error = "Property is already in your investment portfolio.";
                return false;
            }

            Currency cashRequired = withMortgage ? property.DownPaymentAmount : property.PurchasePrice;
            if (Wallet.Balance < cashRequired)
            {
                error = $"Insufficient funds: Requires {cashRequired} cash.";
                return false;
            }

            // Deduct cash via Wallet & Ledger
            if (!Wallet.TrySpend(cashRequired, out string spendError))
            {
                error = spendError ?? "Failed to withdraw acquisition capital.";
                return false;
            }

            property.Acquire(withMortgage, out _, out var createdMortgage);

            string description = withMortgage
                ? $"Down payment for {property.Name}"
                : $"Outright cash acquisition of {property.Name}";

            var currentDate = Clock?.CurrentDate ?? new GameDateTime(2026, 1, 1);
            Ledger.Record(new FinancialTransaction(
                Guid.NewGuid().ToString("N"),
                cashRequired,
                TransactionType.AssetAcquisition,
                TransactionCategory.AssetPurchase,
                currentDate,
                description,
                property.Id
            ));

            UpdateBalanceSheet();
            error = null;
            OnPropertyAcquired?.Invoke(property);
            OnPortfolioUpdated?.Invoke();
            return true;
        }

        public bool SellProperty(string propertyId, out Currency netProceeds, out string error)
        {
            netProceeds = Currency.Zero;
            var property = GetProperty(propertyId);
            if (property == null || !property.IsOwned)
            {
                error = "Property is not owned in your portfolio.";
                return false;
            }

            netProceeds = property.Liquidate(out var mortgagePayoff, 0.05);

            // Deposit net proceeds
            Wallet.Add(netProceeds);
            var currentDate = Clock?.CurrentDate ?? new GameDateTime(2026, 1, 1);
            Ledger.Record(new FinancialTransaction(
                Guid.NewGuid().ToString("N"),
                netProceeds,
                TransactionType.AssetLiquidation,
                TransactionCategory.AssetSale,
                currentDate,
                $"Net liquidation proceeds from sale of {property.Name} (Mortgage settled: {mortgagePayoff})",
                property.Id
            ));

            UpdateBalanceSheet();
            error = null;
            OnPropertySold?.Invoke(property, netProceeds);
            OnPortfolioUpdated?.Invoke();
            return true;
        }

        public bool SignTenant(string propertyId, Currency rentAmount, out string error)
        {
            var property = GetProperty(propertyId);
            if (property == null || !property.IsOwned)
            {
                error = "Property is not owned.";
                return false;
            }

            property.SignTenant(rentAmount);
            error = null;
            OnPortfolioUpdated?.Invoke();
            return true;
        }

        public bool EvictTenant(string propertyId, out string error)
        {
            var property = GetProperty(propertyId);
            if (property == null || !property.IsOwned)
            {
                error = "Property is not owned.";
                return false;
            }

            property.EvictTenant();
            error = null;
            OnPortfolioUpdated?.Invoke();
            return true;
        }

        public bool RenovateProperty(string propertyId, out string error)
        {
            var property = GetProperty(propertyId);
            if (property == null || !property.IsOwned)
            {
                error = "Property is not owned.";
                return false;
            }

            Currency renovationCost = Currency.FromCents(property.MonthlyMaintenanceCost.MinorUnits * 3L);
            if (Wallet.Balance < renovationCost)
            {
                error = $"Insufficient funds: Renovation requires {renovationCost}.";
                return false;
            }

            if (!Wallet.TrySpend(renovationCost, out string spendError))
            {
                error = spendError ?? "Failed to deduct renovation funds.";
                return false;
            }

            property.MaintainProperty(renovationCost, 35.0);
            var currentDate = Clock?.CurrentDate ?? new GameDateTime(2026, 1, 1);
            Ledger.Record(new FinancialTransaction(
                Guid.NewGuid().ToString("N"),
                renovationCost,
                TransactionType.Expense,
                TransactionCategory.Miscellaneous,
                currentDate,
                $"Renovation & repairs for {property.Name}",
                property.Id
            ));

            error = null;
            OnPortfolioUpdated?.Invoke();
            return true;
        }

        public void ProcessMonthlyCycle(double marketCycleMultiplier = 1.0)
        {
            var currentDate = Clock?.CurrentDate ?? new GameDateTime(2026, 1, 1);
            foreach (var prop in OwnedProperties)
            {
                // 1. Physical degradation and market appreciation
                prop.DegradeCondition(1.2);
                prop.UpdateValuation(marketCycleMultiplier);

                // 2. Rental income collection
                if (prop.IsRented)
                {
                    Wallet.Add(prop.TenantMonthlyRent);
                    Ledger.Record(new FinancialTransaction(
                        Guid.NewGuid().ToString("N"),
                        prop.TenantMonthlyRent,
                        TransactionType.Income,
                        TransactionCategory.Miscellaneous,
                        currentDate,
                        $"Monthly rental income from {prop.Name}",
                        prop.Id
                    ));
                }

                // 3. Routine maintenance expense
                if (Wallet.TrySpend(prop.MonthlyMaintenanceCost, out _))
                {
                    Ledger.Record(new FinancialTransaction(
                        Guid.NewGuid().ToString("N"),
                        prop.MonthlyMaintenanceCost,
                        TransactionType.Expense,
                        TransactionCategory.Miscellaneous,
                        currentDate,
                        $"Routine maintenance for {prop.Name}",
                        prop.Id
                    ));
                }
                else
                {
                    // Maintenance neglected -> condition drops faster
                    prop.DegradeCondition(4.0);
                }

                // 4. Monthly Mortgage debt service
                if (prop.Mortgage != null && !prop.Mortgage.IsPaidOff)
                {
                    Currency payment = prop.Mortgage.MonthlyPayment;
                    if (Wallet.TrySpend(payment, out _))
                    {
                        prop.Mortgage.ProcessMonthlyPayment(out var principal, out var interest);
                        Ledger.Record(new FinancialTransaction(
                            Guid.NewGuid().ToString("N"),
                            payment,
                            TransactionType.LiabilityRepay,
                            TransactionCategory.LoanInstallmentPrincipal,
                            currentDate,
                            $"Monthly mortgage payment for {prop.Name} (Principal: {principal}, Interest: {interest})",
                            prop.Mortgage.Id
                        ));
                    }
                    else
                    {
                        // Mortgage missed -> penalty liability
                        FinancialService.BalanceSheet.UpdateOverdueLiabilities(
                            FinancialService.BalanceSheet.OverdueLiabilities + payment
                        );
                    }
                }
            }

            UpdateBalanceSheet();
            OnPortfolioUpdated?.Invoke();
        }

        public void UpdateBalanceSheet()
        {
            FinancialService?.BalanceSheet?.UpdateRealEstateAssets(TotalPropertyValuation);
            FinancialService?.BalanceSheet?.UpdateMortgageLiabilities(TotalMortgageLiabilities);
        }
    }
}

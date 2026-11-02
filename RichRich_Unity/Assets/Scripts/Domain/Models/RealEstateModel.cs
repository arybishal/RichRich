using System;
using System.Collections.Generic;

namespace RichRich.Domain.Models
{
    public enum PropertyType
    {
        ResidentialStarter,
        ResidentialDuplex,
        CommercialRetail,
        ResidentialLuxury,
        CommercialPlaza
    }

    /// <summary>
    /// Represents an active mortgage liability attached to a real estate property.
    /// Tracks principal reduction, interest amortization, and monthly debt service.
    /// </summary>
    public class Mortgage
    {
        public string Id { get; }
        public string PropertyId { get; }
        public Currency OriginalPrincipal { get; }
        public Currency RemainingPrincipal { get; private set; }
        public double AnnualInterestRate { get; }
        public Currency MonthlyPayment { get; }
        public int TermMonths { get; }
        public int MonthsRemaining { get; private set; }
        public int PaymentsMade { get; private set; }

        public bool IsPaidOff => RemainingPrincipal.MinorUnits == 0L || MonthsRemaining <= 0;

        public Mortgage(
            string id,
            string propertyId,
            Currency originalPrincipal,
            double annualInterestRate = 0.055, // 5.5% annual
            int termMonths = 240, // 20 years
            Currency? remainingPrincipal = null,
            int? monthsRemaining = null,
            int paymentsMade = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            PropertyId = propertyId ?? throw new ArgumentNullException(nameof(propertyId));
            OriginalPrincipal = originalPrincipal;
            AnnualInterestRate = annualInterestRate;
            TermMonths = termMonths;
            RemainingPrincipal = remainingPrincipal ?? originalPrincipal;
            MonthsRemaining = monthsRemaining ?? termMonths;
            PaymentsMade = paymentsMade;

            // Monthly amortization calculation: M = P [ r(1 + r)^n ] / [ (1 + r)^n – 1]
            double monthlyRate = annualInterestRate / 12.0;
            if (monthlyRate > 0 && termMonths > 0)
            {
                double factor = Math.Pow(1.0 + monthlyRate, termMonths);
                double paymentAmount = originalPrincipal.ToDollars() * (monthlyRate * factor) / (factor - 1.0);
                MonthlyPayment = Currency.FromDollars(paymentAmount);
            }
            else
            {
                MonthlyPayment = termMonths > 0
                    ? Currency.FromCents(originalPrincipal.MinorUnits / termMonths)
                    : originalPrincipal;
            }
        }

        public void ProcessMonthlyPayment(out Currency principalPaid, out Currency interestPaid)
        {
            if (IsPaidOff)
            {
                principalPaid = Currency.Zero;
                interestPaid = Currency.Zero;
                return;
            }

            double monthlyRate = AnnualInterestRate / 12.0;
            long interestCents = (long)Math.Round(RemainingPrincipal.MinorUnits * monthlyRate);
            long totalPaymentCents = MonthlyPayment.MinorUnits;

            if (totalPaymentCents < interestCents)
            {
                interestCents = totalPaymentCents;
            }

            long principalCents = totalPaymentCents - interestCents;
            if (principalCents > RemainingPrincipal.MinorUnits)
            {
                principalCents = RemainingPrincipal.MinorUnits;
            }

            RemainingPrincipal = Currency.FromCents(RemainingPrincipal.MinorUnits - principalCents);
            MonthsRemaining = Math.Max(0, MonthsRemaining - 1);
            PaymentsMade++;

            principalPaid = Currency.FromCents(principalCents);
            interestPaid = Currency.FromCents(interestCents);
        }

        public Currency SettleRemainingPrincipal()
        {
            var payoff = RemainingPrincipal;
            RemainingPrincipal = Currency.Zero;
            MonthsRemaining = 0;
            return payoff;
        }
    }

    /// <summary>
    /// Represents a real estate asset: valuation, rental income, tenant status,
    /// maintenance costs, and associated mortgage liabilities.
    /// </summary>
    public class RealEstateProperty
    {
        public string Id { get; }
        public string Name { get; }
        public PropertyType Type { get; }
        public Currency PurchasePrice { get; }
        public Currency CurrentValuation { get; private set; }
        public double DownPaymentPercent { get; }
        public double Condition { get; private set; } // 0.0 to 100.0%
        public Currency BaseMonthlyRent { get; }
        public Currency TenantMonthlyRent { get; private set; }
        public Currency MonthlyMaintenanceCost { get; }
        public double AnnualAppreciationRate { get; } // e.g. 0.035 (3.5%/year)

        public bool IsOwned { get; private set; }
        public bool IsRented { get; private set; }
        public Mortgage Mortgage { get; private set; }

        public Currency DownPaymentAmount => Currency.FromCents((long)(PurchasePrice.MinorUnits * DownPaymentPercent));

        public Currency Equity
        {
            get
            {
                if (!IsOwned) return Currency.Zero;
                if (Mortgage == null || Mortgage.IsPaidOff) return CurrentValuation;
                if (CurrentValuation.MinorUnits <= Mortgage.RemainingPrincipal.MinorUnits) return Currency.Zero;
                return CurrentValuation - Mortgage.RemainingPrincipal;
            }
        }

        public Currency MonthlyNetCashFlow
        {
            get
            {
                if (!IsOwned) return Currency.Zero;
                long income = IsRented ? TenantMonthlyRent.MinorUnits : 0L;
                long mortgage = Mortgage != null && !Mortgage.IsPaidOff ? Mortgage.MonthlyPayment.MinorUnits : 0L;
                long maintenance = MonthlyMaintenanceCost.MinorUnits;
                long net = income - (mortgage + maintenance);
                return net >= 0 ? Currency.FromCents(net) : Currency.Zero;
            }
        }

        public string ConditionFormatted => $"{Condition:F1}%";

        public RealEstateProperty(
            string id,
            string name,
            PropertyType type,
            Currency purchasePrice,
            Currency? currentValuation = null,
            double downPaymentPercent = 0.20,
            double condition = 100.0,
            Currency? baseMonthlyRent = null,
            Currency? monthlyMaintenanceCost = null,
            double annualAppreciationRate = 0.035,
            bool isOwned = false,
            bool isRented = false,
            Currency? tenantMonthlyRent = null,
            Mortgage mortgage = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            PurchasePrice = purchasePrice;
            CurrentValuation = currentValuation ?? purchasePrice;
            DownPaymentPercent = Math.Clamp(downPaymentPercent, 0.05, 1.0);
            Condition = Math.Clamp(condition, 0.0, 100.0);
            BaseMonthlyRent = baseMonthlyRent ?? Currency.FromDollars(purchasePrice.ToDollars() * 0.008); // ~0.8% rule
            MonthlyMaintenanceCost = monthlyMaintenanceCost ?? Currency.FromDollars(purchasePrice.ToDollars() * 0.0015);
            AnnualAppreciationRate = annualAppreciationRate;
            IsOwned = isOwned;
            IsRented = isRented;
            TenantMonthlyRent = tenantMonthlyRent ?? BaseMonthlyRent;
            Mortgage = mortgage;
        }

        public void Acquire(bool withMortgage, out Currency cashRequired, out Mortgage createdMortgage)
        {
            if (IsOwned) throw new InvalidOperationException($"Property {Id} is already owned.");

            IsOwned = true;
            Condition = 100.0;
            IsRented = false;

            if (withMortgage)
            {
                cashRequired = DownPaymentAmount;
                Currency loanAmount = PurchasePrice - DownPaymentAmount;
                createdMortgage = new Mortgage($"mortgage_{Id}", Id, loanAmount);
                Mortgage = createdMortgage;
            }
            else
            {
                cashRequired = PurchasePrice;
                createdMortgage = null;
                Mortgage = null;
            }
        }

        public Currency Liquidate(out Currency mortgagePayoff, double agentFeePercent = 0.05)
        {
            if (!IsOwned) throw new InvalidOperationException($"Property {Id} is not owned.");

            long totalValuationCents = CurrentValuation.MinorUnits;
            long feeCents = (long)(totalValuationCents * agentFeePercent);
            long netSaleCents = Math.Max(0L, totalValuationCents - feeCents);

            if (Mortgage != null && !Mortgage.IsPaidOff)
            {
                mortgagePayoff = Mortgage.SettleRemainingPrincipal();
                netSaleCents = Math.Max(0L, netSaleCents - mortgagePayoff.MinorUnits);
            }
            else
            {
                mortgagePayoff = Currency.Zero;
            }

            IsOwned = false;
            IsRented = false;
            Mortgage = null;

            return Currency.FromCents(netSaleCents);
        }

        public bool SignTenant(Currency rentAmount)
        {
            if (!IsOwned) return false;
            IsRented = true;
            TenantMonthlyRent = rentAmount;
            return true;
        }

        public void EvictTenant()
        {
            IsRented = false;
        }

        public void MaintainProperty(Currency cost, double conditionBoost = 25.0)
        {
            Condition = Math.Clamp(Condition + conditionBoost, 0.0, 100.0);
        }

        public void DegradeCondition(double amount = 1.0)
        {
            Condition = Math.Clamp(Condition - amount, 10.0, 100.0);
        }

        public void UpdateValuation(double marketMultiplier = 1.0)
        {
            double monthlyAppreciation = (AnnualAppreciationRate / 12.0) * marketMultiplier;
            double conditionFactor = (Condition / 100.0) * 0.5 + 0.5; // Condition affects appreciation
            long newCents = (long)Math.Round(CurrentValuation.MinorUnits * (1.0 + (monthlyAppreciation * conditionFactor)));
            CurrentValuation = Currency.FromCents(newCents);
        }

        public static List<RealEstateProperty> GetDefaultMarketplace()
        {
            return new List<RealEstateProperty>
            {
                new RealEstateProperty(
                    "prop_starter_home",
                    "Oakwood Suburban Starter",
                    PropertyType.ResidentialStarter,
                    purchasePrice: Currency.FromDollars(65000.00),
                    downPaymentPercent: 0.20,
                    baseMonthlyRent: Currency.FromDollars(650.00),
                    monthlyMaintenanceCost: Currency.FromDollars(95.00),
                    annualAppreciationRate: 0.035
                ),
                new RealEstateProperty(
                    "prop_urban_duplex",
                    "Metro Urban Duplex",
                    PropertyType.ResidentialDuplex,
                    purchasePrice: Currency.FromDollars(160000.00),
                    downPaymentPercent: 0.20,
                    baseMonthlyRent: Currency.FromDollars(1550.00),
                    monthlyMaintenanceCost: Currency.FromDollars(220.00),
                    annualAppreciationRate: 0.040
                ),
                new RealEstateProperty(
                    "prop_retail_strip",
                    "Harbor Retail Commercial Strip",
                    PropertyType.CommercialRetail,
                    purchasePrice: Currency.FromDollars(380000.00),
                    downPaymentPercent: 0.25,
                    baseMonthlyRent: Currency.FromDollars(3900.00),
                    monthlyMaintenanceCost: Currency.FromDollars(480.00),
                    annualAppreciationRate: 0.045
                ),
                new RealEstateProperty(
                    "prop_luxury_tower",
                    "Aria Luxury Penthouse Unit",
                    PropertyType.ResidentialLuxury,
                    purchasePrice: Currency.FromDollars(850000.00),
                    downPaymentPercent: 0.20,
                    baseMonthlyRent: Currency.FromDollars(7800.00),
                    monthlyMaintenanceCost: Currency.FromDollars(850.00),
                    annualAppreciationRate: 0.050
                ),
                new RealEstateProperty(
                    "prop_commercial_plaza",
                    "Crown Metropolitan Financial Plaza",
                    PropertyType.CommercialPlaza,
                    purchasePrice: Currency.FromDollars(2200000.00),
                    downPaymentPercent: 0.25,
                    baseMonthlyRent: Currency.FromDollars(24000.00),
                    monthlyMaintenanceCost: Currency.FromDollars(2600.00),
                    annualAppreciationRate: 0.055
                )
            };
        }
    }
}

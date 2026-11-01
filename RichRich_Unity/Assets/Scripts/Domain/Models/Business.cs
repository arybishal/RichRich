using System;

namespace RichRich.Domain.Models
{
    /// <summary>
    /// Domain representation of a commercial business.
    /// Manages purchase status, level upgrades, and passive cashflow calculations.
    /// </summary>
    public class Business
    {
        public string Id { get; }
        public string Name { get; }
        public Currency BaseCost { get; }
        public Currency BaseHourlyYield { get; }
        public bool IsOwned { get; private set; }
        public int Level { get; private set; }
        public int CompletedTasksCount { get; private set; }

        public event Action<Business> OnBusinessUpdated;

        public Business(
            string id,
            string name,
            Currency baseCost,
            Currency baseHourlyYield,
            bool isOwned = false,
            int level = 1,
            int completedTasksCount = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            BaseCost = baseCost;
            BaseHourlyYield = baseHourlyYield;
            IsOwned = isOwned;
            Level = Math.Max(1, level);
            CompletedTasksCount = Math.Max(0, completedTasksCount);
        }

        public Currency GetPurchaseCost() => BaseCost;

        public Currency GetUpgradeCost()
        {
            if (!IsOwned)
            {
                return BaseCost;
            }

            // Exponential scaling: BaseCost * 1.25^(Level - 1)
            double factor = Math.Pow(EconomyConfig.UpgradeCostMultiplier, Level - 1);
            return BaseCost * factor;
        }

        public Currency GetHourlyIncome()
        {
            if (!IsOwned)
            {
                return Currency.Zero;
            }

            // Income = BaseHourlyYield * (1 + 0.25 * (Level - 1))
            double bonusFactor = 1.0 + (EconomyConfig.UpgradeYieldBonusPercent * (Level - 1));
            return BaseHourlyYield * bonusFactor;
        }

        public void Acquire()
        {
            if (IsOwned)
            {
                throw new InvalidOperationException($"Business '{Name}' is already owned.");
            }
            IsOwned = true;
            Level = 1;
            OnBusinessUpdated?.Invoke(this);
        }

        public void Upgrade()
        {
            if (!IsOwned)
            {
                throw new InvalidOperationException($"Cannot upgrade unowned business '{Name}'.");
            }
            Level++;
            OnBusinessUpdated?.Invoke(this);
        }

        public void RecordCompletedTask()
        {
            CompletedTasksCount++;
            OnBusinessUpdated?.Invoke(this);
        }
    }
}

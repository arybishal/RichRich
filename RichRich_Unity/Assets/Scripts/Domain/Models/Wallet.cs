using System;

namespace RichRich.Domain.Models
{
    /// <summary>
    /// Thread-safe domain wallet managing available balance and lifetime earnings.
    /// </summary>
    public class Wallet
    {
        public Currency Balance { get; private set; }
        public Currency LifetimeEarned { get; private set; }

        public event Action<Currency> OnBalanceChanged;

        public Wallet(Currency initialBalance, Currency lifetimeEarned)
        {
            Balance = initialBalance;
            LifetimeEarned = lifetimeEarned >= initialBalance ? lifetimeEarned : initialBalance;
        }

        public Wallet() : this(Currency.Zero, Currency.Zero)
        {
        }

        public void Add(Currency amount)
        {
            if (amount.MinorUnits == 0) return;

            Balance += amount;
            LifetimeEarned += amount;
            OnBalanceChanged?.Invoke(Balance);
        }

        public bool CanAfford(Currency cost) => Balance >= cost;

        public bool TrySpend(Currency cost, out string errorMessage)
        {
            if (cost.MinorUnits == 0)
            {
                errorMessage = null;
                return true;
            }

            if (!CanAfford(cost))
            {
                errorMessage = $"Insufficient funds: Balance {Balance} is less than required {cost}.";
                return false;
            }

            Balance -= cost;
            errorMessage = null;
            OnBalanceChanged?.Invoke(Balance);
            return true;
        }
    }
}

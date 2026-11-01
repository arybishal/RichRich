using System;
using System.Globalization;

namespace RichRich.Domain.Models
{
    /// <summary>
    /// Value object representing money in integer minor units (cents)
    /// to eliminate all floating-point rounding errors and drift.
    /// </summary>
    [Serializable]
    public readonly struct Currency : IEquatable<Currency>, IComparable<Currency>
    {
        public long MinorUnits { get; }

        public Currency(long minorUnits)
        {
            if (minorUnits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minorUnits), "Currency cannot be negative.");
            }
            MinorUnits = minorUnits;
        }

        public static Currency Zero => new Currency(0L);

        public static Currency FromCents(long cents) => new Currency(cents);

        public static Currency FromDollars(double dollars)
        {
            if (dollars < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dollars), "Currency cannot be negative.");
            }
            long cents = (long)Math.Round(dollars * 100.0, MidpointRounding.AwayFromZero);
            return new Currency(cents);
        }

        public double ToDollars() => MinorUnits / 100.0;

        public static Currency operator +(Currency a, Currency b)
        {
            checked
            {
                return new Currency(a.MinorUnits + b.MinorUnits);
            }
        }

        public static Currency operator -(Currency a, Currency b)
        {
            if (a.MinorUnits < b.MinorUnits)
            {
                throw new InvalidOperationException($"Insufficient currency: {a.MinorUnits} < {b.MinorUnits}.");
            }
            return new Currency(a.MinorUnits - b.MinorUnits);
        }

        public static Currency operator *(Currency a, double factor)
        {
            if (factor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(factor), "Multiplier cannot be negative.");
            }
            checked
            {
                long result = (long)Math.Round(a.MinorUnits * factor, MidpointRounding.AwayFromZero);
                return new Currency(result);
            }
        }

        public static Currency operator *(Currency a, int factor)
        {
            if (factor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(factor), "Multiplier cannot be negative.");
            }
            checked
            {
                return new Currency(a.MinorUnits * factor);
            }
        }

        public static bool operator <(Currency a, Currency b) => a.MinorUnits < b.MinorUnits;
        public static bool operator >(Currency a, Currency b) => a.MinorUnits > b.MinorUnits;
        public static bool operator <=(Currency a, Currency b) => a.MinorUnits <= b.MinorUnits;
        public static bool operator >=(Currency a, Currency b) => a.MinorUnits >= b.MinorUnits;
        public static bool operator ==(Currency a, Currency b) => a.MinorUnits == b.MinorUnits;
        public static bool operator !=(Currency a, Currency b) => a.MinorUnits != b.MinorUnits;

        public override bool Equals(object obj) => obj is Currency other && Equals(other);
        public bool Equals(Currency other) => MinorUnits == other.MinorUnits;
        public override int GetHashCode() => MinorUnits.GetHashCode();
        public int CompareTo(Currency other) => MinorUnits.CompareTo(other.MinorUnits);

        /// <summary>
        /// Formats currency with FinTech standard abbreviations (e.g. $4.00, $1,000.00, $1.25M, $4.88B).
        /// </summary>
        public override string ToString()
        {
            double dollars = ToDollars();
            if (dollars < 1_000_000.0)
            {
                return string.Format(CultureInfo.InvariantCulture, "${0:N2}", dollars);
            }
            if (dollars < 1_000_000_000.0)
            {
                return string.Format(CultureInfo.InvariantCulture, "${0:F2}M", dollars / 1_000_000.0);
            }
            if (dollars < 1_000_000_000_000.0)
            {
                return string.Format(CultureInfo.InvariantCulture, "${0:F2}B", dollars / 1_000_000_000.0);
            }
            return string.Format(CultureInfo.InvariantCulture, "${0:F2}T", dollars / 1_000_000_000_000.0);
        }
    }
}

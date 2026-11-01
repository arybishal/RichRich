using System;

namespace RichRich.Domain.Models
{
    /// <summary>
    /// Immutable simulation calendar timestamp representing Year, Month (1-12), Day (1-30), Hour (0-23), Minute (0-59).
    /// Uses a standardized 30-day month (360 days/year) for clean financial cycles.
    /// Internal representation is absolute simulation minutes from Epoch (Y1-M1-D1 00:00).
    /// </summary>
    public readonly struct GameDateTime : IEquatable<GameDateTime>, IComparable<GameDateTime>
    {
        public const int MinutesPerHour = 60;
        public const int HoursPerDay = 24;
        public const int DaysPerMonth = 30;
        public const int MonthsPerYear = 12;

        public const int MinutesPerDay = HoursPerDay * MinutesPerHour; // 1,440
        public const int MinutesPerMonth = DaysPerMonth * MinutesPerDay; // 43,200
        public const int MinutesPerYear = MonthsPerYear * MinutesPerMonth; // 518,400

        public long TotalMinutes { get; }

        public int Year => (int)(TotalMinutes / MinutesPerYear) + 1;
        public int Month => (int)((TotalMinutes % MinutesPerYear) / MinutesPerMonth) + 1;
        public int Day => (int)((TotalMinutes % MinutesPerMonth) / MinutesPerDay) + 1;
        public int Hour => (int)((TotalMinutes % MinutesPerDay) / MinutesPerHour);
        public int Minute => (int)(TotalMinutes % MinutesPerHour);

        public GameDateTime(long totalMinutes)
        {
            TotalMinutes = Math.Max(0, totalMinutes);
        }

        public GameDateTime(int year, int month, int day, int hour = 0, int minute = 0)
        {
            if (year < 1) throw new ArgumentOutOfRangeException(nameof(year), "Year must be >= 1");
            if (month < 1 || month > MonthsPerYear) throw new ArgumentOutOfRangeException(nameof(month), "Month must be 1..12");
            if (day < 1 || day > DaysPerMonth) throw new ArgumentOutOfRangeException(nameof(day), "Day must be 1..30");
            if (hour < 0 || hour >= HoursPerDay) throw new ArgumentOutOfRangeException(nameof(hour), "Hour must be 0..23");
            if (minute < 0 || minute >= MinutesPerHour) throw new ArgumentOutOfRangeException(nameof(minute), "Minute must be 0..59");

            TotalMinutes = (long)(year - 1) * MinutesPerYear +
                           (long)(month - 1) * MinutesPerMonth +
                           (long)(day - 1) * MinutesPerDay +
                           (long)hour * MinutesPerHour +
                           minute;
        }

        public static GameDateTime DefaultStart => new GameDateTime(1, 1, 1, 8, 0); // Year 1, Month 1, Day 1, 08:00 AM

        public GameDateTime AddMinutes(long minutes) => new GameDateTime(TotalMinutes + minutes);
        public GameDateTime AddHours(int hours) => AddMinutes((long)hours * MinutesPerHour);
        public GameDateTime AddDays(int days) => AddMinutes((long)days * MinutesPerDay);
        public GameDateTime AddMonths(int months) => AddMinutes((long)months * MinutesPerMonth);

        public int TotalDays => (int)(TotalMinutes / MinutesPerDay);

        public bool Equals(GameDateTime other) => TotalMinutes == other.TotalMinutes;
        public override bool Equals(object obj) => obj is GameDateTime other && Equals(other);
        public override int GetHashCode() => TotalMinutes.GetHashCode();
        public int CompareTo(GameDateTime other) => TotalMinutes.CompareTo(other.TotalMinutes);

        public static bool operator ==(GameDateTime left, GameDateTime right) => left.TotalMinutes == right.TotalMinutes;
        public static bool operator !=(GameDateTime left, GameDateTime right) => left.TotalMinutes != right.TotalMinutes;
        public static bool operator <(GameDateTime left, GameDateTime right) => left.TotalMinutes < right.TotalMinutes;
        public static bool operator <=(GameDateTime left, GameDateTime right) => left.TotalMinutes <= right.TotalMinutes;
        public static bool operator >(GameDateTime left, GameDateTime right) => left.TotalMinutes > right.TotalMinutes;
        public static bool operator >=(GameDateTime left, GameDateTime right) => left.TotalMinutes >= right.TotalMinutes;

        public override string ToString()
        {
            return $"Y{Year} M{Month:D2} D{Day:D2} {Hour:D2}:{Minute:D2}";
        }

        public string ToLongDateString()
        {
            return $"Year {Year}, Month {Month}, Day {Day} - {Hour:D2}:{Minute:D2}";
        }

        public string ToShortTimeString() => $"{Hour:D2}:{Minute:D2}";
    }
}

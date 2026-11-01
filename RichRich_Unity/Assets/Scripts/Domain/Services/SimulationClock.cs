using System;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Central simulation calendar and timekeeper.
    /// Converts real-world delta time into deterministic simulation minutes, hours, days, and months.
    /// Guarantees sequential day-by-day dispatching during fast-forwards or offline catch-up
    /// to prevent duplicated or skipped financial events.
    /// </summary>
    public class SimulationClock
    {
        public GameDateTime CurrentDate { get; private set; }
        public long LastProcessedMinute { get; private set; }

        /// <summary>
        /// Simulation speed factor. 1.0 = standard (1 real second = 1 game minute).
        /// Set to 0 to pause the clock.
        /// </summary>
        public float SpeedMultiplier { get; set; } = 1.0f;
        public bool IsPaused => SpeedMultiplier <= 0f;

        /// <summary>Real seconds required for 1 simulation minute at 1.0x speed.</summary>
        public double BaseRealSecondsPerMinute { get; set; } = 1.0;

        private double _accumulatedRealSeconds;

        public event Action<GameDateTime> OnMinuteTicked;
        public event Action<GameDateTime> OnHourTicked;
        public event Action<GameDateTime> OnDayPassed;
        public event Action<GameDateTime> OnMonthPassed;
        public event Action<GameDateTime> OnYearPassed;

        public SimulationClock(GameDateTime? initialDate = null, long? lastProcessedMinute = null)
        {
            CurrentDate = initialDate ?? GameDateTime.DefaultStart;
            LastProcessedMinute = lastProcessedMinute ?? CurrentDate.TotalMinutes;
        }

        /// <summary>
        /// Frame-by-frame tick in real seconds.
        /// </summary>
        public void Tick(double deltaRealSeconds)
        {
            if (IsPaused || deltaRealSeconds <= 0.0) return;

            _accumulatedRealSeconds += deltaRealSeconds * SpeedMultiplier;
            double step = BaseRealSecondsPerMinute;

            if (_accumulatedRealSeconds >= step)
            {
                long minutesToAdvance = (long)(_accumulatedRealSeconds / step);
                _accumulatedRealSeconds -= minutesToAdvance * step;
                AdvanceMinutes(minutesToAdvance);
            }
        }

        /// <summary>
        /// Advances simulation by an exact number of minutes, firing chronological triggers.
        /// </summary>
        public void AdvanceMinutes(long minutes)
        {
            if (minutes <= 0) return;

            long targetMinute = CurrentDate.TotalMinutes + minutes;
            AdvanceTo(new GameDateTime(targetMinute));
        }

        /// <summary>
        /// Advances simulation by whole days (useful for sleeping or fast-forwarding).
        /// </summary>
        public void AdvanceDays(int days)
        {
            if (days <= 0) return;
            AdvanceMinutes((long)days * GameDateTime.MinutesPerDay);
        }

        /// <summary>
        /// Advances to a specific future timestamp, firing intermediate events sequentially.
        /// </summary>
        public void AdvanceTo(GameDateTime targetDate)
        {
            if (targetDate <= CurrentDate) return;

            long startMinute = CurrentDate.TotalMinutes;
            long endMinute = targetDate.TotalMinutes;

            int prevHour = CurrentDate.Hour;
            int prevDay = CurrentDate.Day;
            int prevMonth = CurrentDate.Month;
            int prevYear = CurrentDate.Year;

            // If advancing a large chunk (e.g. days), iterate hour by hour or day by day
            for (long m = startMinute + 1; m <= endMinute; m++)
            {
                CurrentDate = new GameDateTime(m);
                LastProcessedMinute = m;

                OnMinuteTicked?.Invoke(CurrentDate);

                if (CurrentDate.Minute == 0 && CurrentDate.Hour != prevHour)
                {
                    prevHour = CurrentDate.Hour;
                    OnHourTicked?.Invoke(CurrentDate);
                }

                if (CurrentDate.Hour == 0 && CurrentDate.Minute == 0 && CurrentDate.Day != prevDay)
                {
                    prevDay = CurrentDate.Day;
                    OnDayPassed?.Invoke(CurrentDate);

                    if (CurrentDate.Month != prevMonth)
                    {
                        prevMonth = CurrentDate.Month;
                        OnMonthPassed?.Invoke(CurrentDate);

                        if (CurrentDate.Year != prevYear)
                        {
                            prevYear = CurrentDate.Year;
                            OnYearPassed?.Invoke(CurrentDate);
                        }
                    }
                }
            }

            CurrentDate = targetDate;
            LastProcessedMinute = targetDate.TotalMinutes;
        }

        public void FastForwardToNextMorning()
        {
            // Advance to tomorrow 08:00 AM
            int currentDays = CurrentDate.TotalDays;
            long nextMorningMinute = (long)(currentDays + 1) * GameDateTime.MinutesPerDay + (8 * GameDateTime.MinutesPerHour);
            AdvanceTo(new GameDateTime(nextMorningMinute));
        }
    }
}

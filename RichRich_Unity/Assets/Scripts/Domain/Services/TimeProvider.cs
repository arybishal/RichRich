using System;

namespace RichRich.Domain.Services
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
        long CurrentUnixSeconds { get; }
    }

    public class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public long CurrentUnixSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public class MockTimeProvider : ITimeProvider
    {
        public DateTime UtcNow { get; set; }
        public long CurrentUnixSeconds => new DateTimeOffset(UtcNow, TimeSpan.Zero).ToUnixTimeSeconds();

        public MockTimeProvider(DateTime initialUtc)
        {
            UtcNow = initialUtc;
        }

        public void AdvanceSeconds(double seconds)
        {
            UtcNow = UtcNow.AddSeconds(seconds);
        }

        public void Advance(TimeSpan span)
        {
            UtcNow = UtcNow.Add(span);
        }
    }
}

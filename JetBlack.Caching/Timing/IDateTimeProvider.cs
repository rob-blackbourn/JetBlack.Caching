using System;

namespace JetBlack.Caching.Timing
{
    public interface IDateTimeProvider
    {
        DateTime Today { get; }
        DateTime Now { get; }
        long Ticks { get; }
    }
}

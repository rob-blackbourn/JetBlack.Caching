using System;

namespace JetBlack.Caching.Timing
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime Today { get { return DateTime.Today; } }
        public DateTime Now { get { return DateTime.Now; } }
        public long Ticks { get { return DateTime.Now.Ticks; } }
    }
}

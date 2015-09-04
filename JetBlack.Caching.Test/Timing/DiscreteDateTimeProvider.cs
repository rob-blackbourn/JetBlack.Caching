using System;
using JetBlack.Caching.Timing;

namespace JetBlack.Caching.Test.Timing
{
    public class DiscreteDateTimeProvider : IDateTimeProvider
    {
        private readonly TimeSpan _interval;
        private DateTime _dateTime;

        public DiscreteDateTimeProvider(DateTime startDateTime, TimeSpan interval)
        {
            _dateTime = startDateTime;
            _interval = interval;
        }

        public DateTime Today
        {
            get { return Now.Date; }
        }

        public DateTime Now
        {
            get
            {
                var prev = _dateTime;
                _dateTime += _interval;
                return prev;
            }
        }

        public long Ticks { get { return Now.Ticks; } }
    }
}

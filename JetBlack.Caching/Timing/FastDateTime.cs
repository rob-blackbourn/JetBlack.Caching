using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace JetBlack.Caching.Timing
{
    public class FastDateTime : IDateTimeProvider
    {
        private static DateTime _epochDateTime = default(DateTime);
        private static readonly long CountDivisor = QueryPerformance.Frequency / CountsPerMs;
        private static long _epochCount = long.MaxValue;
        private const long TicksPerMs = 10000;
        private const long CountsPerMs = 1000;

        public static long Ticks
        {
            get { return Now.Ticks; }
        }

        public static DateTime Now
        {
            get
            {
                var count = QueryPerformance.Counter;

                if (count < _epochCount)
                {
                    _epochDateTime = DateTime.Now;
                    _epochCount = count;
                    return _epochDateTime;
                }

                var elapsed = count - _epochCount;
                var ticks = (elapsed * TicksPerMs) / CountDivisor;
                return _epochDateTime.AddTicks(ticks);
            }
        }

        public static DateTime Today
        {
            get { return Now.Date; }
        }

        #region IDateTimeProvider

        public static IDateTimeProvider Provider = new FastDateTime();

        DateTime IDateTimeProvider.Now
        {
            get { return Now; }
        }

        DateTime IDateTimeProvider.Today
        {
            get { return Today; }
        }

        long IDateTimeProvider.Ticks
        {
            get { return Ticks; }
        }

        #endregion

        private static class QueryPerformance
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool QueryPerformanceFrequency(out long frequency);

            /// <summary>
            /// Retrieves the current value of the high-resolution performance counter.
            /// </summary>
            public static long Counter
            {
                get
                {
                    long performanceCount;
                    if (!QueryPerformanceCounter(out performanceCount))
                        throw new Win32Exception();
                    return performanceCount;
                }
            }

            /// <summary>
            /// Returns the number of counts per second for the high-performance counter.
            /// </summary>
            public static long Frequency
            {
                get
                {
                    long frequency;
                    if (!QueryPerformanceFrequency(out frequency))
                        throw new Win32Exception();
                    return frequency;
                }
            }
        }
    }
}

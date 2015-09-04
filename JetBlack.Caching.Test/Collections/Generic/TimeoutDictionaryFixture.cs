using System;
using JetBlack.Caching.Collections.Generic;
using JetBlack.Caching.Test.Timing;
using NUnit.Framework;

namespace JetBlack.Caching.Test.Collections.Generic
{
    [TestFixture]
    public class TimeoutDictionaryFixture
    {
        [Test]
        public void TestContainsKey()
        {
            var dateTimeProvider = new DiscreteDateTimeProvider(DateTime.Today, TimeSpan.FromMilliseconds(200));
            var timeoutDictionary = new TimeoutDictionary<string, int>(TimeSpan.FromMilliseconds(500), dateTimeProvider);

            timeoutDictionary.Add("one", 1);
            Assert.IsTrue(timeoutDictionary.ContainsKey("one"));
            Assert.IsTrue(timeoutDictionary.ContainsKey("one"));
            Assert.IsFalse(timeoutDictionary.ContainsKey("one"));
        }
    }
}

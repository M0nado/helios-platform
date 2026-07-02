using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using MonadoBlade.Core.Optimization;

namespace MonadoBlade.Tests.Unit.Core.Optimization
{
    /// <summary>
    /// Unit tests for StringInterningPool optimization (opt-007).
    /// Validates correctness, performance, and memory efficiency of string interning.
    /// </summary>
    public class StringInterningPoolTests
    {
        [Fact]
        public void Intern_ReturnsSameReferenceForSameString()
        {
            var pool = StringInterningPool.Instance;
            var testString = "test-agent-id-unique-" + Guid.NewGuid();

            var interned1 = pool.Intern(testString);
            var interned2 = pool.Intern(testString);

            Assert.Same(interned1, interned2);
        }

        [Fact]
        public void Intern_HandlesNullAndEmptyStrings()
        {
            var pool = StringInterningPool.Instance;

            var nullResult = pool.Intern(null);
            var emptyResult = pool.Intern(string.Empty);

            Assert.Null(nullResult);
            Assert.Empty(emptyResult);
        }

        [Fact]
        public void Intern_ImprovesCacheHitRate()
        {
            var pool = StringInterningPool.Instance;
            pool.ResetMetrics();
            
            var strings = new[] { "agent-1", "agent-2", "agent-1", "agent-3", "agent-1", "agent-2" };

            foreach (var str in strings)
            {
                pool.Intern(str);
            }

            var metrics = pool.GetMetrics();
            var hits = (long)metrics["CacheHits"];
            Assert.True(hits > 0);
        }

        [Fact]
        public void Intern_PerformanceBenefit()
        {
            var pool = StringInterningPool.Instance;
            var testString = "perf-test-" + Guid.NewGuid();
            pool.Intern(testString);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10_000; i++)
            {
                var result = pool.Intern(testString);
                var _ = result.GetHashCode();
            }
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 100);
        }

        [Fact]
        public void GetMetrics_ReportsAccurateStatistics()
        {
            var pool = StringInterningPool.Instance;
            pool.ResetMetrics();

            var testStrings = new[] { "str-1", "str-2", "str-1", "str-3", "str-1" };

            foreach (var str in testStrings)
            {
                pool.Intern(str);
            }

            var metrics = pool.GetMetrics();

            Assert.Equal(5, (long)metrics["TotalLookups"]);
            Assert.True((long)metrics["CacheHits"] > 0);
            Assert.True((double)metrics["HitRate%"] > 0);
        }
    }
}

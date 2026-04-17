using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// Query optimization and performance analysis service
    /// </summary>
    public interface IQueryOptimizationService
    {
        QueryProfile ProfileQuery<T>(string name, Func<List<T>> query);
        Task<QueryProfile> ProfileQueryAsync<T>(string name, Func<Task<List<T>>> query);
        QueryProfile[] GetProfiles();
        void ClearProfiles();
    }

    /// <summary>
    /// Query performance profile
    /// </summary>
    public class QueryProfile
    {
        public string Name { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public int ItemCount { get; set; }
        public long AllocatedBytes { get; set; }
        public DateTime ExecutedAt { get; set; }
        public double ItemsPerSecond => ItemCount > 0 && ElapsedMilliseconds > 0 ? (double)ItemCount / ElapsedMilliseconds * 1000 : 0;
    }

    /// <summary>
    /// Query optimization service for performance analysis
    /// </summary>
    public class QueryOptimizationService : IQueryOptimizationService
    {
        private readonly List<QueryProfile> _profiles = new();
        private readonly Logging.ILogger _logger;
        private readonly object _lock = new object();
        private const int MaxProfiles = 1000;

        public QueryOptimizationService(Logging.ILogger logger) => _logger = logger;

        public QueryProfile ProfileQuery<T>(string name, Func<List<T>> query)
        {
            var sw = Stopwatch.StartNew();
            var beforeMem = GC.GetTotalMemory(false);
            
            List<T> result = null;
            try
            {
                result = query();
                sw.Stop();

                var afterMem = GC.GetTotalMemory(false);
                var profile = new QueryProfile
                {
                    Name = name,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    ItemCount = result?.Count ?? 0,
                    AllocatedBytes = afterMem - beforeMem,
                    ExecutedAt = DateTime.UtcNow
                };

                lock (_lock)
                {
                    _profiles.Add(profile);
                    if (_profiles.Count > MaxProfiles)
                        _profiles.RemoveRange(0, _profiles.Count - MaxProfiles);
                }

                if (sw.ElapsedMilliseconds > 100)
                    _logger?.Warning($"Slow query detected: {name} took {sw.ElapsedMilliseconds}ms");

                return profile;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Query {name} failed: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<QueryProfile> ProfileQueryAsync<T>(string name, Func<Task<List<T>>> query)
        {
            var sw = Stopwatch.StartNew();
            var beforeMem = GC.GetTotalMemory(false);

            try
            {
                var result = await query();
                sw.Stop();

                var afterMem = GC.GetTotalMemory(false);
                var profile = new QueryProfile
                {
                    Name = name,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    ItemCount = result?.Count ?? 0,
                    AllocatedBytes = afterMem - beforeMem,
                    ExecutedAt = DateTime.UtcNow
                };

                lock (_lock)
                {
                    _profiles.Add(profile);
                    if (_profiles.Count > MaxProfiles)
                        _profiles.RemoveRange(0, _profiles.Count - MaxProfiles);
                }

                if (sw.ElapsedMilliseconds > 100)
                    _logger?.Warning($"Slow query detected: {name} took {sw.ElapsedMilliseconds}ms");

                return profile;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Query {name} failed: {ex.Message}", ex);
                throw;
            }
        }

        public QueryProfile[] GetProfiles()
        {
            lock (_lock)
            {
                return _profiles.OrderByDescending(p => p.ExecutedAt).Take(100).ToArray();
            }
        }

        public void ClearProfiles()
        {
            lock (_lock)
            {
                _profiles.Clear();
            }
        }
    }
}

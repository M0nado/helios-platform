using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Core.Caching
{
    /// <summary>
    /// Cache Invalidation Patterns - Event-driven cache invalidation with dependency tracking
    /// Implements pattern-based, event-driven, and dependency-graph invalidation strategies
    /// </summary>
    public interface ICacheInvalidationStrategy
    {
        Task InvalidateAsync(string key, CancellationToken ct = default);
        Task RegisterPatternAsync(string pattern, string[] affectedKeys, CancellationToken ct = default);
    }

    public class EventDrivenInvalidationStrategy : ICacheInvalidationStrategy
    {
        private readonly DistributedCacheWrapper _cache;
        private readonly Dictionary<string, HashSet<string>> _patternToKeys;
        private readonly object _patternLock = new();

        public EventDrivenInvalidationStrategy(DistributedCacheWrapper cache)
        {
            _cache = cache;
            _patternToKeys = new();
        }

        public async Task InvalidateAsync(string key, CancellationToken ct = default)
        {
            await _cache.InvalidateAsync(key, ct);

            // Invalidate all patterns that affect this key
            lock (_patternLock)
            {
                foreach (var (pattern, keys) in _patternToKeys)
                {
                    if (keys.Contains(key))
                    {
                        // Async invalidation via pattern
                        _ = _cache.InvalidatePatternAsync(pattern, ct);
                    }
                }
            }
        }

        public Task RegisterPatternAsync(string pattern, string[] affectedKeys, CancellationToken ct = default)
        {
            lock (_patternLock)
            {
                if (!_patternToKeys.ContainsKey(pattern))
                    _patternToKeys[pattern] = new();
                
                foreach (var key in affectedKeys)
                {
                    _patternToKeys[pattern].Add(key);
                }
            }
            return Task.CompletedTask;
        }
    }

    public class DependencyTrackedInvalidationStrategy : ICacheInvalidationStrategy
    {
        private readonly DistributedCacheWrapper _cache;
        private readonly Dictionary<string, HashSet<string>> _keyToDependents;
        private readonly object _depLock = new();

        public DependencyTrackedInvalidationStrategy(DistributedCacheWrapper cache)
        {
            _cache = cache;
            _keyToDependents = new();
        }

        public async Task InvalidateAsync(string key, CancellationToken ct = default)
        {
            var toInvalidate = new Queue<string>();
            var invalidated = new HashSet<string>();

            toInvalidate.Enqueue(key);

            while (toInvalidate.Count > 0)
            {
                var currentKey = toInvalidate.Dequeue();
                if (invalidated.Contains(currentKey)) continue;

                await _cache.InvalidateAsync(currentKey, ct);
                invalidated.Add(currentKey);

                lock (_depLock)
                {
                    if (_keyToDependents.TryGetValue(currentKey, out var dependents))
                    {
                        foreach (var dependent in dependents)
                        {
                            if (!invalidated.Contains(dependent))
                                toInvalidate.Enqueue(dependent);
                        }
                    }
                }
            }
        }

        public Task RegisterPatternAsync(string pattern, string[] affectedKeys, CancellationToken ct = default)
        {
            lock (_depLock)
            {
                foreach (var key in affectedKeys)
                {
                    if (!_keyToDependents.ContainsKey(key))
                        _keyToDependents[key] = new();
                    _keyToDependents[key].Add(pattern);
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CacheInvalidationPatterns
    {
        private readonly DistributedCacheWrapper _cache;
        private readonly ICacheInvalidationStrategy _strategy;
        private readonly Dictionary<string, CacheInvalidationPolicy> _policies;
        private readonly object _policyLock = new();

        public CacheInvalidationPatterns(DistributedCacheWrapper cache, 
            ICacheInvalidationStrategy strategy = null)
        {
            _cache = cache;
            _strategy = strategy ?? new EventDrivenInvalidationStrategy(cache);
            _policies = new();
        }

        public void RegisterPolicy(CacheInvalidationPolicy policy)
        {
            lock (_policyLock)
            {
                _policies[policy.Name] = policy;
            }
        }

        public async Task OnEntityChangedAsync(string entityType, string entityId, ChangeType changeType, 
            CancellationToken ct = default)
        {
            lock (_policyLock)
            {
                if (_policies.TryGetValue(entityType, out var policy))
                {
                    foreach (var pattern in policy.GetInvalidationPatternsForChange(changeType))
                    {
                        var key = pattern.Replace("{id}", entityId);
                        _ = _cache.InvalidateAsync(key, ct);
                    }
                }
            }
        }

        public async Task OnDataAccessAsync(string dataKey, CancellationToken ct = default)
        {
            // Record data access for patterns (for dependency graph construction)
            // This could feed ML models to predict invalidation patterns
        }

        public void RegisterDependencyChain(string[] keyChain)
        {
            for (int i = 0; i < keyChain.Length - 1; i++)
            {
                _cache.RegisterDependency(keyChain[i], keyChain[i + 1]);
            }
        }

        public async Task InvalidateCascadeAsync(string rootKey, CancellationToken ct = default)
        {
            await _strategy.InvalidateAsync(rootKey, ct);
        }
    }

    public class CacheInvalidationPolicy
    {
        public string Name { get; set; }
        public Dictionary<ChangeType, List<string>> InvalidationPatterns { get; set; }
        public TimeSpan? DefaultTTL { get; set; }

        public List<string> GetInvalidationPatternsForChange(ChangeType changeType)
        {
            if (InvalidationPatterns.TryGetValue(changeType, out var patterns))
                return patterns;
            return new();
        }
    }

    public enum ChangeType
    {
        Created,
        Updated,
        Deleted,
        Bulk
    }

    public class CacheInvalidationPolicyBuilder
    {
        private readonly CacheInvalidationPolicy _policy;

        public CacheInvalidationPolicyBuilder(string name)
        {
            _policy = new() { Name = name, InvalidationPatterns = new() };
        }

        public CacheInvalidationPolicyBuilder InvalidateOnCreate(params string[] patterns)
        {
            _policy.InvalidationPatterns[ChangeType.Created] = new(patterns);
            return this;
        }

        public CacheInvalidationPolicyBuilder InvalidateOnUpdate(params string[] patterns)
        {
            _policy.InvalidationPatterns[ChangeType.Updated] = new(patterns);
            return this;
        }

        public CacheInvalidationPolicyBuilder InvalidateOnDelete(params string[] patterns)
        {
            _policy.InvalidationPatterns[ChangeType.Deleted] = new(patterns);
            return this;
        }

        public CacheInvalidationPolicyBuilder WithDefaultTTL(TimeSpan ttl)
        {
            _policy.DefaultTTL = ttl;
            return this;
        }

        public CacheInvalidationPolicy Build()
        {
            return _policy;
        }
    }

    public class SmartCacheInvalidator
    {
        private readonly CacheInvalidationPatterns _patterns;
        private readonly Dictionary<string, AccessPattern> _accessPatterns;
        private readonly object _accessLock = new();

        private class AccessPattern
        {
            public string Key { get; set; }
            public long AccessCount { get; set; }
            public DateTime LastAccess { get; set; }
            public double AverageTimeBetweenAccesses { get; set; }
        }

        public SmartCacheInvalidator(CacheInvalidationPatterns patterns)
        {
            _patterns = patterns;
            _accessPatterns = new();
        }

        public void RecordAccess(string key)
        {
            lock (_accessLock)
            {
                if (!_accessPatterns.ContainsKey(key))
                {
                    _accessPatterns[key] = new() { Key = key, AccessCount = 0 };
                }

                var pattern = _accessPatterns[key];
                var now = DateTime.UtcNow;
                
                if (pattern.LastAccess != default)
                {
                    var timeBetween = (now - pattern.LastAccess).TotalMilliseconds;
                    pattern.AverageTimeBetweenAccesses = 
                        (pattern.AverageTimeBetweenAccesses * pattern.AccessCount + timeBetween) 
                        / (pattern.AccessCount + 1);
                }

                pattern.AccessCount++;
                pattern.LastAccess = now;
            }
        }

        public TimeSpan PredictOptimalTTL(string key)
        {
            lock (_accessLock)
            {
                if (!_accessPatterns.TryGetValue(key, out var pattern))
                    return TimeSpan.FromMinutes(15);

                // If accessed very frequently, extend TTL
                if (pattern.AccessCount > 100)
                    return TimeSpan.FromHours(1);

                // If moderately accessed, use default
                if (pattern.AccessCount > 10)
                    return TimeSpan.FromMinutes(30);

                // Infrequently accessed - shorter TTL
                return TimeSpan.FromMinutes(5);
            }
        }
    }
}

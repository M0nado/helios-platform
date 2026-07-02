using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MonadoBlade.Core.Caching
{
    /// <summary>
    /// Tracks dependencies between cache entries and provides intelligent invalidation.
    /// Uses smart logic to determine whether full or partial cache invalidation is needed.
    /// </summary>
    public class DependencyTracker
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly Dictionary<string, HashSet<string>> _dependents = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, HashSet<string>> _dependencies = new Dictionary<string, HashSet<string>>();
        private long _invalidationCount = 0;
        private long _partialInvalidationCount = 0;
        private long _fullInvalidationCount = 0;
        private long _totalKeysAffected = 0;

        /// <summary>
        /// Gets the total number of invalidation operations performed.
        /// </summary>
        public long InvalidationCount
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _invalidationCount;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the number of partial invalidation operations performed.
        /// </summary>
        public long PartialInvalidationCount
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _partialInvalidationCount;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the number of full invalidation operations performed.
        /// </summary>
        public long FullInvalidationCount
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _fullInvalidationCount;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the total number of keys affected by invalidation operations.
        /// </summary>
        public long TotalKeysAffected
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _totalKeysAffected;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the current number of tracked keys.
        /// </summary>
        public int TrackingCount
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _dependencies.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Registers a dependency relationship: keyA depends on keyB.
        /// </summary>
        /// <param name="key">The cache key being registered.</param>
        /// <param name="dependsOn">The cache key that this key depends on.</param>
        /// <exception cref="ArgumentException">Thrown when key or dependsOn is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when circular dependency is detected.</exception>
        public void RegisterDependency(string key, string dependsOn)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (string.IsNullOrWhiteSpace(dependsOn))
                throw new ArgumentException("DependsOn cannot be null or empty.", nameof(dependsOn));
            if (key.Equals(dependsOn, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A key cannot depend on itself.", nameof(dependsOn));

            _lock.EnterWriteLock();
            try
            {
                // Check for circular dependencies
                if (WouldCreateCircularDependency(key, dependsOn))
                    throw new InvalidOperationException($"Circular dependency detected: registering '{key}' -> '{dependsOn}'");

                // Register the dependency
                if (!_dependencies.ContainsKey(key))
                    _dependencies[key] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _dependencies[key].Add(dependsOn);

                // Register the dependent relationship
                if (!_dependents.ContainsKey(dependsOn))
                    _dependents[dependsOn] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _dependents[dependsOn].Add(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets all cache keys that depend on the specified key.
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <returns>A set of cache keys that depend on the specified key.</returns>
        public IReadOnlySet<string> GetDependents(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _lock.EnterReadLock();
            try
            {
                if (_dependents.TryGetValue(key, out var dependents))
                    return new HashSet<string>(dependents, StringComparer.OrdinalIgnoreCase);

                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the dependencies for a specific cache key.
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <returns>A set of cache keys that this key depends on.</returns>
        public IReadOnlySet<string> GetDependencies(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _lock.EnterReadLock();
            try
            {
                if (_dependencies.TryGetValue(key, out var dependencies))
                    return new HashSet<string>(dependencies, StringComparer.OrdinalIgnoreCase);

                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Invalidates a cache key and returns the set of all keys that should be invalidated.
        /// Uses smart logic: returns full invalidation signal if too many keys are affected,
        /// otherwise returns partial invalidation with specific keys.
        /// </summary>
        /// <param name="key">The cache key to invalidate.</param>
        /// <param name="totalCacheSize">The total number of keys in the cache.</param>
        /// <returns>An object containing invalidation information.</returns>
        public InvalidationResult InvalidateKey(string key, int totalCacheSize = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _lock.EnterWriteLock();
            try
            {
                var affectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var toProcess = new Queue<string>();
                toProcess.Enqueue(key);

                // Breadth-first search to find all transitively dependent keys
                while (toProcess.Count > 0)
                {
                    var currentKey = toProcess.Dequeue();
                    if (!affectedKeys.Add(currentKey))
                        continue;

                    if (_dependents.TryGetValue(currentKey, out var directDependents))
                    {
                        foreach (var dependent in directDependents)
                        {
                            if (!affectedKeys.Contains(dependent))
                                toProcess.Enqueue(dependent);
                        }
                    }
                }

                // Determine if full or partial invalidation is needed
                var invalidationRatio = totalCacheSize > 0 ? (double)affectedKeys.Count / totalCacheSize : 0;
                var shouldClearAll = invalidationRatio > 0.1; // If more than 10% affected, clear all

                _invalidationCount++;
                _totalKeysAffected += affectedKeys.Count;

                if (shouldClearAll)
                {
                    _fullInvalidationCount++;
                    return new InvalidationResult(affectedKeys, true, invalidationRatio);
                }
                else
                {
                    _partialInvalidationCount++;
                    return new InvalidationResult(affectedKeys, false, invalidationRatio);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Unregisters a specific dependency.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="dependsOn">The dependency to remove.</param>
        public void UnregisterDependency(string key, string dependsOn)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (string.IsNullOrWhiteSpace(dependsOn))
                throw new ArgumentException("DependsOn cannot be null or empty.", nameof(dependsOn));

            _lock.EnterWriteLock();
            try
            {
                if (_dependencies.TryGetValue(key, out var deps))
                    deps.Remove(dependsOn);

                if (_dependents.TryGetValue(dependsOn, out var depts))
                    depts.Remove(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes all dependencies for a specific key.
        /// </summary>
        /// <param name="key">The cache key to clean up.</param>
        public void RemoveKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _lock.EnterWriteLock();
            try
            {
                // Remove all dependencies of this key
                if (_dependencies.TryGetValue(key, out var deps))
                {
                    foreach (var dependency in deps)
                    {
                        if (_dependents.TryGetValue(dependency, out var depts))
                            depts.Remove(key);
                    }
                    _dependencies.Remove(key);
                }

                // Remove all dependents of this key
                if (_dependents.TryGetValue(key, out var depts2))
                {
                    foreach (var dependent in depts2)
                    {
                        if (_dependencies.TryGetValue(dependent, out var deps2))
                            deps2.Remove(key);
                    }
                    _dependents.Remove(key);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Clears all tracked dependencies.
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _dependencies.Clear();
                _dependents.Clear();
                _invalidationCount = 0;
                _partialInvalidationCount = 0;
                _fullInvalidationCount = 0;
                _totalKeysAffected = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Checks if registering a dependency would create a circular reference.
        /// </summary>
        private bool WouldCreateCircularDependency(string key, string dependsOn)
        {
            if (!_dependencies.ContainsKey(dependsOn))
                return false;

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var toProcess = new Queue<string>();
            toProcess.Enqueue(dependsOn);

            while (toProcess.Count > 0)
            {
                var current = toProcess.Dequeue();
                if (!visited.Add(current))
                    continue;

                if (current.Equals(key, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (_dependencies.TryGetValue(current, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        if (!visited.Contains(dep))
                            toProcess.Enqueue(dep);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Disposes the dependency tracker and releases managed resources.
        /// </summary>
        public void Dispose()
        {
            _lock?.Dispose();
        }
    }

    /// <summary>
    /// Represents the result of a cache invalidation operation.
    /// </summary>
    public class InvalidationResult
    {
        /// <summary>
        /// Gets the set of cache keys that should be invalidated.
        /// </summary>
        public IReadOnlySet<string> KeysToInvalidate { get; }

        /// <summary>
        /// Gets a value indicating whether a full cache clear is recommended.
        /// </summary>
        public bool ShouldClearAll { get; }

        /// <summary>
        /// Gets the ratio of affected keys to total cache size.
        /// </summary>
        public double InvalidationRatio { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidationResult"/> class.
        /// </summary>
        public InvalidationResult(IReadOnlySet<string> keysToInvalidate, bool shouldClearAll, double invalidationRatio)
        {
            KeysToInvalidate = keysToInvalidate ?? new HashSet<string>();
            ShouldClearAll = shouldClearAll;
            InvalidationRatio = invalidationRatio;
        }
    }
}

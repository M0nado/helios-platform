using System;
using System.Collections.Generic;

namespace MonadoBlade.Core.Caching
{
    /// <summary>
    /// Represents a generic cache entry with expiration and dependency tracking.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the cache entry.</typeparam>
    public class CacheEntry<T>
    {
        /// <summary>
        /// Gets the unique key for this cache entry.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the cached value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets the timestamp when this entry was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the timestamp when this entry expires.
        /// </summary>
        public DateTime ExpiresAt { get; }

        /// <summary>
        /// Gets the list of cache keys that this entry depends on.
        /// When any of these dependencies are invalidated, this entry becomes invalid.
        /// </summary>
        public IReadOnlyList<string> Dependencies { get; }

        /// <summary>
        /// Gets the number of times this entry has been accessed.
        /// </summary>
        public int AccessCount { get; private set; }

        /// <summary>
        /// Gets the timestamp of the last access.
        /// </summary>
        public DateTime? LastAccessedAt { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheEntry{T}"/> class.
        /// </summary>
        /// <param name="key">The unique cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="expiresAt">The expiration time.</param>
        /// <param name="dependencies">Optional list of cache keys this entry depends on.</param>
        public CacheEntry(string key, T value, DateTime expiresAt, IEnumerable<string> dependencies = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            Key = key;
            Value = value;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = expiresAt;
            Dependencies = dependencies != null ? new List<string>(dependencies).AsReadOnly() : new List<string>().AsReadOnly();
            AccessCount = 0;
            LastAccessedAt = null;
        }

        /// <summary>
        /// Gets a value indicating whether this cache entry is still valid.
        /// An entry is valid if it hasn't expired.
        /// </summary>
        public bool IsValid => DateTime.UtcNow < ExpiresAt;

        /// <summary>
        /// Gets a value indicating whether this cache entry has expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// Gets the time remaining until this entry expires.
        /// </summary>
        public TimeSpan TimeToExpiration => ExpiresAt - DateTime.UtcNow;

        /// <summary>
        /// Records an access to this cache entry.
        /// </summary>
        public void RecordAccess()
        {
            AccessCount++;
            LastAccessedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the dependency keys for this entry.
        /// </summary>
        /// <returns>A list of cache keys that this entry depends on.</returns>
        public IReadOnlyList<string> GetDependencyKeys()
        {
            return Dependencies;
        }

        /// <summary>
        /// Checks if this entry depends on a specific cache key.
        /// </summary>
        /// <param name="dependencyKey">The cache key to check.</param>
        /// <returns>True if this entry depends on the specified key; otherwise, false.</returns>
        public bool DependsOn(string dependencyKey)
        {
            if (string.IsNullOrWhiteSpace(dependencyKey))
                return false;

            return Dependencies.Contains(dependencyKey);
        }

        /// <summary>
        /// Gets the age of this cache entry.
        /// </summary>
        public TimeSpan Age => DateTime.UtcNow - CreatedAt;

        /// <summary>
        /// Gets a string representation of this cache entry.
        /// </summary>
        public override string ToString()
        {
            var dependencyStr = Dependencies.Count > 0 ? $", Dependencies: [{string.Join(", ", Dependencies)}]" : string.Empty;
            var accessStr = AccessCount > 0 ? $", Accesses: {AccessCount}" : string.Empty;
            return $"CacheEntry [Key: {Key}, Valid: {IsValid}, Age: {Age.TotalSeconds:F2}s{dependencyStr}{accessStr}]";
        }
    }
}

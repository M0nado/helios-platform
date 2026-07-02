using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.GUI.Performance
{
    /// <summary>
    /// Token cache manager for secure auth token handling (150 LOC)
    /// Implements caching, automatic refresh, and secure token rotation.
    /// </summary>
    public class TokenCacheManager
    {
        private class CachedToken
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public DateTime ExpiresAt { get; set; }
            public DateTime CachedAt { get; set; }
        }

        private readonly Dictionary<string, CachedToken> _tokenCache = new();
        private readonly object _lockObject = new();
        private readonly int _refreshBufferSeconds = 300; // 5 minutes before expiry
        private readonly int _maxCacheAge = 86400; // 24 hours

        public event Action<string> TokenRefreshed;
        public event Action<string> TokenExpired;
        public event Action<string, Exception> TokenRefreshFailed;

        /// <summary>
        /// Cache a token with expiration time
        /// </summary>
        public void CacheToken(string userId, string accessToken, string refreshToken, 
            int expiresInSeconds)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("userId and accessToken are required");

            lock (_lockObject)
            {
                _tokenCache[userId] = new CachedToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds),
                    CachedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Get cached token if valid
        /// </summary>
        public string GetToken(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            lock (_lockObject)
            {
                if (!_tokenCache.ContainsKey(userId))
                    return null;

                var cached = _tokenCache[userId];

                // Check if cache is expired
                if (DateTime.UtcNow >= cached.ExpiresAt)
                {
                    _tokenCache.Remove(userId);
                    TokenExpired?.Invoke(userId);
                    return null;
                }

                // Check if cache is stale (older than max age)
                if ((DateTime.UtcNow - cached.CachedAt).TotalSeconds > _maxCacheAge)
                {
                    _tokenCache.Remove(userId);
                    return null;
                }

                return cached.AccessToken;
            }
        }

        /// <summary>
        /// Check if token needs refresh
        /// </summary>
        public bool IsTokenNearExpiry(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            lock (_lockObject)
            {
                if (!_tokenCache.ContainsKey(userId))
                    return true;

                var cached = _tokenCache[userId];
                var timeUntilExpiry = cached.ExpiresAt - DateTime.UtcNow;
                
                return timeUntilExpiry.TotalSeconds < _refreshBufferSeconds;
            }
        }

        /// <summary>
        /// Get refresh token
        /// </summary>
        public string GetRefreshToken(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            lock (_lockObject)
            {
                return _tokenCache.ContainsKey(userId) 
                    ? _tokenCache[userId].RefreshToken 
                    : null;
            }
        }

        /// <summary>
        /// Refresh token with custom refresh logic
        /// </summary>
        public async Task<bool> RefreshTokenAsync(string userId, 
            Func<string, Task<(string AccessToken, string RefreshToken, int ExpiresIn)>> refreshFunc)
        {
            if (string.IsNullOrWhiteSpace(userId) || refreshFunc == null)
                return false;

            try
            {
                var refreshToken = GetRefreshToken(userId);
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return false;

                var (newAccessToken, newRefreshToken, expiresIn) = 
                    await refreshFunc(refreshToken);

                CacheToken(userId, newAccessToken, newRefreshToken, expiresIn);
                TokenRefreshed?.Invoke(userId);
                return true;
            }
            catch (Exception ex)
            {
                TokenRefreshFailed?.Invoke(userId, ex);
                return false;
            }
        }

        /// <summary>
        /// Clear token for user
        /// </summary>
        public void ClearToken(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            lock (_lockObject)
            {
                _tokenCache.Remove(userId);
            }
        }

        /// <summary>
        /// Clear all cached tokens
        /// </summary>
        public void ClearAll()
        {
            lock (_lockObject)
            {
                _tokenCache.Clear();
            }
        }

        /// <summary>
        /// Get all cached user IDs
        /// </summary>
        public IEnumerable<string> GetCachedUsers()
        {
            lock (_lockObject)
            {
                return _tokenCache.Keys.ToList();
            }
        }

        /// <summary>
        /// Start automatic token refresh timer
        /// </summary>
        public Timer StartAutoRefreshTimer(string userId, 
            Func<string, Task<(string AccessToken, string RefreshToken, int ExpiresIn)>> refreshFunc,
            int checkIntervalSeconds = 60)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return new Timer(_ =>
            {
                if (IsTokenNearExpiry(userId))
                {
                    _ = RefreshTokenAsync(userId, refreshFunc);
                }
            }, null, TimeSpan.FromSeconds(checkIntervalSeconds), 
            TimeSpan.FromSeconds(checkIntervalSeconds));
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public TokenCacheStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                var expiredCount = _tokenCache.Values.Count(t => 
                    DateTime.UtcNow >= t.ExpiresAt);
                
                return new TokenCacheStatistics
                {
                    TotalCachedTokens = _tokenCache.Count,
                    ValidTokens = _tokenCache.Count - expiredCount,
                    ExpiredTokens = expiredCount,
                    NearExpiryTokens = _tokenCache.Values.Count(t => IsTokenNearExpiry(
                        _tokenCache.First(kvp => kvp.Value == t).Key))
                };
            }
        }
    }

    /// <summary>
    /// Token cache statistics
    /// </summary>
    public class TokenCacheStatistics
    {
        public int TotalCachedTokens { get; set; }
        public int ValidTokens { get; set; }
        public int ExpiredTokens { get; set; }
        public int NearExpiryTokens { get; set; }
    }
}

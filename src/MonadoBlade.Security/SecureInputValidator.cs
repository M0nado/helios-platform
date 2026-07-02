using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Web;
using MonadoBlade.Security.Abstractions;

namespace MonadoBlade.Security;

/// <summary>
/// Provides comprehensive input validation, sanitization, and protection against common web attacks.
/// </summary>
public class SecureInputValidator : IInputValidator
{
    private static readonly char[] PathSeparators = { '\\', '/' };
    private static readonly string[] DangerousPatterns = { "..", "~", "null", "NUL" };
    private static readonly ConcurrentDictionary<string, RateLimitBucket> RateLimitBuckets = new();
    private static readonly ConcurrentDictionary<string, string> CsrfTokens = new();
    private static readonly object _syncLock = new();

    public SecureInputValidator()
    {
        // Initialize background cleanup of expired rate limit buckets
        _ = Task.Run(CleanupExpiredBucketsAsync);
    }

    /// <summary>
    /// Validates and sanitizes user input against various attack vectors.
    /// </summary>
    public async Task<string> ValidateAndSanitizeAsync(string input, string inputType = "text")
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return inputType.ToLowerInvariant() switch
        {
            "url" => SanitizeUrlInput(input),
            "html" => EncodeForXss(input),
            "sql" => EncodeForSql(input),
            "path" => SanitizePathInput(input),
            _ => SanitizeTextInput(input)
        };
    }

    /// <summary>
    /// Checks if a request has exceeded rate limits using a sliding window algorithm.
    /// </summary>
    public async Task<bool> CheckRateLimitAsync(string identifier, int maxRequests = 100, int windowSeconds = 60)
    {
        if (string.IsNullOrEmpty(identifier))
            return false;

        var key = $"{identifier}:{maxRequests}:{windowSeconds}";
        var bucket = RateLimitBuckets.AddOrUpdate(key, 
            _ => new RateLimitBucket(maxRequests, windowSeconds),
            (_, existing) => existing);

        return bucket.IsRequestAllowed();
    }

    /// <summary>
    /// Validates a CSRF token with timing attack resistance.
    /// </summary>
    public async Task<bool> ValidateCsrfTokenAsync(string token, string sessionId)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(sessionId))
            return false;

        var key = $"csrf_{sessionId}";
        
        if (!CsrfTokens.TryGetValue(key, out var storedToken))
            return false;

        // Constant-time comparison to prevent timing attacks
        return ConstantTimeEquals(token, storedToken);
    }

    /// <summary>
    /// Generates a new CSRF token for a session.
    /// </summary>
    public async Task<string> GenerateCsrfTokenAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId));

        var tokenBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }

        var token = Convert.ToBase64String(tokenBytes);
        var key = $"csrf_{sessionId}";
        CsrfTokens.AddOrUpdate(key, token, (_, _) => token);

        return token;
    }

    /// <summary>
    /// Validates a file path to prevent directory traversal attacks.
    /// </summary>
    public async Task<bool> ValidateFilePathAsync(string path, string basePath)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(basePath))
            return false;

        try
        {
            // Normalize paths to prevent bypass attempts
            var normalizedPath = Path.GetFullPath(path);
            var normalizedBase = Path.GetFullPath(basePath);

            // Check for dangerous patterns
            foreach (var pattern in DangerousPatterns)
            {
                if (normalizedPath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Verify path is within allowed base directory
            return normalizedPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Encodes user input to prevent XSS attacks with double encoding support.
    /// </summary>
    public string EncodeForXss(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // HTML encode using .NET's built-in encoder
        var encoded = WebUtility.HtmlEncode(input);
        
        // Additional encoding for double encoding prevention
        encoded = encoded
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#x27;", StringComparison.Ordinal)
            .Replace("/", "&#x2F;", StringComparison.Ordinal);

        return encoded;
    }

    /// <summary>
    /// Encodes input for safer SQL usage (parameterized queries are preferred).
    /// </summary>
    public string EncodeForSql(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Double single quotes for SQL escaping
        return input.Replace("'", "''", StringComparison.Ordinal);
    }

    private string SanitizeTextInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove control characters
        var sb = new StringBuilder();
        foreach (var c in input)
        {
            if (!char.IsControl(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    private string SanitizeUrlInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            // Validate URL format
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
                return string.Empty;

            // Allow only safe schemes
            if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return uri.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private string SanitizePathInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove null characters and path traversal attempts
        var sanitized = input.Replace("\0", "", StringComparison.Ordinal);

        foreach (var separator in PathSeparators)
        {
            sanitized = sanitized.Replace($"{separator}..", "", StringComparison.Ordinal);
        }

        return sanitized;
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a == null || b == null)
            return a == b;

        int result = a.Length ^ b.Length;
        int minLength = Math.Min(a.Length, b.Length);

        for (int i = 0; i < minLength; i++)
            result |= a[i] ^ b[i];

        return result == 0;
    }

    private static async Task CleanupExpiredBucketsAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(5));

            var expiredKeys = RateLimitBuckets
                .Where(x => x.Value.IsExpired)
                .Select(x => x.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                RateLimitBuckets.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Represents a rate limit bucket with sliding window algorithm.
    /// </summary>
    private class RateLimitBucket
    {
        private readonly int _maxRequests;
        private readonly int _windowSeconds;
        private readonly Queue<DateTime> _requests = new();
        private DateTime _lastCleanup = DateTime.UtcNow;

        public bool IsExpired => (DateTime.UtcNow - _lastCleanup).TotalSeconds > _windowSeconds;

        public RateLimitBucket(int maxRequests, int windowSeconds)
        {
            _maxRequests = maxRequests;
            _windowSeconds = windowSeconds;
        }

        public bool IsRequestAllowed()
        {
            lock (_requests)
            {
                var now = DateTime.UtcNow;
                var windowStart = now.AddSeconds(-_windowSeconds);

                // Remove requests outside the window
                while (_requests.Count > 0 && _requests.Peek() < windowStart)
                    _requests.Dequeue();

                if (_requests.Count < _maxRequests)
                {
                    _requests.Enqueue(now);
                    return true;
                }

                return false;
            }
        }
    }
}

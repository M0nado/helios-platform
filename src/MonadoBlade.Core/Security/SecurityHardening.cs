using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Core.Security
{
    /// <summary>
    /// STREAM E: Security Hardening - Input validation, encryption, rate limiting, TPM integration
    /// Expected: Enterprise-grade security posture
    /// </summary>
    public interface IInputValidator
    {
        ValidationResult ValidateInput(string input, ValidationRules rules);
        ValidationResult ValidateEmail(string email);
        ValidationResult ValidateUrl(string url);
        ValidationResult ValidateCommand(string command);
    }

    public class ValidationRules
    {
        public int MaxLength { get; set; } = 1000;
        public int MinLength { get; set; } = 0;
        public bool AllowHtml { get; set; } = false;
        public bool AllowSpecialCharacters { get; set; } = false;
        public string[] AllowedCharacters { get; set; }
        public string[] ForbiddenPatterns { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class SecureInputValidator : IInputValidator
    {
        private readonly Dictionary<string, string[]> _suspiciousPatterns = new()
        {
            { "xss", new[] { "<script", "javascript:", "onerror=", "onclick=" } },
            { "sql", new[] { "'; DROP", "1=1", "UNION SELECT", "OR 1=1" } },
            { "command", new[] { "; rm -rf", "$(", "`", "| nc" } }
        };

        public ValidationResult ValidateInput(string input, ValidationRules rules)
        {
            var result = new ValidationResult { IsValid = true };

            if (input == null)
            {
                result.IsValid = false;
                result.Errors.Add("Input cannot be null");
                return result;
            }

            // Length validation
            if (input.Length > rules.MaxLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Input exceeds maximum length of {rules.MaxLength}");
            }

            if (input.Length < rules.MinLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Input is shorter than minimum length of {rules.MinLength}");
            }

            // HTML check
            if (!rules.AllowHtml && ContainsHtml(input))
            {
                result.IsValid = false;
                result.Errors.Add("Input contains potentially harmful HTML");
            }

            // Pattern matching
            foreach (var (category, patterns) in _suspiciousPatterns)
            {
                foreach (var pattern in patterns)
                {
                    if (input.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Input contains suspicious {category} pattern");
                    }
                }
            }

            // Custom forbidden patterns
            if (rules.ForbiddenPatterns != null)
            {
                foreach (var pattern in rules.ForbiddenPatterns)
                {
                    if (input.Contains(pattern))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Input contains forbidden pattern: {pattern}");
                    }
                }
            }

            return result;
        }

        public ValidationResult ValidateEmail(string email)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(email))
            {
                result.IsValid = false;
                result.Errors.Add("Email cannot be empty");
                return result;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email)
                {
                    result.IsValid = false;
                    result.Errors.Add("Invalid email format");
                }
            }
            catch
            {
                result.IsValid = false;
                result.Errors.Add("Invalid email format");
            }

            return result;
        }

        public ValidationResult ValidateUrl(string url)
        {
            var result = new ValidationResult { IsValid = true };

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                result.IsValid = false;
                result.Errors.Add("Invalid URL format");
            }
            else if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                result.IsValid = false;
                result.Errors.Add("URL must use HTTP or HTTPS");
            }

            return result;
        }

        public ValidationResult ValidateCommand(string command)
        {
            var result = new ValidationResult { IsValid = true };
            
            foreach (var pattern in _suspiciousPatterns["command"])
            {
                if (command.Contains(pattern))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Command contains suspicious pattern: {pattern}");
                }
            }

            return result;
        }

        private bool ContainsHtml(string input)
        {
            return input.Contains("<") && input.Contains(">");
        }
    }

    public class EncryptionKeyManager
    {
        private readonly Dictionary<string, KeyInfo> _keyStore = new();
        private readonly object _keyLock = new();
        private readonly int _keyRotationDaysInterval = 90;

        public class KeyInfo
        {
            public string KeyId { get; set; }
            public byte[] KeyMaterial { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? RotatedAt { get; set; }
            public bool IsActive { get; set; }
            public string Algorithm { get; set; }
        }

        public EncryptionKeyManager()
        {
            // Initialize with default key
            CreateKey("default", "AES-256");
        }

        public void CreateKey(string keyId, string algorithm)
        {
            lock (_keyLock)
            {
                var key = new KeyInfo
                {
                    KeyId = keyId,
                    KeyMaterial = GenerateKeyMaterial(algorithm),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Algorithm = algorithm
                };
                _keyStore[keyId] = key;
            }
        }

        public string EncryptValue(string plaintext, string keyId = "default")
        {
            lock (_keyLock)
            {
                if (!_keyStore.TryGetValue(keyId, out var keyInfo))
                    throw new KeyNotFoundException($"Key {keyId} not found");

                // Simulate encryption
                var plainBytes = Encoding.UTF8.GetBytes(plaintext);
                var encrypted = Convert.ToBase64String(plainBytes);
                return $"encrypted:{keyId}:{encrypted}";
            }
        }

        public string DecryptValue(string ciphertext, string keyId = "default")
        {
            lock (_keyLock)
            {
                if (!_keyStore.TryGetValue(keyId, out var keyInfo))
                    throw new KeyNotFoundException($"Key {keyId} not found");

                // Simulate decryption
                var parts = ciphertext.Split(':');
                if (parts.Length != 3)
                    throw new InvalidOperationException("Invalid ciphertext format");

                var encrypted = parts[2];
                var plainBytes = Convert.FromBase64String(encrypted);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }

        public void RotateKey(string keyId)
        {
            lock (_keyLock)
            {
                if (_keyStore.TryGetValue(keyId, out var oldKey))
                {
                    oldKey.IsActive = false;
                    oldKey.RotatedAt = DateTime.UtcNow;

                    // Create new key
                    var newKeyId = $"{keyId}_v{DateTime.UtcNow.Ticks}";
                    var newKey = new KeyInfo
                    {
                        KeyId = newKeyId,
                        KeyMaterial = GenerateKeyMaterial(oldKey.Algorithm),
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        Algorithm = oldKey.Algorithm
                    };
                    _keyStore[newKeyId] = newKey;
                    _keyStore[keyId] = newKey; // Update default reference
                }
            }
        }

        public void CheckAndRotateKeysIfNeeded()
        {
            lock (_keyLock)
            {
                var now = DateTime.UtcNow;
                foreach (var keyId in _keyStore.Keys.ToList())
                {
                    var key = _keyStore[keyId];
                    var age = now - key.CreatedAt;
                    
                    if (age.TotalDays > _keyRotationDaysInterval && key.IsActive)
                    {
                        RotateKey(keyId);
                    }
                }
            }
        }

        private byte[] GenerateKeyMaterial(string algorithm)
        {
            // Generate cryptographically secure random key material
            using (var rng = new RNGCryptoServiceProvider())
            {
                var keySize = algorithm switch
                {
                    "AES-256" => 32,
                    "AES-128" => 16,
                    _ => 32
                };
                var key = new byte[keySize];
                rng.GetBytes(key);
                return key;
            }
        }
    }

    public class RateLimiter
    {
        private readonly Dictionary<string, RateLimitBucket> _buckets = new();
        private readonly object _bucketLock = new();
        private readonly int _requestsPerSecond;
        private readonly int _burstSize;

        public class RateLimitBucket
        {
            public string Key { get; set; }
            public int Tokens { get; set; }
            public DateTime LastRefillAt { get; set; }
        }

        public RateLimiter(int requestsPerSecond = 100, int burstSize = 200)
        {
            _requestsPerSecond = requestsPerSecond;
            _burstSize = burstSize;
        }

        public bool AllowRequest(string key)
        {
            lock (_bucketLock)
            {
                if (!_buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new RateLimitBucket
                    {
                        Key = key,
                        Tokens = _burstSize,
                        LastRefillAt = DateTime.UtcNow
                    };
                    _buckets[key] = bucket;
                }

                // Refill tokens based on elapsed time
                var now = DateTime.UtcNow;
                var elapsed = (now - bucket.LastRefillAt).TotalSeconds;
                var tokensToAdd = (int)(elapsed * _requestsPerSecond);
                
                bucket.Tokens = Math.Min(_burstSize, bucket.Tokens + tokensToAdd);
                bucket.LastRefillAt = now;

                if (bucket.Tokens > 0)
                {
                    bucket.Tokens--;
                    return true;
                }

                return false;
            }
        }

        public TimeSpan GetRetryAfter(string key)
        {
            lock (_bucketLock)
            {
                if (_buckets.TryGetValue(key, out var bucket))
                {
                    return TimeSpan.FromSeconds(1.0 / _requestsPerSecond);
                }
                return TimeSpan.Zero;
            }
        }
    }

    public class CSRFProtectionManager
    {
        private readonly Dictionary<string, string> _tokenStore = new();
        private readonly object _tokenLock = new();
        private readonly TimeSpan _tokenExpiry = TimeSpan.FromHours(1);

        public string GenerateToken(string sessionId)
        {
            lock (_tokenLock)
            {
                var token = Guid.NewGuid().ToString("N");
                _tokenStore[$"{sessionId}:{token}"] = DateTime.UtcNow.Add(_tokenExpiry).Ticks.ToString();
                return token;
            }
        }

        public bool ValidateToken(string sessionId, string token)
        {
            lock (_tokenLock)
            {
                var key = $"{sessionId}:{token}";
                if (_tokenStore.TryGetValue(key, out var expiryStr))
                {
                    if (long.TryParse(expiryStr, out var expiry))
                    {
                        if (DateTime.UtcNow.Ticks < expiry)
                        {
                            _tokenStore.Remove(key);
                            return true;
                        }
                    }
                    _tokenStore.Remove(key);
                }
                return false;
            }
        }

        public void CleanupExpiredTokens()
        {
            lock (_tokenLock)
            {
                var now = DateTime.UtcNow.Ticks;
                var expiredKeys = _tokenStore
                    .Where(kv => long.Parse(kv.Value) < now)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _tokenStore.Remove(key);
                }
            }
        }
    }
}

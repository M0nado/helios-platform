using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HELIOS.CloudIntegration.Auth
{
    /// <summary>
    /// Base authentication interface for all cloud services
    /// </summary>
    public interface ICloudAuthenticator
    {
        string ServiceName { get; }
        Task<AuthenticationResult> AuthenticateAsync();
        Task<bool> IsValidAsync();
        Task RefreshTokenAsync();
        Dictionary<string, string> GetAuthHeaders();
    }

    /// <summary>
    /// Result of authentication attempt
    /// </summary>
    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Factory for creating authenticators
    /// </summary>
    public class AuthenticationFactory
    {
        private readonly HttpClient _httpClient;
        private readonly ICredentialStore _credentialStore;

        public AuthenticationFactory(HttpClient httpClient, ICredentialStore credentialStore)
        {
            _httpClient = httpClient;
            _credentialStore = credentialStore;
        }

        public ICloudAuthenticator CreateAuthenticator(string serviceName, Dictionary<string, string> config)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentException("Service name is required.", nameof(serviceName));
            }

            return serviceName.Trim().ToLowerInvariant() switch
            {
                "azure" => new AzureAuthenticator(_httpClient, _credentialStore, config),
                "openai" => new APIKeyAuthenticator(_credentialStore, config),
                "github" => new GitHubAuthenticator(_httpClient, _credentialStore, config),
                "claude" => new APIKeyAuthenticator(_credentialStore, config),
                "office365" => new OAuthAuthenticator(_httpClient, _credentialStore, config),
                "fabric" => new OAuthAuthenticator(_httpClient, _credentialStore, config),
                "copilot" => new OAuthAuthenticator(_httpClient, _credentialStore, config),
                _ => throw new ArgumentException($"Unknown service: {serviceName}")
            };
        }
    }

    /// <summary>
    /// Azure Service Principal Authentication
    /// </summary>
    public class AzureAuthenticator : ICloudAuthenticator
    {
        private readonly HttpClient _httpClient;
        private readonly ICredentialStore _credentialStore;
        private readonly Dictionary<string, string> _config;
        private AuthenticationResult? _cachedResult;

        public string ServiceName => "Azure";

        public AzureAuthenticator(HttpClient httpClient, ICredentialStore credentialStore, Dictionary<string, string> config)
        {
            _httpClient = httpClient;
            _credentialStore = credentialStore;
            _config = config;
        }

        public async Task<AuthenticationResult> AuthenticateAsync()
        {
            try
            {
                var tenantId = _credentialStore.GetSecret("AZURE_TENANT_ID");
                var clientId = _credentialStore.GetSecret("AZURE_CLIENT_ID");
                var clientSecret = _credentialStore.GetSecret("AZURE_CLIENT_SECRET");

                var missing = AuthenticationHelpers.GetMissingSecrets(("AZURE_TENANT_ID", tenantId), ("AZURE_CLIENT_ID", clientId), ("AZURE_CLIENT_SECRET", clientSecret));
                if (missing.Count > 0)
                {
                    return AuthenticationHelpers.AuthenticationFailure($"Azure authentication is missing required secret(s): {string.Join(", ", missing)}");
                }

                var tokenUrl = $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenantId!)}/oauth2/v2.0/token";

                var request = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", clientId! },
                    { "client_secret", clientSecret! },
                    { "scope", "https://management.azure.com/.default" }
                };

                var content = new FormUrlEncodedContent(request);
                var response = await _httpClient.PostAsync(tokenUrl, content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = await AuthenticationHelpers.DeserializeTokenResponseAsync<AzureTokenResponse>(response).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
                    {
                        return AuthenticationHelpers.AuthenticationFailure("Azure authentication succeeded but no access token was returned.");
                    }
                    _cachedResult = new AuthenticationResult
                    {
                        Success = true,
                        Token = tokenResponse.AccessToken,
                        ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                        Metadata = new Dictionary<string, object>
                        {
                            { "token_type", tokenResponse.TokenType ?? "Bearer" },
                            { "scope", tokenResponse.Scope ?? string.Empty }
                        }
                    };
                    return _cachedResult;
                }

                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = $"Azure authentication failed: {(int)response.StatusCode} {response.ReasonPhrase}"
                };
            }
            catch (Exception ex)
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = $"Azure authentication error: {ex.Message}"
                };
            }
        }

        public async Task<bool> IsValidAsync()
        {
            if (_cachedResult == null || DateTime.UtcNow >= _cachedResult.ExpiresAt.AddMinutes(-5))
            {
                var result = await AuthenticateAsync();
                return result.Success;
            }
            return true;
        }

        public async Task RefreshTokenAsync()
        {
            await AuthenticateAsync();
        }

        public Dictionary<string, string> GetAuthHeaders()
        {
            return _cachedResult?.Success == true
                ? new() { { "Authorization", $"Bearer {_cachedResult.Token}" } }
                : new();
        }

        private sealed class AzureTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }

            [JsonPropertyName("scope")]
            public string? Scope { get; set; }
        }
    }

    /// <summary>
    /// API Key Authentication (OpenAI, Claude, etc.)
    /// </summary>
    public class APIKeyAuthenticator : ICloudAuthenticator
    {
        private readonly ICredentialStore _credentialStore;
        private readonly Dictionary<string, string> _config;

        public string ServiceName { get; }

        public APIKeyAuthenticator(ICredentialStore credentialStore, Dictionary<string, string> config)
        {
            _credentialStore = credentialStore;
            _config = config;
            ServiceName = config.ContainsKey("service") ? config["service"] : "APIKey";
        }

        public Task<AuthenticationResult> AuthenticateAsync()
        {
            try
            {
                var keyName = $"{ServiceName.ToUpperInvariant()}_API_KEY";
                var apiKey = _credentialStore.GetSecret(keyName);

                if (string.IsNullOrEmpty(apiKey))
                {
                    return Task.FromResult(new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = $"API key not found: {keyName}"
                    });
                }

                return Task.FromResult(new AuthenticationResult
                {
                    Success = true,
                    Token = apiKey,
                    ExpiresAt = DateTime.MaxValue
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = $"API key authentication error: {ex.Message}"
                });
            }
        }

        public Task<bool> IsValidAsync()
        {
            var apiKey = _credentialStore.GetSecret($"{ServiceName.ToUpperInvariant()}_API_KEY");
            return Task.FromResult(!string.IsNullOrWhiteSpace(apiKey));
        }
        public Task RefreshTokenAsync() => Task.CompletedTask;

        public Dictionary<string, string> GetAuthHeaders()
        {
            var apiKey = _credentialStore.GetSecret($"{ServiceName.ToUpperInvariant()}_API_KEY");
            return string.IsNullOrWhiteSpace(apiKey)
                ? new()
                : new() { { "Authorization", $"Bearer {apiKey}" } };
        }
    }

    /// <summary>
    /// GitHub Token Authentication
    /// </summary>
    public class GitHubAuthenticator : ICloudAuthenticator
    {
        private readonly HttpClient _httpClient;
        private readonly ICredentialStore _credentialStore;
        private readonly Dictionary<string, string> _config;

        public string ServiceName => "GitHub";

        public GitHubAuthenticator(HttpClient httpClient, ICredentialStore credentialStore, Dictionary<string, string> config)
        {
            _httpClient = httpClient;
            _credentialStore = credentialStore;
            _config = config;
        }

        public Task<AuthenticationResult> AuthenticateAsync()
        {
            try
            {
                var token = _credentialStore.GetSecret("GITHUB_TOKEN");
                
                return Task.FromResult(new AuthenticationResult
                {
                    Success = !string.IsNullOrEmpty(token),
                    Token = token,
                    ExpiresAt = DateTime.MaxValue,
                    ErrorMessage = string.IsNullOrEmpty(token) ? "GitHub token not found" : null
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = $"GitHub authentication error: {ex.Message}"
                });
            }
        }

        public Task<bool> IsValidAsync()
        {
            var token = _credentialStore.GetSecret("GITHUB_TOKEN");
            return Task.FromResult(!string.IsNullOrWhiteSpace(token));
        }
        public Task RefreshTokenAsync() => Task.CompletedTask;

        public Dictionary<string, string> GetAuthHeaders()
        {
            var token = _credentialStore.GetSecret("GITHUB_TOKEN");
            return string.IsNullOrWhiteSpace(token)
                ? new()
                : new() { { "Authorization", $"Bearer {token}" } };
        }
    }

    /// <summary>
    /// OAuth 2.0 Authentication Handler
    /// </summary>
    public class OAuthAuthenticator : ICloudAuthenticator
    {
        private readonly HttpClient _httpClient;
        private readonly ICredentialStore _credentialStore;
        private readonly Dictionary<string, string> _config;
        private AuthenticationResult? _cachedResult;

        public string ServiceName { get; }

        public OAuthAuthenticator(HttpClient httpClient, ICredentialStore credentialStore, Dictionary<string, string> config)
        {
            _httpClient = httpClient;
            _credentialStore = credentialStore;
            _config = config;
            ServiceName = config.ContainsKey("service") ? config["service"] : "OAuth";
        }

        public async Task<AuthenticationResult> AuthenticateAsync()
        {
            try
            {
                var tenantId = _credentialStore.GetSecret("AZURE_TENANT_ID");
                var clientIdKey = $"{ServiceName.ToUpperInvariant()}_CLIENT_ID";
                var clientSecretKey = $"{ServiceName.ToUpperInvariant()}_CLIENT_SECRET";
                var clientId = _credentialStore.GetSecret(clientIdKey);
                var clientSecret = _credentialStore.GetSecret(clientSecretKey);

                var missing = AuthenticationHelpers.GetMissingSecrets(("AZURE_TENANT_ID", tenantId), (clientIdKey, clientId), (clientSecretKey, clientSecret));
                if (missing.Count > 0)
                {
                    return AuthenticationHelpers.AuthenticationFailure($"{ServiceName} authentication is missing required secret(s): {string.Join(", ", missing)}");
                }

                var tokenUrl = $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenantId!)}/oauth2/v2.0/token";

                var request = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", clientId! },
                    { "client_secret", clientSecret! },
                    { "scope", $"https://graph.microsoft.com/.default" }
                };

                var content = new FormUrlEncodedContent(request);
                var response = await _httpClient.PostAsync(tokenUrl, content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = await AuthenticationHelpers.DeserializeTokenResponseAsync<OAuthTokenResponse>(response).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
                    {
                        return AuthenticationHelpers.AuthenticationFailure($"{ServiceName} authentication succeeded but no access token was returned.");
                    }
                    _cachedResult = new AuthenticationResult
                    {
                        Success = true,
                        Token = tokenResponse.AccessToken,
                        ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                        Metadata = new Dictionary<string, object>
                        {
                            { "token_type", tokenResponse.TokenType ?? "Bearer" }
                        }
                    };
                    return _cachedResult;
                }

                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = $"OAuth authentication failed: {(int)response.StatusCode} {response.ReasonPhrase}"
                };
            }
            catch (Exception ex)
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = $"OAuth authentication error: {ex.Message}"
                };
            }
        }

        public async Task<bool> IsValidAsync()
        {
            if (_cachedResult == null || DateTime.UtcNow >= _cachedResult.ExpiresAt.AddMinutes(-5))
            {
                var result = await AuthenticateAsync();
                return result.Success;
            }
            return true;
        }

        public async Task RefreshTokenAsync()
        {
            await AuthenticateAsync();
        }

        public Dictionary<string, string> GetAuthHeaders()
        {
            return _cachedResult?.Success == true
                ? new() { { "Authorization", $"Bearer {_cachedResult.Token}" } }
                : new();
        }

        private sealed class OAuthTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }
        }
    }


    internal static class AuthenticationHelpers
    {
        private static readonly JsonSerializerOptions TokenJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task<T?> DeserializeTokenResponseAsync<T>(HttpResponseMessage response)
        {
            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(stream, TokenJsonOptions).ConfigureAwait(false);
        }

        public static List<string> GetMissingSecrets(params (string Name, string? Value)[] secrets)
        {
            return secrets
                .Where(secret => string.IsNullOrWhiteSpace(secret.Value))
                .Select(secret => secret.Name)
                .ToList();
        }

        public static AuthenticationResult AuthenticationFailure(string message) => new()
        {
            Success = false,
            ErrorMessage = message
        };
    }

    /// <summary>
    /// Credential storage interface
    /// </summary>
    public interface ICredentialStore
    {
        string GetSecret(string key);
        void StoreSecret(string key, string value);
        void RemoveSecret(string key);
    }
}

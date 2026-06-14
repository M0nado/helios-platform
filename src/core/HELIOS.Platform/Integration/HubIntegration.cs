using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HELIOS.Platform.Integration
{
    /// <summary>
    /// Provides REST APIs for integration with AI/Hub ecosystem for telemetry and feature synchronization.
    /// </summary>
    public class HubIntegration
    {
        private readonly HttpClient _httpClient;
        private readonly string _hubBaseUrl;
        private string _apiKey = string.Empty;
        private string _deviceId = string.Empty;
        private bool _isAuthenticated;
        private DateTime _lastSyncTime = DateTime.MinValue;
        private const int SyncIntervalMinutes = 5;

        public event EventHandler<SyncCompleteEventArgs>? SyncCompleted;
        public event EventHandler<AuthenticationEventArgs>? AuthenticationChanged;

        public class TelemetryData
        {
            public string DeviceId { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string MetricName { get; set; } = string.Empty;
            public double Value { get; set; }
            public Dictionary<string, string>? Tags { get; set; }
        }

        public class FeatureSyncData
        {
            public string FeatureId { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public Dictionary<string, object>? Configuration { get; set; }
            public DateTime LastUpdated { get; set; }
            public bool IsEnabled { get; set; }
        }

        public class CrossDeviceSyncRequest
        {
            public string UserId { get; set; } = string.Empty;
            public List<FeatureSyncData> Features { get; set; } = new();
            public Dictionary<string, string>? UserPreferences { get; set; }
            public DateTime SyncTimestamp { get; set; }
        }

        public class HubResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
            public Dictionary<string, string>? Errors { get; set; }
        }

        public HubIntegration(string hubBaseUrl, HttpClient? httpClient = null)
        {
            _hubBaseUrl = hubBaseUrl.TrimEnd('/');
            _httpClient = httpClient ?? new HttpClient();
            _deviceId = Guid.NewGuid().ToString();
        }

        public async Task<bool> AuthenticateAsync(string apiKey, string userId)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            try
            {
                var payload = new { apiKey, userId, deviceId = _deviceId };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var request = CreateRequest(HttpMethod.Post, "/api/v1/auth/authenticate");
                request.Content = content;
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await ParseResponse<Dictionary<string, string>>(response);
                    if (result?.Success == true)
                    {
                        _apiKey = apiKey;
                        _isAuthenticated = true;
                        AuthenticationChanged?.Invoke(this, new AuthenticationEventArgs { IsAuthenticated = true });
                        return true;
                    }
                }

                _isAuthenticated = false;
                AuthenticationChanged?.Invoke(this, new AuthenticationEventArgs { IsAuthenticated = false });
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendTelemetryAsync(TelemetryData telemetry)
        {
            if (!_isAuthenticated)
                return;

            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(telemetry),
                    Encoding.UTF8,
                    "application/json");

                using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/telemetry/record");
                request.Content = content;
                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();
            }
            catch
            {
                // Gracefully handle telemetry failures - don't impact main system
            }
        }

        public async Task<List<FeatureSyncData>> SyncFeaturesAsync(string userId)
        {
            if (!_isAuthenticated || (DateTime.UtcNow - _lastSyncTime).TotalMinutes < SyncIntervalMinutes)
                return new List<FeatureSyncData>();

            try
            {
                var path = $"/api/v1/features/sync?userId={Uri.EscapeDataString(userId)}&deviceId={Uri.EscapeDataString(_deviceId)}";
                using var request = CreateAuthenticatedRequest(HttpMethod.Get, path);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await ParseResponse<List<FeatureSyncData>>(response);
                    if (result?.Success == true && result.Data != null)
                    {
                        _lastSyncTime = DateTime.UtcNow;
                        SyncCompleted?.Invoke(this, new SyncCompleteEventArgs
                        {
                            FeaturesCount = result.Data.Count,
                            SyncTime = DateTime.UtcNow
                        });
                        return result.Data;
                    }
                }
            }
            catch
            {
                // Gracefully handle sync failures
            }

            return new List<FeatureSyncData>();
        }

        public async Task<bool> SyncUserPreferencesAsync(
            string userId,
            Dictionary<string, string> preferences)
        {
            if (!_isAuthenticated)
                return false;

            try
            {
                var payload = new
                {
                    userId,
                    deviceId = _deviceId,
                    preferences,
                    timestamp = DateTime.UtcNow
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/preferences/sync");
                request.Content = content;
                var response = await _httpClient.SendAsync(request);

                var result = await ParseResponse<Dictionary<string, string>>(response);
                return result?.Success == true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, string>? > GetUserPreferencesAsync(string userId)
        {
            if (!_isAuthenticated)
                return null;

            try
            {
                var path = $"/api/v1/preferences/user?userId={Uri.EscapeDataString(userId)}";
                using var request = CreateAuthenticatedRequest(HttpMethod.Get, path);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await ParseResponse<Dictionary<string, string>>(response);
                    return result?.Data;
                }
            }
            catch
            {
                // Gracefully handle failures
            }

            return null;
        }

        public async Task<bool> SyncCrossDevicesAsync(
            string userId,
            List<FeatureSyncData> features,
            Dictionary<string, string>? preferences = null)
        {
            if (!_isAuthenticated)
                return false;

            try
            {
                var request = new CrossDeviceSyncRequest
                {
                    UserId = userId,
                    Features = features,
                    UserPreferences = preferences,
                    SyncTimestamp = DateTime.UtcNow
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                using var requestMessage = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/sync/cross-device");
                requestMessage.Content = content;
                var response = await _httpClient.SendAsync(requestMessage);

                var result = await ParseResponse<Dictionary<string, object>>(response);
                return result?.Success == true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>? > GetDeviceSyncStateAsync()
        {
            if (!_isAuthenticated)
                return null;

            try
            {
                var path = $"/api/v1/sync/state?deviceId={Uri.EscapeDataString(_deviceId)}";
                using var request = CreateAuthenticatedRequest(HttpMethod.Get, path);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await ParseResponse<Dictionary<string, object>>(response);
                    return result?.Data;
                }
            }
            catch
            {
                // Gracefully handle failures
            }

            return null;
        }

        public bool IsHealthy()
        {
            return _isAuthenticated && !string.IsNullOrWhiteSpace(_apiKey);
        }

        public bool IsAuthenticated => _isAuthenticated;

        public string GetDeviceId() => _deviceId;

        private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string path)
        {
            var request = CreateRequest(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Add("X-Device-Id", _deviceId);
            request.Headers.Add("X-Hub-Version", "1.0");
            return request;
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var relativePath = path.StartsWith("/", StringComparison.Ordinal) ? path : $"/{path}";
            return new HttpRequestMessage(method, $"{_hubBaseUrl}{relativePath}");
        }

        private async Task<HubResponse<T>?> ParseResponse<T>(HttpResponseMessage response)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<HubResponse<T>>(content);
                return result;
            }
            catch
            {
                return null;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_isAuthenticated)
            {
                try
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/auth/disconnect");
                    await _httpClient.SendAsync(request);
                }
                catch
                {
                    // Ignore disconnect errors
                }

                _isAuthenticated = false;
                _apiKey = string.Empty;
                AuthenticationChanged?.Invoke(this, new AuthenticationEventArgs { IsAuthenticated = false });
            }
        }
    }

    public class SyncCompleteEventArgs : EventArgs
    {
        public int FeaturesCount { get; set; }
        public DateTime SyncTime { get; set; }
    }

    public class AuthenticationEventArgs : EventArgs
    {
        public bool IsAuthenticated { get; set; }
    }
}

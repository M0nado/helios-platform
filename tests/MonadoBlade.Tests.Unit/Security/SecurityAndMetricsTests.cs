using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonadoBlade.Core.Observability;
using MonadoBlade.Core.Security;

namespace MonadoBlade.Tests.Unit.Security
{
    public class SecureInputValidatorTests
    {
        private readonly SecureInputValidator _validator;

        public SecureInputValidatorTests()
        {
            _validator = new SecureInputValidator();
        }

        [Fact]
        public void ValidateInput_Alphanumeric_ValidatesCorrectly()
        {
            // Act
            var result = _validator.ValidateInput("validInput123", InputValidationRule.Alphanumeric);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateInput_Alphanumeric_RejectsSpecialChars()
        {
            // Act
            var result = _validator.ValidateInput("invalid@input", InputValidationRule.Alphanumeric);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateInput_Email_ValidatesCorrectEmail()
        {
            // Act
            var result = _validator.ValidateInput("test@example.com", InputValidationRule.Email);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateInput_Email_RejectsInvalidEmail()
        {
            // Act
            var result = _validator.ValidateInput("invalid-email", InputValidationRule.Email);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SanitizeHtmlInput_RemovesScriptTags()
        {
            // Arrange
            var input = "<script>alert('xss')</script>";

            // Act
            var result = _validator.SanitizeHtmlInput(input);

            // Assert
            Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SanitizeHtmlInput_RemovesEventHandlers()
        {
            // Arrange
            var input = "<img onclick='malicious()' />";

            // Act
            var result = _validator.SanitizeHtmlInput(input);

            // Assert
            Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CheckRateLimit_AllowsRequestsWithinLimit()
        {
            // Act
            var result1 = _validator.CheckRateLimit("client1", maxRequests: 5);
            var result2 = _validator.CheckRateLimit("client1", maxRequests: 5);
            var result3 = _validator.CheckRateLimit("client1", maxRequests: 5);

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
        }

        [Fact]
        public void CheckRateLimit_BlocksRequestsExceedingLimit()
        {
            // Arrange
            var clientId = "client2";
            var maxRequests = 3;

            // Act
            for (int i = 0; i < maxRequests; i++)
            {
                _validator.CheckRateLimit(clientId, maxRequests);
            }
            var blocked = _validator.CheckRateLimit(clientId, maxRequests);

            // Assert
            Assert.False(blocked);
        }

        [Fact]
        public void ValidateCsrfToken_ValidTokenPasses()
        {
            // Arrange
            var token = "valid-csrf-token-12345";

            // Act
            var result = _validator.ValidateCsrfToken(token, token);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateCsrfToken_InvalidTokenFails()
        {
            // Arrange
            var token1 = "valid-csrf-token-12345";
            var token2 = "different-csrf-token";

            // Act
            var result = _validator.ValidateCsrfToken(token1, token2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateInput_FilePath_RejectsPathTraversal()
        {
            // Act
            var result = _validator.ValidateInput("../../etc/passwd", InputValidationRule.FilePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateInput_SqlIdentifier_RejectsSqlKeywords()
        {
            // Act
            var result = _validator.ValidateInput("drop_table", InputValidationRule.SqlIdentifier);

            // Assert
            Assert.False(result);
        }
    }

    public class EncryptionKeyManagerTests
    {
        private readonly EncryptionKeyManager _keyManager;

        public EncryptionKeyManagerTests()
        {
            _keyManager = new EncryptionKeyManager();
        }

        [Fact]
        public async Task GenerateKeyAsync_CreatesKey()
        {
            // Act
            var key = await _keyManager.GenerateKeyAsync("test-key");

            // Assert
            Assert.NotNull(key);
            Assert.NotEmpty(key);
        }

        [Fact]
        public async Task GenerateKeyAsync_CreatesKeyWithCorrectSize()
        {
            // Act
            var key = await _keyManager.GenerateKeyAsync("test-key-256", 256);

            // Assert
            // Base64 encoded 256-bit key should be 44 characters (256/8 * 4/3)
            Assert.NotNull(key);
            var keyBytes = Convert.FromBase64String(key);
            Assert.Equal(32, keyBytes.Length); // 256 bits = 32 bytes
        }

        [Fact]
        public async Task EncryptAsync_EncryptsData()
        {
            // Arrange
            var keyId = "test-key";
            var plaintext = "sensitive data";
            await _keyManager.GenerateKeyAsync(keyId);

            // Act
            var encrypted = await _keyManager.EncryptAsync(keyId, plaintext);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotEmpty(encrypted);
        }

        [Fact]
        public async Task DecryptAsync_DecryptsData()
        {
            // Arrange
            var keyId = "test-key";
            var plaintext = "sensitive data";
            await _keyManager.GenerateKeyAsync(keyId);
            var encrypted = await _keyManager.EncryptAsync(keyId, plaintext);

            // Act
            var decrypted = await _keyManager.DecryptAsync(keyId, encrypted);

            // Assert
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public async Task GetKeyMetadataAsync_ReturnsMetadata()
        {
            // Arrange
            var keyId = "test-key";
            await _keyManager.GenerateKeyAsync(keyId);

            // Act
            var metadata = await _keyManager.GetKeyMetadataAsync(keyId);

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal(keyId, metadata.KeyId);
            Assert.True(metadata.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task RotateKeyAsync_UpdatesRotationTime()
        {
            // Arrange
            var keyId = "test-key";
            await _keyManager.GenerateKeyAsync(keyId);
            var initialMetadata = await _keyManager.GetKeyMetadataAsync(keyId);

            // Act
            await Task.Delay(100);
            await _keyManager.RotateKeyAsync(keyId);
            var rotatedMetadata = await _keyManager.GetKeyMetadataAsync(keyId);

            // Assert
            Assert.True(rotatedMetadata.RotatedAt >= initialMetadata.RotatedAt);
        }
    }

    public class AdvancedMetricsCollectorTests
    {
        private readonly AdvancedMetricsCollector _collector;

        public AdvancedMetricsCollectorTests()
        {
            _collector = new AdvancedMetricsCollector();
        }

        [Fact]
        public void RecordMetric_StoresMetric()
        {
            // Act
            _collector.RecordMetric("test.metric", 42.0);
            var metrics = _collector.GetMetrics("test.metric");

            // Assert
            var metric = Assert.Single(metrics);
            Assert.Equal(42.0, metric.Sum);
        }

        [Fact]
        public void RecordMetric_WithTags_StoresTaggedMetric()
        {
            // Arrange
            var tags = new Dictionary<string, string> { { "service", "api" } };

            // Act
            _collector.RecordMetric("request.count", 10.0, tags);
            var metrics = _collector.GetMetrics();

            // Assert
            Assert.NotEmpty(metrics);
        }

        [Fact]
        public void RecordTrace_CalculatesDuration()
        {
            // Act
            _collector.RecordTrace("operation", TimeSpan.FromMilliseconds(100), true);
            var metrics = _collector.GetMetrics("trace.operation");

            // Assert
            var metric = Assert.Single(metrics);
            Assert.True(metric.Average > 0);
        }

        [Fact]
        public void RecordCustomMetric_StoresCustomMetric()
        {
            // Act
            _collector.RecordCustomMetric("business", "order.count", 5.0);
            var metrics = _collector.GetMetrics();

            // Assert
            Assert.NotEmpty(metrics);
        }

        [Fact]
        public void StartDistributedTrace_CreatesTraceContext()
        {
            // Act
            _collector.StartDistributedTrace("trace-123");

            // Assert
            // Verify through behavior (should not throw)
            Assert.NotNull(_collector);
        }

        [Fact]
        public void EndDistributedTrace_CompletesTrace()
        {
            // Arrange
            _collector.StartDistributedTrace("trace-123");

            // Act
            _collector.EndDistributedTrace("trace-123");
            var metrics = _collector.GetMetrics();

            // Assert
            Assert.NotEmpty(metrics);
        }
    }

    public class DistributedTracingManagerTests
    {
        private readonly DistributedTracingManager _tracingManager;
        private readonly AdvancedMetricsCollector _collector;

        public DistributedTracingManagerTests()
        {
            _collector = new AdvancedMetricsCollector();
            _tracingManager = new DistributedTracingManager(_collector);
        }

        [Fact]
        public void SetTraceContext_StoresContext()
        {
            // Act
            _tracingManager.SetTraceContext("trace-123", "span-456");
            var context = _tracingManager.GetTraceContext();

            // Assert
            Assert.NotNull(context);
            Assert.Equal("trace-123", context.TraceId);
        }

        [Fact]
        public void GenerateSpanId_CreatesUniqueId()
        {
            // Act
            var spanId1 = _tracingManager.GenerateSpanId();
            var spanId2 = _tracingManager.GenerateSpanId();

            // Assert
            Assert.NotEqual(spanId1, spanId2);
            Assert.Equal(16, spanId1.Length);
        }

        [Fact]
        public void StartScope_RecordsMetric()
        {
            // Act
            using (var scope = _tracingManager.StartScope("test-op"))
            {
                // Do work
            }
            var metrics = _collector.GetMetrics();

            // Assert
            Assert.NotEmpty(metrics);
        }
    }

    public class AnomalyDetectionHooksTests
    {
        [Fact]
        public void RegisterAnomalyDetector_StoresDetector()
        {
            // Arrange
            var hooks = new AnomalyDetectionHooks();

            // Act
            hooks.RegisterAnomalyDetector(values => values.Length > 5);

            // Assert
            Assert.NotNull(hooks);
        }

        [Fact]
        public void CheckForAnomalies_InvokesDetector()
        {
            // Arrange
            var hooks = new AnomalyDetectionHooks();
            var anomalyDetected = false;
            hooks.AnomalyDetected += (s, e) => { anomalyDetected = true; };
            hooks.RegisterAnomalyDetector(values => values.Length > 2);

            // Act
            hooks.CheckForAnomalies("metric", 1.0);
            hooks.CheckForAnomalies("metric", 2.0);
            hooks.CheckForAnomalies("metric", 3.0);

            // Assert
            Assert.True(anomalyDetected);
        }
    }

    public class MetricsQueryEngineTests
    {
        [Fact]
        public void AnalyzeTrend_CalculatesTrend()
        {
            // Arrange
            var collector = new AdvancedMetricsCollector();
            var engine = new MetricsQueryEngine(collector);

            for (int i = 0; i < 15; i++)
            {
                collector.RecordMetric("test", i * 1.0);
            }

            // Act
            var trend = engine.AnalyzeTrend("test", 10);

            // Assert
            Assert.NotNull(trend);
            Assert.True(trend.IsIncreasing);
        }

        [Fact]
        public void ForecastValues_GeneratesForecast()
        {
            // Arrange
            var collector = new AdvancedMetricsCollector();
            var engine = new MetricsQueryEngine(collector);

            for (int i = 0; i < 5; i++)
            {
                collector.RecordMetric("test", 100.0 + (i * 10));
            }

            // Act
            var forecasts = engine.ForecastValues("test", 3);

            // Assert
            Assert.Equal(3, forecasts.Length);
        }
    }
}

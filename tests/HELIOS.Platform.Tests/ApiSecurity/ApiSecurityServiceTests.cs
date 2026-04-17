using System;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using HELIOS.Platform.BackendServices.AuthService;

namespace HELIOS.Platform.Tests.ApiSecurity
{
    public class ApiSecurityServiceTests
    {
        private readonly Mock<ILogger<ApiSecurityService>> _mockLogger;
        private readonly ApiSecurityService _apiSecurityService;
        private readonly RateLimitConfig _rateLimitConfig;

        public ApiSecurityServiceTests()
        {
            _mockLogger = new Mock<ILogger<ApiSecurityService>>();
            _rateLimitConfig = new RateLimitConfig { RequestsPerSecond = 50, MaxBurstSize = 100 };
            _apiSecurityService = new ApiSecurityService(_mockLogger.Object, _rateLimitConfig);
        }

        [Fact]
        public void CheckRateLimit_WithinLimit_ReturnsTrue()
        {
            // Act & Assert
            for (int i = 0; i < 50; i++)
            {
                Assert.True(_apiSecurityService.CheckRateLimit("client1"));
            }
        }

        [Fact]
        public void CheckRateLimit_ExceedsLimit_ReturnsFalse()
        {
            // Arrange
            var clientId = "client2";

            // Act & Assert
            for (int i = 0; i < 100; i++)
            {
                _apiSecurityService.CheckRateLimit(clientId);
            }

            // This should exceed the limit
            var result = _apiSecurityService.CheckRateLimit(clientId);
            Assert.False(result);
        }

        [Fact]
        public void SignRequest_GeneratesConsistentSignature()
        {
            // Arrange
            var payload = "test_payload";
            var privateKey = "test_key_123";

            // Act
            var signature1 = _apiSecurityService.SignRequest(payload, privateKey);
            var signature2 = _apiSecurityService.SignRequest(payload, privateKey);

            // Assert
            Assert.Equal(signature1, signature2);
        }

        [Fact]
        public void VerifySignature_WithCorrectSignature_ReturnsTrue()
        {
            // Arrange
            var payload = "test_payload";
            var key = "test_key_123";
            var signature = _apiSecurityService.SignRequest(payload, key);

            // Act
            var result = _apiSecurityService.VerifySignature(payload, signature, key);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifySignature_WithIncorrectSignature_ReturnsFalse()
        {
            // Arrange
            var payload = "test_payload";
            var key = "test_key_123";
            var wrongSignature = "wrong_signature";

            // Act
            var result = _apiSecurityService.VerifySignature(payload, wrongSignature, key);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GenerateApiKey_ReturnsNonEmptyString()
        {
            // Act
            var apiKey = _apiSecurityService.GenerateApiKey();

            // Assert
            Assert.NotEmpty(apiKey);
            Assert.True(apiKey.Length > 0);
        }

        [Fact]
        public void GenerateApiKey_ReturnsDifferentKeys()
        {
            // Act
            var key1 = _apiSecurityService.GenerateApiKey();
            var key2 = _apiSecurityService.GenerateApiKey();

            // Assert
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void GetSecurityHeaders_IncludesRequiredHeaders()
        {
            // Act
            var headers = _apiSecurityService.GetSecurityHeaders();

            // Assert
            Assert.NotEmpty(headers);
            Assert.Contains("X-Content-Type-Options", headers.Keys);
            Assert.Contains("X-Frame-Options", headers.Keys);
            Assert.Contains("Strict-Transport-Security", headers.Keys);
            Assert.Contains("Content-Security-Policy", headers.Keys);
        }

        [Fact]
        public void ValidateContentSecurityPolicy_WithValidHeader_ReturnsTrue()
        {
            // Arrange
            var cspHeader = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self';";

            // Act
            var result = _apiSecurityService.ValidateContentSecurityPolicy(cspHeader);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateContentSecurityPolicy_WithInvalidHeader_ReturnsFalse()
        {
            // Arrange
            var cspHeader = "invalid-policy";

            // Act
            var result = _apiSecurityService.ValidateContentSecurityPolicy(cspHeader);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateStrictTransportSecurity_WithValidHeader_ReturnsTrue()
        {
            // Arrange
            var hstsHeader = "max-age=31536000; includeSubDomains; preload";

            // Act
            var result = _apiSecurityService.ValidateStrictTransportSecurity(hstsHeader);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateStrictTransportSecurity_WithInvalidHeader_ReturnsFalse()
        {
            // Arrange
            var hstsHeader = "invalid-header";

            // Act
            var result = _apiSecurityService.ValidateStrictTransportSecurity(hstsHeader);

            // Assert
            Assert.False(result);
        }
    }
}

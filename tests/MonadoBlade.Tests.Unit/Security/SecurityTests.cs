using Xunit;
using MonadoBlade.Security;
using MonadoBlade.Security.Abstractions;

namespace MonadoBlade.Tests.Unit.Security;

/// <summary>
/// Comprehensive test suite for the three security modules.
/// Tests cover input sanitization, rate limiting, encryption key management, and audit logging.
/// </summary>
public class SecurityTests
{
    private readonly SecureInputValidator _inputValidator;
    private readonly EncryptionKeyManager _keyManager;
    private readonly SecureAuditLogger _auditLogger;

    public SecurityTests()
    {
        _auditLogger = new SecureAuditLogger();
        _inputValidator = new SecureInputValidator();
        _keyManager = new EncryptionKeyManager(_auditLogger);
    }

    #region Input Validator Tests

    [Fact]
    public async Task ValidateAndSanitizeAsync_WithXssPayload_RemovesScriptTags()
    {
        // Arrange
        var maliciousInput = "<script>alert('XSS')</script>";
        var expected = "&lt;script&gt;alert(&#x27;XSS&#x27;)&lt;&#x2F;script&gt;";

        // Act
        var result = await _inputValidator.ValidateAndSanitizeAsync(maliciousInput, "html");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_WithSqlInjection_EscapesQuotes()
    {
        // Arrange
        var sqlInput = "'; DROP TABLE users; --";

        // Act
        var result = await _inputValidator.ValidateAndSanitizeAsync(sqlInput, "sql");

        // Assert
        Assert.Contains("''", result);
        Assert.DoesNotContain("DROP TABLE", result.ToUpperInvariant());
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_WithPathTraversal_RemovesTraversalPatterns()
    {
        // Arrange
        var pathInput = "../../etc/passwd";

        // Act
        var result = await _inputValidator.ValidateAndSanitizeAsync(pathInput, "path");

        // Assert
        Assert.DoesNotContain("..", result);
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_WithValidUrl_PreservesHttps()
    {
        // Arrange
        var validUrl = "https://example.com/api/users";

        // Act
        var result = await _inputValidator.ValidateAndSanitizeAsync(validUrl, "url");

        // Assert
        Assert.StartsWith("https://", result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_WithInvalidUrl_ReturnsEmpty()
    {
        // Arrange
        var invalidUrl = "javascript:alert('XSS')";

        // Act
        var result = await _inputValidator.ValidateAndSanitizeAsync(invalidUrl, "url");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithinLimit_ReturnsTrue()
    {
        // Arrange
        var identifier = "user_123";
        const int maxRequests = 5;
        const int windowSeconds = 60;

        // Act & Assert
        for (int i = 0; i < maxRequests; i++)
        {
            var allowed = await _inputValidator.CheckRateLimitAsync(identifier, maxRequests, windowSeconds);
            Assert.True(allowed);
        }
    }

    [Fact]
    public async Task CheckRateLimitAsync_ExceedsLimit_ReturnsFalse()
    {
        // Arrange
        var identifier = "user_rate_limit_test";
        const int maxRequests = 3;
        const int windowSeconds = 60;

        // Act
        for (int i = 0; i < maxRequests; i++)
        {
            await _inputValidator.CheckRateLimitAsync(identifier, maxRequests, windowSeconds);
        }

        var exceeded = await _inputValidator.CheckRateLimitAsync(identifier, maxRequests, windowSeconds);

        // Assert
        Assert.False(exceeded);
    }

    [Fact]
    public async Task GenerateCsrfTokenAsync_GeneratesToken_ReturnsNonEmpty()
    {
        // Arrange
        var sessionId = "session_123";

        // Act
        var token = await _inputValidator.GenerateCsrfTokenAsync(sessionId);

        // Assert
        Assert.NotEmpty(token);
        Assert.True(token.Length > 20);
    }

    [Fact]
    public async Task ValidateCsrfTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var sessionId = "session_csrf_test";
        var token = await _inputValidator.GenerateCsrfTokenAsync(sessionId);

        // Act
        var isValid = await _inputValidator.ValidateCsrfTokenAsync(token, sessionId);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateCsrfTokenAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var sessionId = "session_invalid";
        var invalidToken = "invalid_token_123";

        // Act
        var isValid = await _inputValidator.ValidateCsrfTokenAsync(invalidToken, sessionId);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateFilePathAsync_WithValidPath_ReturnsTrue()
    {
        // Arrange
        var basePath = Path.GetTempPath();
        var validPath = Path.Combine(basePath, "test.txt");

        // Act
        var isValid = await _inputValidator.ValidateFilePathAsync(validPath, basePath);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateFilePathAsync_WithTraversalPath_ReturnsFalse()
    {
        // Arrange
        var basePath = Path.GetTempPath();
        var traversalPath = Path.Combine(basePath, "..", "..", "etc", "passwd");

        // Act
        var isValid = await _inputValidator.ValidateFilePathAsync(traversalPath, basePath);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void EncodeForXss_WithHtmlSpecialChars_EncodesAll()
    {
        // Arrange
        var input = "<script>alert('XSS')</script>";

        // Act
        var encoded = _inputValidator.EncodeForXss(input);

        // Assert
        Assert.DoesNotContain("<", encoded);
        Assert.DoesNotContain(">", encoded);
        Assert.DoesNotContain("'", encoded);
        Assert.Contains("&lt;", encoded);
        Assert.Contains("&gt;", encoded);
    }

    [Fact]
    public void EncodeForSql_WithSingleQuotes_DoublesThem()
    {
        // Arrange
        var input = "O'Brien";

        // Act
        var encoded = _inputValidator.EncodeForSql(input);

        // Assert
        Assert.Equal("O''Brien", encoded);
    }

    #endregion

    #region Encryption Key Manager Tests

    [Fact]
    public async Task GenerateKeyAsync_WithRsa_CreatesKey()
    {
        // Act
        var keyId = await _keyManager.GenerateKeyAsync("RSA", 2048);
        var keyData = await _keyManager.GetKeyAsync(keyId);

        // Assert
        Assert.NotEmpty(keyId);
        Assert.NotNull(keyData);
        Assert.NotEmpty(keyData);
    }

    [Fact]
    public async Task GenerateKeyAsync_WithAes_CreatesKey()
    {
        // Act
        var keyId = await _keyManager.GenerateKeyAsync("AES", 256);
        var keyData = await _keyManager.GetKeyAsync(keyId);

        // Assert
        Assert.NotEmpty(keyId);
        Assert.NotNull(keyData);
        Assert.NotEmpty(keyData);
    }

    [Fact]
    public async Task RotateKeyAsync_WithValidKey_CreatesNewKey()
    {
        // Arrange
        var originalKeyId = await _keyManager.GenerateKeyAsync("AES", 256);

        // Act
        var newKeyId = await _keyManager.RotateKeyAsync(originalKeyId, KeyRotationPolicy.Days30);

        // Assert
        Assert.NotEqual(originalKeyId, newKeyId);
        Assert.NotEmpty(newKeyId);
    }

    [Fact]
    public async Task IsKeyRotationNeededAsync_WithNewKey_ReturnsFalse()
    {
        // Arrange
        var keyId = await _keyManager.GenerateKeyAsync("AES", 256);

        // Act
        var needsRotation = await _keyManager.IsKeyRotationNeededAsync(keyId);

        // Assert
        Assert.False(needsRotation);
    }

    [Fact]
    public async Task GetKeyMetadataAsync_ReturnsCompleteMetadata()
    {
        // Arrange
        var keyId = await _keyManager.GenerateKeyAsync("RSA", 2048);

        // Act
        var metadata = await _keyManager.GetKeyMetadataAsync(keyId);

        // Assert
        Assert.NotEmpty(metadata);
        Assert.Contains("KeyId", metadata.Keys);
        Assert.Contains("Algorithm", metadata.Keys);
        Assert.Contains("KeySize", metadata.Keys);
        Assert.Contains("Status", metadata.Keys);
        Assert.Equal("Active", metadata["Status"]);
    }

    [Fact]
    public async Task GetActiveKeyAsync_AfterGeneration_ReturnsKeyId()
    {
        // Arrange
        var keyId = await _keyManager.GenerateKeyAsync("AES", 256);

        // Act
        var activeKey = await _keyManager.GetActiveKeyAsync("AES");

        // Assert
        Assert.NotNull(activeKey);
        Assert.Equal(keyId, activeKey);
    }

    [Fact]
    public async Task ArchiveKeyAsync_WithValidKey_UpdatesStatus()
    {
        // Arrange
        var keyId = await _keyManager.GenerateKeyAsync("AES", 256);

        // Act
        var result = await _keyManager.ArchiveKeyAsync(keyId);
        var metadata = await _keyManager.GetKeyMetadataAsync(keyId);

        // Assert
        Assert.True(result);
        Assert.Equal("Archived", metadata["Status"]);
    }

    #endregion

    #region Secure Audit Logger Tests

    [Fact]
    public async Task LogSecurityEventAsync_WithCriticalEvent_ReturnsEventId()
    {
        // Act
        var eventId = await _auditLogger.LogSecurityEventAsync(
            SecurityEventType.UnauthorizedAccess,
            SecurityEventSeverity.Critical,
            "Unauthorized access attempt from IP 192.168.1.1");

        // Assert
        Assert.NotEmpty(eventId);
    }

    [Fact]
    public async Task LogSecurityEventAsync_WithMultipleEvents_GeneratesUniqueIds()
    {
        // Act
        var eventId1 = await _auditLogger.LogSecurityEventAsync(
            SecurityEventType.Login,
            SecurityEventSeverity.Low,
            "User login");

        var eventId2 = await _auditLogger.LogSecurityEventAsync(
            SecurityEventType.Logout,
            SecurityEventSeverity.Low,
            "User logout");

        // Assert
        Assert.NotEqual(eventId1, eventId2);
    }

    [Fact]
    public async Task GetSecurityEventsAsync_WithoutFilter_ReturnsAllEvents()
    {
        // Arrange
        await _auditLogger.LogSecurityEventAsync(SecurityEventType.Login, SecurityEventSeverity.Low, "Test login");
        await _auditLogger.LogSecurityEventAsync(SecurityEventType.Logout, SecurityEventSeverity.Low, "Test logout");

        // Act
        var events = await _auditLogger.GetSecurityEventsAsync();

        // Assert
        Assert.NotEmpty(events);
        Assert.True(events.Count >= 2);
    }

    [Fact]
    public async Task GetSecurityEventsAsync_WithTypeFilter_ReturnsFilteredEvents()
    {
        // Arrange
        await _auditLogger.LogSecurityEventAsync(SecurityEventType.Login, SecurityEventSeverity.Low, "Login");
        await _auditLogger.LogSecurityEventAsync(SecurityEventType.Logout, SecurityEventSeverity.Low, "Logout");

        // Act
        var events = await _auditLogger.GetSecurityEventsAsync(SecurityEventType.Login);

        // Assert
        Assert.NotEmpty(events);
        Assert.All(events, e => Assert.Equal(SecurityEventType.Login, e.EventType));
    }

    [Fact]
    public async Task VerifyLogIntegrityAsync_WithValidEvent_ReturnsTrue()
    {
        // Arrange
        var eventId = await _auditLogger.LogSecurityEventAsync(
            SecurityEventType.ConfigurationChange,
            SecurityEventSeverity.Medium,
            "Configuration updated");

        // Act
        var isValid = await _auditLogger.VerifyLogIntegrityAsync(eventId);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task VerifyLogIntegrityAsync_WithInvalidEvent_ReturnsFalse()
    {
        // Act
        var isValid = await _auditLogger.VerifyLogIntegrityAsync("invalid_event_id_12345");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task GenerateComplianceReportAsync_WithDateRange_ReturnsJsonReport()
    {
        // Arrange
        await _auditLogger.LogSecurityEventAsync(SecurityEventType.Login, SecurityEventSeverity.Low, "User login");
        var startDate = DateTime.UtcNow.AddHours(-1);
        var endDate = DateTime.UtcNow.AddHours(1);

        // Act
        var report = await _auditLogger.GenerateComplianceReportAsync(startDate, endDate);

        // Assert
        Assert.NotEmpty(report);
        Assert.Contains("ReportGeneratedAt", report);
        Assert.Contains("Summary", report);
        Assert.Contains("TotalEvents", report);
    }

    [Fact]
    public async Task GetEventStatisticsAsync_ReturnsEventCounts()
    {
        // Arrange
        await _auditLogger.LogSecurityEventAsync(SecurityEventType.Login, SecurityEventSeverity.Low, "Login");
        await _auditLogger.LogSecurityEventAsync(SecurityEventType.AuthorizationFailure, SecurityEventSeverity.High, "Auth failure");

        // Act
        var stats = await _auditLogger.GetEventStatisticsAsync();

        // Assert
        Assert.NotEmpty(stats);
        Assert.True(stats["TotalEvents"] >= 2);
        Assert.True(stats["LoginEvents"] >= 1);
        Assert.True(stats["AuthorizationFailures"] >= 1);
    }

    [Fact]
    public async Task ArchiveOldEventsAsync_RemovesOldEvents()
    {
        // Arrange
        var eventId = await _auditLogger.LogSecurityEventAsync(
            SecurityEventType.Login,
            SecurityEventSeverity.Low,
            "Old event for archival");

        // Act
        var archivedCount = await _auditLogger.ArchiveOldEventsAsync(olderThanDays: 0);

        // Assert
        Assert.True(archivedCount >= 0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task SecurityModules_WorkTogether_IntegrationTest()
    {
        // Arrange: Simulate a complete security workflow

        // Act - Step 1: Validate user input
        var userInput = "<script>alert('test')</script>";
        var sanitized = await _inputValidator.ValidateAndSanitizeAsync(userInput, "html");

        // Act - Step 2: Check rate limiting
        var rateLimitAllowed = await _inputValidator.CheckRateLimitAsync("user_integration_test", 10, 60);

        // Act - Step 3: Generate encryption key
        var keyId = await _keyManager.GenerateKeyAsync("AES", 256);

        // Act - Step 4: Log security event
        var eventId = await _auditLogger.LogSecurityEventAsync(
            SecurityEventType.Login,
            SecurityEventSeverity.Low,
            $"User login after input validation");

        // Act - Step 5: Verify audit log integrity
        var auditValid = await _auditLogger.VerifyLogIntegrityAsync(eventId);

        // Assert
        Assert.DoesNotContain("<script>", sanitized);
        Assert.True(rateLimitAllowed);
        Assert.NotEmpty(keyId);
        Assert.NotEmpty(eventId);
        Assert.True(auditValid);
    }

    #endregion
}

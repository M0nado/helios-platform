using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using HELIOS.Platform.Components.SecurityDashboard;
using HELIOS.Platform.BackendServices.MalwarebytesIntegration;
using HELIOS.Platform.BackendServices.SecurityVault;

namespace HELIOS.Platform.Tests.Dashboard
{
    public class SecurityDashboardTests
    {
        private readonly Mock<IMalwarebytesIntegration> _mockMalwarebytes;
        private readonly Mock<ICredentialVault> _mockVault;
        private readonly Mock<ILogger<SecurityDashboard>> _mockLogger;
        private readonly SecurityDashboard _dashboard;

        public SecurityDashboardTests()
        {
            _mockMalwarebytes = new Mock<IMalwarebytesIntegration>();
            _mockVault = new Mock<ICredentialVault>();
            _mockLogger = new Mock<ILogger<SecurityDashboard>>();

            // Setup default mock returns
            _mockVault.Setup(v => v.IsVaultLockedAsync())
                .ReturnsAsync(true);

            _mockMalwarebytes.Setup(m => m.GetDetectionHistoryAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ThreatDetection>());

            _mockMalwarebytes.Setup(m => m.GetSystemStatusAsync())
                .ReturnsAsync(new Dictionary<string, object>
                {
                    { "RealTimeProtectionEnabled", true },
                    { "ActiveScans", 0 }
                });

            _dashboard = new SecurityDashboard(
                _mockMalwarebytes.Object,
                _mockVault.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetSecurityStatusAsync_ReturnsStatus()
        {
            // Act
            var status = await _dashboard.GetSecurityStatusAsync();

            // Assert
            Assert.NotNull(status);
            Assert.NotNull(status.HealthMetrics);
            Assert.NotNull(status.ComplianceIssues);
        }

        [Fact]
        public async Task GetSecurityStatusAsync_WithThreats_ReportsThreatCount()
        {
            // Arrange
            var threats = new List<ThreatDetection>
            {
                new ThreatDetection { Id = "1", Name = "Threat1", Severity = ThreatSeverity.Critical },
                new ThreatDetection { Id = "2", Name = "Threat2", Severity = ThreatSeverity.High }
            };

            _mockMalwarebytes.Setup(m => m.GetDetectionHistoryAsync(It.IsAny<int>()))
                .ReturnsAsync(threats);

            // Act
            var status = await _dashboard.GetSecurityStatusAsync();

            // Assert
            Assert.Equal(1, status.CriticalThreats);
            Assert.Equal(1, status.HighThreats);
        }

        [Fact]
        public async Task GetActiveAlertsAsync_ReturnsAlerts()
        {
            // Act
            var alerts = await _dashboard.GetActiveAlertsAsync();

            // Assert
            Assert.NotNull(alerts);
            Assert.IsType<List<SecurityAlert>>(alerts);
        }

        [Fact]
        public async Task GetRecentThreatsAsync_ReturnsThreatList()
        {
            // Arrange
            var threats = new List<ThreatDetection>
            {
                new ThreatDetection { Id = "1", Name = "Threat1", DetectedAt = DateTime.UtcNow },
                new ThreatDetection { Id = "2", Name = "Threat2", DetectedAt = DateTime.UtcNow }
            };

            _mockMalwarebytes.Setup(m => m.GetDetectionHistoryAsync(It.IsAny<int>()))
                .ReturnsAsync(threats);

            // Act
            var recentThreats = await _dashboard.GetRecentThreatsAsync(10);

            // Assert
            Assert.NotEmpty(recentThreats);
        }

        [Fact]
        public async Task GetThreatStatisticsAsync_ReturnsStats()
        {
            // Arrange
            var threats = new List<ThreatDetection>
            {
                new ThreatDetection { Id = "1", Severity = ThreatSeverity.Critical, IsQuarantined = true },
                new ThreatDetection { Id = "2", Severity = ThreatSeverity.High, IsQuarantined = false }
            };

            _mockMalwarebytes.Setup(m => m.GetDetectionHistoryAsync(It.IsAny<int>()))
                .ReturnsAsync(threats);

            // Act
            var stats = await _dashboard.GetThreatStatisticsAsync();

            // Assert
            Assert.NotEmpty(stats);
            Assert.Equal(2, stats["Total"]);
            Assert.Equal(1, stats["Critical"]);
        }

        [Fact]
        public async Task ApplySecurityPolicyAsync_WithStrictPolicy_Succeeds()
        {
            // Act
            var result = await _dashboard.ApplySecurityPolicyAsync("strict");

            // Assert
            Assert.True(result);
            _mockMalwarebytes.Verify(m => m.EnableRealTimeProtectionAsync(), Times.Once);
        }

        [Fact]
        public async Task ApplySecurityPolicyAsync_WithBalancedPolicy_Succeeds()
        {
            // Act
            var result = await _dashboard.ApplySecurityPolicyAsync("balanced");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ApplySecurityPolicyAsync_WithUnknownPolicy_Fails()
        {
            // Act
            var result = await _dashboard.ApplySecurityPolicyAsync("unknown");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RunSecurityAuditAsync_Succeeds()
        {
            // Arrange
            var scanResult = new ScanResult
            {
                ScanId = "scan1",
                StartTime = DateTime.UtcNow,
                IsComplete = false
            };

            _mockMalwarebytes.Setup(m => m.StartScanAsync(It.IsAny<ScanType>(), It.IsAny<string>()))
                .ReturnsAsync(scanResult);

            // Act
            var result = await _dashboard.RunSecurityAuditAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetComplianceStatusAsync_ReturnsComplianceInfo()
        {
            // Act
            var compliance = await _dashboard.GetComplianceStatusAsync();

            // Assert
            Assert.NotEmpty(compliance);
            Assert.Contains("VaultEncryption", compliance.Keys);
            Assert.Contains("RealTimeProtection", compliance.Keys);
            Assert.Contains("ThreatFree", compliance.Keys);
        }

        [Fact]
        public async Task GetComplianceStatusAsync_WithHighThreats_ReportsThreatCompliance()
        {
            // Arrange
            var threats = new List<ThreatDetection>
            {
                new ThreatDetection { Id = "1", Severity = ThreatSeverity.High }
            };

            _mockMalwarebytes.Setup(m => m.GetDetectionHistoryAsync(It.IsAny<int>()))
                .ReturnsAsync(threats);

            // Act
            var compliance = await _dashboard.GetComplianceStatusAsync();

            // Assert
            Assert.False(compliance["ThreatFree"]);
        }

        [Fact]
        public async Task GetSecurityStatusAsync_WithUnlockedVault_ReportsConcern()
        {
            // Arrange
            _mockVault.Setup(v => v.IsVaultLockedAsync())
                .ReturnsAsync(false);

            // Act
            var status = await _dashboard.GetSecurityStatusAsync();

            // Assert
            Assert.Contains("Vault", string.Join(",", status.ComplianceIssues));
        }
    }
}

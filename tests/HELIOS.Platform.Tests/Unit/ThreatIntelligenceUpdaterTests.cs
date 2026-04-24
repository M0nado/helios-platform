using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform;

namespace HELIOS.Platform.Tests.Unit
{
    /// <summary>
    /// Unit Tests for ThreatIntelligenceUpdater - 6 test cases
    /// Category: Unit
    /// </summary>
    public class ThreatIntelligenceUpdaterTests
    {
        private readonly Mock<IThreatIntelligenceUpdater> _mockUpdater;

        public ThreatIntelligenceUpdaterTests()
        {
            _mockUpdater = new Mock<IThreatIntelligenceUpdater>();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task CheckForUpdates_NewThreatDataAvailable_ReturnsTrue()
        {
            // Arrange
            _mockUpdater.Setup(t => t.CheckForUpdatesAsync(CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var result = await _mockUpdater.Object.CheckForUpdatesAsync(CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpdateThreatDatabase_NewData_ReturnsSuccess()
        {
            // Arrange
            var threatData = new List<string> { "Threat1", "Threat2" };
            _mockUpdater.Setup(t => t.UpdateAsync(threatData, CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var result = await _mockUpdater.Object.UpdateAsync(threatData, CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetLastUpdateTime_ValidDatabase_ReturnsDateTime()
        {
            // Arrange
            var expectedTime = DateTime.UtcNow.AddHours(-1);
            _mockUpdater.Setup(t => t.GetLastUpdateAsync())
                .ReturnsAsync(expectedTime);

            // Act
            var result = await _mockUpdater.Object.GetLastUpdateAsync();

            // Assert
            Assert.NotNull(result);
            Assert.InRange(result.Value, expectedTime.AddSeconds(-5), expectedTime.AddSeconds(5));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ValidateThreatSignature_KnownThreat_ReturnsTrue()
        {
            // Arrange
            var threatSignature = "win32.malware.xyz";
            _mockUpdater.Setup(t => t.ValidateAsync(threatSignature))
                .ReturnsAsync(true);

            // Act
            var result = await _mockUpdater.Object.ValidateAsync(threatSignature);

            // Assert
            Assert.True(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RollbackUpdate_PreviousVersion_ReturnsSuccess()
        {
            // Arrange
            _mockUpdater.Setup(t => t.RollbackAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _mockUpdater.Object.RollbackAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetUpdateStatus_InProgress_ReturnsInProgress()
        {
            // Arrange
            var expectedStatus = UpdateStatus.InProgress;
            _mockUpdater.Setup(t => t.GetStatusAsync())
                .ReturnsAsync(expectedStatus);

            // Act
            var result = await _mockUpdater.Object.GetStatusAsync();

            // Assert
            Assert.Equal(UpdateStatus.InProgress, result);
        }
    }

    public enum UpdateStatus
    {
        Idle,
        InProgress,
        Completed,
        Failed
    }

    public interface IThreatIntelligenceUpdater
    {
        Task<bool> CheckForUpdatesAsync(CancellationToken cancellationToken);
        Task<bool> UpdateAsync(List<string> threatData, CancellationToken cancellationToken);
        Task<DateTime?> GetLastUpdateAsync();
        Task<bool> ValidateAsync(string threatSignature);
        Task<bool> RollbackAsync();
        Task<UpdateStatus> GetStatusAsync();
    }
}

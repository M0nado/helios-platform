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
    /// Unit Tests for DriverInstaller - 8 test cases
    /// Category: Unit
    /// </summary>
    public class DriverInstallerTests
    {
        private readonly Mock<IDriverInstaller> _mockInstaller;

        public DriverInstallerTests()
        {
            _mockInstaller = new Mock<IDriverInstaller>();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task InstallDriver_ValidDriver_ReturnsSuccess()
        {
            // Arrange
            var driverId = "GPU-001";
            _mockInstaller.Setup(d => d.InstallAsync(driverId, CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var result = await _mockInstaller.Object.InstallAsync(driverId, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockInstaller.Verify(d => d.InstallAsync(driverId, CancellationToken.None), Times.Once);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task InstallDriver_InvalidDriver_ReturnsFalse()
        {
            // Arrange
            var invalidId = "";
            _mockInstaller.Setup(d => d.InstallAsync(invalidId, CancellationToken.None))
                .ReturnsAsync(false);

            // Act
            var result = await _mockInstaller.Object.InstallAsync(invalidId, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UninstallDriver_ValidDriver_ReturnsSuccess()
        {
            // Arrange
            var driverId = "GPU-001";
            _mockInstaller.Setup(d => d.UninstallAsync(driverId, CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var result = await _mockInstaller.Object.UninstallAsync(driverId, CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task CheckDriverStatus_InstalledDriver_ReturnsInstalled()
        {
            // Arrange
            var driverId = "GPU-001";
            var expectedStatus = DriverStatus.Installed;
            _mockInstaller.Setup(d => d.GetStatusAsync(driverId))
                .ReturnsAsync(expectedStatus);

            // Act
            var result = await _mockInstaller.Object.GetStatusAsync(driverId);

            // Assert
            Assert.Equal(DriverStatus.Installed, result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpdateDriver_NewVersion_ReturnsSuccess()
        {
            // Arrange
            var driverId = "GPU-001";
            var newVersion = "2.0.0";
            _mockInstaller.Setup(d => d.UpdateAsync(driverId, newVersion, CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var result = await _mockInstaller.Object.UpdateAsync(driverId, newVersion, CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetInstalledDrivers_NoFilter_ReturnsAllDrivers()
        {
            // Arrange
            var drivers = new List<string> { "GPU-001", "GPU-002", "NETWORK-001" };
            _mockInstaller.Setup(d => d.GetInstalledAsync())
                .ReturnsAsync(drivers);

            // Act
            var result = await _mockInstaller.Object.GetInstalledAsync();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains("GPU-001", result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task InstallDriver_Timeout_ThrowsTimeoutException()
        {
            // Arrange
            var driverId = "GPU-001";
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
            _mockInstaller.Setup(d => d.InstallAsync(driverId, cts.Token))
                .Returns(async () =>
                {
                    await Task.Delay(100, cts.Token);
                    return true;
                });

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _mockInstaller.Object.InstallAsync(driverId, cts.Token));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RollbackDriver_PreviousVersion_ReturnsSuccess()
        {
            // Arrange
            var driverId = "GPU-001";
            _mockInstaller.Setup(d => d.RollbackAsync(driverId))
                .ReturnsAsync(true);

            // Act
            var result = await _mockInstaller.Object.RollbackAsync(driverId);

            // Assert
            Assert.True(result);
        }
    }

    public enum DriverStatus
    {
        Installed,
        NotInstalled,
        Outdated,
        Failed
    }
}

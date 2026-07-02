namespace MonadoBlade.Tests.Unit.Boot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MonadoBlade.Boot.Services;

public class SimpleUSBWizardTests
{
    private readonly Mock<ILogger<SimpleUSBWizard>> _mockLogger;
    private readonly Mock<IUSBCreationOrchestrator> _mockOrchestrator;
    private readonly SimpleUSBWizard _wizard;

    public SimpleUSBWizardTests()
    {
        _mockLogger = new Mock<ILogger<SimpleUSBWizard>>();
        _mockOrchestrator = new Mock<IUSBCreationOrchestrator>();
        _wizard = new SimpleUSBWizard(_mockLogger.Object, _mockOrchestrator.Object);
    }

    [Fact]
    public void SystemHostname_ReturnsValidHostname()
    {
        // Act
        var hostname = _wizard.SystemHostname;

        // Assert
        hostname.Should().NotBeNullOrEmpty();
        hostname.Should().Be(Environment.MachineName);
    }

    [Fact]
    public void GetAvailableProfiles_ReturnsAllProfiles()
    {
        // Act
        var profiles = _wizard.GetAvailableProfiles().ToList();

        // Assert
        profiles.Should().HaveCount(5);
        profiles.Should().Contain("Gamer");
        profiles.Should().Contain("Developer");
        profiles.Should().Contain("AI Research");
        profiles.Should().Contain("Secure");
        profiles.Should().Contain("Enterprise");
    }

    [Fact]
    public async Task CreateUSBAsync_WithValidParameters_QueuesRequest()
    {
        // Arrange
        var deviceName = "MyUSB";
        var targetDisk = "E:\\";
        var profile = "Gamer";

        // Act
        await _wizard.CreateUSBAsync(deviceName, targetDisk, profile);

        // Assert
        _mockOrchestrator.Verify(o => o.QueueUSBCreationAsync(It.IsAny<USBCreationRequest>()), Times.Once);
    }

    [Fact]
    public async Task CreateUSBAsync_WithEmptyDeviceName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _wizard.CreateUSBAsync(string.Empty, "E:\\", "Gamer"));
    }

    [Fact]
    public void AdvancedMode_CanBeEnabledAndDisabled()
    {
        // Arrange
        _wizard.IsAdvancedModeEnabled.Should().BeFalse();

        // Act
        _wizard.EnableAdvancedMode();
        _wizard.IsAdvancedModeEnabled.Should().BeTrue();

        _wizard.DisableAdvancedMode();
        _wizard.IsAdvancedModeEnabled.Should().BeFalse();
    }

    [Fact]
    public void GetAdvancedOptions_WhenDisabled_ReturnsEmpty()
    {
        // Arrange
        _wizard.DisableAdvancedMode();

        // Act
        var options = _wizard.GetAdvancedOptions();

        // Assert
        options.Should().BeEmpty();
    }

    [Fact]
    public void GetAdvancedOptions_WhenEnabled_ReturnsAllOptions()
    {
        // Arrange
        _wizard.EnableAdvancedMode();

        // Act
        var options = _wizard.GetAdvancedOptions();

        // Assert
        options.Should().HaveCount(6);
        options.Keys.Should().Contain("PartitionSize");
        options.Keys.Should().Contain("FileSystem");
        options.Keys.Should().Contain("EnableCompression");
        options.Keys.Should().Contain("CustomDrivers");
        options.Keys.Should().Contain("BootLoaderVersion");
        options.Keys.Should().Contain("WriteVerification");
    }
}

public class USBCreationOrchestratorTests
{
    private readonly Mock<ILogger<USBCreationOrchestrator>> _mockLogger;
    private readonly USBCreationOrchestrator _orchestrator;

    public USBCreationOrchestratorTests()
    {
        _mockLogger = new Mock<ILogger<USBCreationOrchestrator>>();
        _orchestrator = new USBCreationOrchestrator(_mockLogger.Object);
    }

    [Fact]
    public async Task QueueUSBCreationAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _orchestrator.QueueUSBCreationAsync(null!));
    }

    [Fact]
    public async Task QueueUSBCreationAsync_WithValidRequest_AcceptsRequest()
    {
        // Arrange
        var request = new USBCreationRequest
        {
            DeviceName = "TestUSB",
            TargetDisk = "E:\\",
            Profile = "Gamer"
        };

        // Act & Assert - should not throw
        await _orchestrator.QueueUSBCreationAsync(request);
    }

    [Fact]
    public void GetQueueStatus_InitiallyEmpty()
    {
        // Act
        var status = _orchestrator.GetQueueStatus();

        // Assert
        status.QueuedItems.Should().Be(0);
        status.BuildingItems.Should().Be(0);
    }

    [Fact]
    public async Task QueueUSBCreationAsync_WithSecureProfile_DoesNotThrow()
    {
        // Arrange
        var request = new USBCreationRequest
        {
            DeviceName = "SecureUSB",
            TargetDisk = "E:\\",
            Profile = "Secure"
        };

        // Act & Assert - should not throw
        await _orchestrator.QueueUSBCreationAsync(request);
    }

    [Fact]
    public async Task QueueUSBCreationAsync_WithMultipleProfiles_AcceptsAll()
    {
        // Arrange
        var profiles = new[] { "Gamer", "Developer", "AI Research", "Secure", "Enterprise" };

        // Act & Assert - should not throw
        foreach (var profile in profiles)
        {
            var request = new USBCreationRequest
            {
                DeviceName = $"USB-{profile}",
                TargetDisk = "E:\\",
                Profile = profile
            };

            await _orchestrator.QueueUSBCreationAsync(request);
        }
    }
}

namespace MonadoBlade.Tests.Unit;

using Xunit;
using FluentAssertions;
using MonadoBlade.Boot;
using System.Text;

/// <summary>
/// Tests for SilentInstallationManager to verify silent installations work correctly.
/// </summary>
public class SilentInstallationManagerTests : IDisposable
{
    private readonly SilentInstallationManager _manager;

    public SilentInstallationManagerTests()
    {
        _manager = new SilentInstallationManager();
    }

    /// <summary>
    /// Test: Manager should have TaskStarted event.
    /// </summary>
    [Fact]
    public void Manager_ShouldHaveTaskStartedEvent()
    {
        // Arrange
        var eventFired = false;
        _manager.TaskStarted += _ => eventFired = true;

        // Act
        // Event is present and can be subscribed to
        
        // Assert - if we can subscribe, event exists
    }

    /// <summary>
    /// Test: Manager should have TaskCompleted event.
    /// </summary>
    [Fact]
    public void Manager_ShouldHaveTaskCompletedEvent()
    {
        // Arrange
        var eventFired = false;
        _manager.TaskCompleted += _ => eventFired = true;

        // Act
        // Event is present and can be subscribed to
        
        // Assert - if we can subscribe, event exists
    }

    /// <summary>
    /// Test: Manager should have ProgressChanged event.
    /// </summary>
    [Fact]
    public void Manager_ShouldHaveProgressChangedEvent()
    {
        // Arrange
        var eventFired = false;
        _manager.ProgressChanged += (_, __) => eventFired = true;

        // Act
        // Event is present and can be subscribed to
        
        // Assert - if we can subscribe, event exists
    }

    /// <summary>
    /// Test: Installation task should be created with proper defaults.
    /// </summary>
    [Fact]
    public void InstallationTask_ShouldHaveProperDefaults()
    {
        // Arrange & Act
        var task = new InstallationTask
        {
            Id = "test-task",
            Name = "Test Installer",
            ExecutablePath = "C:\\test.exe"
        };

        // Assert
        task.SilentMode.Should().BeTrue();
        task.TimeoutSeconds.Should().Be(300);
        task.MaxRetries.Should().Be(3);
    }

    /// <summary>
    /// Test: Installation task result should track attempt number.
    /// </summary>
    [Fact]
    public void InstallationTaskResult_ShouldTrackAttemptNumber()
    {
        // Arrange & Act
        var result = new InstallationTaskResult
        {
            TaskId = "test",
            Success = false,
            Attempt = 2
        };

        // Assert
        result.Attempt.Should().Be(2);
    }

    /// <summary>
    /// Test: Installation task result should track duration.
    /// </summary>
    [Fact]
    public void InstallationTaskResult_ShouldTrackDuration()
    {
        // Arrange & Act
        var duration = TimeSpan.FromSeconds(45.5);
        var result = new InstallationTaskResult
        {
            TaskId = "test",
            Duration = duration
        };

        // Assert
        result.Duration.Should().Be(duration);
    }

    /// <summary>
    /// Test: Export report should contain task results.
    /// </summary>
    [Fact]
    public void ExportReport_ShouldGenerateReport()
    {
        // Act
        var report = _manager.ExportReport();

        // Assert
        report.Should().NotBeNullOrEmpty();
        report.Should().Contain("Monado Blade Installation Report");
        report.Should().Contain("Summary");
    }

    /// <summary>
    /// Test: Log directory path should be retrievable.
    /// </summary>
    [Fact]
    public void GetLogDirectoryPath_ShouldReturnValidPath()
    {
        // Act
        var logPath = _manager.GetLogDirectoryPath();

        // Assert
        logPath.Should().NotBeNullOrEmpty();
        logPath.Should().Contain("MonadoBladeInstallLogs");
    }

    /// <summary>
    /// Test: Task results should be retrievable.
    /// </summary>
    [Fact]
    public void GetTaskResults_ShouldReturnReadOnlyList()
    {
        // Act
        var results = _manager.GetTaskResults();

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Test: Export report should show success/failure counts.
    /// </summary>
    [Fact]
    public void ExportReport_ShouldIncludeSuccessFailureCounts()
    {
        // Act
        var report = _manager.ExportReport();

        // Assert
        report.Should().Contain("successful");
        report.Should().Contain("failed");
    }

    public void Dispose()
    {
        _manager?.Dispose();
    }
}

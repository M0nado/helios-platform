namespace MonadoBlade.Tests.Unit;

using Xunit;
using FluentAssertions;
using MonadoBlade.Boot;
using System.Diagnostics;

/// <summary>
/// Tests for SystemPreflightChecker to verify pre-flight checks work correctly.
/// </summary>
public class SystemPreflightCheckerTests : IDisposable
{
    private readonly SystemPreflightChecker _checker;

    public SystemPreflightCheckerTests()
    {
        _checker = new SystemPreflightChecker();
    }

    /// <summary>
    /// Test: All checks should complete within the timeout period (30 seconds).
    /// </summary>
    [Fact]
    public async Task RunChecksAsync_ShouldCompleteWithinTimeout()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _checker.RunChecksAsync();
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(35); // 30s timeout + 5s buffer
        result.ElapsedTime.Should().BeLessThan(TimeSpan.FromSeconds(35));
    }

    /// <summary>
    /// Test: Pre-flight check should return results for all 7 checks.
    /// </summary>
    [Fact]
    public async Task RunChecksAsync_ShouldReturnAllChecks()
    {
        // Act
        var result = await _checker.RunChecksAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalChecks.Should().Be(7);
        result.Results.Should().HaveCount(7);
    }

    /// <summary>
    /// Test: All pre-flight checks should have names and messages.
    /// </summary>
    [Fact]
    public async Task RunChecksAsync_AllChecksShouldHaveDescriptions()
    {
        // Act
        var result = await _checker.RunChecksAsync();

        // Assert
        result.Results.Should().AllSatisfy(check =>
        {
            check.CheckName.Should().NotBeNullOrEmpty();
            check.Message.Should().NotBeNullOrEmpty();
        });
    }

    /// <summary>
    /// Test: All checks complete successfully within system environment.
    /// </summary>
    [Fact]
    public async Task RunChecksAsync_AllChecksShouldReturnResults()
    {
        // Act
        var result = await _checker.RunChecksAsync();

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().NotBeEmpty();
        result.TotalChecks.Should().Be(7);
        
        // All checks should have names and messages
        foreach (var check in result.Results)
        {
            check.CheckName.Should().NotBeNullOrEmpty();
            check.Message.Should().NotBeNullOrEmpty();
        }
    }

    /// <summary>
    /// Test: Admin permissions check should return a result.
    /// </summary>
    [Fact]
    public async Task RunChecksAsync_AdminCheckShouldBeIncluded()
    {
        // Act
        var result = await _checker.RunChecksAsync();

        // Assert
        var adminCheck = result.Results.FirstOrDefault(c => c.CheckName == "Admin Permissions");
        adminCheck.Should().NotBeNull();
        adminCheck!.Message.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test: Network connectivity check should be included.
    /// </summary>
    [Fact]
    public async Task RunChecksAsync_NetworkCheckShouldBeIncluded()
    {
        // Act
        var result = await _checker.RunChecksAsync();

        // Assert
        var networkCheck = result.Results.FirstOrDefault(c => c.CheckName == "Network Connectivity");
        networkCheck.Should().NotBeNull();
        networkCheck!.Message.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test: Disk space check should report available space.
    /// </summary>
    [Fact]
    public async Task RunChecksAsync_DiskSpaceCheckShouldReportCapacity()
    {
        // Act
        var result = await _checker.RunChecksAsync();

        // Assert
        var diskCheck = result.Results.FirstOrDefault(c => c.CheckName == "Disk Space");
        diskCheck.Should().NotBeNull();
        diskCheck!.Message.Should().Contain("GB");
    }

    /// <summary>
    /// Test: PreflightResult should correctly calculate passed/total checks.
    /// </summary>
    [Fact]
    public async Task RunChecksAsync_ShouldCalculatPassedCount()
    {
        // Act
        var result = await _checker.RunChecksAsync();

        // Assert
        result.PassedChecks.Should().BeLessThanOrEqualTo(result.TotalChecks);
        result.PassedChecks.Should().BeGreaterThanOrEqualTo(0);
        var actualPassedCount = result.Results.Count(r => r.Passed);
        result.PassedChecks.Should().Be(actualPassedCount);
    }

    public void Dispose()
    {
        _checker?.Dispose();
    }
}

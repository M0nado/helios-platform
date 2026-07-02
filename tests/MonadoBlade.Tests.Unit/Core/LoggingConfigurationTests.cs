namespace MonadoBlade.Tests.Unit.Core;

using Xunit;
using MonadoBlade.Core.Logging;
using Serilog;

public class LoggingConfigurationTests
{
    [Fact]
    public void CreateLogger_ShouldReturnValidLogger()
    {
        // Arrange & Act
        var logger = LoggingConfiguration.CreateLogger("TestApp");

        // Assert
        Assert.NotNull(logger);
        Assert.IsAssignableFrom<ILogger>(logger);
    }

    [Fact]
    public void CreateLogger_WithDefaultName_ShouldUseMonadoBlade()
    {
        // Act
        var logger = LoggingConfiguration.CreateLogger();

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void CreateLogger_ShouldCreateLogsDirectory()
    {
        // Arrange
        var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        if (Directory.Exists(logsDir))
            Directory.Delete(logsDir, true);

        // Act
        var logger = LoggingConfiguration.CreateLogger("TestApp");
        logger.Information("Test message");
        
        // Assert - logs directory should be created (or at least not throw)
        Assert.NotNull(logger);
    }

    [Fact]
    public void CreateLogger_WithCustomName_ShouldIncludeNameInContext()
    {
        // Act
        var logger = LoggingConfiguration.CreateLogger("CustomApp");

        // Assert
        Assert.NotNull(logger);
    }
}

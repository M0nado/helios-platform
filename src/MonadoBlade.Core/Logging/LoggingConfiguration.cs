namespace MonadoBlade.Core.Logging;

using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;
using Microsoft.Extensions.Logging;

/// <summary>
/// Configures and manages Serilog logging for the application.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Creates a configured logger instance with console and file sinks.
    /// </summary>
    public static ILogger CreateLogger(string applicationName = "MonadoBlade")
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", applicationName)
            .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
            .WriteTo.File(
                path: $"logs/{applicationName}-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Extends Microsoft.Extensions.Logging with Serilog.
    /// </summary>
    public static ILoggingBuilder AddSerilog(this ILoggingBuilder builder, string? applicationName = null)
    {
        var logger = CreateLogger(applicationName ?? "MonadoBlade");
        builder.AddSerilog(logger, dispose: true);
        return builder;
    }
}

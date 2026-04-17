using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HELIOS.Platform.Core.Logging
{
    /// <summary>
    /// Comprehensive Serilog configuration with multiple sinks and log levels
    /// </summary>
    public static class LoggerConfiguration
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string ArchiveDirectory = Path.Combine(LogDirectory, "Archive");
        private static readonly string CrashDumpDirectory = Path.Combine(LogDirectory, "CrashDumps");

        static LoggerConfiguration()
        {
            EnsureDirectoriesExist();
        }

        /// <summary>
        /// Configure the global Serilog logger with all sinks and levels
        /// </summary>
        public static void ConfigureGlobalLogger(LogEventLevel minimumLevel = LogEventLevel.Information)
        {
            var loggerConfig = new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "HELIOS.Platform")
                .Enrich.WithProperty("Environment", GetEnvironment())
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .Enrich.WithEnvironmentUserName();

            // Console sink
            loggerConfig = loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

            // File sink - rolling daily with size rollover
            loggerConfig = loggerConfig.WriteTo.File(
                path: Path.Combine(LogDirectory, "application-.txt"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 100 * 1024 * 1024,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

            // JSON structured logging sink
            loggerConfig = loggerConfig.WriteTo.File(
                formatter: new CompactJsonFormatter(),
                path: Path.Combine(LogDirectory, "application-structured-.json"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 100 * 1024 * 1024,
                retainedFileCountLimit: 30);

            // Error-specific file sink
            loggerConfig = loggerConfig.WriteTo.Logger(lc =>
                lc.Filter.ByIncludingOnly(le => le.Level >= LogEventLevel.Error)
                    .WriteTo.File(
                        path: Path.Combine(LogDirectory, "errors-.txt"),
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: 50 * 1024 * 1024,
                        retainedFileCountLimit: 30,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"));

            // Critical and Fatal error sink
            loggerConfig = loggerConfig.WriteTo.Logger(lc =>
                lc.Filter.ByIncludingOnly(le => le.Level >= LogEventLevel.Fatal)
                    .WriteTo.File(
                        path: Path.Combine(LogDirectory, "critical-.txt"),
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: 50 * 1024 * 1024,
                        retainedFileCountLimit: 90,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"));

            Log.Logger = loggerConfig.CreateLogger();
        }

        /// <summary>
        /// Get the current environment (Debug, Release, etc.)
        /// </summary>
        private static string GetEnvironment()
        {
#if DEBUG
            return "Development";
#else
            return "Production";
#endif
        }

        /// <summary>
        /// Ensure all necessary log directories exist
        /// </summary>
        private static void EnsureDirectoriesExist()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                Directory.CreateDirectory(ArchiveDirectory);
                Directory.CreateDirectory(CrashDumpDirectory);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to create log directories: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the log directory path
        /// </summary>
        public static string GetLogDirectory() => LogDirectory;

        /// <summary>
        /// Get the archive directory path
        /// </summary>
        public static string GetArchiveDirectory() => ArchiveDirectory;

        /// <summary>
        /// Get the crash dump directory path
        /// </summary>
        public static string GetCrashDumpDirectory() => CrashDumpDirectory;

        /// <summary>
        /// Archive old log files
        /// </summary>
        public static void ArchiveOldLogs(int daysToKeep = 7)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(LogDirectory, "*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f => File.GetCreationTimeUtc(f) < cutoffDate)
                    .ToList();

                foreach (var file in logFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        var archivePath = Path.Combine(ArchiveDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}_{fileName}");
                        File.Move(file, archivePath, true);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to archive log file {File}: {Exception}", file, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error archiving old logs");
            }
        }

        /// <summary>
        /// Close and flush the logger
        /// </summary>
        public static void CloseLogger()
        {
            Log.CloseAndFlush();
        }
    }
}

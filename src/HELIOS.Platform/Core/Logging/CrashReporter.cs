using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;

namespace HELIOS.Platform.Core.Logging
{
    /// <summary>
    /// Comprehensive crash reporting and recovery system
    /// </summary>
    public class CrashReporter
    {
        private static readonly string CrashDumpDirectory = LoggerConfiguration.GetCrashDumpDirectory();
        private static readonly ILogger _logger = Log.ForContext<CrashReporter>();

        /// <summary>
        /// Register global exception handlers for crash reporting
        /// </summary>
        public static void RegisterGlobalHandlers()
        {
            // Handle unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            // Handle task scheduler exceptions
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// Handle unhandled exceptions in the application domain
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                var isTerminating = e.IsTerminating;

                _logger.Fatal(exception, "Unhandled exception - IsTerminating: {IsTerminating}", isTerminating);
                CreateCrashDump("UnhandledException", exception, isTerminating);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in UnhandledException handler: {ex}");
            }
        }

        /// <summary>
        /// Handle unobserved task exceptions
        /// </summary>
        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                _logger.Fatal(e.Exception, "Unobserved task exception");
                CreateCrashDump("UnobservedTaskException", e.Exception, false);
                e.SetObserved();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in UnobservedTaskException handler: {ex}");
            }
        }

        /// <summary>
        /// Create a detailed crash dump file
        /// </summary>
        public static string CreateCrashDump(string dumpType, Exception exception, bool isTerminating = false)
        {
            try
            {
                Directory.CreateDirectory(CrashDumpDirectory);
                
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                var fileName = $"crash_{dumpType}_{timestamp}.txt";
                var filePath = Path.Combine(CrashDumpDirectory, fileName);

                var sb = new StringBuilder();
                sb.AppendLine($"CRASH DUMP REPORT");
                sb.AppendLine($"================================================================================");
                sb.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
                sb.AppendLine($"Machine: {Environment.MachineName}");
                sb.AppendLine($"User: {Environment.UserName}");
                sb.AppendLine($"OS Version: {Environment.OSVersion}");
                sb.AppendLine($"CLR Version: {Environment.Version}");
                sb.AppendLine($"Process ID: {Process.GetCurrentProcess().Id}");
                sb.AppendLine($"Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                sb.AppendLine($"Dump Type: {dumpType}");
                sb.AppendLine($"Is Terminating: {isTerminating}");
                sb.AppendLine();

                sb.AppendLine("EXCEPTION INFORMATION");
                sb.AppendLine("================================================================================");
                sb.Append(FormatException(exception));
                sb.AppendLine();

                sb.AppendLine("SYSTEM INFORMATION");
                sb.AppendLine("================================================================================");
                sb.AppendLine($"Processor Count: {Environment.ProcessorCount}");
                sb.AppendLine($"Physical Memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
                sb.AppendLine();

                sb.AppendLine("STACK TRACE");
                sb.AppendLine("================================================================================");
                sb.AppendLine(Environment.StackTrace);

                File.WriteAllText(filePath, sb.ToString());
                _logger.Information("Crash dump created: {DumpPath}", filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating crash dump: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Format exception with all inner exceptions
        /// </summary>
        private static string FormatException(Exception exception)
        {
            var sb = new StringBuilder();
            int level = 0;

            Exception current = exception;
            while (current != null)
            {
                var indent = new string(' ', level * 2);
                sb.AppendLine($"{indent}Exception Level {level}:");
                sb.AppendLine($"{indent}  Type: {current.GetType().FullName}");
                sb.AppendLine($"{indent}  Message: {current.Message}");
                sb.AppendLine($"{indent}  Stack Trace:");
                
                if (!string.IsNullOrWhiteSpace(current.StackTrace))
                {
                    foreach (var line in current.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                    {
                        sb.AppendLine($"{indent}    {line}");
                    }
                }

                if (current.Data.Count > 0)
                {
                    sb.AppendLine($"{indent}  Additional Data:");
                    foreach (var key in current.Data.Keys)
                    {
                        sb.AppendLine($"{indent}    {key}: {current.Data[key]}");
                    }
                }

                current = current.InnerException;
                level++;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get all crash dumps
        /// </summary>
        public static string[] GetCrashDumps()
        {
            try
            {
                if (!Directory.Exists(CrashDumpDirectory))
                    return Array.Empty<string>();

                return Directory.GetFiles(CrashDumpDirectory, "crash_*.txt");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving crash dumps");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Clean old crash dumps
        /// </summary>
        public static void CleanOldCrashDumps(int daysToKeep = 30)
        {
            try
            {
                if (!Directory.Exists(CrashDumpDirectory))
                    return;

                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var oldFiles = Directory.GetFiles(CrashDumpDirectory, "crash_*.txt");

                foreach (var file in oldFiles)
                {
                    try
                    {
                        if (File.GetCreationTimeUtc(file) < cutoffDate)
                        {
                            File.Delete(file);
                            _logger.Debug("Deleted old crash dump: {File}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to delete crash dump: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error cleaning old crash dumps");
            }
        }
    }
}

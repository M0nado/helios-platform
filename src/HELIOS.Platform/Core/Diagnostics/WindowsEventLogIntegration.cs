using Serilog;
using System;
using System.Diagnostics;

namespace HELIOS.Platform.Core.Diagnostics
{
    /// <summary>
    /// Integration with Windows Event Log
    /// </summary>
    public class WindowsEventLogIntegration
    {
        private static readonly ILogger _logger = Log.ForContext<WindowsEventLogIntegration>();
        private const string LogSourceName = "HELIOS Platform";
        private const string LogName = "Application";
        private static EventLog _eventLog;

        /// <summary>
        /// Initialize Windows Event Log integration
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (!EventLog.SourceExists(LogSourceName))
                {
                    EventLog.CreateEventSource(LogSourceName, LogName);
                    _logger.Information("Created Event Log source: {Source}", LogSourceName);
                }

                _eventLog = new EventLog(LogName) { Source = LogSourceName };
                _logger.Information("Windows Event Log integration initialized");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize Windows Event Log integration");
            }
        }

        /// <summary>
        /// Write an informational event
        /// </summary>
        public static void WriteInformation(string message, int eventId = 1000)
        {
            WriteEvent(message, EventLogEntryType.Information, eventId);
        }

        /// <summary>
        /// Write a warning event
        /// </summary>
        public static void WriteWarning(string message, int eventId = 2000)
        {
            WriteEvent(message, EventLogEntryType.Warning, eventId);
        }

        /// <summary>
        /// Write an error event
        /// </summary>
        public static void WriteError(string message, int eventId = 3000)
        {
            WriteEvent(message, EventLogEntryType.Error, eventId);
        }

        /// <summary>
        /// Write a failure audit event
        /// </summary>
        public static void WriteFailureAudit(string message, int eventId = 4000)
        {
            WriteEvent(message, EventLogEntryType.FailureAudit, eventId);
        }

        /// <summary>
        /// Write a success audit event
        /// </summary>
        public static void WriteSuccessAudit(string message, int eventId = 5000)
        {
            WriteEvent(message, EventLogEntryType.SuccessAudit, eventId);
        }

        /// <summary>
        /// Write an event to the Windows Event Log
        /// </summary>
        private static void WriteEvent(string message, EventLogEntryType type, int eventId)
        {
            try
            {
                if (_eventLog == null)
                {
                    _logger.Warning("Event Log not initialized");
                    return;
                }

                _eventLog.WriteEntry(message, type, eventId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error writing to Windows Event Log");
            }
        }

        /// <summary>
        /// Write health status event
        /// </summary>
        public static void WriteHealthStatusEvent(string checkName, HealthStatusCode status, string details)
        {
            var message = $"Health Check: {checkName}\nStatus: {status}\nDetails: {details}";
            var eventType = status == HealthStatusCode.Healthy
                ? EventLogEntryType.Information
                : status == HealthStatusCode.Degraded
                ? EventLogEntryType.Warning
                : EventLogEntryType.Error;

            WriteEvent(message, eventType, 6000);
        }

        /// <summary>
        /// Write performance event
        /// </summary>
        public static void WritePerformanceEvent(string counterName, long value, string details = null)
        {
            var message = $"Performance Counter: {counterName}\nValue: {value}";
            if (!string.IsNullOrEmpty(details))
                message += $"\nDetails: {details}";

            WriteEvent(message, EventLogEntryType.Information, 7000);
        }

        /// <summary>
        /// Write crash event
        /// </summary>
        public static void WriteCrashEvent(string crashType, string exceptionMessage, string stackTrace)
        {
            var message = $"Crash Type: {crashType}\nException: {exceptionMessage}\nStack Trace: {stackTrace}";
            WriteEvent(message, EventLogEntryType.Error, 8000);
        }

        /// <summary>
        /// Cleanup Windows Event Log integration
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                _eventLog?.Dispose();
                _logger.Information("Windows Event Log integration cleaned up");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error cleaning up Windows Event Log integration");
            }
        }

        /// <summary>
        /// Check if event log source exists
        /// </summary>
        public static bool SourceExists => EventLog.SourceExists(LogSourceName);
    }

    /// <summary>
    /// Event Log event ID constants
    /// </summary>
    public static class EventLogEventIds
    {
        public const int Information = 1000;
        public const int Warning = 2000;
        public const int Error = 3000;
        public const int FailureAudit = 4000;
        public const int SuccessAudit = 5000;
        public const int HealthStatus = 6000;
        public const int Performance = 7000;
        public const int Crash = 8000;
    }
}

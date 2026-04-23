using System;
using System.IO;

namespace HELIOS.Platform.Phase10.Quarantine
{
    /// <summary>
    /// Console logger implementation
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly string _logPath;
        private readonly LogLevel _minLogLevel;

        public ConsoleLogger(string logPath = null, LogLevel minLogLevel = LogLevel.Info)
        {
            _minLogLevel = minLogLevel;
            
            if (string.IsNullOrEmpty(logPath))
            {
                _logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HELIOS", "quarantine-logs", "system.log");
            }
            else
            {
                _logPath = logPath;
            }

            // Ensure log directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath));
        }

        public void LogInfo(string message)
        {
            if (_minLogLevel <= LogLevel.Info)
                WriteLog(message, "INFO");
        }

        public void LogError(string message)
        {
            if (_minLogLevel <= LogLevel.Error)
                WriteLog(message, "ERROR");
        }

        public void LogWarning(string message)
        {
            if (_minLogLevel <= LogLevel.Warning)
                WriteLog(message, "WARN");
        }

        private void WriteLog(string message, string level)
        {
            try
            {
                string logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                
                // Write to console
                Console.WriteLine(logEntry.TrimEnd());
                
                // Write to file
                File.AppendAllText(_logPath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write log: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
}

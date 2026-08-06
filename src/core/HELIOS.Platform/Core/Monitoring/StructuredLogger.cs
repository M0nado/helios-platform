using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace HELIOS.Platform.Core.Monitoring
{
    /// <summary>
    /// Structured logging with performance metrics and correlation IDs.
    /// </summary>
    public class StructuredLogger : IDisposable
    {
        public class LogEntry
        {
            public string CorrelationId { get; set; }
            public string Level { get; set; }
            public string Category { get; set; }
            public string Message { get; set; }
            public Dictionary<string, object> Properties { get; set; }
            public DateTime Timestamp { get; set; }
            public long ElapsedMs { get; set; }
            public Exception Exception { get; set; }
        }

        private string logFilePath = string.Empty;
        private readonly int maxFileSize;
        private readonly object lockObject = new object();
        private readonly List<LogEntry> logBuffer;
        private readonly int bufferSize;
        private StreamWriter logWriter;
        private string currentCorrelationId;
        private Stopwatch performanceStopwatch;

        public StructuredLogger(string logPath = "logs", int bufferSize = 100, int maxFileSizeMb = 10)
        {
            this.bufferSize = bufferSize;
            this.logBuffer = new List<LogEntry>();
            this.maxFileSize = maxFileSizeMb * 1024 * 1024;
            this.currentCorrelationId = Guid.NewGuid().ToString().Substring(0, 8);
            this.performanceStopwatch = Stopwatch.StartNew();

            InitializeLogFile(logPath);
        }

        private void InitializeLogFile(string logPath)
        {
            try
            {
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);

                logFilePath = Path.Combine(logPath, $"performance_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
                logWriter = new StreamWriter(logFilePath, true, Encoding.UTF8);
                logWriter.WriteLine($"=== Log Session Started: {DateTime.Now:O} ===");
                logWriter.Flush();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Log initialization error: {ex.Message}");
            }
        }

        public string StartOperation(string operationName, string category = "Operation")
        {
            var correlationId = $"{currentCorrelationId}-{Guid.NewGuid().ToString().Substring(0, 4)}";
            performanceStopwatch.Restart();
            
            Log("Info", category, $"Started: {operationName}", new Dictionary<string, object>
            {
                { "OperationName", operationName },
                { "StartTime", DateTime.Now }
            }, correlationId);

            return correlationId;
        }

        public void EndOperation(string correlationId, string operationName, bool success = true)
        {
            performanceStopwatch.Stop();
            var elapsed = performanceStopwatch.ElapsedMilliseconds;

            Log(success ? "Info" : "Warning", "Operation", 
                $"Completed: {operationName} ({elapsed}ms)", 
                new Dictionary<string, object>
                {
                    { "OperationName", operationName },
                    { "ElapsedMs", elapsed },
                    { "Success", success }
                }, correlationId);
        }

        public void Log(string level, string category, string message, 
                       Dictionary<string, object> properties = null, 
                       string correlationId = null)
        {
            correlationId = correlationId ?? currentCorrelationId;
            var entry = new LogEntry
            {
                CorrelationId = correlationId,
                Level = level,
                Category = category,
                Message = message,
                Properties = properties ?? new Dictionary<string, object>(),
                Timestamp = DateTime.Now,
                ElapsedMs = performanceStopwatch.ElapsedMilliseconds
            };

            lock (lockObject)
            {
                logBuffer.Add(entry);
                if (logBuffer.Count >= bufferSize)
                {
                    FlushLog();
                }
            }
        }

        public void LogPerformanceMetric(string metricName, double value, 
                                        string unit, string correlationId = null)
        {
            correlationId = correlationId ?? currentCorrelationId;
            Log("Info", "Performance", $"Metric: {metricName}",
                new Dictionary<string, object>
                {
                    { "MetricName", metricName },
                    { "Value", value },
                    { "Unit", unit }
                }, correlationId);
        }

        public void LogException(Exception ex, string category = "Exception", 
                                string correlationId = null)
        {
            correlationId = correlationId ?? currentCorrelationId;
            var entry = new LogEntry
            {
                CorrelationId = correlationId,
                Level = "Error",
                Category = category,
                Message = ex.Message,
                Exception = ex,
                Timestamp = DateTime.Now,
                Properties = new Dictionary<string, object>
                {
                    { "ExceptionType", ex.GetType().Name },
                    { "StackTrace", ex.StackTrace }
                }
            };

            lock (lockObject)
            {
                logBuffer.Add(entry);
                FlushLog();
            }
        }

        public void FlushLog()
        {
            lock (lockObject)
            {
                if (logWriter == null || logBuffer.Count == 0) return;

                try
                {
                    foreach (var entry in logBuffer)
                    {
                        var line = FormatEntry(entry);
                        logWriter.WriteLine(line);
                    }
                    logWriter.Flush();

                    CheckAndRotateLog();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Log flush error: {ex.Message}");
                }
                finally
                {
                    logBuffer.Clear();
                }
            }
        }

        private string FormatEntry(LogEntry entry)
        {
            var sb = new StringBuilder();
            sb.Append($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{entry.CorrelationId}] ");
            sb.Append($"[{entry.Level}] ");
            sb.Append($"[{entry.Category}] ");
            sb.Append($"{entry.Message}");

            if (entry.Properties.Count > 0)
            {
                sb.Append(" | ");
                var props = string.Join(", ", 
                    entry.Properties.Select(kv => $"{kv.Key}={kv.Value}"));
                sb.Append(props);
            }

            if (entry.Exception != null)
            {
                sb.AppendLine();
                sb.Append($"Exception: {entry.Exception}");
            }

            return sb.ToString();
        }

        private void CheckAndRotateLog()
        {
            try
            {
                if (logWriter == null) return;

                var fileInfo = new FileInfo(logFilePath);
                if (fileInfo.Length > maxFileSize)
                {
                    logWriter.Close();
                    string backupPath = logFilePath.Replace(".log", $".{DateTime.Now:HH-mm-ss}.log");
                    File.Move(logFilePath, backupPath);
                    logWriter = new StreamWriter(logFilePath, true, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Log rotation error: {ex.Message}");
            }
        }

        public List<LogEntry> GetLogHistory(int lastN = 100, string level = null)
        {
            lock (lockObject)
            {
                var history = logBuffer.AsEnumerable();
                if (!string.IsNullOrEmpty(level))
                    history = history.Where(e => e.Level == level);
                return history.TakeLast(lastN).ToList();
            }
        }

        public void Dispose()
        {
            FlushLog();
            logWriter?.Dispose();
        }
    }
}

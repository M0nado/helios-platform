using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Logging
{
    /// <summary>
    /// Interface for log aggregation providers
    /// </summary>
    public interface ILogAggregator
    {
        /// <summary>
        /// Initialize the aggregator with configuration
        /// </summary>
        Task InitializeAsync(LogAggregationConfig config);

        /// <summary>
        /// Send logs to the aggregation service
        /// </summary>
        Task SendLogsAsync(IEnumerable<AggregatedLogEntry> logs);

        /// <summary>
        /// Send a single log entry
        /// </summary>
        Task SendLogAsync(AggregatedLogEntry log);

        /// <summary>
        /// Get the aggregator status
        /// </summary>
        Task<AggregatorStatus> GetStatusAsync();

        /// <summary>
        /// Close the aggregator connection
        /// </summary>
        Task CloseAsync();
    }

    /// <summary>
    /// Configuration for log aggregation
    /// </summary>
    public class LogAggregationConfig
    {
        /// <summary>Endpoint URL for log aggregation service (e.g., Elasticsearch, Splunk)</summary>
        public string EndpointUrl { get; set; }

        /// <summary>API key for authentication</summary>
        public string ApiKey { get; set; }

        /// <summary>Username for authentication</summary>
        public string Username { get; set; }

        /// <summary>Password for authentication</summary>
        public string Password { get; set; }

        /// <summary>Index name or prefix for logs</summary>
        public string IndexName { get; set; } = "helios-logs";

        /// <summary>Batch size for sending logs</summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>Flush interval in milliseconds</summary>
        public int FlushIntervalMs { get; set; } = 5000;

        /// <summary>Enable SSL/TLS verification</summary>
        public bool VerifySsl { get; set; } = true;

        /// <summary>Timeout for HTTP requests in milliseconds</summary>
        public int TimeoutMs { get; set; } = 30000;

        /// <summary>Enable compression</summary>
        public bool EnableCompression { get; set; } = true;
    }

    /// <summary>
    /// Represents a log entry for aggregation
    /// </summary>
    public class AggregatedLogEntry
    {
        public string Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Logger { get; set; }
        public string Exception { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public string SourceContext { get; set; }
        public int ThreadId { get; set; }
        public int ProcessId { get; set; }
        public string MachineName { get; set; }
        public string Environment { get; set; }
    }

    /// <summary>
    /// Status information for the aggregator
    /// </summary>
    public class AggregatorStatus
    {
        public bool IsConnected { get; set; }
        public DateTime LastConnectedTime { get; set; }
        public long LogsQueued { get; set; }
        public long LogsSent { get; set; }
        public long LogsFailed { get; set; }
        public string LastError { get; set; }
    }

    /// <summary>
    /// In-memory implementation of log aggregator (for testing and buffering)
    /// </summary>
    public class InMemoryLogAggregator : ILogAggregator
    {
        private readonly Queue<AggregatedLogEntry> _logQueue = new();
        private LogAggregationConfig _config;
        private AggregatorStatus _status = new();
        private bool _initialized = false;

        public Task InitializeAsync(LogAggregationConfig config)
        {
            _config = config;
            _status.IsConnected = true;
            _status.LastConnectedTime = DateTime.UtcNow;
            _initialized = true;
            return Task.CompletedTask;
        }

        public Task SendLogsAsync(IEnumerable<AggregatedLogEntry> logs)
        {
            if (!_initialized)
                throw new InvalidOperationException("Aggregator not initialized");

            foreach (var log in logs)
            {
                _logQueue.Enqueue(log);
                _status.LogsQueued++;
            }

            return Task.CompletedTask;
        }

        public Task SendLogAsync(AggregatedLogEntry log)
        {
            if (!_initialized)
                throw new InvalidOperationException("Aggregator not initialized");

            _logQueue.Enqueue(log);
            _status.LogsQueued++;
            _status.LogsSent++;

            return Task.CompletedTask;
        }

        public Task<AggregatorStatus> GetStatusAsync()
        {
            return Task.FromResult(_status);
        }

        public Task CloseAsync()
        {
            _initialized = false;
            _status.IsConnected = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get all queued logs
        /// </summary>
        public IEnumerable<AggregatedLogEntry> GetQueuedLogs()
        {
            return new List<AggregatedLogEntry>(_logQueue);
        }

        /// <summary>
        /// Clear the queue
        /// </summary>
        public void ClearQueue()
        {
            _logQueue.Clear();
        }
    }
}

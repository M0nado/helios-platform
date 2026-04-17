using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HELIOS.Platform.Core.Diagnostics
{
    /// <summary>
    /// Performance monitoring and counter system
    /// </summary>
    public class PerformanceMonitor
    {
        private static readonly ILogger _logger = Log.ForContext<PerformanceMonitor>();
        private readonly Dictionary<string, PerformanceCounter> _counters = new();
        private readonly object _lock = new();

        /// <summary>
        /// Register a performance counter
        /// </summary>
        public void RegisterCounter(string name, string description, CounterType type)
        {
            lock (_lock)
            {
                if (!_counters.ContainsKey(name))
                {
                    _counters[name] = new PerformanceCounter
                    {
                        Name = name,
                        Description = description,
                        Type = type,
                        CreatedAt = DateTime.UtcNow
                    };
                    _logger.Debug("Registered performance counter: {CounterName}", name);
                }
            }
        }

        /// <summary>
        /// Increment a counter
        /// </summary>
        public void IncrementCounter(string name, long value = 1)
        {
            lock (_lock)
            {
                if (_counters.TryGetValue(name, out var counter))
                {
                    counter.Value += value;
                    counter.LastUpdated = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Decrement a counter
        /// </summary>
        public void DecrementCounter(string name, long value = 1)
        {
            lock (_lock)
            {
                if (_counters.TryGetValue(name, out var counter))
                {
                    counter.Value -= value;
                    counter.LastUpdated = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Set a counter value
        /// </summary>
        public void SetCounterValue(string name, long value)
        {
            lock (_lock)
            {
                if (_counters.TryGetValue(name, out var counter))
                {
                    counter.Value = value;
                    counter.LastUpdated = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Record a timing measurement
        /// </summary>
        public void RecordTiming(string name, long durationMs)
        {
            lock (_lock)
            {
                if (_counters.TryGetValue(name, out var counter) && counter.Type == CounterType.Timing)
                {
                    counter.RecordedValues.Add(durationMs);
                    counter.Value++;
                    counter.LastUpdated = DateTime.UtcNow;

                    // Keep last 1000 measurements
                    if (counter.RecordedValues.Count > 1000)
                    {
                        counter.RecordedValues.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// Get counter statistics
        /// </summary>
        public PerformanceCounter GetCounter(string name)
        {
            lock (_lock)
            {
                return _counters.TryGetValue(name, out var counter) ? counter : null;
            }
        }

        /// <summary>
        /// Get all counters
        /// </summary>
        public IEnumerable<PerformanceCounter> GetAllCounters()
        {
            lock (_lock)
            {
                return new List<PerformanceCounter>(_counters.Values);
            }
        }

        /// <summary>
        /// Reset a counter
        /// </summary>
        public void ResetCounter(string name)
        {
            lock (_lock)
            {
                if (_counters.TryGetValue(name, out var counter))
                {
                    counter.Value = 0;
                    counter.RecordedValues.Clear();
                    counter.LastUpdated = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Get counter summary
        /// </summary>
        public PerformanceCounterSummary GetSummary()
        {
            lock (_lock)
            {
                var summary = new PerformanceCounterSummary { GeneratedAt = DateTime.UtcNow };

                foreach (var counter in _counters.Values)
                {
                    summary.Counters.Add(counter.GetSnapshot());
                }

                return summary;
            }
        }

        /// <summary>
        /// Create a performance measurement scope
        /// </summary>
        public IDisposable MeasurePerformance(string operationName)
        {
            return new PerformanceMeasurementScope(this, operationName);
        }
    }

    /// <summary>
    /// Performance counter types
    /// </summary>
    public enum CounterType
    {
        Counter,
        Timing,
        Gauge,
        Histogram
    }

    /// <summary>
    /// Performance counter information
    /// </summary>
    public class PerformanceCounter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public CounterType Type { get; set; }
        public long Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<long> RecordedValues { get; set; } = new();

        /// <summary>
        /// Get average for timing counters
        /// </summary>
        public double GetAverage()
        {
            if (RecordedValues.Count == 0)
                return 0;
            return RecordedValues.Average();
        }

        /// <summary>
        /// Get minimum for timing counters
        /// </summary>
        public long GetMinimum()
        {
            if (RecordedValues.Count == 0)
                return 0;
            return RecordedValues.Min();
        }

        /// <summary>
        /// Get maximum for timing counters
        /// </summary>
        public long GetMaximum()
        {
            if (RecordedValues.Count == 0)
                return 0;
            return RecordedValues.Max();
        }

        /// <summary>
        /// Get a snapshot of the counter
        /// </summary>
        public PerformanceCounterSnapshot GetSnapshot()
        {
            return new PerformanceCounterSnapshot
            {
                Name = Name,
                Description = Description,
                Type = Type,
                Value = Value,
                Average = GetAverage(),
                Minimum = GetMinimum(),
                Maximum = GetMaximum(),
                SampleCount = RecordedValues.Count,
                LastUpdated = LastUpdated
            };
        }
    }

    /// <summary>
    /// Performance counter snapshot (read-only)
    /// </summary>
    public class PerformanceCounterSnapshot
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public CounterType Type { get; set; }
        public long Value { get; set; }
        public double Average { get; set; }
        public long Minimum { get; set; }
        public long Maximum { get; set; }
        public int SampleCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Summary of all performance counters
    /// </summary>
    public class PerformanceCounterSummary
    {
        public DateTime GeneratedAt { get; set; }
        public List<PerformanceCounterSnapshot> Counters { get; set; } = new();
    }

    /// <summary>
    /// Scope for automatic performance measurement
    /// </summary>
    public class PerformanceMeasurementScope : IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public PerformanceMeasurementScope(PerformanceMonitor monitor, string operationName)
        {
            _monitor = monitor;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.RecordTiming($"timing_{_operationName}", _stopwatch.ElapsedMilliseconds);
        }
    }
}

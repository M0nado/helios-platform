using Serilog;
using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Diagnostics
{
    /// <summary>
    /// Custom performance counter manager
    /// </summary>
    public class CustomPerformanceCounterManager
    {
        private static readonly ILogger _logger = Log.ForContext<CustomPerformanceCounterManager>();
        private readonly Dictionary<string, CustomCounter> _counters = new();
        private readonly object _lock = new();

        /// <summary>
        /// Create or get a counter
        /// </summary>
        public CustomCounter GetOrCreateCounter(string categoryName, string counterName, CounterDefinition definition = null)
        {
            var key = $"{categoryName}\\{counterName}";

            lock (_lock)
            {
                if (_counters.TryGetValue(key, out var counter))
                    return counter;

                definition ??= new CounterDefinition { Name = counterName };

                var newCounter = new CustomCounter
                {
                    CategoryName = categoryName,
                    CounterName = counterName,
                    Definition = definition,
                    CreatedAt = DateTime.UtcNow
                };

                _counters[key] = newCounter;
                _logger.Debug("Created custom counter: {Key}", key);

                return newCounter;
            }
        }

        /// <summary>
        /// Get a counter by name
        /// </summary>
        public CustomCounter GetCounter(string categoryName, string counterName)
        {
            var key = $"{categoryName}\\{counterName}";
            lock (_lock)
            {
                return _counters.TryGetValue(key, out var counter) ? counter : null;
            }
        }

        /// <summary>
        /// Get all counters in a category
        /// </summary>
        public IEnumerable<CustomCounter> GetCountersInCategory(string categoryName)
        {
            lock (_lock)
            {
                var prefix = $"{categoryName}\\";
                return _counters.FindAll(kvp => kvp.Key.StartsWith(prefix)).ConvertAll(kvp => kvp.Value);
            }
        }

        /// <summary>
        /// Delete a counter
        /// </summary>
        public bool DeleteCounter(string categoryName, string counterName)
        {
            var key = $"{categoryName}\\{counterName}";
            lock (_lock)
            {
                return _counters.Remove(key);
            }
        }

        /// <summary>
        /// Get all counters
        /// </summary>
        public IEnumerable<CustomCounter> GetAllCounters()
        {
            lock (_lock)
            {
                return new List<CustomCounter>(_counters.Values);
            }
        }

        /// <summary>
        /// Export counters as a report
        /// </summary>
        public CustomCounterReport GenerateReport()
        {
            var report = new CustomCounterReport { GeneratedAt = DateTime.UtcNow };

            lock (_lock)
            {
                foreach (var counter in _counters.Values)
                {
                    report.Counters.Add(counter.ToSnapshot());
                }
            }

            return report;
        }
    }

    /// <summary>
    /// Custom counter definition
    /// </summary>
    public class CounterDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public decimal AlertThreshold { get; set; }
        public decimal WarningThreshold { get; set; }
    }

    /// <summary>
    /// Custom performance counter
    /// </summary>
    public class CustomCounter
    {
        public string CategoryName { get; set; }
        public string CounterName { get; set; }
        public CounterDefinition Definition { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal MinValue { get; set; } = decimal.MaxValue;
        public decimal MaxValue { get; set; } = decimal.MinValue;
        public decimal AverageValue { get; set; }
        public int SampleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public List<CounterSample> Samples { get; set; } = new();

        /// <summary>
        /// Record a sample value
        /// </summary>
        public void RecordSample(decimal value)
        {
            CurrentValue = value;
            LastUpdatedAt = DateTime.UtcNow;

            MinValue = Math.Min(MinValue, value);
            MaxValue = Math.Max(MaxValue, value);

            Samples.Add(new CounterSample { Value = value, Timestamp = DateTime.UtcNow });

            // Keep last 1000 samples
            if (Samples.Count > 1000)
            {
                Samples.RemoveAt(0);
            }

            // Recalculate average
            if (Samples.Count > 0)
            {
                decimal sum = 0;
                foreach (var sample in Samples)
                {
                    sum += sample.Value;
                }
                AverageValue = sum / Samples.Count;
            }

            SampleCount++;
        }

        /// <summary>
        /// Get counter status
        /// </summary>
        public CounterStatus GetStatus()
        {
            var status = CounterStatus.Healthy;

            if (Definition.AlertThreshold > 0 && CurrentValue >= Definition.AlertThreshold)
                status = CounterStatus.Alert;
            else if (Definition.WarningThreshold > 0 && CurrentValue >= Definition.WarningThreshold)
                status = CounterStatus.Warning;

            return status;
        }

        /// <summary>
        /// Get a snapshot of this counter
        /// </summary>
        public CustomCounterSnapshot ToSnapshot()
        {
            return new CustomCounterSnapshot
            {
                CategoryName = CategoryName,
                CounterName = CounterName,
                Definition = Definition,
                CurrentValue = CurrentValue,
                MinValue = MinValue,
                MaxValue = MaxValue,
                AverageValue = AverageValue,
                SampleCount = SampleCount,
                Status = GetStatus(),
                LastUpdatedAt = LastUpdatedAt
            };
        }
    }

    /// <summary>
    /// Counter sample
    /// </summary>
    public class CounterSample
    {
        public decimal Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Counter status
    /// </summary>
    public enum CounterStatus
    {
        Healthy,
        Warning,
        Alert
    }

    /// <summary>
    /// Snapshot of a custom counter
    /// </summary>
    public class CustomCounterSnapshot
    {
        public string CategoryName { get; set; }
        public string CounterName { get; set; }
        public CounterDefinition Definition { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
        public decimal AverageValue { get; set; }
        public int SampleCount { get; set; }
        public CounterStatus Status { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    /// <summary>
    /// Report of all custom counters
    /// </summary>
    public class CustomCounterReport
    {
        public DateTime GeneratedAt { get; set; }
        public List<CustomCounterSnapshot> Counters { get; set; } = new();
    }
}

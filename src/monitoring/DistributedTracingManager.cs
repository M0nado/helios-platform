using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace MonadoBlade.Monitoring
{
    /// <summary>
    /// Interface for distributed tracing management
    /// </summary>
    public interface IDistributedTracingManager
    {
        string GenerateCorrelationId();
        string StartSpan(string operationName, string parentSpanId = null, Dictionary<string, string> baggage = null);
        void EndSpan(string spanId, string status = "success");
        (string CorrelationId, string SpanId) GetCurrentContext();
        void SetBaggage(string key, string value);
        string GetBaggage(string key);
        Dictionary<string, string> GetAllBaggage();
        void SetSamplingStrategy(SamplingStrategy strategy);
        List<SpanInfo> GetActiveSpans();
    }

    /// <summary>
    /// Sampling strategy for distributed tracing
    /// </summary>
    public enum SamplingStrategy
    {
        AlwaysSample = 0,
        NeverSample = 1,
        ProbabilisticSample = 2,
        RateLimitedSample = 3
    }

    /// <summary>
    /// Span information
    /// </summary>
    public class SpanInfo
    {
        public string SpanId { get; set; }
        public string ParentSpanId { get; set; }
        public string CorrelationId { get; set; }
        public string OperationName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public long DurationMs => EndTime.HasValue ? (long)(EndTime.Value - StartTime).TotalMilliseconds : -1;
    }

    /// <summary>
    /// Distributed tracing manager with W3C Trace Context support
    /// </summary>
    public class DistributedTracingManager : IDistributedTracingManager
    {
        private readonly ConcurrentDictionary<string, SpanInfo> _activeSpans = new();
        private readonly ConcurrentDictionary<string, Stack<string>> _spanStacks = new();
        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _baggageStorage = new();
        private readonly Random _random = new();
        private readonly ReaderWriterLockSlim _baggageLock = new();

        private SamplingStrategy _samplingStrategy = SamplingStrategy.AlwaysSample;
        private double _samplingProbability = 0.1;
        private int _maxSpanAge = 3600000; // 1 hour in ms

        [ThreadStatic]
        private static string _currentCorrelationId;

        [ThreadStatic]
        private static string _currentSpanId;

        /// <summary>
        /// Generate a new correlation ID (trace ID)
        /// </summary>
        public string GenerateCorrelationId()
        {
            var traceId = GenerateTraceId();
            _currentCorrelationId = traceId;
            return traceId;
        }

        /// <summary>
        /// Start a new span (operation)
        /// </summary>
        public string StartSpan(string operationName, string parentSpanId = null, Dictionary<string, string> baggage = null)
        {
            if (!ShouldSample())
                return null;

            var spanId = GenerateSpanId();
            var correlationId = _currentCorrelationId ?? GenerateCorrelationId();

            var span = new SpanInfo
            {
                SpanId = spanId,
                ParentSpanId = parentSpanId ?? _currentSpanId,
                CorrelationId = correlationId,
                OperationName = operationName,
                StartTime = DateTime.UtcNow,
                Status = "active"
            };

            _activeSpans[spanId] = span;

            var threadId = Thread.CurrentThread.ManagedThreadId.ToString();
            _spanStacks.AddOrUpdate(threadId,
                new Stack<string>(new[] { spanId }),
                (k, stack) =>
                {
                    stack.Push(spanId);
                    return stack;
                });

            _currentSpanId = spanId;

            if (baggage != null && baggage.Count > 0)
            {
                _baggageStorage[spanId] = new Dictionary<string, string>(baggage);
            }

            return spanId;
        }

        /// <summary>
        /// End a span (operation completed)
        /// </summary>
        public void EndSpan(string spanId, string status = "success")
        {
            if (string.IsNullOrEmpty(spanId))
                return;

            if (_activeSpans.TryGetValue(spanId, out var span))
            {
                span.EndTime = DateTime.UtcNow;
                span.Status = status;
            }

            var threadId = Thread.CurrentThread.ManagedThreadId.ToString();
            if (_spanStacks.TryGetValue(threadId, out var stack) && stack.Count > 0)
            {
                if (stack.Peek() == spanId)
                {
                    stack.Pop();
                    if (stack.Count > 0)
                    {
                        _currentSpanId = stack.Peek();
                    }
                }
            }
        }

        /// <summary>
        /// Get current trace context (correlation ID and span ID)
        /// </summary>
        public (string CorrelationId, string SpanId) GetCurrentContext()
        {
            return (_currentCorrelationId, _currentSpanId);
        }

        /// <summary>
        /// Set baggage item for span propagation
        /// </summary>
        public void SetBaggage(string key, string value)
        {
            _baggageLock.EnterWriteLock();
            try
            {
                var spanId = _currentSpanId;
                if (!string.IsNullOrEmpty(spanId))
                {
                    if (!_baggageStorage.ContainsKey(spanId))
                    {
                        _baggageStorage[spanId] = new Dictionary<string, string>();
                    }

                    _baggageStorage[spanId][key] = value;
                }
            }
            finally
            {
                _baggageLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get baggage item
        /// </summary>
        public string GetBaggage(string key)
        {
            _baggageLock.EnterReadLock();
            try
            {
                var spanId = _currentSpanId;
                if (!string.IsNullOrEmpty(spanId) && _baggageStorage.TryGetValue(spanId, out var baggage))
                {
                    return baggage.TryGetValue(key, out var value) ? value : null;
                }
                return null;
            }
            finally
            {
                _baggageLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get all baggage for current span
        /// </summary>
        public Dictionary<string, string> GetAllBaggage()
        {
            _baggageLock.EnterReadLock();
            try
            {
                var spanId = _currentSpanId;
                if (!string.IsNullOrEmpty(spanId) && _baggageStorage.TryGetValue(spanId, out var baggage))
                {
                    return new Dictionary<string, string>(baggage);
                }
                return new Dictionary<string, string>();
            }
            finally
            {
                _baggageLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Set trace sampling strategy
        /// </summary>
        public void SetSamplingStrategy(SamplingStrategy strategy)
        {
            _samplingStrategy = strategy;
        }

        /// <summary>
        /// Set sampling probability (for probabilistic sampling)
        /// </summary>
        public void SetSamplingProbability(double probability)
        {
            if (probability < 0 || probability > 1)
                throw new ArgumentException("Probability must be between 0 and 1");
            _samplingProbability = probability;
        }

        /// <summary>
        /// Get all active spans
        /// </summary>
        public List<SpanInfo> GetActiveSpans()
        {
            CleanupExpiredSpans();
            return _activeSpans.Values.Where(s => s.Status == "active").ToList();
        }

        /// <summary>
        /// Get span information by ID
        /// </summary>
        public SpanInfo GetSpanInfo(string spanId)
        {
            _activeSpans.TryGetValue(spanId, out var span);
            return span;
        }

        /// <summary>
        /// Get trace tree (all spans for a correlation ID)
        /// </summary>
        public List<SpanInfo> GetTraceTree(string correlationId)
        {
            return _activeSpans.Values
                .Where(s => s.CorrelationId == correlationId)
                .ToList();
        }

        private bool ShouldSample()
        {
            return _samplingStrategy switch
            {
                SamplingStrategy.AlwaysSample => true,
                SamplingStrategy.NeverSample => false,
                SamplingStrategy.ProbabilisticSample => _random.NextDouble() < _samplingProbability,
                SamplingStrategy.RateLimitedSample => _random.NextDouble() < 0.5,
                _ => true
            };
        }

        private string GenerateTraceId()
        {
            var buffer = new byte[16];
            _random.NextBytes(buffer);
            return BitConverter.ToString(buffer).Replace("-", "").ToLowerInvariant();
        }

        private string GenerateSpanId()
        {
            var buffer = new byte[8];
            _random.NextBytes(buffer);
            return BitConverter.ToString(buffer).Replace("-", "").ToLowerInvariant();
        }

        private void CleanupExpiredSpans()
        {
            var now = DateTime.UtcNow;
            var expired = _activeSpans
                .Where(kvp => kvp.Value.EndTime.HasValue && 
                    (now - kvp.Value.EndTime.Value).TotalMilliseconds > _maxSpanAge)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var spanId in expired)
            {
                _activeSpans.TryRemove(spanId, out _);
                _baggageStorage.TryRemove(spanId, out _);
            }
        }
    }
}

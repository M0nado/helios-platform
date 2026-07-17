using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using HELIOS.Platform.Core.AdvancedOptimization.Interfaces;
using Contracts = HELIOS.Platform.Core.AdvancedOptimization.Interfaces;
using CanonicalEventAggregationResult = HELIOS.Platform.Core.AdvancedOptimization.Interfaces.EventAggregationResult;
using CanonicalEventPattern = HELIOS.Platform.Core.AdvancedOptimization.Interfaces.EventPattern;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Complex Event Processor implementation.
    /// Provides event stream analysis and pattern detection.
    /// </summary>
    public class ComplexEventProcessor : Contracts.IComplexEventProcessor
    {
        private readonly ILogger<ComplexEventProcessor> _logger;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<EventProcessingResult> _processingHistory;
        private bool _isRunning;

        /// <summary>
        /// Initializes a new instance of the ComplexEventProcessor class.
        /// </summary>
        public ComplexEventProcessor(ILogger<ComplexEventProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(1, 1);
            _processingHistory = new ConcurrentQueue<EventProcessingResult>();
            _isRunning = false;
        }

        /// <inheritdoc/>
        public string ServiceName => nameof(ComplexEventProcessor);

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("{ServiceName} initializing", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = true;
                _logger.LogInformation("{ServiceName} started", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = false;
                _logger.LogInformation("{ServiceName} stopped", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public bool IsRunning() => _isRunning;

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _semaphore?.Dispose();
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<EventProcessingResult> ProcessEventAsync(ComplexEvent complexEvent, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = new EventProcessingResult
                {
                    ProcessingTime = DateTime.UtcNow,
                    ProcessedEventId = complexEvent?.EventId ?? string.Empty,
                    Status = "Success"
                };

                if (complexEvent == null)
                {
                    result.Status = "Failed";
                    sw.Stop();
                    result.ProcessingDurationMs = sw.ElapsedMilliseconds;
                    return result;
                }

                if (complexEvent.Severity == "Critical")
                {
                    result.GeneratedAlerts.Add($"Critical event detected: {complexEvent.EventType}");
                    result.PatternsMatched++;
                }

                if (complexEvent.CorrelatedEventIds.Count > 0)
                {
                    foreach (var correlatedId in complexEvent.CorrelatedEventIds)
                    {
                        result.CorrelationsDetected.Add(new EventCorrelation
                        {
                            EventId1 = complexEvent.EventId,
                            EventId2 = correlatedId,
                            CorrelationStrength = 0.8,
                            CorrelationType = "Temporal",
                            TimeGapSeconds = 5
                        });
                    }
                }

                sw.Stop();
                result.ProcessingDurationMs = sw.ElapsedMilliseconds;

                _processingHistory.Enqueue(result);
                _logger.LogInformation("Event processed: {EventId} in {Duration}ms", complexEvent.EventId, result.ProcessingDurationMs);

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<PatternMatchingResult> MatchPatternsAsync(List<ComplexEvent> events, List<CanonicalEventPattern> patterns, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = new PatternMatchingResult
                {
                    Timestamp = DateTime.UtcNow,
                    EventsAnalyzed = events?.Count ?? 0
                };

                if (events == null || events.Count == 0 || patterns == null || patterns.Count == 0)
                {
                    return result;
                }

                foreach (var pattern in patterns)
                {
                    var matchingEvents = events.Where(e => pattern.EventTypes.Contains(e.EventType)).ToList();

                    if (matchingEvents.Count >= pattern.MinOccurrences)
                    {
                        var match = new PatternMatch
                        {
                            PatternId = pattern.PatternId,
                            PatternName = pattern.Name,
                            MatchingEventIds = matchingEvents.Select(e => e.EventId).ToList(),
                            Confidence = Math.Min(1.0, (double)matchingEvents.Count / (pattern.MinOccurrences * 2)),
                            MatchTime = DateTime.UtcNow,
                            Details = $"Matched {matchingEvents.Count} events"
                        };

                        result.PatternMatches.Add(match);
                        result.TotalMatches++;

                        if (pattern.AlertSeverity == "Critical")
                        {
                            result.CriticalMatches.Add(pattern.Name);
                        }
                    }
                }

                _logger.LogInformation("Pattern matching completed. Matches found: {Count}", result.TotalMatches);

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<CorrelationAnalysis> DetectCorrelationsAsync(List<ComplexEvent> events, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var analysis = new CorrelationAnalysis
                {
                    Timestamp = DateTime.UtcNow,
                    EventsAnalyzed = events?.Count ?? 0,
                    CorrelationThreshold = 0.5
                };

                if (events == null || events.Count < 2)
                {
                    return analysis;
                }

                double totalStrength = 0;

                for (int i = 0; i < events.Count - 1; i++)
                {
                    for (int j = i + 1; j < Math.Min(i + 10, events.Count); j++)
                    {
                        var evt1 = events[i];
                        var evt2 = events[j];

                        double correlation = CalculateEventCorrelation(evt1, evt2);

                        if (correlation >= analysis.CorrelationThreshold)
                        {
                            var timeGap = (int)((evt2.Timestamp - evt1.Timestamp).TotalSeconds);

                            var eventCorr = new EventCorrelation
                            {
                                EventId1 = evt1.EventId,
                                EventId2 = evt2.EventId,
                                CorrelationStrength = correlation,
                                CorrelationType = DetermineCorrelationType(evt1, evt2),
                                TimeGapSeconds = Math.Abs(timeGap)
                            };

                            analysis.Correlations.Add(eventCorr);
                            totalStrength += correlation;

                            if (correlation > 0.8)
                            {
                                analysis.StrongCorrelationCount++;
                            }
                        }
                    }
                }

                analysis.AverageCorrelationStrength = analysis.Correlations.Count > 0
                    ? analysis.Correlations.Average(c => c.CorrelationStrength)
                    : 0;

                _logger.LogInformation("Correlation analysis completed. Found {Count} correlations", analysis.Correlations.Count);

                return analysis;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<CanonicalEventAggregationResult> AggregateEventsAsync(List<ComplexEvent> events, int windowSizeSeconds, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = new CanonicalEventAggregationResult
                {
                    Timestamp = DateTime.UtcNow,
                    WindowSizeSeconds = windowSizeSeconds
                };

                if (events == null || events.Count == 0)
                {
                    return result;
                }

                var windowStart = events.Min(e => e.Timestamp);
                var windowEnd = windowStart.AddSeconds(windowSizeSeconds);

                var eventsInWindow = events.Where(e => e.Timestamp >= windowStart && e.Timestamp <= windowEnd).ToList();

                result.WindowStart = windowStart;
                result.WindowEnd = windowEnd;
                result.TotalEventsInWindow = eventsInWindow.Count;

                foreach (var evt in eventsInWindow)
                {
                    if (!result.EventCountsByType.ContainsKey(evt.EventType))
                    {
                        result.EventCountsByType[evt.EventType] = 0;
                    }
                    result.EventCountsByType[evt.EventType]++;

                    if (!result.EventCountsBySeverity.ContainsKey(evt.Severity))
                    {
                        result.EventCountsBySeverity[evt.Severity] = 0;
                    }
                    result.EventCountsBySeverity[evt.Severity]++;

                    if (!result.EventSources.Contains(evt.Source))
                    {
                        result.EventSources.Add(evt.Source);
                    }
                }

                var criticalCount = result.EventCountsBySeverity.ContainsKey("Critical")
                    ? result.EventCountsBySeverity["Critical"]
                    : 0;

                result.Summary = $"Aggregated {result.TotalEventsInWindow} events from {result.EventSources.Count} sources. " +
                                $"Critical events: {criticalCount}. Event types: {result.EventCountsByType.Count}";

                _logger.LogInformation("Event aggregation completed. Window: {Start} to {End}", result.WindowStart, result.WindowEnd);

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<List<EventProcessingResult>> GetProcessingHistoryAsync(int limit = 100)
        {
            var results = new List<EventProcessingResult>();
            int count = 0;

            foreach (var item in _processingHistory.Reverse())
            {
                if (count >= limit) break;
                results.Add(item);
                count++;
            }

            return await Task.FromResult(results);
        }

        private double CalculateEventCorrelation(ComplexEvent evt1, ComplexEvent evt2)
        {
            double score = 0;

            if (evt1.EventType == evt2.EventType)
                score += 0.3;

            if (evt1.Severity == evt2.Severity)
                score += 0.2;

            var commonTags = evt1.Tags.Intersect(evt2.Tags).Count();
            if (commonTags > 0)
                score += 0.2 * Math.Min(1.0, commonTags / 3.0);

            if (evt1.Source == evt2.Source)
                score += 0.2;

            var timeGap = Math.Abs((evt2.Timestamp - evt1.Timestamp).TotalSeconds);
            if (timeGap < 60)
                score += 0.1;

            return Math.Min(score, 1.0);
        }

        private string DetermineCorrelationType(ComplexEvent evt1, ComplexEvent evt2)
        {
            if (evt1.EventType == evt2.EventType) return "Semantic";
            if (Math.Abs((evt2.Timestamp - evt1.Timestamp).TotalSeconds) < 10) return "Temporal";
            if (evt1.Source == evt2.Source) return "Causal";
            return "Unknown";
        }
    }
}

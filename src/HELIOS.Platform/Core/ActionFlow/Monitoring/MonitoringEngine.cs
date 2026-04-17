using System;
using System.Collections.Generic;
using System.Linq;
using HELIOS.Platform.Core.ActionFlow.Models;

namespace HELIOS.Platform.Core.ActionFlow.Monitoring
{
    /// <summary>
    /// Represents a performance metric
    /// </summary>
    public class PerformanceMetric
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string MetricName { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Represents an execution statistic
    /// </summary>
    public class ExecutionStatistics
    {
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double AverageDuration { get; set; }
        public double MinDuration { get; set; }
        public double MaxDuration { get; set; }
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;
    }

    /// <summary>
    /// Represents an event in the action flow system
    /// </summary>
    public class ActionFlowEvent
    {
        public ActionFlowId EventId { get; set; } = ActionFlowId.New();
        public string EventType { get; set; } = string.Empty;
        public string EventLevel { get; set; } = "Info";
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public ActionFlowId? RelatedWorkflowId { get; set; }
        public ActionFlowId? RelatedActionId { get; set; }
        public ActionFlowId? RelatedPageId { get; set; }
        
        public Dictionary<string, object> ContextData { get; set; } = new();
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Monitoring and analytics engine
    /// </summary>
    public class MonitoringEngine
    {
        private readonly List<PerformanceMetric> _metrics = new();
        private readonly List<ActionFlowEvent> _events = new();
        private readonly Dictionary<ActionFlowId, ExecutionStatistics> _executionStats = new();
        
        private readonly List<Action<ActionFlowEvent>> _eventListeners = new();
        private readonly List<Action<PerformanceMetric>> _metricListeners = new();
        
        private readonly int _maxEventHistory = 10000;
        private readonly int _maxMetricHistory = 50000;
        private readonly object _lockObject = new();

        public event EventHandler<ActionFlowEvent>? EventOccurred;
        public event EventHandler<PerformanceMetric>? MetricRecorded;

        /// <summary>
        /// Records a performance metric
        /// </summary>
        public void RecordMetric(string metricName, double value, string unit = "")
        {
            var metric = new PerformanceMetric
            {
                MetricName = metricName,
                Value = value,
                Unit = unit
            };

            RecordMetric(metric);
        }

        /// <summary>
        /// Records a performance metric
        /// </summary>
        public void RecordMetric(PerformanceMetric metric)
        {
            lock (_lockObject)
            {
                _metrics.Add(metric);

                if (_metrics.Count > _maxMetricHistory)
                {
                    var toRemove = _metrics.Count - _maxMetricHistory;
                    _metrics.RemoveRange(0, toRemove);
                }
            }

            foreach (var listener in _metricListeners.ToList())
            {
                listener(metric);
            }

            MetricRecorded?.Invoke(this, metric);
        }

        /// <summary>
        /// Records an event
        /// </summary>
        public void RecordEvent(string eventType, string message, string level = "Info")
        {
            var @event = new ActionFlowEvent
            {
                EventType = eventType,
                Message = message,
                EventLevel = level
            };

            RecordEvent(@event);
        }

        /// <summary>
        /// Records an event
        /// </summary>
        public void RecordEvent(ActionFlowEvent actionFlowEvent)
        {
            lock (_lockObject)
            {
                _events.Add(actionFlowEvent);

                if (_events.Count > _maxEventHistory)
                {
                    var toRemove = _events.Count - _maxEventHistory;
                    _events.RemoveRange(0, toRemove);
                }
            }

            foreach (var listener in _eventListeners.ToList())
            {
                listener(actionFlowEvent);
            }

            EventOccurred?.Invoke(this, actionFlowEvent);
        }

        /// <summary>
        /// Records workflow execution start
        /// </summary>
        public void RecordWorkflowStart(ActionFlowId workflowId)
        {
            RecordEvent(new ActionFlowEvent
            {
                EventType = "WorkflowStarted",
                Message = $"Workflow execution started: {workflowId}",
                RelatedWorkflowId = workflowId,
                EventLevel = "Info"
            });
        }

        /// <summary>
        /// Records workflow execution completion
        /// </summary>
        public void RecordWorkflowCompleted(ActionFlowId workflowId, TimeSpan duration, bool success)
        {
            RecordEvent(new ActionFlowEvent
            {
                EventType = "WorkflowCompleted",
                Message = $"Workflow execution completed: {workflowId} - Success: {success}",
                RelatedWorkflowId = workflowId,
                EventLevel = success ? "Info" : "Warning",
                ContextData = new() { { "duration", duration.TotalSeconds }, { "success", success } }
            });

            RecordMetric($"workflow.execution.duration", duration.TotalSeconds, "seconds");

            // Update statistics
            UpdateExecutionStats(workflowId, duration, success);
        }

        /// <summary>
        /// Records action execution
        /// </summary>
        public void RecordActionExecution(ActionFlowId actionId, TimeSpan duration, bool success, Exception? error = null)
        {
            RecordEvent(new ActionFlowEvent
            {
                EventType = "ActionExecuted",
                Message = $"Action executed: {actionId} - Success: {success}",
                RelatedActionId = actionId,
                EventLevel = success ? "Info" : "Error",
                Exception = error,
                ContextData = new() { { "duration", duration.TotalSeconds }, { "success", success } }
            });

            RecordMetric($"action.execution.duration", duration.TotalSeconds, "seconds");
        }

        /// <summary>
        /// Records page navigation
        /// </summary>
        public void RecordPageNavigation(ActionFlowId pageId, ActionFlowId? previousPageId = null)
        {
            RecordEvent(new ActionFlowEvent
            {
                EventType = "PageNavigated",
                Message = $"Navigation to page: {pageId}",
                RelatedPageId = pageId,
                EventLevel = "Info",
                ContextData = new() { { "previousPageId", previousPageId?.ToString() ?? "none" } }
            });
        }

        /// <summary>
        /// Gets execution statistics for a workflow
        /// </summary>
        public ExecutionStatistics GetWorkflowStatistics(ActionFlowId workflowId)
        {
            lock (_lockObject)
            {
                if (_executionStats.TryGetValue(workflowId, out var stats))
                    return stats;

                return new ExecutionStatistics();
            }
        }

        /// <summary>
        /// Gets recent events
        /// </summary>
        public List<ActionFlowEvent> GetRecentEvents(int count = 100)
        {
            lock (_lockObject)
            {
                return _events.TakeLast(count).ToList();
            }
        }

        /// <summary>
        /// Gets events by type
        /// </summary>
        public List<ActionFlowEvent> GetEventsByType(string eventType, int count = 100)
        {
            lock (_lockObject)
            {
                return _events
                    .Where(e => e.EventType == eventType)
                    .TakeLast(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets events for a workflow
        /// </summary>
        public List<ActionFlowEvent> GetWorkflowEvents(ActionFlowId workflowId, int count = 100)
        {
            lock (_lockObject)
            {
                return _events
                    .Where(e => e.RelatedWorkflowId == workflowId)
                    .TakeLast(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets error events
        /// </summary>
        public List<ActionFlowEvent> GetErrorEvents(int count = 100)
        {
            lock (_lockObject)
            {
                return _events
                    .Where(e => e.EventLevel == "Error")
                    .TakeLast(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets recent metrics
        /// </summary>
        public List<PerformanceMetric> GetRecentMetrics(int count = 100)
        {
            lock (_lockObject)
            {
                return _metrics.TakeLast(count).ToList();
            }
        }

        /// <summary>
        /// Gets metrics by name
        /// </summary>
        public List<PerformanceMetric> GetMetricsByName(string metricName, int count = 100)
        {
            lock (_lockObject)
            {
                return _metrics
                    .Where(m => m.MetricName == metricName)
                    .TakeLast(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets average metric value
        /// </summary>
        public double GetAverageMetric(string metricName, TimeSpan? timeWindow = null)
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                var window = timeWindow ?? TimeSpan.FromHours(1);

                var metrics = _metrics
                    .Where(m => m.MetricName == metricName && (now - m.RecordedAt) <= window)
                    .ToList();

                if (metrics.Count == 0)
                    return 0;

                return metrics.Average(m => m.Value);
            }
        }

        /// <summary>
        /// Subscribes to events
        /// </summary>
        public void SubscribeToEvents(Action<ActionFlowEvent> listener)
        {
            lock (_lockObject)
            {
                _eventListeners.Add(listener);
            }
        }

        /// <summary>
        /// Unsubscribes from events
        /// </summary>
        public void UnsubscribeFromEvents(Action<ActionFlowEvent> listener)
        {
            lock (_lockObject)
            {
                _eventListeners.Remove(listener);
            }
        }

        /// <summary>
        /// Subscribes to metrics
        /// </summary>
        public void SubscribeToMetrics(Action<PerformanceMetric> listener)
        {
            lock (_lockObject)
            {
                _metricListeners.Add(listener);
            }
        }

        /// <summary>
        /// Unsubscribes from metrics
        /// </summary>
        public void UnsubscribeFromMetrics(Action<PerformanceMetric> listener)
        {
            lock (_lockObject)
            {
                _metricListeners.Remove(listener);
            }
        }

        /// <summary>
        /// Clears all history
        /// </summary>
        public void ClearHistory()
        {
            lock (_lockObject)
            {
                _metrics.Clear();
                _events.Clear();
                _executionStats.Clear();
            }
        }

        /// <summary>
        /// Updates execution statistics
        /// </summary>
        private void UpdateExecutionStats(ActionFlowId workflowId, TimeSpan duration, bool success)
        {
            lock (_lockObject)
            {
                if (!_executionStats.TryGetValue(workflowId, out var stats))
                {
                    stats = new ExecutionStatistics();
                    _executionStats[workflowId] = stats;
                }

                stats.TotalExecutions++;
                if (success)
                    stats.SuccessfulExecutions++;
                else
                    stats.FailedExecutions++;

                var durationSeconds = duration.TotalSeconds;
                stats.AverageDuration = (stats.AverageDuration * (stats.TotalExecutions - 1) + durationSeconds) / stats.TotalExecutions;

                if (stats.MinDuration == 0 || durationSeconds < stats.MinDuration)
                    stats.MinDuration = durationSeconds;

                if (durationSeconds > stats.MaxDuration)
                    stats.MaxDuration = durationSeconds;
            }
        }
    }

    /// <summary>
    /// Health check engine
    /// </summary>
    public class HealthCheckEngine
    {
        private readonly MonitoringEngine _monitoringEngine;
        private readonly Dictionary<string, Func<bool>> _healthChecks = new();
        private readonly object _lockObject = new();

        public HealthCheckEngine(MonitoringEngine monitoringEngine)
        {
            _monitoringEngine = monitoringEngine;
        }

        /// <summary>
        /// Registers a health check
        /// </summary>
        public void RegisterHealthCheck(string name, Func<bool> check)
        {
            lock (_lockObject)
            {
                _healthChecks[name] = check;
            }
        }

        /// <summary>
        /// Performs all health checks
        /// </summary>
        public Dictionary<string, bool> PerformHealthChecks()
        {
            var results = new Dictionary<string, bool>();

            List<KeyValuePair<string, Func<bool>>> checksToRun;
            lock (_lockObject)
            {
                checksToRun = _healthChecks.ToList();
            }

            foreach (var check in checksToRun)
            {
                try
                {
                    results[check.Key] = check.Value();
                }
                catch (Exception ex)
                {
                    results[check.Key] = false;
                    _monitoringEngine.RecordEvent($"HealthCheckFailed", $"Health check '{check.Key}' failed: {ex.Message}", "Error");
                }
            }

            return results;
        }

        /// <summary>
        /// Gets overall system health
        /// </summary>
        public bool IsSystemHealthy()
        {
            var checks = PerformHealthChecks();
            return checks.Values.All(v => v);
        }
    }
}

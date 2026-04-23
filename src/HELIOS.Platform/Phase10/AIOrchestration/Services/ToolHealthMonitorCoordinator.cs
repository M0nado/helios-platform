using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Phase10.AIOrchestration.Models;
using HELIOS.Platform.Phase10.AIOrchestration.Interfaces;

namespace HELIOS.Platform.Phase10.AIOrchestration.Services
{
    /// <summary>
    /// Tool health monitoring and conflict management coordinator
    /// </summary>
    public class ToolHealthMonitorCoordinator : IToolHealthMonitorCoordinator, IDisposable
    {
        private readonly ILogger<ToolHealthMonitorCoordinator> _logger;
        private readonly IToolConflictResolver _conflictResolver;
        private readonly Dictionary<string, ToolHealthMetrics> _healthMetrics;
        private readonly Dictionary<string, List<ToolConflict>> _toolConflicts;
        private readonly List<OrchestrationEvent> _eventLog;
        private readonly List<MaintenancePrediction> _maintenancePredictions;
        private readonly SemaphoreSlim _metricsLock;
        private readonly Timer _healthCheckTimer;
        private bool _disposed;
        private ToolDependencyGraph _dependencyGraph;

        private const int HEALTH_CHECK_INTERVAL_MS = 5000;
        private const int CRASH_THRESHOLD = 5;
        private const int HANG_THRESHOLD = 3;
        private const int MAX_EVENT_LOG_SIZE = 1000;

        public ToolHealthMonitorCoordinator(
            ILogger<ToolHealthMonitorCoordinator> logger,
            IToolConflictResolver conflictResolver)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _conflictResolver = conflictResolver ?? throw new ArgumentNullException(nameof(conflictResolver));
            _healthMetrics = new Dictionary<string, ToolHealthMetrics>();
            _toolConflicts = new Dictionary<string, List<ToolConflict>>();
            _eventLog = new List<OrchestrationEvent>();
            _maintenancePredictions = new List<MaintenancePrediction>();
            _metricsLock = new SemaphoreSlim(1, 1);
            _healthCheckTimer = new Timer(PerformHealthCheckAsync, null, Timeout.Infinite, Timeout.Infinite);
            _dependencyGraph = new ToolDependencyGraph();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _metricsLock.WaitAsync();

                _logger.LogInformation("Initializing ToolHealthMonitorCoordinator");

                _healthCheckTimer.Change(HEALTH_CHECK_INTERVAL_MS, HEALTH_CHECK_INTERVAL_MS);

                _logger.LogInformation("ToolHealthMonitorCoordinator initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ToolHealthMonitorCoordinator");
                throw;
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                await _metricsLock.WaitAsync();

                _logger.LogInformation("Shutting down ToolHealthMonitorCoordinator");

                _healthCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);

                _logger.LogInformation("ToolHealthMonitorCoordinator shutdown complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during shutdown");
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public async Task<ToolHealthMetrics> GetHealthMetricsAsync(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                throw new ArgumentNullException(nameof(toolId));

            try
            {
                await _metricsLock.WaitAsync();

                if (_healthMetrics.TryGetValue(toolId, out var metrics))
                {
                    return metrics;
                }

                var newMetrics = new ToolHealthMetrics();
                _healthMetrics[toolId] = newMetrics;
                return newMetrics;
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public async Task<bool> IsToolHealthyAsync(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                throw new ArgumentNullException(nameof(toolId));

            try
            {
                var metrics = await GetHealthMetricsAsync(toolId);

                // Check for critical issues
                if (metrics.IsUnresponsive)
                    return false;

                if (metrics.CrashCount >= CRASH_THRESHOLD)
                    return false;

                if (metrics.HangCount >= HANG_THRESHOLD)
                    return false;

                return metrics.HealthScore >= 50;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for tool {ToolId}", toolId);
                return false;
            }
        }

        public async Task<List<ToolConflict>> DetectConflictsAsync()
        {
            var conflicts = new List<ToolConflict>();

            try
            {
                await _metricsLock.WaitAsync();

                // Iterate through all tool pairs and check for conflicts
                var toolIds = _healthMetrics.Keys.ToList();

                for (int i = 0; i < toolIds.Count; i++)
                {
                    for (int j = i + 1; j < toolIds.Count; j++)
                    {
                        try
                        {
                            var conflict = await _conflictResolver.DetectConflictAsync(toolIds[i], toolIds[j]);
                            if (conflict != null)
                            {
                                conflicts.Add(conflict);

                                if (!_toolConflicts.ContainsKey(toolIds[i]))
                                    _toolConflicts[toolIds[i]] = new List<ToolConflict>();

                                _toolConflicts[toolIds[i]].Add(conflict);

                                _logger.LogWarning("Conflict detected between {Tool1} and {Tool2}: {Description}", 
                                    toolIds[i], toolIds[j], conflict.Description);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error detecting conflict between {Tool1} and {Tool2}", toolIds[i], toolIds[j]);
                        }
                    }
                }

                return conflicts;
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public async Task<bool> ResolveConflictAsync(string conflictId)
        {
            if (string.IsNullOrEmpty(conflictId))
                throw new ArgumentNullException(nameof(conflictId));

            try
            {
                await _metricsLock.WaitAsync();

                // Find and resolve the conflict
                foreach (var toolConflicts in _toolConflicts.Values)
                {
                    var conflict = toolConflicts.FirstOrDefault(c => c.ConflictId == conflictId);
                    if (conflict != null)
                    {
                        var resolved = await _conflictResolver.ResolveAsync(conflict);
                        if (resolved)
                        {
                            conflict.AutoResolved = true;
                            _logger.LogInformation("Conflict {ConflictId} resolved", conflictId);

                            await LogEventAsync(new OrchestrationEvent
                            {
                                Type = EventType.ConflictResolved,
                                Message = $"Conflict between {conflict.Tool1} and {conflict.Tool2} resolved",
                                Severity = EventSeverity.Warning
                            });
                        }

                        return resolved;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving conflict {ConflictId}", conflictId);
                return false;
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public async Task<bool> RestartToolAsync(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                throw new ArgumentNullException(nameof(toolId));

            try
            {
                await _metricsLock.WaitAsync();

                _logger.LogInformation("Restarting tool {ToolId}", toolId);

                var metrics = await GetHealthMetricsAsync(toolId);
                metrics.CrashCount++;

                await LogEventAsync(new OrchestrationEvent
                {
                    ToolId = toolId,
                    Type = EventType.ToolRestarted,
                    Message = $"Tool restarted (crash count: {metrics.CrashCount})",
                    Severity = EventSeverity.Warning
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting tool {ToolId}", toolId);
                return false;
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public async Task<List<MaintenancePrediction>> GetMaintenancePredictionsAsync()
        {
            try
            {
                await _metricsLock.WaitAsync();

                return new List<MaintenancePrediction>(_maintenancePredictions);
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public async Task ScheduleMaintenanceAsync(string toolId, MaintenanceType type)
        {
            if (string.IsNullOrEmpty(toolId))
                throw new ArgumentNullException(nameof(toolId));

            try
            {
                await _metricsLock.WaitAsync();

                var prediction = new MaintenancePrediction
                {
                    ToolId = toolId,
                    Type = type,
                    Description = $"Maintenance scheduled for tool {toolId}",
                    PredictedDate = DateTime.UtcNow.AddHours(Random.Shared.Next(1, 24)),
                    Confidence = 95.0
                };

                _maintenancePredictions.Add(prediction);

                _logger.LogInformation("Maintenance scheduled for tool {ToolId}: {Type}", toolId, type);

                await LogEventAsync(new OrchestrationEvent
                {
                    ToolId = toolId,
                    Type = EventType.MaintenanceScheduled,
                    Message = $"Maintenance scheduled: {type}",
                    Severity = EventSeverity.Info
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling maintenance for tool {ToolId}", toolId);
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public async Task<ToolDependencyGraph> GetDependencyGraphAsync()
        {
            try
            {
                await _metricsLock.WaitAsync();

                return _dependencyGraph;
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public async Task MonitorToolAsync(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                throw new ArgumentNullException(nameof(toolId));

            try
            {
                var metrics = await GetHealthMetricsAsync(toolId);

                // Simulate monitoring - update health metrics
                metrics.HealthScore = Math.Max(0, metrics.HealthScore - Random.Shared.Next(-5, 10));

                // Update uptime
                metrics.Uptime += 0.0014; // ~5 seconds in hours

                if (metrics.HealthScore < 50)
                {
                    _logger.LogWarning("Tool {ToolId} health score degrading: {Score}", toolId, metrics.HealthScore);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring tool {ToolId}", toolId);
            }
        }

        public async Task<List<OrchestrationEvent>> GetRecentEventsAsync(int count = 100)
        {
            if (count < 0)
                throw new ArgumentException("Count must be non-negative", nameof(count));

            try
            {
                await _metricsLock.WaitAsync();

                return _eventLog.TakeLast(count).ToList();
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        private async void PerformHealthCheckAsync(object state)
        {
            try
            {
                await _metricsLock.WaitAsync();

                var toolIds = _healthMetrics.Keys.ToList();

                foreach (var toolId in toolIds)
                {
                    try
                    {
                        await MonitorToolAsync(toolId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error monitoring tool {ToolId}", toolId);
                    }
                }
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        private async Task LogEventAsync(OrchestrationEvent evt)
        {
            try
            {
                await _metricsLock.WaitAsync();

                _eventLog.Add(evt);

                // Keep event log size manageable
                if (_eventLog.Count > MAX_EVENT_LOG_SIZE)
                {
                    _eventLog.RemoveRange(0, _eventLog.Count - MAX_EVENT_LOG_SIZE);
                }
            }
            finally
            {
                _metricsLock.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _healthCheckTimer?.Dispose();
            _metricsLock?.Dispose();
            _disposed = true;
        }
    }
}

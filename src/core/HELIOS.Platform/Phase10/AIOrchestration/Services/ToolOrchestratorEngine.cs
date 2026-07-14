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
    /// Master orchestration engine that coordinates all 45 system tools
    /// </summary>
    public class ToolOrchestratorEngine : IToolOrchestratorEngine, IDisposable
    {
        private readonly ILogger<ToolOrchestratorEngine> _logger;
        private readonly IToolHealthMonitorCoordinator _healthMonitor;
        private readonly IToolOptimizationProfiler _optimizer;
        private readonly IToolConflictResolver _conflictResolver;
        private readonly IToolCommunicationCoordinator _communicationCoordinator;

        private readonly Dictionary<string, ToolInfo> _tools;
        private readonly SemaphoreSlim _toolsLock;
        private readonly Timer _orchestrationTimer;
        private OrchestrationProfile _currentProfile;
        private bool _isRunning;
        private bool _disposed;

        private const int ORCHESTRATION_INTERVAL_MS = 5000;
        private const int MAX_RESTART_ATTEMPTS = 3;
        private const int HEALTH_CHECK_INTERVAL_MS = 3000;

        public ToolOrchestratorEngine(
            ILogger<ToolOrchestratorEngine> logger,
            IToolHealthMonitorCoordinator healthMonitor,
            IToolOptimizationProfiler optimizer,
            IToolConflictResolver conflictResolver,
            IToolCommunicationCoordinator communicationCoordinator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
            _optimizer = optimizer ?? throw new ArgumentNullException(nameof(optimizer));
            _conflictResolver = conflictResolver ?? throw new ArgumentNullException(nameof(conflictResolver));
            _communicationCoordinator = communicationCoordinator ?? throw new ArgumentNullException(nameof(communicationCoordinator));

            _tools = new Dictionary<string, ToolInfo>();
            _toolsLock = new SemaphoreSlim(1, 1);
            _currentProfile = OrchestrationProfile.Work;
            _isRunning = false;
            _orchestrationTimer = new Timer(OrchestrateAsync, null, Timeout.Infinite, Timeout.Infinite);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _toolsLock.WaitAsync();

                _logger.LogInformation("Initializing ToolOrchestratorEngine");

                await _healthMonitor.InitializeAsync();
                await _optimizer.InitializeAsync();

                _isRunning = true;
                _orchestrationTimer.Change(ORCHESTRATION_INTERVAL_MS, ORCHESTRATION_INTERVAL_MS);

                _logger.LogInformation("ToolOrchestratorEngine initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ToolOrchestratorEngine");
                throw;
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                await _toolsLock.WaitAsync();

                _logger.LogInformation("Shutting down ToolOrchestratorEngine");

                _isRunning = false;
                _orchestrationTimer.Change(Timeout.Infinite, Timeout.Infinite);

                foreach (var tool in _tools.Values.ToList())
                {
                    if (tool.Status != ToolStatus.Stopped)
                    {
                        await StopToolAsync(tool.ToolId);
                    }
                }

                await _healthMonitor.ShutdownAsync();

                _logger.LogInformation("ToolOrchestratorEngine shutdown complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ToolOrchestratorEngine shutdown");
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task<bool> RegisterToolAsync(ToolInfo tool)
        {
            if (tool == null)
                throw new ArgumentNullException(nameof(tool));

            try
            {
                await _toolsLock.WaitAsync();

                if (_tools.ContainsKey(tool.ToolId))
                {
                    _logger.LogWarning("Tool {ToolId} already registered", tool.ToolId);
                    return false;
                }

                _tools[tool.ToolId] = tool;
                tool.Status = ToolStatus.Initializing;

                _logger.LogInformation("Tool {ToolId} ({ToolName}) registered", tool.ToolId, tool.ToolName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering tool {ToolId}", tool.ToolId);
                return false;
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task<bool> UnregisterToolAsync(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                throw new ArgumentNullException(nameof(toolId));

            try
            {
                await _toolsLock.WaitAsync();

                if (!_tools.Remove(toolId, out var tool))
                {
                    _logger.LogWarning("Tool {ToolId} not found for unregistration", toolId);
                    return false;
                }

                _logger.LogInformation("Tool {ToolId} unregistered", toolId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering tool {ToolId}", toolId);
                return false;
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task<ToolInfo> GetToolInfoAsync(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                throw new ArgumentNullException(nameof(toolId));

            try
            {
                await _toolsLock.WaitAsync();

                if (_tools.TryGetValue(toolId, out var tool))
                {
                    return tool;
                }

                return null;
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task<IEnumerable<ToolInfo>> GetAllToolsAsync()
        {
            try
            {
                await _toolsLock.WaitAsync();
                return _tools.Values.ToList();
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task<bool> StartToolAsync(string toolId)
        {
            var tool = await GetToolInfoAsync(toolId);
            if (tool == null)
            {
                _logger.LogWarning("Cannot start unknown tool {ToolId}", toolId);
                return false;
            }

            try
            {
                await _toolsLock.WaitAsync();

                if (tool.Status == ToolStatus.Running)
                {
                    _logger.LogInformation("Tool {ToolId} is already running", toolId);
                    return true;
                }

                tool.Status = ToolStatus.Running;
                tool.UpdatedAt = DateTime.UtcNow;

                // Apply current profile configuration
                var profileConfig = await _optimizer.LoadProfileConfigAsync(toolId, _currentProfile.ToString());
                if (profileConfig != null)
                {
                    await _optimizer.UpdateResourceAllocationAsync(toolId, profileConfig.ResourceAllocation);
                }

                _logger.LogInformation("Tool {ToolId} started", toolId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting tool {ToolId}", toolId);
                return false;
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task<bool> StopToolAsync(string toolId)
        {
            var tool = await GetToolInfoAsync(toolId);
            if (tool == null)
            {
                _logger.LogWarning("Cannot stop unknown tool {ToolId}", toolId);
                return false;
            }

            try
            {
                await _toolsLock.WaitAsync();

                tool.Status = ToolStatus.Stopped;
                tool.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Tool {ToolId} stopped", toolId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping tool {ToolId}", toolId);
                return false;
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task SwitchProfileAsync(OrchestrationProfile profile)
        {
            try
            {
                await _toolsLock.WaitAsync();

                _currentProfile = profile;
                _logger.LogInformation("Switched to {Profile} profile", profile);

                // Reconfigure all running tools for the new profile
                var runningTools = _tools.Values.Where(t => t.Status == ToolStatus.Running).ToList();

                foreach (var tool in runningTools)
                {
                    try
                    {
                        var profileConfig = await _optimizer.LoadProfileConfigAsync(tool.ToolId, profile.ToString());
                        if (profileConfig != null)
                        {
                            await _optimizer.UpdateResourceAllocationAsync(tool.ToolId, profileConfig.ResourceAllocation);
                            _logger.LogDebug("Updated {ToolId} configuration for {Profile}", tool.ToolId, profile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error updating configuration for tool {ToolId} in profile {Profile}", tool.ToolId, profile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching to profile {Profile}", profile);
                throw;
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task<OrchestrationProfile> GetCurrentProfileAsync()
        {
            await _toolsLock.WaitAsync();
            try
            {
                return _currentProfile;
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task<OrchestrationStats> GetStatsAsync()
        {
            try
            {
                await _toolsLock.WaitAsync();

                var stats = new OrchestrationStats
                {
                    TotalToolsManaged = _tools.Count,
                    ToolsRunning = _tools.Values.Count(t => t.Status == ToolStatus.Running),
                    ToolsFailed = _tools.Values.Count(t => t.Status == ToolStatus.Failed),
                    AverageSystemHealth = _tools.Count > 0 
                        ? _tools.Values.Average(t => t.HealthMetrics.HealthScore) 
                        : 100,
                    AverageCpuUtilization = _tools.Count > 0
                        ? _tools.Values.Average(t => t.PerformanceMetrics.AverageCpuUsage)
                        : 0,
                    AverageMemoryUtilization = _tools.Count > 0
                        ? _tools.Values.Average(t => t.PerformanceMetrics.AverageMemoryUsage)
                        : 0
                };

                return stats;
            }
            finally
            {
                _toolsLock.Release();
            }
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var stats = await GetStatsAsync();
                var failures = stats.ToolsFailed;
                var total = stats.TotalToolsManaged;

                if (total == 0)
                    return true;

                var failureRate = (double)failures / total;
                return failureRate < 0.1; // 10% failure threshold
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking orchestration health");
                return false;
            }
        }

        private async void OrchestrateAsync(object state)
        {
            if (!_isRunning)
                return;

            try
            {
                // Detect conflicts
                var conflicts = await _healthMonitor.DetectConflictsAsync();
                foreach (var conflict in conflicts)
                {
                    try
                    {
                        await _healthMonitor.ResolveConflictAsync(conflict.ConflictId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to resolve conflict {ConflictId}", conflict.ConflictId);
                    }
                }

                // Monitor tool health
                var tools = await GetAllToolsAsync();
                foreach (var tool in tools)
                {
                    try
                    {
                        if (!await _healthMonitor.IsToolHealthyAsync(tool.ToolId))
                        {
                            if (tool.HealthMetrics.CrashCount < MAX_RESTART_ATTEMPTS)
                            {
                                _logger.LogWarning("Tool {ToolId} is unhealthy, attempting restart", tool.ToolId);
                                await _healthMonitor.RestartToolAsync(tool.ToolId);
                            }
                            else
                            {
                                _logger.LogError("Tool {ToolId} exceeded max restart attempts", tool.ToolId);
                                await StopToolAsync(tool.ToolId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error monitoring tool {ToolId}", tool.ToolId);
                    }
                }

                // Apply optimizations periodically
                var optimizationInterval = 60000 / ORCHESTRATION_INTERVAL_MS; // Every 60 seconds
                if ((DateTime.UtcNow.Second % optimizationInterval) == 0)
                {
                    foreach (var tool in tools.Where(t => t.Status == ToolStatus.Running))
                    {
                        try
                        {
                            var recommendations = await _optimizer.AnalyzeToolAsync(tool.ToolId);
                            foreach (var rec in recommendations.Where(r => r.Priority >= 8))
                            {
                                await _optimizer.ApplyOptimizationAsync(tool.ToolId, rec);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error optimizing tool {ToolId}", tool.ToolId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in orchestration loop");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _orchestrationTimer?.Dispose();
            _toolsLock?.Dispose();
            _disposed = true;
        }
    }
}

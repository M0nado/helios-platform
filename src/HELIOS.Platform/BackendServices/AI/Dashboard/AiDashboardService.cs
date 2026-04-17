using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using HELIOS.Platform.Core.Infrastructure;

namespace HELIOS.Platform.BackendServices.AI.Dashboard
{
    /// <summary>
    /// Central AI Dashboard Service providing real-time monitoring, analytics, and control
    /// for all AI components including models, agents, and performance metrics.
    /// </summary>
    public interface IAiDashboardService
    {
        Task<DashboardMetrics> GetMetricsAsync();
        Task<ModelStatus[]> GetModelStatusAsync();
        Task<AgentStatus[]> GetAgentStatusAsync();
        Task<PerformanceMetrics> GetPerformanceMetricsAsync();
        Task<TokenUsageReport> GetTokenUsageAsync();
        Task UpdateWorkflowAsync(WorkflowDefinition workflow);
        Task<WorkflowDefinition[]> GetWorkflowsAsync();
        event EventHandler<DashboardUpdateEventArgs> MetricsUpdated;
    }

    public class AiDashboardService : IAiDashboardService
    {
        private readonly ConcurrentDictionary<string, ModelStatus> _modelStates;
        private readonly ConcurrentDictionary<string, AgentStatus> _agentStates;
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _workflows;
        private readonly PerformanceMetricsCollector _metricsCollector;
        private readonly TokenUsageTracker _tokenTracker;

        public event EventHandler<DashboardUpdateEventArgs> MetricsUpdated;

        public AiDashboardService()
        {
            _modelStates = new ConcurrentDictionary<string, ModelStatus>();
            _agentStates = new ConcurrentDictionary<string, AgentStatus>();
            _workflows = new ConcurrentDictionary<string, WorkflowDefinition>();
            _metricsCollector = new PerformanceMetricsCollector();
            _tokenTracker = new TokenUsageTracker();
        }

        public async Task<DashboardMetrics> GetMetricsAsync()
        {
            return await Task.FromResult(new DashboardMetrics
            {
                Timestamp = DateTime.UtcNow,
                ActiveModels = _modelStates.Values.Count(m => m.IsActive),
                ActiveAgents = _agentStates.Values.Count(a => a.IsRunning),
                TotalTokensUsed = _tokenTracker.GetTotalTokensUsed(),
                AverageLatency = _metricsCollector.GetAverageLatency(),
                SystemHealth = CalculateSystemHealth(),
                TopPerformers = GetTopPerformers(),
                BottlenecksDetected = _metricsCollector.GetBottlenecks()
            });
        }

        public async Task<ModelStatus[]> GetModelStatusAsync()
        {
            return await Task.FromResult(_modelStates.Values.ToArray());
        }

        public async Task<AgentStatus[]> GetAgentStatusAsync()
        {
            return await Task.FromResult(_agentStates.Values.ToArray());
        }

        public async Task<PerformanceMetrics> GetPerformanceMetricsAsync()
        {
            return await _metricsCollector.GetDetailedMetricsAsync();
        }

        public async Task<TokenUsageReport> GetTokenUsageAsync()
        {
            return await _tokenTracker.GetUsageReportAsync();
        }

        public async Task UpdateWorkflowAsync(WorkflowDefinition workflow)
        {
            _workflows.AddOrUpdate(workflow.Id, workflow, (_, _) => workflow);
            MetricsUpdated?.Invoke(this, new DashboardUpdateEventArgs 
            { 
                UpdateType = DashboardUpdateType.WorkflowUpdated,
                Timestamp = DateTime.UtcNow 
            });
            await Task.CompletedTask;
        }

        public async Task<WorkflowDefinition[]> GetWorkflowsAsync()
        {
            return await Task.FromResult(_workflows.Values.ToArray());
        }

        public void RegisterModelStatus(ModelStatus status)
        {
            _modelStates.AddOrUpdate(status.ModelId, status, (_, _) => status);
        }

        public void RegisterAgentStatus(AgentStatus status)
        {
            _agentStates.AddOrUpdate(status.AgentId, status, (_, _) => status);
        }

        private SystemHealthStatus CalculateSystemHealth()
        {
            var activeModels = _modelStates.Values.Count(m => m.IsActive);
            var healthyModels = _modelStates.Values.Count(m => m.IsHealthy);
            var errorRate = _metricsCollector.GetErrorRate();

            if (errorRate > 0.1) return SystemHealthStatus.Critical;
            if (errorRate > 0.05) return SystemHealthStatus.Warning;
            if (activeModels > 0 && healthyModels < activeModels * 0.8) return SystemHealthStatus.Degraded;
            return SystemHealthStatus.Healthy;
        }

        private string[] GetTopPerformers()
        {
            return _agentStates.Values
                .OrderByDescending(a => a.SuccessRate)
                .Take(5)
                .Select(a => a.AgentId)
                .ToArray();
        }
    }

    public class DashboardMetrics
    {
        public DateTime Timestamp { get; set; }
        public int ActiveModels { get; set; }
        public int ActiveAgents { get; set; }
        public long TotalTokensUsed { get; set; }
        public double AverageLatency { get; set; }
        public SystemHealthStatus SystemHealth { get; set; }
        public string[] TopPerformers { get; set; }
        public string[] BottlenecksDetected { get; set; }
    }

    public class ModelStatus
    {
        public string ModelId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsHealthy { get; set; }
        public double MemoryUsage { get; set; }
        public double CpuUsage { get; set; }
        public int RequestsHandled { get; set; }
        public double AverageInferenceTime { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class AgentStatus
    {
        public string AgentId { get; set; }
        public string Name { get; set; }
        public bool IsRunning { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksFailed { get; set; }
        public double SuccessRate { get; set; }
        public long MemoryUsage { get; set; }
        public double AverageExecutionTime { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class WorkflowDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<WorkflowStep> Steps { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }

    public class WorkflowStep
    {
        public string StepId { get; set; }
        public string StepType { get; set; }
        public string ComponentId { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public List<string> DependsOn { get; set; }
    }

    public class PerformanceMetrics
    {
        public double P50Latency { get; set; }
        public double P95Latency { get; set; }
        public double P99Latency { get; set; }
        public double ThroughputTasksPerSecond { get; set; }
        public double ErrorRate { get; set; }
        public double CacheHitRatio { get; set; }
        public long MemoryPeakMb { get; set; }
        public DateTime CapturedAt { get; set; }
    }

    public class TokenUsageReport
    {
        public long TotalTokensUsed { get; set; }
        public Dictionary<string, long> TokensByModel { get; set; }
        public Dictionary<string, long> TokensByAgent { get; set; }
        public double AverageTokensPerRequest { get; set; }
        public DateTime ReportedAt { get; set; }
    }

    public enum SystemHealthStatus
    {
        Healthy,
        Degraded,
        Warning,
        Critical
    }

    public enum DashboardUpdateType
    {
        MetricsUpdated,
        ModelStatusChanged,
        AgentStatusChanged,
        WorkflowUpdated,
        PerformanceAlert
    }

    public class DashboardUpdateEventArgs : EventArgs
    {
        public DashboardUpdateType UpdateType { get; set; }
        public DateTime Timestamp { get; set; }
        public object Data { get; set; }
    }

    /// <summary>
    /// Collects and aggregates performance metrics from all components
    /// </summary>
    public class PerformanceMetricsCollector
    {
        private readonly ConcurrentDictionary<string, List<double>> _latencies;
        private readonly ConcurrentDictionary<string, int> _errors;
        private readonly ConcurrentDictionary<string, int> _cacheHits;
        private readonly ConcurrentDictionary<string, int> _cacheMisses;

        public PerformanceMetricsCollector()
        {
            _latencies = new ConcurrentDictionary<string, List<double>>();
            _errors = new ConcurrentDictionary<string, int>();
            _cacheHits = new ConcurrentDictionary<string, int>();
            _cacheMisses = new ConcurrentDictionary<string, int>();
        }

        public double GetAverageLatency()
        {
            var allLatencies = _latencies.Values.SelectMany(l => l).ToList();
            return allLatencies.Any() ? allLatencies.Average() : 0;
        }

        public double GetErrorRate()
        {
            var totalErrors = _errors.Values.Sum();
            var totalLatencies = _latencies.Values.Sum(l => l.Count);
            return totalLatencies > 0 ? (double)totalErrors / totalLatencies : 0;
        }

        public string[] GetBottlenecks()
        {
            var slowComponents = _latencies
                .Where(kvp => kvp.Value.Any() && kvp.Value.Average() > 5000)
                .Select(kvp => kvp.Key)
                .ToArray();
            return slowComponents;
        }

        public async Task<PerformanceMetrics> GetDetailedMetricsAsync()
        {
            var allLatencies = _latencies.Values.SelectMany(l => l).OrderBy(l => l).ToList();
            
            return await Task.FromResult(new PerformanceMetrics
            {
                P50Latency = GetPercentile(allLatencies, 0.5),
                P95Latency = GetPercentile(allLatencies, 0.95),
                P99Latency = GetPercentile(allLatencies, 0.99),
                ThroughputTasksPerSecond = CalculateThroughput(),
                ErrorRate = GetErrorRate(),
                CacheHitRatio = CalculateCacheHitRatio(),
                MemoryPeakMb = GC.GetTotalMemory(false) / (1024 * 1024),
                CapturedAt = DateTime.UtcNow
            });
        }

        public void RecordLatency(string componentId, double latencyMs)
        {
            _latencies.AddOrUpdate(componentId, 
                new List<double> { latencyMs },
                (_, list) => { list.Add(latencyMs); return list; });
        }

        public void RecordError(string componentId)
        {
            _errors.AddOrUpdate(componentId, 1, (_, count) => count + 1);
        }

        public void RecordCacheHit(string componentId)
        {
            _cacheHits.AddOrUpdate(componentId, 1, (_, count) => count + 1);
        }

        public void RecordCacheMiss(string componentId)
        {
            _cacheMisses.AddOrUpdate(componentId, 1, (_, count) => count + 1);
        }

        private double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (!sortedValues.Any()) return 0;
            var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
            return sortedValues[Math.Max(0, index)];
        }

        private double CalculateThroughput()
        {
            var totalLatencies = _latencies.Values.Sum(l => l.Count);
            return totalLatencies / 60.0; // Tasks per second (assuming 60s window)
        }

        private double CalculateCacheHitRatio()
        {
            var totalHits = _cacheHits.Values.Sum();
            var totalMisses = _cacheMisses.Values.Sum();
            var total = totalHits + totalMisses;
            return total > 0 ? (double)totalHits / total : 0;
        }
    }

    /// <summary>
    /// Tracks token usage across all models and agents
    /// </summary>
    public class TokenUsageTracker
    {
        private readonly ConcurrentDictionary<string, long> _tokensByModel;
        private readonly ConcurrentDictionary<string, long> _tokensByAgent;
        private long _totalTokens;

        public TokenUsageTracker()
        {
            _tokensByModel = new ConcurrentDictionary<string, long>();
            _tokensByAgent = new ConcurrentDictionary<string, long>();
            _totalTokens = 0;
        }

        public long GetTotalTokensUsed()
        {
            return _totalTokens;
        }

        public void RecordTokenUsage(string modelId, string agentId, long tokenCount)
        {
            _tokensByModel.AddOrUpdate(modelId, tokenCount, (_, current) => current + tokenCount);
            _tokensByAgent.AddOrUpdate(agentId, tokenCount, (_, current) => current + tokenCount);
            Interlocked.Add(ref _totalTokens, tokenCount);
        }

        public async Task<TokenUsageReport> GetUsageReportAsync()
        {
            var totalRequests = _tokensByModel.Values.Count;
            return await Task.FromResult(new TokenUsageReport
            {
                TotalTokensUsed = _totalTokens,
                TokensByModel = new Dictionary<string, long>(_tokensByModel),
                TokensByAgent = new Dictionary<string, long>(_tokensByAgent),
                AverageTokensPerRequest = totalRequests > 0 ? _totalTokens / (double)totalRequests : 0,
                ReportedAt = DateTime.UtcNow
            });
        }
    }
}

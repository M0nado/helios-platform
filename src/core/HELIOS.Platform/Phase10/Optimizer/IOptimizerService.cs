using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Optimizer
{
    /// <summary>
    /// Interface for all optimizer services in Phase 10G
    /// </summary>
    public interface IOptimizerService
    {
        /// <summary>
        /// Service name identifier
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Initialize the optimizer service
        /// </summary>
        /// <returns>Initialization result</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Start optimization process
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimization result with metrics</returns>
        Task<OptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get current performance metrics
        /// </summary>
        /// <returns>Dictionary of performance metrics</returns>
        Task<Dictionary<string, object>> GetMetricsAsync();

        /// <summary>
        /// Rollback last optimization
        /// </summary>
        /// <returns>Rollback result</returns>
        Task<bool> RollbackAsync();

        /// <summary>
        /// Get service status
        /// </summary>
        /// <returns>Service status information</returns>
        Task<ServiceStatus> GetStatusAsync();
    }

    /// <summary>
    /// Result of optimization operation
    /// </summary>
    public class OptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public TimeSpan ExecutionTime { get; set; }
        public List<string> Changes { get; set; } = new();
        public string RollbackSnapshot { get; set; }
    }

    /// <summary>
    /// Service status information
    /// </summary>
    public class ServiceStatus
    {
        public string ServiceName { get; set; }
        public bool IsInitialized { get; set; }
        public bool IsRunning { get; set; }
        public DateTime LastOptimization { get; set; }
        public float PerformanceGain { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Performance metrics container
    /// </summary>
    public class PerformanceMetrics
    {
        public float CPUUsage { get; set; }
        public float RAMUsage { get; set; }
        public float DiskUsage { get; set; }
        public float GPUUsage { get; set; }
        public float NetworkBandwidth { get; set; }
        public float Temperature { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Configuration profile for optimization
    /// </summary>
    public class OptimizationProfile
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
        public bool EnableRegistry { get; set; }
        public bool EnableProcessManagement { get; set; }
        public bool EnableNetworkTuning { get; set; }
        public bool EnableGPUOptimization { get; set; }
        public bool EnablePowerOptimization { get; set; }
        public int MaxExecutionTimeSeconds { get; set; } = 300;
    }

    /// <summary>
    /// GPU information
    /// </summary>
    public class GPUInfo
    {
        public string GPU { get; set; }
        public string Vendor { get; set; }
        public float VRAMTotal { get; set; }
        public float VRAMAvailable { get; set; }
        public float Temperature { get; set; }
        public float GPUUsage { get; set; }
    }

    /// <summary>
    /// Power profile types
    /// </summary>
    public enum PowerProfileType
    {
        Gaming,
        Work,
        Development,
        Secure,
        BalancedPower,
        PowerSaver
    }

    /// <summary>
    /// Base class for optimizer services
    /// </summary>
    public abstract class BaseOptimizerService : IOptimizerService
    {
        protected string _serviceName;
        protected bool _isInitialized;
        protected DateTime _lastOptimization;
        protected List<string> _errors = new();
        protected OptimizationProfile _profile;

        public string ServiceName => _serviceName;

        public virtual async Task<bool> InitializeAsync()
        {
            try
            {
                _isInitialized = true;
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _errors.Add($"Initialization error: {ex.Message}");
                return false;
            }
        }

        public abstract Task<OptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default);

        public abstract Task<Dictionary<string, object>> GetMetricsAsync();

        public virtual async Task<bool> RollbackAsync()
        {
            return await Task.FromResult(true);
        }

        public virtual async Task<ServiceStatus> GetStatusAsync()
        {
            return await Task.FromResult(new ServiceStatus
            {
                ServiceName = _serviceName,
                IsInitialized = _isInitialized,
                IsRunning = true,
                LastOptimization = _lastOptimization,
                Errors = _errors
            });
        }

        protected void LogError(string message)
        {
            _errors.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }
    }
}

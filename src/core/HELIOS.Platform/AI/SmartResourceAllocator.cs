using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.AI
{
    /// <summary>
    /// Intelligently allocates system resources based on predicted load and usage patterns.
    /// </summary>
    public class SmartResourceAllocator
    {
        private readonly object _lockObj = new();
        private double _cpuUsage;
        private double _memoryUsage;
        private int _activeThreads;
        private int _minThreads = 4;
        private int _maxThreads = 64;
        private long _cacheSize = 512 * 1024 * 1024; // 512 MB
        private long _maxCacheSize = 2L * 1024 * 1024 * 1024; // 2 GB
        private bool _isEnabled = true;
        private DateTime _lastAllocationTime = DateTime.UtcNow;

        public event EventHandler<ResourceAllocationEventArgs>? AllocationChanged;

        public class ResourceAllocation
        {
            public int ThreadPoolSize { get; set; }
            public long CacheSize { get; set; }
            public double CpuBudget { get; set; }
            public double MemoryBudget { get; set; }
            public int Priority { get; set; }
            public DateTime AllocatedAt { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        public class SystemResourceMetrics
        {
            public double CpuUsage { get; set; }
            public double MemoryUsage { get; set; }
            public int AvailableThreads { get; set; }
            public long AvailableMemory { get; set; }
            public double AllocationEfficiency { get; set; }
            public DateTime MeasuredAt { get; set; }
        }

        public void UpdateResourceMetrics(double cpuUsage, double memoryUsage, int activeThreads)
        {
            lock (_lockObj)
            {
                _cpuUsage = Math.Clamp(cpuUsage, 0.0, 1.0);
                _memoryUsage = Math.Clamp(memoryUsage, 0.0, 1.0);
                _activeThreads = activeThreads;
            }
        }

        public async Task<ResourceAllocation> AllocateResources(
            double expectedLoad,
            string workloadType = "general")
        {
            if (!_isEnabled)
                return GetCurrentAllocation();

            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if ((DateTime.UtcNow - _lastAllocationTime).TotalSeconds < 5)
                        return GetCurrentAllocation();

                    var allocation = new ResourceAllocation
                    {
                        AllocatedAt = DateTime.UtcNow,
                        ThreadPoolSize = CalculateOptimalThreadPoolSize(expectedLoad),
                        CacheSize = CalculateOptimalCacheSize(expectedLoad),
                        CpuBudget = CalculateCpuBudget(expectedLoad),
                        MemoryBudget = CalculateMemoryBudget(expectedLoad),
                        Priority = CalculatePriority(expectedLoad),
                        Reason = $"Workload: {workloadType}, Load: {expectedLoad:P}"
                    };

                    _minThreads = Math.Max(2, allocation.ThreadPoolSize / 2);
                    _maxThreads = allocation.ThreadPoolSize;
                    _cacheSize = allocation.CacheSize;

                    _lastAllocationTime = DateTime.UtcNow;

                    AllocationChanged?.Invoke(this, new ResourceAllocationEventArgs { Allocation = allocation });

                    return allocation;
                }
            });
        }

        private int CalculateOptimalThreadPoolSize(double expectedLoad)
        {
            var processorCount = Environment.ProcessorCount;
            var baseSize = processorCount;

            if (expectedLoad > 0.8)
                return Math.Min(_maxThreads, (int)(baseSize * 2));
            else if (expectedLoad > 0.5)
                return Math.Min(_maxThreads, (int)(baseSize * 1.5));
            else if (expectedLoad < 0.2)
                return Math.Max(_minThreads, (int)(baseSize * 0.5));

            return baseSize;
        }

        private long CalculateOptimalCacheSize(double expectedLoad)
        {
            var totalMemory = GC.GetTotalMemory(false);
            var safeLimit = totalMemory / 4;

            if (expectedLoad > 0.7)
            {
                return Math.Min(_maxCacheSize, (long)(safeLimit * 0.6));
            }
            else if (expectedLoad > 0.4)
            {
                return Math.Min(_maxCacheSize, (long)(safeLimit * 0.4));
            }

            return Math.Min(_maxCacheSize, (long)(safeLimit * 0.2));
        }

        private double CalculateCpuBudget(double expectedLoad)
        {
            if (expectedLoad > 0.7)
                return Math.Min(0.9, _cpuUsage + 0.15);
            else if (expectedLoad > 0.4)
                return Math.Min(0.8, _cpuUsage + 0.1);

            return 0.6;
        }

        private double CalculateMemoryBudget(double expectedLoad)
        {
            if (expectedLoad > 0.7)
                return Math.Min(0.85, _memoryUsage + 0.15);
            else if (expectedLoad > 0.4)
                return Math.Min(0.75, _memoryUsage + 0.1);

            return 0.5;
        }

        private int CalculatePriority(double expectedLoad)
        {
            if (expectedLoad > 0.8) return 10;
            if (expectedLoad > 0.6) return 7;
            if (expectedLoad > 0.3) return 5;
            return 1;
        }

        public ResourceAllocation GetCurrentAllocation()
        {
            lock (_lockObj)
            {
                return new ResourceAllocation
                {
                    ThreadPoolSize = _maxThreads,
                    CacheSize = _cacheSize,
                    CpuBudget = _cpuUsage,
                    MemoryBudget = _memoryUsage,
                    Priority = CalculatePriority(_cpuUsage),
                    AllocatedAt = _lastAllocationTime
                };
            }
        }

        public SystemResourceMetrics GetSystemMetrics()
        {
            lock (_lockObj)
            {
                var allocation = GetCurrentAllocation();
                var efficiency = CalculateAllocationEfficiency();

                return new SystemResourceMetrics
                {
                    CpuUsage = _cpuUsage,
                    MemoryUsage = _memoryUsage,
                    AvailableThreads = _maxThreads - _activeThreads,
                    AvailableMemory = GC.GetTotalMemory(false) / 4,
                    AllocationEfficiency = efficiency,
                    MeasuredAt = DateTime.UtcNow
                };
            }
        }

        private double CalculateAllocationEfficiency()
        {
            var cpuEfficiency = 1.0 - Math.Abs(_cpuUsage - 0.7); // Optimal around 70%
            var memoryEfficiency = 1.0 - Math.Abs(_memoryUsage - 0.6); // Optimal around 60%
            var threadEfficiency = _activeThreads > 0 ? Math.Min(1.0, _activeThreads / (double)_maxThreads) : 0.5;

            return (cpuEfficiency + memoryEfficiency + threadEfficiency) / 3.0;
        }

        public bool ShouldScaleUp()
        {
            lock (_lockObj)
            {
                return _cpuUsage > 0.8 || _memoryUsage > 0.75;
            }
        }

        public bool ShouldScaleDown()
        {
            lock (_lockObj)
            {
                return _cpuUsage < 0.3 && _memoryUsage < 0.4 && _activeThreads < _minThreads * 2;
            }
        }

        public void SetResourceLimits(int minThreads, int maxThreads, long maxCache)
        {
            lock (_lockObj)
            {
                _minThreads = Math.Max(1, minThreads);
                _maxThreads = Math.Max(_minThreads, maxThreads);
                _maxCacheSize = Math.Max(100 * 1024 * 1024, maxCache);
            }
        }

        public void EnableDisable(bool enabled) => _isEnabled = enabled;

        public double GetEnergyAwarenessScore()
        {
            lock (_lockObj)
            {
                // Lower score when resources are underutilized (energy waste)
                var idleScore = _cpuUsage < 0.2 ? 0.3 : 1.0;
                var memoryScore = _memoryUsage < 0.3 ? 0.4 : 1.0;

                return (idleScore + memoryScore) / 2.0;
            }
        }

        public async Task RebalanceResources()
        {
            await AllocateResources((_cpuUsage + _memoryUsage) / 2.0, "rebalance");
        }
    }

    public class ResourceAllocationEventArgs : EventArgs
    {
        public SmartResourceAllocator.ResourceAllocation? Allocation { get; set; }
    }
}

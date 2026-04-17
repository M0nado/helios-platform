using System;
using System.Collections.Concurrent;
using System.Text;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// Memory optimization and object pooling service
    /// </summary>
    public interface IMemoryOptimizationService
    {
        void LogMemoryStats();
        void ForceGarbageCollection();
        MemoryStats GetMemoryStats();
    }

    /// <summary>
    /// Memory statistics
    /// </summary>
    public class MemoryStats
    {
        public long TotalMemoryBytes { get; set; }
        public long WorkingSetBytes { get; set; }
        public long ManagedHeapBytes { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }

        public double TotalMemoryMB => TotalMemoryBytes / 1024.0 / 1024.0;
        public double WorkingSetMB => WorkingSetBytes / 1024.0 / 1024.0;
        public double ManagedHeapMB => ManagedHeapBytes / 1024.0 / 1024.0;
    }

    /// <summary>
    /// Memory optimization service
    /// </summary>
    public class MemoryOptimizationService : IMemoryOptimizationService
    {
        private readonly Logging.ILogger _logger;

        public MemoryOptimizationService(Logging.ILogger logger) => _logger = logger;

        public void LogMemoryStats()
        {
            var stats = GetMemoryStats();
            _logger?.Info(
                $"Memory Stats: Total={stats.TotalMemoryMB:F2}MB, " +
                $"WorkingSet={stats.WorkingSetMB:F2}MB, " +
                $"Managed={stats.ManagedHeapMB:F2}MB, " +
                $"GC(Gen0={stats.Gen0Collections}, Gen1={stats.Gen1Collections}, Gen2={stats.Gen2Collections})"
            );
        }

        public void ForceGarbageCollection()
        {
            _logger?.Info("Triggering garbage collection...");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public MemoryStats GetMemoryStats()
        {
            using var proc = System.Diagnostics.Process.GetCurrentProcess();
            return new MemoryStats
            {
                TotalMemoryBytes = GC.GetTotalMemory(false),
                WorkingSetBytes = proc.WorkingSet64,
                ManagedHeapBytes = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };
        }
    }

    /// <summary>
    /// StringBuilder object pool for reducing allocations
    /// </summary>
    public interface IStringBuilderPool
    {
        StringBuilder Get();
        void Return(StringBuilder sb);
    }

    /// <summary>
    /// Lightweight StringBuilder pool
    /// </summary>
    public class StringBuilderPool : IStringBuilderPool
    {
        private readonly ConcurrentBag<StringBuilder> _pool = new();
        private readonly int _maxPoolSize = 32;
        private const int MaxBuilderCapacity = 4096;

        public StringBuilder Get()
        {
            return _pool.TryTake(out var sb) && sb.Capacity <= MaxBuilderCapacity
                ? sb
                : new StringBuilder();
        }

        public void Return(StringBuilder sb)
        {
            if (sb.Capacity > MaxBuilderCapacity)
                return;

            sb.Clear();
            if (_pool.Count < _maxPoolSize)
                _pool.Add(sb);
        }
    }
}

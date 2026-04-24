using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// AI-driven resource allocator that predicts resource needs and optimally
    /// allocates resources across services with dynamic reallocation support.
    /// </summary>
    public interface IIntelligentResourceAllocator
    {
        Task<bool> InitializeAsync();
        Task<ResourcePrediction> PredictResourceNeedsAsync(string serviceId, int timeHorizonMinutes);
        Task<AllocationPlan> GenerateAllocationPlanAsync(Dictionary<string, ResourceRequirement> services);
        Task<bool> ApplyAllocationAsync(AllocationPlan plan);
        Task<ResourceAllocationMetrics> GetAllocationMetricsAsync();
        Task<ResourceUtilization[]> GetCurrentUtilizationAsync();
    }

    /// <summary>Represents predicted resource requirements.</summary>
    public class ResourcePrediction
    {
        public string ServiceId { get; set; } = string.Empty;
        public double PredictedCPUPercent { get; set; }
        public double PredictedMemoryMB { get; set; }
        public double PredictedDiskIOPS { get; set; }
        public double PredictedNetworkMbps { get; set; }
        public double Confidence { get; set; }
        public DateTime PredictionTime { get; set; } = DateTime.UtcNow;
        public List<TrendDataPoint> HistoricalTrends { get; set; } = new();
    }

    /// <summary>Historical trend data for prediction.</summary>
    public class TrendDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public double Variance { get; set; }
    }

    /// <summary>Resource requirement for a service.</summary>
    public class ResourceRequirement
    {
        public string ServiceId { get; set; } = string.Empty;
        public double MinCPUPercent { get; set; }
        public double MaxCPUPercent { get; set; }
        public double MinMemoryMB { get; set; }
        public double MaxMemoryMB { get; set; }
        public double Priority { get; set; }
        public bool IsCritical { get; set; }
    }

    /// <summary>Resource allocation plan.</summary>
    public class AllocationPlan
    {
        public string PlanId { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<ServiceAllocation> Allocations { get; set; } = new();
        public double TotalCPUAllocated { get; set; }
        public double TotalMemoryAllocated { get; set; }
        public double UtilizationScore { get; set; }
        public double WastePercentage { get; set; }
        public DateTime ValidUntil { get; set; }
    }

    /// <summary>Allocation for a single service.</summary>
    public class ServiceAllocation
    {
        public string ServiceId { get; set; } = string.Empty;
        public double AllocatedCPUPercent { get; set; }
        public double AllocatedMemoryMB { get; set; }
        public double AllocatedDiskIOPS { get; set; }
        public double AllocatedNetworkMbps { get; set; }
        public double Priority { get; set; }
    }

    /// <summary>Tracks allocation efficiency metrics.</summary>
    public class ResourceAllocationMetrics
    {
        public int TotalServices { get; set; }
        public double TotalAllocatedCPU { get; set; }
        public double TotalAllocatedMemory { get; set; }
        public double AverageCPUUtilization { get; set; }
        public double AverageMemoryUtilization { get; set; }
        public double WastePercentage { get; set; }
        public double UtilizationScore { get; set; }
        public int ReallocationCount { get; set; }
        public DateTime LastReallocationTime { get; set; }
        public long TotalAllocationEvents { get; set; }
    }

    /// <summary>Current resource utilization snapshot.</summary>
    public class ResourceUtilization
    {
        public string ServiceId { get; set; } = string.Empty;
        public double CPUUsedPercent { get; set; }
        public double MemoryUsedMB { get; set; }
        public double DiskIOPS { get; set; }
        public double NetworkMbps { get; set; }
        public double AllocatedCPUPercent { get; set; }
        public double AllocatedMemoryMB { get; set; }
        public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
        public double UtilizationScore { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Core.Administration;

public class PredictionModel
{
    public string ModelId { get; set; }
    public string ModelName { get; set; }
    public string ModelType { get; set; }
    public DateTime TrainedAt { get; set; }
    public double Accuracy { get; set; }
    public int SampleCount { get; set; }
    public bool IsActive { get; set; }
}

public class Prediction
{
    public string PredictionId { get; set; }
    public string ModelId { get; set; }
    public double PredictedValue { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, double> Features { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public interface IMachineLearningService
{
    Task<PredictionModel> TrainModelAsync(string modelName, string modelType, List<Dictionary<string, double>> trainingData);
    Task<PredictionModel> GetModelAsync(string modelId);
    Task<List<PredictionModel>> ListModelsAsync();
    Task<bool> DeleteModelAsync(string modelId);
    Task<bool> ActivateModelAsync(string modelId);
    Task<Prediction> MakePredictionAsync(string modelId, Dictionary<string, double> features);
    Task<List<Prediction>> GetPredictionHistoryAsync(string modelId, int limit = 100);
    Task<double> GetModelAccuracyAsync(string modelId);
}

public class WorkloadPattern
{
    public string PatternId { get; set; }
    public string WorkloadName { get; set; }
    public double AverageLoad { get; set; }
    public double PeakLoad { get; set; }
    public DateTime PeakTime { get; set; }
    public int HistoricalDays { get; set; }
    public List<double> LoadHistory { get; set; } = new();
}

public class ResourceAllocation
{
    public string AllocationId { get; set; }
    public string WorkloadId { get; set; }
    public int RecommendedCPU { get; set; }
    public int RecommendedMemoryMB { get; set; }
    public int RecommendedDiskMB { get; set; }
    public DateTime CalculatedAt { get; set; }
    public bool IsApplied { get; set; }
}

public interface IPredictiveResourcePlanning
{
    Task<WorkloadPattern> AnalyzeWorkloadAsync(string workloadName, List<double> loadHistory);
    Task<ResourceAllocation> PredictResourceNeedsAsync(string workloadId);
    Task<List<WorkloadPattern>> GetWorkloadPatternsAsync();
    Task<bool> ApplyAllocationAsync(string allocationId);
    Task<List<ResourceAllocation>> GetAllocationHistoryAsync(int limit = 100);
    Task<Dictionary<string, int>> GetResourceUtilizationAsync();
}

public interface ICapacityPlanningService
{
    Task<Dictionary<string, object>> PredictCapacityAsync(int daysAhead);
    Task<List<string>> GetCapacityWarningsAsync();
    Task<bool> TriggerCapacityExpansionAsync();
    Task<Dictionary<string, int>> GetCurrentCapacityAsync();
    Task<Dictionary<string, double>> GetGrowthTrendsAsync();
}

public class MachineLearningEngine : IMachineLearningService
{
    private readonly List<PredictionModel> _models = new();
    private readonly List<Prediction> _predictions = new();

    public async Task<PredictionModel> TrainModelAsync(string modelName, string modelType, List<Dictionary<string, double>> trainingData)
    {
        var model = new PredictionModel
        {
            ModelId = Guid.NewGuid().ToString(),
            ModelName = modelName,
            ModelType = modelType,
            TrainedAt = DateTime.UtcNow,
            Accuracy = 0.85 + (Random.Shared.NextDouble() * 0.14),
            SampleCount = trainingData.Count,
            IsActive = false
        };

        _models.Add(model);
        return await Task.FromResult(model);
    }

    public async Task<PredictionModel> GetModelAsync(string modelId)
    {
        var model = _models.FirstOrDefault(m => m.ModelId == modelId);
        return await Task.FromResult(model);
    }

    public async Task<List<PredictionModel>> ListModelsAsync()
    {
        return await Task.FromResult(new List<PredictionModel>(_models));
    }

    public async Task<bool> DeleteModelAsync(string modelId)
    {
        var model = _models.FirstOrDefault(m => m.ModelId == modelId);
        if (model == null)
            return await Task.FromResult(false);

        _models.Remove(model);
        return await Task.FromResult(true);
    }

    public async Task<bool> ActivateModelAsync(string modelId)
    {
        var model = _models.FirstOrDefault(m => m.ModelId == modelId);
        if (model == null)
            return await Task.FromResult(false);

        foreach (var m in _models.Where(m => m.ModelType == model.ModelType))
            m.IsActive = false;

        model.IsActive = true;
        return await Task.FromResult(true);
    }

    public async Task<Prediction> MakePredictionAsync(string modelId, Dictionary<string, double> features)
    {
        var model = _models.FirstOrDefault(m => m.ModelId == modelId && m.IsActive);
        if (model == null)
            return await Task.FromResult<Prediction>(null);

        var prediction = new Prediction
        {
            PredictionId = Guid.NewGuid().ToString(),
            ModelId = modelId,
            PredictedValue = features.Values.Average(),
            Confidence = model.Accuracy,
            Features = new Dictionary<string, double>(features),
            CreatedAt = DateTime.UtcNow
        };

        _predictions.Add(prediction);
        return await Task.FromResult(prediction);
    }

    public async Task<List<Prediction>> GetPredictionHistoryAsync(string modelId, int limit = 100)
    {
        var history = _predictions
            .Where(p => p.ModelId == modelId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToList();

        return await Task.FromResult(history);
    }

    public async Task<double> GetModelAccuracyAsync(string modelId)
    {
        var model = _models.FirstOrDefault(m => m.ModelId == modelId);
        if (model == null)
            return await Task.FromResult(0.0);

        return await Task.FromResult(model.Accuracy);
    }
}

public class ResourcePlanner : IPredictiveResourcePlanning
{
    private readonly List<WorkloadPattern> _patterns = new();
    private readonly List<ResourceAllocation> _allocations = new();

    public async Task<WorkloadPattern> AnalyzeWorkloadAsync(string workloadName, List<double> loadHistory)
    {
        var pattern = new WorkloadPattern
        {
            PatternId = Guid.NewGuid().ToString(),
            WorkloadName = workloadName,
            AverageLoad = loadHistory.Average(),
            PeakLoad = loadHistory.Max(),
            PeakTime = DateTime.UtcNow.AddHours(-loadHistory.IndexOf(loadHistory.Max())),
            HistoricalDays = 30,
            LoadHistory = new List<double>(loadHistory)
        };

        _patterns.Add(pattern);
        return await Task.FromResult(pattern);
    }

    public async Task<ResourceAllocation> PredictResourceNeedsAsync(string workloadId)
    {
        var pattern = _patterns.FirstOrDefault(p => p.PatternId == workloadId);
        if (pattern == null)
            return await Task.FromResult<ResourceAllocation>(null);

        var allocation = new ResourceAllocation
        {
            AllocationId = Guid.NewGuid().ToString(),
            WorkloadId = workloadId,
            RecommendedCPU = (int)(pattern.PeakLoad * 4),
            RecommendedMemoryMB = (int)(pattern.PeakLoad * 256),
            RecommendedDiskMB = (int)(pattern.PeakLoad * 1024),
            CalculatedAt = DateTime.UtcNow,
            IsApplied = false
        };

        _allocations.Add(allocation);
        return await Task.FromResult(allocation);
    }

    public async Task<List<WorkloadPattern>> GetWorkloadPatternsAsync()
    {
        return await Task.FromResult(new List<WorkloadPattern>(_patterns));
    }

    public async Task<bool> ApplyAllocationAsync(string allocationId)
    {
        var allocation = _allocations.FirstOrDefault(a => a.AllocationId == allocationId);
        if (allocation == null)
            return await Task.FromResult(false);

        allocation.IsApplied = true;
        return await Task.FromResult(true);
    }

    public async Task<List<ResourceAllocation>> GetAllocationHistoryAsync(int limit = 100)
    {
        var history = _allocations.OrderByDescending(a => a.CalculatedAt).Take(limit).ToList();
        return await Task.FromResult(history);
    }

    public async Task<Dictionary<string, int>> GetResourceUtilizationAsync()
    {
        var utilization = new Dictionary<string, int>
        {
            ["CPU"] = Random.Shared.Next(20, 80),
            ["Memory"] = Random.Shared.Next(30, 75),
            ["Disk"] = Random.Shared.Next(40, 90)
        };

        return await Task.FromResult(utilization);
    }
}

public class CapacityPlanner : ICapacityPlanningService
{
    private readonly Dictionary<string, int> _capacities = new()
    {
        ["CPU"] = 100,
        ["Memory"] = 512,
        ["Disk"] = 2048
    };

    public async Task<Dictionary<string, object>> PredictCapacityAsync(int daysAhead)
    {
        var predictions = new Dictionary<string, object>();
        
        foreach (var capacity in _capacities)
        {
            var growthRate = 1.05;
            var predicted = (int)(capacity.Value * Math.Pow(growthRate, daysAhead));
            predictions[capacity.Key] = predicted;
        }

        return await Task.FromResult(predictions);
    }

    public async Task<List<string>> GetCapacityWarningsAsync()
    {
        var warnings = new List<string>();
        
        if (_capacities["CPU"] > 80)
            warnings.Add("CPU capacity at 80%+");
        if (_capacities["Memory"] > 70)
            warnings.Add("Memory capacity at 70%+");
        if (_capacities["Disk"] > 85)
            warnings.Add("Disk capacity at 85%+");

        return await Task.FromResult(warnings);
    }

    public async Task<bool> TriggerCapacityExpansionAsync()
    {
        foreach (var key in _capacities.Keys.ToList())
        {
            _capacities[key] = (int)(_capacities[key] * 1.2);
        }

        return await Task.FromResult(true);
    }

    public async Task<Dictionary<string, int>> GetCurrentCapacityAsync()
    {
        return await Task.FromResult(new Dictionary<string, int>(_capacities));
    }

    public async Task<Dictionary<string, double>> GetGrowthTrendsAsync()
    {
        var trends = new Dictionary<string, double>
        {
            ["CPU_Growth"] = 0.08,
            ["Memory_Growth"] = 0.06,
            ["Disk_Growth"] = 0.12
        };

        return await Task.FromResult(trends);
    }
}

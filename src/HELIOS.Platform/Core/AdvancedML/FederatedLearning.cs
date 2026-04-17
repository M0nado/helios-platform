using HELIOS.Platform.Core.AdvancedML.Interfaces;
using HELIOS.Platform.Core.Performance;
using System.Collections.Concurrent;

namespace HELIOS.Platform.Core.AdvancedML;

/// <summary>
/// Federated learning coordinator for distributed ML model training.
/// </summary>
public class FederatedLearning : IFederatedLearning
{
    private readonly Logging.ILogger _logger;
    private readonly IL1CacheService _cache;
    private readonly ConcurrentDictionary<string, FederatedModelUpdate> _nodeUpdates = new();
    private FederatedModelUpdate _globalModel = new() { IsGlobalModel = true, Epoch = 0, AggregationRound = 0 };
    private int _aggregationRound = 0;
    private long _totalSamplesTrained = 0;
    private DateTime _federationStartTime = DateTime.UtcNow;

    public FederatedLearning(Logging.ILogger logger, IL1CacheService cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        InitializeGlobalModel();
    }

    public async Task<FederatedModelUpdate> TrainLocalAsync(string nodeId, double[] localData, int epochs)
    {
        ArgumentNullException.ThrowIfNull(nodeId);
        ArgumentNullException.ThrowIfNull(localData);
        if (localData.Length == 0) throw new ArgumentException("Local data cannot be empty");

        try
        {
            // Simulate local training
            var localWeights = new double[10];
            for (int i = 0; i < localWeights.Length; i++)
                localWeights[i] = Random.Shared.NextDouble() - 0.5;

            var localUpdate = new FederatedModelUpdate
            {
                NodeId = nodeId,
                Weights = localWeights,
                Biases = new double[5],
                LocalSampleCount = localData.Length,
                LocalLoss = 0.1 + Random.Shared.NextDouble() * 0.2,
                LocalAccuracy = 0.7 + Random.Shared.NextDouble() * 0.25,
                Epoch = epochs,
                UpdatedAt = DateTime.UtcNow,
                AggregationRound = _aggregationRound
            };

            _nodeUpdates[nodeId] = localUpdate;
            _totalSamplesTrained += localData.Length;

            _logger.Info($"Local training completed on node {nodeId}: samples={localData.Length}, accuracy={localUpdate.LocalAccuracy:P2}");
            await Task.CompletedTask;
            return localUpdate;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in FederatedLearning.TrainLocalAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<FederatedModelUpdate> AggregateUpdatesAsync(List<FederatedModelUpdate> updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        if (updates.Count == 0) throw new ArgumentException("Updates list cannot be empty");

        try
        {
            _aggregationRound++;
            var aggregated = await FederatedAveragingAsync(updates);
            aggregated.AggregationRound = _aggregationRound;
            aggregated.IsGlobalModel = true;
            _globalModel = aggregated;

            _logger.Info($"Aggregation round {_aggregationRound} completed: {updates.Count} nodes, global_accuracy={aggregated.LocalAccuracy:P2}");
            await Task.CompletedTask;
            return aggregated;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in AggregateUpdatesAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<FederatedModelUpdate> FederatedAveragingAsync(List<FederatedModelUpdate> nodeUpdates)
    {
        ArgumentNullException.ThrowIfNull(nodeUpdates);
        if (nodeUpdates.Count == 0) throw new ArgumentException("Node updates cannot be empty");

        await Task.CompletedTask;

        // Weighted average based on sample counts
        var totalSamples = nodeUpdates.Sum(u => u.LocalSampleCount);
        var weightedAccuracy = 0.0;
        var weightedLoss = 0.0;

        // Average weights
        var avgWeights = new double[nodeUpdates[0].Weights.Length];
        var avgBiases = new double[Math.Min(5, nodeUpdates[0].Biases.Length)];

        foreach (var update in nodeUpdates)
        {
            var weight = (double)update.LocalSampleCount / totalSamples;
            weightedAccuracy += update.LocalAccuracy * weight;
            weightedLoss += update.LocalLoss * weight;

            for (int i = 0; i < Math.Min(avgWeights.Length, update.Weights.Length); i++)
                avgWeights[i] += update.Weights[i] * weight;

            for (int i = 0; i < Math.Min(avgBiases.Length, update.Biases.Length); i++)
                avgBiases[i] += update.Biases[i] * weight;
        }

        return new FederatedModelUpdate
        {
            UpdateId = Guid.NewGuid().ToString(),
            Weights = avgWeights,
            Biases = avgBiases,
            LocalSampleCount = totalSamples,
            LocalAccuracy = weightedAccuracy,
            LocalLoss = weightedLoss,
            UpdatedAt = DateTime.UtcNow,
            IsGlobalModel = true,
            AggregationRound = _aggregationRound
        };
    }

    public async Task<FederatedModelUpdate> GetGlobalModelAsync()
    {
        await Task.CompletedTask;
        return _globalModel;
    }

    public async Task<FederatedLearningStats> GetStatsAsync()
    {
        var stats = new FederatedLearningStats
        {
            NodeCount = _nodeUpdates.Count,
            CurrentRound = _aggregationRound,
            GlobalAccuracy = _globalModel.LocalAccuracy,
            GlobalLoss = _globalModel.LocalLoss,
            AverageLocalAccuracy = _nodeUpdates.Values.Any() 
                ? _nodeUpdates.Values.Average(u => u.LocalAccuracy) 
                : 0,
            TotalSamplesTrained = _totalSamplesTrained,
            ElapsedTime = DateTime.UtcNow - _federationStartTime,
            PendingUpdates = _nodeUpdates.Count,
            PrivacyMechanism = "Federated Averaging"
        };

        await Task.CompletedTask;
        return stats;
    }

    private void InitializeGlobalModel()
    {
        _globalModel.Weights = new double[10];
        _globalModel.Biases = new double[5];
        for (int i = 0; i < _globalModel.Weights.Length; i++)
            _globalModel.Weights[i] = 0;
        for (int i = 0; i < _globalModel.Biases.Length; i++)
            _globalModel.Biases[i] = 0;
    }
}

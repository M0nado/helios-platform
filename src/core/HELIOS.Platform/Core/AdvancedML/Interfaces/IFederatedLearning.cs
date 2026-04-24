namespace HELIOS.Platform.Core.AdvancedML.Interfaces;

/// <summary>
/// Federated learning for distributed ML model training across multiple nodes.
/// Enables privacy-preserving collaborative learning.
/// </summary>
public interface IFederatedLearning
{
    /// <summary>
    /// Trains model locally on node data without sharing raw data.
    /// </summary>
    /// <param name="nodeId">Unique identifier for this node.</param>
    /// <param name="localData">Local training data.</param>
    /// <param name="epochs">Number of training epochs.</param>
    /// <returns>Local model update.</returns>
    Task<FederatedModelUpdate> TrainLocalAsync(string nodeId, double[] localData, int epochs);

    /// <summary>
    /// Aggregates model updates from multiple nodes.
    /// </summary>
    /// <param name="updates">Model updates from all nodes.</param>
    /// <returns>Aggregated global model.</returns>
    Task<FederatedModelUpdate> AggregateUpdatesAsync(List<FederatedModelUpdate> updates);

    /// <summary>
    /// Performs federated averaging (FedAvg) algorithm.
    /// </summary>
    Task<FederatedModelUpdate> FederatedAveragingAsync(List<FederatedModelUpdate> nodeUpdates);

    /// <summary>
    /// Gets current global model state.
    /// </summary>
    Task<FederatedModelUpdate> GetGlobalModelAsync();

    /// <summary>
    /// Gets federated learning statistics.
    /// </summary>
    Task<FederatedLearningStats> GetStatsAsync();
}

/// <summary>
/// Federated model update.
/// </summary>
public class FederatedModelUpdate
{
    /// <summary>Unique identifier of this update.</summary>
    public string UpdateId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Node that generated this update.</summary>
    public string? NodeId { get; set; }

    /// <summary>Model weights/parameters.</summary>
    public double[] Weights { get; set; } = Array.Empty<double>();

    /// <summary>Bias terms if applicable.</summary>
    public double[] Biases { get; set; } = Array.Empty<double>();

    /// <summary>Number of local samples used for training.</summary>
    public int LocalSampleCount { get; set; }

    /// <summary>Loss value on local data.</summary>
    public double LocalLoss { get; set; }

    /// <summary>Accuracy on local data.</summary>
    public double LocalAccuracy { get; set; }

    /// <summary>Training epoch when update was generated.</summary>
    public int Epoch { get; set; }

    /// <summary>Timestamp of update.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Aggregation round number.</summary>
    public int AggregationRound { get; set; }

    /// <summary>Is this a global aggregated model.</summary>
    public bool IsGlobalModel { get; set; }
}

/// <summary>
/// Federated learning statistics.
/// </summary>
public class FederatedLearningStats
{
    /// <summary>Number of participating nodes.</summary>
    public int NodeCount { get; set; }

    /// <summary>Current aggregation round.</summary>
    public int CurrentRound { get; set; }

    /// <summary>Global model accuracy.</summary>
    public double GlobalAccuracy { get; set; }

    /// <summary>Global model loss.</summary>
    public double GlobalLoss { get; set; }

    /// <summary>Average local accuracy across nodes.</summary>
    public double AverageLocalAccuracy { get; set; }

    /// <summary>Total samples trained across all nodes.</summary>
    public long TotalSamplesTrained { get; set; }

    /// <summary>Time since federation started.</summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>Updates from nodes waiting to be aggregated.</summary>
    public int PendingUpdates { get; set; }

    /// <summary>Data privacy mechanism used (differential privacy, secure aggregation, etc.).</summary>
    public string PrivacyMechanism { get; set; } = "None";
}

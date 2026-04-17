namespace HELIOS.Platform.Core.ML.Interfaces;

/// <summary>
/// Manages ML model lifecycle: training, versioning, deployment, and inference.
/// </summary>
public interface IMLModelManager
{
    /// <summary>
    /// Trains a new model on historical data.
    /// </summary>
    Task<ModelMetadata> TrainModelAsync(string modelName, IList<FeatureVector> trainingData, 
        ModelTrainingConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a model by name and version.
    /// </summary>
    Task<MLModel> GetModelAsync(string modelName, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates model performance on test data.
    /// </summary>
    Task<ModelPerformance> EvaluateModelAsync(string modelName, IList<FeatureVector> testData, 
        string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deploys a model version to production.
    /// </summary>
    Task DeployModelAsync(string modelName, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all models and versions.
    /// </summary>
    Task<IList<ModelMetadata>> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back to a previous model version.
    /// </summary>
    Task RollbackModelAsync(string modelName, string previousVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs inference with deployed model.
    /// </summary>
    Task<double> PredictAsync(string modelName, FeatureVector features, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an ML model.
/// </summary>
public class MLModel
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public ModelType Type { get; set; }
    public byte[] ModelData { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; }
    public DateTime? DeployedAt { get; set; }
    public bool IsActive { get; set; }
    public IDictionary<string, double> Hyperparameters { get; set; } = new Dictionary<string, double>();
}

/// <summary>
/// Metadata about a model.
/// </summary>
public class ModelMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public ModelType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeployedAt { get; set; }
    public bool IsActive { get; set; }
    public ModelPerformance? LastPerformance { get; set; }
    public int TrainingSampleCount { get; set; }
    public TimeSpan TrainingDuration { get; set; }
}

/// <summary>
/// Performance metrics for a model.
/// </summary>
public class ModelPerformance
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double RootMeanSquaredError { get; set; }
    public double MeanAbsoluteError { get; set; }
    public IDictionary<string, double> ConfusionMatrix { get; set; } = new Dictionary<string, double>();
}

/// <summary>
/// Configuration for model training.
/// </summary>
public class ModelTrainingConfig
{
    public int MaxIterations { get; set; } = 1000;
    public double LearningRate { get; set; } = 0.01;
    public int BatchSize { get; set; } = 32;
    public double ValidationSplit { get; set; } = 0.2;
    public int RandomSeed { get; set; } = 42;
    public IDictionary<string, double> HyperParameters { get; set; } = new Dictionary<string, double>();
}

/// <summary>
/// Types of ML models supported.
/// </summary>
public enum ModelType
{
    LinearRegression,
    DecisionTree,
    RandomForest,
    NeuralNetwork,
    GradientBoosting,
    SupportVectorMachine,
    KMeans,
    IsolationForest
}

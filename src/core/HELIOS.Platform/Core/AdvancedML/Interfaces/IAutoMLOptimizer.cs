namespace HELIOS.Platform.Core.AdvancedML.Interfaces;

/// <summary>
/// Automated ML model selection and hyperparameter optimization.
/// Automatically selects best model and optimizes hyperparameters.
/// </summary>
public interface IAutoMLOptimizer
{
    /// <summary>
    /// Automatically selects best model from candidates.
    /// </summary>
    /// <param name="data">Training data points.</param>
    /// <param name="targetValues">Target values for regression.</param>
    /// <param name="timeoutMs">Maximum time to spend on model selection.</param>
    /// <returns>Selected model with optimized hyperparameters.</returns>
    Task<ModelSelection> SelectBestModelAsync(double[] data, double[] targetValues, int timeoutMs = 5000);

    /// <summary>
    /// Optimizes hyperparameters for a specific model type.
    /// </summary>
    Task<HyperparameterTuning> OptimizeHyperparametersAsync(string modelType, double[] data, double[] targetValues);

    /// <summary>
    /// Evaluates multiple models and returns rankings.
    /// </summary>
    Task<List<ModelEvaluation>> EvaluateModelsAsync(double[] data, double[] targetValues, string[] modelTypes);

    /// <summary>
    /// Gets currently selected model configuration.
    /// </summary>
    Task<ModelSelection> GetCurrentSelectionAsync();
}

/// <summary>
/// Model selection result.
/// </summary>
public class ModelSelection
{
    /// <summary>Selected model type (linear, exponential, polynomial, etc.).</summary>
    public string ModelType { get; set; } = string.Empty;

    /// <summary>Model accuracy score.</summary>
    public double AccuracyScore { get; set; }

    /// <summary>Optimized hyperparameters.</summary>
    public Dictionary<string, double> Hyperparameters { get; set; } = new();

    /// <summary>Cross-validation score.</summary>
    public double CrossValidationScore { get; set; }

    /// <summary>Training data size used for selection.</summary>
    public int TrainingDataSize { get; set; }

    /// <summary>Time spent on selection (ms).</summary>
    public long SelectionTimeMs { get; set; }

    /// <summary>Timestamp of selection.</summary>
    public DateTime SelectedAt { get; set; }
}

/// <summary>
/// Hyperparameter tuning result.
/// </summary>
public class HyperparameterTuning
{
    /// <summary>Best hyperparameters found.</summary>
    public Dictionary<string, double> BestParameters { get; set; } = new();

    /// <summary>Best score achieved during tuning.</summary>
    public double BestScore { get; set; }

    /// <summary>Number of combinations evaluated.</summary>
    public int CombinationsEvaluated { get; set; }

    /// <summary>Tuning method used (grid, random, bayesian).</summary>
    public string TuningMethod { get; set; } = string.Empty;

    /// <summary>Time spent tuning (ms).</summary>
    public long TuningTimeMs { get; set; }
}

/// <summary>
/// Model evaluation metrics.
/// </summary>
public class ModelEvaluation
{
    /// <summary>Model type identifier.</summary>
    public string ModelType { get; set; } = string.Empty;

    /// <summary>Overall accuracy score.</summary>
    public double AccuracyScore { get; set; }

    /// <summary>Model ranking (1=best).</summary>
    public int Rank { get; set; }

    /// <summary>Training time (ms).</summary>
    public long TrainingTimeMs { get; set; }

    /// <summary>Prediction time (ms).</summary>
    public long PredictionTimeMs { get; set; }

    /// <summary>Model complexity score (lower=simpler).</summary>
    public double ComplexityScore { get; set; }

    /// <summary>Recommendation reason.</summary>
    public string RecommendationReason { get; set; } = string.Empty;
}

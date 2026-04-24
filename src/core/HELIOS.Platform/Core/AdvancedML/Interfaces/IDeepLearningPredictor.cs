namespace HELIOS.Platform.Core.AdvancedML.Interfaces;

/// <summary>
/// Neural network-based forecasting for advanced time-series prediction.
/// Provides deep learning predictions with confidence scores.
/// </summary>
public interface IDeepLearningPredictor
{
    /// <summary>
    /// Predicts future values using neural network models.
    /// </summary>
    /// <param name="inputData">Historical data points (normalized 0-1).</param>
    /// <param name="sequenceLength">Length of input sequences for LSTM.</param>
    /// <param name="forecastSteps">Number of steps to predict ahead.</param>
    /// <returns>Predicted values with confidence scores.</returns>
    Task<NeuralNetworkPrediction> PredictAsync(double[] inputData, int sequenceLength, int forecastSteps);

    /// <summary>
    /// Trains a neural network model on historical data.
    /// </summary>
    Task TrainAsync(double[] trainingData, int sequenceLength, int epochs);

    /// <summary>
    /// Gets model performance metrics.
    /// </summary>
    Task<ModelMetrics> GetMetricsAsync();

    /// <summary>
    /// Resets model to initial state.
    /// </summary>
    Task ResetAsync();
}

/// <summary>
/// Neural network prediction result with confidence.
/// </summary>
public class NeuralNetworkPrediction
{
    /// <summary>Predicted values.</summary>
    public double[] Values { get; set; } = Array.Empty<double>();

    /// <summary>Confidence scores (0-1) for each prediction.</summary>
    public double[] ConfidenceScores { get; set; } = Array.Empty<double>();

    /// <summary>Upper bound of prediction interval.</summary>
    public double[] UpperBound { get; set; } = Array.Empty<double>();

    /// <summary>Lower bound of prediction interval.</summary>
    public double[] LowerBound { get; set; } = Array.Empty<double>();

    /// <summary>Model accuracy on validation set.</summary>
    public double ModelAccuracy { get; set; }

    /// <summary>Prediction timestamp.</summary>
    public DateTime PredictedAt { get; set; }

    /// <summary>Milliseconds taken for prediction.</summary>
    public long PredictionTimeMs { get; set; }
}

/// <summary>
/// Model performance metrics.
/// </summary>
public class ModelMetrics
{
    /// <summary>Mean Absolute Error on test set.</summary>
    public double MAE { get; set; }

    /// <summary>Root Mean Squared Error on test set.</summary>
    public double RMSE { get; set; }

    /// <summary>Mean Absolute Percentage Error.</summary>
    public double MAPE { get; set; }

    /// <summary>R-squared score (0-1).</summary>
    public double RSquared { get; set; }

    /// <summary>Training epochs completed.</summary>
    public int EpochsTrained { get; set; }

    /// <summary>Model was successfully trained.</summary>
    public bool IsModelTrained { get; set; }

    /// <summary>Timestamp of last training.</summary>
    public DateTime LastTrainedAt { get; set; }
}

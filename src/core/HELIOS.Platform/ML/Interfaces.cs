namespace HELIOS.Platform.ML;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Represents an AI/ML provider (local, cloud, or hybrid).
/// </summary>
public class MLProvider
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // local, cloud, hybrid
    public bool IsAvailable { get; set; }
    public Dictionary<string, object> Config { get; set; } = new();
    public DateTime LastCheckedAt { get; set; }
}

/// <summary>
/// Prediction result from ML model.
/// </summary>
public class PredictionResult
{
    public string ModelId { get; set; }
    public object PredictedValue { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, double> FeatureImportance { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public TimeSpan InferenceTime { get; set; }
}

/// <summary>
/// Training data point for ML models.
/// </summary>
public class TrainingDataPoint
{
    public string Id { get; set; }
    public Dictionary<string, object> Features { get; set; } = new();
    public object Label { get; set; }
    public DateTime Timestamp { get; set; }
    public double Weight { get; set; } = 1.0;
}

/// <summary>
/// Model metadata and status.
/// </summary>
public class MLModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // classification, regression, clustering, anomaly_detection
    public string Status { get; set; } // idle, training, predicting, error
    public double Accuracy { get; set; }
    public int TrainingSampleCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastTrainedAt { get; set; }
    public DateTime LastPredictionAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Anomaly detection result.
/// </summary>
public class AnomalyResult
{
    public string DataPointId { get; set; }
    public bool IsAnomaly { get; set; }
    public double AnomalyScore { get; set; }
    public string Reason { get; set; }
    public DateTime DetectedAt { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// ML service interface for training, prediction, and anomaly detection.
/// </summary>
public interface IMLService
{
    // Model management
    Task<MLModel> CreateModelAsync(string modelId, string modelType);
    Task<MLModel> GetModelAsync(string modelId);
    Task<List<MLModel>> ListModelsAsync();
    Task DeleteModelAsync(string modelId);
    
    // Training
    Task TrainModelAsync(string modelId, List<TrainingDataPoint> trainingData);
    Task<MLModel> GetTrainingStatusAsync(string modelId);
    
    // Prediction
    Task<PredictionResult> PredictAsync(string modelId, Dictionary<string, object> features);
    Task<List<PredictionResult>> BatchPredictAsync(string modelId, List<Dictionary<string, object>> features);
    
    // Anomaly detection
    Task<AnomalyResult> DetectAnomalyAsync(string modelId, Dictionary<string, object> dataPoint);
    Task<List<AnomalyResult>> DetectAnomaliesAsync(string modelId, List<Dictionary<string, object>> dataPoints);
    
    // Model evaluation
    Task<double> EvaluateModelAsync(string modelId, List<TrainingDataPoint> testData);
    Task<Dictionary<string, double>> GetModelMetricsAsync(string modelId);
    
    // Provider management
    Task<List<MLProvider>> GetAvailableProvidersAsync();
    Task SetActiveProviderAsync(string providerId);
}

/// <summary>
/// Data pipeline for feature engineering and preprocessing.
/// </summary>
public interface IDataPipeline
{
    Task<List<TrainingDataPoint>> PreprocessAsync(List<TrainingDataPoint> rawData);
    Task<Dictionary<string, object>> NormalizeAsync(Dictionary<string, object> features);
    Task<List<(string Feature, double Importance)>> FeatureSelectAsync(List<TrainingDataPoint> data, int topN);
    Task<Dictionary<string, object>> HandleMissingValuesAsync(Dictionary<string, object> features);
}

/// <summary>
/// Prediction engine interface.
/// </summary>
public interface IPredictionEngine
{
    Task<PredictionResult> PredictAsync(string modelId, Dictionary<string, object> features);
    Task<List<PredictionResult>> BatchPredictAsync(string modelId, List<Dictionary<string, object>> features);
    Task<Dictionary<string, double>> GetFeatureImportanceAsync(string modelId);
}

/// <summary>
/// Anomaly detection engine.
/// </summary>
public interface IAnomalyDetectionEngine
{
    Task<AnomalyResult> DetectAsync(string modelId, Dictionary<string, object> dataPoint);
    Task<List<AnomalyResult>> DetectBatchAsync(string modelId, List<Dictionary<string, object>> dataPoints);
    Task UpdateThresholdAsync(string modelId, double threshold);
    Task<double> GetThresholdAsync(string modelId);
}

/// <summary>
/// ML exception.
/// </summary>
public class MLException : Exception
{
    public string ModelId { get; set; }
    public MLException(string message) : base(message) { }
    public MLException(string message, Exception innerException) : base(message, innerException) { }
    public MLException(string modelId, string message) : base(message) => ModelId = modelId;
}

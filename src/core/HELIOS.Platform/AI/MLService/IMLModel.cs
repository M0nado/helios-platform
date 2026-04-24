using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.AI.MLService
{
    /// <summary>
    /// Interface for machine learning models with standardized abstraction.
    /// </summary>
    public interface IMLModel
    {
        string ModelId { get; }
        string ModelName { get; }
        string Version { get; }
        ModelType ModelType { get; }
        DateTime CreatedAt { get; }
        DateTime LastUpdatedAt { get; }

        bool IsLoaded { get; }
        double Accuracy { get; }
        long ModelSizeBytes { get; }

        Task<bool> LoadAsync();
        Task UnloadAsync();
        Task<PredictionResult> PredictAsync(float[] features);
        Task<List<PredictionResult>> BatchPredictAsync(List<float[]> features);
        Task<ModelMetrics> GetMetricsAsync();
        bool SupportsGPU { get; }
    }

    /// <summary>
    /// Types of ML models supported.
    /// </summary>
    public enum ModelType
    {
        Unknown = 0,
        ResourceForecasting = 1,
        AnomalyDetection = 2,
        PerformancePrediction = 3,
        LoadBalancing = 4,
        SecurityThreatDetection = 5,
        CustomRegression = 6
    }

    /// <summary>
    /// Result of a single prediction.
    /// </summary>
    public class PredictionResult
    {
        public string ModelId { get; set; } = string.Empty;
        public float[] Prediction { get; set; } = Array.Empty<float>();
        public float Confidence { get; set; }
        public Dictionary<string, float> FeatureImportance { get; set; } = new();
        public DateTime PredictedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Success";
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Metrics for model performance.
    /// </summary>
    public class ModelMetrics
    {
        public string ModelId { get; set; } = string.Empty;
        public double Accuracy { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1Score { get; set; }
        public int TotalPredictions { get; set; }
        public int CorrectPredictions { get; set; }
        public double AverageLatencyMs { get; set; }
        public double MinLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}

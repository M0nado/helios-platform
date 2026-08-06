using HELIOS.Platform.AI.MLService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.AI.Models
{
    /// <summary>
    /// Mock ML model for testing and demonstration purposes.
    /// Implements realistic behavior without requiring ONNX Runtime in this first version.
    /// </summary>
    public class MockResourceForecastingModel : IMLModel
    {
        public string ModelId { get; } = "mock-resource-forecast-v1";
        public string ModelName { get; } = "Mock Resource Forecasting";
        public string Version { get; } = "1.0.0";
        public HELIOS.Platform.AI.MLService.ModelType ModelType { get; } = HELIOS.Platform.AI.MLService.ModelType.ResourceForecasting;
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; private set; } = DateTime.UtcNow;
        public bool IsLoaded { get; private set; }
        public double Accuracy { get; } = 0.92;
        public long ModelSizeBytes { get; } = 1024 * 1024; // 1MB
        public bool SupportsGPU { get; } = false;

        private List<(DateTime, float[])> _predictionHistory = new();

        public async Task<bool> LoadAsync()
        {
            return await Task.Run(() =>
            {
                IsLoaded = true;
                LastUpdatedAt = DateTime.UtcNow;
                return true;
            });
        }

        public async Task UnloadAsync()
        {
            await Task.Run(() =>
            {
                IsLoaded = false;
                _predictionHistory.Clear();
            });
        }

        public async Task<PredictionResult> PredictAsync(float[] features)
        {
            return await Task.Run(() =>
            {
                if (!IsLoaded) return new PredictionResult { Status = "Model not loaded" };

                var prediction = SimulatePrediction(features);
                _predictionHistory.Add((DateTime.UtcNow, features));

                if (_predictionHistory.Count > 1000)
                {
                    _predictionHistory.RemoveAt(0);
                }

                return prediction;
            });
        }

        public async Task<List<PredictionResult>> BatchPredictAsync(List<float[]> features)
        {
            return await Task.Run(() =>
            {
                if (!IsLoaded) return new List<PredictionResult>();

                var results = new List<PredictionResult>();
                foreach (var featureSet in features)
                {
                    results.Add(SimulatePrediction(featureSet));
                    _predictionHistory.Add((DateTime.UtcNow, featureSet));
                }

                if (_predictionHistory.Count > 1000)
                {
                    var removeCount = _predictionHistory.Count - 1000;
                    _predictionHistory.RemoveRange(0, removeCount);
                }

                return results;
            });
        }

        public async Task<ModelMetrics> GetMetricsAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new ModelMetrics
                {
                    ModelId = ModelId,
                    Accuracy = 0.92,
                    Precision = 0.89,
                    Recall = 0.91,
                    F1Score = 0.90,
                    TotalPredictions = _predictionHistory.Count,
                    CorrectPredictions = (int)(_predictionHistory.Count * 0.92),
                    AverageLatencyMs = 15,
                    MinLatencyMs = 5,
                    MaxLatencyMs = 45,
                    CalculatedAt = DateTime.UtcNow
                };

                return metrics;
            });
        }

        private PredictionResult SimulatePrediction(float[] features)
        {
            if (features == null || features.Length == 0)
                return new PredictionResult { Status = "Invalid features" };

            // Simulate resource usage prediction: predict next CPU, Memory, Disk values
            var predictions = new float[3];

            // CPU prediction: slightly increase or decrease based on current value
            predictions[0] = Math.Clamp(features[0] + (float)(new Random().NextDouble() - 0.5f) * 5, 0, 100);

            // Memory prediction: more stable than CPU
            predictions[1] = Math.Clamp(features[1] + (float)(new Random().NextDouble() - 0.5f) * 2, 0, 100);

            // Disk prediction: very stable
            predictions[2] = Math.Clamp(features[2] + (float)(new Random().NextDouble() - 0.1f), 0, 100);

            var confidence = (float)(0.85 + new Random().NextDouble() * 0.1);

            return new PredictionResult
            {
                ModelId = ModelId,
                Prediction = predictions,
                Confidence = confidence,
                PredictedAt = DateTime.UtcNow,
                Status = "Success",
                ExecutionTimeMs = new Random().Next(5, 25),
                FeatureImportance = new Dictionary<string, float>
                {
                    { "cpu_usage", 0.40f },
                    { "memory_usage", 0.35f },
                    { "disk_usage", 0.15f },
                    { "trend", 0.10f }
                }
            };
        }
    }

    /// <summary>
    /// Mock anomaly detection model.
    /// </summary>
    public class MockAnomalyDetectionModel : IMLModel
    {
        public string ModelId { get; } = "mock-anomaly-detect-v1";
        public string ModelName { get; } = "Mock Anomaly Detection";
        public string Version { get; } = "1.0.0";
        public HELIOS.Platform.AI.MLService.ModelType ModelType { get; } = HELIOS.Platform.AI.MLService.ModelType.AnomalyDetection;
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; private set; } = DateTime.UtcNow;
        public bool IsLoaded { get; private set; }
        public double Accuracy { get; } = 0.88;
        public long ModelSizeBytes { get; } = 512 * 1024; // 512KB
        public bool SupportsGPU { get; } = false;

        public async Task<bool> LoadAsync()
        {
            return await Task.Run(() =>
            {
                IsLoaded = true;
                LastUpdatedAt = DateTime.UtcNow;
                return true;
            });
        }

        public async Task UnloadAsync()
        {
            await Task.Run(() =>
            {
                IsLoaded = false;
            });
        }

        public async Task<PredictionResult> PredictAsync(float[] features)
        {
            return await Task.Run(() =>
            {
                if (!IsLoaded) return new PredictionResult { Status = "Model not loaded" };

                // Simulate anomaly score (0 = normal, 1 = anomaly)
                var anomalyScore = CalculateAnomalyScore(features);

                return new PredictionResult
                {
                    ModelId = ModelId,
                    Prediction = new[] { anomalyScore },
                    Confidence = 0.87f,
                    PredictedAt = DateTime.UtcNow,
                    Status = anomalyScore > 0.7f ? "Anomaly Detected" : "Normal",
                    ExecutionTimeMs = new Random().Next(3, 15),
                    FeatureImportance = new Dictionary<string, float>
                    {
                        { "deviation_from_mean", 0.45f },
                        { "seasonal_pattern", 0.30f },
                        { "trend_break", 0.25f }
                    }
                };
            });
        }

        public async Task<List<PredictionResult>> BatchPredictAsync(List<float[]> features)
        {
            return await Task.Run(() =>
            {
                if (!IsLoaded) return new List<PredictionResult>();

                var results = new List<PredictionResult>();
                foreach (var featureSet in features)
                {
                    results.Add((PredictAsync(featureSet).Result));
                }
                return results;
            });
        }

        public async Task<ModelMetrics> GetMetricsAsync()
        {
            return await Task.Run(() => new ModelMetrics
            {
                ModelId = ModelId,
                Accuracy = 0.88,
                Precision = 0.86,
                Recall = 0.89,
                F1Score = 0.87,
                TotalPredictions = 1000,
                CorrectPredictions = 880,
                AverageLatencyMs = 8,
                MinLatencyMs = 3,
                MaxLatencyMs = 20,
                CalculatedAt = DateTime.UtcNow
            });
        }

        private float CalculateAnomalyScore(float[] features)
        {
            if (features == null || features.Length == 0) return 0.5f;

            // Simple anomaly scoring: if values deviate significantly, mark as anomaly
            var score = 0f;
            foreach (var value in features)
            {
                if (value > 90) score += 0.2f;
                else if (value > 80) score += 0.1f;
            }

            // Add some randomness for realism
            score += (float)(new Random().NextDouble() * 0.1 - 0.05);

            return Math.Clamp(score, 0, 1);
        }
    }
}

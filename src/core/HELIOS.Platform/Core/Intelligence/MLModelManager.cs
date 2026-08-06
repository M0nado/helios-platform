using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.Intelligence.Interfaces;

namespace HELIOS.Platform.Core.Intelligence
{
    /// <summary>
    /// Manages ML model lifecycle including creation, training, evaluation, and retraining.
    /// </summary>
    public class MLModelManager : Interfaces.IMLModelManager, IDisposable
    {
        private readonly Dictionary<string, MLModel> _models = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ILogger<MLModelManager> _logger;
        private bool _disposed;

        private class MLModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastTrainedAt { get; set; }
            public double Accuracy { get; set; }
            public int TrainingCount { get; set; }
            public List<double> TrainingData { get; set; }
            public ModelMetrics Metrics { get; set; }
        }

        private class ModelMetrics
        {
            public double Accuracy { get; set; }
            public double Precision { get; set; }
            public double Recall { get; set; }
            public double F1Score { get; set; }
            public double Loss { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the MLModelManager class.
        /// </summary>
        public MLModelManager(ILogger<MLModelManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("MLModelManager initialized");
        }

        /// <summary>
        /// Creates and registers a new ML model.
        /// </summary>
        public async Task<string> CreateModelAsync(string modelName, string modelType, List<double> trainingData)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                throw new ArgumentException("Model name cannot be null or whitespace", nameof(modelName));
            if (string.IsNullOrWhiteSpace(modelType))
                throw new ArgumentException("Model type cannot be null or whitespace", nameof(modelType));
            if (trainingData == null || trainingData.Count == 0)
                throw new ArgumentException("Training data cannot be null or empty", nameof(trainingData));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var modelId = Guid.NewGuid().ToString("N");
                var model = new MLModel
                {
                    Id = modelId,
                    Name = modelName,
                    Type = modelType,
                    CreatedAt = DateTime.UtcNow,
                    LastTrainedAt = DateTime.UtcNow,
                    TrainingCount = 1,
                    TrainingData = new List<double>(trainingData),
                    Metrics = new ModelMetrics { Accuracy = 0 }
                };

                _models[modelId] = model;
                _logger.LogInformation($"Created model '{modelName}' (ID: {modelId}) of type '{modelType}'");

                return modelId;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Trains or retrains a model with new data.
        /// </summary>
        public async Task<double> TrainModelAsync(string modelId, List<double> trainingData)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model ID cannot be null or whitespace", nameof(modelId));
            if (trainingData == null || trainingData.Count == 0)
                throw new ArgumentException("Training data cannot be null or empty", nameof(trainingData));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_models.ContainsKey(modelId))
                    throw new KeyNotFoundException($"Model '{modelId}' not found");

                var stopwatch = Stopwatch.StartNew();
                var model = _models[modelId];
                model.TrainingData.AddRange(trainingData);
                model.LastTrainedAt = DateTime.UtcNow;
                model.TrainingCount++;

                // Simulate training and calculate metrics
                var accuracy = CalculateModelAccuracy(model.TrainingData);
                model.Accuracy = accuracy;
                model.Metrics.Accuracy = accuracy;
                model.Metrics.Precision = accuracy * 0.95;
                model.Metrics.Recall = accuracy * 0.93;
                model.Metrics.F1Score = 2 * (model.Metrics.Precision * model.Metrics.Recall) / (model.Metrics.Precision + model.Metrics.Recall);
                model.Metrics.Loss = 1.0 - accuracy;

                stopwatch.Stop();
                _logger.LogInformation($"Trained model '{modelId}' in {stopwatch.ElapsedMilliseconds}ms. Accuracy: {accuracy:P2}");

                return accuracy;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Evaluates model performance on test data.
        /// </summary>
        public async Task<Dictionary<string, double>> EvaluateModelAsync(string modelId, List<double> testData, List<double> expectedOutputs)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model ID cannot be null or whitespace", nameof(modelId));
            if (testData == null || testData.Count == 0)
                throw new ArgumentException("Test data cannot be null or empty", nameof(testData));
            if (expectedOutputs == null || expectedOutputs.Count != testData.Count)
                throw new ArgumentException("Expected outputs must match test data count", nameof(expectedOutputs));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_models.ContainsKey(modelId))
                    throw new KeyNotFoundException($"Model '{modelId}' not found");

                var stopwatch = Stopwatch.StartNew();
                var model = _models[modelId];

                // Simulate evaluation
                var predictions = testData.Select(d => d + Random.Shared.NextDouble() * 0.1).ToList();
                var accuracy = CalculateAccuracy(predictions, expectedOutputs);
                var precision = CalculatePrecision(predictions, expectedOutputs);
                var recall = CalculateRecall(predictions, expectedOutputs);
                var f1 = 2 * (precision * recall) / (precision + recall + 0.001);

                stopwatch.Stop();

                var results = new Dictionary<string, double>
                {
                    { "Accuracy", accuracy },
                    { "Precision", precision },
                    { "Recall", recall },
                    { "F1Score", f1 },
                    { "EvaluationTime_Ms", stopwatch.ElapsedMilliseconds }
                };

                _logger.LogInformation($"Evaluated model '{modelId}': Accuracy={accuracy:P2}, F1={f1:F3}");
                return results;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets model metadata and status.
        /// </summary>
        public async Task<Dictionary<string, object>> GetModelInfoAsync(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model ID cannot be null or whitespace", nameof(modelId));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_models.ContainsKey(modelId))
                    throw new KeyNotFoundException($"Model '{modelId}' not found");

                var model = _models[modelId];
                return new Dictionary<string, object>
                {
                    { "Id", model.Id },
                    { "Name", model.Name },
                    { "Type", model.Type },
                    { "CreatedAt", model.CreatedAt },
                    { "LastTrainedAt", model.LastTrainedAt },
                    { "TrainingCount", model.TrainingCount },
                    { "Accuracy", model.Accuracy },
                    { "Precision", model.Metrics.Precision },
                    { "Recall", model.Metrics.Recall },
                    { "F1Score", model.Metrics.F1Score },
                    { "Loss", model.Metrics.Loss },
                    { "TrainingDataSize", model.TrainingData.Count }
                };
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Removes a model from management.
        /// </summary>
        public async Task DeleteModelAsync(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model ID cannot be null or whitespace", nameof(modelId));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (_models.Remove(modelId))
                {
                    _logger.LogInformation($"Deleted model '{modelId}'");
                }
                else
                {
                    throw new KeyNotFoundException($"Model '{modelId}' not found");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets all managed models.
        /// </summary>
        public async Task<List<string>> GetAllModelsAsync()
        {
            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                return _models.Keys.ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Auto-retrains models that have degraded performance.
        /// </summary>
        public async Task<int> AutoRetrainDegradedModelsAsync()
        {
            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var retrainedCount = 0;
                var degradedModels = _models.Where(m => m.Value.Accuracy < 0.8).ToList();

                foreach (var model in degradedModels)
                {
                    try
                    {
                        // Generate additional training data for retraining
                        var additionalData = model.Value.TrainingData.Select(d => d + Random.Shared.NextDouble() * 0.05).ToList();
                        await TrainModelAsync(model.Key, additionalData);
                        retrainedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Error retraining model '{model.Key}': {ex.Message}");
                    }
                }

                _logger.LogInformation($"Auto-retrained {retrainedCount} degraded models");
                return retrainedCount;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static double CalculateModelAccuracy(List<double> trainingData)
        {
            if (trainingData.Count < 2)
                return 0;

            // Simulate accuracy based on data variance
            var mean = trainingData.Average();
            var variance = trainingData.Sum(d => Math.Pow(d - mean, 2)) / trainingData.Count;
            var accuracy = Math.Min(0.99, 0.5 + (1.0 / (1.0 + variance)));
            return accuracy;
        }

        private static double CalculateAccuracy(List<double> predictions, List<double> expected)
        {
            var mse = predictions.Zip(expected, (p, e) => Math.Pow(p - e, 2)).Average();
            return Math.Max(0, 1.0 - mse);
        }

        private static double CalculatePrecision(List<double> predictions, List<double> expected)
        {
            var threshold = 0.5;
            var truePositives = predictions.Zip(expected, (p, e) => p > threshold && e > threshold).Count(x => x);
            var falsePositives = predictions.Zip(expected, (p, e) => p > threshold && e <= threshold).Count(x => x);
            return (truePositives + falsePositives) > 0 ? (double)truePositives / (truePositives + falsePositives) : 0;
        }

        private static double CalculateRecall(List<double> predictions, List<double> expected)
        {
            var threshold = 0.5;
            var truePositives = predictions.Zip(expected, (p, e) => p > threshold && e > threshold).Count(x => x);
            var falseNegatives = predictions.Zip(expected, (p, e) => p <= threshold && e > threshold).Count(x => x);
            return (truePositives + falseNegatives) > 0 ? (double)truePositives / (truePositives + falseNegatives) : 0;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MLModelManager));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore?.Dispose();
            _models.Clear();
            _disposed = true;
            _logger.LogInformation("MLModelManager disposed");
        }
    }
}

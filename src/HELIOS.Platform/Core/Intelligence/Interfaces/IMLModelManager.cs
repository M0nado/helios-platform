using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Intelligence.Interfaces
{
    /// <summary>
    /// Interface for ML model lifecycle management.
    /// </summary>
    public interface IMLModelManager
    {
        /// <summary>
        /// Creates and registers a new ML model.
        /// </summary>
        /// <param name="modelName">Name of the model.</param>
        /// <param name="modelType">Type of model (e.g., 'anomaly', 'forecast').</param>
        /// <param name="trainingData">Initial training data.</param>
        /// <returns>Model ID.</returns>
        Task<string> CreateModelAsync(string modelName, string modelType, List<double> trainingData);

        /// <summary>
        /// Trains or retrains a model with new data.
        /// </summary>
        /// <param name="modelId">ID of the model to train.</param>
        /// <param name="trainingData">Training data.</param>
        /// <returns>Training accuracy/loss metric.</returns>
        Task<double> TrainModelAsync(string modelId, List<double> trainingData);

        /// <summary>
        /// Evaluates model performance on test data.
        /// </summary>
        /// <param name="modelId">ID of the model.</param>
        /// <param name="testData">Test data points.</param>
        /// <param name="expectedOutputs">Expected outputs for test data.</param>
        /// <returns>Evaluation metrics (accuracy, precision, recall, F1).</returns>
        Task<Dictionary<string, double>> EvaluateModelAsync(string modelId, List<double> testData, List<double> expectedOutputs);

        /// <summary>
        /// Gets model metadata and status.
        /// </summary>
        /// <param name="modelId">ID of the model.</param>
        /// <returns>Model information dictionary.</returns>
        Task<Dictionary<string, object>> GetModelInfoAsync(string modelId);

        /// <summary>
        /// Removes a model from management.
        /// </summary>
        /// <param name="modelId">ID of the model to remove.</param>
        Task DeleteModelAsync(string modelId);

        /// <summary>
        /// Gets all managed models.
        /// </summary>
        Task<List<string>> GetAllModelsAsync();

        /// <summary>
        /// Auto-retrains models that have degraded performance.
        /// </summary>
        /// <returns>Number of models retrained.</returns>
        Task<int> AutoRetrainDegradedModelsAsync();
    }
}

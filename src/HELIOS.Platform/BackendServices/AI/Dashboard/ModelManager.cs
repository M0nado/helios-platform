using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace HELIOS.Platform.BackendServices.AI.Dashboard
{
    /// <summary>
    /// Manages AI models including registration, lifecycle, and metadata
    /// </summary>
    public interface IModelManager
    {
        Task RegisterModelAsync(ModelMetadata metadata);
        Task<ModelMetadata> GetModelAsync(string modelId);
        Task<ModelMetadata[]> ListModelsAsync();
        Task UnregisterModelAsync(string modelId);
        Task UpdateModelAsync(string modelId, ModelMetadata metadata);
        Task<ModelTestResult> TestModelAsync(string modelId, TestPayload payload);
    }

    public class ModelManager : IModelManager
    {
        private readonly ConcurrentDictionary<string, ModelMetadata> _models;
        private readonly ConcurrentDictionary<string, ModelTestResult> _testResults;
        private readonly string _modelStoragePath;

        public ModelManager(string modelStoragePath = "./models")
        {
            _models = new ConcurrentDictionary<string, ModelMetadata>();
            _testResults = new ConcurrentDictionary<string, ModelTestResult>();
            _modelStoragePath = modelStoragePath;
            Directory.CreateDirectory(_modelStoragePath);
        }

        public async Task RegisterModelAsync(ModelMetadata metadata)
        {
            if (string.IsNullOrEmpty(metadata.ModelId))
                throw new ArgumentException("ModelId is required");

            metadata.RegisteredAt = DateTime.UtcNow;
            _models.TryAdd(metadata.ModelId, metadata);
            
            await Task.CompletedTask;
        }

        public async Task<ModelMetadata> GetModelAsync(string modelId)
        {
            if (_models.TryGetValue(modelId, out var model))
                return await Task.FromResult(model);
            
            throw new KeyNotFoundException($"Model {modelId} not found");
        }

        public async Task<ModelMetadata[]> ListModelsAsync()
        {
            return await Task.FromResult(_models.Values.ToArray());
        }

        public async Task UnregisterModelAsync(string modelId)
        {
            _models.TryRemove(modelId, out _);
            _testResults.TryRemove(modelId, out _);
            await Task.CompletedTask;
        }

        public async Task UpdateModelAsync(string modelId, ModelMetadata metadata)
        {
            if (!_models.ContainsKey(modelId))
                throw new KeyNotFoundException($"Model {modelId} not found");

            metadata.UpdatedAt = DateTime.UtcNow;
            _models[modelId] = metadata;
            await Task.CompletedTask;
        }

        public async Task<ModelTestResult> TestModelAsync(string modelId, TestPayload payload)
        {
            var model = await GetModelAsync(modelId);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = new ModelTestResult
            {
                ModelId = modelId,
                TestInput = payload.Input,
                TestOutput = "Test output (placeholder)",
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                MemoryUsageMb = GC.GetTotalMemory(false) / (1024 * 1024),
                Status = ModelTestStatus.Success,
                Timestamp = DateTime.UtcNow
            };

            _testResults.AddOrUpdate(modelId, result, (_, _) => result);
            return await Task.FromResult(result);
        }
    }

    public class ModelMetadata
    {
        public string ModelId { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Type { get; set; } // e.g., "GPT2", "LLAMA", "Mistral"
        public string Framework { get; set; } // e.g., "PyTorch", "ONNX", "TensorFlow"
        public long ParameterCount { get; set; }
        public double ContextWindowTokens { get; set; }
        public string FileLocation { get; set; }
        public long FileSizeBytes { get; set; }
        public bool SupportsQuantization { get; set; }
        public string[] SupportedQuantizations { get; set; } // "int4", "int8", "fp16"
        public string Description { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class TestPayload
    {
        public string Input { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public int MaxTokens { get; set; } = 100;
        public double Temperature { get; set; } = 0.7;
    }

    public class ModelTestResult
    {
        public string ModelId { get; set; }
        public string TestInput { get; set; }
        public string TestOutput { get; set; }
        public long ExecutionTimeMs { get; set; }
        public long MemoryUsageMb { get; set; }
        public ModelTestStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum ModelTestStatus
    {
        Success,
        PartialFailure,
        Failed,
        Timeout
    }
}

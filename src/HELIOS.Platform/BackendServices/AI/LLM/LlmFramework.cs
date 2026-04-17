using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.BackendServices.AI.LLM
{
    /// <summary>
    /// Unified framework for multi-model LLM support with auto-selection based on hardware
    /// </summary>
    public interface ILlmFramework
    {
        Task RegisterModelAsync(string modelId, ILanguageModel model);
        Task<string> InferAsync(string prompt, InferenceOptions options = null);
        Task<string[]> InferBatchAsync(string[] prompts, InferenceOptions options = null);
        Task<StreamedInference> InferStreamAsync(string prompt, InferenceOptions options = null);
        Task<ModelCapabilities> GetModelCapabilitiesAsync(string modelId);
        ILanguageModel SelectBestModel(HardwareProfile hardware);
        Task<QuantizationInfo> GetQuantizationInfoAsync(string modelId);
    }

    /// <summary>
    /// Core LLM Framework implementation
    /// </summary>
    public class LlmFramework : ILlmFramework
    {
        private readonly ConcurrentDictionary<string, ILanguageModel> _models;
        private readonly ConcurrentDictionary<string, ModelCache> _modelCache;
        private readonly HardwareDetector _hardwareDetector;
        private readonly InferenceQueue _inferenceQueue;

        public LlmFramework()
        {
            _models = new ConcurrentDictionary<string, ILanguageModel>();
            _modelCache = new ConcurrentDictionary<string, ModelCache>();
            _hardwareDetector = new HardwareDetector();
            _inferenceQueue = new InferenceQueue();
        }

        public async Task RegisterModelAsync(string modelId, ILanguageModel model)
        {
            _models.TryAdd(modelId, model);
            _modelCache.TryAdd(modelId, new ModelCache(modelId));
            await model.InitializeAsync();
        }

        public async Task<string> InferAsync(string prompt, InferenceOptions options = null)
        {
            options = options ?? InferenceOptions.Default();
            var model = _models[options.ModelId];
            
            var cachedResult = _modelCache[options.ModelId].Get(prompt);
            if (cachedResult != null && !options.SkipCache)
            {
                return cachedResult;
            }

            var result = await model.GenerateAsync(prompt, options);
            
            if (!options.SkipCache)
            {
                _modelCache[options.ModelId].Set(prompt, result);
            }

            return result;
        }

        public async Task<string[]> InferBatchAsync(string[] prompts, InferenceOptions options = null)
        {
            options = options ?? InferenceOptions.Default();
            var model = _models[options.ModelId];
            
            var results = new string[prompts.Length];
            
            if (options.ParallelBatchSize > 1)
            {
                var tasks = new List<Task>();
                for (int i = 0; i < prompts.Length; i++)
                {
                    var index = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        results[index] = await InferAsync(prompts[index], options);
                    }));

                    if (tasks.Count >= options.ParallelBatchSize)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }
                }

                if (tasks.Any())
                    await Task.WhenAll(tasks);
            }
            else
            {
                for (int i = 0; i < prompts.Length; i++)
                {
                    results[i] = await model.GenerateAsync(prompts[i], options);
                }
            }

            return results;
        }

        public async Task<StreamedInference> InferStreamAsync(string prompt, InferenceOptions options = null)
        {
            options = options ?? InferenceOptions.Default();
            var model = _models[options.ModelId];
            return await model.GenerateStreamAsync(prompt, options);
        }

        public async Task<ModelCapabilities> GetModelCapabilitiesAsync(string modelId)
        {
            if (!_models.TryGetValue(modelId, out var model))
                throw new KeyNotFoundException($"Model {modelId} not found");

            return await model.GetCapabilitiesAsync();
        }

        public ILanguageModel SelectBestModel(HardwareProfile hardware)
        {
            // Auto-select model based on hardware capabilities
            var availableModels = _models.Values.ToList();

            return hardware.VramGb switch
            {
                >= 24 => availableModels.FirstOrDefault(m => m.ModelType == ModelType.LLAMA70B) 
                    ?? availableModels.FirstOrDefault(m => m.ModelType == ModelType.LLAMA13B),
                >= 12 => availableModels.FirstOrDefault(m => m.ModelType == ModelType.LLAMA13B)
                    ?? availableModels.FirstOrDefault(m => m.ModelType == ModelType.Mistral7B),
                >= 8 => availableModels.FirstOrDefault(m => m.ModelType == ModelType.Mistral7B)
                    ?? availableModels.FirstOrDefault(m => m.ModelType == ModelType.LLAMA7B),
                >= 4 => availableModels.FirstOrDefault(m => m.ModelType == ModelType.Phi2B)
                    ?? availableModels.FirstOrDefault(m => m.ModelType == ModelType.GPT2),
                _ => availableModels.FirstOrDefault()
            };
        }

        public async Task<QuantizationInfo> GetQuantizationInfoAsync(string modelId)
        {
            if (!_models.TryGetValue(modelId, out var model))
                throw new KeyNotFoundException($"Model {modelId} not found");

            return await model.GetQuantizationInfoAsync();
        }
    }

    public interface ILanguageModel
    {
        string ModelId { get; }
        ModelType ModelType { get; }
        Task InitializeAsync();
        Task<string> GenerateAsync(string prompt, InferenceOptions options);
        Task<StreamedInference> GenerateStreamAsync(string prompt, InferenceOptions options);
        Task<ModelCapabilities> GetCapabilitiesAsync();
        Task<QuantizationInfo> GetQuantizationInfoAsync();
    }

    public class InferenceOptions
    {
        public string ModelId { get; set; }
        public int MaxTokens { get; set; } = 256;
        public double Temperature { get; set; } = 0.7;
        public double TopP { get; set; } = 0.9;
        public int TopK { get; set; } = 40;
        public double FrequencyPenalty { get; set; } = 0.0;
        public double PresencePenalty { get; set; } = 0.0;
        public string[] StopSequences { get; set; }
        public int ParallelBatchSize { get; set; } = 1;
        public bool UseQuantization { get; set; } = true;
        public QuantizationType QuantizationType { get; set; } = QuantizationType.Int8;
        public bool SkipCache { get; set; } = false;
        public int TimeoutMs { get; set; } = 30000;

        public static InferenceOptions Default() => new();
    }

    public class StreamedInference
    {
        public string ModelId { get; set; }
        public IAsyncEnumerable<string> TokenStream { get; set; }
        public int TotalTokensGenerated { get; set; }
        public double ExecutionTimeMs { get; set; }
    }

    public class ModelCapabilities
    {
        public string ModelId { get; set; }
        public int ContextWindowTokens { get; set; }
        public int MaxGenerationTokens { get; set; }
        public ModelType Type { get; set; }
        public bool SupportsStreaming { get; set; }
        public bool SupportsQuantization { get; set; }
        public string[] SupportedQuantizations { get; set; }
        public long ParameterCount { get; set; }
        public double EstimatedMemoryGb { get; set; }
    }

    public class QuantizationInfo
    {
        public string ModelId { get; set; }
        public QuantizationType CurrentQuantization { get; set; }
        public string[] AvailableQuantizations { get; set; }
        public long OriginalSizeBytes { get; set; }
        public long QuantizedSizeBytes { get; set; }
        public double CompressionRatio { get; set; }
        public double EstimatedAccuracyLoss { get; set; }
    }

    public enum ModelType
    {
        GPT2,
        GPTNeo,
        LLAMA7B,
        LLAMA13B,
        LLAMA70B,
        Mistral7B,
        Phi2B,
        Alpaca,
        Custom
    }

    public enum QuantizationType
    {
        None,
        Int4,
        Int8,
        FP16,
        BNOB4Bit
    }

    /// <summary>
    /// Detects hardware capabilities for optimal model selection
    /// </summary>
    public class HardwareDetector
    {
        public HardwareProfile DetectHardware()
        {
            var profile = new HardwareProfile
            {
                ProcessorCount = Environment.ProcessorCount,
                TotalMemoryGb = GC.GetTotalMemory(false) / (1024L * 1024L * 1024L),
                VramGb = DetectVram(),
                HasGpu = DetectGpu(),
                GpuType = DetectGpuType(),
                CudaCapable = DetectCuda()
            };

            return profile;
        }

        private long DetectVram()
        {
            // Placeholder - in production would query GPU drivers
            try
            {
                return 8; // Assume 8GB default
            }
            catch
            {
                return 0;
            }
        }

        private bool DetectGpu()
        {
            try
            {
                return true; // Placeholder
            }
            catch
            {
                return false;
            }
        }

        private string DetectGpuType()
        {
            try
            {
                return "NVIDIA"; // Placeholder
            }
            catch
            {
                return "CPU";
            }
        }

        private bool DetectCuda()
        {
            try
            {
                return true; // Placeholder
            }
            catch
            {
                return false;
            }
        }
    }

    public class HardwareProfile
    {
        public int ProcessorCount { get; set; }
        public long TotalMemoryGb { get; set; }
        public long VramGb { get; set; }
        public bool HasGpu { get; set; }
        public string GpuType { get; set; }
        public bool CudaCapable { get; set; }
    }

    /// <summary>
    /// Model caching for hot models in VRAM
    /// </summary>
    public class ModelCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly string _modelId;
        private const int MaxCacheSize = 1000;

        public ModelCache(string modelId)
        {
            _modelId = modelId;
            _cache = new ConcurrentDictionary<string, CacheEntry>();
        }

        public string Get(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                entry.LastAccessed = DateTime.UtcNow;
                entry.AccessCount++;
                return entry.Value;
            }

            return null;
        }

        public void Set(string key, string value)
        {
            if (_cache.Count >= MaxCacheSize)
            {
                var lruEntry = _cache.Values.OrderBy(e => e.LastAccessed).First();
                _cache.TryRemove(_cache.First(kvp => kvp.Value == lruEntry).Key, out _);
            }

            _cache.AddOrUpdate(key, 
                new CacheEntry { Value = value, LastAccessed = DateTime.UtcNow, AccessCount = 1 },
                (_, entry) => new CacheEntry { Value = value, LastAccessed = DateTime.UtcNow, AccessCount = entry.AccessCount + 1 });
        }

        private class CacheEntry
        {
            public string Value { get; set; }
            public DateTime LastAccessed { get; set; }
            public int AccessCount { get; set; }
        }
    }

    /// <summary>
    /// Manages inference queue for batch processing
    /// </summary>
    public class InferenceQueue
    {
        private readonly Queue<InferenceTask> _queue;

        public InferenceQueue()
        {
            _queue = new Queue<InferenceTask>();
        }

        public void Enqueue(InferenceTask task)
        {
            lock (_queue)
            {
                _queue.Enqueue(task);
            }
        }

        public bool TryDequeue(out InferenceTask task)
        {
            lock (_queue)
            {
                return _queue.TryDequeue(out task);
            }
        }

        public int GetQueueLength()
        {
            lock (_queue)
            {
                return _queue.Count;
            }
        }
    }

    public class InferenceTask
    {
        public string TaskId { get; set; }
        public string Prompt { get; set; }
        public InferenceOptions Options { get; set; }
        public DateTime EnqueuedAt { get; set; }
    }
}

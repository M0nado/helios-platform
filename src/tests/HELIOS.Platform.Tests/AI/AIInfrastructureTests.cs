using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using HELIOS.Platform.BackendServices.AI.Dashboard;
using HELIOS.Platform.BackendServices.AI.LLM;
using HELIOS.Platform.BackendServices.AI.LLM.Models;
using HELIOS.Platform.BackendServices.AI.LLM.Quantization;
using HELIOS.Platform.BackendServices.AI.TokenOptimization;
using HELIOS.Platform.BackendServices.AI.AgentOptimization;

namespace HELIOS.Platform.Tests.AI
{
    // ====== TASK 1: AI Dashboard Tests ======
    public class AiDashboardServiceTests
    {
        private readonly AiDashboardService _service;

        public AiDashboardServiceTests()
        {
            _service = new AiDashboardService();
        }

        [Fact]
        public async Task GetMetricsAsync_ReturnsValidMetrics()
        {
            var metrics = await _service.GetMetricsAsync();

            Assert.NotNull(metrics);
            Assert.True(metrics.Timestamp > DateTime.UtcNow.AddSeconds(-5));
            Assert.NotNull(metrics.SystemHealth);
        }

        [Fact]
        public async Task RegisterModelStatus_UpdatesState()
        {
            var status = new ModelStatus
            {
                ModelId = "test-model",
                Name = "Test Model",
                IsActive = true,
                IsHealthy = true,
                MemoryUsage = 2048,
                CpuUsage = 45.5
            };

            _service.RegisterModelStatus(status);
            var models = await _service.GetModelStatusAsync();

            Assert.Contains(models, m => m.ModelId == "test-model");
        }

        [Fact]
        public async Task GetTokenUsageAsync_ReturnsUsageReport()
        {
            var report = await _service.GetTokenUsageAsync();

            Assert.NotNull(report);
            Assert.NotNull(report.TokensByModel);
            Assert.NotNull(report.TokensByAgent);
        }
    }

    public class WorkflowBuilderTests
    {
        private readonly WorkflowBuilder _builder;

        public WorkflowBuilderTests()
        {
            _builder = new WorkflowBuilder();
        }

        [Fact]
        public async Task CreateWorkflowAsync_CreatesValidWorkflow()
        {
            var workflow = await _builder.CreateWorkflowAsync("Test Workflow");

            Assert.NotNull(workflow);
            Assert.Equal("Test Workflow", workflow.Name);
            Assert.NotEmpty(workflow.Id);
        }

        [Fact]
        public async Task AddStepAsync_AddsStepToWorkflow()
        {
            var workflow = await _builder.CreateWorkflowAsync("Test");
            var step = new WorkflowStep
            {
                StepType = "Action",
                ComponentId = "component1",
                Parameters = new Dictionary<string, object> { { "key", "value" } }
            };

            await _builder.AddStepAsync(workflow.Id, step);
            var retrieved = await _builder.GetWorkflowAsync(workflow.Id);

            Assert.Single(retrieved.Steps);
        }

        [Fact]
        public async Task ValidateWorkflowAsync_IdentifiesEmptyWorkflow()
        {
            var workflow = await _builder.CreateWorkflowAsync("Empty");
            var validation = await _builder.ValidateWorkflowAsync(workflow.Id);

            Assert.False(validation.IsValid);
            Assert.NotEmpty(validation.Errors);
        }

        [Fact]
        public async Task ExecuteWorkflowAsync_ExecutesValidWorkflow()
        {
            var workflow = await _builder.CreateWorkflowAsync("Execute Test");
            var step = new WorkflowStep
            {
                StepType = "Action",
                ComponentId = "test-component"
            };

            await _builder.AddStepAsync(workflow.Id, step);
            var result = await _builder.ExecuteWorkflowAsync(workflow.Id, new Dictionary<string, object>());

            Assert.NotNull(result);
            Assert.NotEmpty(result.ExecutionId);
        }
    }

    public class PerformanceMonitorTests
    {
        private readonly PerformanceMonitor _monitor;

        public PerformanceMonitorTests()
        {
            _monitor = new PerformanceMonitor(100);
        }

        [Fact]
        public void RecordMetric_StoresMetricPoint()
        {
            var metric = new PerformanceMetricPoint
            {
                Timestamp = DateTime.UtcNow,
                LatencyMs = 150,
                MemoryMb = 512,
                CpuPercent = 45,
                IsSuccess = true
            };

            _monitor.RecordMetric("component1", metric);
            Assert.True(true); // No exception = success
        }

        [Fact]
        public async Task GetStatsAsync_ReturnsValidStats()
        {
            var metric = new PerformanceMetricPoint
            {
                Timestamp = DateTime.UtcNow,
                LatencyMs = 200,
                MemoryMb = 1024,
                CpuPercent = 60,
                IsSuccess = true
            };

            _monitor.RecordMetric("comp1", metric);
            var stats = await _monitor.GetStatsAsync("comp1");

            Assert.NotNull(stats);
            Assert.Equal("comp1", stats.ComponentId);
            Assert.Equal(1, stats.RecordCount);
        }

        [Fact]
        public async Task GetHealthStatusAsync_ReturnsHealthStatus()
        {
            var health = await _monitor.GetHealthStatusAsync();

            Assert.NotNull(health);
            Assert.NotNull(health.Status);
        }
    }

    // ====== TASK 2: LLM Framework Tests ======
    public class LlmFrameworkTests
    {
        private readonly LlmFramework _framework;

        public LlmFrameworkTests()
        {
            _framework = new LlmFramework();
        }

        [Fact]
        public async Task RegisterModelAsync_RegistersModel()
        {
            var model = new GptModel();
            await _framework.RegisterModelAsync("gpt2", model);

            var capabilities = await _framework.GetModelCapabilitiesAsync("gpt2");
            Assert.NotNull(capabilities);
            Assert.Equal("gpt2", capabilities.ModelId);
        }

        [Fact]
        public async Task InferAsync_GeneratesText()
        {
            var model = new GptModel();
            await _framework.RegisterModelAsync("gpt2", model);

            var result = await _framework.InferAsync("Hello world", new InferenceOptions { ModelId = "gpt2" });

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task InferBatchAsync_ProcessesMultiplePrompts()
        {
            var model = new Mistral7bModel();
            await _framework.RegisterModelAsync("mistral", model);

            var prompts = new[] { "Prompt 1", "Prompt 2", "Prompt 3" };
            var results = await _framework.InferBatchAsync(prompts, new InferenceOptions { ModelId = "mistral" });

            Assert.Equal(3, results.Length);
            Assert.All(results, r => Assert.NotEmpty(r));
        }

        [Fact]
        public async Task InferStreamAsync_ReturnsStreamedInference()
        {
            var model = new Llama7bModel();
            await _framework.RegisterModelAsync("llama7b", model);

            var streamed = await _framework.InferStreamAsync("Stream test", new InferenceOptions { ModelId = "llama7b" });

            Assert.NotNull(streamed);
            Assert.NotNull(streamed.TokenStream);
        }

        [Fact]
        public void SelectBestModel_ChoosesAppropriateModel()
        {
            var models = new[]
            {
                new GptModel(),
                new Llama7bModel(),
                new Llama70bModel(),
                new Phi2bModel()
            };

            foreach (var model in models)
            {
                _framework.RegisterModelAsync(model.ModelId, model).Wait();
            }

            var hardware = new HardwareProfile
            {
                VramGb = 12,
                ProcessorCount = 8,
                HasGpu = true
            };

            var selected = _framework.SelectBestModel(hardware);
            Assert.NotNull(selected);
        }
    }

    public class LanguageModelTests
    {
        [Fact]
        public async Task GptModel_GeneratesText()
        {
            var model = new GptModel();
            var result = await model.GenerateAsync("Test prompt", InferenceOptions.Default());

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task LlamaModel_HandlesQuantization()
        {
            var model = new Llama7bModel(quantize: true, quantizationType: QuantizationType.Int8);
            var capabilities = await model.GetCapabilitiesAsync();

            Assert.True(capabilities.SupportsQuantization);
            Assert.Contains("int8", capabilities.SupportedQuantizations);
        }

        [Fact]
        public async Task Phi2bModel_HasSmallFootprint()
        {
            var model = new Phi2bModel(QuantizationType.Int4);
            var capabilities = await model.GetCapabilitiesAsync();

            Assert.True(capabilities.EstimatedMemoryGb < 2);
        }
    }

    public class QuantizationServiceTests
    {
        private readonly QuantizationService _service;

        public QuantizationServiceTests()
        {
            _service = new QuantizationService();
        }

        [Fact]
        public async Task QuantizeModelAsync_ProducesValidResult()
        {
            var result = await _service.QuantizeModelAsync("model.pth", QuantizationType.Int8);

            Assert.True(result.Success);
            Assert.Equal(QuantizationType.Int8, result.QuantizationType);
            Assert.True(result.CompressionRatio < 1.0);
        }

        [Fact]
        public double EstimateMemorySavings_CalculatesCorrectly()
        {
            var savings = _service.EstimateMemorySavings(1_000_000_000, QuantizationType.Int4);

            Assert.True(savings > 0);
            Assert.True(savings < 1_000_000_000);
        }

        [Fact]
        public void QuantizationStrategy_SelectsOptimal()
        {
            var type = QuantizationStrategy.SelectOptimalQuantization(
                modelSizeBytes: 10_000_000_000,
                availableVramBytes: 4_000_000_000,
                accuracyThreshold: 0.95
            );

            Assert.NotEqual(QuantizationType.None, type);
        }
    }

    // ====== TASK 3: Token Optimization Tests ======
    public class TokenBudgetTests
    {
        private readonly TokenBudget _budget;

        public TokenBudgetTests()
        {
            _budget = new TokenBudget(globalLimitPerDay: 100_000, perRequestLimit: 5_000);
        }

        [Fact]
        public async Task GetBudgetAsync_ReturnsValidBudget()
        {
            var budget = await _budget.GetBudgetAsync("req1");

            Assert.NotNull(budget);
            Assert.Equal("req1", budget.RequestId);
            Assert.True(budget.RemainingTokens > 0);
        }

        [Fact]
        public async Task AllocateTokensAsync_SucceedsWhenAvailable()
        {
            var allocated = await _budget.AllocateTokensAsync("req1", 1000);

            Assert.True(allocated);
        }

        [Fact]
        public async Task RecordUsageAsync_TracksCost()
        {
            await _budget.AllocateTokensAsync("req1", 1000);
            await _budget.RecordUsageAsync("req1", 500);

            var analysis = await _budget.AnalyzeBudgetAsync();
            Assert.True(analysis.TotalUsed > 0);
        }
    }

    public class ContextManagerTests
    {
        private readonly ContextManager _manager;

        public ContextManagerTests()
        {
            _manager = new ContextManager();
        }

        [Fact]
        public async Task CreateContextAsync_CreatesContext()
        {
            var context = await _manager.CreateContextAsync("conv1", 2048);

            Assert.NotNull(context);
            Assert.Equal("conv1", context.ConversationId);
            Assert.Equal(2048, context.WindowSize);
        }

        [Fact]
        public async Task AddMessageAsync_AddsToContext()
        {
            await _manager.CreateContextAsync("conv1", 4096);
            var message = new ConversationMessage
            {
                Role = "user",
                Content = "Hello, how are you?"
            };

            await _manager.AddMessageAsync("conv1", message);
            var context = await _manager.GetContextAsync("conv1");

            Assert.Single(context);
        }

        [Fact]
        public async Task EstimateTokensAsync_CalculatesTokenCount()
        {
            await _manager.CreateContextAsync("conv1", 4096);
            var message = new ConversationMessage
            {
                Role = "user",
                Content = "This is a test message"
            };

            await _manager.AddMessageAsync("conv1", message);
            var tokens = await _manager.EstimateTokensAsync("conv1");

            Assert.True(tokens > 0);
        }
    }

    public class PromptCompressorTests
    {
        private readonly PromptCompressor _compressor;

        public PromptCompressorTests()
        {
            _compressor = new PromptCompressor();
        }

        [Fact]
        public async Task CompressAsync_ReducesLength()
        {
            var prompt = "This is a long prompt that contains many words and should be compressed to reduce the length";
            var result = await _compressor.CompressAsync(prompt, 0.5);

            Assert.True(result.CompressedLength < result.OriginalLength);
            Assert.True(result.AchievedCompressionRatio <= 1.0);
        }

        [Fact]
        public string RemoveStopwords_FiltersCommonWords()
        {
            var text = "The quick brown fox jumps over the lazy dog";
            var result = _compressor.RemoveStopwords(text);

            Assert.DoesNotContain("the", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("quick", result);
            return result;
        }

        [Fact]
        public async Task ExtractEntitiesAsync_FindsNamedEntities()
        {
            var text = "Apple Inc. was founded in 1976 by Steve Jobs.";
            var entities = await _compressor.ExtractEntitiesAsync(text);

            Assert.NotEmpty(entities);
            Assert.Contains("Apple", entities);
        }
    }

    // ====== TASK 4: Agent Optimization Tests ======
    public class AgentProfilerTests
    {
        private readonly AgentProfiler _profiler;

        public AgentProfilerTests()
        {
            _profiler = new AgentProfiler();
        }

        [Fact]
        public async Task ProfileAgentAsync_GeneratesProfile()
        {
            var tasks = new[] { "task1", "task2", "task3" };
            var profile = await _profiler.ProfileAgentAsync("agent1", tasks);

            Assert.NotNull(profile);
            Assert.Equal("agent1", profile.AgentId);
            Assert.Equal(3, profile.TotalExecutionsProfiled);
        }

        [Fact]
        public async Task GenerateReportAsync_CreatesDetailedReport()
        {
            var tasks = new[] { "task1", "task2" };
            await _profiler.ProfileAgentAsync("agent1", tasks);

            var report = await _profiler.GenerateReportAsync("agent1");

            Assert.NotNull(report);
            Assert.True(report.AverageExecutionTimeMs > 0);
            Assert.True(report.SuccessRate > 0);
        }
    }

    public class BottleneckDetectorTests
    {
        private readonly AgentProfiler _profiler;
        private readonly BottleneckDetector _detector;

        public BottleneckDetectorTests()
        {
            _profiler = new AgentProfiler();
            _detector = new BottleneckDetector(_profiler);
        }

        [Fact]
        public async Task AnalyzeAsync_IdentifiesBottlenecks()
        {
            var tasks = Enumerable.Range(0, 10).Select(i => $"task{i}").ToArray();
            await _profiler.ProfileAgentAsync("agent1", tasks);

            var analysis = await _detector.AnalyzeAsync("agent1");

            Assert.NotNull(analysis);
            Assert.True(analysis.OverallHealthScore >= 0 && analysis.OverallHealthScore <= 1);
        }

        [Fact]
        public async Task GetSuggestionsAsync_ProvidesSuggestions()
        {
            var tasks = Enumerable.Range(0, 5).Select(i => $"task{i}").ToArray();
            await _profiler.ProfileAgentAsync("agent1", tasks);

            var suggestions = await _detector.GetSuggestionsAsync("agent1");

            Assert.NotNull(suggestions);
        }
    }

    public class AutoTunerTests
    {
        [Fact]
        public async Task ApplyOptimizationAsync_UpdatesConfiguration()
        {
            var tuner = new AutoTuner();
            var suggestion = new OptimizationSuggestion
            {
                Title = "Implement Caching",
                Description = "Add caching",
                Priority = Priority.High,
                EstimatedImprovementPercent = 30
            };

            var result = await tuner.ApplyOptimizationAsync("agent1", suggestion);

            Assert.Equal(TuningStatus.Applied, result.Status);
            Assert.Equal(30, result.EstimatedImprovementPercent);
        }

        [Fact]
        public async Task GetCurrentConfigurationAsync_ReturnsConfig()
        {
            var tuner = new AutoTuner();
            var config = await tuner.GetCurrentConfigurationAsync("agent1");

            Assert.NotNull(config);
            Assert.Equal("agent1", config.AgentId);
        }
    }

    public class LearningSystemTests
    {
        [Fact]
        public async Task TrainModelAsync_BuildsPredictor()
        {
            var profiler = new AgentProfiler();
            var learning = new LearningSystem(profiler);

            var tasks = new[] { "task1", "task2", "task3" };
            await profiler.ProfileAgentAsync("agent1", tasks);

            var model = await learning.TrainModelAsync("agent1", 100);

            Assert.NotNull(model);
            Assert.True(model.ModelAccuracy > 0.8);
        }

        [Fact]
        public async Task PredictExecutionAsync_MakesTimeestimate()
        {
            var profiler = new AgentProfiler();
            var learning = new LearningSystem(profiler);

            var tasks = new[] { "task1", "task2" };
            await profiler.ProfileAgentAsync("agent1", tasks);

            var prediction = await learning.PredictExecutionAsync("agent1", "task");

            Assert.NotNull(prediction);
            Assert.True(prediction.PredictedExecutionTimeMs > 0);
        }
    }

    // ====== Integration Tests ======
    public class IntegrationTests
    {
        [Fact]
        public async Task FullAiPipelineIntegration_WorksEndToEnd()
        {
            // Setup all components
            var dashboard = new AiDashboardService();
            var llm = new LlmFramework();
            var tokenBudget = new TokenBudget();
            var contextManager = new ContextManager();
            var profiler = new AgentProfiler();

            // Register a model
            var model = new Mistral7bModel();
            await llm.RegisterModelAsync("mistral-7b", model);

            // Setup dashboard
            var modelStatus = new ModelStatus
            {
                ModelId = "mistral-7b",
                Name = "Mistral 7B",
                IsActive = true,
                IsHealthy = true
            };
            dashboard.RegisterModelStatus(modelStatus);

            // Create context
            await contextManager.CreateContextAsync("conv1", 4096);
            var msg = new ConversationMessage { Role = "user", Content = "Hello" };
            await contextManager.AddMessageAsync("conv1", msg);

            // Profile agent
            var tasks = new[] { "task1", "task2" };
            await profiler.ProfileAgentAsync("agent1", tasks);

            // Verify everything works
            var metrics = await dashboard.GetMetricsAsync();
            var tokenBudgetInfo = await tokenBudget.GetBudgetAsync("req1");
            var context = await contextManager.GetContextAsync("conv1");
            var report = await profiler.GenerateReportAsync("agent1");

            Assert.NotNull(metrics);
            Assert.NotNull(tokenBudgetInfo);
            Assert.Single(context);
            Assert.NotNull(report);
        }
    }
}

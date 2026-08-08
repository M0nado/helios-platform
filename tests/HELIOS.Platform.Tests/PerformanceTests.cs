using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core;
using Xunit;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Performance tests for deployment execution and scaling helpers.
    /// </summary>
    public class PerformanceTests
    {
        [Theory]
        [InlineData(DeploymentTier.Professional, 1000)]
        [InlineData(DeploymentTier.Enterprise, 1000)]
        [InlineData(DeploymentTier.Ultimate, 1000)]
        public async Task Perf_DeploymentExecution_CompletesWithinBudget(DeploymentTier tier, int maxMilliseconds)
        {
            var deployment = new HeliosDeployment();
            var sw = Stopwatch.StartNew();

            var result = await deployment.Execute(tier);

            sw.Stop();
            Assert.True(result.Success);
            Assert.True(result.Phases.Count >= 6);
            Assert.True(sw.ElapsedMilliseconds < maxMilliseconds, $"Execution took {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task Perf_ConcurrentDeploymentExecution_CompletesUnderLoad()
        {
            const int concurrency = 24;
            var sw = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, concurrency)
                .Select(_ => new HeliosDeployment().Execute(DeploymentTier.Professional))
                .ToArray();
            var results = await Task.WhenAll(tasks);

            sw.Stop();
            Assert.All(results, result => Assert.True(result.Success));
            Assert.True(sw.ElapsedMilliseconds < 5000, $"Concurrent execution took {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task Perf_DeploymentExecution_HasReasonableMemoryGrowth()
        {
            var before = GC.GetTotalMemory(true);

            for (var i = 0; i < 50; i++)
            {
                var result = await new HeliosDeployment().Execute(DeploymentTier.Professional);
                Assert.True(result.Success);
            }

            GC.Collect();
            var after = GC.GetTotalMemory(true);
            var growthBytes = after - before;
            Assert.True(growthBytes < 50_000_000, $"Memory grew by {growthBytes} bytes");
        }

        [Fact]
        public async Task Perf_AgentFleetSimulation_MeetsBaseline()
        {
            var simulator = new AgentFleetSimulator(numberOfAgents: 20, requestsPerAgent: 10, concurrency: 40);
            var metrics = await simulator.RunSimulationAsync(TimeSpan.FromSeconds(30));

            Assert.True(metrics.TotalRequestsSent > 0);
            Assert.True(metrics.SuccessRate > 0.95, $"Success rate was {metrics.SuccessRate:P2}");
            Assert.True(metrics.RequestsPerSecond > 50, $"Throughput was {metrics.RequestsPerSecond:F2} req/sec");
        }

        [Fact]
        public async Task Perf_ConcurrencyStressTester_CompletesWithoutDeadlocks()
        {
            var result = await ConcurrencyStressTester.TestConcurrentAccessAsync(
                agents: 20,
                operationsPerAgent: 20,
                operation: async _ => await Task.Delay(1));

            Assert.Equal(0, result.DeadlocksDetected);
            Assert.Equal(0, result.ExceptionsThrown);
            Assert.True(result.TotalOperations > 0);
        }

        [Fact]
        public void Perf_LoadDistributionAnalyzer_ReportsFairForBalancedLoad()
        {
            var analysis = LoadDistributionAnalyzer.AnalyzeDistribution(new[] { 100, 98, 102, 101, 99 });

            Assert.True(analysis.IsFair);
            Assert.True(analysis.CoefficientOfVariation < 0.2);
            Assert.True(analysis.JainIndex > 0.8);
        }
    }
}

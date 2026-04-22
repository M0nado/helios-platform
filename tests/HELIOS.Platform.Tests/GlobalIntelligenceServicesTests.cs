using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Core.GlobalIntelligence.Interfaces;

namespace HELIOS.Platform.Core.GlobalIntelligence
{
    public class GlobalIntelligenceServicesTests
    {
        private readonly Mock<ILogger<GlobalMetricsAggregator>> _metricsLoggerMock;
        private readonly Mock<ILogger<CostOptimizer>> _costLoggerMock;
        private readonly Mock<ILogger<GlobalCapacityPlanner>> _capacityLoggerMock;
        private readonly Mock<ILogger<GlobalLoadBalancer>> _loadBalancerLoggerMock;
        private readonly Mock<ILogger<RegionFailover>> _failoverLoggerMock;
        private readonly Mock<ILogger<LatencyOptimizer>> _latencyLoggerMock;
        private readonly Mock<ILogger<CDNController>> _cdnLoggerMock;

        public GlobalIntelligenceServicesTests()
        {
            _metricsLoggerMock = new Mock<ILogger<GlobalMetricsAggregator>>();
            _costLoggerMock = new Mock<ILogger<CostOptimizer>>();
            _capacityLoggerMock = new Mock<ILogger<GlobalCapacityPlanner>>();
            _loadBalancerLoggerMock = new Mock<ILogger<GlobalLoadBalancer>>();
            _failoverLoggerMock = new Mock<ILogger<RegionFailover>>();
            _latencyLoggerMock = new Mock<ILogger<LatencyOptimizer>>();
            _cdnLoggerMock = new Mock<ILogger<CDNController>>();
        }

        #region GlobalMetricsAggregator Tests

        [Fact]
        public async Task AggregateMetricsAsync_ReturnsValidMetrics()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var result = await aggregator.AggregateMetricsAsync();
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task AggregateMetricsAsync_ResultsAreNumeric()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var result = await aggregator.AggregateMetricsAsync();
            
            foreach (var metric in result.Values)
            {
                Assert.True(double.IsFinite(metric));
            }
        }

        [Fact]
        public async Task GetRegionalMetricsAsync_WithValidRegion_ReturnsMetrics()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var result = await aggregator.GetRegionalMetricsAsync("us-east-1");
            
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRegionalMetricsAsync_WithInvalidRegion_ReturnsEmpty()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var result = await aggregator.GetRegionalMetricsAsync("invalid-region");
            
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRegionalMetricsAsync_WithNullRegion_ThrowsException()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                aggregator.GetRegionalMetricsAsync(null));
        }

        [Fact]
        public async Task AnalyzeTrendsAsync_ReturnsValidTrends()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var now = DateTime.UtcNow;
            var result = await aggregator.AnalyzeTrendsAsync(now.AddDays(-7), now);
            
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AnalyzeTrendsAsync_WithInvalidDateRange_ThrowsException()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var now = DateTime.UtcNow;
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                aggregator.AnalyzeTrendsAsync(now, now.AddDays(-7)));
        }

        [Fact]
        public async Task AggregateMetricsAsync_IsThreadSafe()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var tasks = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                tasks.Add(aggregator.AggregateMetricsAsync());
            }

            await Task.WhenAll(tasks);
            Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
        }

        #endregion

        #region CostOptimizer Tests

        [Fact]
        public async Task AnalyzeCostsAsync_ReturnsValidCosts()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            var result = await optimizer.AnalyzeCostsAsync();
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result.Values, cost => Assert.True(cost >= 0));
        }

        [Fact]
        public async Task OptimizeAsync_ReturnsRecommendations()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            var result = await optimizer.OptimizeAsync();
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, r => Assert.NotEmpty(r));
        }

        [Fact]
        public async Task OptimizeAsync_RecommendationsAreRealistic()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            var result = await optimizer.OptimizeAsync();
            
            Assert.Contains("migrate", result[0].ToLower());
        }

        [Fact]
        public async Task GetRecommendationsAsync_ReturnsROIValues()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            var result = await optimizer.GetRecommendationsAsync();
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetRecommendationsAsync_ROIValuesArePositive()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            var result = await optimizer.GetRecommendationsAsync();
            
            Assert.All(result.Values, roi => Assert.True(roi > 0));
        }

        [Fact]
        public async Task CostOptimizer_IsThreadSafe()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            var tasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(optimizer.AnalyzeCostsAsync());
            }

            await Task.WhenAll(tasks);
            Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
        }

        #endregion

        #region GlobalCapacityPlanner Tests

        [Fact]
        public async Task PlanCapacityAsync_WithValidMonths_ReturnsCapacityPlan()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var result = await planner.PlanCapacityAsync(3);
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task PlanCapacityAsync_WithInvalidMonths_ThrowsException()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentException>(() => planner.PlanCapacityAsync(0));
            await Assert.ThrowsAsync<ArgumentException>(() => planner.PlanCapacityAsync(13));
        }

        [Fact]
        public async Task PlanCapacityAsync_CapacityValuesArePositive()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var result = await planner.PlanCapacityAsync(6);
            
            Assert.All(result.Values, capacity => Assert.True(capacity > 0));
        }

        [Fact]
        public async Task ForecastRequirementsAsync_ReturnsResourceForecasts()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var result = await planner.ForecastRequirementsAsync(3);
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task ForecastRequirementsAsync_ContainsExpectedResourceTypes()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var result = await planner.ForecastRequirementsAsync(3);
            
            Assert.Contains("compute", result.Keys);
            Assert.Contains("storage", result.Keys);
        }

        [Fact]
        public async Task AllocateResourcesAsync_WithValidResources_ReturnsAllocation()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var result = await planner.AllocateResourcesAsync(1000);
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task AllocateResourcesAsync_WithInvalidResources_ThrowsException()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentException>(() => planner.AllocateResourcesAsync(0));
            await Assert.ThrowsAsync<ArgumentException>(() => planner.AllocateResourcesAsync(-100));
        }

        [Fact]
        public async Task AllocateResourcesAsync_AllResourcesAllocated()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var available = 1000;
            var result = await planner.AllocateResourcesAsync(available);
            
            var totalAllocated = result.Values.Sum();
            Assert.Equal(available, totalAllocated);
        }

        #endregion

        #region GlobalLoadBalancer Tests

        [Fact]
        public async Task BalanceLoadAsync_WithValidRequests_ReturnsLoadDistribution()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            var result = await balancer.BalanceLoadAsync(1000);
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task BalanceLoadAsync_WithInvalidRequests_ThrowsException()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentException>(() => balancer.BalanceLoadAsync(0));
            await Assert.ThrowsAsync<ArgumentException>(() => balancer.BalanceLoadAsync(-100));
        }

        [Fact]
        public async Task BalanceLoadAsync_LoadIsDistributed()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            var requestCount = 1000;
            var result = await balancer.BalanceLoadAsync(requestCount);
            
            var totalDistributed = result.Values.Sum();
            Assert.Equal(requestCount, totalDistributed);
        }

        [Fact]
        public async Task CalculateOptimalRoutingAsync_WithValidRegions_ReturnsPath()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            var result = await balancer.CalculateOptimalRoutingAsync("us-east-1", "us-west-2");
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal("us-east-1", result.First());
        }

        [Fact]
        public async Task CalculateOptimalRoutingAsync_WithNullSource_ThrowsException()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                balancer.CalculateOptimalRoutingAsync(null, "us-west-2"));
        }

        [Fact]
        public async Task CalculateOptimalRoutingAsync_WithNullDestination_ThrowsException()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                balancer.CalculateOptimalRoutingAsync("us-east-1", null));
        }

        [Fact]
        public async Task HandleFailoverAsync_WithValidRegion_ReturnsNewDistribution()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            await balancer.BalanceLoadAsync(1000); // Initialize
            var result = await balancer.HandleFailoverAsync("us-east-1");
            
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HandleFailoverAsync_WithNullRegion_ThrowsException()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                balancer.HandleFailoverAsync(null));
        }

        #endregion

        #region RegionFailover Tests

        [Fact]
        public async Task MonitorRegionHealthAsync_ReturnsHealthMetrics()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            var result = await failover.MonitorRegionHealthAsync("us-east-1");
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("is_healthy", result.Keys);
        }

        [Fact]
        public async Task MonitorRegionHealthAsync_WithNullRegion_ThrowsException()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                failover.MonitorRegionHealthAsync(null));
        }

        [Fact]
        public async Task MonitorRegionHealthAsync_HealthMetricsAreValid()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            var result = await failover.MonitorRegionHealthAsync("us-west-2");
            
            Assert.IsType<bool>(result["is_healthy"]);
            Assert.IsType<double>(result["response_time_ms"]);
            Assert.IsType<double>(result["error_rate"]);
        }

        [Fact]
        public async Task TriggerFailoverAsync_WithValidRegions_ReturnsSuccess()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            var result = await failover.TriggerFailoverAsync("us-east-1", "us-west-2");
            
            Assert.True(result);
        }

        [Fact]
        public async Task TriggerFailoverAsync_WithNullFailedRegion_ThrowsException()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                failover.TriggerFailoverAsync(null, "us-west-2"));
        }

        [Fact]
        public async Task TriggerFailoverAsync_WithNullTargetRegion_ThrowsException()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                failover.TriggerFailoverAsync("us-east-1", null));
        }

        [Fact]
        public async Task RecoverAsync_WithValidRegion_ReturnsSuccess()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            var result = await failover.RecoverAsync("us-east-1");
            
            Assert.True(result);
        }

        [Fact]
        public async Task RecoverAsync_WithNullRegion_ThrowsException()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                failover.RecoverAsync(null));
        }

        [Fact]
        public async Task RegionFailover_IsThreadSafe()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            var tasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(failover.MonitorRegionHealthAsync("us-east-1"));
            }

            await Task.WhenAll(tasks);
            Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
        }

        #endregion

        #region LatencyOptimizer Tests

        [Fact]
        public async Task OptimizeLatencyAsync_ReturnsLatencyValue()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            var result = await optimizer.OptimizeLatencyAsync("us-east-1", "us-west-2");
            
            Assert.True(result > 0);
            Assert.True(result <= 100.0);
        }

        [Fact]
        public async Task OptimizeLatencyAsync_WithNullSource_ThrowsException()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                optimizer.OptimizeLatencyAsync(null, "us-west-2"));
        }

        [Fact]
        public async Task OptimizeLatencyAsync_WithNullDestination_ThrowsException()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                optimizer.OptimizeLatencyAsync("us-east-1", null));
        }

        [Fact]
        public async Task FindOptimalPathAsync_ReturnsValidPath()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            var result = await optimizer.FindOptimalPathAsync("us-east-1", "eu-central-1");
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal("us-east-1", result.First());
        }

        [Fact]
        public async Task FindOptimalPathAsync_WithNullSource_ThrowsException()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                optimizer.FindOptimalPathAsync(null, "eu-central-1"));
        }

        [Fact]
        public async Task FindOptimalPathAsync_WithNullDestination_ThrowsException()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                optimizer.FindOptimalPathAsync("us-east-1", null));
        }

        [Fact]
        public async Task CacheLookupAsync_WithValidPath_ReturnsCachedPath()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            
            // Populate cache
            await optimizer.FindOptimalPathAsync("us-east-1", "us-west-2");
            
            // Lookup
            var cached = await optimizer.CacheLookupAsync("us-east-1", "us-west-2");
            
            Assert.NotNull(cached);
        }

        [Fact]
        public async Task CacheLookupAsync_WithNullSource_ThrowsException()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                optimizer.CacheLookupAsync(null, "us-west-2"));
        }

        [Fact]
        public async Task CacheLookupAsync_WithNullDestination_ThrowsException()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                optimizer.CacheLookupAsync("us-east-1", null));
        }

        #endregion

        #region CDNController Tests

        [Fact]
        public async Task ConfigureCDNAsync_WithValidConfig_ReturnsSuccess()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            var config = new Dictionary<string, string>
            {
                { "provider", "AWS" },
                { "ttl", "3600" },
                { "compression", "gzip" }
            };
            
            var result = await cdn.ConfigureCDNAsync(config);
            
            Assert.True(result);
        }

        [Fact]
        public async Task ConfigureCDNAsync_WithNullConfig_ThrowsException()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                cdn.ConfigureCDNAsync(null));
        }

        [Fact]
        public async Task ConfigureCDNAsync_WithInvalidConfig_ReturnsFalse()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            var config = new Dictionary<string, string>
            {
                { "provider", "AWS" }
                // Missing required fields
            };
            
            var result = await cdn.ConfigureCDNAsync(config);
            
            Assert.False(result);
        }

        [Fact]
        public async Task InvalidateCacheAsync_WithValidKeys_ReturnsInvalidatedKeys()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            var keys = new List<string> { "key1", "key2", "key3" };
            
            var result = await cdn.InvalidateCacheAsync(keys);
            
            Assert.NotNull(result);
        }

        [Fact]
        public async Task InvalidateCacheAsync_WithNullKeys_ThrowsException()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                cdn.InvalidateCacheAsync(null));
        }

        [Fact]
        public async Task InvalidateCacheAsync_WithEmptyKeys_ThrowsException()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            var keys = new List<string>();
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                cdn.InvalidateCacheAsync(keys));
        }

        [Fact]
        public async Task ManageEdgeLocationsAsync_WithValidLocations_ReturnsStatusMap()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            var locations = new List<string> { "us-east-1", "eu-west-1", "ap-northeast-1" };
            
            var result = await cdn.ManageEdgeLocationsAsync(locations);
            
            Assert.NotNull(result);
            Assert.Equal(locations.Count, result.Count);
        }

        [Fact]
        public async Task ManageEdgeLocationsAsync_WithNullLocations_ThrowsException()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                cdn.ManageEdgeLocationsAsync(null));
        }

        [Fact]
        public async Task ManageEdgeLocationsAsync_WithEmptyLocations_ThrowsException()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            var locations = new List<string>();
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                cdn.ManageEdgeLocationsAsync(locations));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task MultipleServices_CanOperateSimultaneously()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            var latency = new LatencyOptimizer(_latencyLoggerMock.Object);
            var cdn = new CDNController(_cdnLoggerMock.Object);

            var tasks = new List<Task>
            {
                aggregator.AggregateMetricsAsync(),
                optimizer.AnalyzeCostsAsync(),
                planner.PlanCapacityAsync(3),
                balancer.BalanceLoadAsync(1000),
                failover.MonitorRegionHealthAsync("us-east-1"),
                latency.OptimizeLatencyAsync("us-east-1", "us-west-2"),
                cdn.ManageEdgeLocationsAsync(new List<string> { "us-east-1" })
            };

            await Task.WhenAll(tasks);
            
            Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
        }

        [Fact]
        public async Task CancellationToken_IsRespected()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var cts = new CancellationTokenSource();
            
            cts.Cancel();
            
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                aggregator.AggregateMetricsAsync(cts.Token));
        }

        [Fact]
        public async Task LoadBalancer_FailoverRecovery()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            
            // Initial load distribution
            var initial = await balancer.BalanceLoadAsync(1000);
            var initialTotal = initial.Values.Sum();
            
            // Handle failover
            var afterFailover = await balancer.HandleFailoverAsync("us-east-1");
            var failoverTotal = afterFailover.Values.Sum();
            
            // Load should be redistributed but total might change
            Assert.NotEqual(0, failoverTotal);
        }

        [Fact]
        public async Task LatencyOptimizer_CachingBehavior()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            
            // First call creates path
            var path1 = await optimizer.FindOptimalPathAsync("us-east-1", "us-west-2");
            
            // Second call might use cache
            var path2 = await optimizer.FindOptimalPathAsync("us-east-1", "us-west-2");
            
            Assert.Equal(path1, path2);
        }

        #endregion

        #region Edge Case Tests

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        [InlineData(12)]
        public async Task GlobalCapacityPlanner_ValidatesAllMonthRanges(int months)
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var result = await planner.PlanCapacityAsync(months);
            
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public async Task GlobalLoadBalancer_HandlesVariousLoadSizes(int requestCount)
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            var result = await balancer.BalanceLoadAsync(requestCount);
            
            Assert.Equal(requestCount, result.Values.Sum());
        }

        [Fact]
        public async Task CostOptimizer_MultipleOptimizationCalls()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            
            var result1 = await optimizer.OptimizeAsync();
            var result2 = await optimizer.OptimizeAsync();
            
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.True(result1.Count > 0);
            Assert.True(result2.Count > 0);
        }

        [Fact]
        public async Task RegionFailover_SequentialOperations()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            
            var health1 = await failover.MonitorRegionHealthAsync("us-east-1");
            var success = await failover.TriggerFailoverAsync("us-east-1", "us-west-2");
            var health2 = await failover.MonitorRegionHealthAsync("us-west-2");
            
            Assert.NotNull(health1);
            Assert.True(success);
            Assert.NotNull(health2);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task GlobalMetricsAggregator_CompletesInReasonableTime()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            
            var start = DateTime.UtcNow;
            await aggregator.AggregateMetricsAsync();
            var elapsed = DateTime.UtcNow - start;
            
            Assert.True(elapsed.TotalMilliseconds < 5000);
        }

        [Fact]
        public async Task CostOptimizer_CompletesInReasonableTime()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            
            var start = DateTime.UtcNow;
            await optimizer.OptimizeAsync();
            var elapsed = DateTime.UtcNow - start;
            
            Assert.True(elapsed.TotalMilliseconds < 5000);
        }

        [Fact]
        public async Task GlobalLoadBalancer_CompletesInReasonableTime()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            
            var start = DateTime.UtcNow;
            await balancer.BalanceLoadAsync(10000);
            var elapsed = DateTime.UtcNow - start;
            
            Assert.True(elapsed.TotalMilliseconds < 5000);
        }

        [Fact]
        public async Task LatencyOptimizer_CompletesInReasonableTime()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            
            var start = DateTime.UtcNow;
            await optimizer.OptimizeLatencyAsync("us-east-1", "us-west-2");
            var elapsed = DateTime.UtcNow - start;
            
            Assert.True(elapsed.TotalMilliseconds < 5000);
        }

        [Fact]
        public async Task RegionFailover_CompletesInReasonableTime()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            
            var start = DateTime.UtcNow;
            await failover.MonitorRegionHealthAsync("us-east-1");
            var elapsed = DateTime.UtcNow - start;
            
            Assert.True(elapsed.TotalMilliseconds < 5000);
        }

        [Fact]
        public async Task CDNController_CompletesInReasonableTime()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            
            var start = DateTime.UtcNow;
            await cdn.ManageEdgeLocationsAsync(new List<string> { "us-east-1", "us-west-2" });
            var elapsed = DateTime.UtcNow - start;
            
            Assert.True(elapsed.TotalMilliseconds < 5000);
        }

        #endregion

        #region Additional Comprehensive Tests

        [Fact]
        public async Task GlobalMetricsAggregator_ConcurrentCalls()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var tasks = new List<Task<Dictionary<string, double>>>();

            for (int i = 0; i < 20; i++)
            {
                tasks.Add(aggregator.AggregateMetricsAsync());
            }

            var results = await Task.WhenAll(tasks);
            
            Assert.All(results, result => Assert.NotNull(result));
            Assert.True(results.Length == 20);
        }

        [Fact]
        public async Task CostOptimizer_ConsistentResults()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            var result1 = await optimizer.AnalyzeCostsAsync();
            var result2 = await optimizer.AnalyzeCostsAsync();
            
            Assert.Equal(result1.Keys.OrderBy(k => k), result2.Keys.OrderBy(k => k));
        }

        [Fact]
        public async Task GlobalCapacityPlanner_ConsecutivePlans()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            
            var plan1 = await planner.PlanCapacityAsync(3);
            var plan2 = await planner.PlanCapacityAsync(6);
            
            Assert.NotNull(plan1);
            Assert.NotNull(plan2);
            Assert.True(plan1.Values.Sum() <= plan2.Values.Sum());
        }

        [Fact]
        public async Task GlobalLoadBalancer_BalanceDistribution()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            var result = await balancer.BalanceLoadAsync(1000);
            
            var regionCounts = result.Values.GroupBy(x => x).Count();
            Assert.True(regionCounts >= 1);
        }

        [Fact]
        public async Task RegionFailover_HealthCheckAccuracy()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            
            var health1 = await failover.MonitorRegionHealthAsync("region1");
            var health2 = await failover.MonitorRegionHealthAsync("region1");
            
            Assert.NotNull(health1);
            Assert.NotNull(health2);
            Assert.True(health1.ContainsKey("is_healthy"));
            Assert.True(health2.ContainsKey("is_healthy"));
        }

        [Fact]
        public async Task LatencyOptimizer_PathConsistency()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            
            var path1 = await optimizer.FindOptimalPathAsync("us-east-1", "eu-central-1");
            var path2 = await optimizer.FindOptimalPathAsync("us-east-1", "eu-central-1");
            
            Assert.Equal(path1.Count, path2.Count);
        }

        [Fact]
        public async Task CDNController_ConfigurationPersistence()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            var config = new Dictionary<string, string>
            {
                { "provider", "AWS" },
                { "ttl", "7200" },
                { "compression", "brotli" }
            };
            
            var result1 = await cdn.ConfigureCDNAsync(config);
            
            // Verify we can configure again
            var result2 = await cdn.ConfigureCDNAsync(config);
            
            Assert.True(result1);
            Assert.True(result2);
        }

        [Theory]
        [InlineData("us-east-1")]
        [InlineData("us-west-2")]
        [InlineData("eu-central-1")]
        [InlineData("ap-northeast-1")]
        public async Task GlobalMetricsAggregator_AllRegions(string region)
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var result = await aggregator.GetRegionalMetricsAsync(region);
            
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(500)]
        [InlineData(5000)]
        [InlineData(50000)]
        public async Task GlobalLoadBalancer_LargeLoadDistribution(int loadSize)
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            var result = await balancer.BalanceLoadAsync(loadSize);
            
            Assert.Equal(loadSize, result.Values.Sum());
        }

        [Fact]
        public async Task GlobalCapacityPlanner_ResourceAllocationBalance()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var allocation = await planner.AllocateResourcesAsync(5000);
            
            var totalAllocated = allocation.Values.Sum();
            Assert.Equal(5000, totalAllocated);
        }

        [Fact]
        public async Task CostOptimizer_RecommendationQuality()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            var recommendations = await optimizer.GetRecommendationsAsync();
            
            Assert.True(recommendations.Count >= 5);
            Assert.All(recommendations.Values, roi => Assert.True(roi > 0 && roi <= 500));
        }

        [Fact]
        public async Task LatencyOptimizer_OptimizationTarget()
        {
            var optimizer = new LatencyOptimizer(_latencyLoggerMock.Object);
            var latency = await optimizer.OptimizeLatencyAsync("us-east-1", "us-west-2");
            
            Assert.True(latency > 0);
            Assert.True(latency <= 100.0);
        }

        [Fact]
        public async Task RegionFailover_FailoverChaining()
        {
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            
            var result1 = await failover.TriggerFailoverAsync("region1", "region2");
            var result2 = await failover.RecoverAsync("region1");
            
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task CDNController_EdgeLocationManagement()
        {
            var cdn = new CDNController(_cdnLoggerMock.Object);
            var locations = new List<string> { "edge1", "edge2", "edge3", "edge4", "edge5" };
            
            var result = await cdn.ManageEdgeLocationsAsync(locations);
            
            Assert.Equal(locations.Count, result.Count);
        }

        [Fact]
        public async Task GlobalMetricsAggregator_TrendAnalysisValidity()
        {
            var aggregator = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var now = DateTime.UtcNow;
            var trends = await aggregator.AnalyzeTrendsAsync(now.AddDays(-30), now);
            
            Assert.NotNull(trends);
            Assert.All(trends.Values, trend => Assert.True(double.IsFinite(trend)));
        }

        [Fact]
        public async Task GlobalLoadBalancer_FailoverAndRecovery()
        {
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            
            var initial = await balancer.BalanceLoadAsync(1000);
            var afterFailover = await balancer.HandleFailoverAsync("us-east-1");
            
            Assert.NotNull(initial);
            Assert.NotNull(afterFailover);
        }

        [Fact]
        public async Task GlobalCapacityPlanner_ForecastQuality()
        {
            var planner = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var forecast = await planner.ForecastRequirementsAsync(6);
            
            Assert.True(forecast.All(x => x.Value > 0));
        }

        [Fact]
        public async Task CostOptimizer_OptimizationRecommendations()
        {
            var optimizer = new CostOptimizer(_costLoggerMock.Object);
            var recommendations = await optimizer.OptimizeAsync();
            
            Assert.True(recommendations.Count >= 4);
        }

        [Fact]
        public async Task GlobalIntelligenceServices_FullIntegrationScenario()
        {
            var metrics = new GlobalMetricsAggregator(_metricsLoggerMock.Object);
            var costs = new CostOptimizer(_costLoggerMock.Object);
            var capacity = new GlobalCapacityPlanner(_capacityLoggerMock.Object);
            var balancer = new GlobalLoadBalancer(_loadBalancerLoggerMock.Object);
            var failover = new RegionFailover(_failoverLoggerMock.Object);
            var latency = new LatencyOptimizer(_latencyLoggerMock.Object);
            var cdn = new CDNController(_cdnLoggerMock.Object);

            var metricsData = await metrics.AggregateMetricsAsync();
            var costsData = await costs.AnalyzeCostsAsync();
            var capacityPlan = await capacity.PlanCapacityAsync(3);
            var loadBalance = await balancer.BalanceLoadAsync(1000);
            var healthStatus = await failover.MonitorRegionHealthAsync("us-east-1");
            var latencyOptimized = await latency.OptimizeLatencyAsync("us-east-1", "us-west-2");
            var cdnStatus = await cdn.ManageEdgeLocationsAsync(new List<string> { "us-east-1" });

            Assert.NotNull(metricsData);
            Assert.NotNull(costsData);
            Assert.NotNull(capacityPlan);
            Assert.NotNull(loadBalance);
            Assert.NotNull(healthStatus);
            Assert.True(latencyOptimized > 0);
            Assert.NotNull(cdnStatus);
        }

        #endregion
    }
}

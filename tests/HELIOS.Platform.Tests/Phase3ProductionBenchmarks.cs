using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Core;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Core.Production.Interfaces;
using HELIOS.Platform.Core.Production.Services;

namespace HELIOS.Platform.Tests;

/// <summary>
/// Performance benchmarks for Phase 3 Tier 4 Production Hardening Services.
/// Verifies all services meet their performance targets under various load conditions.
/// </summary>
public class Phase3ProductionBenchmarks
{
    private readonly ILogger _logger = new ConsoleLogger();

    // ==================== PERFORMANCE BENCHMARKS ====================

    /// <summary>
    /// Benchmark: Distributed Cache - Single operations average performance
    /// Target: &lt;2ms per operation
    /// </summary>
    [Fact]
    public async Task BenchmarkDistributedCache_SingleOperations()
    {
        var cache = new DistributedCacheLayer(_logger);
        var stopwatch = new Stopwatch();
        var measurements = new List<long>();

        // Warm up
        await cache.SetAsync("warmup", "data");

        // Measure 1000 operations
        for (int i = 0; i < 1000; i++)
        {
            stopwatch.Restart();
            await cache.SetAsync($"key-{i}", $"value-{i}");
            stopwatch.Stop();
            measurements.Add(stopwatch.ElapsedMilliseconds);
        }

        var avg = measurements.Average();
        var max = measurements.Max();
        var p99 = measurements.OrderByDescending(x => x).Skip((int)(measurements.Count * 0.01)).First();

        Assert.True(avg < 2, $"Average: {avg}ms (target: <2ms)");
        Assert.True(max < 10, $"Max: {max}ms (target: <10ms)");
        
        Console.WriteLine($"Cache Benchmark: Avg={avg:F3}ms, Max={max}ms, P99={p99}ms");
    }

    /// <summary>
    /// Benchmark: Distributed Cache - Concurrent operations
    /// Ensures thread-safety doesn't significantly impact performance
    /// </summary>
    [Fact]
    public async Task BenchmarkDistributedCache_ConcurrentOperations()
    {
        var cache = new DistributedCacheLayer(_logger);
        var stopwatch = Stopwatch.StartNew();

        // Perform 5000 concurrent operations (500 tasks x 10 ops each)
        var tasks = Enumerable.Range(0, 500).Select(async i =>
        {
            for (int j = 0; j < 10; j++)
            {
                await cache.SetAsync($"key-{i}-{j}", $"value-{i}-{j}");
            }
        }).ToList();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var opsPerMs = 5000.0 / stopwatch.ElapsedMilliseconds;
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Total time: {stopwatch.ElapsedMilliseconds}ms");
        
        Console.WriteLine($"Concurrent Cache: {stopwatch.ElapsedMilliseconds}ms for 5000 ops ({opsPerMs:F0} ops/ms)");
    }

    /// <summary>
    /// Benchmark: Query Plan Analyzer - Single query optimization
    /// Target: &lt;30ms per operation
    /// </summary>
    [Fact]
    public async Task BenchmarkQueryPlanAnalyzer_SingleQueries()
    {
        var analyzer = new QueryPlanAnalyzer(_logger);
        var queries = new[]
        {
            "SELECT * FROM Users",
            "SELECT id, name, email FROM Users WHERE status = 'Active'",
            "SELECT u.*, o.* FROM Users u INNER JOIN Orders o ON u.id = o.user_id WHERE u.created_at > '2024-01-01'",
            "SELECT COUNT(*) FROM Transactions WHERE amount > 1000",
            "SELECT * FROM Products ORDER BY price DESC LIMIT 10"
        };

        var measurements = new List<long>();
        var stopwatch = new Stopwatch();

        // Warm up
        await analyzer.OptimizeQueryAsync("SELECT 1");

        // Measure optimization for 1000 queries
        for (int i = 0; i < 1000; i++)
        {
            var query = queries[i % queries.Length];
            stopwatch.Restart();
            await analyzer.OptimizeQueryAsync(query);
            stopwatch.Stop();
            measurements.Add(stopwatch.ElapsedMilliseconds);
        }

        var avg = measurements.Average();
        var max = measurements.Max();

        Assert.True(avg < 30, $"Average: {avg}ms (target: <30ms)");
        
        Console.WriteLine($"Query Optimizer: Avg={avg:F3}ms, Max={max}ms");
    }

    /// <summary>
    /// Benchmark: Load Balancer - Server selection under load
    /// Target: &lt;10ms per operation
    /// </summary>
    [Fact]
    public async Task BenchmarkLoadBalancer_ServerSelection()
    {
        var lb = new ProductionLoadBalancer(_logger);

        // Register 50 servers
        for (int i = 0; i < 50; i++)
        {
            await lb.RegisterServerAsync($"server-{i}", $"http://server-{i}:8080");
        }

        var measurements = new List<long>();
        var stopwatch = new Stopwatch();

        // Warm up
        await lb.GetNextServerAsync("warmup");

        // Measure 10000 selections
        for (int i = 0; i < 10000; i++)
        {
            stopwatch.Restart();
            await lb.GetNextServerAsync($"request-{i}");
            stopwatch.Stop();
            measurements.Add(stopwatch.ElapsedMilliseconds);
        }

        var avg = measurements.Average();
        var max = measurements.Max();

        Assert.True(avg < 10, $"Average: {avg}ms (target: <10ms)");
        
        Console.WriteLine($"Load Balancer: Avg={avg:F3}ms, Max={max}ms");
    }

    /// <summary>
    /// Benchmark: Zero-Trust - Authentication and policy evaluation
    /// Target: &lt;20ms per operation
    /// </summary>
    [Fact]
    public async Task BenchmarkZeroTrust_AuthenticationAndPolicy()
    {
        var zeroTrust = new ZeroTrustImplementation(_logger);
        var measurements = new List<long>();
        var stopwatch = new Stopwatch();

        // Warm up
        await zeroTrust.AuthenticateAsync("test@example.com", "token");

        // Measure 1000 combined operations
        for (int i = 0; i < 1000; i++)
        {
            stopwatch.Restart();
            var auth = await zeroTrust.AuthenticateAsync($"user-{i}@example.com", "token");
            if (auth)
            {
                await zeroTrust.EvaluatePolicyAsync(new AccessRequest 
                { 
                    UserId = $"user-{i}", 
                    Resource = $"/api/resource/{i}" 
                });
            }
            stopwatch.Stop();
            measurements.Add(stopwatch.ElapsedMilliseconds);
        }

        var avg = measurements.Average();
        var max = measurements.Max();

        Assert.True(avg < 20, $"Average: {avg}ms (target: <20ms)");
        
        Console.WriteLine($"Zero-Trust: Avg={avg:F3}ms, Max={max}ms");
    }

    /// <summary>
    /// Benchmark: Disaster Recovery - Backup creation
    /// Target: &lt;500ms per operation
    /// </summary>
    [Fact]
    public async Task BenchmarkDisasterRecovery_BackupCreation()
    {
        var dr = new DisasterRecoveryOrchestrator(_logger);
        var measurements = new List<long>();
        var stopwatch = new Stopwatch();

        // Warm up
        await dr.CreateBackupAsync("warmup");

        // Measure 100 backup creations
        for (int i = 0; i < 100; i++)
        {
            stopwatch.Restart();
            await dr.CreateBackupAsync($"backup-{i}");
            stopwatch.Stop();
            measurements.Add(stopwatch.ElapsedMilliseconds);
        }

        var avg = measurements.Average();
        var max = measurements.Max();

        Assert.True(avg < 500, $"Average: {avg}ms (target: <500ms)");
        
        Console.WriteLine($"Disaster Recovery: Avg={avg:F3}ms, Max={max}ms");
    }

    // ==================== SCALABILITY BENCHMARKS ====================

    /// <summary>
    /// Benchmark: All services under high concurrent load
    /// Simulates production-like traffic pattern
    /// </summary>
    [Fact]
    public async Task BenchmarkAllServices_HighLoad()
    {
        var cache = new DistributedCacheLayer(_logger);
        var analyzer = new QueryPlanAnalyzer(_logger);
        var lb = new ProductionLoadBalancer(_logger);
        var zeroTrust = new ZeroTrustImplementation(_logger);
        var dr = new DisasterRecoveryOrchestrator(_logger);

        // Register servers
        for (int i = 0; i < 10; i++)
        {
            await lb.RegisterServerAsync($"server-{i}", $"http://server-{i}:8080");
        }

        var stopwatch = Stopwatch.StartNew();
        var operationCount = 0;

        // Run for 5 seconds simulating high load
        var tasks = new List<Task>();
        while (stopwatch.ElapsedMilliseconds < 5000)
        {
            // Mix of operations
            tasks.Add(cache.SetAsync($"key-{operationCount}", $"value-{operationCount}"));
            tasks.Add(analyzer.OptimizeQueryAsync($"SELECT * FROM Table{operationCount}"));
            tasks.Add(lb.GetNextServerAsync($"req-{operationCount}"));
            tasks.Add(zeroTrust.AuthenticateAsync($"user-{operationCount}", "token"));
            
            operationCount += 4;

            if (tasks.Count >= 100)
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var throughput = (operationCount * 1000.0) / stopwatch.ElapsedMilliseconds;
        
        Console.WriteLine($"High Load Test: {operationCount} operations in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Throughput: {throughput:F0} ops/sec");
    }

    /// <summary>
    /// Benchmark: Cache efficiency metrics
    /// Measures cache hit rate and memory usage patterns
    /// </summary>
    [Fact]
    public async Task BenchmarkCache_EfficiencyMetrics()
    {
        var cache = new DistributedCacheLayer(_logger);
        var hits = 0;
        var misses = 0;

        // Pre-populate cache with 100 keys
        for (int i = 0; i < 100; i++)
        {
            await cache.SetAsync($"key-{i}", $"value-{i}");
        }

        // Access pattern: 80% of accesses go to 20% of keys (Pareto principle)
        var random = new Random(42);
        for (int i = 0; i < 10000; i++)
        {
            int keyNum;
            if (random.NextDouble() < 0.8)
            {
                keyNum = random.Next(20); // 20% of keys get 80% of traffic
            }
            else
            {
                keyNum = random.Next(100); // Rest of keys
            }

            var value = await cache.GetAsync($"key-{keyNum}");
            if (value != null)
                hits++;
            else
                misses++;
        }

        var hitRate = (hits * 100.0) / (hits + misses);
        
        Assert.True(hitRate > 90, $"Hit rate: {hitRate:F1}% (expected >90%)");
        
        Console.WriteLine($"Cache Hit Rate: {hitRate:F1}% ({hits} hits, {misses} misses)");
    }

    /// <summary>
    /// Benchmark: Stress test - Maximum concurrent operations
    /// Tests system stability under extreme conditions
    /// </summary>
    [Fact]
    public async Task BenchmarkStress_MaximumConcurrency()
    {
        var cache = new DistributedCacheLayer(_logger);
        var stopwatch = Stopwatch.StartNew();
        var taskCount = 10000;
        var tasks = new List<Task>();

        for (int i = 0; i < taskCount; i++)
        {
            int index = i;
            tasks.Add(cache.SetAsync($"stress-{index}", $"data-{index}"));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var throughput = (taskCount * 1000.0) / stopwatch.ElapsedMilliseconds;
        
        Assert.True(stopwatch.ElapsedMilliseconds < 60000, "Should complete within 60 seconds");
        
        Console.WriteLine($"Stress Test: {taskCount} operations in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Throughput: {throughput:F0} ops/sec");
    }
}

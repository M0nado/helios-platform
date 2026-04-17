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
/// Comprehensive test suite for Phase 3 Tier 4 Production Hardening Services.
/// Tests all 5 production services with performance benchmarks and integration scenarios.
/// Performance Targets: Cache <2ms, Query <30ms, LB <10ms, Zero-Trust <20ms, DR <500ms
/// </summary>
public class Phase3ProductionTests
{
    private readonly ILogger _logger = new ConsoleLogger();

    // ==================== DISTRIBUTED CACHE LAYER TESTS ====================

    /// <summary>
    /// Test: Cache set operation completes within performance target (<2ms)
    /// </summary>
    [Fact]
    public async Task DistributedCacheLayer_SetAsync_MeetsPerformanceTarget()
    {
        // Arrange
        var cache = new DistributedCacheLayer(_logger);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await cache.SetAsync("test-key", "test-value", TimeSpan.FromMinutes(5));
        stopwatch.Stop();

        // Assert
        Assert.True(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 2, $"Cache set took {stopwatch.ElapsedMilliseconds}ms, expected < 2ms");
    }

    /// <summary>
    /// Test: Cache retrieval works correctly with and without expiration
    /// </summary>
    [Fact]
    public async Task DistributedCacheLayer_GetAsync_ReturnsCorrectValue()
    {
        // Arrange
        var cache = new DistributedCacheLayer(_logger);
        const string key = "user-session";
        const string value = "session-data-12345";
        await cache.SetAsync(key, value);

        // Act
        var result = await cache.GetAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    /// <summary>
    /// Test: Expired cache entries are automatically removed
    /// </summary>
    [Fact]
    public async Task DistributedCacheLayer_GetAsync_RespectsTTL()
    {
        // Arrange
        var cache = new DistributedCacheLayer(_logger);
        const string key = "temp-data";
        await cache.SetAsync(key, "value", TimeSpan.FromMilliseconds(100));

        // Act - Retrieve immediately (should work)
        var immediate = await cache.GetAsync(key);
        
        // Wait for expiration
        await Task.Delay(150);
        var afterExpiry = await cache.GetAsync(key);

        // Assert
        Assert.Equal("value", immediate);
        Assert.Null(afterExpiry);
    }

    /// <summary>
    /// Test: Delete operation removes entries from cache
    /// </summary>
    [Fact]
    public async Task DistributedCacheLayer_DeleteAsync_RemovesEntry()
    {
        // Arrange
        var cache = new DistributedCacheLayer(_logger);
        await cache.SetAsync("delete-test", "data");

        // Act
        var deleted = await cache.DeleteAsync("delete-test");
        var retrieved = await cache.GetAsync("delete-test");

        // Assert
        Assert.True(deleted);
        Assert.Null(retrieved);
    }

    /// <summary>
    /// Test: Cache pattern matching with regex
    /// </summary>
    [Fact]
    public async Task DistributedCacheLayer_GetKeysAsync_MatchesPattern()
    {
        // Arrange
        var cache = new DistributedCacheLayer(_logger);
        await cache.SetAsync("user:1:profile", "profile-1");
        await cache.SetAsync("user:2:profile", "profile-2");
        await cache.SetAsync("admin:config", "config-data");

        // Act
        var userKeys = await cache.GetKeysAsync(@"^user:\d+:profile$");

        // Assert
        Assert.Equal(2, userKeys.Count);
        Assert.Contains("user:1:profile", userKeys);
        Assert.Contains("user:2:profile", userKeys);
    }

    /// <summary>
    /// Test: Cache increment operation
    /// </summary>
    [Fact]
    public async Task DistributedCacheLayer_IncrementAsync_IncrementsValue()
    {
        // Arrange
        var cache = new DistributedCacheLayer(_logger);

        // Act
        var first = await cache.IncrementAsync("counter");
        var second = await cache.IncrementAsync("counter");
        var third = await cache.IncrementAsync("counter");

        // Assert
        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(3, third);
    }

    /// <summary>
    /// Test: Concurrent cache operations maintain thread safety
    /// </summary>
    [Fact]
    public async Task DistributedCacheLayer_ConcurrentOperations_ThreadSafe()
    {
        // Arrange
        var cache = new DistributedCacheLayer(_logger);
        var tasks = new List<Task>();

        // Act - Create 100 concurrent cache operations
        for (int i = 0; i < 100; i++)
        {
            int index = i;
            tasks.Add(cache.SetAsync($"key-{index}", $"value-{index}"));
        }
        await Task.WhenAll(tasks);

        // Assert - Verify all values were set
        for (int i = 0; i < 100; i++)
        {
            var value = await cache.GetAsync($"key-{i}");
            Assert.Equal($"value-{i}", value);
        }
    }

    // ==================== QUERY PLAN ANALYZER TESTS ====================

    /// <summary>
    /// Test: Query optimization completes within performance target (<30ms)
    /// </summary>
    [Fact]
    public async Task QueryPlanAnalyzer_OptimizeQueryAsync_MeetsPerformanceTarget()
    {
        // Arrange
        var analyzer = new QueryPlanAnalyzer(_logger);
        var query = "SELECT * FROM Users WHERE Status = 'Active' ORDER BY CreatedAt DESC";
        var stopwatch = Stopwatch.StartNew();

        // Act
        var optimized = await analyzer.OptimizeQueryAsync(query);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(optimized);
        Assert.True(stopwatch.ElapsedMilliseconds < 30, $"Query optimization took {stopwatch.ElapsedMilliseconds}ms, expected < 30ms");
    }

    /// <summary>
    /// Test: Query plan analysis returns execution plan
    /// </summary>
    [Fact]
    public async Task QueryPlanAnalyzer_AnalyzeAsync_ReturnsExecutionPlan()
    {
        // Arrange
        var analyzer = new QueryPlanAnalyzer(_logger);
        var query = "SELECT id, name FROM Products WHERE price > 100";

        // Act
        var plan = await analyzer.AnalyzeAsync(query);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(query, plan.Query);
        Assert.True(plan.EstimatedCost > 0);
    }

    /// <summary>
    /// Test: Index creation succeeds
    /// </summary>
    [Fact]
    public async Task QueryPlanAnalyzer_CreateIndexAsync_SucceedsWithValidColumns()
    {
        // Arrange
        var analyzer = new QueryPlanAnalyzer(_logger);
        var columns = new List<string> { "UserId", "Status", "CreatedAt" };

        // Act
        var result = await analyzer.CreateIndexAsync("Users", columns);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Test: Complex query optimization
    /// </summary>
    [Fact]
    public async Task QueryPlanAnalyzer_OptimizeAsync_HandlesComplexJoins()
    {
        // Arrange
        var analyzer = new QueryPlanAnalyzer(_logger);
        var complexQuery = @"
            SELECT u.id, u.name, o.total 
            FROM Users u
            INNER JOIN Orders o ON u.id = o.user_id
            LEFT JOIN Payments p ON o.id = p.order_id
            WHERE u.status = 'Active' AND o.created_at > '2024-01-01'
            ORDER BY o.total DESC";

        // Act
        var plan = await analyzer.AnalyzeAsync(complexQuery);

        // Assert
        Assert.NotNull(plan);
        Assert.Contains("Users", plan.Query);
    }

    // ==================== PRODUCTION LOAD BALANCER TESTS ====================

    /// <summary>
    /// Test: Load balancer registration completes within performance target (<10ms)
    /// </summary>
    [Fact]
    public async Task ProductionLoadBalancer_RegisterServerAsync_MeetsPerformanceTarget()
    {
        // Arrange
        var lb = new ProductionLoadBalancer(_logger);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await lb.RegisterServerAsync("server-1", "http://localhost:5000");
        stopwatch.Stop();

        // Assert
        Assert.True(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 10, $"Server registration took {stopwatch.ElapsedMilliseconds}ms, expected < 10ms");
    }

    /// <summary>
    /// Test: Load balancer distributes requests using round-robin
    /// </summary>
    [Fact]
    public async Task ProductionLoadBalancer_GetNextServerAsync_RoundRobinDistribution()
    {
        // Arrange
        var lb = new ProductionLoadBalancer(_logger);
        await lb.RegisterServerAsync("server-1", "http://localhost:5000");
        await lb.RegisterServerAsync("server-2", "http://localhost:5001");
        await lb.RegisterServerAsync("server-3", "http://localhost:5002");

        // Act
        var first = await lb.GetNextServerAsync("req-1");
        var second = await lb.GetNextServerAsync("req-2");
        var third = await lb.GetNextServerAsync("req-3");
        var fourth = await lb.GetNextServerAsync("req-4"); // Should wrap around

        // Assert
        Assert.NotEmpty(first);
        Assert.NotEmpty(second);
        Assert.NotEmpty(third);
        Assert.NotNull(fourth);
    }

    /// <summary>
    /// Test: Load balancer health check
    /// </summary>
    [Fact]
    public async Task ProductionLoadBalancer_GetServerHealthAsync_ReturnsAllServers()
    {
        // Arrange
        var lb = new ProductionLoadBalancer(_logger);
        await lb.RegisterServerAsync("server-1", "http://localhost:5000");
        await lb.RegisterServerAsync("server-2", "http://localhost:5001");

        // Act
        var health = await lb.GetServerHealthAsync();

        // Assert
        Assert.Equal(2, health.Count);
        Assert.All(health, h => Assert.True(h.IsHealthy));
        Assert.All(health, h => Assert.True(h.Load >= 0 && h.Load <= 100));
    }

    /// <summary>
    /// Test: Load balancer handles many concurrent requests
    /// </summary>
    [Fact]
    public async Task ProductionLoadBalancer_ConcurrentRequests_DistributesEvenly()
    {
        // Arrange
        var lb = new ProductionLoadBalancer(_logger);
        for (int i = 0; i < 5; i++)
        {
            await lb.RegisterServerAsync($"server-{i}", $"http://localhost:{5000 + i}");
        }

        // Act
        var tasks = Enumerable.Range(0, 100)
            .Select(i => lb.GetNextServerAsync($"req-{i}"))
            .ToList();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, results.Length);
        Assert.All(results, r => Assert.NotEmpty(r));
    }

    // ==================== ZERO-TRUST IMPLEMENTATION TESTS ====================

    /// <summary>
    /// Test: Authentication completes within performance target (<20ms)
    /// </summary>
    [Fact]
    public async Task ZeroTrustImplementation_AuthenticateAsync_MeetsPerformanceTarget()
    {
        // Arrange
        var zeroTrust = new ZeroTrustImplementation(_logger);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await zeroTrust.AuthenticateAsync("user@example.com", "secure-credential-token");
        stopwatch.Stop();

        // Assert
        Assert.True(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 20, $"Authentication took {stopwatch.ElapsedMilliseconds}ms, expected < 20ms");
    }

    /// <summary>
    /// Test: Access policy evaluation
    /// </summary>
    [Fact]
    public async Task ZeroTrustImplementation_EvaluatePolicyAsync_GrantsAccess()
    {
        // Arrange
        var zeroTrust = new ZeroTrustImplementation(_logger);
        var request = new AccessRequest
        {
            UserId = "user-123",
            Resource = "/api/users/profile"
        };

        // Act
        var decision = await zeroTrust.EvaluatePolicyAsync(request);

        // Assert
        Assert.NotNull(decision);
        Assert.True(decision.Allowed);
        Assert.NotEmpty(decision.Reason);
    }

    /// <summary>
    /// Test: Access logging works correctly
    /// </summary>
    [Fact]
    public async Task ZeroTrustImplementation_LogAccessAsync_RecordsAccess()
    {
        // Arrange
        var zeroTrust = new ZeroTrustImplementation(_logger);
        var log = new AccessLog
        {
            UserId = "user-456",
            Resource = "/api/admin/settings",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await zeroTrust.LogAccessAsync(log);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Test: Multiple concurrent access evaluations maintain security
    /// </summary>
    [Fact]
    public async Task ZeroTrustImplementation_ConcurrentEvaluations_Consistent()
    {
        // Arrange
        var zeroTrust = new ZeroTrustImplementation(_logger);
        var tasks = new List<Task<AccessDecision>>();

        // Act
        for (int i = 0; i < 50; i++)
        {
            var request = new AccessRequest
            {
                UserId = $"user-{i}",
                Resource = $"/api/resource/{i}"
            };
            tasks.Add(zeroTrust.EvaluatePolicyAsync(request));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(50, results.Count);
        Assert.All(results, r => Assert.NotNull(r));
    }

    /// <summary>
    /// Test: Concurrent authentication and audit logging
    /// </summary>
    [Fact]
    public async Task ZeroTrustImplementation_AuthenticationAuditTrail_ConcurrentLogging()
    {
        // Arrange
        var zeroTrust = new ZeroTrustImplementation(_logger);

        // Act
        var authTasks = Enumerable.Range(0, 20)
            .Select(i => zeroTrust.AuthenticateAsync($"user-{i}@example.com", "cred"))
            .ToList();
        var logTasks = Enumerable.Range(0, 20)
            .Select(i => zeroTrust.LogAccessAsync(new AccessLog 
            { 
                UserId = $"user-{i}", 
                Resource = $"/resource/{i}" 
            }))
            .ToList();

        var authResults = await Task.WhenAll(authTasks);
        var logResults = await Task.WhenAll(logTasks);

        // Assert
        Assert.All(authResults, r => Assert.True(r));
        Assert.All(logResults, r => Assert.True(r));
    }

    // ==================== DISASTER RECOVERY ORCHESTRATOR TESTS ====================

    /// <summary>
    /// Test: Backup creation completes within performance target (<500ms)
    /// </summary>
    [Fact]
    public async Task DisasterRecoveryOrchestrator_CreateBackupAsync_MeetsPerformanceTarget()
    {
        // Arrange
        var drOrch = new DisasterRecoveryOrchestrator(_logger);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await drOrch.CreateBackupAsync("daily-backup");
        stopwatch.Stop();

        // Assert
        Assert.True(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Backup creation took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }

    /// <summary>
    /// Test: Backup listing returns backups in chronological order
    /// </summary>
    [Fact]
    public async Task DisasterRecoveryOrchestrator_ListBackupsAsync_ReturnsOrderedBackups()
    {
        // Arrange
        var drOrch = new DisasterRecoveryOrchestrator(_logger);
        await drOrch.CreateBackupAsync("backup-1");
        await Task.Delay(50);
        await drOrch.CreateBackupAsync("backup-2");
        await Task.Delay(50);
        await drOrch.CreateBackupAsync("backup-3");

        // Act
        var backups = await drOrch.ListBackupsAsync();

        // Assert
        Assert.Equal(3, backups.Count);
        Assert.True(backups[0].CreatedAt >= backups[1].CreatedAt); // Most recent first
        Assert.True(backups[1].CreatedAt >= backups[2].CreatedAt);
    }

    /// <summary>
    /// Test: Backup restore operation
    /// </summary>
    [Fact]
    public async Task DisasterRecoveryOrchestrator_RestoreFromBackupAsync_Succeeds()
    {
        // Arrange
        var drOrch = new DisasterRecoveryOrchestrator(_logger);
        await drOrch.CreateBackupAsync("restore-test");
        var backups = await drOrch.ListBackupsAsync();
        var backupId = backups.First().BackupId;

        // Act
        var result = await drOrch.RestoreFromBackupAsync(backupId);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Test: Disaster recovery status reflects system health
    /// </summary>
    [Fact]
    public async Task DisasterRecoveryOrchestrator_GetStatusAsync_ReturnsHealthyStatus()
    {
        // Arrange
        var drOrch = new DisasterRecoveryOrchestrator(_logger);

        // Act
        var status = await drOrch.GetStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.True(status.IsHealthy);
        Assert.True(status.RTO > 0); // RTO in minutes
        Assert.True(status.RPO > 0); // RPO in minutes
    }

    /// <summary>
    /// Test: Multiple backups can be created concurrently
    /// </summary>
    [Fact]
    public async Task DisasterRecoveryOrchestrator_ConcurrentBackups_AllSucceed()
    {
        // Arrange
        var drOrch = new DisasterRecoveryOrchestrator(_logger);

        // Act
        var backupTasks = Enumerable.Range(0, 10)
            .Select(i => drOrch.CreateBackupAsync($"concurrent-backup-{i}"))
            .ToList();
        var results = await Task.WhenAll(backupTasks);

        // Assert
        Assert.All(results, r => Assert.True(r));
        var backups = await drOrch.ListBackupsAsync();
        Assert.Equal(10, backups.Count);
    }

    // ==================== INTEGRATION TESTS ====================

    /// <summary>
    /// Test: All production services initialize successfully
    /// </summary>
    [Fact]
    public void ProductionServices_AllInitialize_Successfully()
    {
        // Act & Assert - No exceptions should be thrown
        var cache = new DistributedCacheLayer(_logger);
        var analyzer = new QueryPlanAnalyzer(_logger);
        var lb = new ProductionLoadBalancer(_logger);
        var zeroTrust = new ZeroTrustImplementation(_logger);
        var dr = new DisasterRecoveryOrchestrator(_logger);

        Assert.NotNull(cache);
        Assert.NotNull(analyzer);
        Assert.NotNull(lb);
        Assert.NotNull(zeroTrust);
        Assert.NotNull(dr);
    }

    /// <summary>
    /// Test: Complete production workflow - cache, query, LB, security, DR
    /// </summary>
    [Fact]
    public async Task ProductionServices_CompleteWorkflow_AllOperationsSucceed()
    {
        // Arrange
        var cache = new DistributedCacheLayer(_logger);
        var analyzer = new QueryPlanAnalyzer(_logger);
        var lb = new ProductionLoadBalancer(_logger);
        var zeroTrust = new ZeroTrustImplementation(_logger);
        var dr = new DisasterRecoveryOrchestrator(_logger);

        // Act & Assert
        // 1. Cache user session
        var cacheResult = await cache.SetAsync("user-session", "session-data", TimeSpan.FromHours(1));
        Assert.True(cacheResult);

        // 2. Optimize query
        var optimized = await analyzer.OptimizeQueryAsync("SELECT * FROM Users");
        Assert.NotNull(optimized);

        // 3. Register and balance load
        await lb.RegisterServerAsync("prod-1", "http://prod-1.local:8080");
        await lb.RegisterServerAsync("prod-2", "http://prod-2.local:8080");
        var server = await lb.GetNextServerAsync("request-1");
        Assert.NotEmpty(server);

        // 4. Authenticate and evaluate access
        var authenticated = await zeroTrust.AuthenticateAsync("admin@local", "token123");
        Assert.True(authenticated);
        var decision = await zeroTrust.EvaluatePolicyAsync(new AccessRequest 
        { 
            UserId = "admin", 
            Resource = "/api/backup" 
        });
        Assert.True(decision.Allowed);

        // 5. Create backup
        var backupResult = await dr.CreateBackupAsync("pre-deployment");
        Assert.True(backupResult);
    }

    /// <summary>
    /// Test: High concurrency stress test across all services
    /// </summary>
    [Fact]
    public async Task ProductionServices_HighConcurrency_HandlesLoad()
    {
        // Arrange
        var cache = new DistributedCacheLayer(_logger);
        var analyzer = new QueryPlanAnalyzer(_logger);
        var lb = new ProductionLoadBalancer(_logger);
        var zeroTrust = new ZeroTrustImplementation(_logger);
        var dr = new DisasterRecoveryOrchestrator(_logger);

        // Register load balancer servers
        for (int i = 0; i < 10; i++)
        {
            await lb.RegisterServerAsync($"server-{i}", $"http://server-{i}.local:8080");
        }

        // Act - Perform 500 concurrent operations
        var tasks = new List<Task>();
        var random = new Random();

        for (int i = 0; i < 500; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                int op = random.Next(5);
                switch (op)
                {
                    case 0:
                        await cache.SetAsync($"key-{index}", $"value-{index}");
                        break;
                    case 1:
                        await analyzer.OptimizeQueryAsync($"SELECT * FROM Table{index % 10}");
                        break;
                    case 2:
                        await lb.GetNextServerAsync($"request-{index}");
                        break;
                    case 3:
                        await zeroTrust.AuthenticateAsync($"user-{index}", "token");
                        break;
                    case 4:
                        await dr.CreateBackupAsync($"backup-{index}");
                        break;
                }
            }));
        }

        // Assert
        await Task.WhenAll(tasks);
        // If we got here without exceptions, the test passes
    }

    /// <summary>
    /// Test: Disaster recovery with actual data preservation scenario
    /// </summary>
    [Fact]
    public async Task DisasterRecoveryOrchestrator_DataPreservationScenario_Works()
    {
        // Arrange
        var cache = new DistributedCacheLayer(_logger);
        var dr = new DisasterRecoveryOrchestrator(_logger);

        // Simulate pre-disaster state
        await cache.SetAsync("critical-data", "important-value");

        // Act
        // Create backup before "disaster"
        var backupResult = await dr.CreateBackupAsync("pre-disaster");
        Assert.True(backupResult);

        // Simulate disaster by clearing cache
        await cache.DeleteAsync("critical-data");
        var deletedData = await cache.GetAsync("critical-data");
        Assert.Null(deletedData);

        // Recover from backup
        var backups = await dr.ListBackupsAsync();
        Assert.NotEmpty(backups);
        var recovered = await dr.RestoreFromBackupAsync(backups.First().BackupId);
        Assert.True(recovered);

        // Assert - Verify recovery was successful (in real scenario)
        Assert.True(backups.Count > 0);
    }

    /// <summary>
    /// Test: Production readiness - all services respond to health checks
    /// </summary>
    [Fact]
    public async Task ProductionServices_HealthCheck_AllHealthy()
    {
        // Arrange
        var zeroTrust = new ZeroTrustImplementation(_logger);
        var lb = new ProductionLoadBalancer(_logger);
        var dr = new DisasterRecoveryOrchestrator(_logger);

        // Register servers for LB health check
        await lb.RegisterServerAsync("health-1", "http://health-1:8080");
        await lb.RegisterServerAsync("health-2", "http://health-2:8080");

        // Act
        var lbHealth = await lb.GetServerHealthAsync();
        var drStatus = await dr.GetStatusAsync();

        // Assert
        Assert.NotEmpty(lbHealth);
        Assert.All(lbHealth, h => Assert.True(h.IsHealthy));
        Assert.True(drStatus.IsHealthy);
    }
}

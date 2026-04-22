using Xunit;
using HELIOS.Platform.Core.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Phase 3 Tier 4 Security & Disaster Recovery Services Tests - 40+ comprehensive tests
    /// </summary>
    public class Phase3Tier4SecurityDisasterRecoveryTests
    {
        #region Distributed Cache Layer Tests (10 tests)

        [Fact]
        public async Task DistributedCacheLayer_SetAsync_StoresValue()
        {
            var cache = new DistributedCacheLayer();
            var result = await cache.SetAsync("key1", "value1");

            Assert.True(result);
        }

        [Fact]
        public async Task DistributedCacheLayer_GetAsync_RetrievesStoredValue()
        {
            var cache = new DistributedCacheLayer();
            await cache.SetAsync("key1", "testvalue");
            var result = await cache.GetAsync("key1");

            Assert.Equal("testvalue", result);
        }

        [Fact]
        public async Task DistributedCacheLayer_GetAsync_ReturnsNullForNonExistentKey()
        {
            var cache = new DistributedCacheLayer();
            var result = await cache.GetAsync("nonexistent");

            Assert.Null(result);
        }

        [Fact]
        public async Task DistributedCacheLayer_DeleteAsync_RemovesKey()
        {
            var cache = new DistributedCacheLayer();
            await cache.SetAsync("key1", "value1");
            var deleted = await cache.DeleteAsync("key1");
            var retrieved = await cache.GetAsync("key1");

            Assert.True(deleted);
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task DistributedCacheLayer_TTLExpiry_RemovesExpiredKey()
        {
            var cache = new DistributedCacheLayer();
            await cache.SetAsync("expiring", "value", 1);
            await Task.Delay(1100);

            var result = await cache.GetAsync("expiring");
            Assert.Null(result);
        }

        [Fact]
        public async Task DistributedCacheLayer_IncrementAsync_IncrementsNumericValue()
        {
            var cache = new DistributedCacheLayer();
            await cache.SetAsync("counter", "5");
            var result = await cache.IncrementAsync("counter");

            Assert.Equal(6L, result);
        }

        [Fact]
        public async Task DistributedCacheLayer_DecrementAsync_DecrementsNumericValue()
        {
            var cache = new DistributedCacheLayer();
            await cache.SetAsync("counter", "10");
            var result = await cache.DecrementAsync("counter");

            Assert.Equal(9L, result);
        }

        [Fact]
        public async Task DistributedCacheLayer_MGetAsync_RetrievesMultipleValues()
        {
            var cache = new DistributedCacheLayer();
            await cache.SetAsync("key1", "value1");
            await cache.SetAsync("key2", "value2");
            var result = await cache.MGetAsync("key1", "key2", "key3");

            Assert.Equal("value1", result["key1"]);
            Assert.Equal("value2", result["key2"]);
            Assert.Null(result["key3"]);
        }

        [Fact]
        public async Task DistributedCacheLayer_LRUEviction_EvictsLeastRecentlyUsed()
        {
            var cache = new DistributedCacheLayer(maxCapacity: 3);
            await cache.SetAsync("key1", "value1");
            await cache.SetAsync("key2", "value2");
            await cache.SetAsync("key3", "value3");
            await cache.SetAsync("key4", "value4"); // Should evict key1 (LRU)

            var key1 = await cache.GetAsync("key1");
            Assert.Null(key1);
        }

        [Fact]
        public async Task DistributedCacheLayer_GetStatisticsAsync_ReportsMetrics()
        {
            var cache = new DistributedCacheLayer();
            await cache.SetAsync("key1", "value1");
            await cache.GetAsync("key1"); // Hit
            await cache.GetAsync("nonexistent"); // Miss
            var stats = await cache.GetStatisticsAsync();

            Assert.True(stats.Hits > 0);
            Assert.True(stats.Misses > 0);
            Assert.True(stats.HitRate > 0);
        }

        #endregion

        #region Query Plan Analyzer Tests (10 tests)

        [Fact]
        public async Task QueryPlanAnalyzer_AnalyzeAsync_ReturnsAnalysisResult()
        {
            var analyzer = new QueryPlanAnalyzer();
            var result = await analyzer.AnalyzeAsync("SELECT * FROM users");

            Assert.NotNull(result);
            Assert.NotEmpty(result.QueryHash);
            Assert.True(result.EstimatedCost >= 0);
        }

        [Fact]
        public async Task QueryPlanAnalyzer_EstimateCostAsync_CalculatesCost()
        {
            var analyzer = new QueryPlanAnalyzer();
            var cost = await analyzer.EstimateCostAsync("SELECT * FROM orders WHERE id = 1");

            Assert.True(cost >= 0);
            Assert.True(cost <= 100);
        }

        [Fact]
        public async Task QueryPlanAnalyzer_IdentifyMissingIndexesAsync_FindsMissingIndexes()
        {
            var analyzer = new QueryPlanAnalyzer();
            var indexes = await analyzer.IdentifyMissingIndexesAsync("SELECT * FROM users WHERE username = 'test'");

            Assert.NotNull(indexes);
            Assert.True(indexes.Count > 0);
        }

        [Fact]
        public async Task QueryPlanAnalyzer_IdentifyMissingIndexesAsync_FindsOrderByIndexes()
        {
            var analyzer = new QueryPlanAnalyzer();
            var indexes = await analyzer.IdentifyMissingIndexesAsync("SELECT * FROM products ORDER BY price ASC");

            Assert.NotNull(indexes);
            var orderIndex = indexes.FirstOrDefault(i => i.SuggestedIndexName.Contains("ORDER"));
            Assert.NotNull(orderIndex);
        }

        [Fact]
        public async Task QueryPlanAnalyzer_GetOptimizationSuggestionsAsync_ReturnsOptimizations()
        {
            var analyzer = new QueryPlanAnalyzer();
            var suggestions = await analyzer.GetOptimizationSuggestionsAsync("SELECT * FROM users");

            Assert.NotNull(suggestions);
            Assert.True(suggestions.Count > 0);
        }

        [Fact]
        public async Task QueryPlanAnalyzer_DetectsSelectStar_SuggestsOptimization()
        {
            var analyzer = new QueryPlanAnalyzer();
            var suggestions = await analyzer.GetOptimizationSuggestionsAsync("SELECT * FROM users");

            var selectStarIssue = suggestions.FirstOrDefault(s => s.Type == "SelectColumn");
            Assert.NotNull(selectStarIssue);
        }

        [Fact]
        public async Task QueryPlanAnalyzer_DetectsMissingWhere_SuggestsOptimization()
        {
            var analyzer = new QueryPlanAnalyzer();
            var suggestions = await analyzer.GetOptimizationSuggestionsAsync(
                "SELECT * FROM orders JOIN users ON orders.user_id = users.id");

            var whereIssue = suggestions.FirstOrDefault(s => s.Type == "Where");
            Assert.NotNull(whereIssue);
        }

        [Fact]
        public async Task QueryPlanAnalyzer_CacheAnalysisResultAsync_StoresCachedResult()
        {
            var analyzer = new QueryPlanAnalyzer();
            var query = "SELECT * FROM users";
            var result = await analyzer.AnalyzeAsync(query);

            var cached = await analyzer.GetCachedAnalysisResultAsync(result.QueryHash);
            Assert.NotNull(cached);
            Assert.Equal(result.QueryHash, cached.QueryHash);
        }

        [Fact]
        public async Task QueryPlanAnalyzer_GetTableStatisticsAsync_ReturnsStats()
        {
            var analyzer = new QueryPlanAnalyzer();
            var stats = await analyzer.GetTableStatisticsAsync("users");

            Assert.NotNull(stats);
            Assert.Equal("users", stats.TableName);
            Assert.True(stats.RowCount > 0);
        }

        [Fact]
        public async Task QueryPlanAnalyzer_EstimateRowsAffected_WithLimitClause()
        {
            var analyzer = new QueryPlanAnalyzer();
            var result1 = await analyzer.AnalyzeAsync("SELECT * FROM orders");
            var result2 = await analyzer.AnalyzeAsync("SELECT * FROM orders LIMIT 10");

            Assert.True(result2.EstimatedRowsAffected <= result1.EstimatedRowsAffected);
        }

        #endregion

        #region Production Load Balancer Tests (10 tests)

        [Fact]
        public async Task ProductionLoadBalancer_RegisterServiceAsync_RegistersService()
        {
            var lb = new ProductionLoadBalancer();
            var result = await lb.RegisterServiceAsync("service1", "http://localhost:5000", 1);

            Assert.True(result);
        }

        [Fact]
        public async Task ProductionLoadBalancer_GetAllServicesAsync_ListsServices()
        {
            var lb = new ProductionLoadBalancer();
            await lb.RegisterServiceAsync("service1", "http://localhost:5000");
            await lb.RegisterServiceAsync("service2", "http://localhost:5001");

            var services = await lb.GetAllServicesAsync();
            Assert.Equal(2, services.Count);
        }

        [Fact]
        public async Task ProductionLoadBalancer_GetNextServiceAsync_DistributesRoundRobin()
        {
            var lb = new ProductionLoadBalancer();
            await lb.RegisterServiceAsync("service1", "http://localhost:5000");
            await lb.RegisterServiceAsync("service2", "http://localhost:5001");

            var endpoint1 = await lb.GetNextServiceAsync();
            var endpoint2 = await lb.GetNextServiceAsync();
            var endpoint3 = await lb.GetNextServiceAsync();

            Assert.NotNull(endpoint1);
            Assert.NotNull(endpoint2);
            Assert.NotNull(endpoint3);
            Assert.NotEqual(endpoint1?.ServiceId, endpoint2?.ServiceId);
        }

        [Fact]
        public async Task ProductionLoadBalancer_ReportHealthAsync_UpdatesHealth()
        {
            var lb = new ProductionLoadBalancer();
            await lb.RegisterServiceAsync("service1", "http://localhost:5000");

            var health = new ServiceHealthStatus
            {
                ServiceId = "service1",
                IsHealthy = false,
                ResponseTimeMs = 5000
            };

            var result = await lb.ReportHealthAsync("service1", health);
            Assert.True(result);

            var retrieved = await lb.GetServiceHealthAsync("service1");
            Assert.False(retrieved.IsHealthy);
        }

        [Fact]
        public async Task ProductionLoadBalancer_GetActiveServicesAsync_FiltersUnhealthy()
        {
            var lb = new ProductionLoadBalancer();
            await lb.RegisterServiceAsync("service1", "http://localhost:5000");
            await lb.RegisterServiceAsync("service2", "http://localhost:5001");

            var unhealthyStatus = new ServiceHealthStatus
            {
                ServiceId = "service1",
                IsHealthy = false
            };
            await lb.ReportHealthAsync("service1", unhealthyStatus);

            var active = await lb.GetActiveServicesAsync();
            Assert.Single(active);
            Assert.Equal("service2", active[0].ServiceId);
        }

        [Fact]
        public async Task ProductionLoadBalancer_UpdateServiceWeightAsync_UpdatesWeight()
        {
            var lb = new ProductionLoadBalancer();
            await lb.RegisterServiceAsync("service1", "http://localhost:5000", 1);
            var result = await lb.UpdateServiceWeightAsync("service1", 5);

            Assert.True(result);
            var services = await lb.GetAllServicesAsync();
            Assert.Equal(5, services[0].Weight);
        }

        [Fact]
        public async Task ProductionLoadBalancer_AcquireConnectionAsync_CreatesConnection()
        {
            var lb = new ProductionLoadBalancer();
            await lb.RegisterServiceAsync("service1", "http://localhost:5000");

            var connection = await lb.AcquireConnectionAsync("service1");
            Assert.NotNull(connection);
            Assert.Equal("service1", connection.ServiceId);
            Assert.True(connection.IsActive);
        }

        [Fact]
        public async Task ProductionLoadBalancer_ReleaseConnectionAsync_ReleasesConnection()
        {
            var lb = new ProductionLoadBalancer();
            await lb.RegisterServiceAsync("service1", "http://localhost:5000");

            var connection = await lb.AcquireConnectionAsync("service1");
            var released = await lb.ReleaseConnectionAsync(connection);

            Assert.True(released);
            Assert.False(connection.IsActive);
        }

        [Fact]
        public async Task ProductionLoadBalancer_GetStatisticsAsync_ReturnsMetrics()
        {
            var lb = new ProductionLoadBalancer();
            await lb.RegisterServiceAsync("service1", "http://localhost:5000");
            await lb.GetNextServiceAsync();

            var stats = await lb.GetStatisticsAsync();
            Assert.True(stats.TotalServices > 0);
            Assert.True(stats.TotalRequestsDistributed > 0);
        }

        [Fact]
        public async Task ProductionLoadBalancer_DeregisterServiceAsync_RemovesService()
        {
            var lb = new ProductionLoadBalancer();
            await lb.RegisterServiceAsync("service1", "http://localhost:5000");
            var removed = await lb.DeregisterServiceAsync("service1");

            Assert.True(removed);
            var services = await lb.GetAllServicesAsync();
            Assert.Empty(services);
        }

        #endregion

        #region Zero-Trust Implementation Tests (10 tests)

        [Fact]
        public async Task ZeroTrustImplementation_VerifyRequestAsync_AllowsAdminByDefault()
        {
            var zeroTrust = new ZeroTrustImplementation();
            var context = new SecurityContext
            {
                PrincipalId = "admin",
                Action = "read",
                ResourceId = "resource1",
                RequestTime = DateTime.UtcNow
            };

            var result = await zeroTrust.VerifyRequestAsync(context);
            Assert.True(result.IsVerified);
        }

        [Fact]
        public async Task ZeroTrustImplementation_VerifyRequestAsync_RejectsUnauthorizedPrincipal()
        {
            var zeroTrust = new ZeroTrustImplementation();
            var context = new SecurityContext
            {
                PrincipalId = "unknown_user",
                Action = "delete",
                ResourceId = "sensitive_data",
                RequestTime = DateTime.UtcNow
            };

            var result = await zeroTrust.VerifyRequestAsync(context);
            Assert.False(result.IsVerified);
        }

        [Fact]
        public async Task ZeroTrustImplementation_ContinuousAuthenticationAsync_VerifiesAuthentication()
        {
            var zeroTrust = new ZeroTrustImplementation();
            var result = await zeroTrust.ContinuousAuthenticationAsync("user1");

            Assert.True(result);
        }

        [Fact]
        public async Task ZeroTrustImplementation_RegisterPolicyAsync_RegistersPolicy()
        {
            var zeroTrust = new ZeroTrustImplementation();
            var policy = new SecurityPolicy
            {
                PolicyId = "test-policy",
                Name = "Test Policy",
                Effect = "Allow",
                Principals = new List<string> { "user1" },
                Resources = new List<string> { "resource1" },
                Actions = new List<string> { "read" },
                IsActive = true
            };

            var result = await zeroTrust.RegisterPolicyAsync(policy);
            Assert.True(result);
        }

        [Fact]
        public async Task ZeroTrustImplementation_ValidateResourceAccessAsync_VerifiesAccess()
        {
            var zeroTrust = new ZeroTrustImplementation();
            var result = await zeroTrust.ValidateResourceAccessAsync("admin", "resource1", "read");

            Assert.True(result);
        }

        [Fact]
        public async Task ZeroTrustImplementation_RecordViolationAsync_LogsViolation()
        {
            var zeroTrust = new ZeroTrustImplementation();
            var violation = new SecurityViolation
            {
                PrincipalId = "suspicious_user",
                ViolationType = "UnauthorizedAccess",
                ResourceId = "sensitive_data",
                Severity = 5
            };

            var result = await zeroTrust.RecordViolationAsync(violation);
            Assert.True(result);
        }

        [Fact]
        public async Task ZeroTrustImplementation_GetRecentViolationsAsync_RetrievesViolations()
        {
            var zeroTrust = new ZeroTrustImplementation();
            var violation = new SecurityViolation
            {
                PrincipalId = "user1",
                ViolationType = "FailedAuth",
                ResourceId = "resource1",
                Severity = 2
            };

            await zeroTrust.RecordViolationAsync(violation);
            var violations = await zeroTrust.GetRecentViolationsAsync();

            Assert.True(violations.Count > 0);
        }

        [Fact]
        public async Task ZeroTrustImplementation_EnforceMfaAsync_RequiresMfa()
        {
            var zeroTrust = new ZeroTrustImplementation();
            var result = await zeroTrust.EnforceMfaAsync("user1");

            Assert.True(result);
        }

        [Fact]
        public async Task ZeroTrustImplementation_ValidateCredentialAsync_ValidatesPassword()
        {
            var zeroTrust = new ZeroTrustImplementation();
            var result = await zeroTrust.ValidateCredentialAsync("user1", "password", "SecurePass123");

            Assert.True(result);
        }

        [Fact]
        public async Task ZeroTrustImplementation_GetMetricsAsync_ReturnsSecurityMetrics()
        {
            var zeroTrust = new ZeroTrustImplementation();
            var context = new SecurityContext
            {
                PrincipalId = "admin",
                Action = "read",
                ResourceId = "resource1",
                RequestTime = DateTime.UtcNow
            };

            await zeroTrust.VerifyRequestAsync(context);
            var metrics = await zeroTrust.GetMetricsAsync();

            Assert.True(metrics.TotalVerificationAttempts > 0);
        }

        #endregion

        #region Disaster Recovery Orchestrator Tests (10 tests)

        [Fact]
        public async Task DisasterRecoveryOrchestrator_InitiateBackupAsync_CreatesBackup()
        {
            var dr = new DisasterRecoveryOrchestrator();
            var backup = await dr.InitiateBackupAsync("backup1", BackupType.Full, new List<string> { "database1" });

            Assert.NotNull(backup);
            Assert.NotEmpty(backup.BackupId);
            Assert.Equal("backup1", backup.BackupName);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_GetBackupStatusAsync_RetrievesBackup()
        {
            var dr = new DisasterRecoveryOrchestrator();
            var backup = await dr.InitiateBackupAsync("backup1", BackupType.Full, new List<string> { "database1" });
            await Task.Delay(500);

            var retrieved = await dr.GetBackupStatusAsync(backup.BackupId);
            Assert.NotNull(retrieved);
            Assert.Equal(backup.BackupId, retrieved.BackupId);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_ListBackupsAsync_ListsBackups()
        {
            var dr = new DisasterRecoveryOrchestrator();
            await dr.InitiateBackupAsync("backup1", BackupType.Full, new List<string> { "database1" });
            await dr.InitiateBackupAsync("backup2", BackupType.Incremental, new List<string> { "database1" });

            var backups = await dr.ListBackupsAsync();
            Assert.True(backups.Count >= 2);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_InitiateRecoveryAsync_CreatesRecovery()
        {
            var dr = new DisasterRecoveryOrchestrator();
            var backup = await dr.InitiateBackupAsync("backup1", BackupType.Full, new List<string> { "database1" });
            await Task.Delay(500);

            var recovery = await dr.InitiateRecoveryAsync(backup.BackupId, RecoveryType.Full, new List<string> { "database1" });
            Assert.NotNull(recovery);
            Assert.Equal(backup.BackupId, recovery.BackupId);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_GetRecoveryStatusAsync_RetrievesRecovery()
        {
            var dr = new DisasterRecoveryOrchestrator();
            var backup = await dr.InitiateBackupAsync("backup1", BackupType.Full, new List<string> { "database1" });
            await Task.Delay(500);

            var recovery = await dr.InitiateRecoveryAsync(backup.BackupId, RecoveryType.Full, new List<string> { "database1" });
            await Task.Delay(500);

            var retrieved = await dr.GetRecoveryStatusAsync(recovery.RecoveryId);
            Assert.NotNull(retrieved);
            Assert.Equal(recovery.RecoveryId, retrieved.RecoveryId);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_ConfigureRpoAsync_SetRpo()
        {
            var dr = new DisasterRecoveryOrchestrator();
            var result = await dr.ConfigureRpoAsync("resource1", 30);

            Assert.True(result);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_GetRpoAsync_RetrievesRpo()
        {
            var dr = new DisasterRecoveryOrchestrator();
            await dr.ConfigureRpoAsync("resource1", 25);

            var rpo = await dr.GetRpoAsync("resource1");
            Assert.Equal(25, rpo);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_RegisterBackupDestinationAsync_RegistersDestination()
        {
            var dr = new DisasterRecoveryOrchestrator();
            var result = await dr.RegisterBackupDestinationAsync("azure1", "azure", "DefaultEndpointsProtocol=https;AccountName=test");

            Assert.True(result);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_SetupMultiRegionRecoveryAsync_ConfiguresRegions()
        {
            var dr = new DisasterRecoveryOrchestrator();
            var result = await dr.SetupMultiRegionRecoveryAsync("primary-us-east", new List<string> { "secondary-us-west", "tertiary-eu-west" });

            Assert.True(result);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_GetMetricsAsync_ReturnsMetrics()
        {
            var dr = new DisasterRecoveryOrchestrator();
            await dr.InitiateBackupAsync("backup1", BackupType.Full, new List<string> { "database1" });
            await Task.Delay(500);

            var metrics = await dr.GetMetricsAsync();
            Assert.True(metrics.TotalBackups > 0);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_CancelOperationAsync_CancelsBackup()
        {
            var dr = new DisasterRecoveryOrchestrator();
            var backup = await dr.InitiateBackupAsync("backup1", BackupType.Full, new List<string> { "database1" });

            var result = await dr.CancelOperationAsync(backup.BackupId);
            Assert.True(result);
        }

        #endregion

        #region Integration Tests (5 tests)

        [Fact]
        public async Task Integration_CacheLayer_WithLoadBalancer_CachesHealthStatus()
        {
            var cache = new DistributedCacheLayer();
            var lb = new ProductionLoadBalancer();

            await lb.RegisterServiceAsync("service1", "http://localhost:5000");
            var endpoint = await lb.GetNextServiceAsync();

            await cache.SetAsync(endpoint.ServiceId, endpoint.Endpoint);
            var cached = await cache.GetAsync(endpoint.ServiceId);

            Assert.Equal(endpoint.Endpoint, cached);
        }

        [Fact]
        public async Task Integration_QueryAnalyzer_WithCache_CachesAnalysis()
        {
            var cache = new DistributedCacheLayer();
            var analyzer = new QueryPlanAnalyzer();

            var query = "SELECT * FROM users WHERE id = 1";
            var analysis = await analyzer.AnalyzeAsync(query);

            await cache.SetAsync(analysis.QueryHash, analysis.EstimatedCost.ToString());
            var cached = await cache.GetAsync(analysis.QueryHash);

            Assert.NotNull(cached);
        }

        [Fact]
        public async Task Integration_SecurityAndDisasterRecovery_TrackBackupSecurity()
        {
            var security = new ZeroTrustImplementation();
            var dr = new DisasterRecoveryOrchestrator();

            var context = new SecurityContext
            {
                PrincipalId = "admin",
                Action = "backup",
                ResourceId = "database1",
                RequestTime = DateTime.UtcNow
            };

            var verified = await security.VerifyRequestAsync(context);
            if (verified.IsVerified)
            {
                var backup = await dr.InitiateBackupAsync("secure-backup", BackupType.Full, new List<string> { "database1" });
                Assert.NotNull(backup);
            }
        }

        [Fact]
        public async Task Integration_LoadBalancer_WithCache_TracksDistribution()
        {
            var cache = new DistributedCacheLayer();
            var lb = new ProductionLoadBalancer();

            await lb.RegisterServiceAsync("service1", "http://localhost:5000");
            await lb.RegisterServiceAsync("service2", "http://localhost:5001");

            for (int i = 0; i < 5; i++)
            {
                var endpoint = await lb.GetNextServiceAsync();
                var key = $"request_{i}";
                await cache.SetAsync(key, endpoint.ServiceId);
            }

            var size = await cache.GetSizeAsync();
            Assert.Equal(5, size);
        }

        [Fact]
        public async Task Integration_CompleteWorkflow_BackupWithSecurityAndCache()
        {
            var cache = new DistributedCacheLayer();
            var security = new ZeroTrustImplementation();
            var dr = new DisasterRecoveryOrchestrator();

            // Step 1: Verify security
            var context = new SecurityContext
            {
                PrincipalId = "admin",
                Action = "backup",
                ResourceId = "database1",
                RequestTime = DateTime.UtcNow
            };

            var verified = await security.VerifyRequestAsync(context);
            Assert.True(verified.IsVerified);

            // Step 2: Initiate backup
            var backup = await dr.InitiateBackupAsync("full-backup", BackupType.Full, new List<string> { "database1" });
            await Task.Delay(500);

            // Step 3: Cache backup status
            await cache.SetAsync($"backup_{backup.BackupId}", backup.Status.ToString());

            var cachedStatus = await cache.GetAsync($"backup_{backup.BackupId}");
            Assert.NotNull(cachedStatus);

            // Step 4: Verify recovery possible
            var recovery = await dr.InitiateRecoveryAsync(backup.BackupId, RecoveryType.Full, new List<string> { "database1" });
            Assert.NotNull(recovery);
        }

        #endregion
    }
}

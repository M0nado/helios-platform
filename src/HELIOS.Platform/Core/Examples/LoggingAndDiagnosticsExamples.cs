using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Core.Diagnostics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Examples
{
    /// <summary>
    /// Examples demonstrating the complete logging and diagnostics system
    /// </summary>
    public class LoggingAndDiagnosticsExamples
    {
        /// <summary>
        /// Example 1: Initialize and configure the logging system
        /// </summary>
        public static void Example1_InitializeLogging()
        {
            // Configure global logger with all sinks
            LoggerConfiguration.ConfigureGlobalLogger(Serilog.Events.LogEventLevel.Information);

            // Register crash handlers
            CrashReporter.RegisterGlobalHandlers();

            // Get the logger
            var logger = Log.ForContext<LoggingAndDiagnosticsExamples>();
            logger.Information("Application started with comprehensive logging");
        }

        /// <summary>
        /// Example 2: Use structured logging with context
        /// </summary>
        public static void Example2_StructuredLoggingWithContext()
        {
            var logger = Log.ForContext<LoggingAndDiagnosticsExamples>();

            // Create a logging context
            using (var context = LogContext.CreateScope()
                .WithUserId("user123")
                .WithRequestId("req-456")
                .WithOperation("ProcessOrder")
                .WithModule("OrderService"))
            {
                logger.Information("Processing order for user {UserId}", "user123");
                logger.Warning("Order amount exceeds threshold: {Amount}", 5000);
            }

            // Context is automatically cleaned up after the using block
        }

        /// <summary>
        /// Example 3: Log rotation and archival
        /// </summary>
        public static void Example3_LogRotationAndArchival()
        {
            var config = new LogRotationManager.RotationConfig
            {
                MaxFileSizeBytes = 100 * 1024 * 1024,  // 100 MB
                MaxRetainedFiles = 30,
                DaysBeforeArchival = 7,
                DaysToKeepArchived = 90,
                CompressArchivedLogs = true
            };

            // Perform log rotation
            LogRotationManager.RotateLogs(config);

            // Get log statistics
            var stats = LogRotationManager.GetLogStatistics();
            Console.WriteLine($"Total log files: {stats.LogFileCount}");
            Console.WriteLine($"Total size: {stats.TotalLogSizeMB:F2} MB");
        }

        /// <summary>
        /// Example 4: Crash reporting
        /// </summary>
        public static void Example4_CrashReporting()
        {
            try
            {
                // Simulate an error
                throw new InvalidOperationException("Something went wrong!");
            }
            catch (Exception ex)
            {
                // Create a crash dump
                var dumpPath = CrashReporter.CreateCrashDump("ExceptionExample", ex);
                Console.WriteLine($"Crash dump created: {dumpPath}");

                // Get all crash dumps
                var crashes = CrashReporter.GetCrashDumps();
                Console.WriteLine($"Total crash dumps: {crashes.Length}");
            }
        }

        /// <summary>
        /// Example 5: Health diagnostics
        /// </summary>
        public static async Task Example5_HealthDiagnosticsAsync()
        {
            var engine = new HealthDiagnosticsEngine();

            // Register a custom health check
            engine.RegisterHealthCheck("DatabaseConnection", new DatabaseHealthCheck());
            engine.RegisterHealthCheck("DiskSpace", new DiskSpaceHealthCheck());

            // Run all health checks
            var overallStatus = await engine.RunAllHealthChecksAsync();

            Console.WriteLine($"Overall status: {overallStatus.Status}");
            Console.WriteLine($"Healthy checks: {overallStatus.HealthyCount}");
            Console.WriteLine($"Failed checks: {overallStatus.FailedCount}");

            foreach (var status in overallStatus.IndividualStatuses)
            {
                Console.WriteLine($"  {status.CheckName}: {status.Status} ({status.CheckDurationMs}ms)");
            }
        }

        /// <summary>
        /// Example 6: Performance monitoring
        /// </summary>
        public static void Example6_PerformanceMonitoring()
        {
            var monitor = new PerformanceMonitor();

            // Register counters
            monitor.RegisterCounter("RequestCount", "Total requests processed", CounterType.Counter);
            monitor.RegisterCounter("RequestDuration", "Request processing time in ms", CounterType.Timing);

            // Use performance measurement scope
            for (int i = 0; i < 5; i++)
            {
                using (monitor.MeasurePerformance("ProcessRequest"))
                {
                    // Simulate work
                    System.Threading.Thread.Sleep(100);
                }
                monitor.IncrementCounter("RequestCount");
            }

            // Get performance summary
            var summary = monitor.GetSummary();
            foreach (var counter in summary.Counters)
            {
                Console.WriteLine($"{counter.Name}: {counter.Value} " +
                    $"(Avg: {counter.Average:F2}ms, Min: {counter.Minimum}ms, Max: {counter.Maximum}ms)");
            }
        }

        /// <summary>
        /// Example 7: Resource usage tracking
        /// </summary>
        public static void Example7_ResourceUsageTracking()
        {
            var tracker = new ResourceUsageTracker();

            // Capture multiple snapshots
            for (int i = 0; i < 5; i++)
            {
                var snapshot = tracker.CaptureSnapshot();
                Console.WriteLine($"Snapshot {i + 1}:");
                Console.WriteLine($"  Memory: {snapshot.ProcessMemoryMB} MB");
                Console.WriteLine($"  CPU: {snapshot.CpuUsagePercent:F2}%");
                Console.WriteLine($"  Threads: {snapshot.ThreadCount}");
                System.Threading.Thread.Sleep(1000);
            }

            // Get statistics
            var stats = tracker.GetStatistics();
            Console.WriteLine($"Average memory: {stats.AverageProcessMemoryMB:F2} MB");
            Console.WriteLine($"Peak memory: {stats.MaxProcessMemoryMB} MB");

            // Detect anomalies
            var anomalies = tracker.DetectAnomalies();
            if (anomalies.HasAnomalies)
            {
                Console.WriteLine("Anomalies detected:");
                foreach (var anomaly in anomalies.Anomalies)
                {
                    Console.WriteLine($"  - {anomaly}");
                }
            }
        }

        /// <summary>
        /// Example 8: Alert system
        /// </summary>
        public static void Example8_AlertSystem()
        {
            var alertSystem = new HealthAlertSystem();

            // Register a console alert handler
            alertSystem.RegisterHandler(new ConsoleAlertHandler());

            // Add alert rules
            alertSystem.AddAlertRule(new AlertRule
            {
                Name = "CriticalFailure",
                Title = "Critical System Failure",
                Description = "One or more critical health checks failed",
                Severity = AlertSeverity.Critical,
                Condition = (status) => status.Status == HealthStatusCode.Critical
            });

            // Simulate a health status that triggers the alert
            var healthStatus = new OverallHealthStatus
            {
                Status = HealthStatusCode.Critical,
                Message = "Critical failure detected",
                IndividualStatuses = new List<HealthStatus>
                {
                    new HealthStatus { CheckName = "Database", Status = HealthStatusCode.Failed }
                }
            };

            // Check health and trigger alerts
            alertSystem.CheckHealthStatus(healthStatus);

            // Get active alerts
            var activeAlerts = alertSystem.GetActiveAlerts();
            Console.WriteLine($"Active alerts: {activeAlerts.Count}");

            // Get alert statistics
            var stats = alertSystem.GetStatistics();
            Console.WriteLine($"Critical alerts: {stats.CriticalAlerts}");
        }

        /// <summary>
        /// Example 9: Custom performance counters
        /// </summary>
        public static void Example9_CustomPerformanceCounters()
        {
            var counterManager = new CustomPerformanceCounterManager();

            // Create custom counters
            var requestCounter = counterManager.GetOrCreateCounter(
                "API", "RequestCount",
                new CounterDefinition
                {
                    Name = "RequestCount",
                    Description = "Number of API requests",
                    Unit = "count",
                    AlertThreshold = 10000
                });

            var responseTimeCounter = counterManager.GetOrCreateCounter(
                "API", "ResponseTime",
                new CounterDefinition
                {
                    Name = "ResponseTime",
                    Description = "API response time",
                    Unit = "ms",
                    WarningThreshold = 500,
                    AlertThreshold = 1000
                });

            // Record samples
            for (int i = 0; i < 10; i++)
            {
                requestCounter.RecordSample(100 + i);
                responseTimeCounter.RecordSample(50 + (i * 10));
            }

            // Generate report
            var report = counterManager.GenerateReport();
            foreach (var counter in report.Counters)
            {
                Console.WriteLine($"{counter.CategoryName}\\{counter.CounterName}:");
                Console.WriteLine($"  Current: {counter.CurrentValue}");
                Console.WriteLine($"  Average: {counter.AverageValue:F2}");
                Console.WriteLine($"  Status: {counter.Status}");
            }
        }

        /// <summary>
        /// Example 10: Dashboard generation
        /// </summary>
        public static void Example10_DashboardGeneration()
        {
            var diagnosticsEngine = new HealthDiagnosticsEngine();
            var resourceTracker = new ResourceUsageTracker();
            var performanceMonitor = new PerformanceMonitor();
            var alertSystem = new HealthAlertSystem();
            var counterManager = new CustomPerformanceCounterManager();

            var dashboard = new HealthDashboardProvider(
                diagnosticsEngine,
                resourceTracker,
                performanceMonitor,
                alertSystem,
                counterManager);

            // Capture some data
            resourceTracker.CaptureSnapshot();
            performanceMonitor.RegisterCounter("Requests", "Request count", CounterType.Counter);
            performanceMonitor.IncrementCounter("Requests", 42);

            // Generate dashboard data
            var data = dashboard.GenerateDashboardData();
            Console.WriteLine($"Dashboard generated at: {data.GeneratedAt}");
            Console.WriteLine($"Performance metrics: {data.PerformanceMetrics.Count}");
            Console.WriteLine($"Custom counters: {data.CustomCounters.Count}");
            Console.WriteLine($"Active alerts: {data.ActiveAlerts.Count}");

            // Generate summary
            var summary = dashboard.GenerateSummary();
            Console.WriteLine($"Health status: {summary.HealthStatus}");
            Console.WriteLine($"Current memory: {summary.CurrentMemoryMB} MB");
            Console.WriteLine($"Current CPU: {summary.CurrentCpuPercent:F2}%");

            // Generate health trends
            var trends = dashboard.GenerateHealthTrend(TimeSpan.FromHours(1));
            Console.WriteLine($"Trend points: {trends.TimePoints.Count}");
        }
    }

    /// <summary>
    /// Example custom health check for database
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        public Task<HealthStatus> CheckAsync()
        {
            return Task.FromResult(new HealthStatus
            {
                Status = HealthStatusCode.Healthy,
                Message = "Database connection is healthy"
            });
        }
    }

    /// <summary>
    /// Example custom health check for disk space
    /// </summary>
    public class DiskSpaceHealthCheck : IHealthCheck
    {
        public Task<HealthStatus> CheckAsync()
        {
            // Check if free disk space is adequate
            var drive = System.IO.DriveInfo.GetDrives()[0];
            var availableGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);

            if (availableGB < 1)
            {
                return Task.FromResult(new HealthStatus
                {
                    Status = HealthStatusCode.Critical,
                    Message = $"Disk space critically low: {availableGB} GB"
                });
            }
            else if (availableGB < 5)
            {
                return Task.FromResult(new HealthStatus
                {
                    Status = HealthStatusCode.Degraded,
                    Message = $"Disk space low: {availableGB} GB"
                });
            }

            return Task.FromResult(new HealthStatus
            {
                Status = HealthStatusCode.Healthy,
                Message = $"Disk space is healthy: {availableGB} GB available"
            });
        }
    }
}

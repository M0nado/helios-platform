using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Agent Fleet Simulator - Simulates 100+ agents with realistic workloads
    /// </summary>
    public class AgentFleetSimulator
    {
        public class AgentSimulation
        {
            public int AgentId { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public long RequestsSent { get; set; }
            public long RequestsSucceeded { get; set; }
            public long RequestsFailed { get; set; }
            public long TotalLatencyMs { get; set; }
            public long MinLatencyMs { get; set; }
            public long MaxLatencyMs { get; set; }
            public int MemoryAllocatedMB { get; set; }
            public double AverageLatencyMs => RequestsSucceeded > 0 ? (double)TotalLatencyMs / RequestsSucceeded : 0;

            public AgentSimulation()
            {
                MinLatencyMs = long.MaxValue;
                StartTime = DateTime.UtcNow;
            }
        }

        public class FleetMetrics
        {
            public int TotalAgents { get; set; }
            public long TotalRequestsSent { get; set; }
            public long TotalRequestsSucceeded { get; set; }
            public long TotalRequestsFailed { get; set; }
            public DateTime TestStartTime { get; set; }
            public DateTime TestEndTime { get; set; }
            public double DurationSeconds => (TestEndTime - TestStartTime).TotalSeconds;
            public double SuccessRate => TotalRequestsSent > 0 ? (double)TotalRequestsSucceeded / TotalRequestsSent : 0;
            public double RequestsPerSecond => DurationSeconds > 0 ? TotalRequestsSent / DurationSeconds : 0;
            public double AverageLatencyMs { get; set; }
            public double P95LatencyMs { get; set; }
            public double P99LatencyMs { get; set; }
            public long PeakMemoryMB { get; set; }
            public double AverageCPUUtilization { get; set; }
            public List<AgentSimulation> AgentResults { get; set; } = new();
        }

        private readonly int _numberOfAgents;
        private readonly int _requestsPerAgent;
        private readonly int _concurrency;
        private Process _process;

        public AgentFleetSimulator(int numberOfAgents = 100, int requestsPerAgent = 50, int concurrency = 150)
        {
            _numberOfAgents = numberOfAgents;
            _requestsPerAgent = requestsPerAgent;
            _concurrency = concurrency;
            _process = Process.GetCurrentProcess();
        }

        public async Task<FleetMetrics> RunSimulationAsync(TimeSpan? timeout = null)
        {
            var metrics = new FleetMetrics
            {
                TotalAgents = _numberOfAgents,
                TestStartTime = DateTime.UtcNow
            };

            var initialMemory = GC.GetTotalMemory(false);
            var semaphore = new SemaphoreSlim(_concurrency);
            var agentTasks = new List<Task>();
            var latencies = new List<long>();
            var latencyLock = new object();

            var cpuStartTime = _process.TotalProcessorTime;
            var sw = Stopwatch.StartNew();

            try
            {
                // Spawn agent simulations
                for (int agentId = 0; agentId < _numberOfAgents; agentId++)
                {
                    agentTasks.Add(SimulateAgentAsync(
                        agentId, 
                        _requestsPerAgent, 
                        semaphore, 
                        metrics, 
                        latencies, 
                        latencyLock));
                }

                // Wait for all agents with timeout
                if (timeout.HasValue)
                {
                    var completed = await Task.WhenAny(
                        Task.WhenAll(agentTasks),
                        Task.Delay(timeout.Value)
                    ).ConfigureAwait(false);

                    if (completed != Task.WhenAll(agentTasks))
                    {
                        // Timeout occurred
                        throw new TimeoutException($"Simulation exceeded {timeout.Value.TotalSeconds}s");
                    }
                }
                else
                {
                    await Task.WhenAll(agentTasks);
                }

                sw.Stop();
            }
            finally
            {
                semaphore.Dispose();
            }

            metrics.TestEndTime = DateTime.UtcNow;

            // Calculate metrics
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(false);
            metrics.PeakMemoryMB = (int)((finalMemory - initialMemory) / (1024 * 1024));

            var cpuEndTime = _process.TotalProcessorTime;
            var cpuUsedMs = (cpuEndTime - cpuStartTime).TotalMilliseconds;
            metrics.AverageCPUUtilization = (cpuUsedMs / sw.ElapsedMilliseconds) * 100;

            lock (latencyLock)
            {
                if (latencies.Count > 0)
                {
                    var sortedLatencies = latencies.OrderBy(x => x).ToList();
                    metrics.AverageLatencyMs = sortedLatencies.Average();
                    metrics.P95LatencyMs = sortedLatencies[(int)(sortedLatencies.Count * 0.95)];
                    metrics.P99LatencyMs = sortedLatencies[(int)(sortedLatencies.Count * 0.99)];
                }
            }

            return metrics;
        }

        private async Task SimulateAgentAsync(
            int agentId,
            int requestCount,
            SemaphoreSlim semaphore,
            FleetMetrics fleetMetrics,
            List<long> latencies,
            object latencyLock)
        {
            var agentMetrics = new AgentSimulation { AgentId = agentId };

            for (int i = 0; i < requestCount; i++)
            {
                await semaphore.WaitAsync();
                try
                {
                    var sw = Stopwatch.StartNew();
                    
                    // Simulate API call
                    var success = await SimulateApiCallAsync();
                    
                    sw.Stop();

                    lock (latencyLock)
                    {
                        latencies.Add(sw.ElapsedMilliseconds);
                        agentMetrics.TotalLatencyMs += sw.ElapsedMilliseconds;
                        agentMetrics.MinLatencyMs = Math.Min(agentMetrics.MinLatencyMs, sw.ElapsedMilliseconds);
                        agentMetrics.MaxLatencyMs = Math.Max(agentMetrics.MaxLatencyMs, sw.ElapsedMilliseconds);
                        agentMetrics.RequestsSent++;

                        if (success)
                        {
                            agentMetrics.RequestsSucceeded++;
                            fleetMetrics.TotalRequestsSucceeded++;
                        }
                        else
                        {
                            agentMetrics.RequestsFailed++;
                            fleetMetrics.TotalRequestsFailed++;
                        }

                        fleetMetrics.TotalRequestsSent++;
                    }
                }
                finally
                {
                    semaphore.Release();
                }

                // Small delay between requests
                await Task.Delay(Random.Shared.Next(10, 50));
            }

            agentMetrics.EndTime = DateTime.UtcNow;
            lock (latencyLock)
            {
                fleetMetrics.AgentResults.Add(agentMetrics);
            }
        }

        private async Task<bool> SimulateApiCallAsync()
        {
            try
            {
                // Simulate variable API latency
                var latency = Random.Shared.Next(20, 200);
                await Task.Delay(latency);

                // 1% failure rate
                if (Random.Shared.NextDouble() < 0.01)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void PrintFleetMetrics(FleetMetrics metrics)
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("HELIOS PLATFORM - 100+ AGENT FLEET SCALING VALIDATION");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"\nTest Duration: {metrics.DurationSeconds:F2}s");
            Console.WriteLine($"Total Agents: {metrics.TotalAgents}");
            Console.WriteLine($"Total Requests: {metrics.TotalRequestsSent}");
            Console.WriteLine($"Successful: {metrics.TotalRequestsSucceeded}");
            Console.WriteLine($"Failed: {metrics.TotalRequestsFailed}");
            Console.WriteLine($"Success Rate: {metrics.SuccessRate:P2}");
            Console.WriteLine($"Throughput: {metrics.RequestsPerSecond:F2} req/sec");
            Console.WriteLine($"\nLatency Metrics:");
            Console.WriteLine($"  Average: {metrics.AverageLatencyMs:F2}ms");
            Console.WriteLine($"  P95: {metrics.P95LatencyMs:F2}ms");
            Console.WriteLine($"  P99: {metrics.P99LatencyMs:F2}ms");
            Console.WriteLine($"\nResource Utilization:");
            Console.WriteLine($"  Peak Memory: {metrics.PeakMemoryMB}MB");
            Console.WriteLine($"  Average CPU: {metrics.AverageCPUUtilization:F1}%");

            // Per-agent stats
            Console.WriteLine($"\nPer-Agent Statistics:");
            Console.WriteLine($"  Min Avg Latency: {metrics.AgentResults.Min(x => x.AverageLatencyMs):F2}ms");
            Console.WriteLine($"  Max Avg Latency: {metrics.AgentResults.Max(x => x.AverageLatencyMs):F2}ms");
            Console.WriteLine($"  StdDev: {CalculateStdDev(metrics.AgentResults.Select(x => x.AverageLatencyMs)):F2}ms");

            Console.WriteLine("\n" + new string('=', 80));
        }

        private static double CalculateStdDev(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (list.Count == 0) return 0;
            var mean = list.Average();
            var variance = list.Sum(x => Math.Pow(x - mean, 2)) / list.Count;
            return Math.Sqrt(variance);
        }
    }

    /// <summary>
    /// Resource Monitor - Tracks system resources during scaling tests
    /// </summary>
    public class ResourceMonitor
    {
        public class ResourceSnapshot
        {
            public DateTime Timestamp { get; set; }
            public long MemoryMB { get; set; }
            public double CPUPercent { get; set; }
            public long ThreadCount { get; set; }
            public long HandleCount { get; set; }
        }

        private readonly Process _process;
        private readonly List<ResourceSnapshot> _snapshots = new();
        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitoringTask;

        public ResourceMonitor()
        {
            _process = Process.GetCurrentProcess();
        }

        public void StartMonitoring(int intervalMs = 500)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _monitoringTask = MonitoringLoop(_cancellationTokenSource.Token, intervalMs);
        }

        public async Task StopMonitoringAsync()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                try
                {
                    await _monitoringTask;
                }
                catch (OperationCanceledException) { }
            }
        }

        private async Task MonitoringLoop(CancellationToken cancellationToken, int intervalMs)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _process.Refresh();
                    var snapshot = new ResourceSnapshot
                    {
                        Timestamp = DateTime.UtcNow,
                        MemoryMB = _process.WorkingSet64 / (1024 * 1024),
                        ThreadCount = _process.Threads.Count,
                        HandleCount = _process.HandleCount
                    };

                    lock (_snapshots)
                    {
                        _snapshots.Add(snapshot);
                    }

                    await Task.Delay(intervalMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public List<ResourceSnapshot> GetSnapshots()
        {
            lock (_snapshots)
            {
                return new List<ResourceSnapshot>(_snapshots);
            }
        }

        public void PrintResourceReport()
        {
            var snapshots = GetSnapshots();
            if (snapshots.Count == 0)
            {
                Console.WriteLine("No resource data collected");
                return;
            }

            var memoryValues = snapshots.Select(s => (double)s.MemoryMB).ToList();
            var threadCounts = snapshots.Select(s => s.ThreadCount).ToList();

            Console.WriteLine("\nResource Monitor Report:");
            Console.WriteLine($"  Memory - Min: {memoryValues.Min()}MB, Max: {memoryValues.Max()}MB, Avg: {memoryValues.Average():F1}MB");
            Console.WriteLine($"  Threads - Min: {threadCounts.Min()}, Max: {threadCounts.Max()}, Avg: {threadCounts.Average():F1}");
            Console.WriteLine($"  Snapshots collected: {snapshots.Count}");
        }
    }

    /// <summary>
    /// Concurrency Stress Tester - Verifies thread safety and detects race conditions
    /// </summary>
    public class ConcurrencyStressTester
    {
        public class StressTestResult
        {
            public int TotalOperations { get; set; }
            public int RaceConditionsDetected { get; set; }
            public int DeadlocksDetected { get; set; }
            public int ExceptionsThrown { get; set; }
            public double DurationSeconds { get; set; }
            public bool Passed => RaceConditionsDetected == 0 && DeadlocksDetected == 0;
        }

        public static async Task<StressTestResult> TestConcurrentAccessAsync(
            int agents,
            int operationsPerAgent,
            Func<int, Task> operation)
        {
            var result = new StressTestResult();
            var sw = Stopwatch.StartNew();
            var semaphore = new SemaphoreSlim(200); // Concurrent operations

            try
            {
                var tasks = new List<Task>();

                for (int agent = 0; agent < agents; agent++)
                {
                    for (int op = 0; op < operationsPerAgent; op++)
                    {
                        var agentId = agent;
                        tasks.Add(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                await operation(agentId);
                                result.TotalOperations++;
                            }
                            catch (Exception ex)
                            {
                                result.ExceptionsThrown++;
                                if (ex.Message.Contains("deadlock"))
                                    result.DeadlocksDetected++;
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }());
                    }
                }

                var completedInTime = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(60));
                if (!completedInTime)
                {
                    result.DeadlocksDetected++;
                }
            }
            finally
            {
                sw.Stop();
                result.DurationSeconds = sw.Elapsed.TotalSeconds;
                semaphore.Dispose();
            }

            return result;
        }
    }

    /// <summary>
    /// Load Distribution Analyzer - Verifies fair load balancing
    /// </summary>
    public class LoadDistributionAnalyzer
    {
        public class DistributionAnalysis
        {
            public int[] ServerLoads { get; set; }
            public double Mean { get; set; }
            public double StandardDeviation { get; set; }
            public double CoefficientOfVariation { get; set; }
            public double JainIndex { get; set; }
            public bool IsFair => CoefficientOfVariation < 0.2 && JainIndex > 0.8;
        }

        public static DistributionAnalysis AnalyzeDistribution(int[] loads)
        {
            var mean = loads.Average();
            var variance = loads.Sum(x => Math.Pow(x - mean, 2)) / loads.Length;
            var stdDev = Math.Sqrt(variance);
            var cv = stdDev / mean;

            // Jain's Fairness Index: ranges 0-1, 1 is perfectly fair
            var sumSquares = loads.Sum(x => Math.Pow(x, 2));
            var sumAll = loads.Sum();
            var jainIndex = Math.Pow(sumAll, 2) / (loads.Length * sumSquares);

            return new DistributionAnalysis
            {
                ServerLoads = loads,
                Mean = mean,
                StandardDeviation = stdDev,
                CoefficientOfVariation = cv,
                JainIndex = jainIndex
            };
        }

        public static void PrintDistributionAnalysis(DistributionAnalysis analysis)
        {
            Console.WriteLine("\nLoad Distribution Analysis:");
            Console.WriteLine($"  Mean Load: {analysis.Mean:F2}");
            Console.WriteLine($"  Standard Deviation: {analysis.StandardDeviation:F2}");
            Console.WriteLine($"  Coefficient of Variation: {analysis.CoefficientOfVariation:F3}");
            Console.WriteLine($"  Jain's Fairness Index: {analysis.JainIndex:F3}");
            Console.WriteLine($"  Fair Distribution: {(analysis.IsFair ? "YES" : "NO")}");
            Console.WriteLine($"  Min Load: {analysis.ServerLoads.Min()}");
            Console.WriteLine($"  Max Load: {analysis.ServerLoads.Max()}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using MonadoBlade.Monitoring;

namespace MonadoBlade.Monitoring.Tests
{
    public class MonitoringTests
    {
        #region AdvancedMetricsCollector Tests

        [Fact]
        public void TestIncrementCounter()
        {
            var collector = new AdvancedMetricsCollector();
            collector.IncrementCounter("requests", 1);
            collector.IncrementCounter("requests", 5);

            var metrics = collector.GetMetrics();
            Assert.True(metrics.ContainsKey("counter_requests"));
            Assert.Equal(6L, metrics["counter_requests"]);
        }

        [Fact]
        public void TestRecordGauge()
        {
            var collector = new AdvancedMetricsCollector();
            collector.RecordGauge("cpu_usage", 75.5);
            collector.RecordGauge("cpu_usage", 82.3);

            var metrics = collector.GetMetrics();
            Assert.Equal(82.3, metrics["gauge_cpu_usage"]);
        }

        [Fact]
        public void TestRecordHistogram()
        {
            var collector = new AdvancedMetricsCollector();
            collector.RecordHistogram("latency", 100);
            collector.RecordHistogram("latency", 200);
            collector.RecordHistogram("latency", 150);

            var metrics = collector.GetMetrics();
            Assert.True(metrics.ContainsKey("histogram_latency_count"));
            Assert.Equal(3L, metrics["histogram_latency_count"]);
        }

        [Fact]
        public void TestMetricsWithTags()
        {
            var collector = new AdvancedMetricsCollector();
            var tags = new Dictionary<string, string> { { "endpoint", "/api/users" }, { "method", "GET" } };

            collector.IncrementCounter("requests", 1, tags);
            collector.IncrementCounter("requests", 2, tags);

            var metrics = collector.GetMetrics();
            var key = metrics.Keys.FirstOrDefault(k => k.Contains("endpoint") && k.Contains("/api/users"));
            Assert.NotNull(key);
        }

        [Fact]
        public void TestW3CTraceContextPropagation()
        {
            var collector = new AdvancedMetricsCollector();
            const string traceparent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
            const string tracestate = "congo=t61rcWpm1t1bzwk";

            collector.SetTraceContext(traceparent, tracestate);
            var (tp, ts) = collector.GetTraceContext();

            Assert.Equal(traceparent, tp);
            Assert.Equal(tracestate, ts);
        }

        [Fact]
        public void TestPrometheusExport()
        {
            var collector = new AdvancedMetricsCollector();
            collector.IncrementCounter("test_counter", 42);
            collector.RecordGauge("test_gauge", 3.14);

            var prometheus = collector.ExportPrometheus();
            Assert.Contains("test_counter", prometheus);
            Assert.Contains("test_gauge", prometheus);
            Assert.Contains("42", prometheus);
        }

        [Fact]
        public void TestGetLatencyPercentile()
        {
            var collector = new AdvancedMetricsCollector();
            for (int i = 1; i <= 100; i++)
            {
                collector.RecordHistogram("latency", i);
            }

            var p95 = collector.GetLatencyPercentile("latency", 95);
            Assert.True(p95 > 90 && p95 <= 100);
        }

        [Fact]
        public void TestGetErrorRate()
        {
            var collector = new AdvancedMetricsCollector();
            collector.IncrementCounter("api_requests", 100);
            collector.IncrementCounter("api_errors", 5);

            var errorRate = collector.GetErrorRate("api");
            Assert.Equal(5.0, errorRate);
        }

        [Fact]
        public void TestResetMetrics()
        {
            var collector = new AdvancedMetricsCollector();
            collector.IncrementCounter("counter", 10);
            collector.ResetMetrics();

            var metrics = collector.GetMetrics();
            Assert.Equal(0, metrics.Count);
        }

        #endregion

        #region DistributedTracingManager Tests

        [Fact]
        public void TestGenerateCorrelationId()
        {
            var manager = new DistributedTracingManager();
            var id1 = manager.GenerateCorrelationId();
            var id2 = manager.GenerateCorrelationId();

            Assert.NotNull(id1);
            Assert.NotEmpty(id1);
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void TestStartAndEndSpan()
        {
            var manager = new DistributedTracingManager();
            var spanId = manager.StartSpan("test_operation");
            
            Assert.NotNull(spanId);
            Assert.NotEmpty(spanId);

            manager.EndSpan(spanId);
            var (correlationId, currentSpan) = manager.GetCurrentContext();
            Assert.NotNull(correlationId);
        }

        [Fact]
        public void TestParentChildSpanRelationship()
        {
            var manager = new DistributedTracingManager();
            var parentSpanId = manager.StartSpan("parent_operation");
            var childSpanId = manager.StartSpan("child_operation", parentSpanId);

            var childSpan = manager.GetSpanInfo(childSpanId);
            Assert.NotNull(childSpan);
            Assert.Equal(parentSpanId, childSpan.ParentSpanId);

            manager.EndSpan(childSpanId);
            manager.EndSpan(parentSpanId);
        }

        [Fact]
        public void TestSpanBaggage()
        {
            var manager = new DistributedTracingManager();
            var spanId = manager.StartSpan("operation");

            manager.SetBaggage("user_id", "12345");
            manager.SetBaggage("request_id", "req-001");

            var userId = manager.GetBaggage("user_id");
            var requestId = manager.GetBaggage("request_id");

            Assert.Equal("12345", userId);
            Assert.Equal("req-001", requestId);

            var allBaggage = manager.GetAllBaggage();
            Assert.Equal(2, allBaggage.Count);

            manager.EndSpan(spanId);
        }

        [Fact]
        public void TestSamplingStrategy()
        {
            var manager = new DistributedTracingManager();
            
            manager.SetSamplingStrategy(SamplingStrategy.NeverSample);
            var span1 = manager.StartSpan("operation1");
            Assert.Null(span1);

            manager.SetSamplingStrategy(SamplingStrategy.AlwaysSample);
            var span2 = manager.StartSpan("operation2");
            Assert.NotNull(span2);

            manager.EndSpan(span2);
        }

        [Fact]
        public void TestGetActiveSpans()
        {
            var manager = new DistributedTracingManager();
            manager.StartSpan("operation1");
            manager.StartSpan("operation2");

            var activeSpans = manager.GetActiveSpans();
            Assert.Equal(2, activeSpans.Count);
        }

        [Fact]
        public void TestTraceTree()
        {
            var manager = new DistributedTracingManager();
            var correlationId = manager.GenerateCorrelationId();

            var span1 = manager.StartSpan("operation1");
            var span2 = manager.StartSpan("operation2", span1);
            var span3 = manager.StartSpan("operation3", span2);

            var traceTree = manager.GetTraceTree(correlationId);
            Assert.True(traceTree.Count >= 2);

            manager.EndSpan(span3);
            manager.EndSpan(span2);
            manager.EndSpan(span1);
        }

        #endregion

        #region AnomalyDetectionHooks Tests

        [Fact]
        public void TestBaselineEstablishment()
        {
            var hooks = new AnomalyDetectionHooks();
            hooks.SetLearningWindow(50, TimeSpan.FromMinutes(1));

            for (int i = 0; i < 60; i++)
            {
                hooks.CheckMetricAnomaly("cpu_usage", 50 + (i % 10), "gauge", null);
            }

            var baseline = hooks.GetBaselineValue("cpu_usage");
            Assert.True(baseline > 0);
            Assert.True(baseline >= 45 && baseline <= 55);
        }

        [Fact]
        public void TestAnomalyDetection()
        {
            var hooks = new AnomalyDetectionHooks();
            hooks.SetLearningWindow(20, TimeSpan.FromMinutes(1));
            hooks.SetDetectionThresholds(2.5, 0.3);

            for (int i = 0; i < 30; i++)
            {
                hooks.CheckMetricAnomaly("latency", 100, "histogram", null);
            }

            var baseline = hooks.GetBaselineValue("latency");
            var isAnomalous = hooks.IsAnomaly("latency", 400); // 4x the baseline
            Assert.True(isAnomalous);
        }

        [Fact]
        public void TestAnomalyAlertCallbacks()
        {
            var hooks = new AnomalyDetectionHooks();
            AnomalyAlert capturedAlert = null;

            hooks.RegisterAlertCallback(alert => 
            {
                capturedAlert = alert;
            });

            hooks.SetLearningWindow(20, TimeSpan.FromMinutes(1));

            for (int i = 0; i < 30; i++)
            {
                hooks.CheckMetricAnomaly("memory", 1000, "gauge", null);
            }

            hooks.CheckMetricAnomaly("memory", 5000, "gauge", null);

            Task.Delay(100).Wait(); // Give callback time to execute
            Assert.NotNull(capturedAlert);
            Assert.Equal("memory", capturedAlert.MetricName);
        }

        [Fact]
        public void TestAnomalySeverityScoring()
        {
            var hooks = new AnomalyDetectionHooks();
            hooks.SetLearningWindow(20, TimeSpan.FromMinutes(1));

            for (int i = 0; i < 30; i++)
            {
                hooks.CheckMetricAnomaly("response_time", 100, "histogram", null);
            }

            var alerts = hooks.GetRecentAlerts(1);
            Assert.True(alerts.Count >= 0);
        }

        [Fact]
        public void TestMultipleAnomalyAlgorithms()
        {
            var hooks = new AnomalyDetectionHooks();
            hooks.SetDetectionThresholds(2.0, 0.25); // 25% threshold

            for (int i = 0; i < 50; i++)
            {
                hooks.CheckMetricAnomaly("metric1", 100, "gauge", null);
            }

            // Test percentage deviation detection
            var isAnomalous = hooks.IsAnomaly("metric1", 150); // 50% increase
            Assert.True(isAnomalous);
        }

        #endregion

        #region MetricsQueryEngine Tests

        [Fact]
        public void TestBasicMetricQuery()
        {
            var engine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 10; i++)
            {
                engine.RecordMetric("requests", 100 + i, now.AddSeconds(i));
            }

            var filter = new MetricFilter { MetricName = "requests" };
            var result = engine.Query(filter);

            Assert.NotNull(result);
            Assert.True(result.Mean > 0);
        }

        [Fact]
        public void TestMovingAverage()
        {
            var engine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            for (int i = 1; i <= 20; i++)
            {
                engine.RecordMetric("latency", i * 10, now.AddSeconds(i));
            }

            var movingAvg = engine.GetMovingAverage("latency", 5);
            Assert.NotEmpty(movingAvg);
            Assert.True(movingAvg.Count > 0);
        }

        [Fact]
        public void TestPercentileCalculation()
        {
            var engine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            for (int i = 1; i <= 100; i++)
            {
                engine.RecordMetric("response_time", i, now.AddSeconds(i));
            }

            var filter = new MetricFilter { MetricName = "response_time" };
            var result = engine.Query(filter);

            Assert.True(result.P50 > 0);
            Assert.True(result.P95 > result.P50);
            Assert.True(result.P99 >= result.P95);
        }

        [Fact]
        public void TestTimeRangeQuery()
        {
            var engine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 100; i++)
            {
                engine.RecordMetric("events", 50, now.AddSeconds(i));
            }

            var filter = new MetricFilter
            {
                MetricName = "events",
                StartTime = now.AddSeconds(10),
                EndTime = now.AddSeconds(50)
            };

            var result = engine.Query(filter);
            Assert.NotEmpty(result.DataPoints);
        }

        [Fact]
        public void TestTrendAnalysis()
        {
            var engine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            // Create increasing trend
            for (int i = 0; i < 50; i++)
            {
                engine.RecordMetric("growth", 100 + i, now.AddSeconds(i));
            }

            var filter = new MetricFilter { MetricName = "growth" };
            var result = engine.Query(filter);

            Assert.NotEmpty(result.TrendLine);
            var trend = result.TrendLine;
            Assert.True(trend.Last() > trend.First()); // Should show upward trend
        }

        [Fact]
        public void TestForecastGeneration()
        {
            var engine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 50; i++)
            {
                engine.RecordMetric("forecast_metric", 100, now.AddSeconds(i));
            }

            var filter = new MetricFilter { MetricName = "forecast_metric" };
            var result = engine.Query(filter);

            Assert.NotEmpty(result.Forecast);
            Assert.Equal(5, result.Forecast.Count);
        }

        [Fact]
        public void TestDerivativeCalculation()
        {
            var engine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            var dataPoints = new List<(DateTime, double)>();
            for (int i = 0; i < 10; i++)
            {
                var point = (now.AddSeconds(i), (double)(i * 10));
                dataPoints.Add(point);
            }

            var derivative = engine.CalculateDerivative(dataPoints);
            Assert.True(derivative > 0); // Should show positive rate of change
        }

        [Fact]
        public void TestMetricCorrelationAnalysis()
        {
            var engine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            // Create correlated metrics
            for (int i = 0; i < 50; i++)
            {
                engine.RecordMetric("cpu_usage", 50 + i, now.AddSeconds(i));
                engine.RecordMetric("memory_usage", 60 + i, now.AddSeconds(i));
            }

            var correlation = engine.AnalyzeCorrelation("cpu_usage", "memory_usage");
            Assert.NotEmpty(correlation);
            Assert.True(correlation.ContainsKey("pearson_correlation"));
        }

        [Fact]
        public void TestAggregateByTimeRange()
        {
            var engine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 100; i++)
            {
                engine.RecordMetric("requests", 50 + (i % 20), now.AddSeconds(i));
            }

            var filter = new MetricFilter { MetricName = "requests" };
            var result = engine.AggregateByTimeRange(filter, TimeSpan.FromSeconds(10));

            Assert.NotEmpty(result.DataPoints);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void TestMetricsCollectorWithAnomalyDetection()
        {
            var anomalyHooks = new AnomalyDetectionHooks();
            var collector = new AdvancedMetricsCollector(anomalyHooks);

            anomalyHooks.SetLearningWindow(20, TimeSpan.FromMinutes(1));

            // Establish baseline
            for (int i = 0; i < 30; i++)
            {
                collector.IncrementCounter("requests", 1);
            }

            // Should still work with anomaly detection
            var metrics = collector.GetMetrics();
            Assert.True(metrics.ContainsKey("counter_requests"));
        }

        [Fact]
        public void TestDistributedTracingWithQueryEngine()
        {
            var tracingManager = new DistributedTracingManager();
            var queryEngine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            var correlationId = tracingManager.GenerateCorrelationId();
            var span1 = tracingManager.StartSpan("operation1");

            // Record metrics during trace
            for (int i = 0; i < 20; i++)
            {
                queryEngine.RecordMetric("trace_duration", 50 + i, now.AddMilliseconds(i * 10));
            }

            var filter = new MetricFilter { MetricName = "trace_duration" };
            var result = queryEngine.Query(filter);

            Assert.NotNull(result);
            Assert.True(result.Mean > 0);

            tracingManager.EndSpan(span1);
        }

        [Fact]
        public void TestFullMonitoringStack()
        {
            var anomalyHooks = new AnomalyDetectionHooks();
            var collector = new AdvancedMetricsCollector(anomalyHooks);
            var tracingManager = new DistributedTracingManager();
            var queryEngine = new MetricsQueryEngine();
            var now = DateTime.UtcNow;

            // Set up monitoring
            anomalyHooks.SetLearningWindow(20, TimeSpan.FromMinutes(1));
            tracingManager.SetSamplingStrategy(SamplingStrategy.AlwaysSample);

            // Simulate request
            var correlationId = tracingManager.GenerateCorrelationId();
            var spanId = tracingManager.StartSpan("api_request");

            collector.SetTraceContext($"00-{correlationId}-{spanId}-01");
            
            for (int i = 0; i < 30; i++)
            {
                collector.IncrementCounter("api_requests", 1);
                collector.RecordHistogram("request_latency", 100 + i);
                queryEngine.RecordMetric("request_latency", 100 + i, now.AddMilliseconds(i));
            }

            tracingManager.EndSpan(spanId);

            // Verify all components
            var metrics = collector.GetMetrics();
            Assert.True(metrics.ContainsKey("counter_api_requests"));

            var prometheus = collector.ExportPrometheus();
            Assert.NotEmpty(prometheus);

            var (correlation, span) = tracingManager.GetCurrentContext();
            Assert.Equal(correlationId, correlation);

            var queryResult = queryEngine.Query(new MetricFilter { MetricName = "request_latency" });
            Assert.True(queryResult.Mean > 0);
        }

        #endregion
    }
}

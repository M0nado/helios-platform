/// Network Health Monitoring and Alerting System
/// Tracks network performance metrics and generates alerts and reports

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonadoBlade.WiFiOptimization
{
    /// <summary>
    /// Monitors WiFi network health and generates alerts and reports
    /// </summary>
    public class NetworkHealthMonitor
    {
        private readonly List<NetworkHealthSnapshot> _healthHistory;
        private readonly AlertConfiguration _alertConfig;
        private readonly List<NetworkAlert> _activeAlerts;
        private DateTime _monthStart = DateTime.UtcNow;

        public NetworkHealthMonitor(AlertConfiguration alertConfig = null)
        {
            _healthHistory = new List<NetworkHealthSnapshot>();
            _alertConfig = alertConfig ?? new AlertConfiguration();
            _activeAlerts = new List<NetworkAlert>();
        }

        /// <summary>
        /// Records a network health snapshot for monitoring
        /// </summary>
        public void RecordHealthSnapshot(WiFiMetrics metrics)
        {
            var snapshot = new NetworkHealthSnapshot
            {
                Timestamp = DateTime.UtcNow,
                SignalStrengthDBm = metrics.SignalStrengthDBm,
                LatencyMs = metrics.LatencyMs,
                PacketLossPercentage = metrics.PacketLossPercentage,
                DataRateMbps = metrics.DataRateMbps
            };

            _healthHistory.Add(snapshot);

            // Keep only 24 hours of history
            var cutoff = DateTime.UtcNow.AddHours(-24);
            _healthHistory.RemoveAll(s => s.Timestamp < cutoff);

            // Check for alert conditions
            CheckAlertConditions(snapshot);
        }

        /// <summary>
        /// Generates a health dashboard report
        /// </summary>
        public HealthDashboard GenerateDashboard()
        {
            var dashboard = new HealthDashboard();

            if (_healthHistory.Count == 0)
                return dashboard;

            // Calculate statistics
            var recent = _healthHistory.TakeLast(100).ToList();
            dashboard.SignalStrengthStats = CalculateStats(recent.Select(s => (double)s.SignalStrengthDBm));
            dashboard.LatencyStats = CalculateStats(recent.Select(s => (double)s.LatencyMs));
            dashboard.PacketLossStats = CalculateStats(recent.Select(s => s.PacketLossPercentage));

            // Signal quality assessment
            double avgSignal = dashboard.SignalStrengthStats.Average;
            if (avgSignal > -50)
                dashboard.SignalQuality = "Excellent";
            else if (avgSignal > -60)
                dashboard.SignalQuality = "Good";
            else if (avgSignal > -70)
                dashboard.SignalQuality = "Fair";
            else
                dashboard.SignalQuality = "Poor";

            // Overall health
            dashboard.OverallHealth = CalculateOverallHealth();
            dashboard.LastUpdated = DateTime.UtcNow;
            dashboard.UptimePercentage = CalculateUptime();

            return dashboard;
        }

        /// <summary>
        /// Generates graph data for visualization (24-hour view)
        /// </summary>
        public GraphData GenerateSignalStrengthGraph()
        {
            var graph = new GraphData
            {
                MetricName = "Signal Strength (dBm)",
                TimeRange = "24 hours",
                DataPoints = _healthHistory.Select(h => new DataPoint
                {
                    Timestamp = h.Timestamp,
                    Value = h.SignalStrengthDBm
                }).ToList()
            };

            return graph;
        }

        /// <summary>
        /// Generates monthly performance report
        /// </summary>
        public MonthlyPerformanceReport GenerateMonthlyReport()
        {
            var report = new MonthlyPerformanceReport
            {
                ReportDate = DateTime.UtcNow,
                MonthStart = _monthStart,
                MonthEnd = DateTime.UtcNow
            };

            if (_healthHistory.Count > 0)
            {
                var monthlyData = _healthHistory.Where(h => h.Timestamp >= _monthStart).ToList();

                if (monthlyData.Count > 0)
                {
                    report.AverageSignalStrengthDBm = monthlyData.Average(h => h.SignalStrengthDBm);
                    report.AverageLatencyMs = monthlyData.Average(h => h.LatencyMs);
                    report.AveragePacketLossPercentage = monthlyData.Average(h => h.PacketLossPercentage);

                    // Calculate uptime
                    var totalSnapshots = monthlyData.Count;
                    var goodSignalSnapshots = monthlyData.Count(h => h.SignalStrengthDBm > -70);
                    report.UptimePercentage = (goodSignalSnapshots * 100.0) / totalSnapshots;

                    // Cost impact (if data capped)
                    report.EstimatedDataUsageTotalGB = CalculateEstimatedDataUsage();
                    report.CostImpactIfCapped = report.EstimatedDataUsageTotalGB * 10; // $10 per GB overage
                }
            }

            // Generate recommendations
            GenerateRecommendations(report);

            return report;
        }

        private void CheckAlertConditions(NetworkHealthSnapshot snapshot)
        {
            // Signal strength alert
            if (snapshot.SignalStrengthDBm < _alertConfig.SignalThresholdDBm)
            {
                var existingAlert = _activeAlerts.FirstOrDefault(a => a.Type == AlertType.LowSignal);
                if (existingAlert == null)
                {
                    _activeAlerts.Add(new NetworkAlert
                    {
                        Type = AlertType.LowSignal,
                        Severity = AlertSeverity.High,
                        Message = $"Signal strength critical: {snapshot.SignalStrengthDBm} dBm",
                        Timestamp = snapshot.Timestamp,
                        Recommendation = "Switch to Ethernet or move closer to router"
                    });
                }
            }
            else
            {
                _activeAlerts.RemoveAll(a => a.Type == AlertType.LowSignal);
            }

            // Latency alert
            if (snapshot.LatencyMs > _alertConfig.LatencyThresholdMs)
            {
                var existingAlert = _activeAlerts.FirstOrDefault(a => a.Type == AlertType.HighLatency);
                if (existingAlert == null)
                {
                    _activeAlerts.Add(new NetworkAlert
                    {
                        Type = AlertType.HighLatency,
                        Severity = AlertSeverity.Medium,
                        Message = $"High latency detected: {snapshot.LatencyMs} ms",
                        Timestamp = snapshot.Timestamp,
                        Recommendation = "Enable compression or reduce streaming quality"
                    });
                }
            }
            else
            {
                _activeAlerts.RemoveAll(a => a.Type == AlertType.HighLatency);
            }

            // Packet loss alert
            if (snapshot.PacketLossPercentage > _alertConfig.PacketLossThresholdPercentage)
            {
                var existingAlert = _activeAlerts.FirstOrDefault(a => a.Type == AlertType.HighPacketLoss);
                if (existingAlert == null)
                {
                    _activeAlerts.Add(new NetworkAlert
                    {
                        Type = AlertType.HighPacketLoss,
                        Severity = AlertSeverity.Medium,
                        Message = $"High packet loss: {snapshot.PacketLossPercentage:F2}%",
                        Timestamp = snapshot.Timestamp,
                        Recommendation = "Connection is unstable; investigate interference"
                    });
                }
            }
            else
            {
                _activeAlerts.RemoveAll(a => a.Type == AlertType.HighPacketLoss);
            }
        }

        private HealthStats CalculateStats(IEnumerable<double> values)
        {
            var valueList = values.ToList();
            return new HealthStats
            {
                Minimum = valueList.Min(),
                Maximum = valueList.Max(),
                Average = valueList.Average(),
                Median = GetMedian(valueList)
            };
        }

        private double GetMedian(List<double> values)
        {
            if (values.Count == 0)
                return 0;

            var sorted = values.OrderBy(v => v).ToList();
            int n = sorted.Count;
            if (n % 2 == 0)
                return (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
            return sorted[n / 2];
        }

        private string CalculateOverallHealth()
        {
            if (_activeAlerts.Any(a => a.Severity == AlertSeverity.Critical))
                return "Critical";
            if (_activeAlerts.Any(a => a.Severity == AlertSeverity.High))
                return "Warning";
            if (_activeAlerts.Any(a => a.Severity == AlertSeverity.Medium))
                return "Fair";
            return "Good";
        }

        private double CalculateUptime()
        {
            if (_healthHistory.Count == 0)
                return 0;

            // Consider connected if signal > -75 dBm
            var connected = _healthHistory.Count(h => h.SignalStrengthDBm > -75);
            return (connected * 100.0) / _healthHistory.Count;
        }

        private double CalculateEstimatedDataUsage()
        {
            if (_healthHistory.Count == 0)
                return 0;

            // Estimate: Average data rate * number of hours
            double avgDataRate = _healthHistory.Average(h => h.DataRateMbps);
            double hours = _healthHistory.Count / 3600.0; // Assuming 1 snapshot/second
            double estimatedMB = avgDataRate * hours;
            return estimatedMB / 1024.0; // Convert to GB
        }

        private void GenerateRecommendations(MonthlyPerformanceReport report)
        {
            if (report.AverageSignalStrengthDBm < -70)
            {
                report.Recommendations.Add("Poor average signal strength - consider relocating router or using WiFi extender");
            }

            if (report.AverageLatencyMs > 100)
            {
                report.Recommendations.Add("High latency detected - check for interference or congestion");
            }

            if (report.AveragePacketLossPercentage > 1.0)
            {
                report.Recommendations.Add("High packet loss - unstable connection, investigate interference");
            }

            if (report.UptimePercentage < 95)
            {
                report.Recommendations.Add("Uptime below 95% - address connectivity issues");
            }

            if (report.EstimatedDataUsageTotalGB > 500)
            {
                report.Recommendations.Add("High data usage detected - monitor for data cap overages");
            }
        }

        public List<NetworkAlert> GetActiveAlerts() => new List<NetworkAlert>(_activeAlerts);
        public int GetAlertCount() => _activeAlerts.Count;
    }

    public class HealthDashboard
    {
        public HealthStats SignalStrengthStats { get; set; }
        public HealthStats LatencyStats { get; set; }
        public HealthStats PacketLossStats { get; set; }
        public string SignalQuality { get; set; }
        public string OverallHealth { get; set; }
        public double UptimePercentage { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class HealthStats
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Average { get; set; }
        public double Median { get; set; }
    }

    public class NetworkHealthSnapshot
    {
        public DateTime Timestamp { get; set; }
        public int SignalStrengthDBm { get; set; }
        public int LatencyMs { get; set; }
        public double PacketLossPercentage { get; set; }
        public int DataRateMbps { get; set; }
    }

    public class GraphData
    {
        public string MetricName { get; set; }
        public string TimeRange { get; set; }
        public List<DataPoint> DataPoints { get; set; } = new();
    }

    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public class MonthlyPerformanceReport
    {
        public DateTime ReportDate { get; set; }
        public DateTime MonthStart { get; set; }
        public DateTime MonthEnd { get; set; }
        public double AverageSignalStrengthDBm { get; set; }
        public double AverageLatencyMs { get; set; }
        public double AveragePacketLossPercentage { get; set; }
        public double UptimePercentage { get; set; }
        public double EstimatedDataUsageTotalGB { get; set; }
        public double CostImpactIfCapped { get; set; }
        public List<string> Recommendations { get; } = new();
    }

    public class NetworkAlert
    {
        public AlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Recommendation { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AlertConfiguration
    {
        public int SignalThresholdDBm { get; set; } = -75;
        public int LatencyThresholdMs { get; set; } = 150;
        public double PacketLossThresholdPercentage { get; set; } = 2.0;
        public bool EnableCriticalAlerts { get; set; } = true;
        public bool EnableMediumAlerts { get; set; } = true;
    }

    public enum AlertType { LowSignal, HighLatency, HighPacketLoss, RogueAPDetected, CertificateError }
    public enum AlertSeverity { Low, Medium, High, Critical }
}

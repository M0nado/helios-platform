/// WiFi Performance Optimization
/// Channel scanning, link quality monitoring, adaptive adjustments, and band steering

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonadoBlade.WiFiOptimization
{
    /// <summary>
    /// Optimizes WiFi performance through channel selection, band steering, and quality monitoring
    /// </summary>
    public class WiFiPerformanceOptimizer
    {
        private readonly PerformanceConfig _config;
        private readonly LinkQualityMonitor _linkMonitor;
        private readonly ChannelAnalyzer _channelAnalyzer;
        private readonly BandSteeringController _bandSteering;

        public WiFiPerformanceOptimizer(PerformanceConfig config = null)
        {
            _config = config ?? new PerformanceConfig();
            _linkMonitor = new LinkQualityMonitor();
            _channelAnalyzer = new ChannelAnalyzer();
            _bandSteering = new BandSteeringController();
        }

        /// <summary>
        /// Analyzes available channels and recommends the least congested option
        /// </summary>
        public ChannelRecommendation AnalyzeAndRecommendChannel()
        {
            var recommendation = new ChannelRecommendation();

            // Analyze 2.4GHz band
            var channels24GHz = new[] { 1, 6, 11 }; // Non-overlapping channels
            var channels24Analysis = _channelAnalyzer.AnalyzeChannels(channels24GHz, Band.Band2_4GHz);
            var best24 = channels24Analysis.OrderBy(c => c.CongestionLevel).First();

            // Analyze 5GHz band (more channels available)
            var channels5GHz = Enumerable.Range(36, 49).Where(c => c % 4 == 0).ToArray();
            var channels5Analysis = _channelAnalyzer.AnalyzeChannels(channels5GHz, Band.Band5GHz);
            var best5 = channels5Analysis.OrderBy(c => c.CongestionLevel).First();

            // Analyze 6GHz band if WiFi 6E available
            var channels6Analysis = new List<ChannelAnalysis>();
            if (_config.EnableWiFi6E)
            {
                var channels6GHz = Enumerable.Range(1, 233).Where(c => c % 4 == 0).ToArray();
                channels6Analysis = _channelAnalyzer.AnalyzeChannels(channels6GHz, Band.Band6GHz);
            }

            // Make recommendation (prefer 5GHz/6GHz for less congestion)
            if (best5.CongestionLevel < best24.CongestionLevel)
            {
                recommendation.RecommendedChannel = best5.Channel;
                recommendation.RecommendedBand = Band.Band5GHz;
                recommendation.Congestion = best5.CongestionLevel;
            }
            else if (channels6Analysis.Any())
            {
                var best6 = channels6Analysis.OrderBy(c => c.CongestionLevel).First();
                if (best6.CongestionLevel < best5.CongestionLevel)
                {
                    recommendation.RecommendedChannel = best6.Channel;
                    recommendation.RecommendedBand = Band.Band6GHz;
                    recommendation.Congestion = best6.CongestionLevel;
                }
                else
                {
                    recommendation.RecommendedChannel = best5.Channel;
                    recommendation.RecommendedBand = Band.Band5GHz;
                    recommendation.Congestion = best5.CongestionLevel;
                }
            }
            else
            {
                recommendation.RecommendedChannel = best24.Channel;
                recommendation.RecommendedBand = Band.Band2_4GHz;
                recommendation.Congestion = best24.CongestionLevel;
            }

            return recommendation;
        }

        /// <summary>
        /// Monitors link quality and initiates adaptive adjustments
        /// </summary>
        public LinkQualityReport MonitorLinkQuality(WiFiMetrics metrics)
        {
            var report = _linkMonitor.EvaluateQuality(metrics);

            // Apply adaptive adjustments based on conditions
            if (metrics.SignalStrengthDBm < _config.LowSignalThresholdDBm)
            {
                report.Recommendations.Add("Switch to Ethernet or move closer to router");
                report.SuggestedAction = AdaptiveAction.SwitchToEthernet;
            }

            if (metrics.LatencyMs > _config.HighLatencyThresholdMs)
            {
                report.Recommendations.Add("Enable payload compression to reduce overhead");
                report.SuggestedAction = AdaptiveAction.ReducePayloadSize;
            }

            if (metrics.PacketLossPercentage > _config.PacketLossThreshold)
            {
                report.Recommendations.Add("Reduce refresh rates to improve reliability");
                report.SuggestedAction = AdaptiveAction.ReduceRefreshRate;
            }

            return report;
        }

        /// <summary>
        /// Performs band steering to optimize connection (prefer 5GHz if strong, fallback to 2.4GHz if weak)
        /// </summary>
        public BandSteeringDecision PerformBandSteering(WiFiMetrics metrics24GHz, WiFiMetrics metrics5GHz, 
            bool manualOverride = false, Band forcedBand = Band.Unknown)
        {
            var decision = new BandSteeringDecision();

            if (manualOverride && forcedBand != Band.Unknown)
            {
                decision.SelectedBand = forcedBand;
                decision.Reason = "Manual override by user";
                return decision;
            }

            // Score each band
            var score24 = CalculateBandScore(metrics24GHz, Band.Band2_4GHz);
            var score5 = CalculateBandScore(metrics5GHz, Band.Band5GHz);

            if (score5 > score24)
            {
                decision.SelectedBand = Band.Band5GHz;
                decision.Reason = $"5GHz band preferred (score: {score5:F2} vs 2.4GHz: {score24:F2})";
            }
            else
            {
                decision.SelectedBand = Band.Band2_4GHz;
                decision.Reason = $"2.4GHz band selected (score: {score24:F2} vs 5GHz: {score5:F2})";
            }

            return decision;
        }

        private double CalculateBandScore(WiFiMetrics metrics, Band band)
        {
            double score = 100.0;

            // Signal strength (primary factor)
            double signalWeight = 0.4;
            double signalScore = Math.Max(0, (metrics.SignalStrengthDBm + 100) / 100.0) * 100;
            score = score * (1 - signalWeight) + (signalScore * signalWeight);

            // Latency (secondary factor)
            double latencyWeight = 0.3;
            double latencyScore = Math.Max(0, (200.0 - metrics.LatencyMs) / 200.0) * 100;
            score = score * (1 - latencyWeight) + (latencyScore * latencyWeight);

            // Packet loss (tertiary factor)
            double lossWeight = 0.3;
            double lossScore = (1.0 - metrics.PacketLossPercentage) * 100;
            score = score * (1 - lossWeight) + (lossScore * lossWeight);

            return score;
        }

        public WiFiMetrics GetCurrentMetrics() => _linkMonitor.GetLastMetrics();
    }

    /// <summary>
    /// Monitors WiFi link quality over time
    /// </summary>
    public class LinkQualityMonitor
    {
        private readonly List<WiFiMetrics> _metricsHistory;
        private const int MAX_HISTORY = 1000;

        public LinkQualityMonitor()
        {
            _metricsHistory = new List<WiFiMetrics>();
        }

        public LinkQualityReport EvaluateQuality(WiFiMetrics metrics)
        {
            _metricsHistory.Add(metrics);
            if (_metricsHistory.Count > MAX_HISTORY)
            {
                _metricsHistory.RemoveAt(0);
            }

            var report = new LinkQualityReport();

            // Evaluate signal strength
            if (metrics.SignalStrengthDBm > -50)
                report.SignalQuality = Quality.Excellent;
            else if (metrics.SignalStrengthDBm > -60)
                report.SignalQuality = Quality.Good;
            else if (metrics.SignalStrengthDBm > -70)
                report.SignalQuality = Quality.Fair;
            else
                report.SignalQuality = Quality.Poor;

            // Evaluate latency
            if (metrics.LatencyMs < 20)
                report.LatencyQuality = Quality.Excellent;
            else if (metrics.LatencyMs < 50)
                report.LatencyQuality = Quality.Good;
            else if (metrics.LatencyMs < 100)
                report.LatencyQuality = Quality.Fair;
            else
                report.LatencyQuality = Quality.Poor;

            // Evaluate packet loss
            if (metrics.PacketLossPercentage < 0.1)
                report.ReliabilityQuality = Quality.Excellent;
            else if (metrics.PacketLossPercentage < 0.5)
                report.ReliabilityQuality = Quality.Good;
            else if (metrics.PacketLossPercentage < 2.0)
                report.ReliabilityQuality = Quality.Fair;
            else
                report.ReliabilityQuality = Quality.Poor;

            // Overall quality
            var qualityLevels = new[] 
            { 
                report.SignalQuality, 
                report.LatencyQuality, 
                report.ReliabilityQuality 
            };
            report.OverallQuality = (Quality)qualityLevels.Cast<int>().Average();

            // Calculate trend
            if (_metricsHistory.Count > 10)
            {
                var recent = _metricsHistory.TakeLast(10);
                var older = _metricsHistory.SkipLast(10).TakeLast(10);

                double recentSignalAvg = recent.Average(m => m.SignalStrengthDBm);
                double olderSignalAvg = older.Average(m => m.SignalStrengthDBm);

                if (recentSignalAvg > olderSignalAvg + 3)
                    report.Trend = Trend.Improving;
                else if (recentSignalAvg < olderSignalAvg - 3)
                    report.Trend = Trend.Degrading;
                else
                    report.Trend = Trend.Stable;
            }

            return report;
        }

        public WiFiMetrics GetLastMetrics() => _metricsHistory.LastOrDefault();

        public List<WiFiMetrics> GetMetricsHistory(int seconds = 3600)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-seconds);
            return _metricsHistory.Where(m => m.Timestamp > cutoff).ToList();
        }
    }

    /// <summary>
    /// Analyzes WiFi channels for congestion levels
    /// </summary>
    public class ChannelAnalyzer
    {
        private static readonly Random _random = new Random();

        public List<ChannelAnalysis> AnalyzeChannels(int[] channels, Band band)
        {
            var analysis = new List<ChannelAnalysis>();

            foreach (var channel in channels)
            {
                // In real implementation, would do actual channel scanning
                // For now, simulate based on channel (lower channels typically more congested in 2.4GHz)
                double congestion = 0.5;

                if (band == Band.Band2_4GHz)
                {
                    congestion = channel == 1 ? 0.8 : channel == 6 ? 0.7 : 0.5;
                }
                else if (band == Band.Band5GHz)
                {
                    congestion = 0.3 + _random.NextDouble() * 0.3; // 5GHz typically less congested
                }
                else if (band == Band.Band6GHz)
                {
                    congestion = 0.1 + _random.NextDouble() * 0.2; // 6GHz much less congested
                }

                analysis.Add(new ChannelAnalysis
                {
                    Channel = channel,
                    Band = band,
                    CongestionLevel = congestion,
                    AvailableNetworks = (int)(congestion * 10)
                });
            }

            return analysis;
        }
    }

    /// <summary>
    /// Controls band steering decisions
    /// </summary>
    public class BandSteeringController
    {
        public void ApplyBandSteeringDecision(BandSteeringDecision decision)
        {
            // In real implementation, would apply to WiFi radio
        }
    }

    public enum Band { Band2_4GHz, Band5GHz, Band6GHz, Unknown }
    public enum Quality { Poor = 0, Fair = 1, Good = 2, Excellent = 3 }
    public enum Trend { Degrading, Stable, Improving }
    public enum AdaptiveAction { None, SwitchToEthernet, ReducePayloadSize, ReduceRefreshRate, EnableCompression }

    public class PerformanceConfig
    {
        public int LowSignalThresholdDBm { get; set; } = -70;
        public int HighLatencyThresholdMs { get; set; } = 100;
        public double PacketLossThreshold { get; set; } = 0.02; // 2%
        public bool EnableWiFi6E { get; set; } = true;
        public string PreferredBand { get; set; } = "5GHz";
    }

    public class WiFiMetrics
    {
        public int SignalStrengthDBm { get; set; }
        public int LatencyMs { get; set; }
        public double PacketLossPercentage { get; set; }
        public int DataRateMbps { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ChannelRecommendation
    {
        public int RecommendedChannel { get; set; }
        public Band RecommendedBand { get; set; }
        public double Congestion { get; set; }
    }

    public class ChannelAnalysis
    {
        public int Channel { get; set; }
        public Band Band { get; set; }
        public double CongestionLevel { get; set; } // 0-1
        public int AvailableNetworks { get; set; }
    }

    public class LinkQualityReport
    {
        public Quality SignalQuality { get; set; }
        public Quality LatencyQuality { get; set; }
        public Quality ReliabilityQuality { get; set; }
        public Quality OverallQuality { get; set; }
        public Trend Trend { get; set; } = Trend.Stable;
        public List<string> Recommendations { get; } = new();
        public AdaptiveAction SuggestedAction { get; set; }
    }

    public class BandSteeringDecision
    {
        public Band SelectedBand { get; set; }
        public string Reason { get; set; }
        public DateTime DecisionTime { get; set; } = DateTime.UtcNow;
    }
}

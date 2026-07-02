/// WiFi Optimization Test Suite
/// Comprehensive unit and integration tests for all components

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonadoBlade.WiFiOptimization.Tests
{
    public class WiFiSecurityEnforcerTests
    {
        [Fact]
        public void ValidateSecurityStandards_WPA3Required_ShouldRejectWPA2()
        {
            var config = new WiFiSecurityConfig { RequireWPA3 = true };
            var enforcer = new WiFiSecurityEnforcer(config);

            var profile = new NetworkProfile
            {
                SSID = "TestNetwork",
                SecurityType = "WPA2",
                StandardsSupported = new List<string> { "802.11n", "802.11ac" }
            };

            var result = enforcer.ValidateSecurityStandards(profile);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void ValidateSecurityStandards_WPA3Enterprise_ShouldApprove()
        {
            var config = new WiFiSecurityConfig { RequireWPA3 = true };
            var enforcer = new WiFiSecurityEnforcer(config);

            var profile = new NetworkProfile
            {
                SSID = "CorporateNetwork",
                SecurityType = "WPA3-Enterprise",
                StandardsSupported = new List<string> { "802.11n", "802.11ac", "802.11ax" }
            };

            var result = enforcer.ValidateSecurityStandards(profile);

            // Should have warnings but not errors
            Assert.True(result.IsValid);
        }

        [Fact]
        public void AddWhitelistedMAC_ValidMAC_ShouldSucceed()
        {
            var config = new WiFiSecurityConfig { MacFiltering = true };
            var enforcer = new WiFiSecurityEnforcer(config);

            bool success = enforcer.AddWhitelistedMAC("AA:BB:CC:DD:EE:FF");

            Assert.True(success);
            Assert.Single(enforcer.GetWhitelistedMACs());
        }

        [Fact]
        public void AddWhitelistedMAC_InvalidMAC_ShouldThrow()
        {
            var enforcer = new WiFiSecurityEnforcer(new WiFiSecurityConfig());

            Assert.Throws<ArgumentException>(() => enforcer.AddWhitelistedMAC("INVALID"));
        }

        [Fact]
        public void CheckMACFilter_WhitelistedMAC_ShouldAllow()
        {
            var config = new WiFiSecurityConfig { MacFiltering = true };
            var enforcer = new WiFiSecurityEnforcer(config);
            enforcer.AddWhitelistedMAC("AA:BB:CC:DD:EE:FF");

            var result = enforcer.CheckMACFilter("AA:BB:CC:DD:EE:FF");

            Assert.True(result.Allowed);
        }

        [Fact]
        public void CheckMACFilter_UnwhitelistedMAC_ShouldBlock()
        {
            var config = new WiFiSecurityConfig { MacFiltering = true };
            var enforcer = new WiFiSecurityEnforcer(config);

            var result = enforcer.CheckMACFilter("AA:BB:CC:DD:EE:FF");

            Assert.False(result.Allowed);
        }

        [Fact]
        public void ValidateCertificatePinning_NoPinnedCert_ShouldAlwaysPass()
        {
            var enforcer = new WiFiSecurityEnforcer(new WiFiSecurityConfig());

            // No certificate configured
            Assert.True(enforcer.ValidateCertificatePinning("example.com", null));
        }

        [Fact]
        public void ConfigureDNSOverHTTPS_ValidEndpoint_ShouldSucceed()
        {
            var enforcer = new WiFiSecurityEnforcer(new WiFiSecurityConfig());

            var result = enforcer.ConfigureDNSOverHTTPS("https://dns.cloudflare.com/dns-query");

            Assert.True(result.IsSuccessful);
            Assert.True(result.Enabled);
        }

        [Fact]
        public void ConfigureDNSOverHTTPS_InvalidEndpoint_ShouldFail()
        {
            var enforcer = new WiFiSecurityEnforcer(new WiFiSecurityConfig());

            var result = enforcer.ConfigureDNSOverHTTPS("http://insecure.dns.com"); // HTTP not HTTPS

            Assert.False(result.IsSuccessful);
        }

        [Fact]
        public void EnableDNSSECValidation_ShouldReturnSuccess()
        {
            var enforcer = new WiFiSecurityEnforcer(new WiFiSecurityConfig());

            var result = enforcer.EnableDNSSECValidation();

            Assert.True(result.IsSuccessful);
            Assert.True(result.DNSSECEnabled);
        }
    }

    public class WiFiNetworkDetectorTests
    {
        [Fact]
        public void ClassifyNetwork_EnterpriseSSID_ShouldIdentifyAsCorporate()
        {
            var detector = new WiFiNetworkDetector();

            var profile = new NetworkProfile
            {
                SSID = "CORP-Network",
                SecurityType = "WPA3-Enterprise",
                StandardsSupported = new List<string> { "802.11ax" }
            };

            var classification = detector.ClassifyNetwork(profile);

            Assert.Equal(NetworkType.Corporate, classification.Type);
        }

        [Fact]
        public void ClassifyNetwork_PublicSSID_ShouldIdentifyAsPublic()
        {
            var detector = new WiFiNetworkDetector();

            var profile = new NetworkProfile
            {
                SSID = "FREE_WIFI",
                SecurityType = "None",
                StandardsSupported = new List<string>()
            };

            var classification = detector.ClassifyNetwork(profile);

            Assert.Equal(NetworkType.Public, classification.Type);
            Assert.True(classification.RequireVPN);
        }

        [Fact]
        public void ClassifyNetwork_HomeSSID_ShouldIdentifyAsHome()
        {
            var detector = new WiFiNetworkDetector();

            var profile = new NetworkProfile
            {
                SSID = "My-Home-WiFi",
                SecurityType = "WPA2",
                StandardsSupported = new List<string> { "802.11ac" }
            };

            var classification = detector.ClassifyNetwork(profile);

            Assert.Equal(NetworkType.Home, classification.Type);
        }

        [Fact]
        public void ClassifyNetwork_UnknownNetwork_ShouldReturnHighSecurity()
        {
            var detector = new WiFiNetworkDetector();

            var profile = new NetworkProfile
            {
                SSID = "RandomNetwork123",
                SecurityType = "WPA2",
                StandardsSupported = new List<string>()
            };

            var classification = detector.ClassifyNetwork(profile);

            Assert.Equal(NetworkType.Unknown, classification.Type);
            Assert.Equal(SecurityPosture.VeryHigh, classification.SecurityPosture);
            Assert.True(classification.LocalOnlyMode);
        }

        [Fact]
        public void DetectRogueAPs_SignalChanged_ShouldFlagAsRogue()
        {
            var detector = new WiFiNetworkDetector();

            var profile1 = new NetworkProfile { SSID = "Test", SignalStrengthDBm = -50 };
            detector.ClassifyNetwork(profile1);

            var profile2 = new NetworkProfile { SSID = "Test", SignalStrengthDBm = -75 }; // Changed significantly
            var rogue = detector.DetectRogueAPs(profile2);

            Assert.True(rogue.RogueAPLikelihood > 0);
        }
    }

    public class WiFiPerformanceOptimizerTests
    {
        [Fact]
        public void AnalyzeAndRecommendChannel_ShouldReturnChannel()
        {
            var config = new PerformanceConfig { EnableWiFi6E = false };
            var optimizer = new WiFiPerformanceOptimizer(config);

            var recommendation = optimizer.AnalyzeAndRecommendChannel();

            Assert.NotEqual(0, recommendation.RecommendedChannel);
            Assert.True(recommendation.Congestion >= 0 && recommendation.Congestion <= 1);
        }

        [Fact]
        public void MonitorLinkQuality_LowSignal_ShouldRecommendSwitchToEthernet()
        {
            var config = new PerformanceConfig { LowSignalThresholdDBm = -70 };
            var optimizer = new WiFiPerformanceOptimizer(config);

            var metrics = new WiFiMetrics
            {
                SignalStrengthDBm = -80,
                LatencyMs = 50,
                PacketLossPercentage = 0.5
            };

            var report = optimizer.MonitorLinkQuality(metrics);

            Assert.Equal(AdaptiveAction.SwitchToEthernet, report.SuggestedAction);
        }

        [Fact]
        public void MonitorLinkQuality_HighLatency_ShouldRecommendCompression()
        {
            var config = new PerformanceConfig { HighLatencyThresholdMs = 100 };
            var optimizer = new WiFiPerformanceOptimizer(config);

            var metrics = new WiFiMetrics
            {
                SignalStrengthDBm = -50,
                LatencyMs = 150,
                PacketLossPercentage = 0.5
            };

            var report = optimizer.MonitorLinkQuality(metrics);

            Assert.Equal(AdaptiveAction.ReducePayloadSize, report.SuggestedAction);
        }

        [Fact]
        public void PerformBandSteering_Strong5GHz_ShouldPrefer5GHz()
        {
            var optimizer = new WiFiPerformanceOptimizer();

            var metrics24 = new WiFiMetrics
            {
                SignalStrengthDBm = -70,
                LatencyMs = 50,
                PacketLossPercentage = 1.0
            };

            var metrics5 = new WiFiMetrics
            {
                SignalStrengthDBm = -40,
                LatencyMs = 20,
                PacketLossPercentage = 0.1
            };

            var decision = optimizer.PerformBandSteering(metrics24, metrics5);

            Assert.Equal(Band.Band5GHz, decision.SelectedBand);
        }

        [Fact]
        public void PerformBandSteering_ManualOverride_ShouldUseForcedBand()
        {
            var optimizer = new WiFiPerformanceOptimizer();

            var metrics24 = new WiFiMetrics { SignalStrengthDBm = -40 };
            var metrics5 = new WiFiMetrics { SignalStrengthDBm = -30 };

            var decision = optimizer.PerformBandSteering(metrics24, metrics5, manualOverride: true, 
                forcedBand: Band.Band2_4GHz);

            Assert.Equal(Band.Band2_4GHz, decision.SelectedBand);
            Assert.Contains("Manual", decision.Reason);
        }
    }

    public class VPNIntegrationLayerTests
    {
        [Fact]
        public void DetermineVPNRequirement_PublicNetwork_ShouldRequireVPN()
        {
            var layer = new VPNIntegrationLayer();

            var classification = new NetworkClassification
            {
                Type = NetworkType.Public,
                Allow802_1X = false
            };

            var requirement = layer.DetermineVPNRequirement(classification);

            Assert.True(requirement.VPNRequired);
            Assert.Equal(VPNUrgency.Immediate, requirement.Urgency);
        }

        [Fact]
        public void DetermineVPNRequirement_HomeNetwork_ShouldNotRequireVPN()
        {
            var layer = new VPNIntegrationLayer();

            var classification = new NetworkClassification { Type = NetworkType.Home };

            var requirement = layer.DetermineVPNRequirement(classification);

            Assert.False(requirement.VPNRequired);
        }

        [Fact]
        public void DetermineVPNRequirement_UnknownNetwork_ShouldRequireVPN()
        {
            var layer = new VPNIntegrationLayer();

            var classification = new NetworkClassification { Type = NetworkType.Unknown };

            var requirement = layer.DetermineVPNRequirement(classification);

            Assert.True(requirement.VPNRequired);
            Assert.Equal(VPNUrgency.Immediate, requirement.Urgency);
        }

        [Fact]
        public async Task EstablishVPNConnection_InvalidConfig_ShouldFail()
        {
            var layer = new VPNIntegrationLayer();

            var result = await layer.EstablishVPNConnection("NonExistent");

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }
    }

    public class NetworkOptimizationEngineTests
    {
        [Fact]
        public void OptimizeDNS_ShouldReturnPreferredServer()
        {
            var engine = new NetworkOptimizationEngine();

            var result = engine.OptimizeDNS();

            Assert.NotNull(result.PreferredDNSServer);
            Assert.True(result.LocalCacheEnabled);
            Assert.True(result.DNSSECEnabled);
        }

        [Fact]
        public void AllocateBandwidth_ShouldCreatePlan()
        {
            var engine = new NetworkOptimizationEngine();

            var plan = engine.AllocateBandwidth(100);

            Assert.Equal(100, plan.TotalBandwidthMbps);
            Assert.Equal(10, plan.SystemReserveMbps);
            Assert.Equal(90, plan.UserAllocationMbps);
        }

        [Fact]
        public void RecommendCompression_TextData_ShouldRecommendBrotli()
        {
            var engine = new NetworkOptimizationEngine();

            var recommendation = engine.RecommendCompression(10000, isVideo: false, isImage: false);

            Assert.Equal(CompressionAlgorithm.Brotli, recommendation.Algorithm);
            Assert.True(recommendation.ShouldCompress);
        }

        [Fact]
        public void RecommendCompression_VideoData_ShouldNotCompress()
        {
            var engine = new NetworkOptimizationEngine();

            var recommendation = engine.RecommendCompression(10000000, isVideo: true, isImage: false);

            Assert.False(recommendation.ShouldCompress);
        }

        [Fact]
        public void CompressPayload_BrotliCompression_ShouldReduceSize()
        {
            var engine = new NetworkOptimizationEngine();
            byte[] data = new byte[1000];

            var result = engine.CompressPayload(data, CompressionAlgorithm.Brotli);

            Assert.Less(result.CompressedSizeBytes, result.OriginalSizeBytes);
            Assert.True(result.CompressionRatio > 0);
        }
    }

    public class NetworkHealthMonitorTests
    {
        [Fact]
        public void RecordHealthSnapshot_ShouldStoreMetrics()
        {
            var monitor = new NetworkHealthMonitor();

            var metrics = new WiFiMetrics
            {
                SignalStrengthDBm = -50,
                LatencyMs = 30,
                PacketLossPercentage = 0.1,
                DataRateMbps = 100
            };

            monitor.RecordHealthSnapshot(metrics);
            var dashboard = monitor.GenerateDashboard();

            Assert.NotNull(dashboard.SignalStrengthStats);
            Assert.Equal(-50, dashboard.SignalStrengthStats.Average);
        }

        [Fact]
        public void RecordHealthSnapshot_LowSignal_ShouldGenerateAlert()
        {
            var config = new AlertConfiguration { SignalThresholdDBm = -70 };
            var monitor = new NetworkHealthMonitor(config);

            var metrics = new WiFiMetrics
            {
                SignalStrengthDBm = -80,
                LatencyMs = 30,
                PacketLossPercentage = 0.1
            };

            monitor.RecordHealthSnapshot(metrics);
            var alerts = monitor.GetActiveAlerts();

            Assert.NotEmpty(alerts);
            Assert.Equal(AlertType.LowSignal, alerts.First().Type);
        }

        [Fact]
        public void RecordHealthSnapshot_HighLatency_ShouldGenerateAlert()
        {
            var config = new AlertConfiguration { LatencyThresholdMs = 100 };
            var monitor = new NetworkHealthMonitor(config);

            var metrics = new WiFiMetrics
            {
                SignalStrengthDBm = -50,
                LatencyMs = 150,
                PacketLossPercentage = 0.1
            };

            monitor.RecordHealthSnapshot(metrics);
            var alerts = monitor.GetActiveAlerts();

            Assert.NotEmpty(alerts);
            Assert.Any(alerts, a => a.Type == AlertType.HighLatency);
        }

        [Fact]
        public void GenerateDashboard_ShouldCalculateQuality()
        {
            var monitor = new NetworkHealthMonitor();

            var excellentMetrics = new WiFiMetrics
            {
                SignalStrengthDBm = -40,
                LatencyMs = 15,
                PacketLossPercentage = 0.05
            };

            monitor.RecordHealthSnapshot(excellentMetrics);
            var dashboard = monitor.GenerateDashboard();

            Assert.Equal("Excellent", dashboard.SignalQuality);
        }

        [Fact]
        public void GenerateMonthlyReport_ShouldIncludeRecommendations()
        {
            var monitor = new NetworkHealthMonitor();

            var poorMetrics = new WiFiMetrics
            {
                SignalStrengthDBm = -80,
                LatencyMs = 200,
                PacketLossPercentage = 3.0,
                DataRateMbps = 5
            };

            for (int i = 0; i < 100; i++)
            {
                monitor.RecordHealthSnapshot(poorMetrics);
            }

            var report = monitor.GenerateMonthlyReport();

            Assert.NotEmpty(report.Recommendations);
        }

        [Fact]
        public void GenerateSignalStrengthGraph_ShouldReturnDataPoints()
        {
            var monitor = new NetworkHealthMonitor();

            var metrics = new WiFiMetrics { SignalStrengthDBm = -50 };
            monitor.RecordHealthSnapshot(metrics);

            var graph = monitor.GenerateSignalStrengthGraph();

            Assert.NotEmpty(graph.DataPoints);
            Assert.Contains("Signal Strength", graph.MetricName);
        }
    }

    public class IntegrationTests
    {
        [Fact]
        public void FullNetworkOptimizationWorkflow_ShouldSucceed()
        {
            // Simulate complete workflow
            var detector = new WiFiNetworkDetector();
            var security = new WiFiSecurityEnforcer(new WiFiSecurityConfig());
            var optimizer = new WiFiPerformanceOptimizer();
            var network = new NetworkOptimizationEngine();
            var monitor = new NetworkHealthMonitor();

            // 1. Detect network
            var profile = new NetworkProfile
            {
                SSID = "TestCorp",
                SecurityType = "WPA3-Enterprise",
                SignalStrengthDBm = -50,
                StandardsSupported = new List<string> { "802.11ax" }
            };

            var classification = detector.ClassifyNetwork(profile);
            Assert.Equal(NetworkType.Corporate, classification.Type);

            // 2. Validate security
            var secResult = security.ValidateSecurityStandards(profile);
            Assert.True(secResult.IsValid);

            // 3. Optimize performance
            var channelRec = optimizer.AnalyzeAndRecommendChannel();
            Assert.NotEqual(0, channelRec.RecommendedChannel);

            // 4. Optimize network
            var dns = network.OptimizeDNS();
            Assert.NotNull(dns.PreferredDNSServer);

            // 5. Monitor health
            var metrics = new WiFiMetrics
            {
                SignalStrengthDBm = profile.SignalStrengthDBm,
                LatencyMs = 25,
                PacketLossPercentage = 0.05,
                DataRateMbps = 200
            };
            monitor.RecordHealthSnapshot(metrics);

            var dashboard = monitor.GenerateDashboard();
            Assert.NotNull(dashboard.OverallHealth);
        }

        [Fact]
        public void NetworkClassificationAndBehavior_ShouldAdaptToNetworkType()
        {
            var vpn = new VPNIntegrationLayer();
            var detector = new WiFiNetworkDetector();

            // Public network
            var publicProfile = new NetworkProfile { SSID = "FREE_WIFI", SecurityType = "None" };
            var publicClass = detector.ClassifyNetwork(publicProfile);
            var publicVPN = vpn.DetermineVPNRequirement(publicClass);
            Assert.True(publicVPN.VPNRequired);

            // Home network
            var homeProfile = new NetworkProfile { SSID = "Home-WiFi", SecurityType = "WPA2" };
            var homeClass = detector.ClassifyNetwork(homeProfile);
            var homeVPN = vpn.DetermineVPNRequirement(homeClass);
            Assert.False(homeVPN.VPNRequired);
        }
    }
}

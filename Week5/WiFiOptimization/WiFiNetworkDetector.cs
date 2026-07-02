/// WiFi Network Detection and Profile Classification
/// Automatically detects network type and adapts security/performance settings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonadoBlade.WiFiOptimization
{
    /// <summary>
    /// Detects and classifies WiFi networks, adapting behavior based on security level
    /// </summary>
    public class WiFiNetworkDetector
    {
        private readonly Dictionary<string, NetworkMetadata> _networkHistory;
        private readonly CaptivePortalDetector _captivePortalDetector;
        private readonly RogueAPDetector _rogueAPDetector;

        public WiFiNetworkDetector()
        {
            _networkHistory = new Dictionary<string, NetworkMetadata>();
            _captivePortalDetector = new CaptivePortalDetector();
            _rogueAPDetector = new RogueAPDetector();
        }

        /// <summary>
        /// Classifies a network as Corporate, Public, Home, or Unknown
        /// </summary>
        public NetworkClassification ClassifyNetwork(NetworkProfile profile)
        {
            var classification = new NetworkClassification { SSID = profile.SSID };

            // Check for enterprise characteristics
            if (IsEnterpriseNetwork(profile))
            {
                classification.Type = NetworkType.Corporate;
                classification.SecurityPosture = SecurityPosture.Enterprise;
                classification.RequireVPN = false; // Enterprise has built-in security
                classification.Allow802_1X = true;
                classification.AllowCloudSync = true;
            }
            // Check for public WiFi characteristics
            else if (IsPublicNetwork(profile))
            {
                classification.Type = NetworkType.Public;
                classification.SecurityPosture = SecurityPosture.High;
                classification.RequireVPN = true;
                classification.LocalOnlyMode = true;
                classification.AllowCloudSync = false;
            }
            // Check for home network characteristics
            else if (IsHomeNetwork(profile))
            {
                classification.Type = NetworkType.Home;
                classification.SecurityPosture = SecurityPosture.Medium;
                classification.RequireVPN = false;
                classification.AllowLLMAPICalls = true;
                classification.AllowCloudSync = true;
            }
            // Unknown networks get highest security
            else
            {
                classification.Type = NetworkType.Unknown;
                classification.SecurityPosture = SecurityPosture.VeryHigh;
                classification.LocalOnlyMode = true;
                classification.AllowExternalCalls = false;
            }

            // Store in history for pattern detection
            StoreNetworkMetadata(profile.SSID, classification);

            return classification;
        }

        /// <summary>
        /// Detects enterprise networks by checking for 802.1X support and other markers
        /// </summary>
        private bool IsEnterpriseNetwork(NetworkProfile profile)
        {
            // Check for enterprise SSID patterns
            var enterprisePatterns = new[] 
            { 
                "CORP", "ENTERPRISE", "WORK", "BUSINESS", "OFFICE", "EMPLOYEE" 
            };

            bool hasEnterpriseSSID = enterprisePatterns.Any(p => 
                profile.SSID.Contains(p, StringComparison.OrdinalIgnoreCase)
            );

            // Check for WPA3-Enterprise or 802.1X support
            bool hasEnterpriseAuth = profile.SecurityType?.Contains("Enterprise") == true 
                || profile.SecurityType?.Contains("802.1X") == true;

            // Check standards - enterprise networks typically support modern standards
            bool hasModernStandards = profile.StandardsSupported?.Contains("802.11ax") == true;

            return (hasEnterpriseSSID || hasEnterpriseAuth) && hasModernStandards;
        }

        /// <summary>
        /// Detects public WiFi networks
        /// </summary>
        private bool IsPublicNetwork(NetworkProfile profile)
        {
            // Check for public network patterns
            var publicPatterns = new[] 
            { 
                "PUBLIC", "FREE", "GUEST", "OPEN", "AIRPORT", "HOTEL", 
                "CAFE", "COFFEE", "STARBUCKS", "McDONALDS", "WIFI"
            };

            if (publicPatterns.Any(p => 
                profile.SSID.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Open/WEP networks are likely public
            if (string.IsNullOrEmpty(profile.SecurityType) || 
                profile.SecurityType.Contains("WEP") || 
                profile.SecurityType.Contains("None"))
            {
                return true;
            }

            // Check for captive portal
            if (_captivePortalDetector.DetectCaptivePortal(profile.SSID))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Detects home networks (typically known and trusted)
        /// </summary>
        private bool IsHomeNetwork(NetworkProfile profile)
        {
            // Check for home-like SSID patterns
            var homePatterns = new[] 
            { 
                "HOME", "HOUSE", "FAMILY", "PERSONAL", "RESIDENTIAL", "HOME-" 
            };

            bool hasHomeSSID = homePatterns.Any(p => 
                profile.SSID.Contains(p, StringComparison.OrdinalIgnoreCase)
            );

            // Has reasonable security (WPA2/WPA3)
            bool hasReasonableSecurity = profile.SecurityType?.Contains("WPA") == true;

            // Not open/public
            bool notPublic = !IsPublicNetwork(profile);

            // Check history - previously connected networks
            bool isPreviouslyTrusted = _networkHistory.ContainsKey(profile.SSID) &&
                _networkHistory[profile.SSID].PreviouslySeen;

            return (hasHomeSSID || isPreviouslyTrusted) && hasReasonableSecurity && notPublic;
        }

        /// <summary>
        /// Detects captive portals that may be malicious
        /// </summary>
        public CaptivePortalDetectionResult DetectCaptivePortals(NetworkProfile profile)
        {
            return _captivePortalDetector.Detect(profile);
        }

        /// <summary>
        /// Detects rogue access points by comparing signal history
        /// </summary>
        public RogueAPDetectionResult DetectRogueAPs(NetworkProfile profile)
        {
            return _rogueAPDetector.Detect(profile, _networkHistory);
        }

        /// <summary>
        /// Alerts if network changed unexpectedly (e.g., different BSSID with same SSID)
        /// </summary>
        public List<SecurityAlert> CheckForNetworkChanges(NetworkProfile currentProfile)
        {
            var alerts = new List<SecurityAlert>();

            if (!_networkHistory.TryGetValue(currentProfile.SSID, out var metadata))
            {
                return alerts; // New network, no change to detect
            }

            // Check if BSSID (MAC address) changed unexpectedly
            if (!string.IsNullOrEmpty(metadata.LastBSSID) && 
                metadata.LastBSSID != currentProfile.SSID) // In real impl, would compare actual BSSID
            {
                alerts.Add(new SecurityAlert
                {
                    Severity = AlertSeverity.High,
                    Message = $"Network {currentProfile.SSID} BSSID changed unexpectedly",
                    Type = AlertType.RogueAPDetected,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check if signal strength changed drastically
            if (Math.Abs(currentProfile.SignalStrengthDBm - metadata.LastSignalStrengthDBm) > 20)
            {
                alerts.Add(new SecurityAlert
                {
                    Severity = AlertSeverity.Medium,
                    Message = $"Network {currentProfile.SSID} signal strength changed significantly",
                    Type = AlertType.SignalAnomalyDetected,
                    Timestamp = DateTime.UtcNow
                });
            }

            return alerts;
        }

        private void StoreNetworkMetadata(string ssid, NetworkClassification classification)
        {
            _networkHistory[ssid] = new NetworkMetadata
            {
                SSID = ssid,
                Type = classification.Type,
                LastSeen = DateTime.UtcNow,
                PreviouslySeen = true
            };
        }

        public NetworkMetadata GetNetworkMetadata(string ssid)
        {
            return _networkHistory.TryGetValue(ssid, out var metadata) ? metadata : null;
        }

        public int GetKnownNetworksCount() => _networkHistory.Count;
    }

    /// <summary>
    /// Detects captive portals and blocks suspicious ones
    /// </summary>
    public class CaptivePortalDetector
    {
        public CaptivePortalDetectionResult Detect(NetworkProfile profile)
        {
            var result = new CaptivePortalDetectionResult();

            // Check for common captive portal SSID patterns
            var portalPatterns = new[] 
            { 
                "PORTAL", "LOGIN", "CONNECT", "AUTH", "CAPTIVE", "HOTSPOT" 
            };

            if (portalPatterns.Any(p => profile.SSID.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                result.CaptivePortalDetected = true;
            }

            return result;
        }

        public bool DetectCaptivePortal(string ssid)
        {
            return Detect(new NetworkProfile { SSID = ssid }).CaptivePortalDetected;
        }
    }

    /// <summary>
    /// Detects rogue access points using signal strength and historical data
    /// </summary>
    public class RogueAPDetector
    {
        private const int SIGNAL_VARIANCE_THRESHOLD = 15; // dBm

        public RogueAPDetectionResult Detect(NetworkProfile profile, 
            Dictionary<string, NetworkMetadata> history)
        {
            var result = new RogueAPDetectionResult();

            if (!history.TryGetValue(profile.SSID, out var metadata))
            {
                return result; // Can't detect rogue without history
            }

            // Check if signal strength is abnormally different
            int signalDifference = Math.Abs(profile.SignalStrengthDBm - metadata.LastSignalStrengthDBm);
            if (signalDifference > SIGNAL_VARIANCE_THRESHOLD)
            {
                result.RogueAPLikelihood = 0.7; // 70% confidence
                result.Reason = $"Abnormal signal variance: {signalDifference} dBm";
            }

            // Check frequency of signal changes
            if (metadata.SignalChangeCount > 10)
            {
                result.RogueAPLikelihood = Math.Min(1.0, result.RogueAPLikelihood + 0.2);
                result.Reason += "; High signal instability detected";
            }

            return result;
        }
    }

    public enum NetworkType
    {
        Corporate,
        Public,
        Home,
        Unknown
    }

    public enum SecurityPosture
    {
        Low,
        Medium,
        High,
        VeryHigh,
        Enterprise
    }

    public class NetworkClassification
    {
        public string SSID { get; set; }
        public NetworkType Type { get; set; }
        public SecurityPosture SecurityPosture { get; set; }
        public bool RequireVPN { get; set; }
        public bool Allow802_1X { get; set; }
        public bool AllowCloudSync { get; set; }
        public bool AllowLLMAPICalls { get; set; }
        public bool AllowExternalCalls { get; set; } = true;
        public bool LocalOnlyMode { get; set; }
    }

    public class NetworkMetadata
    {
        public string SSID { get; set; }
        public NetworkType Type { get; set; }
        public DateTime LastSeen { get; set; }
        public bool PreviouslySeen { get; set; }
        public string LastBSSID { get; set; }
        public int LastSignalStrengthDBm { get; set; }
        public int SignalChangeCount { get; set; }
    }

    public class CaptivePortalDetectionResult
    {
        public bool CaptivePortalDetected { get; set; }
        public string PortalURL { get; set; }
    }

    public class RogueAPDetectionResult
    {
        public double RogueAPLikelihood { get; set; }
        public string Reason { get; set; }
    }

    public class SecurityAlert
    {
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; }
        public AlertType Type { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum AlertSeverity { Low, Medium, High, Critical }
    public enum AlertType { RogueAPDetected, SignalAnomalyDetected, DNSSpoofingDetected, CertificateValidationFailed }
}

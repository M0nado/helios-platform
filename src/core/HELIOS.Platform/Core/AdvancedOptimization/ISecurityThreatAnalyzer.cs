using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Advanced threat detection engine that analyzes security events,
    /// detects sophisticated attack patterns, and correlates threat intelligence.
    /// </summary>
    public interface ISecurityThreatAnalyzer
    {
        Task<bool> InitializeAsync();
        Task<bool> AnalyzeSecurityEventAsync(SecurityEvent securityEvent);
        Task<ThreatDetection[]> DetectThreatsAsync();
        Task<bool> GenerateAlertAsync(string threatId, string description);
        Task<ThreatMetrics> GetThreatMetricsAsync();
        Task<ThreatIntelligenceReport> GenerateThreatReportAsync();
    }

    /// <summary>Represents a security event.</summary>
    public class SecurityEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public SecurityEventType EventType { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SeverityLevel { get; set; } = 1;
        public Dictionary<string, object> EventData { get; set; } = new();
        public bool IsBlocked { get; set; }
    }

    /// <summary>Types of security events.</summary>
    public enum SecurityEventType
    {
        FailedLogin,
        UnauthorizedAccess,
        MalwareDetected,
        DDoS,
        SQLInjection,
        XSS,
        PrivilegeEscalation,
        DataExfiltration,
        UnusualActivity,
        PolicyViolation
    }

    /// <summary>Detected threat with analysis results.</summary>
    public class ThreatDetection
    {
        public string ThreatId { get; set; } = Guid.NewGuid().ToString();
        public ThreatType Type { get; set; }
        public double Confidence { get; set; }
        public int SeverityLevel { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> SourceEvents { get; set; } = new();
        public DateTime DetectedTime { get; set; } = DateTime.UtcNow;
        public List<string> RecommendedActions { get; set; } = new();
        public bool IsCorrelatedThreat { get; set; }
    }

    /// <summary>Types of threats.</summary>
    public enum ThreatType
    {
        Intrusion,
        Malware,
        BruteForce,
        DataBreach,
        DDoS,
        Ransomware,
        InsiderThreat,
        SocialEngineering,
        ZeroDay,
        APT
    }

    /// <summary>Threat metrics and statistics.</summary>
    public class ThreatMetrics
    {
        public int TotalEventsAnalyzed { get; set; }
        public int ThreatsDetected { get; set; }
        public int BlockedThreats { get; set; }
        public int FalsePositives { get; set; }
        public double DetectionAccuracy { get; set; }
        public int HighSeverityThreats { get; set; }
        public int CriticalThreats { get; set; }
        public DateTime LastThreatDetectedTime { get; set; }
        public double AverageThreatConfidence { get; set; }
        public long TotalAnalysisRuns { get; set; }
    }

    /// <summary>Comprehensive threat intelligence report.</summary>
    public class ThreatIntelligenceReport
    {
        public string ReportId { get; set; } = Guid.NewGuid().ToString();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ReportingPeriod { get; set; }
        public List<ThreatDetection> Detections { get; set; } = new();
        public List<ThreatPattern> IdentifiedPatterns { get; set; } = new();
        public List<string> TopAttackVectors { get; set; } = new();
        public List<string> RecommendedDefenses { get; set; } = new();
        public double OverallRiskScore { get; set; }
    }

    /// <summary>Identified attack pattern.</summary>
    public class ThreatPattern
    {
        public string PatternId { get; set; } = Guid.NewGuid().ToString();
        public string PatternName { get; set; } = string.Empty;
        public int Occurrences { get; set; }
        public double LikelihoodScore { get; set; }
        public List<SecurityEvent> EventSequence { get; set; } = new();
        public ThreatType AssociatedThreatType { get; set; }
    }
}

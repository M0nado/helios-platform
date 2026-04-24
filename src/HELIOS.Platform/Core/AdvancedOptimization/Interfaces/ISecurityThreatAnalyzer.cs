namespace HELIOS.Platform.Core.AdvancedOptimization.Interfaces
{
    /// <summary>
    /// Interface for the Security Threat Analyzer service.
    /// Provides advanced threat detection and security analysis.
    /// </summary>
    public interface ISecurityThreatAnalyzer : IService
    {
        /// <summary>
        /// Analyzes security threats in system data.
        /// </summary>
        /// <param name="securityData">Security event data to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with threat analysis.</returns>
        Task<ThreatAnalysisResult> AnalyzeThreatsAsync(List<SecurityEvent> securityData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Scores the severity of detected threats.
        /// </summary>
        /// <param name="threats">List of threats to score.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with severity scores.</returns>
        Task<ThreatSeverityScoring> ScoreSeverityAsync(List<ThreatIndicator> threats, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recommends mitigations for identified threats.
        /// </summary>
        /// <param name="threats">Identified threats.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with recommendations.</returns>
        Task<MitigationRecommendations> RecommendMitigationsAsync(List<ThreatIndicator> threats, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records security event for analysis.
        /// </summary>
        /// <param name="securityEvent">Security event to record.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordEventAsync(SecurityEvent securityEvent);

        /// <summary>
        /// Gets threat analysis history.
        /// </summary>
        /// <param name="limit">Maximum records to retrieve.</param>
        /// <returns>List of historical threat analyses.</returns>
        Task<List<ThreatAnalysisResult>> GetAnalysisHistoryAsync(int limit = 100);
    }

    /// <summary>
    /// Represents a security event.
    /// </summary>
    public class SecurityEvent
    {
        /// <summary>
        /// Event identifier.
        /// </summary>
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Event timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Event type (Login, AccessDenied, DataAccess, etc.).
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Source IP address.
        /// </summary>
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Target resource.
        /// </summary>
        public string TargetResource { get; set; } = string.Empty;

        /// <summary>
        /// User or principal involved.
        /// </summary>
        public string Principal { get; set; } = string.Empty;

        /// <summary>
        /// Event result (Success, Failure).
        /// </summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Additional event details.
        /// </summary>
        public Dictionary<string, string> Details { get; set; } = new();
    }

    /// <summary>
    /// Represents threat analysis result.
    /// </summary>
    public class ThreatAnalysisResult
    {
        /// <summary>
        /// Analysis identifier.
        /// </summary>
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Analysis timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Detected threats.
        /// </summary>
        public List<ThreatIndicator> DetectedThreats { get; set; } = new();

        /// <summary>
        /// Overall security score (0-100, higher is better).
        /// </summary>
        public double SecurityScore { get; set; }

        /// <summary>
        /// Number of critical threats.
        /// </summary>
        public int CriticalThreatCount { get; set; }

        /// <summary>
        /// Number of high severity threats.
        /// </summary>
        public int HighSeverityCount { get; set; }

        /// <summary>
        /// Identified attack patterns.
        /// </summary>
        public List<string> AttackPatterns { get; set; } = new();

        /// <summary>
        /// Risk assessment summary.
        /// </summary>
        public string RiskAssessment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a threat indicator.
    /// </summary>
    public class ThreatIndicator
    {
        /// <summary>
        /// Threat identifier.
        /// </summary>
        public string ThreatId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Threat type (Malware, Intrusion, PrivilegeEscalation, etc.).
        /// </summary>
        public string ThreatType { get; set; } = string.Empty;

        /// <summary>
        /// Threat description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Source or trigger of threat.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score (0-1).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Detection timestamp.
        /// </summary>
        public DateTime DetectionTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Related indicators or evidence.
        /// </summary>
        public List<string> Evidence { get; set; } = new();
    }

    /// <summary>
    /// Represents threat severity scoring.
    /// </summary>
    public class ThreatSeverityScoring
    {
        /// <summary>
        /// Scoring timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Severity scores by threat (0-100).
        /// </summary>
        public Dictionary<string, int> ThreatSeverityScores { get; set; } = new();

        /// <summary>
        /// Overall system risk level.
        /// </summary>
        public string OverallRiskLevel { get; set; } = "Low"; // Low, Medium, High, Critical

        /// <summary>
        /// Number of critical threats.
        /// </summary>
        public int CriticalCount { get; set; }

        /// <summary>
        /// Number of high-severity threats.
        /// </summary>
        public int HighCount { get; set; }

        /// <summary>
        /// Scoring methodology notes.
        /// </summary>
        public string ScoringNotes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents mitigation recommendations.
    /// </summary>
    public class MitigationRecommendations
    {
        /// <summary>
        /// Recommendations timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Immediate actions required.
        /// </summary>
        public List<MitigationAction> ImmediateActions { get; set; } = new();

        /// <summary>
        /// Short-term mitigation steps.
        /// </summary>
        public List<MitigationAction> ShortTermActions { get; set; } = new();

        /// <summary>
        /// Long-term preventive measures.
        /// </summary>
        public List<MitigationAction> LongTermMeasures { get; set; } = new();

        /// <summary>
        /// Overall risk reduction potential percentage.
        /// </summary>
        public double RiskReductionPotential { get; set; }

        /// <summary>
        /// Priority ranking of recommendations.
        /// </summary>
        public List<string> PriorityRanking { get; set; } = new();
    }

    /// <summary>
    /// Represents a mitigation action.
    /// </summary>
    public class MitigationAction
    {
        /// <summary>
        /// Action identifier.
        /// </summary>
        public string ActionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Action description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Priority level (1-5, 5 is highest).
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Estimated risk reduction percentage.
        /// </summary>
        public double RiskReduction { get; set; }

        /// <summary>
        /// Estimated implementation effort (1-10).
        /// </summary>
        public int ImplementationEffort { get; set; }

        /// <summary>
        /// Related threats.
        /// </summary>
        public List<string> RelatedThreats { get; set; } = new();
    }
}

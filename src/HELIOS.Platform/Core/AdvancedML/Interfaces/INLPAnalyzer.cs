namespace HELIOS.Platform.Core.AdvancedML.Interfaces;

/// <summary>
/// Natural Language Processing for log analysis and system diagnostics.
/// Extracts insights from unstructured text data.
/// </summary>
public interface INLPAnalyzer
{
    /// <summary>
    /// Analyzes text for sentiment (positive/negative/neutral).
    /// </summary>
    Task<SentimentAnalysis> AnalyzeSentimentAsync(string text);

    /// <summary>
    /// Extracts key topics/themes from text.
    /// </summary>
    Task<TopicExtraction> ExtractTopicsAsync(string text, int topicCount = 5);

    /// <summary>
    /// Performs named entity recognition on text.
    /// </summary>
    Task<List<NamedEntity>> RecognizeEntitiesAsync(string text);

    /// <summary>
    /// Classifies text into predefined categories.
    /// </summary>
    Task<TextClassification> ClassifyTextAsync(string text, string[] categories);

    /// <summary>
    /// Summarizes text to specified length.
    /// </summary>
    Task<string> SummarizeAsync(string text, int maxLength = 100);

    /// <summary>
    /// Detects anomalies in log patterns.
    /// </summary>
    Task<List<LogAnomaly>> DetectLogAnomaliesAsync(List<string> logs);
}

/// <summary>
/// Sentiment analysis result.
/// </summary>
public class SentimentAnalysis
{
    /// <summary>Sentiment label (positive, negative, neutral).</summary>
    public string Sentiment { get; set; } = string.Empty;

    /// <summary>Confidence score (0-1).</summary>
    public double Confidence { get; set; }

    /// <summary>Polarity score (-1 to 1, negative to positive).</summary>
    public double Polarity { get; set; }

    /// <summary>Subjectivity score (0-1, objective to subjective).</summary>
    public double Subjectivity { get; set; }

    /// <summary>Keywords indicating sentiment.</summary>
    public List<string> KeyWords { get; set; } = new();

    /// <summary>Analysis timestamp.</summary>
    public DateTime AnalyzedAt { get; set; }
}

/// <summary>
/// Topic extraction result.
/// </summary>
public class TopicExtraction
{
    /// <summary>Extracted topics with importance scores.</summary>
    public Dictionary<string, double> Topics { get; set; } = new();

    /// <summary>Topic distribution (topic -> percentage).</summary>
    public Dictionary<string, double> Distribution { get; set; } = new();

    /// <summary>Text coherence score (0-1).</summary>
    public double Coherence { get; set; }

    /// <summary>Diversity of topics identified (0-1).</summary>
    public double Diversity { get; set; }

    /// <summary>Primary topic identified.</summary>
    public string PrimaryTopic { get; set; } = string.Empty;

    /// <summary>Analysis method used (LDA, NMF, etc.).</summary>
    public string ExtractionMethod { get; set; } = string.Empty;
}

/// <summary>
/// Named entity extracted from text.
/// </summary>
public class NamedEntity
{
    /// <summary>Entity text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Entity type (PERSON, LOCATION, ORGANIZATION, etc.).</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Confidence score (0-1).</summary>
    public double Confidence { get; set; }

    /// <summary>Start position in text.</summary>
    public int StartIndex { get; set; }

    /// <summary>End position in text.</summary>
    public int EndIndex { get; set; }

    /// <summary>Entity metadata (e.g., category, ID).</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Text classification result.
/// </summary>
public class TextClassification
{
    /// <summary>Predicted category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Confidence score (0-1).</summary>
    public double Confidence { get; set; }

    /// <summary>Scores for all categories.</summary>
    public Dictionary<string, double> CategoryScores { get; set; } = new();

    /// <summary>Classification reasoning.</summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>Timestamp of classification.</summary>
    public DateTime ClassifiedAt { get; set; }
}

/// <summary>
/// Detected anomaly in log data.
/// </summary>
public class LogAnomaly
{
    /// <summary>Log message.</summary>
    public string LogMessage { get; set; } = string.Empty;

    /// <summary>Anomaly type (error spike, pattern change, etc.).</summary>
    public string AnomalyType { get; set; } = string.Empty;

    /// <summary>Anomaly score (0-1, higher=more anomalous).</summary>
    public double AnomalyScore { get; set; }

    /// <summary>Reason for anomaly detection.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Suggested action to take.</summary>
    public string SuggestedAction { get; set; } = string.Empty;

    /// <summary>Related logs that provide context.</summary>
    public List<string> ContextLogs { get; set; } = new();
}

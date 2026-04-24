using HELIOS.Platform.Core.AdvancedML.Interfaces;
using HELIOS.Platform.Core.Performance;
using System.Text.RegularExpressions;

namespace HELIOS.Platform.Core.AdvancedML;

/// <summary>
/// Natural Language Processing analyzer for log analysis and text understanding.
/// </summary>
public class NLPAnalyzer : INLPAnalyzer
{
    private readonly Logging.ILogger _logger;
    private readonly IL1CacheService _cache;

    // Simple sentiment lexicons
    private static readonly Dictionary<string, double> _positiveWords = new()
    {
        { "good", 0.8 }, { "great", 0.9 }, { "excellent", 1.0 }, { "success", 0.85 },
        { "completed", 0.7 }, { "perfect", 0.95 }, { "wonderful", 0.9 }
    };

    private static readonly Dictionary<string, double> _negativeWords = new()
    {
        { "bad", -0.8 }, { "terrible", -1.0 }, { "error", -0.9 }, { "failed", -0.95 },
        { "critical", -0.85 }, { "broken", -0.9 }, { "dangerous", -0.95 }
    };

    public NLPAnalyzer(Logging.ILogger logger, IL1CacheService cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<SentimentAnalysis> AnalyzeSentimentAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text cannot be empty");

        try
        {
            var cacheKey = $"sentiment_{text.GetHashCode()}";
            return await _cache.GetAsync(cacheKey,
                async () => await ComputeSentimentAsync(text),
                TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in AnalyzeSentimentAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<TopicExtraction> ExtractTopicsAsync(string text, int topicCount = 5)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text cannot be empty");
        if (topicCount < 1 || topicCount > 20) throw new ArgumentException("Topic count must be 1-20");

        try
        {
            var topics = new Dictionary<string, double>();
            var words = Tokenize(text);
            var wordFreq = words.GroupBy(w => w).ToDictionary(g => g.Key, g => (double)g.Count());

            // Get top keywords
            foreach (var kvp in wordFreq.OrderByDescending(x => x.Value).Take(topicCount))
            {
                topics[kvp.Key] = kvp.Value / words.Count;
            }

            var extraction = new TopicExtraction
            {
                Topics = topics,
                Distribution = topics.ToDictionary(x => x.Key, x => x.Value),
                Coherence = Math.Min(1.0, words.Distinct().Count() / (double)Math.Max(1, words.Count)),
                Diversity = Math.Min(1.0, (double)words.Distinct().Count() / Math.Max(1, topicCount)),
                PrimaryTopic = topics.Any() ? topics.OrderByDescending(x => x.Value).First().Key : "unknown",
                ExtractionMethod = "TF-IDF"
            };

            await Task.CompletedTask;
            return extraction;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in ExtractTopicsAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<List<NamedEntity>> RecognizeEntitiesAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text cannot be empty");

        try
        {
            var entities = new List<NamedEntity>();
            var lower = text.ToLower();

            // Simple pattern-based NER
            var patterns = new Dictionary<string, string>
            {
                { @"\b[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\b", "IP_ADDRESS" },
                { @"\b(?:error|warning|critical|info|debug)\b", "LOG_LEVEL" },
                { @"\b(?:Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)\b", "DAY" },
                { @"\b\d{4}-\d{2}-\d{2}\b", "DATE" }
            };

            foreach (var (pattern, entityType) in patterns)
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                foreach (Match match in regex.Matches(text))
                {
                    entities.Add(new NamedEntity
                    {
                        Text = match.Value,
                        EntityType = entityType,
                        Confidence = 0.9,
                        StartIndex = match.Index,
                        EndIndex = match.Index + match.Length
                    });
                }
            }

            await Task.CompletedTask;
            return entities;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in RecognizeEntitiesAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<TextClassification> ClassifyTextAsync(string text, string[] categories)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(categories);
        if (categories.Length == 0) throw new ArgumentException("Categories cannot be empty");

        try
        {
            var categoryScores = new Dictionary<string, double>();
            var words = Tokenize(text);

            // Simple classification based on word overlap
            foreach (var category in categories)
            {
                var categoryWords = Tokenize(category);
                var matches = words.Intersect(categoryWords).Count();
                categoryScores[category] = matches > 0 ? matches / (double)Math.Max(1, words.Count) : 0.1;
            }

            var bestCategory = categoryScores.OrderByDescending(x => x.Value).First();

            var classification = new TextClassification
            {
                Category = bestCategory.Key,
                Confidence = Math.Min(0.99, bestCategory.Value + 0.1),
                CategoryScores = categoryScores,
                Reasoning = $"Best match: {bestCategory.Key} ({categoryScores[bestCategory.Key]:P1})",
                ClassifiedAt = DateTime.UtcNow
            };

            await Task.CompletedTask;
            return classification;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in ClassifyTextAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<string> SummarizeAsync(string text, int maxLength = 100)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text cannot be empty");
        if (maxLength < 10) throw new ArgumentException("Max length must be at least 10");

        try
        {
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            var summary = string.Empty;

            foreach (var sentence in sentences)
            {
                if ((summary + sentence).Length <= maxLength)
                    summary += sentence + " ";
                else
                    break;
            }

            await Task.CompletedTask;
            return summary.Trim().Length > 0 ? summary.Trim() : text.Substring(0, Math.Min(maxLength, text.Length));
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in SummarizeAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<List<LogAnomaly>> DetectLogAnomaliesAsync(List<string> logs)
    {
        ArgumentNullException.ThrowIfNull(logs);
        if (logs.Count == 0) throw new ArgumentException("Logs cannot be empty");

        try
        {
            var anomalies = new List<LogAnomaly>();
            var errorCount = 0;
            var warningCount = 0;
            var totalCount = logs.Count;

            foreach (var log in logs)
            {
                if (log.Contains("ERROR", StringComparison.OrdinalIgnoreCase)) errorCount++;
                if (log.Contains("WARNING", StringComparison.OrdinalIgnoreCase)) warningCount++;
            }

            var errorRate = errorCount / (double)totalCount;
            var warningRate = warningCount / (double)totalCount;

            // High error rate is anomalous
            if (errorRate > 0.3)
            {
                anomalies.Add(new LogAnomaly
                {
                    LogMessage = "High error rate detected",
                    AnomalyType = "error_spike",
                    AnomalyScore = Math.Min(1.0, errorRate),
                    Reason = $"Error rate: {errorRate:P1}",
                    SuggestedAction = "Investigate error sources"
                });
            }

            // Pattern anomalies
            var unusualPatterns = DetectUnusualPatterns(logs);
            foreach (var pattern in unusualPatterns)
            {
                anomalies.Add(new LogAnomaly
                {
                    LogMessage = pattern,
                    AnomalyType = "unusual_pattern",
                    AnomalyScore = 0.7,
                    Reason = "Unusual log pattern detected",
                    SuggestedAction = "Review related system activity"
                });
            }

            await Task.CompletedTask;
            return anomalies;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in DetectLogAnomaliesAsync: {ex.Message}");
            throw;
        }
    }

    private async Task<SentimentAnalysis> ComputeSentimentAsync(string text)
    {
        await Task.CompletedTask;

        var words = Tokenize(text);
        double positiveScore = 0, negativeScore = 0;
        var keywords = new List<string>();

        foreach (var word in words)
        {
            if (_positiveWords.TryGetValue(word, out var posScore))
            {
                positiveScore += posScore;
                keywords.Add(word);
            }
            if (_negativeWords.TryGetValue(word, out var negScore))
            {
                negativeScore += negScore;
                keywords.Add(word);
            }
        }

        double polarity = (positiveScore + negativeScore) / Math.Max(1, words.Count);
        double subjectivity = (Math.Abs(positiveScore) + Math.Abs(negativeScore)) / Math.Max(1, words.Count * 2);

        string sentiment = polarity > 0.2 ? "positive" : polarity < -0.2 ? "negative" : "neutral";
        double confidence = Math.Abs(polarity) > 0.5 ? 0.9 : 0.6;

        return new SentimentAnalysis
        {
            Sentiment = sentiment,
            Confidence = confidence,
            Polarity = polarity,
            Subjectivity = Math.Min(1.0, subjectivity),
            KeyWords = keywords,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    private static List<string> Tokenize(string text) =>
        Regex.Split(text.ToLower(), @"\W+")
            .Where(w => w.Length > 2 && !IsStopWord(w))
            .ToList();

    private static bool IsStopWord(string word) =>
        new[] { "the", "and", "or", "is", "a", "an", "to", "of", "in", "for", "with", "by" }
            .Contains(word);

    private static List<string> DetectUnusualPatterns(List<string> logs)
    {
        var unusual = new List<string>();
        var patterns = new Dictionary<string, int>();

        foreach (var log in logs)
        {
            var pattern = Regex.Replace(log, @"\d+", "#");
            patterns[pattern] = patterns.ContainsKey(pattern) ? patterns[pattern] + 1 : 1;
        }

        // Patterns appearing rarely are unusual
        var rarePatterns = patterns.Where(x => x.Value == 1).Select(x => x.Key).Take(3).ToList();
        unusual.AddRange(rarePatterns);

        return unusual;
    }
}

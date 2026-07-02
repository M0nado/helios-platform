using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;

namespace HELIOS.Platform.Integration
{
    /// <summary>
    /// Connector for HELIOS AI Hub
    /// Manages communication with central AI inference and pattern learning services
    /// </summary>
    public class AIHubConnector
    {
        private readonly HttpClient _httpClient;
        private readonly string _aiHubBaseUrl;
        private readonly ILogger<AIHubConnector> _logger;
        private readonly AIHubConfiguration _config;
        private DateTime _lastHealthCheck = DateTime.MinValue;
        private bool _isHealthy = false;

        public event EventHandler<AIHubEventArgs>? OnAIHubEvent;

        public AIHubConnector(
            HttpClient httpClient,
            AIHubConfiguration config,
            ILogger<AIHubConnector> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiHubBaseUrl = config.AiHubBaseUrl;
        }

        /// <summary>
        /// Notify AI Hub of pattern discovery
        /// </summary>
        public async Task<bool> NotifyPatternDiscoveryAsync(
            string agentId,
            string patternId,
            OptimizationPattern pattern,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new PatternDiscoveryNotification
                {
                    AgentId = agentId,
                    PatternId = patternId,
                    Pattern = pattern,
                    Timestamp = DateTime.UtcNow
                };

                using var response = await _httpClient.PostAsJsonAsync(
                    $"{_aiHubBaseUrl}/api/patterns/discover",
                    request,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AI Hub pattern discovery notification failed: {StatusCode}", response.StatusCode);
                    return false;
                }

                _logger.LogInformation("Pattern discovery notified to AI Hub: {PatternId}", patternId);
                
                OnAIHubEvent?.Invoke(this, new AIHubEventArgs
                {
                    EventType = AIHubEventType.PatternDiscovered,
                    PatternId = patternId,
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying AI Hub of pattern discovery");
                return false;
            }
        }

        /// <summary>
        /// Submit feedback to AI Hub for pattern effectiveness learning
        /// </summary>
        public async Task<bool> SubmitFeedbackAsync(
            string agentId,
            string patternId,
            PatternFeedback feedback,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new FeedbackSubmission
                {
                    AgentId = agentId,
                    PatternId = patternId,
                    Feedback = feedback,
                    Timestamp = DateTime.UtcNow
                };

                using var response = await _httpClient.PostAsJsonAsync(
                    $"{_aiHubBaseUrl}/api/feedback/submit",
                    request,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AI Hub feedback submission failed: {StatusCode}", response.StatusCode);
                    return false;
                }

                _logger.LogInformation("Feedback submitted to AI Hub for pattern {PatternId}", patternId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback to AI Hub");
                return false;
            }
        }

        /// <summary>
        /// Query AI Hub for pattern recommendations
        /// </summary>
        public async Task<IEnumerable<PatternRecommendation>> GetPatternRecommendationsAsync(
            string agentId,
            PatternRecommendationQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var queryString = BuildQueryString(query);
                using var response = await _httpClient.GetAsync(
                    $"{_aiHubBaseUrl}/api/recommendations/patterns?{queryString}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AI Hub recommendation query failed: {StatusCode}", response.StatusCode);
                    return new List<PatternRecommendation>();
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var recommendations = JsonSerializer.Deserialize<List<PatternRecommendation>>(content);

                _logger.LogInformation("Retrieved {Count} recommendations from AI Hub", recommendations?.Count ?? 0);
                return recommendations ?? new List<PatternRecommendation>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying AI Hub for recommendations");
                return new List<PatternRecommendation>();
            }
        }

        /// <summary>
        /// Request anomaly analysis from AI Hub
        /// </summary>
        public async Task<AnomalyAnalysisResult> AnalyzeAnomalyAsync(
            string context,
            Dictionary<string, object> metrics,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new AnomalyAnalysisRequest
                {
                    Context = context,
                    Metrics = metrics,
                    Timestamp = DateTime.UtcNow
                };

                using var response = await _httpClient.PostAsJsonAsync(
                    $"{_aiHubBaseUrl}/api/analysis/anomaly",
                    request,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AI Hub anomaly analysis failed: {StatusCode}", response.StatusCode);
                    return new AnomalyAnalysisResult { IsAnomaly = false };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<AnomalyAnalysisResult>(content);

                _logger.LogInformation("Anomaly analysis completed: IsAnomaly={IsAnomaly}", result?.IsAnomaly);
                return result ?? new AnomalyAnalysisResult { IsAnomaly = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing anomaly analysis");
                return new AnomalyAnalysisResult { IsAnomaly = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Get optimization predictions from AI Hub
        /// </summary>
        public async Task<OptimizationPrediction> GetOptimizationPredictionAsync(
            string systemContext,
            Dictionary<string, object> currentMetrics,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new OptimizationPredictionRequest
                {
                    SystemContext = systemContext,
                    CurrentMetrics = currentMetrics,
                    Timestamp = DateTime.UtcNow
                };

                using var response = await _httpClient.PostAsJsonAsync(
                    $"{_aiHubBaseUrl}/api/predictions/optimization",
                    request,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AI Hub optimization prediction failed: {StatusCode}", response.StatusCode);
                    return new OptimizationPrediction { ConfidenceScore = 0 };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var prediction = JsonSerializer.Deserialize<OptimizationPrediction>(content);

                _logger.LogInformation("Optimization prediction generated with confidence {Confidence}", 
                    prediction?.ConfidenceScore);
                return prediction ?? new OptimizationPrediction { ConfidenceScore = 0 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimization prediction");
                return new OptimizationPrediction { ConfidenceScore = 0, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Register an AI model with AI Hub
        /// </summary>
        public async Task<bool> RegisterModelAsync(
            AIModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    $"{_aiHubBaseUrl}/api/models/register",
                    model,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AI model registration failed: {StatusCode}", response.StatusCode);
                    return false;
                }

                _logger.LogInformation("AI Model {ModelId} registered with AI Hub", model.ModelId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering AI model");
                return false;
            }
        }

        /// <summary>
        /// Check AI Hub health status
        /// </summary>
        public async Task<bool> GetHealthAsync()
        {
            try
            {
                // Cache health check for 30 seconds
                if ((DateTime.UtcNow - _lastHealthCheck).TotalSeconds < 30)
                    return _isHealthy;

                using var response = await _httpClient.GetAsync(
                    $"{_aiHubBaseUrl}/health",
                    CancellationToken.None);

                _lastHealthCheck = DateTime.UtcNow;
                _isHealthy = response.IsSuccessStatusCode;

                _logger.LogInformation("AI Hub health check: {IsHealthy}", _isHealthy);
                return _isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking AI Hub health");
                _isHealthy = false;
                return false;
            }
        }

        /// <summary>
        /// Batch submit multiple patterns to AI Hub for learning
        /// </summary>
        public async Task<BatchSubmissionResult> BatchSubmitPatternsAsync(
            IEnumerable<OptimizationPattern> patterns,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new BatchPatternSubmission
                {
                    Patterns = patterns,
                    SubmittedAt = DateTime.UtcNow
                };

                using var response = await _httpClient.PostAsJsonAsync(
                    $"{_aiHubBaseUrl}/api/patterns/batch",
                    request,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Batch pattern submission failed: {StatusCode}", response.StatusCode);
                    return new BatchSubmissionResult { Success = false };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<BatchSubmissionResult>(content);

                _logger.LogInformation("Batch submitted: {Count} patterns", result?.SuccessCount ?? 0);
                return result ?? new BatchSubmissionResult { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch submitting patterns");
                return new BatchSubmissionResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private string BuildQueryString(PatternRecommendationQuery query)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(query.Category))
                parts.Add($"category={Uri.EscapeDataString(query.Category)}");
            if (query.MinConfidenceScore > 0)
                parts.Add($"minConfidence={query.MinConfidenceScore}");
            if (query.MaxResults > 0)
                parts.Add($"maxResults={query.MaxResults}");
            return string.Join("&", parts);
        }
    }

    // Supporting types
    public class AIHubConfiguration
    {
        public string AiHubBaseUrl { get; set; }
        public string ApiKey { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
    }

    public class PatternDiscoveryNotification
    {
        public string AgentId { get; set; }
        public string PatternId { get; set; }
        public OptimizationPattern Pattern { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class FeedbackSubmission
    {
        public string AgentId { get; set; }
        public string PatternId { get; set; }
        public PatternFeedback Feedback { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PatternRecommendation
    {
        public string PatternId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double RecommendationScore { get; set; }
        public Dictionary<string, object> Details { get; set; }
    }

    public class PatternRecommendationQuery
    {
        public string Category { get; set; }
        public double MinConfidenceScore { get; set; } = 0.5;
        public int MaxResults { get; set; } = 50;
    }

    public class AnomalyAnalysisRequest
    {
        public string Context { get; set; }
        public Dictionary<string, object> Metrics { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AnomalyAnalysisResult
    {
        public bool IsAnomaly { get; set; }
        public double AnomalyScore { get; set; }
        public string Analysis { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class OptimizationPredictionRequest
    {
        public string SystemContext { get; set; }
        public Dictionary<string, object> CurrentMetrics { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class OptimizationPrediction
    {
        public string RecommendedAction { get; set; }
        public double ConfidenceScore { get; set; }
        public Dictionary<string, object> PredictedImpact { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AIModel
    {
        public string ModelId { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string ModelType { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class BatchPatternSubmission
    {
        public IEnumerable<OptimizationPattern> Patterns { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class BatchSubmissionResult
    {
        public bool Success { get; set; }
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> FailedPatternIds { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AIHubEventArgs : EventArgs
    {
        public AIHubEventType EventType { get; set; }
        public string PatternId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum AIHubEventType
    {
        PatternDiscovered,
        FeedbackProcessed,
        RecommendationGenerated,
        HealthCheckFailed
    }
}

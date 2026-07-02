using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.API.Controllers
{
    /// <summary>
    /// Pattern API - REST endpoints for pattern management
    /// </summary>
    [ApiController]
    [Route("api/v1/patterns")]
    public class PatternController : ControllerBase
    {
        private readonly PatternBroker _patternBroker;
        private readonly HermesIntegration _hermesIntegration;
        private readonly ILogger<PatternController> _logger;

        public PatternController(
            PatternBroker patternBroker,
            HermesIntegration hermesIntegration,
            ILogger<PatternController> logger)
        {
            _patternBroker = patternBroker ?? throw new ArgumentNullException(nameof(patternBroker));
            _hermesIntegration = hermesIntegration ?? throw new ArgumentNullException(nameof(hermesIntegration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Submit an optimization pattern
        /// </summary>
        [HttpPost("submit")]
        public async Task<ActionResult<PatternSubmissionResponse>> SubmitPattern(
            [FromBody] SubmitPatternRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request?.Pattern == null)
                    return BadRequest("Pattern is required");

                var result = await _hermesIntegration.SubmitOptimizationPatternAsync(
                    request.AgentId,
                    request.Pattern,
                    cancellationToken);

                if (!result.Success)
                    return BadRequest(new { error = result.ErrorMessage });

                return Ok(new PatternSubmissionResponse
                {
                    PatternId = result.PatternId,
                    ConfidenceScore = result.ConfidenceScore,
                    Message = "Pattern submitted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting pattern");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Query patterns
        /// </summary>
        [HttpPost("query")]
        public async Task<ActionResult<IEnumerable<OptimizationPattern>>> QueryPatterns(
            [FromBody] PatternQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new PatternQuery
                {
                    Category = request.Category,
                    MinConfidenceScore = request.MinConfidenceScore ?? 0.5,
                    MaxResults = request.MaxResults ?? 100
                };

                var patterns = await _patternBroker.QueryPatternsAsync(query, cancellationToken);
                return Ok(patterns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying patterns");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get a specific pattern
        /// </summary>
        [HttpGet("{patternId}")]
        public async Task<ActionResult<OptimizationPattern>> GetPattern(
            string patternId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var pattern = await _patternBroker.GetPatternAsync(patternId, cancellationToken);
                if (pattern == null)
                    return NotFound();

                return Ok(pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pattern");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Submit feedback for a pattern
        /// </summary>
        [HttpPost("{patternId}/feedback")]
        public async Task<ActionResult> SubmitFeedback(
            string patternId,
            [FromBody] PatternFeedback feedback,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var success = await _patternBroker.RecordFeedbackAsync(
                    patternId,
                    feedback,
                    cancellationToken);

                if (!success)
                    return BadRequest("Failed to record feedback");

                return Ok(new { message = "Feedback recorded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get patterns by category
        /// </summary>
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<OptimizationPattern>>> GetPatternsByCategory(
            string category,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var patterns = await _patternBroker.GetPatternsByCategoryAsync(
                    category,
                    cancellationToken);
                return Ok(patterns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patterns by category");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get pattern statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<PatternStatistics>> GetStatistics(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _patternBroker.GetStatisticsAsync(cancellationToken);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    /// <summary>
    /// Metrics API - REST endpoints for metrics collection and analysis
    /// </summary>
    [ApiController]
    [Route("api/v1/metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly AIHubConnector _aiConnector;
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(
            AIHubConnector aiConnector,
            ILogger<MetricsController> logger)
        {
            _aiConnector = aiConnector ?? throw new ArgumentNullException(nameof(aiConnector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analyze anomalies in metrics
        /// </summary>
        [HttpPost("analyze-anomaly")]
        public async Task<ActionResult<AnomalyAnalysisResult>> AnalyzeAnomaly(
            [FromBody] AnomalyAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _aiConnector.AnalyzeAnomalyAsync(
                    request.Context,
                    request.Metrics,
                    cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing anomaly");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get optimization prediction
        /// </summary>
        [HttpPost("predict-optimization")]
        public async Task<ActionResult<OptimizationPrediction>> PredictOptimization(
            [FromBody] OptimizationPredictionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var prediction = await _aiConnector.GetOptimizationPredictionAsync(
                    request.SystemContext,
                    request.CurrentMetrics,
                    cancellationToken);

                return Ok(prediction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting optimization");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get recommendations
        /// </summary>
        [HttpPost("recommendations")]
        public async Task<ActionResult<IEnumerable<PatternRecommendation>>> GetRecommendations(
            [FromBody] RecommendationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new PatternRecommendationQuery
                {
                    Category = request.Category,
                    MinConfidenceScore = request.MinConfidenceScore ?? 0.5,
                    MaxResults = request.MaxResults ?? 50
                };

                var recommendations = await _aiConnector.GetPatternRecommendationsAsync(
                    request.AgentId,
                    query,
                    cancellationToken);

                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    /// <summary>
    /// Learning API - REST endpoints for learning loop management
    /// </summary>
    [ApiController]
    [Route("api/v1/learning")]
    public class LearningController : ControllerBase
    {
        private readonly HermesIntegration _hermesIntegration;
        private readonly AIHubConnector _aiConnector;
        private readonly ILogger<LearningController> _logger;

        public LearningController(
            HermesIntegration hermesIntegration,
            AIHubConnector aiConnector,
            ILogger<LearningController> logger)
        {
            _hermesIntegration = hermesIntegration ?? throw new ArgumentNullException(nameof(hermesIntegration));
            _aiConnector = aiConnector ?? throw new ArgumentNullException(nameof(aiConnector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Register an agent for learning
        /// </summary>
        [HttpPost("agents/register")]
        public async Task<ActionResult> RegisterAgent(
            [FromBody] RegisterAgentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var agent = new HermesAgent
                {
                    AgentId = request.AgentId,
                    Name = request.AgentName,
                    Version = request.Version
                };

                var success = await _hermesIntegration.RegisterHermesAgentAsync(
                    request.AgentId,
                    agent);

                if (!success)
                    return BadRequest("Failed to register agent");

                return Ok(new { message = "Agent registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering agent");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Submit pattern feedback
        /// </summary>
        [HttpPost("feedback")]
        public async Task<ActionResult> SubmitFeedback(
            [FromBody] SubmitFeedbackRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var success = await _hermesIntegration.SubmitPatternFeedbackAsync(
                    request.AgentId,
                    request.PatternId,
                    request.Feedback,
                    cancellationToken);

                if (!success)
                    return BadRequest("Failed to submit feedback");

                return Ok(new { message = "Feedback submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get integration health
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<IntegrationHealthStatus>> GetHealth()
        {
            try
            {
                var health = await _hermesIntegration.GetHealthStatusAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    // Request/Response types
    public class SubmitPatternRequest
    {
        public string AgentId { get; set; }
        public OptimizationPattern Pattern { get; set; }
    }

    public class PatternSubmissionResponse
    {
        public string PatternId { get; set; }
        public double ConfidenceScore { get; set; }
        public string Message { get; set; }
    }

    public class PatternQueryRequest
    {
        public string Category { get; set; }
        public double? MinConfidenceScore { get; set; }
        public int? MaxResults { get; set; }
    }

    public class RecommendationRequest
    {
        public string AgentId { get; set; }
        public string Category { get; set; }
        public double? MinConfidenceScore { get; set; }
        public int? MaxResults { get; set; }
    }

    public class RegisterAgentRequest
    {
        public string AgentId { get; set; }
        public string AgentName { get; set; }
        public string Version { get; set; }
    }

    public class SubmitFeedbackRequest
    {
        public string AgentId { get; set; }
        public string PatternId { get; set; }
        public PatternFeedback Feedback { get; set; }
    }
}

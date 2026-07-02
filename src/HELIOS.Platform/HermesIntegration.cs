using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Integration
{
    /// <summary>
    /// Core HELIOS-Hermes integration layer
    /// Manages bidirectional communication between HELIOS and Hermes AI frameworks
    /// </summary>
    public class HermesIntegration
    {
        private readonly AIHubConnector _aiConnector;
        private readonly PatternBroker _patternBroker;
        private readonly Dictionary<string, HermesAgent> _registeredAgents;
        private readonly SemaphoreSlim _integrationLock;
        private readonly ILogger<HermesIntegration> _logger;

        public event EventHandler<HermesIntegrationEventArgs>? OnIntegrationEvent;
        public event EventHandler<PatternUpdateEventArgs>? OnPatternUpdated;

        public HermesIntegration(AIHubConnector aiConnector, PatternBroker patternBroker, ILogger<HermesIntegration> logger)
        {
            _aiConnector = aiConnector ?? throw new ArgumentNullException(nameof(aiConnector));
            _patternBroker = patternBroker ?? throw new ArgumentNullException(nameof(patternBroker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registeredAgents = new Dictionary<string, HermesAgent>();
            _integrationLock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Register a Hermes agent for HELIOS integration
        /// </summary>
        public async Task<bool> RegisterHermesAgentAsync(string agentId, HermesAgent agent)
        {
            await _integrationLock.WaitAsync();
            try
            {
                if (_registeredAgents.ContainsKey(agentId))
                {
                    _logger.LogWarning("Agent {AgentId} already registered", agentId);
                    return false;
                }

                _registeredAgents[agentId] = agent;
                _logger.LogInformation("Hermes agent {AgentId} registered successfully", agentId);
                
                OnIntegrationEvent?.Invoke(this, new HermesIntegrationEventArgs
                {
                    EventType = HermesEventType.AgentRegistered,
                    AgentId = agentId,
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            finally
            {
                _integrationLock.Release();
            }
        }

        /// <summary>
        /// Unregister a Hermes agent
        /// </summary>
        public async Task<bool> UnregisterHermesAgentAsync(string agentId)
        {
            await _integrationLock.WaitAsync();
            try
            {
                if (!_registeredAgents.Remove(agentId))
                {
                    _logger.LogWarning("Agent {AgentId} not found for unregistration", agentId);
                    return false;
                }

                _logger.LogInformation("Hermes agent {AgentId} unregistered", agentId);
                
                OnIntegrationEvent?.Invoke(this, new HermesIntegrationEventArgs
                {
                    EventType = HermesEventType.AgentUnregistered,
                    AgentId = agentId,
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            finally
            {
                _integrationLock.Release();
            }
        }

        /// <summary>
        /// Submit an optimization pattern from a Hermes agent to HELIOS
        /// </summary>
        public async Task<PatternSubmissionResult> SubmitOptimizationPatternAsync(
            string agentId, 
            OptimizationPattern pattern,
            CancellationToken cancellationToken = default)
        {
            if (!_registeredAgents.ContainsKey(agentId))
            {
                return new PatternSubmissionResult
                {
                    Success = false,
                    ErrorMessage = $"Agent {agentId} not registered"
                };
            }

            try
            {
                // Validate pattern
                if (!ValidatePattern(pattern))
                {
                    return new PatternSubmissionResult
                    {
                        Success = false,
                        ErrorMessage = "Pattern validation failed"
                    };
                }

                // Store pattern via broker
                var patternId = await _patternBroker.StorePatternAsync(pattern, cancellationToken);

                // Notify AI hub
                await _aiConnector.NotifyPatternDiscoveryAsync(agentId, patternId, pattern, cancellationToken);

                _logger.LogInformation("Pattern {PatternId} submitted by agent {AgentId}", patternId, agentId);

                OnPatternUpdated?.Invoke(this, new PatternUpdateEventArgs
                {
                    PatternId = patternId,
                    AgentId = agentId,
                    UpdateType = PatternUpdateType.Added,
                    Timestamp = DateTime.UtcNow
                });

                return new PatternSubmissionResult
                {
                    Success = true,
                    PatternId = patternId,
                    ConfidenceScore = pattern.ConfidenceScore
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting pattern from agent {AgentId}", agentId);
                return new PatternSubmissionResult
                {
                    Success = false,
                    ErrorMessage = $"Submission error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Query HELIOS patterns for a Hermes agent
        /// </summary>
        public async Task<IEnumerable<OptimizationPattern>> QueryPatternsAsync(
            string agentId,
            PatternQuery query,
            CancellationToken cancellationToken = default)
        {
            if (!_registeredAgents.ContainsKey(agentId))
            {
                _logger.LogWarning("Unauthorized pattern query from unregistered agent {AgentId}", agentId);
                return Enumerable.Empty<OptimizationPattern>();
            }

            try
            {
                var patterns = await _patternBroker.QueryPatternsAsync(query, cancellationToken);
                _logger.LogInformation("Retrieved {Count} patterns for agent {AgentId}", patterns.Count(), agentId);
                return patterns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying patterns for agent {AgentId}", agentId);
                return Enumerable.Empty<OptimizationPattern>();
            }
        }

        /// <summary>
        /// Feedback from Hermes agent on pattern effectiveness
        /// </summary>
        public async Task<bool> SubmitPatternFeedbackAsync(
            string agentId,
            string patternId,
            PatternFeedback feedback,
            CancellationToken cancellationToken = default)
        {
            if (!_registeredAgents.ContainsKey(agentId))
                return false;

            try
            {
                await _patternBroker.RecordFeedbackAsync(patternId, feedback, cancellationToken);
                
                // Update AI hub with feedback
                await _aiConnector.SubmitFeedbackAsync(agentId, patternId, feedback, cancellationToken);

                _logger.LogInformation("Feedback recorded for pattern {PatternId} by agent {AgentId}", patternId, agentId);
                
                OnPatternUpdated?.Invoke(this, new PatternUpdateEventArgs
                {
                    PatternId = patternId,
                    AgentId = agentId,
                    UpdateType = PatternUpdateType.FeedbackAdded,
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording feedback for pattern {PatternId}", patternId);
                return false;
            }
        }

        /// <summary>
        /// Get integration health status
        /// </summary>
        public async Task<IntegrationHealthStatus> GetHealthStatusAsync()
        {
            try
            {
                var aiHubStatus = await _aiConnector.GetHealthAsync();
                var brokerStatus = await _patternBroker.GetHealthAsync();
                var agentCount = _registeredAgents.Count;

                return new IntegrationHealthStatus
                {
                    IsHealthy = aiHubStatus && brokerStatus,
                    RegisteredAgentCount = agentCount,
                    AIHubConnected = aiHubStatus,
                    PatternBrokerConnected = brokerStatus,
                    LastCheckTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking integration health");
                return new IntegrationHealthStatus
                {
                    IsHealthy = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Validate optimization pattern structure and content
        /// </summary>
        private bool ValidatePattern(OptimizationPattern pattern)
        {
            if (pattern == null)
                return false;

            if (string.IsNullOrWhiteSpace(pattern.Name))
                return false;

            if (pattern.ConfidenceScore < 0 || pattern.ConfidenceScore > 1.0)
                return false;

            return true;
        }
    }

    // Supporting types
    public class HermesAgent
    {
        public string AgentId { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }

    public class OptimizationPattern
    {
        public string PatternId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public double ConfidenceScore { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    }

    public class PatternSubmissionResult
    {
        public bool Success { get; set; }
        public string PatternId { get; set; }
        public double ConfidenceScore { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PatternQuery
    {
        public string Category { get; set; }
        public double MinConfidenceScore { get; set; } = 0.5;
        public int MaxResults { get; set; } = 100;
        public Dictionary<string, object> Filters { get; set; } = new();
    }

    public class PatternFeedback
    {
        public bool WasEffective { get; set; }
        public double EffectivenessScore { get; set; }
        public string Notes { get; set; }
        public DateTime FeedbackTime { get; set; } = DateTime.UtcNow;
    }

    public class IntegrationHealthStatus
    {
        public bool IsHealthy { get; set; }
        public int RegisteredAgentCount { get; set; }
        public bool AIHubConnected { get; set; }
        public bool PatternBrokerConnected { get; set; }
        public DateTime LastCheckTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class HermesIntegrationEventArgs : EventArgs
    {
        public HermesEventType EventType { get; set; }
        public string AgentId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PatternUpdateEventArgs : EventArgs
    {
        public string PatternId { get; set; }
        public string AgentId { get; set; }
        public PatternUpdateType UpdateType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum HermesEventType
    {
        AgentRegistered,
        AgentUnregistered,
        PatternSubmitted,
        FeedbackReceived
    }

    public enum PatternUpdateType
    {
        Added,
        Updated,
        Deleted,
        FeedbackAdded
    }
}

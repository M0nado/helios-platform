using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

namespace HELIOS.Platform.Integration
{
    /// <summary>
    /// Pattern Broker for HELIOS
    /// Manages pattern storage, retrieval, and lifecycle
    /// </summary>
    public class PatternBroker
    {
        private readonly IPatternRepository _repository;
        private readonly ILogger<PatternBroker> _logger;
        private readonly ConcurrentDictionary<string, PatternMetadata> _patternCache;
        private readonly SemaphoreSlim _repositoryLock;

        public event EventHandler<PatternBrokerEventArgs>? OnPatternEvent;

        public PatternBroker(IPatternRepository repository, ILogger<PatternBroker> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _patternCache = new ConcurrentDictionary<string, PatternMetadata>();
            _repositoryLock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Store an optimization pattern
        /// </summary>
        public async Task<string> StorePatternAsync(
            OptimizationPattern pattern,
            CancellationToken cancellationToken = default)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            await _repositoryLock.WaitAsync(cancellationToken);
            try
            {
                // Generate pattern ID if not provided
                if (string.IsNullOrEmpty(pattern.PatternId))
                    pattern.PatternId = GeneratePatternId();

                // Store in repository
                await _repository.InsertPatternAsync(pattern, cancellationToken);

                // Update cache
                var metadata = new PatternMetadata
                {
                    PatternId = pattern.PatternId,
                    Name = pattern.Name,
                    Category = pattern.Category,
                    ConfidenceScore = pattern.ConfidenceScore,
                    StoredAt = DateTime.UtcNow,
                    AccessCount = 0,
                    FeedbackCount = 0
                };

                _patternCache.TryAdd(pattern.PatternId, metadata);

                _logger.LogInformation("Pattern {PatternId} stored successfully", pattern.PatternId);

                OnPatternEvent?.Invoke(this, new PatternBrokerEventArgs
                {
                    EventType = PatternBrokerEventType.PatternStored,
                    PatternId = pattern.PatternId,
                    Timestamp = DateTime.UtcNow
                });

                return pattern.PatternId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing pattern");
                throw;
            }
            finally
            {
                _repositoryLock.Release();
            }
        }

        /// <summary>
        /// Query patterns based on criteria
        /// </summary>
        public async Task<IEnumerable<OptimizationPattern>> QueryPatternsAsync(
            PatternQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var patterns = await _repository.QueryPatternsAsync(query, cancellationToken);

                // Update cache access counts
                foreach (var pattern in patterns)
                {
                    if (_patternCache.TryGetValue(pattern.PatternId, out var metadata))
                    {
                        metadata.AccessCount++;
                    }
                }

                _logger.LogInformation("Query returned {Count} patterns", patterns.Count());
                return patterns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying patterns");
                return Enumerable.Empty<OptimizationPattern>();
            }
        }

        /// <summary>
        /// Retrieve a specific pattern by ID
        /// </summary>
        public async Task<OptimizationPattern> GetPatternAsync(
            string patternId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(patternId))
                throw new ArgumentNullException(nameof(patternId));

            try
            {
                var pattern = await _repository.GetPatternByIdAsync(patternId, cancellationToken);

                if (pattern != null && _patternCache.TryGetValue(patternId, out var metadata))
                {
                    metadata.AccessCount++;
                    metadata.LastAccessedAt = DateTime.UtcNow;
                }

                return pattern;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pattern {PatternId}", patternId);
                return null;
            }
        }

        /// <summary>
        /// Record feedback for a pattern
        /// </summary>
        public async Task<bool> RecordFeedbackAsync(
            string patternId,
            PatternFeedback feedback,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(patternId))
                throw new ArgumentNullException(nameof(patternId));

            if (feedback == null)
                throw new ArgumentNullException(nameof(feedback));

            try
            {
                await _repository.InsertFeedbackAsync(patternId, feedback, cancellationToken);

                if (_patternCache.TryGetValue(patternId, out var metadata))
                {
                    metadata.FeedbackCount++;
                    metadata.LastFeedbackAt = DateTime.UtcNow;
                    metadata.EffectivenessScore = UpdateEffectivenessScore(
                        metadata.EffectivenessScore,
                        feedback.EffectivenessScore,
                        metadata.FeedbackCount);
                }

                _logger.LogInformation("Feedback recorded for pattern {PatternId}", patternId);

                OnPatternEvent?.Invoke(this, new PatternBrokerEventArgs
                {
                    EventType = PatternBrokerEventType.FeedbackRecorded,
                    PatternId = patternId,
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
        /// Get pattern statistics
        /// </summary>
        public async Task<PatternStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var totalPatterns = await _repository.GetPatternCountAsync(cancellationToken);
                var totalFeedback = await _repository.GetFeedbackCountAsync(cancellationToken);

                var topPatterns = _patternCache
                    .Values
                    .OrderByDescending(p => p.EffectivenessScore)
                    .Take(10)
                    .ToList();

                var avgConfidence = _patternCache.Values.Any()
                    ? _patternCache.Values.Average(p => p.ConfidenceScore)
                    : 0;

                return new PatternStatistics
                {
                    TotalPatterns = totalPatterns,
                    TotalFeedbackRecords = totalFeedback,
                    AverageConfidenceScore = avgConfidence,
                    MostEffectivePatterns = topPatterns,
                    CacheSize = _patternCache.Count,
                    CalculatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pattern statistics");
                return new PatternStatistics { ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Update a pattern
        /// </summary>
        public async Task<bool> UpdatePatternAsync(
            OptimizationPattern pattern,
            CancellationToken cancellationToken = default)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            if (string.IsNullOrEmpty(pattern.PatternId))
                throw new ArgumentException("Pattern ID is required", nameof(pattern));

            await _repositoryLock.WaitAsync(cancellationToken);
            try
            {
                var success = await _repository.UpdatePatternAsync(pattern, cancellationToken);

                if (success && _patternCache.TryGetValue(pattern.PatternId, out var metadata))
                {
                    metadata.ConfidenceScore = pattern.ConfidenceScore;
                    metadata.LastModifiedAt = DateTime.UtcNow;
                }

                if (success)
                {
                    _logger.LogInformation("Pattern {PatternId} updated", pattern.PatternId);
                    OnPatternEvent?.Invoke(this, new PatternBrokerEventArgs
                    {
                        EventType = PatternBrokerEventType.PatternUpdated,
                        PatternId = pattern.PatternId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating pattern {PatternId}", pattern.PatternId);
                return false;
            }
            finally
            {
                _repositoryLock.Release();
            }
        }

        /// <summary>
        /// Delete a pattern
        /// </summary>
        public async Task<bool> DeletePatternAsync(
            string patternId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(patternId))
                throw new ArgumentNullException(nameof(patternId));

            await _repositoryLock.WaitAsync(cancellationToken);
            try
            {
                var success = await _repository.DeletePatternAsync(patternId, cancellationToken);

                if (success)
                {
                    _patternCache.TryRemove(patternId, out _);
                    _logger.LogInformation("Pattern {PatternId} deleted", patternId);

                    OnPatternEvent?.Invoke(this, new PatternBrokerEventArgs
                    {
                        EventType = PatternBrokerEventType.PatternDeleted,
                        PatternId = patternId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting pattern {PatternId}", patternId);
                return false;
            }
            finally
            {
                _repositoryLock.Release();
            }
        }

        /// <summary>
        /// Get patterns by category
        /// </summary>
        public async Task<IEnumerable<OptimizationPattern>> GetPatternsByCategoryAsync(
            string category,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(category))
                throw new ArgumentNullException(nameof(category));

            try
            {
                return await _repository.GetPatternsByCategoryAsync(category, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patterns for category {Category}", category);
                return Enumerable.Empty<OptimizationPattern>();
            }
        }

        /// <summary>
        /// Get broker health status
        /// </summary>
        public async Task<bool> GetHealthAsync()
        {
            try
            {
                return await _repository.IsHealthyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pattern broker health");
                return false;
            }
        }

        /// <summary>
        /// Clear pattern cache
        /// </summary>
        public void ClearCache()
        {
            _patternCache.Clear();
            _logger.LogInformation("Pattern cache cleared");
        }

        private string GeneratePatternId()
        {
            return $"pattern_{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}";
        }

        private double UpdateEffectivenessScore(double current, double newScore, int totalFeedback)
        {
            // Weighted average: favor recent feedback slightly
            if (totalFeedback <= 1)
                return newScore;

            return (current * (totalFeedback - 1) + newScore * 1.2) / totalFeedback;
        }
    }

    // Supporting interfaces and types
    public interface IPatternRepository
    {
        Task InsertPatternAsync(OptimizationPattern pattern, CancellationToken cancellationToken = default);
        Task<OptimizationPattern> GetPatternByIdAsync(string patternId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OptimizationPattern>> QueryPatternsAsync(PatternQuery query, CancellationToken cancellationToken = default);
        Task<bool> UpdatePatternAsync(OptimizationPattern pattern, CancellationToken cancellationToken = default);
        Task<bool> DeletePatternAsync(string patternId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OptimizationPattern>> GetPatternsByCategoryAsync(string category, CancellationToken cancellationToken = default);
        Task InsertFeedbackAsync(string patternId, PatternFeedback feedback, CancellationToken cancellationToken = default);
        Task<int> GetPatternCountAsync(CancellationToken cancellationToken = default);
        Task<int> GetFeedbackCountAsync(CancellationToken cancellationToken = default);
        Task<bool> IsHealthyAsync();
    }

    public class PatternMetadata
    {
        public string PatternId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double ConfidenceScore { get; set; }
        public double EffectivenessScore { get; set; }
        public DateTime StoredAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public DateTime? LastFeedbackAt { get; set; }
        public int AccessCount { get; set; }
        public int FeedbackCount { get; set; }
    }

    public class PatternStatistics
    {
        public int TotalPatterns { get; set; }
        public int TotalFeedbackRecords { get; set; }
        public double AverageConfidenceScore { get; set; }
        public List<PatternMetadata> MostEffectivePatterns { get; set; }
        public int CacheSize { get; set; }
        public DateTime CalculatedAt { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PatternBrokerEventArgs : EventArgs
    {
        public PatternBrokerEventType EventType { get; set; }
        public string PatternId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum PatternBrokerEventType
    {
        PatternStored,
        PatternUpdated,
        PatternDeleted,
        FeedbackRecorded,
        CacheCleared
    }
}

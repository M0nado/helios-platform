using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.FeatureFlags.Models;

namespace HELIOS.Platform.Core.FeatureFlags
{
    /// <summary>
    /// Feature flag manager for managing the lifecycle of feature flags
    /// </summary>
    public class FeatureFlagManager
    {
        private readonly FeatureFlagEvaluationEngine _evaluationEngine;
        private readonly List<FeatureFlagAnalyticsEvent> _analyticsEvents;
        private readonly Dictionary<string, FeatureFlagMetrics> _metrics;
        private readonly object _lockObject = new object();

        public FeatureFlagManager()
        {
            _evaluationEngine = new FeatureFlagEvaluationEngine();
            _analyticsEvents = new List<FeatureFlagAnalyticsEvent>();
            _metrics = new Dictionary<string, FeatureFlagMetrics>();
        }

        /// <summary>
        /// Create a new feature flag
        /// </summary>
        public async Task<FeatureFlag> CreateFlagAsync(FeatureFlag flag)
        {
            if (flag == null) throw new ArgumentNullException(nameof(flag));
            if (string.IsNullOrEmpty(flag.Id)) flag.Id = Guid.NewGuid().ToString();

            flag.CreatedAt = DateTime.UtcNow;
            flag.LastModifiedAt = DateTime.UtcNow;

            _evaluationEngine.RegisterFlag(flag);
            
            LogAnalyticsEvent(new FeatureFlagAnalyticsEvent
            {
                EventId = Guid.NewGuid().ToString(),
                FlagId = flag.Id,
                EventType = "Create",
                Timestamp = DateTime.UtcNow
            });

            return await Task.FromResult(flag);
        }

        /// <summary>
        /// Update an existing feature flag
        /// </summary>
        public async Task<FeatureFlag> UpdateFlagAsync(FeatureFlag flag)
        {
            if (flag == null) throw new ArgumentNullException(nameof(flag));

            flag.LastModifiedAt = DateTime.UtcNow;
            _evaluationEngine.RegisterFlag(flag);

            LogAnalyticsEvent(new FeatureFlagAnalyticsEvent
            {
                EventId = Guid.NewGuid().ToString(),
                FlagId = flag.Id,
                EventType = "Update",
                FlagValue = flag.Enabled,
                Timestamp = DateTime.UtcNow
            });

            return await Task.FromResult(flag);
        }

        /// <summary>
        /// Delete a feature flag
        /// </summary>
        public async Task<bool> DeleteFlagAsync(string flagId)
        {
            bool removed = _evaluationEngine.UnregisterFlag(flagId);

            if (removed)
            {
                LogAnalyticsEvent(new FeatureFlagAnalyticsEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    FlagId = flagId,
                    EventType = "Delete",
                    Timestamp = DateTime.UtcNow
                });
            }

            return await Task.FromResult(removed);
        }

        /// <summary>
        /// Enable a feature flag
        /// </summary>
        public async Task<bool> EnableFlagAsync(string flagId, string modifiedBy = null)
        {
            var flag = _evaluationEngine.GetFlag(flagId);
            if (flag == null) return false;

            flag.Enabled = true;
            flag.LastModifiedAt = DateTime.UtcNow;
            flag.LastModifiedBy = modifiedBy;

            await UpdateFlagAsync(flag);
            return true;
        }

        /// <summary>
        /// Disable a feature flag
        /// </summary>
        public async Task<bool> DisableFlagAsync(string flagId, string modifiedBy = null)
        {
            var flag = _evaluationEngine.GetFlag(flagId);
            if (flag == null) return false;

            flag.Enabled = false;
            flag.LastModifiedAt = DateTime.UtcNow;
            flag.LastModifiedBy = modifiedBy;

            await UpdateFlagAsync(flag);
            return true;
        }

        /// <summary>
        /// Evaluate a feature flag
        /// </summary>
        public async Task<FeatureFlagEvaluationResult> EvaluateAsync(string flagId, FeatureFlagContext context = null)
        {
            var result = _evaluationEngine.Evaluate(flagId, context);
            
            LogAnalyticsEvent(new FeatureFlagAnalyticsEvent
            {
                EventId = Guid.NewGuid().ToString(),
                FlagId = flagId,
                UserId = context?.UserId,
                TenantId = context?.TenantId,
                EventType = "Evaluation",
                FlagValue = result.Enabled,
                Context = context?.RequestContext,
                Environment = context?.Environment,
                Timestamp = DateTime.UtcNow
            });

            UpdateMetrics(flagId, result.Enabled, context);

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Batch evaluate multiple flags
        /// </summary>
        public async Task<Dictionary<string, FeatureFlagEvaluationResult>> EvaluateBatchAsync(
            List<string> flagIds, FeatureFlagContext context = null)
        {
            var results = new Dictionary<string, FeatureFlagEvaluationResult>();

            foreach (var flagId in flagIds)
            {
                results[flagId] = await EvaluateAsync(flagId, context);
            }

            return results;
        }

        /// <summary>
        /// Get flag by ID
        /// </summary>
        public async Task<FeatureFlag> GetFlagAsync(string flagId)
        {
            return await Task.FromResult(_evaluationEngine.GetFlag(flagId));
        }

        /// <summary>
        /// Get all flags
        /// </summary>
        public async Task<List<FeatureFlag>> GetAllFlagsAsync()
        {
            return await Task.FromResult(_evaluationEngine.GetAllFlags());
        }

        /// <summary>
        /// Get flags by category
        /// </summary>
        public async Task<List<FeatureFlag>> GetFlagsByCategoryAsync(string category)
        {
            var flags = _evaluationEngine.GetAllFlags()
                .Where(f => f.Category == category)
                .ToList();

            return await Task.FromResult(flags);
        }

        /// <summary>
        /// Get flags by tag
        /// </summary>
        public async Task<List<FeatureFlag>> GetFlagsByTagAsync(string tag)
        {
            var flags = _evaluationEngine.GetAllFlags()
                .Where(f => f.Tags.Contains(tag))
                .ToList();

            return await Task.FromResult(flags);
        }

        /// <summary>
        /// Get metrics for a flag
        /// </summary>
        public async Task<FeatureFlagMetrics> GetMetricsAsync(string flagId)
        {
            lock (_lockObject)
            {
                if (_metrics.TryGetValue(flagId, out var metrics))
                {
                    return Task.FromResult(metrics).Result;
                }
            }

            return await Task.FromResult(null);
        }

        /// <summary>
        /// Get analytics events
        /// </summary>
        public async Task<List<FeatureFlagAnalyticsEvent>> GetAnalyticsEventsAsync(
            string flagId = null, int limit = 1000)
        {
            lock (_lockObject)
            {
                var events = _analyticsEvents
                    .Where(e => flagId == null || e.FlagId == flagId)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(limit)
                    .ToList();

                return Task.FromResult(events).Result;
            }
        }

        /// <summary>
        /// Update metrics for a flag
        /// </summary>
        private void UpdateMetrics(string flagId, bool enabled, FeatureFlagContext context)
        {
            lock (_lockObject)
            {
                if (!_metrics.TryGetValue(flagId, out var metrics))
                {
                    metrics = new FeatureFlagMetrics { FlagId = flagId };
                    _metrics[flagId] = metrics;
                }

                metrics.TotalEvaluations++;
                if (enabled)
                    metrics.EnabledCount++;
                else
                    metrics.DisabledCount++;

                metrics.EnabledPercentage = metrics.TotalEvaluations > 0
                    ? (metrics.EnabledCount * 100.0) / metrics.TotalEvaluations
                    : 0;

                metrics.LastEvaluatedAt = DateTime.UtcNow;
                metrics.CalculatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Log analytics event
        /// </summary>
        private void LogAnalyticsEvent(FeatureFlagAnalyticsEvent analyticsEvent)
        {
            lock (_lockObject)
            {
                _analyticsEvents.Add(analyticsEvent);
                
                // Keep last 10000 events to prevent memory bloat
                if (_analyticsEvents.Count > 10000)
                {
                    _analyticsEvents.RemoveRange(0, 5000);
                }
            }
        }

        /// <summary>
        /// Clear evaluation cache
        /// </summary>
        public void ClearCache()
        {
            _evaluationEngine.ClearEvaluationCache();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HELIOS.Platform.Core.FeatureFlags.Models;

namespace HELIOS.Platform.Core.FeatureFlags
{
    /// <summary>
    /// Core feature flag evaluation engine
    /// </summary>
    public class FeatureFlagEvaluationEngine
    {
        private readonly Dictionary<string, FeatureFlag> _flags;
        private readonly Dictionary<string, FeatureFlagEvaluationResult> _evaluationCache;
        private readonly object _lockObject = new object();

        public FeatureFlagEvaluationEngine()
        {
            _flags = new Dictionary<string, FeatureFlag>();
            _evaluationCache = new Dictionary<string, FeatureFlagEvaluationResult>();
        }

        /// <summary>
        /// Register a feature flag
        /// </summary>
        public void RegisterFlag(FeatureFlag flag)
        {
            if (flag == null) throw new ArgumentNullException(nameof(flag));
            if (string.IsNullOrEmpty(flag.Id)) throw new ArgumentException("Flag ID cannot be empty");

            lock (_lockObject)
            {
                _flags[flag.Id] = flag;
            }
        }

        /// <summary>
        /// Unregister a feature flag
        /// </summary>
        public bool UnregisterFlag(string flagId)
        {
            lock (_lockObject)
            {
                return _flags.Remove(flagId);
            }
        }

        /// <summary>
        /// Evaluate a feature flag for a given context
        /// </summary>
        public FeatureFlagEvaluationResult Evaluate(string flagId, FeatureFlagContext context = null)
        {
            context = context ?? new FeatureFlagContext();
            string cacheKey = GenerateCacheKey(flagId, context);

            lock (_lockObject)
            {
                if (_evaluationCache.TryGetValue(cacheKey, out var cached) &&
                    (DateTime.UtcNow - cached.EvaluatedAt).TotalSeconds < 300)
                {
                    return cached;
                }
            }

            var result = new FeatureFlagEvaluationResult
            {
                FlagId = flagId,
                ContextId = GenerateCacheKey(flagId, context),
                EvaluatedAt = DateTime.UtcNow
            };

            lock (_lockObject)
            {
                if (!_flags.TryGetValue(flagId, out var flag))
                {
                    result.Enabled = false;
                    result.EvaluationReason = "Flag not found";
                    return result;
                }

                result.FlagType = flag.Type;

                // Check expiration
                if (flag.ExpiresAt.HasValue && flag.ExpiresAt < DateTime.UtcNow)
                {
                    result.Enabled = false;
                    result.EvaluationReason = "Flag has expired";
                    _evaluationCache[cacheKey] = result;
                    return result;
                }

                // Check basic state
                result.Enabled = EvaluateState(flag, context);
                result.EvaluationReason = GetEvaluationReason(flag, context);

                // Apply type-specific evaluation
                switch (flag.Type)
                {
                    case FeatureFlagTypeEnum.Percentage:
                        result.Enabled = result.Enabled && EvaluatePercentage(flag, context);
                        break;
                    case FeatureFlagTypeEnum.Contextual:
                        result.Enabled = result.Enabled && EvaluateContextual(flag, context);
                        break;
                    case FeatureFlagTypeEnum.TimeWindow:
                        result.Enabled = result.Enabled && EvaluateTimeWindow(flag, context);
                        break;
                    case FeatureFlagTypeEnum.UserSegment:
                        result.Enabled = result.Enabled && EvaluateUserSegment(flag, context);
                        break;
                }

                _evaluationCache[cacheKey] = result;
                return result;
            }
        }

        /// <summary>
        /// Evaluate flag state
        /// </summary>
        private bool EvaluateState(FeatureFlag flag, FeatureFlagContext context)
        {
            if (!flag.Enabled)
                return false;

            switch (flag.State)
            {
                case FeatureFlagState.Disabled:
                    return false;
                case FeatureFlagState.BetaOnly:
                    return context.IsBetaUser;
                case FeatureFlagState.Experimental:
                    return context.IsExperimentalUser;
                case FeatureFlagState.Deprecated:
                    return false;
                case FeatureFlagState.Enabled:
                default:
                    return true;
            }
        }

        /// <summary>
        /// Evaluate percentage-based rollout
        /// </summary>
        private bool EvaluatePercentage(FeatureFlag flag, FeatureFlagContext context)
        {
            if (!flag.RolloutPercentage.HasValue || flag.RolloutPercentage < 0 || flag.RolloutPercentage > 100)
                return false;

            if (flag.RolloutPercentage == 100)
                return true;

            if (flag.RolloutPercentage == 0)
                return false;

            string hashInput = $"{flag.Id}:{context.UserId ?? context.TenantId ?? "global"}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
            int hashValue = BitConverter.ToInt32(hash, 0);
            int percentage = Math.Abs(hashValue % 100);

            return percentage < flag.RolloutPercentage;
        }

        /// <summary>
        /// Evaluate contextual conditions
        /// </summary>
        private bool EvaluateContextual(FeatureFlag flag, FeatureFlagContext context)
        {
            if (flag.TargetSegments == null || flag.TargetSegments.Count == 0)
                return true;

            // Check if user is in any target segment
            foreach (var segment in flag.TargetSegments)
            {
                if (context.UserAttributes != null && context.UserAttributes.ContainsValue(segment))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Evaluate time window constraints
        /// </summary>
        private bool EvaluateTimeWindow(FeatureFlag flag, FeatureFlagContext context)
        {
            if (flag.ExpiresAt.HasValue && context.EvaluationTime > flag.ExpiresAt)
                return false;

            if (flag.Metadata != null && flag.Metadata.TryGetValue("startTime", out var startTimeObj) &&
                flag.Metadata.TryGetValue("endTime", out var endTimeObj))
            {
                if (DateTime.TryParse(startTimeObj.ToString(), out var startTime) &&
                    DateTime.TryParse(endTimeObj.ToString(), out var endTime))
                {
                    return context.EvaluationTime >= startTime && context.EvaluationTime <= endTime;
                }
            }

            return true;
        }

        /// <summary>
        /// Evaluate user segment membership
        /// </summary>
        private bool EvaluateUserSegment(FeatureFlag flag, FeatureFlagContext context)
        {
            if (flag.TargetSegments == null || flag.TargetSegments.Count == 0)
                return true;

            if (string.IsNullOrEmpty(context.UserId))
                return false;

            // Check user attributes against target segments
            if (context.UserAttributes != null)
            {
                foreach (var segment in flag.TargetSegments)
                {
                    foreach (var attr in context.UserAttributes.Values)
                    {
                        if (attr.Equals(segment, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get evaluation reason string
        /// </summary>
        private string GetEvaluationReason(FeatureFlag flag, FeatureFlagContext context)
        {
            if (flag.State == FeatureFlagState.BetaOnly && !context.IsBetaUser)
                return "User is not in beta program";

            if (flag.State == FeatureFlagState.Experimental && !context.IsExperimentalUser)
                return "User is not in experimental program";

            return "Evaluated successfully";
        }

        /// <summary>
        /// Generate cache key for evaluation
        /// </summary>
        private string GenerateCacheKey(string flagId, FeatureFlagContext context)
        {
            return $"{flagId}:{context.UserId}:{context.TenantId}:{context.Environment}";
        }

        /// <summary>
        /// Get all registered flags
        /// </summary>
        public List<FeatureFlag> GetAllFlags()
        {
            lock (_lockObject)
            {
                return _flags.Values.ToList();
            }
        }

        /// <summary>
        /// Get flag by ID
        /// </summary>
        public FeatureFlag GetFlag(string flagId)
        {
            lock (_lockObject)
            {
                _flags.TryGetValue(flagId, out var flag);
                return flag;
            }
        }

        /// <summary>
        /// Clear evaluation cache
        /// </summary>
        public void ClearEvaluationCache()
        {
            lock (_lockObject)
            {
                _evaluationCache.Clear();
            }
        }
    }
}

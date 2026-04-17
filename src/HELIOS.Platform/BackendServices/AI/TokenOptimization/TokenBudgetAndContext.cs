using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.BackendServices.AI.TokenOptimization
{
    /// <summary>
    /// Token budget management for efficient LLM usage
    /// </summary>
    public interface ITokenBudget
    {
        Task<TokenBudgetInfo> GetBudgetAsync(string requestId);
        Task<bool> AllocateTokensAsync(string requestId, int tokensRequired);
        Task RecordUsageAsync(string requestId, int tokensUsed);
        Task<BudgetAnalysis> AnalyzeBudgetAsync();
        void SetGlobalLimit(int tokensPerDay);
        void SetPerRequestLimit(int maxTokens);
    }

    /// <summary>
    /// Manages token usage budgets across requests and models
    /// </summary>
    public class TokenBudget : ITokenBudget
    {
        private readonly ConcurrentDictionary<string, BudgetEntry> _budgets;
        private readonly ConcurrentDictionary<string, UsageHistory> _usageHistory;
        private int _globalLimitPerDay;
        private int _perRequestLimit;
        private DateTime _dailyReset;

        public TokenBudget(int globalLimitPerDay = 1_000_000, int perRequestLimit = 50_000)
        {
            _budgets = new ConcurrentDictionary<string, BudgetEntry>();
            _usageHistory = new ConcurrentDictionary<string, UsageHistory>();
            _globalLimitPerDay = globalLimitPerDay;
            _perRequestLimit = perRequestLimit;
            _dailyReset = DateTime.UtcNow.AddDays(1);
        }

        public async Task<TokenBudgetInfo> GetBudgetAsync(string requestId)
        {
            CheckDailyReset();

            var entry = _budgets.GetOrAdd(requestId, _ => new BudgetEntry
            {
                RequestId = requestId,
                CreatedAt = DateTime.UtcNow,
                AllocatedTokens = _perRequestLimit,
                RemainingTokens = _perRequestLimit
            });

            return await Task.FromResult(new TokenBudgetInfo
            {
                RequestId = requestId,
                AllocatedTokens = entry.AllocatedTokens,
                RemainingTokens = entry.RemainingTokens,
                UsedTokens = entry.AllocatedTokens - entry.RemainingTokens,
                PercentageUsed = (double)(entry.AllocatedTokens - entry.RemainingTokens) / entry.AllocatedTokens * 100,
                ExpiresAt = entry.CreatedAt.AddHours(1)
            });
        }

        public async Task<bool> AllocateTokensAsync(string requestId, int tokensRequired)
        {
            CheckDailyReset();

            // Check global limit
            var totalUsed = _usageHistory.Values.Sum(h => h.TotalUsed);
            if (totalUsed + tokensRequired > _globalLimitPerDay)
            {
                return false; // Exceeds daily global limit
            }

            var entry = _budgets.GetOrAdd(requestId, _ => new BudgetEntry
            {
                RequestId = requestId,
                CreatedAt = DateTime.UtcNow,
                AllocatedTokens = Math.Min(_perRequestLimit, _globalLimitPerDay - totalUsed),
                RemainingTokens = Math.Min(_perRequestLimit, _globalLimitPerDay - totalUsed)
            });

            if (entry.RemainingTokens >= tokensRequired)
            {
                entry.RemainingTokens -= tokensRequired;
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        public async Task RecordUsageAsync(string requestId, int tokensUsed)
        {
            if (!_budgets.TryGetValue(requestId, out var entry))
            {
                entry = new BudgetEntry { RequestId = requestId, CreatedAt = DateTime.UtcNow };
                _budgets.TryAdd(requestId, entry);
            }

            var history = _usageHistory.GetOrAdd(requestId, _ => new UsageHistory { RequestId = requestId });
            history.TotalUsed += tokensUsed;
            history.LastUpdated = DateTime.UtcNow;

            await Task.CompletedTask;
        }

        public async Task<BudgetAnalysis> AnalyzeBudgetAsync()
        {
            var allEntries = _budgets.Values.ToList();
            var allUsage = _usageHistory.Values.ToList();

            var totalAllocated = allEntries.Sum(e => e.AllocatedTokens);
            var totalUsed = allUsage.Sum(u => u.TotalUsed);
            var averageUsagePerRequest = allEntries.Any() ? totalUsed / (double)allEntries.Count : 0;

            return await Task.FromResult(new BudgetAnalysis
            {
                TotalAllocated = totalAllocated,
                TotalUsed = totalUsed,
                PercentageUtilization = totalAllocated > 0 ? (totalUsed / (double)totalAllocated) * 100 : 0,
                AverageUsagePerRequest = averageUsagePerRequest,
                PeakUsageRequest = allUsage.OrderByDescending(u => u.TotalUsed).FirstOrDefault()?.RequestId,
                PeakUsageAmount = allUsage.MaxBy(u => u.TotalUsed)?.TotalUsed ?? 0,
                RequestCount = allEntries.Count,
                DailyResetAt = _dailyReset,
                AnalyzedAt = DateTime.UtcNow
            });
        }

        public void SetGlobalLimit(int tokensPerDay)
        {
            _globalLimitPerDay = tokensPerDay;
        }

        public void SetPerRequestLimit(int maxTokens)
        {
            _perRequestLimit = maxTokens;
        }

        private void CheckDailyReset()
        {
            if (DateTime.UtcNow >= _dailyReset)
            {
                _budgets.Clear();
                _usageHistory.Clear();
                _dailyReset = DateTime.UtcNow.AddDays(1);
            }
        }

        private class BudgetEntry
        {
            public string RequestId { get; set; }
            public DateTime CreatedAt { get; set; }
            public int AllocatedTokens { get; set; }
            public int RemainingTokens { get; set; }
        }

        private class UsageHistory
        {
            public string RequestId { get; set; }
            public int TotalUsed { get; set; }
            public DateTime LastUpdated { get; set; }
        }
    }

    public class TokenBudgetInfo
    {
        public string RequestId { get; set; }
        public int AllocatedTokens { get; set; }
        public int RemainingTokens { get; set; }
        public int UsedTokens { get; set; }
        public double PercentageUsed { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class BudgetAnalysis
    {
        public long TotalAllocated { get; set; }
        public long TotalUsed { get; set; }
        public double PercentageUtilization { get; set; }
        public double AverageUsagePerRequest { get; set; }
        public string PeakUsageRequest { get; set; }
        public long PeakUsageAmount { get; set; }
        public int RequestCount { get; set; }
        public DateTime DailyResetAt { get; set; }
        public DateTime AnalyzedAt { get; set; }
    }

    /// <summary>
    /// Context window management with sliding window and caching
    /// </summary>
    public interface IContextManager
    {
        Task<ContextWindow> CreateContextAsync(string conversationId, int windowSize);
        Task AddMessageAsync(string conversationId, ConversationMessage message);
        Task<ConversationMessage[]> GetContextAsync(string conversationId);
        Task<int> EstimateTokensAsync(string conversationId);
        Task<string> CompressContextAsync(string conversationId, double compressionRatio);
        Task ClearContextAsync(string conversationId);
    }

    public class ContextManager : IContextManager
    {
        private readonly ConcurrentDictionary<string, ContextWindow> _contexts;
        private readonly ConcurrentDictionary<string, ContextCache> _contextCache;

        public ContextManager()
        {
            _contexts = new ConcurrentDictionary<string, ContextWindow>();
            _contextCache = new ConcurrentDictionary<string, ContextCache>();
        }

        public async Task<ContextWindow> CreateContextAsync(string conversationId, int windowSize)
        {
            var context = new ContextWindow
            {
                ConversationId = conversationId,
                WindowSize = windowSize,
                Messages = new List<ConversationMessage>(),
                CreatedAt = DateTime.UtcNow
            };

            _contexts.TryAdd(conversationId, context);
            _contextCache.TryAdd(conversationId, new ContextCache { ConversationId = conversationId });

            return await Task.FromResult(context);
        }

        public async Task AddMessageAsync(string conversationId, ConversationMessage message)
        {
            if (!_contexts.TryGetValue(conversationId, out var context))
            {
                await CreateContextAsync(conversationId, 8192);
                _contexts.TryGetValue(conversationId, out context);
            }

            message.Timestamp = DateTime.UtcNow;
            message.TokenCount = EstimateTokenCount(message.Content);

            context.Messages.Add(message);

            // Maintain sliding window
            var totalTokens = context.Messages.Sum(m => m.TokenCount);
            while (totalTokens > context.WindowSize && context.Messages.Any())
            {
                var removedMessage = context.Messages[0];
                context.Messages.RemoveAt(0);
                totalTokens -= removedMessage.TokenCount;
            }

            context.LastUpdated = DateTime.UtcNow;
            await Task.CompletedTask;
        }

        public async Task<ConversationMessage[]> GetContextAsync(string conversationId)
        {
            if (_contexts.TryGetValue(conversationId, out var context))
            {
                return await Task.FromResult(context.Messages.ToArray());
            }

            return Array.Empty<ConversationMessage>();
        }

        public async Task<int> EstimateTokensAsync(string conversationId)
        {
            if (_contexts.TryGetValue(conversationId, out var context))
            {
                var totalTokens = context.Messages.Sum(m => m.TokenCount);
                return await Task.FromResult(totalTokens);
            }

            return 0;
        }

        public async Task<string> CompressContextAsync(string conversationId, double compressionRatio)
        {
            if (!_contexts.TryGetValue(conversationId, out var context))
                throw new KeyNotFoundException($"Context {conversationId} not found");

            var summary = SummarizeMessages(context.Messages, compressionRatio);
            return await Task.FromResult(summary);
        }

        public async Task ClearContextAsync(string conversationId)
        {
            _contexts.TryRemove(conversationId, out _);
            _contextCache.TryRemove(conversationId, out _);
            await Task.CompletedTask;
        }

        private int EstimateTokenCount(string text)
        {
            // Rough estimate: ~4 characters per token on average
            return (text?.Length ?? 0) / 4;
        }

        private string SummarizeMessages(List<ConversationMessage> messages, double compressionRatio)
        {
            var targetLength = (int)(messages.Sum(m => m.Content.Length) * compressionRatio);
            var summary = string.Join("\n", messages.Select(m => m.Content.Substring(0, Math.Min(50, m.Content.Length)) + "..."));
            
            if (summary.Length > targetLength)
            {
                summary = summary.Substring(0, targetLength) + "...";
            }

            return summary;
        }
    }

    public class ContextWindow
    {
        public string ConversationId { get; set; }
        public int WindowSize { get; set; }
        public List<ConversationMessage> Messages { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ConversationMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string Role { get; set; } // "user", "assistant", "system"
        public string Content { get; set; }
        public int TokenCount { get; set; }
        public DateTime Timestamp { get; set; }
        public double Importance { get; set; } = 1.0; // 0-1 scale
    }

    public class ContextCache
    {
        public string ConversationId { get; set; }
        public Dictionary<string, string> CachedContexts { get; set; } = new();
        public DateTime LastAccessed { get; set; }
    }
}

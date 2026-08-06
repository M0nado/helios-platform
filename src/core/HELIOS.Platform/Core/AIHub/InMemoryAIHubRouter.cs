using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Core.AI.Interfaces;
using HELIOS.Platform.Core.AI.Router;

namespace HELIOS.Platform.Core.AIHub
{
    public sealed class InMemoryAIHubRouter : IRouter
    {
        private readonly Dictionary<string, (IAgent Agent, HashSet<string> Tags)> _agents = new(StringComparer.OrdinalIgnoreCase);
        private IRoutingStrategy _strategy = new CapabilityRoutingStrategy();
        private long _requests;

        public void RegisterAgent(IAgent agent, string[] tags = null) => _agents[agent.AgentId] = (agent, new HashSet<string>(tags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase));
        public void UnregisterAgent(string agentId) => _agents.Remove(agentId);
        public IReadOnlyList<IAgent> GetAvailableAgents(string[] tags = null) => Filter(tags).Select(a => a.Agent).ToArray();
        public RouterStatistics GetStatistics() => new() { TotalRoutingRequests = _requests, RegisteredAgentsCount = _agents.Count, LastResetTime = DateTime.UtcNow };
        public void SetRoutingStrategy(IRoutingStrategy strategy) => _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        public IRoutingStrategy GetRoutingStrategy() => _strategy;
        public void ClearCache() { }

        public Task<RoutingResult> RouteAsync(AgentRoutingRequest request, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            _requests++;
            var agent = _strategy.SelectAgent(request, GetAvailableAgents(request.RequiredTags));
            sw.Stop();
            return Task.FromResult(new RoutingResult { RequestId = request.RequestId, SelectedAgent = agent, ScoreMetric = agent == null ? 0 : 1, StrategyUsed = _strategy.StrategyName, RoutingLatency = sw.Elapsed });
        }

        public Task<RoutingResultSet> RouteToMultipleAsync(AgentRoutingRequest request, int maxAgents = 3, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            _requests++;
            var selected = _strategy.SelectAgents(request, GetAvailableAgents(request.RequiredTags), maxAgents).Select((a, i) => (a, 1.0 - (i * 0.05))).ToArray();
            sw.Stop();
            return Task.FromResult(new RoutingResultSet { RequestId = request.RequestId, SelectedAgents = selected, StrategyUsed = _strategy.StrategyName, RoutingLatency = sw.Elapsed });
        }

        private IEnumerable<(IAgent Agent, HashSet<string> Tags)> Filter(string[] tags)
        {
            if (tags == null || tags.Length == 0) return _agents.Values;
            return _agents.Values.Where(a => tags.Any(t => a.Tags.Contains(t) || a.Agent.HasCapability(t)));
        }

        private sealed class CapabilityRoutingStrategy : IRoutingStrategy
        {
            public string StrategyName => RoutingStrategies.CapabilityBased;
            public IAgent SelectAgent(AgentRoutingRequest request, IReadOnlyList<IAgent> availableAgents) => SelectAgents(request, availableAgents, 1).FirstOrDefault();
            public IReadOnlyList<IAgent> SelectAgents(AgentRoutingRequest request, IReadOnlyList<IAgent> availableAgents, int maxAgents) => availableAgents
                .OrderByDescending(a => Score(a, request))
                .ThenByDescending(a => a.GetMetrics().SuccessRate)
                .Take(maxAgents)
                .ToArray();

            private static int Score(IAgent agent, AgentRoutingRequest request) =>
                request.RequiredTags.Count(agent.HasCapability) + agent.GetCapabilities().Count(c => request.RequiredTags.Contains(c.CapabilityName, StringComparer.OrdinalIgnoreCase));
        }
    }
}

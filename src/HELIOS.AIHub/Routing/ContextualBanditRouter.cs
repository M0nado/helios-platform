using HELIOS.AIHub.Memory;

namespace HELIOS.AIHub.Routing;

/// <summary>
/// Lightweight epsilon-greedy contextual bandit used by the local Hermes training loop.
/// </summary>
public sealed class ContextualBanditRouter
{
    private readonly Random _random;
    private readonly Dictionary<string, RouteStatistics> _statistics = new(StringComparer.OrdinalIgnoreCase);

    public ContextualBanditRouter(int? seed = 1337)
    {
        _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
    }

    public RouteDecision SelectRoute(RouteRequest request, IReadOnlyList<RouteCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one route candidate is required.", nameof(candidates));
        }

        var explorationRate = Math.Clamp(request.ExplorationRate, 0, 1);
        if (_random.NextDouble() < explorationRate)
        {
            var explored = candidates[_random.Next(candidates.Count)];
            return new RouteDecision(explored.Id, explored.DisplayName, true, "Exploration selected by contextual bandit.");
        }

        var selected = candidates
            .OrderByDescending(candidate => ScoreCandidate(request, candidate))
            .ThenBy(candidate => candidate.Id, StringComparer.Ordinal)
            .First();

        return new RouteDecision(selected.Id, selected.DisplayName, false, "Exploitation selected from quality, latency, and context match.");
    }

    public void ApplyFeedback(RouteFeedback feedback)
    {
        if (!_statistics.TryGetValue(feedback.RouteId, out var statistics))
        {
            statistics = new RouteStatistics(feedback.RouteId);
            _statistics[feedback.RouteId] = statistics;
        }

        statistics.Apply(feedback.QualityScore, feedback.LatencyMilliseconds);
    }

    public IReadOnlyList<RouteStatisticsSnapshot> GetStatistics() => _statistics.Values
        .Select(statistics => statistics.ToSnapshot())
        .OrderBy(snapshot => snapshot.RouteId, StringComparer.Ordinal)
        .ToArray();

    private double ScoreCandidate(RouteRequest request, RouteCandidate candidate)
    {
        var stats = _statistics.TryGetValue(candidate.Id, out var routeStatistics) ? routeStatistics.ToSnapshot() : null;
        var quality = stats?.AverageQualityScore ?? candidate.PriorQualityScore;
        var latencyPenalty = (stats?.AverageLatencyMilliseconds ?? candidate.ExpectedLatencyMilliseconds) / 1000d;
        var contextBonus = request.Tags.Count(tag => candidate.Capabilities.Contains(tag, StringComparer.OrdinalIgnoreCase)) * 0.2;
        return quality + contextBonus - latencyPenalty;
    }

    private sealed class RouteStatistics
    {
        private double _qualityTotal;
        private double _latencyTotal;

        public RouteStatistics(string routeId)
        {
            RouteId = routeId;
        }

        public string RouteId { get; }
        public int Count { get; private set; }

        public void Apply(double qualityScore, double latencyMilliseconds)
        {
            Count++;
            _qualityTotal += Math.Clamp(qualityScore, 0, 1);
            _latencyTotal += Math.Max(0, latencyMilliseconds);
        }

        public RouteStatisticsSnapshot ToSnapshot() => new(
            RouteId,
            Count,
            Count == 0 ? 0 : _qualityTotal / Count,
            Count == 0 ? 0 : _latencyTotal / Count);
    }
}

public sealed record RouteRequest(string TaskId, string Intent, IReadOnlyList<string> Tags, double ExplorationRate = 0.05);

public sealed record RouteCandidate(string Id, string DisplayName, IReadOnlyList<string> Capabilities, double PriorQualityScore = 0.7, double ExpectedLatencyMilliseconds = 250);

public sealed record RouteDecision(string RouteId, string DisplayName, bool Explored, string Reason);

public sealed record RouteStatisticsSnapshot(string RouteId, int Count, double AverageQualityScore, double AverageLatencyMilliseconds);

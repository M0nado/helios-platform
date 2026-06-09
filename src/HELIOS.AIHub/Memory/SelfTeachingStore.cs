namespace HELIOS.AIHub.Memory;

/// <summary>
/// Local-only self-teaching memory store for vector-like recall, RL feedback, and reflection notes.
/// </summary>
public sealed class SelfTeachingStore
{
    private readonly object _gate = new();
    private readonly List<SelfTeachingMemoryItem> _items = [];
    private readonly List<RouteFeedback> _feedback = [];

    public SelfTeachingStore()
    {
        AddMemory("seed-memory-security", "security drift guard endpoint validation", "Security live endpoints must never require cloud credentials.", ["security", "drift", "local"]);
        AddMemory("seed-memory-routing", "contextual bandit route feedback", "Route feedback combines quality and latency into future agent selection.", ["routing", "bandit", "rl"]);
        AddMemory("seed-memory-engines", "deep engine catalog recommendation", "Engine recommendations match task, data, pipeline, tensor, fleet, multi-LLM, subagent, hybrid-mesh, and async-event parallelism.", ["engines", "catalog", "recommend"]);
    }

    public SelfTeachingMemoryItem AddMemory(string id, string prompt, string reflection, IReadOnlyList<string>? tags = null, double qualityScore = 0.75, double latencyMilliseconds = 0)
    {
        var item = new SelfTeachingMemoryItem(id, prompt, reflection, tags ?? [], qualityScore, latencyMilliseconds, DateTimeOffset.UtcNow);
        lock (_gate)
        {
            _items.RemoveAll(existing => string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase));
            _items.Add(item);
        }

        return item;
    }

    public IReadOnlyList<SelfTeachingMemorySearchResult> Search(string? query, int limit = 10)
    {
        var terms = Tokenize(query ?? string.Empty);
        lock (_gate)
        {
            return _items
                .Select(item => new SelfTeachingMemorySearchResult(item, Score(item, terms)))
                .Where(result => terms.Count == 0 || result.Score > 0)
                .OrderByDescending(result => result.Score)
                .ThenByDescending(result => result.Item.CreatedUtc)
                .Take(Math.Clamp(limit, 1, 50))
                .ToArray();
        }
    }

    public void RecordFeedback(RouteFeedback feedback)
    {
        lock (_gate)
        {
            _feedback.Add(feedback);
        }
    }

    public IReadOnlyList<RouteFeedback> GetFeedback(string? routeId = null)
    {
        lock (_gate)
        {
            return _feedback
                .Where(feedback => routeId is null || string.Equals(feedback.RouteId, routeId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(feedback => feedback.TimestampUtc)
                .ToArray();
        }
    }

    private static double Score(SelfTeachingMemoryItem item, ISet<string> terms)
    {
        if (terms.Count == 0)
        {
            return item.QualityScore;
        }

        var searchable = Tokenize($"{item.Prompt} {item.Reflection} {string.Join(' ', item.Tags)}");
        return terms.Count(term => searchable.Contains(term)) + item.QualityScore;
    }

    private static ISet<string> Tokenize(string value) => value
        .Split([' ', '-', '_', '/', '+', ',', '.', ':'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(term => term.ToLowerInvariant())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

public sealed record SelfTeachingMemoryItem(
    string Id,
    string Prompt,
    string Reflection,
    IReadOnlyList<string> Tags,
    double QualityScore,
    double LatencyMilliseconds,
    DateTimeOffset CreatedUtc);

public sealed record SelfTeachingMemorySearchResult(SelfTeachingMemoryItem Item, double Score);

public sealed record RouteFeedback(
    string RouteId,
    string TaskId,
    double QualityScore,
    double LatencyMilliseconds,
    string Reflection,
    DateTimeOffset TimestampUtc);

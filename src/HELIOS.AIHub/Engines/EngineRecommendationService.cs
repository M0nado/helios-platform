namespace HELIOS.AIHub.Engines;

public sealed class EngineRecommendationService
{
    private readonly DeepEngineCatalog _catalog;

    public EngineRecommendationService(DeepEngineCatalog? catalog = null)
    {
        _catalog = catalog ?? new DeepEngineCatalog();
    }

    public EngineRecommendation Recommend(EngineRecommendationRequest request)
    {
        var normalizedTerms = Tokenize($"{request.Task} {request.DataKind} {request.Intent}");
        var ranked = _catalog.GetEngines()
            .Select(engine => new RankedEngine(engine, Score(engine, normalizedTerms)))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Engine.Id, StringComparer.Ordinal)
            .Take(Math.Clamp(request.MaxResults, 1, 8))
            .ToArray();

        if (ranked.Length == 0 || ranked[0].Score == 0)
        {
            ranked = _catalog.GetEngines()
                .Where(engine => engine.Id is "bandit-router" or "retrieval-memory" or "security-anomaly")
                .Select(engine => new RankedEngine(engine, 1))
                .ToArray();
        }

        var primary = ranked[0].Engine;
        return new EngineRecommendation(
            request.Task,
            primary.Id,
            primary.Name,
            ranked.Select(item => new EngineRecommendationOption(item.Engine.Id, item.Engine.Name, item.Score, item.Engine.Capabilities)).ToArray(),
            BuildRationale(primary, normalizedTerms));
    }

    private static int Score(DeepEngineDefinition engine, ISet<string> terms)
    {
        var searchable = Tokenize(string.Join(' ', new[] { engine.Id, engine.Name, engine.Family }.Concat(engine.Capabilities).Concat(engine.SpecializationTools).Concat(engine.HybridizationStrategies)));
        var score = terms.Count(term => searchable.Contains(term));

        if (terms.Contains("security") && engine.Family == "security") score += 5;
        if (terms.Contains("route") || terms.Contains("routing") || terms.Contains("agent"))
        {
            if (engine.Family == "routing") score += 4;
        }

        if (terms.Contains("memory") && engine.Family is "retrieval" or "memory-pressure") score += 4;
        if (terms.Contains("fleet") && engine.ParallelizationTypes.Contains("fleet")) score += 3;
        return score;
    }

    private static string BuildRationale(DeepEngineDefinition engine, ISet<string> terms)
    {
        var matched = engine.Capabilities.Where(capability => terms.Overlaps(Tokenize(capability))).Take(3).ToArray();
        return matched.Length == 0
            ? $"Selected {engine.Name} as a safe local default from the X-Tier catalog."
            : $"Selected {engine.Name} because it matches {string.Join(", ", matched)}.";
    }

    private static ISet<string> Tokenize(string value) => value
        .Split([' ', '-', '_', '/', '+', ',', '.', ':'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(term => term.ToLowerInvariant())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private sealed record RankedEngine(DeepEngineDefinition Engine, int Score);
}

public sealed record EngineRecommendationRequest(string Task, string? DataKind = null, string? Intent = null, int MaxResults = 3);

public sealed record EngineRecommendation(
    string Task,
    string PrimaryEngineId,
    string PrimaryEngineName,
    IReadOnlyList<EngineRecommendationOption> Options,
    string Rationale);

public sealed record EngineRecommendationOption(string EngineId, string Name, int Score, IReadOnlyList<string> Capabilities);

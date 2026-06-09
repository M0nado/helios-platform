using HELIOS.AIHub.Memory;
using HELIOS.AIHub.Routing;

namespace HELIOS.AIHub.Training;

/// <summary>
/// Local Hermes/XCore self-teaching loop with weakness-aware task generation and route feedback.
/// </summary>
public sealed class HermesTrainingLoop
{
    private readonly ContextualBanditRouter _router;
    private readonly SelfTeachingStore _store;
    private readonly IReadOnlyList<RouteCandidate> _candidates;

    public HermesTrainingLoop(ContextualBanditRouter? router = null, SelfTeachingStore? store = null, IEnumerable<RouteCandidate>? candidates = null)
    {
        _router = router ?? new ContextualBanditRouter();
        _store = store ?? new SelfTeachingStore();
        _candidates = (candidates ?? CreateDefaultCandidates()).ToArray();
    }

    public async Task<TrainingRunResult> TriggerAsync(TrainingTriggerRequest request, CancellationToken cancellationToken = default)
    {
        var generatedTasks = GenerateWeaknessAwareTasks(request).Take(Math.Clamp(request.MaxTasks, 1, 25)).ToArray();
        var results = new List<TrainingTaskResult>(generatedTasks.Length);

        foreach (var task in generatedTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var decision = _router.SelectRoute(new RouteRequest(task.TaskId, task.Intent, task.Tags, request.ExplorationRate), _candidates);
            var started = DateTimeOffset.UtcNow;

            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);

            var quality = ScoreTask(task, decision);
            var latency = Math.Max(1, (DateTimeOffset.UtcNow - started).TotalMilliseconds + decision.RouteId.Length);
            var reflection = $"Route {decision.RouteId} handled {task.Intent} with quality {quality:0.00} and latency {latency:0.0}ms.";
            var feedback = new RouteFeedback(decision.RouteId, task.TaskId, quality, latency, reflection, DateTimeOffset.UtcNow);

            _router.ApplyFeedback(feedback);
            _store.RecordFeedback(feedback);
            _store.AddMemory($"training-{task.TaskId}", task.Prompt, reflection, task.Tags, quality, latency);

            results.Add(new TrainingTaskResult(task.TaskId, task.Intent, decision.RouteId, quality, latency, reflection));
        }

        return new TrainingRunResult(
            $"train-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}",
            request.Objective,
            results,
            results.Count == 0 ? 0 : results.Average(result => result.QualityScore),
            DateTimeOffset.UtcNow);
    }

    public IReadOnlyList<TrainingTask> GenerateWeaknessAwareTasks(TrainingTriggerRequest request)
    {
        IReadOnlyList<string> weaknesses = request.Weaknesses.Count == 0
            ? ["routing", "security", "memory", "latency"]
            : request.Weaknesses;

        return weaknesses.Select((weakness, index) => new TrainingTask(
            $"task-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{index + 1}",
            weakness,
            $"Improve Hermes/XCore {weakness} behavior for {request.Objective}.",
            TagsForWeakness(weakness))).ToArray();
    }

    public IReadOnlyList<RouteStatisticsSnapshot> GetRoutingStatistics() => _router.GetStatistics();

    private static IReadOnlyList<RouteCandidate> CreateDefaultCandidates() =>
    [
        new("xcore-inference", "XCore Inference Node", ["inference", "routing", "latency"], 0.76, 120),
        new("hermes-feature-engineering", "Hermes Feature Engineering Node", ["memory", "retrieval", "features"], 0.72, 180),
        new("hermes-trainer", "Hermes Local Trainer", ["training", "security", "drift"], 0.74, 260)
    ];

    private static double ScoreTask(TrainingTask task, RouteDecision decision)
    {
        var tagMatchBonus = task.Tags.Any(tag => decision.RouteId.Contains(tag, StringComparison.OrdinalIgnoreCase)) ? 0.1 : 0;
        return Math.Clamp(0.72 + tagMatchBonus + (decision.Explored ? 0.02 : 0.05), 0, 1);
    }

    private static IReadOnlyList<string> TagsForWeakness(string weakness)
    {
        var normalized = weakness.ToLowerInvariant();
        if (normalized.Contains("security") || normalized.Contains("drift")) return ["security", "drift"];
        if (normalized.Contains("memory") || normalized.Contains("retrieval")) return ["memory", "retrieval"];
        if (normalized.Contains("latency") || normalized.Contains("performance")) return ["latency", "inference"];
        return ["routing", normalized];
    }
}

public sealed record TrainingTriggerRequest(string Objective, IReadOnlyList<string> Weaknesses, int MaxTasks = 4, double ExplorationRate = 0.05)
{
    public static TrainingTriggerRequest Default => new("local-self-teaching", [], 4, 0.05);
}

public sealed record TrainingTask(string TaskId, string Intent, string Prompt, IReadOnlyList<string> Tags);

public sealed record TrainingTaskResult(string TaskId, string Intent, string RouteId, double QualityScore, double LatencyMilliseconds, string Reflection);

public sealed record TrainingRunResult(string RunId, string Objective, IReadOnlyList<TrainingTaskResult> Tasks, double AverageQualityScore, DateTimeOffset CompletedUtc);

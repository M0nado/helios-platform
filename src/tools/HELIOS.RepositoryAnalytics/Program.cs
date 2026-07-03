using System.Text.Json;
using HELIOS.Analytics.FSharp;
using HELIOS.Platform.Contracts.Analytics;

var root = FindRepoRoot(AppContext.BaseDirectory);
var inputPath = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(root, "reports", "branch-intelligence", "analytics-metrics.json");
var outDir = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(root, "reports", "branch-intelligence");
Directory.CreateDirectory(outDir);

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Missing analytics metrics: {inputPath}. Run python3 scripts/analysis/branch_intelligence.py first.");
    return 2;
}

using var document = JsonDocument.Parse(File.ReadAllText(inputPath));
var branchScores = ReadScores(document.RootElement, "branchScores", "score");
var ideaScores = ReadScores(document.RootElement, "ideaScores", "score");
var hermesScores = ReadCategoryScores(document.RootElement, "ideaScores", "hermes", "score");

var engine = new AnalyticsEngine();
var allScores = branchScores.Concat(ideaScores).DefaultIfEmpty(0.0).ToArray();
var branchArray = branchScores.DefaultIfEmpty(0.0).ToArray();
var ideaArray = ideaScores.DefaultIfEmpty(0.0).ToArray();
var hermesArray = hermesScores.DefaultIfEmpty(0.0).ToArray();

var report = new
{
    generatedUtc = DateTimeOffset.UtcNow,
    input = Path.GetRelativePath(root, inputPath),
    analyticsEngine = "HELIOS.Analytics.FSharp.AnalyticsEngine",
    scoreSummary = ToSummary(engine.Summarize(allScores)),
    branchSummary = ToSummary(engine.Summarize(branchArray)),
    ideaSummary = ToSummary(engine.Summarize(ideaArray)),
    hermesFleetSummary = ToSummary(engine.Summarize(hermesArray)),
    branchForecast = ToPrediction(engine.LinearForecast(branchArray)),
    ideaForecast = ToPrediction(engine.MovingAverageForecast(ideaArray, Math.Min(3, ideaArray.Length))),
    anomalies = engine.DetectAnomalies(allScores, 1.5).Select(a => new { a.Value, a.Score }).ToArray(),
};

var jsonPath = Path.Combine(outDir, "fsharp-ranked-health.json");
File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

var mdPath = Path.Combine(outDir, "fsharp-ranked-health.md");
File.WriteAllText(mdPath, $"""
# F# Ranked Health

Generated: `{report.generatedUtc:O}`

Input: `{report.input}`

Analytics engine: `{report.analyticsEngine}`

| Metric | Count | Mean | P95 | StdDev |
| --- | ---: | ---: | ---: | ---: |
| All scores | {report.scoreSummary.Count} | {report.scoreSummary.Mean:F2} | {report.scoreSummary.P95:F2} | {report.scoreSummary.StandardDeviation:F2} |
| Branch scores | {report.branchSummary.Count} | {report.branchSummary.Mean:F2} | {report.branchSummary.P95:F2} | {report.branchSummary.StandardDeviation:F2} |
| Idea scores | {report.ideaSummary.Count} | {report.ideaSummary.Mean:F2} | {report.ideaSummary.P95:F2} | {report.ideaSummary.StandardDeviation:F2} |
| Hermes fleet scores | {report.hermesFleetSummary.Count} | {report.hermesFleetSummary.Mean:F2} | {report.hermesFleetSummary.P95:F2} | {report.hermesFleetSummary.StandardDeviation:F2} |

Branch forecast: `{report.branchForecast.Model}` predicts `{report.branchForecast.PredictedValue:F2}` with confidence `{report.branchForecast.Confidence:F2}`.

Idea forecast: `{report.ideaForecast.Model}` predicts `{report.ideaForecast.PredictedValue:F2}` with confidence `{report.ideaForecast.Confidence:F2}`.

Anomalies detected: `{report.anomalies.Length}`
""");

Console.WriteLine($"Wrote {Path.GetRelativePath(root, jsonPath)}");
Console.WriteLine($"Wrote {Path.GetRelativePath(root, mdPath)}");
return 0;

static string FindRepoRoot(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            return dir.FullName;
        }
        dir = dir.Parent;
    }
    return Directory.GetCurrentDirectory();
}

static IReadOnlyList<double> ReadScores(JsonElement root, string property, string scoreProperty)
{
    if (!root.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
    {
        return Array.Empty<double>();
    }
    return array.EnumerateArray()
        .Select(item => item.TryGetProperty(scoreProperty, out var score) && score.TryGetDouble(out var value) ? value : 0.0)
        .ToArray();
}

static IReadOnlyList<double> ReadCategoryScores(JsonElement root, string property, string category, string scoreProperty)
{
    if (!root.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
    {
        return Array.Empty<double>();
    }
    return array.EnumerateArray()
        .Where(item => item.TryGetProperty("category", out var cat) && string.Equals(cat.GetString(), category, StringComparison.OrdinalIgnoreCase))
        .Select(item => item.TryGetProperty(scoreProperty, out var score) && score.TryGetDouble(out var value) ? value : 0.0)
        .ToArray();
}

static SummaryDto ToSummary(IStatisticalSummary summary) => new(
    summary.Count,
    summary.Mean,
    summary.Median,
    summary.StandardDeviation,
    summary.Percentiles["P95"],
    summary.Minimum,
    summary.Maximum);

static PredictionDto ToPrediction(IPredictionResult prediction) => new(
    prediction.Model,
    prediction.PredictedValue,
    prediction.Confidence,
    prediction.GeneratedAt);

internal sealed record SummaryDto(int Count, double Mean, double Median, double StandardDeviation, double P95, double Minimum, double Maximum);

internal sealed record PredictionDto(string Model, double PredictedValue, double Confidence, DateTimeOffset GeneratedAt);

using System.Text.Json;
using System.Text.Json.Nodes;

var root = FindRepositoryRoot(AppContext.BaseDirectory);
var branchIntelligenceDir = Path.Combine(root, "reports", "branch-intelligence");
var outputDir = Path.Combine(root, "reports", "repository-analytics");
Directory.CreateDirectory(outputDir);

var metricsPath = Path.Combine(branchIntelligenceDir, "analytics-metrics.json");
var metrics = File.Exists(metricsPath)
    ? JsonNode.Parse(File.ReadAllText(metricsPath))?.AsObject() ?? new JsonObject()
    : new JsonObject();

var hermesMetricGroups = LoadHermesMetricGroups(branchIntelligenceDir);
var hermesMetricSummaries = new JsonObject();
foreach (var group in hermesMetricGroups.OrderBy(item => item.Key, StringComparer.Ordinal))
{
    if (group.Value.Count == 0)
    {
        continue;
    }

    hermesMetricSummaries[group.Key] = SummarizeMetricGroup(group.Value);
}

var hermesFleetSummary = hermesMetricGroups.Count == 0
    ? new JsonObject
    {
        ["status"] = "empty/default",
        ["isDefault"] = true,
        ["metricGroupCount"] = 0,
        ["message"] = "No Hermes fleet numeric metrics were present."
    }
    : new JsonObject
    {
        ["status"] = "observed",
        ["isDefault"] = false,
        ["metricGroupCount"] = hermesMetricGroups.Count,
        ["metricValueCount"] = hermesMetricGroups.Values.Sum(values => values.Count)
    };

var report = new JsonObject
{
    ["schema"] = "HELIOS.RepositoryAnalytics.v1",
    ["branchIntelligence"] = metrics.DeepClone(),
    ["hermesFleetSummary"] = hermesFleetSummary,
    ["hermesMetricSummaries"] = hermesMetricSummaries
};

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(Path.Combine(outputDir, "repository-analytics.json"), report.ToJsonString(jsonOptions) + Environment.NewLine);

var markdown = new List<string>
{
    "# HELIOS Repository Analytics",
    "",
    "## Hermes Fleet Metrics",
    ""
};
if (hermesMetricGroups.Count == 0)
{
    markdown.Add("No Hermes fleet numeric metrics were present.");
}
else
{
    foreach (var group in hermesMetricGroups.OrderBy(item => item.Key, StringComparer.Ordinal))
    {
        markdown.Add($"- `{group.Key}`: {group.Value.Count} samples");
    }
}
File.WriteAllText(Path.Combine(outputDir, "repository-analytics.md"), string.Join(Environment.NewLine, markdown) + Environment.NewLine);

Console.WriteLine($"Wrote repository analytics reports to {Path.GetRelativePath(root, outputDir)}");
return 0;

static string FindRepositoryRoot(string start)
{
    var directory = new DirectoryInfo(start);
    while (directory is not null)
    {
        if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return Directory.GetCurrentDirectory();
}

static Dictionary<string, List<double>> LoadHermesMetricGroups(string branchIntelligenceDir)
{
    var groups = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
    var ideaImpactPath = Path.Combine(branchIntelligenceDir, "idea-impact.json");
    if (!File.Exists(ideaImpactPath))
    {
        return groups;
    }

    var ideas = JsonNode.Parse(File.ReadAllText(ideaImpactPath))?.AsArray();
    if (ideas is null)
    {
        return groups;
    }

    foreach (var idea in ideas.OfType<JsonObject>())
    {
        if (!string.Equals((string?)idea["category"], "hermes", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!string.Equals((string?)idea["module"], "hermes-fleet", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        foreach (var metricsPropertyName in new[] { "metrics", "metricValues", "numericMetrics" })
        {
            if (idea[metricsPropertyName] is not JsonObject metricsObject)
            {
                continue;
            }

            foreach (var (name, value) in ExtractNumericProperties(metricsObject))
            {
                groups.TryAdd(name, new List<double>());
                groups[name].Add(value);
            }
        }
    }

    return groups.Where(item => item.Value.Count > 0).ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
}

static IEnumerable<KeyValuePair<string, double>> ExtractNumericProperties(JsonObject node, string prefix = "")
{
    foreach (var property in node)
    {
        var name = string.IsNullOrWhiteSpace(prefix) ? property.Key : $"{prefix}.{property.Key}";
        if (property.Value is JsonValue value && value.TryGetValue<double>(out var number) && double.IsFinite(number))
        {
            yield return new KeyValuePair<string, double>(name, number);
        }
        else if (property.Value is JsonObject child)
        {
            foreach (var nested in ExtractNumericProperties(child, name))
            {
                yield return nested;
            }
        }
    }
}

static JsonObject SummarizeMetricGroup(IReadOnlyList<double> values)
{
    var anomalies = values.Count == 0 ? new List<int>() : DetectAnomalies(values);
    return new JsonObject
    {
        ["count"] = values.Count,
        ["min"] = values.Min(),
        ["max"] = values.Max(),
        ["average"] = values.Average(),
        ["anomalyCount"] = anomalies.Count,
        ["anomalyIndexes"] = new JsonArray(anomalies.Select(index => JsonValue.Create(index)).ToArray<JsonNode?>())
    };
}

static List<int> DetectAnomalies(IReadOnlyList<double> values)
{
    if (values.Count < 2)
    {
        return new List<int>();
    }

    var average = values.Average();
    var variance = values.Sum(value => Math.Pow(value - average, 2)) / values.Count;
    var standardDeviation = Math.Sqrt(variance);
    if (standardDeviation == 0)
    {
        return new List<int>();
    }

    var anomalies = new List<int>();
    for (var index = 0; index < values.Count; index++)
    {
        if (Math.Abs(values[index] - average) > standardDeviation * 2)
        {
            anomalies.Add(index);
        }
    }

    return anomalies;
}

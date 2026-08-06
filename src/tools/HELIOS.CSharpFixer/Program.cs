using System.Text.RegularExpressions;

var logPath = args.FirstOrDefault();
var text = logPath is not null && File.Exists(logPath) ? File.ReadAllText(logPath) : Console.In.ReadToEnd();
var groups = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    ["missing-type-or-namespace"] = Regex.Matches(text, "CS0246|CS0234").Count,
    ["duplicate-type"] = Regex.Matches(text, "CS0101|CS0111").Count,
    ["interface-contract"] = Regex.Matches(text, "CS0535|CS0738").Count,
    ["nullable-warning-as-error"] = Regex.Matches(text, "CS86\\d{2}").Count,
    ["package-restore"] = Regex.Matches(text, "NU\\d{4}|restore", RegexOptions.IgnoreCase).Count
};
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { generatedUtc = DateTimeOffset.UtcNow, groups }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

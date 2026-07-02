namespace HELIOS.Platform.Optimization.CodeOptimization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CodeIssue
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
    public string IssueType { get; set; } // DeadCode, UnoptimizedLoop, MissingCache, etc.
    public string Description { get; set; }
    public string Severity { get; set; } // Low, Medium, High
    public double ImpactScore { get; set; } // 0-100
}

public class OptimizationSuggestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string IssueId { get; set; }
    public string SuggestedFix { get; set; }
    public string Before { get; set; }
    public string After { get; set; }
    public double ExpectedImprovementPercentage { get; set; }
    public string Category { get; set; }
}

public interface ICodeAnalyzer
{
    Task<List<CodeIssue>> AnalyzeCodeAsync(string codeContent, string filePath);
    Task<List<OptimizationSuggestion>> GenerateSuggestionsAsync(CodeIssue issue);
    Task<CodeQualityReport> GenerateQualityReportAsync();
}

public class CodeQualityReport
{
    public int TotalIssuesFound { get; set; }
    public int CriticalIssues { get; set; }
    public int HighIssues { get; set; }
    public int MediumIssues { get; set; }
    public int LowIssues { get; set; }
    public double OverallScore { get; set; } // 0-100
    public List<CodeIssue> TopIssues { get; set; } = new();
    public Dictionary<string, int> IssuesByType { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public class CodeAnalyzer : ICodeAnalyzer
{
    private readonly List<CodeIssue> _allIssues = new();
    private readonly object _lockObj = new();

    public async Task<List<CodeIssue>> AnalyzeCodeAsync(string codeContent, string filePath)
    {
        if (string.IsNullOrEmpty(codeContent))
            return new List<CodeIssue>();

        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                var issues = new List<CodeIssue>();
                var lines = codeContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    // Detect unused variables
                    if (line.Contains("var unused") || line.Contains("var temp"))
                    {
                        issues.Add(new CodeIssue
                        {
                            FilePath = filePath,
                            LineNumber = i + 1,
                            IssueType = "UnusedVariable",
                            Description = "Variable appears to be unused",
                            Severity = "Low",
                            ImpactScore = 20
                        });
                    }

                    // Detect inefficient loops
                    if (line.Contains("for (") && line.Contains(".Count"))
                    {
                        issues.Add(new CodeIssue
                        {
                            FilePath = filePath,
                            LineNumber = i + 1,
                            IssueType = "IneffcientLoop",
                            Description = "Loop accessing Count property repeatedly",
                            Severity = "Medium",
                            ImpactScore = 45
                        });
                    }

                    // Detect missing caching
                    if (line.Contains("Database.Query") || line.Contains("RemoteService.Call"))
                    {
                        issues.Add(new CodeIssue
                        {
                            FilePath = filePath,
                            LineNumber = i + 1,
                            IssueType = "MissingCache",
                            Description = "Expensive operation without caching",
                            Severity = "High",
                            ImpactScore = 75
                        });
                    }

                    // Detect string concatenation in loops
                    if (line.Contains("string += ") || line.Contains("str +="))
                    {
                        issues.Add(new CodeIssue
                        {
                            FilePath = filePath,
                            LineNumber = i + 1,
                            IssueType = "StringConcatenation",
                            Description = "Inefficient string concatenation",
                            Severity = "High",
                            ImpactScore = 65
                        });
                    }

                    // Detect deep nesting
                    if (line.Length - line.TrimStart().Length > 24) // More than 6 levels of indentation
                    {
                        issues.Add(new CodeIssue
                        {
                            FilePath = filePath,
                            LineNumber = i + 1,
                            IssueType = "DeepNesting",
                            Description = "Code is too deeply nested",
                            Severity = "Medium",
                            ImpactScore = 40
                        });
                    }
                }

                foreach (var issue in issues)
                {
                    _allIssues.Add(issue);
                }

                return issues;
            }
        });
    }

    public async Task<List<OptimizationSuggestion>> GenerateSuggestionsAsync(CodeIssue issue)
    {
        return await Task.Run(() =>
        {
            var suggestions = new List<OptimizationSuggestion>();

            switch (issue.IssueType)
            {
                case "UnusedVariable":
                    suggestions.Add(new OptimizationSuggestion
                    {
                        IssueId = issue.Id,
                        SuggestedFix = "Remove unused variable declaration",
                        Before = "var unused = GetValue();",
                        After = "// Variable removed - not used",
                        ExpectedImprovementPercentage = 5,
                        Category = "DeadCodeRemoval"
                    });
                    break;

                case "IneffcientLoop":
                    suggestions.Add(new OptimizationSuggestion
                    {
                        IssueId = issue.Id,
                        SuggestedFix = "Cache the Count value before loop",
                        Before = "for (int i = 0; i < list.Count; i++)",
                        After = "int count = list.Count; for (int i = 0; i < count; i++)",
                        ExpectedImprovementPercentage = 15,
                        Category = "LoopOptimization"
                    });
                    break;

                case "MissingCache":
                    suggestions.Add(new OptimizationSuggestion
                    {
                        IssueId = issue.Id,
                        SuggestedFix = "Add caching layer for database queries",
                        Before = "return Database.Query(sql);",
                        After = "return _cache.Get(key) ?? Database.Query(sql);",
                        ExpectedImprovementPercentage = 50,
                        Category = "Caching"
                    });
                    break;

                case "StringConcatenation":
                    suggestions.Add(new OptimizationSuggestion
                    {
                        IssueId = issue.Id,
                        SuggestedFix = "Use StringBuilder for string concatenation",
                        Before = "for (int i = 0; i < 1000; i++) { result += i.ToString(); }",
                        After = "var sb = new StringBuilder(); for (int i = 0; i < 1000; i++) { sb.Append(i); }",
                        ExpectedImprovementPercentage = 80,
                        Category = "StringHandling"
                    });
                    break;

                case "DeepNesting":
                    suggestions.Add(new OptimizationSuggestion
                    {
                        IssueId = issue.Id,
                        SuggestedFix = "Refactor to reduce nesting depth",
                        Before = "Complex nested structure",
                        After = "Use guard clauses or extract methods",
                        ExpectedImprovementPercentage = 20,
                        Category = "Readability"
                    });
                    break;
            }

            return suggestions;
        });
    }

    public async Task<CodeQualityReport> GenerateQualityReportAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                var report = new CodeQualityReport
                {
                    TotalIssuesFound = _allIssues.Count
                };

                var bySeverity = _allIssues.GroupBy(i => i.Severity)
                    .ToDictionary(g => g.Key, g => g.Count());

                report.CriticalIssues = bySeverity.GetValueOrDefault("Critical", 0);
                report.HighIssues = bySeverity.GetValueOrDefault("High", 0);
                report.MediumIssues = bySeverity.GetValueOrDefault("Medium", 0);
                report.LowIssues = bySeverity.GetValueOrDefault("Low", 0);

                // Calculate score
                report.OverallScore = Math.Max(0, 100 - 
                    (report.CriticalIssues * 10) - 
                    (report.HighIssues * 5) - 
                    (report.MediumIssues * 2) - 
                    (report.LowIssues * 1));

                report.TopIssues = _allIssues
                    .OrderByDescending(i => i.ImpactScore)
                    .Take(10)
                    .ToList();

                report.IssuesByType = _allIssues
                    .GroupBy(i => i.IssueType)
                    .ToDictionary(g => g.Key, g => g.Count());

                return report;
            }
        });
    }
}

public class OptimizationScorer
{
    public double CalculateScore(OptimizationSuggestion suggestion)
    {
        var baseScore = suggestion.ExpectedImprovementPercentage;
        var categoryMultiplier = suggestion.Category switch
        {
            "Caching" => 1.5,
            "LoopOptimization" => 1.3,
            "StringHandling" => 1.2,
            "DeadCodeRemoval" => 0.8,
            "Readability" => 0.9,
            _ => 1.0
        };

        return baseScore * categoryMultiplier;
    }

    public List<OptimizationSuggestion> RankByImpact(List<OptimizationSuggestion> suggestions)
    {
        return suggestions.OrderByDescending(s => CalculateScore(s)).ToList();
    }
}

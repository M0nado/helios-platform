using System.Collections.Generic;

namespace PolyglotXTier;

public sealed class PhaseRuntime
{
    public static IReadOnlyList<string> Phase1Layout() => new[]
    {
        @"D:\DevDrive\src",
        @"D:\DevDrive\pkg",
        @"D:\DevDrive\tools",
        @"D:\DevDrive\ai",
        @"D:\DevDrive\docker",
        @"D:\DevDrive\wsl",
        @"D:\DevDrive\vm",
        @"D:\DevDrive\hermes",
        @"D:\DevDrive\data",
        @"D:\DevDrive\azure",
        @"D:\DevDrive\github"
    };

    public static Dictionary<string, string> Phase1CacheEnv() => new()
    {
        ["npm_cache"] = @"D:\pkg\npm-cache",
        ["pip_cache"] = @"D:\pkg\pip-cache",
        ["nuget_packages"] = @"D:\pkg\nuget-global",
        ["cargo_home"] = @"D:\tools\cargo",
        ["rustup_home"] = @"D:\tools\rustup"
    };

    public static IReadOnlyList<string> Phase2Tools() => new[]
    {
        "Git", "VSCode", "GitHub CLI", "Azure CLI", "Python 3.12",
        "Node.js", ".NET 8 SDK", "PowerShell 7", "Docker", "WSL2", "CUDA"
    };

    public static IReadOnlyList<string> Phase4FeatureColumns() => new[]
    {
        "tokens_in", "tokens_out", "latency_ms", "tokens_total",
        "throughput", "success", "task_complexity", "input_length", "time_of_day_bucket"
    };
}


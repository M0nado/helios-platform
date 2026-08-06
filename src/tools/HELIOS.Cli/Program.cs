using System.Diagnostics;
using HELIOS.Platform.Orchestration;

var command = args.ElementAtOrDefault(0) ?? "help";
var repoRoot = FindRepoRoot(AppContext.BaseDirectory);

return command switch
{
    "language-score" => RunPython(repoRoot, "scripts/github/language_aware_score.py"),
    "merge-decision" => RunPython(repoRoot, "scripts/github/merge_decision_pipeline.py"),
    "final-gate-plan" => PrintFinalGatePlan(),
    "validate-reports" => RunPython(repoRoot, "scripts/automation/validate_report_contracts.py"),
    "native-overlap" => PrintNativeOverlapPlan(),
    _ => Help()
};

static int PrintFinalGatePlan()
{
    var orchestrator = new FinalGateOrchestrator();
    foreach (var step in orchestrator.RequiredLanguageOwnedSteps())
    {
        Console.WriteLine($"{step.Id} [{step.Language}] => {step.Command}");
    }
    return 0;
}

static int PrintNativeOverlapPlan()
{
    Console.WriteLine("C# CLI native-overlap is wired to the C++ merge_analysis.hpp native hot-path plan; build native benchmark before using production interop.");
    return 0;
}

static int RunPython(string repoRoot, string script)
{
    var start = new ProcessStartInfo("python3", script)
    {
        WorkingDirectory = repoRoot,
        RedirectStandardOutput = false,
        RedirectStandardError = false,
        UseShellExecute = false
    };
    using var process = Process.Start(start);
    process?.WaitForExit();
    return process?.ExitCode ?? 1;
}

static int Help()
{
    Console.WriteLine("HELIOS.Cli commands: language-score, merge-decision, final-gate-plan, validate-reports, native-overlap");
    return 0;
}

static string FindRepoRoot(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, ".git"))) return dir.FullName;
        dir = dir.Parent;
    }
    return Directory.GetCurrentDirectory();
}

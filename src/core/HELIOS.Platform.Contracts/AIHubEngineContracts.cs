namespace HELIOS.Platform.Contracts.AIHub;

/// <summary>
/// Language/runtime placement used by the AIHub orchestration layer.
/// </summary>
public enum AIHubEngineKind
{
    CSharpOrchestrator,
    FSharpLearning,
    CppNativePerformance,
    PythonIntegration,
    YamlAutomation,
    BicepCloud,
    JsonLedger,
    MarkdownKnowledge,
    HermesAgent,
    XCoreAgent
}

/// <summary>
/// A scored route from the C# orchestration backbone into a partner engine.
/// </summary>
public sealed record AIHubEngineRoute(
    string Module,
    string Submodule,
    AIHubEngineKind Engine,
    double Score,
    string Purpose,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<string> Outputs,
    IReadOnlyList<string> RequiredChecks);

/// <summary>
/// Contract for orchestration code that chooses the best partner language/agent path.
/// </summary>
public interface IAIHubOrchestrationPlanner
{
    AIHubEngineRoute SelectRoute(string module, string submodule, IReadOnlyDictionary<string, double> signals);
    IReadOnlyList<AIHubEngineRoute> RankRoutes(string module, IReadOnlyDictionary<string, double> signals);
}

/// <summary>
/// Default C# backbone planner: keeps security, identity, GUI, vault, and API flow in C# while
/// pushing math/learning to F#, hot paths to C++, and provider/agent glue to Python.
/// </summary>
public sealed class AIHubDefaultOrchestrationPlanner : IAIHubOrchestrationPlanner
{
    private static readonly string[] CommonInputs = ["repo", "commit", "branch", "tool-readiness", "cost", "quality", "latency", "risk"];

    public AIHubEngineRoute SelectRoute(string module, string submodule, IReadOnlyDictionary<string, double> signals) =>
        RankRoutes(module, signals).OrderByDescending(route => route.Score).First() with { Submodule = submodule };

    public IReadOnlyList<AIHubEngineRoute> RankRoutes(string module, IReadOnlyDictionary<string, double> signals)
    {
        var normalized = signals.ToDictionary(pair => pair.Key, pair => Math.Clamp(pair.Value, 0.0, 1.0), StringComparer.OrdinalIgnoreCase);
        return new[]
        {
            Route(module, "secure-gui-api-vault", AIHubEngineKind.CSharpOrchestrator, normalized, 0.18, 0.10, 0.08, 0.34, "Own the clean frame: auth, vault, GUI state, policy flags, typed APIs, and cross-engine dispatch."),
            Route(module, "deep-learning-ranking", AIHubEngineKind.FSharpLearning, normalized, 0.30, 0.10, 0.10, 0.08, "Rank branches, module/submodule choices, agent XP, prediction, async queries, and absorption scores."),
            Route(module, "native-hot-path", AIHubEngineKind.CppNativePerformance, normalized, 0.12, 0.34, 0.30, 0.05, "Accelerate memory-sensitive algorithms, vector scoring, graph traversal, rendering, and high-volume comparisons."),
            Route(module, "aihub-provider-linux-glue", AIHubEngineKind.PythonIntegration, normalized, 0.10, 0.08, 0.08, 0.10, "Use Python only where libraries, Linux tooling, AIHub providers, or quick automation create a specific edge."),
            Route(module, "hermes-xcore-fleet", AIHubEngineKind.HermesAgent, normalized, 0.18, 0.12, 0.10, 0.14, "Deploy Hermes/XCore specialties for branch testing, autofix, learning, GitHub/Azure checks, and fleet XP."),
        };
    }

    private static AIHubEngineRoute Route(string module, string submodule, AIHubEngineKind engine, IReadOnlyDictionary<string, double> signals, double noveltyWeight, double speedWeight, double memoryWeight, double safetyWeight, string purpose)
    {
        var novelty = Value(signals, "novelty");
        var speed = Value(signals, "speed");
        var memory = Value(signals, "memory");
        var safety = Value(signals, "safety");
        var quality = Value(signals, "quality");
        var tests = Value(signals, "tests");
        var cost = 1.0 - Value(signals, "cost");
        var score = Math.Clamp((novelty * noveltyWeight) + (speed * speedWeight) + (memory * memoryWeight) + (safety * safetyWeight) + (quality * 0.18) + (tests * 0.14) + (cost * 0.08), 0.0, 1.0);
        return new AIHubEngineRoute(module, submodule, engine, score, purpose, CommonInputs, Outputs(engine), Checks(engine));
    }

    private static double Value(IReadOnlyDictionary<string, double> signals, string key) =>
        signals.TryGetValue(key, out var value) ? Math.Clamp(value, 0.0, 1.0) : 0.5;

    private static string[] Outputs(AIHubEngineKind engine) => engine switch
    {
        AIHubEngineKind.FSharpLearning => ["ranked-decisions", "prediction-score", "learning-signal"],
        AIHubEngineKind.CppNativePerformance => ["native-score", "hot-path-result", "memory-profile"],
        AIHubEngineKind.PythonIntegration => ["provider-route", "tool-result", "linux-library-output"],
        AIHubEngineKind.HermesAgent => ["agent-plan", "branch-test-packet", "xp-update"],
        _ => ["orchestration-route", "policy-decision", "gui-state"]
    };

    private static string[] Checks(AIHubEngineKind engine) => engine switch
    {
        AIHubEngineKind.FSharpLearning => ["dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj"],
        AIHubEngineKind.CppNativePerformance => ["cmake -S src/native/HELIOS.Native.Performance -B .build/native", "cmake --build .build/native"],
        AIHubEngineKind.PythonIntegration => ["python3 -m py_compile scripts/integrations/aihub_full_framework.py"],
        AIHubEngineKind.HermesAgent => ["python3 scripts/agents/branch_test_autofix_plan.py"],
        _ => ["dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj"]
    };
}

/// <summary>
/// Named module families in the AIHub nervous system.
/// </summary>
public enum AIHubModuleKind
{
    GuiCommandCenter,
    UsbWizard,
    AIHubEngine,
    GitHubGraph,
    AzureCloudGraph,
    ProfilePerformanceSecurity
}

/// <summary>
/// A C#-owned module/submodule contract that declares where C++, F#, and Python should participate.
/// </summary>
public sealed record AIHubModuleBlueprint(
    AIHubModuleKind Module,
    string Foundation,
    IReadOnlyList<string> Submodules,
    IReadOnlyList<string> CppHotPaths,
    IReadOnlyList<string> FSharpAnalytics,
    IReadOnlyList<string> PythonBridges,
    IReadOnlyList<string> RequiredChecks);

/// <summary>
/// Built-in blueprint catalog used by the GUI, USBWizard, and AIHub setup flows.
/// </summary>
public static class AIHubModuleBlueprintCatalog
{
    public static IReadOnlyList<AIHubModuleBlueprint> All { get; } =
    [
        new AIHubModuleBlueprint(
            AIHubModuleKind.GuiCommandCenter,
            "C# WinUI3 shell, secure commands, vault forms, and orchestration state.",
            ["navigation", "agent-party-cards", "vault-setup", "github-graph", "azure-graph", "learning-feedback"],
            ["graph-layout", "rendering-overlays", "telemetry-panels", "diff-visualization"],
            ["priority-scoring", "recommendation-ranking", "xp-prediction"],
            ["provider-discovery", "report-ingestion", "llm-text-generation"],
            ["dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj", "python3 scripts/dashboard/generate-gui.py"]),
        new AIHubModuleBlueprint(
            AIHubModuleKind.UsbWizard,
            "C# wizard flow, safety policy, profile selection, and rollback plan.",
            ["boot-media-plan", "windows-profile-presets", "partition-model", "security-baseline", "performance-profile", "offline-vault-handoff"],
            ["device-scan", "partition-validation", "checksum-verification", "secure-memory"],
            ["risk-scoring", "profile-fit-prediction", "rollback-confidence", "security-performance-ranking"],
            ["wsl2-imaging-helper", "linux-tool-bridge"],
            ["python3 scripts/integrations/aihub_supershell_vault_wizard.py"]),
        new AIHubModuleBlueprint(
            AIHubModuleKind.AIHubEngine,
            "C# provider router, Hermes/XCore registry, typed policies, and model/tool dispatch.",
            ["llm-provider-router", "agent-registry", "prompt-tool-catalog", "learning-ledger", "cost-speed-quality-optimizer"],
            ["vector-similarity", "agent-graph-comparison", "native-scoring-kernels", "memory-aware-batching"],
            ["deep-score-fusion", "ensemble-ranking", "prediction-analytics", "natural-selection-scoring"],
            ["openai-claude-ollama-adapters", "linux-local-model-bridge", "specialized-ml-libraries"],
            ["python3 scripts/integrations/aihub_unified_control_plane.py"])
    ];
}


/// <summary>
/// C# frame for carrying self-learning notes into grading, dashboard, and route decisions.
/// </summary>
public sealed record AIHubSelfLearningFrame(
    string Module,
    string Submodule,
    IReadOnlyList<string> ThoughtTiers,
    IReadOnlyList<string> SmallFileVariables,
    IReadOnlyList<string> SituationVariables,
    IReadOnlyList<string> ConnectionIdeas,
    IReadOnlyList<string> CompositePlans,
    string CSharpFramePurpose)
{
    public static AIHubSelfLearningFrame Empty(string module, string submodule) =>
        new(module, submodule, [], [], [], [], [], "C# keeps the safe frame while F# scores, C++ accelerates, and Python bridges only where needed.");
}

/// <summary>
/// C#-owned decision that binds self-learning notes to engine routes, native assist, and safe report-only actions.
/// </summary>
public sealed record AIHubSelfLearningBackboneDecision(
    AIHubSelfLearningFrame Frame,
    AIHubEngineRoute PrimaryRoute,
    IReadOnlyList<AIHubEngineRoute> RankedRoutes,
    bool NativeAssistRecommended,
    string SafetyMode,
    IReadOnlyList<string> ReportOnlyActions,
    IReadOnlyList<string> Guardrails);

/// <summary>
/// Small C# backbone that coordinates F# scoring, C++ hot-path hints, Python report glue, and dashboard-safe commands.
/// </summary>
public static class AIHubSelfLearningBackbone
{
    private static readonly AIHubDefaultOrchestrationPlanner Planner = new();

    public static AIHubSelfLearningBackboneDecision Plan(AIHubSelfLearningFrame frame, IReadOnlyDictionary<string, double> signals)
    {
        var rankedRoutes = Planner.RankRoutes(frame.Module, signals);
        var primaryRoute = rankedRoutes.OrderByDescending(route => route.Score).First() with { Submodule = frame.Submodule };
        var nativeAssist = ShouldUseNativeAssist(frame, signals);
        var actions = new[]
        {
            "python3 scripts/analysis/aihub_self_learning_notes.py",
            "python3 scripts/analysis/complex_code_grading.py",
            "python3 scripts/analysis/module_submodule_organizer.py",
            "python3 scripts/dashboard/generate-gui.py"
        };
        var guardrails = new[]
        {
            "report-only until AIHUB_LIVE_MUTATION=true is explicitly set",
            "C# owns policy, identity, GUI state, typed API contracts, and cross-engine dispatch",
            "F# owns learning/ranking math; C++ assists only hot-path comparisons; Python remains provider/report glue",
            "destructive file deletion remains suggestion-only unless apply gates and allowlists pass"
        };

        return new AIHubSelfLearningBackboneDecision(frame, primaryRoute, rankedRoutes, nativeAssist, "report-only", actions, guardrails);
    }

    private static bool ShouldUseNativeAssist(AIHubSelfLearningFrame frame, IReadOnlyDictionary<string, double> signals)
    {
        var hotPath = Value(signals, "speed") >= 0.70 || Value(signals, "memory") >= 0.70;
        var manySmallFiles = frame.SmallFileVariables.Count >= 20 || Value(signals, "small-files") >= 0.65;
        var connectionPressure = frame.ConnectionIdeas.Count >= 8 || Value(signals, "connection-pressure") >= 0.65;
        return hotPath && (manySmallFiles || connectionPressure);
    }

    private static double Value(IReadOnlyDictionary<string, double> signals, string key) =>
        signals.TryGetValue(key, out var value) ? Math.Clamp(value, 0.0, 1.0) : 0.0;
}
/// <summary>
/// C# knowledge-baked memory item used by code fixing, dashboard recall, SQL/vector retrieval, and safe apply planning.
/// </summary>
public sealed record AIHubKnowledgeFact(
    string Id,
    string KnowledgeType,
    string Subject,
    string Summary,
    double Confidence,
    double Freshness,
    string SecurityLabel,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> VectorRefs,
    IReadOnlyList<string> SqlRefs);

/// <summary>
/// A report-only code-fix candidate that links a concrete file to remembered knowledge and validation commands.
/// </summary>
public sealed record AIHubCodeFixCandidate(
    string Path,
    string Issue,
    AIHubEngineKind OwnerEngine,
    double Priority,
    IReadOnlyList<AIHubKnowledgeFact> Knowledge,
    IReadOnlyList<string> SuggestedCommands,
    IReadOnlyList<string> Guardrails);

/// <summary>
/// C# optimizer that fuses SQL/vector knowledge with engine routes so code fixing is ranked before any live apply.
/// </summary>
public static class AIHubKnowledgeBakedCodeFixPlanner
{
    public static IReadOnlyList<AIHubCodeFixCandidate> RankFixes(
        IEnumerable<string> changedPaths,
        IEnumerable<AIHubKnowledgeFact> facts,
        IReadOnlyDictionary<string, double> signals)
    {
        var factList = facts.ToArray();
        var routes = new AIHubDefaultOrchestrationPlanner().RankRoutes("knowledge-baked-code-fix", signals);
        return changedPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => BuildCandidate(path, factList, routes, signals))
            .OrderByDescending(candidate => candidate.Priority)
            .ToArray();
    }

    private static AIHubCodeFixCandidate BuildCandidate(
        string path,
        IReadOnlyList<AIHubKnowledgeFact> facts,
        IReadOnlyList<AIHubEngineRoute> routes,
        IReadOnlyDictionary<string, double> signals)
    {
        var matched = facts
            .Where(fact => path.Contains(fact.Subject, StringComparison.OrdinalIgnoreCase) || fact.EvidenceRefs.Any(path.Contains))
            .DefaultIfEmpty(new AIHubKnowledgeFact("fallback", "repo", path, "No exact memory match; use current reports and tests.", 0.45, 0.50, "internal", [], [], []))
            .OrderByDescending(fact => (fact.Confidence * 0.65) + (fact.Freshness * 0.35))
            .Take(5)
            .ToArray();

        var route = routes.OrderByDescending(r => r.Score).First();
        var knowledgeScore = matched.Average(fact => (fact.Confidence * 0.70) + (fact.Freshness * 0.30));
        var testSignal = signals.TryGetValue("tests", out var tests) ? Math.Clamp(tests, 0.0, 1.0) : 0.5;
        var safetySignal = signals.TryGetValue("safety", out var safety) ? Math.Clamp(safety, 0.0, 1.0) : 0.5;
        var priority = Math.Clamp((knowledgeScore * 0.42) + (route.Score * 0.28) + (testSignal * 0.18) + (safetySignal * 0.12), 0.0, 1.0);

        return new AIHubCodeFixCandidate(
            path,
            "knowledge-backed repair candidate",
            route.Engine,
            priority,
            matched,
            ["python3 scripts/integrations/aihub_learning_knowledge_store.py", "python3 scripts/dashboard/generate-gui.py", .. route.RequiredChecks],
            ["report-only by default", "cite knowledge/evidence before patching", "run required checks before apply", "keep C# orchestration as the safety frame"]);
    }
}
/// <summary>
/// C# orchestration result that synchronizes F# ranking, C++ native pressure, and C# safety/policy at the same time.
/// </summary>
public sealed record AIHubTriEngineOptimizationDecision(
    string Subject,
    double CSharpSafetyScore,
    double FSharpLearningScore,
    double CppNativeScore,
    double CombinedScore,
    AIHubEngineKind LeadEngine,
    IReadOnlyList<string> IntegratedCommands,
    IReadOnlyList<string> KnowledgeRefs);

/// <summary>
/// Integrates all three main engines at once: C# guardrails, F# ranking, and C++ native hot-path pressure.
/// </summary>
public static class AIHubTriEngineOptimizer
{
    public static AIHubTriEngineOptimizationDecision Decide(
        string subject,
        double csharpSafetyScore,
        double fsharpLearningScore,
        double cppNativeScore,
        IEnumerable<string> knowledgeRefs)
    {
        var safeCSharp = Math.Clamp(csharpSafetyScore, 0.0, 1.0);
        var rankedFSharp = Math.Clamp(fsharpLearningScore, 0.0, 1.0);
        var nativeCpp = Math.Clamp(cppNativeScore, 0.0, 1.0);
        var combined = Math.Clamp((safeCSharp * 0.34) + (rankedFSharp * 0.38) + (nativeCpp * 0.28), 0.0, 1.0);
        var lead = safeCSharp < 0.55
            ? AIHubEngineKind.CSharpOrchestrator
            : nativeCpp >= 0.72
                ? AIHubEngineKind.CppNativePerformance
                : rankedFSharp >= 0.66
                    ? AIHubEngineKind.FSharpLearning
                    : AIHubEngineKind.CSharpOrchestrator;

        var commands = new[]
        {
            "dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj --nologo",
            "dotnet build src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj --nologo",
            "g++ -std=c++20 -I. -fsyntax-only /tmp/aihub_header_check.cpp",
            "python3 scripts/integrations/aihub_command_ide.py"
        };

        return new AIHubTriEngineOptimizationDecision(subject, safeCSharp, rankedFSharp, nativeCpp, combined, lead, commands, knowledgeRefs.ToArray());
    }
}
/// <summary>
/// Four-engine integration result: C# safety/orchestration, F# ranking, C++ hot paths, and Python provider glue.
/// </summary>
public sealed record AIHubFourEngineIntegrationDecision(
    string Subject,
    double CSharpSafetyScore,
    double FSharpLearningScore,
    double CppNativeScore,
    double PythonGlueScore,
    double CombinedScore,
    AIHubEngineKind LeadEngine,
    IReadOnlyList<AIHubEngineKind> ParallelEngines,
    IReadOnlyList<string> IntegratedCommands);

/// <summary>
/// Optimizes all four AIHub runtime roles together while preserving report-only guardrails.
/// </summary>
public static class AIHubFourEngineIntegrator
{
    public static AIHubFourEngineIntegrationDecision Decide(
        string subject,
        double csharpSafetyScore,
        double fsharpLearningScore,
        double cppNativeScore,
        double pythonGlueScore)
    {
        var csharp = Math.Clamp(csharpSafetyScore, 0.0, 1.0);
        var fsharp = Math.Clamp(fsharpLearningScore, 0.0, 1.0);
        var cpp = Math.Clamp(cppNativeScore, 0.0, 1.0);
        var python = Math.Clamp(pythonGlueScore, 0.0, 1.0);
        var combined = Math.Clamp((csharp * 0.30) + (fsharp * 0.27) + (cpp * 0.22) + (python * 0.21), 0.0, 1.0);
        var lead = csharp < 0.55 ? AIHubEngineKind.CSharpOrchestrator
            : python >= 0.76 ? AIHubEngineKind.PythonIntegration
            : cpp >= 0.72 ? AIHubEngineKind.CppNativePerformance
            : fsharp >= 0.66 ? AIHubEngineKind.FSharpLearning
            : AIHubEngineKind.CSharpOrchestrator;
        var parallel = new List<AIHubEngineKind> { AIHubEngineKind.CSharpOrchestrator };
        if (fsharp >= 0.55) parallel.Add(AIHubEngineKind.FSharpLearning);
        if (cpp >= 0.60 && csharp >= 0.55) parallel.Add(AIHubEngineKind.CppNativePerformance);
        if (python >= 0.55) parallel.Add(AIHubEngineKind.PythonIntegration);
        var commands = new[]
        {
            "dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj --nologo",
            "dotnet build src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj --nologo",
            "g++ -std=c++20 -I. -fsyntax-only /tmp/aihub_header_check.cpp",
            "python3 -m py_compile scripts/integrations/aihub_command_ide.py scripts/integrations/aihub_learning_knowledge_store.py",
            "python3 scripts/integrations/aihub_command_ide.py"
        };
        return new AIHubFourEngineIntegrationDecision(subject, csharp, fsharp, cpp, python, combined, lead, parallel, commands);
    }
}

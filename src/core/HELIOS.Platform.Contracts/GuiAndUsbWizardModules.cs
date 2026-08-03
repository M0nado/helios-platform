namespace HELIOS.Platform.Contracts.AIHub;

/// <summary>
/// Describes a GUI or USBWizard submodule that C# owns and routes into C++, F#, or Python when useful.
/// </summary>
public sealed record AIHubSubmoduleDescriptor(
    string Id,
    string Title,
    string CSharpResponsibility,
    IReadOnlyList<string> CppAccelerationPoints,
    IReadOnlyList<string> FSharpScoringPoints,
    IReadOnlyList<string> PythonBridgePoints,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> RankCriteria);

/// <summary>
/// Concrete GUI command-center module/submodule layout for the front-facing HELIOS AIHub experience.
/// </summary>
public static class GuiCommandCenterModule
{
    public static string ModuleId => "gui-command-center";

    public static IReadOnlyList<AIHubSubmoduleDescriptor> Submodules { get; } =
    [
        new AIHubSubmoduleDescriptor(
            "winui-shell",
            "C# WinUI3 / remote console shell",
            "Own navigation, settings, safe command surfaces, vault input boxes, and friendly operator workflows.",
            ["native graph layout", "render pressure checks", "low-latency telemetry panels"],
            ["button priority ranking", "operator feedback scoring", "agent XP visualization forecasts"],
            ["report ingestion", "LLM/provider status summaries"],
            ["dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj", "python3 scripts/dashboard/generate-gui.py"],
            ["usability", "security", "latency", "clarity", "automation safety"]),
        new AIHubSubmoduleDescriptor(
            "github-azure-graph",
            "Repository, branch, commit, GitHub, and Azure graph",
            "Show repositories, branches, commits, issues, Actions, Codespaces, Pages, wiki, Azure resources, and what needs to improve.",
            ["large graph comparison", "diff rendering", "commit clustering"],
            ["branch issue priority", "merge/prune value", "cloud readiness score"],
            ["GitHub/Azure CLI probes", "report generation"],
            ["python3 scripts/integrations/aihub_integration_graph.py", "python3 scripts/azure/azure_what_if.py"],
            ["coverage", "freshness", "failure count", "merge value", "cost/risk"]),
        new AIHubSubmoduleDescriptor(
            "agent-party-control",
            "Hermes/XCore party member control",
            "Display each agent as a controllable unit with icon, XP, fleet points, deployment target, specialty, tools, and prompts.",
            ["agent graph scoring", "fleet allocation hot paths"],
            ["XP growth prediction", "specialty fit ranking", "cost-speed-quality optimization"],
            ["LLM/agent provider adapters", "prompt pack generation"],
            ["python3 scripts/agents/agent_fleet_autopilot.py --agents 128 --mode hybrid-cloud"],
            ["agent fit", "cost", "speed", "quality", "learning gain"])
    ];
}

/// <summary>
/// Concrete USBWizard module/submodule layout for boot/profile/partition/security/performance setup.
/// </summary>
public static class UsbWizardModule
{
    public static string ModuleId => "usbwizard";

    public static IReadOnlyList<AIHubSubmoduleDescriptor> Submodules { get; } =
    [
        new AIHubSubmoduleDescriptor(
            "boot-media-plan",
            "Bootable USB and offline setup plan",
            "Guide boot media, offline setup bundles, vault handoff, and rollback-first operator choices.",
            ["checksum verification", "device enumeration", "secure memory handling"],
            ["risk score", "rollback confidence", "profile fit"],
            ["WSL2/Linux imaging helpers when safer than native shell"],
            ["python3 scripts/integrations/aihub_supershell_vault_wizard.py"],
            ["boot safety", "device risk", "operator clarity", "rollback readiness"]),
        new AIHubSubmoduleDescriptor(
            "partition-profile-security",
            "Partition, profile, performance, and security presets",
            "Coordinate Windows profiles, partitions, security baseline, performance mode, network/download tuning, and AI-assisted recommendations.",
            ["partition validation", "profile performance kernels", "network/download scoring", "secure buffer checks"],
            ["security/performance tradeoff ranking", "profile prediction", "anomaly detection"],
            ["specialized hardware/library probes only when needed"],
            ["python3 scripts/analysis/aihub_module_blueprint.py", "python3 scripts/security/apply_gate_preflight.py"],
            ["security", "performance", "profile fit", "data safety", "test coverage"])
    ];
}

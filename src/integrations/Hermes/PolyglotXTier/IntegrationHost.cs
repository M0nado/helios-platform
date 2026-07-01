using System.Text.Json;

namespace PolyglotXTier;

public sealed class IntegrationHost
{
    public string GatewayUrl { get; init; } = "http://127.0.0.1:8788";
    public string GuiUrl { get; init; } = "http://127.0.0.1:8501";
    public string ControlUrl { get; init; } = "http://127.0.0.1:8789";

    public object BuildSystemMap() => new
    {
        runtime = new
        {
            gateway = GatewayUrl,
            gui = GuiUrl,
            control = ControlUrl
        },
        orchestration = new
        {
            languages = new[] { "python", "csharp", "cpp", "powershell" },
            pipelines = new[] { "build_all_polyglot", "aihub_upgrade", "docker_runtime" }
        },
        security = new
        {
            posture = "zero-trust-balanced",
            controls = new[]
            {
                "closed_inbound",
                "smart_egress",
                "training_guardrails",
                "drift_alerts",
                "internet_watch",
                "port_watch",
                "folder_permission_governance",
                "quarantine_escalation",
            }
        }
    };

    public void WriteSystemMap(string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var json = JsonSerializer.Serialize(BuildSystemMap(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
    }
}

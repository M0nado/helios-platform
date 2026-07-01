using System.Text.Json;

namespace PolyglotXTier;

public sealed class SecurityFrontendMap
{
    public object BuildFromCppReport(string cppReportPath)
    {
        var data = JsonDocument.Parse(File.ReadAllText(cppReportPath));
        var root = data.RootElement;

        static Dictionary<string, object> ToDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object>();
            foreach (var prop in element.EnumerateObject())
            {
                result[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.Number => prop.Value.TryGetDouble(out var d) ? d : 0.0,
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.ToString(),
                };
            }
            return result;
        }

        var runtimeDecision = root.TryGetProperty("runtime_decision", out var rd) && rd.ValueKind == JsonValueKind.Object
            ? ToDictionary(rd)
            : new Dictionary<string, object>();
        var resourceData = root.TryGetProperty("cpp_security_optimization_data", out var so) && so.ValueKind == JsonValueKind.Object
            ? ToDictionary(so)
            : new Dictionary<string, object>();
        var watchPlan = root.TryGetProperty("security_watch_plan", out var wp) && wp.ValueKind == JsonValueKind.Object
            ? ToDictionary(wp)
            : new Dictionary<string, object>();
        var folderPlan = root.TryGetProperty("folder_governance_plan", out var fp) && fp.ValueKind == JsonValueKind.Object
            ? ToDictionary(fp)
            : new Dictionary<string, object>();
        var alertChannels = root.TryGetProperty("alert_channels", out var ac) && ac.ValueKind == JsonValueKind.Array
            ? ac.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
            : Array.Empty<string>();
        var parallel = root.TryGetProperty("major_parallelization_types", out var mp) && mp.ValueKind == JsonValueKind.Array
            ? mp.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
            : Array.Empty<string>();

        return new
        {
            source = cppReportPath,
            ui_panels = new[]
            {
                "Security Watch Center",
                "Internet + Port Guard",
                "Quarantine and Isolation",
                "Folder Permission Governance",
                "Compression and Sorting",
                "Fleetwide Alert Bus",
            },
            runtime_decision = runtimeDecision,
            security_watch_plan = watchPlan,
            folder_governance_plan = folderPlan,
            cpp_resource_telemetry = resourceData,
            alert_channels = alertChannels,
            major_parallelization_types = parallel,
            scopes = new[] { "hermes", "xcore", "aihub", "fleet_global" },
        };
    }

    public void Write(string cppReportPath, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var payload = BuildFromCppReport(cppReportPath);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
    }
}

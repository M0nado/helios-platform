using System.Text.Json;
using Helios.Connect.Api;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class AgentCorePolicyTests
{
    [Fact]
    public void Policy_loader_reads_required_machine_readable_contract()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HELIOS_AGENT_CORE_POLICY_PATH"] = LocatePolicyFile("agent-core-policy.json")
        }).Build();

        var policy = AgentCorePolicyLoader.Load(configuration);

        Assert.Equal("1.0", policy.SchemaVersion);
        Assert.True(policy.AllowsTransition("INIT", "PRECHECK"));
        Assert.True(policy.AllowsTransition("PRECHECK", "PLAN"));
        Assert.True(policy.AllowsTransition("PLAN", "AWAIT_APPROVAL"));
        Assert.True(policy.AllowsTransition("NOTIFY", "COMPLETE"));
        Assert.Contains("S0", policy.Incident.Suppression.NeverSuppress);
        Assert.Contains("S1", policy.Incident.Suppression.NeverSuppress);
        Assert.False(policy.Rollback.AutomaticRollbackOnAgentError);
        Assert.True(policy.Rollback.RequiresGovernedRequest);
    }

    [Fact]
    public void Strict_mode_rejects_missing_policy_file()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HELIOS_REQUIRE_ENTRA_AUTH"] = "true",
            ["HELIOS_AGENT_CORE_POLICY_PATH"] = "C:\\definitely-missing\\agent-core-policy.json"
        }).Build();

        Assert.Throws<InvalidOperationException>(() => AgentCorePolicyLoader.Load(configuration));
    }

    [Fact]
    public void Policy_validation_rejects_missing_S1_never_suppress_rule()
    {
        var sourcePath = LocatePolicyFile("agent-core-policy.json");
        using var source = JsonDocument.Parse(File.ReadAllText(sourcePath));
        var mutable = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            source.RootElement.GetRawText(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var incident = (JsonElement)mutable["incident"]!;
        var incidentNode = JsonSerializer.Deserialize<Dictionary<string, object?>>(incident.GetRawText())!;
        var suppression = (JsonElement)incidentNode["suppression"]!;
        var suppressionNode = JsonSerializer.Deserialize<Dictionary<string, object?>>(suppression.GetRawText())!;
        suppressionNode["neverSuppress"] = new[] { "S0" };
        incidentNode["suppression"] = suppressionNode;
        mutable["incident"] = incidentNode;

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, JsonSerializer.Serialize(mutable));
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HELIOS_AGENT_CORE_POLICY_PATH"] = tempFile
            }).Build();
            Assert.Throws<InvalidOperationException>(() => AgentCorePolicyLoader.Load(configuration));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static string LocatePolicyFile(string fileName)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var direct = Path.Combine(directory.FullName, "config", fileName);
            if (File.Exists(direct)) return direct;

            var nested = Path.Combine(directory.FullName, "monado", "helios-control", "config", fileName);
            if (File.Exists(nested)) return nested;
        }

        throw new FileNotFoundException($"Could not locate '{fileName}' from '{AppContext.BaseDirectory}'.");
    }
}

using System.Text.Json;

namespace HELIOS.EnterpriseAgents;

public static class ExtendedRegistryLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false
    };

    public static async Task<EnterpriseSubagentRegistry> LoadSubagentsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<EnterpriseSubagentRegistry>(stream, Options, cancellationToken)
               ?? throw new InvalidDataException($"Subagent registry '{path}' was empty.");
    }

    public static async Task<ToolchainManifest> LoadToolchainAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ToolchainManifest>(stream, Options, cancellationToken)
               ?? throw new InvalidDataException($"Toolchain manifest '{path}' was empty.");
    }

    public static async Task<SupplementalConnectionRegistry> LoadSupplementalConnectionsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SupplementalConnectionRegistry>(stream, Options, cancellationToken)
               ?? throw new InvalidDataException($"Supplemental connection registry '{path}' was empty.");
    }

    public static IReadOnlyList<string> Validate(
        EnterpriseAgentFleet coreFleet,
        EnterpriseSubagentRegistry subagents,
        ToolchainManifest toolchain,
        SupplementalConnectionRegistry supplementalConnections)
    {
        var errors = new List<string>();

        if (!string.Equals(subagents.ParentFleet, coreFleet.FleetId, StringComparison.Ordinal))
        {
            errors.Add("The subagent registry does not target the core enterprise fleet.");
        }

        if (subagents.ProductionBlockerIssue != coreFleet.ProductionBlockerIssue)
        {
            errors.Add("Core and subagent registries must share the same production blocker.");
        }

        if (subagents.Subagents.Count != 3)
        {
            errors.Add($"Expected 3 explicit subagents but found {subagents.Subagents.Count}.");
        }

        string[] requiredSubagents = ["xcore-runtime", "microsoft-copilot", "azure-toolchain"];
        foreach (string id in requiredSubagents)
        {
            EnterpriseSubagentDefinition? subagent = subagents.Subagents.SingleOrDefault(
                item => string.Equals(item.Id, id, StringComparison.Ordinal));

            if (subagent is null)
            {
                errors.Add($"Required subagent '{id}' is missing.");
                continue;
            }

            bool parentExists = coreFleet.Agents.Any(
                agent => string.Equals(agent.Id, subagent.ParentAgent, StringComparison.Ordinal));
            if (!parentExists)
            {
                errors.Add($"Subagent '{id}' references unknown parent '{subagent.ParentAgent}'.");
            }

            if (subagent.Allowed.Count == 0 ||
                subagent.Denied.Count == 0 ||
                subagent.HealthChecks.Count == 0 ||
                subagent.Evidence.Count == 0)
            {
                errors.Add($"Subagent '{id}' must declare allowed, denied, health, and evidence contracts.");
            }
        }

        EnterpriseSubagentDefinition? xcore = subagents.Subagents.SingleOrDefault(
            item => string.Equals(item.Id, "xcore-runtime", StringComparison.Ordinal));
        if (xcore is not null && !xcore.Denied.Contains("azure-production-apply", StringComparer.Ordinal))
        {
            errors.Add("XCore must explicitly deny Azure production apply.");
        }

        EnterpriseSubagentDefinition? copilot = subagents.Subagents.SingleOrDefault(
            item => string.Equals(item.Id, "microsoft-copilot", StringComparison.Ordinal));
        if (copilot is not null && !copilot.RequiresApprovalFor.Contains("graph-admin-consent", StringComparer.Ordinal))
        {
            errors.Add("Microsoft Copilot must require approval for Graph admin consent.");
        }

        if (toolchain.ProductionDeployment)
        {
            errors.Add("The toolchain setup manifest must not perform production deployment.");
        }

        if (!toolchain.PathRepairFirst)
        {
            errors.Add("Toolchain setup must repair and verify PATH before tool execution.");
        }

        string[] requiredTools =
        [
            "azure-cli",
            "azure-developer-cli",
            "bicep-cli",
            "azure-functions-core-tools",
            "power-platform-cli",
            "microsoft-365-agents-toolkit",
            "github-cli",
            "powershell-7",
            "dotnet-sdk-8",
            "docker-desktop",
            "nodejs-lts",
            "visual-studio-code"
        ];

        foreach (string id in requiredTools)
        {
            ToolchainToolDefinition? tool = toolchain.Tools.SingleOrDefault(
                item => string.Equals(item.Id, id, StringComparison.Ordinal));
            if (tool is null)
            {
                errors.Add($"Required tool '{id}' is missing from the toolchain manifest.");
                continue;
            }

            if (tool.Verify.Count == 0)
            {
                errors.Add($"Tool '{id}' must declare at least one verification command.");
            }
        }

        if (!string.Equals(supplementalConnections.DefaultPolicy, "deny-undeclared", StringComparison.Ordinal))
        {
            errors.Add("Supplemental connections must use deny-undeclared policy.");
        }

        string[] requiredConnections =
        [
            "xcore-runtime-mcp",
            "microsoft-copilot-agent-plane",
            "azure-toolchain-control"
        ];

        foreach (string id in requiredConnections)
        {
            CustomConnectionDefinition? connection = supplementalConnections.Connections.SingleOrDefault(
                item => string.Equals(item.Id, id, StringComparison.Ordinal));
            if (connection is null)
            {
                errors.Add($"Required supplemental connection '{id}' is missing.");
                continue;
            }

            if (connection.WriteTools.Count > 0 && connection.ApprovalRequired.Count == 0)
            {
                errors.Add($"Write-capable connection '{id}' must declare approvals.");
            }
        }

        return errors;
    }
}

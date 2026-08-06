using System.Text.Json;

namespace HELIOS.EnterpriseAgents;

public static class AgentRegistryLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false
    };

    public static async Task<EnterpriseAgentFleet> LoadFleetAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using FileStream stream = File.OpenRead(path);
        EnterpriseAgentFleet? registry = await JsonSerializer.DeserializeAsync<EnterpriseAgentFleet>(
            stream,
            Options,
            cancellationToken);

        return registry ?? throw new InvalidDataException($"Fleet registry '{path}' was empty.");
    }

    public static async Task<CustomConnectionRegistry> LoadConnectionsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using FileStream stream = File.OpenRead(path);
        CustomConnectionRegistry? registry = await JsonSerializer.DeserializeAsync<CustomConnectionRegistry>(
            stream,
            Options,
            cancellationToken);

        return registry ?? throw new InvalidDataException($"Connection registry '{path}' was empty.");
    }

    public static IReadOnlyList<string> Validate(
        EnterpriseAgentFleet fleet,
        CustomConnectionRegistry connections)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        ArgumentNullException.ThrowIfNull(connections);

        var errors = new List<string>();

        if (!string.Equals(fleet.CanonicalRepository, "M0nado/helios-platform", StringComparison.Ordinal))
        {
            errors.Add("The canonical repository must be M0nado/helios-platform.");
        }

        if (fleet.ProductionBlockerIssue != 162)
        {
            errors.Add("Production blocker must reference GitHub Issue #162.");
        }

        if (fleet.ProductionEnabled)
        {
            errors.Add("Production must remain disabled until the external gate confirms Issue #162 is closed.");
        }

        if (fleet.Agents.Count != 20)
        {
            errors.Add($"Expected 20 enterprise agents but found {fleet.Agents.Count}.");
        }

        AddDuplicateErrors(fleet.Agents.Select(agent => agent.Id), "agent", errors);
        AddDuplicateErrors(connections.Connections.Select(connection => connection.Id), "connection", errors);

        foreach (EnterpriseAgentDefinition agent in fleet.Agents)
        {
            if (string.IsNullOrWhiteSpace(agent.Id) ||
                string.IsNullOrWhiteSpace(agent.Name) ||
                string.IsNullOrWhiteSpace(agent.Runtime) ||
                string.IsNullOrWhiteSpace(agent.Purpose))
            {
                errors.Add("Every agent requires id, name, runtime, and purpose.");
                continue;
            }

            if (agent.Allowed.Count == 0)
            {
                errors.Add($"Agent '{agent.Id}' does not declare allowed operations.");
            }

            if (agent.Denied.Count == 0)
            {
                errors.Add($"Agent '{agent.Id}' does not declare denied operations.");
            }
        }

        EnterpriseAgentDefinition? openAiProvider = fleet.Agents.SingleOrDefault(
            agent => string.Equals(agent.Id, "openai-provider", StringComparison.Ordinal));

        if (openAiProvider is null)
        {
            errors.Add("The OpenAI Provider Agent is missing.");
        }
        else
        {
            if (!openAiProvider.SecretReferences.Contains("OPENAI_API_KEY", StringComparer.Ordinal))
            {
                errors.Add("The OpenAI Provider Agent must reference OPENAI_API_KEY without storing its value.");
            }

            if (!openAiProvider.Denied.Contains("api-key-readback", StringComparer.Ordinal))
            {
                errors.Add("The OpenAI Provider Agent must explicitly deny API-key readback.");
            }
        }

        if (!string.Equals(connections.DefaultPolicy, "deny-undeclared", StringComparison.Ordinal))
        {
            errors.Add("Custom connections must use deny-undeclared as the default policy.");
        }

        foreach (CustomConnectionDefinition connection in connections.Connections)
        {
            if (string.IsNullOrWhiteSpace(connection.Id) ||
                string.IsNullOrWhiteSpace(connection.Kind) ||
                string.IsNullOrWhiteSpace(connection.Auth.Mode) ||
                string.IsNullOrWhiteSpace(connection.HealthCheck))
            {
                errors.Add($"Connection '{connection.Id}' is missing required metadata.");
            }

            if (connection.WriteTools.Count > 0 && connection.ApprovalRequired.Count == 0)
            {
                bool inherentlyLowRisk = string.Equals(connection.Id, "linear-execution", StringComparison.Ordinal);
                if (!inherentlyLowRisk)
                {
                    errors.Add($"Write-capable connection '{connection.Id}' must declare approval requirements.");
                }
            }
        }

        return errors;
    }

    public static EnterpriseAgentDefinition RequireAgent(
        EnterpriseAgentFleet fleet,
        string agentId)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        return fleet.Agents.SingleOrDefault(agent =>
                   string.Equals(agent.Id, agentId, StringComparison.Ordinal))
               ?? throw new KeyNotFoundException($"Agent '{agentId}' is not registered.");
    }

    private static void AddDuplicateErrors(
        IEnumerable<string> identifiers,
        string category,
        ICollection<string> errors)
    {
        foreach (IGrouping<string, string> group in identifiers
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .GroupBy(value => value, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            errors.Add($"Duplicate {category} id '{group.Key}'.");
        }
    }
}

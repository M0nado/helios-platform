using System.Text.Json;
using Helios.Connect.Contracts;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class EnterpriseSubAgentFleetRegistryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Enterprise_registry_contains_full_issue_165_roster()
    {
        var registry = LoadEnterpriseRegistry();
        var expectedIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "deployment-supervisor-agent",
            "azure-cli-azd-bootstrap-agent",
            "bicep-infrastructure-agent",
            "entra-identity-agent",
            "github-oidc-agent",
            "key-vault-secret-broker-agent",
            "azure-ai-foundry-agent",
            "container-apps-functions-broker-agent",
            "microsoft-graph-agent",
            "teams-operations-agent",
            "sharepoint-onedrive-publisher-agent",
            "linear-synchronization-agent",
            "slack-operations-agent",
            "hermes-registration-agent",
            "aihub-registration-agent",
            "openai-provider-agent",
            "validation-observability-agent",
            "rollback-agent",
            "compliance-evidence-agent",
            "custom-plugin-mcp-connection-agent"
        };

        var actualIds = registry.Agents.Select(agent => agent.Id).ToHashSet(StringComparer.Ordinal);
        var missingIds = expectedIds.Except(actualIds).ToArray();
        var unexpectedIds = actualIds.Except(expectedIds).ToArray();

        Assert.Equal(expectedIds.Count, registry.Agents.Count);
        Assert.Equal(registry.Agents.Count, registry.Agents.Select(agent => agent.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.True(
            expectedIds.SetEquals(actualIds),
            $"Missing IDs: {string.Join(", ", missingIds)}. Unexpected IDs: {string.Join(", ", unexpectedIds)}.");
    }

    [Fact]
    public void Enterprise_registry_enforces_non_mutating_defaults_and_issue_162_gate()
    {
        var registry = LoadEnterpriseRegistry();
        Assert.Equal("what-if-only", registry.DefaultExecutionMode);
        Assert.False(registry.ProductionGate.AutomaticApplyAllowed);
        Assert.True(registry.ProductionGate.ProtectedEnvironmentRequired);
        Assert.True(registry.ProductionGate.ImmutableImageDigestRequired);
        Assert.True(registry.ProductionGate.WhatIfEvidenceRequired);
        Assert.True(registry.ProductionGate.RollbackPlanRequired);
        Assert.Equal(162, registry.IntegrityGate.BlockingIssue);
        Assert.Equal("blocked", registry.IntegrityGate.Status);
        Assert.False(registry.IntegrityGate.ProductionProvisioningAllowed);
    }

    [Fact]
    public void Enterprise_registry_has_bounded_agent_contracts_and_event_requirements()
    {
        var registry = LoadEnterpriseRegistry();
        Assert.Equal("config/integrations/event-contract.schema.json", registry.EventContract.SchemaPath);
        Assert.True(registry.EventContract.CorrelationIdRequired);
        Assert.True(registry.EventContract.EvidenceLinksRequired);
        Assert.False(registry.EventContract.SecretsInPayloadAllowed);

        foreach (var agent in registry.Agents)
        {
            Assert.Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$", agent.Id);
            Assert.False(string.IsNullOrWhiteSpace(agent.DisplayName));
            Assert.InRange(agent.PermissionTier, 0, 4);
            Assert.False(string.IsNullOrWhiteSpace(agent.WriteBoundary));
            Assert.False(string.IsNullOrWhiteSpace(agent.Identity.Type));
            Assert.False(string.IsNullOrWhiteSpace(agent.Identity.Scope));
            Assert.NotEmpty(agent.Identity.LeastPrivilegeRoles);
            Assert.False(string.IsNullOrWhiteSpace(agent.Identity.CredentialSource));
            Assert.NotEmpty(agent.AllowedTools);
            Assert.NotEmpty(agent.DeniedOperations);
            Assert.NotEmpty(agent.RequiredApprovals);
            Assert.NotEmpty(agent.EvidenceOutputs);
            Assert.NotEmpty(agent.HealthChecks);
        }

        var connectorIds = registry.Connectors.Select(connector => connector.Id).ToHashSet(StringComparer.Ordinal);
        var requiredConnectorIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "azure",
            "entra",
            "microsoft-graph",
            "teams",
            "sharepoint-onedrive",
            "azure-ai-foundry",
            "application-insights",
            "github",
            "linear",
            "slack",
            "openai",
            "custom-mcp-connector-plane"
        };

        Assert.True(requiredConnectorIds.SetEquals(connectorIds));
        Assert.All(registry.Connectors, connector =>
        {
            Assert.False(string.IsNullOrWhiteSpace(connector.Provider));
            Assert.False(string.IsNullOrWhiteSpace(connector.Adapter));
            Assert.False(string.IsNullOrWhiteSpace(connector.Mode));
            Assert.NotEmpty(connector.RequiredAgentIds);
        });
    }

    [Fact]
    public void Custom_mcp_contract_is_explicit_and_deny_by_default()
    {
        var registry = LoadEnterpriseRegistry();
        Assert.True(registry.CustomMcpConnectorPlane.ExplicitToolAllowlistRequired);
        Assert.False(registry.CustomMcpConnectorPlane.DynamicToolDiscoveryAllowed);
        Assert.Equal("read-only", registry.CustomMcpConnectorPlane.DefaultConnectorMode);

        var contractPath = ResolveControlRootPath(registry.CustomMcpConnectorPlane.ContractFile);
        Assert.True(File.Exists(contractPath), $"Missing MCP contract file: {contractPath}");

        var contract = LoadCustomMcpContract(contractPath);
        Assert.Equal(1, contract.SchemaVersion);
        Assert.Equal("helios-custom-mcp-connector-plane", contract.ContractId);
        Assert.Equal("deny-by-default", contract.DefaultAccess);
        Assert.True(contract.ExplicitToolAllowlistRequired);
        Assert.False(contract.DynamicToolDiscoveryAllowed);

        var serverIds = contract.Servers.Select(server => server.Id).ToHashSet(StringComparer.Ordinal);
        var expectedServerIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "helios-azure",
            "azure-mcp-readonly",
            "azure-devops-readonly",
            "github",
            "linear",
            "slack",
            "foundry"
        };

        Assert.True(expectedServerIds.SetEquals(serverIds));
        foreach (var server in contract.Servers)
        {
            Assert.Equal("read-only", server.Mode);
            Assert.False(server.WriteToolsAllowed);
            Assert.NotEmpty(server.AllowedToolContracts);
            Assert.NotEmpty(server.DeniedToolContracts);
        }
    }

    private static EnterpriseSubAgentFleetRegistry LoadEnterpriseRegistry()
    {
        var path = ResolveControlRootPath("config/enterprise-sub-agent-fleet.json");
        var registry = JsonSerializer.Deserialize<EnterpriseSubAgentFleetRegistry>(File.ReadAllText(path), JsonOptions);
        return registry ?? throw new InvalidOperationException("Enterprise registry JSON could not be deserialized.");
    }

    private static EnterpriseCustomMcpConnectorPlaneContract LoadCustomMcpContract(string path)
    {
        var contract = JsonSerializer.Deserialize<EnterpriseCustomMcpConnectorPlaneContract>(File.ReadAllText(path), JsonOptions);
        return contract ?? throw new InvalidOperationException("Custom MCP contract JSON could not be deserialized.");
    }

    private static string ResolveControlRootPath(string relativePath)
    {
        var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(GetControlRootDirectory(), normalizedRelativePath));
    }

    private static string GetControlRootDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Helios.Connect.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Helios.Connect.sln from the test execution directory.");
    }
}

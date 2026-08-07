namespace Helios.Connect.Contracts;

public sealed record EnterpriseSubAgentFleetRegistry(
    int SchemaVersion,
    string RegistryId,
    string SourceIssue,
    string ParentIssue,
    IReadOnlyList<string> CoordinatedIssues,
    string DefaultExecutionMode,
    EnterpriseProductionGate ProductionGate,
    EnterpriseIntegrityGate IntegrityGate,
    EnterpriseEventContract EventContract,
    IReadOnlyList<EnterpriseConnectorContract> Connectors,
    EnterpriseCustomMcpConnectorPlaneReference CustomMcpConnectorPlane,
    IReadOnlyList<EnterpriseSubAgentContract> Agents);

public sealed record EnterpriseProductionGate(
    bool AutomaticApplyAllowed,
    bool ProtectedEnvironmentRequired,
    bool ImmutableImageDigestRequired,
    bool WhatIfEvidenceRequired,
    bool RollbackPlanRequired);

public sealed record EnterpriseIntegrityGate(
    int BlockingIssue,
    string Status,
    bool ProductionProvisioningAllowed,
    string BlockingReason);

public sealed record EnterpriseEventContract(
    string SchemaPath,
    bool CorrelationIdRequired,
    bool EvidenceLinksRequired,
    bool SecretsInPayloadAllowed);

public sealed record EnterpriseConnectorContract(
    string Id,
    string Provider,
    string Adapter,
    string Mode,
    IReadOnlyList<string> RequiredAgentIds);

public sealed record EnterpriseCustomMcpConnectorPlaneReference(
    string ContractFile,
    bool ExplicitToolAllowlistRequired,
    bool DynamicToolDiscoveryAllowed,
    string DefaultConnectorMode);

public sealed record EnterpriseSubAgentContract(
    string Id,
    string DisplayName,
    int PermissionTier,
    string WriteBoundary,
    EnterpriseAgentIdentity Identity,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> DeniedOperations,
    IReadOnlyList<string> RequiredApprovals,
    IReadOnlyList<string> EvidenceOutputs,
    IReadOnlyList<string> HealthChecks);

public sealed record EnterpriseAgentIdentity(
    string Type,
    string Scope,
    IReadOnlyList<string> LeastPrivilegeRoles,
    string CredentialSource);

public sealed record EnterpriseCustomMcpConnectorPlaneContract(
    int SchemaVersion,
    string ContractId,
    string DefaultAccess,
    bool ExplicitToolAllowlistRequired,
    bool DynamicToolDiscoveryAllowed,
    IReadOnlyList<EnterpriseMcpServerContract> Servers);

public sealed record EnterpriseMcpServerContract(
    string Id,
    string SourceManifest,
    string Mode,
    bool WriteToolsAllowed,
    IReadOnlyList<string> AllowedToolContracts,
    IReadOnlyList<string> DeniedToolContracts);

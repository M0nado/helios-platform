using System;
using System.Collections.Generic;
using System.Linq;
using HELIOS.Platform.Contracts.Monadoblade;
using Xunit;

namespace HELIOS.Platform.Contracts.Tests;

public class MonadoEnterpriseExperienceFabricV2ContractsTests
{
    [Fact]
    public void ValidateAndFreezeBudgets_RequiresAllProfiles()
    {
        var budgets = BuildBudgets().Where(value => value.ProfileId != MonadoEnterpriseProfileId.Studio);

        var exception = Assert.Throws<InvalidOperationException>(
            () => MonadoEnterpriseExperienceFabricV2Catalog.ValidateAndFreezeBudgets(budgets));
        Assert.Contains("Missing ALVIS profile budgets", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateAndFreezeBudgets_RequiresSysAdminApprovalBoundary()
    {
        var budgets = BuildBudgets().Select(
            value => value.ProfileId == MonadoEnterpriseProfileId.SysAdmin
                ? value with { ApplyDeniedWithoutExplicitApproval = false }
                : value);

        var exception = Assert.Throws<InvalidOperationException>(
            () => MonadoEnterpriseExperienceFabricV2Catalog.ValidateAndFreezeBudgets(budgets));
        Assert.Contains("explicit-approval only", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MonadoOpenAiProposal_RejectsExpiredUtcProposal()
    {
        var now = new DateTimeOffset(2026, 8, 7, 1, 0, 0, TimeSpan.Zero);
        var proposal = BuildProposal(
            actionType: "proposal",
            approvalRequired: false,
            expiresAtUtc: now.AddMinutes(-1));

        var exception = Assert.Throws<InvalidOperationException>(() => proposal.Validate(now));
        Assert.Contains("future UTC instant", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MonadoOpenAiProposal_RequiresApprovalForPrivilegedActions()
    {
        var now = new DateTimeOffset(2026, 8, 7, 1, 0, 0, TimeSpan.Zero);
        var proposal = BuildProposal(
            actionType: "privileged-proposal",
            approvalRequired: false,
            expiresAtUtc: now.AddMinutes(30));

        var exception = Assert.Throws<InvalidOperationException>(() => proposal.Validate(now));
        Assert.Contains("require approval.required=true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MonadoOpenAiProposal_AcceptsSchemaAlignedProposal()
    {
        var now = new DateTimeOffset(2026, 8, 7, 1, 0, 0, TimeSpan.Zero);
        var proposal = BuildProposal(
            actionType: "deployment-proposal",
            approvalRequired: true,
            expiresAtUtc: now.AddMinutes(45));

        proposal.Validate(now);
    }

    private static MonadoOpenAiProposal BuildProposal(
        string actionType,
        bool approvalRequired,
        DateTimeOffset expiresAtUtc)
    {
        return new MonadoOpenAiProposal(
            SchemaVersion: "2.0.0",
            ProposalId: "proposal-206",
            ProfileId: MonadoEnterpriseProfileId.Developer,
            CorrelationId: "corr-206",
            RequestedBy: new MonadoProposalRequester("human", "operator-01"),
            RiskLevel: "medium",
            ActionType: actionType,
            Summary: "Bounded proposal with approval and rollback evidence.",
            ExpiresAtUtc: expiresAtUtc,
            EvidenceLinks:
            [
                new MonadoEvidenceLink("issue", new Uri("https://github.com/M0nado/helios-platform/issues/206")),
            ],
            RollbackPlan: new MonadoRollbackPlan(
                Strategy: "proposal-with-rollback",
                Steps:
                [
                    "cancel pending rollout",
                    "restore known-good settings",
                ]),
            Approval: new MonadoApprovalGate(
                Required: approvalRequired,
                Status: "pending",
                Reference: null));
    }

    private static IReadOnlyList<AlvisProfileToolBudget> BuildBudgets() =>
    [
        new AlvisProfileToolBudget(MonadoEnterpriseProfileId.Core, 20, false, false),
        new AlvisProfileToolBudget(MonadoEnterpriseProfileId.Developer, 120, false, false),
        new AlvisProfileToolBudget(MonadoEnterpriseProfileId.Gamer, 25, false, false),
        new AlvisProfileToolBudget(MonadoEnterpriseProfileId.Studio, 40, false, false),
        new AlvisProfileToolBudget(MonadoEnterpriseProfileId.Personal, 20, false, false),
        new AlvisProfileToolBudget(MonadoEnterpriseProfileId.SysOps, 80, true, false),
        new AlvisProfileToolBudget(MonadoEnterpriseProfileId.AiServer, 100, true, false),
        new AlvisProfileToolBudget(MonadoEnterpriseProfileId.SysAdmin, 30, true, true),
    ];
}

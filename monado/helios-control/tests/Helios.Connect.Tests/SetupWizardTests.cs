using Helios.Connect.Api;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class SetupWizardTests
{
    private readonly SetupWizardService _wizard = new();

    [Fact]
    public void Bootstrap_is_deterministic_plan_only_and_contains_no_secret()
    {
        var request = new SetupBootstrapRequest("11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "helios-dev-rg", "dev");
        var first = _wizard.CreateBootstrap(request);
        var second = _wizard.CreateBootstrap(request);
        Assert.Equal(first.ScriptSha256, second.ScriptSha256);
        Assert.False(first.ContainsSecrets);
        Assert.False(first.AppliesChanges);
        Assert.Contains("-Mode Diagnose", first.Script);
        Assert.Contains("-Mode Plan", first.Script);
        Assert.DoesNotContain("-Mode Apply", first.Script);
        Assert.DoesNotContain("keyvault secret", first.Script, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("group'; az group delete --name bad; '")]
    [InlineData("group\nmalicious")]
    public void Bootstrap_rejects_shell_injection(string group)
    {
        var request = new SetupBootstrapRequest("11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", group, "dev");
        Assert.Throws<ArgumentException>(() => _wizard.CreateBootstrap(request));
    }

    [Fact]
    public void Upgrade_is_deterministic_and_requires_draft_pr_promotion()
    {
        var request = new UpgradeProposalRequest("dns/private-zone-planner", "Add a reviewed private DNS planning module.");
        var proposal = _wizard.CreateUpgradeProposal(request);
        Assert.False(proposal.AutomaticApply);
        Assert.False(proposal.AutomaticMerge);
        Assert.Equal("task-branch-and-draft-pull-request", proposal.Promotion);
        Assert.Contains("protected-review", proposal.RequiredChecks);
        Assert.Equal(proposal.ProposalId, _wizard.CreateUpgradeProposal(request).ProposalId);
    }
}

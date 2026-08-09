using System.Text.Json;
using Helios.Operator;
using Xunit;

namespace Helios.Operator.Tests;

public sealed class OperatorPolicyValidatorTests
{
    private const string Manifest = """
      {"version":"1.0","environment":"production","declared":{"githubApps":["approved-app"],"secretReads":["kv/approved"],"oidcSubjects":["repo:M0nado/helios-platform:environment:production"]}}
      """;

    public static TheoryData<string, string> ProhibitedActions => new()
    {
        { "{\"type\":\"azure.rbac.assign\",\"target\":\"11111111-1111-1111-1111-111111111111\"}", "own RBAC" },
        { "{\"type\":\"github.environment.reviewer.add\",\"target\":\"helios-agent\"}", "itself" },
        { "{\"type\":\"github.branch_rule.update\",\"requireStatusChecks\":false}", "weakened" },
        { "{\"type\":\"github.app.use\",\"app\":\"unknown-app\"}", "not declared" },
        { "{\"type\":\"secret.read\",\"secret\":\"kv/unknown\"}", "not declared" },
        { "{\"type\":\"azure.oidc.configure\",\"subject\":\"repo:M0nado/*\"}", "wildcards" },
        { "{\"type\":\"container.deploy\",\"image\":\"ghcr.io/m0nado/helios:latest\"}", "immutable digest" },
        { "{\"type\":\"repository.checkout\",\"ref\":\"main\"}", "commit SHAs" },
        { "{\"type\":\"command.execute\",\"args\":[\"--client-secret=plaintext\"]}", "CLI arguments" },
        { "{\"type\":\"identity.bind\",\"developmentIdentity\":\"shared\",\"productionIdentity\":\"shared\"}", "must not reuse" },
        { "{\"type\":\"approval.grant\",\"approver\":\"agent-1\"}", "own changes" }
    };

    [Theory]
    [MemberData(nameof(ProhibitedActions))]
    public void RejectsEachProhibitedPolicyCondition(string action, string diagnostic)
    {
        using var manifest = JsonDocument.Parse(Manifest);
        using var plan = JsonDocument.Parse(Plan(action));

        var errors = OperatorPolicyValidator.Validate(manifest.RootElement, plan.RootElement);

        Assert.Contains(errors, error => error.Message.Contains(diagnostic, StringComparison.OrdinalIgnoreCase));
        Assert.All(errors, error => Assert.StartsWith("plan/actions/0", error.Path));
    }

    [Theory]
    [InlineData("{\"type\":\"azure.rbac.assign\",\"target\":\"subscriptions/production\",\"principalId\":\"11111111-1111-1111-1111-111111111111\"}", "principalId")]
    [InlineData("{\"type\":\"github.environment.reviewer.add\",\"target\":\"production\",\"reviewer\":\"helios-agent\"}", "reviewer")]
    public void RejectsActionSpecificSelfAssignmentWhenResourceTargetIsPresent(string action, string field)
    {
        var errors = Validate(action);

        Assert.Contains(errors, error => error.Path.EndsWith(field, StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsRulesetWeakening()
    {
        var errors = Validate("{\"type\":\"github.ruleset.update\",\"requireStatusChecks\":false}");

        Assert.Contains(errors, error => error.Message.Contains("weakened", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsUndeclaredExactOidcSubject()
    {
        var errors = Validate("{\"type\":\"azure.oidc.configure\",\"subject\":\"repo:someone/else:environment:production\"}");

        Assert.Contains(errors, error => error.Message.Contains("not declared", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsSelfApprovalByAzurePrincipalId()
    {
        var errors = Validate("{\"type\":\"approval.grant\",\"approver\":\"11111111-1111-1111-1111-111111111111\"}");

        Assert.Contains(errors, error => error.Message.Contains("own changes", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("/password:plaintext")]
    [InlineData("Server=db;User Id=user;Password=plaintext")]
    public void RejectsWindowsAndConnectionStringSecretArguments(string argument)
    {
        var action = JsonSerializer.Serialize(new { type = "command.execute", args = new[] { argument } });

        var errors = Validate(action);

        Assert.Contains(errors, error => error.Message.Contains("CLI arguments", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsPlanIdentityReuseForUnrelatedActions()
    {
        using var manifest = JsonDocument.Parse(Manifest);
        using var plan = JsonDocument.Parse(Plan("{\"type\":\"repository.checkout\"}", identities: "{\"development\":\"shared\",\"production\":\"shared\"}"));

        var errors = OperatorPolicyValidator.Validate(manifest.RootElement, plan.RootElement);

        Assert.Contains(errors, error => error.Path == "plan/identities" && error.Message.Contains("must not reuse", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsPlanEnvironmentThatDoesNotMatchManifest()
    {
        using var manifest = JsonDocument.Parse(Manifest);
        using var plan = JsonDocument.Parse(Plan("{\"type\":\"noop\",\"environment\":\"development\"}", environment: "development"));

        var errors = OperatorPolicyValidator.Validate(manifest.RootElement, plan.RootElement);

        Assert.Contains(errors, error => error.Path == "plan/environment");
        Assert.Contains(errors, error => error.Path == "plan/actions/0/environment");
    }

    [Fact]
    public void RejectsActionEnvironmentThatDoesNotMatchManifest()
    {
        var errors = Validate("{\"type\":\"container.deploy\",\"environment\":\"development\"}");

        Assert.Contains(errors, error => error.Path == "plan/actions/0/environment");
    }

    [Fact]
    public void ApproveRejectsPrivilegedPlanWithoutApprovalAndRollbackEvidence()
    {
        var result = RunOperator("approve", "{\"type\":\"azure.rbac.assign\",\"principalId\":\"22222222-2222-2222-2222-222222222222\"}");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("approvalEvidence", result.Error);
        Assert.Contains("rollbackEvidence", result.Error);
        Assert.False(File.Exists(result.OutputPath));
    }

    [Fact]
    public void ApproveAcceptsPrivilegedPlanWithIndependentApprovalAndRollbackEvidence()
    {
        const string evidence = "\"approvalEvidence\":{\"approverId\":\"human-reviewer\",\"approvedAt\":\"2026-08-09T00:00:00Z\",\"evidenceLinks\":[\"https://example.test/approval\"]},\"rollbackEvidence\":{\"procedure\":\"Revert the assignment\",\"evidenceLinks\":[\"https://example.test/rollback\"]},";
        var result = RunOperator("approve", "{\"type\":\"azure.rbac.assign\",\"principalId\":\"22222222-2222-2222-2222-222222222222\"}", evidence);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(result.OutputPath));
    }

    [Fact]
    public void ExportWritesTheExactValidatedPlanBytes()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var manifestPath = Path.Combine(directory, "manifest.json");
        var planPath = Path.Combine(directory, "plan.json");
        var outputPath = Path.Combine(directory, "output.json");
        var planBytes = System.Text.Encoding.UTF8.GetBytes(Plan("{\"type\":\"noop\"}") + "\r\n");
        File.WriteAllText(manifestPath, Manifest);
        File.WriteAllBytes(planPath, planBytes);

        var exitCode = Program.Main(["export", manifestPath, planPath, "--output", outputPath]);

        Assert.Equal(0, exitCode);
        Assert.Equal(planBytes, File.ReadAllBytes(outputPath));
    }

    [Theory]
    [InlineData("{\"source\":\"\",\"correlationId\":\"not-a-uuid\",\"evidenceLinks\":[]}", "0.8")]
    [InlineData("{\"source\":\"test\",\"correlationId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"evidenceLinks\":[\"https://example.test/evidence\"]}", "1.1")]
    [InlineData("{\"source\":\"test\",\"correlationId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"evidenceLinks\":[\"https://example.test/evidence\"]}", "\"high\"")]
    public void SchemaValidationRejectsMalformedProvenanceOrConfidence(string provenance, string confidence)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var manifestPath = Path.Combine(directory, "manifest.json");
        var planPath = Path.Combine(directory, "plan.json");
        File.WriteAllText(manifestPath, Manifest);
        File.WriteAllText(planPath, Plan("{\"type\":\"noop\"}", provenance, confidence));

        var errors = Program.Validate(manifestPath, planPath);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, error => error.Path.Contains("provenance") || error.Path.Contains("confidence"));
        Directory.Delete(directory, true);
    }

    private static IReadOnlyList<PolicyDiagnostic> Validate(string action)
    {
        using var manifest = JsonDocument.Parse(Manifest);
        using var plan = JsonDocument.Parse(Plan(action));
        return OperatorPolicyValidator.Validate(manifest.RootElement, plan.RootElement);
    }

    private static (int ExitCode, string Error, string OutputPath) RunOperator(string command, string action, string extra = "")
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var manifestPath = Path.Combine(directory, "manifest.json");
        var planPath = Path.Combine(directory, "plan.json");
        var outputPath = Path.Combine(directory, "output.json");
        File.WriteAllText(manifestPath, Manifest);
        File.WriteAllText(planPath, Plan(action, extra: extra));
        var previousError = Console.Error;
        using var error = new StringWriter();
        Console.SetError(error);
        try
        {
            return (Program.Main([command, manifestPath, planPath, "--output", outputPath]), error.ToString(), outputPath);
        }
        finally
        {
            Console.SetError(previousError);
        }
    }

    private static string Plan(string action, string? provenance = null, string confidence = "0.8", string? identities = null, string environment = "production", string extra = "") => $$"""
      {
        "schemaVersion":"1.0",
        "environment":"{{environment}}",
        "actor":{"id":"agent-1","githubLogin":"helios-agent","azurePrincipalId":"11111111-1111-1111-1111-111111111111"},
        {{extra}}
        {{(identities is null ? "" : $"\"identities\":{identities},")}}
        "actions":[{{action}}],
        "provenance":{{provenance ?? "{\"source\":\"test\",\"correlationId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"evidenceLinks\":[\"https://example.test/evidence\"]}"}},
        "confidence":{{confidence}}
      }
      """;
}

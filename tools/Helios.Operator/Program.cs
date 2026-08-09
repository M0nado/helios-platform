using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using Json.Schema;

namespace Helios.Operator;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: Helios.Operator <validate|plan|approve|export|manual-command> <manifest.json> [plan.json] [--approval-record <path>] [--output <path>]");
            return 2;
        }

        var command = args[0].ToLowerInvariant();
        var manifest = args[1];
        var plan = args.Length > 2 && !args[2].StartsWith("--", StringComparison.Ordinal) ? args[2] : null;
        var outputIndex = Array.IndexOf(args, "--output");
        var output = outputIndex >= 0 && outputIndex + 1 < args.Length ? args[outputIndex + 1] : null;
        var approvalIndex = Array.IndexOf(args, "--approval-record");
        var approvalRecord = approvalIndex >= 0 && approvalIndex + 1 < args.Length ? args[approvalIndex + 1] : null;

        if (command is "plan" or "approve" or "export" or "manual-command" && plan is null)
        {
            Console.Error.WriteLine("plan: a versioned plan document is required before producing operator output");
            return 2;
        }

        var validation = ValidateAndLoad(manifest, plan, command, approvalRecord);
        var diagnostics = validation.Diagnostics;
        if (diagnostics.Count != 0)
        {
            foreach (var diagnostic in diagnostics)
                Console.Error.WriteLine($"{diagnostic.Path}: {diagnostic.Message}");
            return 1;
        }

        if (command == "validate")
            return 0;
        if (command is not ("plan" or "approve" or "export" or "manual-command"))
        {
            Console.Error.WriteLine($"command: unsupported command '{command}'");
            return 2;
        }

        // All persistent output is deliberately below the complete validation gate.
        var document = plan is null ? validation.Manifest!.Bytes : validation.Plan!.Bytes;
        if (output is null)
            Console.OpenStandardOutput().Write(document);
        else
            AtomicWrite(output, document);
        return 0;
    }

    public static IReadOnlyList<PolicyDiagnostic> Validate(string manifestPath, string? planPath = null)
        => ValidateAndLoad(manifestPath, planPath, "validate", null).Diagnostics;

    private static ValidationResult ValidateAndLoad(string manifestPath, string? planPath, string command, string? approvalRecordPath)
    {
        var diagnostics = new List<PolicyDiagnostic>();
        JsonNodeDocument? manifest = LoadAndValidate(manifestPath, SchemaPath("environment-manifest.schema.json"), "manifest", diagnostics);
        JsonNodeDocument? plan = planPath is null ? null : LoadAndValidate(planPath, SchemaPath("operator-plan.v1.schema.json"), "plan", diagnostics);
        if (manifest is not null && plan is not null)
        {
            diagnostics.AddRange(OperatorPolicyValidator.Validate(manifest.Root, plan.Root));
            if (command == "approve")
            {
                var approvalRecord = approvalRecordPath is null
                    ? null
                    : LoadAndValidate(approvalRecordPath, SchemaPath("approval-record.v1.schema.json"), "approvalRecord", diagnostics);
                diagnostics.AddRange(OperatorPolicyValidator.ValidateApproval(manifest.Root, plan.Root, plan.Bytes, approvalRecord?.Root));
            }
        }
        return new(diagnostics, manifest, plan);
    }

    private static JsonNodeDocument? LoadAndValidate(string path, string schemaPath, string label, List<PolicyDiagnostic> diagnostics)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            RejectDuplicateProperties(bytes);
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement.Clone();
            var schema = JsonSchema.FromText(File.ReadAllText(schemaPath));
            var result = schema.Evaluate(root, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                RequireFormatValidation = true
            });
            if (!result.IsValid)
            {
                foreach (var detail in result.Details.Where(x => x.HasErrors))
                {
                    if (detail.Errors is null)
                        continue;

                    foreach (var error in detail.Errors)
                        diagnostics.Add(new($"{label}{detail.InstanceLocation}", error.Value));
                }
                return null;
            }
            return new(root, bytes);
        }
        catch (JsonException ex) { diagnostics.Add(new(label, $"invalid JSON: {ex.Message}")); }
        catch (IOException ex) { diagnostics.Add(new(label, ex.Message)); }
        catch (JsonSchemaException ex) { diagnostics.Add(new(label, $"schema error: {ex.Message}")); }
        return null;
    }

    private static void RejectDuplicateProperties(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow });
        var objectProperties = new Stack<HashSet<string>>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
                objectProperties.Push(new(StringComparer.Ordinal));
            else if (reader.TokenType == JsonTokenType.EndObject)
                objectProperties.Pop();
            else if (reader.TokenType == JsonTokenType.PropertyName && !objectProperties.Peek().Add(reader.GetString()!))
                throw new JsonException($"duplicate property '{reader.GetString()}' is not allowed");
        }
    }

    private static string SchemaPath(string name)
    {
        var candidates = new[] {
            Path.Combine(AppContext.BaseDirectory, "config", "operator", name),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config", "operator", name)),
            Path.Combine(Directory.GetCurrentDirectory(), "config", "operator", name)
        };
        return candidates.First(File.Exists);
    }

    private static void AtomicWrite(string path, byte[] value)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporary = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        File.WriteAllBytes(temporary, value);
        File.Move(temporary, fullPath, true);
    }

    private sealed record JsonNodeDocument(JsonElement Root, byte[] Bytes);
    private sealed record ValidationResult(IReadOnlyList<PolicyDiagnostic> Diagnostics, JsonNodeDocument? Manifest, JsonNodeDocument? Plan);
}

public sealed record PolicyDiagnostic(string Path, string Message);

public static partial class OperatorPolicyValidator
{
    public static IReadOnlyList<PolicyDiagnostic> Validate(JsonElement manifest, JsonElement plan)
    {
        var errors = new List<PolicyDiagnostic>();
        var actorId = Text(plan, "actor", "id") ?? Text(plan, "agent", "id");
        var actorLogin = Text(plan, "actor", "githubLogin") ?? Text(plan, "agent", "githubLogin");
        var actorPrincipal = Text(plan, "actor", "azurePrincipalId") ?? Text(plan, "agent", "azurePrincipalId");
        var declaredApps = Strings(manifest, "githubApps");
        var declaredSecrets = Strings(manifest, "secretReads");
        var declaredOidcSubjects = Strings(manifest, "oidcSubjects");
        var manifestEnvironment = Text(manifest, "environment");
        var planEnvironment = Text(plan, "environment");

        if (!Equal(planEnvironment, manifestEnvironment))
            Add(errors, "plan/environment", "plan environment must match the governing manifest environment");

        if (ReusesProductionIdentity(plan))
            Add(errors, "plan/identities", "development and production must not reuse an identity");

        if (!plan.TryGetProperty("actions", out var actions)) return errors;
        var index = 0;
        foreach (var action in actions.EnumerateArray())
        {
            var path = $"plan/actions/{index++}";
            var type = Text(action, "type")?.ToLowerInvariant() ?? "";
            var actionEnvironment = Text(action, "environment");
            if (actionEnvironment is not null && !Equal(actionEnvironment, manifestEnvironment))
                Add(errors, path + "/environment", "action environment must match the governing manifest environment");
            var target = Text(action, "target");
            var principalId = Text(action, "principalId");
            var reviewer = Text(action, "reviewer");
            if (type.Contains("rbac") && (Equal(principalId, actorPrincipal, actorId) || Equal(target, actorPrincipal, actorId)))
                Add(errors, path + (Equal(principalId, actorPrincipal, actorId) ? "/principalId" : "/target"), "an agent cannot expand its own RBAC permissions");
            if (type.Contains("reviewer") && (Equal(reviewer, actorLogin, actorId) || Equal(target, actorLogin, actorId)))
                Add(errors, path + (Equal(reviewer, actorLogin, actorId) ? "/reviewer" : "/target"), "an agent cannot add itself as a GitHub environment reviewer");
            if ((type.Contains("branch") || type.Contains("ruleset")) && IsWeakening(action))
                Add(errors, path, "branch protection or rulesets cannot be weakened");
            if (type.Contains("github.app") && !Declared(Text(action, "app") ?? target, declaredApps))
                Add(errors, path + "/app", "GitHub App is not declared by the environment manifest");
            if (type.Contains("secret") && type.Contains("read") && !Declared(Text(action, "secret") ?? target, declaredSecrets))
                Add(errors, path + "/secret", "secret read is not declared by the environment manifest");
            var oidcSubject = Text(action, "subject") ?? Text(action, "oidcSubject");
            if (type.Contains("oidc") || action.TryGetProperty("oidcSubject", out _))
            {
                if (ContainsWildcard(oidcSubject))
                    Add(errors, path + "/subject", "OIDC subjects must not contain wildcards");
                else if (!Declared(oidcSubject, declaredOidcSubjects))
                    Add(errors, path + "/subject", "OIDC subject is not declared by the environment manifest");
            }
            if (action.TryGetProperty("image", out var image) && MutableImage(image.GetString()))
                Add(errors, path + "/image", "container images must use an immutable digest");
            if (action.TryGetProperty("ref", out var reference) && MutableRef(reference.GetString()))
                Add(errors, path + "/ref", "repository refs must be immutable commit SHAs");
            if (action.TryGetProperty("args", out var args) && HasSecretArgument(args))
                Add(errors, path + "/args", "secrets must not be passed in CLI arguments");
            if ((type.Contains("identity") || type.Contains("deploy")) && ReusesProductionIdentity(action))
                Add(errors, path, "development and production must not reuse an identity");
            if (type.Contains("approv") && Equal(Text(action, "approver") ?? target, actorId, actorLogin, actorPrincipal))
                Add(errors, path + "/approver", "agents cannot approve their own changes");
        }
        return errors;
    }

    public static IReadOnlyList<PolicyDiagnostic> ValidateApproval(JsonElement manifest, JsonElement plan, byte[] planBytes, JsonElement? approvalRecord)
    {
        var errors = new List<PolicyDiagnostic>();
        var actorId = Text(plan, "actor", "id");
        var actorLogin = Text(plan, "actor", "githubLogin");
        var actorPrincipal = Text(plan, "actor", "azurePrincipalId");
        var privileged = plan.GetProperty("actions").EnumerateArray().Any(action =>
        {
            var type = Text(action, "type")?.ToLowerInvariant() ?? "";
            var principal = Text(action, "principalId") ?? Text(action, "target");
            var environment = Text(action, "environment") ?? Text(plan, "environment");
            return IsGatedOperation(type, environment) ||
                   (type.Contains("rbac") && !Equal(principal, actorId, actorLogin, actorPrincipal));
        });
        if (!privileged)
            return errors;

        if (!plan.TryGetProperty("approvalEvidence", out var approval))
            Add(errors, "plan/approvalEvidence", "privileged approval requires independent approval evidence");
        else if (Equal(Text(approval, "approverId"), actorId, actorLogin, actorPrincipal))
            Add(errors, "plan/approvalEvidence/approverId", "approval evidence must come from an independent approver");
        if (!plan.TryGetProperty("rollbackEvidence", out _))
            Add(errors, "plan/rollbackEvidence", "privileged approval requires rollback evidence");
        ValidateAuthenticatedApproval(manifest, plan, planBytes, approvalRecord, errors);
        return errors;
    }

    private static bool IsGatedOperation(string type, string? environment)
    {
        if (type.Contains("deploy") && Equal(environment, "production")) return true;
        var normalized = type.Replace('-', '.').Replace('_', '.');
        return (normalized.Contains("disk") && (normalized.Contains("format") || normalized.Contains("partition"))) ||
               normalized.Contains("bitlocker") || normalized.Contains("vhdx.key") ||
               normalized.Contains("wdac") || normalized.Contains("applocker") ||
               (normalized.Contains("firewall") && normalized.Contains("lockdown")) ||
               (normalized.Contains("process") && normalized.Contains("terminat")) ||
               normalized.Contains("entra") || normalized.Contains("rbac") || normalized.Contains("tenant.permission") ||
               normalized.Contains("intune") || normalized.Contains("purview") ||
               (normalized.Contains("secret") && normalized.Contains("rotat"));
    }

    private static void ValidateAuthenticatedApproval(JsonElement manifest, JsonElement plan, byte[] planBytes, JsonElement? record, List<PolicyDiagnostic> errors)
    {
        if (record is null)
        {
            Add(errors, "approvalRecord", "privileged approval requires a separately supplied, signed approval record");
            return;
        }

        var authorityId = Text(record.Value, "authorityId");
        var approverId = Text(record.Value, "approverId");
        var correlationId = Text(record.Value, "correlationId");
        var planSha256 = Text(record.Value, "planSha256");
        var approvedAt = Text(record.Value, "approvedAt");
        var signature = Text(record.Value, "signature");
        var expectedCorrelation = Text(plan, "provenance", "correlationId");
        var expectedHash = Convert.ToHexString(SHA256.HashData(planBytes)).ToLowerInvariant();

        if (!Equal(correlationId, expectedCorrelation))
            Add(errors, "approvalRecord/correlationId", "approval record must be bound to the plan correlation ID");
        if (!string.Equals(planSha256, expectedHash, StringComparison.OrdinalIgnoreCase))
            Add(errors, "approvalRecord/planSha256", "approval record must be bound to the exact plan bytes");
        if (plan.TryGetProperty("approvalEvidence", out var evidence) && !Equal(approverId, Text(evidence, "approverId")))
            Add(errors, "approvalRecord/approverId", "signed approver must match the plan approval evidence");

        var authority = manifest.TryGetProperty("approvalAuthorities", out var authorities)
            ? authorities.EnumerateArray().FirstOrDefault(item => Equal(Text(item, "id"), authorityId))
            : default;
        var publicKey = authority.ValueKind == JsonValueKind.Object ? Text(authority, "publicKeyPem") : null;
        if (publicKey is null)
        {
            Add(errors, "approvalRecord/authorityId", "approval authority is not trusted by the governing manifest");
            return;
        }

        try
        {
            using var verifier = ECDsa.Create();
            verifier.ImportFromPem(publicKey);
            var payload = Encoding.UTF8.GetBytes(string.Join('\n', "1.0", authorityId, approverId, correlationId, planSha256, approvedAt));
            if (!verifier.VerifyData(payload, Convert.FromBase64String(signature!), HashAlgorithmName.SHA256))
                Add(errors, "approvalRecord/signature", "approval record signature is invalid");
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
        {
            Add(errors, "approvalRecord/signature", "approval record signature is invalid");
        }
    }

    private static bool IsWeakening(JsonElement action) =>
        Bool(action, "requiredReviews") == false || Bool(action, "requirePullRequest") == false ||
        Bool(action, "requireStatusChecks") == false || Bool(action, "enforceAdmins") == false ||
        Bool(action, "allowForcePushes") == true || Bool(action, "allowDeletions") == true ||
        (Number(action, "requiredApprovingReviewCount") is double count && count < 1);

    private static bool ReusesProductionIdentity(JsonElement node)
    {
        var dev = Text(node, "developmentIdentity") ?? Text(node, "identities", "development");
        var prod = Text(node, "productionIdentity") ?? Text(node, "identities", "production");
        return !string.IsNullOrWhiteSpace(dev) && string.Equals(dev, prod, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSecretArgument(JsonElement args) => args.ValueKind == JsonValueKind.Array && args.EnumerateArray().Any(a =>
        a.ValueKind == JsonValueKind.String && SecretArgumentRegex().IsMatch(a.GetString()!));
    private static bool MutableImage(string? value) => !string.IsNullOrWhiteSpace(value) && !Regex.IsMatch(value, "@sha256:[0-9a-fA-F]{64}$");
    private static bool MutableRef(string? value) => !string.IsNullOrWhiteSpace(value) && !Regex.IsMatch(value, "^[0-9a-fA-F]{40}$");
    private static bool ContainsWildcard(string? value) => value?.IndexOfAny(['*', '?']) >= 0;
    private static bool Declared(string? value, HashSet<string> declared) => value is not null && declared.Contains(value);
    private static bool Equal(string? value, params string?[] candidates) => value is not null && candidates.Any(x => x is not null && string.Equals(value, x, StringComparison.OrdinalIgnoreCase));
    private static void Add(List<PolicyDiagnostic> errors, string path, string message) => errors.Add(new(path, message));
    private static string? Text(JsonElement node, string property) => node.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string? Text(JsonElement node, string parent, string property) => node.TryGetProperty(parent, out var child) ? Text(child, property) : null;
    private static bool? Bool(JsonElement node, string property) => node.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
    private static double? Number(JsonElement node, string property) => node.TryGetProperty(property, out var value) && value.TryGetDouble(out var number) ? number : null;
    private static HashSet<string> Strings(JsonElement node, string property)
    {
        if (node.TryGetProperty("declared", out var declared)) node = declared;
        return node.TryGetProperty(property, out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new(StringComparer.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"(?i)(?:--?(?:password|passwd|token|secret|api[-_]?key|client[-_]?secret)(?:=|$)|/(?:password|passwd|token|secret|api[-_]?key|client[-_]?secret)(?::|=)|(?:^|;)\s*(?:password|pwd|user\s*id|uid)\s*=|Bearer\s+[A-Za-z0-9._~+/-]+|://[^\s/:]+:[^\s/@]+@)")]
    private static partial Regex SecretArgumentRegex();
}

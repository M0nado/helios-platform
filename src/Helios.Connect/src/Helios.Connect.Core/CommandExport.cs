using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Helios.Connect.Core;

public sealed class ReviewBundleExporter
{
    private static readonly string[] CommandFiles = ["github-cli.md", "github-rest.md", "azure-cli.md", "azure-cloud-shell.md", "terraform.md", "bicep.md", "powershell.md", "bash.md", "slack.md", "linear.md", "sharepoint-graph.md"];

    public string Export(ConnectSession session, string sessionDirectory)
    {
        var root = SafeCombine(sessionDirectory, "review-bundle");
        foreach (var directory in new[] { "plans", "approvals", "commands", "verification", "rollback" })
            Directory.CreateDirectory(Path.Combine(root, directory));
        File.WriteAllText(Path.Combine(root, "README.md"), "# HELIOS reviewed command bundle\n\nCommands are inert text. A human must review and execute them externally.\n");
        WriteJson(root, "identities.sanitized.json", session.Repository);
        WriteJson(root, "scopes.json", Array.Empty<string>());
        WriteJson(root, "answers.json", new { session.Stage, session.Mode });
        foreach (var name in CommandFiles)
        {
            var category = name.Split('.')[0];
            var plans = session.Plans.Where(x => x.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
            File.WriteAllText(Path.Combine(root, "commands", name), $"# {category}\n\n" + string.Join("\n\n", plans.Select(Render)));
        }
        WriteJson(root, "evidence-index.json", session.Evidence);
        var hashes = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(x => Path.GetRelativePath(root, x), x => $"sha256:{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(x))).ToLowerInvariant()}");
        WriteJson(root, "hashes.json", hashes);
        return root;
    }

    private static string SafeCombine(string basePath, string childSegment)
    {
        if (Path.IsPathRooted(childSegment))
            throw new ArgumentException("Path segment must be relative.", nameof(childSegment));

        return Path.Combine(basePath, childSegment);
    }

    private static string Render(CommandPlan plan) => $"## {plan.Description}\n\nRisk: {plan.Risk}; role: {plan.RequiredRole}; plan: {plan.PlanHash}\n\n```text\n{plan.Command}\n```";
    private static void WriteJson(string root, string name, object? value)
    {
        if (Path.IsPathRooted(name))
            throw new ArgumentException("Path must be relative.", nameof(name));

        File.WriteAllText(
            Path.Combine(root, name),
            JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }
}

public sealed class AzureCloudShellHandoff : IExternalCommandHandoff
{
    public Uri Destination { get; } = new("https://portal.azure.com/#cloudshell/");
    public string ExportReviewedCommand(CommandPlan plan) => plan.Command;
}

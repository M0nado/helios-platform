using System.Text;

namespace Helios.Connect.Core;

public sealed class DashboardRenderer
{
    public string Render(ConnectSession session)
    {
        var text = new StringBuilder("# HELIOS Connect — sanitized operator status\n\n");
        text.AppendLine($"Repository: {session.Repository?.Url ?? "unavailable"}");
        text.AppendLine($"Commit: {session.Repository?.Sha ?? "unavailable"}");
        text.AppendLine($"Stage: {session.Stage}");
        text.AppendLine($"Observed: {session.Counts.Observed}; Derived: {session.Counts.Derived}; Suggested: {session.Counts.Suggested}; Unresolved: {session.Counts.Unresolved}");
        text.AppendLine("\n| Kind | Summary | Evidence |\n|---|---|---|");
        foreach (var item in session.Evidence) text.AppendLine($"| {item.Kind} | {Safe(item.Summary)} | {Safe(item.Source)} |");
        text.AppendLine("\nCommands executed by HELIOS: 0  ");
        text.AppendLine("Remote mutations performed by HELIOS: 0");
        return text.ToString();
    }

    private static string Safe(string value) => value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}

public sealed class PostExecutionVerifier(RepositoryDetector detector)
{
    public IReadOnlyList<DriftItem> Verify(ConnectSession approved, string repositoryRoot)
    {
        var actual = detector.Detect(repositoryRoot);
        if (approved.Repository is null) return [new("repository", DriftKind.Unverifiable, "Approved repository identity is unavailable.")];
        return
        [
            new("repository.sha", actual.Sha == approved.Repository.Sha ? DriftKind.Matched : DriftKind.Changed, actual.EvidenceHash),
            new("repository.branch", actual.Branch == approved.Repository.Branch ? DriftKind.Matched : DriftKind.Changed, actual.Branch),
            new("repository.worktree", actual.IsDirty == approved.Repository.IsDirty ? DriftKind.Matched : DriftKind.Changed, actual.IsDirty ? "dirty" : "clean")
        ];
    }
}

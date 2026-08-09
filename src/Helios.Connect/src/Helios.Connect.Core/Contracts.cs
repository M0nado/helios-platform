using System.Collections.ObjectModel;

namespace Helios.Connect.Core;

public enum ConnectMode { Guided, Auto, Operator, Dashboard, Verify }
public enum EvidenceKind { Observed, Derived, Suggested, Unresolved }
public enum DriftKind { Matched, Missing, Unexpected, Changed, Stale, Unverifiable }

public sealed record RepositoryIdentity(string Name, string Url, string Sha, string Branch, bool IsDirty, string Ownership, string EvidenceHash);
public sealed record AzureIdentity(string Cloud, string Tenant, string Subscription, string Principal, string PrincipalType, string Access);
public sealed record EvidenceItem(string Id, EvidenceKind Kind, string Summary, string Source, string CorrelationId, string EvidenceReference);
public sealed record SessionCounts(int Observed, int Derived, int Suggested, int Unresolved);
public sealed record CommandPlan(string Category, string Description, string Command, string RequiredRole, string Risk, string PlanHash);
public sealed record DriftItem(string Target, DriftKind Classification, string Evidence);
public sealed record DiscoveryResult(string Provider, bool Available, IReadOnlyList<EvidenceItem> Evidence, string? UnavailableReason);

public sealed class ConnectSession
{
    public required string Id { get; init; }
    public required ConnectMode Mode { get; init; }
    public ConnectStage Stage { get; set; }
    public RepositoryIdentity? Repository { get; set; }
    public Collection<EvidenceItem> Evidence { get; init; } = [];
    public Collection<CommandPlan> Plans { get; init; } = [];
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int CommandsExecutedByHelios => 0;
    public int RemoteMutationsPerformedByHelios => 0;

    public SessionCounts Counts => new(
        Evidence.Count(x => x.Kind == EvidenceKind.Observed),
        Evidence.Count(x => x.Kind == EvidenceKind.Derived),
        Evidence.Count(x => x.Kind == EvidenceKind.Suggested),
        Evidence.Count(x => x.Kind == EvidenceKind.Unresolved));
}

public interface IReadOnlyDiscoveryAdapter
{
    string Provider { get; }
    Task<IReadOnlyList<EvidenceItem>> DiscoverAsync(IReadOnlySet<string> approvedScopes, CancellationToken cancellationToken);
}

public interface IReadOnlyProcessRunner
{
    Task<string> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}

public interface ICommandPlanGenerator
{
    IReadOnlyList<CommandPlan> Generate(IReadOnlyList<EvidenceItem> inventory);
}

public interface IExternalCommandHandoff
{
    Uri Destination { get; }
    string ExportReviewedCommand(CommandPlan plan);
}

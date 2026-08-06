namespace Helios.Connect.Core;

public enum ConnectStage
{
    Start,
    RepositoryDetected,
    GitHubIdentityDetected,
    GitHubIdentityConfirmed,
    AzureIdentityDetected,
    AzureIdentityConfirmed,
    EnrolledScopesSelected,
    DiscoveryScopeConfirmed,
    InventoryCollected,
    AnswersReviewed,
    PlansGenerated,
    SecurityValidated,
    XCoreEvaluated,
    CategoriesReviewed,
    CommandsExported,
    ExternalExecutionAcknowledged,
    ActualStateVerified,
    DriftReported
}

public sealed class ConnectStateMachine
{
    public ConnectStage Stage { get; private set; } = ConnectStage.Start;

    public void Advance(ConnectStage next)
    {
        if ((int)next != (int)Stage + 1)
            throw new InvalidOperationException($"Invalid transition {Stage} -> {next}.");
        Stage = next;
    }
}

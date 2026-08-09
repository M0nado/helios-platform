using Helios.Connect.Core;

var failures = new List<string>();
Assert(typeof(IReadOnlyDiscoveryAdapter).GetMethods().All(x => !ContainsMutation(x.Name)), "Discovery adapter exposes no mutation methods.");
Assert(typeof(ICommandPlanGenerator).GetProperties().All(x => !x.Name.Contains("Token", StringComparison.OrdinalIgnoreCase)), "Plan generators expose no authentication tokens.");
Assert(typeof(IExternalCommandHandoff).GetMethods().All(x => x.Name is not "Execute" and not "Spawn"), "Command handoff cannot execute or spawn.");
Assert(typeof(ReviewBundleExporter).GetMethods().All(x => x.Name is not "Execute" and not "Spawn"), "Review bundle export cannot execute or spawn.");
var session = new ConnectSession { Id = "connect-test", Mode = ConnectMode.Operator, Stage = ConnectStage.Start };
Assert(session.CommandsExecutedByHelios == 0 && session.RemoteMutationsPerformedByHelios == 0, "Zero-mutation counters are immutable.");
var machine = new ConnectStateMachine();
machine.Advance(ConnectStage.RepositoryDetected);
try { machine.Advance(ConnectStage.AzureIdentityDetected); failures.Add("State machine accepted a skipped stage."); } catch (InvalidOperationException) { }
var fake = new RecordingRunner("{\"login\":\"operator\"}");
var github = new GitHubReadOnlyAdapter(fake);
var discovered = await github.DiscoverAsync(new HashSet<string> { "github:identity" }, CancellationToken.None);
Assert(discovered.Single().Summary == "operator", "GitHub identity is parsed from live structured output.");
Assert(discovered.Single().CorrelationId.StartsWith("urn:uuid:", StringComparison.Ordinal), "Provider evidence carries a request correlation ID.");
Assert(discovered.Single().EvidenceReference == "https://api.github.com/user", "Provider evidence carries a supporting evidence reference.");
Assert(fake.Arguments.Contains("X-GitHub-Api-Version: 2022-11-28"), "GitHub REST adapter sends the pinned API version.");
var unapproved = await github.DiscoverAsync(new HashSet<string>(), CancellationToken.None);
Assert(unapproved.Count == 0 && fake.CallCount == 1, "Unapproved discovery scopes never invoke a provider.");
var fork = RepositoryDetector.ParseGitHubRemote("git@github.com:example/helios-platform.git");
Assert(fork.Name == "example/helios-platform" && fork.Url == "https://github.com/example/helios-platform", "Repository identity is derived from the checkout origin, not the canonical manifest.");

if (failures.Count != 0)
{
    foreach (var failure in failures) Console.Error.WriteLine($"FAIL: {failure}");
    return 1;
}
Console.WriteLine("All HELIOS Connect architectural invariants passed.");
return 0;

void Assert(bool condition, string message) { if (!condition) failures.Add(message); }
static bool ContainsMutation(string name) => new[] { "Create", "Update", "Delete", "Apply", "Deploy", "Assign", "Install", "Approve", "Dispatch", "Execute" }
    .Any(term => name.Contains(term, StringComparison.OrdinalIgnoreCase));

sealed class RecordingRunner(string output) : IReadOnlyProcessRunner
{
    public List<string> Arguments { get; } = [];
    public int CallCount { get; private set; }
    public Task<string> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        CallCount++;
        Arguments.AddRange(arguments);
        return Task.FromResult(output);
    }
}

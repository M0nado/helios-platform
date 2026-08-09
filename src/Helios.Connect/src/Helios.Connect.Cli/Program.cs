using Helios.Connect.Core;

return await ConnectCli.RunAsync(args);

internal static class ConnectCli
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args[0] != "connect") return Usage();
var command = args.ElementAtOrDefault(1)?.ToLowerInvariant() ?? "guided";
        try
        {
            var root = RepositoryDetector.FindRoot(Environment.CurrentDirectory);
            var store = new SessionStore(root);
            return command switch
            {
                "guided" => await StartAsync(ConnectMode.Guided, root, store, false),
                "auto" => await StartAsync(ConnectMode.Auto, root, store, args.Contains("--approve-read-only", StringComparer.Ordinal)),
                "operator" => await OperatorAsync(root, store),
                "dashboard" => Dashboard(store, Value(args, "--session")),
                "verify" => Verify(root, store, Value(args, "--session")),
                "cloudshell" => CloudShell(args.ElementAtOrDefault(2)),
                _ => Usage()
            };
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException)
        {
            Console.Error.WriteLine($"HELIOS Connect: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> StartAsync(ConnectMode mode, string root, SessionStore store, bool approvedReadOnly)
    {
        var repository = new RepositoryDetector().Detect(root);
        var session = NewSession(mode, repository);
        session.Evidence.Add(new("repository", EvidenceKind.Observed, repository.Name, repository.EvidenceHash, session.Id, repository.Url));

        if (approvedReadOnly)
        {
            var scopes = new HashSet<string>(["github:identity", "azure:identity"], StringComparer.Ordinal);
            var runner = new ReadOnlyProcessRunner();
            var orchestrator = new ConnectOrchestrator([new GitHubReadOnlyAdapter(runner), new AzureReadOnlyAdapter(runner)]);
            await orchestrator.DiscoverAsync(session, scopes, CancellationToken.None);
            session.Stage = ConnectStage.InventoryCollected;
        }
        else if (mode == ConnectMode.Auto)
        {
            session.Evidence.Add(new("discovery.approval", EvidenceKind.Unresolved, "Read-only remote discovery not approved.", "Re-run with --approve-read-only.", session.Id, repository.Url));
        }

        var path = store.Save(session);
        PrintRepository(repository);
        PrintCounts(session, path);
        if (mode == ConnectMode.Guided) Console.WriteLine("Next: confirm GitHub identity in a human-controlled UI. No identity was guessed.");
        PrintInvariant(session);
        return 0;
    }

    private static Task<int> OperatorAsync(string root, SessionStore store)
    {
        var repository = new RepositoryDetector().Detect(root);
        var session = NewSession(ConnectMode.Operator, repository);
        session.Stage = ConnectStage.CategoriesReviewed;
        session.Plans.Add(new("github-cli", "Inspect reviewed repository settings", $"gh repo view {repository.Name} --json nameWithOwner,defaultBranchRef", "repository reader", "read-only", Hash(repository.Sha + ":github")));
        session.Plans.Add(new("azure-cli", "Inspect the human-selected Azure subscription", "az account show --output json", "Azure reader", "read-only", Hash(repository.Sha + ":azure")));
        var sessionPath = store.Save(session);
        var bundle = new ReviewBundleExporter().Export(session, store.GetSessionDirectory(session.Id));
        session.Stage = ConnectStage.CommandsExported;
        store.Save(session);
        Console.WriteLine($"Reviewed command bundle: {bundle}");
        Console.WriteLine($"Session: {session.Id} ({sessionPath})");
        Console.WriteLine("Commands are displayed/exported only; execute them yourself in a human-controlled terminal.");
        PrintInvariant(session);
        return Task.FromResult(0);
    }

    private static int Dashboard(SessionStore store, string? id)
    {
        var session = id is null ? store.LoadLatest() : store.Load(id);
        Console.Write(new DashboardRenderer().Render(session));
        return 0;
    }

    private static int Verify(string root, SessionStore store, string? id)
    {
        if (id is null) throw new ArgumentException("verify requires --session <session-id>.");
        var session = store.Load(id);
        var drift = new PostExecutionVerifier(new RepositoryDetector()).Verify(session, root);
        foreach (var item in drift) Console.WriteLine($"{item.Classification,-12} {item.Target}: {item.Evidence}");
        Console.WriteLine("Correction plan: generated only when drift exists; no reconciliation was executed.");
        PrintInvariant(session);
        return 0;
    }

    private static int CloudShell(string? action)
    {
        if (action is not ("azure" or "export" or "verify")) return Usage();
        Console.WriteLine($"Azure Cloud Shell handoff: {action}");
        Console.WriteLine($"Human-controlled destination: {new AzureCloudShellHandoff().Destination}");
        Console.WriteLine("No browser/session data, credentials, or terminal access is captured.");
        Console.WriteLine("Commands executed by HELIOS: 0\nRemote mutations performed by HELIOS: 0");
        return 0;
    }

    private static ConnectSession NewSession(ConnectMode mode, RepositoryIdentity repository) => new()
    {
        Id = $"connect-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..39], Mode = mode, Stage = ConnectStage.RepositoryDetected, Repository = repository
    };
    private static string Hash(string input) => $"sha256:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input))).ToLowerInvariant()}";
    private static string? Value(string[] args, string name) { var index = Array.IndexOf(args, name); return index >= 0 && index + 1 < args.Length ? args[index + 1] : null; }
    private static void PrintRepository(RepositoryIdentity r) { Console.WriteLine($"✓ Repository: {r.Name}\n  SHA: {r.Sha}\n  Branch: {r.Branch}\n  State: {(r.IsDirty ? "dirty" : "clean")}\n  Ownership: {r.Ownership}\n  Evidence: {r.EvidenceHash}"); }
    private static void PrintCounts(ConnectSession s, string path) { Console.WriteLine($"\nObserved: {s.Counts.Observed} ({path}#evidence)\nDerived: {s.Counts.Derived} ({path}#evidence)\nSuggested: {s.Counts.Suggested} ({path}#evidence)\nUnresolved: {s.Counts.Unresolved} ({path}#evidence)"); }
    private static void PrintInvariant(ConnectSession s) { Console.WriteLine($"Commands executed by HELIOS: {s.CommandsExecutedByHelios}\nRemote mutations performed by HELIOS: {s.RemoteMutationsPerformedByHelios}"); }
    private static int Usage() { Console.Error.WriteLine("Usage: helios connect [auto [--approve-read-only]|operator|dashboard [--session ID]|verify --session ID|cloudshell azure|export|verify]"); return 2; }
}

using System.Diagnostics;

namespace HELIOS.Platform;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = LauncherOptions.Parse(args);
            if (options.Command == LauncherCommand.Help)
            {
                PrintHelp();
                return 0;
            }

            var repository = RepositoryLocator.Find(options.Repository);
            var invocation = CommandBuilder.Build(repository, options);

            Console.WriteLine("HELIOS one-button launcher");
            Console.WriteLine($"Repository: {repository}");
            Console.WriteLine($"Action: {options.Command.ToString().ToLowerInvariant()}");

            if (options.DryRun)
            {
                Console.WriteLine($"Dry run: {invocation.DisplayText}");
                return 0;
            }

            using var cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };

            return await ProcessRunner.RunAsync(invocation, cancellation.Token);
        }
        catch (LauncherException exception)
        {
            Console.Error.WriteLine($"HELIOS launcher: {exception.Message}");
            return 2;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("HELIOS launcher: canceled.");
            return 130;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            HELIOS one-button launcher

            Usage:
              HELIOS.Platform [start|dashboard|doctor|status|validate|all] [options]

            Commands:
              start       Bootstrap tools and run the safe changed-project pipeline (default)
              dashboard   Bootstrap, validate, and serve the local dashboard
              doctor      Inspect local readiness and print safe repair guidance
              status      Print the GitHub/Azure/AI/Codex control summary
              validate    Validate GitHub workflow structure
              all         Run the safe read-only reporting pipeline
              help        Show this help

            Options:
              --repo PATH       HELIOS repository root (otherwise auto-detected)
              --profile NAME    Build profile: quick or full (start/dashboard only)
              --max-workers N   Maximum build-graph concurrency (start/dashboard only)
              --dry-run         Print the command without executing it

            The launcher never performs production deployment, branch merging, tenant
            changes, secret rotation, or other privileged actions. Those remain behind
            repository and Azure approval gates.
            """);
    }
}

public enum LauncherCommand
{
    Start,
    Dashboard,
    Doctor,
    Status,
    Validate,
    All,
    Help
}

public sealed record LauncherOptions(
    LauncherCommand Command,
    string? Repository,
    string Profile,
    int MaxWorkers,
    bool DryRun)
{
    public static LauncherOptions Parse(IReadOnlyList<string> args)
    {
        var command = LauncherCommand.Start;
        string? repository = null;
        var profile = "quick";
        var maxWorkers = Math.Max(1, Math.Min(4, Environment.ProcessorCount));
        var dryRun = false;
        var index = 0;

        if (args.Count > 0 && !args[0].StartsWith('-'))
        {
            command = ParseCommand(args[0]);
            index++;
        }

        while (index < args.Count)
        {
            switch (args[index])
            {
                case "--repo":
                    repository = RequireValue(args, ref index, "--repo");
                    break;
                case "--profile":
                    profile = RequireValue(args, ref index, "--profile");
                    if (profile is not ("quick" or "full"))
                        throw new LauncherException("--profile must be 'quick' or 'full'.");
                    break;
                case "--max-workers":
                    var value = RequireValue(args, ref index, "--max-workers");
                    if (!int.TryParse(value, out maxWorkers) || maxWorkers is < 1 or > 64)
                        throw new LauncherException("--max-workers must be an integer from 1 to 64.");
                    break;
                case "--dry-run":
                    dryRun = true;
                    index++;
                    break;
                case "--help" or "-h":
                    command = LauncherCommand.Help;
                    index++;
                    break;
                default:
                    throw new LauncherException($"Unknown option '{args[index]}'. Use --help for usage.");
            }
        }

        return new(command, repository, profile, maxWorkers, dryRun);
    }

    private static LauncherCommand ParseCommand(string value) => value.ToLowerInvariant() switch
    {
        "start" => LauncherCommand.Start,
        "dashboard" => LauncherCommand.Dashboard,
        "doctor" => LauncherCommand.Doctor,
        "status" => LauncherCommand.Status,
        "validate" => LauncherCommand.Validate,
        "all" => LauncherCommand.All,
        "help" => LauncherCommand.Help,
        _ => throw new LauncherException($"Unknown command '{value}'. Use --help for usage.")
    };

    private static string RequireValue(IReadOnlyList<string> args, ref int index, string option)
    {
        if (++index >= args.Count)
            throw new LauncherException($"{option} requires a value.");
        return args[index++];
    }
}

public static class RepositoryLocator
{
    public static string Find(string? requestedPath)
    {
        if (!string.IsNullOrWhiteSpace(requestedPath))
            return Validate(Path.GetFullPath(requestedPath));

        foreach (var startingPoint in StartingPoints())
        {
            var directory = new DirectoryInfo(startingPoint);
            while (directory is not null)
            {
                if (IsRepository(directory.FullName))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }

        throw new LauncherException(
            "Could not locate the HELIOS repository. Run inside the checkout or pass --repo PATH.");
    }

    private static IEnumerable<string> StartingPoints()
    {
        yield return Environment.CurrentDirectory;
        var executableDirectory = AppContext.BaseDirectory;
        if (!string.Equals(executableDirectory, Environment.CurrentDirectory, StringComparison.Ordinal))
            yield return executableDirectory;

        // Portable packages place the immutable repository snapshot beside the
        // platform-specific executable directory.
        var portableRepository = Path.GetFullPath(Path.Combine(executableDirectory, "..", "repository"));
        if (Directory.Exists(portableRepository))
            yield return portableRepository;
    }

    private static string Validate(string path)
    {
        if (!Directory.Exists(path))
            throw new LauncherException($"Repository path does not exist: {path}");
        if (!IsRepository(path))
            throw new LauncherException($"Not a HELIOS repository root: {path}");
        return path;
    }

    private static bool IsRepository(string path) =>
        File.Exists(Path.Combine(path, "helios.sh")) &&
        File.Exists(Path.Combine(path, "config", "integrations", "event-contract.schema.json"));
}

public sealed record CommandInvocation(string FileName, IReadOnlyList<string> Arguments, string WorkingDirectory)
{
    public string DisplayText => string.Join(' ', new[] { FileName }.Concat(Arguments.Select(Quote)));

    private static string Quote(string value) =>
        value.All(character => char.IsLetterOrDigit(character) || "-._/:".Contains(character))
            ? value
            : $"\"{value.Replace("\"", "\\\"")}\"";
}

public static class CommandBuilder
{
    public static CommandInvocation Build(string repository, LauncherOptions options, bool? isWindows = null)
    {
        if (isWindows ?? OperatingSystem.IsWindows())
            return BuildWindows(repository, options);

        var arguments = new List<string> { Path.Combine(repository, "helios.sh") };
        switch (options.Command)
        {
            case LauncherCommand.Start:
                arguments.AddRange(["setup", "--profile", options.Profile, "--changed-only", "--max-workers", options.MaxWorkers.ToString()]);
                break;
            case LauncherCommand.Dashboard:
                arguments.AddRange(["dashboard", "--profile", options.Profile, "--changed-only", "--max-workers", options.MaxWorkers.ToString()]);
                break;
            case LauncherCommand.Doctor:
                arguments.Add("doctor");
                break;
            case LauncherCommand.Status:
                arguments.Add("status");
                break;
            case LauncherCommand.Validate:
                arguments.Add("validate");
                break;
            case LauncherCommand.All:
                arguments.Add("all");
                break;
            default:
                throw new LauncherException("No executable command was selected.");
        }

        return new("bash", arguments, repository);
    }

    private static CommandInvocation BuildWindows(string repository, LauncherOptions options)
    {
        var script = Path.Combine(repository, "scripts", "setup", "helios-dev.ps1");
        var arguments = new List<string>
        {
            "-NoLogo", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script
        };

        switch (options.Command)
        {
            case LauncherCommand.Start:
                arguments.AddRange(["-Profile", options.Profile, "-ChangedOnly", "-MaxWorkers", options.MaxWorkers.ToString()]);
                break;
            case LauncherCommand.Dashboard:
                arguments.AddRange(["-Profile", options.Profile, "-ChangedOnly", "-MaxWorkers", options.MaxWorkers.ToString(), "-Serve"]);
                break;
            case LauncherCommand.Doctor:
                arguments.Add("-Doctor");
                break;
            case LauncherCommand.Status:
                arguments.Add("-Status");
                break;
            case LauncherCommand.Validate:
                arguments.Add("-Validate");
                break;
            case LauncherCommand.All:
                arguments.Add("-AllReports");
                break;
            default:
                throw new LauncherException("No executable command was selected.");
        }

        return new("powershell.exe", arguments, repository);
    }
}

public static class ProcessRunner
{
    public static async Task<int> RunAsync(CommandInvocation invocation, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = invocation.FileName,
            WorkingDirectory = invocation.WorkingDirectory,
            UseShellExecute = false
        };
        foreach (var argument in invocation.Arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new LauncherException($"Could not start {invocation.FileName}.");
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new LauncherException($"Could not start '{invocation.FileName}'. Verify the required shell is installed and retry.", exception);
        }

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            throw;
        }
    }
}

public sealed class LauncherException : Exception
{
    public LauncherException(string message) : base(message) { }
    public LauncherException(string message, Exception innerException) : base(message, innerException) { }
}

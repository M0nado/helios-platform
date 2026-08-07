using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Helios.Connect.Core;

public sealed class RepositoryDetector
{
    public RepositoryIdentity Detect(string startDirectory)
    {
        var root = FindRoot(startDirectory);
        var git = Path.Combine(root, ".git");
        if (!Directory.Exists(git)) throw new InvalidOperationException("Git worktrees are not supported by sanitized discovery.");

        var head = File.ReadAllText(Path.Combine(git, "HEAD")).Trim();
        var branch = head.StartsWith("ref: ", StringComparison.Ordinal) ? head[5..].Replace("refs/heads/", "", StringComparison.Ordinal) : "detached";
        var sha = ResolveSha(git, head);
        var manifestPath = Path.Combine(root, "config", "integrations", "repositories.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var canonical = document.RootElement.GetProperty("canonicalPlatform").GetString()
            ?? throw new InvalidDataException("canonicalPlatform is missing.");
        if (!string.Equals(canonical, "M0nado/helios-platform", StringComparison.Ordinal))
            throw new InvalidDataException($"Unexpected canonical repository: {canonical}.");

        var context = $"{canonical}\n{sha}\n{branch}\ncanonical\n1.0";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(context))).ToLowerInvariant();
        return new(canonical, $"https://github.com/{canonical}", sha, branch, IsDirty(root), "canonical", $"sha256:{hash}");
    }

    public static string FindRoot(string path)
    {
        for (var directory = new DirectoryInfo(path); directory is not null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, ".git"))) return directory.FullName;
        throw new InvalidOperationException("Repository not detected.");
    }

    private static string ResolveSha(string git, string head)
    {
        if (!head.StartsWith("ref: ", StringComparison.Ordinal)) return RequireSha(head);
        var reference = head[5..];
        var relativeReference = reference.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(relativeReference))
            throw new InvalidDataException("Git reference must be a relative path.");
        var loose = Path.Combine(git, relativeReference);
        if (File.Exists(loose)) return RequireSha(File.ReadAllText(loose).Trim());
        var match = File.ReadLines(Path.Combine(git, "packed-refs"))
            .FirstOrDefault(x => x.EndsWith($" {reference}", StringComparison.Ordinal));
        return RequireSha(match?.Split(' ')[0] ?? "");
    }

    private static string RequireSha(string value) => value.Length == 40 && value.All(Uri.IsHexDigit)
        ? value.ToLowerInvariant() : throw new InvalidDataException("An exact 40-character commit SHA is required.");

    private static bool IsDirty(string root)
    {
        using var process = Process.Start(new ProcessStartInfo("git", "status --porcelain=v1 --untracked-files=normal")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Git read-only discovery is unavailable.");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException("Git read-only discovery failed.");
        return output.Length != 0;
    }
}

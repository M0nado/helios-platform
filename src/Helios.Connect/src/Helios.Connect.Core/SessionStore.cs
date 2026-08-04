using System.Text.Json;

namespace Helios.Connect.Core;

public sealed class SessionStore(string repositoryRoot)
{
    private readonly string _basePath = Path.Combine(repositoryRoot, ".helios", "connect");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string Save(ConnectSession session)
    {
        ValidateId(session.Id);
        var directory = Path.Combine(_basePath, session.Id);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "session.sanitized.json");
        File.WriteAllText(path, JsonSerializer.Serialize(session, JsonOptions));
        return path;
    }

    public ConnectSession Load(string id)
    {
        ValidateId(id);
        return JsonSerializer.Deserialize<ConnectSession>(File.ReadAllText(Path.Combine(_basePath, id, "session.sanitized.json")), JsonOptions)
            ?? throw new InvalidDataException("Invalid session.");
    }

    public ConnectSession LoadLatest()
    {
        if (!Directory.Exists(_basePath)) throw new DirectoryNotFoundException("No HELIOS Connect sessions are available.");
        var latest = Directory.EnumerateDirectories(_basePath).OrderByDescending(Directory.GetLastWriteTimeUtc).FirstOrDefault()
            ?? throw new DirectoryNotFoundException("No HELIOS Connect sessions are available.");
        return Load(Path.GetFileName(latest));
    }

    public string GetSessionDirectory(string id)
    {
        ValidateId(id);
        return Path.Combine(_basePath, id);
    }

    private static void ValidateId(string id)
    {
        if (id.Length is < 8 or > 64 || id.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
            throw new ArgumentException("Invalid session identifier.", nameof(id));
    }
}

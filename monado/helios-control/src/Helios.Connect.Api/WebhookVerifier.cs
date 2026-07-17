using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Helios.Connect.Api;

public static class WebhookVerifier
{
    public static bool Verify(string provider, IHeaderDictionary headers, ReadOnlySpan<byte> body)
    {
        return provider.ToLowerInvariant() switch
        {
            "github" => VerifyHexHmac(headers["X-Hub-Signature-256"].FirstOrDefault(), "sha256=", body, "GITHUB_WEBHOOK_SECRET"),
            "linear" => VerifyHexHmac(headers["Linear-Signature"].FirstOrDefault(), "", body, "LINEAR_WEBHOOK_SECRET"),
            "slack" => VerifySlack(headers, body),
            // Microsoft webhooks require validated Entra JWT middleware and provider-specific
            // validation challenges. Fail closed until that authentication is configured.
            _ => false
        };
    }

    private static bool VerifySlack(IHeaderDictionary headers, ReadOnlySpan<byte> body)
    {
        var timestamp = headers["X-Slack-Request-Timestamp"].FirstOrDefault();
        var signature = headers["X-Slack-Signature"].FirstOrDefault();
        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)) return false;
        DateTimeOffset requestTime;
        try { requestTime = DateTimeOffset.FromUnixTimeSeconds(seconds); }
        catch (ArgumentOutOfRangeException) { return false; }
        if ((DateTimeOffset.UtcNow - requestTime).Duration() > TimeSpan.FromMinutes(5)) return false;

        var secret = Environment.GetEnvironmentVariable("SLACK_SIGNING_SECRET");
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signature)) return false;
        using var hmac = IncrementalHash.CreateHMAC(
            HashAlgorithmName.SHA256,
            Encoding.UTF8.GetBytes(secret));
        hmac.AppendData(Encoding.UTF8.GetBytes($"v0:{timestamp}:"));
        hmac.AppendData(body);
        return IsExpectedSignature(signature, "v0=", hmac.GetHashAndReset());
    }

    private static bool VerifyHexHmac(string? supplied, string prefix, ReadOnlySpan<byte> body, string secretName)
    {
        var secret = Environment.GetEnvironmentVariable(secretName);
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(supplied) || !supplied.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        return IsExpectedSignature(supplied, prefix, hash);
    }

    private static bool IsExpectedSignature(string supplied, string prefix, ReadOnlySpan<byte> hash)
    {
        if (!supplied.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var expected = prefix + Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(supplied));
    }
}

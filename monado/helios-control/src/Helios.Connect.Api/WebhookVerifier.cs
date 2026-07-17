using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Helios.Connect.Api;

public static class WebhookVerifier
{
    public static bool Verify(string provider, IHeaderDictionary headers, string body)
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

    private static bool VerifySlack(IHeaderDictionary headers, string body)
    {
        var timestamp = headers["X-Slack-Request-Timestamp"].FirstOrDefault();
        var signature = headers["X-Slack-Signature"].FirstOrDefault();
        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)) return false;
        var requestTime = DateTimeOffset.FromUnixTimeSeconds(seconds);
        if ((DateTimeOffset.UtcNow - requestTime).Duration() > TimeSpan.FromMinutes(5)) return false;
        return VerifyHexHmac(signature, "v0=", $"v0:{timestamp}:{body}", "SLACK_SIGNING_SECRET");
    }

    private static bool VerifyHexHmac(string? supplied, string prefix, string body, string secretName)
    {
        var secret = Environment.GetEnvironmentVariable(secretName);
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(supplied) || !supplied.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var expected = prefix + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(supplied));
    }
}

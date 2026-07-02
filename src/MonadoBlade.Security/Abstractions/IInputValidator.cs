namespace MonadoBlade.Security.Abstractions;

/// <summary>
/// Interface for secure input validation and sanitization.
/// </summary>
public interface IInputValidator
{
    /// <summary>
    /// Validates and sanitizes user input against various attack vectors.
    /// </summary>
    /// <param name="input">The input string to validate.</param>
    /// <param name="inputType">The type of input (url, html, sqlParam, etc.).</param>
    /// <returns>The sanitized input string.</returns>
    Task<string> ValidateAndSanitizeAsync(string input, string inputType = "text");

    /// <summary>
    /// Checks if a request has exceeded rate limits.
    /// </summary>
    /// <param name="identifier">The unique identifier for rate limiting (IP, User ID, etc.).</param>
    /// <param name="maxRequests">Maximum requests allowed in the time window.</param>
    /// <param name="windowSeconds">Time window in seconds.</param>
    /// <returns>True if request is allowed, false if rate limit exceeded.</returns>
    Task<bool> CheckRateLimitAsync(string identifier, int maxRequests = 100, int windowSeconds = 60);

    /// <summary>
    /// Validates a CSRF token.
    /// </summary>
    /// <param name="token">The CSRF token to validate.</param>
    /// <param name="sessionId">The session ID associated with the token.</param>
    /// <returns>True if token is valid, false otherwise.</returns>
    Task<bool> ValidateCsrfTokenAsync(string token, string sessionId);

    /// <summary>
    /// Generates a new CSRF token for a session.
    /// </summary>
    /// <param name="sessionId">The session ID to generate a token for.</param>
    /// <returns>A new CSRF token.</returns>
    Task<string> GenerateCsrfTokenAsync(string sessionId);

    /// <summary>
    /// Validates a file path to prevent directory traversal attacks.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <param name="basePath">The allowed base directory.</param>
    /// <returns>True if path is valid and within basePath, false otherwise.</returns>
    Task<bool> ValidateFilePathAsync(string path, string basePath);

    /// <summary>
    /// Encodes user input to prevent XSS attacks.
    /// </summary>
    /// <param name="input">The input to encode.</param>
    /// <returns>The XSS-safe encoded string.</returns>
    string EncodeForXss(string input);

    /// <summary>
    /// Encodes input for safe SQL usage (though parameterized queries are preferred).
    /// </summary>
    /// <param name="input">The input to encode.</param>
    /// <returns>The SQL-safe encoded string.</returns>
    string EncodeForSql(string input);
}

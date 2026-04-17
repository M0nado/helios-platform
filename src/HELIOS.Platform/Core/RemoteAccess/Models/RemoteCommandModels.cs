namespace HELIOS.Platform.Core.RemoteAccess.Models;

/// <summary>
/// Represents a remote command execution request.
/// </summary>
public class RemoteCommandRequest
{
    /// <summary>Gets or sets the unique request ID.</summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the connection ID.</summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the command to execute.</summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>Gets or sets the working directory for command execution.</summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>Gets or sets the command timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>Gets or sets whether to capture output.</summary>
    public bool CaptureOutput { get; set; } = true;

    /// <summary>Gets or sets whether to stream output in real-time.</summary>
    public bool StreamOutput { get; set; } = false;

    /// <summary>Gets or sets custom environment variables.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>Gets or sets the request creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the result of a remote command execution.
/// </summary>
public class RemoteCommandResult
{
    /// <summary>Gets or sets the request ID associated with this result.</summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Gets or sets the exit code.</summary>
    public int ExitCode { get; set; }

    /// <summary>Gets or sets the standard output.</summary>
    public string StandardOutput { get; set; } = string.Empty;

    /// <summary>Gets or sets the standard error.</summary>
    public string StandardError { get; set; } = string.Empty;

    /// <summary>Gets or sets the execution start time.</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Gets or sets the execution end time.</summary>
    public DateTime EndTime { get; set; }

    /// <summary>Gets or sets the execution duration in milliseconds.</summary>
    public long DurationMilliseconds => (long)(EndTime - StartTime).TotalMilliseconds;

    /// <summary>Gets or sets the execution status.</summary>
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;

    /// <summary>Gets or sets error information if execution failed.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Enumeration of command execution statuses.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>Execution pending</summary>
    Pending,

    /// <summary>Execution in progress</summary>
    Running,

    /// <summary>Execution completed successfully</summary>
    Completed,

    /// <summary>Execution failed</summary>
    Failed,

    /// <summary>Execution timed out</summary>
    TimedOut,

    /// <summary>Execution cancelled</summary>
    Cancelled
}

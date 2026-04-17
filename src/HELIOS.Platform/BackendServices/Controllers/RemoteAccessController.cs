using Microsoft.AspNetCore.Mvc;
using HELIOS.Platform.Core.RemoteAccess.Services;

namespace HELIOS.Platform.BackendServices.Controllers;

/// <summary>
/// REST API controller for remote access and management operations.
/// </summary>
[ApiController]
[Route("api/remote")]
[Produces("application/json")]
public class RemoteAccessController : ControllerBase
{
    private readonly IRemoteConnectionManager _connectionManager;
    private readonly IRemoteExecutor _executor;
    private readonly IRemoteMonitor _monitor;
    private readonly IRemoteFileTransferService _fileTransferService;
    private readonly IRemoteSessionManager _sessionManager;
    private readonly ILogger<RemoteAccessController> _logger;

    public RemoteAccessController(
        IRemoteConnectionManager connectionManager,
        IRemoteExecutor executor,
        IRemoteMonitor monitor,
        IRemoteFileTransferService fileTransferService,
        IRemoteSessionManager sessionManager,
        ILogger<RemoteAccessController> logger)
    {
        _connectionManager = connectionManager;
        _executor = executor;
        _monitor = monitor;
        _fileTransferService = fileTransferService;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    #region Connection Management Endpoints

    /// <summary>
    /// Creates a new remote connection.
    /// </summary>
    /// <param name="connectionInfo">Connection information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection ID.</returns>
    [HttpPost("connections/create")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> CreateConnection([FromBody] RemoteConnectionInfo connectionInfo, CancellationToken cancellationToken)
    {
        try
        {
            var connectionId = await _connectionManager.CreateConnectionAsync(connectionInfo, cancellationToken);
            return Ok(new ApiResponse<string> { Data = connectionId, Success = true, Message = "Connection created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating connection");
            return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Establishes a connection.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection status.</returns>
    [HttpPost("connections/{connectionId}/connect")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> Connect(string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _connectionManager.ConnectAsync(connectionId, cancellationToken);
            return Ok(new ApiResponse<bool> { Data = result, Success = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Disconnects a connection.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Disconnection result.</returns>
    [HttpPost("connections/{connectionId}/disconnect")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> Disconnect(string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _connectionManager.DisconnectAsync(connectionId, cancellationToken);
            return Ok(new ApiResponse<bool> { Data = result, Success = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets connection information.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <returns>Connection details.</returns>
    [HttpGet("connections/{connectionId}")]
    [ProducesResponseType(typeof(ApiResponse<RemoteConnectionInfo>), 200)]
    public async Task<IActionResult> GetConnection(string connectionId)
    {
        var connection = await _connectionManager.GetConnectionAsync(connectionId);
        if (connection == null)
            return NotFound(new ApiResponse<RemoteConnectionInfo> { Success = false, Message = "Connection not found" });

        return Ok(new ApiResponse<RemoteConnectionInfo> { Data = connection, Success = true });
    }

    /// <summary>
    /// Lists all connections.
    /// </summary>
    /// <returns>List of connections.</returns>
    [HttpGet("connections")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RemoteConnectionInfo>>), 200)]
    public async Task<IActionResult> ListConnections()
    {
        var connections = await _connectionManager.ListConnectionsAsync();
        return Ok(new ApiResponse<IEnumerable<RemoteConnectionInfo>> { Data = connections, Success = true });
    }

    /// <summary>
    /// Gets connection status.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <returns>Connection state.</returns>
    [HttpGet("connections/{connectionId}/status")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> GetConnectionStatus(string connectionId)
    {
        var status = await _connectionManager.GetConnectionStatusAsync(connectionId);
        return Ok(new ApiResponse<string> { Data = status.ToString(), Success = true });
    }

    /// <summary>
    /// Removes a connection.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <returns>Removal result.</returns>
    [HttpDelete("connections/{connectionId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> RemoveConnection(string connectionId)
    {
        var result = await _connectionManager.RemoveConnectionAsync(connectionId);
        return Ok(new ApiResponse<bool> { Data = result, Success = result });
    }

    #endregion

    #region Command Execution Endpoints

    /// <summary>
    /// Executes a remote command.
    /// </summary>
    /// <param name="request">Command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command execution result.</returns>
    [HttpPost("commands/execute")]
    [ProducesResponseType(typeof(ApiResponse<RemoteCommandResult>), 200)]
    public async Task<IActionResult> ExecuteCommand([FromBody] RemoteCommandRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _executor.ExecuteCommandAsync(request, cancellationToken);
            return Ok(new ApiResponse<RemoteCommandResult> { Data = result, Success = result.Status == ExecutionStatus.Completed });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<RemoteCommandResult> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels a running command.
    /// </summary>
    /// <param name="requestId">Request ID.</param>
    /// <returns>Cancellation result.</returns>
    [HttpPost("commands/{requestId}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> CancelCommand(string requestId)
    {
        var result = await _executor.CancelCommandAsync(requestId);
        return Ok(new ApiResponse<bool> { Data = result, Success = result });
    }

    /// <summary>
    /// Gets command execution status.
    /// </summary>
    /// <param name="requestId">Request ID.</param>
    /// <returns>Execution status.</returns>
    [HttpGet("commands/{requestId}/status")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> GetCommandStatus(string requestId)
    {
        var status = await _executor.GetCommandStatusAsync(requestId);
        return Ok(new ApiResponse<string> { Data = status.ToString(), Success = true });
    }

    /// <summary>
    /// Gets execution history for a connection.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="maxResults">Maximum results.</param>
    /// <returns>Execution history.</returns>
    [HttpGet("commands/{connectionId}/history")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RemoteCommandResult>>), 200)]
    public async Task<IActionResult> GetExecutionHistory(string connectionId, [FromQuery] int maxResults = 100)
    {
        var history = await _executor.GetExecutionHistoryAsync(connectionId, maxResults);
        return Ok(new ApiResponse<IEnumerable<RemoteCommandResult>> { Data = history, Success = true });
    }

    #endregion

    #region Monitoring and Diagnostics Endpoints

    /// <summary>
    /// Collects system diagnostics.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Diagnostic data.</returns>
    [HttpGet("diagnostics/{connectionId}")]
    [ProducesResponseType(typeof(ApiResponse<RemoteSystemDiagnostics>), 200)]
    public async Task<IActionResult> CollectDiagnostics(string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            var diagnostics = await _monitor.CollectDiagnosticsAsync(connectionId, cancellationToken);
            return Ok(new ApiResponse<RemoteSystemDiagnostics> { Data = diagnostics, Success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<RemoteSystemDiagnostics> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Starts monitoring a connection.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="intervalSeconds">Monitoring interval in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Monitoring session ID.</returns>
    [HttpPost("monitoring/{connectionId}/start")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> StartMonitoring(string connectionId, [FromQuery] int intervalSeconds = 60, CancellationToken cancellationToken)
    {
        try
        {
            var sessionId = await _monitor.StartMonitoringAsync(connectionId, intervalSeconds, cancellationToken);
            return Ok(new ApiResponse<string> { Data = sessionId, Success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Stops monitoring a connection.
    /// </summary>
    /// <param name="monitoringSessionId">Monitoring session ID.</param>
    /// <returns>Result.</returns>
    [HttpPost("monitoring/{monitoringSessionId}/stop")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> StopMonitoring(string monitoringSessionId)
    {
        var result = await _monitor.StopMonitoringAsync(monitoringSessionId);
        return Ok(new ApiResponse<bool> { Data = result, Success = result });
    }

    /// <summary>
    /// Gets health status.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <returns>Health status.</returns>
    [HttpGet("health/{connectionId}")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> GetHealthStatus(string connectionId)
    {
        var status = await _monitor.GetHealthStatusAsync(connectionId);
        return Ok(new ApiResponse<string> { Data = status.ToString(), Success = true });
    }

    /// <summary>
    /// Generates a diagnostic report.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <returns>Diagnostic report.</returns>
    [HttpGet("report/{connectionId}")]
    [ProducesResponseType(typeof(ApiResponse<DiagnosticReport>), 200)]
    public async Task<IActionResult> GenerateDiagnosticReport(string connectionId)
    {
        try
        {
            var report = await _monitor.GenerateDiagnosticReportAsync(connectionId);
            return Ok(new ApiResponse<DiagnosticReport> { Data = report, Success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<DiagnosticReport> { Success = false, Message = ex.Message });
        }
    }

    #endregion

    #region File Transfer Endpoints

    /// <summary>
    /// Uploads a file to remote host.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="localPath">Local file path.</param>
    /// <param name="remotePath">Remote file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transfer operation.</returns>
    [HttpPost("files/{connectionId}/upload")]
    [ProducesResponseType(typeof(ApiResponse<FileTransferOperation>), 200)]
    public async Task<IActionResult> UploadFile(string connectionId, [FromQuery] string localPath, [FromQuery] string remotePath, CancellationToken cancellationToken)
    {
        try
        {
            var operation = await _fileTransferService.UploadFileAsync(connectionId, localPath, remotePath, cancellationToken);
            return Ok(new ApiResponse<FileTransferOperation> { Data = operation, Success = operation.Status == TransferStatus.Completed });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<FileTransferOperation> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Downloads a file from remote host.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="remotePath">Remote file path.</param>
    /// <param name="localPath">Local file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transfer operation.</returns>
    [HttpPost("files/{connectionId}/download")]
    [ProducesResponseType(typeof(ApiResponse<FileTransferOperation>), 200)]
    public async Task<IActionResult> DownloadFile(string connectionId, [FromQuery] string remotePath, [FromQuery] string localPath, CancellationToken cancellationToken)
    {
        try
        {
            var operation = await _fileTransferService.DownloadFileAsync(connectionId, remotePath, localPath, cancellationToken);
            return Ok(new ApiResponse<FileTransferOperation> { Data = operation, Success = operation.Status == TransferStatus.Completed });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<FileTransferOperation> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Lists files in remote directory.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="remotePath">Remote directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of files.</returns>
    [HttpGet("files/{connectionId}/list")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RemoteFileSystemObject>>), 200)]
    public async Task<IActionResult> ListFiles(string connectionId, [FromQuery] string remotePath, CancellationToken cancellationToken)
    {
        try
        {
            var files = await _fileTransferService.ListFilesAsync(connectionId, remotePath, cancellationToken);
            return Ok(new ApiResponse<IEnumerable<RemoteFileSystemObject>> { Data = files, Success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<IEnumerable<RemoteFileSystemObject>> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a remote file.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="remotePath">Remote file path.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("files/{connectionId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> DeleteFile(string connectionId, [FromQuery] string remotePath)
    {
        try
        {
            var result = await _fileTransferService.DeleteFileAsync(connectionId, remotePath);
            return Ok(new ApiResponse<bool> { Data = result, Success = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    #endregion

    #region Session Management Endpoints

    /// <summary>
    /// Creates a new session.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="username">Username.</param>
    /// <param name="authMethod">Authentication method.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session ID.</returns>
    [HttpPost("sessions/create")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> CreateSession([FromQuery] string userId, [FromQuery] string username, [FromQuery] AuthenticationMethod authMethod, CancellationToken cancellationToken)
    {
        try
        {
            var sessionId = await _sessionManager.CreateSessionAsync(userId, username, authMethod, cancellationToken);
            return Ok(new ApiResponse<string> { Data = sessionId, Success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets session information.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Session details.</returns>
    [HttpGet("sessions/{sessionId}")]
    [ProducesResponseType(typeof(ApiResponse<RemoteSession>), 200)]
    public async Task<IActionResult> GetSession(string sessionId)
    {
        var session = await _sessionManager.GetSessionAsync(sessionId);
        if (session == null)
            return NotFound(new ApiResponse<RemoteSession> { Success = false, Message = "Session not found" });

        return Ok(new ApiResponse<RemoteSession> { Data = session, Success = true });
    }

    /// <summary>
    /// Lists active sessions.
    /// </summary>
    /// <returns>List of sessions.</returns>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RemoteSession>>), 200)]
    public async Task<IActionResult> ListActiveSessions()
    {
        var sessions = await _sessionManager.ListActiveSessionsAsync();
        return Ok(new ApiResponse<IEnumerable<RemoteSession>> { Data = sessions, Success = true });
    }

    /// <summary>
    /// Terminates a session.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Termination result.</returns>
    [HttpPost("sessions/{sessionId}/terminate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> TerminateSession(string sessionId)
    {
        var result = await _sessionManager.TerminateSessionAsync(sessionId);
        return Ok(new ApiResponse<bool> { Data = result, Success = result });
    }

    /// <summary>
    /// Gets session activity history.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Activity logs.</returns>
    [HttpGet("sessions/{sessionId}/activity")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SessionActivityLog>>), 200)]
    public async Task<IActionResult> GetActivityHistory(string sessionId)
    {
        var activities = await _sessionManager.GetActivityHistoryAsync(sessionId);
        return Ok(new ApiResponse<IEnumerable<SessionActivityLog>> { Data = activities, Success = true });
    }

    #endregion
}

/// <summary>
/// Standard API response wrapper.
/// </summary>
/// <typeparam name="T">Response data type.</typeparam>
public class ApiResponse<T>
{
    /// <summary>Gets or sets whether the request was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the response data.</summary>
    public T? Data { get; set; }

    /// <summary>Gets or sets the response message.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets the error details if any.</summary>
    public string? Error { get; set; }
}

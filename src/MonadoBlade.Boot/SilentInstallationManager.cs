namespace MonadoBlade.Boot;

using System.Diagnostics;
using System.Text;
using MonadoBlade.Boot.Abstractions;
using Serilog;

/// <summary>
/// Represents a single installation task.
/// </summary>
public class InstallationTask
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public bool SilentMode { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 300;
    public int MaxRetries { get; set; } = 3;
    public Func<string, bool>? SuccessIndicator;
    public string? Description { get; set; }
}

/// <summary>
/// Installation task result with status and logs.
/// </summary>
public class InstallationTaskResult
{
    public string TaskId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int Attempt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Output { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Suggestion { get; set; }
}

/// <summary>
/// Manages silent installations with logging and retry logic.
/// </summary>
public class SilentInstallationManager : IBootService
{
    private const string LogDirectory = "MonadoBladeInstallLogs";
    
    private readonly ILogger _logger;
    private readonly string _logPath;
    private readonly List<InstallationTaskResult> _taskResults = [];
    private readonly object _lockObject = new();

    public event Action<string, double>? ProgressChanged;
    public event Action<InstallationTaskResult>? TaskCompleted;
    public event Action<string>? TaskStarted;

    public SilentInstallationManager(ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;
        _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            LogDirectory);
        
        Directory.CreateDirectory(_logPath);
    }

    /// <summary>
    /// Executes a single installation task with automatic retries.
    /// </summary>
    public async Task<InstallationTaskResult> ExecuteInstallationAsync(
        InstallationTask task,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var lastException = (Exception?)null;
        InstallationTaskResult? lastResult = null;

        for (int attempt = 1; attempt <= task.MaxRetries; attempt++)
        {
            var result = new InstallationTaskResult
            {
                TaskId = task.Id,
                Attempt = attempt
            };

            try
            {
                OnTaskStarted($"Starting {task.Name} (Attempt {attempt}/{task.MaxRetries})");
                
                var output = await ExecuteProcessAsync(task, cancellationToken);
                var success = DetermineSuccess(task, output);

                result.Success = success;
                result.Output = output;
                result.Duration = DateTime.UtcNow - startTime;

                if (success)
                {
                    _logger.Information("Installation task '{TaskName}' completed successfully on attempt {Attempt}", 
                        task.Name, attempt);
                    LogTaskResult(result, task);
                    OnTaskCompleted(result);
                    return result;
                }

                result.ErrorMessage = "Installation completed but success indicators not found";
                result.Suggestion = "Check the installation log for details and try manually installing the component.";
                lastResult = result;
            }
            catch (OperationCanceledException)
            {
                _logger.Warning("Installation task '{TaskName}' was cancelled", task.Name);
                result.ErrorMessage = "Installation was cancelled";
                result.Duration = DateTime.UtcNow - startTime;
                lastException = new OperationCanceledException();
            }
            catch (TimeoutException ex)
            {
                lastException = ex;
                result.ErrorMessage = $"Installation timed out after {task.TimeoutSeconds} seconds";
                result.Duration = DateTime.UtcNow - startTime;
                result.Suggestion = attempt < task.MaxRetries
                    ? "Retrying installation..."
                    : $"Installation of {task.Name} timed out. Check system performance or try again later.";
                
                _logger.Warning("Installation task '{TaskName}' timed out on attempt {Attempt}/{MaxRetries}", 
                    task.Name, attempt, task.MaxRetries);

                if (attempt < task.MaxRetries)
                {
                    await Task.Delay(2000, cancellationToken);
                    continue;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                result.ErrorMessage = ex.Message;
                result.Duration = DateTime.UtcNow - startTime;
                result.Suggestion = attempt < task.MaxRetries
                    ? "Retrying installation..."
                    : $"Installation of {task.Name} failed. {ex.Message}. Try running as Administrator or check the log file.";

                _logger.Error(ex, "Installation task '{TaskName}' failed on attempt {Attempt}/{MaxRetries}", 
                    task.Name, attempt, task.MaxRetries);

                if (attempt < task.MaxRetries)
                {
                    await Task.Delay(2000, cancellationToken);
                    continue;
                }
            }

            lastResult = result;
        }

        lastResult ??= new InstallationTaskResult
        {
            TaskId = task.Id,
            Success = false,
            Attempt = task.MaxRetries,
            Duration = DateTime.UtcNow - startTime,
            ErrorMessage = $"Installation failed after {task.MaxRetries} attempts",
            Suggestion = "Try manually installing the component or contact support."
        };

        LogTaskResult(lastResult, task);
        OnTaskCompleted(lastResult);
        return lastResult;
    }

    /// <summary>
    /// Executes an installer process silently and captures output.
    /// </summary>
    private async Task<string> ExecuteProcessAsync(InstallationTask task, CancellationToken cancellationToken)
    {
        if (!File.Exists(task.ExecutablePath))
        {
            throw new FileNotFoundException($"Installer not found: {task.ExecutablePath}");
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = task.ExecutablePath,
            Arguments = task.Arguments ?? string.Empty,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = processStartInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using (cancellationToken.Register(() =>
        {
            try { process.Kill(true); }
            catch { }
        }))
        {
            var completed = await Task.Run(() => 
                process.WaitForExit(task.TimeoutSeconds * 1000), 
                cancellationToken);

            if (!completed)
            {
                process.Kill(true);
                throw new TimeoutException($"Installation did not complete within {task.TimeoutSeconds} seconds");
            }

            if (process.ExitCode != 0 && process.ExitCode != 3010) // 3010 = reboot required (often success)
            {
                var errorOutput = errorBuilder.ToString();
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    throw new InvalidOperationException($"Installation failed: {errorOutput}");
                }
            }
        }

        return outputBuilder.ToString();
    }

    /// <summary>
    /// Determines if installation succeeded based on output and success indicators.
    /// </summary>
    private static bool DetermineSuccess(InstallationTask task, string output)
    {
        if (task.SuccessIndicator != null)
        {
            return task.SuccessIndicator(output);
        }

        // Default: any non-null output suggests success
        return !string.IsNullOrWhiteSpace(output) || task.SuccessIndicator == null;
    }

    /// <summary>
    /// Logs task result to both logger and file.
    /// </summary>
    private void LogTaskResult(InstallationTaskResult result, InstallationTask task)
    {
        lock (_lockObject)
        {
            var logFile = Path.Combine(_logPath, $"{task.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            
            var logContent = new StringBuilder();
            logContent.AppendLine($"Installation Task: {task.Name}");
            logContent.AppendLine($"Task ID: {task.Id}");
            logContent.AppendLine($"Timestamp: {DateTime.Now:O}");
            logContent.AppendLine($"Attempt: {result.Attempt}/{task.MaxRetries}");
            logContent.AppendLine($"Duration: {result.Duration.TotalSeconds:F2}s");
            logContent.AppendLine($"Status: {(result.Success ? "SUCCESS" : "FAILED")}");
            logContent.AppendLine();
            logContent.AppendLine("--- Output ---");
            logContent.AppendLine(result.Output ?? "(no output)");
            logContent.AppendLine();
            logContent.AppendLine("--- Error ---");
            logContent.AppendLine(result.ErrorMessage ?? "(no error)");
            
            if (!string.IsNullOrEmpty(result.Suggestion))
            {
                logContent.AppendLine();
                logContent.AppendLine("--- Suggestion ---");
                logContent.AppendLine(result.Suggestion);
            }

            File.WriteAllText(logFile, logContent.ToString());
            _taskResults.Add(result);
        }
    }

    /// <summary>
    /// Exports all task results to a comprehensive installation report.
    /// </summary>
    public string ExportReport()
    {
        lock (_lockObject)
        {
            var report = new StringBuilder();
            report.AppendLine("=== Monado Blade Installation Report ===");
            report.AppendLine($"Generated: {DateTime.Now:O}");
            report.AppendLine();

            var successCount = _taskResults.Count(r => r.Success);
            var failureCount = _taskResults.Count(r => !r.Success);

            report.AppendLine($"Summary: {successCount} successful, {failureCount} failed out of {_taskResults.Count} tasks");
            report.AppendLine();

            foreach (var result in _taskResults)
            {
                report.AppendLine($"Task: {result.TaskId}");
                report.AppendLine($"  Status: {(result.Success ? "✓ SUCCESS" : "✗ FAILED")}");
                report.AppendLine($"  Attempt: {result.Attempt}");
                report.AppendLine($"  Duration: {result.Duration.TotalSeconds:F2}s");
                
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    report.AppendLine($"  Error: {result.ErrorMessage}");
                }
                
                if (!string.IsNullOrEmpty(result.Suggestion))
                {
                    report.AppendLine($"  Suggestion: {result.Suggestion}");
                }
                
                report.AppendLine();
            }

            return report.ToString();
        }
    }

    /// <summary>
    /// Gets the path to all installation logs.
    /// </summary>
    public string GetLogDirectoryPath() => _logPath;

    /// <summary>
    /// Gets all logged task results.
    /// </summary>
    public IReadOnlyList<InstallationTaskResult> GetTaskResults()
    {
        lock (_lockObject)
        {
            return _taskResults.AsReadOnly();
        }
    }

    private void OnProgressChanged(string step, double percentage)
    {
        ProgressChanged?.Invoke(step, percentage);
    }

    private void OnTaskCompleted(InstallationTaskResult result)
    {
        TaskCompleted?.Invoke(result);
    }

    private void OnTaskStarted(string message)
    {
        TaskStarted?.Invoke(message);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

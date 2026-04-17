using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Core.Sandbox;

public class SandboxProcess
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ExecutablePath { get; set; }
    public Dictionary<string, object> Behavior { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ProcessStatus Status { get; set; }
    public int ExitCode { get; set; }
}

public enum ProcessStatus
{
    Running,
    Suspended,
    Completed,
    Failed,
    Terminated
}

public class SandboxIsolationConfig
{
    public bool EnableNetworkAccess { get; set; }
    public bool EnableFileSystemAccess { get; set; }
    public bool EnableRegistryAccess { get; set; }
    public bool EnableProcessCreation { get; set; }
    public int MaxMemoryMB { get; set; }
    public int MaxCpuPercent { get; set; }
    public int TimeoutSeconds { get; set; }
}

public class BehaviorAnalysisResult
{
    public string ProcessId { get; set; }
    public List<string> SuspiciousActivities { get; set; } = new();
    public List<string> NetworkConnections { get; set; } = new();
    public List<string> FileSystemOperations { get; set; } = new();
    public List<string> RegistryOperations { get; set; } = new();
    public int RiskScore { get; set; } // 0-100
    public bool IsMalicious { get; set; }
}

public interface ISandboxEnvironment
{
    Task<SandboxProcess> CreateProcessAsync(string executablePath, string arguments, SandboxIsolationConfig config);
    Task<bool> TerminateProcessAsync(string processId);
    Task<bool> SuspendProcessAsync(string processId);
    Task<bool> ResumeProcessAsync(string processId);
    Task<SandboxProcess> GetProcessStatusAsync(string processId);
    Task<List<SandboxProcess>> ListActiveProcessesAsync();
    Task<BehaviorAnalysisResult> AnalyzeProcessBehaviorAsync(string processId);
    Task<bool> RollbackEnvironmentAsync();
    Task<List<SandboxProcess>> GetProcessHistoryAsync(int limit = 100);
}

public class SandboxEnvironment : ISandboxEnvironment
{
    private readonly List<SandboxProcess> _activeProcesses = new();
    private readonly List<SandboxProcess> _processHistory = new();

    public async Task<SandboxProcess> CreateProcessAsync(string executablePath, string arguments, SandboxIsolationConfig config)
    {
        var processId = Guid.NewGuid().ToString();
        var process = new SandboxProcess
        {
            Id = processId,
            Name = System.IO.Path.GetFileNameWithoutExtension(executablePath),
            ExecutablePath = executablePath,
            Status = ProcessStatus.Running,
            StartTime = DateTime.UtcNow,
            Behavior = new Dictionary<string, object>()
        };

        _activeProcesses.Add(process);
        _processHistory.Add(process);

        return await Task.FromResult(process);
    }

    public async Task<bool> TerminateProcessAsync(string processId)
    {
        var process = _activeProcesses.FirstOrDefault(p => p.Id == processId);
        if (process == null)
            return await Task.FromResult(false);

        process.Status = ProcessStatus.Terminated;
        process.EndTime = DateTime.UtcNow;
        _activeProcesses.Remove(process);

        return await Task.FromResult(true);
    }

    public async Task<bool> SuspendProcessAsync(string processId)
    {
        var process = _activeProcesses.FirstOrDefault(p => p.Id == processId);
        if (process == null)
            return await Task.FromResult(false);

        process.Status = ProcessStatus.Suspended;
        return await Task.FromResult(true);
    }

    public async Task<bool> ResumeProcessAsync(string processId)
    {
        var process = _activeProcesses.FirstOrDefault(p => p.Id == processId);
        if (process == null)
            return await Task.FromResult(false);

        if (process.Status == ProcessStatus.Suspended)
            process.Status = ProcessStatus.Running;

        return await Task.FromResult(true);
    }

    public async Task<SandboxProcess> GetProcessStatusAsync(string processId)
    {
        var process = _activeProcesses.FirstOrDefault(p => p.Id == processId)
                   ?? _processHistory.FirstOrDefault(p => p.Id == processId);

        return await Task.FromResult(process);
    }

    public async Task<List<SandboxProcess>> ListActiveProcessesAsync()
    {
        return await Task.FromResult(new List<SandboxProcess>(_activeProcesses));
    }

    public async Task<BehaviorAnalysisResult> AnalyzeProcessBehaviorAsync(string processId)
    {
        var process = _activeProcesses.FirstOrDefault(p => p.Id == processId)
                   ?? _processHistory.FirstOrDefault(p => p.Id == processId);

        if (process == null)
            return await Task.FromResult<BehaviorAnalysisResult>(null);

        var result = new BehaviorAnalysisResult
        {
            ProcessId = processId,
            SuspiciousActivities = new List<string>(),
            NetworkConnections = new List<string>(),
            FileSystemOperations = new List<string>(),
            RegistryOperations = new List<string>(),
            RiskScore = 15,
            IsMalicious = false
        };

        if (process.Behavior.ContainsKey("network_access"))
            result.NetworkConnections.Add((string)process.Behavior["network_access"]);

        if (process.Behavior.ContainsKey("file_operations"))
            result.FileSystemOperations.Add((string)process.Behavior["file_operations"]);

        return await Task.FromResult(result);
    }

    public async Task<bool> RollbackEnvironmentAsync()
    {
        _activeProcesses.Clear();
        return await Task.FromResult(true);
    }

    public async Task<List<SandboxProcess>> GetProcessHistoryAsync(int limit = 100)
    {
        return await Task.FromResult(_processHistory.OrderByDescending(p => p.StartTime).Take(limit).ToList());
    }
}

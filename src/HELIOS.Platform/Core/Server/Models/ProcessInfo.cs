using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Server.Models
{
    /// <summary>
    /// Represents information about a running process.
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// Process ID.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Process name/executable name.
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// Full path to the executable.
        /// </summary>
        public string ExecutablePath { get; set; } = string.Empty;

        /// <summary>
        /// Command line arguments.
        /// </summary>
        public string CommandLine { get; set; } = string.Empty;

        /// <summary>
        /// Username of the process owner.
        /// </summary>
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// Process priority level.
        /// </summary>
        public ProcessPriority Priority { get; set; } = ProcessPriority.Normal;

        /// <summary>
        /// CPU affinity mask (which cores the process can use).
        /// </summary>
        public ulong AffinityMask { get; set; }

        /// <summary>
        /// Memory usage in bytes.
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// Virtual memory usage in bytes.
        /// </summary>
        public long VirtualMemoryUsage { get; set; }

        /// <summary>
        /// Working set memory in bytes.
        /// </summary>
        public long WorkingSetMemory { get; set; }

        /// <summary>
        /// CPU usage percentage (0-100).
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// Number of threads.
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        /// Number of handles.
        /// </summary>
        public int HandleCount { get; set; }

        /// <summary>
        /// Process start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Process uptime duration.
        /// </summary>
        public TimeSpan Uptime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Whether the process is responding.
        /// </summary>
        public bool IsResponding { get; set; } = true;

        /// <summary>
        /// Parent process ID.
        /// </summary>
        public int? ParentProcessId { get; set; }

        /// <summary>
        /// Child process IDs.
        /// </summary>
        public List<int> ChildProcessIds { get; set; } = new();

        /// <summary>
        /// Process state.
        /// </summary>
        public ProcessState State { get; set; } = ProcessState.Running;

        /// <summary>
        /// Memory limit in bytes (if enforced).
        /// </summary>
        public long? MemoryLimit { get; set; }

        /// <summary>
        /// CPU limit as percentage (if enforced).
        /// </summary>
        public double? CpuLimit { get; set; }

        /// <summary>
        /// Associated service ID (if this process is running a service).
        /// </summary>
        public string? AssociatedServiceId { get; set; }

        /// <summary>
        /// Process creation time.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update time.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional environment variables.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    }

    /// <summary>
    /// Process priority levels.
    /// </summary>
    public enum ProcessPriority
    {
        Idle = 64,
        BelowNormal = 16384,
        Normal = 32,
        AboveNormal = 32768,
        High = 128,
        RealTime = 256
    }

    /// <summary>
    /// Process state enumeration.
    /// </summary>
    public enum ProcessState
    {
        Running = 0,
        Suspended = 1,
        Terminated = 2,
        Unknown = 3
    }
}

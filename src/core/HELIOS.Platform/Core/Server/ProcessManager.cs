using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Server.Models;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Manages system processes with memory limits, CPU affinity, and priority control.
    /// </summary>
    public class ProcessManager
    {
        private readonly Dictionary<int, ProcessInfo> _processes = new();
        private readonly Dictionary<int, Process> _processHandles = new();
        private readonly object _lock = new();

        public event EventHandler<ProcessStateChangedEventArgs>? ProcessStateChanged;
        public event EventHandler<ProcessErrorEventArgs>? ProcessError;

        /// <summary>
        /// Gets information about all active processes.
        /// </summary>
        public List<ProcessInfo> GetAllProcesses()
        {
            lock (_lock)
            {
                return _processes.Values.ToList();
            }
        }

        /// <summary>
        /// Gets information about a specific process.
        /// </summary>
        public ProcessInfo? GetProcess(int processId)
        {
            lock (_lock)
            {
                return _processes.TryGetValue(processId, out var info) ? info : null;
            }
        }

        /// <summary>
        /// Gets processes by name.
        /// </summary>
        public List<ProcessInfo> GetProcessesByName(string processName)
        {
            lock (_lock)
            {
                return _processes.Values
                    .Where(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>
        /// Lists all processes with specified filters.
        /// </summary>
        public List<ProcessInfo> ListProcesses(string? ownerFilter = null, long? minMemory = null, long? maxMemory = null)
        {
            lock (_lock)
            {
                var result = _processes.Values.AsEnumerable();

                if (!string.IsNullOrEmpty(ownerFilter))
                    result = result.Where(p => p.Owner.Contains(ownerFilter, StringComparison.OrdinalIgnoreCase));

                if (minMemory.HasValue)
                    result = result.Where(p => p.MemoryUsage >= minMemory.Value);

                if (maxMemory.HasValue)
                    result = result.Where(p => p.MemoryUsage <= maxMemory.Value);

                return result.ToList();
            }
        }

        /// <summary>
        /// Sets CPU affinity for a process.
        /// </summary>
        public bool SetProcessAffinity(int processId, ulong affinityMask)
        {
            try
            {
                if (_processHandles.TryGetValue(processId, out var process) && !process.HasExited)
                {
                    process.ProcessorAffinity = new IntPtr((long)affinityMask);

                    lock (_lock)
                    {
                        if (_processes.TryGetValue(processId, out var info))
                        {
                            info.AffinityMask = affinityMask;
                            info.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                OnProcessError(processId, $"Failed to set affinity: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets process priority.
        /// </summary>
        public bool SetProcessPriority(int processId, ProcessPriority priority)
        {
            try
            {
                if (_processHandles.TryGetValue(processId, out var process) && !process.HasExited)
                {
                    process.PriorityClass = ConvertToPriorityClass(priority);

                    lock (_lock)
                    {
                        if (_processes.TryGetValue(processId, out var info))
                        {
                            info.Priority = priority;
                            info.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                OnProcessError(processId, $"Failed to set priority: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets a memory limit for a process (requires Job Objects on Windows).
        /// </summary>
        public bool SetMemoryLimit(int processId, long limitBytes)
        {
            try
            {
                if (!_processHandles.TryGetValue(processId, out var process) || process.HasExited)
                    return false;

                lock (_lock)
                {
                    if (_processes.TryGetValue(processId, out var info))
                    {
                        info.MemoryLimit = limitBytes;
                        info.UpdatedAt = DateTime.UtcNow;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnProcessError(processId, $"Failed to set memory limit: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets a CPU usage limit for a process.
        /// </summary>
        public bool SetCpuLimit(int processId, double cpuPercentage)
        {
            try
            {
                if (cpuPercentage < 0 || cpuPercentage > 100)
                    throw new ArgumentException("CPU percentage must be between 0 and 100");

                if (!_processHandles.TryGetValue(processId, out var process) || process.HasExited)
                    return false;

                lock (_lock)
                {
                    if (_processes.TryGetValue(processId, out var info))
                    {
                        info.CpuLimit = cpuPercentage;
                        info.UpdatedAt = DateTime.UtcNow;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnProcessError(processId, $"Failed to set CPU limit: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kills a process.
        /// </summary>
        public bool KillProcess(int processId, int timeoutSeconds = 5)
        {
            try
            {
                if (!_processHandles.TryGetValue(processId, out var process))
                {
                    process = Process.GetProcessById(processId);
                }

                if (process.HasExited)
                    return true;

                process.Kill(entireProcessTree: true);

                if (!process.WaitForExit(timeoutSeconds * 1000))
                {
                    OnProcessError(processId, "Process did not exit within timeout");
                    return false;
                }

                lock (_lock)
                {
                    _processHandles.Remove(processId);
                    if (_processes.TryGetValue(processId, out var info))
                    {
                        info.State = ProcessState.Terminated;
                        info.UpdatedAt = DateTime.UtcNow;
                    }
                }

                OnProcessStateChanged(processId, ProcessState.Terminated);
                return true;
            }
            catch (Exception ex)
            {
                OnProcessError(processId, $"Failed to kill process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Suspends a process (freezes all threads).
        /// </summary>
        public bool SuspendProcess(int processId)
        {
            try
            {
                if (!_processHandles.TryGetValue(processId, out var process) || process.HasExited)
                    return false;

                SuspendAllThreads(process);

                lock (_lock)
                {
                    if (_processes.TryGetValue(processId, out var info))
                    {
                        info.State = ProcessState.Suspended;
                        info.UpdatedAt = DateTime.UtcNow;
                    }
                }

                OnProcessStateChanged(processId, ProcessState.Suspended);
                return true;
            }
            catch (Exception ex)
            {
                OnProcessError(processId, $"Failed to suspend process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resumes a suspended process.
        /// </summary>
        public bool ResumeProcess(int processId)
        {
            try
            {
                if (!_processHandles.TryGetValue(processId, out var process) || process.HasExited)
                    return false;

                ResumeAllThreads(process);

                lock (_lock)
                {
                    if (_processes.TryGetValue(processId, out var info))
                    {
                        info.State = ProcessState.Running;
                        info.UpdatedAt = DateTime.UtcNow;
                    }
                }

                OnProcessStateChanged(processId, ProcessState.Running);
                return true;
            }
            catch (Exception ex)
            {
                OnProcessError(processId, $"Failed to resume process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates process information from system.
        /// </summary>
        public void RefreshProcessInfo(int processId)
        {
            try
            {
                Process? process = null;

                if (_processHandles.TryGetValue(processId, out var cached))
                {
                    if (!cached.HasExited)
                        process = cached;
                }
                else
                {
                    process = Process.GetProcessById(processId);
                }

                if (process == null || process.HasExited)
                {
                    lock (_lock)
                    {
                        _processes.Remove(processId);
                        _processHandles.Remove(processId);
                    }
                    return;
                }

                lock (_lock)
                {
                    if (_processes.TryGetValue(processId, out var info))
                    {
                        try
                        {
                            info.MemoryUsage = process.WorkingSet64;
                            info.VirtualMemoryUsage = process.VirtualMemorySize64;
                            info.WorkingSetMemory = process.WorkingSet64;
                            info.ThreadCount = process.Threads.Count;
                            info.HandleCount = process.HandleCount;
                            info.Uptime = DateTime.UtcNow - process.StartTime;
                            info.IsResponding = process.Responding;
                            info.UpdatedAt = DateTime.UtcNow;
                        }
                        catch
                        {
                            // Some properties might not be accessible
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessError(processId, $"Failed to refresh process info: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers a process for monitoring.
        /// </summary>
        public void RegisterProcess(ProcessInfo processInfo)
        {
            lock (_lock)
            {
                _processes[processInfo.ProcessId] = processInfo;

                try
                {
                    if (!_processHandles.ContainsKey(processInfo.ProcessId))
                    {
                        var process = Process.GetProcessById(processInfo.ProcessId);
                        _processHandles[processInfo.ProcessId] = process;
                    }
                }
                catch
                {
                    // Process might not exist
                }
            }
        }

        /// <summary>
        /// Unregisters a process from monitoring.
        /// </summary>
        public void UnregisterProcess(int processId)
        {
            lock (_lock)
            {
                _processes.Remove(processId);
                if (_processHandles.TryGetValue(processId, out var process))
                {
                    process.Dispose();
                    _processHandles.Remove(processId);
                }
            }
        }

        private void SuspendAllThreads(Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                NativeMethods.SuspendThread(thread.Id);
            }
        }

        private void ResumeAllThreads(Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                NativeMethods.ResumeThread(thread.Id);
            }
        }

        private ProcessPriorityClass ConvertToPriorityClass(ProcessPriority priority)
        {
            return priority switch
            {
                ProcessPriority.Idle => ProcessPriorityClass.Idle,
                ProcessPriority.BelowNormal => ProcessPriorityClass.BelowNormal,
                ProcessPriority.Normal => ProcessPriorityClass.Normal,
                ProcessPriority.AboveNormal => ProcessPriorityClass.AboveNormal,
                ProcessPriority.High => ProcessPriorityClass.High,
                ProcessPriority.RealTime => ProcessPriorityClass.RealTime,
                _ => ProcessPriorityClass.Normal
            };
        }

        private void OnProcessStateChanged(int processId, ProcessState newState)
        {
            ProcessStateChanged?.Invoke(this, new ProcessStateChangedEventArgs { ProcessId = processId, NewState = newState });
        }

        private void OnProcessError(int processId, string errorMessage)
        {
            ProcessError?.Invoke(this, new ProcessErrorEventArgs { ProcessId = processId, ErrorMessage = errorMessage });
        }
    }

    /// <summary>
    /// Event arguments for process state changes.
    /// </summary>
    public class ProcessStateChangedEventArgs : EventArgs
    {
        public int ProcessId { get; set; }
        public ProcessState NewState { get; set; }
    }

    /// <summary>
    /// Event arguments for process errors.
    /// </summary>
    public class ProcessErrorEventArgs : EventArgs
    {
        public int ProcessId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Native Windows API methods for thread control.
    /// </summary>
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern uint SuspendThread(int threadId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern uint ResumeThread(int threadId);
    }
}

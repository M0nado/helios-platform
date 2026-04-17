using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Diagnostics
{
    /// <summary>
    /// System diagnostics and performance analysis.
    /// </summary>
    public class SystemDiagnostics
    {
        private readonly Core.Logging.ILogger _logger;

        public SystemDiagnostics()
        {
            _logger = ServiceContainer.Instance.GetService<Core.Logging.ILogger>();
        }

        /// <summary>
        /// Get detailed system information.
        /// </summary>
        public async Task<SystemInfo> GetSystemInfoAsync()
        {
            try
            {
                var info = new SystemInfo
                {
                    Timestamp = DateTime.UtcNow,
                    ProcessorCount = Environment.ProcessorCount,
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    WorkingSet = Environment.WorkingSet,
                    TotalMemory = GC.GetTotalMemory(false)
                };

                _logger?.Debug("System information retrieved");
                await Task.Delay(10);
                return info;
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to get system information", ex);
                throw;
            }
        }

        /// <summary>
        /// Get running processes information.
        /// </summary>
        public async Task<List<ProcessInfo>> GetRunningProcessesAsync()
        {
            try
            {
                var processes = new List<ProcessInfo>();
                var allProcesses = Process.GetProcesses();

                foreach (var proc in allProcesses)
                {
                    try
                    {
                        processes.Add(new ProcessInfo
                        {
                            Id = proc.Id,
                            Name = proc.ProcessName,
                            MemoryMB = proc.WorkingSet64 / (1024 * 1024),
                            Threads = proc.Threads.Count,
                            Priority = proc.PriorityClass.ToString()
                        });
                    }
                    catch { /* Skip processes we can't access */ }
                }

                _logger?.Debug($"Retrieved {processes.Count} running processes");
                await Task.Delay(10);
                return processes;
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to get running processes", ex);
                throw;
            }
        }

        /// <summary>
        /// Get network information.
        /// </summary>
        public async Task<NetworkInfo> GetNetworkInfoAsync()
        {
            try
            {
                var info = new NetworkInfo
                {
                    HostName = System.Net.Dns.GetHostName(),
                    Timestamp = DateTime.UtcNow
                };

                _logger?.Debug("Network information retrieved");
                await Task.Delay(10);
                return info;
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to get network information", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// System information data model.
    /// </summary>
    public class SystemInfo
    {
        public DateTime Timestamp { get; set; }
        public int ProcessorCount { get; set; }
        public string MachineName { get; set; }
        public string OSVersion { get; set; }
        public long WorkingSet { get; set; }
        public long TotalMemory { get; set; }
    }

    /// <summary>
    /// Process information data model.
    /// </summary>
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long MemoryMB { get; set; }
        public int Threads { get; set; }
        public string Priority { get; set; }
    }

    /// <summary>
    /// Network information data model.
    /// </summary>
    public class NetworkInfo
    {
        public DateTime Timestamp { get; set; }
        public string HostName { get; set; }
    }
}

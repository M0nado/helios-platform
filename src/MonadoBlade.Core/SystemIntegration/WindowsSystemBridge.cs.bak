// MONADO BLADE v3.4.0 - WINDOWS SYSTEM INTEGRATION
// File: src/MonadoBlade.Core/SystemIntegration/WindowsSystemBridge.cs

using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace MonadoBlade.Core.SystemIntegration
{
    /// <summary>
    /// Windows system integration for Performance Monitor, Task Manager, Settings
    /// </summary>
    public class WindowsSystemBridge
    {
        public class PerformanceMonitor
        {
            public class SystemMetrics
            {
                public float CPUUsagePercent { get; set; }
                public float MemoryUsagePercent { get; set; }
                public float DiskUsagePercent { get; set; }
                public float GPUUsagePercent { get; set; }
                public DateTime SampleTime { get; set; }
            }

            public static SystemMetrics GetSystemMetrics()
            {
                return new SystemMetrics
                {
                    SampleTime = DateTime.Now,
                    CPUUsagePercent = GetCPUUsage(),
                    MemoryUsagePercent = GetMemoryUsage(),
                    DiskUsagePercent = GetDiskUsage(),
                    GPUUsagePercent = 0f
                };
            }

            private static float GetCPUUsage()
            {
                try
                {
                    var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    cpuCounter.NextValue();
                    System.Threading.Thread.Sleep(100);
                    return cpuCounter.NextValue();
                }
                catch { return 0f; }
            }

            private static float GetMemoryUsage()
            {
                try
                {
                    var ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                    return ramCounter.NextValue();
                }
                catch { return 0f; }
            }

            private static float GetDiskUsage()
            {
                try
                {
                    var diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                    diskCounter.NextValue();
                    System.Threading.Thread.Sleep(100);
                    return diskCounter.NextValue();
                }
                catch { return 0f; }
            }
        }

        public class TaskManager
        {
            public class ProcessInfo
            {
                public int ProcessId { get; set; }
                public string ProcessName { get; set; }
                public long MemoryUsageBytes { get; set; }
                public string Status { get; set; }
                public DateTime StartTime { get; set; }
            }

            public static List<ProcessInfo> GetRunningProcesses()
            {
                var processes = new List<ProcessInfo>();
                try
                {
                    foreach (var process in Process.GetProcesses())
                    {
                        processes.Add(new ProcessInfo
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            MemoryUsageBytes = process.WorkingSet64,
                            Status = process.Responding ? "Running" : "Not Responding",
                            StartTime = process.StartTime
                        });
                    }
                }
                catch { }
                return processes;
            }

            public static bool KillProcess(int processId)
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    process.Kill();
                    return true;
                }
                catch { return false; }
            }
        }

        public class WindowsSettings
        {
            public static void OpenSettings(string page = null)
            {
                try
                {
                    var args = string.IsNullOrEmpty(page) ? "/c start ms-settings:" : $"/c start ms-settings:{page}";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = args,
                        UseShellExecute = false
                    });
                }
                catch { }
            }

            public static string GetCurrentTheme()
            {
                try
                {
                    var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value != null && value is int intValue)
                        {
                            return intValue == 1 ? "Light" : "Dark";
                        }
                    }
                }
                catch { }
                return "Light";
            }
        }
    }
}

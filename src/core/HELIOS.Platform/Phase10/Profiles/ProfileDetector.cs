using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Profiles;

/// <summary>
/// Detects optimal profile based on hardware and usage patterns
/// </summary>
public class ProfileDetector : IProfileDetector
{
    private readonly Dictionary<string, int> _profileUsageCount = new();
    private readonly Dictionary<string, TimeSpan> _profileUsageTime = new();

    /// <summary>
    /// Detects the optimal profile based on system capabilities
    /// </summary>
    public async Task<string> DetectOptimalProfileAsync()
    {
        try
        {
            var hardware = await AnalyzeHardwareAsync();
            var usage = await DetectUsageAsync();

            var gpuCores = (int?)hardware["GPUCores"] ?? 0;
            var cpuCores = (int?)hardware["CPUCores"] ?? 0;
            var totalRam = (long?)hardware["TotalRam"] ?? 0;
            var hasGPU = (bool?)hardware["HasDedicatedGPU"] ?? false;

            var runningProcesses = usage["RunningProcesses"] as List<string> ?? new List<string>();

            if (hasGPU && gpuCores >= 8 && cpuCores >= 8 && totalRam >= 16 * 1024 * 1024 * 1024)
            {
                if (DetectGamingApps(runningProcesses))
                    return "Gaming";
            }

            if (DetectDevelopmentApps(runningProcesses))
                return "Development";

            if (DetectProductivityApps(runningProcesses))
                return "Work";

            if (totalRam >= 8 * 1024 * 1024 * 1024)
                return "Development";

            return "Work";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to detect optimal profile", ex);
        }
    }

    /// <summary>
    /// Analyzes hardware capabilities
    /// </summary>
    public async Task<Dictionary<string, object>> AnalyzeHardwareAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                var hardware = new Dictionary<string, object>
                {
                    ["CPUCores"] = Environment.ProcessorCount,
                    ["CPUName"] = GetCPUName(),
                    ["TotalRam"] = GC.GetTotalMemory(false),
                    ["HasDedicatedGPU"] = DetectGPU(),
                    ["GPUCores"] = DetectGPUCores(),
                    ["OSVersion"] = Environment.OSVersion.ToString(),
                    ["Architecture"] = Environment.Is64BitProcess ? "x64" : "x86"
                };

                return hardware;
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to analyze hardware", ex);
        }
    }

    /// <summary>
    /// Detects typical usage patterns
    /// </summary>
    public async Task<Dictionary<string, object>> DetectUsageAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                var runningProcesses = GetRunningProcesses();
                var installedApps = DetectInstalledApplications();

                return new Dictionary<string, object>
                {
                    ["RunningProcesses"] = runningProcesses,
                    ["InstalledApps"] = installedApps,
                    ["IsGamingSetup"] = DetectGamingApps(runningProcesses),
                    ["IsDevelopmentSetup"] = DetectDevelopmentApps(runningProcesses),
                    ["IsProductivitySetup"] = DetectProductivityApps(runningProcesses),
                    ["IsSecurityFocused"] = DetectSecurityFocus()
                };
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to detect usage patterns", ex);
        }
    }

    /// <summary>
    /// Learns from user behavior to improve recommendations
    /// </summary>
    public async Task<bool> LearnBehaviorAsync(string profileUsed, TimeSpan duration, Dictionary<string, object> metrics)
    {
        try
        {
            return await Task.Run(() =>
            {
                if (!_profileUsageCount.ContainsKey(profileUsed))
                    _profileUsageCount[profileUsed] = 0;

                _profileUsageCount[profileUsed]++;

                if (!_profileUsageTime.ContainsKey(profileUsed))
                    _profileUsageTime[profileUsed] = TimeSpan.Zero;

                _profileUsageTime[profileUsed] = _profileUsageTime[profileUsed].Add(duration);

                return true;
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to learn from behavior", ex);
        }
    }

    private static string GetCPUName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                if (obj["Name"] is string name)
                    return name;
            }
        }
        catch { }

        return "Unknown CPU";
    }

    private static bool DetectGPU()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select AdapterRAM from Win32_VideoController");
            return searcher.Get().Count > 0;
        }
        catch { }

        return false;
    }

    private static int DetectGPUCores()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select VideoModeDescription from Win32_VideoController");
            var count = searcher.Get().Count;
            return count > 0 ? 4 : 0;
        }
        catch { }

        return 0;
    }

    private static List<string> GetRunningProcesses()
    {
        var processes = new List<string>();
        try
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    processes.Add(process.ProcessName.ToLower());
                    process.Dispose();
                }
                catch { }
            }
        }
        catch { }

        return processes;
    }

    private static List<string> DetectInstalledApplications()
    {
        var apps = new List<string>();
        try
        {
            var searchPaths = new[]
            {
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };

            foreach (var path in searchPaths)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        var dirs = Directory.GetDirectories(path);
                        apps.AddRange(dirs.Select(d => new DirectoryInfo(d).Name).Take(20));
                    }
                }
                catch { }
            }
        }
        catch { }

        return apps;
    }

    private static bool DetectGamingApps(List<string> runningProcesses)
    {
        var gamingApps = new[] { "steam", "origin", "epicgames", "discord", "nvidia", "amd", "radeon" };
        return runningProcesses.Any(p => gamingApps.Any(g => p.Contains(g)));
    }

    private static bool DetectDevelopmentApps(List<string> runningProcesses)
    {
        var devApps = new[] { "code", "visualstudio", "git", "docker", "node", "python", "powershell" };
        return runningProcesses.Any(p => devApps.Any(d => p.Contains(d)));
    }

    private static bool DetectProductivityApps(List<string> runningProcesses)
    {
        var productivityApps = new[] { "teams", "outlook", "excel", "word", "chrome", "firefox", "edge" };
        return runningProcesses.Any(p => productivityApps.Any(pr => p.Contains(pr)));
    }

    private static bool DetectSecurityFocus()
    {
        try
        {
            var processes = Process.GetProcesses();
            var securityApps = new[] { "defender", "antivirus", "vpn", "malwarebytes", "kaspersky" };
            var found = processes.Any(p => securityApps.Any(s => p.ProcessName.ToLower().Contains(s)));
            
            foreach (var process in processes)
                process.Dispose();

            return found;
        }
        catch { }

        return false;
    }
}

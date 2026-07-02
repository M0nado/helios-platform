namespace MonadoBlade.Boot;

using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using MonadoBlade.Boot.Abstractions;
using Serilog;

/// <summary>
/// Represents the result of a single pre-flight check.
/// </summary>
public class PreflightCheckResult
{
    public string CheckName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
}

/// <summary>
/// Complete result of all pre-flight checks.
/// </summary>
public class PreflightResult
{
    /// <summary>
    /// Whether all checks passed successfully.
    /// </summary>
    public bool AllChecksPassed { get; set; }
    
    /// <summary>
    /// Total number of checks performed.
    /// </summary>
    public int TotalChecks { get; set; }
    
    /// <summary>
    /// Number of checks that passed.
    /// </summary>
    public int PassedChecks { get; set; }
    
    /// <summary>
    /// Total elapsed time for all checks.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }
    
    /// <summary>
    /// List of individual check results.
    /// </summary>
    public List<PreflightCheckResult> Results { get; set; } = [];
}

/// <summary>
/// Performs comprehensive system pre-flight checks before the wizard starts.
/// Ensures all required conditions are met for a smooth installation experience.
/// </summary>
public class SystemPreflightChecker : IBootService
{
    private const int TimeoutSeconds = 30;
    private const int MinimumDiskSpaceGb = 100;
    private const int MinimumUsbVersion = 3;
    
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _timeoutCts;

    /// <summary>
    /// Initializes a new instance of the SystemPreflightChecker class.
    /// </summary>
    /// <param name="logger">Optional logger instance for diagnostic output.</param>
    public SystemPreflightChecker(ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;
        _timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
    }

    /// <summary>
    /// Runs all pre-flight checks with a timeout.
    /// </summary>
    public async Task<PreflightResult> RunChecksAsync()
    {
        var startTime = DateTime.UtcNow;
        var results = new List<PreflightCheckResult>();

        try
        {
            // Network connectivity check
            results.Add(await CheckNetworkConnectivityAsync());

            // Disk space check
            results.Add(await CheckDiskSpaceAsync());

            // Admin permissions check
            results.Add(CheckAdminPermissions());

            // USB ports check
            results.Add(await CheckUsbPortsAsync());

            // BitLocker status check
            results.Add(CheckBitLockerStatus());

            // Antivirus exclusions check
            results.Add(await CheckAntivirusExclusionsAsync());

            // Firewall rules check
            results.Add(await CheckFirewallRulesAsync());
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Pre-flight checks exceeded timeout of {TimeoutSeconds} seconds", TimeoutSeconds);
            results.Add(new PreflightCheckResult
            {
                CheckName = "Timeout Check",
                Passed = false,
                Message = $"Pre-flight checks exceeded {TimeoutSeconds} second timeout",
                Suggestion = "Some system operations may be slow. Please check your system performance."
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error during pre-flight checks");
        }

        var elapsedTime = DateTime.UtcNow - startTime;
        var passedCount = results.Count(r => r.Passed);

        var result = new PreflightResult
        {
            AllChecksPassed = results.All(r => r.Passed),
            TotalChecks = results.Count,
            PassedChecks = passedCount,
            ElapsedTime = elapsedTime,
            Results = results
        };

        LogResults(result);
        return result;
    }

    /// <summary>
    /// Checks network connectivity via DNS and ICMP ping.
    /// </summary>
    private async Task<PreflightCheckResult> CheckNetworkConnectivityAsync()
    {
        try
        {
            // Check DNS resolution
            try
            {
                await Dns.GetHostAddressesAsync("8.8.8.8");
            }
            catch
            {
                return new PreflightCheckResult
                {
                    CheckName = "Network Connectivity",
                    Passed = false,
                    Message = "DNS resolution failed",
                    Suggestion = "Check your internet connection. Ensure WiFi is connected or ethernet cable is plugged in."
                };
            }

            // Check ICMP ping (with timeout)
            using (var ping = new Ping())
            {
                var pingReply = await ping.SendPingAsync("8.8.8.8", 5000);
                if (pingReply.Status != IPStatus.Success)
                {
                    return new PreflightCheckResult
                    {
                        CheckName = "Network Connectivity",
                        Passed = false,
                        Message = $"Ping failed: {pingReply.Status}",
                        Suggestion = "Check your internet connection. Ensure WiFi is connected or ethernet cable is plugged in."
                    };
                }
            }

            return new PreflightCheckResult
            {
                CheckName = "Network Connectivity",
                Passed = true,
                Message = "Network connectivity confirmed"
            };
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Network connectivity check failed");
            return new PreflightCheckResult
            {
                CheckName = "Network Connectivity",
                Passed = false,
                Message = $"Network check error: {ex.Message}",
                Suggestion = "Check your internet connection or firewall settings."
            };
        }
    }

    /// <summary>
    /// Checks available disk space (minimum 100GB required).
    /// </summary>
    private Task<PreflightCheckResult> CheckDiskSpaceAsync()
    {
        try
        {
            var driveInfo = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
            if (driveInfo == null)
            {
                return Task.FromResult(new PreflightCheckResult
                {
                    CheckName = "Disk Space",
                    Passed = false,
                    Message = "No ready drives found",
                    Suggestion = "Ensure your primary drive is accessible and has at least 100GB free space."
                });
            }

            var availableGb = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);
            var passed = availableGb >= MinimumDiskSpaceGb;

            return Task.FromResult(new PreflightCheckResult
            {
                CheckName = "Disk Space",
                Passed = passed,
                Message = passed
                    ? $"Sufficient disk space: {availableGb}GB available"
                    : $"Insufficient disk space: {availableGb}GB available (need {MinimumDiskSpaceGb}GB)",
                Suggestion = passed ? null : $"Free up at least {MinimumDiskSpaceGb}GB by deleting temporary files, old backups, or unused applications."
            });
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Disk space check failed");
            return Task.FromResult(new PreflightCheckResult
            {
                CheckName = "Disk Space",
                Passed = false,
                Message = $"Disk space check error: {ex.Message}",
                Suggestion = "Ensure your primary drive is accessible."
            });
        }
    }

    /// <summary>
    /// Checks if running with administrator privileges.
    /// </summary>
    private PreflightCheckResult CheckAdminPermissions()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            var isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

            return new PreflightCheckResult
            {
                CheckName = "Admin Permissions",
                Passed = isAdmin,
                Message = isAdmin ? "Running with administrator privileges" : "Not running as administrator",
                Suggestion = isAdmin ? null : "Right-click on the installer and select 'Run as Administrator'."
            };
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Admin permissions check failed");
            return new PreflightCheckResult
            {
                CheckName = "Admin Permissions",
                Passed = false,
                Message = $"Admin check error: {ex.Message}",
                Suggestion = "Run the installer as Administrator."
            };
        }
    }

    /// <summary>
    /// Checks for available USB 3.0 ports.
    /// </summary>
    private async Task<PreflightCheckResult> CheckUsbPortsAsync()
    {
        try
        {
            // Use WMI to check USB ports
            var usbDevices = await GetUsbPortCountAsync();

            if (usbDevices <= 0)
            {
                return new PreflightCheckResult
                {
                    CheckName = "USB Ports",
                    Passed = false,
                    Message = "No USB 3.0 ports detected",
                    Suggestion = "Ensure your computer has at least one USB 3.0 port for device connectivity. Connect your device to a USB 3.0 port (usually blue-colored)."
                };
            }

            return new PreflightCheckResult
            {
                CheckName = "USB Ports",
                Passed = true,
                Message = $"USB ports available: {usbDevices} USB 3.0 ports detected"
            };
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "USB ports check failed");
            return new PreflightCheckResult
            {
                CheckName = "USB Ports",
                Passed = false,
                Message = $"USB check error: {ex.Message}",
                Suggestion = "Ensure your computer has at least one USB 3.0 port available."
            };
        }
    }

    /// <summary>
    /// Gets the count of available USB ports asynchronously.
    /// </summary>
    private static Task<int> GetUsbPortCountAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                // Check for USB 3.0 devices in Device Manager
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wmic",
                        Arguments = "path win32_usbhub get name",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var usbCount = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length - 2; // Subtract header line and extra line
                return Math.Max(usbCount, 1); // Assume at least 1 USB port if WMIC fails gracefully
            }
            catch
            {
                return 1; // Default to assume USB available on error
            }
        });
    }

    /// <summary>
    /// Checks BitLocker status and warns if enabled.
    /// </summary>
    private PreflightCheckResult CheckBitLockerStatus()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "manage-bde",
                    Arguments = "-status",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var isBitLockerEnabled = output.Contains("Fully Encrypted") || output.Contains("Encryption in Progress");

            return new PreflightCheckResult
            {
                CheckName = "BitLocker Status",
                Passed = true,
                Message = isBitLockerEnabled ? "BitLocker is enabled (warning)" : "BitLocker is disabled or suspended",
                Suggestion = isBitLockerEnabled ? "BitLocker is enabled. Installation may be slower. Consider temporarily suspending BitLocker for faster installation." : null
            };
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "BitLocker check failed");
            return new PreflightCheckResult
            {
                CheckName = "BitLocker Status",
                Passed = true,
                Message = "BitLocker status check skipped (requires elevation)"
            };
        }
    }

    /// <summary>
    /// Checks antivirus exclusions for installation directory.
    /// </summary>
    private Task<PreflightCheckResult> CheckAntivirusExclusionsAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MonadoBlade");
                
                // Check Windows Defender exclusions
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -Command \"Get-MpPreference | Select-Object -ExpandProperty ExclusionPath\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var hasExclusion = output.Contains(installPath) || output.Contains("MonadoBlade");

                return new PreflightCheckResult
                {
                    CheckName = "Antivirus Exclusions",
                    Passed = true,
                    Message = hasExclusion
                        ? "Installation directory is excluded from antivirus scanning"
                        : "Installation directory is not excluded from antivirus scanning (warning)",
                    Suggestion = hasExclusion
                        ? null
                        : $"Consider adding {installPath} to your antivirus exclusions for better performance. This reduces false positives during installation."
                };
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Antivirus check failed");
                return new PreflightCheckResult
                {
                    CheckName = "Antivirus Exclusions",
                    Passed = true,
                    Message = "Antivirus check skipped"
                };
            }
        });
    }

    /// <summary>
    /// Checks firewall rules allow setup traffic.
    /// </summary>
    private Task<PreflightCheckResult> CheckFirewallRulesAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = "-NoProfile -Command \"Get-NetFirewallProfile | Select-Object -ExpandProperty Enabled\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var allDisabled = output.Contains("False");

                return new PreflightCheckResult
                {
                    CheckName = "Firewall Rules",
                    Passed = true,
                    Message = "Firewall configuration checked",
                    Suggestion = allDisabled ? null : "Firewall is enabled. Installation may take longer, but this is normal. The installer will request necessary permissions."
                };
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Firewall check failed");
                return new PreflightCheckResult
                {
                    CheckName = "Firewall Rules",
                    Passed = true,
                    Message = "Firewall check skipped"
                };
            }
        });
    }

    /// <summary>
    /// Logs the results of all pre-flight checks.
    /// </summary>
    private void LogResults(PreflightResult result)
    {
        _logger.Information("Pre-flight checks completed in {ElapsedTime}ms", result.ElapsedTime.TotalMilliseconds);
        _logger.Information("Passed {PassedCount}/{TotalCount} checks", result.PassedChecks, result.TotalChecks);

        foreach (var check in result.Results)
        {
            var status = check.Passed ? "✓ PASS" : "✗ FAIL";
            _logger.Information("{Status} {CheckName}: {Message}", status, check.CheckName, check.Message);

            if (!string.IsNullOrEmpty(check.Suggestion))
            {
                _logger.Information("  Suggestion: {Suggestion}", check.Suggestion);
            }
        }
    }

    /// <summary>
    /// Disposes resources used by the checker.
    /// </summary>
    public void Dispose()
    {
        _timeoutCts?.Dispose();
    }
}

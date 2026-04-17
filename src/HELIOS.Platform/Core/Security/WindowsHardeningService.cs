using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Core.Administration;

namespace HELIOS.Platform.Core.Security
{
    /// <summary>
    /// Windows hardening configuration and enforcement.
    /// </summary>
    public interface IWindowsHardeningService
    {
        Task<HardeningReport> ApplyHardeningAsync();
        Task<HardeningStatus> GetHardeningStatusAsync();
        Task<bool> EnableDefenderAsync();
        Task<bool> ApplyFirewallRulesAsync();
        Task<bool> EnforceUACAsync();
        Task<List<HardeningIssue>> VerifyHardeningAsync();
    }

    public class HardeningReport
    {
        public DateTime CompletedAt { get; set; }
        public List<string> AppliedSettings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool Success { get; set; }
        public int IssuesFixed { get; set; }
    }

    public class HardeningStatus
    {
        public bool DefenderEnabled { get; set; }
        public bool FirewallActive { get; set; }
        public bool UACEnforced { get; set; }
        public bool WindowsUpdateEnabled { get; set; }
        public int OpenPorts { get; set; }
        public List<string> SuspiciousServices { get; set; } = new();
        public string OverallStatus { get; set; } = "Unknown";
    }

    public class HardeningIssue
    {
        public string Category { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public string Remediation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Windows hardening service for security enforcement.
    /// </summary>
    public class WindowsHardeningService : IWindowsHardeningService
    {
        private readonly Core.Logging.ILogger? _logger;
        private readonly ISystemManagementService? _systemManager;

        public WindowsHardeningService(Core.Logging.ILogger? logger = null, ISystemManagementService? systemManager = null)
        {
            _logger = logger;
            _systemManager = systemManager;
        }

        public async Task<HardeningReport> ApplyHardeningAsync()
        {
            var report = new HardeningReport { CompletedAt = DateTime.UtcNow };

            try
            {
                _logger?.Info("Starting Windows hardening...");

                // Apply Defender
                if (await EnableDefenderAsync())
                    report.AppliedSettings.Add("Windows Defender enabled");

                // Apply Firewall
                if (await ApplyFirewallRulesAsync())
                    report.AppliedSettings.Add("Firewall rules applied");

                // Enforce UAC
                if (await EnforceUACAsync())
                    report.AppliedSettings.Add("UAC enforced");

                // Apply security policy updates
                if (await ApplySecurityPoliciesAsync())
                    report.AppliedSettings.Add("Security policies updated");

                // Disable unnecessary services
                if (await DisableUnnecessaryServicesAsync())
                    report.AppliedSettings.Add("Unnecessary services disabled");

                // Configure Windows Update
                if (await EnableWindowsUpdateAsync())
                    report.AppliedSettings.Add("Windows Update configured for automatic");

                report.IssuesFixed = report.AppliedSettings.Count;
                report.Success = true;

                _logger?.Info($"Windows hardening completed: {report.AppliedSettings.Count} settings applied");
            }
            catch (Exception ex)
            {
                report.Errors.Add($"Hardening failed: {ex.Message}");
                report.Success = false;
                _logger?.Error($"Hardening error: {ex.Message}");
            }

            return report;
        }

        public async Task<HardeningStatus> GetHardeningStatusAsync()
        {
            var status = new HardeningStatus();

            try
            {
                // Check Defender status
                status.DefenderEnabled = await IsDefenderEnabledAsync();

                // Check Firewall status
                status.FirewallActive = await IsFirewallActiveAsync();

                // Check UAC status
                status.UACEnforced = await IsUACEnforcedAsync();

                // Check Windows Update
                status.WindowsUpdateEnabled = await IsWindowsUpdateEnabledAsync();

                // Get open ports
                status.OpenPorts = await GetOpenPortsAsync();

                // Check for suspicious services
                status.SuspiciousServices = await GetSuspiciousServicesAsync();

                // Determine overall status
                status.OverallStatus = DetermineOverallStatus(status);

                _logger?.Info($"Hardening status checked: {status.OverallStatus}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Status check error: {ex.Message}");
                status.OverallStatus = "Error";
            }

            return status;
        }

        public async Task<bool> EnableDefenderAsync()
        {
            try
            {
                // PowerShell command to enable Windows Defender
                var result = await ExecutePowerShellAsync(
                    "Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction SilentlyContinue");

                _logger?.Info("Windows Defender enabled");
                return result;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to enable Defender: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ApplyFirewallRulesAsync()
        {
            try
            {
                // Enable firewall
                var result1 = await ExecutePowerShellAsync(
                    "Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled true -ErrorAction SilentlyContinue");

                // Set default inbound policy to block
                var result2 = await ExecutePowerShellAsync(
                    "Set-NetFirewallProfile -Profile Domain,Public,Private -DefaultInboundAction Block -ErrorAction SilentlyContinue");

                // Set default outbound policy to allow
                var result3 = await ExecutePowerShellAsync(
                    "Set-NetFirewallProfile -Profile Domain,Public,Private -DefaultOutboundAction Allow -ErrorAction SilentlyContinue");

                _logger?.Info("Firewall rules applied");
                return result1 && result2 && result3;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to apply firewall rules: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EnforceUACAsync()
        {
            try
            {
                // Enable UAC - set to highest level (2)
                var result = await ExecutePowerShellAsync(
                    "Set-ItemProperty -Path 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name ConsentPromptBehaviorAdmin -Value 2 -ErrorAction SilentlyContinue");

                _logger?.Info("UAC enforcement applied");
                return result;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to enforce UAC: {ex.Message}");
                return false;
            }
        }

        public async Task<List<HardeningIssue>> VerifyHardeningAsync()
        {
            var issues = new List<HardeningIssue>();

            try
            {
                var status = await GetHardeningStatusAsync();

                if (!status.DefenderEnabled)
                    issues.Add(new HardeningIssue
                    {
                        Category = "Antivirus",
                        Issue = "Windows Defender is not enabled",
                        Severity = "Critical",
                        Remediation = "Enable Windows Defender in Security settings"
                    });

                if (!status.FirewallActive)
                    issues.Add(new HardeningIssue
                    {
                        Category = "Firewall",
                        Issue = "Windows Firewall is not active",
                        Severity = "Critical",
                        Remediation = "Enable Windows Firewall in Security settings"
                    });

                if (!status.UACEnforced)
                    issues.Add(new HardeningIssue
                    {
                        Category = "UAC",
                        Issue = "User Account Control is not enforced",
                        Severity = "High",
                        Remediation = "Set UAC to highest level in Security settings"
                    });

                if (!status.WindowsUpdateEnabled)
                    issues.Add(new HardeningIssue
                    {
                        Category = "Updates",
                        Issue = "Windows Update is not enabled",
                        Severity = "Critical",
                        Remediation = "Enable automatic Windows Update"
                    });

                if (status.OpenPorts > 10)
                    issues.Add(new HardeningIssue
                    {
                        Category = "Network",
                        Issue = $"Excessive open ports detected: {status.OpenPorts}",
                        Severity = "Medium",
                        Remediation = "Review and close unnecessary ports via firewall rules"
                    });

                if (status.SuspiciousServices.Count > 0)
                    issues.Add(new HardeningIssue
                    {
                        Category = "Services",
                        Issue = $"Suspicious services found: {string.Join(", ", status.SuspiciousServices)}",
                        Severity = "High",
                        Remediation = "Investigate and disable unnecessary or suspicious services"
                    });

                _logger?.Info($"Verification complete: {issues.Count} issues found");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Verification error: {ex.Message}");
            }

            return issues;
        }

        private async Task<bool> ApplySecurityPoliciesAsync()
        {
            try
            {
                // Disable SMBv1 (legacy protocol)
                await ExecutePowerShellAsync(
                    "Disable-WindowsOptionalFeature -Online -FeatureName SMB1Protocol -NoRestart -ErrorAction SilentlyContinue");

                // Enable Network Level Authentication for RDP
                await ExecutePowerShellAsync(
                    "Set-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp' -Name SecurityLayer -Value 2 -ErrorAction SilentlyContinue");

                _logger?.Info("Security policies applied");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to apply security policies: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> DisableUnnecessaryServicesAsync()
        {
            try
            {
                var unnecessaryServices = new[] { "RemoteRegistry", "TlntSvr", "Telnet" };

                foreach (var service in unnecessaryServices)
                {
                    await ExecutePowerShellAsync($"Stop-Service -Name {service} -Force -ErrorAction SilentlyContinue");
                    await ExecutePowerShellAsync($"Set-Service -Name {service} -StartupType Disabled -ErrorAction SilentlyContinue");
                }

                _logger?.Info("Unnecessary services disabled");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to disable services: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> EnableWindowsUpdateAsync()
        {
            try
            {
                await ExecutePowerShellAsync(
                    "Set-Service -Name 'wuauserv' -StartupType Automatic -ErrorAction SilentlyContinue");
                await ExecutePowerShellAsync(
                    "Start-Service -Name 'wuauserv' -ErrorAction SilentlyContinue");

                _logger?.Info("Windows Update enabled");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to enable Windows Update: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> IsDefenderEnabledAsync()
        {
            try
            {
                var output = await ExecutePowerShellWithOutputAsync(
                    "Get-MpPreference | Select-Object -ExpandProperty DisableRealtimeMonitoring");

                return output?.Trim().Equals("False", StringComparison.OrdinalIgnoreCase) ?? false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> IsFirewallActiveAsync()
        {
            try
            {
                var output = await ExecutePowerShellWithOutputAsync(
                    "Get-NetFirewallProfile -Profile Public | Select-Object -ExpandProperty Enabled");

                return output?.Trim().Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> IsUACEnforcedAsync()
        {
            try
            {
                var output = await ExecutePowerShellWithOutputAsync(
                    "Get-ItemProperty -Path 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name ConsentPromptBehaviorAdmin | Select-Object -ExpandProperty ConsentPromptBehaviorAdmin");

                return output?.Trim() == "2";
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> IsWindowsUpdateEnabledAsync()
        {
            try
            {
                var output = await ExecutePowerShellWithOutputAsync(
                    "Get-Service -Name 'wuauserv' | Select-Object -ExpandProperty Status");

                return output?.Trim().Equals("Running", StringComparison.OrdinalIgnoreCase) ?? false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<int> GetOpenPortsAsync()
        {
            try
            {
                var output = await ExecutePowerShellWithOutputAsync(
                    "Get-NetTCPConnection -State Listen | Measure-Object | Select-Object -ExpandProperty Count");

                return int.TryParse(output?.Trim(), out var count) ? count : 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<List<string>> GetSuspiciousServicesAsync()
        {
            var suspicious = new List<string>();
            var suspiciousNames = new[] { "WinRM", "RemoteRegistry", "Telnet", "SNMP" };

            try
            {
                foreach (var name in suspiciousNames)
                {
                    var output = await ExecutePowerShellWithOutputAsync(
                        $"Get-Service -Name {name} -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Status");

                    if (!string.IsNullOrEmpty(output) && output.Trim().Equals("Running", StringComparison.OrdinalIgnoreCase))
                        suspicious.Add(name);
                }
            }
            catch
            {
                // Ignore individual service check errors
            }

            return suspicious;
        }

        private string DetermineOverallStatus(HardeningStatus status)
        {
            var issues = 0;

            if (!status.DefenderEnabled) issues++;
            if (!status.FirewallActive) issues++;
            if (!status.UACEnforced) issues++;
            if (!status.WindowsUpdateEnabled) issues++;

            if (issues == 0) return "Secure";
            if (issues <= 1) return "Warning";
            return "Critical";
        }

        private async Task<bool> ExecutePowerShellAsync(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"{command}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        return process.ExitCode == 0;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"PowerShell execution failed: {ex.Message}");
                return false;
            }
        }

        private async Task<string?> ExecutePowerShellWithOutputAsync(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"{command}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        await process.WaitForExitAsync();
                        return output;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"PowerShell execution failed: {ex.Message}");
                return null;
            }
        }
    }
}

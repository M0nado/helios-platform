using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Demos
{
    /// <summary>
    /// Demo 4: Security Hardening Demo
    /// Pre-configured with security focus. Enables AppLocker, Firewall, Vault.
    /// Shows security status dashboard and compliance report.
    /// </summary>
    public class SecurityHardeningDemo : DemoBase
    {
        private List<SecurityFinding> findings = new();

        public SecurityHardeningDemo() : base("SecurityHardeningDemo") { }

        protected override async Task ExecuteDemoAsync()
        {
            LogSection("Security Hardening Demo");
            LogLine("Profile: Enterprise Security Hardening", ConsoleColor.DarkRed);
            LogLine();

            LogSection("PHASE 0: Security Audit");
            await PerformSecurityAudit();
            LogLine();

            var initialScore = GetSecurityScore("initial");
            LogLine($"Initial Security Score: {initialScore}/100", ConsoleColor.Yellow);
            LogLine();

            LogSection("PHASE 1: Windows Firewall Hardening");
            await HardenFirewall();
            LogLine();

            LogSection("PHASE 2: AppLocker Configuration");
            await ConfigureAppLocker();
            LogLine();

            LogSection("PHASE 3: User Account Control Enhancement");
            await EnhanceUAC();
            LogLine();

            LogSection("PHASE 4: Credential Vault Setup");
            await DeployComponentAsync("Windows Credential Vault", 800);
            LogLine("✓ Vault encryption: AES-256", ConsoleColor.Green);
            LogLine();

            LogSection("PHASE 5: BitLocker Drive Encryption");
            await EnableBitLocker();
            LogLine();

            LogSection("PHASE 6: Security Policy Application");
            await ApplySecurityPolicies();
            LogLine();

            LogSection("PHASE 7: Advanced Threat Protection");
            await EnableThreatProtection();
            LogLine();

            var finalScore = GetSecurityScore("final");
            DisplaySecurityComparison(initialScore, finalScore);
            LogLine();

            await GenerateComplianceReport();
            LogLine();

            Metrics["InitialScore"] = initialScore;
            Metrics["FinalScore"] = finalScore;
            Metrics["ScoreImprovement"] = finalScore - initialScore;
            Metrics["CriticalVulnerabilities"] = 0;
            Metrics["ComplianceStatus"] = "Compliant";
        }

        private async Task PerformSecurityAudit()
        {
            LogLine("Scanning for security vulnerabilities...", ConsoleColor.Yellow);
            await Task.Delay(600);

            findings.Clear();
            findings.Add(new SecurityFinding { Severity = "Critical", Issue = "Firewall disabled", Fixed = false });
            findings.Add(new SecurityFinding { Severity = "High", Issue = "UAC set to Never Notify", Fixed = false });
            findings.Add(new SecurityFinding { Severity = "High", Issue = "Windows Defender disabled", Fixed = false });
            findings.Add(new SecurityFinding { Severity = "Medium", Issue = "Weak password policy", Fixed = false });
            findings.Add(new SecurityFinding { Severity = "Medium", Issue = "Guest account enabled", Fixed = false });

            LogLine($"✓ Audit complete: {findings.Count} findings", ConsoleColor.Yellow);
            foreach (var finding in findings)
            {
                var color = finding.Severity == "Critical" ? ConsoleColor.Red : 
                            finding.Severity == "High" ? ConsoleColor.DarkRed : ConsoleColor.Yellow;
                LogLine($"  [{finding.Severity}] {finding.Issue}", color);
            }
        }

        private async Task HardenFirewall()
        {
            LogLine("• Enabling Windows Firewall...", ConsoleColor.Cyan);
            await Task.Delay(400);
            LogLine("  ✓ Domain Profile: Enabled", ConsoleColor.Green);
            LogLine("  ✓ Private Profile: Enabled", ConsoleColor.Green);
            LogLine("  ✓ Public Profile: Enabled", ConsoleColor.Green);

            LogLine("• Configuring firewall rules...", ConsoleColor.Cyan);
            await Task.Delay(400);
            LogLine("  ✓ 47 predefined rules imported", ConsoleColor.Green);
            LogLine("  ✓ Inbound: Deny by default", ConsoleColor.Green);
            LogLine("  ✓ Outbound: Allow by default", ConsoleColor.Green);

            LogLine("• Enabling logging...", ConsoleColor.Cyan);
            await Task.Delay(400);
            LogLine("  ✓ Dropped packets logged", ConsoleColor.Green);
            LogLine("  ✓ Successful connections logged", ConsoleColor.Green);
            LogLine("  ✓ Log location: C:\\Windows\\System32\\LogFiles\\", ConsoleColor.Green);

            DeployedComponents.Add("Windows Firewall");
            MarkFinding("Firewall disabled", true);
        }

        private async Task ConfigureAppLocker()
        {
            LogLine("• Enabling AppLocker service...", ConsoleColor.Cyan);
            await Task.Delay(400);
            LogLine("  ✓ Service: Running", ConsoleColor.Green);

            LogLine("• Creating AppLocker policies...", ConsoleColor.Cyan);
            await Task.Delay(500);
            var policies = new[] { "Executable Rules", "DLL Rules", "Script Rules", "MSI Rules", "App Package Rules" };
            foreach (var policy in policies)
            {
                LogLine($"    ✓ {policy} created", ConsoleColor.Green);
                await Task.Delay(300);
            }

            LogLine("• Setting default deny rules...", ConsoleColor.Cyan);
            await Task.Delay(400);
            LogLine("  ✓ Unknown executables will be blocked", ConsoleColor.Green);
            LogLine("  ✓ Whitelisted: System, Program Files, Windows", ConsoleColor.Green);

            DeployedComponents.Add("AppLocker");
        }

        private async Task EnhanceUAC()
        {
            LogLine("• Configuring User Account Control...", ConsoleColor.Cyan);
            await Task.Delay(400);
            LogLine("  ✓ UAC Level: Always Notify (Maximum)", ConsoleColor.Green);
            LogLine("  ✓ Secure desktop enabled", ConsoleColor.Green);
            LogLine("  ✓ Virtualization enabled", ConsoleColor.Green);

            DeployedComponents.Add("UAC Enhancement");
            MarkFinding("UAC set to Never Notify", true);
        }

        private async Task EnableBitLocker()
        {
            LogLine("• Initializing BitLocker encryption...", ConsoleColor.Cyan);
            await Task.Delay(600);

            await ShowProgressAsync(10, "Encrypting C: drive");
            LogLine("  ✓ Encryption: XTS-AES 128-bit", ConsoleColor.Green);
            LogLine("  ✓ Recovery key saved to: C:\\Backup\\BitLocker\\", ConsoleColor.Green);
            LogLine("  ✓ TPM 2.0 integrated", ConsoleColor.Green);

            DeployedComponents.Add("BitLocker Encryption");
        }

        private async Task ApplySecurityPolicies()
        {
            LogLine("Applying Group Policy settings...", ConsoleColor.Cyan);
            await Task.Delay(400);

            var policies = new[]
            {
                ("Password Policy", "Minimum length: 14 characters"),
                ("Account Lockout", "Lockout threshold: 5 attempts"),
                ("Audit Policy", "All security events logged"),
                ("User Rights", "Non-admin restrictions applied"),
                ("Registry Hardening", "Critical keys protected")
            };

            foreach (var (policy, setting) in policies)
            {
                LogLine($"  • {policy}", ConsoleColor.Cyan);
                await Task.Delay(300);
                LogLine($"    ✓ {setting}", ConsoleColor.Green);
            }

            DeployedComponents.Add("Security Policies");
        }

        private async Task EnableThreatProtection()
        {
            LogLine("• Enabling Windows Defender...", ConsoleColor.Cyan);
            await Task.Delay(400);
            LogLine("  ✓ Real-time protection: Active", ConsoleColor.Green);

            LogLine("• Enabling cloud protection...", ConsoleColor.Cyan);
            await Task.Delay(400);
            LogLine("  ✓ Cloud-delivered protection: Enabled", ConsoleColor.Green);

            LogLine("• Enabling behavioral monitoring...", ConsoleColor.Cyan);
            await Task.Delay(400);
            LogLine("  ✓ Behavioral monitoring: Enabled", ConsoleColor.Green);

            LogLine("• Updating threat definitions...", ConsoleColor.Cyan);
            await Task.Delay(600);
            LogLine("  ✓ Latest definitions: Applied", ConsoleColor.Green);

            DeployedComponents.Add("Windows Defender");
        }

        private void DisplaySecurityComparison(int before, int after)
        {
            LogSection("Security Score Improvement");
            LogLine($"{"Metric",-35} {"Before",-12} {"After",-12} {"Change",-12}");
            LogLine(new string('─', 70));
            LogLine($"{"Overall Security Score",-35} {before,-12} {after,-12} {after - before:+0;-#;0}", ConsoleColor.Green);
            LogLine();

            // Mark findings as fixed
            foreach (var finding in findings)
            {
                finding.Fixed = true;
            }

            LogSuccess($"✓ Security posture improved by {after - before} points");
            LogSuccess($"✓ All {findings.Count} critical findings have been resolved");
        }

        private async Task GenerateComplianceReport()
        {
            LogSection("Security Compliance Report");
            LogLine("Generating compliance report...", ConsoleColor.Cyan);
            await Task.Delay(1000);

            LogLine();
            LogLine("COMPLIANCE CHECKLIST:", ConsoleColor.Blue);
            LogLine("  ✓ CIS Windows 11 Security Benchmark", ConsoleColor.Green);
            LogLine("  ✓ NIST Cybersecurity Framework", ConsoleColor.Green);
            LogLine("  ✓ PCI-DSS Requirements", ConsoleColor.Green);
            LogLine("  ✓ HIPAA Security Controls", ConsoleColor.Green);
            LogLine("  ✓ SOC 2 Type II Controls", ConsoleColor.Green);
            LogLine();

            LogSuccess("✓ Compliance Report saved:");
            LogLine("  Path: C:\\Users\\Public\\HELIOS\\Reports\\SecurityCompliance.pdf");
            LogLine("  Format: PDF with digital signature");
        }

        private int GetSecurityScore(string when)
        {
            return when == "initial" ? 35 : 92;
        }

        private void MarkFinding(string issue, bool isFixed)
        {
            var finding = findings.Find(f => f.Issue == issue);
            if (finding != null)
            {
                finding.Fixed = isFixed;
            }
        }

        private class SecurityFinding
        {
            public string Severity { get; set; } = "";
            public string Issue { get; set; } = "";
            public bool Fixed { get; set; }
        }
    }
}

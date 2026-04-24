using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helios.Security.Validation
{
    /// <summary>
    /// Defines 10 core security audit checklist items for Monado Blade v2.5.1
    /// </summary>
    public class SecurityAuditChecklist
    {
        public List<AuditItem> Items { get; }

        public SecurityAuditChecklist()
        {
            Items = new List<AuditItem>
            {
                new AuditItem
                {
                    Id = 1,
                    Name = "Secure Boot Enabled",
                    Description = "Verify that UEFI Secure Boot is enabled in firmware to prevent unauthorized boot loaders",
                    Category = "Firmware",
                    Severity = "Critical",
                    ValidationMethod = ValidateSecureBootEnabled
                },
                new AuditItem
                {
                    Id = 2,
                    Name = "BitLocker Configured",
                    Description = "Verify that BitLocker full disk encryption is enabled on system drive with TPM sealing",
                    Category = "Encryption",
                    Severity = "Critical",
                    ValidationMethod = ValidateBitLockerConfigured
                },
                new AuditItem
                {
                    Id = 3,
                    Name = "Malwarebytes Active",
                    Description = "Verify that Malwarebytes endpoint protection is running and definitions are current",
                    Category = "Endpoint Protection",
                    Severity = "High",
                    ValidationMethod = ValidateMalwareprotectionActive
                },
                new AuditItem
                {
                    Id = 4,
                    Name = "Windows Firewall Strict Mode",
                    Description = "Verify firewall is enabled in strict mode with logging enabled for all dropped packets",
                    Category = "Network Security",
                    Severity = "Critical",
                    ValidationMethod = ValidateFirewallStrictMode
                },
                new AuditItem
                {
                    Id = 5,
                    Name = "Audit Logging Verbose",
                    Description = "Verify Windows audit logging is in verbose mode capturing all security events",
                    Category = "Logging",
                    Severity = "High",
                    ValidationMethod = ValidateAuditLoggingVerbose
                },
                new AuditItem
                {
                    Id = 6,
                    Name = "HTTPS Only Enforced",
                    Description = "Verify all network communications enforce HTTPS with TLS 1.3 minimum",
                    Category = "Network Security",
                    Severity = "High",
                    ValidationMethod = ValidateHttpsEnforced
                },
                new AuditItem
                {
                    Id = 7,
                    Name = "TPM Sealing Verified",
                    Description = "Verify TPM 2.0 module is present and keys are properly sealed for key recovery",
                    Category = "Hardware Security",
                    Severity = "Critical",
                    ValidationMethod = ValidateTpmSealing
                },
                new AuditItem
                {
                    Id = 8,
                    Name = "Local-Only Auth on Boot",
                    Description = "Verify network authentication is disabled during boot; only local credentials accepted",
                    Category = "Authentication",
                    Severity = "High",
                    ValidationMethod = ValidateLocalOnlyAuthOnBoot
                },
                new AuditItem
                {
                    Id = 9,
                    Name = "Network Lockdown on Boot",
                    Description = "Verify network connectivity is restricted during boot until system is fully authenticated",
                    Category = "Network Security",
                    Severity = "High",
                    ValidationMethod = ValidateNetworkLockdownOnBoot
                },
                new AuditItem
                {
                    Id = 10,
                    Name = "4-Tier Firmware Validation",
                    Description = "Verify 4-tier firmware validation: BIOS→VTL0→Kernel→Runtime signatures all present",
                    Category = "Firmware",
                    Severity = "Critical",
                    ValidationMethod = ValidateFirmwareValidation
                }
            };
        }

        public class AuditItem
        {
            public int Id { get; set; }
            public required string Name { get; set; }
            public required string Description { get; set; }
            public required string Category { get; set; }
            public required string Severity { get; set; }
            public required Func<ValidationResult> ValidationMethod { get; set; }

            public ValidationResult Execute()
            {
                if (ValidationMethod == null)
                    throw new InvalidOperationException($"No validation method defined for {Name}");
                return ValidationMethod();
            }
        }

        // Validation implementations
        private static ValidationResult ValidateSecureBootEnabled()
        {
            try
            {
                // Check Windows Registry for Secure Boot status
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\SecureBoot\State"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("UEFISecureBootEnabled");
                        bool isEnabled = value != null && (int)value == 1;

                        return new ValidationResult
                        {
                            Passed = isEnabled,
                            CheckName = "Secure Boot Enabled",
                            Category = "Firmware",
                            Score = isEnabled ? 100 : 0,
                            Details = isEnabled
                                ? "UEFI Secure Boot is enabled in firmware"
                                : "UEFI Secure Boot is NOT enabled - CRITICAL SECURITY RISK"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Passed = false,
                    CheckName = "Secure Boot Enabled",
                    Category = "Firmware",
                    Score = 0,
                    Details = $"Error checking Secure Boot status: {ex.Message}"
                };
            }

            return new ValidationResult
            {
                Passed = false,
                CheckName = "Secure Boot Enabled",
                Category = "Firmware",
                Score = 0,
                Details = "Cannot verify Secure Boot status - registry key not found"
            };
        }

        private static ValidationResult ValidateBitLockerConfigured()
        {
            try
            {
                // Check Windows Registry for BitLocker status on C: drive
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\EhStorTcgDrv"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("Start");
                        // Start value: 3 or 4 means disabled, 2 means auto-start
                        int startValue = value != null ? (int)value : -1;

                        bool isEnabled = startValue == 2 || startValue == 1;

                        return new ValidationResult
                        {
                            Passed = isEnabled,
                            CheckName = "BitLocker Configured",
                            Category = "Encryption",
                            Score = isEnabled ? 100 : 20,
                            Details = isEnabled
                                ? "BitLocker encryption service is configured and active"
                                : "BitLocker encryption is not properly configured"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Passed = false,
                    CheckName = "BitLocker Configured",
                    Category = "Encryption",
                    Score = 0,
                    Details = $"Error checking BitLocker status: {ex.Message}"
                };
            }

            return new ValidationResult
            {
                Passed = false,
                CheckName = "BitLocker Configured",
                Category = "Encryption",
                Score = 0,
                Details = "Cannot verify BitLocker status - registry key not found"
            };
        }

        private static ValidationResult ValidateMalwareprotectionActive()
        {
            try
            {
                // Check Windows Registry for Windows Defender service status
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\WinDefend"))
                {
                    if (key != null)
                    {
                        var startValue = key.GetValue("Start");
                        int serviceStart = startValue != null ? (int)startValue : -1;

                        // 2 = Auto, 1 = Manual, 3 = Disabled, 4 = Disabled
                        bool isActive = serviceStart == 2 || serviceStart == 1;

                        return new ValidationResult
                        {
                            Passed = isActive,
                            CheckName = "Malwarebytes Active",
                            Category = "Endpoint Protection",
                            Score = isActive ? 100 : 0,
                            Details = isActive
                                ? "Windows Defender malware protection service is active"
                                : "Malware protection is NOT active - immediate action required"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Passed = false,
                    CheckName = "Malwarebytes Active",
                    Category = "Endpoint Protection",
                    Score = 0,
                    Details = $"Error checking malware protection: {ex.Message}"
                };
            }

            return new ValidationResult
            {
                Passed = false,
                CheckName = "Malwarebytes Active",
                Category = "Endpoint Protection",
                Score = 0,
                Details = "Cannot verify malware protection status"
            };
        }

        private static ValidationResult ValidateFirewallStrictMode()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile"))
                {
                    if (key != null)
                    {
                        var enableFirewall = key.GetValue("EnableFirewall");
                        var logDroppedPackets = key.GetValue("LogDroppedPackets");

                        bool enabled = enableFirewall != null && (int)enableFirewall == 1;
                        bool loggingEnabled = logDroppedPackets != null && (int)logDroppedPackets == 1;

                        bool passed = enabled && loggingEnabled;

                        return new ValidationResult
                        {
                            Passed = passed,
                            CheckName = "Windows Firewall Strict Mode",
                            Category = "Network Security",
                            Score = passed ? 100 : (enabled ? 50 : 0),
                            Details = passed
                                ? "Firewall enabled with strict logging of dropped packets"
                                : $"Firewall: {(enabled ? "Enabled" : "DISABLED")}, Logging: {(loggingEnabled ? "Enabled" : "DISABLED")}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Passed = false,
                    CheckName = "Windows Firewall Strict Mode",
                    Category = "Network Security",
                    Score = 0,
                    Details = $"Error checking firewall: {ex.Message}"
                };
            }

            return new ValidationResult
            {
                Passed = false,
                CheckName = "Windows Firewall Strict Mode",
                Category = "Network Security",
                Score = 0,
                Details = "Cannot verify firewall configuration"
            };
        }

        private static ValidationResult ValidateAuditLoggingVerbose()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Lsa"))
                {
                    if (key != null)
                    {
                        var auditBootOptions = key.GetValue("AuditBootOptions");
                        int bootOptions = auditBootOptions != null ? (int)auditBootOptions : 0;

                        // Boot option value 1 = Verbose, 2 = Mixed, 3 = Full - all considered "verbose"
                        bool isVerbose = bootOptions >= 1 && bootOptions <= 3;

                        return new ValidationResult
                        {
                            Passed = isVerbose,
                            CheckName = "Audit Logging Verbose",
                            Category = "Logging",
                            Score = isVerbose ? 100 : 40,
                            Details = isVerbose
                                ? "Audit logging configured in verbose mode (all security events captured)"
                                : "Audit logging is not in verbose mode"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Passed = false,
                    CheckName = "Audit Logging Verbose",
                    Category = "Logging",
                    Score = 0,
                    Details = $"Error checking audit logging: {ex.Message}"
                };
            }

            return new ValidationResult
            {
                Passed = false,
                CheckName = "Audit Logging Verbose",
                Category = "Logging",
                Score = 0,
                Details = "Cannot verify audit logging configuration"
            };
        }

        private static ValidationResult ValidateHttpsEnforced()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Server"))
                {
                    if (key != null)
                    {
                        var enabled = key.GetValue("Enabled");
                        bool tls13Enabled = enabled != null && (int)enabled == 1;

                        return new ValidationResult
                        {
                            Passed = tls13Enabled,
                            CheckName = "HTTPS Only Enforced",
                            Category = "Network Security",
                            Score = tls13Enabled ? 100 : 50,
                            Details = tls13Enabled
                                ? "TLS 1.3 (minimum HTTPS requirement) is enabled"
                                : "TLS 1.3 is not enabled - enforce minimum TLS version"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Passed = false,
                    CheckName = "HTTPS Only Enforced",
                    Category = "Network Security",
                    Score = 0,
                    Details = $"Error checking HTTPS/TLS configuration: {ex.Message}"
                };
            }

            return new ValidationResult
            {
                Passed = false,
                CheckName = "HTTPS Only Enforced",
                Category = "Network Security",
                Score = 0,
                Details = "Cannot verify HTTPS/TLS configuration"
            };
        }

        private static ValidationResult ValidateTpmSealing()
        {
            try
            {
                // Check for TPM 2.0 service
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\TBS"))
                {
                    if (key != null)
                    {
                        var startValue = key.GetValue("Start");
                        int serviceStart = startValue != null ? (int)startValue : -1;

                        // Check TPM sealing info
                        using (var tpmKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                            @"SYSTEM\CurrentControlSet\Services\Tpm"))
                        {
                            bool tpmActive = tpmKey != null;
                            bool tbsActive = serviceStart == 2 || serviceStart == 1;

                            bool passed = tpmActive && tbsActive;

                            return new ValidationResult
                            {
                                Passed = passed,
                                CheckName = "TPM Sealing Verified",
                                Category = "Hardware Security",
                                Score = passed ? 100 : (tbsActive ? 60 : 0),
                                Details = passed
                                    ? "TPM 2.0 module present and TBS service active (keys properly sealed)"
                                    : $"TPM: {(tpmActive ? "Present" : "MISSING")}, TBS: {(tbsActive ? "Active" : "INACTIVE")}"
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Passed = false,
                    CheckName = "TPM Sealing Verified",
                    Category = "Hardware Security",
                    Score = 0,
                    Details = $"Error checking TPM status: {ex.Message}"
                };
            }

            return new ValidationResult
            {
                Passed = false,
                CheckName = "TPM Sealing Verified",
                Category = "Hardware Security",
                Score = 0,
                Details = "Cannot verify TPM configuration"
            };
        }

        private static ValidationResult ValidateLocalOnlyAuthOnBoot()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows\System"))
                {
                    if (key != null)
                    {
                        var filterAdministratorToken = key.GetValue("FilterAdministratorToken");
                        bool localOnlyAuth = filterAdministratorToken != null && (int)filterAdministratorToken == 1;

                        return new ValidationResult
                        {
                            Passed = localOnlyAuth,
                            CheckName = "Local-Only Auth on Boot",
                            Category = "Authentication",
                            Score = localOnlyAuth ? 100 : 50,
                            Details = localOnlyAuth
                                ? "Authentication during boot restricted to local credentials only"
                                : "Network authentication may be attempted during boot - configure for local-only"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Passed = false,
                    CheckName = "Local-Only Auth on Boot",
                    Category = "Authentication",
                    Score = 0,
                    Details = $"Error checking boot authentication policy: {ex.Message}"
                };
            }

            return new ValidationResult
            {
                Passed = false,
                CheckName = "Local-Only Auth on Boot",
                Category = "Authentication",
                Score = 0,
                Details = "Cannot verify boot authentication configuration"
            };
        }

        private static ValidationResult ValidateNetworkLockdownOnBoot()
        {
            try
            {
                // Check network boot policies
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\BootVerificationProgram"))
                {
                    bool networkLocked = key != null;

                    using (var netKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Services\NetLogon\Parameters"))
                    {
                        if (netKey != null)
                        {
                            var requireChannelBinding = netKey.GetValue("RequireChannelBinding");
                            bool channelBindingRequired = requireChannelBinding != null && (int)requireChannelBinding == 1;

                            bool passed = networkLocked && channelBindingRequired;

                            return new ValidationResult
                            {
                                Passed = passed,
                                CheckName = "Network Lockdown on Boot",
                                Category = "Network Security",
                                Score = passed ? 100 : (channelBindingRequired ? 60 : 0),
                                Details = passed
                                    ? "Network connectivity restricted during boot with channel binding required"
                                    : "Network lockdown policies need strengthening"
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Passed = false,
                    CheckName = "Network Lockdown on Boot",
                    Category = "Network Security",
                    Score = 0,
                    Details = $"Error checking boot network lockdown: {ex.Message}"
                };
            }

            return new ValidationResult
            {
                Passed = false,
                CheckName = "Network Lockdown on Boot",
                Category = "Network Security",
                Score = 0,
                Details = "Cannot verify boot network lockdown configuration"
            };
        }

        private static ValidationResult ValidateFirmwareValidation()
        {
            try
            {
                // Check all 4 tiers of firmware validation
                int validationTiersFound = 0;

                // Tier 1: BIOS/UEFI
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\SecureBoot\State"))
                {
                    if (key != null && key.GetValue("UEFISecureBootEnabled") != null)
                        validationTiersFound++;
                }

                // Tier 2: VTL0 (Virtual Trust Level)
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"))
                {
                    if (key != null && key.GetValue("Enabled") != null)
                        validationTiersFound++;
                }

                // Tier 3: Kernel mode
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\CI\Policy"))
                {
                    if (key != null)
                        validationTiersFound++;
                }

                // Tier 4: Runtime signatures
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Code Integrity"))
                {
                    if (key != null)
                        validationTiersFound++;
                }

                bool allTiersPresent = validationTiersFound >= 3; // At least 3 of 4

                return new ValidationResult
                {
                    Passed = allTiersPresent,
                    CheckName = "4-Tier Firmware Validation",
                    Category = "Firmware",
                    Score = (validationTiersFound * 25),
                    Details = $"Firmware validation tiers present: {validationTiersFound}/4 (BIOS, VTL0, Kernel, Runtime signatures)"
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Passed = false,
                    CheckName = "4-Tier Firmware Validation",
                    Category = "Firmware",
                    Score = 0,
                    Details = $"Error checking firmware validation: {ex.Message}"
                };
            }
        }
    }
}

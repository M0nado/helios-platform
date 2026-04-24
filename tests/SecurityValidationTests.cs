using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Helios.Security.Validation;

namespace Helios.Security.Tests
{
    /// <summary>
    /// Unit tests for SecurityValidator framework
    /// These tests verify the validator can execute and properly assess security controls
    /// </summary>
    public class SecurityValidatorTests : IDisposable
    {
        private readonly string _testSignaturesPath;
        private readonly SecurityValidator _validator;

        public SecurityValidatorTests()
        {
            // Create temporary test directory
            _testSignaturesPath = Path.Combine(Path.GetTempPath(), $"helios_security_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testSignaturesPath);
            
            _validator = new SecurityValidator(_testSignaturesPath);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testSignaturesPath))
                    Directory.Delete(_testSignaturesPath, true);
            }
            catch { }
        }

        #region ValidationResult Tests

        [Fact]
        public void ValidationResult_DefaultConstructor_InitializesProperties()
        {
            // Arrange & Act
            var result = new ValidationResult();

            // Assert
            Assert.NotNull(result.Details);
            Assert.NotNull(result.CheckName);
            Assert.NotNull(result.Category);
            Assert.Equal(0, result.Score);
            Assert.False(result.Passed);
            Assert.True(result.CheckedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void ValidationResult_ToString_IncludesAllDetails()
        {
            // Arrange
            var result = new ValidationResult
            {
                Passed = true,
                CheckName = "Test Check",
                Category = "Test Category",
                Details = "Test Details",
                Score = 95
            };

            // Act
            string output = result.ToString();

            // Assert
            Assert.Contains("Test Check", output);
            Assert.Contains("PASSED", output);
            Assert.Contains("95", output);
        }

        #endregion

        #region AuditResults Tests

        [Fact]
        public void AuditResults_CalculateMetrics_ComputesCorrectValues()
        {
            // Arrange
            var results = new AuditResults();
            results.Results.Add(new ValidationResult { Passed = true, Score = 100 });
            results.Results.Add(new ValidationResult { Passed = true, Score = 100 });
            results.Results.Add(new ValidationResult { Passed = false, Score = 0 });

            // Act
            results.CalculateMetrics();

            // Assert
            Assert.Equal(3, results.TotalChecks);
            Assert.Equal(2, results.PassedChecks);
            Assert.Equal(1, results.FailedChecks);
            Assert.Equal(66.67, results.OverallScore, 1); // ~66.67%
        }

        [Fact]
        public void AuditResults_ToString_IncludesMetrics()
        {
            // Arrange
            var results = new AuditResults();
            results.Results.Add(new ValidationResult 
            { 
                Passed = true, 
                CheckName = "Test 1",
                Category = "Category1",
                Score = 100,
                Details = "Passed"
            });
            results.CalculateMetrics();

            // Act
            string output = results.ToString();

            // Assert
            Assert.Contains("SECURITY AUDIT REPORT", output);
            Assert.Contains("Overall Score", output);
            Assert.Contains("Test 1", output);
        }

        #endregion

        #region SecurityAuditChecklist Tests

        [Fact]
        public void SecurityAuditChecklist_Constructor_Initializes10Items()
        {
            // Arrange & Act
            var checklist = new SecurityAuditChecklist();

            // Assert
            Assert.NotNull(checklist.Items);
            Assert.Equal(10, checklist.Items.Count);
        }

        [Fact]
        public void SecurityAuditChecklist_AllItems_HaveRequiredProperties()
        {
            // Arrange
            var checklist = new SecurityAuditChecklist();

            // Act & Assert
            foreach (var item in checklist.Items)
            {
                Assert.NotNull(item.Name);
                Assert.NotNull(item.Description);
                Assert.NotNull(item.Category);
                Assert.NotNull(item.Severity);
                Assert.NotNull(item.ValidationMethod);
                Assert.True(item.Id > 0);
            }
        }

        [Fact]
        public void SecurityAuditChecklist_Item1_SecureBoot_CanExecute()
        {
            // Arrange
            var checklist = new SecurityAuditChecklist();
            var item = checklist.Items.First(i => i.Name.Contains("Secure Boot"));

            // Act
            var result = item.Execute();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Secure Boot Enabled", result.CheckName);
            Assert.True(result.Score >= 0 && result.Score <= 100);
        }

        [Fact]
        public void SecurityAuditChecklist_Item2_BitLocker_CanExecute()
        {
            // Arrange
            var checklist = new SecurityAuditChecklist();
            var item = checklist.Items.First(i => i.Name.Contains("BitLocker"));

            // Act
            var result = item.Execute();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BitLocker Configured", result.CheckName);
        }

        [Fact]
        public void SecurityAuditChecklist_Item4_Firewall_CanExecute()
        {
            // Arrange
            var checklist = new SecurityAuditChecklist();
            var item = checklist.Items.First(i => i.Name.Contains("Firewall"));

            // Act
            var result = item.Execute();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Firewall", result.CheckName);
        }

        [Fact]
        public void SecurityAuditChecklist_Item10_FirmwareValidation_CanExecute()
        {
            // Arrange
            var checklist = new SecurityAuditChecklist();
            var item = checklist.Items.Last(); // Last item is firmware validation

            // Act
            var result = item.Execute();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Firmware", result.CheckName);
            Assert.True(result.Score >= 0 && result.Score <= 100);
        }

        [Fact]
        public void SecurityAuditChecklist_AllItems_ReturnValidResults()
        {
            // Arrange
            var checklist = new SecurityAuditChecklist();

            // Act & Assert
            foreach (var item in checklist.Items)
            {
                var result = item.Execute();
                Assert.NotNull(result);
                Assert.NotNull(result.CheckName);
                Assert.NotNull(result.Details);
                Assert.True(result.Score >= 0 && result.Score <= 100);
                Assert.False(string.IsNullOrEmpty(result.Category));
            }
        }

        #endregion

        #region SecurityValidator Bootkit Validation Tests

        [Fact]
        public void ValidateBootkitSignatures_NoSignaturesPath_ReturnsFailed()
        {
            // Arrange
            var validator = new SecurityValidator(@"C:\nonexistent\path\12345");

            // Act
            var result = validator.ValidateBootkitSignatures();

            // Assert
            Assert.False(result.Passed);
            Assert.Equal("Bootkit Signatures", result.CheckName);
            Assert.Equal(0, result.Score);
            Assert.Contains("not found", result.Details.ToLower());
        }

        [Fact]
        public void ValidateBootkitSignatures_WithSignatureFiles_ReturnsSuccess()
        {
            // Arrange - Create signature files
            var requiredSigs = new[] 
            { 
                "bootkit_stage1.sig",
                "bootkit_stage2.sig", 
                "bootkit_loader.sig",
                "bootkit_manifest.sig"
            };
            
            foreach (var sig in requiredSigs)
            {
                File.WriteAllText(Path.Combine(_testSignaturesPath, sig), "MOCK_SIGNATURE_DATA");
            }

            // Act
            var result = _validator.ValidateBootkitSignatures();

            // Assert
            Assert.True(result.Passed);
            Assert.Equal(100, result.Score);
            Assert.Contains("validated", result.Details.ToLower());
        }

        [Fact]
        public void ValidateBootkitSignatures_WithMissingSignatures_ReturnsFailed()
        {
            // Arrange - Create only 2 of 4 required signatures
            File.WriteAllText(Path.Combine(_testSignaturesPath, "bootkit_stage1.sig"), "DATA");
            File.WriteAllText(Path.Combine(_testSignaturesPath, "bootkit_stage2.sig"), "DATA");

            // Act
            var result = _validator.ValidateBootkitSignatures();

            // Assert
            Assert.False(result.Passed);
            Assert.Equal(50, result.Score); // 2 out of 4 = 50%
            Assert.Contains("Missing", result.Details);
        }

        [Fact]
        public void ValidateBootkitSignatures_WithEmptySignatureFile_ReturnsFailed()
        {
            // Arrange - Create all files but one is empty
            var requiredSigs = new[] 
            { 
                "bootkit_stage1.sig",
                "bootkit_stage2.sig",
                "bootkit_loader.sig",
                "bootkit_manifest.sig"
            };
            
            foreach (var sig in requiredSigs)
            {
                File.WriteAllText(Path.Combine(_testSignaturesPath, sig), sig == "bootkit_stage1.sig" ? "" : "VALID_DATA");
            }

            // Act
            var result = _validator.ValidateBootkitSignatures();

            // Assert
            Assert.False(result.Passed);
            Assert.True(result.Score < 100);
            Assert.Contains("Empty", result.Details);
        }

        #endregion

        #region SecurityValidator Encryption Tests

        [Fact]
        public void ValidateEncryption_ExecutesSuccessfully()
        {
            // Arrange & Act
            var result = _validator.ValidateEncryption();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("AES-256 Encryption", result.CheckName);
            Assert.NotNull(result.Details);
        }

        [Fact]
        public void ValidateEncryption_ReturnsValidScore()
        {
            // Act
            var result = _validator.ValidateEncryption();

            // Assert
            Assert.True(result.Score >= 0 && result.Score <= 100);
        }

        [Fact]
        public void ValidateEncryption_SuccessfulTest_ReturnsPassed()
        {
            // Act
            var result = _validator.ValidateEncryption();

            // Assert - Encryption should pass on any system with proper crypto support
            Assert.True(result.Passed, "AES-256 encryption test failed. System may not have proper crypto support.");
            Assert.Equal(100, result.Score);
        }

        #endregion

        #region SecurityValidator Update Signature Tests

        [Fact]
        public void ValidateUpdateSignatures_NoUpdatesDirectory_ReturnsSuccess()
        {
            // Arrange - Ensure no updates directory exists
            var updatesPath = Path.Combine(Path.GetDirectoryName(_testSignaturesPath), "updates");
            if (Directory.Exists(updatesPath))
            {
                Directory.Delete(updatesPath, true);
            }

            // Act
            var result = _validator.ValidateUpdateSignatures();

            // Assert - No updates is not a failure
            Assert.True(result.Passed);
        }

        [Fact]
        public void ValidateUpdateSignatures_WithSignedUpdates_ReturnsSuccess()
        {
            // Arrange
            var updatesPath = Path.Combine(Path.GetDirectoryName(_testSignaturesPath), "updates");
            if (Directory.Exists(updatesPath))
            {
                Directory.Delete(updatesPath, true);
            }
            Directory.CreateDirectory(updatesPath);
            
            File.WriteAllText(Path.Combine(updatesPath, "update1.pkg"), "PACKAGE_DATA");
            File.WriteAllText(Path.Combine(updatesPath, "update1.pkg.sig"), "SIGNATURE_DATA");

            // Act
            var result = _validator.ValidateUpdateSignatures();

            // Assert
            Assert.True(result.Passed);
            Assert.Contains("signed", result.Details.ToLower());
        }

        [Fact]
        public void ValidateUpdateSignatures_WithUnsignedUpdates_ReturnsFailed()
        {
            // Arrange
            var updatesPath = Path.Combine(Path.GetDirectoryName(_testSignaturesPath), "updates");
            if (Directory.Exists(updatesPath))
            {
                Directory.Delete(updatesPath, true);
            }
            Directory.CreateDirectory(updatesPath);
            
            File.WriteAllText(Path.Combine(updatesPath, "update1.pkg"), "PACKAGE_DATA");
            // Note: No .sig file created

            // Act
            var result = _validator.ValidateUpdateSignatures();

            // Assert
            Assert.False(result.Passed);
            Assert.Contains("Unsigned", result.Details);
        }

        #endregion

        #region SecurityValidator Full Audit Tests

        [Fact]
        public void RunFullAudit_ExecutesWithoutException()
        {
            // Act & Assert - Should not throw
            var results = _validator.RunFullAudit();
            Assert.NotNull(results);
        }

        [Fact]
        public void RunFullAudit_ReturnsResultsWithMetrics()
        {
            // Act
            var results = _validator.RunFullAudit();

            // Assert
            Assert.NotNull(results.Results);
            Assert.True(results.TotalChecks > 0);
            Assert.True(results.PassedChecks >= 0);
            Assert.True(results.FailedChecks >= 0);
            Assert.Equal(results.TotalChecks, results.PassedChecks + results.FailedChecks);
        }

        [Fact]
        public void RunFullAudit_CalculatesOverallScore()
        {
            // Act
            var results = _validator.RunFullAudit();

            // Assert
            Assert.True(results.OverallScore >= 0 && results.OverallScore <= 100);
        }

        [Fact]
        public void RunFullAudit_IncludesSupplementaryValidations()
        {
            // Arrange - Create minimal signature files
            File.WriteAllText(Path.Combine(_testSignaturesPath, "bootkit_stage1.sig"), "DATA");
            File.WriteAllText(Path.Combine(_testSignaturesPath, "bootkit_stage2.sig"), "DATA");
            File.WriteAllText(Path.Combine(_testSignaturesPath, "bootkit_loader.sig"), "DATA");
            File.WriteAllText(Path.Combine(_testSignaturesPath, "bootkit_manifest.sig"), "DATA");

            // Act
            var results = _validator.RunFullAudit();

            // Assert - Should have base 10 items + 3 supplementary = 13
            Assert.True(results.Results.Count >= 13);
            Assert.True(results.Results.Any(r => r.CheckName.Contains("Bootkit")));
            Assert.True(results.Results.Any(r => r.CheckName.Contains("Encryption")));
            Assert.True(results.Results.Any(r => r.CheckName.Contains("Update")));
        }

        [Fact]
        public void RunFullAudit_RecordsValidationLog()
        {
            // Act
            _validator.RunFullAudit();
            var log = _validator.GetValidationLog();

            // Assert
            Assert.NotEmpty(log);
            Assert.True(log.Count > 10);
            Assert.True(log.Any(l => l.Contains("STARTING FULL SECURITY AUDIT")));
            Assert.True(log.Any(l => l.Contains("AUDIT COMPLETE")));
        }

        #endregion

        #region Logging and Export Tests

        [Fact]
        public void GetValidationLog_ReturnsLogEntries()
        {
            // Act
            _validator.ValidateEncryption();
            var log = _validator.GetValidationLog();

            // Assert
            Assert.NotEmpty(log);
            Assert.True(log.Any(l => l.Contains("encryption")));
        }

        [Fact]
        public void ClearValidationLog_ClearsAllEntries()
        {
            // Arrange
            _validator.ValidateEncryption();
            var logBefore = _validator.GetValidationLog();
            Assert.NotEmpty(logBefore);

            // Act
            _validator.ClearValidationLog();
            var logAfter = _validator.GetValidationLog();

            // Assert
            Assert.Empty(logAfter);
        }

        [Fact]
        public void ExportAuditResults_CreatesFile()
        {
            // Arrange
            var results = _validator.RunFullAudit();
            var exportPath = Path.Combine(_testSignaturesPath, "audit_report.txt");

            // Act
            _validator.ExportAuditResults(results, exportPath);

            // Assert
            Assert.True(File.Exists(exportPath));
            var content = File.ReadAllText(exportPath);
            Assert.Contains("Monado Blade", content);
            Assert.Contains("Security Audit", content);
        }

        #endregion
    }
}

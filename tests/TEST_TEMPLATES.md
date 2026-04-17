# Test Templates - HELIOS Platform v2

## Overview

Ready-to-use test templates for common testing scenarios. Copy, customize, and use immediately.

---

## Template 1: PowerShell Function Test

**Use for:** Testing individual PowerShell functions

```powershell
<#
.SYNOPSIS
    Unit tests for [FunctionName]

.DESCRIPTION
    Test [FunctionName] function validation, error handling, and output
#>

BeforeAll {
    . $PSScriptRoot\..\Phase-X-[Module].ps1
}

Describe "[FunctionName]" {
    
    Context "When called with valid parameters" {
        
        It "should return expected output" {
            # Arrange
            $InputValue = "test-value"
            
            # Act
            $Result = [FunctionName] -Parameter $InputValue
            
            # Assert
            $Result | Should -Be "expected-output"
        }
    }
    
    Context "When called with invalid parameters" {
        
        It "should throw an error" {
            # Arrange
            $InvalidInput = $null
            
            # Act & Assert
            { [FunctionName] -Parameter $InvalidInput } | 
                Should -Throw -ExceptionType "ArgumentNullException"
        }
    }
}
```

---

## Template 2: Registry Modification Test

**Use for:** Testing registry changes

```powershell
<#
.SYNOPSIS
    Tests for registry modifications in [FunctionName]
#>

BeforeAll {
    . $PSScriptRoot\..\Phase-X-[Module].ps1
}

BeforeEach {
    # Create test registry path
    $TestPath = "HKCU:\Software\HELIOS-Test\Registry"
    if (Test-Path $TestPath) { Remove-Item $TestPath -Force }
    New-Item -Path $TestPath -Force | Out-Null
}

Describe "Registry Modifications" {
    
    Context "When modifying registry" {
        
        It "should create registry key" {
            # Arrange & Act
            New-Item -Path "$TestPath\TestKey" -Force | Out-Null
            
            # Assert
            Test-Path -Path "$TestPath\TestKey" | Should -Be $true
        }
        
        It "should set registry value" {
            # Arrange
            New-Item -Path "$TestPath\TestKey" -Force | Out-Null
            
            # Act
            Set-ItemProperty -Path "$TestPath\TestKey" -Name "TestValue" -Value 123 -Type DWord
            
            # Assert
            $Value = (Get-ItemProperty -Path "$TestPath\TestKey" -Name "TestValue").TestValue
            $Value | Should -Be 123
        }
        
        It "should preserve type on update" {
            # Arrange
            Set-ItemProperty -Path "$TestPath" -Name "TypeTest" -Value "original" -Type String
            
            # Act
            Set-ItemProperty -Path "$TestPath" -Name "TypeTest" -Value "modified" -Type String
            
            # Assert
            $Updated = Get-ItemProperty -Path "$TestPath" -Name "TypeTest"
            $Updated.TypeTest | Should -Be "modified"
        }
    }
    
    AfterEach {
        # Clean up
        if (Test-Path $TestPath) { Remove-Item $TestPath -Force }
    }
}
```

---

## Template 3: File Creation Test

**Use for:** Testing file and directory operations

```powershell
<#
.SYNOPSIS
    Tests for file operations in [FunctionName]
#>

BeforeAll {
    . $PSScriptRoot\..\Phase-X-[Module].ps1
    $TestDir = "C:\HELIOS-Test\FileOps"
}

BeforeEach {
    if (Test-Path $TestDir) { Remove-Item $TestDir -Recurse -Force }
    New-Item -Path $TestDir -ItemType Directory -Force | Out-Null
}

Describe "File Operations" {
    
    Context "When creating files" {
        
        It "should create file in target directory" {
            # Act
            New-Item -Path "$TestDir\test.txt" -ItemType File -Force | Out-Null
            
            # Assert
            Test-Path -Path "$TestDir\test.txt" | Should -Be $true
        }
        
        It "should write content to file" {
            # Act
            "Test content" | Set-Content -Path "$TestDir\test.txt"
            
            # Assert
            $Content = Get-Content -Path "$TestDir\test.txt"
            $Content | Should -Be "Test content"
        }
        
        It "should handle existing files" {
            # Arrange
            "Original" | Set-Content -Path "$TestDir\test.txt"
            
            # Act
            "Updated" | Set-Content -Path "$TestDir\test.txt" -Force
            
            # Assert
            $Content = Get-Content -Path "$TestDir\test.txt"
            $Content | Should -Be "Updated"
        }
    }
    
    Context "When creating directories" {
        
        It "should create nested directories" {
            # Act
            New-Item -Path "$TestDir\nested\dir\structure" -ItemType Directory -Force | Out-Null
            
            # Assert
            Test-Path -Path "$TestDir\nested\dir\structure" | Should -Be $true
        }
    }
    
    AfterEach {
        if (Test-Path $TestDir) { Remove-Item $TestDir -Recurse -Force }
    }
}
```

---

## Template 4: Performance Test

**Use for:** Measuring execution performance

```powershell
<#
.SYNOPSIS
    Performance tests for [FunctionName]
#>

BeforeAll {
    . $PSScriptRoot\..\Phase-X-[Module].ps1
}

Describe "Performance Metrics" {
    
    Context "When executing [FunctionName]" {
        
        It "should complete within timeout" {
            # Arrange
            $Timeout = 5000  # milliseconds
            
            # Act & Assert
            $Duration = Measure-Command {
                [FunctionName]
            } | Select-Object -ExpandProperty TotalMilliseconds
            
            $Duration | Should -BeLessThan $Timeout
        }
        
        It "should not consume excessive memory" {
            # Arrange
            $MemBefore = (Get-Process -Id $PID).WorkingSet64
            
            # Act
            for ($i = 0; $i -lt 100; $i++) {
                [FunctionName]
            }
            
            # Assert
            $MemAfter = (Get-Process -Id $PID).WorkingSet64
            $MemIncrease = $MemAfter - $MemBefore
            
            # Should not increase more than 50MB
            $MemIncrease | Should -BeLessThan 50MB
        }
        
        It "should scale linearly with input size" {
            # Arrange
            $SmallInput = 1..100
            $LargeInput = 1..1000
            
            # Act
            $SmallDuration = Measure-Command { [FunctionName] -Input $SmallInput }
            $LargeDuration = Measure-Command { [FunctionName] -Input $LargeInput }
            
            # Assert (should be roughly 10x longer for 10x input)
            $Ratio = $LargeDuration.TotalMilliseconds / $SmallDuration.TotalMilliseconds
            $Ratio | Should -BeGreaterThan 5  # At least 5x
            $Ratio | Should -BeLessThan 20    # Not more than 20x
        }
    }
}
```

---

## Template 5: Service Test

**Use for:** Testing Windows services

```powershell
<#
.SYNOPSIS
    Tests for [ServiceName] service
#>

Describe "[ServiceName] Service" {
    
    Context "Service startup" {
        
        It "should be able to start" {
            # Arrange
            Stop-Service -Name "[ServiceName]" -Force -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 500
            
            # Act
            { Start-Service -Name "[ServiceName]" } | Should -Not -Throw
            
            # Assert
            (Get-Service -Name "[ServiceName]").Status | Should -Be "Running"
        }
        
        It "should be able to stop" {
            # Arrange
            Start-Service -Name "[ServiceName]" -ErrorAction SilentlyContinue
            
            # Act
            { Stop-Service -Name "[ServiceName]" -Force } | Should -Not -Throw
            
            # Assert
            (Get-Service -Name "[ServiceName]").Status | Should -Be "Stopped"
        }
    }
    
    Context "Service configuration" {
        
        It "should have start type set correctly" {
            # Assert
            $Service = Get-Service -Name "[ServiceName]"
            $Service.StartType | Should -Match "Automatic|Manual|Disabled"
        }
    }
    
    Context "Service health" {
        
        It "should not crash during operation" {
            # Arrange
            $ProcessBefore = Get-Process -Name "[ServiceName]" -ErrorAction SilentlyContinue
            
            # Act
            Start-Sleep -Seconds 5
            
            # Assert
            $ProcessAfter = Get-Process -Name "[ServiceName]" -ErrorAction SilentlyContinue
            $ProcessAfter | Should -Not -BeNullOrEmpty
            $ProcessAfter.Id | Should -Be $ProcessBefore.Id  # Same process
        }
    }
}
```

---

## Template 6: Rollback Test

**Use for:** Testing rollback procedures

```powershell
<#
.SYNOPSIS
    Tests for rollback functionality of [PhaseName]
#>

BeforeAll {
    . $PSScriptRoot\..\Phase-X-[PhaseName].ps1
}

Describe "Rollback Functionality" {
    
    BeforeEach {
        # Create snapshot before making changes
        $Snapshot = Create-RegistrySnapshot -Path "HKLM:\Software\HELIOS"
    }
    
    Context "When rolling back changes" {
        
        It "should restore registry to previous state" {
            # Arrange
            $OriginalValue = (Get-ItemProperty -Path "HKLM:\Software\HELIOS\Test" -Name "TestReg").TestReg
            
            # Act
            Set-ItemProperty -Path "HKLM:\Software\HELIOS\Test" -Name "TestReg" -Value 999
            Restore-RegistrySnapshot -Snapshot $Snapshot
            
            # Assert
            $RestoredValue = (Get-ItemProperty -Path "HKLM:\Software\HELIOS\Test" -Name "TestReg").TestReg
            $RestoredValue | Should -Be $OriginalValue
        }
        
        It "should restore files to previous state" {
            # Arrange
            $TestFile = "C:\HELIOS-Test\rollback.txt"
            "Original" | Set-Content -Path $TestFile
            $FileSnapshot = Get-FileHashSnapshot -Path $TestFile
            
            # Act
            "Modified" | Set-Content -Path $TestFile -Force
            Restore-FileSnapshot -Snapshot $FileSnapshot
            
            # Assert
            Get-Content -Path $TestFile | Should -Be "Original"
        }
        
        It "should restore services to previous state" {
            # Arrange
            $ServiceSnapshot = Get-ServiceSnapshot -ServiceName "TestService"
            $OriginalStartType = $ServiceSnapshot.StartType
            
            # Act
            Set-Service -Name "TestService" -StartupType Manual
            Restore-ServiceSnapshot -Snapshot $ServiceSnapshot
            
            # Assert
            (Get-Service -Name "TestService").StartType | Should -Be $OriginalStartType
        }
    }
    
    AfterEach {
        # Always restore on test failure
        if ($LASTEXITCODE -ne 0) {
            Restore-RegistrySnapshot -Snapshot $Snapshot
        }
    }
}
```

---

## Template 7: Error Handling Test

**Use for:** Testing error scenarios

```powershell
<#
.SYNOPSIS
    Error handling tests for [FunctionName]
#>

BeforeAll {
    . $PSScriptRoot\..\Phase-X-[Module].ps1
}

Describe "Error Handling" {
    
    Context "When function encounters errors" {
        
        It "should throw descriptive error on null input" {
            # Act & Assert
            { [FunctionName] -Parameter $null } | 
                Should -Throw -ExpectedMessage "*null*"
        }
        
        It "should throw on invalid file path" {
            # Act & Assert
            { [FunctionName] -FilePath "C:\NonExistent\Path\file.txt" } | 
                Should -Throw -ExpectedMessage "*not found*"
        }
        
        It "should continue on non-fatal errors" {
            # Arrange
            Mock -CommandName Get-Item -MockWith { throw "Access denied" } -ParameterFilter { $Path -match "Restricted" }
            Mock -CommandName Get-Item -MockWith { return @() }
            
            # Act & Assert
            { [FunctionName] -Items @("Restricted", "Normal") } | Should -Not -Throw
        }
    }
    
    Context "When function recovers from errors" {
        
        It "should retry on transient errors" {
            # Act & Assert
            [FunctionName] -EnableRetry | Should -Not -BeNullOrEmpty
        }
        
        It "should log errors for diagnostics" {
            # Act
            { [FunctionName] -LogErrors } | Out-Null
            
            # Assert
            $LogFile = "C:\HELIOS\logs\errors.log"
            Test-Path -Path $LogFile | Should -Be $true
        }
    }
}
```

---

## Template 8: Data Validation Test

**Use for:** Testing data integrity

```powershell
<#
.SYNOPSIS
    Data validation tests for [FunctionName]
#>

BeforeAll {
    . $PSScriptRoot\..\Phase-X-[Module].ps1
}

Describe "Data Validation" {
    
    Context "When processing data" {
        
        It "should validate input format" {
            # Arrange
            $InvalidData = @{ Invalid = "format" }
            
            # Act & Assert
            { [FunctionName] -Data $InvalidData } | Should -Throw
        }
        
        It "should handle empty data gracefully" {
            # Act
            $Result = [FunctionName] -Data @()
            
            # Assert
            $Result | Should -BeNullOrEmpty
        }
        
        It "should normalize data format" {
            # Arrange
            $VariedData = @(
                "test_value",
                "Test-Value",
                "TestValue"
            )
            
            # Act
            $Normalized = [FunctionName] -Normalize $VariedData
            
            # Assert
            $Normalized | Select-Object -Unique | Should -HaveCount 1
        }
        
        It "should preserve data types" {
            # Arrange
            $Data = @{
                String = "value"
                Number = 123
                Boolean = $true
            }
            
            # Act
            $Result = [FunctionName] -Data $Data
            
            # Assert
            $Result.String | Should -BeOfType "string"
            $Result.Number | Should -BeOfType "int"
            $Result.Boolean | Should -BeOfType "bool"
        }
    }
}
```

---

## Template 9: Integration Test

**Use for:** Testing component interactions

```powershell
<#
.SYNOPSIS
    Integration tests between [Component1] and [Component2]
#>

BeforeAll {
    . $PSScriptRoot\..\Phase-X-Component1.ps1
    . $PSScriptRoot\..\Phase-X-Component2.ps1
}

Describe "Component Interaction" {
    
    BeforeEach {
        $Snapshot = Create-SystemSnapshot -Label "Integration-Test"
    }
    
    Context "When components execute together" {
        
        It "should not cause conflicts" {
            # Act
            Initialize-Component1
            Initialize-Component2
            
            # Assert
            Test-ComponentConflicts -Component1 "Component1" -Component2 "Component2" | 
                Should -BeNullOrEmpty
        }
        
        It "should maintain data consistency" {
            # Arrange
            $Data = @{ TestValue = "initial" }
            
            # Act
            Set-DataInComponent1 -Data $Data
            $RetrievedData = Get-DataFromComponent2
            
            # Assert
            $RetrievedData.TestValue | Should -Be "initial"
        }
        
        It "should handle communication correctly" {
            # Act
            $Response = Invoke-ComponentCommunication -Component1 "Component1" -Component2 "Component2"
            
            # Assert
            $Response | Should -Not -BeNullOrEmpty
            $Response.Status | Should -Be "Success"
        }
    }
    
    AfterEach {
        if ($LASTEXITCODE -ne 0) {
            Restore-SystemSnapshot -Snapshot $Snapshot
        }
    }
}
```

---

## Template 10: Security Test

**Use for:** Testing security controls

```powershell
<#
.SYNOPSIS
    Security tests for [FunctionName]
#>

BeforeAll {
    . $PSScriptRoot\..\Phase-X-[Module].ps1
}

Describe "Security Controls" {
    
    Context "When function handles sensitive data" {
        
        It "should not expose credentials in logs" {
            # Arrange
            $Cred = New-Object System.Management.Automation.PSCredential(
                "TestUser",
                (ConvertTo-SecureString -String "TestPassword" -AsPlainText -Force)
            )
            
            # Act
            $Result = [FunctionName] -Credential $Cred -Verbose 4>&1
            
            # Assert
            $Result | Should -Not -Match "TestPassword"
        }
        
        It "should require elevated privileges" {
            # Act & Assert
            { [FunctionName] -RequireAdmin } | 
                Should -Throw -ExpectedMessage "*administrator*"
        }
        
        It "should validate permissions before execution" {
            # Act
            [FunctionName] -ValidatePermissions | Out-Null
            
            # Assert (if reached without error, permissions are correct)
            $true | Should -Be $true
        }
    }
}
```

---

## Using Templates

### Quick Start

1. **Copy the template** for your test type
2. **Replace placeholders** like `[FunctionName]`, `[ServiceName]`, etc.
3. **Customize assertions** for your specific requirements
4. **Save as** `test-[component]-[type].ps1`
5. **Run tests** with Invoke-Pester

### Example: Creating a service test

```powershell
# 1. Copy Template 5: Service Test
# 2. Replace [ServiceName] with "DiagTrack"
# 3. Save as test-diagtrack-service.ps1
# 4. Run with: Invoke-Pester -Path test-diagtrack-service.ps1

# Result:
# Describe "DiagTrack Service" { ... }
```

---

## Template Selection Guide

| Scenario | Template |
|----------|----------|
| Testing PowerShell function | Template 1 |
| Registry changes | Template 2 |
| File operations | Template 3 |
| Performance benchmarks | Template 4 |
| Windows services | Template 5 |
| Rollback procedures | Template 6 |
| Error handling | Template 7 |
| Data validation | Template 8 |
| Multi-component testing | Template 9 |
| Security validation | Template 10 |

---

**Version:** 2.0  
**Last Updated:** 2024  
**Maintained By:** HELIOS Development Team

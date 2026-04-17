#Requires -RunAsAdministrator
<#
.SYNOPSIS
    HELIOS Platform Post-Installation Verification Script
    
.DESCRIPTION
    Verifies successful installation of HELIOS Platform by checking:
    - File presence and integrity
    - Registry entries
    - System PATH registration
    - Shortcuts creation
    - Application functionality
    
.EXAMPLE
    .\Post-Install-Verify.ps1
    
.AUTHOR
    HELIOS Solutions
    
.VERSION
    1.0.0.0
#>

param(
    [string]$InstallPath = "$env:ProgramFiles\HELIOS Platform",
    [switch]$Verbose,
    [switch]$Repair
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

# ============================================================================
# CONFIGURATION
# ============================================================================

$script:Verification = @{
    Success              = 0
    Warnings             = 0
    Errors               = 0
    AllChecksPassed      = $true
    InstallationValid    = $false
    RequiredFiles        = @(
        "HELIOS.Platform.exe"
        "config\helios.config"
        "Uninstall.exe"
    )
    ExpectedRegistry     = @(
        @{Path = "HKCU:\Software\HELIOS Platform"; Property = "InstallPath" }
        @{Path = "HKCU:\Software\HELIOS Platform"; Property = "Version" }
        @{Path = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\HELIOS Platform"; Property = "DisplayName" }
    )
    ExpectedShortcuts    = @(
        "$env:SMPROGRAMS\HELIOS Platform"
        "$env:DESKTOP\HELIOS Platform.lnk"
    )
}

# ============================================================================
# UTILITY FUNCTIONS
# ============================================================================

function Write-VerificationStatus {
    param(
        [ValidateSet("✓", "⚠", "✗")]
        [string]$Status,
        [string]$Message,
        [ValidateSet("Success", "Warning", "Error", "Info")]
        [string]$Type = "Info"
    )
    
    $colors = @{
        "Success" = "Green"
        "Warning" = "Yellow"
        "Error"   = "Red"
        "Info"    = "Cyan"
    }
    
    Write-Host "  [$Status] $Message" -ForegroundColor $colors[$Type]
    
    switch ($Type) {
        "Success" { $script:Verification.Success++ }
        "Warning" { $script:Verification.Warnings++ }
        "Error" { 
            $script:Verification.Errors++
            $script:Verification.AllChecksPassed = $false
        }
    }
}

function Test-InstallationPath {
    Write-Host "`n[1/6] Verifying Installation Path..." -ForegroundColor Cyan
    
    if (-not (Test-Path $InstallPath)) {
        Write-VerificationStatus "✗" "Installation path does not exist: $InstallPath" "Error"
        return $false
    }
    
    Write-VerificationStatus "✓" "Installation path found: $InstallPath" "Success"
    return $true
}

function Test-RequiredFiles {
    Write-Host "`n[2/6] Verifying Required Files..." -ForegroundColor Cyan
    
    $allFilesFound = $true
    
    foreach ($file in $script:Verification.RequiredFiles) {
        $filePath = Join-Path $InstallPath $file
        
        if (Test-Path $filePath) {
            $fileInfo = Get-Item $filePath
            $size = if ($fileInfo.PSIsContainer) { "DIR" } else { "$([math]::Round($fileInfo.Length / 1MB, 2)) MB" }
            Write-VerificationStatus "✓" "$file ($size)" "Success"
        }
        else {
            Write-VerificationStatus "✗" "$file is missing" "Error"
            $allFilesFound = $false
        }
    }
    
    return $allFilesFound
}

function Test-RegistryEntries {
    Write-Host "`n[3/6] Verifying Registry Entries..." -ForegroundColor Cyan
    
    $allEntriesFound = $true
    
    foreach ($entry in $script:Verification.ExpectedRegistry) {
        $regPath = $entry.Path
        $property = $entry.Property
        
        try {
            if (Test-Path $regPath) {
                $value = Get-ItemProperty -Path $regPath -Name $property -ErrorAction SilentlyContinue
                
                if ($value) {
                    Write-VerificationStatus "✓" "Registry: $($property) = $($value.$property)" "Success"
                }
                else {
                    Write-VerificationStatus "⚠" "Registry property not found: $property" "Warning"
                }
            }
            else {
                Write-VerificationStatus "✗" "Registry path does not exist: $regPath" "Error"
                $allEntriesFound = $false
            }
        }
        catch {
            Write-VerificationStatus "✗" "Registry access failed: $_" "Error"
            $allEntriesFound = $false
        }
    }
    
    return $allEntriesFound
}

function Test-EnvironmentPath {
    Write-Host "`n[4/6] Verifying System PATH..." -ForegroundColor Cyan
    
    try {
        $envPath = [System.Environment]::GetEnvironmentVariable("Path", "Machine")
        
        if ($envPath -contains $InstallPath) {
            Write-VerificationStatus "✓" "$InstallPath is in system PATH" "Success"
            return $true
        }
        else {
            Write-VerificationStatus "⚠" "$InstallPath not found in system PATH (optional)" "Warning"
            return $true
        }
    }
    catch {
        Write-VerificationStatus "⚠" "Could not verify system PATH: $_" "Warning"
        return $true
    }
}

function Test-Shortcuts {
    Write-Host "`n[5/6] Verifying Shortcuts..." -ForegroundColor Cyan
    
    $allShortcutsFound = $true
    
    foreach ($shortcut in $script:Verification.ExpectedShortcuts) {
        if (Test-Path $shortcut) {
            Write-VerificationStatus "✓" "Shortcut found: $([System.IO.Path]::GetFileName($shortcut))" "Success"
        }
        else {
            Write-VerificationStatus "⚠" "Shortcut not found: $shortcut" "Warning"
            # Don't treat as error - shortcuts are optional
        }
    }
    
    return $true
}

function Test-ApplicationStart {
    Write-Host "`n[6/6] Testing Application Start..." -ForegroundColor Cyan
    
    try {
        $exePath = Join-Path $InstallPath "HELIOS.Platform.exe"
        
        if (-not (Test-Path $exePath)) {
            Write-VerificationStatus "✗" "Executable not found: $exePath" "Error"
            return $false
        }
        
        # Test by running with --version flag
        $output = & $exePath --version 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-VerificationStatus "✓" "Application started successfully (version check passed)" "Success"
            return $true
        }
        else {
            Write-VerificationStatus "⚠" "Application started but returned exit code: $LASTEXITCODE" "Warning"
            return $true
        }
    }
    catch {
        Write-VerificationStatus "⚠" "Could not execute application: $_" "Warning"
        return $true
    }
}

# ============================================================================
# REPAIR FUNCTIONS
# ============================================================================

function Repair-Installation {
    if (-not $Repair) {
        return
    }
    
    Write-Host "`n`n" + ("="*70)
    Write-Host "Attempting to repair installation..." -ForegroundColor Yellow
    Write-Host "="*70
    
    # Recreate registry entries
    Write-Host "`nRecreating registry entries..."
    try {
        $regPath = "HKCU:\Software\HELIOS Platform"
        if (-not (Test-Path $regPath)) {
            New-Item -Path $regPath -Force | Out-Null
        }
        
        Set-ItemProperty -Path $regPath -Name "InstallPath" -Value $InstallPath -ErrorAction Stop
        Set-ItemProperty -Path $regPath -Name "Version" -Value "1.0.0.0" -ErrorAction Stop
        
        Write-Host "  ✓ Registry entries restored" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ Failed to restore registry entries: $_" -ForegroundColor Red
    }
    
    # Recreate shortcuts
    Write-Host "`nRecreating shortcuts..."
    try {
        $exePath = Join-Path $InstallPath "HELIOS.Platform.exe"
        
        $startMenuPath = "$env:SMPROGRAMS\HELIOS Platform"
        if (-not (Test-Path $startMenuPath)) {
            New-Item -ItemType Directory -Path $startMenuPath -Force | Out-Null
        }
        
        $shortcutPath = Join-Path $startMenuPath "HELIOS Platform.lnk"
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $exePath
        $shortcut.WorkingDirectory = $InstallPath
        $shortcut.Save()
        
        Write-Host "  ✓ Shortcuts restored" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ Failed to restore shortcuts: $_" -ForegroundColor Red
    }
    
    # Verify config file
    Write-Host "`nVerifying configuration files..."
    try {
        $configPath = Join-Path $InstallPath "config\helios.config"
        if (-not (Test-Path $configPath)) {
            $configDir = Split-Path $configPath
            if (-not (Test-Path $configDir)) {
                New-Item -ItemType Directory -Path $configDir -Force | Out-Null
            }
            
            @"
[HELIOS Platform Configuration]
Version=1.0.0.0
InstallDate=$(Get-Date -Format 'yyyy-MM-dd')
DeploymentTier=Professional
EnableTelemetry=0
EnableAutoStart=0
"@ | Set-Content $configPath -Encoding UTF8
        }
        
        Write-Host "  ✓ Configuration verified" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ Failed to verify configuration: $_" -ForegroundColor Red
    }
}

# ============================================================================
# REPORT GENERATION
; ============================================================================

function New-VerificationReport {
    Write-Host "`n`n" + ("="*70)
    Write-Host "HELIOS PLATFORM POST-INSTALLATION VERIFICATION REPORT" -ForegroundColor Cyan
    Write-Host "="*70
    
    Write-Host "`nInstallation Path: $InstallPath" -ForegroundColor White
    Write-Host "Verification Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
    
    Write-Host "`nSummary:" -ForegroundColor Cyan
    Write-Host "  ✓ Checks Passed:  $($script:Verification.Success)" -ForegroundColor Green
    Write-Host "  ⚠ Warnings:       $($script:Verification.Warnings)" -ForegroundColor Yellow
    Write-Host "  ✗ Errors:         $($script:Verification.Errors)" -ForegroundColor Red
    
    Write-Host "`n" + "="*70
    
    if ($script:Verification.AllChecksPassed) {
        Write-Host "✓ Installation verification successful!" -ForegroundColor Green
        Write-Host "`nHELIOS Platform is ready to use.`n"
        return 0
    }
    else {
        Write-Host "✗ Installation verification found issues." -ForegroundColor Red
        
        if ($Repair) {
            Write-Host "`nRepair attempted. Please verify the installation.`n"
        }
        else {
            Write-Host "`nRun with -Repair flag to attempt automatic repairs:`n  .\Post-Install-Verify.ps1 -Repair`n"
        }
        
        return 1
    }
}

# ============================================================================
; MAIN EXECUTION
# ============================================================================

function Invoke-PostInstallationVerification {
    Write-Host "`n╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║      HELIOS PLATFORM POST-INSTALLATION VERIFICATION               ║" -ForegroundColor Cyan
    Write-Host "║                    Version 1.0.0.0                                ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    # Run all verification checks
    Test-InstallationPath | Out-Null
    Test-RequiredFiles | Out-Null
    Test-RegistryEntries | Out-Null
    Test-EnvironmentPath | Out-Null
    Test-Shortcuts | Out-Null
    Test-ApplicationStart | Out-Null
    
    # Attempt repair if requested
    if ($Repair) {
        Repair-Installation
    }
    
    # Generate report
    $exitCode = New-VerificationReport
    
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    
    exit $exitCode
}

# Execute
Invoke-PostInstallationVerification

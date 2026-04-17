<#
.SYNOPSIS
Enable/disable specific components in the Helios Platform build.

.DESCRIPTION
Provides functionality to toggle individual components on or off within the current build configuration.
Supports preview mode (-WhatIf), batch mode (from JSON file), dependency checking, and build integrity
validation. Automatically rolls back on failure.

.PARAMETER Component
The component name to toggle.

.PARAMETER Enable
Enable the component (default if neither Enable nor Disable specified).

.PARAMETER Disable
Disable the component.

.PARAMETER WhatIf
Preview the changes without applying them.

.PARAMETER BatchFile
Path to JSON file containing batch operations.

.PARAMETER SkipValidation
Skip build integrity validation after toggle.

.PARAMETER Verbose
Enables verbose logging output.

.EXAMPLE
.\toggle-feature.ps1 -Component gpu-acceleration -Enable
# Enables GPU acceleration component

.EXAMPLE
.\toggle-feature.ps1 -Component development-tools -Disable -WhatIf
# Preview disabling development tools without making changes

.EXAMPLE
.\toggle-feature.ps1 -BatchFile components.json
# Apply batch operations from JSON file

.NOTES
Author: Helios Build System
Version: 1.0
#>

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$Component,
    [switch]$Enable,
    [switch]$Disable,
    [string]$BatchFile,
    [switch]$SkipValidation,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

# Define script paths
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$manifestPath = Join-Path $projectRoot "BUILD_MANIFEST.json"
$backupPath = Join-Path $scriptRoot "backups\manifest-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$logPath = Join-Path $scriptRoot "logs\toggle-feature.log"

# Create directories if they don't exist
@(
    (Split-Path -Parent $backupPath),
    (Split-Path -Parent $logPath)
) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -Path $_ -ItemType Directory -Force | Out-Null
    }
}

<#
.SYNOPSIS
Logs a message to file and console.

.PARAMETER Message
The message to log.

.PARAMETER Level
The log level (Info, Warning, Error, Success).
#>
function Write-Log {
    param(
        [string]$Message,
        [ValidateSet('Info', 'Warning', 'Error', 'Success')]
        [string]$Level = 'Info'
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    Add-Content -Path $logPath -Value $logMessage
    
    switch ($Level) {
        'Info' { Write-Host $logMessage -ForegroundColor Gray }
        'Warning' { Write-Host $logMessage -ForegroundColor Yellow }
        'Error' { Write-Host $logMessage -ForegroundColor Red }
        'Success' { Write-Host $logMessage -ForegroundColor Green }
    }
}

<#
.SYNOPSIS
Loads the BUILD_MANIFEST.json configuration.

.OUTPUTS
The manifest object or $null if not found.
#>
function Get-BuildManifest {
    if (-not (Test-Path $manifestPath)) {
        Write-Log -Message "Manifest not found at $manifestPath" -Level Error
        return $null
    }
    
    try {
        $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
        Write-Verbose "Loaded manifest from $manifestPath"
        return $manifest
    }
    catch {
        Write-Log -Message "Failed to load manifest: $_" -Level Error
        return $null
    }
}

<#
.SYNOPSIS
Creates a backup of the current manifest.

.OUTPUTS
$true on success, $false on failure.
#>
function Backup-Manifest {
    try {
        Copy-Item -Path $manifestPath -Destination $backupPath -Force
        Write-Log -Message "Created backup at $backupPath" -Level Info
        Write-Verbose "Backup location: $backupPath"
        return $true
    }
    catch {
        Write-Log -Message "Failed to create backup: $_" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
Checks if a component exists in the manifest.

.PARAMETER ComponentName
The component name to check.

.PARAMETER Manifest
The manifest object.

.OUTPUTS
$true if component exists, $false otherwise.
#>
function Test-ComponentExists {
    param(
        [string]$ComponentName,
        [object]$Manifest
    )
    
    if (-not $Manifest -or -not $Manifest.components) {
        return $false
    }
    
    return $null -ne ($Manifest.components | Where-Object { $_.name -eq $ComponentName })
}

<#
.SYNOPSIS
Gets component dependencies from manifest configuration.

.PARAMETER ComponentName
The component name.

.PARAMETER Manifest
The manifest object.

.OUTPUTS
Array of dependent components.
#>
function Get-ComponentDependencies {
    param(
        [string]$ComponentName,
        [object]$Manifest
    )
    
    $dependencyMap = @{
        'gpu-acceleration' = @('core', 'advanced-ui')
        'ml-toolkit' = @('core', 'gpu-acceleration', 'compute-libraries')
        'clustering' = @('core', 'network-stack', 'enterprise-security')
        'backup-recovery' = @('core', 'database-client')
        'enterprise-security' = @('core', 'network-stack')
        'advanced-analytics' = @('core', 'database-client', 'monitoring')
        'testing-framework' = @('core', 'development-tools')
        'profiling-tools' = @('core', 'development-tools', 'debug-utilities')
        'multimedia' = @('core', 'advanced-ui')
        'compute-libraries' = @('core', 'gpu-acceleration')
    }
    
    return $dependencyMap[$ComponentName]
}

<#
.SYNOPSIS
Validates that required dependencies are met when disabling a component.

.PARAMETER ComponentName
The component to disable.

.PARAMETER Manifest
The manifest object.

.OUTPUTS
Object with success status and message.
#>
function Test-DependencyCompatibility {
    param(
        [string]$ComponentName,
        [object]$Manifest
    )
    
    if (-not $Manifest -or -not $Manifest.components) {
        return @{ Success = $false; Message = "Manifest is invalid" }
    }
    
    $dependentsMap = @{
        'core' = @('basic-ui', 'advanced-ui', 'network-stack', 'logging', 'monitoring', 'database-client', 'development-tools', 'debug-utilities', 'testing-framework', 'enterprise-security', 'advanced-analytics', 'gpu-acceleration', 'ml-toolkit', 'clustering', 'backup-recovery')
        'network-stack' = @('database-client', 'enterprise-security', 'clustering')
        'gpu-acceleration' = @('ml-toolkit', 'compute-libraries')
        'development-tools' = @('testing-framework', 'profiling-tools', 'debug-utilities')
        'database-client' = @('backup-recovery', 'advanced-analytics')
    }
    
    $enabledComponents = $Manifest.components | Where-Object { $_.enabled } | Select-Object -ExpandProperty name
    $dependents = $dependentsMap[$ComponentName]
    
    if ($dependents) {
        $enabledDependents = $dependents | Where-Object { $_ -in $enabledComponents }
        
        if ($enabledDependents) {
            $message = "Cannot disable '$ComponentName' because the following components depend on it: $($enabledDependents -join ', ')"
            return @{ Success = $false; Message = $message }
        }
    }
    
    return @{ Success = $true; Message = "No dependency conflicts detected" }
}

<#
.SYNOPSIS
Validates build integrity after changes.

.PARAMETER Manifest
The manifest object to validate.

.OUTPUTS
Object with validation results.
#>
function Invoke-BuildValidation {
    param([object]$Manifest)
    
    $validation = @{
        Valid = $true
        Issues = @()
        ComponentCount = 0
        EnabledCount = 0
    }
    
    if (-not $Manifest -or -not $Manifest.components) {
        $validation.Valid = $false
        $validation.Issues += "Manifest is invalid or missing components"
        return $validation
    }
    
    $validation.ComponentCount = @($Manifest.components).Count
    $validation.EnabledCount = @($Manifest.components | Where-Object { $_.enabled }).Count
    
    # Validate core components are present
    $coreComponents = @('core', 'network-stack')
    $enabledNames = $Manifest.components | Where-Object { $_.enabled } | Select-Object -ExpandProperty name
    
    foreach ($core in $coreComponents) {
        if ($core -notin $enabledNames) {
            $validation.Valid = $false
            $validation.Issues += "Critical component '$core' is not enabled"
        }
    }
    
    return $validation
}

<#
.SYNOPSIS
Toggles a single component state.

.PARAMETER ComponentName
The component name.

.PARAMETER NewState
The new enabled state (true/false).

.PARAMETER Manifest
The manifest object.

.OUTPUTS
Updated manifest or $null on failure.
#>
function Update-ComponentState {
    param(
        [string]$ComponentName,
        [bool]$NewState,
        [object]$Manifest
    )
    
    if (-not (Test-ComponentExists -ComponentName $ComponentName -Manifest $Manifest)) {
        Write-Log -Message "Component '$ComponentName' not found in manifest" -Level Error
        return $null
    }
    
    if ($NewState -eq $false) {
        $depCheck = Test-DependencyCompatibility -ComponentName $ComponentName -Manifest $Manifest
        if (-not $depCheck.Success) {
            Write-Log -Message $depCheck.Message -Level Warning
            return $null
        }
    }
    
    try {
        $component = $Manifest.components | Where-Object { $_.name -eq $ComponentName }
        if ($component) {
            $component.enabled = $NewState
            Write-Log -Message "Component '$ComponentName' toggled to $NewState" -Level Info
            return $Manifest
        }
    }
    catch {
        Write-Log -Message "Failed to update component state: $_" -Level Error
        return $null
    }
    
    return $null
}

<#
.SYNOPSIS
Restores manifest from backup.

.PARAMETER BackupFile
Path to the backup file.

.OUTPUTS
$true on success, $false on failure.
#>
function Restore-ManifestFromBackup {
    param([string]$BackupFile)
    
    if (-not (Test-Path $BackupFile)) {
        Write-Log -Message "Backup file not found: $BackupFile" -Level Error
        return $false
    }
    
    try {
        Copy-Item -Path $BackupFile -Destination $manifestPath -Force
        Write-Log -Message "Restored manifest from backup" -Level Success
        return $true
    }
    catch {
        Write-Log -Message "Failed to restore from backup: $_" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
Saves the updated manifest to file.

.PARAMETER Manifest
The manifest object to save.

.OUTPUTS
$true on success, $false on failure.
#>
function Save-Manifest {
    param([object]$Manifest)
    
    try {
        $Manifest.lastUpdated = (Get-Date -Format "o")
        $Manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath -Encoding UTF8
        Write-Log -Message "Manifest saved successfully" -Level Success
        return $true
    }
    catch {
        Write-Log -Message "Failed to save manifest: $_" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
Displays preview of changes without applying them.

.PARAMETER ComponentName
The component name.

.PARAMETER NewState
The new state.

.PARAMETER Manifest
The manifest object.
#>
function Show-ChangePreview {
    param(
        [string]$ComponentName,
        [bool]$NewState,
        [object]$Manifest
    )
    
    $component = $Manifest.components | Where-Object { $_.name -eq $ComponentName }
    $currentState = $component.enabled
    $stateChange = if ($NewState) { "Disabled -> Enabled" } else { "Enabled -> Disabled" }
    
    Write-Host ""
    Write-Host "Preview of Changes:" -ForegroundColor Cyan
    Write-Host "===================" -ForegroundColor Cyan
    Write-Host "Component: $ComponentName" -ForegroundColor White
    Write-Host "Current State: $currentState" -ForegroundColor Yellow
    Write-Host "New State: $NewState" -ForegroundColor Yellow
    Write-Host "Change: $stateChange" -ForegroundColor Cyan
    Write-Host ""
}

<#
.SYNOPSIS
Processes batch operations from JSON file.

.PARAMETER FilePath
Path to JSON file with batch operations.

.OUTPUTS
Array of operation results.
#>
function Invoke-BatchOperations {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        Write-Log -Message "Batch file not found: $FilePath" -Level Error
        return @()
    }
    
    try {
        $operations = Get-Content -Path $FilePath -Raw | ConvertFrom-Json
        Write-Log -Message "Loaded batch operations from $FilePath" -Level Info
        return $operations
    }
    catch {
        Write-Log -Message "Failed to parse batch file: $_" -Level Error
        return @()
    }
}

<#
.SYNOPSIS
Main function to toggle a single component.
#>
function Invoke-ComponentToggle {
    Write-Log -Message "Component toggle operation started" -Level Info
    
    if ($BatchFile -and $Component) {
        Write-Log -Message "Cannot specify both -Component and -BatchFile" -Level Error
        return $false
    }
    
    if (-not $BatchFile -and -not $Component) {
        Write-Log -Message "Must specify either -Component or -BatchFile" -Level Error
        return $false
    }
    
    # Load manifest and create backup
    $manifest = Get-BuildManifest
    if (-not $manifest) {
        return $false
    }
    
    if (-not (Backup-Manifest)) {
        return $false
    }
    
    # Determine desired state
    $desiredState = $true
    if ($Disable) {
        $desiredState = $false
    } elseif ($Enable) {
        $desiredState = $true
    }
    
    # Handle single component toggle
    if ($Component) {
        Show-ChangePreview -ComponentName $Component -NewState $desiredState -Manifest $manifest
        
        if ($PSCmdlet.ShouldProcess("Component: $Component", "Toggle to $desiredState")) {
            $updatedManifest = Update-ComponentState -ComponentName $Component -NewState $desiredState -Manifest $manifest
            
            if (-not $updatedManifest) {
                Write-Log -Message "Failed to update component state" -Level Error
                Restore-ManifestFromBackup -BackupFile $backupPath
                return $false
            }
            
            # Validate build integrity
            if (-not $SkipValidation) {
                $validation = Invoke-BuildValidation -Manifest $updatedManifest
                if (-not $validation.Valid) {
                    Write-Log -Message "Build validation failed: $($validation.Issues -join '; ')" -Level Error
                    Restore-ManifestFromBackup -BackupFile $backupPath
                    return $false
                }
            }
            
            # Save manifest
            if (-not (Save-Manifest -Manifest $updatedManifest)) {
                Restore-ManifestFromBackup -BackupFile $backupPath
                return $false
            }
            
            Write-Host ""
            Write-Host "Component toggle completed successfully!" -ForegroundColor Green
            return $true
        } else {
            Write-Host "WhatIf: Would toggle component '$Component' to $desiredState" -ForegroundColor Blue
            return $true
        }
    }
    
    # Handle batch operations
    $operations = Invoke-BatchOperations -FilePath $BatchFile
    if (-not $operations) {
        return $false
    }
    
    $successCount = 0
    $failCount = 0
    
    foreach ($operation in $operations) {
        $componentName = $operation.component
        $opState = if ($operation.action -eq 'enable') { $true } else { $false }
        
        Write-Host "Processing: $componentName ($($operation.action))" -ForegroundColor Yellow
        
        $updatedManifest = Update-ComponentState -ComponentName $componentName -NewState $opState -Manifest $manifest
        
        if ($updatedManifest) {
            $manifest = $updatedManifest
            $successCount++
        } else {
            $failCount++
        }
    }
    
    if ($failCount -gt 0) {
        Write-Log -Message "Batch operation completed with $failCount failures" -Level Warning
        Restore-ManifestFromBackup -BackupFile $backupPath
        return $false
    }
    
    # Validate and save
    if (-not $SkipValidation) {
        $validation = Invoke-BuildValidation -Manifest $manifest
        if (-not $validation.Valid) {
            Write-Log -Message "Build validation failed: $($validation.Issues -join '; ')" -Level Error
            Restore-ManifestFromBackup -BackupFile $backupPath
            return $false
        }
    }
    
    if (-not (Save-Manifest -Manifest $manifest)) {
        Restore-ManifestFromBackup -BackupFile $backupPath
        return $false
    }
    
    Write-Host ""
    Write-Host "Batch operations completed successfully! ($successCount/$($successCount + $failCount) succeeded)" -ForegroundColor Green
    return $true
}

# Main execution
try {
    $result = Invoke-ComponentToggle
    exit if ($result) { 0 } else { 1 }
}
catch {
    Write-Log -Message "Unhandled error: $_" -Level Error
    Write-Log -Message $_.ScriptStackTrace -Level Error
    exit 1
}

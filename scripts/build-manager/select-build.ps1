<#
.SYNOPSIS
Interactive build variant selector for Helios Platform.

.DESCRIPTION
Displays 7 pre-configured build variants with detailed descriptions and allows interactive
selection. Shows what components will be added/removed, confirms changes, updates the
BUILD_MANIFEST.json, and installs components via the orchestrator.

.PARAMETER Variant
The build variant to select (minimal, standard, developer, enterprise, gpu-optimized, edge-deployment, all-features).
If not provided, shows interactive menu.

.PARAMETER Confirm
Prompts for confirmation before making changes.

.PARAMETER WhatIf
Shows what would happen without making changes.

.PARAMETER Verbose
Enables verbose logging output.

.EXAMPLE
.\select-build.ps1
# Shows interactive menu with 7 variants

.EXAMPLE
.\select-build.ps1 -Variant enterprise -Verbose
# Selects enterprise variant with verbose output

.EXAMPLE
.\select-build.ps1 -WhatIf
# Shows what would happen without making changes

.NOTES
Author: Helios Build System
Version: 1.0
#>

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidateSet('minimal', 'standard', 'developer', 'enterprise', 'gpu-optimized', 'edge-deployment', 'all-features')]
    [string]$Variant,
    
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

# Define script paths and constants
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$manifestPath = Join-Path $projectRoot "BUILD_MANIFEST.json"
$orchestratorPath = Join-Path $projectRoot "scripts\orchestrator.ps1"
$logPath = Join-Path $scriptRoot "logs\build-selection.log"

# Create logs directory if it doesn't exist
$logDir = Split-Path -Parent $logPath
if (-not (Test-Path $logDir)) {
    New-Item -Path $logDir -ItemType Directory -Force | Out-Null
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
Defines build variants with their configurations.

.OUTPUTS
Hashtable containing variant configurations.
#>
function Get-BuildVariants {
    return @{
        'minimal' = @{
            Name = 'Minimal Installation'
            Description = 'Lightweight build for resource-constrained environments'
            Size = '256 MB'
            InstallTime = '2 minutes'
            Components = @('core', 'basic-ui', 'network-stack')
            Optional = @('logging', 'monitoring')
            Removed = @('gpu-acceleration', 'ml-toolkit', 'advanced-analytics', 'multimedia')
        }
        'standard' = @{
            Name = 'Standard Build'
            Description = 'Recommended for most users and production deployments'
            Size = '1.2 GB'
            InstallTime = '8 minutes'
            Components = @('core', 'basic-ui', 'network-stack', 'logging', 'monitoring', 'database-client')
            Optional = @('development-tools')
            Removed = @('gpu-acceleration', 'ml-toolkit', 'advanced-analytics')
        }
        'developer' = @{
            Name = 'Developer Build'
            Description = 'Includes development tools, debuggers, and testing utilities'
            Size = '2.4 GB'
            InstallTime = '12 minutes'
            Components = @('core', 'advanced-ui', 'network-stack', 'logging', 'monitoring', 'database-client', 'development-tools', 'debug-utilities', 'testing-framework')
            Optional = @('profiling-tools', 'documentation-generator')
            Removed = @('gpu-acceleration', 'ml-toolkit')
        }
        'enterprise' = @{
            Name = 'Enterprise Build'
            Description = 'Production-grade with advanced features, high availability, and monitoring'
            Size = '3.8 GB'
            InstallTime = '18 minutes'
            Components = @('core', 'advanced-ui', 'network-stack', 'logging', 'monitoring', 'database-client', 'enterprise-security', 'advanced-analytics', 'clustering', 'backup-recovery')
            Optional = @('ml-toolkit', 'gpu-acceleration')
            Removed = @('development-tools', 'debug-utilities')
        }
        'gpu-optimized' = @{
            Name = 'GPU-Optimized Build'
            Description = 'Optimized for GPU acceleration and compute-intensive workloads'
            Size = '2.8 GB'
            InstallTime = '14 minutes'
            Components = @('core', 'advanced-ui', 'network-stack', 'gpu-acceleration', 'ml-toolkit', 'compute-libraries', 'monitoring')
            Optional = @('advanced-analytics', 'database-client')
            Removed = @('development-tools', 'debug-utilities')
        }
        'edge-deployment' = @{
            Name = 'Edge Deployment Build'
            Description = 'Optimized for edge devices with minimal overhead'
            Size = '512 MB'
            InstallTime = '4 minutes'
            Components = @('core', 'lightweight-ui', 'network-stack', 'edge-connector', 'local-cache')
            Optional = @('logging', 'offline-mode')
            Removed = @('development-tools', 'debug-utilities', 'gpu-acceleration', 'ml-toolkit', 'advanced-analytics', 'enterprise-security', 'clustering')
        }
        'all-features' = @{
            Name = 'All Features Build'
            Description = 'Includes every available component and optional module'
            Size = '5.2 GB'
            InstallTime = '25 minutes'
            Components = @('core', 'advanced-ui', 'network-stack', 'logging', 'monitoring', 'database-client', 'development-tools', 'debug-utilities', 'testing-framework', 'enterprise-security', 'advanced-analytics', 'gpu-acceleration', 'ml-toolkit', 'clustering', 'backup-recovery', 'profiling-tools', 'multimedia')
            Optional = @()
            Removed = @()
        }
    }
}

<#
.SYNOPSIS
Loads the current BUILD_MANIFEST.json configuration.

.OUTPUTS
The manifest object from JSON, or $null if not found.
#>
function Get-BuildManifest {
    if (Test-Path $manifestPath) {
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
    Write-Verbose "Manifest not found at $manifestPath"
    return $null
}

<#
.SYNOPSIS
Displays the interactive build selection menu.

.OUTPUTS
The selected variant key as string.
#>
function Show-InteractiveMenu {
    $variants = Get-BuildVariants
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  Helios Platform Build Variant Selector  " -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
    
    Write-Host "Select a build variant:" -ForegroundColor White
    Write-Host ""
    
    $index = 1
    $variantList = @()
    
    foreach ($variantKey in $variants.Keys) {
        $variant = $variants[$variantKey]
        $variantList += $variantKey
        
        Write-Host "[$index] $($variant.Name)" -ForegroundColor Yellow
        Write-Host "    Description: $($variant.Description)" -ForegroundColor Gray
        Write-Host "    Size: $($variant.Size) | Install Time: $($variant.InstallTime)" -ForegroundColor Gray
        Write-Host "    Components: $($variant.Components.Count) total" -ForegroundColor Gray
        Write-Host ""
        
        $index++
    }
    
    Write-Host "[0] Cancel" -ForegroundColor Yellow
    Write-Host ""
    
    do {
        $selection = Read-Host "Enter selection (0-7)"
        
        if ($selection -eq '0') {
            Write-Log -Message "Build selection cancelled by user" -Level Info
            return $null
        }
        
        if ([int]$selection -ge 1 -and [int]$selection -le 7) {
            $selectedVariant = $variantList[[int]$selection - 1]
            return $selectedVariant
        }
        
        Write-Host "Invalid selection. Please try again." -ForegroundColor Red
    } while ($true)
}

<#
.SYNOPSIS
Shows what components will be added and removed for a variant.

.PARAMETER VariantKey
The variant key to analyze.

.PARAMETER CurrentManifest
The current BUILD_MANIFEST.json object.
#>
function Show-ComponentChanges {
    param(
        [string]$VariantKey,
        [object]$CurrentManifest
    )
    
    $variants = Get-BuildVariants
    $selectedVariant = $variants[$VariantKey]
    $currentComponents = if ($CurrentManifest -and $CurrentManifest.components) {
        $CurrentManifest.components | Where-Object { $_.enabled -eq $true } | Select-Object -ExpandProperty name
    } else {
        @()
    }
    
    Write-Host ""
    Write-Host "Component Changes:" -ForegroundColor Cyan
    Write-Host ""
    
    # Components to add
    $toAdd = $selectedVariant.Components | Where-Object { $_ -notin $currentComponents }
    if ($toAdd) {
        Write-Host "Components to ADD:" -ForegroundColor Green
        foreach ($component in $toAdd) {
            Write-Host "  + $component" -ForegroundColor Green
        }
    } else {
        Write-Host "No components to add" -ForegroundColor Gray
    }
    
    Write-Host ""
    
    # Components to remove
    $toRemove = $currentComponents | Where-Object { $_ -notin $selectedVariant.Components -and $_ -notin $selectedVariant.Optional }
    if ($toRemove) {
        Write-Host "Components to REMOVE:" -ForegroundColor Red
        foreach ($component in $toRemove) {
            Write-Host "  - $component" -ForegroundColor Red
        }
    } else {
        Write-Host "No components to remove" -ForegroundColor Gray
    }
    
    Write-Host ""
}

<#
.SYNOPSIS
Prompts for confirmation before proceeding with build selection.

.PARAMETER VariantKey
The variant key to confirm.

.OUTPUTS
$true if user confirms, $false otherwise.
#>
function Get-UserConfirmation {
    param([string]$VariantKey)
    
    $variants = Get-BuildVariants
    $selectedVariant = $variants[$VariantKey]
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "Build Selection Summary" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Selected Variant: $($selectedVariant.Name)" -ForegroundColor White
    Write-Host "Total Size: $($selectedVariant.Size)" -ForegroundColor White
    Write-Host "Estimated Install Time: $($selectedVariant.InstallTime)" -ForegroundColor White
    Write-Host "Total Components: $($selectedVariant.Components.Count + $selectedVariant.Optional.Count)" -ForegroundColor White
    Write-Host ""
    
    $response = Read-Host "Proceed with build selection? (yes/no)"
    
    if ($response -eq 'yes' -or $response -eq 'y') {
        return $true
    }
    
    return $false
}

<#
.SYNOPSIS
Updates the BUILD_MANIFEST.json with the selected variant.

.PARAMETER VariantKey
The variant key to apply.

.OUTPUTS
$true on success, $false on failure.
#>
function Update-BuildManifest {
    param([string]$VariantKey)
    
    $variants = Get-BuildVariants
    $selectedVariant = $variants[$VariantKey]
    
    try {
        # Load current manifest or create new one
        $manifest = if (Test-Path $manifestPath) {
            Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
        } else {
            @{
                version = "1.0"
                lastUpdated = (Get-Date -Format "o")
                selectedVariant = $null
                components = @()
            }
        }
        
        # Update manifest
        $manifest.selectedVariant = $VariantKey
        $manifest.lastUpdated = (Get-Date -Format "o")
        $manifest.variantName = $selectedVariant.Name
        $manifest.variantDescription = $selectedVariant.Description
        $manifest.estimatedSize = $selectedVariant.Size
        $manifest.estimatedInstallTime = $selectedVariant.InstallTime
        
        # Update components
        $componentList = @()
        
        foreach ($component in $selectedVariant.Components) {
            $componentList += @{
                name = $component
                enabled = $true
                required = $true
                type = 'core'
            }
        }
        
        foreach ($component in $selectedVariant.Optional) {
            $componentList += @{
                name = $component
                enabled = $true
                required = $false
                type = 'optional'
            }
        }
        
        $manifest.components = $componentList
        
        # Save manifest
        $manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath -Encoding UTF8
        
        Write-Log -Message "Updated BUILD_MANIFEST.json with variant: $VariantKey" -Level Success
        return $true
    }
    catch {
        Write-Log -Message "Failed to update manifest: $_" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
Installs components via the orchestrator.

.PARAMETER VariantKey
The variant key components to install.

.OUTPUTS
$true on success, $false on failure.
#>
function Install-Components {
    param([string]$VariantKey)
    
    if (-not (Test-Path $orchestratorPath)) {
        Write-Log -Message "Orchestrator not found at $orchestratorPath" -Level Error
        return $false
    }
    
    try {
        Write-Host ""
        Write-Host "Installing components..." -ForegroundColor Cyan
        
        # Call orchestrator to install components
        & $orchestratorPath -Action Install -Variant $VariantKey -Verbose:$Verbose
        
        if ($LASTEXITCODE -eq 0) {
            Write-Log -Message "Components installed successfully for variant: $VariantKey" -Level Success
            return $true
        } else {
            Write-Log -Message "Component installation failed with exit code: $LASTEXITCODE" -Level Error
            return $false
        }
    }
    catch {
        Write-Log -Message "Failed to install components: $_" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
Main function to handle build selection flow.
#>
function Invoke-BuildSelection {
    Write-Log -Message "Build selection started" -Level Info
    
    # Get current manifest
    $currentManifest = Get-BuildManifest
    
    # Determine variant to select
    if (-not $Variant) {
        $Variant = Show-InteractiveMenu
        
        if (-not $Variant) {
            Write-Log -Message "Build selection cancelled" -Level Info
            return $false
        }
    }
    
    # Validate variant
    $validVariants = @('minimal', 'standard', 'developer', 'enterprise', 'gpu-optimized', 'edge-deployment', 'all-features')
    if ($Variant -notin $validVariants) {
        Write-Log -Message "Invalid variant: $Variant" -Level Error
        return $false
    }
    
    # Show component changes
    Show-ComponentChanges -VariantKey $Variant -CurrentManifest $currentManifest
    
    # Get confirmation
    if (-not (Get-UserConfirmation -VariantKey $Variant)) {
        Write-Log -Message "Build selection cancelled by user" -Level Info
        return $false
    }
    
    # Check WhatIf
    if ($PSCmdlet.ShouldProcess("Build variant: $Variant", "Select")) {
        # Update manifest
        if (-not (Update-BuildManifest -VariantKey $Variant)) {
            return $false
        }
        
        # Install components
        if (-not (Install-Components -VariantKey $Variant)) {
            return $false
        }
        
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Build selection completed successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        
        return $true
    } else {
        Write-Host "WhatIf: Would select build variant: $Variant" -ForegroundColor Blue
        return $true
    }
}

# Main execution
try {
    $result = Invoke-BuildSelection
    exit if ($result) { 0 } else { 1 }
}
catch {
    Write-Log -Message "Unhandled error: $_" -Level Error
    Write-Log -Message $_.ScriptStackTrace -Level Error
    exit 1
}

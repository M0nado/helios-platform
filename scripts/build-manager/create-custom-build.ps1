<#
.SYNOPSIS
Creates a custom build variant with user-selected components.

.DESCRIPTION
Allows interactive selection of individual components to create a custom build variant.
Validates component compatibility, calculates total size and installation time, and saves
the configuration as a reusable variant. Supports both interactive and JSON-based configuration.

.PARAMETER ConfigFile
Path to JSON file containing component configuration.

.PARAMETER VariantName
Name for the custom variant.

.PARAMETER VariantDescription
Description for the custom variant.

.PARAMETER Interactive
Force interactive component selection (default).

.PARAMETER SaveVariant
Save the custom variant for future use.

.PARAMETER Verbose
Enables verbose logging output.

.EXAMPLE
.\create-custom-build.ps1
# Interactively create a custom build

.EXAMPLE
.\create-custom-build.ps1 -ConfigFile custom-config.json -VariantName "my-custom"
# Create custom build from JSON configuration

.EXAMPLE
.\create-custom-build.ps1 -Interactive -SaveVariant
# Create custom build interactively and save as reusable variant

.NOTES
Author: Helios Build System
Version: 1.0
#>

[CmdletBinding()]
param(
    [string]$ConfigFile,
    [string]$VariantName,
    [string]$VariantDescription,
    [switch]$Interactive,
    [switch]$SaveVariant,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

# Define script paths
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$manifestPath = Join-Path $projectRoot "BUILD_MANIFEST.json"
$variantsDir = Join-Path $scriptRoot "custom-variants"
$logPath = Join-Path $scriptRoot "logs\custom-builds.log"

# Create directories if needed
@($variantsDir, (Split-Path -Parent $logPath)) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -Path $_ -ItemType Directory -Force | Out-Null
    }
}

<#
.SYNOPSIS
Logs a message to file and console.
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
Gets all available components with their properties.
#>
function Get-AllComponents {
    return @{
        'core' = @{ Size = 128; Time = 1; Required = $true; Type = 'core' }
        'basic-ui' = @{ Size = 256; Time = 1; Required = $false; Type = 'ui' }
        'advanced-ui' = @{ Size = 384; Time = 2; Required = $false; Type = 'ui' }
        'lightweight-ui' = @{ Size = 96; Time = 0.5; Required = $false; Type = 'ui' }
        'network-stack' = @{ Size = 64; Time = 1; Required = $true; Type = 'core' }
        'logging' = @{ Size = 32; Time = 0.5; Required = $false; Type = 'optional' }
        'monitoring' = @{ Size = 96; Time = 1; Required = $false; Type = 'optional' }
        'database-client' = @{ Size = 128; Time = 1; Required = $false; Type = 'optional' }
        'development-tools' = @{ Size = 512; Time = 2; Required = $false; Type = 'dev' }
        'debug-utilities' = @{ Size = 256; Time = 1; Required = $false; Type = 'dev' }
        'testing-framework' = @{ Size = 384; Time = 2; Required = $false; Type = 'dev' }
        'enterprise-security' = @{ Size = 256; Time = 2; Required = $false; Type = 'optional' }
        'advanced-analytics' = @{ Size = 512; Time = 2; Required = $false; Type = 'optional' }
        'gpu-acceleration' = @{ Size = 768; Time = 3; Required = $false; Type = 'accelerator' }
        'ml-toolkit' = @{ Size = 1024; Time = 3; Required = $false; Type = 'accelerator' }
        'clustering' = @{ Size = 192; Time = 1; Required = $false; Type = 'optional' }
        'backup-recovery' = @{ Size = 256; Time = 1; Required = $false; Type = 'optional' }
        'profiling-tools' = @{ Size = 256; Time = 1; Required = $false; Type = 'dev' }
        'multimedia' = @{ Size = 512; Time = 2; Required = $false; Type = 'optional' }
        'edge-connector' = @{ Size = 64; Time = 0.5; Required = $false; Type = 'optional' }
        'local-cache' = @{ Size = 128; Time = 0.5; Required = $false; Type = 'optional' }
        'compute-libraries' = @{ Size = 512; Time = 1; Required = $false; Type = 'accelerator' }
        'offline-mode' = @{ Size = 96; Time = 0.5; Required = $false; Type = 'optional' }
        'documentation-generator' = @{ Size = 128; Time = 1; Required = $false; Type = 'dev' }
    }
}

<#
.SYNOPSIS
Gets component dependencies.
#>
function Get-ComponentDependencies {
    return @{
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
}

<#
.SYNOPSIS
Shows interactive component selection menu.

.OUTPUTS
Array of selected component names.
#>
function Show-InteractiveComponentSelection {
    $components = Get-AllComponents
    $selected = @('core', 'network-stack')  # Required components
    $dependencies = Get-ComponentDependencies
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  Custom Build Component Selector" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
    
    Write-Host "Required components (always included):" -ForegroundColor Green
    foreach ($comp in @('core', 'network-stack')) {
        Write-Host "  ✓ $comp" -ForegroundColor Green
    }
    Write-Host ""
    
    # Group components by type
    $types = @($components.Values | Select-Object -ExpandProperty Type | Sort-Object -Unique)
    
    foreach ($type in $types) {
        if ($type -eq 'core') { continue }
        
        $typeComponents = $components | Where-Object { $_.Value.Type -eq $type }
        
        Write-Host "Select $type components (enter 'done' when finished):" -ForegroundColor Yellow
        Write-Host ""
        
        $index = 1
        $typeList = @()
        
        foreach ($compName in $typeComponents.Keys) {
            $comp = $typeComponents[$compName]
            $typeList += $compName
            
            $selectedMarker = if ($compName -in $selected) { "✓" } else { "○" }
            Write-Host "  [$index] $selectedMarker $compName (Size: $($comp.Size) MB, Time: $($comp.Time) min)" -ForegroundColor Gray
            $index++
        }
        
        Write-Host ""
        
        do {
            $input = Read-Host "Enter selection (1-$($typeList.Count)), 'a' for all, 'n' for none, or 'done'"
            
            if ($input -eq 'done') {
                break
            }
            
            if ($input -eq 'a') {
                foreach ($comp in $typeList) {
                    if ($comp -notin $selected) {
                        $selected += $comp
                    }
                }
                Write-Host "Added all $type components" -ForegroundColor Green
                break
            }
            
            if ($input -eq 'n') {
                foreach ($comp in $typeList) {
                    if ($comp -in $selected) {
                        $selected = $selected | Where-Object { $_ -ne $comp }
                    }
                }
                Write-Host "Removed all $type components" -ForegroundColor Green
                break
            }
            
            if ([int]$input -ge 1 -and [int]$input -le $typeList.Count) {
                $component = $typeList[[int]$input - 1]
                
                if ($component -in $selected) {
                    $selected = $selected | Where-Object { $_ -ne $component }
                    Write-Host "Removed: $component" -ForegroundColor Red
                } else {
                    $selected += $component
                    Write-Host "Added: $component" -ForegroundColor Green
                }
            } else {
                Write-Host "Invalid input. Please try again." -ForegroundColor Red
            }
        } while ($true)
        
        Write-Host ""
    }
    
    # Resolve dependencies
    $resolved = Resolve-Dependencies -Components $selected
    
    return $resolved
}

<#
.SYNOPSIS
Resolves component dependencies.

.PARAMETER Components
Array of component names.

.OUTPUTS
Array of components with dependencies resolved.
#>
function Resolve-Dependencies {
    param([string[]]$Components)
    
    $all = @($Components)
    $dependencies = Get-ComponentDependencies
    $resolved = $true
    
    do {
        $resolved = $true
        $newComponents = @()
        
        foreach ($comp in $all) {
            if ($dependencies.ContainsKey($comp)) {
                $deps = $dependencies[$comp]
                foreach ($dep in $deps) {
                    if ($dep -notin $all) {
                        $newComponents += $dep
                        $resolved = $false
                    }
                }
            }
        }
        
        $all = @($all + $newComponents | Sort-Object -Unique)
    } while (-not $resolved)
    
    return $all
}

<#
.SYNOPSIS
Loads configuration from JSON file.

.OUTPUTS
Array of component names.
#>
function Load-ConfigurationFromFile {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        Write-Log -Message "Configuration file not found: $FilePath" -Level Error
        return $null
    }
    
    try {
        $config = Get-Content -Path $FilePath -Raw | ConvertFrom-Json
        Write-Verbose "Loaded configuration from $FilePath"
        
        $components = if ($config.components) {
            @($config.components)
        } else {
            @($config)
        }
        
        # Resolve dependencies
        return Resolve-Dependencies -Components $components
    }
    catch {
        Write-Log -Message "Failed to load configuration: $_" -Level Error
        return $null
    }
}

<#
.SYNOPSIS
Calculates total size and time for components.

.OUTPUTS
Hashtable with Size and Time properties.
#>
function Get-ComponentMetrics {
    param([string[]]$Components)
    
    $allComps = Get-AllComponents
    $totalSize = 0
    $totalTime = 0
    
    foreach ($comp in $Components) {
        if ($allComps.ContainsKey($comp)) {
            $totalSize += $allComps[$comp].Size
            $totalTime += $allComps[$comp].Time
        }
    }
    
    return @{
        Size = $totalSize
        Time = [math]::Round($totalTime, 1)
    }
}

<#
.SYNOPSIS
Displays custom build summary.
#>
function Show-CustomBuildSummary {
    param(
        [string[]]$Components,
        [string]$Name,
        [string]$Description
    )
    
    $metrics = Get-ComponentMetrics -Components $Components
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Custom Build Summary" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    if ($Name) {
        Write-Host "Name: $Name" -ForegroundColor Yellow
    }
    
    if ($Description) {
        Write-Host "Description: $Description" -ForegroundColor Gray
    }
    
    Write-Host "Total Components: $($Components.Count)" -ForegroundColor White
    Write-Host "Total Size: ~$($metrics.Size) MB" -ForegroundColor White
    Write-Host "Estimated Install Time: ~$($metrics.Time) minutes" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Components:" -ForegroundColor Cyan
    foreach ($comp in $Components | Sort-Object) {
        Write-Host "  • $comp" -ForegroundColor Gray
    }
    
    Write-Host ""
}

<#
.SYNOPSIS
Saves custom variant for future use.

.OUTPUTS
$true on success, $false on failure.
#>
function Save-CustomVariant {
    param(
        [string]$Name,
        [string]$Description,
        [string[]]$Components
    )
    
    if (-not $Name) {
        Write-Log -Message "Variant name required to save" -Level Warning
        return $false
    }
    
    try {
        $variant = @{
            name = $Name
            description = $Description
            components = @($Components)
            created = (Get-Date -Format "o")
        }
        
        $fileName = "$Name.json"
        $filePath = Join-Path $variantsDir $fileName
        
        $variant | ConvertTo-Json | Set-Content -Path $filePath -Encoding UTF8
        
        Write-Log -Message "Custom variant saved: $Name" -Level Success
        Write-Host "Variant saved to: $filePath" -ForegroundColor Green
        
        return $true
    }
    catch {
        Write-Log -Message "Failed to save variant: $_" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
Updates manifest with custom build.

.OUTPUTS
$true on success, $false on failure.
#>
function Update-ManifestWithCustomBuild {
    param(
        [string[]]$Components,
        [string]$Name,
        [string]$Description
    )
    
    try {
        # Load or create manifest
        $manifest = if (Test-Path $manifestPath) {
            Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
        } else {
            @{
                version = "1.0"
                selectedVariant = "custom"
                components = @()
            }
        }
        
        # Update manifest
        $manifest.selectedVariant = "custom"
        $manifest.variantName = $Name
        $manifest.variantDescription = $Description
        $manifest.lastUpdated = (Get-Date -Format "o")
        
        # Build component list
        $allComps = Get-AllComponents
        $componentList = @()
        
        foreach ($comp in $Components) {
            $componentList += @{
                name = $comp
                enabled = $true
                required = if ($allComps[$comp].Required) { $true } else { $false }
                type = $allComps[$comp].Type
            }
        }
        
        $manifest.components = $componentList
        
        # Save manifest
        $manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath -Encoding UTF8
        
        Write-Log -Message "Manifest updated with custom build" -Level Success
        return $true
    }
    catch {
        Write-Log -Message "Failed to update manifest: $_" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
Main function to create custom build.
#>
function Invoke-CreateCustomBuild {
    Write-Log -Message "Create custom build operation started" -Level Info
    
    # Get components
    $components = $null
    
    if ($ConfigFile) {
        $components = Load-ConfigurationFromFile -FilePath $ConfigFile
    } else {
        $components = Show-InteractiveComponentSelection
    }
    
    if (-not $components -or $components.Count -eq 0) {
        Write-Log -Message "No components selected" -Level Error
        return $false
    }
    
    # Get name and description if not provided
    if (-not $VariantName) {
        Write-Host ""
        $VariantName = Read-Host "Enter variant name (or press Enter to skip)"
    }
    
    if (-not $VariantDescription) {
        $VariantDescription = Read-Host "Enter variant description (or press Enter to skip)"
    }
    
    # Show summary
    Show-CustomBuildSummary -Components $components -Name $VariantName -Description $VariantDescription
    
    # Confirm
    $response = Read-Host "Proceed with custom build? (yes/no)"
    if ($response -ne 'yes' -and $response -ne 'y') {
        Write-Log -Message "Custom build cancelled" -Level Info
        return $false
    }
    
    # Update manifest
    if (-not (Update-ManifestWithCustomBuild -Components $components -Name $VariantName -Description $VariantDescription)) {
        return $false
    }
    
    # Save variant if requested
    if ($SaveVariant -and $VariantName) {
        Save-CustomVariant -Name $VariantName -Description $VariantDescription -Components $components
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Custom Build Created Successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    
    return $true
}

# Main execution
try {
    $result = Invoke-CreateCustomBuild
    exit if ($result) { 0 } else { 1 }
}
catch {
    Write-Log -Message "Unhandled error: $_" -Level Error
    Write-Log -Message $_.ScriptStackTrace -Level Error
    exit 1
}

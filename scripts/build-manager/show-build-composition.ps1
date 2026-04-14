<#
.SYNOPSIS
Displays current build contents and composition.

.DESCRIPTION
Shows all enabled and disabled features in the current build, component version tracking,
installed size display, and component counts. Can export detailed reports to Markdown or HTML.

.PARAMETER ExportMarkdown
Export build composition to Markdown file.

.PARAMETER ExportHtml
Export build composition to HTML file.

.PARAMETER ShowDisabled
Include disabled components in the output.

.PARAMETER Verbose
Enables verbose logging output.

.EXAMPLE
.\show-build-composition.ps1
# Display current build composition

.EXAMPLE
.\show-build-composition.ps1 -ShowDisabled -Verbose
# Display all components (enabled and disabled) with verbose output

.EXAMPLE
.\show-build-composition.ps1 -ExportMarkdown -ExportHtml
# Display and export to both Markdown and HTML

.NOTES
Author: Helios Build System
Version: 1.0
#>

[CmdletBinding()]
param(
    [switch]$ExportMarkdown,
    [switch]$ExportHtml,
    [switch]$ShowDisabled,
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = if ($Verbose) { 'Continue' } else { 'SilentlyContinue' }

# Define script paths
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$manifestPath = Join-Path $projectRoot "BUILD_MANIFEST.json"
$reportsDir = Join-Path $scriptRoot "reports"
$logPath = Join-Path $scriptRoot "logs\show-composition.log"

# Create directories if needed
@($reportsDir, (Split-Path -Parent $logPath)) | ForEach-Object {
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
Loads the BUILD_MANIFEST.json configuration.
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
Calculates total size of enabled components.
#>
function Get-InstalledSize {
    param([object]$Manifest)
    
    $componentSizes = @{
        'core' = 128
        'basic-ui' = 256
        'advanced-ui' = 384
        'lightweight-ui' = 96
        'network-stack' = 64
        'logging' = 32
        'monitoring' = 96
        'database-client' = 128
        'development-tools' = 512
        'debug-utilities' = 256
        'testing-framework' = 384
        'enterprise-security' = 256
        'advanced-analytics' = 512
        'gpu-acceleration' = 768
        'ml-toolkit' = 1024
        'clustering' = 192
        'backup-recovery' = 256
        'profiling-tools' = 256
        'multimedia' = 512
        'edge-connector' = 64
        'local-cache' = 128
        'compute-libraries' = 512
        'offline-mode' = 96
        'documentation-generator' = 128
    }
    
    $total = 0
    
    if ($Manifest -and $Manifest.components) {
        foreach ($component in $Manifest.components) {
            if ($component.enabled) {
                $total += $componentSizes[$component.name]
            }
        }
    }
    
    return $total
}

<#
.SYNOPSIS
Displays build composition in console.
#>
function Show-ConsoleComposition {
    param([object]$Manifest)
    
    if (-not $Manifest) {
        Write-Host "No build manifest found" -ForegroundColor Red
        return $false
    }
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  Current Build Composition" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
    
    # Build summary
    Write-Host "Build Variant: " -ForegroundColor White -NoNewline
    Write-Host "$($Manifest.selectedVariant)" -ForegroundColor Yellow
    
    if ($Manifest.variantName) {
        Write-Host "Variant Name: " -ForegroundColor White -NoNewline
        Write-Host "$($Manifest.variantName)" -ForegroundColor Cyan
    }
    
    Write-Host ""
    
    # Statistics
    $enabledCount = @($Manifest.components | Where-Object { $_.enabled }).Count
    $totalCount = @($Manifest.components).Count
    $coreCount = @($Manifest.components | Where-Object { $_.enabled -and $_.type -eq 'core' }).Count
    $optionalCount = @($Manifest.components | Where-Object { $_.enabled -and $_.type -eq 'optional' }).Count
    
    $installedSize = Get-InstalledSize -Manifest $Manifest
    
    Write-Host "========== Statistics ==========" -ForegroundColor Cyan
    Write-Host "Total Components: $totalCount" -ForegroundColor White
    Write-Host "Enabled Components: $enabledCount" -ForegroundColor Green
    Write-Host "Disabled Components: $($totalCount - $enabledCount)" -ForegroundColor Red
    Write-Host "  - Core Components: $coreCount" -ForegroundColor Green
    Write-Host "  - Optional Components: $optionalCount" -ForegroundColor Green
    Write-Host "Installed Size: ~$installedSize MB" -ForegroundColor White
    
    if ($Manifest.estimatedInstallTime) {
        Write-Host "Estimated Install Time: $($Manifest.estimatedInstallTime)" -ForegroundColor White
    }
    
    if ($Manifest.lastUpdated) {
        Write-Host "Last Updated: $($Manifest.lastUpdated)" -ForegroundColor Gray
    }
    
    Write-Host ""
    
    # Enabled components
    $enabledComponents = @($Manifest.components | Where-Object { $_.enabled } | Sort-Object name)
    
    Write-Host "========== Enabled Features ==========" -ForegroundColor Green
    Write-Host ""
    
    if ($enabledComponents) {
        foreach ($component in $enabledComponents) {
            $typeLabel = if ($component.type -eq 'core') { "[CORE]" } else { "[OPT]" }
            $marker = if ($component.required) { "●" } else { "○" }
            Write-Host "  $marker $typeLabel $($component.name)" -ForegroundColor Green
        }
    } else {
        Write-Host "  No components enabled" -ForegroundColor Gray
    }
    
    Write-Host ""
    
    # Disabled components (if requested)
    if ($ShowDisabled) {
        $disabledComponents = @($Manifest.components | Where-Object { -not $_.enabled } | Sort-Object name)
        
        Write-Host "========== Disabled Features ==========" -ForegroundColor Red
        Write-Host ""
        
        if ($disabledComponents) {
            foreach ($component in $disabledComponents) {
                Write-Host "  ✗ $($component.name)" -ForegroundColor Red
            }
        } else {
            Write-Host "  No components disabled" -ForegroundColor Gray
        }
        
        Write-Host ""
    }
    
    # Component breakdown by type
    $types = @($Manifest.components | Where-Object { $_.enabled } | Select-Object -ExpandProperty type | Sort-Object -Unique)
    
    Write-Host "========== Component Breakdown ==========" -ForegroundColor Cyan
    Write-Host ""
    
    foreach ($type in $types) {
        $componentsOfType = @($Manifest.components | Where-Object { $_.enabled -and $_.type -eq $type })
        Write-Host "$($type.ToUpper()) ($($componentsOfType.Count)): " -ForegroundColor Cyan
        foreach ($component in $componentsOfType) {
            Write-Host "  • $($component.name)" -ForegroundColor Cyan
        }
        Write-Host ""
    }
    
    return $true
}

<#
.SYNOPSIS
Exports composition to Markdown file.
#>
function Export-CompositionMarkdown {
    param([object]$Manifest)
    
    if (-not $Manifest) {
        return $null
    }
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $variant = $Manifest.selectedVariant
    $fileName = "composition-$variant-$(Get-Date -Format 'yyyyMMdd-HHmmss').md"
    $filePath = Join-Path $reportsDir $fileName
    
    $enabledCount = @($Manifest.components | Where-Object { $_.enabled }).Count
    $totalCount = @($Manifest.components).Count
    $installedSize = Get-InstalledSize -Manifest $Manifest
    
    $enabledComponents = @($Manifest.components | Where-Object { $_.enabled } | Sort-Object name)
    $disabledComponents = @($Manifest.components | Where-Object { -not $_.enabled } | Sort-Object name)
    
    $markdown = @"
# Build Composition Report

**Generated:** $timestamp

## Build Information

- **Variant:** $variant
- **Variant Name:** $($Manifest.variantName)
- **Description:** $($Manifest.variantDescription)
- **Last Updated:** $($Manifest.lastUpdated)

## Statistics

| Metric | Value |
|--------|-------|
| Total Components | $totalCount |
| Enabled Components | $enabledCount |
| Disabled Components | $($totalCount - $enabledCount) |
| Installed Size | ~$installedSize MB |
| Estimated Install Time | $($Manifest.estimatedInstallTime) |

## Enabled Features ($enabledCount)

$($enabledComponents | ForEach-Object {
    $marker = if ($_.required) { "●" } else { "○" }
    $type = $_.type.ToUpper()
    "- $marker **$($_.name)** `[$type`]"
} | Out-String)

## Disabled Features ($($disabledComponents.Count))

$($disabledComponents | ForEach-Object {
    "- ✗ $($_.name)"
} | Out-String)

## Component Breakdown

$($Manifest.components | Where-Object { $_.enabled } | Group-Object type | ForEach-Object {
    "`n### $($_.Name.ToUpper()) ($($_.Group.Count))`n"
    $_.Group | ForEach-Object {
        "- $($_.name)"
    } | Out-String
} | Out-String)

---
*Generated by Helios Build Manager*
"@
    
    try {
        $markdown | Set-Content -Path $filePath -Encoding UTF8
        Write-Log -Message "Markdown report exported to $filePath" -Level Success
        Write-Host "Markdown report: $filePath" -ForegroundColor Green
        return $filePath
    }
    catch {
        Write-Log -Message "Failed to export Markdown: $_" -Level Error
        return $null
    }
}

<#
.SYNOPSIS
Exports composition to HTML file.
#>
function Export-CompositionHtml {
    param([object]$Manifest)
    
    if (-not $Manifest) {
        return $null
    }
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $variant = $Manifest.selectedVariant
    $fileName = "composition-$variant-$(Get-Date -Format 'yyyyMMdd-HHmmss').html"
    $filePath = Join-Path $reportsDir $fileName
    
    $enabledCount = @($Manifest.components | Where-Object { $_.enabled }).Count
    $totalCount = @($Manifest.components).Count
    $installedSize = Get-InstalledSize -Manifest $Manifest
    
    $enabledComponents = @($Manifest.components | Where-Object { $_.enabled } | Sort-Object name)
    $disabledComponents = @($Manifest.components | Where-Object { -not $_.enabled } | Sort-Object name)
    
    $componentRows = ""
    foreach ($component in $enabledComponents) {
        $type = $component.type
        $status = if ($component.required) { "Required" } else { "Optional" }
        $componentRows += "    <tr><td>$($component.name)</td><td><span class='badge enabled'>Enabled</span></td><td>$type</td><td>$status</td></tr>`n"
    }
    
    foreach ($component in $disabledComponents) {
        $componentRows += "    <tr><td>$($component.name)</td><td><span class='badge disabled'>Disabled</span></td><td>$($component.type)</td><td>Optional</td></tr>`n"
    }
    
    $html = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Build Composition - $variant</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: Segoe UI, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; min-height: 100vh; }
        .container { max-width: 1000px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); }
        h1 { color: #333; margin-bottom: 10px; }
        .header-info { color: #999; font-size: 14px; margin-bottom: 30px; }
        .stats-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 15px; margin: 30px 0; }
        .stat-box { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 6px; }
        .stat-value { font-size: 24px; font-weight: bold; }
        .stat-label { font-size: 12px; opacity: 0.9; margin-top: 5px; }
        .section { margin: 30px 0; }
        .section h2 { color: #333; font-size: 18px; margin: 20px 0 15px 0; padding-bottom: 10px; border-bottom: 2px solid #667eea; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th { background: #f5f5f5; padding: 12px; text-align: left; font-weight: 600; border-bottom: 2px solid #ddd; }
        td { padding: 10px 12px; border-bottom: 1px solid #eee; }
        tr:hover { background: #f9f9f9; }
        .badge { padding: 4px 8px; border-radius: 3px; font-size: 12px; font-weight: 600; }
        .badge.enabled { background: #d4edda; color: #155724; }
        .badge.disabled { background: #f8d7da; color: #721c24; }
        .component-list { display: grid; grid-template-columns: repeat(2, 1fr); gap: 15px; }
        .component-item { padding: 12px; background: #f9f9f9; border-radius: 4px; border-left: 4px solid #667eea; }
        .component-name { font-weight: 600; color: #333; }
        .component-type { font-size: 12px; color: #999; margin-top: 4px; }
        .timestamp { color: #999; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; }
    </style>
</head>
<body>
    <div class="container">
        <h1>Build Composition Report</h1>
        <div class="header-info">
            <strong>Variant:</strong> $variant | 
            <strong>Generated:</strong> $timestamp
        </div>
        
        <div class="stats-grid">
            <div class="stat-box">
                <div class="stat-value">$totalCount</div>
                <div class="stat-label">Total Components</div>
            </div>
            <div class="stat-box">
                <div class="stat-value">$enabledCount</div>
                <div class="stat-label">Enabled Components</div>
            </div>
            <div class="stat-box">
                <div class="stat-value">~$installedSize MB</div>
                <div class="stat-label">Installed Size</div>
            </div>
            <div class="stat-box">
                <div class="stat-value">$($Manifest.estimatedInstallTime)</div>
                <div class="stat-label">Est. Install Time</div>
            </div>
        </div>
        
        <div class="section">
            <h2>Component Details</h2>
            <table>
                <thead>
                    <tr>
                        <th>Component</th>
                        <th>Status</th>
                        <th>Type</th>
                        <th>Requirement</th>
                    </tr>
                </thead>
                <tbody>
                    $componentRows
                </tbody>
            </table>
        </div>
        
        <div class="timestamp">
            Generated by Helios Build Manager on $timestamp
        </div>
    </div>
</body>
</html>
"@
    
    try {
        $html | Set-Content -Path $filePath -Encoding UTF8
        Write-Log -Message "HTML report exported to $filePath" -Level Success
        Write-Host "HTML report: $filePath" -ForegroundColor Green
        return $filePath
    }
    catch {
        Write-Log -Message "Failed to export HTML: $_" -Level Error
        return $null
    }
}

<#
.SYNOPSIS
Main function to show build composition.
#>
function Invoke-ShowComposition {
    Write-Log -Message "Show composition operation started" -Level Info
    
    $manifest = Get-BuildManifest
    if (-not $manifest) {
        return $false
    }
    
    if (-not (Show-ConsoleComposition -Manifest $manifest)) {
        return $false
    }
    
    if ($ExportMarkdown) {
        Export-CompositionMarkdown -Manifest $manifest
    }
    
    if ($ExportHtml) {
        Export-CompositionHtml -Manifest $manifest
    }
    
    Write-Log -Message "Show composition operation completed successfully" -Level Success
    return $true
}

# Main execution
try {
    $result = Invoke-ShowComposition
    exit if ($result) { 0 } else { 1 }
}
catch {
    Write-Log -Message "Unhandled error: $_" -Level Error
    Write-Log -Message $_.ScriptStackTrace -Level Error
    exit 1
}

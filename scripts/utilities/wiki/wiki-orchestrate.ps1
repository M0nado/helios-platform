<#
.SYNOPSIS
    Master orchestration script for wiki generation and management

.DESCRIPTION
    Coordinates all wiki utilities:
    - Initialize database
    - Generate wiki documentation
    - Validate cross-references
    - Map dependencies
    - Perform full system checks
    
    Supports incremental updates and full regeneration.

.PARAMETER Action
    'init', 'generate', 'validate', 'map', 'full', 'search'

.PARAMETER SearchQuery
    Query string for search action

.PARAMETER Incremental
    Only update changed files

.PARAMETER Verbose
    Show detailed output

.EXAMPLE
    .\wiki-orchestrate.ps1 -Action full

.EXAMPLE
    .\wiki-orchestrate.ps1 -Action generate -Incremental

.EXAMPLE
    .\wiki-orchestrate.ps1 -Action search -SearchQuery "authentication"

.NOTES
    Location: scripts/utilities/wiki/wiki-orchestrate.ps1
    Version: 1.0
#>

param(
    [ValidateSet('init', 'generate', 'validate', 'map', 'full', 'search')]
    [string]$Action = 'full',
    [string]$SearchQuery,
    [switch]$Incremental,
    [switch]$Verbose
)

$wikiDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $wikiDir))

function Write-Status {
    param([string]$Message, [ValidateSet('Info', 'Success', 'Warning', 'Error')]$Type = 'Info')
    $colors = @{
        'Info'    = 'Cyan'
        'Success' = 'Green'
        'Warning' = 'Yellow'
        'Error'   = 'Red'
    }
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor $colors[$Type]
}

function Show-Menu {
    Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║        HELIOS WIKI ORCHESTRATOR        ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Cyan
    
    Write-Host "Actions:" -ForegroundColor Green
    Write-Host "  init     - Initialize wiki database"
    Write-Host "  generate - Generate wiki documentation"
    Write-Host "  validate - Check cross-references"
    Write-Host "  map      - Map dependencies"
    Write-Host "  full     - Complete wiki regeneration"
    Write-Host "  search   - Query wiki database"
    Write-Host ""
}

function Invoke-Action {
    param([string]$Action)
    
    switch ($Action) {
        'init' {
            Write-Status "Initializing wiki database..."
            & "$wikiDir\setup-wiki.ps1" -ForceReset
        }
        'generate' {
            Write-Status "Generating wiki documentation..."
            if ($Incremental) {
                & "$wikiDir\generate-wiki.ps1" -UpdateDb
            } else {
                & "$wikiDir\generate-wiki.ps1" -GenerateHtml -UpdateDb
            }
        }
        'validate' {
            Write-Status "Validating cross-references..."
            & "$wikiDir\check-cross-references.ps1" -ValidateFiles -CheckCircular -GenerateReport
        }
        'map' {
            Write-Status "Mapping dependencies..."
            & "$wikiDir\map-dependencies.ps1" -OutputFormat all
        }
        'full' {
            Write-Status "Starting full wiki regeneration..."
            Write-Host ""
            
            & "$wikiDir\setup-wiki.ps1" -ForceReset
            Write-Host ""
            
            & "$wikiDir\generate-wiki.ps1" -GenerateHtml -UpdateDb
            Write-Host ""
            
            & "$wikiDir\check-cross-references.ps1" -ValidateFiles -CheckCircular -GenerateReport
            Write-Host ""
            
            & "$wikiDir\map-dependencies.ps1" -OutputFormat all
            Write-Host ""
            
            Write-Status "Full regeneration complete!" "Success"
        }
        'search' {
            if ($SearchQuery) {
                Write-Status "Searching wiki..."
                & "$wikiDir\wiki-search.ps1" -SearchType keyword -Query $SearchQuery
            } else {
                Write-Status "Search query required" "Error"
            }
        }
    }
}

# Main
Show-Menu
Invoke-Action $Action

Write-Host "`n✓ Operation complete`n"

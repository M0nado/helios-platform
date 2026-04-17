#requires -Version 5.1
<#
.SYNOPSIS
    Master setup script for complete wiki utilities suite initialization.

.DESCRIPTION
    Orchestrates complete wiki infrastructure setup including:
    - Database creation and schema
    - Wiki generation from source files
    - Cross-reference validation
    - Dependency mapping
    - Final verification

.PARAMETER SkipDatabase
    Skip database creation

.PARAMETER SkipGeneration
    Skip wiki generation

.PARAMETER SkipValidation
    Skip validation checks

.PARAMETER GenerateHTML
    Include HTML wiki generation

.PARAMETER RunAll
    Run all components (default: true)

.EXAMPLE
    .\wiki-setup-complete.ps1
    .\wiki-setup-complete.ps1 -GenerateHTML -Verbose
#>

[CmdletBinding()]
param(
    [switch]$SkipDatabase,
    [switch]$SkipGeneration,
    [switch]$SkipValidation,
    [switch]$GenerateHTML,
    [switch]$RunAll = $true
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$wikiDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $wikiDir))

Write-Host "`n" -ForegroundColor Cyan
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        HELIOS PLATFORM WIKI UTILITIES - COMPLETE SETUP     ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`nConfiguration:" -ForegroundColor Yellow
Write-Host "  Project Root: $projectRoot" -ForegroundColor Gray
Write-Host "  Wiki Scripts: $wikiDir" -ForegroundColor Gray
Write-Host "  Database: $projectRoot\docs\wiki.db" -ForegroundColor Gray

# Step 1: Setup Database
if (-not $SkipDatabase) {
    Write-Host "`n[1/4] Setting up database..." -ForegroundColor Cyan
    try {
        & "$wikiDir\setup-wiki.ps1" -Verbose -Force
        Write-Host "✅ Database setup completed" -ForegroundColor Green
    } catch {
        Write-Host "❌ Database setup failed: $_" -ForegroundColor Red
        exit 1
    }
}

# Step 2: Generate Wiki
if (-not $SkipGeneration) {
    Write-Host "`n[2/4] Generating wiki..." -ForegroundColor Cyan
    try {
        $params = @{
            ProjectRoot = $projectRoot
            UpdateDatabase = $true
            GenerateHtml = $GenerateHTML
            Verbose = $true
        }
        & "$wikiDir\generate-wiki.ps1" @params
        Write-Host "✅ Wiki generation completed" -ForegroundColor Green
    } catch {
        Write-Host "❌ Wiki generation failed: $_" -ForegroundColor Red
        exit 1
    }
}

# Step 3: Validate References
if (-not $SkipValidation) {
    Write-Host "`n[3/4] Validating cross-references..." -ForegroundColor Cyan
    try {
        & "$wikiDir\check-cross-references.ps1" -GenerateReport -Verbose
        Write-Host "✅ Validation completed" -ForegroundColor Green
    } catch {
        Write-Host "❌ Validation failed: $_" -ForegroundColor Red
        exit 1
    }
}

# Step 4: Map Dependencies
Write-Host "`n[4/4] Mapping dependencies..." -ForegroundColor Cyan
try {
    & "$wikiDir\map-dependencies.ps1" -Format all -GenerateVisualization -Verbose
    Write-Host "✅ Dependency mapping completed" -ForegroundColor Green
} catch {
    Write-Host "❌ Dependency mapping failed: $_" -ForegroundColor Red
    exit 1
}

# Final Summary
Write-Host "`n" -ForegroundColor Cyan
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║            🎉 SETUP COMPLETED SUCCESSFULLY 🎉             ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Green

Write-Host "`nGenerated Documentation:" -ForegroundColor Yellow
Write-Host "  📚 Root Index: $projectRoot\docs\WIKI\INDEX.md" -ForegroundColor Gray
Write-Host "  📂 Categories: $projectRoot\docs\WIKI\categories\" -ForegroundColor Gray
Write-Host "  📦 Modules: $projectRoot\docs\WIKI\modules\" -ForegroundColor Gray
Write-Host "  📄 Scripts: $projectRoot\docs\WIKI\scripts\" -ForegroundColor Gray
Write-Host "  🔨 Builds: $projectRoot\docs\WIKI\builds\" -ForegroundColor Gray
Write-Host "  🔗 Graphs: $projectRoot\docs\WIKI\graphs\" -ForegroundColor Gray
Write-Host "  💾 Database: $projectRoot\docs\wiki.db" -ForegroundColor Gray

Write-Host "`nNext Steps:" -ForegroundColor Yellow
Write-Host "  1. Review: $projectRoot\docs\WIKI\INDEX.md" -ForegroundColor Gray
Write-Host "  2. Search: .\wiki-search.ps1 -Query 'term'" -ForegroundColor Gray
Write-Host "  3. Check: .\check-cross-references.ps1" -ForegroundColor Gray
Write-Host "  4. Graph: .\map-dependencies.ps1 -Format dot" -ForegroundColor Gray

Write-Host "`n" -ForegroundColor Cyan

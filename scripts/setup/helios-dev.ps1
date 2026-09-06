[CmdletBinding()]
param(
    [ValidateSet('quick', 'full')]
    [string]$Profile = 'quick',
    [ValidateRange(1, 64)]
    [int]$MaxWorkers = 4,
    [switch]$ChangedOnly,
    [switch]$Serve,
    [switch]$Doctor,
    [switch]$Status,
    [switch]$Validate,
    [switch]$AllReports
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $Root

$LocalToolPaths = @(
    (Join-Path $Root '.tools\dotnet'),
    (Join-Path $Root '.tools\gh\bin'),
    (Join-Path $Root '.tools\azcli-venv\Scripts')
)
$env:PATH = (($LocalToolPaths + @($env:PATH)) -join [IO.Path]::PathSeparator)

function Get-CommandName {
    param([Parameter(Mandatory)][string[]]$Names)
    foreach ($Name in $Names) {
        if (Get-Command $Name -ErrorAction SilentlyContinue) { return $Name }
    }
    return $null
}

function Invoke-Python {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $Python = Get-CommandName @('python3', 'python', 'py')
    if (-not $Python) {
        throw 'Python 3 is required. Install Python 3.12+ or use the HELIOS Codespace.'
    }
    & $Python @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Python exited with code $LASTEXITCODE." }
}

function Write-Readiness {
    $Tools = @('git', 'gh', 'az', 'dotnet', 'python3', 'python', 'cmake', 'docker')
    Write-Host 'HELIOS Windows readiness' -ForegroundColor Cyan
    foreach ($Tool in $Tools) {
        $Found = Get-Command $Tool -ErrorAction SilentlyContinue
        $State = if ($Found) { 'ready' } else { 'not found' }
        Write-Host ("  {0,-10} {1}" -f $Tool, $State)
    }
    Write-Host 'Authentication is stored by official CLIs and is never printed by this setup.'
}

if ($Doctor) {
    Write-Readiness
    Invoke-Python @('scripts/control/doctor.py')
    exit 0
}
if ($Status) {
    Invoke-Python @('scripts/control/helios-control.py')
    exit 0
}
if ($Validate) {
    Invoke-Python @('scripts/control/validate_workflows.py')
    exit 0
}
if ($AllReports) {
    $Steps = @(
        @('scripts/control/helios-control.py'),
        @('scripts/analysis/repo_inventory.py'),
        @('scripts/analysis/hybrid_gap_analysis.py'),
        @('scripts/integrations/readiness_score.py'),
        @('scripts/github/github-inventory.py'),
        @('scripts/azure/azure-inventory.py'),
        @('scripts/analysis/branch_intelligence.py'),
        @('scripts/analysis/merge_prune_recommendations.py'),
        @('scripts/dashboard/generate-gui.py')
    )
    foreach ($Step in $Steps) { Invoke-Python $Step }
    exit 0
}

Write-Readiness
$BuildArguments = @('scripts/build_graph/build_graph.py', 'run', '--profile', $Profile, '--max-workers', "$MaxWorkers")
if ($ChangedOnly -or $Profile -eq 'quick') { $BuildArguments += '--changed-only' }
Invoke-Python $BuildArguments
Invoke-Python @('scripts/dashboard/generate-gui.py')

if ($Serve) {
    Invoke-Python @('scripts/web/helios-web.py', '--no-rebuild')
}

[CmdletBinding()]
param(
    [ValidateRange(1, 16)] [int] $ThrottleLimit = 6,
    [switch] $IncludeNetworkTools,
    [switch] $CheckAuthentication
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$configPath = Join-Path $PSScriptRoot '../config/cli-matrix.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json

$results = $config.tools | ForEach-Object -Parallel {
    $tool = $_
    if ($tool.networkRequired -and -not $using:IncludeNetworkTools) {
        return [pscustomobject]@{ id = $tool.id; found = $false; status = 'network-check-skipped'; version = $null; required = $tool.required }
    }
    $resolved = Get-Command $tool.command -ErrorAction SilentlyContinue
    if (-not $resolved) {
        return [pscustomobject]@{ id = $tool.id; found = $false; status = 'missing'; version = $null; required = $tool.required }
    }
    try {
        $output = & $tool.command @($tool.arguments) 2>&1
        $exitCode = $LASTEXITCODE
        $firstLine = $output | ForEach-Object { "$_".Trim() } | Where-Object { $_ } | Select-Object -First 1
        if ($firstLine -and $firstLine.Length -gt 180) { $firstLine = $firstLine.Substring(0, 180) }
        [pscustomobject]@{
            id = $tool.id
            found = $true
            status = if ($exitCode -eq 0) { 'ready' } else { 'error' }
            version = $firstLine
            required = $tool.required
        }
    }
    catch {
        [pscustomobject]@{ id = $tool.id; found = $true; status = 'error'; version = $null; required = $tool.required }
    }
} -ThrottleLimit $ThrottleLimit

$auth = @()
if ($CheckAuthentication) {
    $auth = $config.authenticationChecks | ForEach-Object -Parallel {
        $check = $_
        if (-not (Get-Command $check.command -ErrorAction SilentlyContinue)) {
            return [pscustomobject]@{ id = $check.id; authenticated = $false; status = 'tool-missing' }
        }
        try {
            & $check.command @($check.arguments) *> $null
            [pscustomobject]@{ id = $check.id; authenticated = ($LASTEXITCODE -eq 0); status = if ($LASTEXITCODE -eq 0) { 'authenticated' } else { 'not-authenticated' } }
        }
        catch {
            [pscustomobject]@{ id = $check.id; authenticated = $false; status = 'not-authenticated' }
        }
    } -ThrottleLimit $ThrottleLimit
}

$missingRequired = @($results | Where-Object { $_.required -and $_.status -ne 'ready' }).Count
[ordered]@{
    schemaVersion = 1
    generatedAt = [DateTimeOffset]::UtcNow.ToString('o')
    parallelism = $ThrottleLimit
    ready = ($missingRequired -eq 0)
    missingRequired = $missingRequired
    tools = @($results | Sort-Object id)
    authentication = @($auth | Sort-Object id)
} | ConvertTo-Json -Depth 6

if ($missingRequired -gt 0) { exit 2 }

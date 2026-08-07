[CmdletBinding()]
param(
    [ValidateSet('Verify', 'Plan')]
    [string] $Mode = 'Verify'
)

$ErrorActionPreference = 'Stop'
$required = @('az', 'azd', 'dotnet', 'gh', 'pwsh', 'docker', 'node', 'npm', 'jq')
$optional = @('func', 'pac', 'atk', 'claude', 'code-insiders')
$report = [ordered]@{ mode = $Mode; required = @(); optional = @(); ready = $true }
$azdFallbacks = @('C:\Program Files\Azure Dev CLI\azd.exe')
if ($env:LOCALAPPDATA) {
    $azdFallbacks = @((Join-Path $env:LOCALAPPDATA 'Programs\Azure Dev CLI\azd.exe')) + $azdFallbacks
}

$fallbacks = @{
    az     = @('C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd', 'C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd')
    azd    = $azdFallbacks
    dotnet = @('C:\Program Files\dotnet\dotnet.exe', 'C:\Program Files (x86)\dotnet\dotnet.exe')
    pwsh   = @('C:\Program Files\PowerShell\7\pwsh.exe', 'C:\Program Files\PowerShell\7-preview\pwsh.exe')
    docker = @('C:\Program Files\Docker\Docker\resources\bin\docker.exe')
    node   = @('C:\Program Files\nodejs\node.exe')
    npm    = @('C:\Program Files\nodejs\npm.cmd', 'C:\Program Files\nodejs\npm.exe')
    jq     = @('C:\Program Files\Git\usr\bin\jq.exe')
}

function Resolve-ToolPath {
    param([Parameter(Mandatory)] [string] $Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) {
        return [ordered]@{
            found = $true
            path = $command.Source
            resolution = 'path'
        }
    }

    if ($fallbacks.ContainsKey($Name)) {
        foreach ($candidate in $fallbacks[$Name]) {
            if (Test-Path $candidate) {
                return [ordered]@{
                    found = $true
                    path = $candidate
                    resolution = 'fallback'
                }
            }
        }
    }

    return [ordered]@{
        found = $false
        path = $null
        resolution = 'missing'
    }
}

foreach ($name in $required) {
    $resolved = Resolve-ToolPath -Name $name
    $report.required += [ordered]@{
        name = $name
        found = $resolved.found
        path = $resolved.path
        resolution = $resolved.resolution
    }
    if (-not $resolved.found) { $report.ready = $false }
}
foreach ($name in $optional) {
    $resolved = Resolve-ToolPath -Name $name
    $report.optional += [ordered]@{
        name = $name
        found = $resolved.found
        path = $resolved.path
        resolution = $resolved.resolution
    }
}

$az = $report.required | Where-Object { $_.name -eq 'az' } | Select-Object -First 1
if ($az.found) {
    & $az.path bicep version | Out-Null
    if ($LASTEXITCODE -ne 0) { $report.ready = $false }
}

$report | ConvertTo-Json -Depth 5
if ($Mode -eq 'Verify' -and -not $report.ready) { exit 2 }

# Installation is deliberately performed by the devcontainer features or an
# approved enterprise software-management policy. This script never downloads
# executables, changes the tenant, or logs in with a secret.

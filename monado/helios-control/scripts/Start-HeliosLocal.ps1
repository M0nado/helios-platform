[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path "$PSScriptRoot/..").Path
$envFile = Join-Path $projectRoot '.env.local'

function Resolve-DotNetPath {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $fallbacks = @(
        'C:\Program Files\dotnet\dotnet.exe',
        'C:\Program Files (x86)\dotnet\dotnet.exe'
    )
    foreach ($candidate in $fallbacks) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }
    return $null
}

if (Test-Path $envFile) {
    foreach ($line in Get-Content $envFile) {
        if ($line -match '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$') {
            [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process')
        }
    }
}
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
$env:HELIOS_EXECUTION_MODE = 'dry-run'
$env:HELIOS_CLOUD_RUNTIME_ONLY = 'false'
$env:HELIOS_LOCAL_RUNTIME_ALLOWED = 'true'
$dotnet = Resolve-DotNetPath
if (-not $dotnet) {
    throw 'dotnet was not found on PATH or standard install locations. Install .NET SDK 8.x.'
}

& $dotnet run --project (Join-Path $PSScriptRoot '../src/Helios.Connect.Api/Helios.Connect.Api.csproj')
if ($LASTEXITCODE -ne 0) {
    throw "dotnet run failed with exit code $LASTEXITCODE. Ensure the SDK pinned by global.json is installed."
}

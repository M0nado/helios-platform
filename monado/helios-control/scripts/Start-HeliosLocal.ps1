[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path "$PSScriptRoot/..").Path
$envFile = Join-Path $projectRoot '.env.local'

# Values injected by the governed runtime launcher must outrank developer-local
# defaults. Capture them before loading .env.local, then restore them afterward
# so smoke evidence reflects the resource envelope and correlation identity that
# were actually requested by the parent process.
$governedEnvironmentNames = @(
    'DOTNET_PROCESSOR_COUNT',
    'DOTNET_GCHeapHardLimit',
    'COMPlus_GCHeapHardLimit',
    'HELIOS_CORRELATION_ID'
)
$governedEnvironment = @{}
foreach ($name in $governedEnvironmentNames) {
    $value = [Environment]::GetEnvironmentVariable($name, 'Process')
    if ($null -ne $value) {
        $governedEnvironment[$name] = $value
    }
}

if (Test-Path $envFile) {
    foreach ($line in Get-Content $envFile) {
        if ($line -match '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$') {
            [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process')
        }
    }
}

foreach ($entry in $governedEnvironment.GetEnumerator()) {
    [Environment]::SetEnvironmentVariable([string] $entry.Key, [string] $entry.Value, 'Process')
}

$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
$env:HELIOS_EXECUTION_MODE = 'dry-run'
$env:HELIOS_CLOUD_RUNTIME_ONLY = 'false'
$env:HELIOS_LOCAL_RUNTIME_ALLOWED = 'true'
dotnet run --project (Join-Path $PSScriptRoot '../src/Helios.Connect.Api/Helios.Connect.Api.csproj')

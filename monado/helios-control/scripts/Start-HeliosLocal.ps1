[CmdletBinding()]
param([string] $WorkspaceRoot = (Resolve-Path "$PSScriptRoot/../..").Path)

$ErrorActionPreference = 'Stop'
$envFile = Join-Path $WorkspaceRoot '.env.local'
if (Test-Path $envFile) {
    foreach ($line in Get-Content $envFile) {
        if ($line -match '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$') {
            [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process')
        }
    }
}
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
$env:HELIOS_EXECUTION_MODE = 'dry-run'
dotnet run --project (Join-Path $PSScriptRoot '../src/Helios.Connect.Api/Helios.Connect.Api.csproj')

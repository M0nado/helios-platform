[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$solution = Join-Path $PSScriptRoot 'HELIOS.Platform.sln'
if (-not (Test-Path $solution)) {
    throw "Solution file not found: $solution"
}

dotnet restore $solution
dotnet build $solution --configuration $Configuration --no-restore

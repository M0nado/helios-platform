#Requires -Version 7.0

param(
    [string]$SourcePath = "docs\x-tier\imported\paste-1780416864960.txt"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

Set-Location (Split-Path -Parent $PSScriptRoot | Split-Path -Parent)

pwsh -File scripts\x-tier\build_all_polyglot.ps1 -SourcePath $SourcePath

dotnet run --project src\PolyglotXTier\PolyglotXTier.csproj -- --host-map artifacts\polyglot\csharp_host_map.json

python python\x-tier\aihub_stack\build_super_outputs.py

Write-Host "Super system build complete."

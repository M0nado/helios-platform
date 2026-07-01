#Requires -Version 7.0

param(
    [string]$SourcePath = "docs\x-tier\imported\paste-1780416864960.txt",
    [int]$Port = 8787,
    [switch]$Rebuild
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

Set-Location (Split-Path -Parent $PSScriptRoot | Split-Path -Parent)

if ($Rebuild) {
    pwsh -File scripts\x-tier\build_all_polyglot.ps1 -SourcePath $SourcePath
}

python python\x-tier\aihub_control_server.py --config config\x-tier\aihub-control.json --port $Port


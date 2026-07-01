#Requires -Version 7.0

param(
    [string]$SourcePath = "docs\x-tier\imported\paste-1780416864960.txt",
    [switch]$ExecuteSecurity,
    [switch]$ExecuteOptimization,
    [switch]$RunDockerStack
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

Set-Location (Split-Path -Parent $PSScriptRoot | Split-Path -Parent)

Write-Host "=== AIHub Upgrade Orchestrator ==="
Write-Host "Source: $SourcePath"

# Core integration pipeline
pwsh -File scripts\x-tier\build_all_polyglot.ps1 -SourcePath $SourcePath

# Validation baseline
if (Test-Path "scripts\x-tier\01_validate_disk_layout.ps1") {
    pwsh -File scripts\x-tier\01_validate_disk_layout.ps1
}

if ($ExecuteOptimization) {
    Write-Host "Optimization mode: enabled"
    if (Test-Path "scripts\x-tier\02_plan_vhdx_moves.ps1") {
        pwsh -File scripts\x-tier\02_plan_vhdx_moves.ps1
    }
}

if ($ExecuteSecurity) {
    Write-Host "Security mode: enabled"
    if (Test-Path "scripts\x-tier\imported\set_security_mode.ps1") {
        Write-Host "Running imported security mode script..."
        pwsh -File scripts\x-tier\imported\set_security_mode.ps1
    }
    if (Test-Path "scripts\x-tier\imported\configure_bitlocker.ps1") {
        Write-Host "Running imported BitLocker configuration script..."
        pwsh -File scripts\x-tier\imported\configure_bitlocker.ps1
    }
}

if ($RunDockerStack) {
    Write-Host "Docker stack mode: enabled"
    docker compose -f runtime\hermes\docker-compose.yml up -d hermes-api hermes-gateway hermes-gui hermes-trainer aihub-control
}

Write-Host "AIHub upgrade orchestration complete."

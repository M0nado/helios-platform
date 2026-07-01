#Requires -Version 7.0
# Validation-only script for X-Tier disk layout.

$ErrorActionPreference = "Stop"

$paths = @(
    "D:\DevDrive",
    "D:\DevDrive\wsl2\distros",
    "D:\DevDrive\wsl2\vhdx",
    "D:\DevDrive\hyperv\vhdx",
    "D:\DevDrive\docker\data-root",
    "D:\DevDrive\postgres",
    "D:\DevDrive\qdrant"
)

foreach ($p in $paths) {
    if (Test-Path $p) {
        Write-Host "[OK] $p"
    } else {
        Write-Warning "[MISSING] $p"
    }
}

Write-Host "Validation pass completed (no changes applied)."


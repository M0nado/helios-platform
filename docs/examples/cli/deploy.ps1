#!/usr/bin/env pwsh
# Example: PowerShell Deployment Script
# Demonstrates using HELIOS CLI cmdlets in PowerShell

param(
    [Parameter(Mandatory=$true)]
    [string]$ConfigFile,
    
    [string]$BackupPath = "./backups",
    [int]$Timeout = 120
)

# Import HELIOS CLI module
Import-Module "$PSScriptRoot/../../scripts/HELIOS.CLI.psm1" -ErrorAction Stop

Write-Host "=== HELIOS Continuous Deployment Example (PowerShell) ===" -ForegroundColor Cyan
Write-Host

try {
    # Step 1: Health Check
    Write-Host "[1/7] Running pre-deployment health checks..." -ForegroundColor Yellow
    $health = Get-HeliosHealth
    if ($health.HealthStatus -ne "Good") {
        Write-Host "Health check failed. Aborting deployment." -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Health check passed" -ForegroundColor Green
    Write-Host

    # Step 2: Create Backup
    Write-Host "[2/7] Creating backup..." -ForegroundColor Yellow
    $backupResult = New-HeliosBackup -Path $BackupPath
    Write-Host "✓ Backup created at $BackupPath" -ForegroundColor Green
    Write-Host

    # Step 3: Get Current Status
    Write-Host "[3/7] Getting current status..." -ForegroundColor Yellow
    $preStatus = Get-HeliosStatus
    $preStatus | ConvertTo-Json | Out-File "/tmp/pre-deploy-status.json"
    Write-Host "✓ Status captured" -ForegroundColor Green
    Write-Host

    # Step 4: Deploy Application
    Write-Host "[4/7] Deploying application..." -ForegroundColor Yellow
    $deployResult = Invoke-HeliosDeploy -Config $ConfigFile
    if ($deployResult.ExitCode -ne 0) {
        Write-Host "Deployment failed. Consider restoring from backup." -ForegroundColor Red
        Write-Host "Backup location: $BackupPath"
        exit 1
    }
    Write-Host "✓ Deployment completed" -ForegroundColor Green
    Write-Host

    # Step 5: Post-Deployment Health Check
    Write-Host "[5/7] Running post-deployment health checks..." -ForegroundColor Yellow
    $postHealth = Get-HeliosHealth
    if ($postHealth.HealthStatus -ne "Good") {
        Write-Host "Post-deployment health check failed." -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Post-deployment health check passed" -ForegroundColor Green
    Write-Host

    # Step 6: Verify Status
    Write-Host "[6/7] Verifying deployment status..." -ForegroundColor Yellow
    $postStatus = Get-HeliosStatus
    $postStatus | ConvertTo-Json | Out-File "/tmp/post-deploy-status.json"
    Write-Host "✓ Status verified" -ForegroundColor Green
    Write-Host

    # Step 7: Generate Report
    Write-Host "[7/7] Generating deployment report..." -ForegroundColor Yellow
    $report = @{
        Timestamp = Get-Date -Format O
        ConfigFile = $ConfigFile
        BackupPath = $BackupPath
        PreDeploymentStatus = $preStatus
        PostDeploymentStatus = $postStatus
        Status = "SUCCESSFUL"
    }
    
    $report | ConvertTo-Json | Out-File "/tmp/deployment-report.json"
    Write-Host "✓ Report generated" -ForegroundColor Green
    Write-Host

    Write-Host "=== DEPLOYMENT COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

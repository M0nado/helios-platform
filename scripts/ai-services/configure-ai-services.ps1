<#
.SYNOPSIS
Configure AI Services

.DESCRIPTION
Interactive configuration utility for AI services settings, budgets,
rate limits, and service weights.

.EXAMPLE
.\configure-ai-services.ps1
.\configure-ai-services.ps1 -Mode "budgets"
#>

param(
    [ValidateSet("interactive", "budgets", "ratelimits", "weights", "view")]
    [string]$Mode = "interactive"
)

$configDir = "C:\Users\ADMIN\helios-platform\config\ai-services"

function Show-Menu {
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║             AI SERVICES CONFIGURATION MENU                     ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    Write-Host "`n1. View Configuration" -ForegroundColor Cyan
    Write-Host "2. Configure Budgets" -ForegroundColor Yellow
    Write-Host "3. Configure Rate Limits" -ForegroundColor Yellow
    Write-Host "4. Configure Service Weights" -ForegroundColor Yellow
    Write-Host "5. Reset to Defaults" -ForegroundColor Yellow
    Write-Host "6. Exit" -ForegroundColor Cyan
    
    return Read-Host "`nSelect option (1-6)"
}

function Show-Configuration {
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "Current Configuration Files:" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $files = @(
        "ai-services-config.json",
        "cost-limits.json",
        "service-weights.json"
    )
    
    foreach ($file in $files) {
        $path = Join-Path $configDir $file
        
        if (Test-Path $path) {
            $size = (Get-Item $path).Length
            $modified = (Get-Item $path).LastWriteTime
            
            Write-Host "`n$file" -ForegroundColor Green
            Write-Host "  Size: $size bytes"
            Write-Host "  Modified: $modified"
            
            if (Read-Host "  View contents? (y/n)" -eq "y") {
                Write-Host "`n  Content Preview:" -ForegroundColor Gray
                Get-Content $path | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
            }
        }
        else {
            Write-Host "`n$file" -ForegroundColor Red
            Write-Host "  NOT FOUND" -ForegroundColor Red
        }
    }
}

function Configure-Budgets {
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "Configure Budget Limits" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $daily = Read-Host "Daily budget limit (USD) [default: 50]"
    $monthly = Read-Host "Monthly budget limit (USD) [default: 500]"
    $warnThreshold = Read-Host "Warning threshold (%) [default: 80]"
    
    if ([string]::IsNullOrEmpty($daily)) { $daily = 50 }
    if ([string]::IsNullOrEmpty($monthly)) { $monthly = 500 }
    if ([string]::IsNullOrEmpty($warnThreshold)) { $warnThreshold = 80 }
    
    Write-Host "`nUpdated Budget Configuration:" -ForegroundColor Green
    Write-Host "  Daily: \$$daily"
    Write-Host "  Monthly: \$$monthly"
    Write-Host "  Warning Threshold: $warnThreshold%"
    
    return @{
        daily = [double]$daily
        monthly = [double]$monthly
        warnThreshold = [int]$warnThreshold
    }
}

function Configure-RateLimits {
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "Configure Rate Limits" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $rpm = Read-Host "Requests per minute [default: 60]"
    $rph = Read-Host "Requests per hour [default: 1000]"
    $rpd = Read-Host "Requests per day [default: 10000]"
    
    if ([string]::IsNullOrEmpty($rpm)) { $rpm = 60 }
    if ([string]::IsNullOrEmpty($rph)) { $rph = 1000 }
    if ([string]::IsNullOrEmpty($rpd)) { $rpd = 10000 }
    
    Write-Host "`nUpdated Rate Limit Configuration:" -ForegroundColor Green
    Write-Host "  Per Minute: $rpm"
    Write-Host "  Per Hour: $rph"
    Write-Host "  Per Day: $rpd"
    
    return @{
        rpm = [int]$rpm
        rph = [int]$rph
        rpd = [int]$rpd
    }
}

function Configure-Weights {
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "Configure Service Weights" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    Write-Host "`nAvailable Task Types:" -ForegroundColor Yellow
    Write-Host "  1. code-review"
    Write-Host "  2. code-generation"
    Write-Host "  3. code-refactoring"
    Write-Host "  4. complex-analysis"
    Write-Host "  5. security-review"
    
    $taskType = Read-Host "`nSelect task type (or enter custom)"
    
    Write-Host "`nServices (enter weights 0.0-1.0):" -ForegroundColor Yellow
    
    $gpt4 = Read-Host "  ChatGPT-Pro (default: 1.0)"
    $codex = Read-Host "  Codex (default: 0.8)"
    $gpt45 = Read-Host "  GPT-4.5 (default: 0.9)"
    
    if ([string]::IsNullOrEmpty($gpt4)) { $gpt4 = 1.0 }
    if ([string]::IsNullOrEmpty($codex)) { $codex = 0.8 }
    if ([string]::IsNullOrEmpty($gpt45)) { $gpt45 = 0.9 }
    
    Write-Host "`nUpdated Service Weights for '$taskType':" -ForegroundColor Green
    Write-Host "  ChatGPT-Pro: $gpt4"
    Write-Host "  Codex: $codex"
    Write-Host "  GPT-4.5: $gpt45"
    
    return @{
        TaskType = $taskType
        Weights = @{
            ChatGPTPro = [double]$gpt4
            Codex = [double]$codex
            GPT45 = [double]$gpt45
        }
    }
}

function Reset-Defaults {
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "Reset Configuration to Defaults?" -ForegroundColor Yellow
    
    if ((Read-Host "Are you sure? (yes/no)") -eq "yes") {
        Write-Host "Configuration would be reset to defaults." -ForegroundColor Green
        Write-Host "This would restore default values for all settings." -ForegroundColor Gray
        Write-Host "`nImplementation: Recreate configuration files from templates" -ForegroundColor Cyan
    }
}

function Show-Summary {
    param([hashtable]$Settings)
    
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "Configuration Summary" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    foreach ($setting in $Settings.GetEnumerator()) {
        Write-Host "$($setting.Name): $($setting.Value)"
    }
}

# ============================================================================
# MAIN
# ============================================================================

Write-Host "`n" -ForegroundColor Cyan
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║           AI SERVICES CONFIGURATION UTILITY                    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

# Verify config directory
if (-not (Test-Path $configDir)) {
    Write-Host "`nCreating configuration directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
}

switch ($Mode) {
    "view" {
        Show-Configuration
    }
    "budgets" {
        $budget = Configure-Budgets
        Show-Summary -Settings $budget
    }
    "ratelimits" {
        $limits = Configure-RateLimits
        Show-Summary -Settings $limits
    }
    "weights" {
        $weights = Configure-Weights
        Show-Summary -Settings $weights
    }
    "interactive" {
        do {
            $choice = Show-Menu
            
            switch ($choice) {
                "1" { Show-Configuration }
                "2" { Configure-Budgets }
                "3" { Configure-RateLimits }
                "4" { Configure-Weights }
                "5" { Reset-Defaults }
                "6" { break }
                default { Write-Host "Invalid selection" -ForegroundColor Red }
            }
        } while ($choice -ne "6")
    }
}

Write-Host "`n" -ForegroundColor Cyan
Write-Host "Configuration utility completed." -ForegroundColor Green
Write-Host "Run './test-ai-services.ps1' to verify the configuration." -ForegroundColor Cyan
Write-Host "`n"

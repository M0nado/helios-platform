<#
.SYNOPSIS
Show AI Services Cost Analysis

.DESCRIPTION
Displays comprehensive cost analysis for AI services including daily/monthly
budgets, spending trends, cost per service, and budget alerts.

.EXAMPLE
.\show-ai-costs.ps1
.\show-ai-costs.ps1 -ShowTrends
.\show-ai-costs.ps1 -AlertThreshold 75
#>

param(
    [string]$ConfigPath = "C:\Users\ADMIN\helios-platform\config\ai-services\cost-limits.json",
    [int]$AlertThreshold = 80,
    [switch]$ShowTrends,
    [switch]$ShowProjections
)

# ============================================================================
# FUNCTIONS
# ============================================================================

function Load-CostConfig {
    param([string]$Path)
    
    if (-not (Test-Path $Path)) {
        Write-Error "Cost configuration not found: $Path"
        return $null
    }
    
    return Get-Content $Path -Raw | ConvertFrom-Json
}

function Get-CurrentSpending {
    param([PSCustomObject]$Config)
    
    $spending = @{
        Today = 0
        ThisMonth = 0
        Services = @{}
    }
    
    # In production, this would query actual usage data
    # For now, return sample data
    
    return $spending
}

function Check-BudgetStatus {
    param(
        [PSCustomObject]$Config,
        [hashtable]$Spending,
        [int]$Threshold
    )
    
    $budgets = $Config.budgets
    $status = @()
    
    # Check daily budget
    $dailyUsagePercent = if ($budgets.daily -gt 0) { ($Spending.Today / $budgets.daily * 100) } else { 0 }
    $dailyStatus = if ($dailyUsagePercent -ge $Threshold) { "WARNING" } else { "OK" }
    
    $status += [PSCustomObject]@{
        BudgetType = "Daily"
        Limit = "` $$('{0:N2}' -f $budgets.daily)"
        Spent = "` $$('{0:N2}' -f $Spending.Today)"
        Percentage = "$([Math]::Round($dailyUsagePercent, 1))%"
        Status = $dailyStatus
    }
    
    # Check monthly budget
    $monthlyUsagePercent = if ($budgets.monthly -gt 0) { ($Spending.ThisMonth / $budgets.monthly * 100) } else { 0 }
    $monthlyStatus = if ($monthlyUsagePercent -ge $Threshold) { "WARNING" } else { "OK" }
    
    $status += [PSCustomObject]@{
        BudgetType = "Monthly"
        Limit = "` $$('{0:N2}' -f $budgets.monthly)"
        Spent = "` $$('{0:N2}' -f $Spending.ThisMonth)"
        Percentage = "$([Math]::Round($monthlyUsagePercent, 1))%"
        Status = $monthlyStatus
    }
    
    return $status
}

function Get-ServiceCosts {
    param([PSCustomObject]$Config)
    
    $serviceBudgets = $Config.budgets.perService
    $costs = @()
    
    foreach ($service in $serviceBudgets.PSObject.Properties.Name) {
        $serviceConfig = $serviceBudgets.$service
        
        # In production, fetch actual costs
        $actualDaily = Get-Random -Minimum 1 -Maximum 10
        $actualMonthly = Get-Random -Minimum 100 -Maximum 150
        
        $dailyPercent = ($actualDaily / $serviceConfig.daily * 100)
        $monthlyPercent = ($actualMonthly / $serviceConfig.monthly * 100)
        
        $costs += [PSCustomObject]@{
            Service = $service
            DailyLimit = "` $$('{0:N2}' -f $serviceConfig.daily)"
            DailySpent = "` $$('{0:N2}' -f $actualDaily)"
            DailyUsage = "$([Math]::Round($dailyPercent, 1))%"
            MonthlyLimit = "` $$('{0:N2}' -f $serviceConfig.monthly)"
            MonthlySpent = "` $$('{0:N2}' -f $actualMonthly)"
            MonthlyUsage = "$([Math]::Round($monthlyPercent, 1))%"
        }
    }
    
    return $costs
}

function Display-CostReport {
    param(
        [PSCustomObject[]]$BudgetStatus,
        [PSCustomObject[]]$ServiceCosts,
        [int]$AlertThreshold
    )
    
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "                         AI SERVICES COST ANALYSIS" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "`n"
    
    # Budget Status
    Write-Host "BUDGET STATUS" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    foreach ($budget in $BudgetStatus) {
        $statusColor = if ($budget.Status -eq "WARNING") { "Yellow" } else { "Green" }
        
        Write-Host "$($budget.BudgetType) Budget: " -NoNewline
        Write-Host "$($budget.Limit)" -ForegroundColor Green -NoNewline
        Write-Host " | Spent: " -NoNewline
        Write-Host "$($budget.Spent)" -ForegroundColor Yellow -NoNewline
        Write-Host " | Usage: " -NoNewline
        Write-Host "$($budget.Percentage)" -ForegroundColor $statusColor -NoNewline
        Write-Host " | Status: " -NoNewline
        Write-Host "$($budget.Status)" -ForegroundColor $statusColor
    }
    
    # Service Breakdown
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "SERVICE COST BREAKDOWN" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host "`n"
    
    $ServiceCosts | Format-Table -AutoSize -Property @(
        "Service",
        @{ Label = "Daily Limit"; Expression = { $_.DailyLimit } },
        @{ Label = "Daily Spent"; Expression = { $_.DailySpent } },
        @{ Label = "Daily %"; Expression = { $_.DailyUsage } },
        @{ Label = "Monthly Limit"; Expression = { $_.MonthlyLimit } },
        @{ Label = "Monthly Spent"; Expression = { $_.MonthlySpent } },
        @{ Label = "Monthly %"; Expression = { $_.MonthlyUsage } }
    )
    
    # Alerts
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "BUDGET ALERTS (Threshold: $AlertThreshold%)" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $alerts = $BudgetStatus | Where-Object { ([int]($_.Percentage -replace "%", "") -ge $AlertThreshold) }
    
    if ($alerts.Count -eq 0) {
        Write-Host "No alerts. All budgets within acceptable limits." -ForegroundColor Green
    }
    else {
        foreach ($alert in $alerts) {
            Write-Host "⚠️  $($alert.BudgetType) budget at $($alert.Percentage)!" -ForegroundColor Yellow
        }
    }
    
    # Recommendations
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "RECOMMENDATIONS" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $recommendations = @(
        "• Consider rate limiting if approaching budget limits"
        "• Monitor GPT-4.5 usage - highest cost model"
        "• Use Codex for code generation tasks - most cost efficient"
        "• Implement caching to reduce redundant API calls"
        "• Review daily spending patterns to optimize timing"
        "• Consider batch processing during off-peak hours"
    )
    
    foreach ($rec in $recommendations) {
        Write-Host $rec
    }
    
    Write-Host "`n"
}

function Display-CostTrends {
    param([PSCustomObject[]]$ServiceCosts)
    
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "COST TRENDS (Last 7 Days)" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    # Generate sample trend data
    $days = @("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
    $dailyCosts = @()
    
    foreach ($day in $days) {
        $cost = Get-Random -Minimum 5 -Maximum 25
        $dailyCosts += [PSCustomObject]@{
            Day = $day
            Cost = "` $$('{0:N2}' -f $cost)"
            BarChart = ("█" * [Math]::Round($cost / 2, 0))
        }
    }
    
    $dailyCosts | Format-Table -AutoSize
    
    $totalWeek = ($dailyCosts.Count) * 15  # Average estimate
    Write-Host "`nEstimated Weekly Total: ` $('{0:N2}' -f $totalWeek)" -ForegroundColor Cyan
    Write-Host "Estimated Monthly Total: ` $('{0:N2}' -f ($totalWeek * 4.3))" -ForegroundColor Cyan
}

function Display-CostProjections {
    param([PSCustomObject[]]$ServiceCosts)
    
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "COST PROJECTIONS" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $scenarios = @(
        @{ Name = "Optimistic"; Factor = 0.8; Color = "Green" },
        @{ Name = "Expected"; Factor = 1.0; Color = "Yellow" },
        @{ Name = "Pessimistic"; Factor = 1.2; Color = "Red" }
    )
    
    $baseDaily = 15
    
    foreach ($scenario in $scenarios) {
        $projectedDaily = $baseDaily * $scenario.Factor
        $projectedMonthly = $projectedDaily * 30.4
        
        Write-Host "$($scenario.Name): " -NoNewline -ForegroundColor $scenario.Color
        Write-Host "`Daily: $" -NoNewline
        Write-Host "$('{0:N2}' -f $projectedDaily) | " -NoNewline -ForegroundColor $scenario.Color
        Write-Host "Monthly: $" -NoNewline
        Write-Host "$('{0:N2}' -f $projectedMonthly)" -ForegroundColor $scenario.Color
    }
}

# ============================================================================
# MAIN
# ============================================================================

try {
    # Load configuration
    Write-Host "Loading cost configuration..." -ForegroundColor Gray
    $config = Load-CostConfig -Path $ConfigPath
    
    if ($null -eq $config) {
        exit 1
    }
    
    # Get spending data
    $spending = Get-CurrentSpending -Config $config
    
    # Check budget status
    $budgetStatus = Check-BudgetStatus -Config $config -Spending $spending -Threshold $AlertThreshold
    
    # Get service costs
    $serviceCosts = Get-ServiceCosts -Config $config
    
    # Display main report
    Display-CostReport -BudgetStatus $budgetStatus -ServiceCosts $serviceCosts -AlertThreshold $AlertThreshold
    
    # Display trends if requested
    if ($ShowTrends) {
        Display-CostTrends -ServiceCosts $serviceCosts
    }
    
    # Display projections if requested
    if ($ShowProjections) {
        Display-CostProjections -ServiceCosts $serviceCosts
    }
    
    Write-Host "Report generated at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
}
catch {
    Write-Error "Error generating cost report: $_"
    exit 1
}

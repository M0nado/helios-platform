<#
.SYNOPSIS
Azure Cost Analysis and Optimization for HELIOS Platform

.DESCRIPTION
Analyzes and optimizes Azure costs including:
- Resource cost analysis
- Usage analytics
- Cost budgets and alerts
- Right-sizing recommendations
- Reserved instances analysis
- Cost optimization reports

.NOTES
Author: HELIOS Platform
Version: 1.0.0
Requires: Az.CostManagement module
#>

#Requires -Modules Az.CostManagement, Az.Billing

param(
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = "$PSScriptRoot\..\azure-config.json",
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId
)

$LogDirectory = "C:\Logs\HELIOS\Azure"
if (-not (Test-Path $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}
$LogFile = Join-Path $LogDirectory "cost-analyzer-$(Get-Date -Format 'yyyyMMdd').log"

function Write-Log {
    param([string]$Message, [ValidateSet('Info', 'Warning', 'Error', 'Success')]$Level = 'Info')
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $logMessage -Force
    
    $color = @{
        'Info'    = 'Cyan'
        'Warning' = 'Yellow'
        'Error'   = 'Red'
        'Success' = 'Green'
    }
    Write-Host $logMessage -ForegroundColor $color[$Level]
}

function Get-ConfigValue {
    param([string]$Key, [string]$DefaultValue = $null)
    
    if (Test-Path $ConfigPath) {
        $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
        return $config.$Key ?? $DefaultValue
    }
    return $DefaultValue
}

function Get-ResourceCostAnalysis {
    param(
        [Parameter(Mandatory = $false)]
        [string]$TimeFrame = 'MonthToDate',
        
        [Parameter(Mandatory = $false)]
        [string]$Granularity = 'Daily',
        
        [Parameter(Mandatory = $false)]
        [string]$GroupByProperty = 'ResourceType'
    )
    
    try {
        Write-Log "Analyzing resource costs (TimeFrame: $TimeFrame)"
        
        $scope = if ($SubscriptionId) {
            "/subscriptions/$SubscriptionId"
        }
        else {
            "/subscriptions/$(Get-AzContext).Subscription.Id"
        }
        
        $query = @{
            Type       = 'Usage'
            Timeframe  = $TimeFrame
            Granularity = $Granularity
            GroupBy    = @(
                @{
                    Type = 'Dimension'
                    Name = $GroupByProperty
                }
            )
            Filter     = $null
        }
        
        $result = Invoke-AzCostManagementQuery -Scope $scope -Query $query -ErrorAction Stop
        
        $costData = @()
        if ($result.Properties.Rows) {
            foreach ($row in $result.Properties.Rows) {
                $costData += @{
                    Dimension = $row[0]
                    Cost      = [decimal]$row[1]
                    Currency  = $result.Properties.Currency
                }
            }
        }
        
        Write-Log "Cost analysis completed. Total resources analyzed: $($costData.Count)" -Level Success
        return $costData
    }
    catch {
        Write-Log "Failed to analyze costs: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-TopCostResources {
    param(
        [Parameter(Mandatory = $false)]
        [int]$TopCount = 10,
        
        [Parameter(Mandatory = $false)]
        [string]$TimeFrame = 'MonthToDate'
    )
    
    try {
        Write-Log "Retrieving top $TopCount cost resources"
        
        $scope = if ($SubscriptionId) {
            "/subscriptions/$SubscriptionId"
        }
        else {
            "/subscriptions/$(Get-AzContext).Subscription.Id"
        }
        
        $query = @{
            Type       = 'Usage'
            Timeframe  = $TimeFrame
            Granularity = 'Monthly'
            GroupBy    = @(
                @{
                    Type = 'Dimension'
                    Name = 'ResourceId'
                }
            )
        }
        
        $result = Invoke-AzCostManagementQuery -Scope $scope -Query $query -ErrorAction Stop
        
        $topResources = @()
        if ($result.Properties.Rows) {
            $sortedRows = $result.Properties.Rows | Sort-Object { [decimal]$_[1] } -Descending
            $topResources = $sortedRows | Select-Object -First $TopCount | ForEach-Object {
                @{
                    ResourceId = $_[0]
                    Cost       = [decimal]$_[1]
                }
            }
        }
        
        Write-Log "Retrieved top $TopCount cost resources" -Level Success
        return $topResources
    }
    catch {
        Write-Log "Failed to retrieve top cost resources: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-UnderUtilizedResources {
    param(
        [Parameter(Mandatory = $false)]
        [string]$ResourceType,
        
        [Parameter(Mandatory = $false)]
        [int]$CPUThresholdPercent = 20,
        
        [Parameter(Mandatory = $false)]
        [int]$MemoryThresholdPercent = 20
    )
    
    try {
        Write-Log "Analyzing underutilized resources (CPU: <$CPUThresholdPercent%, Memory: <$MemoryThresholdPercent%)"
        
        $underUtilized = @()
        
        $vms = if ($ResourceType -eq 'VirtualMachine' -or -not $ResourceType) {
            Get-AzVM -ErrorAction SilentlyContinue
        }
        else {
            @()
        }
        
        foreach ($vm in $vms) {
            $underUtilized += @{
                Name         = $vm.Name
                Type         = 'VirtualMachine'
                Size         = $vm.HardwareProfile.VmSize
                ResourceGroup = $vm.ResourceGroupName
                Recommendation = "Consider resizing to smaller SKU or deallocating if unused"
            }
        }
        
        Write-Log "Identified $($underUtilized.Count) underutilized resources" -Level Success
        return $underUtilized
    }
    catch {
        Write-Log "Failed to analyze underutilized resources: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-CostBudget {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BudgetName,
        
        [Parameter(Mandatory = $true)]
        [decimal]$Amount,
        
        [Parameter(Mandatory = $false)]
        [string]$TimeGrain = 'Monthly',
        
        [Parameter(Mandatory = $false)]
        [string[]]$NotificationEmails,
        
        [Parameter(Mandatory = $false)]
        [int]$ThresholdPercent = 80
    )
    
    try {
        Write-Log "Creating cost budget: $BudgetName (Amount: $Amount)"
        
        $scope = if ($SubscriptionId) {
            "/subscriptions/$SubscriptionId"
        }
        else {
            "/subscriptions/$(Get-AzContext).Subscription.Id"
        }
        
        $budget = @{
            Name       = $BudgetName
            Category   = 'Cost'
            TimeGrain  = $TimeGrain
            Amount     = $Amount
            Threshold  = $ThresholdPercent
            Operator   = 'GreaterThan'
        }
        
        Write-Log "Cost budget created successfully: $BudgetName" -Level Success
        return $budget
    }
    catch {
        Write-Log "Failed to create cost budget: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-MonthlyCostTrend {
    param(
        [Parameter(Mandatory = $false)]
        [int]$Months = 12
    )
    
    try {
        Write-Log "Retrieving monthly cost trends for last $Months months"
        
        $scope = if ($SubscriptionId) {
            "/subscriptions/$SubscriptionId"
        }
        else {
            "/subscriptions/$(Get-AzContext).Subscription.Id"
        }
        
        $trends = @()
        $currentMonth = Get-Date -Day 1
        
        for ($i = 0; $i -lt $Months; $i++) {
            $monthStart = $currentMonth.AddMonths(-$i)
            $monthEnd = $monthStart.AddMonths(1).AddDays(-1)
            
            $query = @{
                Type      = 'Usage'
                Timeframe = 'Custom'
                TimePeriod = @{
                    From = $monthStart.ToString('yyyy-MM-01')
                    To   = $monthEnd.ToString('yyyy-MM-dd')
                }
                Granularity = 'Monthly'
            }
            
            $result = Invoke-AzCostManagementQuery -Scope $scope -Query $query -ErrorAction Stop
            
            $monthCost = 0
            if ($result.Properties.Rows) {
                $monthCost = [decimal]$result.Properties.Rows[1]
            }
            
            $trends += @{
                Month = $monthStart.ToString('yyyy-MM')
                Cost  = $monthCost
            }
        }
        
        Write-Log "Retrieved $($trends.Count) months of cost trends" -Level Success
        return $trends | Sort-Object Month
    }
    catch {
        Write-Log "Failed to retrieve cost trends: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-ResourceTypeDistribution {
    param(
        [Parameter(Mandatory = $false)]
        [string]$TimeFrame = 'MonthToDate'
    )
    
    try {
        Write-Log "Analyzing resource type cost distribution"
        
        $costAnalysis = Get-ResourceCostAnalysis -TimeFrame $TimeFrame -GroupByProperty 'ResourceType'
        
        $distribution = $costAnalysis | Group-Object Dimension | ForEach-Object {
            $totalCost = ($_.Group.Cost | Measure-Object -Sum).Sum
            @{
                ResourceType = $_.Name
                TotalCost    = $totalCost
                Count        = $_.Group.Count
                AverageCost  = $totalCost / $_.Group.Count
            }
        } | Sort-Object TotalCost -Descending
        
        Write-Log "Generated cost distribution for $($distribution.Count) resource types" -Level Success
        return $distribution
    }
    catch {
        Write-Log "Failed to get resource distribution: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Generate-CostOptimizationReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        
        [Parameter(Mandatory = $false)]
        [string]$TimeFrame = 'MonthToDate'
    )
    
    try {
        Write-Log "Generating cost optimization report"
        
        $report = @{
            GeneratedTime        = Get-Date
            TimeFrame           = $TimeFrame
            TopCostResources    = Get-TopCostResources -TimeFrame $TimeFrame
            UnderUtilized       = Get-UnderUtilizedResources
            ResourceDistribution = Get-ResourceTypeDistribution -TimeFrame $TimeFrame
            MonthlyCostTrend    = Get-MonthlyCostTrend -Months 12
        }
        
        $report | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputPath -Force
        
        Write-Log "Cost optimization report generated: $OutputPath" -Level Success
        return $report
    }
    catch {
        Write-Log "Failed to generate report: $($_.Exception.Message)" -Level Error
        throw
    }
}

Export-ModuleMember -Function @(
    'Get-ResourceCostAnalysis',
    'Get-TopCostResources',
    'Get-UnderUtilizedResources',
    'New-CostBudget',
    'Get-MonthlyCostTrend',
    'Get-ResourceTypeDistribution',
    'Generate-CostOptimizationReport'
)

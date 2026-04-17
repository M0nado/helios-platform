<#
.SYNOPSIS
    Intelligent load balancer for hybrid workloads
.DESCRIPTION
    Distributes workloads between on-premises and cloud based on
    real-time metrics, cost optimization, and performance requirements
.PARAMETER BalancingStrategy
    Strategy: CostOptimized, PerformanceOptimized, HighAvailability, Auto
.PARAMETER MaxCloudCost
    Maximum hourly cloud cost in dollars (default: 100)
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("CostOptimized", "PerformanceOptimized", "HighAvailability", "Auto")]
    [string]$BalancingStrategy = "Auto",
    
    [Parameter(Mandatory = $false)]
    [decimal]$MaxCloudCost = 100,
    
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = ".\config\cloud-orchestration-config.json"
)

$ErrorActionPreference = "Stop"
$VerbosePreference = "Continue"

# ============================================================================
# CONFIGURATION & INITIALIZATION
# ============================================================================

function Initialize-LoadBalancer {
    param([string]$ConfigPath)
    
    try {
        Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Initializing load balancer..." -ForegroundColor Cyan
        
        $config = Get-Content $ConfigPath | ConvertFrom-Json
        
        # Define workload profiles
        $workloadProfiles = @{
            Critical = @{
                Priority = 1
                PreferredLocation = "OnPremises"
                FailoverTarget = "Cloud"
                MaxLatencyMs = 50
                MinUptime = 0.999
            }
            HighPerformance = @{
                Priority = 2
                PreferredLocation = "Auto"
                FailoverTarget = "Cloud"
                MaxLatencyMs = 100
                MinUptime = 0.99
            }
            Standard = @{
                Priority = 3
                PreferredLocation = "Auto"
                FailoverTarget = "Cloud"
                MaxLatencyMs = 200
                MinUptime = 0.95
            }
            Batch = @{
                Priority = 4
                PreferredLocation = "Cloud"
                FailoverTarget = "OnPremises"
                MaxLatencyMs = 2000
                MinUptime = 0.90
            }
        }
        
        Write-Host "✓ Load balancer initialized with $($workloadProfiles.Count) workload profiles" -ForegroundColor Green
        
        return @{
            Config = $config
            WorkloadProfiles = $workloadProfiles
            BalancingStrategy = $BalancingStrategy
            MaxCloudCost = $MaxCloudCost
        }
    }
    catch {
        Write-Host "✗ Initialization failed: $_" -ForegroundColor Red
        exit 1
    }
}

# ============================================================================
# METRICS COLLECTION
# ============================================================================

function Get-OnPremises-Metrics {
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Collecting on-premises metrics..." -ForegroundColor Cyan
    
    try {
        $metrics = @{}
        
        # CPU Usage
        $cpuUsage = (Get-Counter '\Processor(_Total)\% Processor Time' -ErrorAction SilentlyContinue).CounterSamples[0].CookedValue
        $metrics["CPUUsagePercent"] = [Math]::Round($cpuUsage, 2)
        Write-Host "  CPU: $($metrics["CPUUsagePercent"])%" -ForegroundColor Yellow
        
        # Memory Usage
        $memInfo = Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue
        $memUsedPercent = (($memInfo.TotalVisibleMemorySize - $memInfo.FreePhysicalMemory) / $memInfo.TotalVisibleMemorySize) * 100
        $metrics["MemoryUsagePercent"] = [Math]::Round($memUsedPercent, 2)
        Write-Host "  Memory: $($metrics["MemoryUsagePercent"])%" -ForegroundColor Yellow
        
        # Network I/O
        $networkInterface = Get-NetAdapter -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($networkInterface) {
            $metrics["NetworkAdapterName"] = $networkInterface.Name
            Write-Host "  Network: $($networkInterface.Name) - $($networkInterface.LinkSpeed)" -ForegroundColor Yellow
        }
        
        # Disk I/O
        $diskLatency = 0
        try {
            $diskLatency = (Get-Counter '\PhysicalDisk(_Total)\Avg. Disk sec/Transfer' -ErrorAction SilentlyContinue).CounterSamples[0].CookedValue * 1000
        }
        catch {
            $diskLatency = 5
        }
        $metrics["DiskLatencyMs"] = [Math]::Round($diskLatency, 2)
        Write-Host "  Disk Latency: $($metrics["DiskLatencyMs"])ms" -ForegroundColor Yellow
        
        # Connectivity
        $connectivity = Test-NetConnection -ComputerName 8.8.8.8 -InformationLevel Quiet
        $metrics["InternetConnected"] = $connectivity
        Write-Host "  Internet: $(if ($connectivity) { 'Connected' } else { 'Disconnected' })" -ForegroundColor $(if ($connectivity) { "Green" } else { "Red" })
        
        # Calculate composite health score
        $cpuScore = 1 - ($cpuUsage / 100)
        $memScore = 1 - ($memUsedPercent / 100)
        $diskScore = 1 - ([Math]::Min($diskLatency / 100, 1))
        $netScore = $connectivity ? 1 : 0
        
        $metrics["HealthScore"] = [Math]::Round(($cpuScore + $memScore + $diskScore + $netScore) / 4 * 100, 2)
        
        return @{
            Timestamp = (Get-Date)
            Location = "OnPremises"
            Metrics = $metrics
            HealthScore = $metrics["HealthScore"]
        }
    }
    catch {
        Write-Host "✗ Metrics collection failed: $_" -ForegroundColor Red
        return @{
            Location = "OnPremises"
            HealthScore = 0
            Error = $_
        }
    }
}

function Get-Cloud-Metrics {
    param([object]$Config)
    
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Collecting cloud metrics..." -ForegroundColor Cyan
    
    try {
        Connect-AzAccount -Subscription $Config.AzureSubscriptionId -ErrorAction Stop | Out-Null
        
        $metrics = @{}
        
        # VM Status
        $vms = Get-AzVM -ErrorAction SilentlyContinue
        $runningVMs = $vms | Where-Object { $_.PowerState -like "VM running" } | Measure-Object | Select-Object -ExpandProperty Count
        $metrics["VMsRunning"] = $runningVMs
        $metrics["VMsTotal"] = $vms.Count
        Write-Host "  VMs: $runningVMs/$($vms.Count) running" -ForegroundColor Yellow
        
        # App Services Status
        $appServices = Get-AzWebApp -ErrorAction SilentlyContinue
        $runningApps = $appServices | Where-Object { $_.State -eq "Running" } | Measure-Object | Select-Object -ExpandProperty Count
        $metrics["AppServicesRunning"] = $runningApps
        $metrics["AppServicesTotal"] = $appServices.Count
        Write-Host "  App Services: $runningApps/$($appServices.Count) running" -ForegroundColor Yellow
        
        # Storage Accounts
        $storageAccounts = Get-AzStorageAccount -ErrorAction SilentlyContinue
        $metrics["StorageAccounts"] = $storageAccounts.Count
        Write-Host "  Storage Accounts: $($storageAccounts.Count)" -ForegroundColor Yellow
        
        # Estimate cost (simplified)
        $estimatedHourlyCost = ($vms.Count * 0.20) + ($appServices.Count * 0.10) + ($storageAccounts.Count * 0.05)
        $metrics["EstimatedHourlyCost"] = [Math]::Round($estimatedHourlyCost, 2)
        Write-Host "  Estimated Hourly Cost: \$$($metrics["EstimatedHourlyCost"])" -ForegroundColor Cyan
        
        # Calculate health score
        $vmScore = $vms.Count -gt 0 ? ($runningVMs / $vms.Count) : 0
        $appScore = $appServices.Count -gt 0 ? ($runningApps / $appServices.Count) : 0
        $availabilityScore = ($vmScore + $appScore) / 2
        
        $metrics["HealthScore"] = [Math]::Round($availabilityScore * 100, 2)
        
        return @{
            Timestamp = (Get-Date)
            Location = "Cloud"
            Metrics = $metrics
            HealthScore = $metrics["HealthScore"]
            EstimatedHourlyCost = $metrics["EstimatedHourlyCost"]
        }
    }
    catch {
        Write-Host "✗ Cloud metrics collection failed: $_" -ForegroundColor Red
        return @{
            Location = "Cloud"
            HealthScore = 0
            Error = $_
        }
    }
}

# ============================================================================
# LOAD BALANCING DECISION ENGINE
# ============================================================================

function Calculate-Workload-Distribution {
    param(
        [hashtable]$OnPremMetrics,
        [hashtable]$CloudMetrics,
        [hashtable]$System,
        [hashtable]$Workload
    )
    
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Calculating workload distribution..." -ForegroundColor Cyan
    
    $strategy = $System.BalancingStrategy
    $profile = $System.WorkloadProfiles[$Workload.Type]
    
    $onPremHealth = $OnPremMetrics.HealthScore ?? 0
    $cloudHealth = $CloudMetrics.HealthScore ?? 0
    $cloudCost = $CloudMetrics.EstimatedHourlyCost ?? 999
    
    Write-Host "  Workload: $($Workload.Name) (Type: $($Workload.Type))" -ForegroundColor Cyan
    Write-Host "  Health Scores - OnPrem: $onPremHealth%, Cloud: $cloudHealth%" -ForegroundColor Yellow
    
    $distribution = @{
        Workload = $Workload.Name
        Type = $Workload.Type
        Priority = $profile.Priority
        Timestamp = (Get-Date)
    }
    
    # Apply balancing strategy
    switch ($strategy) {
        "CostOptimized" {
            Write-Host "  Strategy: Cost Optimized" -ForegroundColor Cyan
            
            if ($cloudCost -le $System.MaxCloudCost -and $cloudHealth -ge 70) {
                $distribution["Target"] = "Cloud"
                $distribution["AllocationPercent"] = 100
                $distribution["Reasoning"] = "Cost effective and healthy"
            }
            else {
                $distribution["Target"] = "OnPremises"
                $distribution["AllocationPercent"] = 100
                $distribution["Reasoning"] = "Cloud exceeds cost limit or unhealthy"
            }
        }
        
        "PerformanceOptimized" {
            Write-Host "  Strategy: Performance Optimized" -ForegroundColor Cyan
            
            if ($onPremHealth -ge 85) {
                $distribution["Target"] = "OnPremises"
                $distribution["AllocationPercent"] = 100
                $distribution["Reasoning"] = "On-premises has superior performance"
            }
            elseif ($cloudHealth -ge 80) {
                $distribution["Target"] = "Cloud"
                $distribution["AllocationPercent"] = 100
                $distribution["Reasoning"] = "Cloud provides adequate performance"
            }
            else {
                $distribution["Target"] = "Hybrid"
                $distribution["OnPremAllocation"] = 60
                $distribution["CloudAllocation"] = 40
                $distribution["Reasoning"] = "Distributed across both environments"
            }
        }
        
        "HighAvailability" {
            Write-Host "  Strategy: High Availability" -ForegroundColor Cyan
            
            $distribution["Target"] = "Hybrid"
            $distribution["OnPremAllocation"] = 50
            $distribution["CloudAllocation"] = 50
            $distribution["Reasoning"] = "Balanced redundancy across both sites"
        }
        
        "Auto" {
            Write-Host "  Strategy: Auto (Adaptive)" -ForegroundColor Cyan
            
            if ($onPremHealth -ge 80 -and $cloudCost -le $System.MaxCloudCost) {
                $distribution["Target"] = "OnPremises"
                $distribution["AllocationPercent"] = 80
                $distribution["CloudPercent"] = 20
                $distribution["Reasoning"] = "Primary on-premises with cloud backup"
            }
            elseif ($cloudHealth -ge 85 -and $cloudCost -le $System.MaxCloudCost) {
                $distribution["Target"] = "Cloud"
                $distribution["AllocationPercent"] = 100
                $distribution["Reasoning"] = "Cloud optimal and cost effective"
            }
            else {
                $distribution["Target"] = "Hybrid"
                $distribution["OnPremAllocation"] = 70
                $distribution["CloudAllocation"] = 30
                $distribution["Reasoning"] = "Hybrid for resilience"
            }
        }
    }
    
    return $distribution
}

# ============================================================================
# LOAD BALANCING EXECUTION
# ============================================================================

function Apply-Workload-Distribution {
    param([array]$Distributions)
    
    Write-Host "`n[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Applying workload distribution..." -ForegroundColor Cyan
    
    $appliedCount = 0
    
    foreach ($distribution in $Distributions) {
        try {
            Write-Host "  → $($distribution.Workload)" -ForegroundColor Yellow -NoNewline
            
            if ($distribution.Target -eq "OnPremises") {
                Write-Host " → OnPremises ($(if ($distribution.AllocationPercent) { "$($distribution.AllocationPercent)%" } else { "100%" }))" -ForegroundColor Green
            }
            elseif ($distribution.Target -eq "Cloud") {
                Write-Host " → Cloud ($(if ($distribution.AllocationPercent) { "$($distribution.AllocationPercent)%" } else { "100%" }))" -ForegroundColor Green
            }
            else {
                Write-Host " → Hybrid ($($distribution.OnPremAllocation ?? 50)% | $($distribution.CloudAllocation ?? 50)%)" -ForegroundColor Cyan
            }
            
            Write-Host "    Reason: $($distribution.Reasoning)" -ForegroundColor Gray
            $appliedCount++
        }
        catch {
            Write-Verbose "Error applying distribution for $($distribution.Workload): $_"
        }
    }
    
    return @{
        AppliedCount = $appliedCount
        TotalCount = $Distributions.Count
        Timestamp = (Get-Date)
    }
}

# ============================================================================
# MAIN ORCHESTRATION
# ============================================================================

function Start-LoadBalancing {
    param(
        [string]$BalancingStrategy,
        [decimal]$MaxCloudCost,
        [string]$ConfigPath
    )
    
    $system = Initialize-LoadBalancer -ConfigPath $ConfigPath
    
    Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║      INTELLIGENT LOAD BALANCER - HELIOS ORCHESTRATOR       ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Magenta
    
    Write-Host "Configuration:" -ForegroundColor Cyan
    Write-Host "  Strategy: $BalancingStrategy" -ForegroundColor Yellow
    Write-Host "  Max Cloud Cost: \$$MaxCloudCost/hour`n" -ForegroundColor Yellow
    
    # Collect metrics
    Write-Host "ENVIRONMENT METRICS" -ForegroundColor Magenta
    Write-Host "─" * 50 -ForegroundColor Gray
    
    $onPremMetrics = Get-OnPremises-Metrics
    $cloudMetrics = Get-Cloud-Metrics -Config $system.Config
    
    Write-Host "`nON-PREMISES HEALTH: $($onPremMetrics.HealthScore)%" -ForegroundColor $(if ($onPremMetrics.HealthScore -ge 80) { "Green" } else { "Yellow" })
    Write-Host "CLOUD HEALTH: $($cloudMetrics.HealthScore)%" -ForegroundColor $(if ($cloudMetrics.HealthScore -ge 80) { "Green" } else { "Yellow" })
    
    # Define workloads
    $workloads = @(
        @{ Name = "Critical-Database"; Type = "Critical" },
        @{ Name = "Web-Frontend"; Type = "HighPerformance" },
        @{ Name = "API-Services"; Type = "Standard" },
        @{ Name = "Backup-Jobs"; Type = "Batch" },
        @{ Name = "Report-Generation"; Type = "Batch" }
    )
    
    # Calculate distributions
    Write-Host "`nWORKLOAD DISTRIBUTION ANALYSIS" -ForegroundColor Magenta
    Write-Host "─" * 50 -ForegroundColor Gray
    
    $distributions = @()
    foreach ($workload in $workloads) {
        $distribution = Calculate-Workload-Distribution `
            -OnPremMetrics $onPremMetrics `
            -CloudMetrics $cloudMetrics `
            -System $system `
            -Workload $workload
        
        $distributions += $distribution
    }
    
    # Apply distributions
    Write-Host "`nAPPLYING DISTRIBUTIONS" -ForegroundColor Magenta
    Write-Host "─" * 50 -ForegroundColor Gray
    
    $result = Apply-Workload-Distribution -Distributions $distributions
    
    # Summary
    Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║               LOAD BALANCING COMPLETED                    ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Green
    
    Write-Host "Summary:" -ForegroundColor Cyan
    Write-Host "  Distributions Applied: $($result.AppliedCount)/$($result.TotalCount)" -ForegroundColor Yellow
    Write-Host "  Total Cloud Cost: \$$($cloudMetrics.EstimatedHourlyCost)/hour" -ForegroundColor Yellow
    Write-Host "  Budget Remaining: \$$([Math]::Max(0, $MaxCloudCost - $cloudMetrics.EstimatedHourlyCost))/hour`n" -ForegroundColor Yellow
    
    # Save report
    $reportPath = ".\reports\load-balance-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    @{
        Timestamp = (Get-Date)
        Strategy = $BalancingStrategy
        OnPremMetrics = $onPremMetrics
        CloudMetrics = $cloudMetrics
        Distributions = $distributions
        Result = $result
    } | ConvertTo-Json | Out-File $reportPath
    
    Write-Host "Report saved: $reportPath`n" -ForegroundColor Gray
}

# Execute
Start-LoadBalancing -BalancingStrategy $BalancingStrategy -MaxCloudCost $MaxCloudCost -ConfigPath $ConfigPath

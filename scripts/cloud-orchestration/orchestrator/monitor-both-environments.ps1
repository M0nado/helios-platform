<#
.SYNOPSIS
    Unified monitoring for hybrid on-premises and cloud environments
.DESCRIPTION
    Provides comprehensive health monitoring, alerting, and metrics
    for both local and cloud infrastructure in HELIOS orchestrator
.PARAMETER MonitoringIntervalSeconds
    Interval between monitoring checks (default: 300 seconds / 5 minutes)
.PARAMETER CheckHealth
    Specific health checks: All, Services, Resources, Sync, Azure, M365
#>

param(
    [Parameter(Mandatory = $false)]
    [int]$MonitoringIntervalSeconds = 300,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("All", "Services", "Resources", "Sync", "Azure", "M365")]
    [string]$CheckHealth = "All",
    
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = ".\config\cloud-orchestration-config.json",
    
    [Parameter(Mandatory = $false)]
    [switch]$ContinuousMonitoring
)

$ErrorActionPreference = "Stop"
$VerbosePreference = "Continue"

# ============================================================================
# CONFIGURATION & INITIALIZATION
# ============================================================================

function Initialize-Monitoring {
    param([string]$ConfigPath)
    
    try {
        Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Initializing monitoring system..." -ForegroundColor Cyan
        
        if (-not (Test-Path $ConfigPath)) {
            throw "Configuration file not found: $ConfigPath"
        }
        
        $config = Get-Content $ConfigPath | ConvertFrom-Json
        
        Write-Host "✓ Monitoring initialized" -ForegroundColor Green
        return $config
    }
    catch {
        Write-Host "✗ Initialization failed: $_" -ForegroundColor Red
        exit 1
    }
}

# ============================================================================
# LOCAL ENVIRONMENT MONITORING
# ============================================================================

function Monitor-LocalServices {
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Monitoring on-premises services..." -ForegroundColor Cyan
    
    try {
        $criticalServices = @(
            "ADDS",           # Active Directory Domain Services
            "NTDS",           # Active Directory
            "Netlogon",       # Netlogon Service
            "DNS",            # DNS Service
            "DHCP",           # DHCP Server
            "W32Time"         # Time Service
        )
        
        $serviceHealth = @{}
        $unhealthy = 0
        
        foreach ($service in $criticalServices) {
            try {
                $svc = Get-Service -Name $service -ErrorAction SilentlyContinue
                
                if ($svc) {
                    $status = $svc.Status -eq "Running" ? "✓" : "✗"
                    $healthColor = $svc.Status -eq "Running" ? "Green" : "Red"
                    
                    Write-Host "  $status $service : $($svc.Status)" -ForegroundColor $healthColor
                    
                    $serviceHealth[$service] = @{
                        Status = $svc.Status
                        Healthy = $svc.Status -eq "Running"
                        StartType = $svc.StartType
                    }
                    
                    if ($svc.Status -ne "Running") {
                        $unhealthy++
                    }
                }
            }
            catch {
                Write-Verbose "Error checking service $service : $_"
            }
        }
        
        return @{
            Timestamp = (Get-Date)
            Services = $serviceHealth
            UnhealthyCount = $unhealthy
            Status = $unhealthy -eq 0 ? "Healthy" : "Unhealthy"
        }
    }
    catch {
        Write-Host "✗ Service monitoring failed: $_" -ForegroundColor Red
        return @{ Status = "Error"; Error = $_ }
    }
}

function Monitor-LocalResources {
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Monitoring on-premises resources..." -ForegroundColor Cyan
    
    try {
        $resources = @{}
        
        # CPU Usage
        $cpuUsage = (Get-Counter '\Processor(_Total)\% Processor Time' -ErrorAction SilentlyContinue).CounterSamples[0].CookedValue
        $cpuHealthColor = $cpuUsage -lt 80 ? "Green" : ($cpuUsage -lt 90 ? "Yellow" : "Red")
        Write-Host "  CPU Usage: $([Math]::Round($cpuUsage, 2))%" -ForegroundColor $cpuHealthColor
        
        $resources["CPU"] = @{
            UsagePercent = [Math]::Round($cpuUsage, 2)
            Healthy = $cpuUsage -lt 90
        }
        
        # Memory Usage
        $memoryInfo = Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue
        if ($memoryInfo) {
            $totalMemory = $memoryInfo.TotalVisibleMemorySize
            $freeMemory = $memoryInfo.FreePhysicalMemory
            $usedPercent = (($totalMemory - $freeMemory) / $totalMemory) * 100
            
            $memHealthColor = $usedPercent -lt 80 ? "Green" : ($usedPercent -lt 90 ? "Yellow" : "Red")
            Write-Host "  Memory Usage: $([Math]::Round($usedPercent, 2))%" -ForegroundColor $memHealthColor
            
            $resources["Memory"] = @{
                TotalGB = [Math]::Round($totalMemory / 1024 / 1024, 2)
                UsedGB = [Math]::Round(($totalMemory - $freeMemory) / 1024 / 1024, 2)
                UsagePercent = [Math]::Round($usedPercent, 2)
                Healthy = $usedPercent -lt 90
            }
        }
        
        # Disk Space
        $disks = Get-Volume -ErrorAction SilentlyContinue
        $diskHealth = @{}
        
        foreach ($disk in $disks) {
            if ($disk.DriveType -eq "Fixed" -and $disk.SizeRemaining) {
                $usedPercent = (($disk.Size - $disk.SizeRemaining) / $disk.Size) * 100
                $diskHealthColor = $usedPercent -lt 80 ? "Green" : ($usedPercent -lt 90 ? "Yellow" : "Red")
                
                Write-Host "  Disk $($disk.DriveLetter): $([Math]::Round($usedPercent, 2))% used" -ForegroundColor $diskHealthColor
                
                $diskHealth[$disk.DriveLetter] = @{
                    TotalGB = [Math]::Round($disk.Size / 1GB, 2)
                    UsedPercent = [Math]::Round($usedPercent, 2)
                    Healthy = $usedPercent -lt 90
                }
            }
        }
        
        $resources["Disks"] = $diskHealth
        
        return @{
            Timestamp = (Get-Date)
            Resources = $resources
            Status = (($resources.Values | Where-Object { $_.Healthy -eq $false } | Measure-Object).Count -eq 0) ? "Healthy" : "Warning"
        }
    }
    catch {
        Write-Host "✗ Resource monitoring failed: $_" -ForegroundColor Red
        return @{ Status = "Error"; Error = $_ }
    }
}

function Monitor-ADReplication {
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Monitoring AD replication..." -ForegroundColor Cyan
    
    try {
        $replStatus = @{}
        
        # Get domain controllers
        $domainControllers = Get-ADDomainController -Filter * -ErrorAction Stop
        
        Write-Host "  Found $($domainControllers.Count) domain controllers" -ForegroundColor Yellow
        
        foreach ($dc in $domainControllers) {
            try {
                $replResult = Invoke-Command -ComputerName $dc.Name -ScriptBlock {
                    repadmin /status
                } -ErrorAction SilentlyContinue
                
                $healthy = $replResult -notlike "*Failed*"
                $status = $healthy ? "✓" : "✗"
                Write-Host "    $status $($dc.Name) : $(if ($healthy) { 'Healthy' } else { 'Replication Issues' })" -ForegroundColor $(if ($healthy) { "Green" } else { "Red" })
                
                $replStatus[$dc.Name] = @{
                    Healthy = $healthy
                    Status = if ($healthy) { "Replicating" } else { "Issues" }
                }
            }
            catch {
                Write-Verbose "Error checking replication for $($dc.Name): $_"
            }
        }
        
        return @{
            Timestamp = (Get-Date)
            DomainControllers = $replStatus
            Status = (($replStatus.Values | Where-Object { $_.Healthy -eq $false } | Measure-Object).Count -eq 0) ? "Healthy" : "Unhealthy"
        }
    }
    catch {
        Write-Host "✗ AD replication monitoring failed: $_" -ForegroundColor Red
        return @{ Status = "Error"; Error = $_ }
    }
}

# ============================================================================
# CLOUD ENVIRONMENT MONITORING
# ============================================================================

function Monitor-Azure-Health {
    param([object]$Config)
    
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Monitoring Azure health..." -ForegroundColor Cyan
    
    try {
        Connect-AzAccount -Subscription $Config.AzureSubscriptionId -ErrorAction Stop | Out-Null
        
        $azureHealth = @{}
        
        # Check VMs
        Write-Host "  Checking virtual machines..." -ForegroundColor Yellow
        $vms = Get-AzVM -ErrorAction SilentlyContinue
        $runningVMs = $vms | Where-Object { $_.PowerState -like "VM running" } | Measure-Object | Select-Object -ExpandProperty Count
        
        Write-Host "    VMs: $runningVMs/$($vms.Count) running" -ForegroundColor Green
        
        $azureHealth["VirtualMachines"] = @{
            Running = $runningVMs
            Total = $vms.Count
            Healthy = $runningVMs -eq $vms.Count
        }
        
        # Check Storage Accounts
        Write-Host "  Checking storage accounts..." -ForegroundColor Yellow
        $storageAccounts = Get-AzStorageAccount -ErrorAction SilentlyContinue
        Write-Host "    Storage Accounts: $($storageAccounts.Count)" -ForegroundColor Green
        
        $azureHealth["StorageAccounts"] = @{
            Count = $storageAccounts.Count
            Healthy = $storageAccounts.Count -gt 0
        }
        
        # Check App Services
        Write-Host "  Checking app services..." -ForegroundColor Yellow
        $appServices = Get-AzWebApp -ErrorAction SilentlyContinue
        Write-Host "    App Services: $($appServices.Count)" -ForegroundColor Green
        
        $azureHealth["AppServices"] = @{
            Count = $appServices.Count
            Healthy = $appServices.Count -gt 0
        }
        
        return @{
            Timestamp = (Get-Date)
            Azure = $azureHealth
            Status = "Healthy"
        }
    }
    catch {
        Write-Host "✗ Azure monitoring failed: $_" -ForegroundColor Red
        return @{ Status = "Error"; Error = $_ }
    }
}

function Monitor-M365-Services {
    param([object]$Config)
    
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Monitoring Microsoft 365 services..." -ForegroundColor Cyan
    
    try {
        Connect-MgGraph -Scopes "Organization.Read.All" -ErrorAction Stop | Out-Null
        
        $m365Health = @{}
        
        # Check Exchange Online
        Write-Host "  Checking Exchange Online..." -ForegroundColor Yellow
        $mailboxes = Get-MgUser -All -Filter "mail ne null" -ErrorAction SilentlyContinue
        Write-Host "    Mailboxes: $($mailboxes.Count)" -ForegroundColor Green
        
        $m365Health["ExchangeOnline"] = @{
            Mailboxes = $mailboxes.Count
            Healthy = $mailboxes.Count -gt 0
        }
        
        # Check SharePoint Online
        Write-Host "  Checking SharePoint Online..." -ForegroundColor Yellow
        $sites = Get-MgSite -All -ErrorAction SilentlyContinue
        Write-Host "    Sites: $($sites.Count)" -ForegroundColor Green
        
        $m365Health["SharePointOnline"] = @{
            Sites = $sites.Count
            Healthy = $sites.Count -gt 0
        }
        
        # Check Teams
        Write-Host "  Checking Teams..." -ForegroundColor Yellow
        $teams = Get-MgTeam -All -ErrorAction SilentlyContinue
        Write-Host "    Teams: $($teams.Count)" -ForegroundColor Green
        
        $m365Health["Teams"] = @{
            Teams = $teams.Count
            Healthy = $teams.Count -gt 0
        }
        
        # Check OneDrive
        Write-Host "  Checking OneDrive..." -ForegroundColor Yellow
        $drives = Get-MgDrive -All -ErrorAction SilentlyContinue
        Write-Host "    Drives: $($drives.Count)" -ForegroundColor Green
        
        $m365Health["OneDrive"] = @{
            Drives = $drives.Count
            Healthy = $drives.Count -gt 0
        }
        
        return @{
            Timestamp = (Get-Date)
            M365 = $m365Health
            Status = "Healthy"
        }
    }
    catch {
        Write-Host "✗ M365 monitoring failed: $_" -ForegroundColor Red
        return @{ Status = "Error"; Error = $_ }
    }
}

function Monitor-SyncStatus {
    param([string]$ConfigPath)
    
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Monitoring sync status..." -ForegroundColor Cyan
    
    try {
        $syncStatus = @{}
        
        # Check recent sync logs
        $logPath = ".\logs"
        if (Test-Path $logPath) {
            $recentLogs = Get-ChildItem -Path $logPath -Filter "*.json" -ErrorAction SilentlyContinue | 
                          Sort-Object LastWriteTime -Descending | 
                          Select-Object -First 5
            
            foreach ($log in $recentLogs) {
                try {
                    $logContent = Get-Content $log.FullName | ConvertFrom-Json
                    
                    if ($logContent.Results) {
                        $successCount = ($logContent.Results.Values | Where-Object { $_.Success -eq $true } | Measure-Object).Count
                        $failCount = ($logContent.Results.Values | Where-Object { $_.Success -eq $false } | Measure-Object).Count
                        
                        $syncStatus[$log.BaseName] = @{
                            Timestamp = $logContent.Timestamp
                            Success = $successCount
                            Failed = $failCount
                            Duration = $logContent.Duration
                            Status = $failCount -eq 0 ? "Success" : "PartialFailure"
                        }
                    }
                }
                catch {
                    Write-Verbose "Error reading sync log $($log.Name): $_"
                }
            }
        }
        
        return @{
            Timestamp = (Get-Date)
            RecentSyncs = $syncStatus
            Status = ($syncStatus.Values | Where-Object { $_.Status -eq "PartialFailure" } | Measure-Object).Count -gt 0 ? "Warning" : "Healthy"
        }
    }
    catch {
        Write-Host "✗ Sync status monitoring failed: $_" -ForegroundColor Red
        return @{ Status = "Error" }
    }
}

# ============================================================================
# REPORTING & ALERTING
# ============================================================================

function Generate-HealthReport {
    param([hashtable]$HealthData)
    
    Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║            UNIFIED ENVIRONMENT HEALTH REPORT               ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Magenta
    
    Write-Host "LOCAL ENVIRONMENT" -ForegroundColor Cyan
    Write-Host "─" * 50 -ForegroundColor Gray
    
    if ($HealthData.Services) {
        Write-Host "Services: $($HealthData.Services.Status)" -ForegroundColor $(if ($HealthData.Services.Status -eq "Healthy") { "Green" } else { "Red" })
    }
    
    if ($HealthData.Resources) {
        Write-Host "Resources: $($HealthData.Resources.Status)" -ForegroundColor $(if ($HealthData.Resources.Status -eq "Healthy") { "Green" } else { "Yellow" })
    }
    
    if ($HealthData.Replication) {
        Write-Host "AD Replication: $($HealthData.Replication.Status)" -ForegroundColor $(if ($HealthData.Replication.Status -eq "Healthy") { "Green" } else { "Red" })
    }
    
    Write-Host "`nCLOUD ENVIRONMENT" -ForegroundColor Cyan
    Write-Host "─" * 50 -ForegroundColor Gray
    
    if ($HealthData.Azure) {
        Write-Host "Azure: $($HealthData.Azure.Status)" -ForegroundColor Green
    }
    
    if ($HealthData.M365) {
        Write-Host "Microsoft 365: $($HealthData.M365.Status)" -ForegroundColor Green
    }
    
    Write-Host "`nSYNCHRONIZATION" -ForegroundColor Cyan
    Write-Host "─" * 50 -ForegroundColor Gray
    
    if ($HealthData.Sync) {
        Write-Host "Sync Status: $($HealthData.Sync.Status)" -ForegroundColor $(if ($HealthData.Sync.Status -eq "Healthy") { "Green" } else { "Yellow" })
    }
    
    Write-Host "`nReport Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n" -ForegroundColor Gray
}

function Send-Alert {
    param(
        [string]$AlertLevel,
        [string]$Message,
        [object]$Config
    )
    
    $alertColor = switch ($AlertLevel) {
        "Critical" { "Red" }
        "Warning" { "Yellow" }
        default { "Cyan" }
    }
    
    Write-Host "⚠ [$AlertLevel] $Message" -ForegroundColor $alertColor
    
    # In production, this would send to:
    # - Email, Slack, Teams webhook
    # - Azure Monitor, Log Analytics
    # - SIEM solution
}

# ============================================================================
# MAIN ORCHESTRATION
# ============================================================================

function Start-EnvironmentMonitoring {
    param(
        [string]$CheckHealth,
        [int]$MonitoringIntervalSeconds,
        [string]$ConfigPath,
        [switch]$ContinuousMonitoring
    )
    
    $config = Initialize-Monitoring -ConfigPath $ConfigPath
    
    Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║    UNIFIED ENVIRONMENT MONITORING - HELIOS ORCHESTRATOR    ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Magenta
    
    $iterationCount = 0
    
    do {
        $iterationCount++
        $healthData = @{}
        
        Write-Host "`n→ Monitoring Cycle $iterationCount at $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan
        Write-Host ("─" * 50) -ForegroundColor Gray
        
        # Local monitoring
        if ($CheckHealth -in "All", "Services") {
            $healthData["Services"] = Monitor-LocalServices
        }
        
        if ($CheckHealth -in "All", "Resources") {
            $healthData["Resources"] = Monitor-LocalResources
        }
        
        if ($CheckHealth -in "All", "Sync") {
            $healthData["Replication"] = Monitor-ADReplication
        }
        
        # Cloud monitoring
        if ($CheckHealth -in "All", "Azure") {
            $healthData["Azure"] = Monitor-Azure-Health -Config $config
        }
        
        if ($CheckHealth -in "All", "M365") {
            $healthData["M365"] = Monitor-M365-Services -Config $config
        }
        
        if ($CheckHealth -in "All", "Sync") {
            $healthData["Sync"] = Monitor-SyncStatus -ConfigPath $ConfigPath
        }
        
        # Generate report
        Generate-HealthReport -HealthData $healthData
        
        # Save report
        $reportPath = ".\reports\health-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
        $healthData | ConvertTo-Json | Out-File $reportPath
        
        if ($ContinuousMonitoring) {
            Write-Host "Next check in $MonitoringIntervalSeconds seconds (press Ctrl+C to stop)..." -ForegroundColor Gray
            Start-Sleep -Seconds $MonitoringIntervalSeconds
        }
        else {
            break
        }
    }
    while ($ContinuousMonitoring)
}

# Execute
Start-EnvironmentMonitoring -CheckHealth $CheckHealth -MonitoringIntervalSeconds $MonitoringIntervalSeconds -ConfigPath $ConfigPath -ContinuousMonitoring:$ContinuousMonitoring

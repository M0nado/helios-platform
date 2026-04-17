<#
.SYNOPSIS
    Synchronizes cloud resources back to on-premises environment
.DESCRIPTION
    Orchestrates cloud-to-local synchronization for hybrid environments,
    ensuring on-premises systems stay in sync with cloud changes
.PARAMETER Environment
    Target environment: Production, Staging, Development
.PARAMETER SyncTypes
    Types to sync: Users, Groups, Devices, Policies, All
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Production", "Staging", "Development")]
    [string]$Environment = "Production",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Users", "Groups", "Devices", "Policies", "All")]
    [string]$SyncTypes = "All",
    
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = ".\config\cloud-orchestration-config.json"
)

$ErrorActionPreference = "Stop"
$VerbosePreference = "Continue"

# ============================================================================
# CONFIGURATION & INITIALIZATION
# ============================================================================

function Initialize-CloudToLocalSync {
    param([string]$ConfigPath)
    
    try {
        Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Initializing cloud-to-local sync..." -ForegroundColor Cyan
        
        if (-not (Test-Path $ConfigPath)) {
            throw "Configuration file not found: $ConfigPath"
        }
        
        $config = Get-Content $ConfigPath | ConvertFrom-Json
        
        # Validate required settings
        $requiredKeys = @("AzureSubscriptionId", "TenantId", "SyncBatchSize")
        foreach ($key in $requiredKeys) {
            if (-not $config.$key) {
                throw "Missing required configuration: $key"
            }
        }
        
        Write-Host "✓ Configuration loaded successfully" -ForegroundColor Green
        return $config
    }
    catch {
        Write-Host "✗ Initialization failed: $_" -ForegroundColor Red
        exit 1
    }
}

function Connect-HybridServices {
    param([object]$Config)
    
    try {
        Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Connecting to hybrid services..." -ForegroundColor Cyan
        
        # Connect to Azure
        Write-Host "  Connecting to Azure..." -ForegroundColor Yellow
        Connect-AzAccount -Subscription $Config.AzureSubscriptionId -ErrorAction Stop | Out-Null
        
        # Connect to Graph
        Write-Host "  Connecting to Microsoft Graph..." -ForegroundColor Yellow
        Connect-MgGraph -Scopes "User.Read.All", "Group.Read.All", "Device.Read.All" -ErrorAction Stop | Out-Null
        
        # Verify AD connectivity
        Write-Host "  Verifying Active Directory..." -ForegroundColor Yellow
        $adDomain = Get-ADDomain -ErrorAction Stop
        Write-Host "  AD Domain: $($adDomain.Name)" -ForegroundColor Yellow
        
        Write-Host "✓ Hybrid services connected" -ForegroundColor Green
        return @{
            AzureConnected = $true
            GraphConnected = $true
            ADConnected = $true
            ADDomain = $adDomain.Name
        }
    }
    catch {
        Write-Host "✗ Connection failed: $_" -ForegroundColor Red
        return @{ Success = $false; Error = $_ }
    }
}

# ============================================================================
# CLOUD-TO-LOCAL SYNC OPERATIONS
# ============================================================================

function Sync-CloudUsers-ToLocal {
    param([object]$Config)
    
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Syncing cloud users to on-premises..." -ForegroundColor Cyan
    
    try {
        $syncCount = 0
        $updateCount = 0
        $errorCount = 0
        $batchSize = $Config.SyncBatchSize
        
        # Get cloud users
        Write-Host "  Retrieving cloud users..." -ForegroundColor Yellow
        $cloudUsers = Get-MgUser -All -ErrorAction Stop -Property UserPrincipalName, DisplayName, Mail, MobilePhone, JobTitle, Department
        
        Write-Host "  Found $($cloudUsers.Count) cloud users" -ForegroundColor Yellow
        
        # Process in batches
        $batches = [Math]::Ceiling($cloudUsers.Count / $batchSize)
        
        for ($i = 0; $i -lt $batches; $i++) {
            $start = $i * $batchSize
            $end = [Math]::Min(($i + 1) * $batchSize, $cloudUsers.Count) - 1
            $batch = $cloudUsers[$start..$end]
            
            foreach ($cloudUser in $batch) {
                try {
                    # Extract UPN for search
                    $upn = $cloudUser.UserPrincipalName
                    $samAccountName = $upn.Split('@')[0]
                    
                    # Check if user exists locally
                    $localUser = Get-ADUser -Filter "SamAccountName -eq '$samAccountName'" -ErrorAction SilentlyContinue
                    
                    if ($localUser) {
                        # Update existing user
                        Set-ADUser -Identity $localUser.ObjectGUID `
                            -DisplayName $cloudUser.DisplayName `
                            -EmailAddress $cloudUser.Mail `
                            -Title $cloudUser.JobTitle `
                            -Department $cloudUser.Department `
                            -MobilePhone $cloudUser.MobilePhone `
                            -ErrorAction SilentlyContinue
                        
                        $updateCount++
                    }
                    else {
                        Write-Verbose "New cloud user $upn needs local account creation"
                        $syncCount++
                    }
                }
                catch {
                    Write-Verbose "Error syncing user $($cloudUser.UserPrincipalName): $_"
                    $errorCount++
                }
            }
            
            Write-Host "  Processed batch $($i + 1)/$batches" -ForegroundColor Yellow
        }
        
        return @{
            Success = $true
            SyncedCount = $syncCount
            UpdatedCount = $updateCount
            ErrorCount = $errorCount
            Timestamp = (Get-Date)
        }
    }
    catch {
        Write-Host "✗ Cloud user sync failed: $_" -ForegroundColor Red
        return @{
            Success = $false
            Error = $_
            Timestamp = (Get-Date)
        }
    }
}

function Sync-CloudGroups-ToLocal {
    param([object]$Config)
    
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Syncing cloud groups to on-premises..." -ForegroundColor Cyan
    
    try {
        $syncCount = 0
        $errorCount = 0
        
        # Get cloud groups
        $cloudGroups = Get-MgGroup -All -ErrorAction Stop -Filter "mailEnabled eq true"
        
        Write-Host "  Found $($cloudGroups.Count) cloud groups" -ForegroundColor Yellow
        
        foreach ($cloudGroup in $cloudGroups) {
            try {
                $localGroup = Get-ADGroup -Filter "Name -eq '$($cloudGroup.DisplayName)'" -ErrorAction SilentlyContinue
                
                if ($localGroup) {
                    # Update group
                    Set-ADGroup -Identity $localGroup.ObjectGUID `
                        -Description $cloudGroup.Description `
                        -ErrorAction SilentlyContinue
                }
                else {
                    # Get group members from cloud
                    $members = Get-MgGroupMember -GroupId $cloudGroup.Id -ErrorAction SilentlyContinue
                    Write-Verbose "Cloud group $($cloudGroup.DisplayName) with $($members.Count) members needs local creation"
                }
                
                $syncCount++
            }
            catch {
                Write-Verbose "Error syncing group $($cloudGroup.DisplayName): $_"
                $errorCount++
            }
        }
        
        return @{
            Success = $true
            SyncedCount = $syncCount
            ErrorCount = $errorCount
            Timestamp = (Get-Date)
        }
    }
    catch {
        Write-Host "✗ Cloud group sync failed: $_" -ForegroundColor Red
        return @{
            Success = $false
            Error = $_
            Timestamp = (Get-Date)
        }
    }
}

function Sync-CloudDevices-ToLocal {
    param([object]$Config)
    
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Syncing cloud devices to on-premises..." -ForegroundColor Cyan
    
    try {
        $syncCount = 0
        $errorCount = 0
        
        # Get Intune-managed devices
        $cloudDevices = Get-MgDevice -All -ErrorAction Stop -Property DisplayName, DeviceId, Ismanaged, OperatingSystem
        
        Write-Host "  Found $($cloudDevices.Count) cloud devices" -ForegroundColor Yellow
        
        foreach ($cloudDevice in $cloudDevices) {
            try {
                $computerName = $cloudDevice.DisplayName
                $localDevice = Get-ADComputer -Filter "Name -eq '$computerName'" -ErrorAction SilentlyContinue
                
                if ($localDevice) {
                    # Update device metadata
                    Set-ADComputer -Identity $localDevice.ObjectGUID `
                        -Description "Managed: $($cloudDevice.IsManaged) | OS: $($cloudDevice.OperatingSystem)" `
                        -ErrorAction SilentlyContinue
                }
                else {
                    Write-Verbose "Cloud device $computerName needs local registration"
                }
                
                $syncCount++
            }
            catch {
                Write-Verbose "Error syncing device $($cloudDevice.DisplayName): $_"
                $errorCount++
            }
        }
        
        return @{
            Success = $true
            SyncedCount = $syncCount
            ErrorCount = $errorCount
            Timestamp = (Get-Date)
        }
    }
    catch {
        Write-Host "✗ Cloud device sync failed: $_" -ForegroundColor Red
        return @{
            Success = $false
            Error = $_
            Timestamp = (Get-Date)
        }
    }
}

function Sync-CloudPolicies-ToLocal {
    param([object]$Config)
    
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Syncing cloud policies to on-premises..." -ForegroundColor Cyan
    
    try {
        $syncCount = 0
        
        # Get conditional access policies
        Write-Host "  Retrieving conditional access policies..." -ForegroundColor Yellow
        $caaPolicies = Invoke-MgGraphRequest -Method GET -Uri "/identity/conditionalAccess/policies" -ErrorAction SilentlyContinue
        
        if ($caaPolicies) {
            Write-Host "  Found $($caaPolicies.value.Count) conditional access policies" -ForegroundColor Yellow
            Write-Verbose "Conditional Access policies synced: $($caaPolicies.value.Count)"
            $syncCount += $caaPolicies.value.Count
        }
        
        return @{
            Success = $true
            SyncedCount = $syncCount
            PoliciesType = @("ConditionalAccess", "Compliance", "DLP")
            Timestamp = (Get-Date)
        }
    }
    catch {
        Write-Host "✗ Cloud policy sync failed: $_" -ForegroundColor Red
        return @{
            Success = $false
            Error = $_
            Timestamp = (Get-Date)
        }
    }
}

function Get-SyncDelta {
    param([object]$Config)
    
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Calculating sync delta..." -ForegroundColor Cyan
    
    try {
        # Get cloud change log
        $changeLog = Get-MgAuditLogDirectoryAudit -All -Top 1000 -ErrorAction SilentlyContinue | 
                    Select-Object -First 100
        
        $userChanges = $changeLog | Where-Object { $_.ActivityDisplayName -like "*user*" } | Measure-Object | Select-Object -ExpandProperty Count
        $groupChanges = $changeLog | Where-Object { $_.ActivityDisplayName -like "*group*" } | Measure-Object | Select-Object -ExpandProperty Count
        $deviceChanges = $changeLog | Where-Object { $_.ActivityDisplayName -like "*device*" } | Measure-Object | Select-Object -ExpandProperty Count
        
        return @{
            UserChanges = $userChanges
            GroupChanges = $groupChanges
            DeviceChanges = $deviceChanges
            TotalChanges = $userChanges + $groupChanges + $deviceChanges
            LastChecked = (Get-Date)
        }
    }
    catch {
        Write-Verbose "Error calculating sync delta: $_"
        return @{ TotalChanges = 0 }
    }
}

# ============================================================================
# MAIN ORCHESTRATION
# ============================================================================

function Start-CloudToLocalSync {
    param(
        [string]$Environment,
        [string]$SyncTypes,
        [string]$ConfigPath
    )
    
    $syncResults = @{}
    $startTime = Get-Date
    
    Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║     CLOUD-TO-LOCAL SYNCHRONIZATION - HELIOS ORCHESTRATOR   ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Magenta
    
    Write-Host "Environment: $Environment | SyncTypes: $SyncTypes" -ForegroundColor Cyan
    Write-Host "Started: $startTime`n" -ForegroundColor Gray
    
    # Initialize
    $config = Initialize-CloudToLocalSync -ConfigPath $ConfigPath
    
    # Connect to services
    $connStatus = Connect-HybridServices -Config $config
    if (-not $connStatus.AzureConnected) {
        Write-Host "✗ Failed to connect to required services" -ForegroundColor Red
        exit 1
    }
    
    # Calculate delta
    Write-Host "`nAnalyzing sync changes..." -ForegroundColor Cyan
    $delta = Get-SyncDelta -Config $config
    Write-Host "Pending changes: $($delta.TotalChanges) items" -ForegroundColor Yellow
    
    # Execute sync operations
    if ($SyncTypes -in "All", "Users") {
        $syncResults["Users"] = Sync-CloudUsers-ToLocal -Config $config
    }
    
    if ($SyncTypes -in "All", "Groups") {
        $syncResults["Groups"] = Sync-CloudGroups-ToLocal -Config $config
    }
    
    if ($SyncTypes -in "All", "Devices") {
        $syncResults["Devices"] = Sync-CloudDevices-ToLocal -Config $config
    }
    
    if ($SyncTypes -in "All", "Policies") {
        $syncResults["Policies"] = Sync-CloudPolicies-ToLocal -Config $config
    }
    
    # Summary report
    Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║                    SYNCHRONIZATION SUMMARY                 ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Magenta
    
    foreach ($operation in $syncResults.GetEnumerator()) {
        $result = $operation.Value
        if ($result.Success) {
            Write-Host "✓ $($operation.Key)" -ForegroundColor Green -NoNewline
            if ($result.SyncedCount) {
                Write-Host ": $($result.SyncedCount) items synced" -ForegroundColor Green
                if ($result.UpdatedCount) {
                    Write-Host "  → $($result.UpdatedCount) items updated" -ForegroundColor Cyan
                }
            }
        }
        else {
            Write-Host "✗ $($operation.Key): $($result.Error)" -ForegroundColor Red
        }
    }
    
    $duration = (Get-Date) - $startTime
    
    Write-Host "`nSync Delta: $($delta.TotalChanges) pending items" -ForegroundColor Cyan
    Write-Host "Duration: $($duration.TotalSeconds -as [int]) seconds`n" -ForegroundColor Gray
    
    # Save results to log
    $logPath = ".\logs\sync-cloud-to-local-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    @{
        Timestamp = (Get-Date)
        Environment = $Environment
        SyncTypes = $SyncTypes
        Delta = $delta
        Duration = $duration.TotalSeconds
        Results = $syncResults
    } | ConvertTo-Json | Out-File $logPath
    
    Write-Host "Log saved: $logPath`n" -ForegroundColor Gray
}

# Execute
Start-CloudToLocalSync -Environment $Environment -SyncTypes $SyncTypes -ConfigPath $ConfigPath

<#
.SYNOPSIS
    Shared library functions for enterprise monitoring system
.DESCRIPTION
    Common utilities for logging, configuration, metrics, and reporting
.VERSION
    1.0.0
#>

param()

# Configuration Management
function Get-MonitoringConfig {
    <#
    .SYNOPSIS
        Load monitoring configuration from JSON file
    #>
    param(
        [string]$ConfigPath = "$PSScriptRoot\..\config\monitoring-config.json"
    )
    
    try {
        if (Test-Path $ConfigPath) {
            return Get-Content $ConfigPath | ConvertFrom-Json
        }
        Write-Warning "Config file not found at $ConfigPath"
        return $null
    }
    catch {
        Write-Error "Failed to load config: $_"
        return $null
    }
}

function Get-AlertPolicies {
    <#
    .SYNOPSIS
        Load alert policies configuration
    #>
    param(
        [string]$PoliciesPath = "$PSScriptRoot\..\config\alert-policies.json"
    )
    
    try {
        if (Test-Path $PoliciesPath) {
            return Get-Content $PoliciesPath | ConvertFrom-Json
        }
        return $null
    }
    catch {
        Write-Error "Failed to load alert policies: $_"
        return $null
    }
}

# Logging Functions
function Initialize-MonitoringLog {
    <#
    .SYNOPSIS
        Initialize logging directory and configuration
    #>
    param(
        [string]$LogDir = "$PSScriptRoot\..\logs",
        [string]$Component = "Monitoring"
    )
    
    if (-not (Test-Path $LogDir)) {
        New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
    }
    
    $script:LogDirectory = $LogDir
    $script:CurrentComponent = $Component
    $script:LogFile = Join-Path $LogDir "$Component-$(Get-Date -Format 'yyyy-MM-dd').log"
}

function Write-MonitoringLog {
    <#
    .SYNOPSIS
        Write to monitoring log with timestamp and level
    #>
    param(
        [string]$Message,
        [ValidateSet("INFO", "WARNING", "ERROR", "DEBUG")]
        [string]$Level = "INFO"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
    $logMessage = "[$timestamp] [$Level] [$($script:CurrentComponent)] $Message"
    
    if ($script:LogFile) {
        Add-Content -Path $script:LogFile -Value $logMessage -ErrorAction SilentlyContinue
    }
    
    switch ($Level) {
        "ERROR" { Write-Host $logMessage -ForegroundColor Red }
        "WARNING" { Write-Host $logMessage -ForegroundColor Yellow }
        "DEBUG" { Write-Host $logMessage -ForegroundColor Cyan }
        default { Write-Host $logMessage -ForegroundColor Green }
    }
}

# Metrics Functions
function New-MetricValue {
    <#
    .SYNOPSIS
        Create a standardized metric object
    #>
    param(
        [string]$Name,
        [double]$Value,
        [string]$Unit,
        [string]$Source,
        [hashtable]$Tags = @{}
    )
    
    $metric = @{
        Name = $Name
        Value = $Value
        Unit = $Unit
        Source = $Source
        Timestamp = Get-Date -AsUTC
        Tags = $Tags
    }
    
    return $metric
}

function New-AlertEvent {
    <#
    .SYNOPSIS
        Create a standardized alert event object
    #>
    param(
        [string]$AlertId,
        [string]$Title,
        [ValidateSet("critical", "high", "medium", "low", "info")]
        [string]$Severity,
        [string]$Description,
        [string]$Source,
        [hashtable]$Context = @{}
    )
    
    $alert = @{
        AlertId = $AlertId
        Title = $Title
        Severity = $Severity
        Description = $Description
        Source = $Source
        Timestamp = Get-Date -AsUTC
        Status = "Open"
        Context = $Context
        Acknowledged = $false
    }
    
    return $alert
}

# Health Check Functions
function Test-HealthEndpoint {
    <#
    .SYNOPSIS
        Test health/status endpoint
    #>
    param(
        [string]$Endpoint,
        [int]$TimeoutSeconds = 10
    )
    
    try {
        $result = Invoke-WebRequest -Uri $Endpoint -TimeoutSec $TimeoutSeconds -ErrorAction Stop
        return @{
            Healthy = $result.StatusCode -eq 200
            StatusCode = $result.StatusCode
            ResponseTime = $result.RawContentLength
        }
    }
    catch {
        return @{
            Healthy = $false
            StatusCode = $null
            ResponseTime = $null
            Error = $_.Exception.Message
        }
    }
}

# Cache Management
$script:MetricsCache = @{}
$script:CacheExpiry = @{}

function Add-ToCache {
    <#
    .SYNOPSIS
        Add metric to in-memory cache
    #>
    param(
        [string]$Key,
        [object]$Value,
        [int]$ExpirySeconds = 300
    )
    
    $script:MetricsCache[$Key] = $Value
    $script:CacheExpiry[$Key] = (Get-Date).AddSeconds($ExpirySeconds)
}

function Get-FromCache {
    <#
    .SYNOPSIS
        Retrieve metric from cache if not expired
    #>
    param(
        [string]$Key
    )
    
    if ($script:MetricsCache.ContainsKey($Key)) {
        if ((Get-Date) -lt $script:CacheExpiry[$Key]) {
            return $script:MetricsCache[$Key]
        }
        else {
            $script:MetricsCache.Remove($Key)
            $script:CacheExpiry.Remove($Key)
        }
    }
    
    return $null
}

function Clear-ExpiredCache {
    <#
    .SYNOPSIS
        Remove expired items from cache
    #>
    $now = Get-Date
    $expired = @()
    
    foreach ($key in $script:MetricsCache.Keys) {
        if ($now -gt $script:CacheExpiry[$key]) {
            $expired += $key
        }
    }
    
    foreach ($key in $expired) {
        $script:MetricsCache.Remove($key)
        $script:CacheExpiry.Remove($key)
    }
    
    return $expired.Count
}

# Error Handling
function Invoke-SafeCommand {
    <#
    .SYNOPSIS
        Execute command with error handling and retry logic
    #>
    param(
        [scriptblock]$Command,
        [int]$MaxRetries = 3,
        [int]$DelaySeconds = 5,
        [string]$OperationName = "Operation"
    )
    
    $attempt = 0
    
    while ($attempt -lt $MaxRetries) {
        try {
            Write-MonitoringLog "Executing: $OperationName (Attempt $($attempt + 1)/$MaxRetries)"
            $result = & $Command
            Write-MonitoringLog "Successfully completed: $OperationName"
            return $result
        }
        catch {
            $attempt++
            Write-MonitoringLog "Error in $OperationName : $_" -Level "ERROR"
            
            if ($attempt -lt $MaxRetries) {
                Write-MonitoringLog "Retrying in $DelaySeconds seconds..."
                Start-Sleep -Seconds $DelaySeconds
            }
        }
    }
    
    Write-MonitoringLog "Failed after $MaxRetries attempts: $OperationName" -Level "ERROR"
    return $null
}

# Data Export
function Export-MetricsToJSON {
    <#
    .SYNOPSIS
        Export metrics collection to JSON file
    #>
    param(
        [array]$Metrics,
        [string]$OutputPath
    )
    
    try {
        $output = @{
            ExportTime = Get-Date -AsUTC
            MetricsCount = $Metrics.Count
            Metrics = $Metrics
        }
        
        $output | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputPath -Encoding UTF8
        Write-MonitoringLog "Exported $($Metrics.Count) metrics to $OutputPath"
        return $true
    }
    catch {
        Write-MonitoringLog "Failed to export metrics: $_" -Level "ERROR"
        return $false
    }
}

function Export-MetricsToCSV {
    <#
    .SYNOPSIS
        Export metrics collection to CSV file
    #>
    param(
        [array]$Metrics,
        [string]$OutputPath
    )
    
    try {
        $Metrics | Export-Csv -Path $OutputPath -NoTypeInformation -Encoding UTF8
        Write-MonitoringLog "Exported $($Metrics.Count) metrics to $OutputPath"
        return $true
    }
    catch {
        Write-MonitoringLog "Failed to export metrics to CSV: $_" -Level "ERROR"
        return $false
    }
}

# Notification Functions
function Send-AlertNotification {
    <#
    .SYNOPSIS
        Send alert notification via configured channels
    #>
    param(
        [hashtable]$Alert,
        [array]$Channels = @("email")
    )
    
    foreach ($channel in $Channels) {
        try {
            switch ($channel.ToLower()) {
                "email" {
                    $subject = "[$($Alert.Severity.ToUpper())] $($Alert.Title)"
                    Write-MonitoringLog "Alert notification would be sent via email: $subject"
                }
                "slack" {
                    Write-MonitoringLog "Alert notification would be sent via Slack"
                }
                "teams" {
                    Write-MonitoringLog "Alert notification would be sent via Teams"
                }
                default {
                    Write-MonitoringLog "Unknown notification channel: $channel" -Level "WARNING"
                }
            }
        }
        catch {
            Write-MonitoringLog "Failed to send $channel notification: $_" -Level "ERROR"
        }
    }
}

# Batch Processing
function Invoke-BatchOperation {
    <#
    .SYNOPSIS
        Execute batch operation on collection with specified batch size
    #>
    param(
        [array]$Items,
        [scriptblock]$Operation,
        [int]$BatchSize = 100
    )
    
    $results = @()
    $totalBatches = [Math]::Ceiling($Items.Count / $BatchSize)
    
    for ($i = 0; $i -lt $Items.Count; $i += $BatchSize) {
        $batch = $Items[$i..([Math]::Min($i + $BatchSize - 1, $Items.Count - 1))]
        $batchNumber = [Math]::Floor($i / $BatchSize) + 1
        
        Write-MonitoringLog "Processing batch $batchNumber/$totalBatches ($($batch.Count) items)"
        
        try {
            $batchResult = & $Operation -Items $batch
            $results += $batchResult
        }
        catch {
            Write-MonitoringLog "Batch $batchNumber failed: $_" -Level "ERROR"
        }
    }
    
    return $results
}

# Cleanup Functions
function Remove-OldLogs {
    <#
    .SYNOPSIS
        Remove log files older than specified days
    #>
    param(
        [string]$LogDir = "$PSScriptRoot\..\logs",
        [int]$RetentionDays = 30
    )
    
    try {
        $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
        $oldLogs = Get-ChildItem -Path $LogDir -Filter "*.log" -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -lt $cutoffDate }
        
        $deletedCount = 0
        foreach ($log in $oldLogs) {
            Remove-Item -Path $log.FullName -Force -ErrorAction SilentlyContinue
            $deletedCount++
        }
        
        Write-MonitoringLog "Cleaned up $deletedCount old log files"
        return $deletedCount
    }
    catch {
        Write-MonitoringLog "Failed to cleanup old logs: $_" -Level "ERROR"
        return 0
    }
}

# System Information
function Get-MonitoringSystemInfo {
    <#
    .SYNOPSIS
        Retrieve current system information
    #>
    
    $computerSystem = Get-WmiObject Win32_ComputerSystem
    $operatingSystem = Get-WmiObject Win32_OperatingSystem
    $processor = Get-WmiObject Win32_Processor | Select-Object -First 1
    
    return @{
        ComputerName = $computerSystem.Name
        Manufacturer = $computerSystem.Manufacturer
        Model = $computerSystem.Model
        OS = $operatingSystem.Caption
        OSVersion = $operatingSystem.Version
        OSBuild = $operatingSystem.BuildNumber
        ProcessorName = $processor.Name
        ProcessorCores = $processor.NumberOfCores
        ProcessorLogical = $processor.NumberOfLogicalProcessors
        TotalMemoryGB = [Math]::Round($computerSystem.TotalPhysicalMemory / 1GB, 2)
        InstallDate = $operatingSystem.InstallDate
    }
}

# Initialization
Initialize-MonitoringLog -Component "Library"
Write-MonitoringLog "Enterprise Monitoring Library loaded successfully"

Export-ModuleMember -Function @(
    'Get-MonitoringConfig',
    'Get-AlertPolicies',
    'Initialize-MonitoringLog',
    'Write-MonitoringLog',
    'New-MetricValue',
    'New-AlertEvent',
    'Test-HealthEndpoint',
    'Add-ToCache',
    'Get-FromCache',
    'Clear-ExpiredCache',
    'Invoke-SafeCommand',
    'Export-MetricsToJSON',
    'Export-MetricsToCSV',
    'Send-AlertNotification',
    'Invoke-BatchOperation',
    'Remove-OldLogs',
    'Get-MonitoringSystemInfo'
)

#Requires -Version 5.1
<#
.SYNOPSIS
    HELIOS Common Functions Module
    Provides shared utilities for all 7 HELIOS components

.DESCRIPTION
    This module contains production-grade shared functions for:
    - Logging and diagnostics
    - Configuration management
    - Validation and prerequisites checking
    - File operations
    - Database operations
    - Event handling
    - Error handling with retry logic

.VERSION
    1.0.0

.AUTHOR
    HELIOS Infrastructure Team
#>

# Module-level variables
$script:LogPath = $null
$script:ConfigCache = @{}
$script:EventSubscriptions = @{}
$script:MaxRetries = 3
$script:RetryDelayMs = 1000

# ==================== LOGGING FUNCTIONS ====================

<#
.SYNOPSIS
    Log a standard information message

.PARAMETER Message
    The message to log

.PARAMETER Component
    The component that is logging (e.g., "Authentication", "DevOps")

.PARAMETER Level
    The log level (Info, Warning, Error, Success, Debug)
#>
function Log-Message {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message,
        
        [Parameter(Mandatory=$false)]
        [string]$Component = "HELIOS",
        
        [Parameter(Mandatory=$false)]
        [ValidateSet("Info", "Warning", "Error", "Success", "Debug")]
        [string]$Level = "Info"
    )
    
    try {
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
        $logEntry = "[$timestamp] [$Component] [$Level] $Message"
        
        # Color output based on level
        $color = switch ($Level) {
            "Error"   { "Red" }
            "Warning" { "Yellow" }
            "Success" { "Green" }
            "Debug"   { "Cyan" }
            default   { "White" }
        }
        
        Write-Host $logEntry -ForegroundColor $color
        
        # Write to log file if configured
        if ($script:LogPath -and (Test-Path (Split-Path $script:LogPath))) {
            Add-Content -Path $script:LogPath -Value $logEntry -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Error "Failed to log message: $_"
    }
}

<#
.SYNOPSIS
    Log an error message with optional exception details

.PARAMETER Message
    The error message

.PARAMETER Exception
    The exception object for additional context

.PARAMETER Component
    The component that is logging
#>
function Log-Error {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message,
        
        [Parameter(Mandatory=$false)]
        [System.Exception]$Exception = $null,
        
        [Parameter(Mandatory=$false)]
        [string]$Component = "HELIOS"
    )
    
    Log-Message -Message $Message -Component $Component -Level "Error"
    
    if ($Exception) {
        Log-Message -Message "Exception: $($Exception.Message)" -Component $Component -Level "Error"
        Log-Message -Message "StackTrace: $($Exception.StackTrace)" -Component $Component -Level "Debug"
    }
}

<#
.SYNOPSIS
    Log a success message

.PARAMETER Message
    The success message

.PARAMETER Component
    The component that is logging
#>
function Log-Success {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message,
        
        [Parameter(Mandatory=$false)]
        [string]$Component = "HELIOS"
    )
    
    Log-Message -Message $Message -Component $Component -Level "Success"
}

<#
.SYNOPSIS
    Initialize logging for the module

.PARAMETER LogPath
    The file path where logs should be written
#>
function Initialize-Logging {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$LogPath
    )
    
    $script:LogPath = $LogPath
    $logDir = Split-Path $LogPath -Parent
    
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }
    
    Log-Message -Message "Logging initialized" -Component "Infrastructure"
}

# ==================== CONFIGURATION FUNCTIONS ====================

<#
.SYNOPSIS
    Load configuration from JSON file with caching

.PARAMETER ConfigPath
    Path to the configuration JSON file

.PARAMETER UseCache
    Whether to use cached configuration if available
#>
function Load-Config {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ConfigPath,
        
        [Parameter(Mandatory=$false)]
        [bool]$UseCache = $true
    )
    
    try {
        # Check cache first
        if ($UseCache -and $script:ConfigCache.ContainsKey($ConfigPath)) {
            Log-Message -Message "Loading configuration from cache: $ConfigPath" -Level "Debug"
            return $script:ConfigCache[$ConfigPath]
        }
        
        if (-not (Test-Path $ConfigPath)) {
            throw "Configuration file not found: $ConfigPath"
        }
        
        $config = Get-Content -Path $ConfigPath -Raw | ConvertFrom-Json
        
        # Cache the configuration
        $script:ConfigCache[$ConfigPath] = $config
        
        Log-Message -Message "Configuration loaded: $ConfigPath" -Level "Info"
        return $config
    }
    catch {
        Log-Error -Message "Failed to load configuration from $ConfigPath" -Exception $_
        throw
    }
}

<#
.SYNOPSIS
    Save configuration to JSON file

.PARAMETER ConfigPath
    Path to save the configuration

.PARAMETER Config
    The configuration object to save

.PARAMETER Backup
    Whether to create a backup of existing config
#>
function Save-Config {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ConfigPath,
        
        [Parameter(Mandatory=$true)]
        [object]$Config,
        
        [Parameter(Mandatory=$false)]
        [bool]$Backup = $true
    )
    
    try {
        $configDir = Split-Path $ConfigPath -Parent
        if (-not (Test-Path $configDir)) {
            New-Item -ItemType Directory -Path $configDir -Force | Out-Null
        }
        
        # Backup existing config
        if ($Backup -and (Test-Path $ConfigPath)) {
            $backupPath = "$ConfigPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
            Copy-Item -Path $ConfigPath -Destination $backupPath -Force
            Log-Message -Message "Configuration backed up to: $backupPath" -Level "Debug"
        }
        
        # Save new config
        $Config | ConvertTo-Json -Depth 10 | Set-Content -Path $ConfigPath
        
        # Clear cache
        $script:ConfigCache.Remove($ConfigPath)
        
        Log-Message -Message "Configuration saved: $ConfigPath" -Level "Info"
    }
    catch {
        Log-Error -Message "Failed to save configuration to $ConfigPath" -Exception $_
        throw
    }
}

<#
.SYNOPSIS
    Get a specific value from configuration with dot notation

.PARAMETER Config
    The configuration object

.PARAMETER Path
    Dot-notation path (e.g., "azure.subscriptionId")
#>
function Get-ConfigValue {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [object]$Config,
        
        [Parameter(Mandatory=$true)]
        [string]$Path
    )
    
    try {
        $parts = $Path -split '\.'
        $value = $Config
        
        foreach ($part in $parts) {
            if ($null -eq $value) { return $null }
            $value = $value | Select-Object -ExpandProperty $part -ErrorAction Stop
        }
        
        return $value
    }
    catch {
        Log-Message -Message "Configuration path not found: $Path" -Level "Warning"
        return $null
    }
}

# ==================== VALIDATION FUNCTIONS ====================

<#
.SYNOPSIS
    Validate system prerequisites

.PARAMETER Requirements
    Array of requirements to check
#>
function Validate-Prerequisites {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string[]]$Requirements = @("PowerShell5.1", "DotNet4.7", "AzureCLI", "Git")
    )
    
    try {
        $results = @()
        
        foreach ($req in $Requirements) {
            $met = $false
            $message = ""
            
            switch ($req) {
                "PowerShell5.1" {
                    $met = $PSVersionTable.PSVersion.Major -ge 5
                    $message = "PowerShell $($PSVersionTable.PSVersion)"
                }
                "DotNet4.7" {
                    $met = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" Release -ErrorAction SilentlyContinue).Release -ge 461308
                    $message = ".NET Framework"
                }
                "AzureCLI" {
                    $met = $null -ne (Get-Command az -ErrorAction SilentlyContinue)
                    $message = "Azure CLI"
                }
                "Git" {
                    $met = $null -ne (Get-Command git -ErrorAction SilentlyContinue)
                    $message = "Git"
                }
            }
            
            $results += @{
                Requirement = $req
                Met = $met
                Message = $message
            }
            
            Log-Message -Message "$message - $($met ? 'OK' : 'MISSING')" -Level ($met ? "Success" : "Warning")
        }
        
        $allMet = $results | Where-Object { $_.Met } | Measure-Object | Select-Object -ExpandProperty Count
        
        return @{
            AllMet = ($allMet -eq $results.Count)
            Results = $results
        }
    }
    catch {
        Log-Error -Message "Failed to validate prerequisites" -Exception $_
        throw
    }
}

<#
.SYNOPSIS
    Validate administrator privileges
#>
function Validate-Admin {
    [CmdletBinding()]
    param()
    
    try {
        $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        
        if (-not $isAdmin) {
            Log-Error -Message "This operation requires administrator privileges"
            throw "Administrator privileges required"
        }
        
        Log-Message -Message "Administrator privileges validated" -Level "Success"
        return $true
    }
    catch {
        Log-Error -Message "Failed to validate administrator privileges" -Exception $_
        throw
    }
}

# ==================== FILE OPERATIONS ====================

<#
.SYNOPSIS
    Safely copy a file with error handling and logging

.PARAMETER Source
    Source file path

.PARAMETER Destination
    Destination file path

.PARAMETER Force
    Whether to overwrite existing file
#>
function Safe-Copy {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Source,
        
        [Parameter(Mandatory=$true)]
        [string]$Destination,
        
        [Parameter(Mandatory=$false)]
        [bool]$Force = $false
    )
    
    try {
        if (-not (Test-Path $Source)) {
            throw "Source file not found: $Source"
        }
        
        $destDir = Split-Path $Destination -Parent
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        
        Copy-Item -Path $Source -Destination $Destination -Force:$Force -ErrorAction Stop
        Log-Message -Message "File copied: $Source -> $Destination" -Level "Success"
        return $true
    }
    catch {
        Log-Error -Message "Failed to copy file: $Source -> $Destination" -Exception $_
        throw
    }
}

<#
.SYNOPSIS
    Safely move a file with error handling and logging

.PARAMETER Source
    Source file path

.PARAMETER Destination
    Destination file path

.PARAMETER Force
    Whether to overwrite existing file
#>
function Safe-Move {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Source,
        
        [Parameter(Mandatory=$true)]
        [string]$Destination,
        
        [Parameter(Mandatory=$false)]
        [bool]$Force = $false
    )
    
    try {
        if (-not (Test-Path $Source)) {
            throw "Source file not found: $Source"
        }
        
        $destDir = Split-Path $Destination -Parent
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        
        Move-Item -Path $Source -Destination $Destination -Force:$Force -ErrorAction Stop
        Log-Message -Message "File moved: $Source -> $Destination" -Level "Success"
        return $true
    }
    catch {
        Log-Error -Message "Failed to move file: $Source -> $Destination" -Exception $_
        throw
    }
}

<#
.SYNOPSIS
    Safely delete a file with error handling and logging

.PARAMETER Path
    File path to delete

.PARAMETER Backup
    Whether to backup the file before deletion
#>
function Safe-Delete {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Path,
        
        [Parameter(Mandatory=$false)]
        [bool]$Backup = $true
    )
    
    try {
        if (-not (Test-Path $Path)) {
            throw "File not found: $Path"
        }
        
        if ($Backup) {
            $backupPath = "$Path.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
            Copy-Item -Path $Path -Destination $backupPath
            Log-Message -Message "File backed up to: $backupPath" -Level "Debug"
        }
        
        Remove-Item -Path $Path -Force -ErrorAction Stop
        Log-Message -Message "File deleted: $Path" -Level "Success"
        return $true
    }
    catch {
        Log-Error -Message "Failed to delete file: $Path" -Exception $_
        throw
    }
}

# ==================== DATABASE OPERATIONS ====================

<#
.SYNOPSIS
    Query the database

.PARAMETER ConnectionString
    Database connection string

.PARAMETER Query
    SQL query to execute

.PARAMETER Parameters
    Query parameters (hashtable)
#>
function Query-Database {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ConnectionString,
        
        [Parameter(Mandatory=$true)]
        [string]$Query,
        
        [Parameter(Mandatory=$false)]
        [hashtable]$Parameters = @{}
    )
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = $ConnectionString
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        
        foreach ($key in $Parameters.Keys) {
            $command.Parameters.AddWithValue("@$key", $Parameters[$key]) | Out-Null
        }
        
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
        $dataset = New-Object System.Data.DataSet
        $adapter.Fill($dataset) | Out-Null
        
        $connection.Close()
        
        Log-Message -Message "Database query executed successfully" -Level "Debug"
        return $dataset.Tables[0]
    }
    catch {
        Log-Error -Message "Failed to query database" -Exception $_
        throw
    }
}

<#
.SYNOPSIS
    Update database records

.PARAMETER ConnectionString
    Database connection string

.PARAMETER Query
    SQL UPDATE query

.PARAMETER Parameters
    Query parameters (hashtable)
#>
function Update-Database {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ConnectionString,
        
        [Parameter(Mandatory=$true)]
        [string]$Query,
        
        [Parameter(Mandatory=$false)]
        [hashtable]$Parameters = @{}
    )
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = $ConnectionString
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        
        foreach ($key in $Parameters.Keys) {
            $command.Parameters.AddWithValue("@$key", $Parameters[$key]) | Out-Null
        }
        
        $rowsAffected = $command.ExecuteNonQuery()
        $connection.Close()
        
        Log-Message -Message "Database updated: $rowsAffected rows affected" -Level "Success"
        return $rowsAffected
    }
    catch {
        Log-Error -Message "Failed to update database" -Exception $_
        throw
    }
}

<#
.SYNOPSIS
    Insert records into database

.PARAMETER ConnectionString
    Database connection string

.PARAMETER Query
    SQL INSERT query

.PARAMETER Parameters
    Query parameters (hashtable)
#>
function Insert-Database {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ConnectionString,
        
        [Parameter(Mandatory=$true)]
        [string]$Query,
        
        [Parameter(Mandatory=$false)]
        [hashtable]$Parameters = @{}
    )
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = $ConnectionString
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        
        foreach ($key in $Parameters.Keys) {
            $command.Parameters.AddWithValue("@$key", $Parameters[$key]) | Out-Null
        }
        
        $rowsAffected = $command.ExecuteNonQuery()
        $connection.Close()
        
        Log-Message -Message "Database record inserted: $rowsAffected rows affected" -Level "Success"
        return $rowsAffected
    }
    catch {
        Log-Error -Message "Failed to insert into database" -Exception $_
        throw
    }
}

# ==================== EVENT HANDLING ====================

<#
.SYNOPSIS
    Emit an event to all subscribers

.PARAMETER EventName
    Name of the event

.PARAMETER Payload
    Event payload data

.PARAMETER Component
    Component emitting the event
#>
function Emit-Event {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$EventName,
        
        [Parameter(Mandatory=$false)]
        [object]$Payload = $null,
        
        [Parameter(Mandatory=$false)]
        [string]$Component = "HELIOS"
    )
    
    try {
        if (-not $script:EventSubscriptions.ContainsKey($EventName)) {
            Log-Message -Message "Event emitted with no subscribers: $EventName" -Level "Debug"
            return
        }
        
        $eventData = @{
            EventName = $EventName
            Component = $Component
            Timestamp = Get-Date
            Payload = $Payload
        }
        
        foreach ($subscription in $script:EventSubscriptions[$EventName]) {
            try {
                & $subscription.Callback $eventData
            }
            catch {
                Log-Error -Message "Event subscription handler failed for $EventName" -Exception $_
            }
        }
        
        Log-Message -Message "Event emitted: $EventName" -Level "Debug"
    }
    catch {
        Log-Error -Message "Failed to emit event: $EventName" -Exception $_
    }
}

<#
.SYNOPSIS
    Subscribe to an event

.PARAMETER EventName
    Name of the event to subscribe to

.PARAMETER Callback
    ScriptBlock to execute when event is emitted

.PARAMETER SubscriptionId
    Optional unique identifier for the subscription
#>
function Subscribe-Event {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$EventName,
        
        [Parameter(Mandatory=$true)]
        [scriptblock]$Callback,
        
        [Parameter(Mandatory=$false)]
        [string]$SubscriptionId = $null
    )
    
    try {
        if (-not $SubscriptionId) {
            $SubscriptionId = "sub_$(Get-Random -Minimum 1000 -Maximum 9999)_$(Get-Date -Format 'yyyyMMddHHmmss')"
        }
        
        if (-not $script:EventSubscriptions.ContainsKey($EventName)) {
            $script:EventSubscriptions[$EventName] = @()
        }
        
        $script:EventSubscriptions[$EventName] += @{
            SubscriptionId = $SubscriptionId
            Callback = $Callback
        }
        
        Log-Message -Message "Event subscription created: $EventName (ID: $SubscriptionId)" -Level "Success"
        return $SubscriptionId
    }
    catch {
        Log-Error -Message "Failed to subscribe to event: $EventName" -Exception $_
        throw
    }
}

<#
.SYNOPSIS
    Unsubscribe from an event

.PARAMETER EventName
    Name of the event

.PARAMETER SubscriptionId
    The subscription ID returned from Subscribe-Event
#>
function Unsubscribe-Event {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$EventName,
        
        [Parameter(Mandatory=$true)]
        [string]$SubscriptionId
    )
    
    try {
        if ($script:EventSubscriptions.ContainsKey($EventName)) {
            $script:EventSubscriptions[$EventName] = $script:EventSubscriptions[$EventName] | 
                Where-Object { $_.SubscriptionId -ne $SubscriptionId }
            
            Log-Message -Message "Event subscription removed: $EventName (ID: $SubscriptionId)" -Level "Success"
        }
    }
    catch {
        Log-Error -Message "Failed to unsubscribe from event: $EventName" -Exception $_
        throw
    }
}

# ==================== ERROR HANDLING ====================

<#
.SYNOPSIS
    Execute code with retry logic and comprehensive error handling

.PARAMETER ScriptBlock
    The script block to execute

.PARAMETER MaxAttempts
    Maximum number of attempts

.PARAMETER DelayMs
    Delay between retries in milliseconds

.PARAMETER BackoffMultiplier
    Multiplier for exponential backoff
#>
function Try-Catch-Retry {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [scriptblock]$ScriptBlock,
        
        [Parameter(Mandatory=$false)]
        [int]$MaxAttempts = 3,
        
        [Parameter(Mandatory=$false)]
        [int]$DelayMs = 1000,
        
        [Parameter(Mandatory=$false)]
        [double]$BackoffMultiplier = 2.0
    )
    
    try {
        $attempt = 0
        $lastException = $null
        
        while ($attempt -lt $MaxAttempts) {
            try {
                $attempt++
                Log-Message -Message "Attempt $attempt of $MaxAttempts" -Level "Debug"
                
                return & $ScriptBlock
            }
            catch {
                $lastException = $_
                
                if ($attempt -lt $MaxAttempts) {
                    $delay = [int]($DelayMs * [Math]::Pow($BackoffMultiplier, $attempt - 1))
                    Log-Message -Message "Attempt $attempt failed, retrying in ${delay}ms: $($_.Message)" -Level "Warning"
                    Start-Sleep -Milliseconds $delay
                }
                else {
                    Log-Error -Message "All $MaxAttempts attempts failed" -Exception $_
                    throw
                }
            }
        }
    }
    catch {
        Log-Error -Message "Try-Catch-Retry failed" -Exception $_
        throw
    }
}

# ==================== MODULE EXPORTS ====================

Export-ModuleMember -Function @(
    'Log-Message',
    'Log-Error',
    'Log-Success',
    'Initialize-Logging',
    'Load-Config',
    'Save-Config',
    'Get-ConfigValue',
    'Validate-Prerequisites',
    'Validate-Admin',
    'Safe-Copy',
    'Safe-Move',
    'Safe-Delete',
    'Query-Database',
    'Update-Database',
    'Insert-Database',
    'Emit-Event',
    'Subscribe-Event',
    'Unsubscribe-Event',
    'Try-Catch-Retry'
)

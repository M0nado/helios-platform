#Requires -Version 5.1
<#
.SYNOPSIS
    HELIOS API Gateway
    Unified interface for cross-component communication

.DESCRIPTION
    Provides:
    - Single entry point for component communication
    - Asynchronous request handling
    - Request/response caching
    - Request logging and monitoring
    - Error handling and fallback routing
    - Component discovery and routing

.VERSION
    1.0.0

.AUTHOR
    HELIOS Infrastructure Team
#>

# Import common functions
$commonFunctionsPath = Join-Path $PSScriptRoot "common-functions.psm1"
if (Test-Path $commonFunctionsPath) {
    Import-Module $commonFunctionsPath -Force
}

# Module-level variables
$script:RequestCache = @{}
$script:CacheTTL = 300  # 5 minutes in seconds
$script:ComponentRegistry = @{}
$script:RequestLog = @()
$script:MaxRequestLogSize = 1000
$script:AsyncJobs = @{}

<#
.SYNOPSIS
    Register a component with the API gateway

.PARAMETER ComponentName
    Name of the component

.PARAMETER Handler
    ScriptBlock that handles requests for this component

.PARAMETER Capabilities
    Array of capabilities provided by the component
#>
function Register-Component {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ComponentName,
        
        [Parameter(Mandatory=$true)]
        [scriptblock]$Handler,
        
        [Parameter(Mandatory=$false)]
        [string[]]$Capabilities = @()
    )
    
    try {
        $script:ComponentRegistry[$ComponentName] = @{
            Handler = $Handler
            Capabilities = $Capabilities
            Registered = Get-Date
            RequestCount = 0
            ErrorCount = 0
        }
        
        Log-Message -Message "Component registered: $ComponentName with $($Capabilities.Count) capabilities" -Component "APIGateway" -Level "Success"
    }
    catch {
        Log-Error -Message "Failed to register component: $ComponentName" -Exception $_ -Component "APIGateway"
        throw
    }
}

<#
.SYNOPSIS
    Invoke a component API with optional caching and async support

.PARAMETER ComponentName
    Name of the target component

.PARAMETER Operation
    Operation/function to invoke

.PARAMETER Parameters
    Operation parameters (hashtable)

.PARAMETER UseCache
    Whether to use cached results

.PARAMETER Async
    Whether to execute asynchronously
#>
function Invoke-ComponentAPI {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ComponentName,
        
        [Parameter(Mandatory=$true)]
        [string]$Operation,
        
        [Parameter(Mandatory=$false)]
        [hashtable]$Parameters = @{},
        
        [Parameter(Mandatory=$false)]
        [bool]$UseCache = $true,
        
        [Parameter(Mandatory=$false)]
        [bool]$Async = $false,
        
        [Parameter(Mandatory=$false)]
        [string]$RequestId = $null
    )
    
    try {
        if (-not $RequestId) {
            $RequestId = "req_$(Get-Random -Minimum 10000 -Maximum 99999)_$(Get-Date -Format 'yyyyMMddHHmmss')"
        }
        
        # Check cache
        $cacheKey = "$ComponentName/$Operation/$($Parameters | ConvertTo-Json)"
        if ($UseCache -and $script:RequestCache.ContainsKey($cacheKey)) {
            $cachedResult = $script:RequestCache[$cacheKey]
            if ((Get-Date) - $cachedResult.Timestamp -lt (New-TimeSpan -Seconds $script:CacheTTL)) {
                Log-Message -Message "Cache HIT: $ComponentName.$Operation (Request: $RequestId)" -Component "APIGateway" -Level "Debug"
                return $cachedResult.Result
            }
            else {
                $script:RequestCache.Remove($cacheKey)
            }
        }
        
        # Log request
        Log-RequestDetails -ComponentName $ComponentName -Operation $Operation -RequestId $RequestId -Status "INITIATED"
        
        # Check component exists
        if (-not $script:ComponentRegistry.ContainsKey($ComponentName)) {
            throw "Component not found: $ComponentName"
        }
        
        $component = $script:ComponentRegistry[$ComponentName]
        $component.RequestCount++
        
        if ($Async) {
            # Start async job
            $jobScriptBlock = {
                param($Handler, $Operation, $Parameters)
                & $Handler -Operation $Operation -Parameters $Parameters
            }
            
            $job = Start-Job -ScriptBlock $jobScriptBlock -ArgumentList $component.Handler, $Operation, $Parameters
            $script:AsyncJobs[$RequestId] = $job
            
            Log-Message -Message "Async request queued: $ComponentName.$Operation (Job: $($job.Id))" -Component "APIGateway" -Level "Info"
            
            return @{
                RequestId = $RequestId
                JobId = $job.Id
                Status = "QUEUED"
                Timestamp = Get-Date
            }
        }
        else {
            # Synchronous execution
            $result = & $component.Handler -Operation $Operation -Parameters $Parameters
            
            # Cache result
            $script:RequestCache[$cacheKey] = @{
                Result = $result
                Timestamp = Get-Date
            }
            
            Log-RequestDetails -ComponentName $ComponentName -Operation $Operation -RequestId $RequestId -Status "COMPLETED"
            
            return $result
        }
    }
    catch {
        Log-Error -Message "Component API invocation failed: $ComponentName.$Operation" -Exception $_ -Component "APIGateway"
        
        if ($script:ComponentRegistry.ContainsKey($ComponentName)) {
            $script:ComponentRegistry[$ComponentName].ErrorCount++
        }
        
        Log-RequestDetails -ComponentName $ComponentName -Operation $Operation -RequestId $RequestId -Status "FAILED" -ErrorMessage $_.Message
        throw
    }
}

<#
.SYNOPSIS
    Query a component with specific criteria

.PARAMETER ComponentName
    Name of the component

.PARAMETER Query
    Query parameters (hashtable)

.PARAMETER Timeout
    Request timeout in seconds
#>
function Query-Component {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ComponentName,
        
        [Parameter(Mandatory=$true)]
        [hashtable]$Query,
        
        [Parameter(Mandatory=$false)]
        [int]$Timeout = 30
    )
    
    try {
        $requestId = "query_$(Get-Random -Minimum 10000 -Maximum 99999)_$(Get-Date -Format 'yyyyMMddHHmmss')"
        
        Log-Message -Message "Query initiated: $ComponentName (Request: $requestId)" -Component "APIGateway" -Level "Info"
        
        $result = Invoke-ComponentAPI -ComponentName $ComponentName `
                                     -Operation "Query" `
                                     -Parameters $Query `
                                     -UseCache $true `
                                     -RequestId $requestId
        
        Log-Message -Message "Query completed: $ComponentName" -Component "APIGateway" -Level "Success"
        return $result
    }
    catch {
        Log-Error -Message "Component query failed: $ComponentName" -Exception $_ -Component "APIGateway"
        throw
    }
}

<#
.SYNOPSIS
    Trigger an action in a component

.PARAMETER ComponentName
    Name of the component

.PARAMETER Action
    Action to trigger

.PARAMETER Parameters
    Action parameters (hashtable)

.PARAMETER WaitForCompletion
    Whether to wait for async completion
#>
function Trigger-Component {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ComponentName,
        
        [Parameter(Mandatory=$true)]
        [string]$Action,
        
        [Parameter(Mandatory=$false)]
        [hashtable]$Parameters = @{},
        
        [Parameter(Mandatory=$false)]
        [bool]$WaitForCompletion = $false
    )
    
    try {
        $requestId = "trigger_$(Get-Random -Minimum 10000 -Maximum 99999)_$(Get-Date -Format 'yyyyMMddHHmmss')"
        
        Log-Message -Message "Trigger initiated: $ComponentName.$Action (Request: $requestId)" -Component "APIGateway" -Level "Info"
        
        $result = Invoke-ComponentAPI -ComponentName $ComponentName `
                                     -Operation $Action `
                                     -Parameters $Parameters `
                                     -Async (-not $WaitForCompletion) `
                                     -RequestId $requestId
        
        if ($WaitForCompletion -and $result.JobId) {
            $job = Get-Job -Id $result.JobId -ErrorAction SilentlyContinue
            if ($job) {
                $job | Wait-Job | Out-Null
                $result = Receive-Job -Job $job
            }
        }
        
        Log-Message -Message "Trigger completed: $ComponentName.$Action" -Component "APIGateway" -Level "Success"
        return $result
    }
    catch {
        Log-Error -Message "Component trigger failed: $ComponentName.$Action" -Exception $_ -Component "APIGateway"
        throw
    }
}

<#
.SYNOPSIS
    Get the status of an async request

.PARAMETER RequestId
    The request ID returned from async invocation
#>
function Get-RequestStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$RequestId
    )
    
    try {
        if (-not $script:AsyncJobs.ContainsKey($RequestId)) {
            return @{
                RequestId = $RequestId
                Status = "NOT_FOUND"
            }
        }
        
        $job = $script:AsyncJobs[$RequestId]
        $jobState = $job.State
        
        $status = switch ($jobState) {
            "Running" { "IN_PROGRESS" }
            "Completed" { "COMPLETED" }
            "Failed" { "FAILED" }
            "Stopped" { "CANCELLED" }
            default { $jobState }
        }
        
        $result = @{
            RequestId = $RequestId
            Status = $status
            JobId = $job.Id
        }
        
        if ($status -eq "COMPLETED" -or $status -eq "FAILED") {
            $result.Result = Receive-Job -Job $job
            Remove-Job -Job $job -Force
            $script:AsyncJobs.Remove($RequestId)
        }
        
        return $result
    }
    catch {
        Log-Error -Message "Failed to get request status: $RequestId" -Exception $_ -Component "APIGateway"
        throw
    }
}

<#
.SYNOPSIS
    Get information about registered components

.PARAMETER ComponentName
    Optional: specific component to query
#>
function Get-ComponentInfo {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string]$ComponentName = $null
    )
    
    try {
        if ($ComponentName) {
            if (-not $script:ComponentRegistry.ContainsKey($ComponentName)) {
                throw "Component not found: $ComponentName"
            }
            
            $component = $script:ComponentRegistry[$ComponentName]
            return @{
                ComponentName = $ComponentName
                Capabilities = $component.Capabilities
                Registered = $component.Registered
                RequestCount = $component.RequestCount
                ErrorCount = $component.ErrorCount
                Status = "ACTIVE"
            }
        }
        else {
            $info = @()
            foreach ($name in $script:ComponentRegistry.Keys) {
                $component = $script:ComponentRegistry[$name]
                $info += @{
                    ComponentName = $name
                    Capabilities = $component.Capabilities
                    RequestCount = $component.RequestCount
                    ErrorCount = $component.ErrorCount
                }
            }
            return $info
        }
    }
    catch {
        Log-Error -Message "Failed to get component info" -Exception $_ -Component "APIGateway"
        throw
    }
}

<#
.SYNOPSIS
    Clear the request cache

.PARAMETER ComponentName
    Optional: clear cache for specific component only
#>
function Clear-RequestCache {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string]$ComponentName = $null
    )
    
    try {
        if ($ComponentName) {
            $keysToRemove = $script:RequestCache.Keys | Where-Object { $_ -like "$ComponentName/*" }
            foreach ($key in $keysToRemove) {
                $script:RequestCache.Remove($key)
            }
            Log-Message -Message "Cache cleared for component: $ComponentName" -Component "APIGateway" -Level "Info"
        }
        else {
            $script:RequestCache.Clear()
            Log-Message -Message "Request cache cleared" -Component "APIGateway" -Level "Info"
        }
    }
    catch {
        Log-Error -Message "Failed to clear cache" -Exception $_ -Component "APIGateway"
        throw
    }
}

<#
.SYNOPSIS
    Log request details for monitoring

.PARAMETER ComponentName
    Component name

.PARAMETER Operation
    Operation name

.PARAMETER RequestId
    Unique request ID

.PARAMETER Status
    Request status

.PARAMETER ErrorMessage
    Optional error message
#>
function Log-RequestDetails {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$ComponentName,
        
        [Parameter(Mandatory=$true)]
        [string]$Operation,
        
        [Parameter(Mandatory=$true)]
        [string]$RequestId,
        
        [Parameter(Mandatory=$true)]
        [string]$Status,
        
        [Parameter(Mandatory=$false)]
        [string]$ErrorMessage = $null
    )
    
    try {
        $logEntry = @{
            RequestId = $RequestId
            ComponentName = $ComponentName
            Operation = $Operation
            Status = $Status
            Timestamp = Get-Date
            ErrorMessage = $ErrorMessage
        }
        
        $script:RequestLog += $logEntry
        
        # Trim log if too large
        if ($script:RequestLog.Count -gt $script:MaxRequestLogSize) {
            $script:RequestLog = $script:RequestLog[-$script:MaxRequestLogSize..-1]
        }
    }
    catch {
        # Silently fail to avoid recursion
    }
}

<#
.SYNOPSIS
    Get request log entries

.PARAMETER ComponentName
    Optional: filter by component

.PARAMETER Status
    Optional: filter by status

.PARAMETER Hours
    Look back this many hours
#>
function Get-RequestLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string]$ComponentName = $null,
        
        [Parameter(Mandatory=$false)]
        [string]$Status = $null,
        
        [Parameter(Mandatory=$false)]
        [int]$Hours = 1
    )
    
    try {
        $cutoffTime = (Get-Date).AddHours(-$Hours)
        
        $log = $script:RequestLog | Where-Object {
            $_.Timestamp -gt $cutoffTime -and
            ($null -eq $ComponentName -or $_.ComponentName -eq $ComponentName) -and
            ($null -eq $Status -or $_.Status -eq $Status)
        }
        
        return $log
    }
    catch {
        Log-Error -Message "Failed to retrieve request log" -Exception $_ -Component "APIGateway"
        throw
    }
}

<#
.SYNOPSIS
    Get API gateway statistics

.PARAMETER ComponentName
    Optional: get stats for specific component
#>
function Get-GatewayStats {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string]$ComponentName = $null
    )
    
    try {
        $stats = @{
            TotalComponents = $script:ComponentRegistry.Count
            TotalRequests = 0
            TotalErrors = 0
            CachedItems = $script:RequestCache.Count
            PendingJobs = $script:AsyncJobs.Count
            RequestLogSize = $script:RequestLog.Count
        }
        
        foreach ($component in $script:ComponentRegistry.Values) {
            $stats.TotalRequests += $component.RequestCount
            $stats.TotalErrors += $component.ErrorCount
        }
        
        return $stats
    }
    catch {
        Log-Error -Message "Failed to get gateway statistics" -Exception $_ -Component "APIGateway"
        throw
    }
}

# Export public functions
Export-ModuleMember -Function @(
    'Register-Component',
    'Invoke-ComponentAPI',
    'Query-Component',
    'Trigger-Component',
    'Get-RequestStatus',
    'Get-ComponentInfo',
    'Clear-RequestCache',
    'Get-RequestLog',
    'Get-GatewayStats'
)

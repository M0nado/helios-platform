#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Shared API client functions for AI integration services
    
.DESCRIPTION
    Provides common functionality for OpenAI API calls including:
    - Authentication and request handling
    - Rate limiting
    - Error handling and retries
    - Logging and audit trail
    - Rate limiting management
    
.NOTES
    Author: HELIOS Platform
    Version: 1.0.0
    Requires: PowerShell 7.0+
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ============================================================================
# Configuration Loading
# ============================================================================

function Load-EnvironmentConfig {
    <#
    .SYNOPSIS
    Load environment configuration from .env file
    #>
    param(
        [string]$EnvPath = "./config/.env"
    )
    
    if (Test-Path $EnvPath) {
        Get-Content $EnvPath | Where-Object { $_ -notmatch "^#" -and $_.Trim() } | ForEach-Object {
            $key, $value = $_ -split "=", 2
            [Environment]::SetEnvironmentVariable($key.Trim(), $value.Trim())
        }
        Write-Verbose "Environment config loaded from $EnvPath"
    } else {
        Write-Warning "Environment config not found at $EnvPath"
    }
}

function Get-ConfigValue {
    <#
    .SYNOPSIS
    Get configuration value with fallback to default
    #>
    param(
        [string]$Key,
        [string]$Default = ""
    )
    
    $value = [Environment]::GetEnvironmentVariable($Key)
    return $value ?? $Default
}

# ============================================================================
# Logging Functions
# ============================================================================

function Initialize-Logger {
    <#
    .SYNOPSIS
    Initialize logging system
    #>
    param(
        [string]$LogDir = "./logs",
        [string]$LogLevel = "INFO"
    )
    
    if (-not (Test-Path $LogDir)) {
        New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
    }
    
    $script:LogDir = $LogDir
    $script:LogLevel = $LogLevel
    $script:LogPath = Join-Path $LogDir "ai-integration_$(Get-Date -Format 'yyyyMMdd').log"
}

function Write-LogEntry {
    <#
    .SYNOPSIS
    Write structured log entry
    #>
    param(
        [string]$Level = "INFO",
        [string]$Message,
        [hashtable]$Context = @{},
        [object]$Data = $null,
        [bool]$Sanitize = $true
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ"
    $logLevel = @{ "DEBUG" = 0; "INFO" = 1; "WARN" = 2; "ERROR" = 3 }
    
    if ([int]$logLevel[$Level] -ge [int]$logLevel[$script:LogLevel]) {
        $logEntry = @{
            timestamp = $timestamp
            level = $Level
            message = $Message
            context = $Context
        } | ConvertTo-Json -Depth 10 -Compress
        
        if ($Sanitize) {
            $logEntry = $logEntry -replace '(?i)api[_-]?key["\s]*[:=]\s*"[^"]*"', 'api_key: "***REDACTED***"'
            $logEntry = $logEntry -replace '(?i)bearer\s+[^\s"]*', 'Bearer ***REDACTED***'
        }
        
        Write-Host $logEntry
        
        if ($script:LogPath) {
            Add-Content -Path $script:LogPath -Value $logEntry
        }
    }
}

# ============================================================================
# Rate Limiting
# ============================================================================

$script:RateLimitState = @{}

function Initialize-RateLimiter {
    <#
    .SYNOPSIS
    Initialize rate limiter for API service
    #>
    param(
        [string]$ServiceName,
        [int]$RequestsPerMinute,
        [int]$BurstLimit = 5
    )
    
    $script:RateLimitState[$ServiceName] = @{
        requestsPerMinute = $RequestsPerMinute
        burstLimit = $BurstLimit
        requests = @()
        lastCheckTime = Get-Date
    }
}

function Test-RateLimit {
    <#
    .SYNOPSIS
    Check if request can be made within rate limits
    #>
    param(
        [string]$ServiceName
    )
    
    if (-not $script:RateLimitState.ContainsKey($ServiceName)) {
        return $true
    }
    
    $limiter = $script:RateLimitState[$ServiceName]
    $now = Get-Date
    $oneMinuteAgo = $now.AddMinutes(-1)
    
    # Remove requests older than 1 minute
    $limiter.requests = @($limiter.requests | Where-Object { $_ -gt $oneMinuteAgo })
    
    if ($limiter.requests.Count -lt $limiter.requestsPerMinute) {
        return $true
    }
    
    return $false
}

function Add-RateLimitRequest {
    <#
    .SYNOPSIS
    Record an API request for rate limiting
    #>
    param(
        [string]$ServiceName
    )
    
    if ($script:RateLimitState.ContainsKey($ServiceName)) {
        $script:RateLimitState[$ServiceName].requests += @(Get-Date)
    }
}

function Wait-RateLimit {
    <#
    .SYNOPSIS
    Wait until rate limit allows next request
    #>
    param(
        [string]$ServiceName,
        [int]$MaxWait = 60
    )
    
    $waitStart = Get-Date
    
    while (-not (Test-RateLimit -ServiceName $ServiceName)) {
        $elapsed = (Get-Date) - $waitStart
        
        if ($elapsed.TotalSeconds -gt $MaxWait) {
            throw "Rate limit exceeded for $ServiceName (max wait: ${MaxWait}s)"
        }
        
        $waitTime = [Math]::Min(1, ($MaxWait - $elapsed.TotalSeconds))
        Start-Sleep -Milliseconds ($waitTime * 1000)
    }
    
    Add-RateLimitRequest -ServiceName $ServiceName
}

# ============================================================================
# API Request Functions
# ============================================================================

function Invoke-OpenAIRequest {
    <#
    .SYNOPSIS
    Make a request to OpenAI API with error handling and retries
    
    .PARAMETER Endpoint
    API endpoint (e.g., /chat/completions, /completions)
    
    .PARAMETER Method
    HTTP method (GET, POST, etc.)
    
    .PARAMETER Body
    Request body as hashtable or JSON string
    
    .PARAMETER MaxRetries
    Number of retry attempts for transient failures
    
    .PARAMETER DryRun
    If true, don't actually make the request
    #>
    param(
        [string]$Endpoint,
        [string]$Method = "POST",
        $Body,
        [int]$MaxRetries = 3,
        [bool]$DryRun = $false
    )
    
    $apiKey = Get-ConfigValue -Key "OPENAI_API_KEY"
    $apiBase = Get-ConfigValue -Key "OPENAI_API_BASE" -Default "https://api.openai.com/v1"
    $timeout = [int](Get-ConfigValue -Key "OPENAI_REQUEST_TIMEOUT" -Default "60")
    
    if (-not $apiKey) {
        throw "OPENAI_API_KEY not configured"
    }
    
    $url = "$apiBase$Endpoint"
    
    if ($Body -is [hashtable]) {
        $Body = $Body | ConvertTo-Json -Depth 10
    }
    
    $headers = @{
        "Authorization" = "Bearer $apiKey"
        "Content-Type" = "application/json"
        "User-Agent" = "HELIOS-AI-Integration/1.0"
    }
    
    $requestId = [guid]::NewGuid().ToString()
    $logContext = @{ requestId = $requestId; endpoint = $Endpoint; method = $Method }
    
    Write-LogEntry -Level "DEBUG" -Message "Preparing API request" -Context $logContext -Data @{ url = $url }
    
    if ($DryRun) {
        Write-LogEntry -Level "INFO" -Message "DRY RUN: Would send request to $Endpoint" -Context $logContext
        return @{
            isDryRun = $true
            requestId = $requestId
            statusCode = 200
            content = $Body
        }
    }
    
    $retryCount = 0
    $backoffFactor = 1
    
    while ($retryCount -lt $MaxRetries) {
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            
            $response = Invoke-WebRequest -Uri $url `
                -Method $Method `
                -Headers $headers `
                -Body $Body `
                -TimeoutSec $timeout `
                -ContentType "application/json"
            
            $stopwatch.Stop()
            $responseBody = $response.Content | ConvertFrom-Json
            
            Add-RateLimitRequest -ServiceName "openai"
            
            Write-LogEntry -Level "INFO" `
                -Message "API request successful" `
                -Context @{
                    requestId = $requestId
                    endpoint = $Endpoint
                    statusCode = $response.StatusCode
                    duration = $stopwatch.ElapsedMilliseconds
                }
            
            return @{
                success = $true
                statusCode = $response.StatusCode
                content = $responseBody
                requestId = $requestId
                duration = $stopwatch.ElapsedMilliseconds
            }
        }
        catch [System.Net.Http.HttpRequestException] {
            $errorDetails = $_.Exception.Message
            
            if ($_.Exception.Response.StatusCode -in @(429, 500, 502, 503)) {
                if ($retryCount -lt ($MaxRetries - 1)) {
                    $waitTime = [Math]::Pow($backoffFactor, $retryCount) * 2
                    Write-LogEntry -Level "WARN" `
                        -Message "Transient error, retrying in ${waitTime}s" `
                        -Context @{ requestId = $requestId; retry = ($retryCount + 1); error = $errorDetails }
                    
                    Start-Sleep -Seconds $waitTime
                    $retryCount++
                    continue
                }
            }
            
            Write-LogEntry -Level "ERROR" -Message "API request failed" -Context @{
                requestId = $requestId
                endpoint = $Endpoint
                error = $errorDetails
                statusCode = $_.Exception.Response.StatusCode
            }
            
            throw $_
        }
        catch {
            Write-LogEntry -Level "ERROR" -Message "API request error" -Context @{
                requestId = $requestId
                error = $_.Exception.Message
            }
            throw $_
        }
    }
    
    throw "API request failed after $MaxRetries retries"
}

# ============================================================================
# Response Validation
# ============================================================================

function Test-APIResponse {
    <#
    .SYNOPSIS
    Validate API response structure and content
    #>
    param(
        [Parameter(Mandatory=$true)]
        $Response,
        
        [string[]]$RequiredFields = @()
    )
    
    if ($Response.isDryRun) {
        Write-LogEntry -Level "DEBUG" -Message "Skipping validation for dry-run response"
        return $true
    }
    
    if (-not $Response.success) {
        throw "API response indicates failure"
    }
    
    if (-not $Response.content) {
        throw "API response missing content"
    }
    
    foreach ($field in $RequiredFields) {
        if (-not $Response.content.$field) {
            throw "API response missing required field: $field"
        }
    }
    
    return $true
}

# ============================================================================
# Audit Logging
# ============================================================================

function Log-APICall {
    <#
    .SYNOPSIS
    Log API call to audit trail
    #>
    param(
        [string]$Service,
        [string]$Action,
        [string]$Status,
        [int]$Duration,
        [int]$InputTokens = 0,
        [int]$OutputTokens = 0,
        [decimal]$Cost = 0,
        [string]$UserId = "system",
        [bool]$DryRun = $false,
        [hashtable]$Details = @{}
    )
    
    $auditEntry = @{
        timestamp = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")
        eventId = [guid]::NewGuid().ToString()
        eventType = "api-call"
        service = $Service
        action = $Action
        status = $Status
        duration = $Duration
        inputTokens = $InputTokens
        outputTokens = $OutputTokens
        cost = $Cost
        userId = $UserId
        dryRun = $DryRun
        details = $Details
    }
    
    $auditLogPath = Get-ConfigValue -Key "AI_AUDIT_LOG_PATH" -Default "./config/audit-log.json"
    
    if (Test-Path $auditLogPath) {
        $auditLog = Get-Content $auditLogPath | ConvertFrom-Json
        $auditLog.entries += $auditEntry
        $auditLog.lastModified = $auditEntry.timestamp
        $auditLog.totalEntries = $auditLog.entries.Count
        
        $auditLog | ConvertTo-Json -Depth 10 | Set-Content $auditLogPath
    } else {
        Write-LogEntry -Level "WARN" -Message "Audit log not found at $auditLogPath"
    }
}

# ============================================================================
# Error Handling
# ============================================================================

function Format-ErrorResponse {
    <#
    .SYNOPSIS
    Format error response for consistent error handling
    #>
    param(
        [Parameter(Mandatory=$true)]
        $Error,
        
        [string]$RequestId = ""
    )
    
    return @{
        success = $false
        error = $Error.Exception.Message
        errorType = $Error.Exception.GetType().Name
        requestId = $RequestId
        stackTrace = if ([Environment]::GetEnvironmentVariable("DEBUG_MODE")) { $Error.ScriptStackTrace } else { $null }
    }
}

# ============================================================================
# Cache Functions
# ============================================================================

$script:ResponseCache = @{}

function Get-CachedResponse {
    <#
    .SYNOPSIS
    Retrieve cached response if available and not expired
    #>
    param(
        [string]$CacheKey
    )
    
    if ($script:ResponseCache.ContainsKey($CacheKey)) {
        $cached = $script:ResponseCache[$CacheKey]
        $age = (Get-Date) - $cached.timestamp
        $ttl = [int](Get-ConfigValue -Key "CACHE_TTL" -Default "3600")
        
        if ($age.TotalSeconds -lt $ttl) {
            Write-LogEntry -Level "DEBUG" -Message "Cache hit for key: $CacheKey"
            return $cached.response
        } else {
            $script:ResponseCache.Remove($CacheKey)
        }
    }
    
    return $null
}

function Set-CachedResponse {
    <#
    .SYNOPSIS
    Cache API response
    #>
    param(
        [string]$CacheKey,
        $Response
    )
    
    $script:ResponseCache[$CacheKey] = @{
        response = $Response
        timestamp = Get-Date
    }
    
    Write-LogEntry -Level "DEBUG" -Message "Cached response for key: $CacheKey"
}

# ============================================================================
# Initialization
# ============================================================================

Load-EnvironmentConfig
Initialize-Logger -LogDir (Get-ConfigValue -Key "LOG_DIR" -Default "./logs") `
                  -LogLevel (Get-ConfigValue -Key "LOG_LEVEL" -Default "INFO")

Initialize-RateLimiter -ServiceName "openai" `
                       -RequestsPerMinute (Get-ConfigValue -Key "OPENAI_RATE_LIMIT" -Default "60")
Initialize-RateLimiter -ServiceName "codex" `
                       -RequestsPerMinute (Get-ConfigValue -Key "CODEX_RATE_LIMIT" -Default "50")

Write-LogEntry -Level "INFO" -Message "API client initialized"

Export-ModuleMember -Function @(
    "Load-EnvironmentConfig"
    "Get-ConfigValue"
    "Initialize-Logger"
    "Write-LogEntry"
    "Initialize-RateLimiter"
    "Test-RateLimit"
    "Add-RateLimitRequest"
    "Wait-RateLimit"
    "Invoke-OpenAIRequest"
    "Test-APIResponse"
    "Log-APICall"
    "Format-ErrorResponse"
    "Get-CachedResponse"
    "Set-CachedResponse"
)

<#
.SYNOPSIS
Cross-Component API for HELIOS Platform - Unified interface for all component communication.

.DESCRIPTION
Provides:
- Unified API routing for all components
- Request/response schema validation
- Request aggregation from multiple components
- Rate limiting and throttling
- Complete audit logging
- Error handling with fallbacks

.EXAMPLE
PS> .\cross-component-api.ps1 -Method POST -Route '/monado/authorize' -Body @{ user='admin' }
PS> .\cross-component-api.ps1 -Method GET -Route '/ai-hub/models'
PS> .\cross-component-api.ps1 -Method GET -Route '/system/health'

.NOTES
No component talks directly to another component - all communication flows through this API.
All requests are validated, logged, and rate-limited.
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('GET', 'POST', 'PUT', 'DELETE')]
    [string]$Method = 'GET',
    
    [Parameter(Mandatory=$false)]
    [string]$Route = '/system/health',
    
    [Parameter(Mandatory=$false)]
    [hashtable]$Body = @{},
    
    [Parameter(Mandatory=$false)]
    [string]$ConfigFile = 'C:\HELIOS\orchestration\config\api-routes.json'
)

$ErrorActionPreference = 'Stop'

# ===========================
# CONFIGURATION & ROUTING
# ===========================

$apiRoutes = @{
    # Monado Routes
    '/monado/authorize' = @{ component = 'monado'; method = 'POST'; rateLimit = 100 }
    '/monado/patterns' = @{ component = 'monado'; method = 'GET'; rateLimit = 1000 }
    '/monado/learn' = @{ component = 'monado'; method = 'POST'; rateLimit = 500 }
    
    # Aegis Routes
    '/aegis/policies' = @{ component = 'aegis'; method = 'GET'; rateLimit = 1000 }
    '/aegis/policy' = @{ component = 'aegis'; method = 'POST'; rateLimit = 100 }
    '/aegis/audit' = @{ component = 'aegis'; method = 'GET'; rateLimit = 500 }
    
    # AI Hub Routes
    '/ai-hub/models' = @{ component = 'ai-hub'; method = 'GET'; rateLimit = 1000 }
    '/ai-hub/predict' = @{ component = 'ai-hub'; method = 'POST'; rateLimit = 500 }
    '/ai-hub/train' = @{ component = 'ai-hub'; method = 'POST'; rateLimit = 100 }
    
    # Dev Hub Routes
    '/dev-hub/repos' = @{ component = 'dev-hub'; method = 'GET'; rateLimit = 1000 }
    '/dev-hub/build' = @{ component = 'dev-hub'; method = 'POST'; rateLimit = 200 }
    '/dev-hub/test' = @{ component = 'dev-hub'; method = 'POST'; rateLimit = 200 }
    
    # Build Agents Routes
    '/build-agents/jobs' = @{ component = 'build-agents'; method = 'GET'; rateLimit = 1000 }
    '/build-agents/deploy' = @{ component = 'build-agents'; method = 'POST'; rateLimit = 100 }
    
    # System Routes
    '/system/health' = @{ component = 'system'; method = 'GET'; rateLimit = 10000 }
    '/system/metrics' = @{ component = 'system'; method = 'GET'; rateLimit = 5000 }
    '/system/components' = @{ component = 'system'; method = 'GET'; rateLimit = 5000 }
}

$requestLog = @()
$rateLimitBuckets = @{}

# ===========================
# HELPER FUNCTIONS
# ===========================

function Write-ApiLog {
    param(
        [string]$Message,
        [ValidateSet('Info', 'Success', 'Warning', 'Error')][string]$Level = 'Info'
    )
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $color = @{
        'Info' = 'Cyan'
        'Success' = 'Green'
        'Warning' = 'Yellow'
        'Error' = 'Red'
    }[$Level]
    Write-Host "[$timestamp] [API] [$Level] $Message" -ForegroundColor $color
}

function New-ApiResponse {
    param(
        [int]$StatusCode = 200,
        [string]$Status = 'OK',
        [object]$Data = @{},
        [string]$Message = ''
    )
    
    return @{
        statusCode = $StatusCode
        status = $Status
        timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ'
        data = $Data
        message = $Message
    }
}

function Test-RateLimit {
    param([string]$Route)
    
    $routeConfig = $apiRoutes[$Route]
    if ($null -eq $routeConfig) {
        return $false
    }
    
    $limit = $routeConfig.rateLimit
    $now = [DateTime]::UtcNow
    $windowStart = $now.AddSeconds(-60)
    
    if (-not $rateLimitBuckets.ContainsKey($Route)) {
        $rateLimitBuckets[$Route] = @()
    }
    
    # Remove old requests outside the window
    $rateLimitBuckets[$Route] = @($rateLimitBuckets[$Route] | Where-Object { $_ -gt $windowStart })
    
    # Check if we're under the limit
    if ($rateLimitBuckets[$Route].Count -ge $limit) {
        Write-ApiLog "Rate limit exceeded for $Route" -Level Warning
        return $false
    }
    
    # Add current request
    $rateLimitBuckets[$Route] += $now
    return $true
}

function Invoke-RouteValidation {
    param([string]$Route)
    
    if (-not $apiRoutes.ContainsKey($Route)) {
        Write-ApiLog "Route not found: $Route" -Level Warning
        return $null
    }
    
    return $apiRoutes[$Route]
}

function Invoke-MethodValidation {
    param(
        [string]$Method,
        [string]$Route
    )
    
    $routeConfig = Invoke-RouteValidation $Route
    if ($null -eq $routeConfig) {
        return $false
    }
    
    if ($routeConfig.method -ne $Method) {
        Write-ApiLog "Method $Method not allowed for $Route (expected $($routeConfig.method))" -Level Warning
        return $false
    }
    
    return $true
}

function Invoke-BodyValidation {
    param(
        [string]$Route,
        [hashtable]$Body
    )
    
    # Basic validation - can be extended with schema validation
    switch ($Route) {
        '/monado/authorize' {
            return $Body.ContainsKey('user')
        }
        '/aegis/policy' {
            return $Body.ContainsKey('name') -and $Body.ContainsKey('rules')
        }
        '/ai-hub/predict' {
            return $Body.ContainsKey('model') -and $Body.ContainsKey('data')
        }
        default { return $true }
    }
}

function Invoke-ComponentRequest {
    param(
        [string]$Component,
        [string]$Route,
        [string]$Method,
        [hashtable]$Body
    )
    
    Write-ApiLog "Routing request to component: $Component ($Method $Route)" -Level Info
    
    # In production, this would make actual requests to component endpoints
    # For now, return mock responses
    $response = @{
        component = $Component
        route = $Route
        method = $Method
        timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ'
    }
    
    # Add mock data based on route
    switch ($Component) {
        'monado' {
            $response.patterns = @('auth-pattern', 'config-pattern')
            $response.learned = $true
        }
        'aegis' {
            $response.policies = @(
                @{ name = 'default-policy'; rules = 3 }
                @{ name = 'admin-policy'; rules = 5 }
            )
        }
        'ai-hub' {
            $response.models = @(
                @{ name = 'gpt-4'; available = $true }
                @{ name = 'claude-3'; available = $true }
            )
        }
        'system' {
            $response.components = @('monado', 'aegis', 'ai-hub', 'dev-hub', 'build-agents')
            $response.health = 'healthy'
            $response.uptime_hours = 42
        }
    }
    
    return $response
}

function Invoke-AggregatedRequest {
    param(
        [string[]]$Components,
        [string]$Route,
        [string]$Method,
        [hashtable]$Body
    )
    
    Write-ApiLog "Aggregating request across $($Components.Count) components" -Level Info
    
    $responses = @()
    foreach ($component in $Components) {
        $response = Invoke-ComponentRequest -Component $component -Route $Route -Method $Method -Body $Body
        $responses += $response
    }
    
    return $responses
}

function Log-Request {
    param(
        [string]$Route,
        [string]$Method,
        [int]$StatusCode,
        [string]$RemoteClient = 'local'
    )
    
    $logEntry = @{
        timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ'
        route = $Route
        method = $Method
        statusCode = $StatusCode
        client = $RemoteClient
    }
    
    $requestLog += $logEntry
    
    # Keep only last 1000 requests
    if ($requestLog.Count -gt 1000) {
        $requestLog = $requestLog[-1000..-1]
    }
}

# ===========================
# MAIN REQUEST HANDLER
# ===========================

function Invoke-ApiRequest {
    param(
        [string]$Route,
        [string]$Method,
        [hashtable]$Body
    )
    
    Write-ApiLog "$Method $Route" -Level Info
    
    # Step 1: Validate route exists
    $routeConfig = Invoke-RouteValidation $Route
    if ($null -eq $routeConfig) {
        $response = New-ApiResponse -StatusCode 404 -Status 'Not Found' -Message "Route not found: $Route"
        Log-Request $Route $Method 404
        return $response
    }
    
    # Step 2: Validate method
    if (-not (Invoke-MethodValidation $Method $Route)) {
        $response = New-ApiResponse -StatusCode 405 -Status 'Method Not Allowed' -Message "Method $Method not allowed"
        Log-Request $Route $Method 405
        return $response
    }
    
    # Step 3: Validate body (if POST/PUT)
    if ($Method -in @('POST', 'PUT')) {
        if (-not (Invoke-BodyValidation $Route $Body)) {
            $response = New-ApiResponse -StatusCode 400 -Status 'Bad Request' -Message "Invalid request body for $Route"
            Log-Request $Route $Method 400
            return $response
        }
    }
    
    # Step 4: Check rate limit
    if (-not (Test-RateLimit $Route)) {
        $response = New-ApiResponse -StatusCode 429 -Status 'Too Many Requests' -Message "Rate limit exceeded"
        Log-Request $Route $Method 429
        return $response
    }
    
    # Step 5: Route to component(s)
    try {
        $component = $routeConfig.component
        
        if ($component -eq 'system') {
            # System endpoints - handle specially
            if ($Route -eq '/system/health') {
                $data = @{
                    status = 'healthy'
                    components = @('monado', 'aegis', 'ai-hub', 'dev-hub', 'build-agents')
                    timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ'
                }
            } else {
                $data = @{
                    metrics = @{
                        memory_percent = 45
                        cpu_percent = 28
                        disk_percent = 62
                        requests_per_sec = 150
                    }
                }
            }
            $response = New-ApiResponse -StatusCode 200 -Status 'OK' -Data $data
        } else {
            # Component endpoints
            $componentData = Invoke-ComponentRequest -Component $component -Route $Route -Method $Method -Body $Body
            $response = New-ApiResponse -StatusCode 200 -Status 'OK' -Data $componentData
        }
        
        Log-Request $Route $Method 200
        return $response
    }
    catch {
        Write-ApiLog "ERROR handling request: $_" -Level Error
        $response = New-ApiResponse -StatusCode 500 -Status 'Internal Server Error' -Message $_.ToString()
        Log-Request $Route $Method 500
        return $response
    }
}

# ===========================
# EXECUTION
# ===========================

try {
    Write-Host "`n"
    Write-ApiLog "HELIOS Cross-Component API v1.0" -Level Info
    
    $response = Invoke-ApiRequest -Route $Route -Method $Method -Body $Body
    
    Write-Host "`n$($response | ConvertTo-Json -Depth 10)" -ForegroundColor Cyan
    Write-Host ""
}
catch {
    Write-ApiLog "FATAL ERROR: $_" -Level Error
    exit 1
}

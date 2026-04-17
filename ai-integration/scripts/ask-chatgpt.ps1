<#
.SYNOPSIS
ChatGPT API integration for HELIOS Platform

.DESCRIPTION
Sends prompts to OpenAI's ChatGPT API and returns responses. 
Includes caching, error handling, and cost tracking.

.PARAMETER Prompt
The user prompt to send to ChatGPT

.PARAMETER SystemPrompt
System prompt to set ChatGPT's behavior. 
Use predefined prompts: "HELIOS Optimizer", "Security Architect", etc.

.PARAMETER Model
GPT model to use: gpt-4 (default) or gpt-3.5-turbo

.PARAMETER Temperature
Response creativity: 0.0-1.0 (default: 0.7)

.PARAMETER MaxTokens
Maximum response length (default: 2000)

.PARAMETER UseCache
Whether to cache responses (default: $true)

.PARAMETER CacheDuration
How long to keep cached responses (default: 1 day)

.EXAMPLE
$response = Invoke-ChatGPT -Prompt "What phases should I enable?" `
    -SystemPrompt "HELIOS Optimizer"

.NOTES
Requires OPENAI_API_KEY environment variable
AI-Generated: Yes
#>

function Invoke-ChatGPT {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Prompt,
        
        [Parameter(Mandatory=$false)]
        [string]$SystemPrompt = "HELIOS Expert",
        
        [Parameter(Mandatory=$false)]
        [ValidateSet("gpt-4", "gpt-3.5-turbo")]
        [string]$Model = "gpt-4",
        
        [Parameter(Mandatory=$false)]
        [ValidateRange(0.0, 1.0)]
        [double]$Temperature = 0.7,
        
        [Parameter(Mandatory=$false)]
        [int]$MaxTokens = 2000,
        
        [Parameter(Mandatory=$false)]
        [bool]$UseCache = $true,
        
        [Parameter(Mandatory=$false)]
        [timespan]$CacheDuration = (New-TimeSpan -Days 1)
    )
    
    # Verify API key exists
    if (-not $env:OPENAI_API_KEY) {
        throw "OPENAI_API_KEY environment variable not set"
    }
    
    # Check cache first
    if ($UseCache) {
        $cached = Get-ChatGPTCache -Prompt $Prompt -SystemPrompt $SystemPrompt
        if ($cached -and $cached.ExpirationTime -gt [DateTime]::Now) {
            Write-Verbose "Using cached response"
            return $cached.Response
        }
    }
    
    # Prepare request
    $headers = @{
        "Authorization" = "Bearer $($env:OPENAI_API_KEY)"
        "Content-Type" = "application/json"
    }
    
    $body = @{
        model = $Model
        messages = @(
            @{role = "system"; content = $SystemPrompt},
            @{role = "user"; content = $Prompt}
        )
        temperature = $Temperature
        max_tokens = $MaxTokens
    } | ConvertTo-Json
    
    # Call API with error handling
    try {
        $startTime = [DateTime]::Now
        $response = Invoke-RestMethod `
            -Uri "https://api.openai.com/v1/chat/completions" `
            -Method Post `
            -Headers $headers `
            -Body $body `
            -ErrorAction Stop
        
        $duration = ([DateTime]::Now - $startTime).TotalSeconds
        
        # Log the interaction
        Log-ChatGPTInteraction -Prompt $Prompt -Response $response `
            -Model $Model -Duration $duration
        
        # Cache result if enabled
        if ($UseCache) {
            Save-ChatGPTCache -Prompt $Prompt -SystemPrompt $SystemPrompt `
                -Response $response -Duration $CacheDuration
        }
        
        return $response
    }
    catch {
        Write-Error "ChatGPT API error: $_"
        throw
    }
}

<#
.SYNOPSIS
Get cached ChatGPT response
#>
function Get-ChatGPTCache {
    param(
        [string]$Prompt,
        [string]$SystemPrompt
    )
    
    $cacheDir = "$env:LOCALAPPDATA\helios-ai-cache"
    if (-not (Test-Path $cacheDir)) {
        return $null
    }
    
    $hash = ([System.Security.Cryptography.SHA256]::Create()).ComputeHash(
        [System.Text.Encoding]::UTF8.GetBytes("$SystemPrompt|$Prompt")
    ) | ForEach-Object { $_.ToString("x2") } | Join-String
    
    $cachePath = "$cacheDir\$hash.json"
    if (Test-Path $cachePath) {
        $cached = Get-Content $cachePath | ConvertFrom-Json
        return @{
            Response = $cached.response
            ExpirationTime = [DateTime]::Parse($cached.expiration)
        }
    }
    
    return $null
}

<#
.SYNOPSIS
Save ChatGPT response to cache
#>
function Save-ChatGPTCache {
    param(
        [string]$Prompt,
        [string]$SystemPrompt,
        [PSObject]$Response,
        [timespan]$Duration
    )
    
    $cacheDir = "$env:LOCALAPPDATA\helios-ai-cache"
    if (-not (Test-Path $cacheDir)) {
        New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
    }
    
    $hash = ([System.Security.Cryptography.SHA256]::Create()).ComputeHash(
        [System.Text.Encoding]::UTF8.GetBytes("$SystemPrompt|$Prompt")
    ) | ForEach-Object { $_.ToString("x2") } | Join-String
    
    $cachePath = "$cacheDir\$hash.json"
    
    $cacheData = @{
        prompt = $Prompt
        system = $SystemPrompt
        response = $Response
        expiration = ([DateTime]::Now + $Duration).ToString("o")
        created = [DateTime]::Now.ToString("o")
    } | ConvertTo-Json
    
    $cacheData | Set-Content -Path $cachePath -Encoding UTF8
}

<#
.SYNOPSIS
Log ChatGPT API interaction
#>
function Log-ChatGPTInteraction {
    param(
        [string]$Prompt,
        [PSObject]$Response,
        [string]$Model,
        [double]$Duration
    )
    
    $logDir = "$env:LOCALAPPDATA\helios-ai-logs"
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }
    
    $logFile = "$logDir\chatgpt-$(Get-Date -Format 'yyyyMMdd').log"
    
    $logEntry = @"
[$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')] CHATGPT_REQUEST
- Model: $Model
- Prompt length: $($Prompt.Length) chars
- Duration: $($Duration.ToString('F2'))s
- Status: Success
- Tokens used: Input=$($Response.usage.prompt_tokens) Output=$($Response.usage.completion_tokens)
- Cost: `$$(CalculateCost -InputTokens $Response.usage.prompt_tokens -OutputTokens $Response.usage.completion_tokens -Model $Model)

"@
    
    Add-Content -Path $logFile -Value $logEntry -Encoding UTF8
}

<#
.SYNOPSIS
Calculate API cost for tokens
#>
function CalculateCost {
    param(
        [int]$InputTokens,
        [int]$OutputTokens,
        [string]$Model
    )
    
    $costs = @{
        "gpt-4" = @{ input = 0.03; output = 0.06 }
        "gpt-3.5-turbo" = @{ input = 0.0005; output = 0.0015 }
    }
    
    if ($costs[$Model]) {
        $cost = ($InputTokens * $costs[$Model].input / 1000) + 
                ($OutputTokens * $costs[$Model].output / 1000)
        return $cost.ToString("F4")
    }
    
    return "0.0000"
}

# Export function
Export-ModuleMember -Function Invoke-ChatGPT

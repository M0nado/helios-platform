[CmdletBinding()]
param(
    [ValidateRange(1, 16)] [int] $ThrottleLimit = 6,
    [switch] $IncludeNetworkTools,
    [switch] $CheckAuthentication
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$configPath = Join-Path $PSScriptRoot '../config/cli-matrix.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$parallelSupported = $PSVersionTable.PSVersion.Major -ge 7

function Invoke-ToolProbe {
    param(
        [Parameter(Mandatory)] [object] $Tool,
        [Parameter(Mandatory)] [bool] $AllowNetworkTool
    )

    if ($Tool.networkRequired -and -not $AllowNetworkTool) {
        return [pscustomobject]@{ id = $Tool.id; found = $false; status = 'network-check-skipped'; version = $null; required = $Tool.required }
    }

    $resolved = Get-Command $Tool.command -ErrorAction SilentlyContinue
    if (-not $resolved) {
        return [pscustomobject]@{ id = $Tool.id; found = $false; status = 'missing'; version = $null; required = $Tool.required }
    }

    try {
        $arguments = @()
        if ($null -ne $Tool.arguments) {
            $arguments = @($Tool.arguments)
        }
        $output = & $Tool.command @arguments 2>&1
        $exitCode = $LASTEXITCODE
        $firstLine = $output | ForEach-Object { "$_".Trim() } | Where-Object { $_ } | Select-Object -First 1
        if ($firstLine -and $firstLine.Length -gt 180) {
            $firstLine = $firstLine.Substring(0, 180)
        }
        return [pscustomobject]@{
            id = $Tool.id
            found = $true
            status = if ($exitCode -eq 0) { 'ready' } else { 'error' }
            version = $firstLine
            required = $Tool.required
        }
    }
    catch {
        return [pscustomobject]@{ id = $Tool.id; found = $true; status = 'error'; version = $null; required = $Tool.required }
    }
}

function Invoke-AuthenticationProbe {
    param([Parameter(Mandatory)] [object] $Check)

    if (-not (Get-Command $Check.command -ErrorAction SilentlyContinue)) {
        return [pscustomobject]@{ id = $Check.id; authenticated = $false; status = 'tool-missing' }
    }

    try {
        $arguments = @()
        if ($null -ne $Check.arguments) {
            $arguments = @($Check.arguments)
        }
        & $Check.command @arguments *> $null
        return [pscustomobject]@{
            id = $Check.id
            authenticated = ($LASTEXITCODE -eq 0)
            status = if ($LASTEXITCODE -eq 0) { 'authenticated' } else { 'not-authenticated' }
        }
    }
    catch {
        return [pscustomobject]@{ id = $Check.id; authenticated = $false; status = 'not-authenticated' }
    }
}

$results = @()
if ($parallelSupported) {
    $results = $config.tools | ForEach-Object -Parallel {
        $tool = $_
        if ($tool.networkRequired -and -not $using:IncludeNetworkTools) {
            return [pscustomobject]@{ id = $tool.id; found = $false; status = 'network-check-skipped'; version = $null; required = $tool.required }
        }
        $resolved = Get-Command $tool.command -ErrorAction SilentlyContinue
        if (-not $resolved) {
            return [pscustomobject]@{ id = $tool.id; found = $false; status = 'missing'; version = $null; required = $tool.required }
        }
        try {
            $arguments = @()
            if ($null -ne $tool.arguments) {
                $arguments = @($tool.arguments)
            }
            $output = & $tool.command @arguments 2>&1
            $exitCode = $LASTEXITCODE
            $firstLine = $output | ForEach-Object { "$_".Trim() } | Where-Object { $_ } | Select-Object -First 1
            if ($firstLine -and $firstLine.Length -gt 180) {
                $firstLine = $firstLine.Substring(0, 180)
            }
            return [pscustomobject]@{
                id = $tool.id
                found = $true
                status = if ($exitCode -eq 0) { 'ready' } else { 'error' }
                version = $firstLine
                required = $tool.required
            }
        }
        catch {
            return [pscustomobject]@{ id = $tool.id; found = $true; status = 'error'; version = $null; required = $tool.required }
        }
    } -ThrottleLimit $ThrottleLimit
}
else {
    foreach ($tool in $config.tools) {
        $results += Invoke-ToolProbe -Tool $tool -AllowNetworkTool $IncludeNetworkTools.IsPresent
    }
}

$auth = @()
if ($CheckAuthentication) {
    if ($parallelSupported) {
        $auth = $config.authenticationChecks | ForEach-Object -Parallel {
            $check = $_
            if (-not (Get-Command $check.command -ErrorAction SilentlyContinue)) {
                return [pscustomobject]@{ id = $check.id; authenticated = $false; status = 'tool-missing' }
            }
            try {
                $arguments = @()
                if ($null -ne $check.arguments) {
                    $arguments = @($check.arguments)
                }
                & $check.command @arguments *> $null
                return [pscustomobject]@{
                    id = $check.id
                    authenticated = ($LASTEXITCODE -eq 0)
                    status = if ($LASTEXITCODE -eq 0) { 'authenticated' } else { 'not-authenticated' }
                }
            }
            catch {
                return [pscustomobject]@{ id = $check.id; authenticated = $false; status = 'not-authenticated' }
            }
        } -ThrottleLimit $ThrottleLimit
    }
    else {
        foreach ($check in $config.authenticationChecks) {
            $auth += Invoke-AuthenticationProbe -Check $check
        }
    }
}

$missingRequired = @($results | Where-Object { $_.required -and $_.status -ne 'ready' }).Count
[ordered]@{
    schemaVersion = 1
    generatedAt = [DateTimeOffset]::UtcNow.ToString('o')
    executionEngine = if ($parallelSupported) { 'parallel' } else { "sequential-ps$($PSVersionTable.PSVersion.Major)" }
    parallelism = if ($parallelSupported) { $ThrottleLimit } else { 1 }
    ready = ($missingRequired -eq 0)
    missingRequired = $missingRequired
    tools = @($results | Sort-Object id)
    authentication = @($auth | Sort-Object id)
} | ConvertTo-Json -Depth 6

if ($missingRequired -gt 0) { exit 2 }

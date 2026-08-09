[CmdletBinding()]
param(
    [ValidateSet('local-windows', 'local-docker', 'hybrid-windows-docker-fleet')]
    [string] $Mode,
    [string] $ManifestPath = '',
    [string] $OutputPath = '',
    [switch] $SkipRuntimeStart
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-SmokeCheck {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [ValidateSet('passed', 'failed', 'skipped')] [string] $Status,
        [Parameter(Mandatory = $true)] [string] $Detail
    )

    return [ordered]@{
        name = $Name
        status = $Status
        detail = $Detail
    }
}

function Add-SmokeCheck {
    param(
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [System.Collections.Generic.List[object]] $Checks,
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [ValidateSet('passed', 'failed', 'skipped')] [string] $Status,
        [Parameter(Mandatory = $true)] [string] $Detail
    )

    $Checks.Add((New-SmokeCheck -Name $Name -Status $Status -Detail $Detail)) | Out-Null
}

function Wait-ForHealthEndpoint {
    param(
        [Parameter(Mandatory = $true)] [string] $Endpoint,
        [Parameter(Mandatory = $true)] [int] $TimeoutSeconds,
        [Parameter(Mandatory = $true)] [int] $ProbeTimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Endpoint -Method GET -TimeoutSec $ProbeTimeoutSeconds
            if ($response.StatusCode -eq 200) {
                return $true
            }
        }
        catch [System.Net.WebException] {
        }
        catch [System.Net.Http.HttpRequestException] {
        }

        Start-Sleep -Seconds 2
    }

    return $false
}

function Stop-ProcessSafe {
    param([System.Diagnostics.Process] $Process)

    if ($null -eq $Process) { return }
    if ($Process.HasExited) { return }
    Stop-Process -Id $Process.Id -Force
}

function Stop-ContainerSafe {
    param([string] $ContainerName)

    if ([string]::IsNullOrWhiteSpace($ContainerName)) { return }
    & docker stop $ContainerName *> $null
}

function Start-LocalRuntime {
    param(
        [Parameter(Mandatory = $true)] [string] $RepositoryRoot
    )

    $startScript = Join-Path $RepositoryRoot 'monado/helios-control/scripts/Start-HeliosLocal.ps1'
    if (-not (Test-Path -LiteralPath $startScript)) {
        throw "Missing local runtime script: $startScript"
    }

    $stdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ("xcore9-local-runtime-{0}.out.log" -f ([Guid]::NewGuid().ToString('N')))
    $stderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ("xcore9-local-runtime-{0}.err.log" -f ([Guid]::NewGuid().ToString('N')))
    $process = Start-Process -FilePath 'pwsh' -ArgumentList @('-NoProfile', '-File', $startScript) -WorkingDirectory $RepositoryRoot -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    return [ordered]@{
        process = $process
        stdoutPath = $stdoutPath
        stderrPath = $stderrPath
    }
}

function Start-DockerRuntime {
    param(
        [Parameter(Mandatory = $true)] [string] $RepositoryRoot
    )

    & docker info --format '{{json .ServerVersion}}' *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker daemon is not available.'
    }

    $buildContext = Join-Path $RepositoryRoot 'monado/helios-control'
    $dockerfile = Join-Path $buildContext 'src/Helios.Connect.Api/Dockerfile'
    if (-not (Test-Path -LiteralPath $dockerfile)) {
        throw "Missing Dockerfile: $dockerfile"
    }

    $imageTag = 'helios-connect:xcore9-local-smoke'
    & docker build --file $dockerfile --tag $imageTag $buildContext
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker build failed.'
    }

    $containerName = 'helios-connect-xcore9-smoke-' + ([Guid]::NewGuid().ToString('N').Substring(0, 10))
    & docker run --detach --rm --name $containerName --publish 5081:8080 --env HELIOS_EXECUTION_MODE=dry-run --env HELIOS_CLOUD_RUNTIME_ONLY=true $imageTag *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker run failed.'
    }

    return $containerName
}

function Write-SmokeSummary {
    param(
        [Parameter(Mandatory = $true)] [hashtable] $Result,
        [Parameter(Mandatory = $true)] [string] $SummaryPath
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# Runtime smoke summary: $($Result.mode)")
    $lines.Add('')
    $lines.Add("- Status: " + [string] $Result.status)
    $lines.Add("- Correlation ID: " + [string] $Result.correlationId)
    $lines.Add("- Generated (UTC): " + [string] $Result.generatedAtUtc)
    $lines.Add('')
    $lines.Add('| Check | Status | Detail |')
    $lines.Add('| --- | --- | --- |')
    foreach ($check in $Result.checks) {
        $detail = ([string] $check.detail).Replace('|', '/')
        $lines.Add("| $($check.name) | $($check.status) | $detail |")
    }

    $summaryDirectory = Split-Path -Parent $SummaryPath
    if (-not (Test-Path -LiteralPath $summaryDirectory)) {
        New-Item -ItemType Directory -Path $summaryDirectory | Out-Null
    }

    [System.IO.File]::WriteAllLines($SummaryPath, $lines)
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$manifestFile = if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    Join-Path $repositoryRoot 'monado/helios-control/config/xcore9-runtime-matrix.v1.json'
}
else {
    $ManifestPath
}

if (-not (Test-Path -LiteralPath $manifestFile)) {
    throw "Runtime matrix manifest not found: $manifestFile"
}

$manifest = Get-Content -LiteralPath $manifestFile -Raw | ConvertFrom-Json
$modeConfig = $manifest.modes | Where-Object { $_.id -eq $Mode } | Select-Object -First 1
if ($null -eq $modeConfig) {
    throw "Mode '$Mode' was not found in $manifestFile"
}

$resolvedOutputPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Join-Path $repositoryRoot ([string] $modeConfig.smokeEvidence.data)
}
else {
    $OutputPath
}
$resolvedSummaryPath = Join-Path $repositoryRoot ([string] $modeConfig.smokeEvidence.summary)
$manifestReference = if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    'monado/helios-control/config/xcore9-runtime-matrix.v1.json'
}
else {
    $ManifestPath
}

$checks = [System.Collections.Generic.List[object]]::new()
$correlationId = [Guid]::NewGuid().ToString()

Add-SmokeCheck -Checks $checks -Name 'manifest-load' -Status 'passed' -Detail "Loaded $manifestReference"
if ($manifest.defaultExecutionMode -eq 'validation-first' -and $manifest.governance.nonDestructiveDefault -eq $true) {
    Add-SmokeCheck -Checks $checks -Name 'governance-default' -Status 'passed' -Detail 'validation-first non-destructive default is enforced.'
}
else {
    Add-SmokeCheck -Checks $checks -Name 'governance-default' -Status 'failed' -Detail 'validation-first non-destructive default is not enforced.'
}

$modeDeny = [System.Collections.Generic.HashSet[string]]::new([string[]] $modeConfig.disallowedOperations)
$missingDeny = @()
foreach ($required in $manifest.requiredDenyList) {
    if (-not $modeDeny.Contains([string] $required)) {
        $missingDeny += [string] $required
    }
}
if ($missingDeny.Count -eq 0) {
    Add-SmokeCheck -Checks $checks -Name 'mode-deny-list' -Status 'passed' -Detail 'All required deny-list operations are present.'
}
else {
    Add-SmokeCheck -Checks $checks -Name 'mode-deny-list' -Status 'failed' -Detail ('Missing deny-list entries: ' + ($missingDeny -join ', '))
}

$localRuntime = $null
$localRuntimeStdout = $null
$localRuntimeStderr = $null
$containerName = ''

try {
    if ($SkipRuntimeStart) {
        Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'skipped' -Detail 'Runtime startup checks were skipped by request.'
    }
    elseif ($Mode -eq 'local-windows') {
        $localRuntime = Start-LocalRuntime -RepositoryRoot $repositoryRoot
        $localRuntimeStdout = $localRuntime.stdoutPath
        $localRuntimeStderr = $localRuntime.stderrPath
        $healthy = Wait-ForHealthEndpoint -Endpoint ([string] $modeConfig.healthContract.endpoint) -TimeoutSeconds ([int] $modeConfig.healthContract.maxStartupSeconds) -ProbeTimeoutSeconds ([int] $modeConfig.healthContract.probeTimeoutSeconds)
        if ($healthy) {
            Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'passed' -Detail "Endpoint $($modeConfig.healthContract.endpoint) returned HTTP 200."
        }
        else {
            Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail "Endpoint $($modeConfig.healthContract.endpoint) did not become healthy in time."
        }
    }
    elseif ($Mode -eq 'local-docker') {
        $containerName = Start-DockerRuntime -RepositoryRoot $repositoryRoot
        $healthy = Wait-ForHealthEndpoint -Endpoint ([string] $modeConfig.healthContract.endpoint) -TimeoutSeconds ([int] $modeConfig.healthContract.maxStartupSeconds) -ProbeTimeoutSeconds ([int] $modeConfig.healthContract.probeTimeoutSeconds)
        if ($healthy) {
            Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'passed' -Detail "Endpoint $($modeConfig.healthContract.endpoint) returned HTTP 200."
        }
        else {
            Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail "Endpoint $($modeConfig.healthContract.endpoint) did not become healthy in time."
        }
    }
    else {
        $localRuntime = Start-LocalRuntime -RepositoryRoot $repositoryRoot
        $localRuntimeStdout = $localRuntime.stdoutPath
        $localRuntimeStderr = $localRuntime.stderrPath
        $containerName = Start-DockerRuntime -RepositoryRoot $repositoryRoot

        $endpoints = [string[]] $modeConfig.healthContract.endpoints
        $healthyWindows = Wait-ForHealthEndpoint -Endpoint $endpoints[0] -TimeoutSeconds ([int] $modeConfig.healthContract.maxStartupSeconds) -ProbeTimeoutSeconds ([int] $modeConfig.healthContract.probeTimeoutSeconds)
        $healthyDocker = Wait-ForHealthEndpoint -Endpoint $endpoints[1] -TimeoutSeconds ([int] $modeConfig.healthContract.maxStartupSeconds) -ProbeTimeoutSeconds ([int] $modeConfig.healthContract.probeTimeoutSeconds)

        if ($healthyWindows -and $healthyDocker) {
            Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'passed' -Detail 'Both hybrid runtime endpoints returned HTTP 200.'
        }
        else {
            Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail 'One or more hybrid runtime endpoints failed health checks.'
        }

        $secretScopes = $manifest.modes | ForEach-Object { [string] $_.boundaries.secrets.scopeId }
        if (($secretScopes | Select-Object -Unique).Count -eq $secretScopes.Count) {
            Add-SmokeCheck -Checks $checks -Name 'hybrid-secret-isolation' -Status 'passed' -Detail 'Hybrid secret scopes are unique across runtime modes.'
        }
        else {
            Add-SmokeCheck -Checks $checks -Name 'hybrid-secret-isolation' -Status 'failed' -Detail 'Hybrid secret scopes are not unique across runtime modes.'
        }
    }
}
catch [System.Management.Automation.CommandNotFoundException] {
    Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail $_.Exception.Message
}
catch [System.IO.IOException] {
    Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail $_.Exception.Message
}
catch [System.UnauthorizedAccessException] {
    Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail $_.Exception.Message
}
catch [System.Management.Automation.RuntimeException] {
    Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail $_.Exception.Message
}
finally {
    if ($null -ne $localRuntime -and $null -ne $localRuntime.process) {
        Stop-ProcessSafe -Process $localRuntime.process
    }
    if (-not [string]::IsNullOrWhiteSpace($containerName)) {
        Stop-ContainerSafe -ContainerName $containerName
    }
}

$failedChecks = @($checks | Where-Object { $_.status -eq 'failed' })
$status = if ($failedChecks.Count -eq 0) { 'passed' } else { 'failed' }
$result = [ordered]@{
    schemaVersion = 1
    mode = $Mode
    status = $status
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    correlationId = $correlationId
    manifestPath = $manifestReference
    checks = @($checks)
    diagnostics = [ordered]@{
        localRuntimeLogsCaptured = ($null -ne $localRuntimeStdout -or $null -ne $localRuntimeStderr)
        localRuntimeLogPathsRedacted = $true
    }
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

$result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $resolvedOutputPath -Encoding utf8
Write-SmokeSummary -Result $result -SummaryPath $resolvedSummaryPath

Write-Output ($result | ConvertTo-Json -Depth 10)
if ($status -ne 'passed') { exit 1 }

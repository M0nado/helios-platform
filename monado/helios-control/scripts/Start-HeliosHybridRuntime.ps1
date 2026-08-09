[CmdletBinding()]
param(
    [switch] $SkipDockerBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Stop-ProcessTreeSafe {
    param([int] $ProcessId)

    if ($ProcessId -le 0) { return }

    if ($IsWindows) {
        $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $ProcessId" -ErrorAction SilentlyContinue)
        foreach ($child in $children) {
            Stop-ProcessTreeSafe -ProcessId ([int] $child.ProcessId)
        }
    }

    try {
        Stop-Process -Id $ProcessId -Force -ErrorAction Stop
    }
    catch {
    }
}

function Start-ProcessWithEnvironment {
    param(
        [Parameter(Mandatory = $true)] [hashtable] $Parameters,
        [Parameter(Mandatory = $true)] [hashtable] $Environment
    )

    $startProcessCommand = Get-Command Start-Process -ErrorAction Stop
    if ($Environment.Count -gt 0 -and $startProcessCommand.Parameters.ContainsKey('Environment')) {
        $withEnvironment = @{}
        foreach ($entry in $Parameters.GetEnumerator()) {
            $withEnvironment[$entry.Key] = $entry.Value
        }
        $withEnvironment['Environment'] = $Environment
        return Start-Process @withEnvironment
    }

    if ($Environment.Count -eq 0) {
        return Start-Process @Parameters
    }

    $previousEnvironment = @{}
    foreach ($entry in $Environment.GetEnumerator()) {
        $entryName = [string] $entry.Key
        $previousEnvironment[$entryName] = [Environment]::GetEnvironmentVariable($entryName, 'Process')
        [Environment]::SetEnvironmentVariable($entryName, [string] $entry.Value, 'Process')
    }
    try {
        return Start-Process @Parameters
    }
    finally {
        foreach ($entry in $Environment.GetEnumerator()) {
            $entryName = [string] $entry.Key
            [Environment]::SetEnvironmentVariable($entryName, $previousEnvironment[$entryName], 'Process')
        }
    }
}

function Wait-ForHealthEndpoint {
    param(
        [Parameter(Mandatory = $true)] [string] $Endpoint,
        [Parameter(Mandatory = $true)] [datetime] $DeadlineUtc,
        [Parameter(Mandatory = $true)] [int] $ProbeTimeoutSeconds,
        [string] $CorrelationId = ''
    )

    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($CorrelationId)) {
        $headers['x-correlation-id'] = $CorrelationId
    }

    while ((Get-Date) -lt $DeadlineUtc) {
        $remainingSeconds = [int] [Math]::Ceiling(($DeadlineUtc - (Get-Date)).TotalSeconds)
        if ($remainingSeconds -le 0) {
            break
        }
        $effectiveProbeTimeout = [int] [Math]::Max(1, [Math]::Min($ProbeTimeoutSeconds, $remainingSeconds))

        try {
            if ($headers.Count -gt 0) {
                $response = Invoke-WebRequest -Uri $Endpoint -Method GET -TimeoutSec $effectiveProbeTimeout -Headers $headers
            }
            else {
                $response = Invoke-WebRequest -Uri $Endpoint -Method GET -TimeoutSec $effectiveProbeTimeout
            }
            if ($response.StatusCode -eq 200) {
                return $true
            }
        }
        catch [System.Net.WebException] {
        }
        catch [System.Net.Http.HttpRequestException] {
        }
        catch [System.Management.Automation.RuntimeException] {
        }

        Start-Sleep -Seconds ([int] [Math]::Max(1, [Math]::Min(2, $remainingSeconds)))
    }

    return $false
}

function Get-RunningContainerId {
    param([Parameter(Mandatory = $true)] [string] $ContainerName)

    if ([string]::IsNullOrWhiteSpace($ContainerName)) {
        return ''
    }

    return [string] ((& docker ps --filter "name=^/$ContainerName$" --format '{{.ID}}' 2>$null) | Select-Object -First 1)
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$startLocalScript = Join-Path $PSScriptRoot 'Start-HeliosLocal.ps1'
if (-not (Test-Path -LiteralPath $startLocalScript)) {
    throw "Missing local runtime script: $startLocalScript"
}

$correlationId = [string] $env:HELIOS_CORRELATION_ID
if ([string]::IsNullOrWhiteSpace($correlationId)) {
    $correlationId = [Guid]::NewGuid().ToString()
}
$startupTimeoutSeconds = 300
$healthProbeTimeoutSeconds = 10
$windowsHealthEndpoint = 'http://127.0.0.1:5080/health/ready'
$dockerHealthEndpoint = 'http://127.0.0.1:5081/health/ready'

$localCpuCores = 6
$localMemoryGb = 12
$localMemoryHex = ('0x{0:x}' -f ([Int64] $localMemoryGb * 1GB))
$localResourceEnvironment = @{
    DOTNET_PROCESSOR_COUNT = [string] $localCpuCores
    DOTNET_GCHeapHardLimit = $localMemoryHex
    COMPlus_GCHeapHardLimit = $localMemoryHex
    HELIOS_CORRELATION_ID = $correlationId
}

$localStdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ("xcore9-hybrid-local-{0}.out.log" -f ([Guid]::NewGuid().ToString('N')))
$localStderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ("xcore9-hybrid-local-{0}.err.log" -f ([Guid]::NewGuid().ToString('N')))

$localProcessParameters = @{
    FilePath = 'pwsh'
    ArgumentList = @('-NoProfile', '-File', $startLocalScript)
    WorkingDirectory = $repositoryRoot
    PassThru = $true
    RedirectStandardOutput = $localStdoutPath
    RedirectStandardError = $localStderrPath
}

$containerName = 'helios-connect-xcore9-hybrid'
$localProcess = Start-ProcessWithEnvironment -Parameters $localProcessParameters -Environment $localResourceEnvironment

try {
    & docker info --format '{{json .ServerVersion}}' *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker daemon is not available.'
    }

    $buildContext = Join-Path $repositoryRoot 'monado/helios-control'
    $dockerfile = Join-Path $buildContext 'src/Helios.Connect.Api/Dockerfile'
    if (-not (Test-Path -LiteralPath $dockerfile)) {
        throw "Missing Dockerfile: $dockerfile"
    }

    $imageTag = 'helios-connect:xcore9-local'
    if (-not $SkipDockerBuild) {
        & docker build --file $dockerfile --tag $imageTag $buildContext
        if ($LASTEXITCODE -ne 0) {
            throw 'Docker build failed.'
        }
    }

    $existingContainerId = Get-RunningContainerId -ContainerName $containerName
    if (-not [string]::IsNullOrWhiteSpace($existingContainerId)) {
        if ($env:HELIOS_APPROVE_HYBRID_CONTAINER_REPLACE -ne 'true') {
            throw "Container '$containerName' is already running. Set HELIOS_APPROVE_HYBRID_CONTAINER_REPLACE=true to authorize replacement."
        }
        & docker rm --force $containerName *> $null
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to replace running container '$containerName'."
        }
    }

    $dockerRunOutput = & docker run --detach --rm --name $containerName --publish '127.0.0.1:5081:8080' --cpus '6' --memory '12g' --env 'HELIOS_EXECUTION_MODE=dry-run' --env 'HELIOS_CLOUD_RUNTIME_ONLY=false' --env 'HELIOS_LOCAL_RUNTIME_ALLOWED=true' --env "HELIOS_CORRELATION_ID=$correlationId" $imageTag 2>&1
    if ($LASTEXITCODE -ne 0) {
        $joinedOutput = ($dockerRunOutput | ForEach-Object { [string] $_.ToString().Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' | '
        Stop-ProcessTreeSafe -ProcessId $localProcess.Id
        if ([string]::IsNullOrWhiteSpace($joinedOutput)) {
            throw 'Docker run failed.'
        }
        throw ("Docker run failed: " + $joinedOutput)
    }

    $startupDeadline = (Get-Date).AddSeconds($startupTimeoutSeconds)
    $localProcess.Refresh()
    if ($localProcess.HasExited) {
        throw 'Local runtime process exited before hybrid readiness checks completed.'
    }

    $runningContainerId = Get-RunningContainerId -ContainerName $containerName
    if ([string]::IsNullOrWhiteSpace($runningContainerId)) {
        throw "Container '$containerName' is not running after startup."
    }

    $windowsReady = Wait-ForHealthEndpoint -Endpoint $windowsHealthEndpoint -DeadlineUtc $startupDeadline -ProbeTimeoutSeconds $healthProbeTimeoutSeconds -CorrelationId $correlationId
    $dockerReady = Wait-ForHealthEndpoint -Endpoint $dockerHealthEndpoint -DeadlineUtc $startupDeadline -ProbeTimeoutSeconds $healthProbeTimeoutSeconds -CorrelationId $correlationId

    $localProcess.Refresh()
    $runningContainerId = Get-RunningContainerId -ContainerName $containerName
    if ($localProcess.HasExited -or [string]::IsNullOrWhiteSpace($runningContainerId) -or -not $windowsReady -or -not $dockerReady) {
        $containerState = [string] ((& docker inspect --format '{{.State.Status}}' $containerName 2>$null) | Select-Object -First 1)
        if ([string]::IsNullOrWhiteSpace($containerState)) {
            $containerState = 'missing'
        }
        throw "Hybrid runtime failed readiness verification (localExited=$($localProcess.HasExited), containerState=$containerState, windowsReady=$windowsReady, dockerReady=$dockerReady)."
    }

    $result = [ordered]@{
        schemaVersion = 1
        startupMode = 'persistent-hybrid-runtime'
        correlationId = $correlationId
        localRuntimeProcessId = $localProcess.Id
        localRuntimeStdoutLog = $localStdoutPath
        localRuntimeStderrLog = $localStderrPath
        localRuntimeResourceEnvironment = $localResourceEnvironment
        dockerContainerName = $containerName
        dockerContainerId = $runningContainerId
        endpoints = [ordered]@{
            windows = $windowsHealthEndpoint
            docker = $dockerHealthEndpoint
        }
        notes = @(
            'This command starts persistent local and Docker runtimes.',
            'Use Stop-Process on localRuntimeProcessId and docker stop on dockerContainerName to stop both runtimes.'
        )
    }

    $result | ConvertTo-Json -Depth 10 | Write-Output
}
catch {
    if ($null -ne $localProcess -and -not $localProcess.HasExited) {
        Stop-ProcessTreeSafe -ProcessId $localProcess.Id
    }
    if (-not [string]::IsNullOrWhiteSpace($containerName)) {
        & docker rm --force $containerName *> $null
    }
    throw
}

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
        [int] $TimeoutSeconds = 0,
        [Parameter(Mandatory = $true)] [int] $ProbeTimeoutSeconds,
        [datetime] $DeadlineUtc = [datetime]::MinValue,
        [string] $CorrelationId = '',
        [System.Diagnostics.Process] $RequiredProcess = $null,
        [string] $ExpectedRuntimeCorrelationId = ''
    )

    if ($DeadlineUtc -gt [datetime]::MinValue) {
        $deadline = $DeadlineUtc
    }
    else {
        if ($TimeoutSeconds -le 0) {
            throw 'TimeoutSeconds must be a positive integer when DeadlineUtc is not provided.'
        }
        $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    }

    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($CorrelationId)) {
        $headers['x-correlation-id'] = $CorrelationId
    }

    while ((Get-Date) -lt $deadline) {
        if ($null -ne $RequiredProcess) {
            $RequiredProcess.Refresh()
            if ($RequiredProcess.HasExited) {
                return $false
            }
        }

        $remainingSeconds = [int] [Math]::Ceiling(($deadline - (Get-Date)).TotalSeconds)
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
                if (-not [string]::IsNullOrWhiteSpace($ExpectedRuntimeCorrelationId)) {
                    $matchesRuntimeCorrelation = $false
                    try {
                        $responseContent = [string] $response.Content
                        if (-not [string]::IsNullOrWhiteSpace($responseContent)) {
                            $payload = $responseContent | ConvertFrom-Json -ErrorAction Stop
                            if ($null -ne $payload -and $payload.PSObject.Properties.Name -contains 'runtimeCorrelationId') {
                                $matchesRuntimeCorrelation = ([string] $payload.runtimeCorrelationId -eq $ExpectedRuntimeCorrelationId)
                            }
                        }
                    }
                    catch [System.Management.Automation.RuntimeException] {
                        $matchesRuntimeCorrelation = $false
                    }

                    if (-not $matchesRuntimeCorrelation) {
                        Start-Sleep -Seconds ([int] [Math]::Max(1, [Math]::Min(2, $remainingSeconds)))
                        continue
                    }
                }

                if ($null -ne $RequiredProcess) {
                    $RequiredProcess.Refresh()
                    if ($RequiredProcess.HasExited) {
                        return $false
                    }
                }
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

function Stop-ProcessSafe {
    param([System.Diagnostics.Process] $Process)

    if ($null -eq $Process) { return }
    if ($Process.HasExited) { return }
    if ($IsWindows) {
        function Stop-ProcessTreeWindows {
            param([int] $ProcessId)

            $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $ProcessId" -ErrorAction SilentlyContinue)
            foreach ($child in $children) {
                Stop-ProcessTreeWindows -ProcessId ([int] $child.ProcessId)
            }
            try {
                Stop-Process -Id $ProcessId -Force -ErrorAction Stop
            }
            catch {
            }
        }

        Stop-ProcessTreeWindows -ProcessId $Process.Id
        return
    }

    Stop-Process -Id $Process.Id -Force
}

function Stop-ContainerSafe {
    param([string] $ContainerName)

    if ([string]::IsNullOrWhiteSpace($ContainerName)) { return }
    & docker stop $ContainerName *> $null
}

function Get-LocalRuntimeResourceEnvironment {
    param(
        [Parameter(Mandatory = $true)] [object] $ResourceEnvelope
    )

    $environmentOverrides = @{}
    if ($null -eq $ResourceEnvelope -or $null -eq $ResourceEnvelope.PSObject) {
        return $environmentOverrides
    }

    if ($ResourceEnvelope.PSObject.Properties.Name -contains 'maxCpuCores') {
        $maxCpuCores = [int] $ResourceEnvelope.maxCpuCores
        if ($maxCpuCores -gt 0) {
            $environmentOverrides['DOTNET_PROCESSOR_COUNT'] = [string] $maxCpuCores
        }
    }

    if ($ResourceEnvelope.PSObject.Properties.Name -contains 'maxMemoryGb') {
        $maxMemoryGb = [int] $ResourceEnvelope.maxMemoryGb
        if ($maxMemoryGb -gt 0) {
            $maxMemoryBytes = [Int64] $maxMemoryGb * 1GB
            $hexMemoryBytes = ('0x{0:x}' -f $maxMemoryBytes)
            $environmentOverrides['DOTNET_GCHeapHardLimit'] = $hexMemoryBytes
            $environmentOverrides['COMPlus_GCHeapHardLimit'] = $hexMemoryBytes
        }
    }

    return $environmentOverrides
}

function Start-LocalRuntime {
    param(
        [Parameter(Mandatory = $true)] [string] $RepositoryRoot,
        [Parameter(Mandatory = $true)] [object] $ResourceEnvelope,
        [Parameter(Mandatory = $true)] [object] $EnvironmentVariables
    )

    $startScript = Join-Path $RepositoryRoot 'monado/helios-control/scripts/Start-HeliosLocal.ps1'
    if (-not (Test-Path -LiteralPath $startScript)) {
        throw "Missing local runtime script: $startScript"
    }

    $stdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ("xcore9-local-runtime-{0}.out.log" -f ([Guid]::NewGuid().ToString('N')))
    $stderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ("xcore9-local-runtime-{0}.err.log" -f ([Guid]::NewGuid().ToString('N')))
    $resourceEnvironment = Get-LocalRuntimeResourceEnvironment -ResourceEnvelope $ResourceEnvelope
    foreach ($property in $EnvironmentVariables.PSObject.Properties) {
        $resourceEnvironment[$property.Name] = [string] $property.Value
    }

    $startProcessParameters = @{
        FilePath = 'pwsh'
        ArgumentList = @('-NoProfile', '-File', $startScript)
        WorkingDirectory = $RepositoryRoot
        PassThru = $true
        RedirectStandardOutput = $stdoutPath
        RedirectStandardError = $stderrPath
    }

    $startProcessCommand = Get-Command Start-Process -ErrorAction Stop
    if ($resourceEnvironment.Count -gt 0 -and $startProcessCommand.Parameters.ContainsKey('Environment')) {
        $startProcessParameters['Environment'] = $resourceEnvironment
        $process = Start-Process @startProcessParameters
    }
    elseif ($resourceEnvironment.Count -gt 0) {
        $previousEnvironment = @{}
        foreach ($entry in $resourceEnvironment.GetEnumerator()) {
            $entryName = [string] $entry.Key
            $previousEnvironment[$entryName] = [Environment]::GetEnvironmentVariable($entryName, 'Process')
            [Environment]::SetEnvironmentVariable($entryName, [string] $entry.Value, 'Process')
        }
        try {
            $process = Start-Process @startProcessParameters
        }
        finally {
            foreach ($entry in $resourceEnvironment.GetEnumerator()) {
                $entryName = [string] $entry.Key
                [Environment]::SetEnvironmentVariable($entryName, $previousEnvironment[$entryName], 'Process')
            }
        }
    }
    else {
        $process = Start-Process @startProcessParameters
    }

    return [ordered]@{
        process = $process
        stdoutPath = $stdoutPath
        stderrPath = $stderrPath
        resourceEnvironment = $resourceEnvironment
    }
}

function Add-LocalRuntimeResourceCheck {
    param(
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [System.Collections.Generic.List[object]] $Checks,
        [Parameter(Mandatory = $true)] [hashtable] $ResourceEnvironment
    )

    if ($ResourceEnvironment.Count -eq 0) {
        Add-SmokeCheck -Checks $Checks -Name 'local-resource-envelope' -Status 'failed' -Detail 'No local runtime resource envelope was applied.'
        return
    }

    $resourceDetail = ($ResourceEnvironment.GetEnumerator() | Sort-Object Name | ForEach-Object { "{0}={1}" -f $_.Key, $_.Value }) -join ', '
    Add-SmokeCheck -Checks $Checks -Name 'local-resource-envelope' -Status 'passed' -Detail ("Applied local runtime resource envelope via process environment: $resourceDetail")
}

function Add-CorrelationEnvironment {
    param(
        [Parameter(Mandatory = $true)] [object] $EnvironmentVariables,
        [Parameter(Mandatory = $true)] [string] $CorrelationId
    )

    $result = [ordered]@{}
    foreach ($property in $EnvironmentVariables.PSObject.Properties) {
        $result[$property.Name] = [string] $property.Value
    }
    if (-not [string]::IsNullOrWhiteSpace($CorrelationId)) {
        $result['HELIOS_CORRELATION_ID'] = $CorrelationId
    }

    return [pscustomobject] $result
}

function Split-HybridResourceEnvelope {
    param(
        [Parameter(Mandatory = $true)] [object] $ResourceEnvelope
    )

    if ($null -eq $ResourceEnvelope -or $null -eq $ResourceEnvelope.PSObject) {
        throw 'Hybrid mode requires a valid resource envelope.'
    }

    $totalCpu = [int] $ResourceEnvelope.maxCpuCores
    $totalMemoryGb = [int] $ResourceEnvelope.maxMemoryGb
    if ($totalCpu -lt 2) {
        throw 'Hybrid mode requires resourceEnvelope.maxCpuCores >= 2 so limits can be split across runtimes.'
    }
    if ($totalMemoryGb -lt 2) {
        throw 'Hybrid mode requires resourceEnvelope.maxMemoryGb >= 2 so limits can be split across runtimes.'
    }

    $localCpu = [int] [Math]::Ceiling($totalCpu / 2.0)
    $dockerCpu = [int] ($totalCpu - $localCpu)
    $localMemoryGb = [int] [Math]::Ceiling($totalMemoryGb / 2.0)
    $dockerMemoryGb = [int] ($totalMemoryGb - $localMemoryGb)

    $localEnvelope = [pscustomobject]@{
        maxCpuCores = $localCpu
        maxMemoryGb = $localMemoryGb
        maxGpuProcesses = [int] $ResourceEnvelope.maxGpuProcesses
        maxConcurrentDeepLearningJobs = [int] $ResourceEnvelope.maxConcurrentDeepLearningJobs
        maxConcurrentAgentRuns = [int] $ResourceEnvelope.maxConcurrentAgentRuns
    }
    $dockerEnvelope = [pscustomobject]@{
        maxCpuCores = $dockerCpu
        maxMemoryGb = $dockerMemoryGb
        maxGpuProcesses = [int] $ResourceEnvelope.maxGpuProcesses
        maxConcurrentDeepLearningJobs = [int] $ResourceEnvelope.maxConcurrentDeepLearningJobs
        maxConcurrentAgentRuns = [int] $ResourceEnvelope.maxConcurrentAgentRuns
    }

    return [ordered]@{
        local = $localEnvelope
        docker = $dockerEnvelope
        summary = "local maxCpuCores=$localCpu maxMemoryGb=$localMemoryGb; docker maxCpuCores=$dockerCpu maxMemoryGb=$dockerMemoryGb; total maxCpuCores=$totalCpu maxMemoryGb=$totalMemoryGb"
    }
}

function Start-DockerRuntime {
    param(
        [Parameter(Mandatory = $true)] [string] $RepositoryRoot,
        [Parameter(Mandatory = $true)] [object] $EnvironmentVariables,
        [Parameter(Mandatory = $true)] [object] $ResourceEnvelope
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
    $dockerRunArgs = @('run', '--detach', '--rm', '--name', $containerName, '--publish', '127.0.0.1:5081:8080')
    if ($null -ne $ResourceEnvelope -and $null -ne $ResourceEnvelope.PSObject) {
        if ($ResourceEnvelope.PSObject.Properties.Name -contains 'maxCpuCores') {
            $requestedCpu = [double] $ResourceEnvelope.maxCpuCores
            $hostCpu = [double] [Environment]::ProcessorCount
            $effectiveCpu = if ($hostCpu -gt 0) { [Math]::Min($requestedCpu, $hostCpu) } else { $requestedCpu }
            if ($effectiveCpu -lt 1) {
                $effectiveCpu = 1
            }
            $dockerRunArgs += @('--cpus', ("{0:0.##}" -f $effectiveCpu))
        }
        if ($ResourceEnvelope.PSObject.Properties.Name -contains 'maxMemoryGb') {
            $requestedMemoryGb = [int] $ResourceEnvelope.maxMemoryGb
            $hostMemoryGb = 0
            if (Test-Path -LiteralPath '/proc/meminfo') {
                $memTotalLine = (Get-Content -LiteralPath '/proc/meminfo' -ErrorAction SilentlyContinue | Select-String -Pattern '^MemTotal:\s+(\d+)\s+kB$' | Select-Object -First 1)
                if ($null -ne $memTotalLine) {
                    $hostMemoryKb = [double] $memTotalLine.Matches[0].Groups[1].Value
                    $hostMemoryGb = [int] [Math]::Floor($hostMemoryKb / 1048576.0)
                }
            }
            elseif ($IsWindows) {
                try {
                    $hostMemoryBytes = [double] (Get-CimInstance -ClassName Win32_ComputerSystem -ErrorAction Stop).TotalPhysicalMemory
                    $hostMemoryGb = [int] [Math]::Floor($hostMemoryBytes / 1GB)
                }
                catch {
                    $hostMemoryGb = 0
                }
            }
            $effectiveMemoryGb = if ($hostMemoryGb -gt 0) { [Math]::Min($requestedMemoryGb, $hostMemoryGb) } else { $requestedMemoryGb }
            if ($effectiveMemoryGb -lt 1) {
                $effectiveMemoryGb = 1
            }
            $dockerRunArgs += @('--memory', ("{0}g" -f $effectiveMemoryGb))
        }
    }
    foreach ($property in $EnvironmentVariables.PSObject.Properties) {
        $dockerRunArgs += '--env'
        $dockerRunArgs += ("{0}={1}" -f $property.Name, [string] $property.Value)
    }
    $dockerRunArgs += $imageTag
    $dockerRunOutput = & docker @dockerRunArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        $joinedOutput = ($dockerRunOutput | ForEach-Object { [string] $_.ToString().Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' | '
        if ([string]::IsNullOrWhiteSpace($joinedOutput)) {
            throw 'Docker run failed.'
        }
        throw ("Docker run failed: " + $joinedOutput)
    }

    return $containerName
}

function Write-SmokeSummary {
    param(
        [Parameter(Mandatory = $true)] [System.Collections.IDictionary] $Result,
        [Parameter(Mandatory = $true)] [string] $SummaryPath
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# Runtime smoke summary: $($Result.mode)")
    $lines.Add('')
    $lines.Add("- Status: " + [string] $Result.status)
    $lines.Add("- Correlation ID: " + [string] $Result.correlationId)
    $lines.Add("- Generated (UTC): " + [string] $Result.generatedAtUtc)
    if ($Result.Contains('evidenceLinks')) {
        $summaryLink = [string] $Result.evidenceLinks.summary
        $dataLink = [string] $Result.evidenceLinks.data
        if (-not [string]::IsNullOrWhiteSpace($summaryLink)) {
            $lines.Add("- Evidence summary: $summaryLink")
        }
        if (-not [string]::IsNullOrWhiteSpace($dataLink)) {
            $lines.Add("- Evidence data: $dataLink")
        }
    }
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
$startupEnvironment = Add-CorrelationEnvironment -EnvironmentVariables $modeConfig.startupContract.requiredEnvironment -CorrelationId $correlationId

try {
    if ($SkipRuntimeStart) {
        Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'skipped' -Detail 'Runtime startup checks were skipped by request.'
    }
    elseif ($Mode -eq 'local-windows') {
        $localRuntime = Start-LocalRuntime -RepositoryRoot $repositoryRoot -ResourceEnvelope $modeConfig.resourceEnvelope -EnvironmentVariables $startupEnvironment
        $localRuntimeStdout = $localRuntime.stdoutPath
        $localRuntimeStderr = $localRuntime.stderrPath
        Add-LocalRuntimeResourceCheck -Checks $checks -ResourceEnvironment $localRuntime.resourceEnvironment
        $healthy = Wait-ForHealthEndpoint -Endpoint ([string] $modeConfig.healthContract.endpoint) -TimeoutSeconds ([int] $modeConfig.healthContract.maxStartupSeconds) -ProbeTimeoutSeconds ([int] $modeConfig.healthContract.probeTimeoutSeconds) -CorrelationId $correlationId -RequiredProcess $localRuntime.process -ExpectedRuntimeCorrelationId $correlationId
        if ($healthy) {
            Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'passed' -Detail "Endpoint $($modeConfig.healthContract.endpoint) returned HTTP 200."
        }
        else {
            $localRuntime.process.Refresh()
            if ($localRuntime.process.HasExited) {
                Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail 'Launched local runtime process exited before readiness could be verified.'
            }
            else {
                Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail "Endpoint $($modeConfig.healthContract.endpoint) did not become healthy with the expected runtime correlation ID in time."
            }
        }
    }
    elseif ($Mode -eq 'local-docker') {
        $containerName = Start-DockerRuntime -RepositoryRoot $repositoryRoot -EnvironmentVariables $startupEnvironment -ResourceEnvelope $modeConfig.resourceEnvelope
        $healthy = Wait-ForHealthEndpoint -Endpoint ([string] $modeConfig.healthContract.endpoint) -TimeoutSeconds ([int] $modeConfig.healthContract.maxStartupSeconds) -ProbeTimeoutSeconds ([int] $modeConfig.healthContract.probeTimeoutSeconds) -CorrelationId $correlationId -ExpectedRuntimeCorrelationId $correlationId
        if ($healthy) {
            Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'passed' -Detail "Endpoint $($modeConfig.healthContract.endpoint) returned HTTP 200."
        }
        else {
            Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail "Endpoint $($modeConfig.healthContract.endpoint) did not become healthy in time."
        }
    }
    else {
        $hybridEnvelope = Split-HybridResourceEnvelope -ResourceEnvelope $modeConfig.resourceEnvelope
        Add-SmokeCheck -Checks $checks -Name 'hybrid-resource-envelope' -Status 'passed' -Detail ("Split hybrid envelope across runtimes: $($hybridEnvelope.summary)")

        $localRuntime = Start-LocalRuntime -RepositoryRoot $repositoryRoot -ResourceEnvelope $hybridEnvelope.local -EnvironmentVariables $startupEnvironment
        $localRuntimeStdout = $localRuntime.stdoutPath
        $localRuntimeStderr = $localRuntime.stderrPath
        Add-LocalRuntimeResourceCheck -Checks $checks -ResourceEnvironment $localRuntime.resourceEnvironment
        $containerName = Start-DockerRuntime -RepositoryRoot $repositoryRoot -EnvironmentVariables $startupEnvironment -ResourceEnvelope $hybridEnvelope.docker

        $endpoints = [string[]] $modeConfig.healthContract.endpoints
        $sharedDeadline = (Get-Date).AddSeconds([int] $modeConfig.healthContract.maxStartupSeconds)
        $healthyWindows = Wait-ForHealthEndpoint -Endpoint $endpoints[0] -DeadlineUtc $sharedDeadline -ProbeTimeoutSeconds ([int] $modeConfig.healthContract.probeTimeoutSeconds) -CorrelationId $correlationId -RequiredProcess $localRuntime.process -ExpectedRuntimeCorrelationId $correlationId
        $healthyDocker = Wait-ForHealthEndpoint -Endpoint $endpoints[1] -DeadlineUtc $sharedDeadline -ProbeTimeoutSeconds ([int] $modeConfig.healthContract.probeTimeoutSeconds) -CorrelationId $correlationId -ExpectedRuntimeCorrelationId $correlationId

        if ($healthyWindows -and $healthyDocker) {
            Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'passed' -Detail 'Both hybrid runtime endpoints returned HTTP 200.'
        }
        else {
            $localRuntime.process.Refresh()
            if ($localRuntime.process.HasExited) {
                Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail 'Launched local runtime process exited before hybrid readiness could be verified.'
            }
            else {
                Add-SmokeCheck -Checks $checks -Name 'runtime-start' -Status 'failed' -Detail 'One or more hybrid runtime endpoints failed health checks with the expected runtime correlation ID.'
            }
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
$runtimeChecks = @($checks | Where-Object { $_.name -eq 'runtime-start' })
$skippedRuntimeChecks = @($runtimeChecks | Where-Object { $_.status -eq 'skipped' })
$status = if ($failedChecks.Count -gt 0) {
    'failed'
}
elseif ($runtimeChecks.Count -gt 0 -and $skippedRuntimeChecks.Count -eq $runtimeChecks.Count) {
    'skipped'
}
else {
    'passed'
}
$result = [ordered]@{
    schemaVersion = 1
    mode = $Mode
    status = $status
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    correlationId = $correlationId
    manifestPath = $manifestReference
    evidenceLinks = [ordered]@{
        summary = [string] $modeConfig.smokeEvidence.summary
        data = [string] $modeConfig.smokeEvidence.data
    }
    checks = @($checks)
    diagnostics = [ordered]@{
        localRuntimeLogsCaptured = ($null -ne $localRuntimeStdout -or $null -ne $localRuntimeStderr)
        localRuntimeLogPathsRedacted = $true
    }
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

$result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $resolvedOutputPath -Encoding utf8
Write-SmokeSummary -Result $result -SummaryPath $resolvedSummaryPath

Write-Output ($result | ConvertTo-Json -Depth 10)
if ($status -eq 'failed') { exit 1 }

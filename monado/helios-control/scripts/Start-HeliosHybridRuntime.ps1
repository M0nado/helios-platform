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

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$startLocalScript = Join-Path $PSScriptRoot 'Start-HeliosLocal.ps1'
if (-not (Test-Path -LiteralPath $startLocalScript)) {
    throw "Missing local runtime script: $startLocalScript"
}

$localCpuCores = 6
$localMemoryGb = 12
$localMemoryHex = ('0x{0:x}' -f ([Int64] $localMemoryGb * 1GB))
$localResourceEnvironment = @{
    DOTNET_PROCESSOR_COUNT = [string] $localCpuCores
    DOTNET_GCHeapHardLimit = $localMemoryHex
    COMPlus_GCHeapHardLimit = $localMemoryHex
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

    $containerName = 'helios-connect-xcore9-hybrid'
    $existingContainerId = [string] ((& docker ps --filter "name=^/$containerName$" --format '{{.ID}}' 2>$null) | Select-Object -First 1)
    if (-not [string]::IsNullOrWhiteSpace($existingContainerId)) {
        if ($env:HELIOS_APPROVE_HYBRID_CONTAINER_REPLACE -ne 'true') {
            throw "Container '$containerName' is already running. Set HELIOS_APPROVE_HYBRID_CONTAINER_REPLACE=true to authorize replacement."
        }
        & docker rm --force $containerName *> $null
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to replace running container '$containerName'."
        }
    }
    $dockerRunOutput = & docker run --detach --rm --name $containerName --publish '127.0.0.1:5081:8080' --cpus '6' --memory '12g' --env 'HELIOS_EXECUTION_MODE=dry-run' --env 'HELIOS_CLOUD_RUNTIME_ONLY=false' --env 'HELIOS_LOCAL_RUNTIME_ALLOWED=true' $imageTag 2>&1
    if ($LASTEXITCODE -ne 0) {
        $joinedOutput = ($dockerRunOutput | ForEach-Object { [string] $_.ToString().Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' | '
        Stop-ProcessTreeSafe -ProcessId $localProcess.Id
        if ([string]::IsNullOrWhiteSpace($joinedOutput)) {
            throw 'Docker run failed.'
        }
        throw ("Docker run failed: " + $joinedOutput)
    }
    $containerId = ([string] ($dockerRunOutput | Select-Object -Last 1)).Trim()

    $result = [ordered]@{
        schemaVersion = 1
        startupMode = 'persistent-hybrid-runtime'
        localRuntimeProcessId = $localProcess.Id
        localRuntimeStdoutLog = $localStdoutPath
        localRuntimeStderrLog = $localStderrPath
        localRuntimeResourceEnvironment = $localResourceEnvironment
        dockerContainerName = $containerName
        dockerContainerId = $containerId
        endpoints = [ordered]@{
            windows = 'http://127.0.0.1:5080/health/ready'
            docker = 'http://127.0.0.1:5081/health/ready'
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
    throw
}

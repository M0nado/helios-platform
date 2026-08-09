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

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$startLocalScript = Join-Path $PSScriptRoot 'Start-HeliosLocal.ps1'
if (-not (Test-Path -LiteralPath $startLocalScript)) {
    throw "Missing local runtime script: $startLocalScript"
}

$localStdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ("xcore9-hybrid-local-{0}.out.log" -f ([Guid]::NewGuid().ToString('N')))
$localStderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ("xcore9-hybrid-local-{0}.err.log" -f ([Guid]::NewGuid().ToString('N')))

$localProcess = Start-Process -FilePath 'pwsh' -ArgumentList @('-NoProfile', '-File', $startLocalScript) -WorkingDirectory $repositoryRoot -PassThru -RedirectStandardOutput $localStdoutPath -RedirectStandardError $localStderrPath

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
    & docker rm --force $containerName *> $null
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

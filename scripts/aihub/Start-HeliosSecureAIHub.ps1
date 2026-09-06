#Requires -Version 7.0
<#
.SYNOPSIS
    Starts the loopback-only HELIOS Secure AIHub runtime as the current user.

.DESCRIPTION
    No administrator rights are required. The launcher creates or reads a
    current-user API token, places it only in the child process environment,
    and starts the hardened queue/proposal-only compatibility API.

    The token is never printed. This script does not deploy Azure resources,
    execute queued tasks, modify machine environment variables, or change
    Windows security policy.
#>
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$ConfigPath,
    [string]$TokenFile = (Join-Path $env:LOCALAPPDATA 'HELIOS\Secrets\aihub.local.token'),
    [switch]$HealthCheck,
    [switch]$InitializeToken
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-HeliosApiToken {
    $bytes = [Security.Cryptography.RandomNumberGenerator]::GetBytes(48)
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function Protect-HeliosTokenFile {
    param([Parameter(Mandatory)][string]$Path)

    if ($IsWindows) {
        $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
        $sid = $identity.User.Value
        $icacls = Join-Path ($env:SystemRoot ?? 'C:\Windows') 'System32\icacls.exe'
        if (Test-Path -LiteralPath $icacls) {
            & $icacls $Path /inheritance:r /grant:r "*$sid:(R,W)" | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "Unable to restrict token-file ACL: $Path"
            }
        }
    }
    elseif (Get-Command chmod -ErrorAction SilentlyContinue) {
        & chmod 600 $Path
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to restrict token-file mode: $Path"
        }
    }
}

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $RepositoryRoot 'config\aihub\secure-runtime.v1.json'
}
if (-not (Test-Path -LiteralPath $ConfigPath)) {
    throw "Secure AIHub configuration not found: $ConfigPath"
}

$python = Get-Command python -ErrorAction SilentlyContinue
if (-not $python) {
    $python = Get-Command py -ErrorAction SilentlyContinue
}
if (-not $python) {
    throw 'Python 3 is required. Use the repository dev container, Codespace, or a current-user Python installation.'
}

$tokenDirectory = Split-Path -Parent $TokenFile
New-Item -ItemType Directory -Path $tokenDirectory -Force | Out-Null

if ($InitializeToken -or -not (Test-Path -LiteralPath $TokenFile)) {
    $token = New-HeliosApiToken
    [IO.File]::WriteAllText($TokenFile, $token, [Text.UTF8Encoding]::new($false))
    Protect-HeliosTokenFile -Path $TokenFile
}

$token = [IO.File]::ReadAllText($TokenFile, [Text.Encoding]::UTF8).Trim()
if ($token.Length -lt 32) {
    throw 'The local AIHub token is invalid. Re-run with -InitializeToken.'
}

$previousToken = $env:AIHUB_API_TOKEN
$previousLocation = Get-Location
try {
    $env:AIHUB_API_TOKEN = $token
    Set-Location -LiteralPath $RepositoryRoot

    $arguments = @('-m', 'python.aihub.secure_runtime', '--config', $ConfigPath)
    if ($HealthCheck) {
        $arguments += '--health-check'
    }

    & $python.Source @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Secure AIHub exited with code $LASTEXITCODE."
    }
}
finally {
    Set-Location -LiteralPath $previousLocation
    if ($null -eq $previousToken) {
        Remove-Item Env:AIHUB_API_TOKEN -ErrorAction SilentlyContinue
    }
    else {
        $env:AIHUB_API_TOKEN = $previousToken
    }
    $token = $null
}

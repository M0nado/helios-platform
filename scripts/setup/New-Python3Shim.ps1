[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $InterpreterPath,
    [Parameter(Mandatory)]
    [string] $OutputPath,
    [switch] $UsePythonLauncher
)

$ErrorActionPreference = 'Stop'
$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

# Percent signs must be doubled when an absolute path is written to a batch file.
$escapedInterpreterPath = $InterpreterPath.Replace('%', '%%')
$pythonArguments = if ($UsePythonLauncher) { ' -3' } else { '' }
Set-Content -Encoding Ascii -Path $OutputPath -Value @(
    '@echo off'
    "`"$escapedInterpreterPath`"$pythonArguments %*"
)

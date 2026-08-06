[CmdletBinding()]
param([Parameter(Mandatory)] [string] $SolutionZip)

$ErrorActionPreference = 'Stop'
pac auth list
pac solution check --path $SolutionZip
atk doctor
atk validate --path "$PSScriptRoot/../appPackage"

Write-Host 'Copilot/Teams package validation complete. Nothing was published.'

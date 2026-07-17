#Requires -Version 7.2
[CmdletBinding()]
param(
    [string]$ConnectionFile = (Join-Path $PSScriptRoot '..\config\connections.json'),
    [switch]$CheckLocalServices,
    [switch]$CheckAzureDevOps,
    [switch]$CheckClaudeMcp
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-CommandStatus {
    param([Parameter(Mandatory)][string]$Name)
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    [ordered]@{
        name = $Name
        installed = [bool]$command
        path = if ($command) { $command.Source } else { $null }
    }
}

function Invoke-HealthCheck {
    param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][string]$Uri)
    try {
        $response = Invoke-RestMethod -Method Get -Uri $Uri -TimeoutSec 4
        [ordered]@{ name = $Name; uri = $Uri; healthy = $true; response = $response }
    } catch {
        [ordered]@{ name = $Name; uri = $Uri; healthy = $false; error = $_.Exception.Message }
    }
}

$config = Get-Content -LiteralPath (Resolve-Path -LiteralPath $ConnectionFile) -Raw | ConvertFrom-Json -Depth 100
$report = [ordered]@{
    timestamp = [DateTimeOffset]::UtcNow.ToString('O')
    environment = $config.environment
    productionEnabled = $config.productionEnabled
    tools = @(
        Get-CommandStatus -Name 'az'
        Get-CommandStatus -Name 'gh'
        Get-CommandStatus -Name 'claude'
        Get-CommandStatus -Name 'npx'
    )
    github = $config.github
    mcpServers = $config.mcpServers
    collaborationDestinations = $config.collaborationDestinations
    localServices = @()
    commandChecks = @()
}

if (Get-Command az -ErrorAction SilentlyContinue) {
    try {
        $account = az account show --query '{subscriptionId:id,subscriptionName:name,tenantId:tenantId,user:user}' --output json | ConvertFrom-Json
        $report.azure = [ordered]@{ authenticated = $true; account = $account }
    } catch {
        $report.azure = [ordered]@{ authenticated = $false; error = $_.Exception.Message }
    }
} else {
    $report.azure = [ordered]@{ authenticated = $false; error = 'Azure CLI is not installed.' }
}

if (Get-Command gh -ErrorAction SilentlyContinue) {
    & gh auth status --hostname github.com *> $null
    $report.githubCli = [ordered]@{ authenticated = ($LASTEXITCODE -eq 0) }
}

if ($CheckClaudeMcp -and (Get-Command claude -ErrorAction SilentlyContinue)) {
    $output = & claude mcp list 2>&1
    $report.commandChecks += [ordered]@{
        command = 'claude mcp list'
        succeeded = ($LASTEXITCODE -eq 0)
        output = $output
    }
}

if ($CheckAzureDevOps -and (Get-Command az -ErrorAction SilentlyContinue)) {
    $output = & az devops project list --output json 2>&1
    $report.commandChecks += [ordered]@{
        command = 'az devops project list'
        succeeded = ($LASTEXITCODE -eq 0)
        output = $output
    }
}

if ($CheckLocalServices) {
    foreach ($property in $config.localServices.PSObject.Properties) {
        $report.localServices += Invoke-HealthCheck -Name $property.Name -Uri ([string]$property.Value)
    }
}

$report | ConvertTo-Json -Depth 100

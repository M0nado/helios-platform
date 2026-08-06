#Requires -Version 7.2
[CmdletBinding()]
param(
    [string]$ConnectionFile = (Join-Path $PSScriptRoot '..\config\connections.json'),
    [ValidatePattern('^[0-9a-fA-F-]{36}$')]
    [string]$SubscriptionId,
    [ValidatePattern('^[0-9a-fA-F-]{36}$')]
    [string]$TenantId,
    [string]$ResourceGroup = 'rg-helios-dev',
    [string]$Repository = 'M0nado/helios-platform',
    [string]$GitHubEnvironment = 'azure-dev',
    [switch]$CheckCloudConnections,
    [switch]$CheckLocalServices,
    [switch]$CheckAzureDevOps,
    [switch]$CheckClaudeMcp
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$CanonicalRepository = 'M0nado/helios-platform'
$CanonicalGitHubEnvironment = 'azure-dev'

if ($Repository -cne $CanonicalRepository) {
    throw "Repository must be the canonical value: $CanonicalRepository"
}
if ($GitHubEnvironment -cne $CanonicalGitHubEnvironment) {
    throw "GitHubEnvironment must be the canonical value: $CanonicalGitHubEnvironment"
}

function Get-CommandStatus {
    param([Parameter(Mandatory)][string]$Name)
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    [ordered]@{
        name = $Name
        installed = [bool]$command
        path = $(if ($command) { $command.Source } else { $null })
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

function Invoke-ReadOnlyJsonCommand {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$ArgumentList
    )
    $output = & $FilePath @ArgumentList 2>&1
    if ($LASTEXITCODE -ne 0) {
        return [ordered]@{
            name = $Name
            healthy = $false
            error = 'Authenticated read-only command failed.'
        }
    }
    try {
        $data = ($output -join [Environment]::NewLine) | ConvertFrom-Json -Depth 100
        [ordered]@{ name = $Name; healthy = $true; data = $data }
    } catch {
        [ordered]@{ name = $Name; healthy = $false; error = 'Read-only command returned invalid JSON.' }
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
    cloudConnections = @()
    commandChecks = @()
}

if ($config.github.repository -cne $CanonicalRepository -or $config.github.environment -cne $CanonicalGitHubEnvironment) {
    throw 'Connection registry GitHub repository/environment does not match the canonical HELIOS OIDC boundary.'
}

if (Get-Command az -ErrorAction SilentlyContinue) {
    try {
        $account = az account show --query '{subscriptionId:id,subscriptionName:name,tenantId:tenantId,user:user}' --output json | ConvertFrom-Json
        $report['azure'] = [ordered]@{ authenticated = $true; account = $account }
    } catch {
        $report['azure'] = [ordered]@{ authenticated = $false; error = $_.Exception.Message }
    }
} else {
    $report['azure'] = [ordered]@{ authenticated = $false; error = 'Azure CLI is not installed.' }
}

if (Get-Command gh -ErrorAction SilentlyContinue) {
    & gh auth status --hostname github.com *> $null
    $report['githubCli'] = [ordered]@{ authenticated = ($LASTEXITCODE -eq 0) }
}

if ($CheckCloudConnections) {
    $azCommand = Get-Command az -ErrorAction SilentlyContinue
    if (-not $azCommand) {
        $report['cloudConnections'] += [ordered]@{ name = 'azure-account'; healthy = $false; error = 'Azure CLI is not installed.' }
    } else {
        $azureAccount = Invoke-ReadOnlyJsonCommand `
            -Name 'azure-account' `
            -FilePath $azCommand.Source `
            -ArgumentList @('account', 'show', '--query', '{subscriptionId:id,tenantId:tenantId,name:name}', '--output', 'json')
        if ($azureAccount.healthy -and $SubscriptionId -and $azureAccount.data.subscriptionId -ine $SubscriptionId) {
            $azureAccount.healthy = $false
            $azureAccount['error'] = 'Authenticated subscription does not match the approved subscription.'
        }
        if ($azureAccount.healthy -and $TenantId -and $azureAccount.data.tenantId -ine $TenantId) {
            $azureAccount.healthy = $false
            $azureAccount['error'] = 'Authenticated tenant does not match the approved tenant.'
        }
        $report['cloudConnections'] += $azureAccount

        if ($azureAccount.healthy) {
            $report['cloudConnections'] += Invoke-ReadOnlyJsonCommand `
                -Name 'azure-resource-group' `
                -FilePath $azCommand.Source `
                -ArgumentList @('group', 'show', '--name', $ResourceGroup, '--query', '{name:name,location:location,id:id}', '--output', 'json')
            $report['cloudConnections'] += Invoke-ReadOnlyJsonCommand `
                -Name 'azure-resource-inventory' `
                -FilePath $azCommand.Source `
                -ArgumentList @('resource', 'list', '--resource-group', $ResourceGroup, '--query', '[].{name:name,type:type,location:location}', '--output', 'json')
        }
    }

    $ghCommand = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $ghCommand) {
        $report['cloudConnections'] += [ordered]@{ name = 'github-repository'; healthy = $false; error = 'GitHub CLI is not installed.' }
    } else {
        & $ghCommand.Source auth status --hostname github.com *> $null
        if ($LASTEXITCODE -ne 0) {
            $report['cloudConnections'] += [ordered]@{ name = 'github-repository'; healthy = $false; error = 'GitHub CLI is not authenticated.' }
        } else {
            $report['cloudConnections'] += Invoke-ReadOnlyJsonCommand `
                -Name 'github-repository' `
                -FilePath $ghCommand.Source `
                -ArgumentList @('repo', 'view', $CanonicalRepository, '--json', 'nameWithOwner,visibility,defaultBranchRef')
            $report['cloudConnections'] += Invoke-ReadOnlyJsonCommand `
                -Name 'github-azure-dev-environment' `
                -FilePath $ghCommand.Source `
                -ArgumentList @('api', "repos/$CanonicalRepository/environments/$CanonicalGitHubEnvironment")
        }
    }
}

if ($CheckClaudeMcp -and (Get-Command claude -ErrorAction SilentlyContinue)) {
    $output = & claude mcp list 2>&1
    $report['commandChecks'] += [ordered]@{
        command = 'claude mcp list'
        succeeded = ($LASTEXITCODE -eq 0)
        output = $output
    }
}

if ($CheckAzureDevOps -and (Get-Command az -ErrorAction SilentlyContinue)) {
    $output = & az devops project list --output json 2>&1
    $report['commandChecks'] += [ordered]@{
        command = 'az devops project list'
        succeeded = ($LASTEXITCODE -eq 0)
        output = $output
    }
}

if ($CheckLocalServices) {
    foreach ($property in $config.localServices.PSObject.Properties) {
        $report['localServices'] += Invoke-HealthCheck -Name $property.Name -Uri ([string]$property.Value)
    }
}

$report | ConvertTo-Json -Depth 100

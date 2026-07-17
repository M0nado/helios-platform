#Requires -Version 7.2
<#
.SYNOPSIS
Connects the HELIOS development control plane to Azure and GitHub OIDC.

.DESCRIPTION
Performs explicit, auditable phases:
1. Tool and authentication preflight.
2. Azure subscription selection.
3. Optional resource-group creation.
4. Optional secretless Entra application and GitHub environment federation.
5. Optional GitHub azure-dev environment/variable configuration.
6. Bicep validation and development what-if.

No client secret is created. No Azure deployment is performed.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-fA-F-]{36}$')]
    [string]$SubscriptionId,

    [ValidatePattern('^[0-9a-fA-F-]{36}$')]
    [string]$TenantId,

    [string]$ResourceGroup = 'rg-helios-dev',
    [string]$Location = 'eastus2',
    [string]$Repository = 'M0nado/helios-platform',
    [string]$GitHubEnvironment = 'azure-dev',
    [string]$EntraAppDisplayName = 'HELIOS GitHub azure-dev',
    [string]$TemplateFile = (Join-Path $PSScriptRoot '..\infra\main.bicep'),
    [string]$EvidenceDirectory = (Join-Path $PSScriptRoot '..\evidence'),

    [switch]$UseDeviceCode,
    [switch]$CreateResourceGroup,
    [switch]$ConfigureOidc,
    [switch]$ConfigureGitHubEnvironment,
    [switch]$RunWhatIf,

    [string]$RequiredReviewerId,

    [string]$ResourceGroupConfirmation,
    [string]$OidcConfirmation,
    [string]$GitHubEnvironmentConfirmation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ExpectedResourceGroupConfirmation = 'CREATE HELIOS AZURE DEV RESOURCE GROUP'
$ExpectedOidcConfirmation = 'CONFIGURE HELIOS AZURE DEV OIDC'
$ExpectedGitHubEnvironmentConfirmation = 'CONFIGURE HELIOS AZURE DEV ENVIRONMENT'
$FederationSubject = "repo:$Repository`:environment:$GitHubEnvironment"
$FederationAudience = 'api://AzureADTokenExchange'

function Require-Command {
    param([Parameter(Mandatory)][string]$Name)
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "Required command is not installed or not on PATH: $Name"
    }
    $command.Source
}

function Invoke-JsonCommand {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$ArgumentList
    )
    $output = & $FilePath @ArgumentList 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed: $($output -join [Environment]::NewLine)"
    }
    $text = $output -join [Environment]::NewLine
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    $text | ConvertFrom-Json -Depth 100
}

function Require-Confirmation {
    param(
        [Parameter(Mandatory)][string]$Actual,
        [Parameter(Mandatory)][string]$Expected
    )
    if ($Actual -cne $Expected) {
        throw "Exact confirmation required: $Expected"
    }
}

$az = Require-Command -Name 'az'
$gh = $null
if ($ConfigureGitHubEnvironment) {
    $gh = Require-Command -Name 'gh'
}

$resolvedTemplate = (Resolve-Path -LiteralPath $TemplateFile).Path
New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
$resolvedEvidence = (Resolve-Path -LiteralPath $EvidenceDirectory).Path

Write-Host 'Checking Azure CLI authentication...'
$account = $null
try {
    $account = Invoke-JsonCommand -FilePath $az -ArgumentList @(
        'account', 'show', '--output', 'json'
    )
} catch {
    $loginArgs = @('login', '--output', 'none')
    if ($UseDeviceCode) { $loginArgs += '--use-device-code' }
    if ($TenantId) { $loginArgs += @('--tenant', $TenantId) }
    & $az @loginArgs
    if ($LASTEXITCODE -ne 0) { throw 'Azure interactive login failed.' }
}

& $az account set --subscription $SubscriptionId
if ($LASTEXITCODE -ne 0) { throw 'Unable to select the approved subscription.' }

$account = Invoke-JsonCommand -FilePath $az -ArgumentList @(
    'account', 'show', '--query', '{subscriptionId:id,subscriptionName:name,tenantId:tenantId,user:user}', '--output', 'json'
)
if ($account.subscriptionId -ne $SubscriptionId) {
    throw 'The active Azure subscription does not match the approved subscription.'
}
if ($TenantId -and $account.tenantId -ne $TenantId) {
    throw 'The active Azure tenant does not match the approved tenant.'
}
$TenantId = [string]$account.tenantId

$groupExists = (& $az group exists --name $ResourceGroup --output tsv).Trim() -eq 'true'
if (-not $groupExists) {
    if (-not $CreateResourceGroup) {
        throw "Resource group '$ResourceGroup' does not exist. Re-run with -CreateResourceGroup and the exact confirmation."
    }
    Require-Confirmation -Actual $ResourceGroupConfirmation -Expected $ExpectedResourceGroupConfirmation
    if ($PSCmdlet.ShouldProcess($ResourceGroup, 'Create Azure development resource group')) {
        & $az group create --name $ResourceGroup --location $Location --tags system=HELIOS environment=dev managedBy=Connect-HeliosAzureDev --output none
        if ($LASTEXITCODE -ne 0) { throw 'Resource-group creation failed.' }
    }
}

$clientId = $null
$appObjectId = $null
$servicePrincipalObjectId = $null

if ($ConfigureOidc) {
    Require-Confirmation -Actual $OidcConfirmation -Expected $ExpectedOidcConfirmation

    $app = Invoke-JsonCommand -FilePath $az -ArgumentList @(
        'ad', 'app', 'list', '--display-name', $EntraAppDisplayName,
        '--query', '[0].{appId:appId,id:id,displayName:displayName}', '--output', 'json'
    )
    if (-not $app) {
        if ($PSCmdlet.ShouldProcess($EntraAppDisplayName, 'Create secretless Entra application')) {
            $app = Invoke-JsonCommand -FilePath $az -ArgumentList @(
                'ad', 'app', 'create', '--display-name', $EntraAppDisplayName,
                '--sign-in-audience', 'AzureADMyOrg',
                '--query', '{appId:appId,id:id,displayName:displayName}', '--output', 'json'
            )
        }
    }
    if (-not $app) { throw 'Unable to resolve the HELIOS Entra application.' }

    $clientId = [string]$app.appId
    $appObjectId = [string]$app.id

    $servicePrincipal = Invoke-JsonCommand -FilePath $az -ArgumentList @(
        'ad', 'sp', 'list', '--filter', "appId eq '$clientId'",
        '--query', '[0].{id:id,appId:appId}', '--output', 'json'
    )
    if (-not $servicePrincipal) {
        if ($PSCmdlet.ShouldProcess($clientId, 'Create Entra service principal')) {
            $servicePrincipal = Invoke-JsonCommand -FilePath $az -ArgumentList @(
                'ad', 'sp', 'create', '--id', $clientId,
                '--query', '{id:id,appId:appId}', '--output', 'json'
            )
        }
    }
    if (-not $servicePrincipal) { throw 'Unable to resolve the HELIOS service principal.' }
    $servicePrincipalObjectId = [string]$servicePrincipal.id

    $credentialName = 'github-azure-dev'
    $existingCredential = Invoke-JsonCommand -FilePath $az -ArgumentList @(
        'ad', 'app', 'federated-credential', 'list', '--id', $appObjectId,
        '--query', "[?name=='$credentialName'] | [0]", '--output', 'json'
    )
    if (-not $existingCredential) {
        $credential = [ordered]@{
            name = $credentialName
            issuer = 'https://token.actions.githubusercontent.com'
            subject = $FederationSubject
            audiences = @($FederationAudience)
            description = 'HELIOS GitHub Actions azure-dev environment'
        }
        $credentialFile = Join-Path $env:TEMP "helios-azure-dev-federation-$([guid]::NewGuid().ToString('N')).json"
        try {
            $credential | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $credentialFile -Encoding utf8NoBOM
            if ($PSCmdlet.ShouldProcess($FederationSubject, 'Create GitHub OIDC federated credential')) {
                & $az ad app federated-credential create --id $appObjectId --parameters "@$credentialFile" --output none
                if ($LASTEXITCODE -ne 0) { throw 'Federated credential creation failed.' }
            }
        } finally {
            Remove-Item -LiteralPath $credentialFile -Force -ErrorAction SilentlyContinue
        }
    }
}

if ($ConfigureGitHubEnvironment) {
    Require-Confirmation -Actual $GitHubEnvironmentConfirmation -Expected $ExpectedGitHubEnvironmentConfirmation
    if (-not $clientId) {
        $app = Invoke-JsonCommand -FilePath $az -ArgumentList @(
            'ad', 'app', 'list', '--display-name', $EntraAppDisplayName,
            '--query', '[0].{appId:appId,id:id}', '--output', 'json'
        )
        if (-not $app) { throw 'Configure OIDC first or provide an existing HELIOS Entra application.' }
        $clientId = [string]$app.appId
    }

    & $gh auth status --hostname github.com
    if ($LASTEXITCODE -ne 0) { throw 'GitHub CLI is not authenticated.' }

    $environmentPayload = [ordered]@{
        wait_timer = 0
        prevent_self_review = $true
        deployment_branch_policy = [ordered]@{
            protected_branches = $true
            custom_branch_policies = $false
        }
    }
    if ($RequiredReviewerId) {
        $environmentPayload.reviewers = @(
            [ordered]@{ type = 'User'; id = [int64]$RequiredReviewerId }
        )
    }
    $environmentFile = Join-Path $env:TEMP "helios-github-environment-$([guid]::NewGuid().ToString('N')).json"
    try {
        $environmentPayload | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $environmentFile -Encoding utf8NoBOM
        if ($PSCmdlet.ShouldProcess("$Repository/$GitHubEnvironment", 'Create or update protected GitHub environment')) {
            Get-Content -LiteralPath $environmentFile -Raw | & $gh api --method PUT "repos/$Repository/environments/$GitHubEnvironment" --input - | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'GitHub environment configuration failed.' }
        }
    } finally {
        Remove-Item -LiteralPath $environmentFile -Force -ErrorAction SilentlyContinue
    }

    $variables = [ordered]@{
        AZURE_CLIENT_ID = $clientId
        AZURE_TENANT_ID = $TenantId
        AZURE_SUBSCRIPTION_ID = $SubscriptionId
        AZURE_RESOURCE_GROUP = $ResourceGroup
    }
    foreach ($entry in $variables.GetEnumerator()) {
        & $gh variable set $entry.Key --repo $Repository --env $GitHubEnvironment --body ([string]$entry.Value)
        if ($LASTEXITCODE -ne 0) { throw "Unable to set GitHub environment variable: $($entry.Key)" }
    }
}

& $az bicep build --file $resolvedTemplate --stdout | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Bicep compilation failed.' }

& $az deployment group validate `
    --resource-group $ResourceGroup `
    --name 'helios-dev-live-connect-validate' `
    --template-file $resolvedTemplate `
    --parameters environmentName=dev `
    --mode Incremental `
    --validation-level ProviderNoRbac `
    --output json | Set-Content -LiteralPath (Join-Path $resolvedEvidence 'validation.json') -Encoding utf8NoBOM
if ($LASTEXITCODE -ne 0) { throw 'Azure deployment validation failed.' }

if ($RunWhatIf) {
    $whatIfPath = Join-Path $resolvedEvidence 'what-if.json'
    & $az deployment group what-if `
        --resource-group $ResourceGroup `
        --name 'helios-dev-live-connect' `
        --template-file $resolvedTemplate `
        --parameters environmentName=dev `
        --mode Incremental `
        --no-prompt true `
        --result-format FullResourcePayloads `
        --no-pretty-print `
        --output json | Set-Content -LiteralPath $whatIfPath -Encoding utf8NoBOM
    if ($LASTEXITCODE -ne 0) { throw 'Azure development what-if failed.' }
}

$summary = [ordered]@{
    connected = $true
    deploymentPerformed = $false
    subscriptionId = $SubscriptionId
    tenantId = $TenantId
    resourceGroup = $ResourceGroup
    location = $Location
    repository = $Repository
    githubEnvironment = $GitHubEnvironment
    federationSubject = $FederationSubject
    clientId = $clientId
    servicePrincipalObjectId = $servicePrincipalObjectId
    validationEvidence = (Join-Path $resolvedEvidence 'validation.json')
    whatIfEvidence = if ($RunWhatIf) { Join-Path $resolvedEvidence 'what-if.json' } else { $null }
}
$summary | ConvertTo-Json -Depth 10

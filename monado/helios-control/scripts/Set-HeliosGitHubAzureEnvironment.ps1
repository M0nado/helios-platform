#requires -Version 7.0

<#
.SYNOPSIS
Configures HELIOS Azure GitHub environments and required non-secret variables.

.DESCRIPTION
Bootstraps azure-dev, azure-test, or azure-prod with reviewer protection and
the non-secret variables required by .github/workflows/helios-cloud-deploy.yml.
This script does not authenticate to Azure and does not mutate Azure resources.

.EXAMPLE
pwsh -NoProfile -File ./scripts/Set-HeliosGitHubAzureEnvironment.ps1 `
  -Mode validate `
  -TargetEnvironment azure-dev `
  -RequiredReviewerId 12345678 `
  -AzureClientId 00000000-0000-0000-0000-000000000000 `
  -AzureTenantId 11111111-1111-1111-1111-111111111111 `
  -AzureSubscriptionId 22222222-2222-2222-2222-222222222222 `
  -AzureResourceGroup rg-helios-dev `
  -HeliosEntraClientId 33333333-3333-3333-3333-333333333333 `
  -HeliosAllowedPrincipalObjectId 44444444-4444-4444-4444-444444444444 `
  -HeliosContainerRegistryName heliosdev12345

.EXAMPLE
pwsh -NoProfile -File ./scripts/Set-HeliosGitHubAzureEnvironment.ps1 `
  -Mode apply `
  -TargetEnvironment all `
  -RequiredReviewerId 12345678 `
  -AzureClientId 00000000-0000-0000-0000-000000000000 `
  -AzureTenantId 11111111-1111-1111-1111-111111111111 `
  -AzureSubscriptionId 22222222-2222-2222-2222-222222222222 `
  -AzureResourceGroup rg-helios-dev `
  -HeliosEntraClientId 33333333-3333-3333-3333-333333333333 `
  -HeliosAllowedPrincipalObjectId 44444444-4444-4444-4444-444444444444 `
  -HeliosContainerRegistryName heliosdev12345 `
  -HeliosAzureConnectorUrl https://helios-dev.example.com `
  -AzureDevOpsOrganization helios-org
#>

[CmdletBinding()]
param(
    [ValidateSet('validate', 'apply')]
    [string]$Mode = 'validate',

    [ValidateSet('azure-dev', 'azure-test', 'azure-prod', 'all')]
    [string]$TargetEnvironment = 'azure-dev',

    [string]$Repository = 'M0nado/helios-platform',

    [Parameter(Mandatory)]
    [string]$RequiredReviewerId,

    [Parameter(Mandatory)]
    [string]$AzureClientId,

    [Parameter(Mandatory)]
    [string]$AzureTenantId,

    [Parameter(Mandatory)]
    [string]$AzureSubscriptionId,

    [Parameter(Mandatory)]
    [string]$AzureResourceGroup,

    [Parameter(Mandatory)]
    [string]$HeliosEntraClientId,

    [Parameter(Mandatory)]
    [string]$HeliosAllowedPrincipalObjectId,

    [Parameter(Mandatory)]
    [string]$HeliosContainerRegistryName,

    [string]$HeliosAzureConnectorUrl,

    [string]$AzureDevOpsOrganization
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-GuidValue {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Value
    )

    $parsed = [guid]::Empty
    if (-not [guid]::TryParse($Value, [ref]$parsed)) {
        throw "$Name must be a GUID."
    }
}

function Assert-RepositoryValue {
    param([Parameter(Mandatory)][string]$Value)
    if ($Value -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
        throw 'Repository must use owner/name format.'
    }
}

function Assert-OptionalHttpsOrigin {
    param(
        [Parameter(Mandatory)][string]$Name,
        [AllowEmptyString()][string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    [uri]$parsed = $null
    $isValid = [uri]::TryCreate($Value, [System.UriKind]::Absolute, [ref]$parsed) -and
        $parsed.Scheme -eq 'https' -and
        [string]::IsNullOrWhiteSpace($parsed.Query) -and
        [string]::IsNullOrWhiteSpace($parsed.Fragment) -and
        ($parsed.AbsolutePath -eq '/' -or [string]::IsNullOrWhiteSpace($parsed.AbsolutePath))

    if (-not $isValid) {
        throw "$Name must be an HTTPS origin without query, fragment, or path."
    }
}

function Assert-OptionalAzureDevOpsOrganization {
    param(
        [Parameter(Mandatory)][string]$Name,
        [AllowEmptyString()][string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    if ($Value -notmatch '^[A-Za-z0-9][A-Za-z0-9-]{1,99}$') {
        throw "$Name must match Azure DevOps organization naming rules."
    }
}

function Invoke-Gh {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$Operation
    )

    $output = & $script:GhPath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        $detail = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine
        if ([string]::IsNullOrWhiteSpace($detail)) {
            $detail = 'No diagnostic text was returned.'
        }
        throw "$Operation failed: $detail"
    }
    return $output
}

function Set-GitHubEnvironmentProtection {
    param(
        [Parameter(Mandatory)][string]$RepositoryName,
        [Parameter(Mandatory)][string]$EnvironmentName,
        [Parameter(Mandatory)][int64]$ReviewerId
    )

    $payload = [ordered]@{
        wait_timer = 0
        prevent_self_review = $true
        reviewers = @(
            [ordered]@{
                type = 'User'
                id = $ReviewerId
            }
        )
        deployment_branch_policy = [ordered]@{
            protected_branches = $true
            custom_branch_policies = $false
        }
    }

    $tempFile = Join-Path $env:TEMP "helios-github-environment-$([guid]::NewGuid().ToString('N')).json"
    try {
        [System.IO.File]::WriteAllText(
            $tempFile,
            ($payload | ConvertTo-Json -Depth 10 -Compress),
            [System.Text.UTF8Encoding]::new($false)
        )

        [void] (Invoke-Gh `
            -Arguments @(
                'api',
                '--method',
                'PUT',
                '--header',
                'X-GitHub-Api-Version: 2022-11-28',
                "repos/$RepositoryName/environments/$EnvironmentName",
                '--input',
                $tempFile
            ) `
            -Operation "Configuring protected GitHub environment '$EnvironmentName'")
    }
    finally {
        Remove-Item -LiteralPath $tempFile -Force -ErrorAction SilentlyContinue
    }
}

function Set-GitHubEnvironmentVariable {
    param(
        [Parameter(Mandatory)][string]$RepositoryName,
        [Parameter(Mandatory)][string]$EnvironmentName,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Value
    )

    [void] (Invoke-Gh `
        -Arguments @(
            'variable',
            'set',
            $Name,
            '--repo',
            $RepositoryName,
            '--env',
            $EnvironmentName,
            '--body',
            $Value
        ) `
        -Operation "Setting GitHub variable '$Name' in '$EnvironmentName'")
}

Assert-RepositoryValue -Value $Repository

[int64]$reviewerId = 0
if (-not [int64]::TryParse($RequiredReviewerId, [ref]$reviewerId) -or $reviewerId -le 0) {
    throw 'RequiredReviewerId must be a positive numeric GitHub user ID.'
}

Assert-GuidValue -Name 'AZURE_CLIENT_ID' -Value $AzureClientId
Assert-GuidValue -Name 'AZURE_TENANT_ID' -Value $AzureTenantId
Assert-GuidValue -Name 'AZURE_SUBSCRIPTION_ID' -Value $AzureSubscriptionId
Assert-GuidValue -Name 'HELIOS_ENTRA_CLIENT_ID' -Value $HeliosEntraClientId
Assert-GuidValue -Name 'HELIOS_ALLOWED_PRINCIPAL_OBJECT_ID' -Value $HeliosAllowedPrincipalObjectId

if ($HeliosContainerRegistryName -notmatch '^[a-z0-9]{5,50}$') {
    throw 'HELIOS_CONTAINER_REGISTRY_NAME must be a lowercase Azure Container Registry resource name.'
}

if ([string]::IsNullOrWhiteSpace($AzureResourceGroup)) {
    throw 'AZURE_RESOURCE_GROUP is required.'
}

Assert-OptionalHttpsOrigin -Name 'HELIOS_AZURE_CONNECTOR_URL' -Value $HeliosAzureConnectorUrl
Assert-OptionalAzureDevOpsOrganization -Name 'AZURE_DEVOPS_ORGANIZATION' -Value $AzureDevOpsOrganization

$gh = Get-Command gh -CommandType Application -ErrorAction SilentlyContinue
if ($null -eq $gh) {
    throw 'GitHub CLI (gh) is required.'
}
$script:GhPath = $gh.Source

[void] (Invoke-Gh `
    -Arguments @('auth', 'status', '--hostname', 'github.com') `
    -Operation 'Checking GitHub authentication')

[void] (Invoke-Gh `
    -Arguments @(
        'api',
        '--method',
        'GET',
        '--header',
        'X-GitHub-Api-Version: 2022-11-28',
        "repos/$Repository"
    ) `
    -Operation "Reading repository '$Repository'")

$targetEnvironments = if ($TargetEnvironment -eq 'all') {
    @('azure-dev', 'azure-test', 'azure-prod')
}
else {
    @($TargetEnvironment)
}

$requiredVariables = [ordered]@{
    AZURE_CLIENT_ID = $AzureClientId
    AZURE_TENANT_ID = $AzureTenantId
    AZURE_SUBSCRIPTION_ID = $AzureSubscriptionId
    AZURE_RESOURCE_GROUP = $AzureResourceGroup
    HELIOS_ENTRA_CLIENT_ID = $HeliosEntraClientId
    HELIOS_ALLOWED_PRINCIPAL_OBJECT_ID = $HeliosAllowedPrincipalObjectId
    HELIOS_CONTAINER_REGISTRY_NAME = $HeliosContainerRegistryName
}

$optionalVariables = [ordered]@{}
if (-not [string]::IsNullOrWhiteSpace($HeliosAzureConnectorUrl)) {
    $optionalVariables['HELIOS_AZURE_CONNECTOR_URL'] = $HeliosAzureConnectorUrl.TrimEnd('/')
}
if (-not [string]::IsNullOrWhiteSpace($AzureDevOpsOrganization)) {
    $optionalVariables['AZURE_DEVOPS_ORGANIZATION'] = $AzureDevOpsOrganization
}

$results = [System.Collections.Generic.List[object]]::new()

foreach ($environmentName in $targetEnvironments) {
    if ($Mode -eq 'apply') {
        Set-GitHubEnvironmentProtection `
            -RepositoryName $Repository `
            -EnvironmentName $environmentName `
            -ReviewerId $reviewerId

        foreach ($entry in $requiredVariables.GetEnumerator()) {
            Set-GitHubEnvironmentVariable `
                -RepositoryName $Repository `
                -EnvironmentName $environmentName `
                -Name ([string]$entry.Key) `
                -Value ([string]$entry.Value)
        }

        foreach ($entry in $optionalVariables.GetEnumerator()) {
            Set-GitHubEnvironmentVariable `
                -RepositoryName $Repository `
                -EnvironmentName $environmentName `
                -Name ([string]$entry.Key) `
                -Value ([string]$entry.Value)
        }
    }

    $results.Add([pscustomobject][ordered]@{
            environment = $environmentName
            mode = $Mode
            requiredVariables = @($requiredVariables.Keys)
            optionalVariables = @($optionalVariables.Keys)
            reviewer = [pscustomobject][ordered]@{
                requiredReviewerId = $reviewerId
                preventSelfReview = $true
            }
            protectedBranchesOnly = $true
        })
}

[pscustomobject][ordered]@{
    repository = $Repository
    mode = $Mode
    targetEnvironment = $TargetEnvironment
    environments = $results
    notes = if ($Mode -eq 'apply') {
        @('GitHub environments and variables were updated.')
    }
    else {
        @('Validation completed.', 'No GitHub settings were changed.')
    }
} | ConvertTo-Json -Depth 10

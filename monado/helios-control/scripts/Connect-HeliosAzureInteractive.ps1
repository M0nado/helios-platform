#requires -Version 7.0

<#
.SYNOPSIS
Interactively plans, configures, or publishes the cloud-only Helios Azure connector.

.DESCRIPTION
Plan is the default and makes no cloud resource changes. Configure creates only
explicitly confirmed Entra/OIDC/RBAC/GitHub bindings and pre-registers the exact
Azure providers the runtime needs. Publish performs an ACR cloud build without
a local Docker daemon, grants the runtime identity pull-only access, and returns
an immutable digest. Application deployment is intentionally available only
through the two-stage protected GitHub workflow.

.EXAMPLE
./scripts/Connect-HeliosAzureInteractive.ps1 -UseDeviceCode

.EXAMPLE
./scripts/Connect-HeliosAzureInteractive.ps1 -Mode Configure -EnvironmentName dev -ContainerRegistryName heliosdev12345 -RequiredReviewerId 12345678

.EXAMPLE
./scripts/Connect-HeliosAzureInteractive.ps1 -Mode Publish -EnvironmentName dev -ContainerRegistryName heliosdev12345
#>

[CmdletBinding()]
param(
    [ValidateSet('Plan', 'Configure', 'Publish')]
    [string] $Mode = 'Plan',

    [ValidateSet('dev', 'test', 'preview', 'prod')]
    [string] $EnvironmentName = 'dev',

    [string] $TenantId,
    [string] $SubscriptionId,
    [string] $ResourceGroup,
    [string] $Location = 'eastus2',
    [string] $ImageReference,
    [string] $ContainerRegistryName,
    [string] $ImageRepository = 'helios-connect',
    [string] $ImageTag,
    [string] $BuildContext,
    [string] $DockerfilePath = 'src/Helios.Connect.Api/Dockerfile',
    [string] $AllowedPrincipalObjectId,

    [string] $GitHubOwner = 'M0nado',
    [string] $GitHubRepository = 'helios-platform',
    [string] $GitHubEnvironment,
    [string] $GitHubDeploymentBranch = 'main',
    [string] $RequiredReviewerId,
    [string] $ConnectorAppName = 'helios-azure-connector',
    [string] $GitHubOidcAppName = 'helios-github-oidc',

    [switch] $UseDeviceCode,
    [switch] $ForceLogin
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:AzPath = $null
$script:GhPath = $null
$script:PlaceholderClientId = '00000000-0000-0000-0000-000000000000'
$script:PlaceholderDigest = 'sha256:0000000000000000000000000000000000000000000000000000000000000000'
$script:ContributorRole = [pscustomobject]@{ Name = 'Contributor'; Id = 'b24988ac-6180-42a0-ab88-20f7382dd24c' }
$script:ReaderRole = [pscustomobject]@{ Name = 'Reader'; Id = 'acdd72a7-3385-48ef-bd42-f606fba81ae7' }
$script:RequiredProviders = @(
    'Microsoft.App',
    'Microsoft.ContainerRegistry',
    'Microsoft.Insights',
    'Microsoft.KeyVault',
    'Microsoft.ManagedIdentity',
    'Microsoft.OperationalInsights'
)

function Protect-DiagnosticText {
    param([AllowEmptyString()] [string] $Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return ''
    }

    # Azure CLI and GitHub CLI errors should not contain credentials for the
    # commands used here. Redact token-shaped data anyway before surfacing it.
    $protected = $Text
    $protected = $protected -replace '(?i)(authorization\s*:\s*bearer\s+)[^\s]+', '$1[REDACTED]'
    $protected = $protected -replace '(?i)(access[_-]?token|client[_-]?secret|api[_-]?key)(\s*[=:]\s*)[^\s,;]+', '$1$2[REDACTED]'
    $protected = $protected -replace 'eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+', '[REDACTED_JWT]'
    return $protected.Trim()
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)] [string] $FilePath,
        [Parameter(Mandatory)] [string[]] $ArgumentList,
        [string] $Operation = 'command',
        [switch] $AllowEmptyOutput
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    foreach ($argument in $ArgumentList) {
        [void] $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw "Unable to start $Operation."
        }

        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = $stdoutTask.GetAwaiter().GetResult()
        $stderr = $stderrTask.GetAwaiter().GetResult()

        if ($process.ExitCode -ne 0) {
            $safeError = Protect-DiagnosticText $stderr
            if ([string]::IsNullOrWhiteSpace($safeError)) {
                $safeError = 'The CLI did not return diagnostic text.'
            }
            throw "$Operation failed with exit code $($process.ExitCode): $safeError"
        }

        if (-not $AllowEmptyOutput -and [string]::IsNullOrWhiteSpace($stdout)) {
            throw "$Operation succeeded but returned no output."
        }

        return $stdout
    }
    finally {
        $process.Dispose()
    }
}

function Invoke-AzJson {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [string] $Operation = 'Azure CLI operation',
        [switch] $AllowEmptyOutput
    )

    $argumentsWithOutput = [System.Collections.Generic.List[string]]::new()
    foreach ($argument in $Arguments) {
        [void] $argumentsWithOutput.Add($argument)
    }
    [void] $argumentsWithOutput.Add('--only-show-errors')
    [void] $argumentsWithOutput.Add('--output')
    [void] $argumentsWithOutput.Add('json')

    $raw = Invoke-NativeCommand `
        -FilePath $script:AzPath `
        -ArgumentList $argumentsWithOutput.ToArray() `
        -Operation $Operation `
        -AllowEmptyOutput:$AllowEmptyOutput

    if ([string]::IsNullOrWhiteSpace($raw)) {
        return $null
    }

    try {
        return $raw | ConvertFrom-Json -Depth 100
    }
    catch {
        throw "$Operation returned malformed JSON. No response content was printed."
    }
}

function Invoke-AzNoOutput {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [string] $Operation = 'Azure CLI operation'
    )

    $argumentsWithOutput = [System.Collections.Generic.List[string]]::new()
    foreach ($argument in $Arguments) {
        [void] $argumentsWithOutput.Add($argument)
    }
    [void] $argumentsWithOutput.Add('--only-show-errors')
    [void] $argumentsWithOutput.Add('--output')
    [void] $argumentsWithOutput.Add('none')

    [void] (Invoke-NativeCommand `
        -FilePath $script:AzPath `
        -ArgumentList $argumentsWithOutput.ToArray() `
        -Operation $Operation `
        -AllowEmptyOutput)
}

function Invoke-GhNoOutput {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [string] $Operation = 'GitHub CLI operation'
    )

    [void] (Invoke-NativeCommand `
        -FilePath $script:GhPath `
        -ArgumentList $Arguments `
        -Operation $Operation `
        -AllowEmptyOutput)
}

function Invoke-GhJson {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [string] $Operation = 'GitHub CLI operation'
    )

    $raw = Invoke-NativeCommand `
        -FilePath $script:GhPath `
        -ArgumentList $Arguments `
        -Operation $Operation
    try {
        return $raw | ConvertFrom-Json -Depth 100
    }
    catch {
        throw "$Operation returned malformed JSON. No response content was printed."
    }
}

function Read-RequiredValue {
    param(
        [Parameter(Mandatory)] [string] $Prompt,
        [string] $CurrentValue
    )

    if (-not [string]::IsNullOrWhiteSpace($CurrentValue)) {
        return $CurrentValue.Trim()
    }

    while ($true) {
        $value = (Read-Host $Prompt).Trim()
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
        Write-Warning 'A non-empty value is required.'
    }
}

function Test-GuidValue {
    param([AllowEmptyString()] [string] $Value)

    $parsed = [guid]::Empty
    return [guid]::TryParse($Value, [ref] $parsed)
}

function Read-Selection {
    param(
        [Parameter(Mandatory)] [object[]] $Items,
        [Parameter(Mandatory)] [scriptblock] $Label,
        [Parameter(Mandatory)] [string] $Prompt
    )

    if ($Items.Count -eq 0) {
        throw "No choices are available for $Prompt."
    }

    if ($Items.Count -eq 1) {
        $onlyLabel = & $Label $Items[0]
        Write-Host "Selected: $onlyLabel"
        return $Items[0]
    }

    for ($index = 0; $index -lt $Items.Count; $index++) {
        $itemLabel = & $Label $Items[$index]
        Write-Host ('  [{0}] {1}' -f ($index + 1), $itemLabel)
    }

    while ($true) {
        $answer = Read-Host "$Prompt [1-$($Items.Count)]"
        $choice = 0
        if ([int]::TryParse($answer, [ref] $choice) -and $choice -ge 1 -and $choice -le $Items.Count) {
            return $Items[$choice - 1]
        }
        Write-Warning 'Enter one of the listed numbers.'
    }
}

function Assert-ExactConfirmation {
    param(
        [Parameter(Mandatory)] [string] $Expected,
        [Parameter(Mandatory)] [string] $Purpose
    )

    Write-Warning "$Purpose is a cloud mutation."
    $answer = Read-Host "Type exactly '$Expected' to continue"
    if (-not [string]::Equals($answer, $Expected, [StringComparison]::Ordinal)) {
        throw "$Purpose was not confirmed. No mutation from that step was performed."
    }
}

function Test-AzSession {
    try {
        $account = Invoke-AzJson -Arguments @('account', 'show') -Operation 'Checking the Azure CLI session'
        return ($null -ne $account)
    }
    catch {
        return $false
    }
}

function Connect-AzureInteractively {
    if (-not $ForceLogin -and (Test-AzSession)) {
        Write-Host 'Using the existing Azure CLI session. Use -ForceLogin to choose a different identity.'
        return
    }

    $arguments = [System.Collections.Generic.List[string]]::new()
    [void] $arguments.Add('login')
    [void] $arguments.Add('--allow-no-subscriptions')
    if (-not [string]::IsNullOrWhiteSpace($TenantId)) {
        [void] $arguments.Add('--tenant')
        [void] $arguments.Add($TenantId)
    }
    if ($UseDeviceCode) {
        [void] $arguments.Add('--use-device-code')
    }
    [void] $arguments.Add('--only-show-errors')
    [void] $arguments.Add('--output')
    [void] $arguments.Add('none')

    Write-Host 'Starting Azure CLI authentication. Azure CLI manages its normal auth cache; this wizard never requests, exports, or prints raw tokens.'
    $loginArguments = $arguments.ToArray()
    & $script:AzPath @loginArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI authentication failed with exit code $LASTEXITCODE."
    }
}

function Select-AzureAccount {
    $accounts = @(Invoke-AzJson -Arguments @('account', 'list', '--all') -Operation 'Enumerating Azure tenants and subscriptions')
    $accounts = @($accounts | Where-Object { $_.state -eq 'Enabled' -and -not [string]::IsNullOrWhiteSpace([string] $_.id) })
    if ($accounts.Count -eq 0) {
        throw 'The authenticated identity has no enabled Azure subscriptions.'
    }

    $tenantIds = @($accounts | Select-Object -ExpandProperty tenantId -Unique | Sort-Object)
    if (-not [string]::IsNullOrWhiteSpace($TenantId)) {
        $tenantIds = @($tenantIds | Where-Object { $_ -eq $TenantId })
        if ($tenantIds.Count -eq 0) {
            throw "Tenant '$TenantId' is not present in the authenticated Azure CLI account list."
        }
    }

    $tenantItems = @($tenantIds | ForEach-Object {
        $tenantIdValue = $_
        $example = $accounts | Where-Object tenantId -eq $tenantIdValue | Select-Object -First 1
        $tenantDisplayProperty = $example.PSObject.Properties['tenantDisplayName']
        [pscustomobject]@{
            Id = $tenantIdValue
            DisplayName = if ($tenantDisplayProperty -and $tenantDisplayProperty.Value) { [string] $tenantDisplayProperty.Value } else { 'Azure tenant' }
        }
    })
    $selectedTenant = Read-Selection `
        -Items $tenantItems `
        -Label { param($item) "$($item.DisplayName) ($($item.Id))" } `
        -Prompt 'Select an Azure tenant'

    $subscriptions = @($accounts | Where-Object tenantId -eq $selectedTenant.Id | Sort-Object name)
    if (-not [string]::IsNullOrWhiteSpace($SubscriptionId)) {
        $subscriptions = @($subscriptions | Where-Object id -eq $SubscriptionId)
        if ($subscriptions.Count -eq 0) {
            throw "Subscription '$SubscriptionId' is not enabled in tenant '$($selectedTenant.Id)'."
        }
    }
    $selectedSubscription = Read-Selection `
        -Items $subscriptions `
        -Label { param($item) "$($item.name) ($($item.id))" } `
        -Prompt 'Select an Azure subscription'

    Invoke-AzNoOutput `
        -Arguments @('account', 'set', '--subscription', [string] $selectedSubscription.id) `
        -Operation 'Selecting the Azure subscription'

    $selectedContext = Invoke-AzJson -Arguments @('account', 'show') -Operation 'Verifying the selected Azure subscription'
    if ($selectedContext.id -ne $selectedSubscription.id -or $selectedContext.tenantId -ne $selectedTenant.Id) {
        throw 'Azure CLI did not retain the selected tenant/subscription context.'
    }

    return [pscustomobject]@{
        TenantId = [string] $selectedTenant.Id
        SubscriptionId = [string] $selectedSubscription.id
        SubscriptionName = [string] $selectedSubscription.name
    }
}

function Select-ResourceGroup {
    param([Parameter(Mandatory)] [pscustomobject] $Context)

    $groups = @(Invoke-AzJson `
        -Arguments @('group', 'list', '--subscription', $Context.SubscriptionId) `
        -Operation 'Enumerating Azure resource groups')
    $groups = @($groups | Sort-Object name)

    if (-not [string]::IsNullOrWhiteSpace($ResourceGroup)) {
        $existing = $groups | Where-Object name -eq $ResourceGroup | Select-Object -First 1
        if ($existing) {
            return $existing
        }

        if ($Mode -notin @('Configure', 'Publish')) {
            throw "Resource group '$ResourceGroup' does not exist. Creation is only allowed in -Mode Configure or -Mode Publish."
        }
        return New-ConfirmedResourceGroup -Context $Context -Name $ResourceGroup -LocationName $Location
    }

    if ($groups.Count -gt 0) {
        Write-Host 'Available resource groups:'
        for ($index = 0; $index -lt $groups.Count; $index++) {
            Write-Host ('  [{0}] {1} ({2})' -f ($index + 1), $groups[$index].name, $groups[$index].location)
        }
        if ($Mode -in @('Configure', 'Publish')) {
            Write-Host '  [N] Create a new resource group'
        }

        while ($true) {
            $answer = Read-Host 'Select a resource group'
            if ($Mode -in @('Configure', 'Publish') -and $answer -ceq 'N') {
                $newName = Read-RequiredValue -Prompt 'New resource group name'
                $newLocation = Read-RequiredValue -Prompt 'Azure location' -CurrentValue $Location
                return New-ConfirmedResourceGroup -Context $Context -Name $newName -LocationName $newLocation
            }
            $choice = 0
            if ([int]::TryParse($answer, [ref] $choice) -and $choice -ge 1 -and $choice -le $groups.Count) {
                return $groups[$choice - 1]
            }
            Write-Warning 'Enter one of the listed numbers.'
        }
    }

    if ($Mode -notin @('Configure', 'Publish')) {
        throw 'No resource groups are available. Run -Mode Configure or -Mode Publish to explicitly create one.'
    }

    $newGroupName = Read-RequiredValue -Prompt 'New resource group name'
    $newGroupLocation = Read-RequiredValue -Prompt 'Azure location' -CurrentValue $Location
    return New-ConfirmedResourceGroup -Context $Context -Name $newGroupName -LocationName $newGroupLocation
}

function New-ConfirmedResourceGroup {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Context,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $LocationName
    )

    if ($Mode -notin @('Configure', 'Publish')) {
        throw 'Resource-group creation is not available in this mode.'
    }
    if ($Name.Length -gt 90 -or $Name.EndsWith('.', [StringComparison]::Ordinal) -or $Name -notmatch '^[A-Za-z0-9._()\-]+$') {
        throw 'Resource-group name is invalid. Use up to 90 supported characters and do not end with a period.'
    }
    Assert-ExactConfirmation `
        -Expected "CREATE RESOURCE GROUP $Name" `
        -Purpose "Creating Azure resource group '$Name'"

    $created = Invoke-AzJson `
        -Arguments @(
            'group', 'create',
            '--subscription', $Context.SubscriptionId,
            '--name', $Name,
            '--location', $LocationName
        ) `
        -Operation "Creating resource group '$Name'"
    if ($created.name -ne $Name) {
        throw "Azure returned an unexpected resource group after creating '$Name'."
    }
    return $created
}

function ConvertTo-ODataLiteral {
    param([Parameter(Mandatory)] [string] $Value)
    return $Value.Replace("'", "''")
}

function Find-EntraApplication {
    param([Parameter(Mandatory)] [string] $DisplayName)

    $literal = ConvertTo-ODataLiteral $DisplayName
    $apps = @(Invoke-AzJson `
        -Arguments @('ad', 'app', 'list', '--filter', "displayName eq '$literal'") `
        -Operation "Looking up Entra application '$DisplayName'")
    if ($apps.Count -gt 1) {
        throw "More than one Entra application is named '$DisplayName'. Pass unique app names before continuing."
    }
    if ($apps.Count -eq 1) {
        return $apps[0]
    }
    return $null
}

function Ensure-ServicePrincipal {
    param(
        [Parameter(Mandatory)] [string] $ApplicationId,
        [Parameter(Mandatory)] [string] $DisplayName
    )

    $servicePrincipals = @(Invoke-AzJson `
        -Arguments @('ad', 'sp', 'list', '--filter', "appId eq '$ApplicationId'") `
        -Operation "Looking up the service principal for '$DisplayName'")
    if ($servicePrincipals.Count -gt 1) {
        throw "Multiple service principals were returned for '$DisplayName'."
    }
    if ($servicePrincipals.Count -eq 0) {
        return Invoke-AzJson `
            -Arguments @('ad', 'sp', 'create', '--id', $ApplicationId) `
            -Operation "Creating the service principal for '$DisplayName'"
    }
    return $servicePrincipals[0]
}

function Ensure-EntraApplication {
    param(
        [Parameter(Mandatory)] [string] $DisplayName,
        [switch] $ExposeDelegatedScope
    )

    $application = Find-EntraApplication -DisplayName $DisplayName
    $managementTag = "HeliosManaged=$DisplayName"
    if (-not $application) {
        $application = Invoke-AzJson `
            -Arguments @(
                'ad', 'app', 'create',
                '--display-name', $DisplayName,
                '--sign-in-audience', 'AzureADMyOrg'
            ) `
            -Operation "Creating Entra application '$DisplayName'"
        [void] (Invoke-AzJson `
            -Arguments @(
                'ad', 'app', 'update',
                '--id', [string] $application.id,
                '--set', "tags=['$managementTag']"
            ) `
            -Operation "Marking Entra application '$DisplayName' as Helios-managed" `
            -AllowEmptyOutput)
        $application = Find-EntraApplication -DisplayName $DisplayName
    }
    elseif (@($application.tags) -notcontains $managementTag) {
        throw "Entra application '$DisplayName' already exists but is not marked '$managementTag'. Refusing to adopt or mutate an unrelated application. Use a unique app name or review and tag the intended application explicitly."
    }
    if ([string]::IsNullOrWhiteSpace([string] $application.appId) -or [string]::IsNullOrWhiteSpace([string] $application.id)) {
        throw "Entra application '$DisplayName' is missing appId or object id."
    }
    if ([string] $application.signInAudience -ne 'AzureADMyOrg') {
        throw "Entra application '$DisplayName' is not single-tenant (AzureADMyOrg). Refusing to broaden or reuse it."
    }

    [void] (Ensure-ServicePrincipal -ApplicationId $application.appId -DisplayName $DisplayName)

    if ($ExposeDelegatedScope) {
        $requiredIdentifierUri = "api://$($application.appId)"
        $existingScopes = @($application.api.oauth2PermissionScopes)
        $matchingScopes = @($existingScopes | Where-Object value -eq 'user_impersonation')
        if ($matchingScopes.Count -gt 1) {
            throw "Entra application '$DisplayName' has duplicate user_impersonation scopes. Refusing to guess which grant is authoritative."
        }
        if ($matchingScopes.Count -eq 0) {
            $scope = [ordered]@{
                id = [guid]::NewGuid().ToString()
                adminConsentDescription = 'Allow Helios clients to read governed Azure inventory.'
                adminConsentDisplayName = 'Read Helios Azure inventory'
                isEnabled = $true
                type = 'User'
                userConsentDescription = 'Allow this client to read governed Helios Azure inventory.'
                userConsentDisplayName = 'Read Helios Azure inventory'
                value = 'user_impersonation'
            }
            $scopeJson = ConvertTo-Json @($existingScopes + $scope) -Compress -Depth 10
            [void] (Invoke-AzJson `
                -Arguments @(
                    'ad', 'app', 'update',
                    '--id', [string] $application.id,
                    '--set', "api.oauth2PermissionScopes=$scopeJson"
                ) `
                -Operation "Exposing the delegated API scope for '$DisplayName'" `
                -AllowEmptyOutput)
            $application = Find-EntraApplication -DisplayName $DisplayName
        }

        $identifierUris = @($application.identifierUris)
        if ($identifierUris -notcontains $requiredIdentifierUri) {
            $identifierUris += $requiredIdentifierUri
            $uriArguments = [System.Collections.Generic.List[string]]::new()
            foreach ($argument in @('ad', 'app', 'update', '--id', [string] $application.id, '--identifier-uris')) {
                [void] $uriArguments.Add($argument)
            }
            foreach ($uri in $identifierUris) {
                [void] $uriArguments.Add([string] $uri)
            }
            [void] (Invoke-AzJson `
                -Arguments $uriArguments.ToArray() `
                -Operation "Setting the API identifier URI for '$DisplayName'" `
                -AllowEmptyOutput)
            $application = Find-EntraApplication -DisplayName $DisplayName
        }

        if ([int] $application.api.requestedAccessTokenVersion -ne 2) {
            [void] (Invoke-AzJson `
                -Arguments @(
                    'ad', 'app', 'update',
                    '--id', [string] $application.id,
                    '--set', 'api.requestedAccessTokenVersion=2'
                ) `
                -Operation "Requiring v2 access tokens for '$DisplayName'" `
                -AllowEmptyOutput)
            $application = Find-EntraApplication -DisplayName $DisplayName
        }

        $finalScopes = @($application.api.oauth2PermissionScopes | Where-Object {
            $_.value -eq 'user_impersonation' -and [bool] $_.isEnabled -and $_.type -eq 'User'
        })
        if ($finalScopes.Count -ne 1 -or
            @($application.identifierUris) -notcontains $requiredIdentifierUri -or
            [int] $application.api.requestedAccessTokenVersion -ne 2) {
            throw "Entra application '$DisplayName' did not converge to the exact v2 api:// client ID and user_impersonation scope contract."
        }

        # Microsoft documents Azure CLI as a governed public client for this
        # delegated-flow pattern. Preauthorization avoids AADSTS65001 while
        # Easy Auth still limits callers to the configured Entra principal.
        $azureCliClientId = '04b07795-8ddb-461a-bbee-02f9e1bf7b46'
        $scopeId = [string] $finalScopes[0].id
        $preauthorized = @($application.api.preAuthorizedApplications)
        $azureCliEntry = $preauthorized | Where-Object appId -eq $azureCliClientId | Select-Object -First 1
        if (-not $azureCliEntry -or @($azureCliEntry.delegatedPermissionIds) -notcontains $scopeId) {
            $updatedPreauthorized = [System.Collections.Generic.List[object]]::new()
            foreach ($entry in $preauthorized) {
                if ($entry.appId -eq $azureCliClientId) {
                    $permissionIds = @($entry.delegatedPermissionIds)
                    if ($permissionIds -notcontains $scopeId) {
                        $permissionIds += $scopeId
                    }
                    [void] $updatedPreauthorized.Add([ordered]@{
                        appId = $azureCliClientId
                        delegatedPermissionIds = $permissionIds
                    })
                }
                else {
                    [void] $updatedPreauthorized.Add($entry)
                }
            }
            if (-not $azureCliEntry) {
                [void] $updatedPreauthorized.Add([ordered]@{
                    appId = $azureCliClientId
                    delegatedPermissionIds = @($scopeId)
                })
            }
            $preauthorizedJson = ConvertTo-Json -InputObject @($updatedPreauthorized) -Compress -Depth 10
            [void] (Invoke-AzJson `
                -Arguments @(
                    'ad', 'app', 'update',
                    '--id', [string] $application.id,
                    '--set', "api.preAuthorizedApplications=$preauthorizedJson"
                ) `
                -Operation "Preauthorizing Azure CLI for '$DisplayName' user_impersonation" `
                -AllowEmptyOutput)
            $application = Find-EntraApplication -DisplayName $DisplayName
        }
        $verifiedAzureCli = @($application.api.preAuthorizedApplications | Where-Object {
            $_.appId -eq $azureCliClientId -and @($_.delegatedPermissionIds) -contains $scopeId
        })
        if ($verifiedAzureCli.Count -ne 1) {
            throw "Entra application '$DisplayName' did not retain the exact Azure CLI preauthorization for user_impersonation."
        }
    }

    return $application
}

function Resolve-GitHubOidcSubject {
    param(
        [Parameter(Mandatory)] [string] $Owner,
        [Parameter(Mandatory)] [string] $Repository,
        [Parameter(Mandatory)] [string] $Environment
    )

    $repositoryName = "$Owner/$Repository"
    $apiVersionHeader = 'X-GitHub-Api-Version: 2026-03-10'
    $repositoryInfo = Invoke-GhJson `
        -Arguments @('api', '--method', 'GET', '--header', $apiVersionHeader, "repos/$repositoryName") `
        -Operation "Resolving immutable GitHub repository identity for '$repositoryName'"
    $oidcConfiguration = Invoke-GhJson `
        -Arguments @('api', '--method', 'GET', '--header', $apiVersionHeader, "repos/$repositoryName/actions/oidc/customization/sub") `
        -Operation "Reading the effective GitHub OIDC subject policy for '$repositoryName'"

    if (-not [bool] $oidcConfiguration.use_default) {
        throw "Repository '$repositoryName' uses a customized OIDC subject template. Helios will not infer or overwrite it; configure a reviewed default repository subject policy first."
    }
    if (-not $oidcConfiguration.PSObject.Properties['use_immutable_subject']) {
        throw 'GitHub did not return use_immutable_subject. Upgrade GitHub CLI/API access before creating Azure trust; guessing the subject format is unsafe.'
    }

    $canonicalOwner = [string] $repositoryInfo.owner.login
    $canonicalRepository = [string] $repositoryInfo.name
    if ([string]::IsNullOrWhiteSpace($canonicalOwner) -or
        [string]::IsNullOrWhiteSpace($canonicalRepository) -or
        [string]::IsNullOrWhiteSpace([string] $repositoryInfo.owner.id) -or
        [string]::IsNullOrWhiteSpace([string] $repositoryInfo.id)) {
        throw "GitHub did not return the canonical owner/repository IDs required to construct an immutable OIDC subject."
    }

    $repoSegment = if ([bool] $oidcConfiguration.use_immutable_subject) {
        "$canonicalOwner@$($repositoryInfo.owner.id)/$canonicalRepository@$($repositoryInfo.id)"
    }
    else {
        "$canonicalOwner/$canonicalRepository"
    }
    return "repo:$repoSegment`:environment:$Environment"
}

function Ensure-GitHubFederation {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Application,
        [Parameter(Mandatory)] [string] $Subject,
        [Parameter(Mandatory)] [string] $Environment
    )

    $credentialName = "github-$Environment"
    $credentials = @(Invoke-AzJson `
        -Arguments @('ad', 'app', 'federated-credential', 'list', '--id', [string] $Application.id) `
        -Operation "Looking up GitHub OIDC federation '$credentialName'")
    $existing = $credentials | Where-Object name -eq $credentialName | Select-Object -First 1
    if ($existing) {
        if ($existing.subject -ne $Subject -or
            $existing.issuer -ne 'https://token.actions.githubusercontent.com' -or
            @($existing.audiences).Count -ne 1 -or
            @($existing.audiences) -notcontains 'api://AzureADTokenExchange') {
            throw "Federated credential '$credentialName' exists with a different issuer, subject, or audience. Refusing to overwrite trust."
        }
        return
    }

    $definition = [ordered]@{
        name = $credentialName
        issuer = 'https://token.actions.githubusercontent.com'
        subject = $Subject
        audiences = @('api://AzureADTokenExchange')
    }
    $temporaryFile = Join-Path ([System.IO.Path]::GetTempPath()) ("helios-oidc-{0}.json" -f [guid]::NewGuid())
    try {
        [System.IO.File]::WriteAllText($temporaryFile, ($definition | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))
        [void] (Invoke-AzJson `
            -Arguments @(
                'ad', 'app', 'federated-credential', 'create',
                '--id', [string] $Application.id,
                '--parameters', $temporaryFile
            ) `
            -Operation "Creating GitHub OIDC federation '$credentialName'")
    }
    finally {
        if (Test-Path -LiteralPath $temporaryFile) {
            Remove-Item -LiteralPath $temporaryFile -Force
        }
    }
}

function Assert-GitHubFederationPresent {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Application,
        [Parameter(Mandatory)] [string] $Subject,
        [Parameter(Mandatory)] [string] $Environment
    )

    $credentialName = "github-$Environment"
    $credentials = @(Invoke-AzJson `
        -Arguments @('ad', 'app', 'federated-credential', 'list', '--id', [string] $Application.id) `
        -Operation "Verifying GitHub OIDC federation '$credentialName'")
    $matching = @($credentials | Where-Object {
        $_.name -eq $credentialName -and
        $_.issuer -eq 'https://token.actions.githubusercontent.com' -and
        $_.subject -eq $Subject -and
        @($_.audiences) -contains 'api://AzureADTokenExchange'
    })
    if ($matching.Count -ne 1) {
        throw "The exact GitHub OIDC federation for '$Subject' is missing. Run -Mode Configure before deploying."
    }
}

function Ensure-RoleAssignment {
    param(
        [Parameter(Mandatory)] [string] $ApplicationId,
        [Parameter(Mandatory)] [string] $Scope
    )

    $servicePrincipals = @(Invoke-AzJson `
        -Arguments @('ad', 'sp', 'list', '--filter', "appId eq '$ApplicationId'") `
        -Operation 'Resolving the GitHub OIDC service principal for RBAC')
    if ($servicePrincipals.Count -ne 1) {
        throw 'Exactly one GitHub OIDC service principal is required before RBAC can be configured.'
    }
    $servicePrincipalObjectId = [string] $servicePrincipals[0].id

    $assignments = @(Invoke-AzJson `
        -Arguments @(
            'role', 'assignment', 'list',
            '--assignee', $servicePrincipalObjectId,
            '--scope', $Scope
        ) `
        -Operation 'Checking GitHub OIDC resource-group RBAC')

    $role = $script:ContributorRole
    $matching = @($assignments | Where-Object {
        ([string] $_.roleDefinitionId).EndsWith("/$($role.Id)", [StringComparison]::OrdinalIgnoreCase)
    })
    if ($matching.Count -eq 0) {
        [void] (Invoke-AzJson `
            -Arguments @(
                'role', 'assignment', 'create',
                '--assignee-object-id', $servicePrincipalObjectId,
                '--assignee-principal-type', 'ServicePrincipal',
                '--role', $role.Id,
                '--scope', $Scope
            ) `
            -Operation 'Assigning GitHub OIDC Contributor at resource-group scope')
    }
}

function Assert-RoleAssignmentPresent {
    param(
        [Parameter(Mandatory)] [string] $ApplicationId,
        [Parameter(Mandatory)] [string] $Scope
    )

    $servicePrincipals = @(Invoke-AzJson `
        -Arguments @('ad', 'sp', 'list', '--filter', "appId eq '$ApplicationId'") `
        -Operation 'Resolving the GitHub OIDC service principal for RBAC verification')
    if ($servicePrincipals.Count -ne 1) {
        throw 'The GitHub OIDC service principal is missing or ambiguous. Run -Mode Configure before deploying.'
    }
    $servicePrincipalObjectId = [string] $servicePrincipals[0].id

    $assignments = @(Invoke-AzJson `
        -Arguments @(
            'role', 'assignment', 'list',
            '--assignee', $servicePrincipalObjectId,
            '--scope', $Scope
        ) `
        -Operation 'Verifying GitHub OIDC resource-group RBAC')

    $role = $script:ContributorRole
    $matching = @($assignments | Where-Object {
        ([string] $_.roleDefinitionId).EndsWith("/$($role.Id)", [StringComparison]::OrdinalIgnoreCase)
    })
    if ($matching.Count -eq 0) {
        throw "The GitHub OIDC application does not have $($role.Name) at '$Scope'. Run -Mode Configure before deploying."
    }
}

function Ensure-RequiredResourceProviders {
    param([Parameter(Mandatory)] [pscustomobject] $Context)

    $missing = [System.Collections.Generic.List[string]]::new()
    foreach ($namespace in $script:RequiredProviders) {
        $provider = Invoke-AzJson `
            -Arguments @('provider', 'show', '--subscription', $Context.SubscriptionId, '--namespace', $namespace) `
            -Operation "Checking Azure provider '$namespace'"
        if ([string] $provider.registrationState -ne 'Registered') {
            [void] $missing.Add($namespace)
        }
    }
    if ($missing.Count -eq 0) {
        return
    }

    Write-Host 'Azure subscription providers requiring registration:'
    $missing | Sort-Object | ForEach-Object { Write-Host "  $_" }
    Assert-ExactConfirmation `
        -Expected 'REGISTER HELIOS PROVIDERS' `
        -Purpose 'Registering only the Azure resource providers listed above at subscription scope'
    foreach ($namespace in $missing) {
        Invoke-AzNoOutput `
            -Arguments @('provider', 'register', '--subscription', $Context.SubscriptionId, '--namespace', $namespace, '--wait') `
            -Operation "Registering Azure provider '$namespace'"
        $provider = Invoke-AzJson `
            -Arguments @('provider', 'show', '--subscription', $Context.SubscriptionId, '--namespace', $namespace) `
            -Operation "Verifying Azure provider '$namespace'"
        if ([string] $provider.registrationState -ne 'Registered') {
            throw "Azure provider '$namespace' did not reach Registered state."
        }
    }
}

function Ensure-PrincipalRoleAssignment {
    param(
        [Parameter(Mandatory)] [string] $PrincipalObjectId,
        [Parameter(Mandatory)] [string] $PrincipalType,
        [Parameter(Mandatory)] [string] $Role,
        [Parameter(Mandatory)] [string] $Scope
    )

    $assignments = @(Invoke-AzJson `
        -Arguments @('role', 'assignment', 'list', '--assignee', $PrincipalObjectId, '--scope', $Scope) `
        -Operation "Checking '$Role' at the approved scope")
    $matching = @($assignments | Where-Object {
        $_.roleDefinitionName -eq $Role -or ([string] $_.roleDefinitionId).EndsWith("/$Role", [StringComparison]::OrdinalIgnoreCase)
    })
    if ($matching.Count -eq 0) {
        [void] (Invoke-AzJson `
            -Arguments @(
                'role', 'assignment', 'create',
                '--assignee-object-id', $PrincipalObjectId,
                '--assignee-principal-type', $PrincipalType,
                '--role', $Role,
                '--scope', $Scope
            ) `
            -Operation "Assigning '$Role' at the approved scope")
    }
}

function Assert-PrincipalRoleAssignment {
    param(
        [Parameter(Mandatory)] [string] $PrincipalObjectId,
        [Parameter(Mandatory)] [string] $Role,
        [Parameter(Mandatory)] [string] $Scope
    )

    $assignments = @(Invoke-AzJson `
        -Arguments @('role', 'assignment', 'list', '--assignee', $PrincipalObjectId, '--scope', $Scope) `
        -Operation "Verifying '$Role' at the approved scope")
    $matching = @($assignments | Where-Object {
        $_.roleDefinitionName -eq $Role -or ([string] $_.roleDefinitionId).EndsWith("/$Role", [StringComparison]::OrdinalIgnoreCase)
    })
    if ($matching.Count -eq 0) {
        throw "Required role '$Role' is missing at '$Scope'. Run the administrator Configure/Publish phase first."
    }
}

function Ensure-RuntimeManagedIdentity {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Context,
        [Parameter(Mandatory)] [string] $ResourceGroupName,
        [Parameter(Mandatory)] [string] $LocationName
    )

    $identityName = "helios-connector-$EnvironmentName-id"
    $identities = @(Invoke-AzJson `
        -Arguments @('identity', 'list', '--subscription', $Context.SubscriptionId, '--resource-group', $ResourceGroupName) `
        -Operation "Looking up managed identity '$identityName'")
    $identity = $identities | Where-Object name -eq $identityName | Select-Object -First 1
    if (-not $identity) {
        $identity = Invoke-AzJson `
            -Arguments @(
                'identity', 'create',
                '--subscription', $Context.SubscriptionId,
                '--resource-group', $ResourceGroupName,
                '--location', $LocationName,
                '--name', $identityName,
                '--tags', 'system=helios', "environment=$EnvironmentName", 'managed-by=helios-operator'
            ) `
            -Operation "Creating managed identity '$identityName'"
    }
    if ([string]::IsNullOrWhiteSpace([string] $identity.principalId)) {
        throw "Managed identity '$identityName' has no principal ID."
    }

    $resourceGroupScope = "/subscriptions/$($Context.SubscriptionId)/resourceGroups/$ResourceGroupName"
    Ensure-PrincipalRoleAssignment `
        -PrincipalObjectId ([string] $identity.principalId) `
        -PrincipalType 'ServicePrincipal' `
        -Role $script:ReaderRole.Id `
        -Scope $resourceGroupScope
    return $identity
}

function Get-RuntimeManagedIdentity {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Context,
        [Parameter(Mandatory)] [string] $ResourceGroupName
    )

    $identityName = "helios-connector-$EnvironmentName-id"
    $identities = @(Invoke-AzJson `
        -Arguments @('identity', 'list', '--subscription', $Context.SubscriptionId, '--resource-group', $ResourceGroupName) `
        -Operation "Looking up managed identity '$identityName'")
    $identity = $identities | Where-Object name -eq $identityName | Select-Object -First 1
    if (-not $identity -or [string]::IsNullOrWhiteSpace([string] $identity.principalId)) {
        throw "Managed identity '$identityName' is missing. Run -Mode Configure first."
    }
    return $identity
}

function Ensure-RegistryPullAccess {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Context,
        [Parameter(Mandatory)] [string] $ResourceGroupName,
        [Parameter(Mandatory)] [string] $RegistryName,
        [Parameter(Mandatory)] [pscustomobject] $Identity,
        [Parameter(Mandatory)] [string] $GitHubApplicationId
    )

    $registry = Invoke-AzJson `
        -Arguments @('acr', 'show', '--subscription', $Context.SubscriptionId, '--resource-group', $ResourceGroupName, '--name', $RegistryName) `
        -Operation "Reading authorization mode for registry '$RegistryName'"
    $role = if ([string] $registry.roleAssignmentMode -eq 'rbac-abac') {
        'Container Registry Repository Reader'
    }
    else {
        'AcrPull'
    }
    Ensure-PrincipalRoleAssignment `
        -PrincipalObjectId ([string] $Identity.principalId) `
        -PrincipalType 'ServicePrincipal' `
        -Role $role `
        -Scope ([string] $registry.id)

    $githubServicePrincipals = @(Invoke-AzJson `
        -Arguments @('ad', 'sp', 'list', '--filter', "appId eq '$GitHubApplicationId'") `
        -Operation 'Resolving the GitHub OIDC service principal for ACR read verification')
    if ($githubServicePrincipals.Count -ne 1) {
        throw 'The GitHub OIDC service principal is missing or ambiguous. Run -Mode Configure first.'
    }
    Ensure-PrincipalRoleAssignment `
        -PrincipalObjectId ([string] $githubServicePrincipals[0].id) `
        -PrincipalType 'ServicePrincipal' `
        -Role $role `
        -Scope ([string] $registry.id)

    $armAuthentication = Invoke-AzJson `
        -Arguments @('acr', 'config', 'authentication-as-arm', 'show', '--subscription', $Context.SubscriptionId, '--registry', $RegistryName) `
        -Operation "Checking ARM-audience authentication for registry '$RegistryName'"
    if ([string] $armAuthentication.status -notin @('enabled', 'disabled')) {
        throw "Registry '$RegistryName' returned an unknown authentication-as-ARM policy state."
    }
    Write-Host "Registry authentication-as-ARM policy: $($armAuthentication.status). Helios supports both modes and does not weaken this policy."
}

function Assert-RegistryPullAccess {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Context,
        [Parameter(Mandatory)] [string] $ResourceGroupName,
        [Parameter(Mandatory)] [string] $RegistryName,
        [Parameter(Mandatory)] [pscustomobject] $Identity,
        [Parameter(Mandatory)] [string] $GitHubApplicationId
    )

    $registry = Invoke-AzJson `
        -Arguments @('acr', 'show', '--subscription', $Context.SubscriptionId, '--resource-group', $ResourceGroupName, '--name', $RegistryName) `
        -Operation "Verifying registry '$RegistryName'"
    $role = if ([string] $registry.roleAssignmentMode -eq 'rbac-abac') {
        'Container Registry Repository Reader'
    }
    else {
        'AcrPull'
    }
    Assert-PrincipalRoleAssignment `
        -PrincipalObjectId ([string] $Identity.principalId) `
        -Role $role `
        -Scope ([string] $registry.id)
    $githubServicePrincipals = @(Invoke-AzJson `
        -Arguments @('ad', 'sp', 'list', '--filter', "appId eq '$GitHubApplicationId'") `
        -Operation 'Resolving the GitHub OIDC service principal for ACR verification')
    if ($githubServicePrincipals.Count -ne 1) {
        throw 'The GitHub OIDC service principal is missing or ambiguous. Run -Mode Configure first.'
    }
    Assert-PrincipalRoleAssignment `
        -PrincipalObjectId ([string] $githubServicePrincipals[0].id) `
        -Role $role `
        -Scope ([string] $registry.id)
    $armAuthentication = Invoke-AzJson `
        -Arguments @('acr', 'config', 'authentication-as-arm', 'show', '--subscription', $Context.SubscriptionId, '--registry', $RegistryName) `
        -Operation "Verifying ARM-audience authentication for registry '$RegistryName'"
    if ([string] $armAuthentication.status -notin @('enabled', 'disabled')) {
        throw "Registry '$RegistryName' returned an unknown authentication-as-ARM policy state."
    }
}

function Assert-GitHubCliReady {
    [int64] $reviewerId = 0
    if ([string]::IsNullOrWhiteSpace($RequiredReviewerId) -or
        -not [int64]::TryParse($RequiredReviewerId, [ref] $reviewerId) -or
        $reviewerId -le 0) {
        throw 'Configure mode requires a positive numeric RequiredReviewerId.'
    }

    $ghCommand = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $ghCommand) {
        throw 'GitHub CLI is required for Configure mode. Authenticate it separately; this script does not handle or store GitHub credentials.'
    }
    $script:GhPath = $ghCommand.Source
    Invoke-GhNoOutput -Arguments @('auth', 'status', '--hostname', 'github.com') -Operation 'Checking GitHub CLI authentication'
}

function Set-GitHubEnvironmentVariables {
    param(
        [Parameter(Mandatory)] [hashtable] $Values,
        [Parameter(Mandatory)] [string] $Owner,
        [Parameter(Mandatory)] [string] $Repository,
        [Parameter(Mandatory)] [string] $Environment
    )

    [int64] $reviewerId = 0
    if ([string]::IsNullOrWhiteSpace($RequiredReviewerId) -or
        -not [int64]::TryParse($RequiredReviewerId, [ref] $reviewerId) -or
        $reviewerId -le 0) {
        throw 'Configure mode requires a positive numeric RequiredReviewerId.'
    }

    $repositoryName = "$Owner/$Repository"
    $escapedEnvironment = [uri]::EscapeDataString($Environment)
    $environmentDefinition = [ordered]@{
        wait_timer = 0
        prevent_self_review = $true
        reviewers = @([ordered]@{ type = 'User'; id = $reviewerId })
        deployment_branch_policy = [ordered]@{
            protected_branches = $false
            custom_branch_policies = $true
        }
    }

    # Create or update the environment before reading its branch policies. A
    # fresh repository returns 404 for the policy endpoint until this exists.
    $environmentFile = Join-Path ([System.IO.Path]::GetTempPath()) ("helios-environment-{0}.json" -f [guid]::NewGuid())
    try {
        [System.IO.File]::WriteAllText(
            $environmentFile,
            ($environmentDefinition | ConvertTo-Json -Depth 8),
            [System.Text.UTF8Encoding]::new($false))
        Invoke-GhNoOutput `
            -Arguments @(
                'api', '--method', 'PUT',
                "repos/$repositoryName/environments/$escapedEnvironment",
                '--input', $environmentFile
            ) `
            -Operation "Ensuring reviewer-protected GitHub environment '$Environment'"
    }
    finally {
        if (Test-Path -LiteralPath $environmentFile) {
            Remove-Item -LiteralPath $environmentFile -Force
        }
    }

    $policies = Invoke-GhJson `
        -Arguments @('api', '--method', 'GET', "repos/$repositoryName/environments/$escapedEnvironment/deployment-branch-policies") `
        -Operation "Reading deployment branch policies for '$Environment'"
    $matchingBranchPolicies = @($policies.branch_policies | Where-Object {
        $_.name -eq $GitHubDeploymentBranch -and $_.type -eq 'branch'
    })
    if ($matchingBranchPolicies.Count -eq 0) {
        $branchPolicy = [ordered]@{ name = $GitHubDeploymentBranch; type = 'branch' }
        $branchPolicyFile = Join-Path ([System.IO.Path]::GetTempPath()) ("helios-branch-policy-{0}.json" -f [guid]::NewGuid())
        try {
            [System.IO.File]::WriteAllText(
                $branchPolicyFile,
                ($branchPolicy | ConvertTo-Json -Depth 5),
                [System.Text.UTF8Encoding]::new($false))
            Invoke-GhNoOutput `
                -Arguments @(
                    'api', '--method', 'POST',
                    "repos/$repositoryName/environments/$escapedEnvironment/deployment-branch-policies",
                    '--input', $branchPolicyFile
                ) `
                -Operation "Restricting GitHub environment '$Environment' to branch '$GitHubDeploymentBranch'"
        }
        finally {
            if (Test-Path -LiteralPath $branchPolicyFile) {
                Remove-Item -LiteralPath $branchPolicyFile -Force
            }
        }
    }

    foreach ($name in ($Values.Keys | Sort-Object)) {
        Invoke-GhNoOutput `
            -Arguments @(
                'variable', 'set', $name,
                '--body', [string] $Values[$name],
                '--repo', $repositoryName,
                '--env', $Environment
            ) `
            -Operation "Setting GitHub environment variable '$name'"
    }

    Assert-GitHubEnvironmentProtection `
        -Owner $Owner `
        -Repository $Repository `
        -Environment $Environment `
        -ReviewerId $reviewerId `
        -DeploymentBranch $GitHubDeploymentBranch
}

function Assert-GitHubEnvironmentProtection {
    param(
        [Parameter(Mandatory)] [string] $Owner,
        [Parameter(Mandatory)] [string] $Repository,
        [Parameter(Mandatory)] [string] $Environment,
        [Parameter(Mandatory)] [int64] $ReviewerId,
        [Parameter(Mandatory)] [string] $DeploymentBranch
    )

    $repositoryName = "$Owner/$Repository"
    $escapedEnvironment = [uri]::EscapeDataString($Environment)
    $definition = Invoke-GhJson `
        -Arguments @('api', '--method', 'GET', "repos/$repositoryName/environments/$escapedEnvironment") `
        -Operation "Verifying protected GitHub environment '$Environment'"
    $reviewerRules = @($definition.protection_rules | Where-Object type -eq 'required_reviewers')
    $matchingReviewers = @($reviewerRules.reviewers | Where-Object { [int64] $_.reviewer.id -eq $ReviewerId })
    if ($reviewerRules.Count -ne 1 -or
        @($reviewerRules[0].reviewers).Count -ne 1 -or
        $matchingReviewers.Count -ne 1 -or
        -not [bool] $reviewerRules[0].prevent_self_review -or
        -not [bool] $definition.deployment_branch_policy.custom_branch_policies -or
        [bool] $definition.deployment_branch_policy.protected_branches) {
        throw "GitHub environment '$Environment' is not fail-closed with the exact required reviewer and custom branch policy. Azure trust was not created."
    }

    $policies = Invoke-GhJson `
        -Arguments @('api', '--method', 'GET', "repos/$repositoryName/environments/$escapedEnvironment/deployment-branch-policies") `
        -Operation "Verifying deployment branch policy for '$Environment'"
    $matchingPolicies = @($policies.branch_policies | Where-Object {
        $_.name -eq $DeploymentBranch -and $_.type -eq 'branch'
    })
    if (@($policies.branch_policies).Count -ne 1 -or $matchingPolicies.Count -ne 1) {
        throw "GitHub environment '$Environment' is not restricted to the reviewed '$DeploymentBranch' branch. Azure trust was not created."
    }
}

function Resolve-AllowedPrincipalObjectId {
    if (-not [string]::IsNullOrWhiteSpace($AllowedPrincipalObjectId)) {
        if (-not (Test-GuidValue $AllowedPrincipalObjectId)) {
            throw 'AllowedPrincipalObjectId must be an Entra object GUID.'
        }
        return $AllowedPrincipalObjectId
    }

    try {
        $user = Invoke-AzJson -Arguments @('ad', 'signed-in-user', 'show') -Operation 'Resolving the signed-in Entra user'
        if (Test-GuidValue ([string] $user.id)) {
            return [string] $user.id
        }
    }
    catch {
        Write-Warning 'The Azure CLI identity is not a user or its directory object could not be resolved.'
    }

    $entered = Read-RequiredValue -Prompt 'Allowed Entra principal object ID'
    if (-not (Test-GuidValue $entered)) {
        throw 'The entered allowed principal object ID is not a GUID.'
    }
    return $entered
}

function Assert-ImmutableAcrImage {
    param(
        [Parameter(Mandatory)] [string] $Reference,
        [Parameter(Mandatory)] [string] $SelectedSubscriptionId,
        [Parameter(Mandatory)] [string] $SelectedResourceGroup
    )

    $pattern = '^(?<server>[a-zA-Z0-9][a-zA-Z0-9.-]*\.azurecr\.io)/(?<repository>[a-z0-9]+(?:[._/-][a-z0-9]+)*)@(?<digest>sha256:[0-9a-fA-F]{64})$'
    $match = [regex]::Match($Reference, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        throw 'ImageReference must be an immutable Azure Container Registry reference: registry.azurecr.io/repository@sha256:<64 hex characters>.'
    }
    if ($match.Groups['digest'].Value -eq ('sha256:' + ('0' * 64))) {
        throw 'The all-zero preview placeholder is not an approved image.'
    }

    $server = $match.Groups['server'].Value.ToLowerInvariant()
    $registryName = $server.Substring(0, $server.IndexOf('.azurecr.io', [StringComparison]::OrdinalIgnoreCase))
    $repository = $match.Groups['repository'].Value
    $digest = $match.Groups['digest'].Value.ToLowerInvariant()

    $registry = Invoke-AzJson `
        -Arguments @('acr', 'show', '--name', $registryName, '--subscription', $SelectedSubscriptionId) `
        -Operation "Resolving Azure Container Registry '$registryName'"
    if ([string] $registry.loginServer -ne $server) {
        throw "The image registry '$server' does not match the selected Azure registry login server."
    }
    if (-not [string]::Equals([string] $registry.resourceGroup, $SelectedResourceGroup, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Registry '$registryName' is in resource group '$($registry.resourceGroup)', not the selected Helios resource group '$SelectedResourceGroup'."
    }

    Invoke-AzNoOutput `
        -Arguments @('account', 'get-access-token', '--scope', 'https://containerregistry.azure.net/.default') `
        -Operation 'Acquiring an ACR-scoped token without printing or persisting it'
    $manifest = Invoke-AzJson `
        -Arguments @(
            'acr', 'repository', 'show',
            '--name', $registryName,
            '--image', "$repository@$digest",
            '--subscription', $SelectedSubscriptionId
        ) `
        -Operation "Verifying immutable image manifest '$repository@$digest'"
    if ([string]::IsNullOrWhiteSpace([string] $manifest.digest) -or ([string] $manifest.digest).ToLowerInvariant() -ne $digest) {
        throw 'Azure Container Registry did not return the requested immutable image digest.'
    }

    Write-Host "Verified immutable ACR image: $server/$repository@$digest"
    return "$server/$repository@$digest"
}

function Assert-ContainerRegistryName {
    param([Parameter(Mandatory)] [string] $Name)

    if ($Name -cnotmatch '^[a-z0-9]{5,50}$') {
        throw 'ContainerRegistryName must contain 5-50 lowercase letters or digits.'
    }
}

function Get-RegistryNameFromImageReference {
    param([Parameter(Mandatory)] [string] $Reference)

    $match = [regex]::Match(
        $Reference,
        '^(?<name>[a-z0-9]{5,50})\.azurecr\.io/',
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        throw 'The image reference does not contain a supported Azure Container Registry login server.'
    }
    return $match.Groups['name'].Value.ToLowerInvariant()
}

function Get-ResourceGroupRegistries {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Context,
        [Parameter(Mandatory)] [string] $ResourceGroupName
    )

    return @(Invoke-AzJson `
        -Arguments @(
            'acr', 'list',
            '--subscription', $Context.SubscriptionId,
            '--resource-group', $ResourceGroupName
        ) `
        -Operation "Enumerating container registries in '$ResourceGroupName'")
}

function Get-TagValue {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Resource,
        [Parameter(Mandatory)] [string] $Name
    )

    $tagsProperty = $Resource.PSObject.Properties['tags']
    if (-not $tagsProperty -or $null -eq $tagsProperty.Value) {
        return $null
    }
    $tagProperty = $tagsProperty.Value.PSObject.Properties[$Name]
    if (-not $tagProperty) {
        return $null
    }
    return [string] $tagProperty.Value
}

function Assert-DedicatedHeliosRegistry {
    param([Parameter(Mandatory)] [pscustomobject] $Registry)

    if ((Get-TagValue -Resource $Registry -Name 'system') -ne 'helios' -or
        (Get-TagValue -Resource $Registry -Name 'environment') -ne $EnvironmentName -or
        (Get-TagValue -Resource $Registry -Name 'managed-by') -ne 'helios-operator') {
        throw "Registry '$($Registry.name)' is not tagged as a dedicated Helios $EnvironmentName registry. Refusing to adopt or modify a shared registry."
    }
    if ([bool] $Registry.adminUserEnabled) {
        throw "Registry '$($Registry.name)' has the admin user enabled. Disable it before Helios can use this registry."
    }
}

function Resolve-ContainerRegistryName {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Context,
        [Parameter(Mandatory)] [string] $ResourceGroupName,
        [Parameter(Mandatory)] [string] $ResourceLocation,
        [string] $Image
    )

    $nameFromImage = $null
    if (-not [string]::IsNullOrWhiteSpace($Image)) {
        $nameFromImage = Get-RegistryNameFromImageReference -Reference $Image
    }
    if (-not [string]::IsNullOrWhiteSpace($ContainerRegistryName) -and
        -not [string]::IsNullOrWhiteSpace($nameFromImage) -and
        -not [string]::Equals($ContainerRegistryName, $nameFromImage, [StringComparison]::OrdinalIgnoreCase)) {
        throw "ContainerRegistryName '$ContainerRegistryName' does not match image registry '$nameFromImage'."
    }

    $registries = @(Get-ResourceGroupRegistries -Context $Context -ResourceGroupName $ResourceGroupName)
    if (-not [string]::IsNullOrWhiteSpace($nameFromImage)) {
        $resolvedName = $nameFromImage
    }
    elseif (-not [string]::IsNullOrWhiteSpace($ContainerRegistryName)) {
        $resolvedName = $ContainerRegistryName.ToLowerInvariant()
    }
    elseif ($Mode -eq 'Publish' -and $registries.Count -gt 0) {
        Write-Host 'Available container registries in the selected resource group:'
        for ($index = 0; $index -lt $registries.Count; $index++) {
            Write-Host ('  [{0}] {1} ({2})' -f ($index + 1), $registries[$index].name, $registries[$index].location)
        }
        Write-Host '  [N] Create a new Azure Container Registry'
        while ($true) {
            $answer = Read-Host 'Select a container registry'
            if ($answer -ceq 'N') {
                $resolvedName = Read-RequiredValue -Prompt 'New globally unique ACR name (lowercase letters/digits)'
                break
            }
            $choice = 0
            if ([int]::TryParse($answer, [ref] $choice) -and $choice -ge 1 -and $choice -le $registries.Count) {
                $resolvedName = [string] $registries[$choice - 1].name
                break
            }
            Write-Warning 'Enter one of the listed numbers or N.'
        }
    }
    else {
        $resolvedName = Read-RequiredValue -Prompt 'Helios Azure Container Registry name (lowercase letters/digits)'
    }

    if ($resolvedName -cne $resolvedName.ToLowerInvariant()) {
        throw 'ContainerRegistryName must be lowercase; the wizard will not silently rename a cloud resource.'
    }
    Assert-ContainerRegistryName -Name $resolvedName

    $matching = $registries | Where-Object name -eq $resolvedName | Select-Object -First 1
    if ($matching) {
        if (-not [string]::Equals([string] $matching.location, $ResourceLocation, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Registry '$resolvedName' is in location '$($matching.location)', but the Helios deployment location is '$ResourceLocation'."
        }
        Assert-DedicatedHeliosRegistry -Registry $matching
        return [pscustomobject]@{
            Name = $resolvedName
            Exists = $true
            Resource = $matching
        }
    }

    # Detect a same-named registry outside the selected Helios resource group
    # without treating authorization or network failures as a 404.
    $subscriptionRegistries = @(Invoke-AzJson `
        -Arguments @('acr', 'list', '--subscription', $Context.SubscriptionId) `
        -Operation 'Checking the selected subscription for registry name conflicts')
    $elsewhere = $subscriptionRegistries | Where-Object name -eq $resolvedName | Select-Object -First 1
    if ($elsewhere -and -not [string]::Equals([string] $elsewhere.resourceGroup, $ResourceGroupName, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Registry '$resolvedName' already exists in resource group '$($elsewhere.resourceGroup)'. Helios requires the source registry in '$ResourceGroupName'."
    }

    $availability = Invoke-AzJson `
        -Arguments @('acr', 'check-name', '--name', $resolvedName, '--subscription', $Context.SubscriptionId) `
        -Operation "Checking global availability of registry name '$resolvedName'"
    if (-not [bool] $availability.nameAvailable) {
        $reasonProperty = $availability.PSObject.Properties['message']
        $reason = if ($reasonProperty) { Protect-DiagnosticText ([string] $reasonProperty.Value) } else { 'Azure reported that the name is unavailable.' }
        throw "Container registry name '$resolvedName' is not available: $reason"
    }

    return [pscustomobject]@{
        Name = $resolvedName
        Exists = $false
        Resource = $null
    }
}

function New-ConfirmedContainerRegistry {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Context,
        [Parameter(Mandatory)] [string] $ResourceGroupName,
        [Parameter(Mandatory)] [string] $LocationName,
        [Parameter(Mandatory)] [string] $Name
    )

    if ($Mode -ne 'Publish') {
        throw 'Container-registry creation by this wizard is restricted to Publish mode.'
    }
    $availability = Invoke-AzJson `
        -Arguments @('acr', 'check-name', '--name', $Name, '--subscription', $Context.SubscriptionId) `
        -Operation "Checking global availability of registry name '$Name'"
    if (-not [bool] $availability.nameAvailable) {
        $reason = Protect-DiagnosticText ([string] $availability.message)
        throw "Container registry name '$Name' is not available: $reason"
    }
    Assert-ExactConfirmation `
        -Expected "CREATE CONTAINER REGISTRY $Name" `
        -Purpose "Creating Azure Container Registry '$Name'"
    $registry = Invoke-AzJson `
        -Arguments @(
            'acr', 'create',
            '--subscription', $Context.SubscriptionId,
            '--resource-group', $ResourceGroupName,
            '--location', $LocationName,
            '--name', $Name,
            '--sku', 'Basic',
            '--admin-enabled', 'false',
            '--tags', 'system=helios', "environment=$EnvironmentName", 'managed-by=helios-operator'
        ) `
        -Operation "Creating Azure Container Registry '$Name'"
    if (-not [string]::Equals([string] $registry.name, $Name, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([string] $registry.resourceGroup, $ResourceGroupName, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Azure returned an unexpected registry after creation.'
    }
    Assert-DedicatedHeliosRegistry -Registry $registry
    return $registry
}

function Resolve-SafeCloudBuildContext {
    param([Parameter(Mandatory)] [string] $ProjectRoot)

    $contextCandidate = if ([string]::IsNullOrWhiteSpace($BuildContext)) { $ProjectRoot } else { $BuildContext }
    $contextAbsolute = if ([System.IO.Path]::IsPathRooted($contextCandidate)) {
        $contextCandidate
    }
    else {
        Join-Path (Get-Location).Path $contextCandidate
    }
    $resolvedContext = [System.IO.Path]::GetFullPath($contextAbsolute)
    $resolvedRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
    $pathComparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    $rootPrefix = $resolvedRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not [string]::Equals($resolvedContext, $resolvedRoot, $pathComparison) -and -not $resolvedContext.StartsWith($rootPrefix, $pathComparison)) {
        throw 'BuildContext must be the Helios project root or a directory inside it.'
    }
    if (-not [string]::Equals($resolvedContext, $resolvedRoot, $pathComparison)) {
        throw 'Cloud publication uses an allowlisted project-root context. Custom subdirectory BuildContext values are not supported.'
    }
    if (-not (Test-Path -LiteralPath $resolvedContext -PathType Container)) {
        throw "Cloud build context does not exist: $resolvedContext"
    }

    $dockerCandidate = if ([System.IO.Path]::IsPathRooted($DockerfilePath)) {
        [System.IO.Path]::GetFullPath($DockerfilePath)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $resolvedContext $DockerfilePath))
    }
    $contextPrefix = $resolvedContext.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $dockerCandidate.StartsWith($contextPrefix, $pathComparison)) {
        throw 'DockerfilePath must resolve inside BuildContext.'
    }
    if (-not (Test-Path -LiteralPath $dockerCandidate -PathType Leaf)) {
        throw "Dockerfile does not exist: $dockerCandidate"
    }

    $gitMetadata = Join-Path $resolvedContext '.git'
    if (Test-Path -LiteralPath $gitMetadata) {
        $dockerIgnore = Join-Path $resolvedContext '.dockerignore'
        if (-not (Test-Path -LiteralPath $dockerIgnore -PathType Leaf)) {
            throw 'BuildContext contains .git metadata but no .dockerignore. Add a reviewed .dockerignore that excludes .git before cloud publication.'
        }
        $ignoreRules = @(Get-Content -LiteralPath $dockerIgnore | ForEach-Object { $_.Trim() } | Where-Object { $_ -and -not $_.StartsWith('#') })
        if ($ignoreRules -notcontains '.git' -and $ignoreRules -notcontains '.git/' -and $ignoreRules -notcontains '**/.git') {
            throw 'The reviewed .dockerignore must explicitly exclude .git before cloud publication.'
        }
    }

    $sensitiveNames = @('.env', '.env.local', 'secrets.json', 'id_rsa', 'id_ed25519')
    $sensitiveExtensions = @('.pfx', '.p12', '.pem', '.key')
    $sensitiveFiles = @(Get-ChildItem -LiteralPath $resolvedContext -File -Force -Recurse -ErrorAction Stop | Where-Object {
        $_.Name -in $sensitiveNames -or $_.Extension.ToLowerInvariant() -in $sensitiveExtensions
    })
    if ($sensitiveFiles.Count -gt 0) {
        $relativeNames = @($sensitiveFiles | ForEach-Object { [System.IO.Path]::GetRelativePath($resolvedContext, $_.FullName) })
        throw "Cloud build context contains secret-shaped files and will not be uploaded: $($relativeNames -join ', ')"
    }

    $stagingContext = Join-Path ([System.IO.Path]::GetTempPath()) ("helios-build-{0}" -f [guid]::NewGuid())
    [void] (New-Item -ItemType Directory -Path $stagingContext)
    try {
        $sourceDirectory = Join-Path $resolvedContext 'src'
        if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
            throw 'The reviewed src directory is missing from the Helios project root.'
        }
        Copy-Item -LiteralPath $sourceDirectory -Destination $stagingContext -Recurse -Force
        foreach ($optionalBuildFile in @('Directory.Build.props', 'Directory.Build.targets', 'NuGet.config', 'global.json')) {
            $candidate = Join-Path $resolvedContext $optionalBuildFile
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                Copy-Item -LiteralPath $candidate -Destination $stagingContext -Force
            }
        }
    }
    catch {
        Remove-Item -LiteralPath $stagingContext -Recurse -Force -ErrorAction SilentlyContinue
        throw
    }

    return [pscustomobject]@{
        Context = $stagingContext
        Dockerfile = [System.IO.Path]::GetRelativePath($resolvedContext, $dockerCandidate).Replace('\', '/')
        Temporary = $true
    }
}

function Publish-HeliosCloudImage {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Context,
        [Parameter(Mandatory)] [string] $ResourceGroupName,
        [Parameter(Mandatory)] [string] $RegistryName,
        [Parameter(Mandatory)] [pscustomobject] $BuildInput
    )

    if ($ImageRepository -cnotmatch '^[a-z0-9]+(?:[._-][a-z0-9]+)*(?:/[a-z0-9]+(?:[._-][a-z0-9]+)*)*$') {
        throw 'ImageRepository must be a lowercase OCI repository path.'
    }
    $effectiveTag = $ImageTag
    if ([string]::IsNullOrWhiteSpace($effectiveTag)) {
        $effectiveTag = 'helios-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
    }
    if ($effectiveTag -cnotmatch '^[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}$') {
        throw 'ImageTag is not a valid OCI tag.'
    }

    Write-Host 'Cloud image publication plan:'
    Write-Host "  registry: $RegistryName (resource group $ResourceGroupName)"
    Write-Host "  image: ${ImageRepository}:$effectiveTag"
    Write-Host "  build service: Azure Container Registry Tasks (no local Docker daemon)"
    Write-Host "  Dockerfile: $($BuildInput.Dockerfile)"
    Assert-ExactConfirmation -Expected 'PUBLISH HELIOS IMAGE' -Purpose 'Uploading the reviewed source context and running an ACR cloud build'

    $build = Invoke-AzJson `
        -Arguments @(
            'acr', 'build',
            '--subscription', $Context.SubscriptionId,
            '--registry', $RegistryName,
            '--resource-group', $ResourceGroupName,
            '--image', "${ImageRepository}:$effectiveTag",
            '--file', $BuildInput.Dockerfile,
            '--no-logs',
            $BuildInput.Context
        ) `
        -Operation 'Running the Helios image build in Azure Container Registry'
    $buildStatusProperty = $build.PSObject.Properties['status']
    $buildStatus = if ($buildStatusProperty) { [string] $buildStatusProperty.Value } else { $null }
    if (-not [string]::IsNullOrWhiteSpace($buildStatus) -and $buildStatus -notin @('Succeeded', 'Completed')) {
        throw "Azure Container Registry build finished with status '$buildStatus'."
    }

    Invoke-AzNoOutput `
        -Arguments @('account', 'get-access-token', '--scope', 'https://containerregistry.azure.net/.default') `
        -Operation 'Acquiring an ACR-scoped token without printing or persisting it'
    $manifest = Invoke-AzJson `
        -Arguments @(
            'acr', 'repository', 'show',
            '--name', $RegistryName,
            '--image', "${ImageRepository}:$effectiveTag",
            '--subscription', $Context.SubscriptionId
        ) `
        -Operation 'Resolving the published image digest'
    $digest = ([string] $manifest.digest).ToLowerInvariant()
    if ($digest -notmatch '^sha256:[0-9a-f]{64}$') {
        throw 'Azure Container Registry did not return an immutable digest for the published image.'
    }

    $registry = Invoke-AzJson `
        -Arguments @('acr', 'show', '--name', $RegistryName, '--subscription', $Context.SubscriptionId) `
        -Operation 'Resolving the registry login server after publication'
    if (-not [string]::Equals([string] $registry.resourceGroup, $ResourceGroupName, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The published image registry is not in the selected Helios resource group.'
    }
    $immutableReference = "$($registry.loginServer)/$ImageRepository@$digest"
    $verified = Assert-ImmutableAcrImage `
        -Reference $immutableReference `
        -SelectedSubscriptionId $Context.SubscriptionId `
        -SelectedResourceGroup $ResourceGroupName
    return $verified
}

function Get-ExistingEntraContext {
    $connectorApplication = Find-EntraApplication -DisplayName $ConnectorAppName
    $githubApplication = Find-EntraApplication -DisplayName $GitHubOidcAppName
    return [pscustomobject]@{
        Connector = $connectorApplication
        GitHub = $githubApplication
    }
}

function Invoke-BicepPreview {
    param(
        [Parameter(Mandatory)] [pscustomobject] $Context,
        [Parameter(Mandatory)] [string] $ResourceGroupName,
        [Parameter(Mandatory)] [string] $ConnectorClientId,
        [Parameter(Mandatory)] [string] $PrincipalObjectId,
        [Parameter(Mandatory)] [string] $ImmutableImage,
        [Parameter(Mandatory)] [string] $RegistryName,
        [Parameter(Mandatory)] [string] $TemplatePath
    )

    $isPreviewPlaceholder = $ImmutableImage.EndsWith('@sha256:' + ('0' * 64), [StringComparison]::OrdinalIgnoreCase)
    $parameters = @(
        "environmentName=$EnvironmentName",
        "containerImage=$ImmutableImage",
        "containerRegistryName=$RegistryName",
        "allowPreviewPlaceholder=$($isPreviewPlaceholder.ToString().ToLowerInvariant())",
        "entraClientId=$ConnectorClientId",
        "entraTenantId=$($Context.TenantId)",
        "allowedPrincipalObjectId=$PrincipalObjectId"
    )

    $validateArguments = @(
        'deployment', 'group', 'validate',
        '--subscription', $Context.SubscriptionId,
        '--resource-group', $ResourceGroupName,
        '--template-file', $TemplatePath,
        '--parameters'
    ) + $parameters
    [void] (Invoke-AzJson `
        -Arguments $validateArguments `
        -Operation 'Validating the Helios Azure deployment')
    Write-Host 'Bicep validation passed.'

    $whatIfArguments = @(
        'deployment', 'group', 'what-if',
        '--subscription', $Context.SubscriptionId,
        '--resource-group', $ResourceGroupName,
        '--template-file', $TemplatePath,
        '--no-pretty-print',
        '--parameters'
    ) + $parameters
    $whatIf = Invoke-AzJson `
        -Arguments $whatIfArguments `
        -Operation 'Running the Helios Azure what-if'

    $changes = @($whatIf.changes)
    if ($changes.Count -eq 0) {
        Write-Host 'Azure what-if completed: no resource changes were reported.'
    }
    else {
        Write-Host "Azure what-if completed: $($changes.Count) resource change(s)."
        $changes | Group-Object changeType | Sort-Object Name | ForEach-Object {
            Write-Host ('  {0}: {1}' -f $_.Name, $_.Count)
        }
    }

    return $parameters
}

try {
    if ($PSVersionTable.PSVersion.Major -lt 7) {
        throw 'PowerShell 7 or later is required.'
    }

    $azCommand = Get-Command az -ErrorAction SilentlyContinue
    if (-not $azCommand) {
        throw 'Azure CLI is required. Install it from Microsoft before running this wizard.'
    }
    $script:AzPath = $azCommand.Source

    if ([string]::IsNullOrWhiteSpace($GitHubEnvironment)) {
        $GitHubEnvironment = "azure-$EnvironmentName"
    }
    if ($GitHubOwner -notmatch '^[A-Za-z0-9](?:[A-Za-z0-9-]{0,38})$') {
        throw 'GitHubOwner is not a valid GitHub account or organization name.'
    }
    if ($GitHubRepository -notmatch '^[A-Za-z0-9_.-]+$') {
        throw 'GitHubRepository contains unsupported characters.'
    }
    if ($GitHubEnvironment -notmatch '^[A-Za-z0-9_.-]+$') {
        throw 'GitHubEnvironment contains unsupported characters.'
    }
    if ($GitHubDeploymentBranch -notmatch '^[A-Za-z0-9._/-]+$') {
        throw 'GitHubDeploymentBranch contains unsupported characters.'
    }

    Write-Host 'Helios Azure interactive connector'
    Write-Host "Mode: $Mode (Plan is the non-mutating default)"
    Write-Host 'Azure CLI manages sign-in state. The wizard never requests, exports, logs, or writes raw tokens, client secrets, API keys, or model credentials.'

    $root = Split-Path $PSScriptRoot -Parent
    $template = Join-Path $root 'infra/connector.bicep'
    if (-not (Test-Path -LiteralPath $template -PathType Leaf)) {
        throw "Bicep template not found: $template"
    }

    Connect-AzureInteractively
    $azureContext = Select-AzureAccount
    [void] (Invoke-AzJson `
        -Arguments @('bicep', 'build', '--file', $template, '--stdout') `
        -Operation 'Compiling the Helios Bicep template before any cloud mutation')

    if ($Mode -eq 'Configure') {
        Assert-GitHubCliReady
    }
    $githubOidcSubject = if ($Mode -eq 'Configure') {
        Resolve-GitHubOidcSubject `
            -Owner $GitHubOwner `
            -Repository $GitHubRepository `
            -Environment $GitHubEnvironment
    }
    else {
        $null
    }

    $principalObjectId = Resolve-AllowedPrincipalObjectId
    $selectedGroup = Select-ResourceGroup -Context $azureContext
    $selectedResourceGroup = [string] $selectedGroup.name
    $selectedLocation = [string] $selectedGroup.location

    $imageForRegistryResolution = if ([string]::IsNullOrWhiteSpace($ImageReference)) { $null } else { $ImageReference }
    $registrySelection = Resolve-ContainerRegistryName `
        -Context $azureContext `
        -ResourceGroupName $selectedResourceGroup `
        -ResourceLocation $selectedLocation `
        -Image $imageForRegistryResolution
    $resolvedRegistryName = [string] $registrySelection.Name

    if ($Mode -eq 'Configure') {
        Write-Host 'Configuration plan:'
        Write-Host "  Azure: $($azureContext.SubscriptionName) / tenant $($azureContext.TenantId)"
        Write-Host "  resource group: $selectedResourceGroup ($selectedLocation)"
        Write-Host "  container registry binding: $resolvedRegistryName"
        Write-Host "  Entra apps: '$ConnectorAppName' and '$GitHubOidcAppName'"
        Write-Host "  GitHub OIDC: $githubOidcSubject"
        Write-Host '  CI RBAC: Contributor at the selected resource-group scope only (no role-assignment authority)'
        Write-Host '  runtime RBAC: pre-created user-assigned identity with Reader; ACR pull is added during Publish'
        Write-Host "  GitHub environment: $GitHubOwner/$GitHubRepository / $GitHubEnvironment; reviewer-gated; branch $GitHubDeploymentBranch"
        Assert-ExactConfirmation -Expected 'CONFIGURE HELIOS AZURE' -Purpose 'Configuring Helios Entra, OIDC, RBAC, and GitHub bindings'
        Ensure-RequiredResourceProviders -Context $azureContext
    }

    if ($Mode -eq 'Publish') {
        $runtimeIdentity = Get-RuntimeManagedIdentity -Context $azureContext -ResourceGroupName $selectedResourceGroup
        $publishGitHubApplication = Find-EntraApplication -DisplayName $GitHubOidcAppName
        if (-not $publishGitHubApplication) {
            throw "GitHub OIDC app '$GitHubOidcAppName' is missing. Run -Mode Configure before Publish."
        }
        $resourceGroupScope = "/subscriptions/$($azureContext.SubscriptionId)/resourceGroups/$selectedResourceGroup"
        Assert-PrincipalRoleAssignment `
            -PrincipalObjectId ([string] $runtimeIdentity.principalId) `
            -Role $script:ReaderRole.Id `
            -Scope $resourceGroupScope
        $buildInput = Resolve-SafeCloudBuildContext -ProjectRoot $root
        if (-not $registrySelection.Exists) {
            [void] (New-ConfirmedContainerRegistry `
                -Context $azureContext `
                -ResourceGroupName $selectedResourceGroup `
                -LocationName $selectedLocation `
                -Name $resolvedRegistryName)
        }
        try {
            $immutableImage = Publish-HeliosCloudImage `
                -Context $azureContext `
                -ResourceGroupName $selectedResourceGroup `
                -RegistryName $resolvedRegistryName `
                -BuildInput $buildInput
        }
        finally {
            if ([bool] $buildInput.Temporary -and (Test-Path -LiteralPath $buildInput.Context)) {
                Remove-Item -LiteralPath $buildInput.Context -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
        Ensure-RegistryPullAccess `
            -Context $azureContext `
            -ResourceGroupName $selectedResourceGroup `
            -RegistryName $resolvedRegistryName `
            -Identity $runtimeIdentity `
            -GitHubApplicationId ([string] $publishGitHubApplication.appId)
        Write-Host "Published immutable image: $immutableImage"
    }
    elseif ([string]::IsNullOrWhiteSpace($ImageReference)) {
        # Keep preview-only Bicep validation bound to the selected registry.
        # A fixed placeholder registry would violate the template's exact
        # registry assertion and make fresh Plan/Configure runs fail.
        $immutableImage = "$resolvedRegistryName.azurecr.io/helios-connect@$($script:PlaceholderDigest)"
        Write-Warning 'No image digest was supplied. This preview uses an all-zero placeholder that the protected deployment workflow rejects.'
    }
    else {
        $immutableImage = Assert-ImmutableAcrImage `
            -Reference $ImageReference `
            -SelectedSubscriptionId $azureContext.SubscriptionId `
            -SelectedResourceGroup $selectedResourceGroup
        $registryFromImage = Get-RegistryNameFromImageReference -Reference $immutableImage
        if (-not [string]::Equals($registryFromImage, $resolvedRegistryName, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The verified image registry does not match the selected Helios container registry.'
        }
    }

    $entraContext = Get-ExistingEntraContext
    if ($Mode -eq 'Configure') {
        $connectorApplication = Ensure-EntraApplication -DisplayName $ConnectorAppName -ExposeDelegatedScope
        $githubApplication = Ensure-EntraApplication -DisplayName $GitHubOidcAppName
        $runtimeIdentity = Ensure-RuntimeManagedIdentity `
            -Context $azureContext `
            -ResourceGroupName $selectedResourceGroup `
            -LocationName $selectedLocation

        $resourceGroupScope = "/subscriptions/$($azureContext.SubscriptionId)/resourceGroups/$selectedResourceGroup"
        Ensure-RoleAssignment -ApplicationId $githubApplication.appId -Scope $resourceGroupScope

        $githubValues = @{
            AZURE_CLIENT_ID = [string] $githubApplication.appId
            AZURE_TENANT_ID = [string] $azureContext.TenantId
            AZURE_SUBSCRIPTION_ID = [string] $azureContext.SubscriptionId
            AZURE_RESOURCE_GROUP = $selectedResourceGroup
            HELIOS_CONTAINER_REGISTRY_NAME = $resolvedRegistryName
            HELIOS_ENTRA_CLIENT_ID = [string] $connectorApplication.appId
            HELIOS_ALLOWED_PRINCIPAL_OBJECT_ID = $principalObjectId
            HELIOS_REQUIRED_REVIEWER_ID = $RequiredReviewerId
            HELIOS_OIDC_SUBJECT = $githubOidcSubject
        }
        Set-GitHubEnvironmentVariables `
            -Values $githubValues `
            -Owner $GitHubOwner `
            -Repository $GitHubRepository `
            -Environment $GitHubEnvironment
        Ensure-GitHubFederation `
            -Application $githubApplication `
            -Subject $githubOidcSubject `
            -Environment $GitHubEnvironment
    }
    else {
        $connectorApplication = $entraContext.Connector
        $githubApplication = $entraContext.GitHub
    }

    if (-not $connectorApplication) {
        if ($Mode -in @('Plan', 'Publish')) {
            Write-Warning "Connector app '$ConnectorAppName' does not exist. Bicep preview will use a non-persisted placeholder client ID."
            $connectorClientId = $script:PlaceholderClientId
        }
        else {
            throw "Connector app '$ConnectorAppName' does not exist. Run -Mode Configure first."
        }
    }
    else {
        $connectorClientId = [string] $connectorApplication.appId
    }

    Write-Host 'Reviewed Azure target:'
    Write-Host "  tenant: $($azureContext.TenantId)"
    Write-Host "  subscription: $($azureContext.SubscriptionName) ($($azureContext.SubscriptionId))"
    Write-Host "  resource group: $selectedResourceGroup ($selectedLocation)"
    Write-Host "  container registry: $resolvedRegistryName"
    Write-Host "  environment: $EnvironmentName"
    Write-Host "  GitHub trust environment: $GitHubEnvironment"
    Write-Host '  runtime: Azure Container Apps only; local runtime disabled'

    [void] (Invoke-BicepPreview `
        -Context $azureContext `
        -ResourceGroupName $selectedResourceGroup `
        -ConnectorClientId $connectorClientId `
        -PrincipalObjectId $principalObjectId `
        -ImmutableImage $immutableImage `
        -RegistryName $resolvedRegistryName `
        -TemplatePath $template)

    if ($Mode -eq 'Configure') {
        Write-Host 'Configuration and what-if are complete. No application deployment was performed.'
        Write-Host 'Run -Mode Publish to build in ACR, then dispatch the protected helios-cloud-deploy workflow with the immutable digest.'
    }
    elseif ($Mode -eq 'Publish') {
        Write-Host "HELIOS_CONTAINER_REGISTRY_NAME=$resolvedRegistryName"
        Write-Host "HELIOS_CONTAINER_IMAGE=$immutableImage"
        Write-Host 'Cloud publication and what-if are complete. No application deployment was performed.'
        Write-Host 'Use the protected helios-cloud-deploy workflow for reviewed what-if and deployment; this operator script has no direct apply path.'
    }
    else {
        Write-Host 'Plan complete. No Azure, Entra, GitHub, or application resources were changed.'
    }

    Write-Host 'No OpenAI, Azure OpenAI, Anthropic, GitHub, or Microsoft 365 secret was created, retrieved, printed, or written by Helios.'
}
catch {
    $safeMessage = Protect-DiagnosticText $_.Exception.Message
    Write-Error $safeMessage
    exit 1
}

#requires -Version 7.0

<#
.SYNOPSIS
Runs the remaining Helios cloud setup steps with guided, approval-preserving automation.

.DESCRIPTION
This wrapper keeps all existing protection boundaries in place:
- Configure/Publish still run through Connect-HeliosAzureInteractive.ps1.
- Deploy still runs through the protected helios-cloud-deploy workflow.
- Environment approvals, reviewer checks, and exact confirmations remain required.

The script helps close the last operational blockers by checking readiness, resolving
or validating a distinct reviewer ID, dispatching deploy mode intentionally,
and binding HELIOS_AZURE_CONNECTOR_URL after deployment.
#>

[CmdletBinding()]
param(
    [ValidateSet('Status', 'Configure', 'Publish', 'Deploy', 'Finalize', 'Verify')]
    [string] $Mode = 'Status',

    [ValidateSet('dev', 'test', 'prod')]
    [string] $EnvironmentName = 'dev',

    [string] $GitHubOwner = 'M0nado',
    [string] $GitHubRepository = 'helios-platform',
    [string] $GitHubDeploymentBranch = 'main',

    [string] $ResourceGroup,
    [string] $ContainerRegistryName,
    [string] $RequiredReviewerId,

    [switch] $UseDeviceCode,
    [switch] $PersistLocalEnv,
    [switch] $InteractiveAuth,
    [switch] $Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$script:ConnectScript = Join-Path $PSScriptRoot 'Connect-HeliosAzureInteractive.ps1'
$script:VerifyScript = Join-Path $PSScriptRoot 'Test-HeliosCloudConnection.ps1'
$script:RepositoryName = "$GitHubOwner/$GitHubRepository"
$script:TargetEnvironment = "azure-$EnvironmentName"
$script:GhPath = $null
$script:AzPath = $null
$script:RequiredWorkflowVariables = @(
    'AZURE_CLIENT_ID',
    'AZURE_TENANT_ID',
    'AZURE_SUBSCRIPTION_ID',
    'AZURE_RESOURCE_GROUP',
    'HELIOS_ENTRA_CLIENT_ID',
    'HELIOS_ALLOWED_PRINCIPAL_OBJECT_ID',
    'HELIOS_CONTAINER_REGISTRY_NAME'
)
$script:OptionalControlVariables = @(
    'HELIOS_REQUIRED_REVIEWER_ID',
    'HELIOS_OIDC_SUBJECT'
)

function Protect-DiagnosticText {
    param([AllowEmptyString()] [string] $Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return ''
    }

    $protected = $Text
    $protected = $protected -replace '(?i)(authorization\s*:\s*bearer\s+)[^\s]+', '$1[REDACTED]'
    $protected = $protected -replace '(?i)(access[_-]?token|client[_-]?secret|api[_-]?key)(\s*[=:]\s*)[^\s,;]+', '$1$2[REDACTED]'
    $protected = $protected -replace 'eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+', '[REDACTED_JWT]'
    return $protected.Trim()
}

function Get-RequiredCommandPath {
    param(
        [Parameter(Mandatory)] [string] $CommandName,
        [Parameter(Mandatory)] [string] $Purpose
    )

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "$Purpose requires '$CommandName' on PATH."
    }
    return $command.Source
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)] [string] $FilePath,
        [Parameter(Mandatory)] [string[]] $ArgumentList,
        [Parameter(Mandatory)] [string] $Operation,
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

function Invoke-GhNoOutput {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $Operation
    )

    if (-not $script:GhPath) {
        $script:GhPath = Get-RequiredCommandPath -CommandName 'gh' -Purpose 'GitHub operations'
    }

    [void] (Invoke-NativeCommand `
        -FilePath $script:GhPath `
        -ArgumentList $Arguments `
        -Operation $Operation `
        -AllowEmptyOutput)
}

function Invoke-GhJson {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $Operation
    )

    if (-not $script:GhPath) {
        $script:GhPath = Get-RequiredCommandPath -CommandName 'gh' -Purpose 'GitHub operations'
    }

    $raw = Invoke-NativeCommand `
        -FilePath $script:GhPath `
        -ArgumentList $Arguments `
        -Operation $Operation
    try {
        return $raw | ConvertFrom-Json -Depth 100
    }
    catch {
        throw "$Operation returned malformed JSON."
    }
}

function Invoke-AzNoOutput {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $Operation
    )

    if (-not $script:AzPath) {
        $script:AzPath = Get-RequiredCommandPath -CommandName 'az' -Purpose 'Azure operations'
    }

    [void] (Invoke-NativeCommand `
        -FilePath $script:AzPath `
        -ArgumentList ($Arguments + @('--only-show-errors', '--output', 'none')) `
        -Operation $Operation `
        -AllowEmptyOutput)
}

function Invoke-AzText {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $Operation
    )

    if (-not $script:AzPath) {
        $script:AzPath = Get-RequiredCommandPath -CommandName 'az' -Purpose 'Azure operations'
    }

    return (Invoke-NativeCommand `
        -FilePath $script:AzPath `
        -ArgumentList ($Arguments + @('--only-show-errors')) `
        -Operation $Operation `
        -AllowEmptyOutput).Trim()
}

function Assert-GitHubCliReady {
    if (-not $script:GhPath) {
        $script:GhPath = Get-RequiredCommandPath -CommandName 'gh' -Purpose 'GitHub operations'
    }
    Invoke-GhNoOutput `
        -Arguments @('auth', 'status', '--hostname', 'github.com') `
        -Operation 'Checking GitHub CLI authentication'
}

function Test-GitHubCliAuthenticated {
    try {
        Assert-GitHubCliReady
        return $true
    }
    catch {
        return $false
    }
}

function Test-AzureCliAuthenticated {
    try {
        Invoke-AzNoOutput `
            -Arguments @('account', 'show') `
            -Operation 'Checking Azure CLI authentication'
        return $true
    }
    catch {
        return $false
    }
}

function Test-GitHubEnvironmentExists {
    Assert-GitHubCliReady
    $escapedEnvironment = [uri]::EscapeDataString($script:TargetEnvironment)
    try {
        [void] (Invoke-GhJson `
            -Arguments @('api', '--method', 'GET', "repos/$($script:RepositoryName)/environments/$escapedEnvironment") `
            -Operation "Checking GitHub environment '$($script:TargetEnvironment)'")
        return $true
    }
    catch {
        if ($_.Exception.Message -like '*HTTP 404*') {
            return $false
        }
        throw
    }
}

function Get-GitHubEnvironmentVariables {
    Assert-GitHubCliReady

    $variables = @(Invoke-GhJson `
        -Arguments @(
            'variable', 'list',
            '--repo', $script:RepositoryName,
            '--env', $script:TargetEnvironment,
            '--json', 'name,value'
        ) `
        -Operation "Reading GitHub environment variables for '$($script:TargetEnvironment)'")

    $map = @{}
    foreach ($entry in $variables) {
        $name = [string] $entry.name
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            $map[$name] = [string] $entry.value
        }
    }
    return $map
}

function Get-AuthenticatedGitHubUser {
    Assert-GitHubCliReady
    $user = Invoke-GhJson `
        -Arguments @('api', '--method', 'GET', 'user') `
        -Operation 'Resolving the authenticated GitHub user'

    [int64] $userId = 0
    if (-not [int64]::TryParse([string] $user.id, [ref] $userId) -or $userId -le 0) {
        throw 'GitHub did not return a valid authenticated user ID.'
    }
    $login = [string] $user.login
    if ([string]::IsNullOrWhiteSpace($login)) {
        throw 'GitHub did not return an authenticated user login.'
    }

    return [pscustomobject]@{
        Id = $userId
        Login = $login
    }
}

function ConvertTo-ReviewerId {
    param(
        [Parameter(Mandatory)] [string] $Value,
        [Parameter(Mandatory)] [string] $Source
    )

    [int64] $parsed = 0
    if (-not [int64]::TryParse($Value.Trim(), [ref] $parsed) -or $parsed -le 0) {
        throw "$Source must be a positive numeric GitHub user ID."
    }
    return [string] $parsed
}

function Get-GitHubReviewerId {
    param([hashtable] $Variables = @{})

    $dispatcher = Get-AuthenticatedGitHubUser
    $candidate = $null
    $source = $null

    if (-not [string]::IsNullOrWhiteSpace($RequiredReviewerId)) {
        $candidate = ConvertTo-ReviewerId -Value $RequiredReviewerId -Source 'RequiredReviewerId'
        $source = 'RequiredReviewerId argument'
    }
    elseif ($Variables.ContainsKey('HELIOS_REQUIRED_REVIEWER_ID') -and
        -not [string]::IsNullOrWhiteSpace([string] $Variables['HELIOS_REQUIRED_REVIEWER_ID'])) {
        $candidate = ConvertTo-ReviewerId `
            -Value ([string] $Variables['HELIOS_REQUIRED_REVIEWER_ID']) `
            -Source 'HELIOS_REQUIRED_REVIEWER_ID'
        $source = 'HELIOS_REQUIRED_REVIEWER_ID'
    }
    else {
        Write-Host "Authenticated dispatcher: $($dispatcher.Login) ($($dispatcher.Id))"
        Write-Host 'A distinct reviewer is required because protected environments enforce prevent_self_review=true.'
        $entered = Read-Host 'Required reviewer GitHub user ID'
        $candidate = ConvertTo-ReviewerId -Value $entered -Source 'Interactive reviewer input'
        $source = 'interactive input'
    }

    if ([int64] $candidate -eq $dispatcher.Id) {
        throw "Required reviewer ID '$candidate' matches the authenticated dispatcher '$($dispatcher.Login)'. Select a different reviewer."
    }

    Write-Host "Using GitHub reviewer ID: $candidate ($source)"
    return $candidate
}

function Assert-GitHubEnvironmentProtection {
    param([Parameter(Mandatory)] [int64] $ReviewerId)

    Assert-GitHubCliReady
    $escapedEnvironment = [uri]::EscapeDataString($script:TargetEnvironment)
    $definition = Invoke-GhJson `
        -Arguments @('api', '--method', 'GET', "repos/$($script:RepositoryName)/environments/$escapedEnvironment") `
        -Operation "Verifying protected GitHub environment '$($script:TargetEnvironment)'"
    $reviewerRules = @($definition.protection_rules | Where-Object type -eq 'required_reviewers')
    $matchingReviewers = @($reviewerRules.reviewers | Where-Object { [int64] $_.reviewer.id -eq $ReviewerId })
    if ($reviewerRules.Count -ne 1 -or
        @($reviewerRules[0].reviewers).Count -ne 1 -or
        $matchingReviewers.Count -ne 1 -or
        -not [bool] $reviewerRules[0].prevent_self_review -or
        -not [bool] $definition.deployment_branch_policy.custom_branch_policies -or
        [bool] $definition.deployment_branch_policy.protected_branches) {
        throw "GitHub environment '$($script:TargetEnvironment)' is not fail-closed with the exact required reviewer and custom branch policy."
    }

    $policies = Invoke-GhJson `
        -Arguments @('api', '--method', 'GET', "repos/$($script:RepositoryName)/environments/$escapedEnvironment/deployment-branch-policies") `
        -Operation "Verifying deployment branch policy for '$($script:TargetEnvironment)'"
    $matchingPolicies = @($policies.branch_policies | Where-Object {
        $_.name -eq $GitHubDeploymentBranch -and $_.type -eq 'branch'
    })
    if (@($policies.branch_policies).Count -ne 1 -or $matchingPolicies.Count -ne 1) {
        throw "GitHub environment '$($script:TargetEnvironment)' is not restricted to branch '$GitHubDeploymentBranch'."
    }
}

function Assert-ExactConfirmation {
    param(
        [Parameter(Mandatory)] [string] $Expected,
        [Parameter(Mandatory)] [string] $Purpose
    )

    Write-Warning "$Purpose requires explicit confirmation."
    $answer = Read-Host "Type exactly '$Expected' to continue"
    if (-not [string]::Equals($answer, $Expected, [StringComparison]::Ordinal)) {
        throw "$Purpose was not confirmed."
    }
}

function Get-StatusReport {
    $githubAuthenticated = Test-GitHubCliAuthenticated
    $azureAuthenticated = Test-AzureCliAuthenticated
    $environmentExists = $false
    $variables = @{}
    if ($githubAuthenticated) {
        $environmentExists = Test-GitHubEnvironmentExists
        if ($environmentExists) {
            $variables = Get-GitHubEnvironmentVariables
        }
    }

    $configured = [ordered]@{}
    $missing = [System.Collections.Generic.List[string]]::new()
    foreach ($name in $script:RequiredWorkflowVariables) {
        $value = if ($variables.ContainsKey($name)) { [string] $variables[$name] } else { '' }
        $isConfigured = -not [string]::IsNullOrWhiteSpace($value)
        $configured[$name] = $isConfigured
        if (-not $isConfigured) {
            $missing.Add($name)
        }
    }

    $optionalConfigured = [ordered]@{}
    $optionalMissing = [System.Collections.Generic.List[string]]::new()
    foreach ($name in $script:OptionalControlVariables) {
        $value = if ($variables.ContainsKey($name)) { [string] $variables[$name] } else { '' }
        $isConfigured = -not [string]::IsNullOrWhiteSpace($value)
        $optionalConfigured[$name] = $isConfigured
        if (-not $isConfigured) {
            $optionalMissing.Add($name)
        }
    }

    $connectorUrlConfigured = (
        $variables.ContainsKey('HELIOS_AZURE_CONNECTOR_URL') -and
        -not [string]::IsNullOrWhiteSpace([string] $variables['HELIOS_AZURE_CONNECTOR_URL'])
    )

    $blockers = [System.Collections.Generic.List[string]]::new()
    if (-not $githubAuthenticated) {
        $blockers.Add('GitHub CLI is not authenticated.')
    }
    if (-not $azureAuthenticated) {
        $blockers.Add('Azure CLI is not authenticated.')
    }
    if (-not $environmentExists) {
        $blockers.Add("GitHub environment '$($script:TargetEnvironment)' does not exist.")
    }
    if ($missing.Count -gt 0) {
        $blockers.Add("Protected workflow variables are missing: $($missing -join ', ')")
    }
    if ($githubAuthenticated -and $environmentExists -and
        $variables.ContainsKey('HELIOS_REQUIRED_REVIEWER_ID') -and
        -not [string]::IsNullOrWhiteSpace([string] $variables['HELIOS_REQUIRED_REVIEWER_ID'])) {
        try {
            $dispatcher = Get-AuthenticatedGitHubUser
            $reviewerId = [int64] (ConvertTo-ReviewerId `
                -Value ([string] $variables['HELIOS_REQUIRED_REVIEWER_ID']) `
                -Source 'HELIOS_REQUIRED_REVIEWER_ID')
            if ($reviewerId -eq $dispatcher.Id) {
                $blockers.Add('HELIOS_REQUIRED_REVIEWER_ID matches the authenticated dispatcher; protected approvals will stall.')
            }
        }
        catch {
            $blockers.Add('HELIOS_REQUIRED_REVIEWER_ID is invalid and must be a positive numeric GitHub user ID.')
        }
    }
    if (-not $connectorUrlConfigured) {
        $blockers.Add('HELIOS_AZURE_CONNECTOR_URL is not configured.')
    }

    return [pscustomobject][ordered]@{
        mode = $Mode
        repository = $script:RepositoryName
        targetEnvironment = $script:TargetEnvironment
        githubCliAuthenticated = $githubAuthenticated
        azureCliAuthenticated = $azureAuthenticated
        githubEnvironmentExists = $environmentExists
        requiredVariablesConfigured = $configured
        missingRequiredVariables = @($missing)
        optionalVariablesConfigured = $optionalConfigured
        missingOptionalVariables = @($optionalMissing)
        connectorUrlConfigured = $connectorUrlConfigured
        blockers = @($blockers)
    }
}

function Write-StatusReport {
    param([Parameter(Mandatory)] [pscustomobject] $Report)

    Write-Host 'Helios cloud setup status'
    Write-Host "  repository: $($Report.repository)"
    Write-Host "  GitHub environment: $($Report.targetEnvironment)"
    Write-Host "  GitHub CLI authenticated: $($Report.githubCliAuthenticated)"
    Write-Host "  Azure CLI authenticated: $($Report.azureCliAuthenticated)"
    Write-Host "  environment exists: $($Report.githubEnvironmentExists)"
    Write-Host "  connector URL configured: $($Report.connectorUrlConfigured)"
    if ($Report.missingRequiredVariables.Count -gt 0) {
        Write-Host "  missing required variables: $($Report.missingRequiredVariables -join ', ')"
    }
    else {
        Write-Host '  missing required variables: none'
    }
    if ($Report.missingOptionalVariables.Count -gt 0) {
        Write-Host "  missing optional control variables: $($Report.missingOptionalVariables -join ', ')"
    }
    else {
        Write-Host '  missing optional control variables: none'
    }
    if ($Report.blockers.Count -gt 0) {
        Write-Host '  blockers:'
        foreach ($blocker in $Report.blockers) {
            Write-Host "    - $blocker"
        }
    }
    else {
        Write-Host '  blockers: none'
    }
}

function Invoke-ConnectMode {
    param(
        [Parameter(Mandatory)] [ValidateSet('Configure', 'Publish')] [string] $ConnectMode,
        [Parameter(Mandatory)] [string] $ReviewerId,
        [string] $ResourceGroupName,
        [string] $ContainerRegistryNameOverride
    )

    if (-not (Test-Path -LiteralPath $script:ConnectScript -PathType Leaf)) {
        throw "Required script not found: $($script:ConnectScript)"
    }

    $arguments = @(
        '-NoLogo',
        '-NoProfile',
        '-File', $script:ConnectScript,
        '-Mode', $ConnectMode,
        '-EnvironmentName', $EnvironmentName,
        '-GitHubOwner', $GitHubOwner,
        '-GitHubRepository', $GitHubRepository,
        '-GitHubDeploymentBranch', $GitHubDeploymentBranch,
        '-RequiredReviewerId', $ReviewerId
    )
    $effectiveResourceGroup = if (-not [string]::IsNullOrWhiteSpace($ResourceGroupName)) { $ResourceGroupName } else { $ResourceGroup }
    if (-not [string]::IsNullOrWhiteSpace($effectiveResourceGroup)) {
        $arguments += @('-ResourceGroup', $effectiveResourceGroup)
    }
    $effectiveRegistryName = if (-not [string]::IsNullOrWhiteSpace($ContainerRegistryNameOverride)) { $ContainerRegistryNameOverride } else { $ContainerRegistryName }
    if (-not [string]::IsNullOrWhiteSpace($effectiveRegistryName)) {
        $arguments += @('-ContainerRegistryName', $effectiveRegistryName)
    }
    if ($UseDeviceCode) {
        $arguments += '-UseDeviceCode'
    }

    & pwsh @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Connect-HeliosAzureInteractive.ps1 -Mode $ConnectMode failed."
    }
}

function Get-LatestWorkflowRun {
    Assert-GitHubCliReady
    $runs = @(Invoke-GhJson `
        -Arguments @(
            'run', 'list',
            '--repo', $script:RepositoryName,
            '--workflow', 'helios-cloud-deploy.yml',
            '--event', 'workflow_dispatch',
            '--branch', $GitHubDeploymentBranch,
            '--limit', '1',
            '--json', 'databaseId,url,status,conclusion,displayTitle'
        ) `
        -Operation 'Reading the latest helios-cloud-deploy workflow run')
    if ($runs.Count -eq 0) {
        return $null
    }
    return $runs[0]
}

function Dispatch-DeployWorkflow {
    param([Parameter(Mandatory)] [string] $ReviewerId)

    Assert-GitHubCliReady
    Assert-GitHubEnvironmentProtection -ReviewerId ([int64] $ReviewerId)
    Assert-ExactConfirmation `
        -Expected 'DISPATCH HELIOS DEPLOY' `
        -Purpose "Dispatching deploy mode for '$($script:TargetEnvironment)'"
    Invoke-GhNoOutput `
        -Arguments @(
            'workflow', 'run', 'helios-cloud-deploy.yml',
            '--repo', $script:RepositoryName,
            '--ref', $GitHubDeploymentBranch,
            '--field', "targetEnvironment=$($script:TargetEnvironment)",
            '--field', 'mode=deploy',
            '--field', 'confirmDeployment=DEPLOY'
        ) `
        -Operation 'Dispatching helios-cloud-deploy in deploy mode'

    $latestRun = Get-LatestWorkflowRun
    if ($latestRun) {
        Write-Host "Deploy workflow dispatched: $($latestRun.url)"
    }
    else {
        Write-Host 'Deploy workflow dispatched.'
    }
}

function Get-RequiredVariableValue {
    param(
        [Parameter(Mandatory)] [hashtable] $Variables,
        [Parameter(Mandatory)] [string] $Name
    )

    if (-not $Variables.ContainsKey($Name) -or [string]::IsNullOrWhiteSpace([string] $Variables[$Name])) {
        throw "GitHub environment variable '$Name' is required but not configured."
    }
    return [string] $Variables[$Name]
}

function Resolve-ProtectedResourceGroupName {
    param([Parameter(Mandatory)] [hashtable] $Variables)

    $configuredResourceGroup = Get-RequiredVariableValue -Variables $Variables -Name 'AZURE_RESOURCE_GROUP'
    if (-not [string]::IsNullOrWhiteSpace($ResourceGroup) -and
        -not [string]::Equals($ResourceGroup, $configuredResourceGroup, [StringComparison]::OrdinalIgnoreCase)) {
        throw "ResourceGroup override '$ResourceGroup' does not match protected AZURE_RESOURCE_GROUP '$configuredResourceGroup'."
    }
    return $configuredResourceGroup
}

function Resolve-ProtectedRegistryName {
    param([Parameter(Mandatory)] [hashtable] $Variables)

    $configuredRegistry = Get-RequiredVariableValue -Variables $Variables -Name 'HELIOS_CONTAINER_REGISTRY_NAME'
    if (-not [string]::IsNullOrWhiteSpace($ContainerRegistryName) -and
        -not [string]::Equals($ContainerRegistryName, $configuredRegistry, [StringComparison]::OrdinalIgnoreCase)) {
        throw "ContainerRegistryName override '$ContainerRegistryName' does not match protected HELIOS_CONTAINER_REGISTRY_NAME '$configuredRegistry'."
    }
    return $configuredRegistry
}

function Resolve-ConnectorUrlFromAzure {
    param([Parameter(Mandatory)] [hashtable] $Variables)

    $subscriptionId = Get-RequiredVariableValue -Variables $Variables -Name 'AZURE_SUBSCRIPTION_ID'
    $resourceGroupName = Resolve-ProtectedResourceGroupName -Variables $Variables

    Invoke-AzNoOutput `
        -Arguments @('account', 'show') `
        -Operation 'Checking Azure CLI authentication before connector discovery'
    Invoke-AzNoOutput `
        -Arguments @('account', 'set', '--subscription', $subscriptionId) `
        -Operation "Selecting subscription '$subscriptionId'"

    $containerAppName = "helios-connector-$EnvironmentName-api"
    $fqdn = Invoke-AzText `
        -Arguments @(
            'containerapp', 'show',
            '--subscription', $subscriptionId,
            '--resource-group', $resourceGroupName,
            '--name', $containerAppName,
            '--query', 'properties.configuration.ingress.fqdn',
            '--output', 'tsv'
        ) `
        -Operation "Resolving the deployed connector hostname from '$containerAppName'"

    if ([string]::IsNullOrWhiteSpace($fqdn) -or $fqdn -eq 'null') {
        throw "Container App '$containerAppName' did not return a hostname."
    }
    if ([Uri]::CheckHostName($fqdn) -ne [UriHostNameType]::Dns) {
        throw "Container App '$containerAppName' returned an invalid hostname."
    }

    return "https://$($fqdn.ToLowerInvariant())"
}

function Set-GitHubEnvironmentVariable {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Value
    )

    Assert-GitHubCliReady
    Invoke-GhNoOutput `
        -Arguments @(
            'variable', 'set', $Name,
            '--body', $Value,
            '--repo', $script:RepositoryName,
            '--env', $script:TargetEnvironment
        ) `
        -Operation "Setting GitHub environment variable '$Name'"
}

function Set-LocalEnvValue {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Value
    )

    $envPath = Join-Path $script:ProjectRoot '.env.local'
    $examplePath = Join-Path $script:ProjectRoot '.env.example'
    if (-not (Test-Path -LiteralPath $envPath -PathType Leaf)) {
        if (-not (Test-Path -LiteralPath $examplePath -PathType Leaf)) {
            throw "Cannot create .env.local because .env.example was not found at '$examplePath'."
        }
        Copy-Item -LiteralPath $examplePath -Destination $envPath -Force
    }

    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in (Get-Content -LiteralPath $envPath)) {
        $lines.Add([string] $line)
    }

    $pattern = "^{0}=" -f [regex]::Escape($Name)
    $updated = $false
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match $pattern) {
            $lines[$index] = "$Name=$Value"
            $updated = $true
            break
        }
    }
    if (-not $updated) {
        $lines.Add("$Name=$Value")
    }

    [System.IO.File]::WriteAllLines($envPath, $lines, [System.Text.UTF8Encoding]::new($false))
}

function Persist-LocalRuntimeValues {
    param(
        [Parameter(Mandatory)] [hashtable] $Variables,
        [string] $ConnectorUrl
    )

    if (-not $PersistLocalEnv) {
        return
    }

    foreach ($name in @(
        'AZURE_CLIENT_ID',
        'AZURE_TENANT_ID',
        'AZURE_SUBSCRIPTION_ID',
        'AZURE_RESOURCE_GROUP',
        'HELIOS_ENTRA_CLIENT_ID',
        'HELIOS_CONTAINER_REGISTRY_NAME'
    )) {
        if ($Variables.ContainsKey($name) -and -not [string]::IsNullOrWhiteSpace([string] $Variables[$name])) {
            Set-LocalEnvValue -Name $name -Value ([string] $Variables[$name])
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($ConnectorUrl)) {
        Set-LocalEnvValue -Name 'HELIOS_AZURE_CONNECTOR_URL' -Value $ConnectorUrl
    }
}

function Invoke-CloudVerification {
    param([Parameter(Mandatory)] [hashtable] $Variables)

    if (-not (Test-Path -LiteralPath $script:VerifyScript -PathType Leaf)) {
        throw "Required script not found: $($script:VerifyScript)"
    }

    $connectorUrl = Get-RequiredVariableValue -Variables $Variables -Name 'HELIOS_AZURE_CONNECTOR_URL'
    $entraClientId = Get-RequiredVariableValue -Variables $Variables -Name 'HELIOS_ENTRA_CLIENT_ID'
    $tenantId = Get-RequiredVariableValue -Variables $Variables -Name 'AZURE_TENANT_ID'

    $arguments = @(
        '-NoLogo',
        '-NoProfile',
        '-File', $script:VerifyScript,
        '-ConnectorUrl', $connectorUrl,
        '-EntraClientId', $entraClientId,
        '-TenantId', $tenantId
    )
    if ($InteractiveAuth) {
        $arguments += '-InteractiveAuth'
    }

    & pwsh @arguments
    if ($LASTEXITCODE -ne 0) {
        throw 'Test-HeliosCloudConnection.ps1 reported a failed verification.'
    }
}

if (-not (Test-Path -LiteralPath $script:ConnectScript -PathType Leaf)) {
    throw "Required script not found: $($script:ConnectScript)"
}

switch ($Mode) {
    'Status' {
        $report = Get-StatusReport
        if ($Json) {
            $report | ConvertTo-Json -Depth 10
        }
        else {
            Write-StatusReport -Report $report
        }
    }
    'Configure' {
        $reviewerId = Get-GitHubReviewerId
        Invoke-ConnectMode -ConnectMode 'Configure' -ReviewerId $reviewerId
        $variables = Get-GitHubEnvironmentVariables
        Persist-LocalRuntimeValues -Variables $variables
        $report = Get-StatusReport
        Write-StatusReport -Report $report
    }
    'Publish' {
        $variables = if (Test-GitHubEnvironmentExists) { Get-GitHubEnvironmentVariables } else { @{} }
        $reviewerId = Get-GitHubReviewerId -Variables $variables
        Invoke-ConnectMode -ConnectMode 'Publish' -ReviewerId $reviewerId
        $latestRun = Get-LatestWorkflowRun
        if ($latestRun) {
            Write-Host "Latest workflow run: $($latestRun.url)"
        }
    }
    'Deploy' {
        $variables = Get-GitHubEnvironmentVariables
        $reviewerId = Get-GitHubReviewerId -Variables $variables
        Dispatch-DeployWorkflow -ReviewerId $reviewerId
    }
    'Finalize' {
        $variables = Get-GitHubEnvironmentVariables
        $reviewerId = Get-GitHubReviewerId -Variables $variables
        $protectedResourceGroup = Resolve-ProtectedResourceGroupName -Variables $variables
        $protectedRegistryName = Resolve-ProtectedRegistryName -Variables $variables
        Write-Host 'Running Configure to rebind the domain-qualified Entra API identifier before verification.'
        Invoke-ConnectMode `
            -ConnectMode 'Configure' `
            -ReviewerId $reviewerId `
            -ResourceGroupName $protectedResourceGroup `
            -ContainerRegistryNameOverride $protectedRegistryName
        $variables = Get-GitHubEnvironmentVariables
        $connectorUrl = Resolve-ConnectorUrlFromAzure -Variables $variables
        Set-GitHubEnvironmentVariable -Name 'HELIOS_AZURE_CONNECTOR_URL' -Value $connectorUrl
        $variables['HELIOS_AZURE_CONNECTOR_URL'] = $connectorUrl
        Persist-LocalRuntimeValues -Variables $variables -ConnectorUrl $connectorUrl
        Write-Host "Bound HELIOS_AZURE_CONNECTOR_URL=$connectorUrl"
        Invoke-CloudVerification -Variables $variables
    }
    'Verify' {
        $variables = Get-GitHubEnvironmentVariables
        Invoke-CloudVerification -Variables $variables
    }
}

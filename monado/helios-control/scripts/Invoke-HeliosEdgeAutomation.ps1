#requires -Version 7.0

<#
.SYNOPSIS
Diagnoses and plans the governed HELIOS Azure edge.

.DESCRIPTION
This local operator helper is non-mutating. Diagnose reads bounded Azure resource
metadata; Plan validates Bicep and captures FullResourcePayloads what-if evidence.
Azure apply and vault writes are intentionally unavailable here and must use a
separately reviewed protected workflow.
#>

[CmdletBinding()]
param(
    [ValidateSet('Diagnose', 'Plan')]
    [string] $Mode = 'Diagnose',
    [ValidateSet('dev', 'test', 'preview', 'prod')]
    [string] $EnvironmentName = 'dev',
    [Parameter(Mandatory)] [string] $TenantId,
    [Parameter(Mandatory)] [string] $SubscriptionId,
    [Parameter(Mandatory)] [string] $ResourceGroup,
    [string] $TemplateFile = (Join-Path $PSScriptRoot '../infra/main.bicep'),
    [string] $ParametersFile = (Join-Path $PSScriptRoot '../infra/main.parameters.json'),
    [string] $ContainerImage = $env:HELIOS_CONTAINER_IMAGE,
    [string] $ContainerRegistryName = $env:HELIOS_CONTAINER_REGISTRY_NAME,
    [string] $EntraClientId = $env:HELIOS_ENTRA_CLIENT_ID,
    [string] $AllowedPrincipalObjectId = $env:HELIOS_ALLOWED_PRINCIPAL_OBJECT_ID,
    [string] $SourceCommitSha = $env:HELIOS_SOURCE_SHA,
    [string] $ContainerAppsInfrastructureSubnetId = $env:HELIOS_CONTAINER_APPS_INFRASTRUCTURE_SUBNET_ID,
    [string] $EvidenceDirectory = (Join-Path (Get-Location) 'evidence/helios-edge')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Protect-DiagnosticText {
    param([AllowEmptyString()] [string] $Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return '' }
    $safe = $Text -replace '(?i)(authorization\s*:\s*bearer\s+)[^\s]+', '$1[REDACTED]'
    $safe = $safe -replace '(?i)(access[_-]?token|client[_-]?secret|api[_-]?key)(\s*[=:]\s*)[^\s,;]+', '$1$2[REDACTED]'
    return ($safe -replace 'eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+', '[REDACTED_JWT]').Trim()
}

function Invoke-Native {
    param(
        [Parameter(Mandatory)] [string] $FilePath,
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $Operation
    )
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $FilePath
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($argument in $Arguments) { [void] $info.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $info
    try {
        if (-not $process.Start()) { throw "Unable to start $Operation." }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = $stdoutTask.GetAwaiter().GetResult()
        $stderr = $stderrTask.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) {
            $detail = Protect-DiagnosticText $stderr
            if (-not $detail) { $detail = 'No diagnostic text was returned.' }
            throw "$Operation failed with exit code $($process.ExitCode): $detail"
        }
        return $stdout
    }
    finally { $process.Dispose() }
}

function Invoke-AzJson {
    param([Parameter(Mandatory)] [string[]] $Arguments, [Parameter(Mandatory)] [string] $Operation)
    $all = [Collections.Generic.List[string]]::new()
    foreach ($argument in $Arguments) { [void] $all.Add($argument) }
    [void] $all.Add('--only-show-errors')
    [void] $all.Add('--output')
    [void] $all.Add('json')
    $raw = Invoke-Native -FilePath $script:AzPath -Arguments $all.ToArray() -Operation $Operation
    if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
    try { return $raw | ConvertFrom-Json -Depth 100 }
    catch { throw "$Operation returned malformed JSON." }
}

function Get-CanonicalJson {
    param([Parameter(Mandatory)] [object] $Value)
    return $Value | ConvertTo-Json -Depth 100 -Compress
}

function Write-Utf8NoBom {
    param([Parameter(Mandatory)] [string] $Path, [Parameter(Mandatory)] [string] $Value)
    [IO.File]::WriteAllText($Path, $Value, [Text.UTF8Encoding]::new($false))
}

function Get-FileSha256 {
    param([Parameter(Mandatory)] [string] $Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Assert-AzureContext {
    $account = Invoke-AzJson -Arguments @('account', 'show') -Operation 'Reading Azure account context'
    if ([string] $account.tenantId -ne $TenantId) { throw 'Azure CLI tenant does not match -TenantId.' }
    if ([string] $account.id -ne $SubscriptionId) { throw 'Azure CLI subscription does not match -SubscriptionId.' }
    $group = Invoke-AzJson -Arguments @('group', 'show', '--name', $ResourceGroup) -Operation 'Reading Azure resource group'
    return [pscustomobject]@{
        tenantId = [string] $account.tenantId
        subscriptionId = [string] $account.id
        resourceGroup = [string] $group.name
        location = [string] $group.location
        environment = if ($group.tags) { [string] $group.tags.'helios-environment' } else { '' }
    }
}

function Assert-DeploymentInputs {
    $script:ResolvedTemplate = (Resolve-Path -LiteralPath $TemplateFile).Path
    $script:ResolvedParameters = (Resolve-Path -LiteralPath $ParametersFile).Path
    if ($ContainerImage -notmatch '^[a-z0-9]{5,50}\.azurecr\.io/[a-z0-9._/-]+@sha256:[0-9a-fA-F]{64}$') {
        throw 'Plan requires -ContainerImage with an immutable Azure Container Registry sha256 digest.'
    }
    if ($ContainerRegistryName -notmatch '^[a-z0-9]{5,50}$' -or -not $ContainerImage.StartsWith("$ContainerRegistryName.azurecr.io/", [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Container image and -ContainerRegistryName must identify the same Azure Container Registry.'
    }
    foreach ($binding in @(
        @{ Name = 'EntraClientId'; Value = $EntraClientId },
        @{ Name = 'AllowedPrincipalObjectId'; Value = $AllowedPrincipalObjectId }
    )) {
        $parsed = [guid]::Empty
        if (-not [guid]::TryParse([string] $binding.Value, [ref] $parsed)) { throw "$($binding.Name) must be a GUID." }
    }
    if ($EnvironmentName -eq 'prod' -and [string]::IsNullOrWhiteSpace($ContainerAppsInfrastructureSubnetId)) { throw 'Production plan requires -ContainerAppsInfrastructureSubnetId.' }
    if ($SourceCommitSha -notmatch '^[0-9a-fA-F]{40}$') { throw 'SourceCommitSha must be the exact 40-character Git commit built into the image.' }
    $script:ResolvedDeploymentParameters = @(
        '--parameters',
        "@$script:ResolvedParameters",
        "environmentName=$EnvironmentName",
        "containerImage=$ContainerImage",
        "containerRegistryName=$ContainerRegistryName",
        'allowPreviewPlaceholder=false',
        "entraClientId=$EntraClientId",
        "entraTenantId=$TenantId",
        "allowedPrincipalObjectId=$AllowedPrincipalObjectId",
        "sourceCommitSha=$($SourceCommitSha.ToLowerInvariant())",
        "containerAppsInfrastructureSubnetId=$ContainerAppsInfrastructureSubnetId"
    )
}

function Invoke-WhatIf {
    $arguments = @(
        'deployment', 'group', 'what-if',
        '--resource-group', $ResourceGroup,
        '--template-file', $script:ResolvedTemplate
    )
    $arguments += $script:ResolvedDeploymentParameters
    $arguments += @('--result-format', 'FullResourcePayloads')
    $result = Invoke-AzJson -Arguments $arguments -Operation 'Running ARM what-if'
    return Get-CanonicalJson $result
}

$az = Get-Command az -CommandType Application -ErrorAction Stop
$script:AzPath = $az.Source
$context = Assert-AzureContext

if ($Mode -eq 'Diagnose') {
    $resources = Invoke-AzJson -Arguments @(
        'resource', 'list', '--resource-group', $ResourceGroup,
        '--query', '[].{name:name,type:type,location:location}'
    ) -Operation 'Listing resource metadata'
    [pscustomobject]@{
        mode = 'diagnose'
        context = $context
        resources = $resources
        mutations = 0
    } | ConvertTo-Json -Depth 20
    return
}

Assert-DeploymentInputs
$validationArguments = @(
    'deployment', 'group', 'validate', '--resource-group', $ResourceGroup,
    '--template-file', $script:ResolvedTemplate
)
$validationArguments += $script:ResolvedDeploymentParameters
[void] (Invoke-AzJson -Arguments $validationArguments -Operation 'Validating Bicep deployment')
$whatIf = Invoke-WhatIf
$directory = Join-Path $EvidenceDirectory "$EnvironmentName-$((Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'))"
[void] (New-Item -ItemType Directory -Path $directory -Force)
$whatIfPath = Join-Path $directory 'what-if.json'
Write-Utf8NoBom -Path $whatIfPath -Value $whatIf
$sha256 = Get-FileSha256 $whatIfPath
$request = [ordered]@{
    schema = 'helios.edgePlan.v1'
    mode = 'plan'
    environment = $EnvironmentName
    tenantId = $TenantId
    subscriptionId = $SubscriptionId
    resourceGroup = $ResourceGroup
    resourceGroupEnvironment = $context.environment
    templateFile = $script:ResolvedTemplate
    parametersFile = $script:ResolvedParameters
    resolvedParameters = [ordered]@{
        environmentName = $EnvironmentName
        containerImage = $ContainerImage
        containerRegistryName = $ContainerRegistryName
        allowPreviewPlaceholder = $false
        entraClientId = $EntraClientId
        entraTenantId = $TenantId
        allowedPrincipalObjectId = $AllowedPrincipalObjectId
        sourceCommitSha = $SourceCommitSha.ToLowerInvariant()
        containerAppsInfrastructureSubnetId = $ContainerAppsInfrastructureSubnetId
    }
    whatIfSha256 = $sha256
    applyRequires = @('protected-github-environment', 'immutable-image-digest', 'full-resource-what-if', 'fresh-drift-match', 'second-environment-approval', 'explicit-deploy-confirmation')
}
$requestPath = Join-Path $directory 'request.json'
Write-Utf8NoBom -Path $requestPath -Value (Get-CanonicalJson $request)
$requestSha256 = Get-FileSha256 $requestPath
[pscustomobject]@{
    mode = 'plan'
    evidenceDirectory = $directory
    whatIfFile = $whatIfPath
    whatIfSha256 = $sha256
    requestFile = $requestPath
    requestSha256 = $requestSha256
    mutations = 0
    applyAvailableLocally = $false
} | ConvertTo-Json -Depth 10

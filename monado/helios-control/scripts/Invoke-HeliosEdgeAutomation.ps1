#requires -Version 7.0

<#
.SYNOPSIS
Plans, diagnoses, applies, or updates one secret for the governed HELIOS Azure edge.

.DESCRIPTION
Diagnose and Plan are non-mutating. Apply requires a previously reviewed what-if
file, the exact SHA-256 of that file, a fresh matching what-if, and typed
confirmation. VaultSet accepts a secret only through a secure prompt, writes it
to a restricted temporary file for Azure CLI, and removes the file immediately.
The script never reads a secret value back from Key Vault.
#>

[CmdletBinding()]
param(
    [ValidateSet('Diagnose', 'Plan', 'Apply', 'VaultSet')]
    [string] $Mode = 'Diagnose',
    [ValidateSet('dev', 'test', 'preview', 'prod')]
    [string] $EnvironmentName = 'dev',
    [Parameter(Mandatory)] [string] $TenantId,
    [Parameter(Mandatory)] [string] $SubscriptionId,
    [Parameter(Mandatory)] [string] $ResourceGroup,
    [string] $TemplateFile = (Join-Path $PSScriptRoot '../infra/main.bicep'),
    [string] $ParametersFile = (Join-Path $PSScriptRoot '../infra/main.parameters.json'),
    [string] $EvidenceDirectory = (Join-Path (Get-Location) 'evidence/helios-edge'),
    [string] $ApprovedPlanFile,
    [string] $ApprovedPlanSha256,
    [string] $KeyVaultName,
    [string] $SecretName,
    [string] $Confirmation,
    [switch] $AllowProduction
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

function Assert-ExactConfirmation {
    param([Parameter(Mandatory)] [string] $Expected)
    if ($Confirmation -cne $Expected) { throw "Operation blocked. Pass -Confirmation '$Expected'." }
}

function Assert-ProductionGate {
    if ($EnvironmentName -eq 'prod' -and -not $AllowProduction) {
        throw 'Production mutation requires -AllowProduction in addition to plan approval and typed confirmation.'
    }
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
    }
}

function Assert-DeploymentInputs {
    $script:ResolvedTemplate = (Resolve-Path -LiteralPath $TemplateFile).Path
    $script:ResolvedParameters = (Resolve-Path -LiteralPath $ParametersFile).Path
}

function Invoke-WhatIf {
    $result = Invoke-AzJson -Arguments @(
        'deployment', 'group', 'what-if',
        '--resource-group', $ResourceGroup,
        '--template-file', $script:ResolvedTemplate,
        '--parameters', "@$script:ResolvedParameters",
        '--result-format', 'ResourceIdOnly'
    ) -Operation 'Running ARM what-if'
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

if ($Mode -eq 'VaultSet') {
    Assert-ProductionGate
    if ($KeyVaultName -notmatch '^[a-zA-Z0-9-]{3,24}$') { throw 'VaultSet requires a valid -KeyVaultName.' }
    if ($SecretName -notmatch '^[0-9a-zA-Z-]{1,127}$') { throw 'VaultSet requires a valid -SecretName.' }
    Assert-ExactConfirmation -Expected "SET HELIOS VAULT SECRET $SecretName"

    $secureValue = Read-Host "Enter the new value for '$SecretName'" -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureValue)
    $temporaryPath = Join-Path ([IO.Path]::GetTempPath()) "helios-secret-$([guid]::NewGuid().ToString('n')).txt"
    try {
        $plainValue = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
        Write-Utf8NoBom -Path $temporaryPath -Value $plainValue
        $plainValue = $null
        if (-not $IsWindows) { [void] (Invoke-Native -FilePath (Get-Command chmod).Source -Arguments @('600', $temporaryPath) -Operation 'Restricting temporary secret permissions') }
        $metadata = Invoke-AzJson -Arguments @(
            'keyvault', 'secret', 'set', '--vault-name', $KeyVaultName,
            '--name', $SecretName, '--file', $temporaryPath,
            '--query', '{id:id,name:name,enabled:attributes.enabled,created:attributes.created}'
        ) -Operation 'Creating a Key Vault secret version'
        [pscustomobject]@{ mode = 'vault-set'; metadata = $metadata; secretValueReturned = $false } | ConvertTo-Json -Depth 10
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
        if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath -Force }
    }
    return
}

Assert-DeploymentInputs
if ($Mode -eq 'Plan') {
    [void] (Invoke-AzJson -Arguments @(
        'deployment', 'group', 'validate', '--resource-group', $ResourceGroup,
        '--template-file', $script:ResolvedTemplate, '--parameters', "@$script:ResolvedParameters"
    ) -Operation 'Validating Bicep deployment')
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
        templateFile = $script:ResolvedTemplate
        parametersFile = $script:ResolvedParameters
        whatIfSha256 = $sha256
        applyRequires = @('approved-plan-sha256', 'fresh-matching-what-if', 'protected-environment-reviewer', 'typed-confirmation')
    }
    Write-Utf8NoBom -Path (Join-Path $directory 'request.json') -Value (Get-CanonicalJson $request)
    [pscustomobject]@{ mode = 'plan'; evidenceDirectory = $directory; whatIfFile = $whatIfPath; whatIfSha256 = $sha256; mutations = 0 } | ConvertTo-Json -Depth 10
    return
}

Assert-ProductionGate
if (-not (Test-Path -LiteralPath $ApprovedPlanFile -PathType Leaf)) { throw 'Apply requires -ApprovedPlanFile.' }
if ($ApprovedPlanSha256 -notmatch '^[0-9a-fA-F]{64}$') { throw 'Apply requires a valid -ApprovedPlanSha256.' }
$approvedHash = Get-FileSha256 (Resolve-Path -LiteralPath $ApprovedPlanFile).Path
if ($approvedHash -ne $ApprovedPlanSha256.ToLowerInvariant()) { throw 'Approved plan file hash does not match -ApprovedPlanSha256.' }
$freshWhatIfPath = Join-Path ([IO.Path]::GetTempPath()) "helios-what-if-$([guid]::NewGuid().ToString('n')).json"
try {
    Write-Utf8NoBom -Path $freshWhatIfPath -Value (Invoke-WhatIf)
    if ((Get-FileSha256 $freshWhatIfPath) -ne $approvedHash) { throw 'Azure state or deployment inputs changed after plan approval.' }
}
finally {
    if (Test-Path -LiteralPath $freshWhatIfPath) { Remove-Item -LiteralPath $freshWhatIfPath -Force }
}
Assert-ExactConfirmation -Expected "APPLY HELIOS EDGE $($EnvironmentName.ToUpperInvariant())"
$deployment = Invoke-AzJson -Arguments @(
    'deployment', 'group', 'create', '--resource-group', $ResourceGroup,
    '--name', "helios-edge-$EnvironmentName-$((Get-Date).ToUniversalTime().ToString('yyyyMMddHHmmss'))",
    '--template-file', $script:ResolvedTemplate, '--parameters', "@$script:ResolvedParameters",
    '--mode', 'Incremental'
) -Operation 'Applying reviewed HELIOS edge deployment'
[pscustomobject]@{
    mode = 'apply'
    deploymentId = [string] $deployment.id
    provisioningState = [string] $deployment.properties.provisioningState
    approvedPlanSha256 = $approvedHash
    secretValuesReturned = $false
} | ConvertTo-Json -Depth 10

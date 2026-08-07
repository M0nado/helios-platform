[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolId = 'microsoft-365-agents-toolkit'
$packageName = '@microsoft/m365agentstoolkit-cli'
$auditFixtureRelativePath = 'security/m365agentstoolkit-cli-audit'

function Get-RequiredObjectProperty {
    param(
        [Parameter(Mandatory)] [object] $Object,
        [Parameter(Mandatory)] [string] $Name
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        throw "Missing required property '$Name'."
    }
    return $property.Value
}

function Get-FirstNonEmptyLine {
    param([string] $Text)

    foreach ($line in $Text -split "`r?`n") {
        $trimmed = $line.Trim()
        if ($trimmed) { return $trimmed }
    }
    return ''
}

function Assert-NonNegativeInteger {
    param(
        [Parameter(Mandatory)] [object] $Value,
        [Parameter(Mandatory)] [string] $Name
    )

    if ($Value -is [bool]) {
        throw "$Name must be a non-negative integer."
    }
    if (-not ($Value -is [int] -or $Value -is [long])) {
        throw "$Name must be a non-negative integer."
    }
    if ([int64]$Value -lt 0) {
        throw "$Name must be a non-negative integer."
    }
    return [int64]$Value
}

function Invoke-NpmCommand {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $script:ErrorActionPreference = 'Continue'
        $output = & npm @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $script:ErrorActionPreference = $previousErrorActionPreference
    }

    return [pscustomobject]@{
        Output = $output
        ExitCode = $exitCode
    }
}

function ConvertFrom-NpmJsonOutput {
    param(
        [Parameter(Mandatory)] [string] $Text
    )

    $trimmed = $Text.Trim()
    if (-not $trimmed) {
        throw 'npm did not return JSON output.'
    }

    try {
        return $trimmed | ConvertFrom-Json
    }
    catch {
    }

    $start = $trimmed.IndexOf('{')
    $end = $trimmed.LastIndexOf('}')
    if ($start -lt 0 -or $end -le $start) {
        $sample = Get-FirstNonEmptyLine -Text $trimmed
        throw "npm output is not valid JSON. First line: $sample"
    }

    $candidate = $trimmed.Substring($start, $end - $start + 1)
    try {
        return $candidate | ConvertFrom-Json
    }
    catch {
        $sample = Get-FirstNonEmptyLine -Text $trimmed
        throw "npm output is not valid JSON. First line: $sample"
    }
}

$controlRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$toolchainPath = Join-Path (Join-Path $controlRoot 'config') 'microsoft-toolchain.json'
$auditFixturePath = Join-Path $controlRoot $auditFixtureRelativePath
$fixtureManifestPath = Join-Path $auditFixturePath 'package.json'
$fixtureLockPath = Join-Path $auditFixturePath 'package-lock.json'

$toolchain = Get-Content -LiteralPath $toolchainPath -Raw | ConvertFrom-Json
$tools = Get-RequiredObjectProperty -Object $toolchain -Name 'tools'
if (-not ($tools -is [System.Collections.IEnumerable])) {
    throw "config/microsoft-toolchain.json tools must be an array."
}

$tool = $tools | Where-Object { $_.id -eq $toolId } | Select-Object -First 1
if ($null -eq $tool) {
    throw "config/microsoft-toolchain.json is missing '$toolId'."
}

$declaredPackage = [string](Get-RequiredObjectProperty -Object $tool -Name 'package')
if ($declaredPackage -ne $packageName) {
    throw "$toolId package must be '$packageName', found '$declaredPackage'."
}

$version = [string](Get-RequiredObjectProperty -Object $tool -Name 'version')
if (-not $version.Trim()) {
    throw "$toolId version must be pinned."
}

$automaticInstall = Get-RequiredObjectProperty -Object $tool -Name 'automaticInstall'
if (-not ($automaticInstall -is [bool])) {
    throw "$toolId automaticInstall must be true or false."
}

$auditFixture = [string](Get-RequiredObjectProperty -Object $tool -Name 'auditFixture')
if ($auditFixture -ne $auditFixtureRelativePath) {
    throw "$toolId auditFixture must be '$auditFixtureRelativePath'."
}

if (-not (Test-Path -LiteralPath $fixtureManifestPath)) {
    throw "Missing audit fixture manifest: $fixtureManifestPath"
}
if (-not (Test-Path -LiteralPath $fixtureLockPath)) {
    throw "Missing audit fixture lockfile: $fixtureLockPath"
}

$fixtureManifest = Get-Content -LiteralPath $fixtureManifestPath -Raw | ConvertFrom-Json
$dependencies = Get-RequiredObjectProperty -Object $fixtureManifest -Name 'dependencies'
$dependencyProperty = $dependencies.PSObject.Properties[$packageName]
if ($null -eq $dependencyProperty) {
    throw "$fixtureManifestPath must pin $packageName."
}
$fixtureVersion = [string]$dependencyProperty.Value
if ($fixtureVersion -ne $version) {
    throw "Fixture version '$fixtureVersion' does not match microsoft-toolchain pin '$version'."
}

Push-Location $auditFixturePath
try {
    $installResult = Invoke-NpmCommand -Arguments @('ci', '--ignore-scripts', '--no-fund')
    if ($installResult.ExitCode -ne 0) {
        $detail = Get-FirstNonEmptyLine -Text ($installResult.Output | Out-String)
        if (-not $detail) { $detail = 'npm ci failed without output.' }
        throw "npm ci failed for audit fixture: $detail"
    }

    $atkExecutable = if ($IsWindows) {
        Join-Path $auditFixturePath 'node_modules\.bin\atk.cmd'
    }
    else {
        Join-Path $auditFixturePath 'node_modules/.bin/atk'
    }
    if (-not (Test-Path -LiteralPath $atkExecutable)) {
        throw "Toolkit binary was not installed at expected path: $atkExecutable"
    }

    if ($IsWindows -and -not $env:windir -and $env:SystemRoot) {
        $env:windir = $env:SystemRoot
    }

    $smokeOutput = & $atkExecutable '--version' 2>&1
    $smokeExitCode = $LASTEXITCODE
    if ($smokeExitCode -ne 0) {
        $detail = Get-FirstNonEmptyLine -Text ($smokeOutput | Out-String)
        if (-not $detail) { $detail = 'atk --version failed without output.' }
        throw "atk smoke test failed: $detail"
    }

    $smokeFirstLine = Get-FirstNonEmptyLine -Text ($smokeOutput | Out-String)
    $smokeVersionMatch = [regex]::Match($smokeFirstLine, '\d+\.\d+\.\d+(?:[-+][0-9A-Za-z\.-]+)?')
    if (-not $smokeVersionMatch.Success) {
        throw "atk smoke test did not report a semantic version. First line: $smokeFirstLine"
    }
    if ($smokeVersionMatch.Value -ne $version) {
        throw "atk smoke test resolved version '$($smokeVersionMatch.Value)' but microsoft-toolchain pin is '$version'."
    }
    Write-Host "Toolkit smoke test: atk version $($smokeVersionMatch.Value)"

    $auditResult = Invoke-NpmCommand -Arguments @('audit', '--audit-level=high', '--json')
    $auditText = $auditResult.Output | Out-String
    $auditReport = ConvertFrom-NpmJsonOutput -Text $auditText
}
finally {
    Pop-Location
}

$metadata = Get-RequiredObjectProperty -Object $auditReport -Name 'metadata'
$vulnerabilities = Get-RequiredObjectProperty -Object $metadata -Name 'vulnerabilities'
$high = Assert-NonNegativeInteger -Value (Get-RequiredObjectProperty -Object $vulnerabilities -Name 'high') -Name 'metadata.vulnerabilities.high'
$critical = Assert-NonNegativeInteger -Value (Get-RequiredObjectProperty -Object $vulnerabilities -Name 'critical') -Name 'metadata.vulnerabilities.critical'

Write-Host "Toolkit audit summary: version=$version high=$high critical=$critical automaticInstall=$automaticInstall"

if ($automaticInstall -and ($high -gt 0 -or $critical -gt 0)) {
    throw 'automaticInstall is true while high/critical advisories are present. Keep automaticInstall false until the audit gate is clean.'
}

if (-not $automaticInstall -and ($high -gt 0 -or $critical -gt 0)) {
    Write-Host 'automaticInstall remains disabled because high/critical advisories are present.'
}
elseif (-not $automaticInstall) {
    Write-Host 'Audit gate is clean but automaticInstall is still false. Re-enable only through a reviewed policy change.'
}
else {
    Write-Host 'Audit gate is clean and automaticInstall is enabled.'
}

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

function Invoke-PinnedPackageAudit {
    param(
        [Parameter(Mandatory)] [string] $PackageName,
        [Parameter(Mandatory)] [string] $Version
    )

    $auditRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("helios-atk-direct-audit-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $auditRoot | Out-Null
    try {
        $manifestPath = Join-Path $auditRoot 'package.json'
        $manifest = @{
            name = 'helios-m365agentstoolkit-cli-direct-audit'
            private = $true
            dependencies = @{
                $PackageName = $Version
            }
        } | ConvertTo-Json -Depth 4
        Set-Content -LiteralPath $manifestPath -Value $manifest -Encoding utf8

        Push-Location $auditRoot
        try {
            $installResult = Invoke-NpmCommand -Arguments @('install', '--ignore-scripts', '--no-fund', '--silent')
            if ($installResult.ExitCode -ne 0) {
                $detail = Get-FirstNonEmptyLine -Text ($installResult.Output | Out-String)
                if (-not $detail) { $detail = 'npm install failed without output.' }
                throw "npm install failed for direct package audit: $detail"
            }

            $auditResult = Invoke-NpmCommand -Arguments @('audit', '--audit-level=high', '--json')
            $auditText = $auditResult.Output | Out-String
            return ConvertFrom-NpmJsonOutput -Text $auditText
        }
        finally {
            Pop-Location
        }
    }
    finally {
        Remove-Item -LiteralPath $auditRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$controlRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$toolchainPath = Join-Path (Join-Path $controlRoot 'config') 'microsoft-toolchain.json'
$cliMatrixPath = Join-Path (Join-Path $controlRoot 'config') 'cli-matrix.json'
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

$cliMatrix = Get-Content -LiteralPath $cliMatrixPath -Raw | ConvertFrom-Json
$cliTools = Get-RequiredObjectProperty -Object $cliMatrix -Name 'tools'
if (-not ($cliTools -is [System.Collections.IEnumerable])) {
    throw "config/cli-matrix.json tools must be an array."
}

$cliToolId = 'm365-agents'
$cliTool = $cliTools | Where-Object { $_.id -eq $cliToolId } | Select-Object -First 1
if ($null -eq $cliTool) {
    throw "config/cli-matrix.json is missing '$cliToolId'."
}

$cliCommand = [string](Get-RequiredObjectProperty -Object $cliTool -Name 'command')
if ($cliCommand -ne 'atk') {
    throw "$cliToolId command must be 'atk', found '$cliCommand'."
}

$cliPinnedVersion = [string](Get-RequiredObjectProperty -Object $cliTool -Name 'pinnedVersion')
if (-not $cliPinnedVersion.Trim()) {
    throw "$cliToolId pinnedVersion must be set."
}
if ($cliPinnedVersion -ne $version) {
    throw "$cliToolId pinnedVersion '$cliPinnedVersion' must match microsoft-toolchain version '$version'."
}

$cliAutomaticInstall = Get-RequiredObjectProperty -Object $cliTool -Name 'automaticInstall'
if (-not ($cliAutomaticInstall -is [bool])) {
    throw "$cliToolId automaticInstall must be true or false."
}
if ($cliAutomaticInstall -ne $automaticInstall) {
    throw "$cliToolId automaticInstall '$cliAutomaticInstall' must match microsoft-toolchain automaticInstall '$automaticInstall'."
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

$fixtureMetadata = Get-RequiredObjectProperty -Object $auditReport -Name 'metadata'
$fixtureVulnerabilities = Get-RequiredObjectProperty -Object $fixtureMetadata -Name 'vulnerabilities'
$fixtureHigh = Assert-NonNegativeInteger -Value (Get-RequiredObjectProperty -Object $fixtureVulnerabilities -Name 'high') -Name 'fixture.metadata.vulnerabilities.high'
$fixtureCritical = Assert-NonNegativeInteger -Value (Get-RequiredObjectProperty -Object $fixtureVulnerabilities -Name 'critical') -Name 'fixture.metadata.vulnerabilities.critical'

$directInstallAuditReport = Invoke-PinnedPackageAudit -PackageName $packageName -Version $version
$directMetadata = Get-RequiredObjectProperty -Object $directInstallAuditReport -Name 'metadata'
$directVulnerabilities = Get-RequiredObjectProperty -Object $directMetadata -Name 'vulnerabilities'
$directHigh = Assert-NonNegativeInteger -Value (Get-RequiredObjectProperty -Object $directVulnerabilities -Name 'high') -Name 'direct.metadata.vulnerabilities.high'
$directCritical = Assert-NonNegativeInteger -Value (Get-RequiredObjectProperty -Object $directVulnerabilities -Name 'critical') -Name 'direct.metadata.vulnerabilities.critical'

Write-Host "Toolkit audit summary: version=$version fixtureHigh=$fixtureHigh fixtureCritical=$fixtureCritical directHigh=$directHigh directCritical=$directCritical automaticInstall=$automaticInstall"

$hasBlockingAdvisories = ($fixtureHigh -gt 0 -or $fixtureCritical -gt 0 -or $directHigh -gt 0 -or $directCritical -gt 0)
if ($automaticInstall -and $hasBlockingAdvisories) {
    throw 'automaticInstall is true while high/critical advisories are present in the fixture or direct-install graph. Keep automaticInstall false until both audits are clean.'
}

if (-not $automaticInstall -and $hasBlockingAdvisories) {
    Write-Host 'automaticInstall remains disabled because high/critical advisories are present.'
}
elseif (-not $automaticInstall) {
    Write-Host 'Audit gate is clean but automaticInstall is still false. Re-enable only through a reviewed policy change.'
}
else {
    Write-Host 'Audit gate is clean and automaticInstall is enabled.'
}

$global:LASTEXITCODE = 0

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [ValidateSet('Audit', 'Winget', 'Chocolatey')]
    [string] $Mode = 'Audit',
    [switch] $IncludePreview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$stablePackages = @(
    'Microsoft.VisualStudio.2022.BuildTools',
    'Microsoft.VisualStudioCode',
    'Microsoft.WindowsSDK.10.0.26100',
    'Microsoft.PowerShell',
    'Microsoft.AzureCLI',
    'GitHub.cli',
    'Python.Python.3.14',
    'OpenJS.NodeJS.LTS',
    'Microsoft.Edge.Dev'
)
$previewPackages = @('Microsoft.VisualStudio.2022.Community.Preview')
$packages = if ($IncludePreview) { $stablePackages + $previewPackages } else { $stablePackages }

if ($Mode -eq 'Audit') {
    Write-Host 'Audit-only developer tool plan. No machine state will change.'
    $packages | ForEach-Object { Write-Output "winget upgrade --id $_ --exact" }
    Write-Output 'az extension add --name azure-devops --version 1.0.6 --yes'
    Write-Output 'az extension add --name ml --version 2.44.1 --yes'
    exit 0
}

if (-not $PSCmdlet.ShouldProcess($env:COMPUTERNAME, "Upgrade $($packages.Count) developer tools using $Mode")) {
    exit 0
}

switch ($Mode) {
    'Winget' {
        if (-not (Get-Command winget -ErrorAction SilentlyContinue)) { throw 'winget is required.' }
        foreach ($package in $packages) {
            & winget upgrade --id $package --exact --accept-package-agreements --accept-source-agreements
            if ($LASTEXITCODE -notin @(0, -1978335189)) { throw "winget failed for $package ($LASTEXITCODE)." }
        }
    }
    'Chocolatey' {
        if (-not (Get-Command choco -ErrorAction SilentlyContinue)) { throw 'Chocolatey is required.' }
        $chocoPackages = @('visualstudio2022buildtools', 'visualstudio2022-workload-vctools', 'windows-sdk-10.0', 'powershell-core', 'azure-cli', 'gh', 'python314', 'nodejs-lts', 'microsoft-edge-insider-dev')
        & choco upgrade @chocoPackages --yes --no-progress
        if ($LASTEXITCODE -ne 0) { throw "Chocolatey upgrade failed ($LASTEXITCODE)." }
    }
}

if (Get-Command az -ErrorAction SilentlyContinue) {
    & az extension add --name azure-devops --version 1.0.6 --yes
    & az extension add --name ml --version 2.44.1 --yes
}

[CmdletBinding()]
param(
    [string] $ToolsDirectory = (Join-Path (Get-Location) '.tools'),
    [string] $DotnetVersion,
    [string] $GhVersion = '2.76.2',
    [string] $RgVersion = '14.1.1'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not $DotnetVersion) {
    $DotnetVersion = (Get-Content -Raw (Join-Path $repositoryRoot 'global.json') | ConvertFrom-Json).sdk.version
}
$ToolsDirectory = [IO.Path]::GetFullPath($ToolsDirectory)
$dotnetDirectory = Join-Path $ToolsDirectory 'dotnet'
$ghDirectory = Join-Path $ToolsDirectory 'gh'
$azureDirectory = Join-Path $ToolsDirectory 'azcli-venv'
$rgDirectory = Join-Path $ToolsDirectory 'rg'
$pythonDirectory = Join-Path $ToolsDirectory 'python'
New-Item -ItemType Directory -Force -Path $ToolsDirectory | Out-Null

$dotnetCommand = Join-Path $dotnetDirectory 'dotnet.exe'
$installedDotnetVersion = if (Test-Path $dotnetCommand) { & $dotnetCommand --version 2>$null } else { $null }
if ($installedDotnetVersion -ne $DotnetVersion) {
    Write-Host "Installing .NET SDK $DotnetVersion into $dotnetDirectory"
    $installer = Join-Path $ToolsDirectory 'dotnet-install.ps1'
    Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile $installer
    & $installer -Version $DotnetVersion -InstallDir $dotnetDirectory -NoPath
} else {
    Write-Host ".NET already installed at $dotnetDirectory"
}
$installedDotnetVersion = & $dotnetCommand --version
if ($installedDotnetVersion -ne $DotnetVersion) {
    throw "Expected .NET SDK $DotnetVersion, but local dotnet selected $installedDotnetVersion."
}

if (-not (Test-Path (Join-Path $ghDirectory 'bin/gh.exe'))) {
    Write-Host "Installing GitHub CLI $GhVersion into $ghDirectory"
    $archive = Join-Path $ToolsDirectory "gh_${GhVersion}_windows_amd64.zip"
    $extracted = Join-Path $ToolsDirectory "gh_${GhVersion}_windows_amd64"
    Invoke-WebRequest "https://github.com/cli/cli/releases/download/v$GhVersion/gh_${GhVersion}_windows_amd64.zip" -OutFile $archive
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $extracted, $ghDirectory
    Expand-Archive -Force $archive $ToolsDirectory
    Move-Item $extracted $ghDirectory
} else {
    Write-Host "GitHub CLI already installed at $ghDirectory"
}

$python = Get-Command py -ErrorAction SilentlyContinue
if (-not $python) { $python = Get-Command python -ErrorAction SilentlyContinue }
if (-not $python) { throw 'Python 3 is required to install the Azure CLI.' }

# Git Bash commonly exposes the Windows launcher as `py` or `python`, while the
# repository's cross-platform scripts invoke `python3`. Keep that contract by
# placing a small command shim on the PATH used by helios-dev.sh.
New-Item -ItemType Directory -Force -Path $pythonDirectory | Out-Null
$python3Command = Join-Path $pythonDirectory 'python3.cmd'
$usesPythonLauncher = [IO.Path]::GetFileNameWithoutExtension($python.Source) -eq 'py'
& (Join-Path $PSScriptRoot 'New-Python3Shim.ps1') -InterpreterPath $python.Source `
    -OutputPath $python3Command -UsePythonLauncher:$usesPythonLauncher

$azureCommand = Join-Path $azureDirectory 'Scripts/az.cmd'
if (-not (Test-Path $azureCommand)) {
    Write-Host "Installing Azure CLI into $azureDirectory"
    if ($usesPythonLauncher) {
        & $python.Source -3 -m venv $azureDirectory
    } else {
        & $python.Source -m venv $azureDirectory
    }
    $venvPython = Join-Path $azureDirectory 'Scripts/python.exe'
    & $venvPython -m pip install --disable-pip-version-check --upgrade pip
    & $venvPython -m pip install --disable-pip-version-check azure-cli
} else {
    Write-Host "Azure CLI already installed at $azureDirectory"
}

if (-not (Test-Path (Join-Path $rgDirectory 'rg.exe'))) {
    Write-Host "Installing ripgrep $RgVersion into $rgDirectory"
    $archive = Join-Path $ToolsDirectory "ripgrep-${RgVersion}-x86_64-pc-windows-msvc.zip"
    $extracted = Join-Path $ToolsDirectory "ripgrep-${RgVersion}-x86_64-pc-windows-msvc"
    Invoke-WebRequest "https://github.com/BurntSushi/ripgrep/releases/download/$RgVersion/ripgrep-${RgVersion}-x86_64-pc-windows-msvc.zip" -OutFile $archive
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $extracted, $rgDirectory
    Expand-Archive -Force $archive $ToolsDirectory
    Move-Item $extracted $rgDirectory
} else {
    Write-Host "ripgrep already installed at $rgDirectory"
}

$pathEntries = @($pythonDirectory, $dotnetDirectory, (Join-Path $ghDirectory 'bin'),
    (Join-Path $azureDirectory 'Scripts'), $rgDirectory) -join ';'
Write-Host "`nAdd these tools to your shell:"
Write-Host "`$env:PATH = `"$pathEntries;`$env:PATH`""
Write-Host "`nAuthenticate as needed:`ngh auth login`naz login"

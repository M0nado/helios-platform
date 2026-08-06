[CmdletBinding()]
param(
    [ValidateSet('Plan', 'Status', 'OpenWorkbench')]
    [string] $Mode = 'Plan',
    [switch] $InstallRecommendedExtensions
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$config = Get-Content -LiteralPath (Join-Path $root 'config/agent-fleet.json') -Raw | ConvertFrom-Json
$workspace = Join-Path $root 'HeliosControl.code-workspace'

if ($Mode -eq 'Plan') {
    [ordered]@{
        name = $config.name
        executionMode = $config.executionMode
        automaticProviderRuns = $config.automaticProviderRuns
        maxParallelAgents = $config.limits.maxParallelAgents
        providers = @($config.providers | ForEach-Object { $_.id })
        agents = @($config.agents | ForEach-Object { [ordered]@{ id = $_.id; permission = $_.permission } })
        workflow = @($config.workflow)
    } | ConvertTo-Json -Depth 5
    exit 0
}

if ($Mode -eq 'Status') {
    & (Join-Path $PSScriptRoot 'Invoke-HeliosCliMatrix.ps1') -CheckAuthentication
    exit $LASTEXITCODE
}

if ($env:HELIOS_CONFIRM_LOCAL_WORKBENCH -ne 'YES') {
    throw 'Set HELIOS_CONFIRM_LOCAL_WORKBENCH=YES before opening or modifying the VS Code Insiders workbench.'
}
$code = Get-Command code-insiders -ErrorAction SilentlyContinue
if (-not $code) { throw 'VS Code Insiders is not installed or code-insiders is not on PATH.' }

if ($InstallRecommendedExtensions) {
    $extensions = @(
        'github.copilot',
        'github.copilot-chat',
        'ms-azuretools.vscode-azure-github-copilot',
        'ms-azuretools.vscode-azure-mcp-server',
        'ms-azuretools.azure-dev',
        'ms-azuretools.vscode-bicep',
        'ms-dotnettools.csdevkit',
        'teamsdevapp.ms-teams-vscode-extension'
    )
    foreach ($extension in $extensions) {
        & code-insiders --install-extension $extension --profile 'Helios Agent Fabric'
        if ($LASTEXITCODE -ne 0) { throw "Extension installation failed: $extension" }
    }
}

& code-insiders $workspace --profile 'Helios Agent Fabric' --new-window

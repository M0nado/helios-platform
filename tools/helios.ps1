<#
.SYNOPSIS
    HELIOS command shell for setup, status, Azure, branch, agent, build, test, report, and gate orchestration.

.DESCRIPTION
    Provides one safe-by-default entry point for the HELIOS Hermes XCore integration flow.
    Commands are intentionally layered in this order:
    shell entry point -> status -> azure -> branches -> agents -> build -> test -> reports -> gate.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Position = 0)]
    [ValidateSet('help', 'setup', 'status', 'azure', 'branches', 'github', 'upgrade', 'agents', 'build', 'test', 'reports', 'gate')]
    [string]$Command = 'help',

    [Parameter(Position = 1)]
    [string]$Action = 'default',

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ReportRoot = Join-Path $RepoRoot 'reports/generated/helios-shell'
$AgentRegistryPath = Join-Path $RepoRoot 'config/helios-agents.json'
$AzureBootstrapPath = Join-Path $RepoRoot 'tools/azure/setup-helios-azure-cli.ps1'

function Write-HeliosHeader {
    param([string]$Title)
    Write-Host "HELIOS :: $Title" -ForegroundColor Cyan
}

function Write-HeliosOk {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-HeliosWarn {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-HeliosFail {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function New-HeliosReportDirectory {
    if (-not (Test-Path $ReportRoot)) {
        New-Item -Path $ReportRoot -ItemType Directory -Force | Out-Null
    }
}

function New-HeliosReport {
    param(
        [string]$Name,
        [string]$Status,
        [object[]]$Checks,
        [string[]]$Commands = @()
    )

    New-HeliosReportDirectory
    $Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $BaseName = "$Timestamp-$Name"
    $JsonPath = Join-Path $ReportRoot "$BaseName.json"
    $MarkdownPath = Join-Path $ReportRoot "$BaseName.md"
    $Report = [ordered]@{
        name = $Name
        status = $Status
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        commands = $Commands
        checks = $Checks
    }

    $Report | ConvertTo-Json -Depth 8 | Set-Content -Path $JsonPath -Encoding UTF8

    $Markdown = @()
    $Markdown += "# HELIOS $Name report"
    $Markdown += ''
    $Markdown += "- Status: $Status"
    $Markdown += "- Generated: $($Report.generatedAtUtc)"
    $Markdown += ''
    $Markdown += '## Checks'
    foreach ($Check in $Checks) {
        $Markdown += "- [$($Check.status)] $($Check.name): $($Check.message)"
    }
    if ($Commands.Count -gt 0) {
        $Markdown += ''
        $Markdown += '## Commands'
        foreach ($InvokedCommand in $Commands) {
            $Markdown += "- `$InvokedCommand`"
        }
    }
    $Markdown | Set-Content -Path $MarkdownPath -Encoding UTF8

    Write-HeliosOk "Report written: $MarkdownPath"
}

function Test-CommandAvailable {
    param([string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Invoke-ExternalCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments = @(),
        [switch]$AllowFailure
    )

    Write-Host "> $FilePath $($Arguments -join ' ')"
    & $FilePath @Arguments
    $Exit = $LASTEXITCODE
    if (($Exit -ne 0) -and (-not $AllowFailure)) {
        throw "Command failed with exit code $Exit`: $FilePath $($Arguments -join ' ')"
    }
    return $Exit
}

function Get-GitStatusShort {
    if (-not (Test-CommandAvailable git)) {
        return 'git unavailable'
    }
    return (& git status --short --branch 2>$null) -join [Environment]::NewLine
}

function Show-Help {
    Write-HeliosHeader 'single shell entry point'
    @'
Usage:
  ./tools/helios.ps1 <command> [action] [options]

Commands in integration order:
  setup verify|all               Verify or run deep capability setup order.
  status                         Read-only readiness report.
  azure setup|verify             Bootstrap or verify Azure CLI for Hermes XCore.
  branches fetch|list|integrate  Fetch, inspect, and integrate HELIOS/Hermes branches.
  github mass-score|mass-plan|mass-branch|mass-pr|mass-merge|mass-all
                                  Score, branch, PR, and auto-merge mass integration.
  github repo-verify|repo-setup      Verify or apply GitHub repository automation setup.
  upgrade plan|verify|gui|apply      Plan, report, render GUI, or execute deep auto-upgrade.
  agents list|validate|run       Inspect and run registered HELIOS agents.
  build contracts|csharp|fsharp|native|frontend|all
  test csharp|security|fsharp|native|python-aihub|all
  reports latest                 Show latest generated report.
  gate final                     Run final quality gate.

Safe defaults:
  - Branch integration requires a clean working tree.
  - Azure verification does not print secrets.
  - Generated reports are written under reports/generated/helios-shell.
'@ | Write-Host
}


function Invoke-SetupCommand {
    param([string]$SubAction)
    Write-HeliosHeader "setup $SubAction"
    $ScriptPath = Join-Path $RepoRoot 'scripts/integrations/helios_capability_setup.py'
    if (-not (Test-Path $ScriptPath)) {
        throw "Capability setup script not found: $ScriptPath"
    }
    switch ($SubAction) {
        'default' { Invoke-ExternalCommand python3 @($ScriptPath, 'verify') }
        'verify' { Invoke-ExternalCommand python3 @($ScriptPath, 'verify') }
        'all' { Invoke-ExternalCommand python3 @($ScriptPath, 'setup', '--apply') }
        default { throw "Unknown setup action '$SubAction'. Use verify or all." }
    }
    New-HeliosReport -Name "setup-$SubAction" -Status 'completed' -Checks @([ordered]@{ name = "setup:$SubAction"; status = 'ok'; message = 'capability setup command completed' }) -Commands @("python3 $ScriptPath $SubAction")
}

function Invoke-Status {
    Write-HeliosHeader 'status command'
    $Checks = New-Object System.Collections.Generic.List[object]

    foreach ($Tool in @('git', 'dotnet', 'pwsh', 'az', 'cmake', 'python', 'node')) {
        if (Test-CommandAvailable $Tool) {
            Write-HeliosOk "$Tool available"
            $Checks.Add([ordered]@{ name = "tool:$Tool"; status = 'ok'; message = 'available' })
        }
        else {
            Write-HeliosWarn "$Tool unavailable"
            $Checks.Add([ordered]@{ name = "tool:$Tool"; status = 'warn'; message = 'not found on PATH' })
        }
    }

    $GitStatus = Get-GitStatusShort
    Write-Host $GitStatus
    $Checks.Add([ordered]@{ name = 'git:status'; status = 'ok'; message = ($GitStatus -replace [Environment]::NewLine, ' | ') })

    $Remotes = if (Test-CommandAvailable git) { (& git remote -v 2>$null) -join '; ' } else { '' }
    if ($Remotes) {
        Write-HeliosOk "Git remotes configured"
        $Checks.Add([ordered]@{ name = 'git:remotes'; status = 'ok'; message = $Remotes })
    }
    else {
        Write-HeliosWarn 'No Git remotes configured'
        $Checks.Add([ordered]@{ name = 'git:remotes'; status = 'warn'; message = 'No remotes configured' })
    }

    foreach ($Path in @(
        'HELIOS.Platform.csproj',
        'src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj',
        'src/native/HELIOS.Native.Performance/CMakeLists.txt',
        'tools/azure/setup-helios-azure-cli.ps1',
        'config/helios-agents.json'
    )) {
        $FullPath = Join-Path $RepoRoot $Path
        if (Test-Path $FullPath) {
            Write-HeliosOk "$Path exists"
            $Checks.Add([ordered]@{ name = "file:$Path"; status = 'ok'; message = 'present' })
        }
        else {
            Write-HeliosWarn "$Path missing"
            $Checks.Add([ordered]@{ name = "file:$Path"; status = 'warn'; message = 'missing' })
        }
    }

    foreach ($VariableName in @('HELIOS_AZURE_SUBSCRIPTION_ID', 'HELIOS_AZURE_TENANT_ID', 'HELIOS_AZURE_LOCATION', 'HELIOS_RESOURCE_GROUP', 'HELIOS_ENVIRONMENT', 'HELIOS_HERMES_XCORE_ENABLED')) {
        if ([Environment]::GetEnvironmentVariable($VariableName)) {
            Write-HeliosOk "$VariableName set"
            $Checks.Add([ordered]@{ name = "env:$VariableName"; status = 'ok'; message = 'set' })
        }
        else {
            Write-HeliosWarn "$VariableName not set"
            $Checks.Add([ordered]@{ name = "env:$VariableName"; status = 'warn'; message = 'not set' })
        }
    }

    New-HeliosReport -Name 'status' -Status 'completed' -Checks $Checks
}

function Invoke-AzureCommand {
    param([string]$SubAction)
    Write-HeliosHeader "azure $SubAction"

    if ($SubAction -eq 'setup') {
        if (-not (Test-Path $AzureBootstrapPath)) {
            throw "Azure bootstrap script not found: $AzureBootstrapPath"
        }
        & $AzureBootstrapPath @RemainingArgs
        New-HeliosReport -Name 'azure-setup' -Status 'completed' -Checks @([ordered]@{ name = 'azure:setup'; status = 'ok'; message = 'bootstrap invoked' }) -Commands @("$AzureBootstrapPath $($RemainingArgs -join ' ')")
        return
    }

    if ($SubAction -ne 'verify') {
        throw "Unknown azure action '$SubAction'. Use setup or verify."
    }

    $Checks = New-Object System.Collections.Generic.List[object]
    if (-not (Test-CommandAvailable az)) {
        Write-HeliosFail 'Azure CLI not found'
        $Checks.Add([ordered]@{ name = 'azure:cli'; status = 'fail'; message = 'az not found' })
        New-HeliosReport -Name 'azure-verify' -Status 'failed' -Checks $Checks
        throw 'Azure CLI is required for azure verify.'
    }

    $Account = (& az account show --query '{id:id,tenantId:tenantId,name:name}' -o json 2>$null) -join ''
    if ($Account) {
        Write-HeliosOk 'Azure account context available'
        $Checks.Add([ordered]@{ name = 'azure:account'; status = 'ok'; message = 'account context available' })
    }
    else {
        Write-HeliosWarn 'Azure account context unavailable; run azure setup or az login'
        $Checks.Add([ordered]@{ name = 'azure:account'; status = 'warn'; message = 'not logged in or no active subscription' })
    }

    foreach ($ExtensionName in @('azure-devops', 'containerapp', 'ml', 'resource-graph')) {
        $Installed = (& az extension show --name $ExtensionName --query name -o tsv 2>$null) -join ''
        if ($Installed) {
            Write-HeliosOk "Azure extension $ExtensionName installed"
            $Checks.Add([ordered]@{ name = "azure:extension:$ExtensionName"; status = 'ok'; message = 'installed' })
        }
        else {
            Write-HeliosWarn "Azure extension $ExtensionName missing"
            $Checks.Add([ordered]@{ name = "azure:extension:$ExtensionName"; status = 'warn'; message = 'missing' })
        }
    }

    foreach ($VariableName in @('HELIOS_AZURE_SUBSCRIPTION_ID', 'HELIOS_AZURE_TENANT_ID', 'HELIOS_AZURE_LOCATION', 'HELIOS_RESOURCE_GROUP', 'HELIOS_ENVIRONMENT', 'HELIOS_HERMES_XCORE_ENABLED')) {
        if ([Environment]::GetEnvironmentVariable($VariableName)) {
            $Checks.Add([ordered]@{ name = "env:$VariableName"; status = 'ok'; message = 'set' })
        }
        else {
            $Checks.Add([ordered]@{ name = "env:$VariableName"; status = 'warn'; message = 'not set' })
        }
    }

    New-HeliosReport -Name 'azure-verify' -Status 'completed' -Checks $Checks -Commands @('az account show', 'az extension show')
}

function Assert-CleanGitTree {
    if (-not (Test-CommandAvailable git)) {
        throw 'git is required.'
    }
    $Porcelain = (& git status --porcelain 2>$null) -join ''
    if ($Porcelain) {
        throw 'Working tree must be clean before branch integration.'
    }
}

function Invoke-BranchesCommand {
    param([string]$SubAction)
    Write-HeliosHeader "branches $SubAction"

    if (-not (Test-CommandAvailable git)) {
        throw 'git is required for branch commands.'
    }

    switch ($SubAction) {
        'fetch' {
            Invoke-ExternalCommand git @('fetch', '--all', '--prune')
            New-HeliosReport -Name 'branches-fetch' -Status 'completed' -Checks @([ordered]@{ name = 'git:fetch'; status = 'ok'; message = 'fetch completed' }) -Commands @('git fetch --all --prune')
        }
        'list' {
            $Branches = (& git branch -r 2>$null)
            $Branches | Write-Host
            $Targets = $Branches | Where-Object { $_ -match 'helios|hermes|HE' }
            New-HeliosReport -Name 'branches-list' -Status 'completed' -Checks @([ordered]@{ name = 'git:remote-branches'; status = 'ok'; message = "target branches: $($Targets.Count)" }) -Commands @('git branch -r')
        }
        'integrate' {
            Assert-CleanGitTree
            $IntegrationBranch = 'integration/helios-hermes-xcore'
            $CurrentBranches = (& git branch --format '%(refname:short)')
            if ($CurrentBranches -contains $IntegrationBranch) {
                Invoke-ExternalCommand git @('switch', $IntegrationBranch)
            }
            else {
                Invoke-ExternalCommand git @('switch', '-c', $IntegrationBranch)
            }

            $PreferredBranches = @('upstream/helios-control', 'upstream/hermes-fleet-production')
            $Merged = New-Object System.Collections.Generic.List[string]
            $Skipped = New-Object System.Collections.Generic.List[string]
            foreach ($BranchName in $PreferredBranches) {
                $Exists = (& git rev-parse --verify --quiet $BranchName 2>$null)
                if ($LASTEXITCODE -eq 0) {
                    Invoke-ExternalCommand git @('merge', '--no-ff', $BranchName)
                    $Merged.Add($BranchName)
                }
                else {
                    Write-HeliosWarn "$BranchName not found; skipping"
                    $Skipped.Add($BranchName)
                }
            }
            $Check = [ordered]@{ name = 'git:integrate'; status = 'ok'; message = "merged=$($Merged -join ','); skipped=$($Skipped -join ',')" }
            New-HeliosReport -Name 'branches-integrate' -Status 'completed' -Checks @($Check) -Commands @('git switch -c integration/helios-hermes-xcore', 'git merge --no-ff upstream/helios-control', 'git merge --no-ff upstream/hermes-fleet-production')
        }
        default {
            throw "Unknown branches action '$SubAction'. Use fetch, list, or integrate."
        }
    }
}

function Get-HeliosAgentRegistry {
    if (-not (Test-Path $AgentRegistryPath)) {
        throw "Agent registry not found: $AgentRegistryPath"
    }
    return Get-Content $AgentRegistryPath -Raw | ConvertFrom-Json
}


function Invoke-GitHubCommand {
    param([string]$SubAction)
    Write-HeliosHeader "github $SubAction"
    $ScriptPath = Join-Path $RepoRoot 'scripts/github/mass_integration.py'
    if (-not (Test-Path $ScriptPath)) {
        throw "Mass integration script not found: $ScriptPath"
    }
    if ($SubAction -eq 'repo-verify' -or $SubAction -eq 'repo-setup') {
        $RepoSetupPath = Join-Path $RepoRoot 'scripts/github/setup_repository.py'
        if (-not (Test-Path $RepoSetupPath)) {
            throw "GitHub repository setup script not found: $RepoSetupPath"
        }
        $RepoMode = if ($SubAction -eq 'repo-setup') { 'setup' } else { 'verify' }
        $RepoArgs = @($RepoSetupPath, $RepoMode) + $RemainingArgs
        Invoke-ExternalCommand python3 $RepoArgs
        New-HeliosReport -Name "github-$SubAction" -Status 'completed' -Checks @([ordered]@{ name = "github:$SubAction"; status = 'ok'; message = 'repository setup command completed' }) -Commands @("python3 $($RepoArgs -join ' ')")
        return
    }
    $ModeMap = @{
        'mass-score' = 'score'
        'mass-plan' = 'plan'
        'mass-branch' = 'branch'
        'mass-pr' = 'pr'
        'mass-merge' = 'merge'
        'mass-all' = 'all'
    }
    if (-not $ModeMap.ContainsKey($SubAction)) {
        throw "Unknown github action '$SubAction'. Use repo-verify, repo-setup, mass-score, mass-plan, mass-branch, mass-pr, mass-merge, or mass-all."
    }
    $Mode = $ModeMap[$SubAction]
    $Args = @($ScriptPath, $Mode) + $RemainingArgs
    Invoke-ExternalCommand python3 $Args
    New-HeliosReport -Name "github-$SubAction" -Status 'completed' -Checks @([ordered]@{ name = "github:$SubAction"; status = 'ok'; message = 'mass integration command completed' }) -Commands @("python3 $($Args -join ' ')")
}


function Invoke-UpgradeCommand {
    param([string]$SubAction)
    Write-HeliosHeader "upgrade $SubAction"
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/helios_auto_upgrade.py'
    if (-not (Test-Path $ScriptPath)) {
        throw "Auto-upgrade script not found: $ScriptPath"
    }
    $Mode = if ($SubAction -eq 'default') { 'plan' } else { $SubAction }
    if ($Mode -notin @('plan', 'verify', 'gui', 'apply')) {
        throw "Unknown upgrade action '$SubAction'. Use plan, verify, gui, or apply."
    }
    $Args = @($ScriptPath, $Mode) + $RemainingArgs
    Invoke-ExternalCommand python3 $Args
    New-HeliosReport -Name "upgrade-$Mode" -Status 'completed' -Checks @([ordered]@{ name = "upgrade:$Mode"; status = 'ok'; message = 'auto-upgrade command completed' }) -Commands @("python3 $($Args -join ' ')")
}

function Invoke-AgentsCommand {
    param([string]$SubAction)
    Write-HeliosHeader "agents $SubAction"
    $Registry = Get-HeliosAgentRegistry

    switch ($SubAction) {
        'list' {
            $Registry.agents | ForEach-Object { Write-Host "$($_.name) :: $($_.responsibility)" }
            New-HeliosReport -Name 'agents-list' -Status 'completed' -Checks @([ordered]@{ name = 'agents:list'; status = 'ok'; message = "agents=$($Registry.agents.Count)" })
        }
        'validate' {
            $Checks = New-Object System.Collections.Generic.List[object]
            foreach ($Agent in $Registry.agents) {
                $MissingTools = @($Agent.requiredTools | Where-Object { -not (Test-CommandAvailable $_) })
                if ($MissingTools.Count -eq 0) {
                    Write-HeliosOk "$($Agent.name) tools available"
                    $Checks.Add([ordered]@{ name = "agent:$($Agent.name)"; status = 'ok'; message = 'required tools available' })
                }
                else {
                    Write-HeliosWarn "$($Agent.name) missing tools: $($MissingTools -join ', ')"
                    $Checks.Add([ordered]@{ name = "agent:$($Agent.name)"; status = 'warn'; message = "missing tools: $($MissingTools -join ', ')" })
                }
            }
            New-HeliosReport -Name 'agents-validate' -Status 'completed' -Checks $Checks
        }
        'run' {
            $AgentName = if ($RemainingArgs.Count -gt 0) { $RemainingArgs[0] } else { throw 'agents run requires an agent name.' }
            $Agent = @($Registry.agents | Where-Object { $_.name -eq $AgentName })[0]
            if (-not $Agent) {
                throw "Unknown agent '$AgentName'."
            }
            Write-Host "> $($Agent.command)"
            Invoke-Expression $Agent.command
            New-HeliosReport -Name "agent-$AgentName" -Status 'completed' -Checks @([ordered]@{ name = "agent:$AgentName"; status = 'ok'; message = 'agent command completed' }) -Commands @($Agent.command)
        }
        default {
            throw "Unknown agents action '$SubAction'. Use list, validate, or run."
        }
    }
}

function Invoke-BuildCommand {
    param([string]$SubAction)
    Write-HeliosHeader "build $SubAction"
    $Commands = New-Object System.Collections.Generic.List[string]

    function Invoke-BuildStep {
        param([string]$Label, [string]$FilePath, [string[]]$Arguments)
        Write-HeliosHeader "build $Label"
        $Commands.Add("$FilePath $($Arguments -join ' ')")
        Invoke-ExternalCommand $FilePath $Arguments
    }

    switch ($SubAction) {
        'contracts' { Invoke-BuildStep 'contracts' 'dotnet' @('build', 'src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj', '--configuration', 'Release') }
        'csharp' { Invoke-BuildStep 'csharp' 'dotnet' @('build', 'HELIOS.Platform.csproj', '--configuration', 'Release') }
        'fsharp' { Invoke-BuildStep 'fsharp' 'dotnet' @('build', 'src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj', '--configuration', 'Release') }
        'native' {
            Invoke-BuildStep 'native-configure' 'cmake' @('-S', 'src/native/HELIOS.Native.Performance', '-B', 'build/native-performance')
            Invoke-BuildStep 'native-build' 'cmake' @('--build', 'build/native-performance', '--config', 'Release')
        }
        'frontend' { Invoke-BuildStep 'frontend' 'dotnet' @('build', 'docs/ui-xenoblade/HELIOS.WPF.csproj', '--configuration', 'Release') }
        'all' {
            foreach ($Layer in @('contracts', 'csharp', 'fsharp', 'native', 'frontend')) {
                Invoke-BuildCommand $Layer
            }
            return
        }
        default { throw "Unknown build action '$SubAction'." }
    }
    New-HeliosReport -Name "build-$SubAction" -Status 'completed' -Checks @([ordered]@{ name = "build:$SubAction"; status = 'ok'; message = 'completed' }) -Commands $Commands
}

function Invoke-TestCommand {
    param([string]$SubAction)
    Write-HeliosHeader "test $SubAction"
    $Commands = New-Object System.Collections.Generic.List[string]

    function Invoke-TestStep {
        param([string]$Label, [string]$FilePath, [string[]]$Arguments)
        Write-HeliosHeader "test $Label"
        $Commands.Add("$FilePath $($Arguments -join ' ')")
        Invoke-ExternalCommand $FilePath $Arguments
    }

    switch ($SubAction) {
        'csharp' { Invoke-TestStep 'csharp' 'dotnet' @('test', 'src/tests/HELIOS.Platform.Tests.csproj', '--configuration', 'Release') }
        'security' { Invoke-TestStep 'security' 'dotnet' @('test', 'tests/SecurityValidationTests.csproj', '--configuration', 'Release') }
        'fsharp' { Invoke-TestStep 'fsharp' 'dotnet' @('test', 'tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj', '--configuration', 'Release') }
        'native' {
            Invoke-BuildCommand 'native'
            $Commands.Add('native benchmark placeholder')
            Write-HeliosWarn 'Native benchmarks are not implemented yet; build completed as readiness check.'
        }
        'python-aihub' {
            $Probe = Join-Path $RepoRoot 'tools/aihub/smoke-test.py'
            if (Test-Path $Probe) {
                Invoke-TestStep 'python-aihub' 'python' @($Probe)
            }
            else {
                Write-HeliosWarn 'Python AIHub smoke test is not implemented yet.'
                $Commands.Add('python tools/aihub/smoke-test.py')
            }
        }
        'all' {
            foreach ($Domain in @('csharp', 'security', 'fsharp', 'native', 'python-aihub')) {
                Invoke-TestCommand $Domain
            }
            return
        }
        default { throw "Unknown test action '$SubAction'." }
    }
    New-HeliosReport -Name "test-$SubAction" -Status 'completed' -Checks @([ordered]@{ name = "test:$SubAction"; status = 'ok'; message = 'completed or reported readiness gap' }) -Commands $Commands
}

function Invoke-ReportsCommand {
    param([string]$SubAction)
    Write-HeliosHeader "reports $SubAction"
    if ($SubAction -ne 'latest') {
        throw "Unknown reports action '$SubAction'. Use latest."
    }
    if (-not (Test-Path $ReportRoot)) {
        Write-HeliosWarn 'No generated reports found.'
        return
    }
    $Latest = Get-ChildItem $ReportRoot -Filter '*.md' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($Latest) {
        Get-Content $Latest.FullName | Write-Host
    }
    else {
        Write-HeliosWarn 'No generated Markdown reports found.'
    }
}

function Invoke-GateCommand {
    param([string]$SubAction)
    if ($SubAction -ne 'final') {
        throw "Unknown gate action '$SubAction'. Use final."
    }
    Write-HeliosHeader 'gate final'
    Assert-CleanGitTree
    Invoke-ExternalCommand git @('diff', '--check')
    Invoke-Status
    Invoke-AzureCommand 'verify'
    Invoke-BuildCommand 'all'
    Invoke-TestCommand 'all'
    New-HeliosReport -Name 'gate-final' -Status 'completed' -Checks @([ordered]@{ name = 'gate:final'; status = 'ok'; message = 'final gate completed' }) -Commands @('git diff --check', './tools/helios.ps1 status', './tools/helios.ps1 azure verify', './tools/helios.ps1 build all', './tools/helios.ps1 test all')
}

switch ($Command) {
    'help' { Show-Help }
    'setup' { Invoke-SetupCommand $Action }
    'status' { Invoke-Status }
    'azure' { Invoke-AzureCommand $Action }
    'branches' { Invoke-BranchesCommand $Action }
    'github' { Invoke-GitHubCommand $Action }
    'upgrade' { Invoke-UpgradeCommand $Action }
    'agents' { Invoke-AgentsCommand $Action }
    'build' { Invoke-BuildCommand $Action }
    'test' { Invoke-TestCommand $Action }
    'reports' { Invoke-ReportsCommand $Action }
    'gate' { Invoke-GateCommand $Action }
    default { Show-Help }
}

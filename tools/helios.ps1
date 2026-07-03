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
    [ValidateSet('help', 'setup', 'status', 'azure', 'branches', 'github', 'fix', 'policy', 'security', 'dashboard', 'connect', 'openai', 'models', 'hermes', 'm365', 'audit', 'upgrade', 'finish', 'start', 'ideas', 'llm', 'agents', 'build', 'test', 'reports', 'gate')]
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
  azure setup|verify|bicep-build|validate|what-if|deploy|report-build|report-validate|report-what-if|report-deploy|outputs
                                  Bootstrap Azure CLI and run Bicep validation/deploy/reporting.
  branches fetch|list|integrate  Fetch, inspect, and integrate HELIOS/Hermes branches.
  github mass-score|mass-plan|mass-branch|mass-pr|mass-merge|mass-all
                                  Score, branch, PR, and auto-merge mass integration.
  github conflict-forecast           Forecast merge conflict risk before branch apply.
  github repo-verify|repo-setup|connect-plan|connect-verify|connect-apply
                                  Verify/apply GitHub repository automation setup and connection.
  dashboard render                Render local/hybrid HTML operator dashboard.
  openai run                      Generate OpenAI Responses API dry-run report.
  models list                     Render the HELIOS model store.
  hermes models                   Verify Hermes/XCore model packs.
  m365 copilot                    Verify Copilot/Microsoft 365 readiness.
  audit latest                    Record/read automation audit events.
  connect plan|verify|apply       Run one-command local/cloud autoconnect setup.
  security vault                  Verify vault/secrets readiness without printing secrets.
  fix plan|apply|csharp              Plan/apply autofix tasks or parse C# build blockers.
  policy check                       Run safety policy checks before apply/deploy/merge.
  upgrade plan|verify|gui|apply      Plan, report, render GUI, or execute deep auto-upgrade.
  finish plan|verify|apply           Run the full setup finisher and final reports.
  start plan|verify|apply            Run the shortest ASAP start/merge sequence.
  ideas super|specialties          Rank next additions or render specialization matrix.
  llm plan                         Render multi-LLM cross-optimization routing plan.
  agents list|validate|runtime|xp|party|run       Inspect and run registered HELIOS agents.
  build contracts|csharp|fsharp|native|frontend|all
  test csharp|security|fsharp|fsharp-report|native|native-benchmark|python-aihub|all
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
        'deep-all' {
            $DeepPath = Join-Path $RepoRoot 'scripts/automation/deep_setup_all.py'
            Invoke-ExternalCommand python3 @($DeepPath, 'verify')
        }
        default { throw "Unknown setup action '$SubAction'. Use verify, all, or deep-all." }
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

    if ($SubAction -in @('report-build', 'report-validate', 'report-what-if', 'report-deploy', 'outputs')) {
        $ReportScript = Join-Path $RepoRoot 'scripts/azure/bicep_report.py'
        $ModeMap = @{ 'report-build' = 'build'; 'report-validate' = 'validate'; 'report-what-if' = 'what-if'; 'report-deploy' = 'deploy'; 'outputs' = 'outputs' }
        $ReportMode = $ModeMap[$SubAction]
        $ReportArgs = @($ReportScript, $ReportMode) + $RemainingArgs
        Invoke-ExternalCommand python3 $ReportArgs
        New-HeliosReport -Name "azure-$SubAction" -Status 'completed' -Checks @([ordered]@{ name = "azure:$SubAction"; status = 'ok'; message = 'structured Azure Bicep report completed' }) -Commands @("python3 $($ReportArgs -join ' ')")
        return
    }

    if ($SubAction -in @('bicep-build', 'validate', 'what-if', 'deploy')) {
        $TemplateFile = 'infra/azure/main.bicep'
        $ResourceGroup = if ($env:HELIOS_RESOURCE_GROUP) { $env:HELIOS_RESOURCE_GROUP } else { 'helios-hermes-xcore-rg' }
        $ParametersPath = 'infra/azure/parameters/dev.json'
        if ($SubAction -eq 'bicep-build') {
            Invoke-ExternalCommand az @('bicep', 'build', '--file', $TemplateFile)
            New-HeliosReport -Name 'azure-bicep-build' -Status 'completed' -Checks @([ordered]@{ name = 'azure:bicep-build'; status = 'ok'; message = $TemplateFile }) -Commands @("az bicep build --file $TemplateFile")
            return
        }
        $DeploymentArgs = @('deployment', 'group', $SubAction, '--resource-group', $ResourceGroup, '--template-file', $TemplateFile)
        if (Test-Path (Join-Path $RepoRoot $ParametersPath)) {
            $DeploymentArgs += @('--parameters', "@$ParametersPath")
        }
        if ($SubAction -eq 'deploy') {
            if ($RemainingArgs -notcontains '--apply') {
                throw 'azure deploy requires --apply to prevent accidental cloud changes.'
            }
            $DeploymentArgs[2] = 'create'
        }
        Invoke-ExternalCommand az $DeploymentArgs
        New-HeliosReport -Name "azure-$SubAction" -Status 'completed' -Checks @([ordered]@{ name = "azure:$SubAction"; status = 'ok'; message = $TemplateFile }) -Commands @("az $($DeploymentArgs -join ' ')")
        return
    }

    if ($SubAction -ne 'verify') {
        throw "Unknown azure action '$SubAction'. Use setup, verify, bicep-build, validate, what-if, deploy, report-build, report-validate, report-what-if, report-deploy, or outputs."
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
    if ($SubAction -eq 'takeover') {
        $TakeoverPath = Join-Path $RepoRoot 'scripts/github/github_takeover_status.py'
        Invoke-ExternalCommand python3 @($TakeoverPath)
        New-HeliosReport -Name 'github-takeover' -Status 'completed' -Checks @([ordered]@{ name = 'github:takeover'; status = 'ok'; message = 'GitHub takeover status completed' }) -Commands @("python3 $TakeoverPath")
        return
    }
    if ($SubAction -in @('connect-plan', 'connect-verify', 'connect-apply')) {
        $ConnectPath = Join-Path $RepoRoot 'scripts/github/connect_github.py'
        $ConnectModeMap = @{ 'connect-plan' = 'plan'; 'connect-verify' = 'verify'; 'connect-apply' = 'apply' }
        $ConnectMode = $ConnectModeMap[$SubAction]
        Invoke-ExternalCommand python3 @($ConnectPath, $ConnectMode)
        New-HeliosReport -Name "github-$SubAction" -Status 'completed' -Checks @([ordered]@{ name = "github:$SubAction"; status = 'ok'; message = 'GitHub connect completed' }) -Commands @("python3 $ConnectPath $ConnectMode")
        return
    }
    if ($SubAction -eq 'conflict-forecast') {
        $ForecastPath = Join-Path $RepoRoot 'scripts/github/conflict_forecast.py'
        Invoke-ExternalCommand python3 @($ForecastPath)
        New-HeliosReport -Name 'github-conflict-forecast' -Status 'completed' -Checks @([ordered]@{ name = 'github:conflict-forecast'; status = 'ok'; message = 'conflict forecast completed' }) -Commands @("python3 $ForecastPath")
        return
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
        throw "Unknown github action '$SubAction'. Use repo-verify, repo-setup, takeover, connect-plan, connect-verify, connect-apply, conflict-forecast, mass-score, mass-plan, mass-branch, mass-pr, mass-merge, or mass-all."
    }
    $Mode = $ModeMap[$SubAction]
    $Args = @($ScriptPath, $Mode) + $RemainingArgs
    Invoke-ExternalCommand python3 $Args
    New-HeliosReport -Name "github-$SubAction" -Status 'completed' -Checks @([ordered]@{ name = "github:$SubAction"; status = 'ok'; message = 'mass integration command completed' }) -Commands @("python3 $($Args -join ' ')")
}


function Invoke-FixCommand {
    param([string]$SubAction)
    Write-HeliosHeader "fix $SubAction"
    $Mode = if ($SubAction -eq 'default') { 'plan' } else { $SubAction }
    if ($Mode -eq 'csharp') {
        $ScriptPath = Join-Path $RepoRoot 'scripts/automation/fix_csharp_compile.py'
        Invoke-ExternalCommand python3 @($ScriptPath)
        New-HeliosReport -Name 'fix-csharp' -Status 'completed' -Checks @([ordered]@{ name = 'fix:csharp'; status = 'ok'; message = 'C# compile classifier completed' }) -Commands @("python3 $ScriptPath")
        return
    }
    if ($Mode -notin @('plan', 'apply')) {
        throw "Unknown fix action '$SubAction'. Use plan, apply, or csharp."
    }
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/autofix_loop.py'
    Invoke-ExternalCommand python3 @($ScriptPath, $Mode)
    New-HeliosReport -Name "fix-$Mode" -Status 'completed' -Checks @([ordered]@{ name = "fix:$Mode"; status = 'ok'; message = 'autofix loop completed' }) -Commands @("python3 $ScriptPath $Mode")
}

function Invoke-PolicyCommand {
    param([string]$SubAction)
    Write-HeliosHeader "policy $SubAction"
    if ($SubAction -ne 'check' -and $SubAction -ne 'default') {
        throw "Unknown policy action '$SubAction'. Use check."
    }
    $ScriptPath = Join-Path $RepoRoot 'scripts/security/policy_gate.py'
    $Args = @($ScriptPath) + $RemainingArgs
    Invoke-ExternalCommand python3 $Args
    New-HeliosReport -Name 'policy-check' -Status 'completed' -Checks @([ordered]@{ name = 'policy:check'; status = 'ok'; message = 'policy gate completed' }) -Commands @("python3 $($Args -join ' ')")
}

function Invoke-DashboardCommand {
    param([string]$SubAction)
    Write-HeliosHeader "dashboard $SubAction"
    if ($SubAction -ne 'render' -and $SubAction -ne 'default') { throw "Unknown dashboard action '$SubAction'. Use render." }
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/render_operator_dashboard.py'
    Invoke-ExternalCommand python3 @($ScriptPath)
    New-HeliosReport -Name 'dashboard-render' -Status 'completed' -Checks @([ordered]@{ name = 'dashboard:render'; status = 'ok'; message = 'operator dashboard rendered' }) -Commands @("python3 $ScriptPath")
}

function Invoke-ConnectCommand {
    param([string]$SubAction)
    Write-HeliosHeader "connect $SubAction"
    $Mode = if ($SubAction -eq 'default') { 'plan' } else { $SubAction }
    if ($Mode -notin @('plan', 'verify', 'apply')) { throw "Unknown connect action '$SubAction'. Use plan, verify, or apply." }
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/autoconnect_setup.py'
    Invoke-ExternalCommand python3 @($ScriptPath, $Mode)
    New-HeliosReport -Name "connect-$Mode" -Status 'completed' -Checks @([ordered]@{ name = "connect:$Mode"; status = 'ok'; message = 'autoconnect setup completed' }) -Commands @("python3 $ScriptPath $Mode")
}

function Invoke-SecurityCommand {
    param([string]$SubAction)
    Write-HeliosHeader "security $SubAction"
    if ($SubAction -ne 'vault' -and $SubAction -ne 'default') { throw "Unknown security action '$SubAction'. Use vault." }
    $ScriptPath = Join-Path $RepoRoot 'scripts/security/vault_readiness.py'
    Invoke-ExternalCommand python3 @($ScriptPath, 'verify')
    New-HeliosReport -Name 'security-vault' -Status 'completed' -Checks @([ordered]@{ name = 'security:vault'; status = 'ok'; message = 'vault readiness completed' }) -Commands @("python3 $ScriptPath verify")
}

function Invoke-OpenAiCommand {
    param([string]$SubAction)
    Write-HeliosHeader "openai $SubAction"
    if ($SubAction -ne 'run' -and $SubAction -ne 'default') { throw "Unknown openai action '$SubAction'. Use run." }
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/openai_responses_runner.py'
    Invoke-ExternalCommand python3 @($ScriptPath)
    New-HeliosReport -Name 'openai-run' -Status 'completed' -Checks @([ordered]@{ name = 'openai:run'; status = 'ok'; message = 'OpenAI dry-run report generated' }) -Commands @("python3 $ScriptPath")
}

function Invoke-ModelsCommand {
    param([string]$SubAction)
    Write-HeliosHeader "models $SubAction"
    if ($SubAction -ne 'list' -and $SubAction -ne 'default') { throw "Unknown models action '$SubAction'. Use list." }
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/model_store_report.py'
    Invoke-ExternalCommand python3 @($ScriptPath)
    New-HeliosReport -Name 'models-list' -Status 'completed' -Checks @([ordered]@{ name = 'models:list'; status = 'ok'; message = 'model store generated' }) -Commands @("python3 $ScriptPath")
}

function Invoke-HermesCommand {
    param([string]$SubAction)
    Write-HeliosHeader "hermes $SubAction"
    if ($SubAction -ne 'models' -and $SubAction -ne 'default') { throw "Unknown hermes action '$SubAction'. Use models." }
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/hermes_xcore_model_setup.py'
    Invoke-ExternalCommand python3 @($ScriptPath)
    New-HeliosReport -Name 'hermes-models' -Status 'completed' -Checks @([ordered]@{ name = 'hermes:models'; status = 'ok'; message = 'Hermes/XCore models generated' }) -Commands @("python3 $ScriptPath")
}

function Invoke-M365Command {
    param([string]$SubAction)
    Write-HeliosHeader "m365 $SubAction"
    if ($SubAction -ne 'copilot' -and $SubAction -ne 'default') { throw "Unknown m365 action '$SubAction'. Use copilot." }
    $ScriptPath = Join-Path $RepoRoot 'scripts/microsoft365/copilot_m365_readiness.py'
    Invoke-ExternalCommand python3 @($ScriptPath)
    New-HeliosReport -Name 'm365-copilot' -Status 'completed' -Checks @([ordered]@{ name = 'm365:copilot'; status = 'ok'; message = 'Copilot/M365 readiness generated' }) -Commands @("python3 $ScriptPath")
}

function Invoke-AuditCommand {
    param([string]$SubAction)
    Write-HeliosHeader "audit $SubAction"
    if ($SubAction -ne 'latest' -and $SubAction -ne 'default') { throw "Unknown audit action '$SubAction'. Use latest." }
    $ScriptPath = Join-Path $RepoRoot 'scripts/security/automation_audit.py'
    Invoke-ExternalCommand python3 @($ScriptPath, '--command', 'audit latest')
    New-HeliosReport -Name 'audit-latest' -Status 'completed' -Checks @([ordered]@{ name = 'audit:latest'; status = 'ok'; message = 'audit latest generated' }) -Commands @("python3 $ScriptPath --command 'audit latest'")
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


function Invoke-FinishCommand {
    param([string]$SubAction)
    Write-HeliosHeader "finish $SubAction"
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/finish_helios_setup.py'
    if (-not (Test-Path $ScriptPath)) {
        throw "Finish setup script not found: $ScriptPath"
    }
    $Mode = if ($SubAction -eq 'default') { 'verify' } else { $SubAction }
    if ($Mode -notin @('plan', 'verify', 'apply')) {
        throw "Unknown finish action '$SubAction'. Use plan, verify, or apply."
    }
    $Args = @($ScriptPath, $Mode) + $RemainingArgs
    Invoke-ExternalCommand python3 $Args
    New-HeliosReport -Name "finish-$Mode" -Status 'completed' -Checks @([ordered]@{ name = "finish:$Mode"; status = 'ok'; message = 'finish setup command completed' }) -Commands @("python3 $($Args -join ' ')")
}



function Invoke-StartCommand {
    param([string]$SubAction)
    Write-HeliosHeader "start $SubAction"
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/start_asap.py'
    if (-not (Test-Path $ScriptPath)) {
        throw "Start ASAP script not found: $ScriptPath"
    }
    $Mode = if ($SubAction -eq 'default') { 'plan' } else { $SubAction }
    if ($Mode -notin @('plan', 'verify', 'apply')) {
        throw "Unknown start action '$SubAction'. Use plan, verify, or apply."
    }
    $Args = @($ScriptPath, $Mode) + $RemainingArgs
    Invoke-ExternalCommand python3 $Args
    New-HeliosReport -Name "start-$Mode" -Status 'completed' -Checks @([ordered]@{ name = "start:$Mode"; status = 'ok'; message = 'ASAP start sequence completed' }) -Commands @("python3 $($Args -join ' ')")
}

function Invoke-IdeasCommand {
    param([string]$SubAction)
    Write-HeliosHeader "ideas $SubAction"
    if ($SubAction -eq 'specialties') {
        $ScriptPath = Join-Path $RepoRoot 'scripts/automation/specialization_matrix.py'
        Invoke-ExternalCommand python3 @($ScriptPath)
        New-HeliosReport -Name 'ideas-specialties' -Status 'completed' -Checks @([ordered]@{ name = 'ideas:specialties'; status = 'ok'; message = 'specialization matrix generated' }) -Commands @("python3 $ScriptPath")
        return
    }
    if ($SubAction -ne 'super' -and $SubAction -ne 'default') {
        throw "Unknown ideas action '$SubAction'. Use super or specialties."
    }
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/super_automation_backlog.py'
    Invoke-ExternalCommand python3 @($ScriptPath)
    New-HeliosReport -Name 'ideas-super' -Status 'completed' -Checks @([ordered]@{ name = 'ideas:super'; status = 'ok'; message = 'super automation backlog generated' }) -Commands @("python3 $ScriptPath")
}


function Invoke-LlmCommand {
    param([string]$SubAction)
    Write-HeliosHeader "llm $SubAction"
    if ($SubAction -ne 'plan' -and $SubAction -ne 'default') {
        throw "Unknown llm action '$SubAction'. Use plan."
    }
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/llm_router_plan.py'
    Invoke-ExternalCommand python3 @($ScriptPath)
    New-HeliosReport -Name 'llm-plan' -Status 'completed' -Checks @([ordered]@{ name = 'llm:plan'; status = 'ok'; message = 'LLM router plan generated' }) -Commands @("python3 $ScriptPath")
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
        'party' {
            $ScriptPath = Join-Path $RepoRoot 'scripts/learning/agent_party.py'
            Invoke-ExternalCommand python3 @($ScriptPath)
            New-HeliosReport -Name 'agents-party' -Status 'completed' -Checks @([ordered]@{ name = 'agents:party'; status = 'ok'; message = 'agent party generated' }) -Commands @("python3 $ScriptPath")
        }
        'xp' {
            $ScriptPath = Join-Path $RepoRoot 'scripts/learning/agent_xp.py'
            Invoke-ExternalCommand python3 @($ScriptPath)
            New-HeliosReport -Name 'agents-xp' -Status 'completed' -Checks @([ordered]@{ name = 'agents:xp'; status = 'ok'; message = 'agent XP generated' }) -Commands @("python3 $ScriptPath")
        }
        'runtime' {
            $ScriptPath = Join-Path $RepoRoot 'scripts/automation/agent_runtime_matrix.py'
            Invoke-ExternalCommand python3 @($ScriptPath)
            New-HeliosReport -Name 'agents-runtime' -Status 'completed' -Checks @([ordered]@{ name = 'agents:runtime'; status = 'ok'; message = 'runtime matrix generated' }) -Commands @("python3 $ScriptPath")
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
            throw "Unknown agents action '$SubAction'. Use list, validate, runtime, xp, party, or run."
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
        'native-benchmark' { Invoke-TestStep 'native-benchmark' 'python3' @('scripts/native/benchmark_native.py') }
        'fsharp-report' { Invoke-TestStep 'fsharp-report' 'python3' @('scripts/analytics/fsharp_test_report.py') }
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
            foreach ($Domain in @('csharp', 'security', 'fsharp', 'fsharp-report', 'native', 'native-benchmark', 'python-aihub')) {
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
    $ScriptPath = Join-Path $RepoRoot 'scripts/automation/final_gate.py'
    Invoke-ExternalCommand python3 @($ScriptPath)
    New-HeliosReport -Name 'gate-final' -Status 'completed' -Checks @([ordered]@{ name = 'gate:final'; status = 'ok'; message = 'blocking final gate completed' }) -Commands @("python3 $ScriptPath")
}

switch ($Command) {
    'help' { Show-Help }
    'setup' { Invoke-SetupCommand $Action }
    'status' { Invoke-Status }
    'azure' { Invoke-AzureCommand $Action }
    'branches' { Invoke-BranchesCommand $Action }
    'github' { Invoke-GitHubCommand $Action }
    'fix' { Invoke-FixCommand $Action }
    'policy' { Invoke-PolicyCommand $Action }
    'security' { Invoke-SecurityCommand $Action }
    'dashboard' { Invoke-DashboardCommand $Action }
    'connect' { Invoke-ConnectCommand $Action }
    'openai' { Invoke-OpenAiCommand $Action }
    'models' { Invoke-ModelsCommand $Action }
    'hermes' { Invoke-HermesCommand $Action }
    'm365' { Invoke-M365Command $Action }
    'audit' { Invoke-AuditCommand $Action }
    'upgrade' { Invoke-UpgradeCommand $Action }
    'finish' { Invoke-FinishCommand $Action }
    'start' { Invoke-StartCommand $Action }
    'ideas' { Invoke-IdeasCommand $Action }
    'llm' { Invoke-LlmCommand $Action }
    'agents' { Invoke-AgentsCommand $Action }
    'build' { Invoke-BuildCommand $Action }
    'test' { Invoke-TestCommand $Action }
    'reports' { Invoke-ReportsCommand $Action }
    'gate' { Invoke-GateCommand $Action }
    default { Show-Help }
}

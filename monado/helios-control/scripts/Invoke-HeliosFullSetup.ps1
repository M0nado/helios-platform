[CmdletBinding()]
param(
    [ValidateSet('Plan', 'Apply')]
    [string] $Mode = 'Plan',

    [switch] $CheckAuthentication,
    [switch] $IncludeNetworkTools,
    [switch] $CreateLocalEnv,
    [switch] $RunRestore,
    [switch] $RunTests,
    [switch] $BuildBicep,
    [switch] $RunAzureWhatIf,

    [ValidateSet('dev', 'test', 'preview', 'prod')]
    [string] $EnvironmentName = 'dev',

    [string] $ResourceGroup,
    [string] $ContainerRegistryName,
    [string] $ContainerImage,
    [string] $EntraClientId,
    [string] $EntraTenantId,
    [string] $AllowedPrincipalObjectId,
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string] $SourceCommitSha,

    [string] $ReportPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$controlRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path

$effectiveCreateLocalEnv = $CreateLocalEnv.IsPresent -or $Mode -eq 'Apply'
$effectiveRunRestore = $RunRestore.IsPresent -or $Mode -eq 'Apply'
$effectiveRunTests = $RunTests.IsPresent
if ($effectiveRunTests) { $effectiveRunRestore = $true }
$effectiveBuildBicep = $BuildBicep.IsPresent -or $Mode -eq 'Apply'

$steps = [System.Collections.Generic.List[object]]::new()

function Get-RelativePath {
    param([Parameter(Mandatory)] [string] $Path)

    $full = [IO.Path]::GetFullPath($Path)
    $root = [IO.Path]::GetFullPath($script:repoRoot)
    if ($full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        $relative = $full.Substring($root.Length).TrimStart('\')
        return ($relative -replace '\\', '/')
    }
    return $full
}

function Add-StepResult {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [ValidateSet('configured', 'pending', 'blocked', 'failed')] [string] $Status,
        [Parameter(Mandatory)] [string] $Detail,
        [AllowNull()] [object] $Data
    )

    $steps.Add([pscustomobject][ordered]@{
            name = $Name
            status = $Status
            detail = $Detail
            data = $Data
        })
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)] [string] $Command,
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $Operation
    )

    $output = & $Command @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $lines = @($output | ForEach-Object { "$_".Trim() } | Where-Object { $_ } | Select-Object -First 12)

    return [pscustomobject]@{
        operation = $Operation
        exitCode = $exitCode
        output = $lines
    }
}

function Test-JsonFile {
    param([Parameter(Mandatory)] [string] $Path)

    try {
        Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json | Out-Null
        return $null
    }
    catch {
        return $_.Exception.Message
    }
}

function Resolve-SourceCommitSha {
    if (-not [string]::IsNullOrWhiteSpace($script:SourceCommitSha)) {
        return $script:SourceCommitSha.ToLowerInvariant()
    }

    if ($env:GITHUB_SHA -match '^[0-9a-fA-F]{40}$') {
        return $env:GITHUB_SHA.ToLowerInvariant()
    }

    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($git) {
        $resolved = & $git.Source rev-parse HEAD 2>$null
        if ($LASTEXITCODE -eq 0) {
            $candidate = @($resolved | Select-Object -First 1) -join ''
            $candidate = $candidate.Trim()
            if ($candidate -match '^[0-9a-fA-F]{40}$') {
                return $candidate.ToLowerInvariant()
            }
        }
    }

    return $null
}

function Test-IntegrationContractInvariants {
    param(
        [Parameter(Mandatory)] [string] $RepositoriesMapPath,
        [Parameter(Mandatory)] [string] $EventSchemaPath
    )

    $errors = [System.Collections.Generic.List[string]]::new()
    $requiredRepositories = @(
        'M0nado/helios-platform',
        'Heli0s-Dynamics/adaptive-multibrain-bootstrap',
        'M0nado/Helios-Control-Center',
        'M0nado/helios-ai-hub',
        'M0nado/helios-monado-blade',
        'Yolkster64/hermes-fleet-platforms'
    )

    $repoMap = $null
    $schema = $null

    try {
        $repoMap = Get-Content -LiteralPath $RepositoriesMapPath -Raw | ConvertFrom-Json
    }
    catch {
        $errors.Add("Unable to parse repositories map '$([IO.Path]::GetFileName($RepositoriesMapPath))': $($_.Exception.Message)")
    }

    try {
        $schema = Get-Content -LiteralPath $EventSchemaPath -Raw | ConvertFrom-Json
    }
    catch {
        $errors.Add("Unable to parse event schema '$([IO.Path]::GetFileName($EventSchemaPath))': $($_.Exception.Message)")
    }

    if ($repoMap) {
        if ($repoMap.canonicalPlatform -ne 'M0nado/helios-platform') {
            $errors.Add('Canonical platform must be M0nado/helios-platform.')
        }
        if ($repoMap.controlPlane -ne 'Heli0s-Dynamics/adaptive-multibrain-bootstrap') {
            $errors.Add('Control plane mapping must be Heli0s-Dynamics/adaptive-multibrain-bootstrap.')
        }

        $presentNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($entry in @($repoMap.repositories)) {
            if ($entry -and -not [string]::IsNullOrWhiteSpace([string] $entry.name)) {
                [void] $presentNames.Add([string] $entry.name)
            }
        }

        foreach ($requiredRepository in $requiredRepositories) {
            if (-not $presentNames.Contains($requiredRepository)) {
                $errors.Add("Repository role map is missing required entry '$requiredRepository'.")
            }
        }
    }

    if ($schema) {
        if ($schema.title -ne 'HELIOS Integration Event') {
            $errors.Add('Integration event schema title must be HELIOS Integration Event.')
        }
    }

    return @($errors)
}

$moduleProjects = @(
    Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
        Where-Object {
            $normalizedPath = $_.FullName.Replace('\', '/')
            $normalizedPath -notmatch '/(bin|obj|artifacts|packages|reference|docs|samples)/' -and
            ($_.BaseName -match '^(HELIOS\.|Helios\.Connect|MonadoBlade\.)')
        } |
        Sort-Object FullName
)

if ($moduleProjects.Count -gt 0) {
    Add-StepResult `
        -Name 'helios-modules' `
        -Status 'configured' `
        -Detail "Discovered $($moduleProjects.Count) HELIOS module projects." `
        -Data ([ordered]@{
            count = $moduleProjects.Count
            projects = @($moduleProjects | ForEach-Object { Get-RelativePath -Path $_.FullName })
        })
}
else {
    Add-StepResult `
        -Name 'helios-modules' `
        -Status 'blocked' `
        -Detail 'No HELIOS module projects were discovered.' `
        -Data $null
}

if ($effectiveRunRestore -or $effectiveRunTests) {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Add-StepResult `
            -Name 'module-restore-and-tests' `
            -Status 'blocked' `
            -Detail 'dotnet was not found on PATH.' `
            -Data ([ordered]@{
                restoreRequested = $effectiveRunRestore
                testsRequested = $effectiveRunTests
            })
    }
    else {
        $commandResults = [System.Collections.Generic.List[object]]::new()
        $failedCommands = 0
        $restoreTargets = @(
            (Join-Path $repoRoot 'HELIOS.Platform.slnx')
            (Join-Path $controlRoot 'Helios.Connect.sln')
        ) | Where-Object { Test-Path -LiteralPath $_ }

        foreach ($target in $restoreTargets) {
            $relativeTarget = Get-RelativePath -Path $target
            $result = Invoke-NativeCommand `
                -Command $dotnet.Source `
                -Arguments @('restore', $target) `
                -Operation "dotnet restore $relativeTarget"
            if ($result.exitCode -ne 0) { $failedCommands++ }
            $commandResults.Add($result)
        }

        if ($effectiveRunTests) {
            $testTargets = @(
                (Join-Path $repoRoot 'HELIOS.Platform.slnx')
                (Join-Path $controlRoot 'Helios.Connect.sln')
            ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -Unique

            foreach ($testTarget in $testTargets) {
                $testArguments = @('test', $testTarget, '--configuration', 'Release')
                if ($effectiveRunRestore) {
                    $testArguments += '--no-restore'
                }
                $testResult = Invoke-NativeCommand `
                    -Command $dotnet.Source `
                    -Arguments $testArguments `
                    -Operation "dotnet test $(Get-RelativePath -Path $testTarget)"
                if ($testResult.exitCode -ne 0) { $failedCommands++ }
                $commandResults.Add($testResult)
            }
        }

        Add-StepResult `
            -Name 'module-restore-and-tests' `
            -Status $(if ($failedCommands -eq 0) { 'configured' } else { 'blocked' }) `
            -Detail $(if ($failedCommands -eq 0) { 'Requested .NET setup commands completed.' } else { "$failedCommands .NET setup command(s) failed." }) `
            -Data ([ordered]@{
                commands = @($commandResults)
                failedCommands = $failedCommands
            })
    }
}
else {
    Add-StepResult `
        -Name 'module-restore-and-tests' `
        -Status 'pending' `
        -Detail 'Restore/tests were not requested. Use -RunRestore and/or -RunTests.' `
        -Data ([ordered]@{
            restoreRequested = $false
            testsRequested = $false
        })
}

$workflowDirectories = @(
    (Join-Path $repoRoot '.github\workflows')
    (Join-Path $controlRoot '.github\workflows')
)

$workflowFiles = @()
foreach ($directory in $workflowDirectories) {
    if (Test-Path -LiteralPath $directory) {
        $workflowFiles += Get-ChildItem -LiteralPath $directory -File | Where-Object { $_.Extension -in @('.yml', '.yaml') }
    }
}

if ($workflowFiles.Count -eq 0) {
    Add-StepResult `
        -Name 'github-workflows' `
        -Status 'blocked' `
        -Detail 'No GitHub workflow files were found.' `
        -Data $null
}
else {
    $invalidFiles = [System.Collections.Generic.List[object]]::new()
    $pinnedActions = 0
    $unpinnedActions = 0

    foreach ($workflow in $workflowFiles) {
        $content = Get-Content -LiteralPath $workflow.FullName -Raw
        $issues = [System.Collections.Generic.List[string]]::new()
        if ($content -notmatch '(?m)^name\s*:') { $issues.Add('missing-top-level-name') }
        if ($content -notmatch '(?m)^(?:on|"on"|''on'')\s*:') { $issues.Add('missing-top-level-on') }
        if ($issues.Count -gt 0) {
            $invalidFiles.Add([pscustomobject]@{
                    file = Get-RelativePath -Path $workflow.FullName
                    issues = @($issues)
                })
        }

        $usesMatches = [regex]::Matches($content, '(?m)^\s*(?:-\s*)?uses:\s*([^\s#]+)')
        foreach ($match in $usesMatches) {
            $reference = $match.Groups[1].Value.Trim()
            if ($reference.StartsWith('./', [StringComparison]::Ordinal)) {
                continue
            }
            if ($reference -notmatch '@') {
                continue
            }
            if ($reference -match '@[0-9a-fA-F]{40}$') {
                $pinnedActions++
            }
            else {
                $unpinnedActions++
            }
        }
    }

    $rootWorkflowCount = @($workflowFiles | Where-Object { $_.FullName.StartsWith((Join-Path $repoRoot '.github\workflows'), [StringComparison]::OrdinalIgnoreCase) }).Count
    $controlWorkflowCount = @($workflowFiles | Where-Object { $_.FullName.StartsWith((Join-Path $controlRoot '.github\workflows'), [StringComparison]::OrdinalIgnoreCase) }).Count

    Add-StepResult `
        -Name 'github-workflows' `
        -Status $(if ($invalidFiles.Count -eq 0) { 'configured' } else { 'blocked' }) `
        -Detail $(if ($invalidFiles.Count -eq 0) { "Validated $($workflowFiles.Count) workflow files." } else { "$($invalidFiles.Count) workflow file(s) are missing required top-level keys." }) `
        -Data ([ordered]@{
            total = $workflowFiles.Count
            root = $rootWorkflowCount
            heliosControl = $controlWorkflowCount
            pinnedActionReferences = $pinnedActions
            unpinnedActionReferences = $unpinnedActions
            invalid = @($invalidFiles)
        })
}

$requiredFiles = @(
    'AGENTS.md',
    '.github/copilot-instructions.md',
    'config/integrations/repositories.json',
    'config/integrations/event-contract.schema.json',
    'docs/architecture/UNIFIED_AGENT_COMMUNICATION.md',
    'monado/helios-control/.env.example',
    'monado/helios-control/config/agent-fleet.json',
    'monado/helios-control/config/cloud-runtime.json',
    'monado/helios-control/config/integrations.json',
    'monado/helios-control/config/microsoft-toolchain.json',
    'monado/helios-control/infra/main.parameters.json'
)

$missingFiles = @()
foreach ($requiredFile in $requiredFiles) {
    $fullPath = Join-Path $repoRoot ($requiredFile -replace '/', '\')
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missingFiles += $requiredFile
    }
}

$jsonValidationTargets = [System.Collections.Generic.List[string]]::new()
foreach ($path in @(
        (Join-Path $repoRoot 'config\integrations')
        (Join-Path $controlRoot 'config')
    )) {
    if (Test-Path -LiteralPath $path) {
        foreach ($file in Get-ChildItem -LiteralPath $path -Filter '*.json' -File) {
            $jsonValidationTargets.Add($file.FullName)
        }
    }
}
foreach ($file in @(
        (Join-Path $controlRoot 'infra\main.parameters.json')
        (Join-Path $controlRoot 'infra\main.parameters.example.json')
    )) {
    if (Test-Path -LiteralPath $file) {
        $jsonValidationTargets.Add($file)
    }
}

$jsonErrors = [System.Collections.Generic.List[object]]::new()
foreach ($jsonFile in ($jsonValidationTargets | Sort-Object -Unique)) {
    $errorDetail = Test-JsonFile -Path $jsonFile
    if ($errorDetail) {
        $jsonErrors.Add([pscustomobject]@{
                file = Get-RelativePath -Path $jsonFile
                error = $errorDetail
            })
    }
}

$contractErrors = [System.Collections.Generic.List[object]]::new()
$repositoriesMapPath = Join-Path $repoRoot 'config\integrations\repositories.json'
$eventSchemaPath = Join-Path $repoRoot 'config\integrations\event-contract.schema.json'
if ((Test-Path -LiteralPath $repositoriesMapPath) -and (Test-Path -LiteralPath $eventSchemaPath)) {
    foreach ($contractError in (Test-IntegrationContractInvariants -RepositoriesMapPath $repositoriesMapPath -EventSchemaPath $eventSchemaPath)) {
        $contractErrors.Add([pscustomobject]@{
                scope = 'integration-contract'
                error = $contractError
            })
    }
}

$envExample = Join-Path $controlRoot '.env.example'
$envLocal = Join-Path $controlRoot '.env.local'
$envLocalState = 'present'
if (-not (Test-Path -LiteralPath $envLocal)) {
    if ($effectiveCreateLocalEnv) {
        Copy-Item -LiteralPath $envExample -Destination $envLocal -Force
        $envLocalState = 'created'
    }
    else {
        $envLocalState = 'missing'
    }
}

$configStatus = 'configured'
$configDetail = 'Configuration files validated.'
if ($missingFiles.Count -gt 0 -or $jsonErrors.Count -gt 0 -or $contractErrors.Count -gt 0) {
    $configStatus = 'blocked'
    $configDetail = 'Missing, invalid, or contract-mismatched configuration files were found.'
}
elseif ($envLocalState -eq 'missing') {
    $configStatus = 'pending'
    $configDetail = '.env.local is missing; run with -CreateLocalEnv or -Mode Apply.'
}

Add-StepResult `
    -Name 'config-files' `
    -Status $configStatus `
    -Detail $configDetail `
    -Data ([ordered]@{
        requiredCount = $requiredFiles.Count
        missing = @($missingFiles)
        jsonChecked = @($jsonValidationTargets | Sort-Object -Unique | ForEach-Object { Get-RelativePath -Path $_ })
        jsonErrors = @($jsonErrors)
        contractErrors = @($contractErrors)
        envLocal = [ordered]@{
            path = Get-RelativePath -Path $envLocal
            state = $envLocalState
        }
    })

$cliMatrixScript = Join-Path $PSScriptRoot 'Invoke-HeliosCliMatrix.ps1'
if (-not (Test-Path -LiteralPath $cliMatrixScript)) {
    Add-StepResult `
        -Name 'local-dev-tooling' `
        -Status 'blocked' `
        -Detail 'CLI matrix script was not found.' `
        -Data ([ordered]@{ script = Get-RelativePath -Path $cliMatrixScript })
}
else {
    try {
        $cliParams = @{}
        if ($IncludeNetworkTools) { $cliParams.IncludeNetworkTools = $true }
        if ($CheckAuthentication) { $cliParams.CheckAuthentication = $true }
        $cliRaw = (& $cliMatrixScript @cliParams | Out-String).Trim()
        $cliExit = $LASTEXITCODE
        $cliReport = $cliRaw | ConvertFrom-Json
        $missingRequiredIds = @($cliReport.tools | Where-Object { $_.required -and $_.status -ne 'ready' } | ForEach-Object { $_.id })
        $authenticationFailures = @()
        if ($CheckAuthentication) {
            $authenticationFailures = @($cliReport.authentication | Where-Object { $_.status -ne 'authenticated' })
        }
        $toolingReady = ($cliExit -eq 0 -and $cliReport.ready -and $authenticationFailures.Count -eq 0)

        Add-StepResult `
            -Name 'local-dev-tooling' `
            -Status $(if ($toolingReady) { 'configured' } else { 'blocked' }) `
            -Detail $(if ($toolingReady) { 'Required CLI tooling is ready.' } else { if ($authenticationFailures.Count -gt 0) { "$($authenticationFailures.Count) authentication check(s) are not ready." } else { "$($missingRequiredIds.Count) required CLI tool(s) are not ready." } }) `
            -Data ([ordered]@{
                executionEngine = $cliReport.executionEngine
                ready = $toolingReady
                missingRequired = $cliReport.missingRequired
                missingRequiredIds = @($missingRequiredIds)
                authenticationFailures = @($authenticationFailures | ForEach-Object { $_.id })
                tools = @($cliReport.tools)
                authentication = @($cliReport.authentication)
            })
    }
    catch {
        Add-StepResult `
            -Name 'local-dev-tooling' `
            -Status 'failed' `
            -Detail 'Unable to execute the CLI matrix check.' `
            -Data ([ordered]@{
                script = Get-RelativePath -Path $cliMatrixScript
                error = $_.Exception.Message
            })
    }
}

$bicepFiles = @()
$infraPath = Join-Path $controlRoot 'infra'
if (Test-Path -LiteralPath $infraPath) {
    $bicepFiles = @(Get-ChildItem -LiteralPath $infraPath -Filter '*.bicep' -File | Sort-Object FullName)
}

if ($bicepFiles.Count -eq 0) {
    Add-StepResult `
        -Name 'azure-resources-bicep' `
        -Status 'blocked' `
        -Detail 'No Bicep templates were found under monado/helios-control/infra.' `
        -Data $null
}
elseif (-not $effectiveBuildBicep) {
    Add-StepResult `
        -Name 'azure-resources-bicep' `
        -Status 'pending' `
        -Detail 'Bicep compilation was not requested. Use -BuildBicep or -Mode Apply.' `
        -Data ([ordered]@{
            templates = @($bicepFiles | ForEach-Object { Get-RelativePath -Path $_.FullName })
        })
}
else {
    $az = Get-Command az -ErrorAction SilentlyContinue
    if (-not $az) {
        Add-StepResult `
            -Name 'azure-resources-bicep' `
            -Status 'blocked' `
            -Detail 'Azure CLI (az) is required to compile Bicep templates.' `
            -Data ([ordered]@{
                templates = @($bicepFiles | ForEach-Object { Get-RelativePath -Path $_.FullName })
            })
    }
    else {
        $compileResults = [System.Collections.Generic.List[object]]::new()
        $compileFailures = 0
        foreach ($bicepFile in $bicepFiles) {
            $result = Invoke-NativeCommand `
                -Command $az.Source `
                -Arguments @('bicep', 'build', '--file', $bicepFile.FullName, '--stdout') `
                -Operation "az bicep build --file $(Get-RelativePath -Path $bicepFile.FullName)"
            if ($result.exitCode -ne 0) { $compileFailures++ }
            $compileResults.Add($result)
        }

        Add-StepResult `
            -Name 'azure-resources-bicep' `
            -Status $(if ($compileFailures -eq 0) { 'configured' } else { 'blocked' }) `
            -Detail $(if ($compileFailures -eq 0) { "Compiled $($bicepFiles.Count) Bicep template(s)." } else { "$compileFailures Bicep compilation command(s) failed." }) `
            -Data ([ordered]@{
                templates = @($bicepFiles | ForEach-Object { Get-RelativePath -Path $_.FullName })
                commands = @($compileResults)
                failures = $compileFailures
            })
    }
}

if (-not $RunAzureWhatIf) {
    Add-StepResult `
        -Name 'azure-resources-what-if' `
        -Status 'pending' `
        -Detail 'What-if preview was not requested. Use -RunAzureWhatIf with required Azure identifiers.' `
        -Data ([ordered]@{
            requiredParameters = @('ResourceGroup', 'ContainerRegistryName', 'EntraClientId', 'EntraTenantId', 'AllowedPrincipalObjectId', 'SourceCommitSha')
        })
}
else {
    $previewScript = Join-Path $PSScriptRoot 'Invoke-HeliosProvisionPreview.ps1'
    if (-not (Test-Path -LiteralPath $previewScript)) {
        Add-StepResult `
            -Name 'azure-resources-what-if' `
            -Status 'blocked' `
            -Detail 'Invoke-HeliosProvisionPreview.ps1 was not found.' `
            -Data $null
    }
    else {
        $missingParameters = [System.Collections.Generic.List[string]]::new()
        $resolvedSourceCommitSha = Resolve-SourceCommitSha
        if ([string]::IsNullOrWhiteSpace($ResourceGroup)) { $missingParameters.Add('ResourceGroup') }
        if ([string]::IsNullOrWhiteSpace($ContainerRegistryName)) { $missingParameters.Add('ContainerRegistryName') }
        if ([string]::IsNullOrWhiteSpace($EntraClientId)) { $missingParameters.Add('EntraClientId') }
        if ([string]::IsNullOrWhiteSpace($EntraTenantId)) { $missingParameters.Add('EntraTenantId') }
        if ([string]::IsNullOrWhiteSpace($AllowedPrincipalObjectId)) { $missingParameters.Add('AllowedPrincipalObjectId') }
        if ([string]::IsNullOrWhiteSpace($resolvedSourceCommitSha)) { $missingParameters.Add('SourceCommitSha') }

        if ($missingParameters.Count -gt 0) {
            Add-StepResult `
                -Name 'azure-resources-what-if' `
                -Status 'blocked' `
                -Detail 'Required Azure what-if parameters are missing.' `
                -Data ([ordered]@{
                    missing = @($missingParameters)
                    sourceCommitShaResolution = 'Provide -SourceCommitSha explicitly, set GITHUB_SHA, or run inside a git checkout.'
                })
        }
        else {
            $previewParams = @{
                ResourceGroup = $ResourceGroup
                EnvironmentName = $EnvironmentName
                ContainerRegistryName = $ContainerRegistryName
                EntraClientId = $EntraClientId
                EntraTenantId = $EntraTenantId
                AllowedPrincipalObjectId = $AllowedPrincipalObjectId
                SourceCommitSha = $resolvedSourceCommitSha
            }
            if (-not [string]::IsNullOrWhiteSpace($ContainerImage)) {
                $previewParams.ContainerImage = $ContainerImage
            }

            $previewExitCode = 0
            $previewLines = @()
            try {
                $previewOutput = & $previewScript @previewParams 2>&1
                $previewExitCode = $LASTEXITCODE
                $previewLines = @($previewOutput | ForEach-Object { "$_".Trim() } | Where-Object { $_ } | Select-Object -First 15)
            }
            catch {
                $previewExitCode = 1
                $previewLines = @((@($_.Exception.Message, $_.InvocationInfo.PositionMessage) | Where-Object { $_ } | ForEach-Object { "$_".Trim() }) | Select-Object -First 15)
            }

            Add-StepResult `
                -Name 'azure-resources-what-if' `
                -Status $(if ($previewExitCode -eq 0) { 'configured' } else { 'blocked' }) `
                -Detail $(if ($previewExitCode -eq 0) { 'Azure what-if preview completed.' } else { "Azure what-if preview failed with exit code $previewExitCode." }) `
                -Data ([ordered]@{
                    command = 'Invoke-HeliosProvisionPreview.ps1'
                    exitCode = $previewExitCode
                    sourceCommitSha = $resolvedSourceCommitSha
                    output = @($previewLines)
                })
        }
    }
}

$summary = [ordered]@{
    total = $steps.Count
    configured = @($steps | Where-Object { $_.status -eq 'configured' }).Count
    pending = @($steps | Where-Object { $_.status -eq 'pending' }).Count
    blocked = @($steps | Where-Object { $_.status -eq 'blocked' }).Count
    failed = @($steps | Where-Object { $_.status -eq 'failed' }).Count
}
$summary.ready = ($summary.blocked -eq 0 -and $summary.failed -eq 0)

$report = [ordered]@{
    schemaVersion = 1
    generatedAt = [DateTimeOffset]::UtcNow.ToString('o')
    mode = $Mode
    repository = 'M0nado/helios-platform'
    scope = 'monado/helios-control + repo integration surfaces'
    actions = [ordered]@{
        createLocalEnv = $effectiveCreateLocalEnv
        runRestore = $effectiveRunRestore
        runTests = $effectiveRunTests
        buildBicep = $effectiveBuildBicep
        runAzureWhatIf = $RunAzureWhatIf.IsPresent
        includeNetworkTools = $IncludeNetworkTools.IsPresent
        checkAuthentication = $CheckAuthentication.IsPresent
    }
    steps = @($steps)
    summary = $summary
}

if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
    $resolvedReportPath = if ([IO.Path]::IsPathRooted($ReportPath)) { $ReportPath } else { Join-Path $repoRoot $ReportPath }
    $reportDirectory = Split-Path -Parent $resolvedReportPath
    if (-not [string]::IsNullOrWhiteSpace($reportDirectory)) {
        [void] (New-Item -ItemType Directory -Path $reportDirectory -Force)
    }
    $report | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath $resolvedReportPath -Encoding utf8
}

$report | ConvertTo-Json -Depth 14

if ($summary.failed -gt 0) { exit 2 }
if ($Mode -eq 'Apply' -and $summary.blocked -gt 0) { exit 2 }

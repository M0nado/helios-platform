# HELIOS / MonadoBlade Copilot Instructions

## Product identity and authority

HELIOS / MonadoBlade is a C#-first secure Windows development and control platform. `M0nado/helios-platform` is the canonical product and execution source of truth. `Heli0s-Dynamics/adaptive-multibrain-bootstrap` is the shared policy, integration-contract, GitHub/Azure bootstrap, and Hermes/XCore evaluation control plane.

PowerShell is an adapter surface; durable system configuration belongs in typed services and JSON/YAML contracts.

Before cross-repository or cross-system work, read:

- `AGENTS.md`
- `config/integrations/repositories.json`
- `config/integrations/event-contract.schema.json`
- `docs/architecture/UNIFIED_AGENT_COMMUNICATION.md`

For control-plane changes, read first:

- `monado/helios-control/docs/ARCHITECTURE.md`
- `monado/helios-control/docs/IMPLEMENTATION_STATUS.md`
- `monado/helios-control/CLAUDE.md`

## Session setup order (optimal flow)

1. **Authority-first intake:** load the docs above before editing.
2. **Recency triage:** inspect latest issue/PR/branch/commit activity.
3. **Workstream selection:** pick the newest active stream unless blocked.
4. **Polyglot lane mapping:** identify required C#/F#/C++/Python/plugin checks.
5. **Implementation + validation:** run only the smallest relevant lanes.
6. **Merge-readiness prep:** ensure issue linkage, CI lanes, evidence, and approvals.

### Recency triage commands

```powershell
gh issue list --repo M0nado/helios-platform --state open --limit 30 --json number,title,updatedAt,url
gh pr list --repo M0nado/helios-platform --state all --limit 40 --json number,title,state,isDraft,headRefName,baseRefName,updatedAt,url
git --no-pager branch --all --sort=-committerdate
git --no-pager log --date=iso --decorate --pretty=format:"%h|%ad|%d|%s" -n 50
```

## Build, test, and lint command matrix

Use lane-targeted commands from active workflows.

### Core .NET lanes (platform and contracts)

```powershell
dotnet restore HELIOS.Platform.slnx
dotnet build HELIOS.Platform.slnx --configuration Release

dotnet test tests/contracts/HELIOS.Platform.Contracts.Tests/HELIOS.Platform.Contracts.Tests.csproj --configuration Release
dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj --configuration Release
dotnet test tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj --configuration Release --filter "FullyQualifiedName!~Integration&FullyQualifiedName!~Performance&FullyQualifiedName!~EndToEnd&FullyQualifiedName!~Security"
```

Single-test/class example:

```powershell
dotnet test tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj --configuration Release --filter "FullyQualifiedName~Phase10.Quarantine.QuarantineSystemTests"
```

### Helios control-plane lane (`monado/helios-control`)

```powershell
dotnet restore monado/helios-control/Helios.Connect.sln
dotnet test monado/helios-control/Helios.Connect.sln --configuration Release --no-restore
dotnet test monado/helios-control/tests/Helios.Connect.Tests/Helios.Connect.Tests.csproj --configuration Release --filter "FullyQualifiedName~WebhookTests"
```

### Plugin lane (`plugins/helios-control-fabric`)

```powershell
python -m unittest discover -s plugins/helios-control-fabric/scripts -p "test_*.py" -v
python plugins/helios-control-fabric/scripts/helios.py doctor --json
python plugins/helios-control-fabric/scripts/helios.py plan --environment azure-dev --json
python plugins/helios-control-fabric/scripts/helios.py oidc --environment azure-dev --json
python plugins/helios-control-fabric/scripts/helios.py edge --environment azure-dev --json
```

### Native C++ lane

```powershell
cmake -S src/native/HELIOS.Native.Performance -B build/native -DBUILD_TESTING=ON -DCMAKE_BUILD_TYPE=Release
cmake --build build/native --config Release --parallel 2
ctest --test-dir build/native --build-config Release --output-on-failure
```

### Python deployment-agent lane

```powershell
python -m pip install --disable-pip-version-check uv==0.9.25
uv sync --frozen --no-dev --project services/helios-deployment-agent
python -m unittest discover -s services/helios-deployment-agent/tests -v
python services/helios-deployment-agent/evals/run_local.py
```

### Node MCP lane (`plugins/openai/helios-mcp`)

```powershell
cd plugins/openai/helios-mcp
npm install
npm run check
```

### Lint and schema checks

```powershell
pwsh -Command "Install-Module -Name PSScriptAnalyzer -Force -Scope CurrentUser"
pwsh -Command "Invoke-ScriptAnalyzer -Path './src','./scripts' -Recurse -Severity Error,Warning"

python -m json.tool config/integrations/event-contract.schema.json > $null
python -m json.tool config/integrations/repositories.json > $null
```

## Architecture map (big picture)

- **Core platform modules:** `src/core/HELIOS.Platform`, `src/core/HELIOS.Platform.Contracts`, `src/core/HELIOS.Platform.Minimal`.
- **Typed local read-only integration core:** `src/Helios.Connect` (zero-remote-mutation CLI/core patterns).
- **Cloud-first control-plane runtime:** `monado/helios-control/src/Helios.Connect.Api` + `monado/helios-control/src/Helios.Connect.Contracts`.
- **Operator plugin surface:** `plugins/helios-control-fabric` (plan-first, deterministic, read-only defaults).
- **OpenAI MCP bridge surface:** `plugins/openai/helios-mcp` (bounded tool proxy to broker).
- **Policy-bounded deployment agent:** `services/helios-deployment-agent` (plan-only, approval-aware).
- **Native/analytics support lanes:** `src/native`, `tests/analytics`.

### Hermes/XCore and fleet context

- Fleet/runtime contracts: `monado/helios-control/config/agent-fleet.json`, `config/hermes-fleet.example.json`.
- Evaluator boundary: `monado/helios-control/.github/agents/xcore-evaluator.agent.md`.
- Learning-plane design (planned, gated): `monado/helios-control/docs/HERMES_MICROSOFT_LEARNING.md`.
- Local fleet launcher: `monado/helios-control/scripts/Start-HeliosLocalFleet.ps1`.
- Performance evidence surfaces: `docs/guides/PERFORMANCE_BENCHMARK.md`, `tests/PERFORMANCE_BENCHMARK.md`, `src/core/HELIOS.Platform/Core/Performance/PerformanceBenchmarkService.cs`.

## Repository-specific conventions (required)

1. Use a scoped issue, feature branch, and PR for substantial work.
2. Keep changes in the owning repository; no competing platform roots.
3. Treat Hermes/XCore learning as candidate-only until explicitly approved promotion paths exist.
4. Fail closed by default: if auth/policy/scope is missing, stop instead of guessing.
5. Keep remote mutation actions behind explicit approvals (deploy/RBAC/consent/merge/publication).
6. Never commit secrets, tokens, recovery keys, private endpoints, or raw tenant credentials.
7. Prefer workload identity, managed identity, and Key Vault references over static secrets.
8. Keep generated logs/caches/model artifacts/local databases out of Git.
9. Preserve event correlation IDs and evidence links across GitHub/Azure/Microsoft 365/HELIOS systems.
10. Use normalized event envelope fields from `config/integrations/event-contract.schema.json`.
11. Default destructive Windows automation to dry-run/`-WhatIf` and document rollback behavior.
12. Microsoft Copilot/Copilot Studio actions must traverse approved broker APIs and cannot bypass GitHub/Azure approval gates.

## Merge and repo-sync readiness playbook

Before proposing merge/repo sync:

1. Confirm newest active issue/PR stream and branch head recency.
2. Map `issue -> branch -> PR -> required CI lanes`.
3. Run targeted lane commands for touched surfaces (do not default to unrelated full-suite runs).
4. Confirm guardrails: no auto-merge, no implicit deploys, no implicit RBAC/consent/publication.
5. Link evidence in GitHub; sync status to Slack/Linear/Teams/SharePoint through approved governed paths only.

Useful GitHub commands:

```powershell
gh pr status
gh pr view <number> --json number,title,state,isDraft,headRefName,baseRefName,mergeStateStatus,url
gh issue view <number> --json number,title,state,assignees,labels,url
```

## Collaboration system boundaries

- GitHub: code, architecture, issues, PRs, Actions, releases, evidence.
- Azure: workload identity, integration broker, Service Bus/Event Grid, Key Vault, Foundry, telemetry, deployment.
- Copilot/Codex/Claude/ChatGPT: bounded implementation and review under repository guardrails.
- Hermes/XCore: routing, evaluation, pruning, regression checks, reviewable learning state.
- Slack/Linear/Teams/SharePoint/Fabric/Power Platform: governed operations and evidence surfaces, not bypass channels.

## Completion criteria

A change is complete only when:

- implementation is scoped and documented;
- the owning repository is correct;
- affected projects build;
- targeted tests and integration-contract validations pass;
- security and rollback effects are documented;
- CI status is green for impacted lanes;
- operational documentation is updated when required;
- cross-system events preserve correlation IDs and evidence links.

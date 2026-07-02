## HELIOS Control Plane Update

### Command Center

Run locally:

```bash
./helios.sh all
./helios.sh dashboard
```

### Readiness

# HELIOS Readiness Score

Generated: `2026-07-02T19:17:30.260620+00:00`

Score: **82%**

| Check | Target | Status |
| --- | --- | --- |
| command-center | `helios.sh` | ✅ |
| execution-order | `config/execution-order.json` | ✅ |
| secrets-map | `config/secrets-map.example.json` | ✅ |
| cross-access-profiles | `config/cross-access-profiles.example.json` | ✅ |
| github-inventory | `scripts/github/github-inventory.py` | ✅ |
| azure-inventory | `scripts/azure/azure-inventory.py` | ✅ |
| repo-inventory | `scripts/analysis/repo_inventory.py` | ✅ |
| hybrid-gap-analysis | `scripts/analysis/hybrid_gap_analysis.py` | ✅ |
| gui-dashboard | `scripts/dashboard/generate-gui.py` | ✅ |
| web-server | `scripts/web/helios-web.py` | ✅ |
| pr-update-helper | `scripts/github/update-pr-from-reports.py` | ✅ |
| python3-cli | `python3` | ✅ |
| git-cli | `git` | ✅ |
| github-cli | `gh` | ⚠️ |
| azure-cli | `az` | ⚠️ |
| dotnet-cli | `dotnet` | ⚠️ |
| cmake-cli | `cmake` | ✅ |


### Cross-Access Profiles

# Cross-Access Profiles

Generated: `2026-07-02T19:17:29.579703+00:00`

| Profile | Provider | Scope | CLI | Auth | Apply mode |
| --- | --- | --- | --- | --- | --- |
| GitHub User | github | user | ⚠️ | ⚠️ | disabled |
| GitHub Organization | github | org | ⚠️ | ⚠️ | disabled |
| GitHub Enterprise | github | enterprise | ⚠️ | ⚠️ | disabled |
| Azure Subscription | azure | subscription | ⚠️ | ⚠️ | disabled |
| Microsoft Entra ID | microsoft-365 | tenant | ⚠️ | ⚠️ | disabled |
| Microsoft Purview | microsoft-365 | tenant | ⚠️ | ⚠️ | disabled |
| Microsoft 365 Copilot | microsoft-365 | tenant | ⚠️ | ⚠️ | disabled |
| OpenAI / ChatGPT APIs | openai | api | ✅ | ⚠️ | disabled |
| Azure OpenAI | azure-openai | api | ⚠️ | ⚠️ | disabled |
| Anthropic Claude | anthropic | api | ✅ | ⚠️ | disabled |
| Codex | openai | developer-agent | ⚠️ | ⚠️ | disabled |
| Visual Studio / MAUI | dotnet | developer-workstation | ⚠️ | ⚠️ | not-applicable |


### Branch Recommendations

# Branch Merge / Prune Recommendations

No branches are merged or deleted by this report.

| Branch | Score | CI | Files | Recommendation |
| --- | ---: | ---: | ---: | --- |
| `work` | 56 | 0 | 63 | extract-ideas-only |


### Hybrid Gap Analysis

# Hybrid Integration Gap Analysis

Generated: `2026-07-02T19:17:28.075910+00:00`

| Domain | Priority | Readiness | Next |
| --- | ---: | ---: | --- |
| command-center | 100 | 100 | Keep one entry point for every local, Cloud Shell, Codespaces, and CI action. |
| github-control | 95 | 100 | Add drift reports before any apply-mode support. |
| azure-hybrid | 95 | 100 | Add private endpoint and self-hosted runner modules after what-if validation. |
| ai-orchestration | 90 | 100 | Unify ChatGPT/OpenAI, Azure OpenAI, Claude, and Codex routing behind safe provider manifests. |
| security-compliance | 90 | 100 | Add secret scanning, policy checks, and compliance report generation to CI. |
| dotnet-visual-studio-maui | 88 | 100 | Add solution-level build targets for GUI, MAUI-ready front ends, contracts, and analytics. |
| fsharp-analytics | 86 | 100 | Connect analytics scoring into branch intelligence and build graph scoring. |
| native-xcore | 84 | 100 | Add benchmark harness and C ABI boundaries for C#/F#/Python callers. |
| python-automation | 82 | 100 | Normalize all Python entry points into build graph nodes and command-center actions. |
| docs-dashboard | 78 | 100 | Keep generated status pages separate from durable architecture docs. |


### Safety

- Secret values are never printed.
- GitHub org/enterprise, Azure subscription/tenant, Entra, Purview, and provider apply modes remain disabled unless explicitly implemented later behind `--apply` and reviewed scopes.

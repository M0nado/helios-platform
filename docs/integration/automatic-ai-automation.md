# HELIOS Automatic AI and Automation Integration

This guide wires together the repository's AI coordination scripts, Azure CLI setup, branch discovery, HELIOS Control profile, and Hermes Fleet Production profile through `scripts/automation/helios_auto_integration.py`.

## What the integrator covers

- Discovers local and remote Git branches and can produce a safe no-commit merge plan.
- Scans the repository for C#, C++, F#, Python, and XAML surfaces so front-end, backend, analytics, and AIHub gaps are visible in one report.
- Checks HELIOS Control readiness for WinUI 3/WPF UI, C# orchestration, security validation, AIHub routing, and Azure setup.
- Checks Hermes Fleet Production readiness for performance backend, F# analytics, Python automation, monitoring, and orchestration assets.
- Checks Azure CLI availability and plans required extensions for control-plane and fleet-production automation.
- Checks AIHub configuration files and required OpenAI/Azure OpenAI environment variables without exposing secrets.

## Safe dry-run usage

Run from the repository root:

```bash
python3 scripts/automation/helios_auto_integration.py --merge-all --output artifacts/helios-auto-integration-report.json
```

Dry-run mode is the default. It prints JSON and writes the optional report file without merging branches, installing Azure extensions, or changing Azure account state.

## Live setup usage

After reviewing the dry-run report, set credentials and run execute mode only when you are ready for mutations:

```bash
export AZURE_SUBSCRIPTION_ID="<subscription-id>"
export AZURE_TENANT_ID="<tenant-id>"
export OPENAI_API_KEY="<openai-key>"
python3 scripts/automation/helios_auto_integration.py --fetch --merge-all --execute --output artifacts/helios-auto-integration-report.json
```

Execute mode performs only supported actions: `git fetch --all --prune`, no-commit branch merges, Azure extension installation or upgrade, and Azure subscription selection. Merge conflicts stop the run and leave Git in its standard conflict state for manual resolution.

## Profile configuration

Profile and language-role settings live in `config/automation/ai-automation-profiles.json`. Update that file when adding dedicated `helios-control` or `hermes-fleet-production` source roots, C++ backend projects, F# analytics projects, or Python AIHub modules.

## Azure CLI setup checklist

1. Install Azure CLI from the official Microsoft package for the host operating system.
2. Run `az login --tenant "$AZURE_TENANT_ID"` or use a managed identity/service principal in CI.
3. Export `AZURE_SUBSCRIPTION_ID` so the integrator can run `az account set` in execute mode.
4. Run dry-run mode first and confirm the extension plan.
5. Run execute mode to install or upgrade the planned Azure extensions.

## AIHub setup checklist

1. Copy `scripts/ai-integration/config/.env.template` to a private `.env` file or provide environment variables through CI secrets.
2. Set either OpenAI variables or Azure OpenAI variables.
3. Run the integrator and verify `ai_hub.apiKeyEnvironmentVariables` reports at least one configured provider.
4. Use the existing AI coordination scripts for conflict detection, approval, audit tracking, and rollback after automated recommendations are generated.

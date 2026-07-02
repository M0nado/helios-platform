# HELIOS Hybrid Integration Expansion Plan

This plan pulls the repo's existing C#, F#, C++, Python, Azure, GitHub, AI, dashboard, installer, security, monitoring, and documentation assets into one integration roadmap.

## Highest-priority upgrades

1. Keep `helios.sh` as the only required local entry point.
2. Treat `config/secrets-map.example.json` as the source of truth for GitHub, Codespaces, Azure Key Vault, OpenAI, Azure OpenAI, Claude, Slack, Microsoft 365, Hermes, and XCore connection names.
3. Use `reports/project-inventory/repo-inventory.md` to keep every module visible before pruning or merging branches.
4. Use `reports/project-inventory/hybrid-gap-analysis.md` to rank what should be merged, rewritten, archived, or promoted.
5. Keep GitHub/Azure apply-mode disabled until drift reports and permission tiers are reviewed.

## Integration lanes

| Lane | Current assets | Next upgrade |
| --- | --- | --- |
| C# / GUI / Visual Studio / MAUI | `*.csproj`, `HELIOS.Platform.slnx`, `docs/integration/VISUAL_STUDIO_MAUI_SETUP.md` | Add solution filters and MAUI-ready shell project once UI target is chosen. |
| F# analytics | `src/analytics/HELIOS.Analytics.FSharp` | Use analytics scoring in branch intelligence and gap analysis. |
| C++ / XCore | `src/native/HELIOS.Native.Performance` | Add native benchmark and interop ABI plan. |
| Python automation | `scripts/analysis`, `scripts/control`, `scripts/github`, `scripts/azure` | Normalize all safe actions into `helios.sh` and build graph nodes. |
| GitHub control | `.github/workflows`, `scripts/github` | Add drift report before any settings apply mode. |
| Azure hybrid | `infra/azure`, `scripts/azure` | Add private endpoints, app hosting, and self-hosted runner modules after what-if validation. |
| AI providers | `scripts/ai`, `scripts/ai-services`, `ai-integration` | Add provider router for OpenAI, Azure OpenAI, Claude, and Codex in opt-in mode only. |
| Security/compliance | `docs/security`, `scripts/security`, `installer/security` | Add CI policy checks and generated compliance summaries. |
| Dashboards/docs | `status-site`, `reports`, `docs` | Separate durable docs from generated reports and publish both. |

## Optimization rules

- Keep generated outputs under `reports/` and `status-site/`.
- Keep durable architecture and setup guidance under `docs/`.
- Keep credentials out of repo; only names and readiness booleans are allowed.
- Prefer read-only inventory and what-if validation before any apply mode.
- Prefer module-level tasks over broad, cross-cutting edits.

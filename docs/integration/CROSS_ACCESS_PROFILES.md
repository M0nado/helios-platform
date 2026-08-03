# HELIOS Cross-Access Profiles

Cross-access profiles describe every external control surface HELIOS can work with safely from one GUI or command center.

## Covered profiles

- GitHub user, organization, and enterprise scopes
- Azure subscription scope
- Microsoft Entra ID tenant scope
- Microsoft Purview tenant scope
- Microsoft 365 Copilot tenant readiness
- OpenAI / ChatGPT APIs
- Azure OpenAI
- Anthropic Claude
- Codex developer agent readiness
- Visual Studio / .NET MAUI developer workstation readiness

## Safety model

- Profiles are inventory/readiness only by default.
- Secret values are never printed.
- Org, enterprise, tenant, and subscription apply-mode controls remain disabled.
- Use `reports/integrations/cross-access-profiles.md` to see readiness.
- Use `status-site/index.html` for the local GUI.

## Commands

```bash
python3 scripts/integrations/cross_access_profiles.py
python3 scripts/dashboard/generate-gui.py
./helios.sh gui
./helios.sh profiles
./helios.sh dashboard
```

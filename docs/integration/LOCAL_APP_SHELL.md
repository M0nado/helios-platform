# HELIOS Local App Shell

The local app shell gives you one easy entry point for the HELIOS GUI plus the web apps that usually hold authenticated context: Microsoft 365 Copilot, ChatGPT, Claude, GitHub, Azure Cloud Shell, Slack, and Linear.

## Commands

Linux/macOS/WSL:

```bash
./helios.sh local-app
```

Windows PowerShell:

```powershell
./scripts/local-app/helios-local-app.ps1
```

Serve the HELIOS dashboard first when you want the local GUI live:

```bash
./helios.sh dashboard
```

## What gets opened

- HELIOS GUI: `http://127.0.0.1:8787/`
- Microsoft 365 Copilot: `https://copilot.microsoft.com` or `COPILOT_WEB_URL`
- ChatGPT: `https://chatgpt.com`
- Claude: `https://claude.ai`
- GitHub: `https://github.com`
- Azure Cloud Shell: `https://shell.azure.com`
- Slack: `https://app.slack.com`
- Linear: `https://linear.app`

## Auth and data safety

- The shell uses official browser sign-in and official CLIs; it does not read cookies, saved passwords, authenticator app secrets, or token stores.
- Local browser/app scratch data goes under `.helios/browser-profile/` by default.
- Manual exports or temporary app downloads belong under `.helios/private/`.
- `.helios/` is ignored by git so local data is not committed.

## Internet Explorer / legacy app mode

Internet Explorer is retired. This uses Microsoft Edge/Chrome app-mode windows when available. If a legacy Microsoft 365 or intranet flow needs IE compatibility, configure Microsoft Edge IE Mode by device or enterprise policy outside the repository, then keep HELIOS automation in readiness/reporting mode.

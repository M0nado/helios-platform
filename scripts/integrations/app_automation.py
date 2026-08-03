#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, os, shutil, subprocess, webbrowser
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description='Generate safe app automation readiness for HELIOS apps.')
parser.add_argument('--open', action='store_true', help='Open app URLs in the default browser after writing the report.')
args = parser.parse_args()
OUT_JSON = ROOT / 'reports' / 'integrations' / 'app-automation-readiness.json'
OUT_MD = ROOT / 'reports' / 'integrations' / 'app-automation-readiness.md'

def have(cmd: str) -> bool:
    return shutil.which(cmd) is not None

def run(cmd: list[str]) -> tuple[bool, str]:
    try:
        p = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True, timeout=15)
        return p.returncode == 0, (p.stdout or p.stderr).strip().splitlines()[0:3]
    except Exception as exc:
        return False, [str(exc)]

def env(names: list[str]) -> dict[str, bool]:
    return {name: bool(os.environ.get(name)) for name in names}

def profile(name, kind, envs, url=None, cli=None, auth_cmd=None, setup=None):
    available = have(cli[0]) if cli else True
    authenticated = False
    detail = []
    if auth_cmd and have(auth_cmd[0]):
        authenticated, detail = run(auth_cmd)
    elif envs:
        authenticated = any(os.environ.get(n) for n in envs)
    return {
        'name': name,
        'kind': kind,
        'url': url,
        'cli': cli,
        'cliAvailable': available,
        'authenticatedOrTokenPresent': authenticated,
        'env': env(envs),
        'setupCommands': setup or [],
        'authDetail': detail,
    }

apps = [
    profile('GitHub CLI / Actions token','source-control',['GITHUB_TOKEN'], 'https://github.com', ['gh'], ['gh','auth','status'], ['gh auth login', 'gh workflow run helios-control-plane.yml']),
    profile('GitHub MCP readiness','mcp',['GITHUB_TOKEN','GITHUB_MCP_SERVER_URL'], 'https://github.com', None, None, ['export GITHUB_TOKEN=...', 'Configure MCP host with GitHub server outside git; keep tokens in environment or secret store.']),
    profile('Azure CLI / Cloud Shell','cloud',['AZURE_CLIENT_ID','AZURE_TENANT_ID','AZURE_SUBSCRIPTION_ID'], 'https://shell.azure.com', ['az'], ['az','account','show'], ['az login', 'az account set --subscription <subscription-id>']),
    profile('Microsoft 365 Copilot web','browser-ai',['M365_TENANT_ID','M365_CLIENT_ID','M365_COPILOT_PRO_ACCOUNT','COPILOT_WEB_URL'], os.environ.get('COPILOT_WEB_URL','https://copilot.microsoft.com'), None, None, ['Open Copilot in your already-authenticated browser profile. Browser cookies are never read or stored.']),
    profile('ChatGPT web and OpenAI API','browser-ai',['CHATGPT_WORKSPACE_ID','OPENAI_API_KEY','OPENAI_ORG_ID','OPENAI_PROJECT_ID'], 'https://chatgpt.com', None, None, ['Set OPENAI_API_KEY for API automation; use browser auth for ChatGPT UI.']),
    profile('Claude web and API','browser-ai',['ANTHROPIC_API_KEY'], 'https://claude.ai', None, None, ['Set ANTHROPIC_API_KEY for API automation; use browser auth for Claude UI.']),
    profile('Slack app integration','collaboration',['SLACK_WEBHOOK_URL','SLACK_BOT_TOKEN','SLACK_APP_TOKEN','SLACK_SIGNING_SECRET'], 'https://app.slack.com', None, None, ['Create a Slack app, store tokens in GitHub secrets/Azure Key Vault, then run readiness again.']),
    profile('Linear integration','planning',['LINEAR_API_KEY','LINEAR_TEAM_ID'], 'https://linear.app', None, None, ['Create a Linear API key, set LINEAR_API_KEY and LINEAR_TEAM_ID as secrets.']),
    profile('Local HELIOS GUI','local-gui',[], 'http://127.0.0.1:8787/', ['python3'], None, ['./helios.sh dashboard']),
]

report = {
    'safeByDefault': True,
    'usesExistingAuth': 'CLI/browser auth can be reused only through official CLIs/browser launch; the script never reads cookies, keychains, or token files.',
    'internetExplorer': 'Internet Explorer is retired; use Microsoft Edge. For legacy IE-mode sites, configure Edge IE Mode outside this repo by enterprise policy.',
    'apps': apps,
}
OUT_JSON.parent.mkdir(parents=True, exist_ok=True)
OUT_JSON.write_text(json.dumps(report, indent=2, sort_keys=True) + '\n')
lines = ['# App Automation Readiness', '', report['usesExistingAuth'], '', f"Internet Explorer note: {report['internetExplorer']}", '', '| App | Type | CLI | Auth/token | URL |', '|---|---|---:|---:|---|']
for app in apps:
    lines.append(f"| {app['name']} | {app['kind']} | {'yes' if app['cliAvailable'] else 'no'} | {'yes' if app['authenticatedOrTokenPresent'] else 'no'} | {app.get('url') or ''} |")
lines += ['', '## Safe setup order', '', '1. Run `./helios.sh setup`.', '2. Authenticate official CLIs: `gh auth login` and `az login`.', '3. Add provider secrets to GitHub Actions/Codespaces/Azure Key Vault; do not commit values.', '4. Run `./helios.sh apps` and `./helios.sh all`.', '5. Start the GUI with `./helios.sh dashboard`.']
OUT_MD.write_text('\n'.join(lines) + '\n')
if args.open:
    for app in apps:
        if app.get('url'):
            webbrowser.open(app['url'])
print(f'Wrote {OUT_JSON.relative_to(ROOT)} and {OUT_MD.relative_to(ROOT)}')

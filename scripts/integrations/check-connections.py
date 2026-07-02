#!/usr/bin/env python3
from __future__ import annotations
import json
import os
import shutil
import subprocess
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / "config" / "integrations.example.json"
SECRETS_MAP = ROOT / "config" / "secrets-map.example.json"
OUT = ROOT / "reports" / "integrations" / "connection-readiness.json"

def run(cmd):
    proc = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True)
    return proc.returncode, proc.stdout.strip(), proc.stderr.strip()

def tool(name, check_cmd):
    if shutil.which(check_cmd[0]) is None:
        return {"available": False, "authenticated": False, "detail": f"{check_cmd[0]} not found"}
    code, out, err = run(check_cmd)
    return {"available": True, "authenticated": code == 0, "detail": (out or err).splitlines()[:3]}

def env_status(names):
    return {name: bool(os.environ.get(name)) for name in names}

def path_status(paths):
    return {path: (ROOT / path).exists() for path in paths}

cfg = json.loads(CONFIG.read_text())
secrets_map = json.loads(SECRETS_MAP.read_text()) if SECRETS_MAP.exists() else {"secrets": []}
secret_env_names = [item["localEnv"] for item in secrets_map.get("secrets", [])]
report = {
    "github": tool("github", ["gh", "auth", "status"]),
    "azure": tool("azure", ["az", "account", "show"]),
    "dotnet": tool("dotnet", ["dotnet", "--version"]),
    "python": tool("python", ["python3", "--version"]),
    "openai": env_status(cfg["openai"]["env"]),
    "azureOpenAI": env_status(cfg["azureOpenAI"]["env"]),
    "slack": env_status(cfg["slack"]["env"]),
    "microsoft365Copilot": env_status(cfg["microsoft365Copilot"]["env"]),
    "claude": env_status(cfg.get("claude", {}).get("env", [])),
    "visualStudioMaui": cfg.get("visualStudioMaui", {}),
    "secretsMap": env_status(secret_env_names),
    "localWeb": cfg.get("localWeb", {}),
    "automationPaths": path_status([
        "scripts/web/helios-web.py",
        "scripts/azure/sync-keyvault-secrets.sh",
        "scripts/ai/enrich-ideas.py",
        "infra/azure/main.bicep",
        ".github/workflows/helios-control-plane.yml",
        "config/execution-order.json",
        "docs/security/CONTROL_PLANE_PERMISSIONS.md"
    ]),
    "notes": "No secret values are printed; booleans only. Prefer GitHub secrets or Azure Key Vault."
}
OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n")
print(f"Wrote {OUT.relative_to(ROOT)}")

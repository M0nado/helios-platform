---
name: azure-toolchain-setup
description: Detect, install, update, verify, and inventory the approved HELIOS Azure and Microsoft Copilot development tools on Windows.
---

# Azure and Microsoft Toolchain Setup

## Purpose

Make the local HELIOS development machine and self-hosted runner reproducible without mixing tool installation, account login, subscription selection, and production deployment into one unsafe script.

The authoritative manifest is `config/toolchains/azure-microsoft-copilot-toolchain.v1.json`.

## Phases

### 1. Preflight

- verify Windows architecture and version;
- repair current-process System32 and approved tool paths before detection;
- detect administrator state without auto-elevating;
- verify WinGet, PowerShell 7, TLS/proxy connectivity, free disk space, and reboot state;
- write a preflight report.

### 2. Plan

- compare installed tools and versions with the manifest;
- resolve official package sources;
- classify each action as user-scope, machine-scope, extension install, or major upgrade;
- require approval for elevation, extensions, and major upgrades;
- never log credentials or token-cache contents.

### 3. Approved installation

Install or update only approved entries:

- Azure CLI;
- Azure Developer CLI (`azd`);
- Bicep CLI;
- Azure Functions Core Tools;
- Microsoft Power Platform CLI;
- Microsoft 365 Agents Toolkit;
- GitHub CLI;
- PowerShell 7;
- .NET SDK;
- Docker Desktop;
- Node.js LTS;
- Visual Studio Code.

### 4. Verification

- close/reopen terminal boundary is recorded where required;
- resolve every executable from PATH;
- run version commands;
- build the enterprise Bicep entrypoint;
- build the typed enterprise-agent project;
- validate agent, subagent, toolchain, and connector JSON;
- verify Docker/WSL2/Hyper-V readiness for XCore;
- verify Copilot toolkit and `pac` readiness;
- inspect authentication status only after the user signs in.

### 5. Authentication handoff

Installation does not imply authorization.

- `az login`, `azd auth login`, `gh auth login`, Power Platform authentication, Microsoft 365 authentication, and OpenAI key creation remain interactive owner actions;
- Azure subscription selection must be explicit;
- tenant IDs, subscription IDs, tokens, and API keys are never fabricated;
- production workflows use OIDC or managed identity rather than long-lived client secrets.

## Output

Generate:

- preflight report;
- installation plan;
- installed-version inventory;
- PATH health report;
- authentication-readiness report with identifiers redacted where appropriate;
- failed-tool remediation steps;
- reboot requirement;
- immutable evidence hash.

## Prohibited

- arbitrary package repositories;
- disabling Defender, execution policy, WDAC, firewall, or security mitigations to make installation pass;
- silent administrator elevation;
- automatic account login or subscription selection;
- production deployment from the setup script;
- deleting official token caches without a separate repair request;
- claiming success when a command is unresolved or unauthenticated.

# HELIOS Installer – Phase Reference Guide

This document describes every installation phase, explains what it does, and shows how to customize, skip, or extend each phase.

---

## Table of Contents

1. [Phase Overview](#phase-overview)
2. [Phase 1 – GUI Foundation](#phase-1--gui-foundation)
3. [Phase 2 – Application Installation](#phase-2--application-installation)
4. [Phase 3 – Security Hardening](#phase-3--security-hardening)
5. [Phase 4 – User Account Setup](#phase-4--user-account-setup)
6. [Phase 5 – Data & Storage Configuration](#phase-5--data--storage-configuration)
7. [Phase 6 – Cloud & AI Integration](#phase-6--cloud--ai-integration)
8. [Phase 7 – Hardware & OS Foundation](#phase-7--hardware--os-foundation)
9. [Adding Custom Phases](#adding-custom-phases)
10. [Phase Dependencies](#phase-dependencies)
11. [Error Handling Per Phase](#error-handling-per-phase)

---

## Phase Overview

| # | Name | Approx. Time | Can Skip? | Requires |
|---|------|-------------|-----------|----------|
| 1 | GUI Foundation | 5 min | Yes | – |
| 2 | Application Installation | 20 min | Yes | – |
| 3 | Security Hardening | 15 min | Yes | – |
| 4 | User Account Setup | 5 min | Yes | – |
| 5 | Data & Storage | 10 min | Yes | Phase 7 |
| 6 | Cloud & AI Integration | 15 min | Yes | Phase 2 |
| 7 | Hardware & OS Foundation | 10 min | No | – |

Phase 7 always runs first internally (even if listed last) to ensure the OS foundation is ready.

---

## Phase 1 – GUI Foundation

### What it does

- Installs the Xenoblade-inspired HELIOS UI framework
- Applies the hexagonal hub desktop theme
- Configures display scaling and colour profiles
- Sets taskbar layout and pinned application shortcuts
- Installs custom fonts and icon packs

### How to skip

```json
{ "phases": { "skip": [1] } }
```

> Skipping Phase 1 leaves the default Windows desktop intact.

### How to modify

```json
{
  "layers": {
    "layer1_gui": {
      "enabled": true,
      "theme": "xenoblade_dark",
      "scaling": 125,
      "taskbar_position": "bottom"
    }
  }
}
```

### How to add a task

```json
{
  "phases": {
    "custom": [
      {
        "id": 1,
        "inject_task": {
          "name": "Apply company wallpaper",
          "type": "powershell",
          "script": "hooks/set_wallpaper.ps1",
          "order": "after_all"
        }
      }
    ]
  }
}
```

---

## Phase 2 – Application Installation

### What it does

- Installs all programs defined in the Layer 2 application manifest
- Handles silent installs for all 50 built-in applications
- Configures application default settings
- Associates file extensions
- Applies per-application licence keys (if configured)

### How to skip

```json
{ "phases": { "skip": [2] } }
```

### How to control which apps are installed

```json
{
  "layers": {
    "layer2_apps": {
      "enabled": true,
      "app_list": ["vscode", "git", "python", "docker", "notepadplusplus"],
      "skip_optional": true
    }
  }
}
```

### Built-in app categories

| Category | Examples |
|----------|---------|
| Core System | Windows Admin Center, HWiNFO, Process Lasso |
| Security | Windows Defender, Sentinel One, BitLocker, WireGuard |
| Performance | CCleaner, Autoruns, GPU-Z |
| Development | Node.js, Python, Docker, Git, VS Code |
| Creative | Reaper DAW, OBS Studio, FFmpeg, Blender, GIMP |
| DevOps | Jenkins, Terraform, Ansible |
| Utilities | 7-Zip, Notepad++, PuTTY, Wireshark |
| Communication | Discord, Steam, VLC |

---

## Phase 3 – Security Hardening

### What it does

- Configures Windows Defender (real-time, cloud, exclusions)
- Enables BitLocker on selected drives
- Applies Windows Firewall rules
- Configures AppLocker policies
- Sets audit logging policies
- Applies USB device restrictions
- Hardens local security policy (password complexity, lockout thresholds)

### How to skip

```json
{ "phases": { "skip": [3] } }
```

> ⚠  Skipping Phase 3 leaves the system with default Windows security settings.

### How to modify individual security components

See [CUSTOMIZATION.md – Security Customization](CUSTOMIZATION.md#security-customization) for the full JSON reference.

### Phase 3 task order

1. Backup current security settings (used for rollback)
2. Configure Windows Defender
3. Configure Firewall
4. Configure AppLocker
5. Enable BitLocker (triggers key generation)
6. Apply audit logging policies
7. Apply USB restrictions
8. Apply local security policy hardening
9. Verify all settings were applied

### Adding a post-Phase 3 validation hook

```json
{
  "hooks": {
    "post_phase": {
      "3": "hooks/verify_security.ps1"
    }
  }
}
```

---

## Phase 4 – User Account Setup

### What it does

- Creates all user accounts defined in the `users` array
- Assigns group memberships
- Applies password policies
- Configures user profiles and home directories
- Sets per-user registry settings
- Optionally joins domain (enterprise setups)

### How to skip

```json
{ "phases": { "skip": [4] } }
```

> Skipping Phase 4 means only the built-in Windows accounts will exist.

### How to modify

See [CUSTOMIZATION.md – User Account Customization](CUSTOMIZATION.md#user-account-customization).

### Domain join (enterprise)

```json
{
  "users": [],
  "domain": {
    "enabled": true,
    "name": "CORP",
    "fqdn": "corp.example.com",
    "ou": "OU=Workstations,DC=corp,DC=example,DC=com",
    "credential_hook": "hooks/get_domain_creds.ps1"
  }
}
```

---

## Phase 5 – Data & Storage Configuration

### What it does

- Creates and formats all enabled partitions
- Creates and mounts VHDX virtual disks
- Sets volume labels and drive letters
- Configures BitLocker on individual drives (if enabled in Phase 3)
- Creates standard directory structure (`Documents`, `Downloads`, `Backups`, etc.)
- Configures VSS (Volume Shadow Copy) for backup drives

### How to skip

```json
{ "phases": { "skip": [5] } }
```

> Skipping Phase 5 leaves existing partition layout unchanged.

### How to modify

See [CUSTOMIZATION.md – Partition Customization](CUSTOMIZATION.md#partition-customization).

### Phase 5 dependencies

- Requires Phase 7 to have completed (OS foundation must be stable)
- If BitLocker is enabled, Phase 3 must have run first

---

## Phase 6 – Cloud & AI Integration

### What it does

- Installs and configures the NEXUS cloud orchestration agent
- Sets up connections to configured AI services (GitHub Copilot, ChatGPT, Azure OpenAI, etc.)
- Deploys the 25+ cloud AI agent connectors
- Configures the closed tunnel (8-layer security)
- Sets up Office 365 integration
- Initializes the Monado optimization engine's cloud feedback loop

### How to skip

```json
{ "phases": { "skip": [6] } }
```

### How to disable specific cloud services

```json
{
  "layers": {
    "layer6_cloud": {
      "enabled": true,
      "services": {
        "github_copilot": true,
        "chatgpt": true,
        "azure_openai": false,
        "office365": true,
        "nexus_agents": true
      },
      "tunnel": {
        "enabled": true,
        "security_layers": 8
      }
    }
  }
}
```

### Phase 6 dependencies

- Requires Phase 2 (applications must be installed)
- Requires internet connectivity
- API keys must be configured before this phase runs (see `hooks/pre_phase["6"]`)

---

## Phase 7 – Hardware & OS Foundation

### What it does

- Installs/updates all device drivers
- Configures NVIDIA GPU (CUDA, driver settings)
- Applies Windows registry optimisations
- Sets system locale, timezone, and keyboard layout
- Configures Windows Update policy
- Applies power plan settings
- Installs Visual C++ Redistributables and .NET runtimes
- Configures Windows Subsystem for Linux (WSL2) if requested

### How to skip

Phase 7 **cannot be skipped** – it provides the foundation that all other phases depend on.

### How to modify

```json
{
  "layers": {
    "layer7_hardware": {
      "enabled": true,
      "drivers": {
        "nvidia": true,
        "auto_update": false
      },
      "wsl2": true,
      "locale": "en-US",
      "timezone": "America/New_York",
      "dotnet_versions": ["6.0", "7.0", "8.0"],
      "vcredist": true
    }
  }
}
```

---

## Adding Custom Phases

Insert a phase that runs between existing phases:

```json
{
  "phases": {
    "custom": [
      {
        "id": 10,
        "name": "Company Software Deployment",
        "description": "Deploy internal company tools via SCCM",
        "script": "hooks/deploy_company_software.ps1",
        "after_phase": 2,
        "rollback_script": "hooks/remove_company_software.ps1",
        "timeout_minutes": 30,
        "retry_count": 2
      }
    ]
  }
}
```

Custom phase fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | int | Yes | Unique phase ID (use 10+ to avoid conflicts) |
| `name` | string | Yes | Display name |
| `description` | string | No | Long description |
| `script` | string | Yes | Script to execute |
| `after_phase` | int | Yes | Run after this built-in phase number |
| `rollback_script` | string | No | Script to undo changes |
| `timeout_minutes` | int | No | Max execution time (default 60) |
| `retry_count` | int | No | Number of retries on failure (default 0) |

---

## Phase Dependencies

```
Phase 7 (Hardware/OS)
    └─ Phase 1 (GUI)
    └─ Phase 2 (Applications)
           └─ Phase 6 (Cloud)
    └─ Phase 3 (Security)
    └─ Phase 4 (Users)
    └─ Phase 5 (Storage)  ← also depends on Phase 3 when encryption is on
```

The installer automatically validates that dependencies are satisfied when building the execution order.  If you skip a phase that another phase depends on, the installer will warn you.

---

## Error Handling Per Phase

### Default behaviour

| Phase | On error |
|-------|---------|
| 1 | Log warning, continue (GUI is optional) |
| 2 | Log error, continue (partial app install is acceptable) |
| 3 | Log error, **stop** if `rollback_on_error: true` (security failure is critical) |
| 4 | Log error, continue (admin account creation failure is re-attempted) |
| 5 | Log error, **stop** (storage failure can corrupt data) |
| 6 | Log warning, continue (cloud is optional) |
| 7 | Log error, **stop** (foundation failure cannot be recovered) |

### Overriding error behaviour per phase

```json
{
  "phases": {
    "error_policy": {
      "2": "continue",
      "3": "rollback",
      "5": "stop"
    }
  }
}
```

`error_policy` values:

| Value | Behaviour |
|-------|-----------|
| `"continue"` | Log error and move to next phase |
| `"rollback"` | Undo current phase and stop |
| `"stop"` | Stop immediately without rollback |
| `"retry"` | Retry the phase (use `retry_count` to limit) |

### Phase-specific rollback hooks

```json
{
  "hooks": {
    "post_phase": {
      "3": "hooks/after_security.ps1"
    },
    "on_phase_error": {
      "3": "hooks/security_error.ps1",
      "5": "hooks/storage_error.ps1"
    }
  }
}
```

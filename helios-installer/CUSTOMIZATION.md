# HELIOS Installer – Complete Customization Guide

This guide explains every option available in the HELIOS installer and shows you how to tailor the setup to match your exact requirements.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Configuration File Overview](#configuration-file-overview)
3. [Partition Customization](#partition-customization)
4. [Security Customization](#security-customization)
5. [User Account Customization](#user-account-customization)
6. [Layer Customization](#layer-customization)
7. [Optimization Customization](#optimization-customization)
8. [Feature Toggles](#feature-toggles)
9. [Phase Control](#phase-control)
10. [Custom Scripts & Hooks](#custom-scripts--hooks)
11. [Real-World Examples](#real-world-examples)
12. [Validation Rules](#validation-rules)

---

## Quick Start

```bash
# 1. Generate an interactive config with the customization tool
python customize.py

# 2. Or copy an example preset and edit manually
cp examples/example_minimal.json my_config.json

# 3. Run installer with your config
python install.py --config my_config.json

# 4. Dry-run (preview only, no changes applied)
python install.py --config my_config.json --dry-run
```

---

## Configuration File Overview

All customization is driven by a single JSON file.  The default template is at `config/default_config.json`.

Top-level sections:

| Section | Purpose |
|---------|---------|
| `installation` | Global installer settings |
| `phases` | Which phases to run and in what order |
| `partitions` | Drive/partition layout |
| `security` | Security policy options |
| `users` | User accounts to create |
| `layers` | 7-layer feature flags |
| `optimization` | Performance tuning |
| `features` | Individual feature toggles |
| `hooks` | Custom pre/post scripts |

---

## Partition Customization

### Adding a custom partition

```json
{
  "partitions": {
    "E": {
      "enabled": true,
      "size_gb": 200,
      "label": "Projects",
      "purpose": "developer_workspace",
      "type": "vhdx",
      "encrypt": false
    }
  }
}
```

### Removing a default partition

Set `"enabled": false` on any built-in drive letter:

```json
{
  "partitions": {
    "D": { "enabled": false }
  }
}
```

### Configuring VHDX drives

```json
{
  "partitions": {
    "V": {
      "enabled": true,
      "size_gb": 500,
      "label": "VirtualDisk",
      "type": "vhdx",
      "vhdx_path": "C:\\VHDs\\storage.vhdx",
      "dynamic": true,
      "encrypt": true,
      "encrypt_algorithm": "AES-256"
    }
  }
}
```

### Partition fields reference

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `enabled` | bool | `true` | Include this partition in setup |
| `size_gb` | int | varies | Desired size in GB |
| `label` | string | drive letter | Volume label |
| `purpose` | string | `"general"` | Logical role (`os`, `data`, `backup`, `dev`, `games`, `vhdx`) |
| `type` | string | `"basic"` | `"basic"` or `"vhdx"` |
| `encrypt` | bool | `false` | Enable BitLocker for this drive |
| `encrypt_algorithm` | string | `"AES-128"` | `"AES-128"` or `"AES-256"` |

---

## Security Customization

### Enabling / disabling Windows Defender

```json
{
  "security": {
    "defender": {
      "enabled": true,
      "realtime_protection": true,
      "cloud_protection": true,
      "automatic_sample_submission": false
    }
  }
}
```

### BitLocker configuration

```json
{
  "security": {
    "bitlocker": {
      "enabled": true,
      "drives": ["C", "D"],
      "algorithm": "AES-256",
      "require_tpm": true,
      "recovery_key_path": "D:\\Recovery"
    }
  }
}
```

### Firewall rules

```json
{
  "security": {
    "firewall": {
      "enabled": true,
      "inbound_policy": "block",
      "outbound_policy": "allow",
      "custom_rules": [
        {
          "name": "Allow SSH",
          "direction": "inbound",
          "port": 22,
          "protocol": "TCP",
          "action": "allow"
        }
      ]
    }
  }
}
```

### AppLocker whitelist

```json
{
  "security": {
    "applocker": {
      "enabled": true,
      "mode": "audit",
      "extra_allowed_publishers": [
        "O=MOZILLA CORPORATION, L=MOUNTAIN VIEW, S=CA, C=US"
      ],
      "extra_allowed_paths": [
        "%PROGRAMFILES%\\MyApp\\*"
      ]
    }
  }
}
```

> **mode** options: `"enforce"` (block unknown), `"audit"` (log only), `"disabled"`

### Audit logging

```json
{
  "security": {
    "audit_logging": {
      "enabled": true,
      "level": "verbose",
      "log_path": "D:\\Logs\\Audit",
      "max_log_size_mb": 100,
      "retention_days": 90
    }
  }
}
```

> **level** options: `"minimal"`, `"standard"`, `"verbose"`

### USB restrictions

```json
{
  "security": {
    "usb": {
      "restrict": true,
      "allow_hid": true,
      "allow_storage": false,
      "whitelist_vendor_ids": ["0x1234"]
    }
  }
}
```

---

## User Account Customization

### Adding a user

```json
{
  "users": [
    {
      "name": "alice",
      "display_name": "Alice Smith",
      "type": "standard",
      "enabled": true,
      "password_policy": "strong",
      "groups": ["Users", "Remote Desktop Users"]
    }
  ]
}
```

### Adding an administrator

```json
{
  "users": [
    {
      "name": "sysadmin",
      "type": "administrator",
      "enabled": true,
      "password_policy": "strong",
      "groups": ["Administrators"]
    }
  ]
}
```

### Removing a default account

```json
{
  "users": [
    {
      "name": "DefaultGamer",
      "enabled": false
    }
  ]
}
```

### User field reference

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Login name |
| `display_name` | string | Full display name |
| `type` | string | `"administrator"`, `"standard"`, `"service"` |
| `enabled` | bool | Create this account |
| `password_policy` | string | `"strong"`, `"standard"`, `"none"` |
| `groups` | array | Windows groups to assign |
| `profile_path` | string | Custom profile directory |

---

## Layer Customization

### Enabling / disabling individual layers

```json
{
  "layers": {
    "layer1_gui": true,
    "layer2_apps": true,
    "layer3_optimization": true,
    "layer4_security": true,
    "layer5_data": true,
    "layer6_cloud": false,
    "layer7_hardware": true
  }
}
```

### Layer-specific parameters

```json
{
  "layers": {
    "layer2_apps": {
      "enabled": true,
      "app_list": ["vscode", "git", "python", "docker"],
      "skip_optional": true
    },
    "layer3_optimization": {
      "enabled": true,
      "profile": "developer"
    }
  }
}
```

---

## Optimization Customization

### Setting the optimization level

```json
{
  "optimization": {
    "level": "standard"
  }
}
```

> **level** options: `"minimal"` | `"standard"` | `"aggressive"`

| Level | Description |
|-------|-------------|
| `minimal` | Only critical tweaks; maximum compatibility |
| `standard` | Balanced; recommended for most users |
| `aggressive` | Maximum performance; may disable some background services |

### Configuring startup items

```json
{
  "optimization": {
    "startup": {
      "disable_apps": ["OneDrive", "Teams", "Spotify"],
      "aggressive": false
    }
  }
}
```

### Service management

```json
{
  "optimization": {
    "services": {
      "disable_count": 15,
      "keep_essential": true,
      "custom_disable": ["DiagTrack", "SysMain"],
      "custom_enable": ["RemoteRegistry"]
    }
  }
}
```

### Power plan

```json
{
  "optimization": {
    "power": {
      "plan": "balanced",
      "custom_settings": {
        "sleep_timeout_minutes": 30,
        "hibernate": false
      }
    }
  }
}
```

> **plan** options: `"power_saver"` | `"balanced"` | `"high_performance"` | `"ultimate"`

---

## Feature Toggles

Toggle individual HELIOS features on or off:

```json
{
  "features": {
    "defender": true,
    "bitlocker": false,
    "firewall": true,
    "applocker": false,
    "audit_logging": true,
    "usb_restrictions": false,
    "seven_layer_setup": true,
    "optimization": true
  }
}
```

---

## Phase Control

### Running only specific phases

```json
{
  "phases": {
    "enabled": [1, 2, 4, 7]
  }
}
```

### Skipping a phase

```json
{
  "phases": {
    "skip": [3, 5]
  }
}
```

### Changing phase order

```json
{
  "phases": {
    "order": [7, 1, 2, 3, 4, 5, 6]
  }
}
```

### Adding a custom phase

```json
{
  "phases": {
    "custom": [
      {
        "id": 99,
        "name": "Install company tools",
        "script": "hooks/company_tools.ps1",
        "after_phase": 4,
        "rollback_script": "hooks/company_tools_rollback.ps1"
      }
    ]
  }
}
```

### Rollback behavior

```json
{
  "phases": {
    "rollback_on_error": true,
    "continue_on_warning": true
  }
}
```

---

## Custom Scripts & Hooks

Hooks run at specific points in the installation lifecycle.

```json
{
  "hooks": {
    "pre_install": "hooks/pre_install.ps1",
    "post_install": "hooks/post_install.ps1",
    "pre_phase": {
      "3": "hooks/before_security.ps1"
    },
    "post_phase": {
      "3": "hooks/after_security.ps1",
      "6": "hooks/after_cloud.ps1"
    },
    "on_error": "hooks/handle_error.ps1"
  }
}
```

Hook scripts receive the following environment variables:

| Variable | Description |
|----------|-------------|
| `HELIOS_PHASE` | Current phase number |
| `HELIOS_CONFIG` | Path to active config file |
| `HELIOS_LOG` | Path to current log file |
| `HELIOS_DRY_RUN` | `1` if running in dry-run mode |

---

## Real-World Examples

### Example 1 – Minimal Setup (skip security, fastest install)

```json
{
  "phases": { "enabled": [1, 2, 7] },
  "features": {
    "defender": false,
    "bitlocker": false,
    "applocker": false,
    "audit_logging": false,
    "usb_restrictions": false,
    "optimization": false
  }
}
```

### Example 2 – Developer Setup

```json
{
  "layers": { "layer6_cloud": false },
  "users": [
    { "name": "dev", "type": "administrator", "enabled": true }
  ],
  "optimization": { "level": "standard" },
  "partitions": {
    "D": { "enabled": true, "size_gb": 500, "label": "Dev", "purpose": "dev" }
  }
}
```

### Example 3 – Maximum Security Setup

```json
{
  "security": {
    "defender": { "enabled": true, "realtime_protection": true, "cloud_protection": true },
    "bitlocker": { "enabled": true, "drives": ["C", "D"], "algorithm": "AES-256" },
    "firewall": { "enabled": true, "inbound_policy": "block" },
    "applocker": { "enabled": true, "mode": "enforce" },
    "audit_logging": { "enabled": true, "level": "verbose" },
    "usb": { "restrict": true, "allow_storage": false }
  }
}
```

---

## Validation Rules

Before the installer runs, all configuration values are validated:

| Rule | Details |
|------|---------|
| Partition sizes | Min 10 GB per partition; total must not exceed available disk space |
| Drive letter conflicts | Each letter must be unique |
| User names | Alphanumeric, hyphen, underscore, and period (cannot start with a period); max 20 characters |
| Security policy compatibility | BitLocker requires TPM 2.0 or USB key |
| Phase dependencies | Phase 3 (Security) must run before Phase 5 (Data) when encryption is on |
| Hook scripts | Must exist on disk before installer starts |

Run validation without installing:

```bash
python install.py --config my_config.json --validate-only
```

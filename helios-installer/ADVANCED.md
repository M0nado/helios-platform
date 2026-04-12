# HELIOS Installer – Advanced Customization Guide

This guide covers advanced topics for users who need to go beyond the standard configuration options.

---

## Table of Contents

1. [Modifying Individual Phases](#modifying-individual-phases)
2. [Adding Custom Hooks](#adding-custom-hooks)
3. [Pre/Post-Installation Scripts](#prepost-installation-scripts)
4. [Custom Security Policies](#custom-security-policies)
5. [Network Configuration](#network-configuration)
6. [Performance Tuning Parameters](#performance-tuning-parameters)
7. [VHDX / Virtual Disk Configuration](#vhdx--virtual-disk-configuration)
8. [Logging and Diagnostics](#logging-and-diagnostics)
9. [Unattended / Silent Installation](#unattended--silent-installation)
10. [Rolling Back an Installation](#rolling-back-an-installation)

---

## Modifying Individual Phases

Each phase is defined by a set of tasks.  You can inject, reorder, or skip tasks within a phase using the `phases.custom` block.

### Injecting a task into an existing phase

```json
{
  "phases": {
    "custom": [
      {
        "id": 3,
        "inject_task": {
          "name": "Apply custom GPO",
          "type": "powershell",
          "script": "hooks/apply_gpo.ps1",
          "order": "after_all"
        }
      }
    ]
  }
}
```

`order` options:

| Value | Behaviour |
|-------|-----------|
| `"before_all"` | Run before any built-in phase tasks |
| `"after_all"` | Run after all built-in phase tasks |
| `"after:<task_name>"` | Run immediately after the named built-in task |

### Replacing a built-in task

```json
{
  "phases": {
    "custom": [
      {
        "id": 4,
        "replace_task": {
          "name": "configure_bitlocker",
          "script": "hooks/my_bitlocker.ps1"
        }
      }
    ]
  }
}
```

### Skipping a built-in task

```json
{
  "phases": {
    "custom": [
      {
        "id": 4,
        "skip_tasks": ["configure_applocker", "configure_usb"]
      }
    ]
  }
}
```

---

## Adding Custom Hooks

Hooks are PowerShell (`.ps1`) or Python (`.py`) scripts executed at lifecycle events.

### Hook execution order

```
pre_install
  └─ Phase 1 starts
       ├─ pre_phase[1]
       │   [Phase 1 tasks]
       └─ post_phase[1]
  └─ Phase 2 starts
       ...
  └─ Phase N ends
post_install
```

### Passing data between hooks

The installer writes a shared state file at `%HELIOS_TEMP%\state.json`.  Your hooks can read and write to this file.

```powershell
# hooks/my_hook.ps1

$statePath = $env:HELIOS_TEMP + "\state.json"
$state = Get-Content $statePath | ConvertFrom-Json

# Read a value set by a previous hook
Write-Host "Previous result: $($state.my_previous_result)"

# Write a value for a subsequent hook
$state | Add-Member -NotePropertyName "my_result" -NotePropertyValue "ok" -Force
$state | ConvertTo-Json | Set-Content $statePath
```

### Hook environment variables

| Variable | Value |
|----------|-------|
| `HELIOS_PHASE` | Current phase number (0 = outside any phase) |
| `HELIOS_PHASE_NAME` | Current phase name |
| `HELIOS_CONFIG` | Absolute path to the active configuration JSON |
| `HELIOS_LOG` | Absolute path to the current log file |
| `HELIOS_TEMP` | Temporary working directory |
| `HELIOS_DRY_RUN` | `1` if running in dry-run mode, else `0` |
| `HELIOS_VERSION` | Installer version string |

### Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success – continue installation |
| 1 | Warning – log and continue |
| 2 | Error – trigger rollback (if `rollback_on_error: true`) |
| 3 | Fatal – stop immediately without rollback |

---

## Pre/Post-Installation Scripts

### Pre-install script example

```powershell
# hooks/pre_install.ps1
# Verify prerequisites before the installer starts

$RequiredFreeGB = 500
$drive = Get-PSDrive C
$freeGB = [math]::Round($drive.Free / 1GB, 1)

if ($freeGB -lt $RequiredFreeGB) {
    Write-Error "Insufficient disk space: ${freeGB} GB free, need ${RequiredFreeGB} GB"
    exit 2
}

Write-Host "Pre-install check passed: ${freeGB} GB free on C:"
exit 0
```

### Post-install script example

```powershell
# hooks/post_install.ps1
# Run any final configuration after all phases complete

Write-Host "HELIOS installation complete. Running post-install tasks..."

# Example: pin application to taskbar
# Example: set desktop wallpaper
# Example: send notification

Write-Host "Post-install tasks finished."
exit 0
```

### Python hook example

```python
#!/usr/bin/env python3
# hooks/validate_environment.py

import os
import sys
import json
import pathlib

config_path = os.environ.get("HELIOS_CONFIG", "")
if not config_path:
    print("HELIOS_CONFIG not set", file=sys.stderr)
    sys.exit(2)

with open(config_path) as fh:
    cfg = json.load(fh)

print(f"Validating config: {cfg['installation']['name']}")
# ... custom validation logic ...
sys.exit(0)
```

---

## Custom Security Policies

### Importing a Group Policy Object (GPO)

```json
{
  "security": {
    "custom_gpo": {
      "enabled": true,
      "backup_path": "config/gpo_backup",
      "gpo_name": "HELIOS Corporate Policy"
    }
  }
}
```

```powershell
# hooks/apply_gpo.ps1
$gpoPath = Join-Path $env:HELIOS_CONFIG "..\..\config\gpo_backup"
Import-GPO -BackupId "HELIOS Corporate Policy" -Path $gpoPath -CreateIfNeeded
```

### Custom AppLocker rules

Place an `AppLocker.xml` policy export in `config/` and reference it:

```json
{
  "security": {
    "applocker": {
      "enabled": true,
      "mode": "enforce",
      "policy_xml": "config/AppLocker.xml"
    }
  }
}
```

### Custom Windows Defender exclusions

```json
{
  "security": {
    "defender": {
      "enabled": true,
      "exclusions": {
        "paths": ["C:\\DevTools\\", "D:\\Projects\\"],
        "extensions": [".tmp", ".log"],
        "processes": ["node.exe", "python.exe"]
      }
    }
  }
}
```

---

## Network Configuration

### Static IP assignment

```json
{
  "network": {
    "adapter": "Ethernet",
    "dhcp": false,
    "static_ip": "192.168.1.100",
    "subnet_mask": "255.255.255.0",
    "gateway": "192.168.1.1",
    "dns": ["192.168.1.1", "8.8.8.8"]
  }
}
```

### Proxy configuration

```json
{
  "network": {
    "proxy": {
      "enabled": true,
      "server": "proxy.company.com",
      "port": 8080,
      "bypass": ["localhost", "192.168.*"]
    }
  }
}
```

### VPN auto-connect (WireGuard)

```json
{
  "network": {
    "vpn": {
      "enabled": true,
      "type": "wireguard",
      "config_path": "config/wireguard.conf",
      "auto_connect": true
    }
  }
}
```

---

## Performance Tuning Parameters

### Full optimization object reference

```json
{
  "optimization": {
    "level": "aggressive",
    "startup": {
      "disable_apps": ["OneDrive", "Teams", "Spotify"],
      "aggressive": true
    },
    "services": {
      "disable_count": 20,
      "keep_essential": true,
      "custom_disable": ["DiagTrack", "SysMain", "WSearch"],
      "custom_enable": []
    },
    "power": {
      "plan": "ultimate",
      "custom_settings": {
        "sleep_timeout_minutes": 0,
        "hibernate": false,
        "usb_selective_suspend": false,
        "pcie_link_state": "off"
      }
    },
    "visual_effects": "performance",
    "memory_management": {
      "clear_page_file_on_shutdown": true,
      "large_system_cache": false,
      "prefetch": "application_launch"
    },
    "network": "gaming",
    "cpu": {
      "priority_boost": true,
      "affinity_mask": null
    },
    "gpu": {
      "power_management": "prefer_maximum_performance",
      "vsync": false
    }
  }
}
```

### `visual_effects` options

| Value | Description |
|-------|-------------|
| `"system_default"` | Let Windows decide |
| `"best_appearance"` | All effects enabled |
| `"performance"` | All effects disabled |
| `"custom"` | Use `visual_effects_custom` block |

### `network` options

| Value | Description |
|-------|-------------|
| `"standard"` | Default Windows networking |
| `"gaming"` | Nagle algorithm disabled, TCP optimizations |
| `"server"` | High-throughput settings |

---

## VHDX / Virtual Disk Configuration

```json
{
  "partitions": {
    "V": {
      "enabled": true,
      "size_gb": 500,
      "label": "VirtualStorage",
      "type": "vhdx",
      "vhdx_path": "C:\\VHDs\\storage.vhdx",
      "dynamic": true,
      "block_size_mb": 2,
      "sector_size_bytes": 4096,
      "encrypt": true,
      "encrypt_algorithm": "AES-256",
      "auto_mount": true,
      "mount_point": "V:"
    }
  }
}
```

VHDX-specific fields:

| Field | Type | Description |
|-------|------|-------------|
| `vhdx_path` | string | Path where the `.vhdx` file will be created |
| `dynamic` | bool | `true` = thin-provisioned; `false` = fixed size |
| `block_size_mb` | int | VHDX internal block size (1 or 2 MB) |
| `sector_size_bytes` | int | Logical sector size (512 or 4096) |
| `auto_mount` | bool | Mount automatically on Windows startup |
| `mount_point` | string | Drive letter or folder path to mount at |

---

## Logging and Diagnostics

### Log levels

| Level | What is logged |
|-------|---------------|
| `DEBUG` | Everything, including low-level API calls |
| `INFO` | Normal progress messages (default) |
| `WARNING` | Non-fatal issues |
| `ERROR` | Errors only |

### Changing log path

```json
{
  "installation": {
    "logging_level": "DEBUG",
    "log_path": "D:\\Logs\\HELIOS"
  }
}
```

Log files are created as `helios_install_<timestamp>.log` in `log_path`.

### Enabling verbose phase logs

```json
{
  "phases": {
    "verbose_logging": true
  }
}
```

---

## Unattended / Silent Installation

Run the installer without any interactive prompts:

```bash
python install.py --config my_config.json --unattended
```

For CI/CD pipelines, combine with `--validate-only` first:

```bash
python install.py --config my_config.json --validate-only && \
python install.py --config my_config.json --unattended
```

Set `"auto_reboot": false` in your config when running unattended to control the reboot separately.

---

## Rolling Back an Installation

### Automatic rollback

Enable in config:

```json
{
  "phases": {
    "rollback_on_error": true
  }
}
```

When an error occurs, the installer will undo changes made by the current and all previous phases in reverse order.

### Manual rollback

```bash
python install.py --rollback --log helios_install_20260412.log
```

The installer uses the log to identify which operations were applied and reverses them.

### Rollback scope

| Phase | What is rolled back |
|-------|---------------------|
| 1 | GUI components removed |
| 2 | Installed programs uninstalled |
| 3 | Security policies reverted to defaults |
| 4 | User accounts removed |
| 5 | Data partitions unmounted (not deleted) |
| 6 | Cloud agents stopped and removed |
| 7 | Hardware/OS tweaks restored to defaults |

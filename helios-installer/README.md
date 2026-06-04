# HELIOS Quick Start USB Installer

> **Automated 7-phase Windows workstation provisioning in 30–45 minutes.**
> Zero manual interaction required after launch.

---

## Table of Contents

1. [Overview](#overview)
2. [Requirements](#requirements)
3. [Quick Start](#quick-start)
4. [Creating a Bootable USB](#creating-a-bootable-usb)
5. [Installation Phases](#installation-phases)
6. [What Gets Configured](#what-gets-configured)
7. [Command-Line Options](#command-line-options)
8. [Log File](#log-file)
9. [Troubleshooting](#troubleshooting)

---

## Overview

The HELIOS Quick Start USB Installer is a self-contained Python package that
automates the complete initial configuration of a HELIOS workstation:

```
User runs installer
        ↓
System validates (admin / preflight checks)
        ↓
Partitions created (9 drives, 4 TB layout)
        ↓
Security baseline applied (BitLocker, Firewall, AppLocker, Audit)
        ↓
User accounts created (7 profiles)
        ↓
7-Layer foundation initialised (all layers active)
        ↓
System optimisation applied (startup, services, network, power)
        ↓
Verification & validation
        ↓
Complete! Ready for HELIOS Build 1 deployment
```

---

## Requirements

| Requirement | Minimum |
|-------------|---------|
| OS | Windows 10 / 11 (64-bit) |
| Python | 3.10+ |
| RAM | 16 GB recommended |
| Disk | 4 TB target volume |
| Privileges | Administrator / root |

Python packages (installed automatically by the launchers):

```
colorama>=0.4.6
psutil>=5.9.0
pywin32>=306          (Windows only)
requests>=2.31.0
tqdm>=4.66.0
```

---

## Quick Start

### Windows

```bat
:: Right-click → Run as administrator
run_installer.bat
```

### Linux / macOS

```bash
sudo bash run_installer.sh
```

### Manual (any platform)

```bash
pip install -r requirements.txt
python main.py
```

To simulate without making any real changes:

```bash
python main.py --dry-run
```

---

## Creating a Bootable USB

Use `create_bootable_usb.py` to copy the installer onto a USB drive so it can
be transported to the target machine.

### Windows

```bat
:: Run as Administrator
python create_bootable_usb.py --drive E
```

### Linux / macOS

```bash
# Identify your USB device first (e.g. /dev/sdb)
lsblk

sudo python3 create_bootable_usb.py --drive /dev/sdb
```

**⚠  WARNING:** The target drive will be formatted (all data erased).
The tool will prompt for confirmation unless `--force` is supplied.

#### USB Tool Options

| Option | Description |
|--------|-------------|
| `--drive DRIVE` | **Required.** Drive letter (Windows) or device path (Linux). |
| `--source DIR` | Source directory. Defaults to the script's own directory. |
| `--label LABEL` | Volume label. Default: `HELIOS_USB`. |
| `--dry-run` | Preview actions without touching the drive. |
| `--force` | Skip the confirmation prompt. |

---

## Installation Phases

| Phase | Name | Duration |
|-------|------|----------|
| 1 | Pre-flight system checks | ~1 min |
| 2 | Disk partitioning (9 drives, 4 TB) | ~5 min |
| 3 | Security baseline | ~5 min |
| 4 | User account setup (7 profiles) | ~2 min |
| 5 | 7-Layer foundation initialisation | ~3 min |
| 6 | System optimisation | ~5 min |
| 7 | Verification & validation | ~2 min |

Total: **~23 min** (varies by hardware; full deployment with optional downloads
takes 30–45 min).

You can run individual phases with `--phases`:

```bash
# Run only phases 3 and 4
python main.py --phases 3 4
```

---

## What Gets Configured

### Partitions (9 drives)

| Drive | Label | Size |
|-------|-------|------|
| C: | HELIOS_SYSTEM | 200 GB |
| D: | HELIOS_DATA | 500 GB |
| F: | HELIOS_APPS | 300 GB |
| G: | HELIOS_GAMES | 1 TB |
| H: | HELIOS_MEDIA | 500 GB |
| I: | HELIOS_ISO | 200 GB |
| J: | HELIOS_BACKUP | 500 GB |
| K: | HELIOS_SECURE | 300 GB |
| L: | HELIOS_LOGS | 100 GB |

### Security

- Windows Defender (real-time protection, advanced MAPS)
- Windows Firewall (all profiles enabled)
- AppLocker via Application Guard
- Full audit policy (success + failure for all categories)

### User Accounts (7 profiles)

| Account | Group | Purpose |
|---------|-------|---------|
| HeliosAdmin | Administrators | Primary administrator |
| HeliosDev | Developers | Development workloads |
| HeliosCreator | Users | Creative workloads |
| HeliosGamer | Users | Gaming profile |
| HeliosGuest | Guests | Restricted access |
| HeliosService | Users | Background services |
| HeliosBackup | Backup Operators | Backup & restore |

### 7-Layer Foundation

1. **Layer 1** – UI & Rendering (Xenoblade GUI)
2. **Layer 2** – Application Management (50+ programs)
3. **Layer 3** – Optimisation Engine (MONADO)
4. **Layer 4** – Security Framework (AEGIS)
5. **Layer 5** – Data & Storage Management
6. **Layer 6** – Cloud & AI Orchestration (NEXUS)
7. **Layer 7** – Hardware & OS Management

Each layer gets its own directory tree under `C:\HELIOS\layer{N}\` with
`config\`, `logs\`, and `data\` subdirectories plus an initialisation marker.

### Optimisation

- Non-essential services (SysMain, WSearch) set to on-demand start
- TCP auto-tuning and RSS enabled
- High Performance power plan activated
- Visual effects optimised for performance

---

## Command-Line Options

```
usage: main.py [-h] [--dry-run] [--log-level {DEBUG,INFO,WARNING,ERROR}]
               [--phases N [N ...]]

options:
  -h, --help            show this help message and exit
  --dry-run             Simulate all phases without making real changes
  --log-level LEVEL     Logging verbosity (default: INFO)
  --phases N [N ...]    Run only the specified phases (1-7)
```

---

## Log File

All activity is written to `helios_setup.log` in the current working directory.
The log includes timestamps, log levels, command output, and a final
success/failure summary.

---

## Troubleshooting

| Symptom | Resolution |
|---------|------------|
| "Administrator privileges required" | Right-click the launcher → Run as administrator |
| Partition creation fails | Run Phase 2 separately after booting from a Windows PE / WinRE environment |
| `pywin32` install fails | Ensure Visual C++ Redistributable is installed or use `--dry-run` for testing |
| Layer directories not created | Verify `C:\HELIOS` is accessible; re-run Phase 5 with `--phases 5` |
| Command not found (diskpart, net, etc.) | Installer is designed for Windows; non-Windows phases are automatically skipped |

For additional help, open an issue at:
<https://github.com/M0nado/helios-platform/issues>

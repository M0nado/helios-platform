# helios-platform
HELIOS Platform - Complete Windows workstation optimization, security, and automation ecosystem

---

## Quick Start USB Installer

Get a HELIOS workstation fully provisioned in 30–45 minutes with **zero manual interaction**.

```
helios-installer/
├── main.py                 # 7-phase automated installer
├── requirements.txt        # Python dependencies
├── run_installer.bat       # Windows launcher (Run as Administrator)
├── run_installer.sh        # Linux/macOS launcher (sudo)
├── create_bootable_usb.py  # Write installer to a USB drive
└── README.md               # Full documentation
```

### Windows – one command

```bat
cd helios-installer
run_installer.bat
```

### Linux / macOS – one command

```bash
cd helios-installer
sudo bash run_installer.sh
```

### What the installer configures

| Phase | Description |
|-------|-------------|
| 1 | Pre-flight system checks (admin, OS, RAM, disk) |
| 2 | Disk partitioning – 9 drives (C D F G H I J K L), 4 TB layout |
| 3 | Security baseline – Defender, Firewall, AppLocker, Audit logging |
| 4 | User accounts – 7 profiles (Admin, Dev, Creator, Gamer, Guest, Service, Backup) |
| 5 | 7-Layer foundation – all layers initialised and active |
| 6 | System optimisation – startup, services, network, power plan |
| 7 | Verification & validation |

See **[helios-installer/README.md](helios-installer/README.md)** for full documentation.

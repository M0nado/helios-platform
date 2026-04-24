# HELIOS Platform v3.6.0 - Quick Reference Card

**Version**: 3.6.0 | **Updated**: 2026-05-15

## Common Tasks

### Cloud Synchronization
```
Enable sync:    Settings > Cloud Sync > Provider > Enable
Configure:      Settings > Cloud Sync > [Provider] > Options
Manual sync:    Dashboard > Cloud Sync > "Sync Now" button
View status:    Dashboard > Cloud Sync > Status & Statistics
Resolve conflicts: Dashboard > Cloud Sync > Conflicts > Resolve
```

### Plugin Management
```
Browse plugins:     Extensions > Plugin Marketplace
Install plugin:     Marketplace > [Plugin] > Install
View installed:     Extensions > Installed Plugins
Enable/disable:     Installed Plugins > [Toggle]
Configure:          Installed Plugins > [Plugin] > Configure
Uninstall:          Installed Plugins > [Plugin] > Uninstall
```

### Dashboard Access
```
Access:           https://localhost:8080
Overview:         Dashboard > Overview (system metrics)
Performance:      Dashboard > Performance (CPU, Memory, Disk, Network)
Logs:             Dashboard > Logs (real-time log stream)
Plugins:          Dashboard > Plugins (installed plugins)
```

### Theme & Appearance
```
Switch theme:     Settings > Appearance > Theme > Light/Dark/Auto
Create custom:    Settings > Appearance > Custom Theme > Create
Apply theme:      Settings > Appearance > Themes > [Theme] > Apply
```

## Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Dashboard refresh | F5 |
| Dark mode toggle | Ctrl+Shift+D |
| Clear logs | Ctrl+L |
| Settings | Ctrl+, |

## Important Ports

| Service | Port |
|---------|------|
| Dashboard | 8080 |
| Core API | 8081 |

## Troubleshooting Quick Guide

| Issue | Solution |
|-------|----------|
| Can't access dashboard | Verify HELIOS service: `Get-Service HELIOS` |
| Cloud sync failing | Check credentials, internet, provider auth |
| Plugin won't load | Check compatibility, verify permissions |
| Dark mode not working | Clear browser cache, hard refresh |

---

**Print this card for quick reference!**

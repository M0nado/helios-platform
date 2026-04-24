# HELIOS Platform v3.6.0 - User Guide

**Version**: 3.6.0  
**Status**: Production Ready ✅  
**Last Updated**: 2026-05-15

## Table of Contents

1. [Getting Started with v3.6.0](#getting-started-with-v360)
2. [Cloud Sync Setup Wizard](#cloud-sync-setup-wizard)
3. [Installing & Managing Plugins](#installing--managing-plugins)
4. [Using the Developer Dashboard](#using-the-developer-dashboard)
5. [Customizing Dark Mode](#customizing-dark-mode)
6. [Troubleshooting Guide](#troubleshooting-guide)

---

## Getting Started with v3.6.0

### System Requirements

| Component | Requirement |
|-----------|------------|
| Operating System | Windows 11 Pro or Server 2022+ |
| Processor | Intel/AMD 64-bit, 2+ cores |
| RAM | 4GB minimum, 8GB recommended |
| Disk Space | 2GB for installation, 5GB+ for cloud sync cache |
| .NET Runtime | .NET 8.0 or later |
| Network | Stable internet connection (100 Mbps recommended) |

### Installation

**Windows Installer (Recommended)**

1. Download `HELIOS-Platform-v3.6.0-Setup.exe` from [releases](https://github.com/M0nado/helios-platform/releases)
2. Run the installer with administrator privileges
3. Follow the setup wizard
4. Accept default paths or choose custom installation directory
5. Wait for installation to complete (2-3 minutes)
6. HELIOS will launch automatically

**PowerShell Installation**

```powershell
# Download and install via PowerShell
$downloadUrl = "https://github.com/M0nado/helios-platform/releases/download/v3.6.0/HELIOS-Platform-v3.6.0.zip"
Invoke-WebRequest -Uri $downloadUrl -OutFile "$env:TEMP\helios-v3.6.0.zip"

# Extract and install
Expand-Archive -Path "$env:TEMP\helios-v3.6.0.zip" -DestinationPath "C:\Program Files\HELIOS"

# Add to PATH
[Environment]::SetEnvironmentVariable(
    "Path",
    "$env:Path;C:\Program Files\HELIOS\bin",
    "User"
)
```

### First Launch

1. Accept license agreement
2. Choose initial configuration preset (Minimal, Standard, Advanced)
3. Set administrator password for dashboard access
4. Configure initial cloud storage (optional)
5. Review and confirm settings
6. Wait for system initialization (1-2 minutes)

### Post-Installation Checklist

- [ ] Installation completed successfully
- [ ] HELIOS service running (`Get-Service -Name "HELIOS*"`)
- [ ] Dashboard accessible at https://localhost:8080
- [ ] Configure administrator credentials
- [ ] Set up cloud synchronization (optional)
- [ ] Install desired plugins from marketplace
- [ ] Configure theme preference

---

## Cloud Sync Setup Wizard

### Step 1: Access Cloud Sync Settings

1. Open HELIOS Dashboard (https://localhost:8080)
2. Navigate to **Settings > Cloud Synchronization**
3. Click **Setup New Provider**

### Step 2: Choose Cloud Provider

Available providers:

- **OneDrive/SharePoint** - Microsoft 365 integration
- **Azure Blob Storage** - Azure enterprise storage
- **AWS S3** - Amazon S3 storage
- **Google Drive** - Google Workspace support
- **Generic Nextcloud** - Self-hosted option

### Step 3: Authenticate

**For OneDrive:**

1. Click "Authenticate with Microsoft"
2. Sign in with your Microsoft account
3. Grant permissions for file access
4. Authorization code will auto-fill

**For Azure Storage:**

1. Enter Storage Account Name
2. Provide Connection String or Access Key
3. Test connection
4. Proceed if successful

**For AWS S3:**

1. Enter AWS Access Key ID
2. Enter AWS Secret Access Key
3. Select region
4. Test connection

### Step 4: Configure Sync Settings

```
Sync Behavior:
  ☐ Auto-sync enabled
  ☐ Sync interval: [5] minutes
  ☐ Sync on startup

Paths to Sync:
  ☐ Documents
  ☐ Desktop
  ☐ Custom path: [_____________]

Exclusions:
  ☐ Exclude temporary files
  ☐ Exclude system files
  ☐ Exclude: [_____________]

Performance:
  Upload limit: [10] Mbps
  Download limit: [20] Mbps
  Max concurrent transfers: [5]

Security:
  ☐ Enable encryption (AES-256)
  ☐ Compress data before upload
  ☐ Enable integrity verification
```

### Step 5: Review and Activate

1. Review configuration summary
2. Click **Start Sync** to begin initial synchronization
3. Monitor progress in dashboard
4. Verify files are synchronized

### Manual Sync Operations

```
Dashboard > Cloud Sync > Actions:

⟳ Sync Now          - Perform immediate full sync
↑ Push Local         - Upload local changes only
↓ Pull Remote        - Download remote changes only
⚙ View Conflicts     - Resolve conflicting changes
📊 View Statistics   - See sync history and stats
```

---

## Installing & Managing Plugins

### Browse Marketplace

1. Navigate to **Extensions > Plugin Marketplace**
2. Browse categories or search by name
3. Click plugin card to view details
4. Review permissions, ratings, and documentation

### Install Plugin

1. Click **Install** on plugin card
2. Review required permissions
3. Confirm installation
4. Wait for download and installation (depends on size)
5. Plugin automatically loads

### Manage Installed Plugins

**View Installed Plugins:**

1. Navigate to **Extensions > Installed Plugins**
2. See list of all installed plugins with:
   - Current version
   - Status (enabled/disabled)
   - Last updated date
   - Resource usage

**Enable/Disable Plugin:**

```
For each plugin in the list:
  ☑ Checkbox to toggle enabled/disabled
  
  Disabled plugins:
    • Won't load on startup
    • Won't respond to commands
    • Resources freed
```

**Update Plugin:**

1. Navigate to **Extensions > Installed Plugins**
2. Look for **Updates Available** badge
3. Click **Update** button
4. System automatically downloads and installs latest version
5. Plugin reloads if enabled

**Uninstall Plugin:**

1. Click plugin in Installed Plugins list
2. Click **Uninstall** button
3. Confirm removal
4. Plugin removed, configuration preserved for 30 days

### Plugin Configuration

1. Navigate to **Extensions > Installed Plugins**
2. Click plugin name
3. Click **Configure** tab
4. Modify settings as needed
5. Click **Save** to apply

---

## Using the Developer Dashboard

### Dashboard Access

- **URL**: https://localhost:8080
- **Default Port**: 8080 (configurable)
- **Authentication**: Required by default
- **HTTPS**: Enabled with self-signed certificate

### Login

1. Navigate to https://localhost:8080
2. Enter administrator username/password (set during installation)
3. Click **Sign In**
4. Dashboard loads

### Main Views

#### System Overview

Real-time system status:

```
┌─────────────────────────────────────┐
│  CPU Usage: 42% (3.2 GHz)          │
│  ████████░░░░░░░░░░░░░░░░░░░░░░    │
│                                     │
│  Memory: 8.2 / 16 GB (51%)         │
│  ████████████░░░░░░░░░░░░░░░░░░    │
│                                     │
│  Disk: 256.4 / 512 GB (50%)        │
│  ████████████░░░░░░░░░░░░░░░░░░    │
│                                     │
│  Network: ↓ 12.5 MB/s ↑ 3.2 MB/s  │
│  Uptime: 45 days 3 hours           │
└─────────────────────────────────────┘
```

**Key Metrics:**
- CPU usage per-core
- Memory utilization and pressure
- Disk I/O operations
- Network throughput and active connections
- Process count and thread count

#### Performance Metrics

Historical performance data:

```
View Options:
  Time Range: [Last 1 hour ▼]
  Metric: [CPU Usage ▼]
  Granularity: [1-minute ▼]

Graph:
  [Line graph showing CPU usage over time]

Export:
  [Download as CSV] [Download as JSON]
```

**Available Metrics:**
- CPU usage (overall and per-core)
- Memory usage and available memory
- Disk read/write performance
- Network throughput (upload/download)
- Process creation rate
- Context switches

#### Logs and Events

Real-time log viewer:

```
Filter Options:
  ☐ Timestamp: [Start] [End]
  ☐ Level: [All ▼]
  ☐ Source: [All ▼]
  ☐ Search: [________________]

Log Entries (with timestamps):
  2026-05-15 14:32:01 [INFO] Cloud sync completed
  2026-05-15 14:31:45 [WARN] Memory usage above 80%
  2026-05-15 14:31:20 [INFO] Plugin loaded: custom-monitor
```

**Features:**
- Real-time log streaming
- Filtering by level, source, timestamp
- Search with regex support
- Export logs to file
- Configurable retention period

#### Plugins View

Plugin management interface:

```
Plugin List:
  [Plugin Name]
  Status: ☑ Enabled | Memory: 45 MB | CPU: 2%
  
  Actions:
    [Configure] [Disable] [Uninstall] [Logs]
```

**Plugin Details:**
- Current version and status
- Resource consumption (CPU, Memory, Disk)
- Last execution time
- Error count and logs
- Enable/disable control
- Configuration access

### Custom Views

Create custom dashboard views:

1. Click **+ Add View** button
2. Select view type (Chart, Metric, Log, Custom)
3. Configure view:
   - Data source
   - Time range
   - Refresh interval
   - Display options
4. Click **Save**

---

## Customizing Dark Mode

### Enable Dark Mode

**Automatic (System Setting):**

1. Navigate to **Settings > Appearance > Theme**
2. Select **Auto** (follows system setting)
3. Theme automatically switches based on system preference

**Manual Dark Mode:**

1. Navigate to **Settings > Appearance > Theme**
2. Select **Dark**
3. Theme immediately switches to dark

**Manual Light Mode:**

1. Navigate to **Settings > Appearance > Theme**
2. Select **Light**
3. Theme immediately switches to light

### Custom Color Scheme

1. Navigate to **Settings > Appearance > Custom Theme**
2. Click **Create New Theme**
3. Configure colors:

```
Primary Color: [#1E88E5        ▢]
Secondary Color: [#FF5722      ▢]
Background: [#121212           ▢]
Surface: [#1E1E1E             ▢]
Text: [#FFFFFF                ▢]
Text Secondary: [#B0B0B0      ▢]
Error: [#F44336               ▢]
Success: [#4CAF50             ▢]
Warning: [#FF9800             ▢]
```

4. Configure typography:

```
Font Family: [Segoe UI         ▼]
Base Font Size: [14            ]
Line Height: [1.5              ]
Letter Spacing: [0             ]
```

5. Preview changes in real-time
6. Click **Save Theme**

### Apply Custom Theme

1. Navigate to **Settings > Appearance > Themes**
2. Select custom theme from list
3. Click **Apply**
4. Theme active immediately

---

## Troubleshooting Guide

### Cloud Synchronization Issues

**Problem: Sync not starting**

```
Solutions:
1. Check internet connectivity: ping 8.8.8.8
2. Verify cloud provider credentials in Settings
3. Check HELIOS service status: Get-Service HELIOS.CloudSync
4. Review logs: Dashboard > Logs, filter by CloudSync
5. Restart cloud sync: Settings > Cloud Sync > Restart Service
```

**Problem: Files not syncing**

```
Solutions:
1. Check sync exclusions: Settings > Cloud Sync > Exclusions
2. Verify file permissions on local and cloud
3. Check file size limits (max 4GB per file)
4. Check disk space: Need 20% free for sync operations
5. View pending transfers: Dashboard > Cloud Sync > Pending
6. Force re-sync: Dashboard > Cloud Sync > Force Full Sync
```

**Problem: Sync conflicts**

```
Resolution Steps:
1. Navigate to Dashboard > Cloud Sync > Conflicts
2. Review each conflict showing local and remote versions
3. Choose: Keep Local | Keep Remote | Manual Merge
4. Click Resolve
5. Conflicts cleared and files synced
```

### Plugin Issues

**Problem: Plugin won't load**

```
Solutions:
1. Check compatibility: Extension marketplace shows requirements
2. Check HELIOS version: Dashboard > About
3. Verify plugin permissions granted
4. Review plugin logs: Dashboard > Plugins > [Plugin] > Logs
5. Restart plugin: Dashboard > Plugins > [Plugin] > Restart
6. Reinstall if needed: Uninstall > Reinstall from marketplace
```

**Problem: Plugin consuming too much CPU/Memory**

```
Solutions:
1. Check plugin configuration: Dashboard > Plugins > Configure
2. Reduce update frequency if applicable
3. Check plugin logs for errors
4. Contact plugin developer with logs
5. Consider disabling if blocking operations
```

### Dashboard Access Issues

**Problem: Can't access dashboard at localhost:8080**

```
Solutions:
1. Check HELIOS service running: Get-Service HELIOS
2. Check port not in use: netstat -ano | findstr :8080
3. Check firewall rules
4. Check dashboard service: Get-Service HELIOS.Dashboard
5. Restart dashboard: Restart-Service HELIOS.Dashboard
6. Check for port configuration in Settings
```

**Problem: HTTPS certificate warning**

```
Solutions:
1. This is normal for self-signed certificates
2. Proceed past warning (certificate is trusted locally)
3. To use real certificate: Settings > Dashboard > Certificate
4. Install certificate from file or Let's Encrypt
```

### Theme Issues

**Problem: Dark mode not applied everywhere**

```
Solutions:
1. Clear browser cache: Ctrl+Shift+Delete
2. Hard refresh dashboard: Ctrl+Shift+R
3. Check custom theme syntax in Settings > Appearance
4. Reapply theme: Settings > Appearance > Themes > [Theme] > Apply
5. Restart dashboard for persistent changes
```

### General Troubleshooting

**Check System Health:**

```powershell
# Check HELIOS services
Get-Service -Name "HELIOS*" | Select Name, Status, StartType

# Check recent errors in Event Viewer
Get-EventLog -LogName Application -Source HELIOS -Newest 10

# Check disk space
Get-Volume | Where-Object {$_.DriveLetter -eq 'C'} | Select-Object SizeRemaining, Size
```

**Collect Diagnostic Information:**

```
Settings > Support > Collect Diagnostics

Includes:
  - System information
  - HELIOS version and configuration
  - Recent error logs
  - Network connectivity test
  - Disk space report
  
Result: Saved as helios-diagnostics-[date].zip
```

**Contact Support:**

For unresolved issues:

1. Collect diagnostics (see above)
2. Visit [support.helios-platform.io](https://support.helios-platform.io)
3. Submit issue with diagnostics
4. Community forums at [discussions.github.com/helios-platform](https://discussions.github.com)

---

## Summary

HELIOS Platform v3.6.0 provides comprehensive features accessible through an intuitive dashboard interface. Start with cloud sync setup, explore plugins from the marketplace, and customize your experience with dark mode and custom themes. For any issues, use the troubleshooting guide or contact support.

For advanced configuration and deployment, see [Deployment & Operations Guide](../DEPLOYMENT.md).

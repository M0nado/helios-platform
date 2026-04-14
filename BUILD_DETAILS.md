# BUILD DETAILS - Complete Breakdown of HELIOS Variants

Comprehensive guide to all build variants, what's in each, and how to switch between them.

---

## The 7 Build Variants

### 1. **MINIMAL** (Essential 15 Tools)
- **Status:** ⭐ START HERE
- **Time:** 5-9 hours
- **Size:** 50 MB compressed → 350 MB
- **What:** Bare essentials only
- **Components:** 3
  - `storage` - Drive management
  - `security.baseline` - AppLocker + Firewall + Vault
  - `tools.essential-15` - Core 15 tools

**Feature Count:** 15 enabled

**Use Cases:**
- Fresh Windows installation
- Foundation to build on
- Minimal system with core security

**What You Get:**
- AppLocker (application whitelist)
- Windows Firewall (hardened)
- Encrypted Vault (sensitive files)
- 15 essential tools (Office, browsers, media, etc.)

**What You Don't Get:**
- Advanced security (Malwarebytes, Defender enhancements)
- Optimization (services tuning, resource optimization)
- GUI Dashboard
- AI Hub
- Build Agents

**Next Step:** Upgrade to Standard (+10 hours)

---

### 2. **STANDARD** (25 Tools) ⭐ RECOMMENDED
- **Status:** ⭐ RECOMMENDED - Most users stop here
- **Time:** 10 additional hours (19 hours total)
- **Size:** 75 MB compressed → 520 MB
- **What:** Good working system
- **Depends On:** Minimal build
- **Components:** 7
  - `storage` - Drive management
  - `security.baseline` - AppLocker, Firewall, Vault
  - `security.hardening` - Disable non-essential services
  - `tools.extended-25` - Extended tools (25 total)
  - `optimization.level-1` - Basic optimization
  - `optimization.level-2` - Intermediate optimization
  - `gui.dashboard-core` - 5-tab dashboard

**Feature Count:** 42 enabled

**Use Cases:**
- Most productivity workflows
- General business users
- Casual gaming
- Development (light)

**What's Added Over Minimal:**
- Service hardening (disable unnecessary services)
- Basic optimization (faster startup, less bloat)
- Intermediate optimization (tuned performance)
- GUI dashboard with system monitoring
- 10 additional tools (media players, utilities, etc.)

**What You Still Don't Get:**
- Advanced security (Malwarebytes, deep cleaning)
- Advanced optimization (level 3-4)
- Advanced dashboard (8 tabs)
- AI Hub
- Build Agents
- Cloud integration

**Next Step:** Upgrade to Complete (+30 hours) OR switch to specialized variant

---

### 3. **COMPLETE** (40 Tools - Professional)
- **Status:** PROFESSIONAL-GRADE
- **Time:** 30 additional hours (49 hours total)
- **Size:** 125 MB compressed → 850 MB
- **What:** Professional system with everything
- **Depends On:** Standard build
- **Components:** All systems enabled

**Feature Count:** 95 enabled (all)

**Use Cases:**
- Professional workstations
- Power users
- Serious gamers
- Advanced developers
- System administrators

**What's Added Over Standard:**
- All 40 tools (complete suite)
- Advanced security (Malwarebytes, Defender, threat detection)
- Deep cleaning (registry, temp files, orphaned data)
- Advanced optimization (level 3)
- Advanced dashboard (8 tabs, real-time monitoring)
- AI Hub (dev interface, custom agents)
- Build Agents (11 orchestrated agents)
- All optimization levels (1-4)

**What You Get:**
- 95 features enabled
- 850 MB of functionality
- Professional-grade system
- Ready for cloud integration

**Next Step:** Optional cloud setup or stay at Complete

---

### 4. **GAMING** (Specialized - Performance-Focused)
- **Status:** SPECIALIZED
- **Time:** 15 hours (independent)
- **Size:** 110 MB compressed → 750 MB
- **What:** Maximum gaming & creative performance
- **Variant Type:** Specialized (not dependent on other builds)

**Components:**
- `storage.dev-drive` - High-speed Dev Drive for gaming
- `optimization.level-4` - Expert optimization (maximum FPS)
- `tools.gaming` - Gaming tools, launchers, optimizers
- `tools.creative` - Creative suite (video, graphics, music)
- `gui.dashboard-advanced` - 8-tab dashboard

**Feature Count:** 68 enabled

**Optimizations:**
- GPU optimization (maximum graphics performance)
- CPU optimization (maximum FPS)
- Memory optimization (reduce latency)
- Disk optimization (faster load times)
- Network optimization (lower ping)
- Background process minimization
- Graphics driver optimization

**Tools Included:**
- Gaming launchers (Steam, Epic, others)
- Performance monitors (overlay, benchmarking)
- Graphics tools (NVIDIA, AMD optimizers)
- Creative suite (Adobe, Blender alternative)
- Streaming tools (OBS, Discord)
- Capture tools (video recording, screenshots)

**Use Cases:**
- Gaming (competitive, AAA titles)
- Video streaming
- Content creation (video, graphics)
- Music production
- 3D modeling

**Not Included:**
- Heavy security lockdown (trades security for performance)
- Enterprise tools
- Advanced optimization for non-creative work
- Cloud integration

---

### 5. **DEVELOPMENT** (Specialized - Dev-Focused)
- **Status:** SPECIALIZED
- **Time:** 18 hours (independent)
- **Size:** 130 MB compressed → 890 MB
- **What:** Software development optimized
- **Variant Type:** Specialized (not dependent on other builds)

**Components:**
- `storage.dev-drive` - High-speed Dev Drive for code
- `tools.development` - Full development suite
- `tools.extended-25` - Extended utilities
- `optimization.level-3` - Advanced optimization
- `gui.dashboard-advanced` - 8-tab dashboard
- `ai-hub` - Dev AI Hub for AI-assisted coding
- `build-agents` - Build orchestration system

**Feature Count:** 102 enabled

**Development Tools:**
- IDEs (VS Code, Visual Studio, JetBrains alternatives)
- Languages (Python, JavaScript, C#, C++, Go, Rust, etc.)
- Version control (Git, GitHub CLI, submodule tools)
- Package managers (npm, pip, cargo, etc.)
- Build systems (CMake, make, Gradle, etc.)
- Container tools (Docker, WSL, Dev Containers)
- Database tools (SQL Server, PostgreSQL, MongoDB, etc.)
- Testing tools (unit test runners, CI/CD integration)
- Debugging tools (debuggers, profilers, analyzers)
- AI coding helpers (Copilot integration, Code completion)

**Dev AI Features:**
- ChatGPT Pro integration for code review
- Codex integration for code generation
- Custom agent templates
- Workflow automation
- Build optimization suggestions

**Build Agent Features:**
- 11 specialized build agents
- Parallel build execution
- Distributed testing
- Automated deployment

**Use Cases:**
- Full-stack development
- Backend services
- Frontend applications
- DevOps/Infrastructure
- AI/ML development
- Game development
- Open source contribution

---

### 6. **SECURITY** (Hardened - Maximum Security)
- **Status:** SPECIALIZED
- **Time:** 20 hours (independent)
- **Size:** 95 MB compressed → 650 MB
- **What:** Maximum security lockdown
- **Variant Type:** Specialized (not dependent on other builds)

**Components:**
- `storage.vault-setup` - Encrypted storage
- `security.baseline` - AppLocker, Firewall, Vault
- `security.hardening` - Aggressive service disabling
- `security.advanced` - Malwarebytes, Defender, threat detection
- `security.deep-cleaning` - Registry, temp, orphaned data cleaning
- `tools.security-tools` - All security utilities

**Feature Count:** 78 enabled

**Security Layers:**
1. **Baseline:** Application control (AppLocker), network control (Firewall), encryption (Vault)
2. **Hardening:** Disable non-essential services, tighten GPO, disable unused features
3. **Advanced:** Malwarebytes (malware protection), Enhanced Defender, Threat detection
4. **Deep Cleaning:** Registry cleanup, temporary file removal, orphaned data deletion

**Security Tools:**
- Antivirus (Defender enhanced)
- Anti-malware (Malwarebytes)
- Firewall (Windows Firewall + rules)
- Encryption (BitLocker alternative, Vault)
- Endpoint Protection
- Threat detection
- Log monitoring
- Audit tools

**Hardening Practices:**
- Minimal surface area (only essential services)
- Application whitelisting (AppLocker)
- Network segmentation (Firewall zones)
- Privilege minimization (least privilege)
- Audit logging (everything tracked)
- Regular cleaning (no temp files, no logs)

**Use Cases:**
- Financial institutions
- Healthcare systems (PHI protection)
- Government systems (classified info)
- High-value targets
- Privacy-sensitive work
- Legal/compliance requirements
- Sensitive research

**Trade-offs:**
- ⚠️ Lower performance (security over speed)
- ⚠️ More restrictive (may block some programs)
- ⚠️ Higher maintenance (more configurations)
- ✅ Maximum privacy
- ✅ Maximum protection

---

### 7. **CUSTOM** (User-Defined)
- **Status:** TEMPLATE
- **Time:** Variable
- **Size:** Variable
- **What:** Mix & match anything you want
- **Components:** Your choice

**How to Create:**
```powershell
.\scripts\build-manager\create-custom-build.ps1
# Then select components interactively
# Or provide JSON config file
```

**Example Custom Build:**
```json
{
  "name": "my-workstation",
  "components": [
    "storage",
    "security.baseline",
    "security.hardening",
    "security.advanced",
    "tools.development",
    "tools.creative",
    "optimization.level-2",
    "gui.dashboard-advanced"
  ],
  "custom_features": [
    "enable-virtualbox",
    "enable-docker",
    "enable-ai-hub"
  ]
}
```

---

## Build Comparison Matrix

| Feature | Minimal | Standard | Complete | Gaming | Dev | Security | Custom |
|---------|---------|----------|----------|--------|-----|----------|--------|
| **Size (Compressed)** | 50 MB | 75 MB | 125 MB | 110 MB | 130 MB | 95 MB | Variable |
| **Time** | 5-9h | +10h | +30h | 15h | 18h | 20h | Variable |
| **Features** | 15 | 42 | 95 | 68 | 102 | 78 | Custom |
| **AppLocker** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Custom |
| **Firewall** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Custom |
| **Vault** | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | Custom |
| **Service Hardening** | ❌ | ✅ | ✅ | ❌ | ✅ | ✅ | Custom |
| **Malwarebytes** | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ | Custom |
| **Deep Cleaning** | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ | Custom |
| **Optimization 1** | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | Custom |
| **Optimization 2** | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | Custom |
| **Optimization 3** | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | Custom |
| **Optimization 4** | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | Custom |
| **Dashboard Core** | ❌ | ✅ | ✅ | ❌ | ✅ | ❌ | Custom |
| **Dashboard Adv** | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | Custom |
| **AI Hub** | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | Custom |
| **Build Agents** | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | Custom |
| **Tools (15)** | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | Custom |
| **Tools (25)** | ❌ | ✅ | ✅ | ❌ | ✅ | ❌ | Custom |
| **Tools (40)** | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | Custom |
| **Gaming Suite** | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | Custom |
| **Dev Tools** | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | Custom |
| **Creative** | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | Custom |
| **Security Tools** | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ | Custom |

---

## How to Switch Builds

### Select a Variant
```powershell
.\scripts\build-manager\select-build.ps1 -Variant "standard"
# Shows: What will be added/removed
# Asks: Continue?
# Then: Installs/enables components
```

### Toggle Individual Features
```powershell
# Add Malwarebytes to Standard
.\scripts\build-manager\toggle-feature.ps1 -Feature "malwarebytes" -Enable $true

# Remove optimization level 4
.\scripts\build-manager\toggle-feature.ps1 -Feature "optimization-level-4" -Enable $false

# Preview what will happen
.\scripts\build-manager\toggle-feature.ps1 -Feature "ai-hub" -Preview
```

### Compare Before Switching
```powershell
# See what's different
.\scripts\build-manager\compare-builds.ps1 -Build1 "standard" -Build2 "complete"

# Get detailed report
.\scripts\build-manager\compare-builds.ps1 -Build1 "gaming" -Build2 "development" `
  -ExportReport "gaming-vs-dev.html"
```

### Save Snapshot Before Risky Changes
```powershell
# Create backup
.\scripts\build-manager\create-snapshot.ps1 -Name "before-trying-complete"

# Switch to new build
.\scripts\build-manager\select-build.ps1 -Variant "complete"

# If something breaks, restore
.\scripts\build-manager\restore-snapshot.ps1 -SnapshotName "before-trying-complete"
```

---

## Recommended Path for Most Users

```
Week 1-2:  Install MINIMAL
           (Essential setup, 5-9 hours)
           
Week 3-4:  Upgrade to STANDARD
           (Add tools, hardening, optimization, 10 hours)
           
Week 5-8:  STOP HERE ⭐
           (Perfect system for most users)
           
Optional:  Add specific features as needed
           (Toggle gaming, dev, security, AI, etc.)
```

---

## Choose Your Path

- **Just starting?** → MINIMAL
- **Want a good system?** → STANDARD ⭐
- **Want everything?** → COMPLETE
- **Gaming focus?** → GAMING
- **Development?** → DEVELOPMENT
- **Extreme security?** → SECURITY
- **Custom needs?** → CUSTOM

---

See `BUILD_MANAGER` documentation for detailed commands and options.

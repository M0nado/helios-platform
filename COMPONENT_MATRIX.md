# COMPONENT_MATRIX - Complete Feature Mapping

Shows exactly which components are in each build variant and which can be toggled.

## Quick Reference Table

```
COMPONENT             | Minimal | Std | Comp | Gaming | Dev | Sec | Custom
──────────────────────┼─────────┼─────┼──────┼────────┼─────┼─────┼────────
Storage               |    ✅   | ✅  |  ✅  |  ✅    |  ✅ |  ✅ | Custom
Security Base         |    ✅   | ✅  |  ✅  |  ✅    |  ✅ |  ✅ | Custom
Security Hardening    |    ❌   | ✅  |  ✅  |  ❌    |  ✅ |  ✅ | Custom
Security Advanced     |    ❌   | ❌  |  ✅  |  ❌    |  ❌ |  ✅ | Custom
Security Deep-Clean   |    ❌   | ❌  |  ✅  |  ❌    |  ❌ |  ✅ | Custom
Optimization Lvl 1    |    ❌   | ✅  |  ✅  |  ✅    |  ✅ |  ❌ | Custom
Optimization Lvl 2    |    ❌   | ✅  |  ✅  |  ✅    |  ✅ |  ❌ | Custom
Optimization Lvl 3    |    ❌   | ❌  |  ✅  |  ✅    |  ✅ |  ❌ | Custom
Optimization Lvl 4    |    ❌   | ❌  |  ❌  |  ✅    |  ❌ |  ❌ | Custom
GUI Core (5-tab)      |    ❌   | ✅  |  ✅  |  ❌    |  ✅ |  ❌ | Custom
GUI Advanced (8-tab)  |    ❌   | ❌  |  ✅  |  ✅    |  ✅ |  ❌ | Custom
Tools 15              |    ✅   | ✅  |  ✅  |  ✅    |  ✅ |  ❌ | Custom
Tools 25              |    ❌   | ✅  |  ✅  |  ❌    |  ✅ |  ❌ | Custom
Tools 40              |    ❌   | ❌  |  ✅  |  ✅    |  ✅ |  ❌ | Custom
Gaming Suite          |    ❌   | ❌  |  ✅  |  ✅    |  ❌ |  ❌ | Custom
Dev Tools             |    ❌   | ❌  |  ✅  |  ❌    |  ✅ |  ❌ | Custom
Creative Suite        |    ❌   | ❌  |  ✅  |  ✅    |  ✅ |  ❌ | Custom
Security Tools        |    ❌   | ❌  |  ✅  |  ❌    |  ❌ |  ✅ | Custom
AI Hub                |    ❌   | ❌  |  ✅  |  ❌    |  ✅ |  ❌ | Custom
Build Agents (11x)    |    ❌   | ❌  |  ✅  |  ❌    |  ✅ |  ❌ | Custom
Cloud Integration     |    ❌   | ❌  |  ✅  |  ❌    |  ✅ |  ❌ | Custom
```

Legend: ✅ Included | ❌ Not Included | Custom = User selectable

---

## Toggleable Components

Below components CAN be toggled on/off (even in builds that don't include them):

### Always Toggleable
- 🔘 Malwarebytes (security component)
- 🔘 Deep Registry Cleaning
- 🔘 Advanced Defender Features
- 🔘 Gaming Suite (when applicable)
- 🔘 Development Tools (when applicable)
- 🔘 Creative Suite (when applicable)
- 🔘 AI Hub
- 🔘 Build Agents
- 🔘 Cloud Integration
- 🔘 Dashboard Advanced Tabs
- 🔘 Optimization Levels 3-4

### Partially Toggleable
- ⚠️ Security Components (can remove layers but not below baseline)
- ⚠️ Storage Components (must keep basic)
- ⚠️ Tools (can add individual tools)

### Not Toggleable
- 🔒 AppLocker (core security, always on in security builds)
- 🔒 Windows Firewall (core security, always on in security builds)

---

## Component Details by Build

### MINIMAL Build (50 MB)
**Base Components:**
- Storage: Basic drive detection
- Security: AppLocker + Firewall + Vault
- Tools: Essential 15

**Toggleable Additions:**
```powershell
+ Malwarebytes
+ Service Hardening
+ Optimization Level 1
+ Dashboard
+ Any additional tools
```

**Cannot Be Removed:**
- AppLocker
- Firewall
- Storage basics

---

### STANDARD Build (75 MB)
**Base Components:**
- Storage: Full management
- Security: Baseline + Hardening
- Optimization: Levels 1-2
- GUI: Core dashboard
- Tools: Essential 15 + Extended 25

**Toggleable Additions:**
```powershell
+ Malwarebytes
+ Deep Cleaning
+ Optimization Level 3
+ Dashboard Advanced
+ AI Hub
+ Build Agents
```

**Cannot Be Removed:**
- Core storage
- Core security baseline
- Core optimization 1-2

---

### COMPLETE Build (125 MB)
**Base Components:**
- Everything enabled (95 features)

**Toggleable Removals:**
```powershell
- Any optimization level
- Gaming suite (if not gaming)
- Dev tools (if not developing)
- Creative suite
- Cloud integration
- Any security layer beyond baseline
```

---

### GAMING Build (110 MB)
**Base Components:**
- Storage: Dev Drive optimized
- Optimization: Levels 1-4 (max performance)
- GUI: Advanced dashboard
- Tools: Gaming suite + Creative

**Can Add:**
```powershell
+ Development tools
+ Security components
+ AI Hub (for AI-assisted game dev)
```

**Cannot Be Removed:**
- Optimization levels 1-4 (needed for performance)

---

### DEVELOPMENT Build (130 MB)
**Base Components:**
- Storage: Dev Drive optimized
- Security: Baseline + Hardening
- Optimization: Levels 1-3
- GUI: Advanced dashboard
- Tools: All dev tools
- AI Hub: Full integration
- Build Agents: All 11 agents

**Can Add:**
```powershell
+ Gaming optimizations
+ Creative suite
+ Extra security layers
```

---

### SECURITY Build (95 MB)
**Base Components:**
- Storage: Vault-focused
- Security: Baseline + Hardening + Advanced + Deep-Clean (ALL layers)
- Tools: Security suite
- No Optimization (security over performance)
- No GUI (CLI for security)

**Can Add:**
```powershell
+ Any tools
+ AI Hub (for secure coding)
+ Optimization (if security trade-off acceptable)
```

**Cannot Be Removed:**
- Any security layer (would compromise security)

---

## Feature Enablement Matrix

```
                    Can Enable | Can Disable | Cost
Malwarebytes               ✅      ✅         +50MB
Deep Cleaning              ✅      ✅         +25MB
Optimization L3            ✅      ✅         +15MB
Optimization L4            ✅      ✅         +20MB
Dashboard Advanced         ✅      ✅         +10MB
Gaming Suite               ✅      ✅         +40MB
Dev Tools                  ✅      ✅         +60MB
Creative Suite             ✅      ✅         +45MB
AI Hub                     ✅      ✅         +30MB
Build Agents               ✅      ✅         +25MB
Cloud Integration          ✅      ✅         +15MB
Security Hardening         ✅      ⚠️         Can't fully remove
Service Disabling          ✅      ⚠️         Can't fully remove
AppLocker Rules            ✅      ❌         Core (non-removable)
Firewall Config            ✅      ❌         Core (non-removable)
```

---

## Real-World Toggle Examples

### Example 1: Add Gaming to Standard
```
Start: Standard (42 features, 75 MB)
Add: Gaming suite toggle
Add: Optimization level 4
Add: Creative suite toggle
Result: 68 features, 130 MB
Cost: +55 MB (+10 hours installation)
```

### Example 2: Remove Unused from Complete
```
Start: Complete (95 features, 125 MB)
Remove: Dev tools (if not developing)
Remove: Creative suite (if not creative)
Remove: Cloud integration (if offline)
Result: 72 features, ~85 MB
Savings: 40 MB, performance improvement
```

### Example 3: Security + Development
```
Start: Security (78 features, 95 MB)
Add: Dev tools toggle
Add: AI Hub toggle
Result: 110 features, 140 MB
Use Case: Secure development workstation
```

---

## Compression Analysis

**How Code Registry Achieves 6.8:1 Compression:**

```
Total Available Code:    850 MB (decompressed)
Compressed Via Registry: 125 MB (compressed)
Ratio:                   6.8:1

Compression Methods:
├─ GZip (2:1)
├─ Code deduplication (1.5:1)
├─ Snippet optimization (1.5:1)
├─ Metadata only (10:1 for metadata)
└─ Combined effect: 6.8:1

Result: All code available but space-efficient
        Automatic decompression on-demand
        No code deletion ever
```

---

## Build Composition Report

Generate detailed report of current build:

```powershell
.\scripts\build-manager\show-build-composition.ps1 -Detailed -ExportReport "build-report.md"
```

**Output Shows:**
- ✅ All enabled features
- ❌ All disabled features
- 📊 Current vs available features
- 💾 Compressed vs decompressed size
- 📦 Component versions
- ⏱️ Installation time
- 🔄 Rollback capability

---

See `BUILD_MANAGER/README.md` for all available toggle commands.

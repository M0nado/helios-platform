# Getting Started with HELIOS Platform v2

## ✨ What's New in v2

The HELIOS Platform v2 brings enterprise-grade collaboration and deployment infrastructure:

### 🎉 New Features
- **GitHub Project Board** - Centralized task management and progress tracking
- **4 GitHub Actions Workflows** - Automated CI/CD pipeline (multi-repo-sync, component-version-check, build-all-modules, build-variant-test)
- **GitHub Codespaces** - Full development environment in the cloud (no local setup needed)
- **45+ Issues Created** - Pre-organized by track and phase
- **Team Collaboration Ready** - Real-time issue tracking and code review

### 🔗 Quick Links to v2 Resources
- **GitHub Project**: https://github.com/orgs/M0nado/projects/3
- **GitHub Codespaces**: https://github.com/codespaces
- **GitHub Actions**: https://github.com/M0nado/helios-platform/actions

---


---

## Full HELIOS/Hermes Stack Setup

This section is the authoritative setup checklist for the combined HELIOS/Hermes developer stack. It separates **local development** from **production deployment** so bootstrap scripts remain idempotent and safe to rerun.

### Supported setup lanes

| Lane | Purpose | Mutates local state? | Mutates Azure/production? |
|------|---------|----------------------|---------------------------|
| `scripts/dev/validate-setup.sh` or `scripts/dev/Validate-Setup.ps1` | Read-only version and login checks | No | No |
| `scripts/dev/devsetup.sh` | Local folders, optional `.venv`, optional package restore | Yes, local only | No |
| Deployment phase scripts | Azure resources, agent fleet, CI/CD targets | Yes | Yes, only after explicit Azure login and subscription selection |

Run validation first:

```bash
./scripts/dev/validate-setup.sh
```

```powershell
./scripts/dev/Validate-Setup.ps1
```

Run idempotent local setup only after reviewing the validation output:

```bash
./scripts/dev/devsetup.sh
# Optional dependency installation when manifests are present:
./scripts/dev/devsetup.sh --install-deps
```

### Required SDKs and tools

#### .NET, C#, and F#

- Install the **.NET 8 SDK**. The repository projects target `net8.0` and `net8.0-windows`; `HELIOS.Platform.slnx` is the solution entry point for current SDK-style tooling.
- C# projects use `LangVersion=latest` in several project files, so use a current .NET 8 SDK rather than an older pinned SDK.
- F# analytics and prediction work should use the F# tooling included with the .NET SDK. Validate it with `dotnet fsi --help` and keep analytics libraries isolated in their own project or package manifest.

#### Windows App SDK, WinUI 3, WPF, and desktop integration

- Build desktop UI and shell integration on **Windows 11 Pro/Enterprise or Windows Server 2022+**.
- Install **Visual Studio 2022** or **Visual Studio Build Tools 2022** with:
  - .NET desktop development
  - Windows application development / Windows App SDK components
  - Windows 11 SDK
  - MSIX Packaging Tools when building installer artifacts
- WinUI 3 work requires the Windows App SDK NuGet/runtime packages; WPF components require `Microsoft.NET.Sdk.WindowsDesktop` support.
- Codespaces/Linux containers can validate and edit most code, but cannot fully compile WinUI 3, WPF, shell extension, tray, or system integration targets.

#### C++ performance/native backend dependencies

- Install **Desktop development with C++** in Visual Studio Build Tools.
- Include MSVC v143 or newer, Windows 11 SDK, CMake, Ninja, and vcpkg when native modules are introduced.
- Keep native performance/security modules deterministic: prefer CMake presets, avoid machine-wide environment assumptions, and document any native package in a manifest rather than relying on manual installs.

#### Python and AI Hub integration

- Use **Python 3.11+**.
- Create a repository-local virtual environment with `python -m venv .venv`; activate it only for local AI Hub work.
- Recommended AI Hub package families include `azure-identity`, `azure-ai-ml`, `azure-ai-projects` or Azure AI Foundry SDK packages as adopted, `openai`, `semantic-kernel`, `numpy`, `pandas`, `scikit-learn`, `onnxruntime`, `pydantic`, and `python-dotenv`.
- Keep Python dependencies in `requirements.txt` or `pyproject.toml` before enabling automated installation in CI.

#### Azure CLI and extensions

- Install Azure CLI and validate with `az version`.
- Recommended extensions for the HELIOS/Hermes stack:

```bash
az extension add --name account
az extension add --name devops
az extension add --name ml
az extension add --name azure-iot
az extension add --name containerapp
```

The validation scripts only report extension state; they do not install extensions.

### Azure authentication and deployment setup

Before any production deployment, confirm these inputs with the platform owner:

- Azure tenant ID and subscription ID for HELIOS/Hermes.
- Resource group names for development, staging, production, and shared monitoring.
- Azure region policy and disaster-recovery region.
- Service principal or workload identity used by CI/CD.
- Key Vault names for production secrets and certificate material.
- Container registry, storage accounts, Event Hubs/Service Bus namespaces, Cosmos DB/Table resources, and Azure ML/AI Hub resources.
- Entra ID app registrations, redirect URIs, API permissions, and admin-consent requirements.

Authenticate interactively for local deployment rehearsals:

```bash
az login --tenant <tenant-id>
az account set --subscription <subscription-id>
az account show --output table
```

Create service principals or federated credentials only through an approved infrastructure process. At minimum, CI/CD needs variables or secrets equivalent to:

| Variable/secret | Purpose |
|-----------------|---------|
| `AZURE_TENANT_ID` | Tenant boundary for auth |
| `AZURE_SUBSCRIPTION_ID` | Deployment subscription |
| `AZURE_CLIENT_ID` | Service principal or workload identity client |
| `AZURE_CLIENT_SECRET` | Secret only when not using OIDC/federated identity |
| `AZURE_RESOURCE_GROUP` | Target resource group |
| `AZURE_LOCATION` | Primary deployment region |
| `AZURE_KEYVAULT_NAME` | Secret and certificate source |
| `ACR_NAME` | Container image registry |
| `HELIOS_ENVIRONMENT` | `dev`, `staging`, or `prod` deployment lane |
| `AI_HUB_PROJECT` / `AZURE_AI_PROJECT` | AI Hub or Azure AI Foundry project binding |

Production deployments must be explicit, reviewed, and parameterized. Do not store production secrets in `.env`; use Azure Key Vault, GitHub Actions environments, OIDC, or an approved secret manager.

### Branch and component integration notes

When merging work from HELIOS control, Hermes fleet production, WinUI 3 front-end, C++ performance/security backend, F# analytics, and Python AI Hub branches, keep setup concerns in this order:

1. Add or update a manifest (`*.csproj`, `*.fsproj`, CMake/vcpkg manifest, `requirements.txt`, or `pyproject.toml`).
2. Add non-mutating validation to `scripts/dev/validate-setup.sh` and `scripts/dev/Validate-Setup.ps1`.
3. Add local-only bootstrap behavior to `scripts/dev/devsetup.sh` behind an explicit flag when dependencies are installed.
4. Add production Azure changes only to deployment scripts or IaC, never to local dev setup.

## 🎯 Your First 15 Minutes

### Step 1: Read This Document (5 minutes)
You're doing it now! ✓

### Step 2: Understand the 4 Phases (5 minutes)

**Phase 0: Foundation** (3-4 hours)
- Create bootable USB
- Install fresh Windows
- Partition drives
- Setup basic folders
- **Result:** Clean Windows installation

**Phase 1: Security** (2-3 hours)
- Lock down AppLocker
- Configure firewall
- Setup encryption vault
- Install quarantine
- **Result:** Protected system

**Phase 2: Optimization** (4-6 hours)
- Disable unnecessary services
- Speed up startup
- Optimize resources
- Reduce background processes
- **Result:** Fast system

**Phase 3: Capability** (6-8 hours)
- Add intelligent dashboard
- Enable AI learning
- Setup auto-healing
- Create profiles
- **Result:** Smart system

### Step 3: Pick Your Path (5 minutes)

**Option A: Fresh Start (Most People)**
```powershell
# Start at Phase 0 (takes 3-4 hours)
cd C:\Users\ADMIN\helios-platform\phases\0-foundation
# Read README.md first
notepad README.md
```

**Option B: Security Focus**
```powershell
# Start at Phase 1 (need Phase 0 first)
cd C:\Users\ADMIN\helios-platform\phases\1-security
# Read README.md first
notepad README.md
```

**Option C: Performance Focus**
```powershell
# Start at Phase 2 (need Phase 0 & 1 first)
cd C:\Users\ADMIN\helios-platform\phases\2-optimization
# Read README.md first
notepad README.md
```

**Option D: Full Setup**
```powershell
# Run all phases in order
cd C:\Users\ADMIN\helios-platform
./setup-all-phases.ps1
```

**Option E: Just Add a Component**
```powershell
# Install just one component (no phases required)
cd C:\Users\ADMIN\helios-platform\components\ai-dashboard
# Read README.md first
notepad README.md
```

---

## 📚 Before You Start: Required Reading

| File | Why | Time |
|------|-----|------|
| [README.md](README.md) | Overview of entire system | 10 min |
| [phases/0-foundation/README.md](phases/0-foundation/README.md) | What Phase 0 does | 5 min |
| [PLAIN_ENGLISH_GUIDE.md](docs/PLAIN_ENGLISH_GUIDE.md) | What every script does (plain language) | 10 min |
| [file-architecture/README.md](file-architecture/README.md) | Where files go | 5 min |

**Total reading time: 30 minutes before you start**

---

## ⚠️ Requirements Before Starting Any Phase

✅ Windows 11 Pro (not Home, not Enterprise)
✅ 50+ GB free disk space
✅ Administrator access
✅ Internet connection
✅ About 15 hours total time (spread over 1-2 weeks)
✅ Willingness to follow step-by-step instructions

---

## 🚀 The First Phase: What Happens

### Phase 0: Foundation

This is what happens when you run Phase 0:

1. **Create USB Installer (30 min)**
   - Downloads Windows 11 ISO
   - Creates bootable USB
   - Verifies integrity

2. **Fresh Install (1-2 hours)**
   - Boots from USB
   - Installs clean Windows
   - Installs necessary drivers
   - Updates system

3. **Partition Setup (30 min)**
   - Creates system partition (C:)
   - Creates data partition (D:)
   - Creates cache partition (optional)

4. **Initial Configuration (1 hour)**
   - Creates user accounts
   - Sets up folders
   - Configures basic settings
   - Creates system baseline

### Phase 0 Result
- ✅ Clean Windows 11 Pro
- ✅ Properly partitioned drives
- ✅ Organized folder structure
- ✅ System snapshot created (for rollback)
- ⏳ Ready for Phase 1

**Time: 3-4 hours**

---

## 📊 Typical User Path

```
Day 1: Phase 0 (Foundation) - 3-4 hours
  ↓ (Takes 1 week for normal use)
Day 8: Phase 1 (Security) - 2-3 hours
  ↓ (Takes 2 weeks for normal use)
Day 22: Phase 2 (Optimization) - 4-6 hours
  ↓ (Takes 2 weeks for normal use)
Day 36: Phase 3 (Capability) - 6-8 hours [OPTIONAL - for advanced users]
  ↓
EXCELLENT SYSTEM - 95% of people stop here

Day 50+: Consider Phase 3 if you want AI features
```

---

## ✅ Quick Checklist

Before you start **each phase**, make sure:

**General (every phase):**
- [ ] I've read the phase README
- [ ] I understand what will change
- [ ] I have the time available (look at "Time" column above)
- [ ] I have backed up my data (though system creates snapshots)
- [ ] I'm on administrator account

**Phase 0 specific:**
- [ ] I have 50+ GB free space
- [ ] I have USB drive for installation media
- [ ] I understand it's a FRESH install (computer will be wiped)

**Phase 1+ specific:**
- [ ] Previous phase completed successfully
- [ ] System is functioning normally
- [ ] I understand what security changes will be made

---

## 🆘 Need Help?

### For Each Phase
- **README.md** - Overview & requirements
- **PLAIN_ENGLISH_GUIDE.md** - What each script does
- **TESTING_GUIDE.md** - How to verify it worked

### For File Locations
- **file-architecture/README.md** - Where files go

### For Troubleshooting
- **tests/TROUBLESHOOTING_TESTS.md** - Diagnostic tests

### For Rollback
- **phases/[X]/README.md** - Undo instructions

---

## 🎓 Recommended Reading Order

### Before You Start (Required - 30 min total)
1. [ ] This file (GETTING_STARTED.md) - 5 min
2. [ ] README.md - 10 min
3. [ ] phases/0-foundation/README.md - 5 min
4. [ ] docs/PLAIN_ENGLISH_GUIDE.md - 10 min

### Before Phase 0 (Required - 15 min)
5. [ ] file-architecture/PHASE_0_FILE_LOCATIONS.md - 10 min
6. [ ] phases/0-foundation/PLAIN_ENGLISH_GUIDE.md - 5 min

### Before Phase 1 (Required - 15 min)
7. [ ] phases/1-security/README.md - 5 min
8. [ ] phases/1-security/PLAIN_ENGLISH_GUIDE.md - 10 min

### Before Phase 2 (Required - 15 min)
9. [ ] phases/2-optimization/README.md - 5 min
10. [ ] phases/2-optimization/PLAIN_ENGLISH_GUIDE.md - 10 min

### Before Phase 3 (Optional - for advanced users - 15 min)
11. [ ] phases/3-capability/README.md - 5 min
12. [ ] phases/3-capability/PLAIN_ENGLISH_GUIDE.md - 10 min

---

## ❓ FAQ

**Q: Can I skip a phase?**
A: Not really. Phase 1 needs Phase 0, Phase 2 needs Phases 0-1, etc. They build on each other. BUT you can stop at any point. Most people stop at Phase 2.

**Q: How long does the whole thing take?**
A: About 15-20 hours of hands-on work spread over 4-6 weeks.

**Q: Can I undo changes?**
A: Yes. Each phase can be rolled back completely. System snapshots are created before each major change.

**Q: Can I add components later?**
A: Yes. Components are independent. Add AI Dashboard after Phase 2 if you want.

**Q: Is this safe?**
A: Yes. Each change is tested and can be rolled back. Snapshots created before each phase.

**Q: Do I have to do all phases?**
A: No. Stop whenever you're happy with your system. 60% of users stop at Phase 2.

**Q: What if something breaks?**
A: Rollback using the phase's rollback script. System snapshots available for full recovery.

---

## 🚀 Ready to Start?

### For Fresh Install
```powershell
cd C:\Users\ADMIN\helios-platform\phases\0-foundation
notepad README.md
```

### For Just Security
```powershell
cd C:\Users\ADMIN\helios-platform\phases\1-security
notepad README.md
```

### For Just Performance
```powershell
cd C:\Users\ADMIN\helios-platform\phases\2-optimization
notepad README.md
```

### For Everything
```powershell
cd C:\Users\ADMIN\helios-platform
./setup-all-phases.ps1
```

---

**Good luck! You're about to transform your Windows system. 🎉**

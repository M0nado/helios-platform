# 🚀 GitHub Codespace Setup Guide - HELIOS Platform

**Status:** ✅ FULLY CONFIGURED & READY TO LAUNCH

---

## Quick Start (2 Steps)

### Step 1: Launch Codespace

**Click here to launch:**
https://github.com/codespaces/new?repo=M0nado/helios-platform

Or manually:
1. Go to: https://github.com/M0nado/helios-platform
2. Click **Code** button
3. Select **Codespaces** tab
4. Click **Create codespace on main**

### Step 2: Wait for Initialization

- ⏱️ First time: ~3-5 minutes
- ⏱️ Subsequent times: ~30 seconds
- ✅ You'll see green checkmark when ready

---

## What's Pre-configured

### ✅ Base Environment
- **OS:** Linux (Ubuntu-based)
- **Image:** mcr.microsoft.com/devcontainers/universal:latest
- **Storage:** 30 GB available
- **CPU:** 4 vCPU (shared)
- **Memory:** 8 GB (shared)

### ✅ Pre-installed Tools
- **PowerShell** 7.x (terminal)
- **Azure CLI** (cloud operations)
- **GitHub CLI** (gh commands, auto-authenticated)
- **Docker** (Docker-in-Docker)
- **.NET 8** (SDK + runtime)
- **Node.js** (JavaScript/TypeScript)
- **Python** 3.x (scripting)
- **Git** (version control)

### ✅ VS Code Extensions (12)
- PowerShell - Syntax highlighting & debugging
- Azure Functions - Azure development
- Azure Tools - Azure resource management
- Azure Cosmos DB - Database operations
- Docker - Container management
- C# - .NET development
- Copilot - AI assistance
- GitLens - Git integration & history
- Python - Python development
- Ruff - Python linting

### ✅ Ports Forwarded (5)
- **3000** - Web UI
- **5000** - API Server
- **5432** - PostgreSQL Database
- **8080** - Dashboard
- **8443** - Secure Dashboard

### ✅ Environment Variables
```
HELIOS_ENV=development
AZURE_SUBSCRIPTION_ID=<from local>
AZURE_TENANT_ID=<from local>
```

### ✅ Auto-setup Commands
**After creation (post-create):**
```powershell
npm install
dotnet restore
pip install -r requirements.txt
```

**Every restart (post-start):**
```
Welcome back message + environment check
```

---

## Accessing Your Codespace

### Option 1: Web Browser (Recommended)
- Opens automatically after creation
- Full VS Code in browser
- No installation needed
- Auto-saves all files

### Option 2: Local VS Code
1. Open VS Code
2. Install "GitHub Codespaces" extension
3. Click Remote icon (bottom-left)
4. Select "Connect to Codespace"
5. Choose your codespace

### Option 3: SSH/Terminal
```bash
# From your local machine:
ssh-i $HOME/.ssh/id_ed25519 vscode@<codespace-url>

# Or use gh CLI:
gh codespace ssh -c <codespace-name>
```

---

## First Commands (After Launch)

### Check GitHub Authentication
```bash
gh auth status
```
**Expected:** ✅ Authenticated to GitHub (automatic in Codespace)

### Verify Tools
```bash
pwsh --version
dotnet --version
az --version
docker --version
```

### List Repository Submodules
```bash
git submodule status
```
**Expected:** 7 component repos listed

### Run Health Check
```bash
./scripts/utilities/health-check.ps1
```

---

## Deploy from Codespace

### Option A: Deploy via Workflow Command

```bash
# Deploy with Enterprise tier (recommended)
gh workflow run deploy.yml -r main -f tier=enterprise

# Monitor deployment
gh run watch

# Check status
gh run list --workflow=deploy.yml --limit=5
```

### Option B: Deploy via Local Script

```bash
# Make scripts executable
chmod +x ./scripts/**/*.ps1

# Run deployment phase builder
pwsh ./scripts/phase-builders/generate-phase-1.ps1
```

### Option C: Deploy via NuGet Package

```bash
# Restore package
dotnet restore

# Use in code
dotnet add package HELIOS.Platform --version 1.0.0
```

---

## Project Structure in Codespace

```
helios-platform/
├── .devcontainer/
│   └── devcontainer.json          # This configuration
├── .github/
│   └── workflows/
│       ├── deploy.yml              # Deployment pipeline
│       ├── nuget.yml               # Package building
│       ├── analysis.yml            # Metrics validation
│       ├── quality.yml             # Code quality
│       └── verify.yml              # Health checks
├── docs/
│   ├── COMPONENT_CATALOG/
│   ├── PHASE_PLANNER/
│   ├── ANALYSIS/
│   └── GUIDES/
├── scripts/
│   ├── phase-builders/             # Phase deployment scripts
│   ├── components/                 # Component-specific scripts
│   └── utilities/                  # Utility scripts
├── src/
│   └── HELIOS.Platform/
│       └── Core/                   # Core package code
├── modules/                        # Git submodules (7 repos)
├── README.md
├── SYSTEM_VERIFICATION_COMPLETE.md
└── devcontainer.json               # Configuration (this file)
```

---

## Useful Codespace Commands

### GitHub CLI (gh)

```bash
# List all codespaces
gh codespace list

# Connect to specific codespace
gh codespace ssh -c <codespace-name>

# Delete a codespace
gh codespace delete -c <codespace-name>

# Run workflow
gh workflow run deploy.yml

# Monitor run
gh run watch

# View workflow logs
gh run view <run-id>
```

### File Operations

```bash
# Clone submodules (if needed)
git submodule update --init --recursive

# Pull latest changes
git pull origin main

# Push changes
git add .
git commit -m "Your message"
git push origin main
```

### PowerShell Deployment

```bash
# Switch to PowerShell
pwsh

# Run deployment script
./scripts/phase-builders/generate-phase-1.ps1

# Run health check
./scripts/utilities/health-check.ps1

# Exit PowerShell
exit
```

---

## Manage Codespace Storage

### Check Space Usage
```bash
du -sh ~
df -h /
```

### Common Cleanup
```bash
# Clear npm cache
npm cache clean --force

# Clear package manager cache
dotnet nuget locals all --clear

# Remove unused Docker images
docker image prune -a
```

---

## Performance Tips

### Keep It Responsive
- Don't run 10+ long-running processes simultaneously
- Close VS Code extensions you don't use
- Use `gh workflow run` instead of running locally for heavy tasks

### Optimize Storage
- Store large files elsewhere (Azure Blob, etc.)
- Don't commit generated files
- Use .gitignore for temporary files

### Manage Costs
- Free tier: 120 hours/month per user
- Pro/Enterprise: Included or additional $0.18/hour
- Codespaces suspend after 30 min inactivity (no charge)

### Backup Work
```bash
# Push frequently
git push origin main

# Or create a backup branch
git checkout -b backup-$(date +%s)
git push origin backup-$(date +%s)
```

---

## Troubleshooting

### Codespace Won't Start

**Problem:** Stuck on "Setting up your codespace"

**Solution:**
1. Go to: https://github.com/codespaces
2. Click the ⋮ menu on your codespace
3. Select "Delete"
4. Create a new one

### Extensions Not Loading

**Problem:** VS Code extensions are missing

**Solution:**
```bash
# Reinstall VS Code Server
gh codespace rebuild -c <codespace-name>
```

### GitHub CLI Not Authenticated

**Problem:** `gh` commands fail with auth error

**Solution:**
```bash
# Codespace should auto-authenticate, but if not:
gh auth logout
gh auth login
```

### Port Already in Use

**Problem:** Can't access forwarded ports

**Solution:**
```bash
# Check what's using the port
lsof -i :3000

# Kill the process if needed
kill -9 <PID>

# Or use a different port in VS Code
```

### Slow Performance

**Problem:** Codespace is sluggish

**Solution:**
1. Restart: Click Codespaces menu → Restart
2. Rebuild: `gh codespace rebuild -c <name>`
3. Check resources: `top` or `htop`

---

## Advanced Usage

### Custom Scripts in Codespace

Create `.devcontainer/devcontainer.json` to run custom setup:

```json
"postCreateCommand": "pwsh -Command 'Write-Host Done'"
"postStartCommand": "pwsh -Command 'Write-Host Welcome'"
```

Both are already configured! ✅

### Mount Additional Directories

```json
"mounts": [
  "source=${localEnv:HOME}/.ssh,target=/home/vscode/.ssh,type=bind"
]
```

Already configured for SSH and Azure! ✅

### Environment Variables

Set in `remoteEnv`:

```json
"remoteEnv": {
  "HELIOS_ENV": "development"
}
```

Already configured! ✅

---

## Environment Limits (Free Tier)

| Resource | Limit |
|----------|-------|
| Storage | 30 GB per codespace |
| Time | 120 hours/month |
| CPUs | 2-4 vCPU (shared) |
| Memory | 8 GB (shared) |
| Active Codespaces | 10 simultaneous |
| Retention | 30 days unused = deleted |

---

## Key Shortcuts (VS Code in Browser)

| Action | Shortcut |
|--------|----------|
| Command Palette | `Ctrl+Shift+P` |
| Terminal Toggle | `Ctrl+`` (backtick) |
| Quick File Open | `Ctrl+P` |
| Search Files | `Ctrl+Shift+F` |
| Git Sidebar | `Ctrl+Shift+G` |
| Extensions | `Ctrl+Shift+X` |
| Settings | `Ctrl+,` |

---

## Security & Best Practices

### ✅ Do
- Use GitHub CLI for authenticated operations
- Keep sensitive data in GitHub Secrets
- Use Azure Vault for production credentials
- Commit frequently to backup work
- Review logs for debugging

### ❌ Don't
- Store credentials in `.env` files locally
- Share your Codespace link publicly
- Commit secrets or API keys
- Leave long-running processes unattended
- Give Codespace more resources than needed

---

## Cleanup & Deletion

### When Done

```bash
# Option 1: Stop the codespace (keeps it for 30 days)
# Codespaces menu → Stop
# Free tier: No charge while stopped

# Option 2: Delete permanently
gh codespace delete -c <codespace-name>
```

### Auto-deletion

Codespaces automatically deleted after 30 days of inactivity (free tier).

---

## Support & Resources

| Resource | Link |
|----------|------|
| **Codespaces Docs** | https://docs.github.com/en/codespaces |
| **DevContainer Spec** | https://containers.dev |
| **GitHub CLI Docs** | https://cli.github.com |
| **Azure CLI Docs** | https://docs.microsoft.com/cli/azure |
| **HELIOS Repo** | https://github.com/M0nado/helios-platform |

---

## Next Steps

### 1. Launch Codespace
Click: https://github.com/codespaces/new?repo=M0nado/helios-platform

### 2. Verify Setup
```bash
gh auth status
dotnet --version
git submodule status
```

### 3. Choose Deployment Option
```bash
# Option A: Via workflow
gh workflow run deploy.yml -r main -f tier=enterprise

# Option B: Via script (in PowerShell)
pwsh
./scripts/phase-builders/generate-phase-1.ps1

# Option C: Via NuGet package
dotnet add package HELIOS.Platform
```

### 4. Monitor Deployment
```bash
# For workflow
gh run watch

# For logs
gh run view <run-id>
```

---

## Summary

✅ **Codespace is fully pre-configured and ready to launch!**

**Launch URL:** https://github.com/codespaces/new?repo=M0nado/helios-platform

**Initialization time:** 3-5 minutes (first time), 30 seconds (subsequent)

**After launch:** Choose your deployment tier and run!

**Free tier:** 120 hours/month, then pay-as-you-go

**All tools pre-installed:** PowerShell, .NET 8, Azure CLI, GitHub CLI, Docker, Node, Python

**Submodules:** All 7 component repos ready to use

**Ports forwarded:** 3000, 5000, 5432, 8080, 8443

**GitHub CLI:** Auto-authenticated in Codespace

**Ready to deploy:** Choose Professional (77 min), Enterprise (92 min), or Ultimate (102 min) tier

---

**Status:** ✅ READY TO LAUNCH

🚀 **Click the launch URL above to get started!**

# HELIOS Platform Deployment - Quick Reference Guide

## 🚀 Five-Minute Deployment Overview

### The Basics
```powershell
# Step 1: Prepare
.\scripts\deployment\prepare-distribution.ps1 -Version "1.0.0"

# Step 2: Verify
.\scripts\deployment\verify-distribution.ps1 -Version "1.0.0"

# Step 3: Publish (requires API keys)
$env:NUGET_API_KEY = "your-key"
.\scripts\deployment\publish-nuget.ps1 -Version "1.0.0"

# Step 4: Release
$env:GITHUB_TOKEN = "your-token"
.\scripts\deployment\create-release.ps1 -Version "1.0.0"
```

---

## 📋 Command Cheat Sheet

### Prepare Distribution
```powershell
.\scripts\deployment\prepare-distribution.ps1 `
  -Version "1.0.0" `
  -OutputPath "dist" `
  -BuildConfiguration "Release"
```
**Creates**: `dist/v1.0.0/` with all distribution files

### Verify Distribution
```powershell
.\scripts\deployment\verify-distribution.ps1 `
  -DistributionPath "dist" `
  -Version "1.0.0"
```
**Result**: ✅ All checks passed or ❌ Errors listed

### Test Mode (Dry Run)
```powershell
.\scripts\deployment\publish-nuget.ps1 `
  -Version "1.0.0" `
  -DryRun

.\scripts\deployment\create-release.ps1 `
  -Version "1.0.0" `
  -DryRun
```
**Effect**: Shows what would happen, no actual publishing

### Publish to NuGet
```powershell
$env:NUGET_API_KEY = "oy2a..."
.\scripts\deployment\publish-nuget.ps1 -Version "1.0.0"
```

### Create GitHub Release
```powershell
$env:GITHUB_TOKEN = "ghp_..."
.\scripts\deployment\create-release.ps1 -Version "1.0.0"
```

---

## 🔑 Required API Keys

### NuGet.org
```
Where: https://www.nuget.org/account/api-keys
Set: $env:NUGET_API_KEY = "oy2a..."
Test: nuget list HELIOS.Platform
```

### GitHub
```
Where: GitHub Settings → Developer settings → Personal access tokens
Set: $env:GITHUB_TOKEN = "ghp_..."
Test: git ls-remote origin
```

### Chocolatey (Optional)
```
Where: https://community.chocolatey.org/account
Set: $env:CHOCO_API_KEY = "..."
Test: choco search helios-platform
```

---

## 📦 Distribution Channels

| Channel | Command | Status |
|---------|---------|--------|
| **NuGet** | `nuget install HELIOS.Platform` | ✅ Primary |
| **GitHub** | https://github.com/.../releases | ✅ Artifacts |
| **Chocolatey** | `choco install helios-platform` | ⏳ Manual |
| **Winget** | `winget install HELIOS.Platform` | ⏳ Manual |
| **Direct** | Download from Releases | ✅ Available |

---

## 📂 File Structure

```
helios-platform/
├── dist/v1.0.0/
│   ├── executables/              ← .exe files
│   ├── nuget/                    ← .nupkg and .nuspec
│   ├── demos/                    ← Demo applications
│   ├── documentation/            ← Guides
│   ├── installer/                ← Setup.exe
│   ├── checksums/                ← Verification hashes
│   └── CHECKSUMS.txt             ← All checksums
├── scripts/deployment/
│   ├── prepare-distribution.ps1
│   ├── verify-distribution.ps1
│   ├── publish-nuget.ps1
│   └── create-release.ps1
├── .github/workflows/
│   ├── publish-nuget.yml
│   ├── create-release.yml
│   └── publish-to-packagemanagers.yml
└── docs/
    ├── DISTRIBUTION_GUIDE.md
    ├── INSTALLATION_GUIDE.md
    ├── DEPLOYMENT_CHECKLIST.md
    └── DEPLOYMENT_VERIFICATION_ROLLBACK.md
```

---

## ⚠️ Common Issues & Fixes

### "API Key Not Set"
```powershell
# Fix:
$env:NUGET_API_KEY = "your-key-here"
```

### "NuSpec file not found"
```powershell
# Verify path:
Test-Path "dist\v1.0.0\nuget\HELIOS.Platform.nuspec"

# Create if missing:
.\scripts\deployment\prepare-distribution.ps1
```

### "Verification Failed"
```powershell
# Re-run verification:
.\scripts\deployment\verify-distribution.ps1

# Check for missing files:
Get-ChildItem dist\v1.0.0 -Recurse | Format-List
```

### "GitHub Release Fails"
```powershell
# Verify token:
$env:GITHUB_TOKEN | Select-Object -First 4

# Check repository:
git config --get remote.origin.url
```

---

## 🧪 Testing Checklist

### Before Deployment
- [ ] All tests passing
- [ ] Code review completed
- [ ] Documentation updated
- [ ] Version numbers current
- [ ] Git tag created

### During Deployment
- [ ] Distribution prepared successfully
- [ ] Verification all green
- [ ] NuGet publishes without error
- [ ] GitHub Release created
- [ ] All artifacts uploaded

### After Deployment
- [ ] Can install from NuGet
- [ ] Can download from GitHub
- [ ] Documentation accessible
- [ ] Version displays correctly
- [ ] Demo applications work

---

## 📈 Deployment Timeline

| Phase | Duration | What Happens |
|-------|----------|--------------|
| **Preparation** | 15 min | Build, prepare files |
| **Verification** | 5 min | Check integrity |
| **NuGet Publish** | 3 min | Upload to NuGet.org |
| **GitHub Release** | 5 min | Create release, upload |
| **Package Managers** | 30 min | Auto-approve Choco, PR Winget |
| **Monitoring** | 24 hrs | Track downloads, feedback |

**Total Time**: ~1 hour automated + manual verification

---

## 🎯 Success Checklist

✅ Distribution prepared  
✅ Verification passed  
✅ NuGet.org shows package  
✅ GitHub Release created  
✅ Can install from NuGet  
✅ Can download from GitHub  
✅ Documentation online  
✅ Users can access  

---

## 🔄 Release Cycle

```
┌──────────────────┐
│  Tag v1.0.0      │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  GitHub Actions  │ ◄─── Automated
│  workflows run   │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Publish NuGet   │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Create Release  │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Package Mgrs    │ ◄─── Manual submission
│  (Choco, Winget) │
└──────────────────┘
```

---

## 💾 Backup Locations

**Store these before deployment:**
- Git repository (already there)
- dist/v1.0.0/ folder
- RELEASE_NOTES.md
- CHANGELOG.md

**Recovery:**
```powershell
# If needed, copy from backup
Copy-Item backup\dist\v1.0.0 dist\v1.0.0 -Recurse -Force
```

---

## 📞 Support Quick Links

| Need | Resource |
|------|----------|
| **Help** | DEPLOYMENT_AUTOMATION_README.md |
| **Guide** | DISTRIBUTION_GUIDE.md |
| **Install** | INSTALLATION_GUIDE.md |
| **Check** | DEPLOYMENT_CHECKLIST.md |
| **Rollback** | DEPLOYMENT_VERIFICATION_ROLLBACK.md |
| **Issues** | GitHub Issues page |
| **Discuss** | GitHub Discussions |
| **Email** | support@helios-platform.org |

---

## 🚨 Emergency Contacts

| Role | Action |
|------|--------|
| **Critical Issue** | Create GitHub issue (P0 label) |
| **Release Problems** | Email: support@helios-platform.org |
| **Security Issue** | GitHub Security Advisory |
| **Urgent Help** | GitHub Discussions (fastest response) |

---

## 📊 At a Glance

- **Version**: 1.0.0
- **Status**: Production Ready ✅
- **Channels**: 5 major platforms
- **Frameworks**: 4+ .NET versions
- **Scripts**: 4 automated
- **Workflows**: 3 CI/CD
- **Documentation**: 50,000+ words
- **Time to Deploy**: ~1 hour

---

## 🎓 Learn More

1. Start here: `DEPLOYMENT_AUTOMATION_README.md`
2. Understand flow: `DISTRIBUTION_GUIDE.md`
3. Prepare to deploy: `DEPLOYMENT_CHECKLIST.md`
4. Install for users: `INSTALLATION_GUIDE.md`
5. Emergency?: `DEPLOYMENT_VERIFICATION_ROLLBACK.md`

---

**Quick Reference Version**: 1.0  
**Last Updated**: 2024  
**Status**: Ready to Deploy 🚀

# HELIOS Platform - Development Configuration

This VS Code workspace is configured for HELIOS Platform development.

## Features

- ✅ Remote container support
- ✅ PowerShell 7.x environment
- ✅ Git + GitHub CLI pre-configured
- ✅ Docker support
- ✅ All dependencies pre-installed

## Usage

1. Open in VS Code
2. Install Remote Containers extension if needed
3. Click "Reopen in Container"
4. Workspace ready to develop

## Workspace Structure

- `/docs/` - All documentation
- `/scripts/` - All executable scripts
- `/configs/` - Configuration files
- `/templates/` - Script templates
- `.devcontainer/` - Container configuration
- `.vscode/` - VS Code settings

## Quick Commands

```powershell
# Run setup
.\devsetup.ps1

# Generate wiki
.\scripts\utilities\wiki\generate-wiki.ps1

# Check structure
.\scripts\utilities\wiki\check-structure.ps1

# Search wiki
.\scripts\utilities\wiki\wiki-search.ps1
```

# Helios Platform Development Container Configuration Index

## 🎯 Overview

Complete production-ready development environment for Helios Platform with:
- **PowerShell 7.4** with advanced modules
- **Node.js 20 & 22 LTS** with npm, yarn, pnpm
- **Python 3** with venv, pipenv, poetry
- **GitHub CLI** for repository management
- **Docker & Compose** for containerization
- **PostgreSQL 16** optional database
- **25+ VS Code Extensions** pre-configured
- **Git Hooks** for code quality
- **8 Port Mappings** for development services

**Total Configuration**: 16 files, 1500+ lines, ~55 KB, production-ready

---

## 📂 File Organization

### Core Container Configuration (`.devcontainer/`)

#### 1. **devcontainer.json** (208 lines, 5.7 KB)
**Purpose**: VS Code dev container specification

**Contains**:
- Docker Compose orchestration setup
- Feature installation (git, GitHub CLI, PowerShell, Docker)
- Mount configurations (SSH, git, Docker socket, volumes)
- Port forwarding (8080, 5432, 3000, 5173, 3001)
- Remote user setup (devuser)
- VS Code extensions (25+ extensions)
- Settings and customizations
- Post-create and post-start commands

**Key Sections**:
- `features`: Pre-installed dev containers features
- `mounts`: Volume and bind mounts
- `forwardPorts`: Port mappings with attributes
- `customizations.vscode`: Editor settings, extensions
- `remoteEnv`: Environment variables

---

#### 2. **Dockerfile** (150 lines, 6.2 KB)
**Purpose**: Container image definition

**Contains**:
- **Base**: mcr.microsoft.com/devcontainers/universal:2-focal
- **System Tools**: build-essential, curl, git, sqlite3, docker
- **PowerShell 7.4**: Complete installation with modules
- **Node.js**: Via NVM (versions 20 & 22)
- **Python 3**: With dev tools (pytest, black, pylint)
- **GitHub CLI**: Installed and configured
- **Global Tools**: npm packages (TypeScript, ESLint, Prettier, etc.)
- **PowerShell Modules**: PSReadLine, posh-git, oh-my-posh, Terminal-Icons, Az, Pester
- **Non-root User**: devuser with docker group access

**Key Sections**:
- Environment setup
- System dependencies installation
- PowerShell configuration
- Node.js/NVM setup
- Python environment
- User creation and permissions

---

#### 3. **docker-compose.yml** (180 lines, 4.3 KB)
**Purpose**: Multi-container orchestration

**Services**:

1. **devcontainer** (Main development container)
   - Volume mounts (workspace, node_modules, venv, etc.)
   - Port mappings (8 ports)
   - Environment variables (Node, Python, Docker, DB)
   - Health checks
   - Capabilities and security options

2. **postgres** (Optional PostgreSQL database)
   - Image: postgres:16-alpine
   - Port: 5432
   - Auto-initialization via init-db.sh
   - Data persistence in named volume
   - Health checks

**Volumes** (Named volumes for persistence):
- devcontainer-node-modules
- devcontainer-python-cache
- devcontainer-home
- devcontainer-npm-cache
- devcontainer-nvm
- postgres-data

**Networks**:
- helios-network (bridge driver)

---

#### 4. **onCreateCommand.sh** (280+ lines, 10.6 KB)
**Purpose**: Post-creation environment initialization

**Execution Steps**:

1. **Git Configuration**
   - User name and email
   - Core settings (autocrlf, safecrlf, longpaths, pull.rebase)

2. **Git Hooks**
   - **pre-commit**: Prettier formatting, code quality
   - **commit-msg**: Conventional commit validation
   - **post-merge**: Dependency update detection

3. **Directory Structure**
   - Creates: src/, tests/, docs/, scripts/, logs/, data/

4. **Node.js Environment**
   - Checks Node.js installation
   - Initializes npm if no package.json
   - Creates .npmrc configuration

5. **Python Environment**
   - Creates virtual environment (.venv)
   - Upgrades pip, setuptools, wheel
   - Installs requirements if present

6. **PowerShell Profile**
   - Imports modules
   - Sets prompt theme (paradox)
   - Configures PSReadLine

7. **Database Setup**
   - SQLite database initialization
   - Wiki database schema

8. **Environment File**
   - Creates .env with development defaults

9. **Helper Scripts**
   - setup.sh - Quick project setup
   - dev.sh - Development server startup

10. **Verification**
    - Displays all installed tools and versions

---

#### 5. **init-db.sh** (68 lines, 2.1 KB)
**Purpose**: PostgreSQL database initialization

**Creates**:
- Extensions (uuid-ossp, pg_trgm, btree_gin)
- Tables (users, wiki_pages, wiki_versions)
- Indexes for performance
- User permissions

---

#### 6. **README.md** (9.6 KB)
**Purpose**: Comprehensive documentation

**Sections**:
- Quick start guide
- Feature overview
- File structure
- Configuration details
- Usage examples
- Troubleshooting
- Security considerations

---

### VS Code Configuration (`.vscode/`)

#### 1. **settings.json** (2.8 KB)
**Contains**:
- Python (pylint, black formatter, 100 char limit)
- JavaScript/TypeScript (Prettier, ESLint)
- PowerShell formatting (OTBS preset)
- YAML formatting (2 spaces)
- Editor preferences (rulers at 80/120, word wrap)
- File exclusions (node_modules, .venv, etc.)
- Terminal profiles (bash, PowerShell)
- Docker settings
- Git configuration

---

#### 2. **extensions.json** (0.8 KB)
**Recommended Extensions** (26 total):
- **PowerShell**: ms-vscode.powershell
- **Python**: ms-python.python, ms-python.vscode-pylance, ms-python.black-formatter
- **Formatting**: esbenp.prettier-vscode
- **Linting**: dbaeumer.vscode-eslint
- **Container**: ms-azuretools.vscode-docker, ms-vscode-remote.remote-containers
- **Version Control**: GitHub.vscode-pull-request-github, eamodio.gitlens
- **Documentation**: DavidAnson.vscode-markdownlint, redhat.vscode-yaml
- **Other**: Postman.postman-for-vscode, rangav.vscode-thunder-client

---

#### 3. **launch.json** (1.9 KB)
**Debug Configurations**:
- PowerShell attach and launch
- Python current file, Django, FastAPI
- Node.js debugging

---

#### 4. **tasks.json** (3.2 KB)
**Build Tasks**:
- npm (install, build, test, lint, format)
- Docker Compose (build, start, stop)
- Python (requirements install, pytest)

---

### Root Configuration Files

#### 1. **.gitignore** (1.3 KB)
**Excludes** (50+ patterns):
- Dependencies: node_modules/, .venv/
- Build artifacts: dist/, build/, out/
- IDE: .vscode/, .idea/, *.swp
- OS: .DS_Store, Thumbs.db
- Testing: .coverage, .pytest_cache/
- Logs: *.log, npm-debug.log*
- Database: *.db, *.sqlite
- Temporary: temp/, *.tmp, *.bak

---

#### 2. **.editorconfig** (0.8 KB)
**Standardizes**:
- Indentation (2 spaces default, 4 for Python)
- Line endings (LF)
- Charset (UTF-8)
- Trim whitespace
- Insert final newline

---

#### 3. **.prettierrc** (0.7 KB)
**Formatting Rules**:
- 100 char line width
- Single quotes
- ES5 trailing commas
- Semicolons enabled
- LF line endings
- File-specific overrides (JSON, YAML, Markdown)

---

#### 4. **devsetup.sh** (3.2 KB)
**Local Setup Script** (no Docker required):
- Checks Node.js, Python
- Creates virtual environments
- Initializes package managers
- Sets up git hooks
- Creates directory structure

---

#### 5. **verify-setup.sh** (3.4 KB)
**Validation Script**:
- Checks Docker installation
- Validates configuration files
- Verifies JSON syntax
- Tests Docker daemon

---

#### 6. **DEVCONTAINER_REFERENCE.md** (6.4 KB)
**Quick Reference**:
- File manifest
- Common commands
- Port reference
- Troubleshooting
- Verification checklist

---

## 🔄 Workflow

### Setup Workflow
```
1. Clone/Initialize Project
   ↓
2. Review Configuration Files
   ↓
3. Run verify-setup.sh
   ↓
4. Start Container: docker-compose up -d
   ↓
5. Initialize Dependencies: npm install, pip install
   ↓
6. Begin Development
```

### Development Workflow
```
1. Code Changes
   ↓
2. Pre-commit Hook (Prettier, formatting)
   ↓
3. Git Commit (Message validation)
   ↓
4. Push to Repository
```

### Debugging Workflow
```
1. Set Breakpoint
   ↓
2. Open Debug Configuration
   ↓
3. Run Debugger (F5)
   ↓
4. Step Through Code
```

---

## 📊 Resource Requirements

### Minimum
- Docker 20.10+
- Docker Compose 1.29+
- 8GB RAM
- 20GB disk space

### Recommended
- Docker 25+
- Docker Compose 2.0+
- 16GB+ RAM
- 50GB+ disk space
- SSD storage

---

## 🔐 Security Checklist

- ✓ Non-root user (devuser)
- ✓ SSH keys mounted read-only
- ✓ No hardcoded credentials
- ✓ .env for sensitive values
- ✓ seccomp unconfined (for debugging)
- ✓ SYS_PTRACE capability
- ✓ Docker socket via bind mount

---

## 📈 Performance Optimization

### Volume Caching
- node_modules cached in named volume
- Python venv in separate volume
- Reduces container startup time

### Memory Management
- NODE_OPTIONS set to 4096MB
- Docker memory limits configurable
- Idle container cleanup

### Disk Usage
- Multi-stage Docker builds
- Layer caching
- Named volume reuse

---

## 🆘 Common Issues & Solutions

### Port Already in Use
```bash
lsof -i :8080
kill -9 <PID>
```

### Permission Denied
```bash
docker-compose down -v
docker-compose up -d
```

### Container Won't Start
```bash
docker-compose logs devcontainer
docker-compose build --no-cache
```

### Database Connection Failed
```bash
docker-compose exec postgres psql -U devuser -d helios_dev
```

---

## 📚 Documentation

| Document | Purpose | Size |
|----------|---------|------|
| `.devcontainer/README.md` | Full guide | 9.6 KB |
| `DEVCONTAINER_REFERENCE.md` | Quick reference | 6.4 KB |
| This file | Index & overview | 12+ KB |
| Configuration files | Self-documenting | 55 KB |

---

## ✨ Features Checklist

- ✓ PowerShell 7.4 with 6+ modules
- ✓ Node.js 20 & 22 (via NVM)
- ✓ Python 3 with venv
- ✓ GitHub CLI integration
- ✓ Docker-in-Docker
- ✓ PostgreSQL 16
- ✓ Git hooks (3 types)
- ✓ VS Code extensions (25+)
- ✓ Debug configurations (3 languages)
- ✓ Build tasks (npm, Docker, Python)
- ✓ Port mappings (8 ports)
- ✓ Volume persistence (10 named volumes)
- ✓ Environment variables (20+ configured)
- ✓ Health checks
- ✓ Security capabilities
- ✓ Comprehensive documentation

---

## 🚀 Getting Started

1. **Review Documentation**: Read `.devcontainer/README.md`
2. **Verify Setup**: Run `bash verify-setup.sh`
3. **Start Container**: `cd .devcontainer && docker-compose up -d`
4. **Access Shell**: `docker-compose exec devcontainer bash`
5. **Initialize Project**: `npm install && pip install -r requirements.txt`
6. **Start Development**: Begin coding!

---

## 📞 Support

- Check `.devcontainer/README.md` for detailed troubleshooting
- Review `DEVCONTAINER_REFERENCE.md` for quick answers
- Inspect Docker logs: `docker-compose logs -f [service]`
- Validate configuration: Run `verify-setup.sh`

---

**Version**: 1.0  
**Created**: 2024  
**Status**: Production Ready  
**Maintainer**: Helios Platform Dev Team

# HELIOS Platform - Development Environment Setup

Complete devcontainer and VS Code workspace configuration for the HELIOS Platform project.

## 📋 Overview

This setup provides a fully containerized development environment with:
- **OS**: Ubuntu 22.04 LTS
- **Runtime**: PowerShell 7.x, Node.js 20.x, Python 3.x
- **Tools**: Docker-in-Docker, Git, GitHub CLI, Git LFS
- **IDE**: VS Code with 40+ recommended extensions
- **Debugging**: PowerShell, Python, Node.js, Chrome
- **Automation**: 30+ VS Code tasks for building, testing, and deployment

## 📁 Configuration Files

### 1. Devcontainer Configuration

#### `.devcontainer/devcontainer.json`
- **Base Image**: `mcr.microsoft.com/devcontainers/universal:latest`
- **Features**: Git, GitHub CLI, PowerShell, Docker-in-Docker
- **Forwarded Ports**: 3000, 5000, 8080, 8081, 8888, 5432
- **Post-create**: Automatic `devsetup.ps1` execution
- **User**: `vscode` (non-root with Docker access)

**Key Settings**:
```json
{
  "image": "mcr.microsoft.com/devcontainers/universal:latest",
  "features": {
    "ghcr.io/devcontainers/features/powershell:latest": {},
    "ghcr.io/devcontainers/features/docker-in-docker:latest": {}
  },
  "forwardPorts": [3000, 5000, 8080, 8081, 8888, 5432],
  "postCreateCommand": "bash -c 'cd ${containerWorkspaceFolder} && pwsh -NoProfile -ExecutionPolicy Bypass -Command \". ./devsetup.ps1 -PostCreate\"'"
}
```

#### `.devcontainer/Dockerfile`
Custom Ubuntu 22.04-based image with:
- **Build Tools**: gcc, make, build-essential
- **Runtimes**: PowerShell 7.x, Node.js 20.x, Python 3.x
- **Container Tools**: Docker CLI, GitHub CLI, Git LFS
- **Development**: vim, nano, tmux, htop, jq, yq
- **Python Packages**: pylint, pytest, black, flake8, pyyaml
- **PowerShell Modules**: PSScriptAnalyzer, PSReadLine, posh-git, oh-my-posh

**Entrypoint**: `/opt/microsoft/powershell/7/pwsh` (PowerShell)

#### `.devcontainer/docker-compose.yml`
Multi-service orchestration:
- **devcontainer**: Main development container
- **sqlite-server** (optional): SQLite database service
- **wiki-server** (optional): Wiki server (requarks/wiki)
- **postgres** (optional): PostgreSQL 15 database

**Usage**:
```bash
# Start main container
docker-compose up -d

# Start with optional services
docker-compose --profile optional up -d

# View logs
docker-compose logs -f devcontainer
```

### 2. VS Code Workspace Configuration

#### `.vscode/settings.json`
Comprehensive editor and tool configuration:

**Editor Settings**:
- Font: Fira Code 14pt with ligatures
- Tab Size: 2 spaces (JS/YAML), 4 spaces (Python/PowerShell)
- Formatting: Auto-format on save and paste
- Word Wrap: Enabled
- Rulers: 80pt and 120pt columns
- Bracket colorization and pair guides

**Language-Specific Formatting**:
- **JavaScript/TypeScript**: Prettier (100pt width, single quotes)
- **Python**: Black (88pt width, line-length=88)
- **PowerShell**: OTBS style, 4-space indent
- **YAML**: 2-space indent, 120pt width
- **Markdown**: 2-space indent, no trim on trailing spaces

**Tool Integration**:
- Python: Pylint, Flake8, pytest, Black formatter
- PowerShell: PSScriptAnalyzer enabled
- Git: Auto-fetch every 3 minutes
- Terminal: PowerShell as default shell
- Remote: Container-specific extensions

#### `.vscode/extensions.json`
40+ recommended extensions:

**Core Development**:
- ms-vscode.powershell
- ms-vscode-remote.remote-containers
- GitHub.copilot
- eamodio.gitlens

**Language Support**:
- ms-python.python, ms-python.vscode-pylance
- dbaeumer.vscode-eslint
- redhat.vscode-yaml

**Tools**:
- ms-azuretools.vscode-docker
- mtxr.sqltools (with SQLite & PostgreSQL drivers)
- Gruntfuggly.todo-tree
- timonwong.shellcheck

**Theming**:
- zhuangtongfa.Material-theme
- PKief.material-icon-theme

#### `.vscode/launch.json`
Debug configurations for multiple languages:

**PowerShell**:
- Current file debugging
- devsetup.ps1 debugging
- Process attachment with runspace selection

**Python**:
- File execution and debugging
- pytest test debugging
- FastAPI server debugging
- Arguments support

**Node.js**:
- File execution and debugging
- npm start debugging
- Jest test debugging

**Web**:
- Chrome debugger attachment (localhost:3000)

**Compound Configurations**:
- Full Stack: API + Web (simultaneous debugging)

#### `.vscode/tasks.json`
30+ automated tasks organized by category:

**PowerShell Tasks**:
- `PowerShell: Check Syntax` - Validate script syntax
- `PowerShell: Analyze Script` - Run PSScriptAnalyzer
- `PowerShell: Run as Admin` - Execute with elevated privileges

**Git Tasks**:
- `Git: Status` - Repository status
- `Git: Pull Latest` - Pull from main branch
- `Git: Fetch All` - Fetch all remotes

**Docker Tasks**:
- `Docker: Build devcontainer` - Build custom image
- `Docker: Run devcontainer` - Start container
- `Docker: Stop devcontainer` - Stop container
- `Docker: View logs` - Stream container logs

**Build Tasks**:
- `Build: Generate Wiki` - Generate Wiki documentation (default)
- `Build: Test Builds` - Run build verification
- `Build: Compile Project` - npm run build

**Testing Tasks**:
- `Test: Run All Tests` - Execute test suite
- `Test: Run Tests (Watch)` - Watch mode testing
- `Test: Python pytest` - pytest with verbose output

**Linting Tasks**:
- `Lint: ESLint` - JavaScript linting
- `Lint: Python (pylint)` - Python code analysis
- `Lint: Flake8` - Python style checking

**Formatting Tasks**:
- `Format: Prettier` - Format all supported files
- `Format: Python (Black)` - Format Python code

**Development Tasks**:
- `Dev: Start Server` - npm run dev
- `Dev: Start API` - uvicorn FastAPI server

**Setup Tasks**:
- `Setup: Initialize Environment` - Run devsetup.ps1
- `Setup: Install Dependencies` - npm install
- `Setup: Install Python Deps` - pip install -r requirements.txt

**Cleanup Tasks**:
- `Clean: Remove Build Artifacts` - Remove dist, build, cache

### 3. Root Configuration Files

#### `.gitignore`
Comprehensive 402-line exclusion list covering:
- OS files (macOS, Windows, Linux)
- Build artifacts (dist, build, node_modules)
- IDE files (.vscode, .idea, sublime-workspace)
- Environment variables (.env, secrets/)
- Logs and temp files (logs/, *.log, tmp/)
- Database files (*.db, *.sqlite)
- Cache directories (__pycache__, .pytest_cache)
- Python virtual environments (.venv, venv)
- HELIOS-specific (wiki_generated/, reports/)

#### `.editorconfig`
EditorConfig standards for consistent formatting across all file types:

| Format | Indent | Size | Width | Notes |
|--------|--------|------|-------|-------|
| JavaScript | space | 2 | 100pt | Modern web dev |
| Python | space | 4 | 88pt | PEP 8 standard |
| PowerShell | space | 4 | 120pt | PS style guide |
| YAML | space | 2 | 120pt | Configuration |
| Markdown | space | 2 | off | Preserve wrapping |
| Shell | space | 2 | 120pt | POSIX compliance |
| Makefile | tab | 8 | — | Standard practice |
| Go | tab | 8 | — | Go convention |

#### `.prettierrc`
Prettier formatting configuration:
- Print Width: 100pt
- Semi: true
- Quotes: Single
- Trailing Comma: ES5
- Bracket Spacing: true
- Arrow Parens: always
- HTML Whitespace: css
- End of Line: lf

Language-specific overrides:
- JSON: 80pt width
- YAML: 120pt width

#### `devsetup.ps1`
One-command environment initialization (364 lines, 11.6 KB):

**Capabilities**:
- `Initialize-Git`: Sets up git repository and hooks
- `Initialize-PowerShellModules`: Installs 5 core modules
- `Install-NodeDependencies`: npm install
- `Install-PythonDependencies`: pip install -r requirements.txt
- `Initialize-Directories`: Creates project structure
- `Setup-GitHooks`: Pre-commit hook configuration
- `Clean-BuildArtifacts`: Clean build artifacts (-CleanBuild)
- `Show-EnvironmentInfo`: Display environment details
- Comprehensive logging to `logs/setup.log`

**Usage**:
```powershell
# Full setup
.\devsetup.ps1

# Post-create in devcontainer
.\devsetup.ps1 -PostCreate

# Skip dependencies
.\devsetup.ps1 -SkipDeps

# Clean rebuild
.\devsetup.ps1 -CleanBuild
```

## 🚀 Quick Start

### 1. Prerequisites
- Windows/macOS/Linux with Docker installed
- VS Code with Remote - Containers extension
- Git installed locally

### 2. Open in VS Code
```bash
code C:\Users\ADMIN\helios-platform
```

### 3. Reopen in Container
1. Click the Remote Indicator (bottom-left corner)
2. Select "Reopen in Container"
3. Wait for the image to build (first time: 5-10 minutes)

### 4. Initialize Environment
In the container terminal (PowerShell):
```powershell
.\devsetup.ps1 -PostCreate
```

Or manually:
```powershell
pwsh
.\devsetup.ps1
```

### 5. Start Developing
- Press `F5` to start debugging
- Press `Ctrl+Shift+B` to run the default build task
- Press `Ctrl+`` to open the integrated terminal

## 🔧 Task Execution

### Running Tasks
Press `Ctrl+Shift+P` and select "Tasks: Run Task" or use keybindings:

**Common Tasks**:
- `Ctrl+Shift+B` - Generate Wiki (default build)
- `Ctrl+Shift+D` - Open debug view

**Via Terminal**:
```powershell
# Run a specific task
Invoke-VSCodeTask "Build: Generate Wiki"

# Or in terminal:
code --list-extensions  # List installed extensions
```

## 🐳 Docker Usage

### Build Container Image
```bash
docker build -t helios-platform-dev:latest -f .devcontainer/Dockerfile .devcontainer
```

### Run Container Standalone
```bash
docker run -it \
  -v $(pwd):/workspace \
  -p 3000:3000 -p 5000:5000 -p 8080:8080 \
  helios-platform-dev:latest
```

### Docker Compose
```bash
# Start services
docker-compose -f .devcontainer/docker-compose.yml up -d

# Start with optional services
docker-compose -f .devcontainer/docker-compose.yml --profile optional up -d

# View logs
docker-compose -f .devcontainer/docker-compose.yml logs -f

# Stop services
docker-compose -f .devcontainer/docker-compose.yml down
```

## 🔌 Port Forwarding

| Port | Service | Purpose |
|------|---------|---------|
| 3000 | Application | Web application or dev server |
| 5000 | API Server | Python/FastAPI backend |
| 8080 | Wiki Server | Documentation wiki |
| 8081 | Alternative Web | Secondary web service |
| 8888 | Jupyter Notebook | Python notebook server |
| 5432 | PostgreSQL | Database (optional) |

## 🎨 VS Code Extensions

Automatically recommended for installation in container:
- 40+ productivity extensions
- Language support: PowerShell, Python, JavaScript, YAML, Docker
- Version control: GitLens, GitHub PRs
- Database tools: SQLTools with drivers
- Debugging: Python, Node.js, Chrome
- Formatting: Prettier, Black

## 📝 PowerShell Modules

Automatically installed by `devsetup.ps1`:
- **PSScriptAnalyzer** (1.21.0) - PowerShell code quality
- **PSReadLine** (2.3.4) - Enhanced PowerShell REPL
- **posh-git** (1.1.0) - Git integration for PowerShell
- **oh-my-posh** (18.18.1) - PowerShell theming

## 📚 Key Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+B` | Run default build task |
| `Ctrl+Shift+D` | Open debug view |
| `Ctrl+`` | Toggle integrated terminal |
| `Ctrl+Shift+P` | Open command palette |
| `F5` | Start debugging |
| `Shift+F5` | Stop debugger |
| `Ctrl+J` | Toggle panel |
| `Ctrl+K Ctrl+O` | Open folder |

## 🐛 Debugging

### PowerShell Debugging
1. Open `.ps1` file
2. Press `F5` or select "PowerShell: Current File" from debug menu
3. Set breakpoints by clicking left margin
4. Variables and call stack available in debug panel

### Python Debugging
1. Open `.py` file
2. Press `F5` or select "Python: Current File"
3. Arguments can be set in launch configuration
4. Pytest support for test debugging

### Node.js Debugging
1. Open `.js` file or select "Node.js: npm start"
2. Press `F5` to attach debugger
3. Breakpoints work automatically
4. Chrome DevTools integration available

## 📊 Directory Structure

```
helios-platform/
├── .devcontainer/
│   ├── devcontainer.json      # Container configuration
│   ├── Dockerfile             # Custom image definition
│   └── docker-compose.yml     # Multi-service orchestration
├── .vscode/
│   ├── settings.json          # Editor & tool settings
│   ├── extensions.json        # Recommended extensions
│   ├── launch.json            # Debug configurations
│   └── tasks.json             # Build automation tasks
├── .gitignore                 # Git exclusion patterns
├── .editorconfig              # Editor formatting rules
├── .prettierrc                # Prettier formatting config
├── devsetup.ps1               # One-command setup script
├── src/                       # Source code
├── tests/                     # Test files
├── scripts/                   # Utility scripts
├── docs/                      # Documentation
├── logs/                      # Application logs
└── README.md                  # This file
```

## 🔒 Security Considerations

### Secrets Management
- Never commit `.env` files (excluded by `.gitignore`)
- Use environment variables for sensitive data
- Store secrets in container-only volumes
- GitHub Actions: Use secrets management

### Git Configuration
- Pre-commit hooks (created by `devsetup.ps1`)
- GPG signing support ready
- SSH key mounting in devcontainer

### Container Security
- Non-root user (`vscode`) by default
- Docker-in-Docker isolation
- Volume mounts with read-only options for configs

## 🛠️ Troubleshooting

### Container fails to build
```bash
# Check Docker logs
docker build --no-cache -t helios-platform-dev:latest -f .devcontainer/Dockerfile .devcontainer

# Verify Docker daemon
docker ps
```

### VS Code can't connect to container
1. Ensure Remote - Containers extension is installed
2. Verify Docker is running
3. Check container logs: `docker logs helios-platform-dev`
4. Rebuild container: `F1` → "Remote Containers: Rebuild Container"

### Python debugging not working
- Verify `ms-python.debugpy` extension is installed
- Check Python path: `python3 -c "import sys; print(sys.executable)"`
- Ensure pytest is installed: `python3 -m pip install pytest`

### PowerShell script execution blocked
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope CurrentUser
```

## 📖 Documentation

- **Container**: `.devcontainer/devcontainer.json`
- **Docker Image**: `.devcontainer/Dockerfile`
- **Editor Config**: `.vscode/settings.json`
- **Tasks**: `.vscode/tasks.json`
- **Debugging**: `.vscode/launch.json`
- **Setup Script**: `devsetup.ps1`

## 🤝 Contributing

When contributing to HELIOS Platform:
1. All changes must work in devcontainer
2. Follow formatting rules in `.editorconfig`
3. Run applicable linters (ESLint, pylint)
4. Use `Format: Prettier` or `Format: Python (Black)` tasks
5. Include test cases in `tests/` directory

## 📝 License

See `LICENSE` file in repository root.

## 🎯 Support

For issues or questions:
1. Check `.devcontainer/devcontainer.json` for environment details
2. Review `logs/setup.log` for initialization errors
3. Check VS Code output panel for extension errors
4. Verify Docker daemon is running

---

**Setup Created**: 2024
**Configuration Version**: 1.0
**Status**: ✅ Production Ready

# Helios Platform Dev Container Quick Reference

## 📋 File Manifest

### .devcontainer/ - Container Configuration
- **devcontainer.json** - VS Code dev container specification
  - 200+ lines of configuration
  - Feature definitions (git, GitHub CLI, PowerShell, Docker)
  - Port forwarding (8080, 5432, 3000, 5173, 3001)
  - Volume mounts for persistence
  - VS Code settings and extensions configuration
  
- **Dockerfile** - Container image definition
  - 150+ lines
  - Ubuntu 22.04 base image
  - PowerShell 7.4 installation
  - Node.js 20 & 22 via NVM
  - Python 3 with dev tools
  - GitHub CLI, Docker, SQLite3
  - Non-root user (devuser) setup

- **docker-compose.yml** - Multi-container orchestration
  - 180+ lines
  - Devcontainer service
  - PostgreSQL 16 Alpine database
  - 8 port mappings
  - Volume persistence
  - Network configuration
  - Health checks

- **onCreateCommand.sh** - Post-creation setup
  - 280+ lines
  - Git configuration and hooks
  - Directory structure creation
  - Python venv initialization
  - PowerShell profile setup
  - Database initialization
  - Tool verification

- **init-db.sh** - PostgreSQL initialization
  - Database schema creation
  - User and permission setup

- **README.md** - Comprehensive documentation
  - Usage instructions
  - Troubleshooting guide
  - Configuration details

### .vscode/ - Editor Configuration
- **settings.json** - Editor preferences (2.8 KB)
  - Python: pylint, black formatter
  - JavaScript/TypeScript: Prettier, ESLint
  - PowerShell formatting
  - Terminal profiles
  - File exclusions

- **extensions.json** - Recommended extensions (0.8 KB)
  - 26 extensions listed
  - PowerShell, Python, Pylance
  - Docker, Remote Containers
  - GitHub integration

- **launch.json** - Debug configurations (1.9 KB)
  - PowerShell attach/launch
  - Python current file, Django, FastAPI
  - Node.js debugging

- **tasks.json** - Build tasks (3.2 KB)
  - npm operations (install, build, test, lint)
  - Docker Compose commands
  - Python testing

### Root Configuration
- **.gitignore** - Git exclusions (1.3 KB)
  - 50+ patterns
  - node_modules, .venv, __pycache__
  - Build artifacts, IDE files, OS files

- **.editorconfig** - Editor settings (0.8 KB)
  - Python: 4 spaces, 100 char limit
  - JavaScript/JSON: 2 spaces
  - YAML: 2 spaces
  - Markdown: preserve whitespace

- **.prettierrc** - Code formatter (0.7 KB)
  - 100 char line width
  - Single quotes
  - ES5 trailing commas
  - LF line endings

### Setup Scripts
- **devsetup.sh** - Local setup (3.2 KB)
  - Node.js environment
  - Python venv creation
  - Git hooks setup
  - Directory structure

- **verify-setup.sh** - Verification script
  - Docker checks
  - File validation
  - JSON syntax verification

## 🔧 Common Commands

### Start Development Environment
```bash
cd .devcontainer
docker-compose up -d
docker-compose exec devcontainer bash
```

### Stop Development Environment
```bash
docker-compose down
```

### Rebuild Container
```bash
docker-compose build --no-cache
docker-compose up -d
```

### Run Tests
```bash
docker-compose exec devcontainer npm test
docker-compose exec devcontainer pytest -v
```

### Access Database
```bash
docker-compose exec postgres psql -U devuser -d helios_dev
```

### View Logs
```bash
docker-compose logs -f devcontainer
docker-compose logs -f postgres
```

## 🎯 Port Reference

| Port | Service | Description |
|------|---------|-------------|
| 8080 | Wiki | Main application |
| 5432 | PostgreSQL | Database (optional) |
| 3000 | Node Dev | Express/React development |
| 5173 | Vite | Frontend build tool |
| 3001 | API | REST API server |
| 4200 | Angular | Angular dev server |
| 8000 | Python | FastAPI/Django |
| 8888 | Jupyter | Notebook server |

## 📦 Included Tools

### Languages & Runtimes
- PowerShell 7.4 with posh-git, oh-my-posh
- Node.js 20 & 22 LTS (via NVM)
- Python 3 with venv

### Package Managers
- npm & yarn & pnpm (Node.js)
- pip, pipenv, poetry (Python)

### Development Utilities
- TypeScript, Babel, Webpack
- ESLint, Prettier, Husky
- Jest, Mocha, Pytest
- Azure CLI, GitHub CLI

### System Tools
- Git with hooks
- Docker & Docker Compose
- SQLite3
- curl, wget, rsync
- vim, nano, less

## 🔐 Security Features

- Non-root user (devuser)
- Read-only SSH key mounting
- Seccomp unconfined (for debugging)
- SYS_PTRACE capability
- No hardcoded credentials

## 📊 Performance Tips

1. **Volume Caching**
   - node_modules cached in named volume
   - Python venv in separate volume
   - Reduces reinstalls on container restart

2. **Memory Configuration**
   - Default: 4GB
   - Adjust in Docker Desktop settings if needed
   - NODE_OPTIONS set to 4096MB

3. **Disk Space**
   - Requires ~20GB for images
   - ~5GB for development volumes
   - Clean up: `docker system prune -a`

## 🐛 Troubleshooting

### Container won't start
```bash
docker-compose logs devcontainer
docker-compose down -v  # Remove volumes
docker-compose up --build
```

### Port conflicts
```bash
lsof -i :8080
kill -9 <PID>
# Or change port in docker-compose.yml
```

### Permission issues
```bash
docker-compose down -v
docker-compose up -d
```

### Database connection refused
```bash
# Wait for PostgreSQL startup
sleep 10
docker-compose exec devcontainer psql -h postgres -U devuser -d helios_dev
```

## 📚 Documentation

- `.devcontainer/README.md` - Comprehensive guide
- `.vscode/settings.json` - Editor configuration details
- `.editorconfig` - Editor standards
- Each shell script contains inline documentation

## ✅ Verification Checklist

- [ ] Docker installed and running
- [ ] Docker Compose available
- [ ] All configuration files present
- [ ] JSON files are valid
- [ ] .devcontainer/ directory exists
- [ ] .vscode/ directory exists
- [ ] Git initialized (if needed)

## 🚀 First Time Setup

1. Run verification: `bash verify-setup.sh`
2. Start container: `cd .devcontainer && docker-compose up -d`
3. Access container: `docker-compose exec devcontainer bash`
4. Initialize project: `npm install && pip install -r requirements.txt`
5. Start development: `npm start`

---

**Version**: 1.0  
**Created**: 2024  
**Maintained by**: Helios Platform Dev Team

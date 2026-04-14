#!/bin/bash
set -e

echo "=========================================="
echo "Helios Platform Dev Environment Setup"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[✓]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[✗]${NC} $1"
}

# Step 1: Initialize Git Configuration
log_info "Setting up Git configuration..."
if ! git config --global user.name &>/dev/null; then
    git config --global user.name "Helios Dev User"
    log_success "Git user name configured"
else
    log_success "Git user name already configured"
fi

if ! git config --global user.email &>/dev/null; then
    git config --global user.email "dev@helios-platform.local"
    log_success "Git user email configured"
else
    log_success "Git user email already configured"
fi

git config --global core.autocrlf input
git config --global core.safecrlf warn
git config --global core.longpaths true
git config --global pull.rebase true
git config --global fetch.prune true
log_success "Git configuration completed"
echo ""

# Step 2: Setup Git Hooks
log_info "Setting up git hooks..."
mkdir -p /workspace/.git/hooks

# Create pre-commit hook
cat > /workspace/.git/hooks/pre-commit << 'EOF'
#!/bin/bash
# Pre-commit hook for code quality checks

echo "[Pre-commit] Running code quality checks..."

# Check for staged files
if git diff --cached --quiet; then
    exit 0
fi

# Run prettier on staged JavaScript/TypeScript files
staged_js=$(git diff --cached --name-only --diff-filter=ACM | grep -E '\.(js|ts|jsx|tsx|json|md|yaml|yml)$' || true)
if [ ! -z "$staged_js" ]; then
    echo "$staged_js" | xargs npx prettier --write 2>/dev/null || true
    git add $staged_js
fi

exit 0
EOF

chmod +x /workspace/.git/hooks/pre-commit
log_success "Pre-commit hook installed"

# Create commit-msg hook
cat > /workspace/.git/hooks/commit-msg << 'EOF'
#!/bin/bash
# Commit message validation hook

commit_msg_file=$1
commit_msg=$(cat "$commit_msg_file")

# Allow merge commits
if [[ $commit_msg == Merge\ * ]]; then
    exit 0
fi

# Basic commit message format check
if ! [[ $commit_msg =~ ^[A-Za-z0-9\-]+:\ .+ ]]; then
    echo "Error: Commit message must follow format: 'type: message'"
    echo "Valid types: feat, fix, docs, style, refactor, test, chore, ci"
    exit 1
fi

exit 0
EOF

chmod +x /workspace/.git/hooks/commit-msg
log_success "Commit-msg hook installed"

# Create post-merge hook
cat > /workspace/.git/hooks/post-merge << 'EOF'
#!/bin/bash
# Post-merge hook to check for dependency changes

changed_files=$(git diff HEAD@{1} HEAD --name-only)

if echo "$changed_files" | grep -q "package.json"; then
    echo "[Post-merge] package.json changed, consider running: npm install"
fi

if echo "$changed_files" | grep -q "requirements.txt"; then
    echo "[Post-merge] requirements.txt changed, consider running: pip install -r requirements.txt"
fi

if echo "$changed_files" | grep -q "poetry.lock"; then
    echo "[Post-merge] poetry.lock changed, consider running: poetry install"
fi

exit 0
EOF

chmod +x /workspace/.git/hooks/post-merge
log_success "Post-merge hook installed"
echo ""

# Step 3: Setup directory structure
log_info "Creating necessary directories..."
mkdir -p /workspace/{src,tests,docs,scripts,config,.devcontainer-state}
mkdir -p /workspace/logs
mkdir -p /workspace/.vscode
log_success "Directory structure created"
echo ""

# Step 4: Setup Node.js environment
log_info "Checking Node.js environment..."
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    NPM_VERSION=$(npm --version)
    log_success "Node.js ${NODE_VERSION} and npm ${NPM_VERSION} detected"
    
    # Create .npmrc for consistent npm behavior
    cat > /workspace/.npmrc << 'EOF'
legacy-peer-deps=false
audit=true
fund=true
engine-strict=false
ignore-scripts=false
EOF
    log_success ".npmrc configured"
else
    log_warning "Node.js not found"
fi
echo ""

# Step 5: Setup Python environment
log_info "Checking Python environment..."
if command -v python3 &> /dev/null; then
    PYTHON_VERSION=$(python3 --version)
    log_success "${PYTHON_VERSION} detected"
    
    # Create virtual environment if it doesn't exist
    if [ ! -d "/workspace/.venv" ]; then
        log_info "Creating Python virtual environment..."
        cd /workspace
        python3 -m venv .venv
        source .venv/bin/activate
        pip install --upgrade pip setuptools wheel
        log_success "Python virtual environment created and upgraded"
    else
        log_success "Python virtual environment already exists"
    fi
else
    log_warning "Python3 not found"
fi
echo ""

# Step 6: Initialize package managers
log_info "Initializing package managers..."

# Initialize npm if package.json doesn't exist
if [ ! -f "/workspace/package.json" ]; then
    log_info "Initializing npm..."
    cd /workspace
    npm init -y > /dev/null 2>&1
    log_success "npm initialized"
else
    log_success "package.json already exists"
    log_info "Installing npm dependencies..."
    cd /workspace
    npm install --prefer-offline --no-audit 2>/dev/null || npm install 2>/dev/null || true
fi

# Initialize poetry if pyproject.toml doesn't exist
if [ ! -f "/workspace/pyproject.toml" ] && [ ! -f "/workspace/requirements.txt" ]; then
    log_success "No Python package configuration found (optional)"
fi
echo ""

# Step 7: Setup PowerShell profile
log_info "Configuring PowerShell environment..."
mkdir -p ~/.config/powershell
if [ ! -f ~/.config/powershell/profile.ps1 ]; then
    cat > ~/.config/powershell/profile.ps1 << 'EOF'
# PowerShell Profile for Helios Development

# Import modules
Import-Module posh-git -ErrorAction SilentlyContinue
Import-Module oh-my-posh -ErrorAction SilentlyContinue
Import-Module Terminal-Icons -ErrorAction SilentlyContinue

# Set prompt theme
Set-PoshPrompt -Theme paradox -ErrorAction SilentlyContinue

# PSReadLine configuration
Set-PSReadLineOption -PredictionSource History
Set-PSReadLineOption -PredictionViewStyle ListView
Set-PSReadLineKeyHandler -Key Tab -Function MenuComplete

# Aliases
Set-Alias -Name ll -Value Get-ChildItem
Set-Alias -Name which -Value Get-Command

# Functions
function cd-workspace { Set-Location /workspace }
function cd-src { Set-Location /workspace/src }
function cd-tests { Set-Location /workspace/tests }

# Environment
$env:NODE_ENV = "development"
$env:PYTHONUNBUFFERED = "1"

# Welcome message
Write-Host "✓ PowerShell environment loaded" -ForegroundColor Green
EOF
    log_success "PowerShell profile created"
else
    log_success "PowerShell profile already exists"
fi
echo ""

# Step 8: Setup database (if using SQLite)
log_info "Initializing local database..."
mkdir -p /workspace/data
if [ ! -f "/workspace/data/helios-dev.db" ]; then
    touch /workspace/data/helios-dev.db
    sqlite3 /workspace/data/helios-dev.db "PRAGMA journal_mode=WAL;"
    log_success "SQLite database initialized at /workspace/data/helios-dev.db"
else
    log_success "SQLite database already exists"
fi
echo ""

# Step 9: Setup .env file
log_info "Checking environment configuration..."
if [ ! -f "/workspace/.env" ]; then
    cat > /workspace/.env << 'EOF'
# Development Environment Configuration
NODE_ENV=development
PYTHONUNBUFFERED=1

# Server Configuration
PORT=8080
API_PORT=3001
DEV_SERVER_PORT=3000
VITE_PORT=5173

# Database Configuration
DB_HOST=postgres
DB_PORT=5432
DB_NAME=helios_dev
DB_USER=devuser
DB_PASSWORD=devpassword

# Wiki Configuration
WIKI_PORT=8080
WIKI_DB_PATH=/workspace/data/helios-dev.db

# Logging
LOG_LEVEL=debug
LOG_DIR=/workspace/logs

# Features
DEBUG=true
VERBOSE=true
EOF
    log_success ".env file created"
else
    log_success ".env file already exists"
fi
echo ""

# Step 10: Verify all tools
log_info "Verifying installed tools..."
echo ""

tools=(
    "bash:Bash"
    "git:Git"
    "node:Node.js"
    "npm:npm"
    "python3:Python 3"
    "pwsh:PowerShell"
    "gh:GitHub CLI"
    "sqlite3:SQLite 3"
    "docker:Docker"
)

for tool_cmd in "${tools[@]}"; do
    tool="${tool_cmd%%:*}"
    tool_name="${tool_cmd##*:}"
    
    if command -v "$tool" &> /dev/null; then
        version=$($tool --version 2>&1 | head -n1)
        log_success "${tool_name}: ${version}"
    else
        log_warning "${tool_name}: not found"
    fi
done
echo ""

# Step 11: Create helpful scripts
log_info "Creating helper scripts..."

# Create quick setup script
cat > /workspace/scripts/setup.sh << 'EOF'
#!/bin/bash
echo "Running Helios Platform setup..."
[ -f "package.json" ] && npm install
[ -f "requirements.txt" ] && pip install -r requirements.txt
[ -f "pyproject.toml" ] && poetry install
echo "Setup complete!"
EOF
chmod +x /workspace/scripts/setup.sh

# Create development server script
cat > /workspace/scripts/dev.sh << 'EOF'
#!/bin/bash
echo "Starting Helios Platform development environment..."
# Add your startup commands here
echo "Development environment started!"
EOF
chmod +x /workspace/scripts/dev.sh

log_success "Helper scripts created in /workspace/scripts/"
echo ""

# Step 12: Generate summary
log_info "Dev environment setup complete!"
echo ""
echo "=========================================="
echo "HELIOS PLATFORM DEV ENVIRONMENT READY"
echo "=========================================="
echo ""
echo "Quick Start:"
echo "  cd /workspace"
echo "  npm install             # Install Node dependencies"
echo "  python3 -m venv .venv  # Create Python venv (if needed)"
echo "  source .venv/bin/activate  # Activate Python venv"
echo ""
echo "Available Tools:"
echo "  • Node.js & npm"
echo "  • Python 3"
echo "  • PowerShell 7.x"
echo "  • GitHub CLI"
echo "  • Docker & Docker Compose"
echo "  • Git with hooks"
echo "  • SQLite 3"
echo ""
echo "Port Mappings:"
echo "  8080  → Wiki Application"
echo "  5432  → PostgreSQL Database"
echo "  3000  → Node.js Dev Server"
echo "  5173  → Vite Dev Server"
echo "  3001  → API Server"
echo ""
echo "Documentation: /workspace/docs"
echo "Logs: /workspace/logs"
echo ""
echo "=========================================="
log_success "Environment ready for development!"

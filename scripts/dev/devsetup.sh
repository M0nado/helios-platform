#!/bin/bash
# Helios Platform Development Setup Script
# This script initializes the development environment for Helios Platform

set -euo pipefail

CHECK_ONLY=false
INSTALL_AZURE_CLI=false
for arg in "$@"; do
    case "$arg" in
        --check-only) CHECK_ONLY=true ;;
        --install-azure-cli) INSTALL_AZURE_CLI=true ;;
        *) echo "[WARN] Unknown option: $arg" ;;
    esac
done

run_or_report() {
    if [ "$CHECK_ONLY" = true ]; then
        log_info "check-only: $*"
    else
        "$@"
    fi
}

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Functions
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

# Main setup
echo ""
echo "=========================================="
echo "Helios Platform Dev Setup"
echo "=========================================="
echo ""

# Check if running in development container
if [ -f "/.dockerenv" ]; then
    log_info "Running inside development container"
    IN_CONTAINER=true
else
    log_warning "Not running inside development container"
    log_info "To use the full development environment, run:"
    echo "  cd .devcontainer && docker-compose up -d"
    IN_CONTAINER=false
fi
echo ""

# Setup Node.js environment
if command -v node &> /dev/null; then
    log_success "Node.js $(node --version) detected"
    
    if [ -f "package.json" ]; then
        log_info "Installing npm dependencies..."
        run_or_report npm install --prefer-offline
        log_success "Dependencies installed"
    else
        log_warning "No package.json found"
    fi
else
    log_warning "Node.js not found"
fi
echo ""

# Setup Python environment
if command -v python3 &> /dev/null; then
    log_success "Python3 detected"
    
    if [ ! -d ".venv" ]; then
        log_info "Creating Python virtual environment..."
        run_or_report python3 -m venv .venv
        if [ "$CHECK_ONLY" != true ]; then
            source .venv/bin/activate
            pip install --upgrade pip setuptools wheel
        fi
        
        if [ -f "requirements.txt" ]; then
            log_info "Installing Python dependencies..."
            run_or_report pip install -r requirements.txt
        fi
        log_success "Python environment ready"
    else
        log_success "Python virtual environment already exists"
    fi
else
    log_warning "Python3 not found"
fi
echo ""

# Setup Azure CLI
if command -v az &> /dev/null; then
    log_success "Azure CLI $(az version --query 'azure-cli' -o tsv 2>/dev/null || az --version | head -n 1) detected"
    log_info "Authenticate when needed with: az login"
elif [ "$INSTALL_AZURE_CLI" = true ]; then
    log_info "Installing Azure CLI using Microsoft installer"
    run_or_report bash -c "curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash"
else
    log_warning "Azure CLI not found"
    log_info "Install with: scripts/dev/devsetup.sh --install-azure-cli"
fi
echo ""

# Repository optimization audit
if [ -x "scripts/dev/repo-optimize.sh" ]; then
    log_info "Running repository merge/optimization audit..."
    scripts/dev/repo-optimize.sh
fi
echo ""

# Setup Git hooks
log_info "Setting up Git hooks..."
if [ -d ".git" ]; then
    run_or_report mkdir -p .git/hooks
    log_success "Git hooks directory ready"
else
    log_warning "Not a git repository"
fi
echo ""

# Create necessary directories
log_info "Creating project directories..."
run_or_report mkdir -p src tests docs scripts logs data
log_success "Directories created"
echo ""

# Setup environment file
if [ ! -f ".env" ]; then
    log_info "Creating .env file..."
    if [ "$CHECK_ONLY" = true ]; then
        log_info "check-only: would create .env"
    else
    cat > .env << 'EOF'
NODE_ENV=development
PYTHONUNBUFFERED=1
DEBUG=true
LOG_LEVEL=debug
EOF
    fi
    log_success ".env file created"
else
    log_success ".env file already exists"
fi
echo ""

# Final message
echo "=========================================="
log_success "Setup complete!"
echo "=========================================="
echo ""
echo "Next steps:"
echo "  1. Review and update .env if needed"
echo "  2. Run: npm install (if using Node.js)"
echo "  3. Run: source .venv/bin/activate (if using Python)"
echo "  4. Run: scripts/dev/repo-optimize.sh"
echo "  5. Start developing!"
echo ""
echo "Development containers:"
echo "  cd .devcontainer && docker-compose up -d"
echo ""

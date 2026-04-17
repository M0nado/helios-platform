#!/bin/bash
# Comprehensive installation and verification guide for Helios Platform
# This script validates the development container setup

set -e

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║   Helios Platform Dev Container - Setup Verification          ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

# Check Docker installation
echo "[1/5] Checking Docker installation..."
if ! command -v docker &> /dev/null; then
    echo "    ✗ Docker not found. Install from: https://www.docker.com/"
    exit 1
fi
DOCKER_VERSION=$(docker --version | awk '{print $3}' | cut -d, -f1)
echo "    ✓ Docker $DOCKER_VERSION installed"

# Check Docker Compose
echo "[2/5] Checking Docker Compose..."
if ! command -v docker-compose &> /dev/null; then
    echo "    ✗ Docker Compose not found"
    exit 1
fi
COMPOSE_VERSION=$(docker-compose --version | awk '{print $3}' | cut -d, -f1)
echo "    ✓ Docker Compose $COMPOSE_VERSION installed"

# Check devcontainer files
echo "[3/5] Checking configuration files..."
FILES=(
    ".devcontainer/devcontainer.json"
    ".devcontainer/Dockerfile"
    ".devcontainer/docker-compose.yml"
    ".devcontainer/onCreateCommand.sh"
    ".vscode/settings.json"
    ".vscode/extensions.json"
    ".gitignore"
    ".editorconfig"
    ".prettierrc"
)

MISSING=0
for file in "${FILES[@]}"; do
    if [ -f "$file" ]; then
        echo "    ✓ $file"
    else
        echo "    ✗ $file (missing)"
        MISSING=$((MISSING + 1))
    fi
done

if [ $MISSING -gt 0 ]; then
    echo "    ✗ $MISSING file(s) missing. Run regeneration script."
    exit 1
fi

# Check JSON validity
echo "[4/5] Validating JSON configurations..."
if command -v jq &> /dev/null; then
    if jq empty .devcontainer/devcontainer.json 2>/dev/null; then
        echo "    ✓ devcontainer.json is valid"
    else
        echo "    ✗ devcontainer.json has syntax errors"
        exit 1
    fi
    
    if jq empty .vscode/settings.json 2>/dev/null; then
        echo "    ✓ .vscode/settings.json is valid"
    else
        echo "    ✗ .vscode/settings.json has syntax errors"
        exit 1
    fi
else
    echo "    ⚠ jq not installed, skipping JSON validation"
fi

# Ready to build
echo "[5/5] Checking Docker daemon..."
if docker ps &> /dev/null; then
    echo "    ✓ Docker daemon is running"
else
    echo "    ✗ Docker daemon is not running"
    exit 1
fi

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║                    Setup Complete!                            ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""
echo "Next Steps:"
echo ""
echo "  1. Open the project in VS Code:"
echo "     code ."
echo ""
echo "  2. When prompted, 'Reopen in Container' or:"
echo "     - Press Ctrl+Shift+P"
echo "     - Select 'Dev Containers: Reopen in Container'"
echo ""
echo "  3. Or start manually:"
echo "     cd .devcontainer"
echo "     docker-compose up -d"
echo "     docker-compose exec devcontainer bash"
echo ""
echo "  4. Verify environment inside container:"
echo "     node --version"
echo "     python3 --version"
echo "     pwsh --version"
echo ""
echo "Documentation: .devcontainer/README.md"
echo ""

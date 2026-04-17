#!/bin/bash
# HELIOS CLI Startup Script
# Used for installation and development

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo "HELIOS CLI Build and Setup Script"
echo "=================================="
echo

# Build the CLI
echo "Building HELIOS CLI..."
cd "$PROJECT_ROOT/src/HELIOS.Platform/Core/CLI"

if command -v dotnet &> /dev/null; then
    dotnet build -c Release
    BUILD_STATUS=$?
else
    echo "Error: .NET SDK not found. Please install .NET 6.0 or later."
    exit 1
fi

if [ $BUILD_STATUS -eq 0 ]; then
    echo "Build successful!"
    
    # Optional: Install to system
    if [ "$1" == "--install" ]; then
        echo "Installing HELIOS CLI to /usr/local/bin..."
        sudo cp "$PROJECT_ROOT/scripts/helios-cli.sh" /usr/local/bin/helios-cli
        sudo chmod +x /usr/local/bin/helios-cli
        echo "Installation complete!"
    fi
else
    echo "Build failed!"
    exit 1
fi

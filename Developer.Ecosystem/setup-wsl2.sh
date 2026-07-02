#!/bin/bash
# MONADO BLADE Developer Ecosystem - WSL2 Setup Script
# Run this inside WSL2 (Ubuntu 24.04)

set -e

echo "🚀 MONADO BLADE Developer Ecosystem - WSL2 Setup"
echo "=================================================="

# Update system
echo "📦 Updating system packages..."
sudo apt-get update
sudo apt-get upgrade -y

# Install essential tools
echo "🔧 Installing essential tools..."
sudo apt-get install -y \
    curl \
    wget \
    git \
    build-essential \
    python3 \
    python3-pip \
    nodejs \
    npm \
    docker.io \
    docker-compose \
    htop \
    tmux \
    vim \
    neofetch

# Install .NET 8.0 SDK
echo "📙 Installing .NET 8.0 SDK..."
sudo apt-get install -y \
    dotnet-sdk-8.0 \
    dotnet-runtime-8.0

# Install Ollama (Hermes LLM backend)
echo "🤖 Installing Ollama..."
curl -fsSL https://ollama.ai/install.sh | sh

# Create dev directories
echo "📂 Creating development directories..."
mkdir -p ~/dev/monado-blade
mkdir -p ~/dev/monado-blade/projects
mkdir -p ~/dev/monado-blade/models
mkdir -p ~/dev/monado-blade/backups

# Configure Docker
echo "🐳 Configuring Docker..."
sudo usermod -aG docker $USER
sudo systemctl enable docker

# Create .bashrc profile for development
echo "⚙️  Setting up development environment..."
cat >> ~/.bashrc << 'EOF'

# MONADO BLADE Developer Environment
export DEV_ROOT=$HOME/dev/monado-blade
export PATH=$PATH:$DEV_ROOT/bin

# Ollama setup
export OLLAMA_HOST=0.0.0.0:11434
export OLLAMA_NUM_GPU=1

# Docker aliases
alias docker-ps="docker ps --format 'table {{.Names}}\t{{.Status}}'"
alias docker-clean="docker system prune -af"

# Development aliases
alias dev="cd $DEV_ROOT"
alias devserve="ollama serve"

neofetch
EOF

source ~/.bashrc

# Download Hermes models
echo "📥 Downloading Hermes LLM models..."
echo "This may take a while (5-20GB depending on models selected)"

# Start Ollama service
sudo systemctl start ollama

# Download models (optional, user can choose)
echo "🤖 Model download options:"
echo "1. hermes-7b (4GB, fast)"
echo "2. hermes-13b (8GB, balanced)"
echo "3. hermes-70b (40GB, best)"
echo ""
echo "Downloading 7B model by default (press Ctrl+C to skip)..."
sleep 5

ollama pull neural-chat:7b || true

# Setup complete
echo ""
echo "✅ WSL2 Setup Complete!"
echo ""
echo "📋 Next steps:"
echo "1. Reload shell: source ~/.bashrc"
echo "2. Start Ollama: ollama serve"
echo "3. Download additional models: ollama pull neural-chat:13b"
echo "4. Navigate to project: cd ~/dev/monado-blade"
echo "5. Build ecosystem: dotnet build"
echo "6. Run GUI: dotnet run"
echo ""
echo "📊 Verify installation:"
echo "   ollama list              # Check models"
echo "   docker ps                # Check Docker"
echo "   dotnet --version         # Check .NET"
echo ""

FROM mcr.microsoft.com/devcontainers/universal:2-focal

LABEL maintainer="Helios Platform Dev Team"
LABEL description="Complete development container for Helios Platform"

# Avoid prompts during installation
ENV DEBIAN_FRONTEND=noninteractive \
    SHELL=/bin/bash \
    PATH="/home/devuser/.local/bin:${PATH}" \
    PYTHONUNBUFFERED=1 \
    NODE_ENV=development

# Install system dependencies and tools
RUN apt-get update && apt-get install -y --no-install-recommends \
    # Core development tools
    build-essential \
    curl \
    wget \
    git \
    ca-certificates \
    openssh-client \
    # Python development
    python3-dev \
    python3-pip \
    python3-venv \
    # Additional tools
    sqlite3 \
    libsqlite3-dev \
    jq \
    tree \
    htop \
    netcat \
    iputils-ping \
    dnsutils \
    telnet \
    vim \
    nano \
    less \
    # Compression tools
    zip \
    unzip \
    tar \
    gzip \
    bzip2 \
    xz-utils \
    # Ruby (for additional tooling)
    ruby \
    # Perl
    perl \
    # Container runtime
    docker.io \
    docker-compose \
    # SSL/TLS
    openssl \
    # Additional utilities
    lsof \
    psmisc \
    procps \
    && apt-get clean && rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Install PowerShell 7.x
RUN curl -fsSL https://github.com/PowerShell/PowerShell/releases/download/v7.4.1/powershell-7.4.1-linux-x64.tar.gz \
    | tar -xzf - -C /opt/microsoft/powershell && \
    ln -s /opt/microsoft/powershell/pwsh /usr/bin/pwsh && \
    chmod +x /usr/bin/pwsh && \
    mkdir -p /usr/local/share/powershell/Modules

# Install Node.js using nvm (latest LTS + latest)
RUN curl -fsSL https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash && \
    bash -c 'export NVM_DIR="/home/devuser/.nvm" && \
    [ -s "$NVM_DIR/nvm.sh" ] && . "$NVM_DIR/nvm.sh" && \
    nvm install 20 && \
    nvm install 22 && \
    nvm use 20 && \
    npm install -g \
      npm@latest \
      yarn \
      pnpm \
      tsx \
      ts-node \
      @nestjs/cli \
      @angular/cli \
      create-react-app \
      vite \
      vercel'

# Install GitHub CLI
RUN (type -p wget >/dev/null || (apt-get update && apt-get install wget -y)) && \
    mkdir -p -m 755 /etc/apt/keyrings && \
    wget -qO- https://cli.github.com/packages/githubcli-archive-keyring.gpg | tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null && \
    apt-get update && apt-get install -y gh && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Upgrade pip and install Python development tools
RUN python3 -m pip install --upgrade pip setuptools wheel && \
    python3 -m pip install --no-cache-dir \
      pipenv \
      poetry \
      black \
      pylint \
      flake8 \
      isort \
      mypy \
      pytest \
      pytest-cov \
      requests \
      click \
      colorama \
      pyyaml \
      python-dotenv \
      psycopg2-binary \
      sqlalchemy \
      flask \
      fastapi \
      uvicorn \
      jupyter \
      jupyter-contrib-nbextensions

# Install additional useful global tools
RUN npm install -g \
    dotenv-cli \
    http-server \
    tldr \
    nodemon \
    @types/node \
    typescript \
    eslint \
    prettier \
    husky \
    lint-staged \
    commitlint

# Create non-root user
RUN if ! id "devuser" &>/dev/null; then \
      useradd -m -s /bin/bash -G docker,sudo devuser && \
      echo "devuser ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers.d/devuser && \
      chmod 0440 /etc/sudoers.d/devuser; \
    fi

# Setup home directory for devuser
RUN mkdir -p /home/devuser/.local/bin && \
    mkdir -p /home/devuser/.config && \
    mkdir -p /home/devuser/.ssh && \
    chmod 700 /home/devuser/.ssh && \
    chown -R devuser:devuser /home/devuser

# Install PowerShell modules
RUN pwsh -Command 'Install-Module -Name PSReadLine -Repository PSGallery -Force -Scope AllUsers' && \
    pwsh -Command 'Install-Module -Name posh-git -Repository PSGallery -Force -Scope AllUsers' && \
    pwsh -Command 'Install-Module -Name oh-my-posh -Repository PSGallery -Force -Scope AllUsers' && \
    pwsh -Command 'Install-Module -Name PSScriptTools -Repository PSGallery -Force -Scope AllUsers' && \
    pwsh -Command 'Install-Module -Name Terminal-Icons -Repository PSGallery -Force -Scope AllUsers' && \
    pwsh -Command 'Install-Module -Name Az -Repository PSGallery -Force -Scope AllUsers -AllowClobber' && \
    pwsh -Command 'Install-Module -Name Pester -Repository PSGallery -Force -Scope AllUsers -SkipPublisherCheck'

# Create /workspace directory
RUN mkdir -p /workspace && \
    chown -R devuser:devuser /workspace

# Create devuser directories
USER devuser
RUN mkdir -p ~/.nvm ~/.npm ~/.local/share/powershell/Modules && \
    echo 'export NVM_DIR="$HOME/.nvm"' >> ~/.bashrc && \
    echo '[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"' >> ~/.bashrc && \
    echo '[ -s "$NVM_DIR/bash_completion" ] && \. "$NVM_DIR/bash_completion"' >> ~/.bashrc

# Setup git configuration defaults
RUN git config --global core.autocrlf input && \
    git config --global core.safecrlf warn && \
    git config --global core.longpaths true && \
    git config --global pull.rebase true

# Configure PowerShell profile
RUN mkdir -p ~/.config/powershell && \
    echo 'Import-Module posh-git' > ~/.config/powershell/profile.ps1 && \
    echo 'Import-Module oh-my-posh' >> ~/.config/powershell/profile.ps1 && \
    echo 'Set-PoshPrompt -Theme paradox' >> ~/.config/powershell/profile.ps1 && \
    echo 'Import-Module Terminal-Icons' >> ~/.config/powershell/profile.ps1 && \
    echo 'Set-PSReadLineOption -PredictionSource History' >> ~/.config/powershell/profile.ps1 && \
    echo 'Set-PSReadLineOption -PredictionViewStyle ListView' >> ~/.config/powershell/profile.ps1

WORKDIR /workspace

# Default shell
ENTRYPOINT ["/bin/bash"]
CMD ["-i"]

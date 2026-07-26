FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-noble

USER root

RUN rm -f /etc/apt/sources.list.d/yarn.list \
    && apt-get update \
    && export DEBIAN_FRONTEND=noninteractive \
    && apt-get install --yes --no-install-recommends \
        build-essential \
        cmake \
        jq \
        ninja-build \
        ripgrep \
        shellcheck \
    && rm -rf /var/lib/apt/lists/*

USER vscode

# HELIOS Platform - Repository Structure

This document describes the reorganized directory structure of the HELIOS Platform repository.

## Directory Layout

### `/src` - Source Code

Main application source code organized by component type:

- **`/src/core/`** - Core platform services and infrastructure
  - `HELIOS.Platform/` - Main platform implementation
  - `HELIOS.Platform.Minimal/` - Minimal executable variant
  - Core services like ConfigurationManager, DatabaseService

- **`/src/gui/`** - User interface components
  - `MonadoBlade.GUI/` - WinUI3-based graphical interface
  - All UI components and view models

- **`/src/adapters/`** - Cloud and external integrations
  - `gateway/` - API Gateway implementation
  - AWS, Azure, cloud-specific integrations
  - Cloud provider SDKs and adapters

- **`/src/utilities/`** - Shared utilities and helpers
  - Common utilities and helper libraries
  - Shared components across projects

- **`/src/tests/`** - Consolidated test projects
  - `HELIOS.Platform.Tests/` - Main test suite
  - `adapters/`, `core/`, `e2e/`, `gateway/` - Component-specific tests
  - `integration/`, `performance/`, `security/` - Specialized test suites
  - Unit, integration, system, and E2E tests

### `/docs` - Documentation

Documentation organized by purpose:

- **`/docs/architecture/`** - System design and architecture
  - System architecture diagrams
  - Component relationships and design patterns
  - LINQ optimization guides
  - Integration layer documentation

- **`/docs/api/`** - API reference documentation
  - API endpoints and specifications
  - Service contracts
  - Integration guides

- **`/docs/guides/`** - How-to and getting started guides
  - Getting started guide
  - Installation instructions
  - Development setup
  - Troubleshooting guides
  - Testing guides

- **`/docs/images/`** - Screenshots and diagrams
  - Architecture diagrams
  - UI screenshots
  - Flowcharts

- **`/docs/phases/`** - Phase-specific documentation and deployment scripts
  - Phase build scripts
  - Phase-specific configurations

### `/scripts` - Automation Scripts

Scripts organized by purpose:

- **`/scripts/build/`** - Build automation
  - `build.ps1` - Main build script
  - CI/CD build automation
  - Compilation and packaging

- **`/scripts/deploy/`** - Deployment automation
  - Deployment scripts for staging and production
  - GitHub setup and integration
  - Installation and setup

- **`/scripts/dev/`** - Development tools
  - Development environment setup
  - Codespace launch scripts
  - Local development helpers

- **`/scripts/test/`** - Test automation
  - Test execution scripts
  - Test orchestration
  - Test result collection

### `/config` - Configuration Files

Application and build configuration:

- **`/config/app-settings/`** - Application settings
  - `appsettings.json` and variants
  - Environment-specific configurations

- **`/config/build/`** - Build configuration
  - Build settings and definitions
  - Compilation configuration

### `/samples` - Example Implementations

Sample projects and usage examples:

- Quick start examples
- Reference implementations
- Integration examples

### `/tests` - Legacy Test Directory

⚠️ **Note**: Most tests have been consolidated to `/src/tests/`. This directory contains remaining placeholder and configuration files.

## Migration Notes

This structure was reorganized from the original flat layout to improve:

1. **Clarity**: Components are grouped logically by type
2. **Maintainability**: Clear separation of concerns
3. **Discoverability**: Easier to find related components
4. **Scalability**: Supports growth and new components
5. **Git History**: All moves used `git mv` to preserve commit history

## Navigation Guide

### Finding Things

- **Looking for a specific service?** → Check `/src/core/HELIOS.Platform/`
- **Need to update the UI?** → Go to `/src/gui/MonadoBlade.GUI/`
- **Cloud integration?** → Look in `/src/adapters/`
- **How do I install this?** → See `/docs/guides/INSTALLATION_GUIDE.md`
- **Understanding the architecture?** → Review `/docs/architecture/`
- **Want to automate a build?** → Check `/scripts/build/`
- **Running tests?** → Look in `/src/tests/`

### Key Files

- **Main Platform**: `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- **GUI Application**: `src/gui/MonadoBlade.GUI/` (find `.csproj` within)
- **Tests**: `src/tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`
- **README**: `README.md` (root level)
- **Getting Started**: `docs/guides/GETTING_STARTED.md`

## Build and Test

### Building the Project

```powershell
# Restore dependencies
dotnet restore

# Build solution
dotnet build src/core/HELIOS.Platform/HELIOS.Platform.csproj

# Build GUI
# (Locate and build MonadoBlade.GUI)
```

### Running Tests

```powershell
# Run all tests
dotnet test src/tests/

# Run specific test project
dotnet test src/tests/HELIOS.Platform.Tests/
```

## Continuous Integration

GitHub Actions workflows have been updated to reference the new paths:

- Build workflows use `src/core/HELIOS.Platform/`
- Test workflows use `src/tests/`
- Documentation paths reference `/docs/`

See `.github/workflows/` for workflow definitions.

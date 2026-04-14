# Submodule Template & Standard Structure

This document defines the standard structure and required files for every HELIOS submodule.

## Directory Structure Template

Every submodule MUST follow this structure:

```
submodules/PHASE-X-ModuleName/
├── README.md                    # Main documentation
├── PLAIN_ENGLISH_GUIDE.md       # User-friendly guide
├── FILE_ARCHITECTURE.md         # Internal structure
├── SCRIPTS_INDEX.md             # Available scripts
├── TESTING_GUIDE.md             # How to test
├── STATUS.json                  # Current status
├── CHANGELOG.md                 # Version history
├── src/                         # Implementation files
│   ├── Public/                  # Public functions
│   ├── Private/                 # Internal functions
│   └── Module.psm1              # Module manifest
├── tests/                       # Test files
│   ├── Unit/
│   ├── Integration/
│   └── E2E/
├── config/                      # Configuration files
├── schema/                      # Data schemas
├── docs/                        # Extended documentation
└── examples/                    # Usage examples
```

## Required Documentation Files

### 1. README.md - Main Entry Point

**Contains**:
- Quick summary (what it does)
- Owner and status
- Dependencies list
- API quick reference
- Integration points
- Current metrics

### 2. PLAIN_ENGLISH_GUIDE.md - No Jargon Version

**Contains**:
- Simple explanation
- Why we need it
- Step-by-step usage
- Common tasks with examples
- Troubleshooting section
- FAQ

### 3. FILE_ARCHITECTURE.md - Code Organization

**Contains**:
- Directory structure
- Key files and their purpose
- Data files explanation
- Registry structure
- File dependencies

### 4. SCRIPTS_INDEX.md - Function Catalog

**Contains**:
- All public functions with signatures
- Parameter descriptions
- Return values
- Usage examples
- Internal functions (reference only)

### 5. TESTING_GUIDE.md - Test Instructions

**Contains**:
- Unit testing instructions
- Integration testing instructions
- E2E testing steps
- Test coverage targets
- Running tests command
- Debugging test failures

### 6. CHANGELOG.md - Version History

**Contains**:
- All versions in reverse chronological order
- Version format: ## v1.0.0 - YYYY-MM-DD
- Features, fixes, breaking changes
- Migration guides for breaking changes

### 7. STATUS.json - Current Status Metrics

**Format**:
```json
{
  "module": "PHASE-1-AppLocker",
  "status": "In Progress",
  "version": "1.0.0-beta",
  "owner": "John Doe",
  "owner_email": "john@example.com",
  "start_date": "2024-01-08",
  "target_completion": "2024-01-22",
  "actual_completion": null,
  "progress_percent": 45,
  "dependencies": ["PHASE-0-System-Setup"],
  "dependents": ["PHASE-1-Quarantine"],
  "test_coverage_percent": 80,
  "unit_tests_passing": true,
  "integration_tests_passing": false,
  "blockers": [
    {
      "issue": "Vault API not finalized",
      "impact": "high",
      "blocking_feature": "Credential storage",
      "owner": "Vault team"
    }
  ],
  "metrics": {
    "code_lines": 1250,
    "test_lines": 2340,
    "cyclomatic_complexity": 8,
    "performance_score": 95
  },
  "last_updated": "2024-01-10T14:30:00Z"
}
```

## Implementation Guidelines

### Public Functions (src/Public/)

**Requirements**:
1. Clear, descriptive name (verb-noun pattern)
2. Comment-based help documentation
3. Parameter validation
4. Error handling
5. Return consistent object types

**Example**:
```powershell
<#
.SYNOPSIS
    Enables AppLocker policies on the system.

.DESCRIPTION
    Configures and enables Windows AppLocker with default rules.
    Requires administrator privileges.

.PARAMETER RuleSet
    Which rule set to apply: Developer, Standard, Strict

.EXAMPLE
    Enable-AppLocker -RuleSet Standard

.NOTES
    Author: Security Team
    Requires: Administrator role
#>
function Enable-AppLocker {
    param(
        [Parameter(Mandatory=$true)]
        [ValidateSet('Developer','Standard','Strict')]
        [string]$RuleSet
    )
    
    # Validation
    if (-not ([Security.Principal.WindowsPrincipal] `
        [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(`
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Must run as administrator"
    }
    
    # Implementation
    try {
        # Actual implementation
        Write-Output @{ Status = "Success"; Version = "1.0" }
    }
    catch {
        Write-Error "Failed to enable AppLocker: $_"
        throw
    }
}
```

### Private Functions (src/Private/)

**Requirements**:
1. Helper functions not exposed to users
2. Minimal documentation (internal notes)
3. Shared utilities between public functions
4. Prefixed with internal naming convention

### Tests (tests/)

**Organization**:
```
tests/
├── Unit/
│   └── AppLocker.Unit.Tests.ps1
├── Integration/
│   └── AppLocker-Vault-Integration.Tests.ps1
└── E2E/
    └── Phase-1-Complete.Tests.ps1
```

**Naming**: `ModuleName.Type.Tests.ps1`

**Requirements**:
1. Use Pester framework
2. Describe blocks for feature groups
3. It blocks for individual tests
4. Minimum 80% code coverage
5. All tests must pass before merge

### Configuration Files (config/)

**schema.json** - Data format specification
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "rules": {
      "type": "array",
      "description": "AppLocker rules"
    }
  },
  "required": ["rules"]
}
```

**default-config.json** - Default values
```json
{
  "enabled": true,
  "enforcement_mode": "Audit",
  "rules_version": "1.0.0",
  "log_path": "C:\\ProgramData\\HELIOS\\AppLocker\\logs",
  "backup_enabled": true
}
```

### Schema Files (schema/)

Define data structures exchanged with other submodules:

```
schema/
├── configuration.schema.json
├── data.schema.json
└── events.schema.json
```

## Version Numbering

**Format**: MAJOR.MINOR.PATCH (-PrereleaseTAG)

- **MAJOR**: Breaking changes (1.0.0 → 2.0.0)
- **MINOR**: New features, backward compatible (1.0.0 → 1.1.0)
- **PATCH**: Bug fixes (1.0.0 → 1.0.1)
- **Prerelease**: alpha, beta, rc (1.0.0-alpha, 1.0.0-beta2)

**Examples**:
- v1.0.0 - Initial release
- v1.1.0 - Added bulk rule import
- v1.0.1 - Fixed registry parsing bug
- v2.0.0 - BREAKING: Removed deprecated functions
- v2.0.0-beta1 - Beta for v2 with breaking changes

## API Contracts

Each submodule must define contracts with modules that depend on it.

**Contract Definition** (in docs/API_REFERENCE.md):
```markdown
# API Contract for PHASE-1-Vault

## Public Functions

### New-SecretVault
```
New-SecretVault -Name <string> -MasterPassword <string> -BackupEnabled <bool>
```
**Returns**: Object with properties:
- VaultId (string): Unique identifier
- CreatedAt (datetime): Creation timestamp
- Status (string): "Ready" | "Error"

**Throws**: If vault already exists or encryption fails

### Save-Secret
```
Save-Secret -VaultId <string> -Name <string> -Secret <string>
```
**Returns**: Object with properties:
- SecretId (string)
- SavedAt (datetime)
- Encrypted (bool)

**Throws**: If vault not found or encryption fails

### Get-Secret
```
Get-Secret -VaultId <string> -SecretId <string>
```
**Returns**: String (decrypted secret)

**Throws**: If secret not found or unauthorized
```

## Integration Checklist

When creating a new submodule:

- [ ] Directory structure created per template
- [ ] README.md written with quick summary
- [ ] PLAIN_ENGLISH_GUIDE.md written for users
- [ ] FILE_ARCHITECTURE.md documents code organization
- [ ] SCRIPTS_INDEX.md catalogs all public functions
- [ ] TESTING_GUIDE.md explains how to test
- [ ] STATUS.json created with initial values
- [ ] CHANGELOG.md started
- [ ] src/ directory with Public/, Private/ folders
- [ ] tests/ directory with Unit/, Integration/, E2E/ folders
- [ ] config/ directory with schema and defaults
- [ ] Example with Module.psm1 created
- [ ] Basic unit tests written
- [ ] Integration tests defined
- [ ] API contract documented in docs/
- [ ] README linked from submodules/README.md

## Updating Submodule Status

**When to update STATUS.json**:
1. When you start work (progress_percent > 0)
2. Daily during active development
3. When tests pass/fail (status of tests)
4. When blockers appear/resolve
5. When you complete work (status = "Done")

**Commands to help**:
```powershell
# View status
Get-Content "submodules/PHASE-1-AppLocker/STATUS.json" | ConvertFrom-Json

# Update progress
$status = Get-Content "submodules/PHASE-1-AppLocker/STATUS.json" | ConvertFrom-Json
$status.progress_percent = 75
$status | ConvertTo-Json | Set-Content "submodules/PHASE-1-AppLocker/STATUS.json"
```

---

**Document Version**: 1.0  
**Last Updated**: 2024  
**Maintained By**: Architecture Team

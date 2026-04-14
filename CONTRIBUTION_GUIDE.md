# How to Contribute to HELIOS Platform

## Getting Started

### 1. Pick a Submodule

Start with **submodules/README.md** to find available submodules.

**Filter by**:
- Status: Planned (just starting), In Progress (join team)
- Phase: 0 (foundation), 1 (security), 2 (optimization), 3 (intelligence)
- Team size: Solo (1 dev), Pair (2 devs), Team (3+ devs)

Example:
```
PHASE-1-AppLocker
├─ Status: In Progress
├─ Owner: John Doe
├─ Team size: 2 developers needed
├─ Dependencies: PHASE-0-System-Setup v1.0+
└─ Link: submodules/PHASE-1-AppLocker/README.md
```

### 2. Read the Submodule Docs

Read in this order:

1. **README.md** - What it does, owner, current status, API overview
2. **PLAIN_ENGLISH_GUIDE.md** - How to use it, examples, common tasks
3. **FILE_ARCHITECTURE.md** - How code is organized
4. **SUBMODULE_TEMPLATE.md** - Structure you'll follow

### 3. Understand the Interface

Every submodule has an interface contract. Read it carefully.

Example: PHASE-1-AppLocker interface
```powershell
# What functions are public?
Enable-AppLocker -RuleSet <string>
New-AppLockerRule -Path <string> -RuleType <string>
Get-AppLockerStatus
Invoke-AppLockerValidation

# What data format is used?
# See schema/applocker-schema.json for details

# What does it depend on?
# Must have PHASE-0-System-Setup v1.0+
# Must provide encryption through PHASE-1-Vault

# What depends on it?
# PHASE-1-Quarantine uses its APIs
# Must not break between versions
```

### 4. Set Up Your Environment

```powershell
# Clone the repository
git clone https://github.com/your-org/helios-platform.git
cd helios-platform

# Navigate to your submodule
cd submodules/PHASE-1-AppLocker

# Install Pester for testing
Install-Module Pester -Force

# Check that tests run
Invoke-Pester tests/ -Verbose
```

### 5. Understand the Development Process

```
Feature Development Flow:

1. Create Feature Branch
   └─ git checkout -b feature/your-feature-name

2. Make Changes
   ├─ Write code in src/
   ├─ Write tests in tests/
   ├─ Update docs/
   └─ All tests passing

3. Validate Locally
   ├─ Run unit tests
   ├─ Run integration tests
   ├─ Manual testing
   └─ Check for breaking changes

4. Create Pull Request
   ├─ Describe what changed
   ├─ Reference any issues
   ├─ Explain design decisions
   └─ Link to docs

5. Code Review
   ├─ Team reviews changes
   ├─ Address feedback
   ├─ Update based on review
   └─ Repeat until approved

6. Integration Review
   ├─ Integration team checks
   ├─ API compatibility verified
   ├─ No breaking changes
   └─ Merge conflicts resolved

7. Merge
   └─ Code merged to main branch

8. Release
   ├─ Version bumped
   ├─ Tag created
   ├─ Release notes published
   └─ Teams notified
```

## Making Changes

### Step 1: Write Tests First

Use test-driven development:

```powershell
# tests/Unit/AppLocker.Unit.Tests.ps1
Describe "New-AppLockerRule" {
    It "creates executable rule" {
        $rule = New-AppLockerRule -Path "C:\Program Files\*" -RuleType "Executable"
        $rule.Type | Should -Be "Executable"
        $rule.Path | Should -Be "C:\Program Files\*"
    }
    
    It "throws on missing parameters" {
        { New-AppLockerRule -Path "C:\*" } | Should -Throw
    }
}

# Run test (will fail, as expected)
Invoke-Pester tests/Unit/AppLocker.Unit.Tests.ps1 -Verbose
```

### Step 2: Implement the Feature

```powershell
# src/Public/New-AppLockerRule.ps1
function New-AppLockerRule {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Path,
        
        [Parameter(Mandatory=$true)]
        [ValidateSet("Executable","DLL","Script","Installer")]
        [string]$RuleType
    )
    
    # Validate path
    if (-not (Test-Path -Path $Path -IsValid)) {
        throw "Invalid path: $Path"
    }
    
    # Create rule
    $rule = @{
        Type = $RuleType
        Path = $Path
        CreatedAt = Get-Date
        Id = [guid]::NewGuid().ToString()
    }
    
    return $rule
}
```

### Step 3: Run Tests

```powershell
# Run all tests for your submodule
Invoke-Pester tests/ -Verbose

# Run specific test file
Invoke-Pester tests/Unit/AppLocker.Unit.Tests.ps1 -Verbose

# Run with code coverage
Invoke-Pester tests/ -CodeCoverage src/Public/*.ps1 -Verbose
```

### Step 4: Update Documentation

Update README.md and SCRIPTS_INDEX.md with new function:

```markdown
### New-AppLockerRule

Creates a new AppLocker rule with specified parameters.

Usage:
```powershell
New-AppLockerRule -Path "C:\Program Files\*" -RuleType "Executable"
```

Parameters:
- Path: Path pattern for the rule
- RuleType: Type of files to control

Returns: Rule object with Id, Type, Path, CreatedAt
```

### Step 5: Update STATUS.json

Track your progress:

```powershell
$status = Get-Content "STATUS.json" | ConvertFrom-Json
$status.progress_metrics.overall_percent = 65  # You're 65% done
$status.quality_metrics.unit_tests_passing = 18
$status.last_updated = Get-Date -AsUTC
$status | ConvertTo-Json | Set-Content "STATUS.json"
```

## Submitting Your Work

### Create a Pull Request

1. Push your branch
```powershell
git push origin feature/your-feature-name
```

2. Go to GitHub and create PR
   - Title: "Add new AppLocker rule creation feature"
   - Description:
     ```
     ## What changed
     Added New-AppLockerRule function for programmatic rule creation
     
     ## Tests
     - 3 new unit tests
     - 2 integration tests
     - All passing
     
     ## Breaking changes
     None
     
     ## Resolves
     Issue #123
     ```

3. Link to documentation
   ```
   See PHASE-1-AppLocker/PLAIN_ENGLISH_GUIDE.md for usage examples
   ```

### Code Review

Team reviews for:
- ✓ Code correctness
- ✓ Test coverage >= 80%
- ✓ No breaking changes
- ✓ Documentation complete
- ✓ Performance acceptable

Address all feedback before merge.

### Integration Review

Integration team validates:
- ✓ API contract honored
- ✓ No data format incompatibilities
- ✓ All tests passing
- ✓ No conflicts with other modules

### Merge

Once approved:
```powershell
# Merge PR (via GitHub UI or)
git checkout main
git pull
git merge feature/your-feature-name
git push
```

## Testing Guidelines

### Unit Test Coverage

Minimum 80% code coverage:

```powershell
# Example unit tests
Describe "AppLocker Module" {
    Context "Enable-AppLocker" {
        It "enables AppLocker in audit mode" { }
        It "enables AppLocker in enforce mode" { }
        It "throws if not admin" { }
    }
    
    Context "New-AppLockerRule" {
        It "creates executable rule" { }
        It "creates DLL rule" { }
        It "validates path" { }
    }
}
```

### Integration Tests

Test with dependencies:

```powershell
# AppLocker + Vault integration
Describe "AppLocker Vault Integration" {
    It "stores rules in Vault" {
        $vault = New-SecretVault
        $rule = New-AppLockerRule -Path "C:\*"
        Save-RuleToVault -Vault $vault -Rule $rule
        $retrieved = Get-RuleFromVault -Vault $vault -RuleId $rule.Id
        $retrieved | Should -Not -Be $null
    }
}
```

### Running Full Test Suite

```powershell
# Before pushing any code
cd submodules/PHASE-1-AppLocker

# Run all tests
Invoke-Pester tests/ -Verbose

# Verify coverage
$results = Invoke-Pester tests/ -CodeCoverage src/Public/*.ps1 -PassThru
$results.CodeCoverage.CoveragePercent  # Should be >= 80
```

## Dealing with Blockers

### When You're Blocked

Example: Can't finish AppLocker tests because Vault API not ready

1. **Alert your team lead**
   - "I'm blocked on Vault API"
   - "Expected resolution: Jan 15"

2. **Add to STATUS.json**
   ```json
   "blockers": [
     {
       "issue": "Vault API not finalized",
       "blocking_feature": "Rule storage",
       "blocker_module": "PHASE-1-Credential-Vault"
     }
   ]
   ```

3. **Continue with what you can**
   - Write tests for when API is ready (mock the API)
   - Complete other features
   - Prepare integration code

4. **Mock the dependency**
   ```powershell
   # Mock Vault API until real one ready
   function Save-RuleToVault {
     param($Vault, $Rule)
     # Mock implementation
     return @{ Status = "Saved" }
   }
   
   # Use real implementation later
   ```

## Asking for Help

### If You Need Guidance

1. Check **PLAIN_ENGLISH_GUIDE.md** in your submodule
2. Check **docs/** folder for examples
3. Look at similar submodules for patterns
4. Contact your submodule owner
5. Ask in team Slack/chat

### If You Find a Bug in Another Module

1. Create an issue in GitHub
2. Email the module owner
3. In STATUS.json, add blocker pointing to the bug

## Integration with Other Submodules

### When Your Submodule Uses Another

1. Import the module
```powershell
Import-Module "../PHASE-1-Credential-Vault" -Function "New-SecretVault", "Save-Secret"
```

2. Use its public APIs
```powershell
$vault = New-SecretVault -Name "AppLocker"
Save-Secret -Vault $vault -Name "rule-backup" -Secret $serializedRule
```

3. Write integration tests
```powershell
# tests/Integration/AppLocker-Vault-Integration.Tests.ps1
Describe "AppLocker Vault Integration" {
    It "can store and retrieve rules" { }
}
```

4. Document the integration
```markdown
# Integration Points

## PHASE-1-Credential-Vault
Stores encrypted rule backups and credentials.

Usage:
```powershell
$vault = New-SecretVault -Name "AppLocker"
Save-AppLockerRuleBackup -Vault $vault -Rules $rules
```
```

## Best Practices

1. **Test First** - Write tests before implementation
2. **Document Always** - Update docs with every change
3. **Small Changes** - Keep PRs focused and reviewable
4. **Update Status** - Keep STATUS.json current
5. **No Breaking Changes** - Same major version = compatible
6. **Clean Code** - Readable, commented where needed
7. **Error Handling** - Handle edge cases gracefully
8. **Security** - Review security implications

---

**Document Version**: 1.0  
**Last Updated**: 2024  
**Maintained By**: Developer Relations Team

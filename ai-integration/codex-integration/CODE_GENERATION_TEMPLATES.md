# Code Generation Templates for GitHub Codex

Templates for generating AppLocker scripts with GitHub Codex.

## Template 1: AppLocker Publisher Rule Script

```
Create PowerShell script that:
1. Generates AppLocker Publisher rules
2. Accepts app path and publisher parameters
3. Returns rule as PSObject
4. Includes error handling
5. Logs to Windows Event Log
```

## Template 2: Rule Consolidation Script

```
Create PowerShell script that:
1. Analyzes existing AppLocker rules
2. Identifies consolidation opportunities
3. Merges compatible rules
4. Validates consolidated rules
5. Generates change report
6. Creates rollback script
```

## Template 3: Pester Test Generation

```
Generate comprehensive Pester tests that:
1. Test rule evaluation logic
2. Verify no false positives
3. Verify no false negatives
4. Test conflicting rules
5. Test exception handling
6. Mock API responses
```

## Template 4: AppLocker Configuration

```
Create PowerShell script that:
1. Configures AppLocker on computer
2. Applies predefined rules
3. Sets audit policy
4. Configures Group Policy integration
5. Validates configuration
6. Generates compliance report
```

## Template 5: Documentation Generation

```
Create PowerShell script that:
1. Reads AppLocker configuration
2. Extracts all rules
3. Generates markdown documentation
4. Creates ASCII diagrams
5. Includes usage examples
6. Exports to HTML/PDF
```

## Template 6: Security Audit Script

```
Create PowerShell script that:
1. Audits AppLocker configuration
2. Checks for security gaps
3. Identifies rule conflicts
4. Evaluates bypass potential
5. Assesses performance impact
6. Generates risk report
```

## Usage Example

```powershell
. .\scripts\ask-codex.ps1

$spec = @"
Generate PowerShell function:
- Name: Invoke-AppLockerOptimization
- Purpose: Find and merge similar rules
- Input: Array of rules
- Output: Consolidated rules
- Validation: Test for conflicts
"@

$script = Invoke-Codex -Spec $spec -Language "powershell" `
    -AddSafetyChecks $true

Write-Host $script
```

## Safety Checklist

- [ ] No hardcoded credentials
- [ ] Proper error handling
- [ ] Input validation
- [ ] Logging enabled
- [ ] Tested non-production
- [ ] Security reviewed
- [ ] Performance acceptable
- [ ] Documentation complete

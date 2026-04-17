# HELIOS AI Integration - Reference Card

Quick reference for all AI integration functions and workflows.

## PowerShell Functions

### ChatGPT Integration

```powershell
# Load the module
. .\scripts\ask-chatgpt.ps1

# Basic usage
Invoke-ChatGPT -Prompt "Your question" -SystemPrompt "HELIOS Optimizer"

# With all options
Invoke-ChatGPT `
    -Prompt "Question" `
    -SystemPrompt "HELIOS Optimizer" `
    -Model "gpt-4" `
    -Temperature 0.7 `
    -MaxTokens 2000 `
    -UseCache $true `
    -CacheDuration (New-TimeSpan -Hours 24)

# Extract response text
$response = Invoke-ChatGPT -Prompt "..."
$response.choices[0].message.content
```

### Codex Integration

```powershell
# Load the module
. .\scripts\ask-codex.ps1

# Basic code generation
Invoke-Codex -Spec "Generate AppLocker script"

# With safety checks
Invoke-Codex `
    -Spec "Your specification" `
    -Language "powershell" `
    -AddSafetyChecks $true `
    -IncludeErrorHandling $true `
    -IncludeTests $true `
    -ValidateBeforeReturn $true
```

### AI Coordination

```powershell
# Load all modules
. .\scripts\ask-chatgpt.ps1
. .\scripts\ask-codex.ps1
. .\scripts\coordinate-ai.ps1

# Coordinate responses
Invoke-AICoordination `
    -ChatGPTResponse $gptResponse `
    -CodexResponse $codexResponse `
    -ConflictResolution $true `
    -GenerateReport $true

# Get statistics
Get-AICoordinationStats -Days 30
```

## System Prompts

```powershell
# HELIOS Optimizer - Optimization suggestions
"You are a HELIOS AppLocker optimization specialist..."

# Security Architect - Security analysis
"You are a HELIOS Security Architect..."

# Performance Analyst - Performance impact
"You are a HELIOS Performance Analyst..."

# Build Advisor - Phase recommendations
"You are a HELIOS Build Advisor..."

# Conflict Detector - Rule conflicts
"You are a HELIOS Conflict Detection Expert..."

# Code Reviewer - Code analysis
"You are a HELIOS Code Review Expert..."
```

## Common Workflows

### Workflow 1: Get Optimization Advice

```powershell
. .\scripts\ask-chatgpt.ps1

$advice = Invoke-ChatGPT `
    -Prompt "I have 500 AppLocker rules. How can I optimize them?" `
    -SystemPrompt "HELIOS Optimizer" `
    -Model "gpt-4"

$advice.choices[0].message.content
```

### Workflow 2: Generate Consolidation Script

```powershell
. .\scripts\ask-codex.ps1

$spec = "Generate PowerShell script to consolidate AppLocker rules"

$script = Invoke-Codex `
    -Spec $spec `
    -Language "powershell" `
    -AddSafetyChecks $true

# Save and use
$script | Out-File -FilePath "consolidate.ps1"
```

### Workflow 3: Security Review

```powershell
. .\scripts\ask-chatgpt.ps1

$review = Invoke-ChatGPT `
    -Prompt "Review this AppLocker configuration for security gaps" `
    -SystemPrompt "Security Architect" `
    -Model "gpt-4"

$review.choices[0].message.content
```

### Workflow 4: Full Coordination

```powershell
# 1. Get plan from ChatGPT
. .\scripts\ask-chatgpt.ps1
$plan = Invoke-ChatGPT `
    -Prompt "Should we consolidate rules?" `
    -SystemPrompt "Build Advisor" `
    -Model "gpt-4"

# 2. Generate code from plan
. .\scripts\ask-codex.ps1
$code = Invoke-Codex `
    -Spec $plan.choices[0].message.content `
    -Language "powershell" `
    -AddSafetyChecks $true

# 3. Review code
$review = Invoke-ChatGPT `
    -Prompt "Review this code: $code" `
    -SystemPrompt "Code Reviewer" `
    -Model "gpt-4"

# 4. Coordinate
. .\scripts\coordinate-ai.ps1
$final = Invoke-AICoordination `
    -ChatGPTResponse $plan `
    -CodexResponse $code `
    -ConflictResolution $true `
    -GenerateReport $true
```

## API Models

```powershell
# GPT-4 (Most capable, most expensive)
-Model "gpt-4"                    # $0.03/$0.06 per 1K tokens

# GPT-3.5-Turbo (Faster, cheaper)
-Model "gpt-3.5-turbo"            # $0.0005/$0.0015 per 1K tokens

# Choose based on:
# - gpt-4: Complex analysis, security decisions, accuracy critical
# - gpt-3.5-turbo: Quick questions, general guidance, cost sensitive
```

## Temperature Settings

```powershell
# Analytical responses (0.0-0.3)
-Temperature 0.1                  # Very deterministic, factual
-Temperature 0.3                  # Analytical, consistent

# Balanced (0.5-0.7)
-Temperature 0.5                  # Mix of accuracy and variety
-Temperature 0.7                  # Default, good for most tasks

# Creative responses (0.8-1.0)
-Temperature 0.9                  # Creative, varied
-Temperature 1.0                  # Maximum randomness
```

## Response Extraction

```powershell
# From ChatGPT responses
$response = Invoke-ChatGPT -Prompt "..."
$textContent = $response.choices[0].message.content
$tokenUsage = $response.usage  # { prompt_tokens, completion_tokens, total_tokens }

# From Codex responses (returns string)
$code = Invoke-Codex -Spec "..."
# $code is the generated code string
```

## Caching

```powershell
# Enable caching (saves API calls and money)
$cached = Invoke-ChatGPT -Prompt "..." -UseCache $true

# Set cache duration
-CacheDuration (New-TimeSpan -Hours 24)     # Cache for 1 day
-CacheDuration (New-TimeSpan -Days 7)       # Cache for 1 week
-CacheDuration (New-TimeSpan -Days 30)      # Cache for 1 month

# Cache location
# Windows: $env:LOCALAPPDATA\helios-ai-cache
```

## Logging

```powershell
# Logs are automatically created
# Location: $env:LOCALAPPDATA\helios-ai-logs

# Log files
# - chatgpt-YYYYMMDD.log           # ChatGPT interactions
# - codex-YYYYMMDD.log             # Codex generations
# - coordination-YYYYMMDD.log      # Coordination events

# View logs
Get-Content "$env:LOCALAPPDATA\helios-ai-logs\chatgpt-*.log"
```

## Environment Variables

```powershell
# Required
$env:OPENAI_API_KEY = "sk-..."   # ChatGPT and Codex API key

# Optional
$env:GITHUB_COPILOT_API_KEY = "ghp-..."  # Direct Copilot access
```

## File Locations

```
C:\Users\ADMIN\helios-platform\ai-integration\
├── README.md                                      # Main documentation
├── QUICK_START.md                                 # This guide
├── REFERENCE.md                                   # Reference card
├── chatgpt-integration\
│   ├── README.md                                  # ChatGPT setup
│   └── SYSTEM_PROMPTS.md                          # Pre-built prompts
├── codex-integration\
│   ├── README.md                                  # Codex setup
│   └── CODE_GENERATION_TEMPLATES.md               # Code templates
├── ai-coordination\
│   ├── README.md                                  # Coordination guide
│   ├── CONFLICT_RESOLUTION.md                     # Conflict handling
│   └── VERSION_CONTROL.md                         # Code tracking
├── scripts\
│   ├── ask-chatgpt.ps1                           # ChatGPT integration
│   ├── ask-codex.ps1                             # Codex integration
│   └── coordinate-ai.ps1                         # Coordination
└── .github\workflows\
    └── ai-code-review.yml                        # GitHub Actions
```

## Troubleshooting

```powershell
# Check API key
if ($env:OPENAI_API_KEY) { "✓ Key set" } else { "✗ Key missing" }

# Test ChatGPT
Invoke-ChatGPT -Prompt "Hello" -Model "gpt-3.5-turbo"

# Test Codex
Invoke-Codex -Spec "Say hello in PowerShell" -Language "powershell"

# Check logs
Get-ChildItem "$env:LOCALAPPDATA\helios-ai-logs\" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 5
```

## Cost Estimation

```powershell
# GPT-4 costs approximately:
# Average prompt: 200 tokens
# Average response: 500 tokens
# Per request: (200 * $0.03 + 500 * $0.06) / 1000 = $0.036

# GPT-3.5-Turbo costs approximately:
# Same tokens: (200 * $0.0005 + 500 * $0.0015) / 1000 = $0.00085

# Savings with caching: ~70% reduction for repeated questions
# Savings with GPT-3.5: ~40x cheaper than GPT-4
```

## Decision Matrix

```
Choose ChatGPT if you need:
  ✓ Strategic analysis
  ✓ Security decisions
  ✓ Complex reasoning
  ✓ Accuracy critical
  → Use: gpt-4

Choose Codex if you need:
  ✓ Code generation
  ✓ Scripts and templates
  ✓ Automation
  ✓ Fast turnaround
  → Use: codex-v1

Choose Coordination if you need:
  ✓ Both planning and code
  ✓ Conflict resolution
  ✓ Unified recommendations
  → Use: Invoke-AICoordination
```

## Common Prompts

```powershell
# Optimization
"Analyze my AppLocker rules and suggest consolidation opportunities"

# Security
"What are the security gaps in this configuration?"

# Performance
"Will these rules impact performance? How can I optimize?"

# Planning
"What phases should we follow? What order?"

# Testing
"Generate test cases for this script"

# Documentation
"Generate markdown documentation for this code"

# Review
"Review this code for best practices and issues"

# Conflict Detection
"Will these two rules conflict with each other?"
```

## Success Metrics

```powershell
# Track these metrics
# - API calls per day (trending down = better)
# - Total cost (track for budget)
# - Cache hit rate (target: >50%)
# - Code generation quality (manual review score)
# - Conflict resolutions (target: >95% automated)
# - Security issues found (by AI scanning)
```

## Next Steps

1. Load scripts: `. .\scripts\ask-chatgpt.ps1`
2. Set API key: `$env:OPENAI_API_KEY = "sk-..."`
3. Test basic: `Invoke-ChatGPT -Prompt "Hello"`
4. Try a task: `Invoke-ChatGPT -Prompt "Optimize AppLocker" -SystemPrompt "HELIOS Optimizer"`
5. Generate code: `Invoke-Codex -Spec "Generate AppLocker script"`
6. Coordinate: `Invoke-AICoordination -ChatGPTResponse $gpt -CodexResponse $codex`

---

**Last Updated**: 2024-01-15  
**Version**: 1.0  
**Maintained By**: HELIOS Team

# HELIOS AI Services Hub

Multi-AI integration system coordinating gpt-5.4, gpt-5.4-mini, and gpt-5.5 for intelligent development.

---

## 🤖 Three AI Services

### 1. gpt-5.4
**Best for:** Strategic analysis, code review, optimization suggestions

```powershell
# Get optimization suggestions
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4" -Task "analyze-build" -BuildVariant "phase-2"

# Code review
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4" -Task "review-code" -FilePath "scripts/security/baseline/applock/setup.ps1"

# Generate documentation
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4" -Task "document-code" -FilePath "scripts/optimization/level-2/tune.ps1"
```

**Strengths:**
- Advanced reasoning
- Context-aware suggestions
- Strategic thinking
- Best for high-level decisions

---

### 2. gpt-5.4-mini
**Best for:** Code generation, refactoring, test creation

```powershell
# Generate code from description
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4_mini" -Task "generate-code" `
  -Description "Create AppLocker rule for Microsoft Office"

# Refactor existing code
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4_mini" -Task "refactor" `
  -FilePath "scripts/optimization/level-2/tune.ps1" -Goal "improve-performance"

# Generate tests
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4_mini" -Task "generate-tests" `
  -FilePath "scripts/build-agents/agent-1-storage/run.ps1"
```

**Strengths:**
- Code generation
- Refactoring
- Test creation
- Specific implementations

---

### 3. gpt-5.5 (Latest Model)
**Best for:** Complex analysis, architectural decisions, multi-component reasoning

```powershell
# Analyze build architecture
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_5" -Task "analyze-architecture" -BuildVariant "complete"

# Design new component
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_5" -Task "design-component" `
  -Description "Design distributed sync component for multi-machine support"

# Complex problem solving
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_5" -Task "solve-problem" -Problem "component-conflict"
```

**Strengths:**
- Advanced reasoning
- Multi-step problem solving
- Architectural decisions
- Complex analysis

---

## 🎯 Service Router

Automatically routes tasks to best AI service:

```powershell
# Smart routing (auto-selects best service)
.\scripts\ai-services\smart-route.ps1 -Task "optimize-security" -Context "build phase-2"

# Returns: Recommended service + reasoning
# "Use ChatGPT (97% confidence): Strategic security review"
```

### Routing Logic
```
Task Type              | Primary Service | Secondary | Tertiary
-----------------------|-----------------|-----------|----------
Code Generation        | gpt-5.4-mini           | gpt-5.5   | ChatGPT
Code Refactoring       | gpt-5.4-mini           | gpt-5.5   | ChatGPT
Test Creation          | gpt-5.4-mini           | gpt-5.5   | ChatGPT
Code Review            | ChatGPT         | gpt-5.5   | gpt-5.4-mini
Documentation Gen      | ChatGPT         | gpt-5.4-mini     | gpt-5.5
Optimization Suggest   | ChatGPT         | gpt-5.5   | gpt-5.4-mini
Bug Detection          | ChatGPT         | gpt-5.5   | gpt-5.4-mini
Security Analysis      | ChatGPT         | gpt-5.5   | gpt-5.4-mini
Architecture Design    | gpt-5.5         | ChatGPT   | gpt-5.4-mini
Component Analysis     | gpt-5.5         | ChatGPT   | gpt-5.4-mini
Conflict Resolution    | gpt-5.5         | ChatGPT   | gpt-5.4-mini
Build Optimization     | ChatGPT         | gpt-5.5   | gpt-5.4-mini
```

---

## 🔄 Consensus Recommendations

When multiple services disagree:

```powershell
# Get multi-service analysis
.\scripts\ai-services\consensus.ps1 -Task "optimize-phase-2" -Confidence 0.85

# Returns consensus with:
# - Agreement level (how much services agree)
# - Conflicting points
# - Recommended approach
# - Alternative approaches
# - Confidence scores
```

---

## 💰 Cost Management

Track usage and costs across services:

```powershell
# View usage and costs
.\scripts\ai-services\show-costs.ps1
# Shows per-service breakdown, totals, trends

# Set cost limits
.\scripts\ai-services\set-cost-limits.ps1 -GPT4 100 -gpt-5.4-mini 50 -GPT55 75
# Cost in USD per month

# View usage analytics
.\scripts\ai-services\show-usage.ps1 -Period "week"
```

---

## 🛡️ Configuration

### Setup (First Time)

```powershell
.\scripts\ai-services\setup-ai-hub.ps1
# 1. Enter gpt-5.4 API key
# 2. Enter gpt-5.4-mini API key
# 3. Enter gpt-5.5 API key
# 4. Set cost limits
# 5. Verify all services
```

### .env File (Sensitive - NOT IN GIT)
```bash
CHATGPT_API_KEY="sk-..."
OPENAI_MODEL="gpt-5.4"
CHATGPT_ENABLED=true

CODEX_API_KEY="sk-..."
CODEX_TEMPERATURE=0.3
CODEX_ENABLED=true

GPT55_API_KEY="sk-..."
GPT55_MODEL="gpt-5.5"
GPT55_ENABLED=true

# Cost limits (USD per month)
COST_LIMIT_TOTAL=300
COST_LIMIT_CHATGPT=100
COST_LIMIT_CODEX=50
COST_LIMIT_GPT55=150

# Rate limiting
RATE_LIMIT_REQUESTS_PER_MINUTE=30
RATE_LIMIT_REQUESTS_PER_DAY=500
```

### Service Weights (ai-services-config.json)
```json
{
  "service_weights": {
    "code_generation": {"gpt_5_4_mini": 0.9, "gpt_5_5": 0.1},
    "code_review": {"gpt_5_4": 0.7, "gpt_5_5": 0.3},
    "optimization": {"gpt_5_4": 0.6, "gpt_5_5": 0.4},
    "architecture": {"gpt_5_5": 0.8, "gpt_5_4": 0.2}
  },
  "min_confidence_for_auto_approval": 0.9,
  "conflict_resolution_strategy": "consensus"
}
```

---

## 🔐 Security

### API Key Protection
- ✅ Never stored in code
- ✅ Environment variables only
- ✅ Encrypted .env file (optional)
- ✅ No logging of API keys
- ✅ Audit trail of API calls (sanitized)

### Safety Checks
- ✅ Dry-run mode for all changes
- ✅ User approval for risky suggestions
- ✅ Automatic rollback capability
- ✅ Complete change tracking
- ✅ Version history

---

## 📊 Audit & Tracking

### Audit Trail
Track every AI interaction:

```powershell
# View audit log
.\scripts\ai-services\view-audit-log.ps1

# Shows:
# - Timestamp
# - AI Service used
# - Task/Prompt (sanitized)
# - Response
# - User action (approved/rejected)
# - Cost
```

### Change Attribution
Every AI-made change is tracked:

```powershell
# Show what AI changed
.\scripts\ai-services\show-ai-changes.ps1 -Since "1-week-ago"

# Can rollback any AI change
.\scripts\ai-services\rollback-ai-change.ps1 -ChangeID "ai-2026-04-13-001"
```

---

## 🚀 Common Workflows

### Workflow 1: Optimize Current Build

```powershell
# 1. Analyze current build
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4" `
  -Task "analyze-build" -BuildVariant "phase-2"

# 2. Get optimization suggestions
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_5" `
  -Task "suggest-optimizations" -BuildVariant "phase-2"

# 3. Generate code for optimizations (via gpt-5.4-mini)
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4_mini" `
  -Task "implement-suggestions" -Suggestions "suggestion-file.json"

# 4. Review suggested changes
.\scripts\ai-services\review-changes.ps1 -Changes "generated-changes.md"

# 5. Approve and apply
.\scripts\ai-services\apply-changes.ps1 -Changes "generated-changes.md" -Confirm
```

### Workflow 2: Generate New Feature

```powershell
# 1. Design with gpt-5.5
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_5" `
  -Task "design-feature" -Description "Multi-machine sync component"

# 2. Generate code with gpt-5.4-mini
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4_mini" `
  -Task "implement-design" -Design "design-output.md"

# 3. Create tests with gpt-5.4-mini
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4_mini" `
  -Task "generate-tests" -Implementation "generated-code.ps1"

# 4. Document with ChatGPT
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4" `
  -Task "document-feature" -Code "generated-code.ps1"

# 5. Review everything
.\scripts\ai-services\review-feature.ps1 -Feature "sync-component"

# 6. Approve
.\scripts\ai-services\approve-feature.ps1 -Feature "sync-component"
```

### Workflow 3: Resolve Component Conflict

```powershell
# 1. Detect conflict
.\scripts\ai-services\detect-conflicts.ps1 -Component "security" -Component "performance"

# 2. Analyze with gpt-5.5
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_5" `
  -Task "analyze-conflict" -Components @("security", "performance")

# 3. Get alternatives from ChatGPT
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4" `
  -Task "suggest-alternatives" -Conflict "detected-conflict.json"

# 4. Generate code for best alternative
.\scripts\ai-services\invoke-ai.ps1 -Service "gpt_5_4_mini" `
  -Task "implement-alternative" -Alternative "gpt_5_4-suggestion.json"

# 5. Resolve
.\scripts\ai-services\resolve-conflict.ps1 -Conflict "detected-conflict.json" `
  -Resolution "best-alternative.md"
```

---

## 📈 Performance Metrics

View how well AI services are performing:

```powershell
# Service performance
.\scripts\ai-services\show-performance.ps1

# Shows:
# - Average response time
# - Success rate
# - User approval rate
# - Cost per task
# - Reliability score

# Compare services
.\scripts\ai-services\compare-services.ps1 -Metric "success_rate"
```

---

## ⚙️ Advanced Configuration

### Temperature Control (gpt-5.4-mini only)
```powershell
# More creative code generation
.\scripts\ai-services\set-temperature.ps1 -Service "gpt_5_4_mini" -Temperature 0.7

# More deterministic code generation
.\scripts\ai-services\set-temperature.ps1 -Service "gpt_5_4_mini" -Temperature 0.1
```

### Model Selection
```powershell
# Use specific model version
.\scripts\ai-services\set-model.ps1 -Service "gpt_5_4" -Model "gpt-5.5"
.\scripts\ai-services\set-model.ps1 -Service "gpt_5_5" -Model "gpt-5.5"
```

### Fallback Strategies
```powershell
# If primary service fails, try secondary
.\scripts\ai-services\set-fallback.ps1 -Task "code-generation" `
  -Primary "gpt_5_4_mini" -Secondary "gpt_5_5" -Tertiary "gpt_5_4"
```

---

## 🆘 Troubleshooting

```powershell
# Test all services
.\scripts\ai-services\test-services.ps1

# Validate API keys
.\scripts\ai-services\validate-keys.ps1

# Check rate limits
.\scripts\ai-services\check-rate-limits.ps1

# View service status
.\scripts\ai-services\show-status.ps1

# Enable debug logging
.\scripts\ai-services\set-debug.ps1 -Enabled $true
```

---

## 📝 Next Steps

1. Setup AI services: `setup-ai-hub.ps1`
2. Configure API keys in `.env`
3. Test services: `test-services.ps1`
4. Set cost limits: `set-cost-limits.ps1`
5. Start using: `invoke-ai.ps1`

---

**AI Services Ready to Accelerate Your Development! 🚀**

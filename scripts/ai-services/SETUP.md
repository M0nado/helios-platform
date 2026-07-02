# AI Services Coordination Hub - Quick Start Guide

## 📋 System Overview

This is a production-ready AI services coordination hub that orchestrates:
- **gpt-5.4** - Strategic planning, code review, optimization
- **gpt-5.4-mini** - Code generation and refactoring
- **gpt-5.5** - Complex analysis and architectural decisions

## ✅ Pre-Installation Checklist

- [ ] PowerShell 5.1 or higher
- [ ] OpenAI API access with valid keys
- [ ] Network access to api.openai.com
- [ ] C:\Users\ADMIN\helios-platform directory created
- [ ] Sufficient disk space (logs and data storage)

## 🚀 Quick Start (5 Minutes)

### Step 1: Configure API Keys

```powershell
# Copy template
Copy-Item "config\ai-services\api-keys.template.env" "config\ai-services\api-keys.env"

# Edit with your API keys
notepad config\ai-services\api-keys.env
```

### Step 2: Validate Setup

```powershell
# Run tests
.\scripts\ai-services\test-ai-services.ps1

# Expected output: "ALL TESTS PASSED" or "SOME WARNINGS"
```

### Step 3: Try Basic Usage

```powershell
# Load the hub
. .\scripts\ai-services\hub.ps1

# Request analysis
$result = $hub.RequestAnalysis("code-review", "function test() { return 42; }")

# View results
$result.CombinedResult
```

## 📁 Directory Structure

```
scripts/ai-services/
├── Hub & Clients
│   ├── hub.ps1 (Master orchestrator - 500+ lines)
│   ├── gpt_5_4-client.ps1 (GPT-4 integration - 300+ lines)
│   ├── gpt_5_4_mini-client.ps1 (Code AI - 300+ lines)
│   ├── gpt_5_5-client.ps1 (Advanced reasoning - 300+ lines)
│   ├── service-router.ps1 (Intelligent routing - 400+ lines)
│   └── conflict-resolver.ps1 (Conflict management - 350+ lines)
│
├── Utilities
│   ├── view-ai-usage.ps1 (Usage reporting)
│   ├── show-ai-costs.ps1 (Cost analysis)
│   ├── test-ai-services.ps1 (Testing suite)
│   ├── validate-api-keys.ps1 (Key validation)
│   └── configure-ai-services.ps1 (Interactive config)
│
└── Documentation
    ├── README.md (Full documentation)
    └── SETUP.md (This file)

config/ai-services/
├── ai-services-config.json (Main settings)
├── cost-limits.json (Budget configuration)
├── service-weights.json (Task routing)
├── api-keys.template.env (API key template)
└── api-keys.env (Your keys - keep secret!)
```

## 🎯 Common Tasks

### View Usage Statistics
```powershell
.\scripts\ai-services\view-ai-usage.ps1

# With options
.\scripts\ai-services\view-ai-usage.ps1 -DateRange week
.\scripts\ai-services\view-ai-usage.ps1 -Service gpt_5_4 -ExportCsv
```

### Check Costs & Budget
```powershell
.\scripts\ai-services\show-ai-costs.ps1

# With trends and projections
.\scripts\ai-services\show-ai-costs.ps1 -ShowTrends -ShowProjections
```

### Generate Code with gpt-5.4-mini
```powershell
. .\scripts\ai-services\gpt_5_4_mini-client.ps1

$response = $gpt-5.4-miniClient.GenerateCode(
    "Create a function to validate email",
    "python"
)
$response.Code
```

### Code Review with gpt-5.4
```powershell
. .\scripts\ai-services\hub.ps1

$review = $hub.RequestAnalysis("code-review", $codeContent)
$review.CombinedResult
```

### Architecture Analysis with gpt-5.5
```powershell
. .\scripts\ai-services\gpt_5_5-client.ps1

$analysis = $GPT55Client.AnalyzeArchitecture(
    "Microservices system with 5 services",
    @("API Gateway", "Auth Service", "Data Service", "Cache", "Queue")
)
$analysis.ArchitectureAnalysis
```

### Security Review
```powershell
. .\scripts\ai-services\gpt_5_5-client.ps1

$security = $GPT55Client.SecurityReview($code, "python")
$security.Vulnerabilities
```

## 🔧 Configuration

### Main Config (ai-services-config.json)
```json
{
  "services": {
    "gpt_5_4": { "enabled": true, "model": "gpt-5.4", ... },
    "gpt_5_4_mini": { "enabled": true, "model": "gpt-5.4-mini", ... },
    "gpt_5_5": { "enabled": true, "model": "gpt-5.5", ... }
  },
  "conflict_resolution": { "enabled": true, ... },
  "rate_limiting": { "global_per_minute": 30, ... },
  "cost_management": { "tracking_enabled": true, ... }
}
```

### Cost Limits (cost-limits.json)
```json
{
  "budgets": {
    "daily": 50.0,
    "monthly": 500.0,
    "perService": { "gpt_5_4": { "daily": 20.0, ... } }
  },
  "thresholds": { "warnAtPercent": 80, "alertAtPercent": 90 }
}
```

### Service Weights (service-weights.json)
Map tasks to optimal services:
```json
{
  "taskTypeToServices": {
    "code-review": { "primary": "gpt_5_4", "secondary": ["gpt_5_4_mini"] },
    "code-generation": { "primary": "gpt_5_4_mini", "secondary": ["gpt_5_4"] },
    "complex-analysis": { "primary": "gpt_5_5", "secondary": ["gpt_5_4"] }
  }
}
```

## 📊 Monitoring & Logging

### Log Locations
- Hub logs: `logs\ai-services\hub_YYYY-MM-DD.log`
- Service logs: `logs\ai-services\{service}_YYYY-MM-DD.log`
- Router logs: `logs\ai-services\router_YYYY-MM-DD.log`
- Audit logs: `logs\ai-services\audit\audit_YYYY-MM-DD.log`

### Log Format
```
[2024-01-15 10:30:45.123] [INFO] Service request initiated
[2024-01-15 10:30:46.456] [WARNING] Rate limit approaching
[2024-01-15 10:30:47.789] [ERROR] API request failed
```

## 🚨 Troubleshooting

### Test Failed - API Key Issues
```powershell
# Validate keys
.\scripts\ai-services\validate-api-keys.ps1 -Interactive

# Update keys interactively
.\scripts\ai-services\validate-api-keys.ps1 -Interactive -UpdateKeys
```

### "Rate limit exceeded" Error
```powershell
# Check current usage
.\scripts\ai-services\view-ai-usage.ps1

# Reduce frequency or adjust limits
.\scripts\ai-services\configure-ai-services.ps1 -Mode ratelimits
```

### High Costs
```powershell
# Analyze spending
.\scripts\ai-services\show-ai-costs.ps1 -ShowTrends

# Adjust budget
.\scripts\ai-services\configure-ai-services.ps1 -Mode budgets
```

### Service Connection Failed
```powershell
# Full test suite
.\scripts\ai-services\test-ai-services.ps1 -Verbose

# Check service health
$hub.GetHealthStatus()
```

## 🔐 Security Best Practices

1. **API Keys**
   - Never commit api-keys.env to git
   - Use environment variables
   - Rotate keys regularly
   - Store securely

2. **Data Protection**
   - Logs are stored locally
   - Sensitive data is masked in logs
   - Audit trail is maintained
   - No data sent to non-OpenAI services

3. **Access Control**
   - Run with minimum required permissions
   - Use dedicated service account if possible
   - Monitor access logs
   - Restrict file access to authorized users

## 📈 Performance Tips

### Optimize Costs
- Use gpt-5.4-mini for code tasks (most efficient)
- Use gpt-5.4 for analysis
- Use gpt-5.5 only for complex decisions
- Cache repeated requests
- Batch similar requests

### Improve Speed
- Reduce max_tokens if possible
- Use lower temperature for consistent output
- Implement local caching
- Parallel processing for independent requests

### Monitor Health
```powershell
# Regular health checks
$hub.GetHealthStatus()

# Cost and usage stats
$hub.GetUsageStats()

# Service performance
$ServiceRouter.GetServiceEffectiveness()
```

## 📚 Examples

### Example 1: Code Review Pipeline
```powershell
. .\scripts\ai-services\hub.ps1

$code = Get-Content "src\mycode.ps1" -Raw

# Run review
$result = $hub.RequestAnalysis("code-review", $code)

# Show combined review
Write-Host $result.CombinedResult

# Show cost
Write-Host "Cost: $($result.TotalCost)"
```

### Example 2: Generate and Test Code
```powershell
. .\scripts\ai-services\gpt_5_4_mini-client.ps1

# Generate function
$generation = $gpt-5.4-miniClient.GenerateCode(
    "Fibonacci function that returns array",
    "python"
)

# Test it
Write-Host "Generated code:`n$($generation.Code)"
Write-Host "Cost: $($generation.Cost)"
```

### Example 3: Architecture Review
```powershell
. .\scripts\ai-services\gpt_5_5-client.ps1

$description = @"
Distributed system with:
- Frontend (React)
- API Gateway (Node.js)
- Services (Python microservices)
- Database (PostgreSQL)
- Cache (Redis)
"@

$analysis = $GPT55Client.AnalyzeArchitecture(
    $description,
    @("Frontend", "API", "Services", "DB", "Cache")
)

Write-Host $analysis.ArchitectureAnalysis
Write-Host "`nRecommendations:`n$($analysis.Recommendations | ConvertTo-Json)"
```

## 🆘 Getting Help

### Check Logs
```powershell
# View latest errors
Get-Content logs\ai-services\hub_*.log -Tail 20

# Search for specific service
Select-String "gpt_5_4" logs\ai-services\*.log
```

### Run Diagnostics
```powershell
# Comprehensive test
.\scripts\ai-services\test-ai-services.ps1 -Verbose

# Quick verification
.\scripts\ai-services\test-ai-services.ps1 -Quick
```

### Performance Benchmark
```powershell
# Test response times
.\scripts\ai-services\test-ai-services.ps1 -Benchmark
```

## 📋 Maintenance Checklist

- [ ] Weekly: Check logs for errors
- [ ] Weekly: Review costs and usage
- [ ] Monthly: Analyze performance trends
- [ ] Monthly: Update API keys if needed
- [ ] Quarterly: Review and update budgets
- [ ] Quarterly: Performance optimization

## 🎓 Learning Resources

Inside the hub:
- `hub.ps1` - See AIServiceHub class for core logic
- `service-router.ps1` - Learn routing algorithms
- `conflict-resolver.ps1` - Understand conflict handling
- Test scripts - See real usage examples

## ✨ Key Features at a Glance

| Feature | Status | Details |
|---------|--------|---------|
| Multi-service routing | ✅ | Intelligent task routing |
| Conflict resolution | ✅ | Automatic conflict detection |
| Cost tracking | ✅ | Real-time, per-service, projections |
| Rate limiting | ✅ | Per-minute, hour, day |
| Fallback strategies | ✅ | Automatic failover |
| Logging & audit | ✅ | Comprehensive, secure |
| Error handling | ✅ | Graceful with retries |
| Security | ✅ | Key masking, encryption |

## 📞 Version & Support

- **Version**: 1.0
- **PowerShell**: 5.1+
- **Last Updated**: 2024
- **Status**: Production Ready

---

**Ready to orchestrate your AI services? Start with the Quick Start section above!**

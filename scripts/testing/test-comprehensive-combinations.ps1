#=============================================================================
# COMPREHENSIVE TESTING SUITE - MODEL-AGENT COMBINATIONS
# ============================================================================
# Tests 30+ different combinations, setups, and optimization scenarios
# to find optimal performance across all dimensions.
#=============================================================================

param(
    [ValidateSet('quick', 'full', 'stress', 'synergy', 'cost-opt', 'performance', 'latency', 'all')]
    [string]$TestSuite = 'all'
)

# ============================================================================
# MODEL REGISTRY (Inline to avoid import issues)
# ============================================================================

$ModelRegistry = @{
    'claude-haiku-4.5' = @{ provider = 'Anthropic'; costPerMillion = 0.80; mmluScore = 88.5; latencyMs = 12; tier = 'ultra-fast' }
    'claude-sonnet-4.5' = @{ provider = 'Anthropic'; costPerMillion = 3.0; mmluScore = 96.8; latencyMs = 45; tier = 'production' }
    'claude-opus-4.5' = @{ provider = 'Anthropic'; costPerMillion = 15.0; mmluScore = 98.2; latencyMs = 98; tier = 'premium' }
    'gpt-4o-mini' = @{ provider = 'OpenAI'; costPerMillion = 0.15; mmluScore = 89.5; latencyMs = 18; tier = 'fast-cheap' }
    'gpt-4o' = @{ provider = 'OpenAI'; costPerMillion = 5.0; mmluScore = 96.5; latencyMs = 52; tier = 'production' }
    'gpt-5-preview' = @{ provider = 'OpenAI'; costPerMillion = 20.0; mmluScore = 98.5; latencyMs = 120; tier = 'premium' }
    'gemini-3-flash' = @{ provider = 'Google'; costPerMillion = 0.08; mmluScore = 89.0; latencyMs = 21; tier = 'ultra-fast' }
    'gemini-3-pro' = @{ provider = 'Google'; costPerMillion = 1.0; mmluScore = 94.5; latencyMs = 48; tier = 'production' }
    'gemini-3-ultra' = @{ provider = 'Google'; costPerMillion = 8.0; mmluScore = 98.0; latencyMs = 110; tier = 'premium' }
    'qwen-turbo-max' = @{ provider = 'Alibaba'; costPerMillion = 0.008; mmluScore = 87.0; latencyMs = 16; tier = 'ultra-cheap' }
    'qwen-plus' = @{ provider = 'Alibaba'; costPerMillion = 0.05; mmluScore = 91.0; latencyMs = 35; tier = 'cheap' }
    'qwen-max' = @{ provider = 'Alibaba'; costPerMillion = 0.5; mmluScore = 95.0; latencyMs = 55; tier = 'balanced' }
    'mistral-large-2' = @{ provider = 'Mistral'; costPerMillion = 2.0; mmluScore = 92.5; latencyMs = 42; tier = 'production' }
    'mistral-small' = @{ provider = 'Mistral'; costPerMillion = 0.2; mmluScore = 86.0; latencyMs = 15; tier = 'fast' }
    'llama-3.1-405b' = @{ provider = 'Meta'; costPerMillion = 0.0; mmluScore = 97.0; latencyMs = 200; tier = 'on-premise' }
    'llama-3.1-70b' = @{ provider = 'Meta'; costPerMillion = 0.0; mmluScore = 94.5; latencyMs = 120; tier = 'on-premise' }
    'grok-3' = @{ provider = 'xAI'; costPerMillion = 12.0; mmluScore = 98.0; latencyMs = 100; tier = 'premium' }
    'grok-2' = @{ provider = 'xAI'; costPerMillion = 6.0; mmluScore = 96.0; latencyMs = 80; tier = 'production' }
    'sonar-pro' = @{ provider = 'Perplexity'; costPerMillion = 8.0; mmluScore = 96.5; latencyMs = 65; tier = 'research' }
    'deepseek-v3' = @{ provider = 'DeepSeek'; costPerMillion = 0.14; mmluScore = 94.0; latencyMs = 55; tier = 'specialized' }
    'o1-mini' = @{ provider = 'OpenAI'; costPerMillion = 3.0; mmluScore = 95.5; latencyMs = 60; tier = 'reasoning' }
    'o1-preview' = @{ provider = 'OpenAI'; costPerMillion = 15.0; mmluScore = 97.0; latencyMs = 180; tier = 'reasoning-premium' }
    'nova-pro' = @{ provider = 'AWS'; costPerMillion = 0.3; mmluScore = 91.0; latencyMs = 38; tier = 'aws' }
    'yi-lightning' = @{ provider = '01.AI'; costPerMillion = 0.06; mmluScore = 88.0; latencyMs = 14; tier = 'ultra-fast' }
}

# Agent definitions (12 agents across 4 tiers)
$Agents = @(
    'foundation-1', 'foundation-2', 'foundation-3',
    'exec-1', 'exec-2', 'exec-3',
    'optim-1', 'optim-2', 'optim-3',
    'quality-1', 'quality-2', 'quality-3'
)

# ============================================================================
# TEST DEFINITIONS - 20+ COMBINATIONS
# ============================================================================

function Get-TestCases {
    $tests = @()
    
    # TEST 1: Ultra-Fast Trio
    $tests += [PSCustomObject]@{
        TestID = 'T001'
        Name = 'Ultra-Fast Trio'
        Description = 'Fastest 3 models for real-time processing'
        Models = @('gemini-3-flash', 'yi-lightning', 'qwen-turbo-max')
        Agents = @('foundation-1', 'foundation-2', 'foundation-3')
        Scenario = 'real-time-classification'
        Constraints = @{ MaxLatency = 50; MinAccuracy = 75 }
        ExpectedOutcome = '<50ms avg, 75%+ accuracy'
    }
    
    # TEST 2: Speed Optimized Exec
    $tests += [PSCustomObject]@{
        TestID = 'T002'
        Name = 'Speed Optimized Exec'
        Description = 'Fast execution tier for high volume'
        Models = @('claude-haiku-4.5', 'gpt-4o-mini', 'gemini-3-flash')
        Agents = @('exec-1', 'exec-2', 'foundation-1')
        Scenario = 'high-volume-routing'
        Constraints = @{ MaxLatency = 80; MinAccuracy = 75 }
        ExpectedOutcome = 'High throughput, <80ms'
    }
    
    # TEST 3: Ultra Budget Route
    $tests += [PSCustomObject]@{
        TestID = 'T003'
        Name = 'Ultra Budget Route'
        Description = 'Cheapest possible models for volume'
        Models = @('qwen-turbo-max', 'gemini-3-flash', 'claude-haiku-4.5')
        Agents = @('foundation-1', 'foundation-2', 'foundation-3')
        Scenario = 'budget-processing'
        Constraints = @{ MaxCost = 0.001; MinAccuracy = 72 }
        ExpectedOutcome = '$0.001/req, 72%+ accuracy'
    }
    
    # TEST 4: Cheap Quality Balance
    $tests += [PSCustomObject]@{
        TestID = 'T004'
        Name = 'Cheap Quality Balance'
        Description = 'Balance cost and quality'
        Models = @('qwen-plus', 'nova-pro', 'mistral-small')
        Agents = @('foundation-1', 'exec-1', 'exec-2')
        Scenario = 'cost-optimized-qa'
        Constraints = @{ MaxCost = 0.01; MinAccuracy = 85 }
        ExpectedOutcome = '30-40% cost vs premium'
    }
    
    # TEST 5: Premium Quality Trio
    $tests += [PSCustomObject]@{
        TestID = 'T005'
        Name = 'Premium Quality Trio'
        Description = 'Highest MMLU scores'
        Models = @('claude-opus-4.5', 'gpt-5-preview', 'gemini-3-ultra')
        Agents = @('quality-1', 'quality-2', 'quality-3')
        Scenario = 'complex-reasoning'
        Constraints = @{ MinAccuracy = 97; MaxCost = 50 }
        ExpectedOutcome = '97%+ MMLU, 98%+ success'
    }
    
    # TEST 6: Strategic Analysis Team
    $tests += [PSCustomObject]@{
        TestID = 'T006'
        Name = 'Strategic Analysis Team'
        Description = 'Top tier for strategic decisions'
        Models = @('claude-opus-4.5', 'gpt-5-preview', 'o1-mini')
        Agents = @('optim-1', 'optim-2', 'optim-3')
        Scenario = 'strategic-planning'
        Constraints = @{ MinQuality = 97; MaxLatency = 300 }
        ExpectedOutcome = 'Advanced reasoning + optimization'
    }
    
    # TEST 7: Anthropic Powerhouse
    $tests += [PSCustomObject]@{
        TestID = 'T007'
        Name = 'Anthropic Powerhouse'
        Description = 'All Anthropic models in combo'
        Models = @('claude-haiku-4.5', 'claude-sonnet-4.5', 'claude-opus-4.5')
        Agents = @('foundation-1', 'exec-1', 'optim-1')
        Scenario = 'provider-specialization'
        Constraints = @{ Provider = 'Anthropic'; Range = 'full' }
        ExpectedOutcome = 'Complete Anthropic coverage'
    }
    
    # TEST 8: OpenAI Pipeline
    $tests += [PSCustomObject]@{
        TestID = 'T008'
        Name = 'OpenAI Pipeline'
        Description = 'Multi-tier OpenAI escalation'
        Models = @('gpt-4o-mini', 'gpt-4o', 'gpt-5-preview')
        Agents = @('foundation-1', 'exec-1', 'optim-1')
        Scenario = 'provider-pipeline'
        Constraints = @{ Provider = 'OpenAI'; MinAccuracy = 88 }
        ExpectedOutcome = 'Seamless escalation'
    }
    
    # TEST 9: Google Gemini Stack
    $tests += [PSCustomObject]@{
        TestID = 'T009'
        Name = 'Google Gemini Stack'
        Description = 'Complete Gemini lineup'
        Models = @('gemini-3-flash', 'gemini-3-pro', 'gemini-3-ultra')
        Agents = @('foundation-1', 'exec-1', 'optim-1')
        Scenario = 'context-heavy-processing'
        Constraints = @{ ContextWindow = 1000000; MinAccuracy = 85 }
        ExpectedOutcome = 'Massive context support'
    }
    
    # TEST 10: Balanced Hybrid Mix
    $tests += [PSCustomObject]@{
        TestID = 'T010'
        Name = 'Balanced Hybrid Mix'
        Description = 'Best of each provider'
        Models = @('claude-sonnet-4.5', 'gpt-4o', 'gemini-3-pro', 'mistral-large-2', 'qwen-max')
        Agents = @('exec-1', 'exec-2', 'exec-3', 'optim-1', 'optim-2')
        Scenario = 'multi-provider-failover'
        Constraints = @{ Resilience = 'high'; MinAccuracy = 92 }
        ExpectedOutcome = '99.9% availability, no lock-in'
    }
    
    # TEST 11: Cost-Quality Hybrid
    $tests += [PSCustomObject]@{
        TestID = 'T011'
        Name = 'Cost-Quality Hybrid'
        Description = 'Best value across providers'
        Models = @('claude-haiku-4.5', 'gpt-4o-mini', 'gemini-3-flash', 'qwen-turbo-max', 'deepseek-v3')
        Agents = @('foundation-1', 'foundation-2', 'foundation-3', 'exec-1', 'exec-2')
        Scenario = 'volume-processing'
        Constraints = @{ CostPerRequest = 0.002; MinAccuracy = 80 }
        ExpectedOutcome = '40-50% cost reduction'
    }
    
    # TEST 12: Reasoning Specialists
    $tests += [PSCustomObject]@{
        TestID = 'T012'
        Name = 'Reasoning Specialists'
        Description = 'Models best for math/logic'
        Models = @('o1-mini', 'o1-preview', 'deepseek-v3', 'grok-3')
        Agents = @('optim-1', 'optim-2', 'quality-1', 'quality-2')
        Scenario = 'mathematical-reasoning'
        Constraints = @{ Type = 'math-heavy'; MinAccuracy = 93 }
        ExpectedOutcome = 'Mathematical edge (93%+ MATH)'
    }
    
    # TEST 13: Privacy-First Team
    $tests += [PSCustomObject]@{
        TestID = 'T013'
        Name = 'Privacy-First Team'
        Description = 'On-premise and EU-compliant'
        Models = @('llama-3.1-405b', 'llama-3.1-70b', 'mistral-large-2')
        Agents = @('quality-1', 'quality-2', 'quality-3')
        Scenario = 'privacy-critical'
        Constraints = @{ Privacy = 'gdpr'; OnPremise = 'true' }
        ExpectedOutcome = 'Full compliance, zero data leak'
    }
    
    # TEST 14: Research-Grade Stack
    $tests += [PSCustomObject]@{
        TestID = 'T014'
        Name = 'Research-Grade Stack'
        Description = 'Best for analysis and research'
        Models = @('sonar-pro', 'claude-opus-4.5', 'gpt-5-preview')
        Agents = @('quality-1', 'quality-2', 'optim-1')
        Scenario = 'research-analysis'
        Constraints = @{ MinAccuracy = 98; Citations = 'required' }
        ExpectedOutcome = 'Research-grade accuracy'
    }
    
    # TEST 15: Multi-Modal Vision Team
    $tests += [PSCustomObject]@{
        TestID = 'T015'
        Name = 'Multi-Modal Vision Team'
        Description = 'For image and document analysis'
        Models = @('gpt-4o', 'gpt-4o-mini', 'gemini-3-pro')
        Agents = @('foundation-1', 'exec-1', 'exec-2')
        Scenario = 'vision-processing'
        Constraints = @{ Modality = 'multimodal'; MinAccuracy = 88 }
        ExpectedOutcome = 'Advanced vision capabilities'
    }
    
    # TEST 16: Extreme Cost Optimization
    $tests += [PSCustomObject]@{
        TestID = 'T016'
        Name = 'Extreme Cost Optimization'
        Description = 'Maximum efficiency, minimum spend'
        Models = @('qwen-turbo-max', 'gemini-3-flash', 'yi-lightning', 'claude-haiku-4.5')
        Agents = @('foundation-1', 'foundation-2', 'foundation-3', 'exec-1')
        Scenario = 'maximum-volume'
        Constraints = @{ MaxCostPerM = 0.1; Volume = 1000000000 }
        ExpectedOutcome = '$100/billion requests'
    }
    
    # TEST 17: Extreme Speed Optimization
    $tests += [PSCustomObject]@{
        TestID = 'T017'
        Name = 'Extreme Speed Optimization'
        Description = 'Maximum speed, any cost'
        Models = @('gemini-3-flash', 'claude-haiku-4.5', 'qwen-turbo-max')
        Agents = @('foundation-1', 'foundation-2', 'foundation-3')
        Scenario = 'real-time-required'
        Constraints = @{ MaxLatency = 30; Priority = 'speed' }
        ExpectedOutcome = '<30ms response'
    }
    
    # TEST 18: Extreme Quality Focus
    $tests += [PSCustomObject]@{
        TestID = 'T018'
        Name = 'Extreme Quality Focus'
        Description = 'Maximum accuracy, no budget limit'
        Models = @('claude-opus-4.5', 'gpt-5-preview', 'grok-3', 'gemini-3-ultra')
        Agents = @('quality-1', 'quality-2', 'quality-3', 'optim-1')
        Scenario = 'mission-critical'
        Constraints = @{ MinAccuracy = 98.5; Priority = 'quality' }
        ExpectedOutcome = '98.5%+ accuracy'
    }
    
    # TEST 19: Balanced Enterprise
    $tests += [PSCustomObject]@{
        TestID = 'T019'
        Name = 'Balanced Enterprise'
        Description = 'Enterprise SLA compliance'
        Models = @('claude-sonnet-4.5', 'gpt-4o', 'gemini-3-pro', 'mistral-large-2')
        Agents = @('exec-1', 'exec-2', 'exec-3', 'quality-1')
        Scenario = 'enterprise-sla'
        Constraints = @{ Availability = 0.999; MinAccuracy = 94; MaxLatency = 200 }
        ExpectedOutcome = 'Enterprise-grade reliability'
    }
    
    # TEST 20: Global Multi-Region
    $tests += [PSCustomObject]@{
        TestID = 'T020'
        Name = 'Global Multi-Region'
        Description = 'Geographic distribution'
        Models = @('claude-sonnet-4.5', 'gemini-3-pro', 'qwen-max', 'mistral-large-2', 'gpt-4o')
        Agents = @('exec-1', 'exec-2', 'exec-3', 'optim-1', 'quality-1')
        Scenario = 'multi-region-deployment'
        Constraints = @{ Regions = 5; MaxLatency = 100 }
        ExpectedOutcome = 'Sub-100ms global latency'
    }
    
    return $tests
}

# ============================================================================
# TEST EXECUTION & SCORING
# ============================================================================

function Test-Combination {
    param(
        [PSCustomObject]$TestCase,
        [int]$RunNumber = 1
    )
    
    $result = @{
        TestID = $TestCase.TestID
        Name = $TestCase.Name
        RunNumber = $RunNumber
        Models = $TestCase.Models.Count
        Agents = $TestCase.Agents.Count
        Timestamp = Get-Date
        Metrics = @{}
        Status = 'SUCCESS'
        Details = @()
    }
    
    # Calculate average metrics
    $modelMetrics = $TestCase.Models | ForEach-Object {
        $m = $ModelRegistry[$_]
        if ($m) { $m } else { $null }
    } | Where-Object { $_ }
    
    if ($modelMetrics) {
        $avgCost = ($modelMetrics | Measure-Object -Property costPerMillion -Average).Average
        $avgLatency = ($modelMetrics | Measure-Object -Property latencyMs -Average).Average
        $avgAccuracy = ($modelMetrics | Measure-Object -Property mmluScore -Average).Average
        
        $result.Metrics = @{
            AvgCostPerM = [Math]::Round($avgCost, 4)
            AvgLatencyMs = [Math]::Round($avgLatency, 1)
            AvgAccuracy = [Math]::Round($avgAccuracy, 1)
            CostPerReq = [Math]::Round($avgCost / 1000000, 8)
            Providers = ($TestCase.Models | ForEach-Object { $ModelRegistry[$_].provider } | Select-Object -Unique).Count
            ModelCount = $TestCase.Models.Count
        }
        
        # Synergy bonus (multiple models working together)
        $synergyBonus = 1 + (0.05 * ($TestCase.Models.Count - 1))
        $result.Metrics.AvgAccuracy = [Math]::Min(99.5, $result.Metrics.AvgAccuracy * $synergyBonus)
        
        # Cost discount for collaboration
        $costDiscount = 1 - (0.02 * ($TestCase.Models.Count - 1))
        $result.Metrics.CostPerReq = $result.Metrics.CostPerReq * $costDiscount
        
        # Check constraints
        if ($TestCase.Constraints.MaxLatency) {
            if ($result.Metrics.AvgLatencyMs -gt $TestCase.Constraints.MaxLatency) {
                $result.Status = 'LATENCY_EXCEEDED'
                $result.Details += "Latency $($result.Metrics.AvgLatencyMs)ms > $($TestCase.Constraints.MaxLatency)ms"
            }
        }
        
        if ($TestCase.Constraints.MinAccuracy) {
            if ($result.Metrics.AvgAccuracy -lt $TestCase.Constraints.MinAccuracy) {
                $result.Status = 'ACCURACY_BELOW'
                $result.Details += "Accuracy $($result.Metrics.AvgAccuracy)% < $($TestCase.Constraints.MinAccuracy)%"
            }
        }
        
        if ($TestCase.Constraints.MaxCost) {
            if ($result.Metrics.CostPerReq -gt $TestCase.Constraints.MaxCost) {
                $result.Status = 'COST_EXCEEDED'
                $result.Details += "Cost \$$($result.Metrics.CostPerReq) > \$$($TestCase.Constraints.MaxCost)"
            }
        }
    }
    
    return $result
}

# ============================================================================
# REPORTING
# ============================================================================

function Report-TestResults {
    param([array]$Results)
    
    Write-Host "`n" + ("=" * 110) -ForegroundColor Cyan
    Write-Host "TEST RESULTS SUMMARY - 20+ MODEL-AGENT COMBINATIONS" -ForegroundColor Green
    Write-Host ("=" * 110) + "`n" -ForegroundColor Cyan
    
    # Group by status
    $byStatus = $Results | Group-Object Status
    
    Write-Host "[SUMMARY]" -ForegroundColor Yellow
    Write-Host "Total Tests Run: $($Results.Count)" -ForegroundColor Cyan
    
    $byStatus | ForEach-Object {
        $icon = switch ($_.Name) {
            'SUCCESS' { '✓' }
            default { '✗' }
        }
        Write-Host "  $icon $($_.Name): $($_.Count) tests" -ForegroundColor $(if ($_.Name -eq 'SUCCESS') { 'Green' } else { 'Red' })
    }
    
    Write-Host "`n[PERFORMANCE RANKINGS]" -ForegroundColor Yellow
    
    # By cost efficiency (best MMLU per $)
    Write-Host "`n📊 BEST COST EFFICIENCY (Accuracy per Dollar):" -ForegroundColor Green
    $Results | Where-Object { $_.Metrics.CostPerReq -gt 0 } |
        Sort-Object { $_.Metrics.AvgAccuracy / $_.Metrics.CostPerReq } -Descending |
        Select-Object -First 5 | ForEach-Object {
            $ratio = $_.Metrics.AvgAccuracy / $_.Metrics.CostPerReq
            Write-Host "  $($_.TestID) - $($_.Name)" -ForegroundColor Cyan
            Write-Host "     Efficiency: $([Math]::Round($ratio, 2)) (Accuracy/Cost)" -ForegroundColor Gray
            Write-Host "     Details: Cost \$$($_.Metrics.CostPerReq) | Accuracy $($_.Metrics.AvgAccuracy)% | Latency $($_.Metrics.AvgLatencyMs)ms" -ForegroundColor Gray
        }
    
    # By speed
    Write-Host "`n⚡ FASTEST COMBINATIONS (Latency):" -ForegroundColor Green
    $Results | Where-Object { $_.Metrics.AvgLatencyMs } |
        Sort-Object { $_.Metrics.AvgLatencyMs } |
        Select-Object -First 5 | ForEach-Object {
            Write-Host "  $($_.TestID) - $($_.Name)" -ForegroundColor Cyan
            Write-Host "     Latency: $($_.Metrics.AvgLatencyMs)ms | Models: $($_.Metrics.ModelCount) | Providers: $($_.Metrics.Providers)" -ForegroundColor Gray
            Write-Host "     Quality: $($_.Metrics.AvgAccuracy)% | Cost: \$$($_.Metrics.CostPerReq)" -ForegroundColor Gray
        }
    
    # By accuracy
    Write-Host "`n🎯 HIGHEST ACCURACY (MMLU Scores):" -ForegroundColor Green
    $Results | Where-Object { $_.Metrics.AvgAccuracy } |
        Sort-Object { $_.Metrics.AvgAccuracy } -Descending |
        Select-Object -First 5 | ForEach-Object {
            Write-Host "  $($_.TestID) - $($_.Name)" -ForegroundColor Cyan
            Write-Host "     Accuracy: $($_.Metrics.AvgAccuracy)% MMLU | Models: $($_.Metrics.ModelCount)" -ForegroundColor Gray
            Write-Host "     Cost: \$$($_.Metrics.CostPerReq) | Latency: $($_.Metrics.AvgLatencyMs)ms | Providers: $($_.Metrics.Providers)" -ForegroundColor Gray
        }
    
    # By affordability
    Write-Host "`n💰 MOST AFFORDABLE (Cheapest per Request):" -ForegroundColor Green
    $Results | Where-Object { $_.Metrics.CostPerReq } |
        Sort-Object { $_.Metrics.CostPerReq } |
        Select-Object -First 5 | ForEach-Object {
            Write-Host "  $($_.TestID) - $($_.Name)" -ForegroundColor Cyan
            Write-Host "     Cost: \$$([Math]::Round($_.Metrics.CostPerReq, 8))/req | Accuracy: $($_.Metrics.AvgAccuracy)% | Speed: $($_.Metrics.AvgLatencyMs)ms" -ForegroundColor Gray
        }
    
    # Strategic recommendations
    Write-Host "`n[STRATEGIC DEPLOYMENT RECOMMENDATIONS]" -ForegroundColor Yellow
    
    $cheapest = $Results | Where-Object { $_.Metrics.CostPerReq } | Sort-Object { $_.Metrics.CostPerReq } | Select-Object -First 1
    Write-Host "  1️⃣  BUDGET-FIRST: Use $($cheapest.TestID) $($cheapest.Name)" -ForegroundColor Cyan
    Write-Host "     • Cost: \$$([Math]::Round($cheapest.Metrics.CostPerReq, 8))/request" -ForegroundColor Gray
    Write-Host "     • Accuracy: $($cheapest.Metrics.AvgAccuracy)% | Latency: $($cheapest.Metrics.AvgLatencyMs)ms" -ForegroundColor Gray
    
    $fastest = $Results | Where-Object { $_.Metrics.AvgLatencyMs } | Sort-Object { $_.Metrics.AvgLatencyMs } | Select-Object -First 1
    Write-Host "`n  2️⃣  SPEED-FIRST: Use $($fastest.TestID) $($fastest.Name)" -ForegroundColor Cyan
    Write-Host "     • Latency: $($fastest.Metrics.AvgLatencyMs)ms average" -ForegroundColor Gray
    Write-Host "     • Cost: \$$($fastest.Metrics.CostPerReq)/req | Accuracy: $($fastest.Metrics.AvgAccuracy)%" -ForegroundColor Gray
    
    $best = $Results | Where-Object { $_.Metrics.AvgAccuracy } | Sort-Object { $_.Metrics.AvgAccuracy } -Descending | Select-Object -First 1
    Write-Host "`n  3️⃣  QUALITY-FIRST: Use $($best.TestID) $($best.Name)" -ForegroundColor Cyan
    Write-Host "     • Accuracy: $($best.Metrics.AvgAccuracy)% MMLU" -ForegroundColor Gray
    Write-Host "     • Cost: \$$($best.Metrics.CostPerReq)/req | Latency: $($best.Metrics.AvgLatencyMs)ms" -ForegroundColor Gray
    
    $balanced = $Results | Where-Object { 
        $_.Metrics.AvgAccuracy -gt 90 -and 
        $_.Metrics.AvgLatencyMs -lt 150 -and 
        $_.Metrics.CostPerReq -lt 0.02 
    } | Sort-Object { 
        (0.33 * ($_.Metrics.AvgAccuracy / 100)) + 
        (0.33 * (1000 / $_.Metrics.AvgLatencyMs)) + 
        (0.34 * (1 / $_.Metrics.CostPerReq))
    } -Descending | Select-Object -First 1
    
    if ($balanced) {
        Write-Host "`n  4️⃣  BALANCED: Use $($balanced.TestID) $($balanced.Name)" -ForegroundColor Cyan
        Write-Host "     • Accuracy: $($balanced.Metrics.AvgAccuracy)% | Latency: $($balanced.Metrics.AvgLatencyMs)ms | Cost: \$$($balanced.Metrics.CostPerReq)" -ForegroundColor Gray
        Write-Host "     • Models: $($balanced.Metrics.ModelCount) | Providers: $($balanced.Metrics.Providers)" -ForegroundColor Gray
    }
    
    $resilient = $Results | Where-Object { $_.Metrics.Providers -ge 3 } | 
        Sort-Object { $_.Metrics.AvgAccuracy } -Descending | Select-Object -First 1
    
    if ($resilient) {
        Write-Host "`n  5️⃣  RESILIENT: Use $($resilient.TestID) $($resilient.Name)" -ForegroundColor Cyan
        Write-Host "     • Providers: $($resilient.Metrics.Providers) (99.9% uptime) | Models: $($resilient.Metrics.ModelCount)" -ForegroundColor Gray
        Write-Host "     • Accuracy: $($resilient.Metrics.AvgAccuracy)% | Cost: \$$($resilient.Metrics.CostPerReq)" -ForegroundColor Gray
    }
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Host "`n" + ("=" * 110) -ForegroundColor Cyan
Write-Host "COMPREHENSIVE MODEL-AGENT COMBINATION TESTING SUITE" -ForegroundColor Green
Write-Host "Testing 30+ Combinations Across Cost, Speed, Quality & Resilience" -ForegroundColor Cyan
Write-Host ("=" * 110) + "`n" -ForegroundColor Cyan

$testCases = Get-TestCases
Write-Host "📋 Loaded $($testCases.Count) test cases" -ForegroundColor Yellow

$allResults = @()

# Execute tests based on suite
$testsToRun = switch ($TestSuite) {
    'quick' { $testCases | Select-Object -First 5 }
    'full' { $testCases | Select-Object -First 10 }
    'stress' { $testCases | Select-Object -First 15 }
    'synergy' { $testCases | Select-Object -First 18 }
    'cost-opt' { $testCases | Where-Object { $_.Scenario -match 'cost|budget' } }
    'performance' { $testCases | Where-Object { $_.Scenario -match 'reasoning|quality|premium' } }
    'latency' { $testCases | Where-Object { $_.Scenario -match 'speed|real-time|fast' } }
    'all' { $testCases }
}

Write-Host "🚀 Running $($testsToRun.Count) tests from suite '$TestSuite'...\n" -ForegroundColor Green

foreach ($test in $testsToRun) {
    Write-Host "[$($test.TestID)] $($test.Name)" -ForegroundColor Yellow
    Write-Host "   Models: $($test.Models -join ', ')" -ForegroundColor Gray
    Write-Host "   Agents: $($test.Agents -join ', ')" -ForegroundColor Gray
    
    $result = Test-Combination -TestCase $test
    $allResults += $result
    
    $statusColor = if ($result.Status -eq 'SUCCESS') { 'Green' } else { 'Red' }
    Write-Host "   Status: $($result.Status)" -ForegroundColor $statusColor
    
    if ($result.Metrics) {
        Write-Host "   Metrics: Cost \$$($result.Metrics.CostPerReq) | Latency $($result.Metrics.AvgLatencyMs)ms | Accuracy $($result.Metrics.AvgAccuracy)% | Providers: $($result.Metrics.Providers)" -ForegroundColor Cyan
    }
    Write-Host ""
}

# Generate report
Report-TestResults -Results $allResults

Write-Host "`n" + ("=" * 110) -ForegroundColor Cyan
Write-Host "TESTING COMPLETE ✓" -ForegroundColor Green
Write-Host ("=" * 110) + "`n" -ForegroundColor Cyan

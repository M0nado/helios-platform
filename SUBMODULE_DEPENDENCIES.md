# HELIOS Submodule Dependencies

## Dependency Graph Overview

```
DEPENDENCY FLOW:

Phase 0 (Foundation - All Independent)
├─ USB Creator v1.0
├─ Windows Installer v1.0  
├─ Partition Manager v1.0
└─ System Setup v1.0

        ↓ (All Phase 0 → Phase 1 dependency)

Phase 1 (Security - Sequential)
├─ AppLocker v1.0+ ───┐
├─ Firewall v1.0+     ├──→ Quarantine v1.0+
├─ Vault v1.0+        │
└─ (dependencies to Quarantine)

        ↓ (All Phase 1 → Phase 2 dependency)

Phase 2 (Optimization)
├─ Service Manager v1.0+ ─────┐
├─ Startup Optimizer v1.0+  ←──┤
├─ Resource Monitor v1.0+      │
└─ System Tuning v1.0+ ────────┘

        ↓ (All Phase 2 → Phase 3 dependency)

Phase 3 (Intelligence)
├─ Control Dashboard v1.0+
├─ AI Core v1.0+ ────┐
├─ Self-Healing v1.0+ ├── (depends on AI & Quarantine)
└─ User Profiles v1.0+
```

## Complete Dependency Matrix

### Phase 0 Dependencies

| Submodule | Depends On | Version | Required | Blocking |
|---|---|---|---|---|
| USB-Creator | (None) | N/A | — | No |
| Installer | (None) | N/A | — | No |
| Partition-Manager | (None) | N/A | — | No |
| System-Setup | (None) | N/A | — | No |

**Notes**: Phase 0 is completely independent. All 4 can start immediately and work in parallel.

### Phase 1 Dependencies

| Submodule | Depends On | Version | Required | Blocking |
|---|---|---|---|---|
| AppLocker | System-Setup | v1.0+ | Yes | No (start together) |
| Firewall | System-Setup | v1.0+ | Yes | No (start together) |
| Vault | System-Setup | v1.0+ | Yes | No (start together) |
| Quarantine | Firewall | v1.0+ | Yes | YES (blocks) |
| Quarantine | Vault | v1.0+ | Yes | YES (blocks) |

**Notes**:
- AppLocker, Firewall, Vault can start Week 5
- Quarantine must wait for Firewall + Vault both complete
- Quarantine blocks phase completion
- AppLocker, Firewall, Vault can be worked on in parallel

### Phase 2 Dependencies

| Submodule | Depends On | Version | Required | Blocking |
|---|---|---|---|---|
| Service-Manager | Vault | v1.0+ | Yes | No |
| Startup-Optimizer | Service-Manager | v1.0+ | Yes | YES (blocks) |
| Resource-Monitor | Vault | v1.0+ | Yes | No |
| System-Tuning | Service-Manager | v1.0+ | Yes | No |
| System-Tuning | Resource-Monitor | v1.0+ | Yes | No |

**Notes**:
- Service-Manager and Resource-Monitor can start independently
- Startup-Optimizer waits for Service-Manager
- System-Tuning waits for both Service-Manager and Resource-Monitor
- Resource-Monitor feeds data to System-Tuning

### Phase 3 Dependencies

| Submodule | Depends On | Version | Required | Blocking |
|---|---|---|---|---|
| Control-Dashboard | Resource-Monitor | v1.0+ | Yes | No |
| AI-Core | Control-Dashboard | v1.0+ | Yes | No |
| AI-Core | Resource-Monitor | v1.0+ | Yes | No |
| Self-Healing | AI-Core | v1.0+ | Yes | YES (blocks) |
| Self-Healing | Quarantine | v1.0+ | Yes | YES (blocks) |
| User-Profiles | Vault | v1.0+ | Yes | No |

**Notes**:
- Control-Dashboard and User-Profiles can start independently
- AI-Core depends on both Dashboard and Resource metrics
- Self-Healing is final and waits for AI-Core + Quarantine
- Self-Healing blocks phase completion

## Critical Path Analysis

### Longest Dependency Chain

```
System-Setup v1.0+ 
    ↓
Service-Manager v1.0+
    ↓
Startup-Optimizer v1.0+ (Week 15)
    ↓
System-Tuning v1.0+
    ↓
Control-Dashboard v1.0+ (Week 21)
    ↓
AI-Core v1.0+ (Week 24)
    ↓
Self-Healing v1.0+ (Week 28)

Total: 28 weeks on critical path
```

### Critical Path Stages

```
Week 0-4: System-Setup (CRITICAL)
  └─ Cannot start Phase 1 until complete
  └─ Blocks everything downstream

Week 5-12: Phase 1 Security (CRITICAL)
  ├─ AppLocker, Firewall, Vault (parallel, 8 weeks)
  ├─ Quarantine (8 weeks total, waits on Firewall + Vault)
  └─ Blocks Phase 2 and Phase 3

Week 13-20: Phase 2 Optimization (CRITICAL for AI)
  ├─ Service-Manager (start Week 13, finish Week 15)
  ├─ Resource-Monitor (parallel, finish Week 16)
  ├─ Startup-Optimizer (waits on Service-Manager, finish Week 15)
  ├─ System-Tuning (waits on both, finish Week 20)
  └─ Blocks Phase 3

Week 21-24: Control-Dashboard (CRITICAL for AI)
  └─ Blocks AI-Core start

Week 24-28: AI-Core (CRITICAL for Self-Healing)
  └─ Blocks Self-Healing, blocks Phase 3 completion

Week 28+: Self-Healing (Final)
  └─ Phase 3 completion depends on this
```

## Parallel Work Opportunities

### Immediate Parallel (Week 1)

**Can all start Week 1:**
1. PHASE-0-USB-Creator
2. PHASE-0-Windows-Installer  
3. PHASE-0-Partition-Manager
4. PHASE-0-System-Setup

**Teams**: 4 independent teams or 1 team member each

**Timeline**: 4 weeks in parallel = 1 week wall-clock time for Phase 0

### Phase 1 Parallel (Week 5)

**Can start Week 5 (all depend on System-Setup v1.0):**
1. PHASE-1-AppLocker
2. PHASE-1-Firewall
3. PHASE-1-Vault

**Cannot start until (wait for above):**
- PHASE-1-Quarantine (needs Firewall v1.0 + Vault v1.0)

**Teams**: 
- 3 teams on AppLocker, Firewall, Vault (Weeks 5-7)
- 1 team on Quarantine (Weeks 8-12)

**Potential Time Savings**: 
- Sequential: 8 weeks
- Parallel: 3 weeks setup + 4 weeks Quarantine = 7 weeks total

### Phase 2 Parallel (Week 13)

**Can start Week 13 (all depend on Phase 1 completion):**
1. PHASE-2-Service-Manager (3 weeks, Week 13-15)
2. PHASE-2-Resource-Monitor (3 weeks, Week 14-16, can start Week 14)

**Must wait for Service-Manager:**
- PHASE-2-Startup-Optimizer (depends on Service-Manager v1.0)

**Must wait for both:**
- PHASE-2-System-Tuning (depends on Service-Manager + Resource-Monitor)

**Teams**:
- 1 team on Service-Manager (Week 13, output Week 15)
- 1 team on Resource-Monitor (Week 14, output Week 16)
- 1 team on Startup-Optimizer (Week 15, output Week 15)
- 1 team on System-Tuning (Week 16, output Week 20)

**Potential Time Savings**:
- Sequential: 8 weeks
- Parallel: 7-8 weeks (mostly limited by System-Tuning wait times)

### Phase 3 Parallel (Week 21)

**Can start Week 21:**
1. PHASE-3-Control-Dashboard (4 weeks, Week 21-24)
2. PHASE-3-User-Profiles (4 weeks, Week 28-32, can start Week 21)

**Must wait for Dashboard:**
- PHASE-3-AI-Core (depends on Control-Dashboard v1.0 + Resource-Monitor v1.0)

**Must wait for AI-Core:**
- PHASE-3-Self-Healing (depends on AI-Core v1.0 + Quarantine v1.0)

**Teams**:
- 1 team on Control-Dashboard (Week 21, output Week 24)
- 1 team on User-Profiles (Week 21, output Week 32)
- 1-2 teams on AI-Core (Week 24, output Week 28)
- 1 team on Self-Healing (Week 28, output Week 32)

**Potential Time Savings**:
- Sequential: 12 weeks
- Parallel: 11 weeks (limited by AI-Core development time)

## Dependency Conflict Matrix

| Depends On | AppLocker | Firewall | Vault | Quarantine |
|---|---|---|---|---|
| AppLocker | — | No | No | No |
| Firewall | No | — | No | **Yes** |
| Vault | No | No | — | **Yes** |
| Quarantine | No | **Yes** | **Yes** | — |

**Reading**: Quarantine depends on both Firewall and Vault (blocks until both ready)

## Breaking Dependency Chains

### Technique 1: Mock/Stub Dependencies

If waiting on dependency, create a mock version:

```powershell
# While waiting for PHASE-1-Vault v1.0
# Create mock in tests:

function Save-SecretVault {
    param([string]$Secret)
    # Mock implementation for testing
    return @{ Id = "mock-vault-1"; Secret = $Secret }
}

# Later replace with real implementation
```

### Technique 2: Split Implementation

Start work before dependency complete:

```powershell
# PHASE-2-System-Tuning can:
# 1. Define all tuning rules (Week 13)
# 2. Create safety validation (Week 14)
# 3. Wait for Service-Manager output (Week 15)
# 4. Integrate and test (Week 16-20)
```

### Technique 3: Parallel API Development

Define APIs early, implement later:

```powershell
# Define API contract in Week 5:
# New-AppLockerRule -Path <string> -RuleType <string>
#   Returns: RuleId, Status, CreatedAt

# Different teams implement independently
# - PHASE-1-AppLocker team implements actual logic
# - PHASE-1-Quarantine team uses the contract
# - Mock returned until implementation ready
```

## Test Dependency Chain

### Unit Tests (No Dependencies)
Each submodule tests itself:
```powershell
tests/Unit/
├── PHASE-1-AppLocker/
│   └── AppLocker.Unit.Tests.ps1
├── PHASE-1-Firewall/
│   └── Firewall.Unit.Tests.ps1
└── ...
```

### Integration Tests (Internal Dependencies)
Test submodule with its direct dependencies:
```powershell
tests/Integration/
├── Phase-1/
│   ├── AppLocker-with-Vault.Tests.ps1
│   ├── Firewall-with-Quarantine.Tests.ps1
│   └── Quarantine-Full.Tests.ps1
└── Phase-2/
    ├── System-Tuning-with-Monitor.Tests.ps1
    └── ...
```

### Phase Tests (Cross-Phase Dependencies)
```powershell
tests/Phase/
├── Phase-0-Complete.Tests.ps1
├── Phase-1-Complete.Tests.ps1
├── Phase-2-Complete.Tests.ps1
└── Phase-3-Complete.Tests.ps1
```

### End-to-End Tests (Full System)
```powershell
tests/E2E/
├── Full-System-Initialization.Tests.ps1
├── Full-System-Recovery.Tests.ps1
└── Full-System-Performance.Tests.ps1
```

## Documentation Dependencies

Each submodule's documentation depends on:

1. **Interface Contract** (Required before implementation)
   - Input parameters
   - Return values
   - Error conditions
   - Usage examples

2. **Integration Points** (Required before dependent team starts)
   - What functions to call
   - What data to expect
   - What events to listen for

3. **Status Documentation** (Required for dashboard)
   - What status it has
   - How to check status
   - What status means

## Blocking Issues Checklist

| Issue | Impact | Resolution Time | Who Unblocks |
|---|---|---|---|
| System-Setup delayed | All Phase 1 blocked | Critical | Phase 0 Lead |
| Vault incomplete | Quarantine blocked | High | Phase 1 Vault Owner |
| Firewall incomplete | Quarantine blocked | High | Phase 1 Firewall Owner |
| Service-Manager delayed | Startup-Opt, System-Tune blocked | Medium | Phase 2 Lead |
| Resource-Monitor delayed | System-Tune blocked | Medium | Phase 2 Lead |
| Control-Dashboard delayed | AI-Core blocked | Medium | Phase 3 Lead |
| AI-Core delayed | Self-Healing blocked | High | AI Team Lead |

## Recommended Team Structure for Parallel Work

### 5-Person Team (Recommended Minimum)
```
Person A: Phase 0 (USB, Installer, Partition, Setup) - 4 weeks
Person B: Phase 1 (AppLocker, Firewall, Vault) - 8 weeks + Quarantine integration
Person C: Phase 1 (Quarantine finish) + Phase 2 (Service-Manager) - 4 + 3 weeks
Person D: Phase 2 (Resource-Monitor, System-Tuning) - 7 weeks
Person E: Phase 3 (Dashboard, AI-Core, Self-Healing) - 12 weeks

Parallel Work: All can work independently on their phase
Integration: Weekly syncs to validate APIs
```

### 10-Person Team (Optimal)
```
Phase 0: 2 people (4 weeks, then move to Phase 1)
Phase 1: 4 people (3 on AppLocker/Firewall/Vault parallel, 1 on Quarantine)
Phase 2: 2 people (Service-Manager + Resource-Monitor parallel)
Phase 3: 2 people (Dashboard + AI-Core)
Phase 3 (later): +2 people (Self-Healing + Components)
```

---

**Document Version**: 1.0  
**Last Updated**: 2024  
**Maintained By**: Platform Architecture Team

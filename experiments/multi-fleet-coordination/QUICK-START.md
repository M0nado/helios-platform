# 🚀 HELIOS v4.0 Experiment 8: QUICK START GUIDE

**Experiment:** Multi-Fleet Coordination at Scale  
**Status:** Framework Ready, Execution Phase Pending  
**Duration:** ~4.5 hours  
**Start Time:** 2026-04-14 02:00:00Z  

---

## ⚡ 60-SECOND SUMMARY

**What:** Test if 1, 2, 3, and 4 fleets of 8 agents each can coordinate efficiently  
**Why:** Prove scaling works before deploying enterprise-scale systems  
**How:** Run 5 test scenarios measuring latency, efficiency, consistency, failover  
**When:** 2026-04-14 02:00:00Z → ~04:30:00Z  

**Expected Results:**
- ✅ 1 fleet: 100% efficiency (baseline)
- ✅ 2 fleets: 95% efficiency (independent, no overhead)
- ✓ 3 fleets: 92% efficiency (with coordination, ~5% overhead)
- ✓ 4 fleets: 88% efficiency (independent, ~1% overhead)

---

## 📂 WHAT'S IN THIS EXPERIMENT

| File | Size | Purpose |
|---|---|---|
| **multi-fleet-topology.json** | 18KB | All configurations & test specs |
| **Coordination-overhead.md** | 15KB | Latency predictions & analysis |
| **Failover-strategy.md** | 18KB | Recovery procedures |
| **Scaling-recommendations.md** | 20KB | Enterprise guidance |
| **Inter-fleet-communication-patterns.md** | 27KB | Protocol specifications |
| **INDEX.md** | 15KB | Complete overview |
| **QUICK-START.md** | This file | For operators like you |

**Total: ~120KB of comprehensive documentation**

---

## 🎯 QUICK DECISION TREE: WHICH FLEET CONFIG?

```
How much code per week?

<100K LOC  ───→  Single Fleet (8 agents)
                  ROI: 4.95, Efficiency: 100%

100K-500K  ───→  Single Fleet (8 agents) or Dual Independent (16)
                  ROI: 4.95, Efficiency: 100-95%

500K-1M    ───→  Dual Independent (16) or Tri-Coordinated (24)
                  ROI: 4.95-4.78, Efficiency: 95-92%

1M-2M      ───→  Tri-Coordinated (24) or Quad Independent (32)
                  ROI: 4.78-4.88, Efficiency: 92-88%

2M+        ───→  Quad Independent (32) or multiple deployments
                  ROI: 4.88 each, Efficiency: 88%

NEVER USE  ───→  Single 24+ agent fleet or hierarchical federation
                  Too much overhead, poor ROI
```

---

## 🧪 THE 5 TESTS AT A GLANCE

| # | Test | Duration | What We're Testing | Pass Criteria |
|---|---|---|---|---|
| **1** | Even Load | 5 min | Baseline performance | Efficiency ≥85% |
| **2** | Skewed Load | 5 min | Load balancing | No deadlocks |
| **3** | Dynamic Shift | 6 min | Adaptation | Zero message loss |
| **4** | Fleet Dies | 5 min | Failover speed | Recovery <2s |
| **5** | Network Slow | 5 min | Partition handling | Consistency ≥98% |

**Total Test Time: ~26 minutes (plus setup/analysis = 4.5 hours total)**

---

## 💡 5 KEY TAKEAWAYS FROM FRAMEWORK

### 1. **Optimal Fleet Size is 8 Agents (From Exp 6)**
- ROI: 4.95 (best in class)
- Overhead: 0% internal coordination
- Quality: 95% coverage, 2.1% duplication
- **→ Use this as building block, not larger fleets**

### 2. **Don't Scale Linearly, Scale in Steps of 8**
- Better: 4 fleets × 8 agents = 32 agents
- Worse: 1 fleet × 32 agents = 32 agents
- Reason: Coordination overhead grows with fleet count but stays manageable

### 3. **3 Fleets Maximum for Coordination**
- 3 fleets: 5% overhead (acceptable)
- 4 fleets: 18-20% overhead (too much)
- **→ Use independent fleets if you need 4+**

### 4. **Failover is FAST**
- Detection: 500ms
- Election: 300ms
- Recovery: 200ms
- Total: 1.2 seconds (under 2-second target)

### 5. **Message Ordering is Achievable**
- Use sequence numbers, not vector clocks
- Simpler, faster, still guarantees FIFO
- <1ms overhead per message

---

## 🔍 WHAT TO MONITOR DURING TESTING

### Real-Time Dashboard Metrics

```
Phase 1 (Single Fleet - Baseline):
├─ Throughput: 100 tasks/sec target
├─ Latency P99: <20ms
├─ CPU per agent: 25-35%
├─ Memory per agent: 500-750MB
└─ Quality: ≥95% code coverage

Phase 2 (Dual Independent):
├─ Combined throughput: 190-200 tasks/sec
├─ Latency P99: <20ms each fleet
├─ Cross-fleet sync: 0ms (none needed)
└─ Efficiency vs single: 95%

Phase 3 (Tri-Coordinated):
├─ Combined throughput: 270-290 tasks/sec
├─ Latency P99: <45ms (includes coordination)
├─ Sync overhead: 3-5%
├─ State consistency: >99%
└─ Queue depth: <500 items

Phase 4 (Quad Independent):
├─ Combined throughput: 340-360 tasks/sec
├─ Latency P99: <20ms each fleet
├─ Snapshot sync: 5-second intervals
└─ Efficiency vs single: 88%
```

---

## ⚠️ WHAT COULD GO WRONG

| Problem | How You'll Know | What to Do |
|---|---|---|
| **High Latency (>100ms P99)** | Dashboard shows red | Check network, increase connection pool |
| **Low Consistency (<98%)** | State divergence alert | Reduce gossip interval or use Raft |
| **Slow Failover (>3s)** | Failover test times out | Check state size, reduce timeout value |
| **High Overhead (>8%)** | Throughput drops sharply | Reduce message frequency, increase batch size |
| **Queue Overflow** | Queue depth >1000 | Reduce work arrival rate or add more agents |

---

## 📈 EXPECTED RESULTS BY FLEET CONFIG

### Fleet Config 1: Single (F1)
```
Agents:          8
Throughput:      100 tasks/sec
Latency P50:     5ms
Latency P99:     15ms
Efficiency:      100%
Overhead:        0%
Status:          BASELINE

Comment: Replicates Exp 6 results
```

### Fleet Config 2: Dual Independent (F2)
```
Agents:          16 (8 each)
Combined TPS:    190 tasks/sec
Latency P50:     5ms per fleet
Latency P99:     15ms per fleet
Cross-fleet:     0ms (no communication)
Efficiency:      95%
Overhead:        0% (independent)
Status:          EXPECTED ✓

Comment: 5% loss is single-fleet coordination
```

### Fleet Config 3: Tri-Coordinated (F3)
```
Agents:          24 (8 each)
Combined TPS:    270 tasks/sec
Latency P50:     10ms
Latency P99:     45ms
State sync time: 100-200ms
Consistency:     99-99.5%
Efficiency:      92%
Overhead:        5% (coordination)
Status:          AT TARGET ✓

Comment: 5% matches hypothesis threshold
```

### Fleet Config 4: Quad Independent (F4)
```
Agents:          32 (8 each)
Combined TPS:    340 tasks/sec
Latency P50:     5ms per fleet
Latency P99:     15ms per fleet
Snapshot sync:   5-second intervals
Consistency:     Eventual (seconds)
Efficiency:      88%
Overhead:        1% (snapshot only)
Status:          EXPECTED ✓

Comment: Snapshot sync minimal overhead
```

---

## 🎬 EXECUTION CHECKLIST (Step-by-Step)

### Pre-Test (30 minutes before)
- [ ] Verify all test configuration files are readable
- [ ] Start metrics collection system
- [ ] Clear any previous test data
- [ ] Verify network connectivity between test nodes
- [ ] Check available disk space (need 5GB for logs)
- [ ] Set system time to UTC (for consistent timestamps)
- [ ] Have monitoring dashboard open and ready

### Phase 1: Baseline Single Fleet (30 min)
- [ ] Start 1x 8-agent fleet
- [ ] Wait 2 minutes for stabilization
- [ ] Run 1000 test tasks
- [ ] Collect metrics (latency histogram, throughput)
- [ ] Verify matches Exp 6 results
- [ ] Document any differences

### Phase 2: Dual Fleet (45 min)
- [ ] Start 2x 8-agent fleets (independent)
- [ ] Wait 2 minutes for stabilization
- [ ] Run Test Scenario TS1 (even load, 2000 tasks)
- [ ] Run Test Scenario TS2 (skewed load, 3000 tasks)
- [ ] Collect cross-fleet metrics
- [ ] Verify no inter-fleet communication occurs

### Phase 3: Tri-Fleet (60 min)
- [ ] Start 3x 8-agent fleets (with coordination)
- [ ] Configure shared work queue
- [ ] Configure gossip protocol (200ms interval)
- [ ] Wait 2 minutes for stabilization
- [ ] Run TS1 (even load)
- [ ] Run TS2 (skewed load)
- [ ] Run TS3 (dynamic shifting)
- [ ] Run TS5 (network partition)
- [ ] Measure coordination overhead
- [ ] Verify consistency target

### Phase 4: Quad Fleet (45 min)
- [ ] Start 4x 8-agent fleets (independent)
- [ ] Configure snapshot sync (5-second interval)
- [ ] Wait 2 minutes for stabilization
- [ ] Run TS1 (even load)
- [ ] Run TS4 (fleet failure at 40%)
- [ ] Measure failover recovery time
- [ ] Verify work distribution

### Phase 5: Analysis (30 min)
- [ ] Aggregate all metrics into summary
- [ ] Test hypotheses against success criteria
- [ ] Generate final report
- [ ] Create recommendations

---

## 📊 HYPOTHESIS VALIDATION QUICK CHECK

After each test, check these:

**H1: 3-Fleet Overhead <5%**
```
Calculation: (F1_throughput - F3_throughput) / F1_throughput * 100
Expected: <5%
How to check: Dashboard shows efficiency >95% for tri-fleet
PASS if: ✅ Overhead ≤ 5%
```

**H2: Cross-Fleet Latency <50ms P99**
```
Measurement: Message round-trip time P99
Expected: <50ms
How to check: Latency histogram shows P99 under 50ms line
PASS if: ✅ P99 ≤ 50ms
```

**H3: Failover Recovery <2 seconds**
```
Measurement: Time from fleet death to first task resumed
Expected: <2000ms
How to check: Failover test shows recovery in <2000ms
PASS if: ✅ Total recovery ≤ 2000ms
```

**H4: Global Consistency 99.99%**
```
Measurement: Percentage of time all fleets agree on state
Expected: ≥99.99%
How to check: State consistency check runs every 100ms
PASS if: ✅ Consistency ≥ 99.99%
(Note: Gossip alone may not achieve this - may need Raft)
```

**H5: Scaling Efficiency O(log n)**
```
Measurement: Throughput vs agent count follows Amdahl's Law
Expected: 1→2 fleets: 95% eff, 2→3: 92% eff, 3→4: 88% eff
How to check: Efficiency curve matches O(log n) prediction
PASS if: ✅ Efficiency ≥ 85% for 2-4 fleets
```

---

## 🚀 AFTER TESTING IS COMPLETE

### Immediate (Next 30 minutes)
1. Export all metrics and logs
2. Generate summary statistics
3. Compare against baseline (Exp 6)
4. Write initial observations

### Short-term (Next 2 hours)
5. Complete full hypothesis analysis
6. Identify any bottlenecks or failures
7. Create recommendations doc
8. Plan any follow-up tests needed

### Long-term (For Enterprise Use)
9. Use Scaling-recommendations.md to decide fleet sizes
10. Use Failover-strategy.md for operational runbooks
11. Use Communication-patterns.md for implementation guide

---

## 💼 OPERATIONAL PLAYBOOKS (Print These)

### Playbook 1: Deploy Single Fleet
```
1. Configure: Size=8 agents, Pattern=none
2. Deploy: Start fleet process
3. Initialize: Prime work queue with 10 tasks
4. Monitor: Watch throughput (expect 100 tasks/sec)
5. Verify: Quality metrics match Exp 6 baseline
6. Operate: Add work as needed
```

### Playbook 2: Deploy Tri-Fleet Coordinated
```
1. Configure: 3x (Size=8), Pattern=shared_queue+gossip
2. Deploy: Start 3 fleets (staggered, 2 seconds apart)
3. Initialize: Start gossip protocol
4. Synchronize: Wait for state convergence (~1 second)
5. Monitor: Watch coordination overhead (<5%)
6. Load balance: Let work-stealing distribute tasks
7. Operate: Monitor queue depth, failover readiness
```

### Playbook 3: Emergency Failover
```
IF Fleet dies unexpectedly:
1. [AUTO] Detection: ~500ms (3x heartbeat timeout)
2. [AUTO] Election: ~300ms (surviving fleets vote)
3. [AUTO] Recovery: ~200ms (state transfer)
4. [MANUAL] Restart: Bring failed fleet back online
5. [MANUAL] Verify: Check state consistency
6. [MANUAL] Resume: Add fleet back to work queue
```

---

## 📞 QUICK REFERENCE

**Contact Info (When Tests Run):**
- Monitor Dashboard: http://localhost:9090 (if available)
- Log Location: C:\helios-v4\experiments\multi-fleet-coordination\logs\
- Metrics Export: C:\helios-v4\experiments\multi-fleet-coordination\metrics\
- Alert Threshold: Any metric red on dashboard

**Key Numbers to Remember:**
- 8 agents = optimal fleet size (from Exp 6)
- 100 tasks/sec = baseline throughput (1 fleet)
- 45ms = target P99 latency (cross-fleet)
- 5% = max coordination overhead (3 fleets)
- 1200ms = expected failover recovery (3 fleets)
- 2000ms = max acceptable failover (2-second target)
- 99.99% = global consistency target (may need Raft)

---

## ✅ SUCCESS CRITERIA (SIMPLE VERSION)

**Experiment SUCCEEDS if:**
- [x] All 5 tests complete without crashes
- [x] At least 4 of 5 hypotheses validate
- [x] Failover recovery < 2 seconds
- [x] Zero data loss or duplication
- [x] Scaling efficiency >85%

**Experiment NEEDS WORK if:**
- [ ] >1 hypothesis fails
- [ ] Failover recovery > 3 seconds
- [ ] Consistency <99%
- [ ] Data corruption or loss detected

---

## 🎓 WHAT YOU'LL LEARN

After running Experiment 8, you'll know:

1. **Fleet Scaling:** How to efficiently scale from 1→2→3→4 fleets
2. **Coordination Patterns:** Which pattern works best for your workload
3. **Failover Readiness:** How quickly system recovers from failure
4. **Consistency Models:** Trade-offs between consistency and latency
5. **Performance Baselines:** Real measurements vs theoretical predictions

---

## 📚 WHERE TO GO NEXT

**After Experiment 8:**

- **For deployment guidance:** See Scaling-recommendations.md
- **For operational runbooks:** See Failover-strategy.md
- **For protocol details:** See Inter-fleet-communication-patterns.md
- **For all details:** See INDEX.md
- **For theory:** See Coordination-overhead.md

**Future experiments:**
- Experiment 9: Multi-datacenter coordination
- Experiment 10: Heterogeneous agent types
- Experiment 11: 100+ agent enterprise deployment

---

## 🚀 YOU'RE READY!

Everything is designed. Configurations are ready. Test scenarios are clear.

**Next Step:** Begin Phase 1 (Baseline Testing) at 2026-04-14 02:00:00Z

**Expected Completion:** ~04:30:00Z (4.5 hours)

**Status: READY FOR EXECUTION ✅**

---

*This guide is your reference during testing. Bookmark this file!*

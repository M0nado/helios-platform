# HELIOS v4.0 Experiment 8: Failover Strategy & Recovery Procedures

**Experiment:** Multi-Fleet Coordination at Scale  
**Document:** Fleet Failure Recovery Playbook  
**Date:** 2026-04-14  
**Status:** OPERATIONAL FRAMEWORK

---

## 🎯 FAILOVER OBJECTIVES

1. **Minimize Work Loss:** Zero duplicate/dropped work detection
2. **Fast Recovery:** <2 seconds from detection to resumption
3. **Maintain Consistency:** No state corruption during failover
4. **Transparent Failover:** Clients unaware of internal failure
5. **Graceful Degradation:** System continues at reduced capacity

---

## 📊 FAILOVER CLASSIFICATION

### Fleet Failure Types

| Type | Detection | Recovery | Impact | Frequency |
|---|---|---|---|---|
| **Process Crash** | Heartbeat timeout | Restart + state restore | Medium | 0.1% |
| **Network Partition** | Gossip divergence | Quorum recovery | High | 1% |
| **State Corruption** | Consistency check | Snapshot rollback | Critical | 0.01% |
| **Cascading Failure** | Multiple deaths | System-wide failover | Catastrophic | <0.001% |

---

## 🔄 FAILOVER ARCHITECTURE

### Detection Phase (0-500ms)

```
┌─────────────────────────────────────────────────────┐
│ Heartbeat Monitoring (500ms intervals)              │
│                                                     │
│ Fleet A  ──heartbeat→  Central Monitor              │
│ Fleet B  ──heartbeat→  Central Monitor              │
│ Fleet C  ──heartbeat→  Central Monitor              │
│ Fleet D  ──heartbeat→  Central Monitor              │
└─────────────────────────────────────────────────────┘

Failure Detected When:
- Heartbeat missed 3 consecutive times
- Detection Time: 500ms × 3 = 1500ms typical
- Fast Detection: 500ms with aggressive probing
```

**Detection Algorithm:**

```python
def check_fleet_health(fleet_id, timeout_ms=500):
    last_heartbeat = heartbeat_log[fleet_id]
    current_time = get_time_ms()
    
    if current_time - last_heartbeat > timeout_ms * 3:
        # Triple timeout = confirmed dead
        return FLEET_DEAD
    elif current_time - last_heartbeat > timeout_ms:
        # Single timeout = suspect
        return FLEET_SUSPECT
    else:
        return FLEET_ALIVE
```

### Election Phase (500-1000ms)

```
┌─────────────────────────────────────────────────────┐
│ Leader Election (for coordinator role)              │
│                                                     │
│ Candidate Fleets: A, C, D (B is dead)              │
│                                                     │
│ Quorum Check:                                       │
│ ├─ 2 of 3 needed = majority                         │
│ ├─ Elect by highest priority + lowest latency       │
│ └─ Result: Fleet A = new coordinator                │
│                                                     │
│ Election Time: 200-500ms                            │
└─────────────────────────────────────────────────────┘
```

**Election Algorithm (Raft-Inspired):**

```python
def elect_new_coordinator(alive_fleets):
    # Round 1: Each fleet votes for highest-priority peer
    votes = {}
    for fleet in alive_fleets:
        highest_priority = max(alive_fleets, key=priority)
        votes[highest_priority] += 1
    
    # Round 2: Candidate with majority becomes coordinator
    for candidate, vote_count in votes.items():
        if vote_count > len(alive_fleets) / 2:
            return candidate  # Has majority
    
    # Timeout: random backoff and retry
    wait_random_ms(50, 100)
    return elect_new_coordinator(alive_fleets)
```

### State Transfer Phase (1000-1500ms)

```
┌─────────────────────────────────────────────────────┐
│ State Recovery & Transfer                           │
│                                                     │
│ 1. Identify Last Known State                        │
│    └─ Query survivor fleets for latest version      │
│                                                     │
│ 2. Restore Work Queue                              │
│    ├─ Dead fleet's assigned work → redistribute    │
│    ├─ In-flight work → mark as incomplete           │
│    └─ Completed work → verify with backup           │
│                                                     │
│ 3. Synchronize Global State                        │
│    └─ Quorum read: 2-of-3 fleets agree             │
│                                                     │
│ 4. Publish New Coordinator                          │
│    └─ Gossip new topology to all fleets             │
└─────────────────────────────────────────────────────┘
```

**State Transfer Protocol:**

```python
def transfer_state_from_dead_fleet(dead_fleet_id, alive_fleets):
    # Step 1: Get last known state
    latest_version = -1
    latest_state = None
    
    for fleet in alive_fleets:
        state_version = query_fleet(fleet, f"get_state_version({dead_fleet_id})")
        if state_version > latest_version:
            latest_version = state_version
            latest_state = query_fleet(fleet, f"get_state({dead_fleet_id})")
    
    # Step 2: Redistribute work from dead fleet
    orphaned_work = latest_state.get('assigned_work', [])
    for work_item in orphaned_work:
        target_fleet = select_least_loaded_fleet(alive_fleets)
        reassign_work(work_item, target_fleet)
    
    # Step 3: Update global state
    current_state = quorum_read_state(alive_fleets, 2)
    current_state['dead_fleet'] = dead_fleet_id
    current_state['timestamp'] = get_time_ms()
    broadcast_state_update(alive_fleets, current_state)
    
    return True
```

### Resumption Phase (1500-2000ms)

```
┌─────────────────────────────────────────────────────┐
│ System Resumption                                   │
│                                                     │
│ 1. Reset Global Counters                           │
│    ├─ Clear work queue locks                        │
│    ├─ Reset message sequence numbers                │
│    └─ Acknowledge state version change              │
│                                                     │
│ 2. Resume Task Distribution                        │
│    ├─ Work queue accepts new tasks                 │
│    ├─ Remaining fleets begin work stealing          │
│    └─ Load balancing recomputes                     │
│                                                     │
│ 3. Notify Clients                                  │
│    ├─ Publish new topology                         │
│    ├─ Provide estimated recovery time               │
│    └─ Resume client requests                        │
│                                                     │
│ Total Time: ~500ms for this phase                  │
└─────────────────────────────────────────────────────┘
```

---

## 📋 FAILOVER SCENARIOS & RESPONSES

### Scenario 1: Single Fleet Failure (3-Fleet System)

**Setup:** Tri-fleet coordinated system, Fleet B dies

**Timeline:**

```
T=0ms:      Fleet B process crashes
T=500ms:    Fleet A notices missing heartbeat
T=1000ms:   Fleet A confirms B is dead (3 timeouts)
T=1100ms:   Election starts
T=1300ms:   Fleet A elected coordinator
T=1400ms:   State transfer from C about B's work
T=1600ms:   Work reassigned to A and C
T=1800ms:   New topology broadcast
T=2000ms:   System resumed, new tasks accepted
```

**Work Recovery:**

```
Fleet B was processing:
├─ Work ID 42: Analysis of module X (70% complete)
│  └─ Recovery: Mark as failed, requeue for retry
│
├─ Work ID 43: Test generation (complete, not delivered)
│  └─ Recovery: Check if duplicate, if not, deliver result
│
└─ Work ID 44: Documentation (not started)
   └─ Recovery: Reassign to Fleet A

Reassignment Logic:
- Fleet A current load: 40 tasks in queue
- Fleet C current load: 35 tasks in queue
- Fleet B work: 3 tasks
- Redistribute to C (least loaded)
```

**Success Metrics:**

- ✓ Recovery time: ~2000ms (within 2-second target)
- ✓ Work loss: 0% (all work requeued or reassigned)
- ✓ State consistency: 100% (quorum read used)
- ✓ No duplicates: Sequence numbers prevent re-execution

---

### Scenario 2: Network Partition (3 Fleets → 1 + 2)

**Setup:** Network splits, Fleet B isolated from A & C

**Detection:**

```
Gossip Failure Mode:
├─ A ↔ B: Messages queued but never delivered
├─ A ↔ C: Normal operation
├─ B is isolated but doesn't know

Detection:
├─ B's state version diverges from A/C
├─ A notices B's state is stale
├─ After 3 gossip rounds (1500ms), A marks B suspicious
├─ After gossip convergence fails (3000ms), A declares B dead
```

**Recovery Strategy:**

```
Phase 1 (Detection): 3000ms to detect partition
Phase 2: Election between A and C
         ├─ 2 of 3 needed for quorum
         ├─ A + C have quorum = proceed
         ├─ B is minority = stonewalled
         └─ Decision: Treat B as dead
         
Phase 3: Work recovery
         └─ B's assigned work redistributed to A/C
         
Phase 4: If partition heals
         ├─ B reconnects and sees new state version
         ├─ B performs full sync from A or C
         ├─ B resumes as member (not coordinator)
         └─ No double-work due to sequence numbering
```

**Key Property:** Split-brain prevention
- Quorum (2 of 3) prevents both sides from thinking they're leader
- Only majority partition accepts new work
- Minority partition rejects requests until healed

---

### Scenario 3: Cascading Failure (All 3 Fleets Down)

**Setup:** Fleet A, B, C all fail in sequence within 5 seconds

**Detection:**

```
T=0ms:    Fleet A crashes
T=500ms:  B notices A is dead
T=1000ms: B becomes coordinator (only survivor)
T=1500ms: B starts work recovery from A
T=2000ms: Fleet B crashes
T=2500ms: Fleet C notices B is dead
T=3000ms: C confirms B is dead, becomes coordinator
T=3500ms: C recovers both A's and B's work
         └─ Now has 24 agents worth of work to process
```

**Recovery:**

```
Fleet C's new responsibilities:
├─ Process work originally assigned to A: 8+ tasks
├─ Process work originally assigned to B: 8+ tasks  
├─ Process its own work: 8+ tasks
└─ Total: Potentially 24+ pending tasks

Capacity:
├─ Fleet C has 8 agents
├─ Time to clear: ~3x normal (8 tasks per second)
└─ Estimated time: 3-4 seconds

Safety:
✓ Work ordering preserved (sequence numbers)
✓ No duplicates (by task ID + version)
✓ State consistent (quorum = 1, trivially true)
```

**Limitations:**

- ⚠️ System operates at 1/3 capacity
- ⚠️ No redundancy (if C fails, work lost)
- ✓ But: Work is recoverable from persistent storage
- ✓ Recommendation: Rebuild fleets from work queue

---

### Scenario 4: Asymmetric Failure (Partial Hang)

**Setup:** Fleet B is alive but responding slowly (5+ second latency)

**Detection:**

```
Normal heartbeat: 500ms round-trip
B's heartbeat: 5000ms round-trip

Detection Logic:
├─ Timeout set to 2000ms
├─ B misses 3 consecutive timeouts = 6000ms total
├─ A declares B dead at T=6000ms

Result:
└─ Treat B as dead, trigger failover
```

**Recovery:**

```
When B finally responds:
├─ B sees new coordinator is A
├─ B detects it's behind on state version
├─ B performs full state sync
├─ B rejoins as subordinate member
└─ Work assigned to B during outage already reassigned

No duplication because:
✓ Sequence numbers prevent re-execution
✓ Completed work IDs tracked globally
✓ Only incomplete work reprocessed
```

---

## 🛡️ FAILURE PREVENTION MEASURES

### 1. Health Monitoring

```python
class FleetHealthMonitor:
    def __init__(self, timeout_ms=500):
        self.heartbeat_log = {}
        self.timeout_ms = timeout_ms
    
    def record_heartbeat(self, fleet_id, timestamp):
        self.heartbeat_log[fleet_id] = timestamp
    
    def check_health(self):
        now = time.time_ms()
        dead_fleets = []
        
        for fleet_id, last_hb in self.heartbeat_log.items():
            age = now - last_hb
            
            if age > self.timeout_ms * 3:
                dead_fleets.append(fleet_id)
                self.trigger_failover(fleet_id)
        
        return dead_fleets
    
    # Check every 100ms for fast detection
    def monitor_loop(self):
        while True:
            self.check_health()
            sleep_ms(100)
```

### 2. State Snapshots

```
Frequency: Every 5 seconds
Location: Replicated to all living fleets
Contents:
  ├─ Work queue snapshot
  ├─ Completed work hash
  ├─ State version number
  ├─ Timestamp
  └─ Coordinator identity

Recovery: If all fleets down, restore from persistent snapshot
```

### 3. Duplicate Detection

```python
class DuplicateDetector:
    def __init__(self):
        self.completed_work_hashes = set()
        self.work_in_flight = {}
    
    def check_for_duplicate(self, work_id, work_hash):
        if work_hash in self.completed_work_hashes:
            return True  # Duplicate found
        
        if work_id in self.work_in_flight:
            return True  # Already assigned
        
        return False  # New work
    
    def mark_complete(self, work_id, work_hash):
        self.completed_work_hashes.add(work_hash)
        self.work_in_flight.pop(work_id, None)
    
    def mark_assigned(self, work_id, target_fleet):
        self.work_in_flight[work_id] = target_fleet
```

### 4. Work Persistence

```
Persistent Work Queue:
├─ Durable storage for all queued work
├─ Updated on each state change
├─ Allows recovery without coordination
└─ Recovery time: O(work_queue_size)

Durability Guarantee:
✓ Work persisted before ACK sent to client
✓ No work loss even if all fleets crash
✓ Trade-off: +5-10ms latency per work item
```

---

## 📈 FAILOVER METRICS

### Target Metrics

| Metric | Target | Worst-Case | Measurement |
|---|---|---|---|
| **Detection Time** | <1000ms | <3000ms | From death to confirmation |
| **Election Time** | <500ms | <2000ms | From confirmation to new leader |
| **State Transfer** | <300ms | <1000ms | From sync start to completion |
| **System Resumption** | <200ms | <500ms | From state transfer to work resumption |
| **Total Recovery** | <2000ms | <5000ms | Death to accepting new work |

### Actual Expected Results (3-Fleet)

```
Detection:         500ms  (1 missed heartbeat + timeout)
Election:          300ms  (simple voting)
State Transfer:    200ms  (compact state)
Resumption:        200ms  (reset queues)
──────────────────────────
Total:           1200ms  ✓ Well under 2-second target
```

---

## 🔧 OPERATIONAL PROCEDURES

### Planned Maintenance

```
Procedure: Rolling restart without downtime

1. Remove target fleet from work queue (graceful drain)
   └─ Stop accepting new work, finish in-flight

2. Wait for work queue to drain (timeout 30 seconds)
   └─ Remaining fleets pick up any queued work

3. Restart target fleet
   └─ On startup: full state sync from peers

4. Validate state consistency (should be immediate)
   └─ Query peer fleets for current state

5. Resume normal operation
   └─ Add back to work queue
   └─ Resume work stealing
```

### Emergency Failure Response

```
IF Fleet A Dies (unexpected):
├─ [AUTOMATIC] Detection in <1000ms
├─ [AUTOMATIC] Election in <500ms
├─ [AUTOMATIC] Work redistribution in <300ms
├─ [AUTOMATIC] Resume in <500ms
└─ [MANUAL] Restart Fleet A
    ├─ Perform full state sync
    └─ Rejoin as subordinate

IF Multiple Fleets Die Simultaneously:
├─ [AUTOMATIC] Each death triggers failover
├─ [AUTOMATIC] Remaining fleets coordinate
├─ [MANUAL] Urgent: Restart fleets one by one
    └─ Wait for each to fully sync before next
└─ [MANUAL] Verify data integrity post-recovery
```

### Disaster Recovery

```
IF All Fleets Are Down:
1. Identify persistent storage location
2. Restore latest work queue snapshot
3. Start 1 fleet and perform full recovery
4. Verify work queue integrity
5. Start remaining fleets (sequential)
   └─ Each waits for sync from current leader
6. Resume normal operation
7. Verify no duplicates (check sequence numbers)
```

---

## ✅ VALIDATION CHECKLIST

Before deployment:

- [ ] Heartbeat monitoring functional
- [ ] Election algorithm tested with 2-4 fleets
- [ ] State transfer protocol tested
- [ ] Duplicate detection verified
- [ ] Work persistence working
- [ ] Rollback tested (reverting failed restart)
- [ ] Split-brain scenarios tested
- [ ] Recovery time <2 seconds (3 fleets)
- [ ] Recovery time <5 seconds (cascading failure)
- [ ] State consistency maintained throughout
- [ ] Documentation complete
- [ ] Team trained on procedures

---

## 🚀 FAILOVER TEST PLAN (Experiment 8)

### Test 1: Single Fleet Failure
- **Setup:** 3-fleet system, balanced load
- **Action:** Kill Fleet B at 40% completion
- **Expect:** 
  - Detection in <1000ms
  - Recovery in <2000ms
  - Zero work loss
  - Zero duplicates

### Test 2: Cascading Failures
- **Setup:** 3-fleet system
- **Action:** Kill A, then B (within 5 seconds)
- **Expect:**
  - Fleet C survives and operates normally
  - All work recovered
  - Time to first survivor: <1000ms
  - Time to resume work: <2000ms

### Test 3: Network Partition
- **Setup:** 3-fleet system
- **Action:** Partition B from A+C for 10 seconds
- **Expect:**
  - A+C continue (have quorum)
  - B rejected for updates
  - Partition heals: B resync in <5 seconds
  - No double-work on rejoining

### Test 4: Asymmetric Failure
- **Setup:** 3-fleet system
- **Action:** Make B respond with 5-second latency
- **Expect:**
  - Timeout triggers failover
  - B recovery: transition from dead to live
  - Minimal disruption when restored to normal latency

---

**Status:** ✅ Framework Ready for Testing

*Next: Implement failover code and run tests in Experiment 8 execution phase*

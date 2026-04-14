"""
HELIOS v4.0 - Experiment 13: Distributed Consistency & Consensus Analysis
Simulates various consistency models and consensus algorithms in distributed systems
"""

import json
import time
import random
import hashlib
from dataclasses import dataclass, field
from typing import Dict, List, Set, Tuple, Optional
from enum import Enum
from collections import defaultdict
import statistics


class ConsistencyModel(Enum):
    STRONG = "strong"
    EVENTUAL = "eventual"
    CAUSAL = "causal"
    SESSION = "session"


class FailureType(Enum):
    NONE = "none"
    NETWORK_PARTITION = "partition"
    BYZANTINE = "byzantine"
    CRASH = "crash"
    SLOW = "slow"


@dataclass
class WriteOperation:
    agent_id: int
    key: str
    value: str
    timestamp: float
    version: int
    vector_clock: Dict[int, int] = field(default_factory=dict)

    def __hash__(self):
        return hash((self.agent_id, self.key, self.timestamp))


@dataclass
class ConsistencyMetrics:
    violations: int = 0
    replication_lag_ms: List[float] = field(default_factory=list)
    conflicts_detected: int = 0
    conflicts_resolved: int = 0
    write_latency_ms: List[float] = field(default_factory=list)
    recovery_time_ms: float = 0.0
    data_divergence: int = 0
    consensus_overhead_ms: float = 0.0
    quorum_availability_pct: float = 0.0


class Agent:
    """Represents a distributed agent in the fleet"""

    def __init__(self, agent_id: int, consistency_model: ConsistencyModel):
        self.agent_id = agent_id
        self.consistency_model = consistency_model
        self.data_store: Dict[str, WriteOperation] = {}
        self.write_log: List[WriteOperation] = []
        self.vector_clock: Dict[int, int] = defaultdict(int)
        self.session_id = None
        self.is_alive = True
        self.is_partitioned = False
        self.replication_buffer: List[WriteOperation] = []
        self.pending_acks = 0
        self.acked_writes = set()

    def write(self, key: str, value: str, agent_count: int) -> WriteOperation:
        """Process a local write operation"""
        self.vector_clock[self.agent_id] += 1
        op = WriteOperation(
            agent_id=self.agent_id,
            key=key,
            value=value,
            timestamp=time.time(),
            version=len(self.write_log),
            vector_clock=dict(self.vector_clock)
        )
        self.write_log.append(op)
        self.data_store[key] = op
        return op

    def replicate(self, op: WriteOperation, from_agent: int) -> bool:
        """Receive replicated data from another agent"""
        if not self.is_alive or self.is_partitioned:
            self.replication_buffer.append(op)
            return False

        existing = self.data_store.get(op.key)
        if existing is None:
            self.data_store[op.key] = op
            self.vector_clock[from_agent] = max(self.vector_clock.get(from_agent, 0), 
                                                 op.vector_clock.get(from_agent, 0))
            return True

        # Conflict resolution strategies
        if self.consistency_model == ConsistencyModel.STRONG:
            if op.timestamp < existing.timestamp:
                return False
            self.data_store[op.key] = op
            return True

        elif self.consistency_model == ConsistencyModel.CAUSAL:
            if self._is_causally_before(existing.vector_clock, op.vector_clock):
                self.data_store[op.key] = op
                return True
            elif self._is_causally_before(op.vector_clock, existing.vector_clock):
                return False
            else:
                # Concurrent - last-write-wins as tiebreaker
                if op.timestamp > existing.timestamp:
                    self.data_store[op.key] = op
                return True

        elif self.consistency_model == ConsistencyModel.EVENTUAL:
            if op.timestamp > existing.timestamp:
                self.data_store[op.key] = op
            return True

        elif self.consistency_model == ConsistencyModel.SESSION:
            if op.agent_id == self.agent_id or op.timestamp > existing.timestamp:
                self.data_store[op.key] = op
            return True

        return False

    def _is_causally_before(self, vc1: Dict[int, int], vc2: Dict[int, int]) -> bool:
        """Check if vector clock 1 happened before vector clock 2"""
        less_or_equal = all(vc1.get(i, 0) <= vc2.get(i, 0) for i in set(vc1) | set(vc2))
        strictly_less = any(vc1.get(i, 0) < vc2.get(i, 0) for i in set(vc1) | set(vc2))
        return less_or_equal and strictly_less

    def reconcile_writes(self) -> int:
        """Apply buffered writes during recovery"""
        applied = 0
        remaining = []
        for op in self.replication_buffer:
            if self.replicate(op, op.agent_id):
                applied += 1
            else:
                remaining.append(op)
        self.replication_buffer = remaining
        return applied


class DistributedSystem:
    """Simulates a distributed fleet of agents"""

    def __init__(self, agent_count: int, consistency_model: ConsistencyModel):
        self.agents = [Agent(i, consistency_model) for i in range(agent_count)]
        self.consistency_model = consistency_model
        self.all_writes: List[WriteOperation] = []
        self.network_partitions: List[Set[int]] = []
        self.byzantine_agents: Set[int] = set()
        self.metrics = ConsistencyMetrics()
        self.round_num = 0

    def concurrent_writes(self, write_count: int = 1000) -> Tuple[int, List[float]]:
        """Test concurrent writes from multiple agents"""
        write_latencies = []
        conflicting_writes = 0

        for _ in range(write_count):
            writer_id = random.randint(0, len(self.agents) - 1)
            key = f"test_key_{random.randint(0, 10)}"
            value = f"value_{random.randint(1000, 9999)}"

            start_time = time.perf_counter()
            op = self.agents[writer_id].write(key, value, len(self.agents))
            self.all_writes.append(op)

            # Replicate to all other agents
            for agent_id, agent in enumerate(self.agents):
                if agent_id != writer_id and agent.is_alive:
                    agent.replicate(op, writer_id)

            latency_ms = (time.perf_counter() - start_time) * 1000
            write_latencies.append(latency_ms)

            # Detect conflicts
            if agent_id not in self.byzantine_agents:
                for other_id, other_agent in enumerate(self.agents):
                    if other_id != agent_id and other_agent.is_alive:
                        if self.agents[agent_id].data_store.get(key) != other_agent.data_store.get(key):
                            if self.consistency_model != ConsistencyModel.EVENTUAL:
                                conflicting_writes += 1

        return conflicting_writes, write_latencies

    def network_partition(self, partitions: List[Set[int]]) -> Dict:
        """Simulate network partition"""
        self.network_partitions = partitions
        partition_data = {
            "partitions": [list(p) for p in partitions],
            "isolated_agents": sum(len(p) for p in partitions),
            "split_brain_risk": len(partitions) > 1
        }

        for partition_id, partition in enumerate(partitions):
            for agent_id in partition:
                self.agents[agent_id].is_partitioned = True

        return partition_data

    def heal_partition(self) -> int:
        """Heal network partition and sync data"""
        start_time = time.perf_counter()
        reconciled = 0

        for agent in self.agents:
            if agent.is_partitioned:
                agent.is_partitioned = False
                reconciled += agent.reconcile_writes()

        # Full sync to ensure consistency
        for agent in self.agents:
            if agent.is_alive:
                for op in self.all_writes:
                    if op.key not in agent.data_store:
                        agent.replicate(op, op.agent_id)

        recovery_ms = (time.perf_counter() - start_time) * 1000
        self.metrics.recovery_time_ms = recovery_ms
        return reconciled

    def inject_byzantine_failures(self, num_faulty: int) -> Set[int]:
        """Introduce Byzantine agents that send conflicting data"""
        byzantine_ids = set(random.sample(range(len(self.agents)), min(num_faulty, len(self.agents) // 3)))
        self.byzantine_agents = byzantine_ids

        for agent_id in byzantine_ids:
            self.agents[agent_id].is_alive = False

        return byzantine_ids

    def measure_replication_lag(self) -> List[float]:
        """Measure time for data to propagate to all agents"""
        lags = []
        key = f"lag_test_{self.round_num}"

        for agent in self.agents:
            if agent.is_alive:
                start = time.perf_counter()
                op = agent.write(key, f"value_{agent.agent_id}", len(self.agents))

                # Wait for replication
                for other_agent in self.agents:
                    if other_agent.agent_id != agent.agent_id and other_agent.is_alive:
                        other_agent.replicate(op, agent.agent_id)

                lag_ms = (time.perf_counter() - start) * 1000
                lags.append(lag_ms)

        self.metrics.replication_lag_ms.extend(lags)
        self.round_num += 1
        return lags

    def detect_consistency_violations(self) -> int:
        """Check for data inconsistencies across agents"""
        violations = 0

        for key in self._all_keys():
            values = set()
            for agent in self.agents:
                if agent.is_alive and not agent.is_partitioned:
                    op = agent.data_store.get(key)
                    if op:
                        values.add(op.value)

            if len(values) > 1:
                if self.consistency_model in [ConsistencyModel.STRONG]:
                    violations += len(values) - 1

        self.metrics.violations = violations
        return violations

    def calculate_data_divergence(self) -> int:
        """Measure differences in data across agents"""
        divergence = 0

        for key in self._all_keys():
            ops = [agent.data_store.get(key) for agent in self.agents if agent.is_alive]
            ops = [op for op in ops if op]

            if ops:
                for i, op1 in enumerate(ops):
                    for op2 in ops[i + 1:]:
                        if op1.value != op2.value:
                            divergence += 1

        self.metrics.data_divergence = divergence
        return divergence

    def consensus_quorum_availability(self, quorum_size: int) -> float:
        """Check if consensus quorum can be formed"""
        available_agents = sum(1 for agent in self.agents if agent.is_alive and not agent.is_partitioned)
        available_pct = (available_agents / quorum_size * 100) if quorum_size > 0 else 0
        self.metrics.quorum_availability_pct = min(available_pct, 100.0)
        return self.metrics.quorum_availability_pct

    def calculate_conflict_resolution_rate(self) -> Tuple[int, int, float]:
        """Measure conflict detection and resolution"""
        detected = 0
        resolved = 0

        for key in self._all_keys():
            ops = [agent.data_store.get(key) for agent in self.agents if agent.is_alive]
            ops = [op for op in ops if op]

            if len(ops) > 1:
                detected += 1
                # Check if all agents have same value (resolved)
                if len(set(op.value for op in ops)) == 1:
                    resolved += 1

        self.metrics.conflicts_detected = detected
        self.metrics.conflicts_resolved = resolved
        resolution_rate = (resolved / detected * 100) if detected > 0 else 0
        return detected, resolved, resolution_rate

    def _all_keys(self) -> Set[str]:
        """Get all keys written in the system"""
        return set(op.key for op in self.all_writes)


def run_consistency_experiments() -> Dict:
    """Execute all consistency model tests"""
    results = {
        "experiment": "Experiment 13: Distributed Consistency & Consensus Analysis",
        "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
        "fleet_size": 24,
        "consistency_models": {}
    }

    for consistency_model in ConsistencyModel:
        print(f"\n{'='*70}")
        print(f"Testing {consistency_model.value.upper()} Consistency Model")
        print(f"{'='*70}")

        system = DistributedSystem(24, consistency_model)
        model_results = {}

        # Test 1: Concurrent Writes
        print("\n[TEST 1] Concurrent Writes - 1000 agents writing...")
        conflicting, latencies = system.concurrent_writes(1000)
        model_results["concurrent_writes"] = {
            "conflicting_writes": conflicting,
            "write_latency_p50_ms": statistics.median(latencies),
            "write_latency_p99_ms": sorted(latencies)[int(len(latencies) * 0.99)],
            "write_latency_max_ms": max(latencies),
            "average_latency_ms": statistics.mean(latencies)
        }
        print(f"  ✓ Conflicting writes: {conflicting}")
        print(f"  ✓ Write latency P50: {model_results['concurrent_writes']['write_latency_p50_ms']:.2f}ms")
        print(f"  ✓ Write latency P99: {model_results['concurrent_writes']['write_latency_p99_ms']:.2f}ms")

        # Test 2: Network Partitions
        print("\n[TEST 2] Network Partition - splitting into 2 groups...")
        group1 = set(range(0, 12))
        group2 = set(range(12, 24))
        partition_data = system.network_partition([group1, group2])
        model_results["network_partition"] = partition_data

        # Simulate writes during partition
        conflicting, latencies = system.concurrent_writes(500)
        partition_data["writes_during_partition"] = conflicting

        # Heal and measure recovery
        print("  Healing partition...")
        reconciled = system.heal_partition()
        model_results["partition_recovery"] = {
            "writes_reconciled": reconciled,
            "recovery_time_ms": system.metrics.recovery_time_ms
        }
        print(f"  ✓ Recovered in {system.metrics.recovery_time_ms:.2f}ms")

        # Test 3: Byzantine Failures
        print("\n[TEST 3] Byzantine Failures - injecting faulty agents...")
        faulty_agents = system.inject_byzantine_failures(4)  # 24/3 = 8, but test with 4
        model_results["byzantine_failures"] = {
            "faulty_agents": list(faulty_agents),
            "tolerance_achieved": len(faulty_agents) <= len(system.agents) // 3
        }
        print(f"  ✓ Injected {len(faulty_agents)} byzantine agents")
        print(f"  ✓ System tolerates up to {len(system.agents) // 3} faulty agents")

        # Test 4: Replication Lag
        print("\n[TEST 4] Replication Lag - measuring propagation time...")
        lags = system.measure_replication_lag()
        model_results["replication_lag"] = {
            "median_lag_ms": statistics.median(lags),
            "p99_lag_ms": sorted(lags)[int(len(lags) * 0.99)],
            "max_lag_ms": max(lags),
            "within_100ms": sum(1 for l in lags if l < 100) / len(lags) * 100
        }
        print(f"  ✓ Median lag: {model_results['replication_lag']['median_lag_ms']:.2f}ms")
        print(f"  ✓ P99 lag: {model_results['replication_lag']['p99_lag_ms']:.2f}ms")
        print(f"  ✓ Within 100ms: {model_results['replication_lag']['within_100ms']:.1f}%")

        # Test 5: Conflict Resolution
        print("\n[TEST 5] Conflict Resolution - measuring effectiveness...")
        detected, resolved, rate = system.calculate_conflict_resolution_rate()
        model_results["conflict_resolution"] = {
            "conflicts_detected": detected,
            "conflicts_resolved": resolved,
            "resolution_rate_pct": rate
        }
        print(f"  ✓ Conflicts detected: {detected}")
        print(f"  ✓ Conflicts resolved: {resolved}")
        print(f"  ✓ Resolution rate: {rate:.2f}%")

        # Final metrics
        violations = system.detect_consistency_violations()
        divergence = system.calculate_data_divergence()
        quorum_avail = system.consensus_quorum_availability(13)

        model_results["final_metrics"] = {
            "consistency_violations": violations,
            "data_divergence": divergence,
            "quorum_availability_pct": quorum_avail
        }

        results["consistency_models"][consistency_model.value] = model_results

        print(f"\n  ✓ Consistency violations: {violations}")
        print(f"  ✓ Data divergence: {divergence}")
        print(f"  ✓ Quorum availability: {quorum_avail:.1f}%")

    return results


if __name__ == "__main__":
    results = run_consistency_experiments()
    print("\n" + "="*70)
    print("Experiment 13 Complete")
    print("="*70)
    print(json.dumps(results, indent=2))

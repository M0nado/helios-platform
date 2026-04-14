/**
 * HELIOS v4.0 Fault Injection Framework
 * 
 * Comprehensive failure mode injection and measurement system
 * for testing resilience across all hierarchy levels (2-5)
 */

class FailureScenario {
  constructor(id, name, description, failureType) {
    this.id = id;
    this.name = name;
    this.description = description;
    this.failureType = failureType;
    this.startTime = null;
    this.endTime = null;
    this.recovered = false;
    this.metrics = {};
  }

  start() {
    this.startTime = Date.now();
  }

  end() {
    this.endTime = Date.now();
  }

  getDuration() {
    if (!this.startTime || !this.endTime) return null;
    return this.endTime - this.startTime;
  }
}

class AgentMockState {
  constructor(agentId, tier = 'worker') {
    this.agentId = agentId;
    this.tier = tier; // worker, coordinator-1, coordinator-2, coordinator-3, coordinator-4
    this.healthy = true;
    this.tasksInFlight = [];
    this.tasksCompleted = [];
    this.lastHeartbeat = Date.now();
    this.state = {}; // arbitrary state
    this.isResponding = true;
    this.errorRate = 0;
    this.corruptDataRate = 0;
  }

  crash() {
    this.healthy = false;
    this.isResponding = false;
  }

  recover() {
    this.healthy = true;
    this.isResponding = true;
    this.errorRate = 0;
    this.corruptDataRate = 0;
  }
}

class FaultInjectionFramework {
  constructor(config = {}) {
    this.config = {
      heartbeatInterval: 50,
      detectionTimeout: 100,
      recoveryTimeout: 5000,
      simulationSpeed: 1.0, // 1.0 = realtime, >1.0 = faster
      ...config
    };

    this.agents = new Map();
    this.scenarios = [];
    this.metrics = {
      detectionTime: [],
      recoveryTime: [],
      dataLoss: [],
      consistencyViolations: [],
      cascadingEffects: [],
      requestFailures: []
    };
    this.timeline = [];
  }

  // ===== Setup Methods =====

  createAgentFleet(size = 16, coordinatorLevels = [2, 3]) {
    // Create a fleet with multiple hierarchy levels
    const fleet = new Map();
    
    // Create worker agents
    for (let i = 0; i < size; i++) {
      const agent = new AgentMockState(`worker-${i}`, 'worker');
      fleet.set(agent.agentId, agent);
    }

    // Add coordinators for each level
    for (const level of coordinatorLevels) {
      const coordCount = Math.ceil(size / (4 ** (level - 1)));
      for (let i = 0; i < coordCount; i++) {
        const tierName = `coordinator-${level}-${i}`;
        const agent = new AgentMockState(tierName, `coordinator-${level}`);
        fleet.set(agent.agentId, agent);
      }
    }

    this.agents = fleet;
    return fleet;
  }

  getAgentsByTier(tier) {
    return Array.from(this.agents.values()).filter(a => a.tier === tier);
  }

  // ===== Failure Injection Methods =====

  injectRandomAgentFailure(targetTier = 'worker') {
    const candidates = this.getAgentsByTier(targetTier);
    if (candidates.length === 0) return null;
    
    const target = candidates[Math.floor(Math.random() * candidates.length)];
    target.crash();
    
    this.log('INJECT', `Random ${targetTier} failure: ${target.agentId}`);
    return {
      type: 'random-agent-failure',
      target: target.agentId,
      tier: targetTier
    };
  }

  injectCascadingFailure(initialTier = 'worker', cascadeDepth = 3) {
    // Start with initial failure, then cascade to dependent agents
    const failures = [];
    const initialFailure = this.injectRandomAgentFailure(initialTier);
    if (!initialFailure) return [];
    
    failures.push(initialFailure);

    // Simulate cascading: dependent agents fail
    for (let i = 0; i < cascadeDepth - 1; i++) {
      const nextTier = ['worker', 'coordinator-1', 'coordinator-2', 'coordinator-3'][i];
      if (Math.random() < 0.6) { // 60% chance to cascade
        const nextFailure = this.injectRandomAgentFailure(nextTier);
        if (nextFailure) failures.push(nextFailure);
      }
    }

    this.log('INJECT', `Cascading failure: ${failures.length} agents down`);
    return failures;
  }

  injectResourceExhaustion(targetTier = 'worker', resourceType = 'memory') {
    const candidates = this.getAgentsByTier(targetTier);
    if (candidates.length === 0) return null;
    
    const target = candidates[Math.floor(Math.random() * candidates.length)];
    target.healthy = false;
    target.corruptDataRate = 0.3;
    target.errorRate = 0.5;
    
    this.log('INJECT', `${resourceType} exhaustion on ${target.agentId}`);
    return {
      type: 'resource-exhaustion',
      target: target.agentId,
      resourceType,
      tier: targetTier
    };
  }

  injectTimeoutFailure(targetTier = 'worker', timeoutMs = 5000) {
    const candidates = this.getAgentsByTier(targetTier);
    if (candidates.length === 0) return null;
    
    const target = candidates[Math.floor(Math.random() * candidates.length)];
    target.isResponding = false;
    
    this.log('INJECT', `Timeout failure on ${target.agentId} (${timeoutMs}ms)`);
    
    // Schedule recovery
    setTimeout(() => {
      target.isResponding = true;
    }, timeoutMs / this.config.simulationSpeed);

    return {
      type: 'timeout-failure',
      target: target.agentId,
      tier: targetTier,
      timeoutMs
    };
  }

  injectByzantineFailure(targetTier = 'worker') {
    const candidates = this.getAgentsByTier(targetTier);
    if (candidates.length === 0) return null;
    
    const target = candidates[Math.floor(Math.random() * candidates.length)];
    target.corruptDataRate = 1.0; // Always return corrupt data
    
    this.log('INJECT', `Byzantine failure on ${target.agentId}`);
    return {
      type: 'byzantine-failure',
      target: target.agentId,
      tier: targetTier
    };
  }

  injectCoordinatorFailure(level = 2) {
    const coordinators = this.getAgentsByTier(`coordinator-${level}`);
    if (coordinators.length === 0) return null;
    
    // Always pick the primary (first one)
    const target = coordinators[0];
    target.crash();
    
    this.log('INJECT', `Coordinator-${level} failure: ${target.agentId}`);
    return {
      type: 'coordinator-failure',
      level,
      target: target.agentId
    };
  }

  injectNetworkPartition(partition1Ids, partition2Ids) {
    // Mark agents as unable to communicate with each other
    const affectedAgents = [...partition1Ids, ...partition2Ids];
    for (const agentId of affectedAgents) {
      const agent = this.agents.get(agentId);
      if (agent) {
        agent.networkPartitioned = true;
      }
    }
    
    this.log('INJECT', `Network partition: ${partition1Ids.length} vs ${partition2Ids.length} agents`);
    return {
      type: 'network-partition',
      partition1: partition1Ids,
      partition2: partition2Ids
    };
  }

  injectNetworkDegradation(packetLossRate = 0.5, latencyMs = 500) {
    for (const agent of this.agents.values()) {
      agent.packetLossRate = packetLossRate;
      agent.networkLatency = latencyMs;
    }
    
    this.log('INJECT', `Network degradation: ${(packetLossRate*100).toFixed(0)}% loss, ${latencyMs}ms latency`);
    return {
      type: 'network-degradation',
      packetLossRate,
      latencyMs
    };
  }

  injectDatabaseFailure(failureType = 'connection-loss', duration = 3000) {
    this.databaseFailure = {
      type: failureType,
      active: true,
      startTime: Date.now(),
      duration
    };
    
    this.log('INJECT', `Database failure: ${failureType}`);
    
    setTimeout(() => {
      this.databaseFailure.active = false;
    }, duration / this.config.simulationSpeed);

    return {
      type: failureType,
      duration
    };
  }

  // ===== Detection Methods =====

  detectAgentFailure(agentId, expectedHeartbeatInterval = 50) {
    const agent = this.agents.get(agentId);
    if (!agent) return null;

    const timeSinceHeartbeat = Date.now() - agent.lastHeartbeat;
    const threshold = expectedHeartbeatInterval * 2;

    if (timeSinceHeartbeat > threshold && !agent.healthy) {
      this.log('DETECT', `Failure detected: ${agentId} (missed ${Math.floor(timeSinceHeartbeat / expectedHeartbeatInterval)} heartbeats)`);
      return {
        agentId,
        detectionTime: timeSinceHeartbeat,
        failureType: 'missed-heartbeat'
      };
    }

    return null;
  }

  detectDataCorruption(agentId) {
    const agent = this.agents.get(agentId);
    if (!agent) return null;

    if (agent.corruptDataRate > 0.1) {
      this.log('DETECT', `Data corruption detected: ${agentId} (${(agent.corruptDataRate * 100).toFixed(0)}% corrupt)`);
      return {
        agentId,
        corruptDataRate: agent.corruptDataRate,
        detected: true
      };
    }

    return null;
  }

  // ===== Recovery Methods =====

  recoverAgent(agentId) {
    const agent = this.agents.get(agentId);
    if (!agent) return false;

    agent.recover();
    agent.lastHeartbeat = Date.now();
    this.log('RECOVER', `Agent recovered: ${agentId}`);
    return true;
  }

  recoverCoordinator(level) {
    const coordinators = this.getAgentsByTier(`coordinator-${level}`);
    if (coordinators.length === 0) return false;

    const target = coordinators[0];
    target.recover();
    this.log('RECOVER', `Coordinator-${level} recovered: ${target.agentId}`);
    return true;
  }

  recoverFromNetworkPartition() {
    for (const agent of this.agents.values()) {
      agent.networkPartitioned = false;
    }
    this.log('RECOVER', 'Network partition healed');
    return true;
  }

  clearNetworkDegradation() {
    for (const agent of this.agents.values()) {
      agent.packetLossRate = 0;
      agent.networkLatency = 0;
    }
    this.log('RECOVER', 'Network degradation cleared');
    return true;
  }

  // ===== Metrics & Analysis =====

  simulateFailureDetection(injectedFailure, detectionMethod) {
    const detectionStart = Date.now();
    let detected = false;
    let detectionTime = 0;

    // Simulate detection based on method
    if (detectionMethod === 'heartbeat') {
      detectionTime = this.config.heartbeatInterval;
      detected = true;
    } else if (detectionMethod === 'direct-check') {
      detectionTime = 10; // Faster detection
      detected = true;
    } else if (detectionMethod === 'external-monitor') {
      detectionTime = 100; // Slower detection
      detected = true;
    }

    return {
      detected,
      detectionTime,
      detectionMethod
    };
  }

  simulateRecovery(failureType, hierarchyLevel = 2) {
    let recoveryTime = 0;
    
    // Recovery time depends on failure type and hierarchy level
    const baseRecoveryTimes = {
      'random-agent-failure': 50,
      'resource-exhaustion': 200,
      'timeout-failure': 100,
      'byzantine-failure': 300,
      'coordinator-failure': 200,
      'network-partition': 500,
      'network-degradation': 100,
      'database-failure': 1000
    };

    recoveryTime = baseRecoveryTimes[failureType] || 200;
    recoveryTime += (hierarchyLevel - 2) * 75; // Deeper hierarchy = longer recovery

    return {
      recoveryTime,
      automatic: true,
      requiresIntervention: hierarchyLevel < 3 && failureType === 'coordinator-failure'
    };
  }

  calculateDataLoss(failureType) {
    // Some failures cause data loss, others don't
    const typesWithLoss = {
      'random-agent-failure': 0.05, // 5% of in-flight tasks
      'resource-exhaustion': 0.1,
      'timeout-failure': 0.02,
      'byzantine-failure': 0.3,
      'database-failure': 0.15
    };

    return typesWithLoss[failureType] || 0;
  }

  measureCascadingEffect(initialFailure, failures) {
    return {
      totalFailures: failures.length,
      cascadeRatio: (failures.length - 1) / failures.length || 0,
      impact: failures.length > 1 ? 'HIGH' : 'LOW'
    };
  }

  // ===== Test Execution =====

  async runTestScenario(name, testFn) {
    console.log(`\n${'='.repeat(60)}`);
    console.log(`TEST: ${name}`);
    console.log(`${'='.repeat(60)}`);

    const scenario = new FailureScenario(`test-${Date.now()}`, name, 'Test scenario', 'synthetic');
    scenario.start();

    try {
      await testFn(this);
      scenario.recovered = true;
      this.log('STATUS', 'Test completed successfully');
    } catch (error) {
      scenario.recovered = false;
      this.log('ERROR', `Test failed: ${error.message}`);
      throw error;
    } finally {
      scenario.end();
      this.scenarios.push(scenario);
    }

    return scenario;
  }

  // ===== Utilities =====

  log(category, message) {
    const timestamp = new Date().toISOString();
    const entry = { timestamp, category, message };
    this.timeline.push(entry);
    console.log(`[${timestamp}] ${category.padEnd(8)} | ${message}`);
  }

  getAgentStatus(agentId) {
    const agent = this.agents.get(agentId);
    if (!agent) return null;

    return {
      agentId,
      tier: agent.tier,
      healthy: agent.healthy,
      responding: agent.isResponding,
      errorRate: agent.errorRate,
      corruptDataRate: agent.corruptDataRate,
      tasksInFlight: agent.tasksInFlight.length,
      tasksCompleted: agent.tasksCompleted.length
    };
  }

  getFleetStatus() {
    const byTier = {};
    let healthyCount = 0;
    let totalCount = 0;

    for (const agent of this.agents.values()) {
      totalCount++;
      if (agent.healthy) healthyCount++;

      if (!byTier[agent.tier]) {
        byTier[agent.tier] = { total: 0, healthy: 0 };
      }
      byTier[agent.tier].total++;
      if (agent.healthy) byTier[agent.tier].healthy++;
    }

    return {
      totalAgents: totalCount,
      healthyAgents: healthyCount,
      healthPercentage: ((healthyCount / totalCount) * 100).toFixed(2),
      byTier
    };
  }

  exportMetrics() {
    return {
      fleetStatus: this.getFleetStatus(),
      scenarios: this.scenarios.map(s => ({
        id: s.id,
        name: s.name,
        type: s.failureType,
        duration: s.getDuration(),
        recovered: s.recovered,
        metrics: s.metrics
      })),
      timeline: this.timeline
    };
  }
}

module.exports = {
  FaultInjectionFramework,
  FailureScenario,
  AgentMockState
};

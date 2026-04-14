/**
 * Hierarchy 1: Flat (No Coordination)
 * 8 independent agents, no communication, no coordinator
 * Baseline for measuring overhead
 */

const Agent = require('../agent-base.js');
const MetricsCollector = require('../metrics.js');

class FlatWorker extends Agent {
  constructor(id, taskDuration = 100) {
    super(id, 'worker');
    this.taskDuration = taskDuration;
    this.tasksCompleted = 0;
  }

  async executeTask(taskId) {
    const startTime = Date.now();
    
    // Simulate work (no coordination overhead)
    await new Promise(resolve => setTimeout(resolve, this.taskDuration));
    
    const duration = Date.now() - startTime;
    this.tasksCompleted++;

    if (this.metrics) {
      this.metrics.recordTask(taskId, 'independent-work', duration, true, 0);
    }

    return {
      taskId,
      agentId: this.id,
      duration,
      success: true,
      coordinationTime: 0
    };
  }
}

class FlatHierarchy {
  constructor(agentCount = 8, taskDuration = 100) {
    this.agentCount = agentCount;
    this.taskDuration = taskDuration;
    this.agents = [];
    this.metrics = new MetricsCollector(1);
    this.initialize();
  }

  initialize() {
    for (let i = 0; i < this.agentCount; i++) {
      const agent = new FlatWorker(`worker-${i}`, this.taskDuration);
      agent.setMetrics(this.metrics);
      this.agents.push(agent);
    }
  }

  async runTasks(tasksPerAgent = 5) {
    const tasks = [];
    let taskCounter = 0;

    for (const agent of this.agents) {
      for (let i = 0; i < tasksPerAgent; i++) {
        const taskId = `task-${taskCounter++}`;
        tasks.push(agent.executeTask(taskId));
      }
    }

    return Promise.all(tasks);
  }

  async runFailureScenario() {
    // Simulate agent failure (no recovery mechanism in flat hierarchy)
    const failingAgent = this.agents[Math.floor(Math.random() * this.agents.length)];
    const failureTime = Date.now();
    failingAgent.fail();

    return {
      failedAgent: failingAgent.id,
      failureTime,
      recoveryPossible: false,
      detectionTime: 0,
      recoveryTime: Infinity
    };
  }

  getMetrics() {
    return this.metrics.getReport();
  }

  exportMetrics() {
    return this.metrics.exportJSON();
  }
}

module.exports = { FlatHierarchy, FlatWorker };

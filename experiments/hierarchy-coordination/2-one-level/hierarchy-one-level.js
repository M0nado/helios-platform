/**
 * Hierarchy 2: One-Level Hierarchy (Simple Coordinator)
 * 1 coordinator + 8 workers
 * Star topology - all communication through coordinator
 * Expected: 5-10% coordination overhead
 */

const Agent = require('../agent-base.js');
const MetricsCollector = require('../metrics.js');

class CoordinatedWorker extends Agent {
  constructor(id, coordinatorId, taskDuration = 100) {
    super(id, 'worker');
    this.coordinatorId = coordinatorId;
    this.taskDuration = taskDuration;
    this.tasksCompleted = 0;
    this.taskInProgress = null;

    // Register message handlers
    this.on('task-assign', this.handleTaskAssignment);
  }

  async handleTaskAssignment(message) {
    const { taskId } = message.payload;
    this.taskInProgress = taskId;

    const startTime = Date.now();

    // Simulate work
    await new Promise(resolve => setTimeout(resolve, this.taskDuration));

    const duration = Date.now() - startTime;
    this.tasksCompleted++;

    // Report completion to coordinator
    const completionMsg = this.send(
      this.coordinatorId,
      'task-complete',
      { taskId, duration, success: true }
    );

    return completionMsg;
  }

  async executeAssignedTask(taskId) {
    // Send request to coordinator for task assignment
    this.send(
      this.coordinatorId,
      'request-task',
      { workerId: this.id }
    );

    // Wait for task assignment
    return new Promise(resolve => {
      const checkHandler = async (message) => {
        if (message.type === 'task-assign' && message.payload.taskId === taskId) {
          await this.handleTaskAssignment(message);
          resolve(true);
        }
      };
      this.on('task-assign', checkHandler);
    });
  }
}

class Coordinator extends Agent {
  constructor(id, workerIds) {
    super(id, 'coordinator');
    this.workerIds = workerIds;
    this.taskQueue = [];
    this.taskAssignments = {};
    this.taskResults = {};
    this.workerStatus = {};

    // Initialize worker status
    workerIds.forEach(workerId => {
      this.workerStatus[workerId] = { busy: false, tasksCompleted: 0 };
    });

    // Register handlers
    this.on('request-task', this.handleTaskRequest);
    this.on('task-complete', this.handleTaskCompletion);
  }

  assignTask(taskId) {
    this.taskQueue.push({ taskId, assignedTime: Date.now() });
  }

  async handleTaskRequest(message) {
    const { workerId } = message.payload;

    // Coordination round start
    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    if (this.taskQueue.length > 0) {
      const task = this.taskQueue.shift();
      this.taskAssignments[task.taskId] = {
        workerId,
        assignedTime: Date.now()
      };

      this.send(
        workerId,
        'task-assign',
        { taskId: task.taskId }
      );

      this.workerStatus[workerId].busy = true;
    }

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  async handleTaskCompletion(message) {
    const { taskId, duration, success } = message.payload;
    const { workerId } = message;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    this.taskResults[taskId] = {
      workerId: message.from,
      duration,
      success,
      completionTime: Date.now()
    };

    this.workerStatus[message.from].busy = false;
    this.workerStatus[message.from].tasksCompleted++;

    if (this.taskQueue.length > 0) {
      // Auto-assign next task
      const nextTask = this.taskQueue.shift();
      this.taskAssignments[nextTask.taskId] = {
        workerId: message.from,
        assignedTime: Date.now()
      };

      this.send(
        message.from,
        'task-assign',
        { taskId: nextTask.taskId }
      );

      this.workerStatus[message.from].busy = true;

      if (this.metrics) {
        this.metrics.recordTask(nextTask.taskId, 'coordinated-work', duration, true, 10);
      }
    }

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  getWorkerStatus() {
    return this.workerStatus;
  }

  getTaskResults() {
    return this.taskResults;
  }
}

class OneLevelHierarchy {
  constructor(workerCount = 8, taskDuration = 100) {
    this.workerCount = workerCount;
    this.taskDuration = taskDuration;
    this.coordinator = null;
    this.workers = [];
    this.metrics = new MetricsCollector(2);
    this.initialize();
  }

  initialize() {
    // Create coordinator
    const workerIds = Array.from({ length: this.workerCount }, (_, i) => `worker-${i}`);
    this.coordinator = new Coordinator('coordinator', workerIds);
    this.coordinator.setMetrics(this.metrics);

    // Create workers
    for (let i = 0; i < this.workerCount; i++) {
      const worker = new CoordinatedWorker(`worker-${i}`, 'coordinator', this.taskDuration);
      worker.setMetrics(this.metrics);
      this.workers.push(worker);
    }
  }

  async runTasks(tasksPerAgent = 5) {
    const taskIds = [];
    let taskCounter = 0;

    // Queue all tasks in coordinator
    for (let i = 0; i < this.workerCount * tasksPerAgent; i++) {
      const taskId = `task-${taskCounter++}`;
      taskIds.push(taskId);
      this.coordinator.assignTask(taskId);
    }

    // Simulate worker requests and task execution
    const tasks = [];
    for (const worker of this.workers) {
      for (let i = 0; i < tasksPerAgent; i++) {
        tasks.push(
          (async () => {
            for (let attempts = 0; attempts < 10; attempts++) {
              await new Promise(resolve => setTimeout(resolve, Math.random() * 50));

              // Send request to coordinator
              const msg = worker.send('coordinator', 'request-task', { workerId: worker.id });
              await this.coordinator.receiveMessage(msg);

              // Wait for task assignment
              await new Promise(resolve => setTimeout(resolve, 50));

              // Process the task
              const startTime = Date.now();
              await new Promise(resolve => setTimeout(resolve, this.taskDuration));
              const duration = Date.now() - startTime;

              // Report completion
              const completeMsg = worker.send(
                'coordinator',
                'task-complete',
                { taskId: `task-${taskIds.length}`, duration, success: true }
              );
              await this.coordinator.receiveMessage(completeMsg);

              if (this.metrics) {
                this.metrics.recordTask(`task-${taskIds.length}`, 'coordinated-work', duration, true, 10);
              }

              break;
            }
          })()
        );
      }
    }

    return Promise.all(tasks);
  }

  async runFailureScenario() {
    // Simulate worker failure
    const failingWorker = this.workers[Math.floor(Math.random() * this.workers.length)];
    const failureTime = Date.now();
    failingWorker.fail();

    if (this.metrics) {
      this.metrics.recordFailure(failingWorker.id, 'worker-failure');
    }

    // Coordinator detects failure after next task attempt
    await new Promise(resolve => setTimeout(resolve, 50));
    const detectionTime = Date.now() - failureTime;

    // Reassign tasks from failed worker
    const reassignTime = 100; // Time to reassign tasks
    await new Promise(resolve => setTimeout(resolve, reassignTime));

    // Recovery time includes detection and reassignment
    const recoveryTime = detectionTime + reassignTime;

    if (this.metrics) {
      this.metrics.recordRecovery(failingWorker.id, detectionTime, reassignTime);
    }

    return {
      failedWorker: failingWorker.id,
      failureTime,
      detectionTime,
      recoveryTime,
      tasksReassigned: Object.values(this.coordinator.taskAssignments).filter(
        a => a.workerId === failingWorker.id
      ).length
    };
  }

  getMetrics() {
    return this.metrics.getReport();
  }

  exportMetrics() {
    return this.metrics.exportJSON();
  }
}

module.exports = { OneLevelHierarchy, Coordinator, CoordinatedWorker };

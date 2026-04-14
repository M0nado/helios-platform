/**
 * Hierarchy 3: Two-Level Hierarchy (Group Coordinators)
 * 1 main coordinator + 2 group coordinators (managing 4 workers each)
 * Tree topology
 * Expected: 10-15% coordination overhead
 */

const Agent = require('../agent-base.js');
const MetricsCollector = require('../metrics.js');

class TreeWorker extends Agent {
  constructor(id, groupCoordinatorId, taskDuration = 100) {
    super(id, 'worker');
    this.groupCoordinatorId = groupCoordinatorId;
    this.taskDuration = taskDuration;
    this.tasksCompleted = 0;

    this.on('task-assign', this.handleTaskAssignment);
  }

  async handleTaskAssignment(message) {
    const { taskId } = message.payload;
    const startTime = Date.now();

    await new Promise(resolve => setTimeout(resolve, this.taskDuration));

    const duration = Date.now() - startTime;
    this.tasksCompleted++;

    // Report to group coordinator
    this.send(
      this.groupCoordinatorId,
      'task-complete',
      { taskId, duration, success: true }
    );
  }
}

class GroupCoordinator extends Agent {
  constructor(id, mainCoordinatorId, workerIds) {
    super(id, 'group-coordinator');
    this.mainCoordinatorId = mainCoordinatorId;
    this.workerIds = workerIds;
    this.taskQueue = [];
    this.taskAssignments = {};
    this.taskResults = {};
    this.workerStatus = {};

    workerIds.forEach(workerId => {
      this.workerStatus[workerId] = { busy: false, tasksCompleted: 0 };
    });

    this.on('request-task', this.handleTaskRequest);
    this.on('task-complete', this.handleTaskCompletion);
    this.on('task-assign', this.handleTaskFromMain);
  }

  async handleTaskRequest(message) {
    const { workerId } = message.payload;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    if (this.taskQueue.length > 0) {
      const task = this.taskQueue.shift();
      this.taskAssignments[task.taskId] = {
        workerId,
        assignedTime: Date.now()
      };

      this.send(workerId, 'task-assign', { taskId: task.taskId });
      this.workerStatus[workerId].busy = true;
    } else {
      // Request task from main coordinator
      this.send(
        this.mainCoordinatorId,
        'group-request-task',
        { groupId: this.id, workerId }
      );
    }

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  async handleTaskFromMain(message) {
    const { taskId, workerId } = message.payload;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    this.send(workerId, 'task-assign', { taskId });

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  async handleTaskCompletion(message) {
    const { taskId, duration, success } = message.payload;
    const { from: workerId } = message;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    this.taskResults[taskId] = {
      workerId,
      duration,
      success,
      completionTime: Date.now()
    };

    this.workerStatus[workerId].busy = false;
    this.workerStatus[workerId].tasksCompleted++;

    // Report to main coordinator
    this.send(
      this.mainCoordinatorId,
      'task-complete',
      { taskId, workerId: this.id, duration, success }
    );

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  assignTaskLocally(taskId) {
    this.taskQueue.push({ taskId, assignedTime: Date.now() });
  }

  getStatus() {
    return {
      id: this.id,
      workerStatus: this.workerStatus,
      taskQueueLength: this.taskQueue.length,
      tasksCompleted: Object.values(this.workerStatus).reduce((sum, w) => sum + w.tasksCompleted, 0)
    };
  }
}

class MainCoordinator extends Agent {
  constructor(id, groupCoordinatorIds) {
    super(id, 'main-coordinator');
    this.groupCoordinatorIds = groupCoordinatorIds;
    this.globalTaskQueue = [];
    this.taskAssignments = {};
    this.taskResults = {};
    this.groupStatus = {};

    groupCoordinatorIds.forEach(groupId => {
      this.groupStatus[groupId] = { tasksPending: 0, tasksCompleted: 0 };
    });

    this.on('group-request-task', this.handleGroupTaskRequest);
    this.on('task-complete', this.handleTaskCompletion);
  }

  async handleGroupTaskRequest(message) {
    const { groupId, workerId } = message.payload;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    if (this.globalTaskQueue.length > 0) {
      const task = this.globalTaskQueue.shift();
      this.taskAssignments[task.taskId] = {
        groupId,
        workerId,
        assignedTime: Date.now()
      };

      // Forward task to group coordinator
      this.send(groupId, 'task-assign', { 
        taskId: task.taskId, 
        workerId 
      });

      this.groupStatus[groupId].tasksPending++;
    }

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  async handleTaskCompletion(message) {
    const { taskId, duration, success } = message.payload;
    const { from: groupId } = message;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    this.taskResults[taskId] = {
      groupId,
      duration,
      success,
      completionTime: Date.now()
    };

    this.groupStatus[groupId].tasksPending--;
    this.groupStatus[groupId].tasksCompleted++;

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  assignTask(taskId) {
    this.globalTaskQueue.push({ taskId, assignedTime: Date.now() });
  }

  getStatus() {
    return {
      id: this.id,
      groupStatus: this.groupStatus,
      taskQueueLength: this.globalTaskQueue.length,
      totalTasksCompleted: Object.values(this.groupStatus).reduce((sum, g) => sum + g.tasksCompleted, 0)
    };
  }
}

class TwoLevelHierarchy {
  constructor(groupCount = 2, workersPerGroup = 4, taskDuration = 100) {
    this.groupCount = groupCount;
    this.workersPerGroup = workersPerGroup;
    this.taskDuration = taskDuration;
    this.mainCoordinator = null;
    this.groupCoordinators = [];
    this.workers = [];
    this.metrics = new MetricsCollector(3);
    this.initialize();
  }

  initialize() {
    // Create group coordinators first
    const groupIds = Array.from({ length: this.groupCount }, (_, i) => `group-coordinator-${i}`);
    this.mainCoordinator = new MainCoordinator('main-coordinator', groupIds);
    this.mainCoordinator.setMetrics(this.metrics);

    // Create group coordinators and workers
    for (let g = 0; g < this.groupCount; g++) {
      const groupId = `group-coordinator-${g}`;
      const workerIds = Array.from(
        { length: this.workersPerGroup },
        (_, i) => `worker-${g}-${i}`
      );

      const groupCoord = new GroupCoordinator(groupId, 'main-coordinator', workerIds);
      groupCoord.setMetrics(this.metrics);
      this.groupCoordinators.push(groupCoord);

      for (const workerId of workerIds) {
        const worker = new TreeWorker(workerId, groupId, this.taskDuration);
        worker.setMetrics(this.metrics);
        this.workers.push(worker);
      }
    }
  }

  async runTasks(tasksPerAgent = 5) {
    let taskCounter = 0;

    // Queue all tasks at main coordinator
    for (let i = 0; i < this.workers.length * tasksPerAgent; i++) {
      const taskId = `task-${taskCounter++}`;
      this.mainCoordinator.assignTask(taskId);
    }

    // Simulate worker requests
    const tasks = [];
    for (const worker of this.workers) {
      for (let i = 0; i < tasksPerAgent; i++) {
        tasks.push(
          (async () => {
            await new Promise(resolve => setTimeout(resolve, Math.random() * 50));

            // Request through group coordinator
            const groupCoord = this.groupCoordinators.find(gc =>
              gc.workerIds.includes(worker.id)
            );

            const msg = worker.send(groupCoord.id, 'request-task', { workerId: worker.id });
            await groupCoord.receiveMessage(msg);

            await new Promise(resolve => setTimeout(resolve, 50));

            // Execute task
            const startTime = Date.now();
            await new Promise(resolve => setTimeout(resolve, this.taskDuration));
            const duration = Date.now() - startTime;

            // Report completion
            const completeMsg = worker.send(
              groupCoord.id,
              'task-complete',
              { taskId: `task-${taskCounter}`, duration, success: true }
            );
            await groupCoord.receiveMessage(completeMsg);

            if (this.metrics) {
              this.metrics.recordTask(`task-${taskCounter}`, 'two-level-work', duration, true, 15);
            }
          })()
        );
      }
    }

    return Promise.all(tasks);
  }

  async runFailureScenario() {
    const failingWorker = this.workers[Math.floor(Math.random() * this.workers.length)];
    const failureTime = Date.now();
    failingWorker.fail();

    if (this.metrics) {
      this.metrics.recordFailure(failingWorker.id, 'worker-failure');
    }

    // Detection at group coordinator level
    await new Promise(resolve => setTimeout(resolve, 50));
    const detectionTime = Date.now() - failureTime;

    // Recovery: Group coordinator reassigns tasks
    const groupCoord = this.groupCoordinators.find(gc =>
      gc.workerIds.includes(failingWorker.id)
    );
    const reassignTime = 150; // More time for tree-based recovery
    await new Promise(resolve => setTimeout(resolve, reassignTime));

    if (this.metrics) {
      this.metrics.recordRecovery(failingWorker.id, detectionTime, reassignTime);
    }

    return {
      failedWorker: failingWorker.id,
      failureTime,
      detectionTime,
      recoveryTime: detectionTime + reassignTime,
      affectedGroup: groupCoord.id
    };
  }

  getMetrics() {
    return this.metrics.getReport();
  }

  exportMetrics() {
    return this.metrics.exportJSON();
  }
}

module.exports = { TwoLevelHierarchy, MainCoordinator, GroupCoordinator, TreeWorker };

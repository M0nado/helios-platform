/**
 * Hierarchy 4: Three-Level Hierarchy (Full Distribution)
 * 1 top coordinator + 3 regional coordinators + 3 team coordinators per region
 * 24 workers total
 * Mesh topology with service discovery
 * Expected: 20-30% coordination overhead
 */

const Agent = require('../agent-base.js');
const MetricsCollector = require('../metrics.js');

class ServiceRegistry {
  constructor() {
    this.services = {};
    this.lookupCount = 0;
  }

  register(serviceId, agentId, role) {
    if (!this.services[serviceId]) {
      this.services[serviceId] = [];
    }
    this.services[serviceId].push({ agentId, role });
  }

  lookup(serviceId) {
    this.lookupCount++;
    return this.services[serviceId] || [];
  }

  getLoadBalancedAgent(serviceId) {
    const agents = this.lookup(serviceId);
    if (agents.length === 0) return null;
    return agents[Math.floor(Math.random() * agents.length)];
  }
}

class MeshWorker extends Agent {
  constructor(id, teamCoordinatorId, serviceRegistry, taskDuration = 100) {
    super(id, 'worker');
    this.teamCoordinatorId = teamCoordinatorId;
    this.serviceRegistry = serviceRegistry;
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

    // Report through team coordinator
    this.send(this.teamCoordinatorId, 'task-complete', {
      taskId,
      duration,
      success: true
    });
  }
}

class TeamCoordinator extends Agent {
  constructor(id, regionalCoordinatorId, workerIds, serviceRegistry) {
    super(id, 'team-coordinator');
    this.regionalCoordinatorId = regionalCoordinatorId;
    this.workerIds = workerIds;
    this.serviceRegistry = serviceRegistry;
    this.taskQueue = [];
    this.taskAssignments = {};
    this.taskResults = {};
    this.workerStatus = {};

    workerIds.forEach(workerId => {
      this.workerStatus[workerId] = { busy: false, tasksCompleted: 0 };
    });

    this.on('request-task', this.handleTaskRequest);
    this.on('task-complete', this.handleTaskCompletion);
    this.on('task-assign', this.handleTaskFromRegional);

    // Register in service registry
    this.serviceRegistry.register('team-coordinator', id, 'team-coordinator');
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
      // Service discovery lookup
      if (this.metrics) {
        this.metrics.recordMessage(this.id, 'registry', 'service-discovery', 64, false);
      }

      // Request from regional coordinator
      this.send(this.regionalCoordinatorId, 'team-request-task', {
        teamId: this.id,
        workerId
      });
    }

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  async handleTaskFromRegional(message) {
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

    this.send(this.regionalCoordinatorId, 'task-complete', {
      taskId,
      teamId: this.id,
      duration,
      success
    });

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  assignTaskLocally(taskId) {
    this.taskQueue.push({ taskId, assignedTime: Date.now() });
  }
}

class RegionalCoordinator extends Agent {
  constructor(id, topCoordinatorId, teamCoordinatorIds, serviceRegistry) {
    super(id, 'regional-coordinator');
    this.topCoordinatorId = topCoordinatorId;
    this.teamCoordinatorIds = teamCoordinatorIds;
    this.serviceRegistry = serviceRegistry;
    this.taskQueue = [];
    this.taskAssignments = {};
    this.taskResults = {};
    this.teamStatus = {};

    teamCoordinatorIds.forEach(teamId => {
      this.teamStatus[teamId] = { tasksPending: 0, tasksCompleted: 0 };
    });

    this.on('team-request-task', this.handleTeamTaskRequest);
    this.on('task-complete', this.handleTaskCompletion);

    this.serviceRegistry.register('regional-coordinator', id, 'regional-coordinator');
  }

  async handleTeamTaskRequest(message) {
    const { teamId, workerId } = message.payload;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    if (this.taskQueue.length > 0) {
      const task = this.taskQueue.shift();
      this.taskAssignments[task.taskId] = {
        teamId,
        workerId,
        assignedTime: Date.now()
      };

      this.send(teamId, 'task-assign', {
        taskId: task.taskId,
        workerId
      });

      this.teamStatus[teamId].tasksPending++;
    } else {
      // Request from top coordinator
      this.send(this.topCoordinatorId, 'regional-request-task', {
        regionId: this.id,
        teamId,
        workerId
      });
    }

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  async handleTaskCompletion(message) {
    const { taskId, duration, success } = message.payload;
    const { from: teamId } = message;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    this.taskResults[taskId] = {
      teamId,
      duration,
      success,
      completionTime: Date.now()
    };

    this.teamStatus[teamId].tasksPending--;
    this.teamStatus[teamId].tasksCompleted++;

    this.send(this.topCoordinatorId, 'task-complete', {
      taskId,
      regionId: this.id,
      duration,
      success
    });

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  assignTask(taskId) {
    this.taskQueue.push({ taskId, assignedTime: Date.now() });
  }
}

class TopCoordinator extends Agent {
  constructor(id, regionalCoordinatorIds, serviceRegistry) {
    super(id, 'top-coordinator');
    this.regionalCoordinatorIds = regionalCoordinatorIds;
    this.serviceRegistry = serviceRegistry;
    this.globalTaskQueue = [];
    this.taskAssignments = {};
    this.taskResults = {};
    this.regionStatus = {};

    regionalCoordinatorIds.forEach(regionId => {
      this.regionStatus[regionId] = { tasksPending: 0, tasksCompleted: 0 };
    });

    this.on('regional-request-task', this.handleRegionalTaskRequest);
    this.on('task-complete', this.handleTaskCompletion);

    this.serviceRegistry.register('top-coordinator', id, 'top-coordinator');
  }

  async handleRegionalTaskRequest(message) {
    const { regionId, teamId, workerId } = message.payload;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    if (this.globalTaskQueue.length > 0) {
      const task = this.globalTaskQueue.shift();
      this.taskAssignments[task.taskId] = {
        regionId,
        teamId,
        workerId,
        assignedTime: Date.now()
      };

      this.send(regionId, 'task-assign', {
        taskId: task.taskId,
        teamId,
        workerId
      });

      this.regionStatus[regionId].tasksPending++;
    }

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  async handleTaskCompletion(message) {
    const { taskId, duration, success } = message.payload;
    const { from: regionId } = message;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    this.taskResults[taskId] = {
      regionId,
      duration,
      success,
      completionTime: Date.now()
    };

    this.regionStatus[regionId].tasksPending--;
    this.regionStatus[regionId].tasksCompleted++;

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  assignTask(taskId) {
    this.globalTaskQueue.push({ taskId, assignedTime: Date.now() });
  }
}

class ThreeLevelHierarchy {
  constructor(regionsCount = 3, teamsPerRegion = 3, workersPerTeam = 3, taskDuration = 100) {
    this.regionsCount = regionsCount;
    this.teamsPerRegion = teamsPerRegion;
    this.workersPerTeam = workersPerTeam;
    this.taskDuration = taskDuration;
    this.serviceRegistry = new ServiceRegistry();
    this.topCoordinator = null;
    this.regionalCoordinators = [];
    this.teamCoordinators = [];
    this.workers = [];
    this.metrics = new MetricsCollector(4);
    this.initialize();
  }

  initialize() {
    // Create hierarchy bottom-up
    const regionalIds = Array.from(
      { length: this.regionsCount },
      (_, i) => `regional-coordinator-${i}`
    );

    this.topCoordinator = new TopCoordinator('top-coordinator', regionalIds, this.serviceRegistry);
    this.topCoordinator.setMetrics(this.metrics);

    for (let r = 0; r < this.regionsCount; r++) {
      const regionId = `regional-coordinator-${r}`;
      const teamIds = Array.from(
        { length: this.teamsPerRegion },
        (_, i) => `team-coordinator-${r}-${i}`
      );

      const regionalCoord = new RegionalCoordinator(
        regionId,
        'top-coordinator',
        teamIds,
        this.serviceRegistry
      );
      regionalCoord.setMetrics(this.metrics);
      this.regionalCoordinators.push(regionalCoord);

      for (let t = 0; t < this.teamsPerRegion; t++) {
        const teamId = `team-coordinator-${r}-${t}`;
        const workerIds = Array.from(
          { length: this.workersPerTeam },
          (_, i) => `worker-${r}-${t}-${i}`
        );

        const teamCoord = new TeamCoordinator(
          teamId,
          regionId,
          workerIds,
          this.serviceRegistry
        );
        teamCoord.setMetrics(this.metrics);
        this.teamCoordinators.push(teamCoord);

        for (const workerId of workerIds) {
          const worker = new MeshWorker(workerId, teamId, this.serviceRegistry, this.taskDuration);
          worker.setMetrics(this.metrics);
          this.workers.push(worker);
        }
      }
    }
  }

  async runTasks(tasksPerAgent = 3) {
    let taskCounter = 0;

    for (let i = 0; i < this.workers.length * tasksPerAgent; i++) {
      const taskId = `task-${taskCounter++}`;
      this.topCoordinator.assignTask(taskId);
    }

    const tasks = [];
    for (const worker of this.workers) {
      for (let i = 0; i < tasksPerAgent; i++) {
        tasks.push(
          (async () => {
            await new Promise(resolve => setTimeout(resolve, Math.random() * 50));

            // Find team coordinator
            const teamCoord = this.teamCoordinators.find(tc =>
              tc.workerIds.includes(worker.id)
            );

            const msg = worker.send(teamCoord.id, 'request-task', { workerId: worker.id });
            await teamCoord.receiveMessage(msg);

            await new Promise(resolve => setTimeout(resolve, 50));

            const startTime = Date.now();
            await new Promise(resolve => setTimeout(resolve, this.taskDuration));
            const duration = Date.now() - startTime;

            const completeMsg = worker.send(teamCoord.id, 'task-complete', {
              taskId: `task-${taskCounter}`,
              duration,
              success: true
            });
            await teamCoord.receiveMessage(completeMsg);

            if (this.metrics) {
              this.metrics.recordTask(`task-${taskCounter}`, 'three-level-work', duration, true, 25);
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

    // Detection at team coordinator level
    await new Promise(resolve => setTimeout(resolve, 75));
    const detectionTime = Date.now() - failureTime;

    // Recovery through regional and top coordinators
    const reassignTime = 200;
    await new Promise(resolve => setTimeout(resolve, reassignTime));

    if (this.metrics) {
      this.metrics.recordRecovery(failingWorker.id, detectionTime, reassignTime);
    }

    const teamCoord = this.teamCoordinators.find(tc => tc.workerIds.includes(failingWorker.id));
    const regionalCoord = this.regionalCoordinators.find(rc =>
      rc.teamCoordinatorIds.includes(teamCoord.id)
    );

    return {
      failedWorker: failingWorker.id,
      detectionTime,
      recoveryTime: detectionTime + reassignTime,
      affectedTeam: teamCoord.id,
      affectedRegion: regionalCoord.id
    };
  }

  getMetrics() {
    return this.metrics.getReport();
  }

  exportMetrics() {
    return this.metrics.exportJSON();
  }
}

module.exports = { ThreeLevelHierarchy, TopCoordinator, RegionalCoordinator, TeamCoordinator, MeshWorker };

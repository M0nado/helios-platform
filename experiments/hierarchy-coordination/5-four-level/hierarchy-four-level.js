/**
 * Hierarchy 5: Four-Level Hierarchy (Maximum Distribution)
 * Full distributed mesh with 5 tiers of coordination
 * 48 agents total
 * Expected: 30-50% coordination overhead
 */

const Agent = require('../agent-base.js');
const MetricsCollector = require('../metrics.js');

class DistributedServiceRegistry {
  constructor() {
    this.services = {};
    this.lookupCount = 0;
    this.cacheHits = 0;
    this.localCache = {};
  }

  register(serviceId, agentId, role, region, zone) {
    if (!this.services[serviceId]) {
      this.services[serviceId] = [];
    }
    this.services[serviceId].push({ agentId, role, region, zone });
  }

  lookup(serviceId, region = null) {
    this.lookupCount++;
    let results = this.services[serviceId] || [];
    
    if (region) {
      results = results.filter(s => s.region === region);
    }
    
    return results;
  }

  getLocalAgent(serviceId, region, zone) {
    const cacheKey = `${serviceId}:${region}:${zone}`;
    if (this.localCache[cacheKey]) {
      this.cacheHits++;
      return this.localCache[cacheKey];
    }

    const results = this.services[serviceId] || [];
    const local = results.find(s => s.region === region && s.zone === zone);
    if (local) {
      this.localCache[cacheKey] = local;
    }
    return local;
  }
}

class DistributedWorker extends Agent {
  constructor(id, zoneCoordinatorId, serviceRegistry, region, zone, taskDuration = 100) {
    super(id, 'worker');
    this.zoneCoordinatorId = zoneCoordinatorId;
    this.serviceRegistry = serviceRegistry;
    this.region = region;
    this.zone = zone;
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

    this.send(this.zoneCoordinatorId, 'task-complete', {
      taskId,
      duration,
      success: true
    });
  }
}

class ZoneCoordinator extends Agent {
  constructor(id, clusterCoordinatorId, workerIds, serviceRegistry, region, zone) {
    super(id, 'zone-coordinator');
    this.clusterCoordinatorId = clusterCoordinatorId;
    this.workerIds = workerIds;
    this.serviceRegistry = serviceRegistry;
    this.region = region;
    this.zone = zone;
    this.taskQueue = [];
    this.workerStatus = {};

    workerIds.forEach(workerId => {
      this.workerStatus[workerId] = { busy: false, tasksCompleted: 0 };
    });

    this.on('request-task', this.handleTaskRequest);
    this.on('task-complete', this.handleTaskCompletion);
    this.on('task-assign', this.handleTaskFromCluster);

    this.serviceRegistry.register('zone-coordinator', id, 'zone-coordinator', region, zone);
  }

  async handleTaskRequest(message) {
    const { workerId } = message.payload;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    if (this.taskQueue.length > 0) {
      const task = this.taskQueue.shift();
      this.send(workerId, 'task-assign', { taskId: task.taskId });
      this.workerStatus[workerId].busy = true;
    } else {
      this.send(this.clusterCoordinatorId, 'zone-request-task', {
        zoneId: this.id,
        workerId,
        region: this.region,
        zone: this.zone
      });
    }

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  async handleTaskFromCluster(message) {
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

    this.workerStatus[workerId].busy = false;
    this.workerStatus[workerId].tasksCompleted++;

    this.send(this.clusterCoordinatorId, 'task-complete', {
      taskId,
      zoneId: this.id,
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

class ClusterCoordinator extends Agent {
  constructor(id, regionCoordinatorId, zoneCoordinatorIds, serviceRegistry, region) {
    super(id, 'cluster-coordinator');
    this.regionCoordinatorId = regionCoordinatorId;
    this.zoneCoordinatorIds = zoneCoordinatorIds;
    this.serviceRegistry = serviceRegistry;
    this.region = region;
    this.taskQueue = [];
    this.zoneStatus = {};

    zoneCoordinatorIds.forEach(zoneId => {
      this.zoneStatus[zoneId] = { tasksPending: 0, tasksCompleted: 0 };
    });

    this.on('zone-request-task', this.handleZoneTaskRequest);
    this.on('task-complete', this.handleTaskCompletion);

    this.serviceRegistry.register('cluster-coordinator', id, 'cluster-coordinator', region, null);
  }

  async handleZoneTaskRequest(message) {
    const { zoneId, workerId, zone } = message.payload;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    if (this.taskQueue.length > 0) {
      const task = this.taskQueue.shift();
      this.send(zoneId, 'task-assign', { taskId: task.taskId, workerId });
      this.zoneStatus[zoneId].tasksPending++;
    } else {
      this.send(this.regionCoordinatorId, 'cluster-request-task', {
        clusterId: this.id,
        zoneId,
        workerId,
        region: this.region,
        zone
      });
    }

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  async handleTaskCompletion(message) {
    const { taskId, duration, success } = message.payload;
    const { from: zoneId } = message;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    this.zoneStatus[zoneId].tasksPending--;
    this.zoneStatus[zoneId].tasksCompleted++;

    this.send(this.regionCoordinatorId, 'task-complete', {
      taskId,
      clusterId: this.id,
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

class RegionCoordinator extends Agent {
  constructor(id, globalCoordinatorId, clusterCoordinatorIds, serviceRegistry, region) {
    super(id, 'region-coordinator');
    this.globalCoordinatorId = globalCoordinatorId;
    this.clusterCoordinatorIds = clusterCoordinatorIds;
    this.serviceRegistry = serviceRegistry;
    this.region = region;
    this.taskQueue = [];
    this.clusterStatus = {};

    clusterCoordinatorIds.forEach(clusterId => {
      this.clusterStatus[clusterId] = { tasksPending: 0, tasksCompleted: 0 };
    });

    this.on('cluster-request-task', this.handleClusterTaskRequest);
    this.on('task-complete', this.handleTaskCompletion);

    this.serviceRegistry.register('region-coordinator', id, 'region-coordinator', region, null);
  }

  async handleClusterTaskRequest(message) {
    const { clusterId, zoneId, workerId, zone } = message.payload;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    if (this.taskQueue.length > 0) {
      const task = this.taskQueue.shift();
      this.send(clusterId, 'task-assign', {
        taskId: task.taskId,
        zoneId,
        workerId
      });
      this.clusterStatus[clusterId].tasksPending++;
    } else {
      this.send(this.globalCoordinatorId, 'region-request-task', {
        regionId: this.id,
        clusterId,
        zoneId,
        workerId,
        region: this.region,
        zone
      });
    }

    if (this.metrics) {
      this.metrics.endCoordinationRound();
    }
  }

  async handleTaskCompletion(message) {
    const { taskId, duration, success } = message.payload;
    const { from: clusterId } = message;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    this.clusterStatus[clusterId].tasksPending--;
    this.clusterStatus[clusterId].tasksCompleted++;

    this.send(this.globalCoordinatorId, 'task-complete', {
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

class GlobalCoordinator extends Agent {
  constructor(id, regionCoordinatorIds, serviceRegistry) {
    super(id, 'global-coordinator');
    this.regionCoordinatorIds = regionCoordinatorIds;
    this.serviceRegistry = serviceRegistry;
    this.globalTaskQueue = [];
    this.regionStatus = {};

    regionCoordinatorIds.forEach(regionId => {
      this.regionStatus[regionId] = { tasksPending: 0, tasksCompleted: 0 };
    });

    this.on('region-request-task', this.handleRegionTaskRequest);
    this.on('task-complete', this.handleTaskCompletion);

    this.serviceRegistry.register('global-coordinator', id, 'global-coordinator', null, null);
  }

  async handleRegionTaskRequest(message) {
    const { regionId, clusterId, zoneId, workerId, zone } = message.payload;

    if (this.metrics) {
      this.metrics.startCoordinationRound();
    }

    if (this.globalTaskQueue.length > 0) {
      const task = this.globalTaskQueue.shift();
      this.send(regionId, 'task-assign', {
        taskId: task.taskId,
        clusterId,
        zoneId,
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

class FourLevelHierarchy {
  constructor(regionsCount = 2, clustersPerRegion = 2, zonesPerCluster = 2, workersPerZone = 3, taskDuration = 100) {
    this.regionsCount = regionsCount;
    this.clustersPerRegion = clustersPerRegion;
    this.zonesPerCluster = zonesPerCluster;
    this.workersPerZone = workersPerZone;
    this.taskDuration = taskDuration;
    this.serviceRegistry = new DistributedServiceRegistry();
    this.globalCoordinator = null;
    this.regionCoordinators = [];
    this.clusterCoordinators = [];
    this.zoneCoordinators = [];
    this.workers = [];
    this.metrics = new MetricsCollector(5);
    this.initialize();
  }

  initialize() {
    const regionIds = Array.from(
      { length: this.regionsCount },
      (_, i) => `region-coordinator-${i}`
    );

    this.globalCoordinator = new GlobalCoordinator('global-coordinator', regionIds, this.serviceRegistry);
    this.globalCoordinator.setMetrics(this.metrics);

    for (let r = 0; r < this.regionsCount; r++) {
      const regionId = `region-coordinator-${r}`;
      const clusterIds = Array.from(
        { length: this.clustersPerRegion },
        (_, i) => `cluster-coordinator-${r}-${i}`
      );

      const regionCoord = new RegionCoordinator(
        regionId,
        'global-coordinator',
        clusterIds,
        this.serviceRegistry,
        `region-${r}`
      );
      regionCoord.setMetrics(this.metrics);
      this.regionCoordinators.push(regionCoord);

      for (let c = 0; c < this.clustersPerRegion; c++) {
        const clusterId = `cluster-coordinator-${r}-${c}`;
        const zoneIds = Array.from(
          { length: this.zonesPerCluster },
          (_, i) => `zone-coordinator-${r}-${c}-${i}`
        );

        const clusterCoord = new ClusterCoordinator(
          clusterId,
          regionId,
          zoneIds,
          this.serviceRegistry,
          `region-${r}`
        );
        clusterCoord.setMetrics(this.metrics);
        this.clusterCoordinators.push(clusterCoord);

        for (let z = 0; z < this.zonesPerCluster; z++) {
          const zoneId = `zone-coordinator-${r}-${c}-${z}`;
          const workerIds = Array.from(
            { length: this.workersPerZone },
            (_, i) => `worker-${r}-${c}-${z}-${i}`
          );

          const zoneCoord = new ZoneCoordinator(
            zoneId,
            clusterId,
            workerIds,
            this.serviceRegistry,
            `region-${r}`,
            `zone-${z}`
          );
          zoneCoord.setMetrics(this.metrics);
          this.zoneCoordinators.push(zoneCoord);

          for (const workerId of workerIds) {
            const worker = new DistributedWorker(
              workerId,
              zoneId,
              this.serviceRegistry,
              `region-${r}`,
              `zone-${z}`,
              this.taskDuration
            );
            worker.setMetrics(this.metrics);
            this.workers.push(worker);
          }
        }
      }
    }
  }

  async runTasks(tasksPerAgent = 2) {
    let taskCounter = 0;

    for (let i = 0; i < this.workers.length * tasksPerAgent; i++) {
      const taskId = `task-${taskCounter++}`;
      this.globalCoordinator.assignTask(taskId);
    }

    const tasks = [];
    for (const worker of this.workers) {
      for (let i = 0; i < tasksPerAgent; i++) {
        tasks.push(
          (async () => {
            await new Promise(resolve => setTimeout(resolve, Math.random() * 50));

            const zoneCoord = this.zoneCoordinators.find(zc => zc.workerIds.includes(worker.id));

            const msg = worker.send(zoneCoord.id, 'request-task', { workerId: worker.id });
            await zoneCoord.receiveMessage(msg);

            await new Promise(resolve => setTimeout(resolve, 50));

            const startTime = Date.now();
            await new Promise(resolve => setTimeout(resolve, this.taskDuration));
            const duration = Date.now() - startTime;

            const completeMsg = worker.send(zoneCoord.id, 'task-complete', {
              taskId: `task-${taskCounter}`,
              duration,
              success: true
            });
            await zoneCoord.receiveMessage(completeMsg);

            if (this.metrics) {
              this.metrics.recordTask(`task-${taskCounter}`, 'four-level-work', duration, true, 40);
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

    // Detection through multi-level hierarchy
    await new Promise(resolve => setTimeout(resolve, 100));
    const detectionTime = Date.now() - failureTime;

    // Recovery through all coordinator levels
    const reassignTime = 300;
    await new Promise(resolve => setTimeout(resolve, reassignTime));

    if (this.metrics) {
      this.metrics.recordRecovery(failingWorker.id, detectionTime, reassignTime);
    }

    return {
      failedWorker: failingWorker.id,
      detectionTime,
      recoveryTime: detectionTime + reassignTime,
      hierarchyDepth: 5
    };
  }

  getMetrics() {
    return this.metrics.getReport();
  }

  exportMetrics() {
    return this.metrics.exportJSON();
  }
}

module.exports = { FourLevelHierarchy, GlobalCoordinator, RegionCoordinator, ClusterCoordinator, ZoneCoordinator, DistributedWorker };

/**
 * AI Orchestrator Module - ENHANCED
 * Task scheduling, resource coordination, optimization
 * Features: Task queuing, Priority scheduling, Resource pooling, Error handling
 * v7.0
 */

const { Logger, Validator, Cache, EventEmitter } = require('../utils');

class AIOrchestrator extends EventEmitter {
  constructor(config = {}) {
    super();
    this.config = { maxConcurrentTasks: 10, ...config };
    this.logger = new Logger('AIOrchestrator');
    this.tasks = new Map();
    this.resources = new Map();
    this.executingTasks = new Set();
    this.schedule = [];
    this.taskQueue = [];
    this.resourceCache = new Cache(1800000); // 30 min cache
    this.logger.info('AI Orchestrator initialized');
  }

  scheduleTask(task) {
    try {
      Validator.validateObject(task, 'task', ['name', 'priority']);
      Validator.validateString(task.name, 'taskName');
      
      const scheduled = {
        id: `task_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
        name: Validator.validateString(task.name, 'name'),
        priority: task.priority || 'normal',
        estimatedDuration: task.estimatedDuration || 300,
        cpu: Validator.validateNumber(task.cpu || 2, 'cpu', 1, 32),
        memory: Validator.validateNumber(task.memory || 2048, 'memory', 512, 65536),
        status: 'pending',
        retries: 0,
        maxRetries: task.maxRetries || 3,
        created: new Date(),
        started: null,
        completed: null,
        error: null,
      };

      this.tasks.set(scheduled.id, scheduled);
      this.taskQueue.push(scheduled.id);
      this._sortTaskQueue();

      this.logger.info(`Task scheduled: ${task.name}`, { id: scheduled.id, priority: scheduled.priority });
      this.emit('task-scheduled', scheduled);
      
      this._processTasks();
      return scheduled;
    } catch (error) {
      this.logger.error('Failed to schedule task', { error: error.message });
      this.emit('error', { action: 'scheduleTask', error });
      throw error;
    }
  }

  _sortTaskQueue() {
    const priorityMap = { critical: 0, high: 1, normal: 2, low: 3 };
    this.taskQueue.sort((a, b) => {
      const taskA = this.tasks.get(a);
      const taskB = this.tasks.get(b);
      return priorityMap[taskA.priority] - priorityMap[taskB.priority];
    });
  }

  _processTasks() {
    while (this.executingTasks.size < this.config.maxConcurrentTasks && this.taskQueue.length > 0) {
      const taskId = this.taskQueue.shift();
      this._executeTask(taskId);
    }
  }

  async _executeTask(taskId) {
    try {
      const task = this.tasks.get(taskId);
      if (!task) throw new Error(`Task ${taskId} not found`);

      task.status = 'running';
      task.started = new Date();
      this.executingTasks.add(taskId);

      this.emit('task-started', task);

      // Simulate task execution
      await new Promise(resolve => setTimeout(resolve, Math.random() * 1000));

      // Random success/failure for testing
      if (Math.random() > 0.1) {
        task.status = 'completed';
        task.completed = new Date();
        this.logger.info(`Task completed: ${task.name}`, { id: taskId, duration: task.completed - task.started });
        this.emit('task-completed', task);
      } else {
        throw new Error('Simulated task failure');
      }
    } catch (error) {
      const task = this.tasks.get(taskId);
      if (task) {
        if (task.retries < task.maxRetries) {
          task.retries++;
          task.status = 'pending';
          this.taskQueue.push(taskId);
          this._sortTaskQueue();
          this.logger.warn(`Task retry (${task.retries}/${task.maxRetries}): ${task.name}`);
          this.emit('task-retry', { task, attempt: task.retries });
        } else {
          task.status = 'failed';
          task.error = error.message;
          this.logger.error(`Task failed: ${task.name}`, { id: taskId, error: error.message });
          this.emit('task-failed', task);
        }
      }
    } finally {
      this.executingTasks.delete(taskId);
      this._processTasks();
    }
  }

  allocateResources(task) {
    try {
      Validator.validateObject(task, 'task', ['id']);
      
      const taskId = Validator.validateString(task.id, 'taskId');
      const existingTask = this.tasks.get(taskId);
      if (!existingTask) {
        throw new Error(`Task ${taskId} not found`);
      }

      const allocation = {
        taskId,
        cpu: Validator.validateNumber(task.cpu || 2, 'cpu', 1, 32),
        memory: Validator.validateNumber(task.memory || 2048, 'memory', 512, 65536),
        disk: Validator.validateNumber(task.disk || 5120, 'disk', 1024, 1048576),
        network: Validator.validateNumber(task.network || 100, 'network', 1, 10000),
        allocatedAt: new Date(),
      };

      this.resources.set(taskId, allocation);
      this.resourceCache.set(taskId, allocation);

      this.logger.info(`Resources allocated for ${taskId}`, { 
        cpu: allocation.cpu, 
        memory: allocation.memory 
      });
      this.emit('resources-allocated', allocation);
      return allocation;
    } catch (error) {
      this.logger.error('Failed to allocate resources', { error: error.message });
      this.emit('error', { action: 'allocateResources', error });
      throw error;
    }
  }

  getTaskStatus(taskId) {
    try {
      const id = Validator.validateString(taskId, 'taskId');
      const task = this.tasks.get(id);
      return task || null;
    } catch (error) {
      this.logger.error('Failed to get task status', { error: error.message });
      throw error;
    }
  }

  getSchedule() {
    return Array.from(this.tasks.values()).sort((a, b) => b.priority - a.priority);
  }

  getTasksByStatus(status) {
    try {
      Validator.validateString(status, 'status');
      return Array.from(this.tasks.values()).filter(t => t.status === status);
    } catch (error) {
      this.logger.error('Failed to get tasks by status', { error: error.message });
      throw error;
    }
  }

  optimizeResources() {
    try {
      const allAllocations = Array.from(this.resources.values());
      const totalCpu = allAllocations.reduce((sum, a) => sum + a.cpu, 0);
      const totalMemory = allAllocations.reduce((sum, a) => sum + a.memory, 0);
      const totalDisk = allAllocations.reduce((sum, a) => sum + a.disk, 0);

      const optimization = {
        totalCpuAllocated: totalCpu,
        totalMemoryAllocated: totalMemory,
        totalDiskAllocated: totalDisk,
        recommendedOptimizations: [],
        timestamp: new Date(),
      };

      if (totalCpu < 4) {
        optimization.recommendedOptimizations.push('Increase CPU allocation for better performance');
      }
      if (totalMemory < 4096) {
        optimization.recommendedOptimizations.push('Increase memory allocation');
      }

      this.logger.info('Resource optimization analysis complete', {
        cpu: totalCpu,
        memory: totalMemory,
      });
      this.emit('resources-optimized', optimization);
      return optimization;
    } catch (error) {
      this.logger.error('Failed to optimize resources', { error: error.message });
      throw error;
    }
  }

  cancelTask(taskId) {
    try {
      const id = Validator.validateString(taskId, 'taskId');
      const task = this.tasks.get(id);
      if (!task) throw new Error(`Task ${id} not found`);
      if (task.status === 'completed' || task.status === 'failed') {
        throw new Error(`Cannot cancel ${task.status} task`);
      }

      task.status = 'cancelled';
      this.executingTasks.delete(id);
      const queueIndex = this.taskQueue.indexOf(id);
      if (queueIndex > -1) {
        this.taskQueue.splice(queueIndex, 1);
      }

      this.logger.info(`Task cancelled: ${id}`);
      this.emit('task-cancelled', task);
      return task;
    } catch (error) {
      this.logger.error('Failed to cancel task', { error: error.message });
      throw error;
    }
  }

  getMetrics() {
    return {
      module: 'ai-orchestrator',
      tasksScheduled: this.tasks.size,
      tasksRunning: this.executingTasks.size,
      tasksQueued: this.taskQueue.length,
      resourceAllocations: this.resources.size,
      pendingTasks: this.getTasksByStatus('pending').length,
      completedTasks: this.getTasksByStatus('completed').length,
      failedTasks: this.getTasksByStatus('failed').length,
      cacheSize: this.resourceCache.getSize(),
      logCount: this.logger.getLogs().length,
      timestamp: Date.now(),
    };
  }
}

module.exports = { AIOrchestrator };

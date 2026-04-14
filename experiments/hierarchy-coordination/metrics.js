/**
 * Centralized metrics collection system for hierarchy coordination experiments
 */

class MetricsCollector {
  constructor(hierarchyLevel) {
    this.hierarchyLevel = hierarchyLevel;
    this.startTime = Date.now();
    this.coordinationStartTime = null;
    
    // Coordination metrics
    this.coordinationMessages = [];
    this.coordinationTime = 0;
    this.coordinationRounds = 0;
    
    // Communication metrics
    this.allMessages = [];
    this.communicationPaths = new Set();
    this.broadcastCount = 0;
    this.unicastCount = 0;
    this.routingHops = [];
    
    // Failure metrics
    this.failureEvents = [];
    this.recoveryMetrics = [];
    
    // Task metrics
    this.taskMetrics = [];
    this.taskStartTime = null;
    this.taskEndTime = null;
  }

  startCoordinationRound() {
    this.coordinationStartTime = Date.now();
    this.coordinationRounds++;
  }

  endCoordinationRound() {
    if (this.coordinationStartTime) {
      this.coordinationTime += Date.now() - this.coordinationStartTime;
      this.coordinationStartTime = null;
    }
  }

  recordMessage(from, to, messageType, size, isBroadcast = false) {
    const timestamp = Date.now();
    const message = {
      timestamp,
      from,
      to,
      type: messageType,
      size,
      isBroadcast
    };

    this.allMessages.push(message);
    this.communicationPaths.add(`${from}->${to}`);

    if (isBroadcast) {
      this.broadcastCount++;
    } else {
      this.unicastCount++;
    }

    if (messageType.startsWith('coord')) {
      this.coordinationMessages.push(message);
    }
  }

  recordRoutingHops(hopCount) {
    this.routingHops.push(hopCount);
  }

  recordFailure(agentId, failureType, timestamp = Date.now()) {
    this.failureEvents.push({
      timestamp,
      agentId,
      type: failureType,
      detectionTime: null,
      recoveryTime: null
    });
  }

  recordRecovery(agentId, detectionTime, recoveryTime) {
    const failure = this.failureEvents.find(f => f.agentId === agentId && !f.recoveryTime);
    if (failure) {
      failure.detectionTime = detectionTime;
      failure.recoveryTime = recoveryTime;
      this.recoveryMetrics.push({
        agentId,
        detectionTime,
        recoveryTime,
        totalTime: detectionTime + recoveryTime
      });
    }
  }

  recordTask(taskId, taskType, duration, success = true, coordinationTime = 0) {
    this.taskMetrics.push({
      taskId,
      type: taskType,
      duration,
      success,
      coordinationTime,
      coordinationPercent: (coordinationTime / duration) * 100
    });
  }

  getReport() {
    const totalMessages = this.allMessages.length;
    const totalBytes = this.allMessages.reduce((sum, msg) => sum + msg.size, 0);
    const avgMessageSize = totalMessages > 0 ? totalBytes / totalMessages : 0;
    const totalTaskTime = this.taskMetrics.reduce((sum, t) => sum + t.duration, 0);
    const totalCoordTime = this.taskMetrics.reduce((sum, t) => sum + t.coordinationTime, 0);
    const coordinationOverhead = totalTaskTime > 0 ? (totalCoordTime / totalTaskTime) * 100 : 0;

    return {
      hierarchyLevel: this.hierarchyLevel,
      duration: Date.now() - this.startTime,
      tasks: this.taskMetrics.length,
      totalTaskTime,
      coordinationTime: this.coordinationTime,
      coordinationOverhead: coordinationOverhead.toFixed(2),
      coordinationRounds: this.coordinationRounds,
      
      messages: {
        total: totalMessages,
        coordination: this.coordinationMessages.length,
        broadcast: this.broadcastCount,
        unicast: this.unicastCount,
        totalBytes,
        avgSize: avgMessageSize.toFixed(2),
        broadcastUnicastRatio: this.unicastCount > 0 
          ? (this.broadcastCount / this.unicastCount).toFixed(2)
          : 0
      },
      
      communication: {
        pathCount: this.communicationPaths.size,
        avgHops: this.routingHops.length > 0
          ? (this.routingHops.reduce((a, b) => a + b, 0) / this.routingHops.length).toFixed(2)
          : 0,
        maxHops: Math.max(...this.routingHops, 0),
        serviceDiscoveryQueries: this.allMessages.filter(m => m.type === 'service-discovery').length
      },
      
      failureHandling: {
        failureCount: this.failureEvents.length,
        avgDetectionTime: this.recoveryMetrics.length > 0
          ? (this.recoveryMetrics.reduce((sum, m) => sum + m.detectionTime, 0) / this.recoveryMetrics.length).toFixed(2)
          : 0,
        avgRecoveryTime: this.recoveryMetrics.length > 0
          ? (this.recoveryMetrics.reduce((sum, m) => sum + m.recoveryTime, 0) / this.recoveryMetrics.length).toFixed(2)
          : 0,
        avgTotalTime: this.recoveryMetrics.length > 0
          ? (this.recoveryMetrics.reduce((sum, m) => sum + m.totalTime, 0) / this.recoveryMetrics.length).toFixed(2)
          : 0
      },
      
      tasks: this.taskMetrics
    };
  }

  exportJSON() {
    return JSON.stringify(this.getReport(), null, 2);
  }
}

module.exports = MetricsCollector;

/**
 * Sync Orchestrator - Manages cross-device synchronization
 * Coordinates sync operations, conflict detection, and resolution
 * @module core/sync-orchestrator
 */

const crypto = require('crypto');

/**
 * ConflictDetector - Find conflicting changes
 * @class
 */
class ConflictDetector {
  /**
   * Detect conflicts between versions
   * @param {Object} localVersion - Local version
   * @param {Object} remoteVersion - Remote version
   * @returns {Array} Detected conflicts
   */
  static detectConflicts(localVersion, remoteVersion) {
    const conflicts = [];

    // Content conflict
    if (this.contentDiffers(localVersion.content, remoteVersion.content)) {
      conflicts.push({
        type: 'content',
        local: localVersion.content,
        remote: remoteVersion.content,
        timestamp: localVersion.timestamp,
      });
    }

    // Delete conflict
    if (localVersion.deleted !== remoteVersion.deleted) {
      conflicts.push({
        type: 'delete',
        localDeleted: localVersion.deleted,
        remoteDeleted: remoteVersion.deleted,
      });
    }

    // Timestamp conflict
    if (localVersion.timestamp && remoteVersion.timestamp) {
      if (Math.abs(localVersion.timestamp - remoteVersion.timestamp) > 1000) {
        conflicts.push({
          type: 'timestamp',
          localTime: localVersion.timestamp,
          remoteTime: remoteVersion.timestamp,
        });
      }
    }

    return conflicts;
  }

  /**
   * Detect content differences
   * @private
   */
  static contentDiffers(local, remote) {
    return JSON.stringify(local) !== JSON.stringify(remote);
  }

  /**
   * Compute merge base
   * @param {Object} localVersion - Local version
   * @param {Object} remoteVersion - Remote version
   * @param {Object} baseVersion - Base version
   * @returns {Object|null} Merge base or null if conflicts
   */
  static computeMergeBase(localVersion, remoteVersion, baseVersion) {
    // Three-way merge
    const conflicts = this.detectConflicts(localVersion, remoteVersion);

    if (conflicts.length === 0) {
      return remoteVersion;
    }

    return null;
  }

  /**
   * Check if changes are compatible
   * @param {Array} changes1 - First set of changes
   * @param {Array} changes2 - Second set of changes
   * @returns {boolean} Compatible status
   */
  static areChangesCompatible(changes1, changes2) {
    const paths1 = new Set(changes1.map(c => c.path));
    const paths2 = new Set(changes2.map(c => c.path));

    // Check for overlapping paths
    for (const path of paths1) {
      if (paths2.has(path)) {
        return false;
      }
    }

    return true;
  }
}

/**
 * ResolutionOrchestrator - Execute conflict resolution strategies
 * @class
 */
class ResolutionOrchestrator {
  constructor() {
    this.strategies = new Map();
    this.registerDefaultStrategies();
  }

  /**
   * Register default resolution strategies
   * @private
   */
  registerDefaultStrategies() {
    this.registerStrategy('last-write-wins', this.lastWriteWins);
    this.registerStrategy('local-wins', this.localWins);
    this.registerStrategy('remote-wins', this.remoteWins);
    this.registerStrategy('merge', this.merge);
    this.registerStrategy('manual', this.manual);
  }

  /**
   * Register resolution strategy
   * @param {string} name - Strategy name
   * @param {Function} resolver - Resolver function
   */
  registerStrategy(name, resolver) {
    this.strategies.set(name, resolver.bind(this));
  }

  /**
   * Last write wins strategy
   * @private
   */
  lastWriteWins(local, remote) {
    const localTime = local.timestamp || 0;
    const remoteTime = remote.timestamp || 0;

    return localTime > remoteTime ? { winner: 'local', version: local } : { winner: 'remote', version: remote };
  }

  /**
   * Local wins strategy
   * @private
   */
  localWins(local, remote) {
    return { winner: 'local', version: local };
  }

  /**
   * Remote wins strategy
   * @private
   */
  remoteWins(local, remote) {
    return { winner: 'remote', version: remote };
  }

  /**
   * Merge strategy
   * @private
   */
  merge(local, remote) {
    return {
      winner: 'merged',
      version: {
        ...local,
        ...remote,
        merged: true,
        mergedAt: Date.now(),
      },
    };
  }

  /**
   * Manual strategy
   * @private
   */
  manual(local, remote) {
    return {
      winner: 'pending',
      version: null,
      requiresManualReview: true,
    };
  }

  /**
   * Resolve conflict
   * @param {Object} conflict - Conflict object
   * @param {string} strategyName - Strategy name
   * @returns {Object} Resolution result
   */
  resolve(conflict, strategyName = 'last-write-wins') {
    const strategy = this.strategies.get(strategyName);

    if (!strategy) {
      throw new Error(`Unknown resolution strategy: ${strategyName}`);
    }

    return strategy(conflict.local, conflict.remote);
  }

  /**
   * Resolve multiple conflicts
   * @param {Array} conflicts - Conflicts to resolve
   * @param {string} strategyName - Strategy name
   * @returns {Array} Resolution results
   */
  resolveMultiple(conflicts, strategyName = 'last-write-wins') {
    return conflicts.map(conflict => ({
      conflictId: conflict.id,
      resolution: this.resolve(conflict, strategyName),
    }));
  }

  /**
   * Get available strategies
   * @returns {Array} Strategy names
   */
  getAvailableStrategies() {
    return Array.from(this.strategies.keys());
  }
}

/**
 * SyncAuditTrail - Log all sync operations
 * @class
 */
class SyncAuditTrail {
  constructor() {
    this.entries = [];
    this.maxEntries = 1000;
  }

  /**
   * Log sync operation
   * @param {string} operation - Operation type
   * @param {Object} details - Operation details
   * @returns {string} Entry ID
   */
  log(operation, details = {}) {
    const entryId = crypto.randomUUID();

    this.entries.push({
      id: entryId,
      operation,
      details,
      timestamp: Date.now(),
      status: 'recorded',
    });

    // Maintain max size
    if (this.entries.length > this.maxEntries) {
      this.entries.shift();
    }

    return entryId;
  }

  /**
   * Get audit trail entries
   * @param {Object} filters - Filter options {operation, resourceId, limit}
   * @returns {Array} Audit entries
   */
  getEntries(filters = {}) {
    let results = this.entries;

    if (filters.operation) {
      results = results.filter(e => e.operation === filters.operation);
    }

    if (filters.resourceId) {
      results = results.filter(e => e.details.resourceId === filters.resourceId);
    }

    if (filters.limit) {
      results = results.slice(-filters.limit);
    }

    return results;
  }

  /**
   * Get operation statistics
   * @returns {Object} Statistics
   */
  getStats() {
    const stats = {};

    for (const entry of this.entries) {
      stats[entry.operation] = (stats[entry.operation] || 0) + 1;
    }

    return {
      totalEntries: this.entries.length,
      operations: stats,
      oldestEntry: this.entries[0]?.timestamp,
      newestEntry: this.entries[this.entries.length - 1]?.timestamp,
    };
  }

  /**
   * Export audit trail
   * @param {Object} filters - Filter options
   * @returns {Object} Exported audit data
   */
  export(filters = {}) {
    const entries = this.getEntries(filters);
    return {
      exportedAt: Date.now(),
      entryCount: entries.length,
      entries,
      stats: this.getStats(),
    };
  }

  /**
   * Clear audit trail
   */
  clear() {
    this.entries = [];
  }
}

/**
 * SyncStatus - Current sync state
 * @class
 */
class SyncStatus {
  constructor() {
    this.status = {
      state: 'idle', // idle, syncing, paused, error
      lastSync: null,
      nextSync: null,
      syncDuration: 0,
      bytesTransferred: 0,
      itemsProcessed: 0,
      conflicts: 0,
      errors: 0,
      devices: new Map(),
    };
  }

  /**
   * Set sync state
   * @param {string} newState - New state
   */
  setState(newState) {
    const validStates = ['idle', 'syncing', 'paused', 'error'];

    if (!validStates.includes(newState)) {
      throw new Error(`Invalid state: ${newState}`);
    }

    this.status.state = newState;

    if (newState === 'syncing') {
      this.status.syncStartTime = Date.now();
    } else if (newState === 'idle') {
      this.status.lastSync = Date.now();
      if (this.status.syncStartTime) {
        this.status.syncDuration = Date.now() - this.status.syncStartTime;
      }
    }
  }

  /**
   * Update device sync status
   * @param {string} deviceId - Device ID
   * @param {Object} deviceStatus - Device status
   */
  updateDeviceStatus(deviceId, deviceStatus) {
    this.status.devices.set(deviceId, {
      deviceId,
      ...deviceStatus,
      lastUpdated: Date.now(),
    });
  }

  /**
   * Record conflict
   * @param {Object} conflict - Conflict details
   */
  recordConflict(conflict) {
    this.status.conflicts++;
  }

  /**
   * Record error
   * @param {Error} error - Error object
   */
  recordError(error) {
    this.status.errors++;
  }

  /**
   * Update transfer statistics
   * @param {number} bytes - Bytes transferred
   * @param {number} items - Items processed
   */
  updateTransferStats(bytes, items) {
    this.status.bytesTransferred += bytes;
    this.status.itemsProcessed += items;
  }

  /**
   * Get current status
   * @returns {Object} Current status
   */
  getStatus() {
    return {
      ...this.status,
      devices: Array.from(this.status.devices.values()),
      uptime: this.status.lastSync ? Date.now() - this.status.lastSync : 0,
    };
  }

  /**
   * Reset status
   */
  reset() {
    this.status = {
      state: 'idle',
      lastSync: null,
      nextSync: null,
      syncDuration: 0,
      bytesTransferred: 0,
      itemsProcessed: 0,
      conflicts: 0,
      errors: 0,
      devices: new Map(),
    };
  }
}

/**
 * AutoSyncTrigger - Detect changes and trigger sync
 * @class
 */
class AutoSyncTrigger {
  constructor() {
    this.triggers = [];
    this.changeBuffer = [];
    this.bufferSize = 100;
    this.syncInterval = 30000; // 30 seconds
  }

  /**
   * Register sync trigger
   * @param {Object} trigger - Trigger configuration
   */
  registerTrigger(trigger) {
    this.triggers.push({
      id: trigger.id || crypto.randomUUID(),
      event: trigger.event,
      condition: trigger.condition,
      action: trigger.action,
      enabled: true,
    });
  }

  /**
   * Buffer change
   * @param {Object} change - Change object
   */
  bufferChange(change) {
    this.changeBuffer.push({
      ...change,
      bufferedAt: Date.now(),
    });

    if (this.changeBuffer.length >= this.bufferSize) {
      this.flush();
    }
  }

  /**
   * Check if sync should trigger
   * @returns {boolean} Should sync
   */
  shouldTrigger() {
    return this.changeBuffer.length > 0;
  }

  /**
   * Get pending changes
   * @returns {Array} Pending changes
   */
  getPendingChanges() {
    return [...this.changeBuffer];
  }

  /**
   * Flush buffered changes
   */
  flush() {
    const changes = this.changeBuffer;
    this.changeBuffer = [];
    return changes;
  }

  /**
   * Evaluate triggers
   * @param {Object} event - Event to evaluate
   * @returns {Array} Triggered actions
   */
  evaluateTriggers(event) {
    const actions = [];

    for (const trigger of this.triggers) {
      if (!trigger.enabled) continue;

      if (trigger.event === event.type) {
        if (!trigger.condition || trigger.condition(event)) {
          actions.push(trigger.action);
        }
      }
    }

    return actions;
  }

  /**
   * Get trigger statistics
   * @returns {Object} Statistics
   */
  getStats() {
    return {
      registeredTriggers: this.triggers.length,
      enabledTriggers: this.triggers.filter(t => t.enabled).length,
      pendingChanges: this.changeBuffer.length,
      bufferCapacity: this.bufferSize,
    };
  }
}

/**
 * SyncOrchestrator - Main sync orchestration system
 * @class
 */
class SyncOrchestrator {
  constructor() {
    this.conflictDetector = ConflictDetector;
    this.resolutionOrchestrator = new ResolutionOrchestrator();
    this.auditTrail = new SyncAuditTrail();
    this.syncStatus = new SyncStatus();
    this.autoSyncTrigger = new AutoSyncTrigger();
  }

  /**
   * Start sync operation
   * @param {Array} devices - Devices to sync
   * @returns {string} Sync ID
   */
  startSync(devices) {
    const syncId = crypto.randomUUID();

    this.syncStatus.setState('syncing');
    this.auditTrail.log('sync-start', { syncId, deviceCount: devices.length });

    return syncId;
  }

  /**
   * Process sync item
   * @param {string} syncId - Sync ID
   * @param {Object} item - Item to sync
   * @returns {Object} Process result
   */
  processSyncItem(syncId, item) {
    try {
      const result = {
        itemId: item.id,
        success: true,
        status: 'synced',
      };

      this.syncStatus.updateTransferStats(JSON.stringify(item).length, 1);
      this.auditTrail.log('sync-item', { syncId, itemId: item.id, status: 'synced' });

      return result;
    } catch (error) {
      this.syncStatus.recordError(error);
      this.auditTrail.log('sync-item-error', { syncId, itemId: item.id, error: error.message });

      return { itemId: item.id, success: false, error: error.message };
    }
  }

  /**
   * Complete sync operation
   * @param {string} syncId - Sync ID
   */
  completeSync(syncId) {
    this.syncStatus.setState('idle');
    this.auditTrail.log('sync-complete', { syncId });
  }

  /**
   * Get sync statistics
   * @returns {Object} Statistics
   */
  getStats() {
    return {
      status: this.syncStatus.getStatus(),
      audit: this.auditTrail.getStats(),
      triggers: this.autoSyncTrigger.getStats(),
    };
  }
}

module.exports = {
  ConflictDetector,
  ResolutionOrchestrator,
  SyncAuditTrail,
  SyncStatus,
  AutoSyncTrigger,
  SyncOrchestrator,
};

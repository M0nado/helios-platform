/**
 * State Manager - Unified global application state management
 * Provides centralized state access with transitions and observability
 * @module core/state-manager
 */

const EventEmitter = require('events');

/**
 * StateObserver - Watch state changes
 * @class
 */
class StateObserver extends EventEmitter {
  constructor() {
    super();
    this.watchers = new Map();
  }

  /**
   * Watch state path for changes
   * @param {string} path - State path (e.g., 'user.id')
   * @param {Function} callback - Change callback
   * @returns {Function} Unwatch function
   */
  watch(path, callback) {
    if (!this.watchers.has(path)) {
      this.watchers.set(path, []);
    }

    this.watchers.get(path).push(callback);

    return () => {
      const callbacks = this.watchers.get(path);
      const index = callbacks.indexOf(callback);
      if (index > -1) {
        callbacks.splice(index, 1);
      }
    };
  }

  /**
   * Notify watchers of change
   * @param {string} path - State path
   * @param {*} newValue - New value
   * @param {*} oldValue - Old value
   */
  notifyWatchers(path, newValue, oldValue) {
    const callbacks = this.watchers.get(path) || [];
    for (const callback of callbacks) {
      try {
        callback(newValue, oldValue);
      } catch (error) {
        console.error(`Watcher callback failed for ${path}:`, error.message);
      }
    }

    this.emit('change', { path, newValue, oldValue });
  }

  /**
   * Get watcher count
   * @returns {number} Total watchers
   */
  getWatcherCount() {
    let count = 0;
    for (const callbacks of this.watchers.values()) {
      count += callbacks.length;
    }
    return count;
  }

  /**
   * Clear all watchers
   */
  clearWatchers() {
    this.watchers.clear();
  }
}

/**
 * AppState - Global application state
 * @class
 */
class AppState {
  constructor() {
    this.state = {
      initialized: false,
      user: null,
      auth: null,
      currentService: 'backend',
      preferences: {},
      metadata: { createdAt: Date.now() },
    };
    this.observer = new StateObserver();
    this.history = [];
    this.maxHistory = 50;
  }

  /**
   * Get state value
   * @param {string} path - State path
   * @returns {*} State value
   */
  get(path) {
    const parts = path.split('.');
    let value = this.state;

    for (const part of parts) {
      value = value?.[part];
      if (value === undefined) return undefined;
    }

    return value;
  }

  /**
   * Set state value
   * @param {string} path - State path
   * @param {*} value - New value
   */
  set(path, value) {
    const oldValue = this.get(path);
    const parts = path.split('.');
    const lastKey = parts.pop();

    let obj = this.state;
    for (const part of parts) {
      if (!(part in obj)) {
        obj[part] = {};
      }
      obj = obj[part];
    }

    obj[lastKey] = value;

    this.addToHistory({ type: 'state-change', path, value, oldValue, timestamp: Date.now() });
    this.observer.notifyWatchers(path, value, oldValue);
  }

  /**
   * Update multiple state values
   * @param {Object} updates - Updates object
   */
  update(updates) {
    for (const [path, value] of Object.entries(updates)) {
      this.set(path, value);
    }
  }

  /**
   * Add to history
   * @private
   */
  addToHistory(entry) {
    this.history.push(entry);
    if (this.history.length > this.maxHistory) {
      this.history.shift();
    }
  }

  /**
   * Get state history
   * @returns {Array} History entries
   */
  getHistory() {
    return [...this.history];
  }

  /**
   * Get entire state
   * @returns {Object} Current state
   */
  getState() {
    return JSON.parse(JSON.stringify(this.state));
  }

  /**
   * Watch state path
   * @param {string} path - State path
   * @param {Function} callback - Change callback
   * @returns {Function} Unwatch function
   */
  watch(path, callback) {
    return this.observer.watch(path, callback);
  }

  /**
   * Reset state
   * @param {Object} initialState - Initial state
   */
  reset(initialState = {}) {
    this.state = { ...initialState, metadata: { createdAt: Date.now() } };
    this.history = [];
    this.observer.clearWatchers();
  }
}

/**
 * SyncState - Multi-device sync state
 * @class
 */
class SyncState {
  constructor() {
    this.state = {
      syncing: false,
      lastSync: null,
      nextSync: null,
      devices: new Map(),
      conflicts: [],
      pendingChanges: [],
    };
    this.observer = new StateObserver();
  }

  /**
   * Register device
   * @param {string} deviceId - Device ID
   * @param {Object} info - Device info
   */
  registerDevice(deviceId, info) {
    this.state.devices.set(deviceId, {
      id: deviceId,
      ...info,
      lastSync: null,
      version: 0,
      registeredAt: Date.now(),
    });

    this.observer.notifyWatchers('devices', this.state.devices, null);
  }

  /**
   * Update device sync status
   * @param {string} deviceId - Device ID
   * @param {Object} status - Sync status
   */
  updateDeviceSync(deviceId, status) {
    const device = this.state.devices.get(deviceId);
    if (device) {
      device.lastSync = Date.now();
      device.version = status.version || device.version;
      device.status = status.status || 'synced';

      this.observer.notifyWatchers(`devices.${deviceId}`, device, null);
    }
  }

  /**
   * Add conflict
   * @param {Object} conflict - Conflict object
   */
  addConflict(conflict) {
    this.state.conflicts.push({
      id: `conflict-${Date.now()}-${Math.random()}`,
      ...conflict,
      createdAt: Date.now(),
      resolved: false,
    });

    this.observer.notifyWatchers('conflicts', this.state.conflicts, null);
  }

  /**
   * Resolve conflict
   * @param {string} conflictId - Conflict ID
   * @param {Object} resolution - Resolution details
   */
  resolveConflict(conflictId, resolution) {
    const conflict = this.state.conflicts.find(c => c.id === conflictId);
    if (conflict) {
      conflict.resolved = true;
      conflict.resolution = resolution;
      conflict.resolvedAt = Date.now();

      this.observer.notifyWatchers('conflicts', this.state.conflicts, null);
    }
  }

  /**
   * Get unresolved conflicts
   * @returns {Array} Unresolved conflicts
   */
  getUnresolvedConflicts() {
    return this.state.conflicts.filter(c => !c.resolved);
  }

  /**
   * Set syncing status
   * @param {boolean} syncing - Syncing status
   */
  setSyncing(syncing) {
    const oldValue = this.state.syncing;
    this.state.syncing = syncing;

    if (syncing) {
      this.state.nextSync = Date.now() + 30000;
    } else {
      this.state.lastSync = Date.now();
    }

    this.observer.notifyWatchers('syncing', syncing, oldValue);
  }

  /**
   * Get sync state
   * @returns {Object} Current sync state
   */
  getState() {
    return {
      syncing: this.state.syncing,
      lastSync: this.state.lastSync,
      nextSync: this.state.nextSync,
      deviceCount: this.state.devices.size,
      conflictCount: this.state.conflicts.length,
      unresolvedConflicts: this.getUnresolvedConflicts().length,
    };
  }

  /**
   * Watch sync state
   * @param {string} path - State path
   * @param {Function} callback - Change callback
   * @returns {Function} Unwatch function
   */
  watch(path, callback) {
    return this.observer.watch(path, callback);
  }

  /**
   * Reset state
   */
  reset() {
    this.state = {
      syncing: false,
      lastSync: null,
      nextSync: null,
      devices: new Map(),
      conflicts: [],
      pendingChanges: [],
    };
  }
}

/**
 * CacheState - Redis cache state
 * @class
 */
class CacheState {
  constructor() {
    this.state = {
      entries: new Map(),
      stats: {
        hits: 0,
        misses: 0,
        evictions: 0,
      },
      ttl: new Map(),
      maxSize: 1000,
    };
    this.observer = new StateObserver();
  }

  /**
   * Set cache entry
   * @param {string} key - Cache key
   * @param {*} value - Cache value
   * @param {number} ttlMs - TTL in milliseconds
   */
  set(key, value, ttlMs = null) {
    if (this.state.entries.size >= this.state.maxSize) {
      this.evict();
    }

    this.state.entries.set(key, value);

    if (ttlMs) {
      this.state.ttl.set(key, Date.now() + ttlMs);
    }

    this.observer.notifyWatchers(`cache.${key}`, value, null);
  }

  /**
   * Get cache entry
   * @param {string} key - Cache key
   * @returns {*} Cache value or undefined
   */
  get(key) {
    if (!this.state.entries.has(key)) {
      this.state.stats.misses++;
      return undefined;
    }

    const ttlTime = this.state.ttl.get(key);
    if (ttlTime && Date.now() > ttlTime) {
      this.state.entries.delete(key);
      this.state.ttl.delete(key);
      this.state.stats.misses++;
      return undefined;
    }

    this.state.stats.hits++;
    return this.state.entries.get(key);
  }

  /**
   * Delete cache entry
   * @param {string} key - Cache key
   * @returns {boolean} Success status
   */
  delete(key) {
    this.state.ttl.delete(key);
    return this.state.entries.delete(key);
  }

  /**
   * Clear cache
   */
  clear() {
    this.state.entries.clear();
    this.state.ttl.clear();
  }

  /**
   * Evict oldest entry
   * @private
   */
  evict() {
    const firstKey = this.state.entries.keys().next().value;
    if (firstKey) {
      this.delete(firstKey);
      this.state.stats.evictions++;
    }
  }

  /**
   * Get cache statistics
   * @returns {Object} Statistics
   */
  getStats() {
    const total = this.state.stats.hits + this.state.stats.misses;
    return {
      ...this.state.stats,
      hitRate: total > 0 ? (this.state.stats.hits / total * 100).toFixed(2) + '%' : '0%',
      size: this.state.entries.size,
      maxSize: this.state.maxSize,
    };
  }

  /**
   * Watch cache changes
   * @param {string} key - Cache key
   * @param {Function} callback - Change callback
   * @returns {Function} Unwatch function
   */
  watch(key, callback) {
    return this.observer.watch(`cache.${key}`, callback);
  }
}

/**
 * ErrorState - Recent errors and recovery status
 * @class
 */
class ErrorState {
  constructor() {
    this.state = {
      recentErrors: [],
      criticalErrors: [],
      recoveryStatus: 'healthy',
      lastError: null,
      errorCount: 0,
      maxErrors: 100,
    };
    this.observer = new StateObserver();
  }

  /**
   * Record error
   * @param {Error} error - Error object
   * @param {string} context - Error context
   */
  recordError(error, context = 'unknown') {
    const errorEntry = {
      id: `err-${Date.now()}-${Math.random()}`,
      message: error.message,
      stack: error.stack,
      context,
      timestamp: Date.now(),
      severity: this.determineSeverity(error),
    };

    this.state.recentErrors.push(errorEntry);
    if (this.state.recentErrors.length > this.state.maxErrors) {
      this.state.recentErrors.shift();
    }

    if (errorEntry.severity === 'critical') {
      this.state.criticalErrors.push(errorEntry);
      this.state.recoveryStatus = 'recovering';
    }

    this.state.lastError = errorEntry;
    this.state.errorCount++;

    this.observer.notifyWatchers('errors', this.state.recentErrors, null);
  }

  /**
   * Determine error severity
   * @private
   */
  determineSeverity(error) {
    if (error.message.includes('critical') || error.message.includes('fatal')) {
      return 'critical';
    }
    if (error.message.includes('warning')) {
      return 'warning';
    }
    return 'error';
  }

  /**
   * Mark recovery complete
   */
  markRecovered() {
    this.state.recoveryStatus = 'healthy';
    this.observer.notifyWatchers('recoveryStatus', 'healthy', 'recovering');
  }

  /**
   * Get recent errors
   * @param {number} limit - Result limit
   * @returns {Array} Recent errors
   */
  getRecentErrors(limit = 10) {
    return this.state.recentErrors.slice(-limit);
  }

  /**
   * Get error statistics
   * @returns {Object} Statistics
   */
  getStats() {
    return {
      totalErrors: this.state.errorCount,
      recentCount: this.state.recentErrors.length,
      criticalCount: this.state.criticalErrors.length,
      recoveryStatus: this.state.recoveryStatus,
      lastErrorTime: this.state.lastError?.timestamp,
    };
  }

  /**
   * Watch error state
   * @param {Function} callback - Change callback
   * @returns {Function} Unwatch function
   */
  watch(callback) {
    return this.observer.watch('errors', callback);
  }

  /**
   * Clear errors
   */
  clear() {
    this.state.recentErrors = [];
    this.state.criticalErrors = [];
    this.state.errorCount = 0;
    this.state.lastError = null;
  }
}

/**
 * StateTransitions - Update state safely with validation
 * @class
 */
class StateTransitions {
  constructor(appState, syncState, cacheState, errorState) {
    this.appState = appState;
    this.syncState = syncState;
    this.cacheState = cacheState;
    this.errorState = errorState;
    this.validators = new Map();
  }

  /**
   * Register state validator
   * @param {string} path - State path
   * @param {Function} validator - Validator function
   */
  registerValidator(path, validator) {
    this.validators.set(path, validator);
  }

  /**
   * Perform state transition
   * @param {string} path - State path
   * @param {*} value - New value
   * @returns {Object} Transition result
   */
  transition(path, value) {
    const validator = this.validators.get(path);
    if (validator && !validator(value)) {
      return { success: false, error: `Invalid value for ${path}` };
    }

    try {
      this.appState.set(path, value);
      return { success: true, value };
    } catch (error) {
      this.errorState.recordError(error, `transition:${path}`);
      return { success: false, error: error.message };
    }
  }

  /**
   * Perform atomic transition
   * @param {Array} transitions - Transition array
   * @returns {Object} Result
   */
  atomicTransition(transitions) {
    const rollbacks = [];

    try {
      for (const { path, value } of transitions) {
        const oldValue = this.appState.get(path);
        const result = this.transition(path, value);

        if (!result.success) {
          throw new Error(`Transition failed for ${path}`);
        }

        rollbacks.push({ path, value: oldValue });
      }

      return { success: true };
    } catch (error) {
      for (const { path, value } of rollbacks) {
        this.appState.set(path, value);
      }
      return { success: false, error: error.message };
    }
  }
}

/**
 * StateManager - Main state management system
 * @class
 */
class StateManager {
  constructor() {
    this.appState = new AppState();
    this.syncState = new SyncState();
    this.cacheState = new CacheState();
    this.errorState = new ErrorState();
    this.transitions = new StateTransitions(this.appState, this.syncState, this.cacheState, this.errorState);
  }

  /**
   * Get all state
   * @returns {Object} All state
   */
  getAllState() {
    return {
      app: this.appState.getState(),
      sync: this.syncState.getState(),
      cache: this.cacheState.getStats(),
      errors: this.errorState.getStats(),
    };
  }

  /**
   * Reset all state
   */
  reset() {
    this.appState.reset();
    this.syncState.reset();
    this.cacheState.clear();
    this.errorState.clear();
  }
}

module.exports = {
  AppState,
  SyncState,
  CacheState,
  ErrorState,
  StateObserver,
  StateTransitions,
  StateManager,
};

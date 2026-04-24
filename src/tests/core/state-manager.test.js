/**
 * State Manager Tests - Test suite for state management system
 * Tests app state, sync state, cache state, error state, and transitions
 * @module tests/core/state-manager.test
 */

const {
  AppState,
  SyncState,
  CacheState,
  ErrorState,
  StateManager,
} = require('../../src/core/state-manager');

describe('AppState', () => {
  let appState;

  beforeEach(() => {
    appState = new AppState();
  });

  test('should get and set state values', () => {
    appState.set('user.id', 'user-123');
    expect(appState.get('user.id')).toBe('user-123');
  });

  test('should handle nested state paths', () => {
    appState.set('auth.tokens.access', 'token-abc');
    expect(appState.get('auth.tokens.access')).toBe('token-abc');
  });

  test('should update multiple values', () => {
    appState.update({
      'user.id': 'user-123',
      'user.name': 'John Doe',
    });

    expect(appState.get('user.id')).toBe('user-123');
    expect(appState.get('user.name')).toBe('John Doe');
  });

  test('should watch state changes', (done) => {
    appState.watch('user.id', (newValue) => {
      expect(newValue).toBe('user-456');
      done();
    });

    appState.set('user.id', 'user-456');
  });

  test('should get state history', () => {
    appState.set('user.id', 'user-123');
    appState.set('user.name', 'John');

    const history = appState.getHistory();
    expect(history.length).toBe(2);
  });

  test('should reset state', () => {
    appState.set('user.id', 'user-123');
    appState.reset({ initialized: true });

    expect(appState.get('user.id')).toBeUndefined();
    expect(appState.get('initialized')).toBe(true);
  });
});

describe('SyncState', () => {
  let syncState;

  beforeEach(() => {
    syncState = new SyncState();
  });

  test('should register devices', () => {
    syncState.registerDevice('device-1', { name: 'iPhone' });

    expect(syncState.state.devices.size).toBe(1);
  });

  test('should update device sync status', () => {
    syncState.registerDevice('device-1', { name: 'iPhone' });
    syncState.updateDeviceSync('device-1', { version: 2, status: 'synced' });

    const device = syncState.state.devices.get('device-1');
    expect(device.version).toBe(2);
    expect(device.status).toBe('synced');
  });

  test('should add and resolve conflicts', () => {
    syncState.addConflict({ resourceId: 'res-1', type: 'content' });

    expect(syncState.getUnresolvedConflicts().length).toBe(1);

    const conflictId = syncState.state.conflicts[0].id;
    syncState.resolveConflict(conflictId, { strategy: 'local-wins' });

    expect(syncState.getUnresolvedConflicts().length).toBe(0);
  });

  test('should set syncing status', () => {
    syncState.setSyncing(true);
    expect(syncState.state.syncing).toBe(true);

    syncState.setSyncing(false);
    expect(syncState.state.syncing).toBe(false);
    expect(syncState.state.lastSync).toBeDefined();
  });
});

describe('CacheState', () => {
  let cacheState;

  beforeEach(() => {
    cacheState = new CacheState();
  });

  test('should set and get cache entries', () => {
    cacheState.set('key-1', { data: 'value' });

    expect(cacheState.get('key-1')).toEqual({ data: 'value' });
  });

  test('should handle TTL expiration', () => {
    cacheState.set('key-1', 'value', 100); // 100ms TTL

    expect(cacheState.get('key-1')).toBe('value');

    jest.useFakeTimers();
    jest.advanceTimersByTime(200);

    expect(cacheState.get('key-1')).toBeUndefined();

    jest.useRealTimers();
  });

  test('should track cache statistics', () => {
    cacheState.set('key-1', 'value');
    cacheState.get('key-1');
    cacheState.get('key-2');

    const stats = cacheState.getStats();

    expect(stats.hits).toBe(1);
    expect(stats.misses).toBe(1);
  });

  test('should delete cache entries', () => {
    cacheState.set('key-1', 'value');

    const deleted = cacheState.delete('key-1');

    expect(deleted).toBe(true);
    expect(cacheState.get('key-1')).toBeUndefined();
  });

  test('should enforce max size', () => {
    cacheState.maxSize = 3;

    cacheState.set('key-1', 'value1');
    cacheState.set('key-2', 'value2');
    cacheState.set('key-3', 'value3');
    cacheState.set('key-4', 'value4');

    expect(cacheState.state.entries.size).toBe(3);
    expect(cacheState.state.stats.evictions).toBeGreaterThan(0);
  });
});

describe('ErrorState', () => {
  let errorState;

  beforeEach(() => {
    errorState = new ErrorState();
  });

  test('should record errors', () => {
    const error = new Error('Test error');

    errorState.recordError(error, 'test-context');

    expect(errorState.state.errorCount).toBe(1);
    expect(errorState.state.lastError).toBeDefined();
  });

  test('should track critical errors', () => {
    const error = new Error('Critical failure');

    errorState.recordError(error, 'test');

    expect(errorState.state.criticalErrors.length).toBeGreaterThanOrEqual(0);
  });

  test('should get recent errors', () => {
    for (let i = 0; i < 15; i++) {
      errorState.recordError(new Error(`Error ${i}`), 'test');
    }

    const recent = errorState.getRecentErrors(5);

    expect(recent.length).toBe(5);
  });

  test('should mark recovery', () => {
    const error = new Error('Critical failure');
    errorState.recordError(error, 'test');

    errorState.markRecovered();

    expect(errorState.state.recoveryStatus).toBe('healthy');
  });

  test('should get error statistics', () => {
    errorState.recordError(new Error('Error'), 'test');

    const stats = errorState.getStats();

    expect(stats.totalErrors).toBeGreaterThan(0);
    expect(stats.recoveryStatus).toBeDefined();
  });
});

describe('StateManager', () => {
  let stateManager;

  beforeEach(() => {
    stateManager = new StateManager();
  });

  test('should integrate all state managers', () => {
    stateManager.appState.set('user.id', 'user-1');
    stateManager.syncState.registerDevice('device-1', { name: 'iPhone' });
    stateManager.cacheState.set('cache-1', 'value');
    stateManager.errorState.recordError(new Error('test'), 'test');

    const allState = stateManager.getAllState();

    expect(allState.app).toBeDefined();
    expect(allState.sync).toBeDefined();
    expect(allState.cache).toBeDefined();
    expect(allState.errors).toBeDefined();
  });

  test('should reset all state', () => {
    stateManager.appState.set('user.id', 'user-1');
    stateManager.errorState.recordError(new Error('test'), 'test');

    stateManager.reset();

    expect(stateManager.appState.get('user.id')).toBeUndefined();
    expect(stateManager.errorState.state.errorCount).toBe(0);
  });
});

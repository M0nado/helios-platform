/**
 * Sync Orchestrator Tests - Test suite for sync orchestration
 * Tests conflict detection, resolution, audit trail, and sync coordination
 * @module tests/core/sync-orchestrator.test
 */

const {
  ConflictDetector,
  ResolutionOrchestrator,
  SyncAuditTrail,
  SyncStatus,
  AutoSyncTrigger,
  SyncOrchestrator,
} = require('../../src/core/sync-orchestrator');

describe('ConflictDetector', () => {
  test('should detect content conflicts', () => {
    const local = { content: { text: 'local' }, timestamp: Date.now() };
    const remote = { content: { text: 'remote' }, timestamp: Date.now() };

    const conflicts = ConflictDetector.detectConflicts(local, remote);

    expect(conflicts.length).toBeGreaterThan(0);
    expect(conflicts[0].type).toBe('content');
  });

  test('should detect delete conflicts', () => {
    const local = { content: 'data', deleted: false };
    const remote = { content: 'data', deleted: true };

    const conflicts = ConflictDetector.detectConflicts(local, remote);

    expect(conflicts.some(c => c.type === 'delete')).toBe(true);
  });

  test('should detect timestamp conflicts', () => {
    const local = { content: 'data', timestamp: 1000 };
    const remote = { content: 'data', timestamp: 5000 };

    const conflicts = ConflictDetector.detectConflicts(local, remote);

    expect(conflicts.some(c => c.type === 'timestamp')).toBe(true);
  });

  test('should not detect conflicts when identical', () => {
    const local = { content: 'data', deleted: false, timestamp: 1000 };
    const remote = { content: 'data', deleted: false, timestamp: 1000 };

    const conflicts = ConflictDetector.detectConflicts(local, remote);

    expect(conflicts.length).toBe(0);
  });

  test('should check if changes are compatible', () => {
    const changes1 = [{ path: 'user.name' }, { path: 'user.age' }];
    const changes2 = [{ path: 'settings.theme' }];

    expect(ConflictDetector.areChangesCompatible(changes1, changes2)).toBe(true);
  });

  test('should detect incompatible changes', () => {
    const changes1 = [{ path: 'user.name' }];
    const changes2 = [{ path: 'user.name' }];

    expect(ConflictDetector.areChangesCompatible(changes1, changes2)).toBe(false);
  });
});

describe('ResolutionOrchestrator', () => {
  let orchestrator;

  beforeEach(() => {
    orchestrator = new ResolutionOrchestrator();
  });

  test('should resolve with last-write-wins strategy', () => {
    const local = { content: 'local', timestamp: 2000 };
    const remote = { content: 'remote', timestamp: 1000 };
    const conflict = { local, remote };

    const result = orchestrator.resolve(conflict, 'last-write-wins');

    expect(result.winner).toBe('local');
    expect(result.version).toEqual(local);
  });

  test('should resolve with local-wins strategy', () => {
    const local = { content: 'local' };
    const remote = { content: 'remote' };
    const conflict = { local, remote };

    const result = orchestrator.resolve(conflict, 'local-wins');

    expect(result.winner).toBe('local');
  });

  test('should resolve with remote-wins strategy', () => {
    const local = { content: 'local' };
    const remote = { content: 'remote' };
    const conflict = { local, remote };

    const result = orchestrator.resolve(conflict, 'remote-wins');

    expect(result.winner).toBe('remote');
  });

  test('should merge conflicts', () => {
    const local = { name: 'John' };
    const remote = { age: 30 };
    const conflict = { local, remote };

    const result = orchestrator.resolve(conflict, 'merge');

    expect(result.winner).toBe('merged');
    expect(result.version.merged).toBe(true);
  });

  test('should require manual review', () => {
    const conflict = { local: {}, remote: {} };

    const result = orchestrator.resolve(conflict, 'manual');

    expect(result.winner).toBe('pending');
    expect(result.requiresManualReview).toBe(true);
  });

  test('should resolve multiple conflicts', () => {
    const conflicts = [
      { id: 1, local: {}, remote: {} },
      { id: 2, local: {}, remote: {} },
    ];

    const results = orchestrator.resolveMultiple(conflicts, 'last-write-wins');

    expect(results.length).toBe(2);
  });

  test('should get available strategies', () => {
    const strategies = orchestrator.getAvailableStrategies();

    expect(strategies).toContain('last-write-wins');
    expect(strategies).toContain('local-wins');
    expect(strategies).toContain('remote-wins');
  });
});

describe('SyncAuditTrail', () => {
  let trail;

  beforeEach(() => {
    trail = new SyncAuditTrail();
  });

  test('should log sync operations', () => {
    const entryId = trail.log('sync-start', { deviceCount: 2 });

    expect(entryId).toBeDefined();
    expect(trail.entries.length).toBe(1);
  });

  test('should get audit entries', () => {
    trail.log('sync-start', { deviceCount: 2 });
    trail.log('sync-item', { itemId: 'item-1' });
    trail.log('sync-complete', { duration: 1000 });

    const entries = trail.getEntries();

    expect(entries.length).toBe(3);
  });

  test('should filter audit entries', () => {
    trail.log('sync-start', { deviceCount: 2 });
    trail.log('sync-item', { itemId: 'item-1', resourceId: 'res-1' });
    trail.log('sync-complete', { duration: 1000 });

    const itemEntries = trail.getEntries({ operation: 'sync-item' });

    expect(itemEntries.length).toBe(1);
    expect(itemEntries[0].operation).toBe('sync-item');
  });

  test('should get operation statistics', () => {
    trail.log('sync-start', {});
    trail.log('sync-item', {});
    trail.log('sync-item', {});
    trail.log('sync-complete', {});

    const stats = trail.getStats();

    expect(stats.totalEntries).toBe(4);
    expect(stats.operations['sync-start']).toBe(1);
    expect(stats.operations['sync-item']).toBe(2);
  });

  test('should export audit trail', () => {
    trail.log('sync-start', { deviceCount: 2 });

    const exported = trail.export();

    expect(exported.exportedAt).toBeDefined();
    expect(exported.entryCount).toBe(1);
    expect(exported.entries).toBeDefined();
  });

  test('should maintain max entries', () => {
    trail.maxEntries = 5;

    for (let i = 0; i < 10; i++) {
      trail.log('sync-item', { itemId: i });
    }

    expect(trail.entries.length).toBe(5);
  });
});

describe('SyncStatus', () => {
  let status;

  beforeEach(() => {
    status = new SyncStatus();
  });

  test('should set sync state', () => {
    status.setState('syncing');

    expect(status.status.state).toBe('syncing');
  });

  test('should update device status', () => {
    status.updateDeviceStatus('device-1', { version: 2, status: 'synced' });

    const device = status.status.devices.get('device-1');

    expect(device).toBeDefined();
    expect(device.version).toBe(2);
  });

  test('should record conflicts', () => {
    status.recordConflict({ resourceId: 'res-1' });

    expect(status.status.conflicts).toBe(1);
  });

  test('should record errors', () => {
    status.recordError(new Error('test'));

    expect(status.status.errors).toBe(1);
  });

  test('should update transfer statistics', () => {
    status.updateTransferStats(1000, 5);
    status.updateTransferStats(500, 3);

    expect(status.status.bytesTransferred).toBe(1500);
    expect(status.status.itemsProcessed).toBe(8);
  });

  test('should get current status', () => {
    status.setState('syncing');
    status.updateDeviceStatus('device-1', { version: 1 });

    const currentStatus = status.getStatus();

    expect(currentStatus.state).toBe('syncing');
    expect(currentStatus.devices).toBeDefined();
  });

  test('should reset status', () => {
    status.setState('syncing');
    status.recordError(new Error('test'));

    status.reset();

    expect(status.status.state).toBe('idle');
    expect(status.status.errors).toBe(0);
  });
});

describe('AutoSyncTrigger', () => {
  let trigger;

  beforeEach(() => {
    trigger = new AutoSyncTrigger();
  });

  test('should buffer changes', () => {
    trigger.bufferChange({ type: 'update', resourceId: 'res-1' });

    expect(trigger.changeBuffer.length).toBe(1);
  });

  test('should detect when sync should trigger', () => {
    expect(trigger.shouldTrigger()).toBe(false);

    trigger.bufferChange({ type: 'update' });

    expect(trigger.shouldTrigger()).toBe(true);
  });

  test('should get pending changes', () => {
    trigger.bufferChange({ type: 'update', resourceId: 'res-1' });
    trigger.bufferChange({ type: 'delete', resourceId: 'res-2' });

    const pending = trigger.getPendingChanges();

    expect(pending.length).toBe(2);
  });

  test('should flush buffer', () => {
    trigger.bufferChange({ type: 'update' });

    const flushed = trigger.flush();

    expect(flushed.length).toBe(1);
    expect(trigger.changeBuffer.length).toBe(0);
  });

  test('should auto-flush on buffer size', () => {
    trigger.bufferSize = 3;

    trigger.bufferChange({ id: 1 });
    trigger.bufferChange({ id: 2 });
    trigger.bufferChange({ id: 3 });
    trigger.bufferChange({ id: 4 });

    expect(trigger.changeBuffer.length).toBeLessThan(4);
  });

  test('should register and evaluate triggers', () => {
    const action = jest.fn();

    trigger.registerTrigger({
      event: 'update',
      action,
    });

    const actions = trigger.evaluateTriggers({ type: 'update' });

    expect(actions.length).toBe(1);
  });

  test('should get trigger statistics', () => {
    trigger.registerTrigger({ event: 'update', action: jest.fn() });
    trigger.bufferChange({ type: 'update' });

    const stats = trigger.getStats();

    expect(stats.registeredTriggers).toBe(1);
    expect(stats.pendingChanges).toBe(1);
  });
});

describe('SyncOrchestrator', () => {
  let orchestrator;

  beforeEach(() => {
    orchestrator = new SyncOrchestrator();
  });

  test('should start sync operation', () => {
    const syncId = orchestrator.startSync(['device-1', 'device-2']);

    expect(syncId).toBeDefined();
    expect(orchestrator.syncStatus.status.state).toBe('syncing');
  });

  test('should process sync items', () => {
    const syncId = orchestrator.startSync(['device-1']);
    const result = orchestrator.processSyncItem(syncId, { id: 'item-1', content: 'test' });

    expect(result.success).toBe(true);
    expect(result.status).toBe('synced');
  });

  test('should complete sync operation', () => {
    const syncId = orchestrator.startSync(['device-1']);

    orchestrator.completeSync(syncId);

    expect(orchestrator.syncStatus.status.state).toBe('idle');
  });

  test('should get sync statistics', () => {
    const syncId = orchestrator.startSync(['device-1']);
    orchestrator.processSyncItem(syncId, { id: 'item-1' });
    orchestrator.completeSync(syncId);

    const stats = orchestrator.getStats();

    expect(stats.status).toBeDefined();
    expect(stats.audit).toBeDefined();
    expect(stats.triggers).toBeDefined();
  });
});

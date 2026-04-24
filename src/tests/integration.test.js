/**
 * Integration Tests - End-to-end service integration tests
 * Tests cross-service communication via event bus and adapters
 * @module tests/integration.test
 */

const { EventBus } = require('../src/core/event-bus');
const { CorrelationIDGenerator, TraceIDPropagation } = require('../src/core/correlation');
const { StateManager } = require('../src/core/state-manager');
const { AdapterRegistry } = require('../src/adapters');
const { APIGateway } = require('../src/gateway/api-gateway');
const { SyncOrchestrator } = require('../src/core/sync-orchestrator');

describe('Integration Tests - Full Platform Communication', () => {
  let eventBus;
  let correlationIdGen;
  let traceIdPropagation;
  let stateManager;
  let adapterRegistry;
  let apiGateway;
  let syncOrchestrator;
  let correlationId;

  beforeEach(() => {
    eventBus = new EventBus();
    correlationIdGen = new CorrelationIDGenerator();
    traceIdPropagation = new TraceIDPropagation();
    stateManager = new StateManager();
    adapterRegistry = new AdapterRegistry();
    apiGateway = new APIGateway();
    syncOrchestrator = new SyncOrchestrator();
    correlationId = correlationIdGen.generate();
  });

  describe('E2E: User Authentication Flow', () => {
    test('should complete full user authentication', async () => {
      // 1. Create correlation context
      const context = traceIdPropagation.createContext(correlationId, { userId: null });

      // 2. Issue token
      const { token } = apiGateway.auth.issueToken('user-123', { scope: 'write' });

      // 3. Update app state
      stateManager.appState.set('user.id', 'user-123');
      stateManager.appState.set('auth.token', token);

      // 4. Publish authentication event
      eventBus.publish(
        'user:authenticated',
        {
          userId: 'user-123',
          token,
          expiresAt: Date.now() + 3600000,
          scopes: ['read', 'write'],
        },
        { correlationId }
      );

      // 5. Verify state
      expect(stateManager.appState.get('user.id')).toBe('user-123');
      expect(stateManager.appState.get('auth.token')).toBe(token);

      // 6. Verify event was stored
      const stored = eventBus.persistence.retrieve({ eventType: 'user:authenticated' });
      expect(stored.length).toBeGreaterThan(0);
    });
  });

  describe('E2E: AI Suggestion Processing', () => {
    test('should process AI suggestion with format adaptation', async () => {
      const backendData = {
        content: 'What is machine learning?',
        userId: 'user-123',
        documentId: 'doc-456',
        model: 'gpt-4',
      };

      // 1. Transform to AI format
      const aiFormat = adapterRegistry.transform('ai', backendData, 'to');

      // 2. Simulate AI service response
      const aiResponse = {
        id: 'suggestion-789',
        response: 'Machine learning is...',
        confidence: 0.95,
        model: 'gpt-4',
        tokensUsed: 150,
      };

      // 3. Transform back to backend format
      const backendResponse = adapterRegistry.transform('ai', aiResponse, 'from');

      // 4. Publish AI event
      eventBus.publish(
        'ai:suggestion',
        backendResponse,
        { correlationId }
      );

      // 5. Update state with suggestion
      stateManager.cacheState.set(`ai-suggestion-${backendResponse.suggestionsId}`, backendResponse);

      // 6. Verify
      const cached = stateManager.cacheState.get(`ai-suggestion-${backendResponse.suggestionsId}`);
      expect(cached.confidence).toBe(0.95);
    });
  });

  describe('E2E: Cross-Device Sync', () => {
    test('should synchronize data across devices', async () => {
      // 1. Register devices
      syncOrchestrator.syncStatus.setState('syncing');
      const syncId = syncOrchestrator.startSync(['device-1', 'device-2']);

      // 2. Register devices in state
      stateManager.syncState.registerDevice('device-1', { name: 'iPhone', platform: 'iOS' });
      stateManager.syncState.registerDevice('device-2', { name: 'Laptop', platform: 'Windows' });

      // 3. Transform data to sync format for each device
      const localData = {
        resourceId: 'note-123',
        deviceId: 'device-1',
        content: { title: 'My Note', text: 'Content' },
        version: 1,
        timestamp: Date.now(),
      };

      const syncData = adapterRegistry.transform('sync', localData, 'to');

      // 4. Process sync item
      syncOrchestrator.processSyncItem(syncId, syncData);

      // 5. Publish sync event
      eventBus.publish('sync:conflict', {
        resourceId: 'note-123',
        localVersion: 1,
        remoteVersion: 1,
        conflictType: 'content',
      }, { correlationId });

      // 6. Update device sync status
      stateManager.syncState.updateDeviceSync('device-1', { version: 2, status: 'synced' });

      // 7. Complete sync
      syncOrchestrator.completeSync(syncId);

      // Verify
      expect(stateManager.syncState.state.syncing).toBe(false);
    });
  });

  describe('E2E: Analytics Event Processing', () => {
    test('should collect and process analytics events', async () => {
      const backendEvent = {
        eventName: 'page_view',
        userId: 'user-123',
        timestamp: Date.now(),
        properties: { page: '/dashboard', duration: 5000 },
      };

      // 1. Transform to analytics format
      const analyticsFormat = adapterRegistry.transform('analytics', backendEvent, 'to');

      // 2. Publish analytics event
      eventBus.publish('analytics:recorded', analyticsFormat, { correlationId });

      // 3. Subscribe to analytics events
      let recordedEvent;
      eventBus.subscribe('analytics:recorded', (data) => {
        recordedEvent = data;
      });

      // 4. Publish again for subscriber
      eventBus.publish('analytics:recorded', analyticsFormat, { correlationId });

      // 5. Verify event was recorded
      expect(recordedEvent).toBeDefined();
    });
  });

  describe('E2E: Plugin Installation and Registration', () => {
    test('should install and register plugin', async () => {
      const pluginData = {
        pluginId: 'plugin-translator',
        name: 'Language Translator',
        version: '1.0.0',
        author: 'Dev Team',
        permissions: ['read', 'write'],
      };

      // 1. Transform to plugin format
      const pluginFormat = adapterRegistry.transform('plugin', pluginData, 'to');

      // 2. Publish plugin installation event
      eventBus.publish('plugin:installed', pluginFormat, { correlationId });

      // 3. Cache plugin data
      stateManager.cacheState.set(`plugin-${pluginData.pluginId}`, pluginData);

      // 4. Verify
      const cached = stateManager.cacheState.get(`plugin-${pluginData.pluginId}`);
      expect(cached.name).toBe('Language Translator');
    });
  });

  describe('E2E: Error Handling and Recovery', () => {
    test('should handle errors with correlation tracking', async () => {
      // 1. Create correlation context
      const context = traceIdPropagation.createContext(correlationId);

      // 2. Record error
      const error = new Error('Database connection failed');
      stateManager.errorState.recordError(error, 'database-service');

      // 3. Publish error event
      eventBus.publish('error:occurred', {
        errorId: 'err-123',
        message: error.message,
        service: 'database-service',
        severity: 'high',
        stack: error.stack,
      }, { correlationId });

      // 4. Verify error was tracked
      const stats = stateManager.errorState.getStats();
      expect(stats.totalErrors).toBeGreaterThan(0);

      // 5. Mark recovery
      stateManager.errorState.markRecovered();
      expect(stateManager.errorState.state.recoveryStatus).toBe('healthy');
    });
  });

  describe('E2E: Rate Limiting and Authentication', () => {
    test('should enforce rate limits and authentication', () => {
      // 1. Create API key
      apiGateway.auth.registerAPIKey('api-key-123', { tier: 'pro', userId: 'user-123' });

      // 2. Make multiple requests
      let requestCount = 0;
      for (let i = 0; i < 5; i++) {
        const limit = apiGateway.rateLimiter.checkLimit('user-123', 'pro');
        if (limit.allowed) {
          requestCount++;
        }
      }

      expect(requestCount).toBe(5);

      // 3. Check stats
      const stats = apiGateway.getStats();
      expect(stats.apiKeys).toBeGreaterThan(0);
    });
  });

  describe('E2E: Multi-Service Choreography', () => {
    test('should coordinate multiple services via event bus', async () => {
      const services = {
        ai: [],
        analytics: [],
        sync: [],
        plugin: [],
      };

      // 1. Subscribe all services
      for (const [service, events] of Object.entries(services)) {
        eventBus.subscribe(`${service}:installed`, (data) => {
          events.push(data);
        });
      }

      // 2. Simulate multi-service workflow
      eventBus.publish('ai:suggestion', {
        suggestionsId: 'ai-1',
        content: 'suggestion',
        confidence: 0.9,
      }, { correlationId });

      eventBus.publish('analytics:recorded', {
        eventName: 'suggestion_viewed',
        userId: 'user-1',
        timestamp: Date.now(),
      }, { correlationId });

      eventBus.publish('sync:conflict', {
        resourceId: 'res-1',
        localVersion: 1,
        remoteVersion: 2,
        conflictType: 'content',
      }, { correlationId });

      // 3. Verify event bus statistics
      const stats = eventBus.getStats();
      expect(stats.totalSubscribers).toBeGreaterThan(0);
      expect(stats.storedEvents).toBeGreaterThan(0);
    });
  });

  describe('E2E: State Consistency Across Services', () => {
    test('should maintain state consistency', () => {
      // 1. Set up initial state
      stateManager.appState.set('user.id', 'user-123');
      stateManager.appState.set('session.device', 'device-1');

      // 2. Register device in sync state
      stateManager.syncState.registerDevice('device-1', { name: 'iPhone' });

      // 3. Cache user data
      stateManager.cacheState.set('user-123-profile', { name: 'John Doe' });

      // 4. Monitor state changes
      let changes = [];
      stateManager.appState.watch('user.id', (newValue) => {
        changes.push({ field: 'user.id', value: newValue });
      });

      // 5. Make state changes
      stateManager.appState.set('user.id', 'user-456');

      // 6. Verify consistency
      expect(stateManager.appState.get('user.id')).toBe('user-456');
      expect(changes.length).toBe(1);

      // 7. Check all state managers
      const allState = stateManager.getAllState();
      expect(allState.app).toBeDefined();
      expect(allState.sync).toBeDefined();
      expect(allState.cache).toBeDefined();
    });
  });

  describe('E2E: Request Tracing Through System', () => {
    test('should trace request through multiple services', () => {
      // 1. Start request tracking
      const headers = traceIdPropagation.extractHeaders(correlationId);

      // 2. Create child spans for different services
      const aiSpan = traceIdPropagation.createChildSpan(correlationId, 'ai-service');
      const analyticsSpan = traceIdPropagation.createChildSpan(correlationId, 'analytics-service');
      const syncSpan = traceIdPropagation.createChildSpan(correlationId, 'sync-service');

      // 3. Verify trace hierarchy
      expect(aiSpan.parentSpanId).toBe(traceIdPropagation.getContext(correlationId).spanId);
      expect(analyticsSpan.service).toBe('analytics-service');
      expect(syncSpan.service).toBe('sync-service');

      // 4. Verify headers contain trace info
      expect(headers['x-correlation-id']).toBe(correlationId);
      expect(headers['x-trace-id']).toBeDefined();
      expect(headers['x-span-id']).toBeDefined();
    });
  });
});

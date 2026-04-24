/**
 * Adapter Tests - Test suite for data adapters
 * Tests bidirectional transformations for all service formats
 * @module tests/adapters/adapters.test
 */

const {
  Transformers,
  AIAdapter,
  AnalyticsAdapter,
  SyncAdapter,
  PluginAdapter,
  PWAAdapter,
  AdapterRegistry,
} = require('../../src/adapters');

describe('Transformers', () => {
  test('should normalize timestamps', () => {
    const timestamp = Transformers.normalizeTimestamp(new Date('2024-01-01'));

    expect(typeof timestamp).toBe('number');
    expect(timestamp).toBeGreaterThan(0);
  });

  test('should normalize version strings', () => {
    const version = Transformers.normalizeVersion('1.2.3');

    expect(version.major).toBe(1);
    expect(version.minor).toBe(2);
    expect(version.patch).toBe(3);
  });

  test('should normalize metadata', () => {
    const metadata = Transformers.normalizeMetadata({ userId: 'user-1' });

    expect(metadata.source).toBeDefined();
    expect(metadata.userId).toBe('user-1');
    expect(metadata.timestamp).toBeDefined();
  });

  test('should flatten nested objects', () => {
    const obj = { user: { profile: { name: 'John' } } };
    const flattened = Transformers.flattenObject(obj);

    expect(flattened['user.profile.name']).toBe('John');
  });

  test('should unflatten objects', () => {
    const flattened = { 'user.profile.name': 'John' };
    const unflattened = Transformers.unflattenObject(flattened);

    expect(unflattened.user.profile.name).toBe('John');
  });

  test('should sanitize strings', () => {
    const sanitized = Transformers.sanitizeString('<script>alert("xss")</script>');

    expect(sanitized).not.toContain('<');
    expect(sanitized).not.toContain('>');
  });

  test('should validate required fields', () => {
    const data = { name: 'John', age: 30 };
    const result = Transformers.validateRequired(data, ['name', 'age']);

    expect(result.valid).toBe(true);
  });

  test('should detect missing required fields', () => {
    const data = { name: 'John' };
    const result = Transformers.validateRequired(data, ['name', 'age']);

    expect(result.valid).toBe(false);
    expect(result.missing).toContain('age');
  });
});

describe('AIAdapter', () => {
  test('should transform backend to AI format', () => {
    const data = {
      content: 'What is AI?',
      userId: 'user-1',
      model: 'gpt-4',
    };

    const aiFormat = AIAdapter.toAIFormat(data);

    expect(aiFormat.query).toBe('What is AI?');
    expect(aiFormat.context.userId).toBe('user-1');
    expect(aiFormat.parameters.model).toBe('gpt-4');
  });

  test('should transform AI to backend format', () => {
    const aiData = {
      id: 'ai-123',
      response: 'AI is...',
      confidence: 0.95,
      model: 'gpt-4',
    };

    const backendFormat = AIAdapter.fromAIFormat(aiData);

    expect(backendFormat.suggestionsId).toBe('ai-123');
    expect(backendFormat.content).toBe('AI is...');
    expect(backendFormat.confidence).toBe(0.95);
  });

  test('should validate AI input', () => {
    const valid = { content: 'test', userId: 'user-1' };
    const invalid = { content: 'test' };

    expect(AIAdapter.validate(valid).valid).toBe(true);
    expect(AIAdapter.validate(invalid).valid).toBe(false);
  });
});

describe('AnalyticsAdapter', () => {
  test('should transform backend to Analytics format', () => {
    const data = {
      eventName: 'page_view',
      userId: 'user-1',
      timestamp: Date.now(),
    };

    const analyticsFormat = AnalyticsAdapter.toAnalyticsFormat(data);

    expect(analyticsFormat.event).toBe('page_view');
    expect(analyticsFormat.properties.userId).toBe('user-1');
  });

  test('should transform Analytics to backend format', () => {
    const analyticsData = {
      event: 'page_view',
      properties: {
        userId: 'user-1',
        timestamp: Date.now(),
      },
    };

    const backendFormat = AnalyticsAdapter.fromAnalyticsFormat(analyticsData);

    expect(backendFormat.eventName).toBe('page_view');
    expect(backendFormat.userId).toBe('user-1');
  });

  test('should validate analytics input', () => {
    const valid = { eventName: 'test', userId: 'user-1' };
    const invalid = { eventName: 'test' };

    expect(AnalyticsAdapter.validate(valid).valid).toBe(true);
    expect(AnalyticsAdapter.validate(invalid).valid).toBe(false);
  });
});

describe('SyncAdapter', () => {
  test('should transform backend to Sync format', () => {
    const data = {
      resourceId: 'res-1',
      deviceId: 'device-1',
      content: { text: 'data' },
      version: 1,
    };

    const syncFormat = SyncAdapter.toSyncFormat(data);

    expect(syncFormat.resourceId).toBe('res-1');
    expect(syncFormat.deviceId).toBe('device-1');
    expect(syncFormat.contentHash).toBeDefined();
  });

  test('should transform Sync to backend format', () => {
    const syncData = {
      resourceId: 'res-1',
      deviceId: 'device-1',
      content: { text: 'data' },
      version: 1,
      timestamp: Date.now(),
      metadata: {
        lastModified: Date.now(),
        syncStatus: 'synced',
      },
    };

    const backendFormat = SyncAdapter.fromSyncFormat(syncData);

    expect(backendFormat.resourceId).toBe('res-1');
    expect(backendFormat.version).toBe(1);
  });

  test('should validate sync input', () => {
    const valid = { resourceId: 'res-1', deviceId: 'device-1', content: {} };
    const invalid = { resourceId: 'res-1' };

    expect(SyncAdapter.validate(valid).valid).toBe(true);
    expect(SyncAdapter.validate(invalid).valid).toBe(false);
  });
});

describe('PluginAdapter', () => {
  test('should transform backend to Plugin format', () => {
    const data = {
      pluginId: 'plugin-1',
      name: 'Test Plugin',
      version: '1.0.0',
      author: 'Dev',
    };

    const pluginFormat = PluginAdapter.toPluginFormat(data);

    expect(pluginFormat.pluginId).toBe('plugin-1');
    expect(pluginFormat.name).toBe('Test Plugin');
    expect(pluginFormat.version.major).toBe(1);
  });

  test('should transform Plugin to backend format', () => {
    const pluginData = {
      pluginId: 'plugin-1',
      name: 'Test Plugin',
      version: { major: 1, minor: 0, patch: 0 },
      manifest: { author: 'Dev', description: 'test' },
    };

    const backendFormat = PluginAdapter.fromPluginFormat(pluginData);

    expect(backendFormat.pluginId).toBe('plugin-1');
    expect(backendFormat.version).toBe('1.0.0');
  });

  test('should validate plugin input', () => {
    const valid = { pluginId: 'p-1', name: 'test', version: '1.0.0' };
    const invalid = { pluginId: 'p-1', name: 'test' };

    expect(PluginAdapter.validate(valid).valid).toBe(true);
    expect(PluginAdapter.validate(invalid).valid).toBe(false);
  });
});

describe('PWAAdapter', () => {
  test('should transform backend to WebSocket format', () => {
    const data = {
      type: 'update',
      content: 'test message',
      correlationId: 'corr-1',
    };

    const wsFormat = PWAAdapter.toWebSocketFormat(data);

    expect(wsFormat.type).toBe('update');
    expect(wsFormat.payload.content).toBe('test message');
    expect(wsFormat.id).toBeDefined();
  });

  test('should transform WebSocket to backend format', () => {
    const wsData = {
      type: 'update',
      id: 'msg-1',
      payload: { content: 'test', metadata: { source: 'pwa' } },
      timestamp: Date.now(),
      correlationId: 'corr-1',
    };

    const backendFormat = PWAAdapter.fromWebSocketFormat(wsData);

    expect(backendFormat.type).toBe('update');
    expect(backendFormat.content).toBe('test');
  });

  test('should validate PWA input', () => {
    const valid = { type: 'update', content: 'test' };
    const invalid = { type: 'update' };

    expect(PWAAdapter.validate(valid).valid).toBe(true);
    expect(PWAAdapter.validate(invalid).valid).toBe(false);
  });
});

describe('AdapterRegistry', () => {
  let registry;

  beforeEach(() => {
    registry = new AdapterRegistry();
  });

  test('should register default adapters', () => {
    expect(registry.get('ai')).toBe(AIAdapter);
    expect(registry.get('analytics')).toBe(AnalyticsAdapter);
    expect(registry.get('sync')).toBe(SyncAdapter);
  });

  test('should get registered adapter', () => {
    const adapter = registry.get('ai');

    expect(adapter).toBe(AIAdapter);
  });

  test('should transform data using adapter', () => {
    const data = {
      content: 'test',
      userId: 'user-1',
      model: 'test-model',
    };

    const transformed = registry.transform('ai', data, 'to');

    expect(transformed.query).toBe('test');
  });

  test('should handle unknown adapters', () => {
    expect(() => {
      registry.transform('unknown', {}, 'to');
    }).toThrow();
  });
});

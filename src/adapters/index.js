/**
 * Data Adapters - Bidirectional data format transformation
 * Handles format normalization across all HELIOS services
 * @module adapters/index
 */

/**
 * Transformers - Data normalization functions
 * @class
 */
class Transformers {
  /**
   * Normalize timestamp to standard format
   * @param {number|Date|string} timestamp - Timestamp to normalize
   * @returns {number} Unix milliseconds
   */
  static normalizeTimestamp(timestamp) {
    if (typeof timestamp === 'number') return timestamp;
    if (timestamp instanceof Date) return timestamp.getTime();
    if (typeof timestamp === 'string') return new Date(timestamp).getTime();
    return Date.now();
  }

  /**
   * Normalize version string to object
   * @param {string} version - Version string (e.g., "1.2.3")
   * @returns {Object} Parsed version
   */
  static normalizeVersion(version) {
    const parts = (version || '0.0.0').split('.');
    return {
      major: parseInt(parts[0]) || 0,
      minor: parseInt(parts[1]) || 0,
      patch: parseInt(parts[2]) || 0,
    };
  }

  /**
   * Normalize metadata
   * @param {Object} metadata - Metadata object
   * @returns {Object} Normalized metadata
   */
  static normalizeMetadata(metadata = {}) {
    return {
      source: metadata.source || 'unknown',
      userId: metadata.userId || null,
      deviceId: metadata.deviceId || null,
      timestamp: this.normalizeTimestamp(metadata.timestamp),
      ...metadata,
    };
  }

  /**
   * Flatten nested object
   * @param {Object} obj - Object to flatten
   * @param {string} prefix - Key prefix
   * @returns {Object} Flattened object
   */
  static flattenObject(obj, prefix = '') {
    const flattened = {};

    for (const [key, value] of Object.entries(obj || {})) {
      const newKey = prefix ? `${prefix}.${key}` : key;

      if (value && typeof value === 'object' && !Array.isArray(value) && !(value instanceof Date)) {
        Object.assign(flattened, this.flattenObject(value, newKey));
      } else {
        flattened[newKey] = value;
      }
    }

    return flattened;
  }

  /**
   * Unflatten object
   * @param {Object} obj - Flattened object
   * @returns {Object} Nested object
   */
  static unflattenObject(obj) {
    const result = {};

    for (const [key, value] of Object.entries(obj || {})) {
      const parts = key.split('.');
      let current = result;

      for (let i = 0; i < parts.length - 1; i++) {
        const part = parts[i];
        if (!(part in current)) {
          current[part] = {};
        }
        current = current[part];
      }

      current[parts[parts.length - 1]] = value;
    }

    return result;
  }

  /**
   * Sanitize string
   * @param {string} str - String to sanitize
   * @returns {string} Sanitized string
   */
  static sanitizeString(str) {
    if (typeof str !== 'string') return '';
    return str.replace(/[<>]/g, '').trim();
  }

  /**
   * Validate required fields
   * @param {Object} data - Data object
   * @param {Array} requiredFields - Required field names
   * @returns {Object} Validation result
   */
  static validateRequired(data, requiredFields) {
    const missing = [];

    for (const field of requiredFields) {
      if (!(field in data) || data[field] === null || data[field] === undefined) {
        missing.push(field);
      }
    }

    return {
      valid: missing.length === 0,
      missing,
    };
  }
}

/**
 * AIAdapter - Transform between Backend and AI service formats
 * @class
 */
class AIAdapter {
  /**
   * Transform backend data to AI format
   * @param {Object} data - Backend data
   * @returns {Object} AI-formatted data
   */
  static toAIFormat(data) {
    return {
      query: data.content || data.text || '',
      context: {
        documentId: data.documentId,
        userId: data.userId,
        language: data.language || 'en',
      },
      parameters: {
        model: data.model || 'default',
        temperature: data.temperature || 0.7,
        maxTokens: data.maxTokens || 500,
      },
      metadata: Transformers.normalizeMetadata(data.metadata),
    };
  }

  /**
   * Transform AI format to backend data
   * @param {Object} aiData - AI service response
   * @returns {Object} Backend-formatted data
   */
  static fromAIFormat(aiData) {
    return {
      suggestionsId: aiData.id || `ai-${Date.now()}`,
      content: aiData.response || aiData.text || '',
      confidence: aiData.confidence || 0.5,
      suggestions: aiData.suggestions || [],
      metadata: {
        model: aiData.model,
        tokensUsed: aiData.tokensUsed,
        executionTime: aiData.executionTime,
        ...aiData.metadata,
      },
    };
  }

  /**
   * Validate AI input
   * @param {Object} data - Data to validate
   * @returns {Object} Validation result
   */
  static validate(data) {
    return Transformers.validateRequired(data, ['content', 'userId']);
  }
}

/**
 * AnalyticsAdapter - Transform between Backend and Analytics formats
 * @class
 */
class AnalyticsAdapter {
  /**
   * Transform backend data to Analytics format
   * @param {Object} data - Backend data
   * @returns {Object} Analytics-formatted data
   */
  static toAnalyticsFormat(data) {
    return {
      event: data.eventName || data.event || '',
      properties: {
        userId: data.userId,
        sessionId: data.sessionId,
        timestamp: Transformers.normalizeTimestamp(data.timestamp),
        source: data.source || 'backend',
        ...data.properties,
      },
      dimensions: {
        environment: data.environment || 'production',
        version: data.version,
        platform: data.platform,
      },
    };
  }

  /**
   * Transform Analytics format to backend data
   * @param {Object} analyticsData - Analytics data
   * @returns {Object} Backend-formatted data
   */
  static fromAnalyticsFormat(analyticsData) {
    return {
      eventName: analyticsData.event,
      userId: analyticsData.properties.userId,
      timestamp: analyticsData.properties.timestamp,
      properties: analyticsData.properties,
      metrics: analyticsData.metrics || {},
    };
  }

  /**
   * Validate analytics input
   * @param {Object} data - Data to validate
   * @returns {Object} Validation result
   */
  static validate(data) {
    return Transformers.validateRequired(data, ['eventName', 'userId']);
  }
}

/**
 * SyncAdapter - Transform between Backend and Sync formats
 * @class
 */
class SyncAdapter {
  /**
   * Transform backend data to Sync format
   * @param {Object} data - Backend data
   * @returns {Object} Sync-formatted data
   */
  static toSyncFormat(data) {
    return {
      resourceId: data.resourceId || data.id,
      deviceId: data.deviceId,
      version: data.version || 1,
      timestamp: Transformers.normalizeTimestamp(data.timestamp),
      content: data.content || data.data,
      contentHash: this.computeHash(data.content || data.data),
      metadata: {
        lastModified: data.lastModified,
        modifiedBy: data.modifiedBy,
        syncStatus: data.syncStatus || 'pending',
      },
    };
  }

  /**
   * Transform Sync format to backend data
   * @param {Object} syncData - Sync data
   * @returns {Object} Backend-formatted data
   */
  static fromSyncFormat(syncData) {
    return {
      id: syncData.resourceId,
      resourceId: syncData.resourceId,
      content: syncData.content,
      version: syncData.version,
      timestamp: syncData.timestamp,
      deviceId: syncData.deviceId,
      lastModified: syncData.metadata.lastModified,
      syncStatus: syncData.metadata.syncStatus,
    };
  }

  /**
   * Validate sync input
   * @param {Object} data - Data to validate
   * @returns {Object} Validation result
   */
  static validate(data) {
    return Transformers.validateRequired(data, ['resourceId', 'deviceId', 'content']);
  }

  /**
   * Compute content hash
   * @private
   */
  static computeHash(content) {
    const crypto = require('crypto');
    return crypto.createHash('sha256').update(JSON.stringify(content)).digest('hex');
  }
}

/**
 * PluginAdapter - Transform between Backend and Plugin formats
 * @class
 */
class PluginAdapter {
  /**
   * Transform backend data to Plugin format
   * @param {Object} data - Backend data
   * @returns {Object} Plugin-formatted data
   */
  static toPluginFormat(data) {
    return {
      pluginId: data.pluginId,
      name: data.name,
      version: Transformers.normalizeVersion(data.version),
      permissions: data.permissions || [],
      manifest: {
        author: data.author,
        description: data.description,
        homepage: data.homepage,
      },
      config: data.config || {},
    };
  }

  /**
   * Transform Plugin format to backend data
   * @param {Object} pluginData - Plugin data
   * @returns {Object} Backend-formatted data
   */
  static fromPluginFormat(pluginData) {
    return {
      pluginId: pluginData.pluginId,
      name: pluginData.name,
      version: `${pluginData.version.major}.${pluginData.version.minor}.${pluginData.version.patch}`,
      permissions: pluginData.permissions,
      author: pluginData.manifest.author,
      description: pluginData.manifest.description,
      config: pluginData.config,
    };
  }

  /**
   * Validate plugin input
   * @param {Object} data - Data to validate
   * @returns {Object} Validation result
   */
  static validate(data) {
    return Transformers.validateRequired(data, ['pluginId', 'name', 'version']);
  }
}

/**
 * PWAAdapter - Transform between Backend and PWA formats
 * @class
 */
class PWAAdapter {
  /**
   * Transform backend data to PWA WebSocket format
   * @param {Object} data - Backend data
   * @returns {Object} PWA-formatted data
   */
  static toWebSocketFormat(data) {
    return {
      type: data.type || 'message',
      id: data.id || `msg-${Date.now()}`,
      payload: {
        content: data.content,
        metadata: data.metadata,
      },
      timestamp: Transformers.normalizeTimestamp(data.timestamp),
      correlationId: data.correlationId,
    };
  }

  /**
   * Transform PWA format to backend data
   * @param {Object} pwaData - PWA data
   * @returns {Object} Backend-formatted data
   */
  static fromWebSocketFormat(pwaData) {
    return {
      id: pwaData.id,
      type: pwaData.type,
      content: pwaData.payload.content,
      metadata: pwaData.payload.metadata,
      timestamp: pwaData.timestamp,
      correlationId: pwaData.correlationId,
    };
  }

  /**
   * Validate PWA input
   * @param {Object} data - Data to validate
   * @returns {Object} Validation result
   */
  static validate(data) {
    return Transformers.validateRequired(data, ['type', 'content']);
  }
}

/**
 * AdapterRegistry - Central adapter management
 * @class
 */
class AdapterRegistry {
  constructor() {
    this.adapters = new Map();
    this.registerDefaultAdapters();
  }

  /**
   * Register default adapters
   * @private
   */
  registerDefaultAdapters() {
    this.register('ai', AIAdapter);
    this.register('analytics', AnalyticsAdapter);
    this.register('sync', SyncAdapter);
    this.register('plugin', PluginAdapter);
    this.register('pwa', PWAAdapter);
  }

  /**
   * Register adapter
   * @param {string} name - Adapter name
   * @param {Object} adapter - Adapter class
   */
  register(name, adapter) {
    this.adapters.set(name, adapter);
  }

  /**
   * Get adapter
   * @param {string} name - Adapter name
   * @returns {Object} Adapter class
   */
  get(name) {
    return this.adapters.get(name);
  }

  /**
   * Transform data using adapter
   * @param {string} adapterName - Adapter name
   * @param {Object} data - Data to transform
   * @param {string} direction - Direction ('to' or 'from')
   * @returns {Object} Transformed data
   */
  transform(adapterName, data, direction = 'to') {
    const adapter = this.get(adapterName);
    if (!adapter) {
      throw new Error(`Unknown adapter: ${adapterName}`);
    }

    const methodName = direction === 'to' ? `to${this.capitalize(adapterName)}Format` : `from${this.capitalize(adapterName)}Format`;
    const method = adapter[methodName];

    if (!method) {
      throw new Error(`Method ${methodName} not found in adapter`);
    }

    return method.call(adapter, data);
  }

  /**
   * Capitalize string
   * @private
   */
  capitalize(str) {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }
}

module.exports = {
  Transformers,
  AIAdapter,
  AnalyticsAdapter,
  SyncAdapter,
  PluginAdapter,
  PWAAdapter,
  AdapterRegistry,
};

/**
 * HELIOS Utils - Shared utilities for all modules
 * Error handling, validation, logging, monitoring
 * v7.0
 */

class Logger {
  constructor(moduleName) {
    this.moduleName = moduleName;
    this.logs = [];
  }

  log(level, message, data = null) {
    const logEntry = {
      timestamp: new Date().toISOString(),
      module: this.moduleName,
      level,
      message,
      data,
    };
    this.logs.push(logEntry);
    if (level === 'error') {
      console.error(`[${this.moduleName}] ${message}`, data);
    } else {
      console.log(`[${this.moduleName}] ${message}`, data || '');
    }
    return logEntry;
  }

  info(message, data) {
    return this.log('info', message, data);
  }

  warn(message, data) {
    return this.log('warn', message, data);
  }

  error(message, data) {
    return this.log('error', message, data);
  }

  debug(message, data) {
    return this.log('debug', message, data);
  }

  getLogs(level = null) {
    return level ? this.logs.filter(l => l.level === level) : this.logs;
  }

  clearLogs() {
    this.logs = [];
  }
}

class ValidationError extends Error {
  constructor(message, field = null) {
    super(message);
    this.name = 'ValidationError';
    this.field = field;
  }
}

class Validator {
  static validateString(value, fieldName, minLength = 1, maxLength = 1000) {
    if (typeof value !== 'string') {
      throw new ValidationError(`${fieldName} must be a string`, fieldName);
    }
    if (value.length < minLength) {
      throw new ValidationError(
        `${fieldName} must be at least ${minLength} characters`,
        fieldName
      );
    }
    if (value.length > maxLength) {
      throw new ValidationError(
        `${fieldName} must be at most ${maxLength} characters`,
        fieldName
      );
    }
    return value.trim();
  }

  static validateNumber(value, fieldName, min = -Infinity, max = Infinity) {
    if (typeof value !== 'number' || isNaN(value)) {
      throw new ValidationError(`${fieldName} must be a number`, fieldName);
    }
    if (value < min || value > max) {
      throw new ValidationError(
        `${fieldName} must be between ${min} and ${max}`,
        fieldName
      );
    }
    return value;
  }

  static validateObject(value, fieldName, requiredKeys = []) {
    if (typeof value !== 'object' || value === null) {
      throw new ValidationError(`${fieldName} must be an object`, fieldName);
    }
    for (const key of requiredKeys) {
      if (!(key in value)) {
        throw new ValidationError(
          `${fieldName} missing required key: ${key}`,
          fieldName
        );
      }
    }
    return value;
  }

  static validateArray(value, fieldName, minLength = 0) {
    if (!Array.isArray(value)) {
      throw new ValidationError(`${fieldName} must be an array`, fieldName);
    }
    if (value.length < minLength) {
      throw new ValidationError(
        `${fieldName} must have at least ${minLength} items`,
        fieldName
      );
    }
    return value;
  }
}

class EventEmitter {
  constructor() {
    this.events = new Map();
  }

  on(eventName, callback) {
    if (!this.events.has(eventName)) {
      this.events.set(eventName, []);
    }
    this.events.get(eventName).push(callback);
    return this;
  }

  off(eventName, callback) {
    if (this.events.has(eventName)) {
      const callbacks = this.events.get(eventName);
      const index = callbacks.indexOf(callback);
      if (index > -1) {
        callbacks.splice(index, 1);
      }
    }
    return this;
  }

  emit(eventName, data) {
    if (this.events.has(eventName)) {
      for (const callback of this.events.get(eventName)) {
        try {
          callback(data);
        } catch (error) {
          console.error(`Error in event ${eventName}:`, error);
        }
      }
    }
    return this;
  }

  once(eventName, callback) {
    const onceWrapper = (data) => {
      callback(data);
      this.off(eventName, onceWrapper);
    };
    this.on(eventName, onceWrapper);
    return this;
  }
}

class CircuitBreaker {
  constructor(operation, options = {}) {
    this.operation = operation;
    this.failureThreshold = options.failureThreshold || 5;
    this.resetTimeout = options.resetTimeout || 60000;
    this.state = 'closed'; // closed, open, half-open
    this.failureCount = 0;
    this.lastFailureTime = null;
    this.nextAttemptTime = null;
  }

  async execute(...args) {
    if (this.state === 'open') {
      if (Date.now() > this.nextAttemptTime) {
        this.state = 'half-open';
      } else {
        throw new Error('Circuit breaker is open');
      }
    }

    try {
      const result = await this.operation(...args);
      this.onSuccess();
      return result;
    } catch (error) {
      this.onFailure();
      throw error;
    }
  }

  onSuccess() {
    this.failureCount = 0;
    this.state = 'closed';
  }

  onFailure() {
    this.failureCount++;
    this.lastFailureTime = Date.now();

    if (this.failureCount >= this.failureThreshold) {
      this.state = 'open';
      this.nextAttemptTime = Date.now() + this.resetTimeout;
    }
  }

  getState() {
    return {
      state: this.state,
      failureCount: this.failureCount,
      lastFailureTime: this.lastFailureTime,
    };
  }
}

class Cache {
  constructor(ttl = 3600000) {
    this.ttl = ttl;
    this.cache = new Map();
  }

  set(key, value) {
    this.cache.set(key, {
      value,
      timestamp: Date.now(),
    });
    return this;
  }

  get(key) {
    if (!this.cache.has(key)) {
      return null;
    }

    const entry = this.cache.get(key);
    if (Date.now() - entry.timestamp > this.ttl) {
      this.cache.delete(key);
      return null;
    }

    return entry.value;
  }

  has(key) {
    return this.get(key) !== null;
  }

  delete(key) {
    return this.cache.delete(key);
  }

  clear() {
    this.cache.clear();
    return this;
  }

  getSize() {
    return this.cache.size;
  }
}

class RetryHandler {
  constructor(options = {}) {
    this.maxRetries = options.maxRetries || 3;
    this.initialDelay = options.initialDelay || 100;
    this.maxDelay = options.maxDelay || 10000;
    this.backoffMultiplier = options.backoffMultiplier || 2;
  }

  async execute(operation, context = null) {
    let lastError;
    let delay = this.initialDelay;

    for (let attempt = 0; attempt <= this.maxRetries; attempt++) {
      try {
        return await operation();
      } catch (error) {
        lastError = error;
        if (attempt < this.maxRetries) {
          await new Promise(resolve => setTimeout(resolve, delay));
          delay = Math.min(delay * this.backoffMultiplier, this.maxDelay);
        }
      }
    }

    throw lastError;
  }

  getConfig() {
    return {
      maxRetries: this.maxRetries,
      initialDelay: this.initialDelay,
      maxDelay: this.maxDelay,
      backoffMultiplier: this.backoffMultiplier,
    };
  }
}

module.exports = {
  Logger,
  Validator,
  ValidationError,
  EventEmitter,
  CircuitBreaker,
  Cache,
  RetryHandler,
};

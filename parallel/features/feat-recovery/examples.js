/**
 * feat-recovery - Usage Examples
 * Demonstrates real-world usage patterns for error recovery
 * @example
 */

const {
  RetryPolicy,
  ExponentialBackoff,
  FallbackStrategy,
  GracefulDegradation
} = require('./implementation');

// ============================================================================
// Example 1: Simple Retry with Exponential Backoff
// ============================================================================
console.log('\n=== Example 1: Simple Retry ===');
{
  const policy = new RetryPolicy({
    maxRetries: 3,
    initialDelay: 500,
    maxDelay: 10000
  });

  let attemptCount = 0;
  const unreliableOperation = async () => {
    attemptCount++;
    console.log(`  Attempt ${attemptCount}...`);
    
    if (attemptCount < 3) {
      const error = new Error('Temporary failure');
      error.code = 'ECONNRESET'; // Network error
      throw error;
    }
    
    return { success: true, data: 'Operation succeeded!' };
  };

  (async () => {
    try {
      const result = await policy.execute(unreliableOperation);
      console.log('Result:', result);
    } catch (error) {
      console.error('Failed:', error.message);
    }
  })();
}

// ============================================================================
// Example 2: Retrying HTTP Requests
// ============================================================================
console.log('\n=== Example 2: HTTP Request Retry ===');
{
  const policy = new RetryPolicy({
    maxRetries: 5,
    initialDelay: 1000,
    retryableStatuses: [408, 429, 500, 502, 503, 504]
  });

  const fetchWithRetry = async (url) => {
    return await policy.execute(async () => {
      // Simulate HTTP response
      const response = {
        status: 503, // Service Unavailable (retryable)
        data: null
      };

      if (response.status >= 400) {
        const error = new Error(`HTTP ${response.status}`);
        error.status = response.status;
        throw error;
      }

      return response.data;
    });
  };

  console.log('Configured to retry on: 408, 429, 500, 502, 503, 504');
  console.log('Will attempt up to 5 times with exponential backoff');
}

// ============================================================================
// Example 3: Exponential Backoff Strategies
// ============================================================================
console.log('\n=== Example 3: Backoff Strategies ===');
{
  const backoff = new ExponentialBackoff({
    baseDelay: 1000,
    maxDelay: 30000,
    multiplier: 2,
    jitterFactor: 0.1
  });

  console.log('Standard Backoff with Jitter:');
  for (let i = 0; i < 4; i++) {
    const delay = backoff.calculateDelay(i);
    console.log(`  Attempt ${i}: ${delay}ms`);
  }

  console.log('\nFull Jitter Strategy (better for thundering herd):');
  for (let i = 0; i < 4; i++) {
    const delay = backoff.calculateDelayFullJitter(i);
    console.log(`  Attempt ${i}: ${delay}ms`);
  }

  console.log('\nEqual Jitter Strategy (balanced):');
  for (let i = 0; i < 4; i++) {
    const delay = backoff.calculateDelayEqualJitter(i);
    console.log(`  Attempt ${i}: ${delay}ms`);
  }
}

// ============================================================================
// Example 4: Fallback to Alternative Service
// ============================================================================
console.log('\n=== Example 4: Fallback Strategy ===');
{
  const fallback = new FallbackStrategy([
    async () => {
      console.log('  Trying primary service...');
      const error = new Error('Primary service down');
      throw error;
    },
    async () => {
      console.log('  Trying secondary service...');
      return { source: 'secondary', data: 'Hello from secondary' };
    },
    async () => {
      console.log('  Using cached response...');
      return { source: 'cache', data: 'Last known response' };
    }
  ]);

  (async () => {
    try {
      const result = await fallback.execute();
      console.log('Success:', result);
      
      const log = fallback.getExecutionLog();
      const successIndex = log.findIndex(e => e.success);
      console.log(`Fallback succeeded with strategy #${successIndex}`);
    } catch (error) {
      console.error('All strategies failed:', error.message);
    }
  })();
}

// ============================================================================
// Example 5: Fallback with Timeout per Strategy
// ============================================================================
console.log('\n=== Example 5: Fallback with Timeout ===');
{
  const strategies = [
    async () => {
      console.log('  Trying slow service (5 second timeout)...');
      await new Promise(resolve => setTimeout(resolve, 10000)); // Slow
      return { data: 'slow service' };
    },
    async () => {
      console.log('  Trying fast service (5 second timeout)...');
      await new Promise(resolve => setTimeout(resolve, 100)); // Fast
      return { data: 'fast service' };
    }
  ];

  const fallback = new FallbackStrategy(strategies);

  (async () => {
    try {
      const result = await fallback.executeWithTimeout(5000); // 5 second timeout
      console.log('Result:', result);
    } catch (error) {
      console.error('Timeout or all failed:', error.message);
    }
  })();
}

// ============================================================================
// Example 6: Circuit Breaker Pattern
// ============================================================================
console.log('\n=== Example 6: Circuit Breaker ===');
{
  class CircuitBreaker {
    constructor(policy, failureThreshold = 5, resetTimeout = 60000) {
      this.policy = policy;
      this.failureThreshold = failureThreshold;
      this.resetTimeout = resetTimeout;
      this.failureCount = 0;
      this.state = 'CLOSED'; // CLOSED, OPEN, HALF_OPEN
      this.lastFailureTime = null;
    }

    async execute(fn) {
      if (this.state === 'OPEN') {
        const elapsed = Date.now() - this.lastFailureTime;
        if (elapsed > this.resetTimeout) {
          this.state = 'HALF_OPEN';
          console.log('  [CB] Half-open: testing recovery...');
        } else {
          throw new Error('Circuit breaker is OPEN');
        }
      }

      try {
        const result = await this.policy.execute(fn);
        if (this.state === 'HALF_OPEN') {
          console.log('  [CB] Closed: recovered!');
          this.state = 'CLOSED';
          this.failureCount = 0;
        }
        return result;
      } catch (error) {
        this.failureCount++;
        this.lastFailureTime = Date.now();

        if (this.failureCount >= this.failureThreshold) {
          this.state = 'OPEN';
          console.log('  [CB] Open: failure threshold reached');
        }
        throw error;
      }
    }

    getStatus() {
      return {
        state: this.state,
        failureCount: this.failureCount,
        lastFailureTime: this.lastFailureTime
      };
    }
  }

  const policy = new RetryPolicy({ maxRetries: 2, initialDelay: 100 });
  const breaker = new CircuitBreaker(policy, 3, 1000);

  console.log('Circuit breaker initialized with 3-failure threshold');
  console.log('Status:', breaker.getStatus());
}

// ============================================================================
// Example 7: Graceful Degradation
// ============================================================================
console.log('\n=== Example 7: Graceful Degradation ===');
{
  const degradation = new GracefulDegradation({
    full: {
      includeDetails: true,
      includeComments: true,
      timeout: 5000
    },
    partial: {
      includeDetails: true,
      includeComments: false,
      timeout: 2000
    },
    minimal: {
      includeDetails: false,
      includeComments: false,
      timeout: 1000
    }
  });

  console.log('Degradation modes configured:');
  console.log('  1. Full: All details and comments (5s timeout)');
  console.log('  2. Partial: Details only (2s timeout)');
  console.log('  3. Minimal: Essentials only (1s timeout)');

  let callCount = 0;
  const fetchData = async (mode) => {
    callCount++;
    if (callCount < 2) {
      throw new Error('Full mode failed (timeout)');
    }
    return {
      id: 123,
      details: mode.includeDetails,
      comments: mode.includeComments ? [...] : undefined
    };
  };

  (async () => {
    try {
      const result = await degradation.execute('full,partial,minimal', fetchData);
      console.log('Data retrieved with mode:', degradation.getStatus().currentMode);
      console.log('Status:', degradation.getStatus());
    } catch (error) {
      console.error('Failed:', error.message);
    }
  })();
}

// ============================================================================
// Example 8: Degradation with Recovery
// ============================================================================
console.log('\n=== Example 8: Degradation Recovery ===');
{
  const degradation = new GracefulDegradation({
    full: { quality: 'high', resources: 'full' },
    reduced: { quality: 'medium', resources: 'limited' },
    minimum: { quality: 'low', resources: 'minimal' }
  });

  (async () => {
    // Simulate operations
    console.log('Initial status:', degradation.getStatus().currentMode);

    // Simulate some failures and successes
    const operations = [
      { success: true, mode: 'full' },
      { success: true, mode: 'full' },
      { success: false, mode: 'full' },
      { success: false, mode: 'reduced' },
      { success: true, mode: 'reduced' },
      { success: true, mode: 'reduced' },
      { success: true, mode: 'reduced' }
    ];

    for (const op of operations) {
      if (op.success) {
        degradation.successCount++;
      } else {
        degradation.failureCount++;
      }
    }

    const status = degradation.getStatus();
    console.log('After operations:');
    console.log(`  Health Ratio: ${(status.healthRatio * 100).toFixed(1)}%`);
    console.log(`  Successes: ${status.successCount}`);
    console.log(`  Failures: ${status.failureCount}`);
    console.log(`  Degraded: ${status.isDegraded}`);
  })();
}

// ============================================================================
// Example 9: Combining Retry and Backoff
// ============================================================================
console.log('\n=== Example 9: Integrated Retry + Backoff ===');
{
  const backoff = new ExponentialBackoff({
    baseDelay: 500,
    maxDelay: 10000,
    multiplier: 2
  });

  let attemptCount = 0;
  const faultyAPI = async () => {
    attemptCount++;
    console.log(`  Attempt ${attemptCount}`);
    
    if (attemptCount < 3) {
      const error = new Error('Temporary error');
      error.code = 'ETIMEDOUT';
      throw error;
    }
    
    return { success: true };
  };

  (async () => {
    try {
      let lastError;
      for (let attempt = 0; attempt < 5; attempt++) {
        try {
          const result = await faultyAPI();
          console.log('Success:', result);
          break;
        } catch (error) {
          lastError = error;
          if (attempt < 4) {
            const delay = backoff.calculateDelay(attempt);
            console.log(`  Waiting ${delay}ms before retry...`);
            await new Promise(resolve => setTimeout(resolve, delay));
          }
        }
      }
    } catch (error) {
      console.error('Failed after retries:', error.message);
    }
  })();
}

// ============================================================================
// Example 10: Real-world Database Retry
// ============================================================================
console.log('\n=== Example 10: Database Connection Retry ===');
{
  const policy = new RetryPolicy({
    maxRetries: 5,
    initialDelay: 1000,
    maxDelay: 30000,
    shouldRetry: (error) => {
      // Retry on temporary database errors
      const retryableErrors = [
        'ECONNREFUSED',
        'ECONNRESET',
        'ETIMEDOUT',
        'PROTOCOL_CONNECTION_LOST',
        'PROTOCOL_ENQUEUE_AFTER_FATAL_ERROR'
      ];
      return retryableErrors.includes(error.code);
    }
  });

  const queryDatabase = async (sql) => {
    return await policy.execute(async () => {
      // Simulate database connection
      console.log('Executing query: ' + sql.substring(0, 30) + '...');
      return { rows: [{ id: 1, name: 'Alice' }] };
    });
  };

  console.log('Database retry policy:');
  console.log('  Max 5 retries with exponential backoff');
  console.log('  Retryable: Connection errors, protocol errors');
}

console.log('\n=== All Examples Complete ===\n');

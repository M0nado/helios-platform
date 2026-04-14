/**
 * @fileoverview Real-world usage examples for mod-retry
 * Demonstrates retry patterns for transient failure recovery
 * 
 * Scenarios covered:
 * - Transient network errors with exponential backoff
 * - API rate limiting with jitter
 * - Database reconnection
 * - Jitter strategies comparison
 * - Custom retry conditions
 */

const { RetryManager, RetryPolicy, JitterStrategy } = require('./index');

// ============================================================================
// EXAMPLE 1: Transient Network Errors
// ============================================================================
console.log('\n=== EXAMPLE 1: Transient Network Errors ===\n');

const networkRetry = new RetryManager({
  name: 'network-call',
  maxAttempts: 5,
  policy: RetryPolicy.EXPONENTIAL,
  baseDelay: 500,
  jitterStrategy: JitterStrategy.DECORRELATED,
  onRetry: (error, attempt, summary) => {
    console.log(`  Retry ${attempt}: ${error.message}`);
    console.log(`    Time elapsed: ${summary.totalElapsed}ms`);
    console.log(`    Attempts remaining: ${summary.attemptsRemaining}`);
  },
  onMaxAttemptsExceeded: (error, summary) => {
    console.log(`✗ Max attempts exceeded after ${summary.totalElapsed}ms`);
  }
});

let attemptCount = 0;
async function unreliableNetworkCall() {
  attemptCount++;
  if (attemptCount <= 2) {
    throw new Error('Network timeout - connection refused');
  }
  return { status: 200, data: 'Success' };
}

(async function exampleNetworkRetry() {
  attemptCount = 0;
  try {
    const result = await networkRetry.execute(() => unreliableNetworkCall());
    console.log('✓ Network call succeeded:', result);
    console.log('Stats:', networkRetry.getStatistics());
  } catch (error) {
    console.error('✗ Final failure:', error.message);
  }
})();

// ============================================================================
// EXAMPLE 2: API Rate Limiting with Jitter
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 2: API Rate Limiting with Jitter ===\n');

  const rateLimit = new RetryManager({
    name: 'rate-limited-api',
    maxAttempts: 4,
    policy: RetryPolicy.EXPONENTIAL,
    baseDelay: 100,
    jitterStrategy: JitterStrategy.FULL,
    shouldRetry: (error, attempt) => {
      // Only retry on rate limiting errors
      return error.code === 'RATE_LIMIT' && attempt < 4;
    }
  });

  let apiAttempts = 0;
  async function rateLimitedAPI() {
    apiAttempts++;
    if (apiAttempts <= 2) {
      const error = new Error('Rate limit exceeded');
      error.code = 'RATE_LIMIT';
      throw error;
    }
    return { data: [1, 2, 3], cached: false };
  }

  (async function exampleRateLimit() {
    apiAttempts = 0;
    try {
      const result = await rateLimit.execute(() => rateLimitedAPI());
      console.log('✓ API call succeeded:', result);
      console.log('Success rate:', rateLimit.getStatistics().successRate);
    } catch (error) {
      console.error('✗ API call failed:', error.message);
    }
  })();
}, 3000);

// ============================================================================
// EXAMPLE 3: Database Reconnection
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 3: Database Reconnection ===\n');

  const dbRetry = new RetryManager({
    name: 'database',
    maxAttempts: 4,
    policy: RetryPolicy.LINEAR,        // Linear growth for DB reconnects
    baseDelay: 200,
    jitterStrategy: JitterStrategy.EQUAL,
    onRetry: (error, attempt, summary) => {
      console.log(`  Reconnection attempt ${attempt}...`);
    }
  });

  let dbAttempts = 0;
  async function databaseOperation() {
    dbAttempts++;
    if (dbAttempts <= 1) {
      throw new Error('Connection refused - database unavailable');
    }
    return { rows: 42, affected: 1 };
  }

  (async function exampleDBRetry() {
    dbAttempts = 0;
    try {
      const result = await dbRetry.execute(() => databaseOperation());
      console.log('✓ Database query succeeded:', result);
    } catch (error) {
      console.error('✗ Database operation failed:', error.message);
    }
  })();
}, 4000);

// ============================================================================
// EXAMPLE 4: Comparing Jitter Strategies
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 4: Jitter Strategies Comparison ===\n');

  const baseDelay = 1000;

  // Full jitter
  const fullJitter = new RetryManager({
    name: 'full-jitter',
    maxAttempts: 1,
    policy: RetryPolicy.EXPONENTIAL,
    baseDelay: baseDelay,
    jitterStrategy: JitterStrategy.FULL
  });

  // Equal jitter
  const equalJitter = new RetryManager({
    name: 'equal-jitter',
    maxAttempts: 1,
    policy: RetryPolicy.EXPONENTIAL,
    baseDelay: baseDelay,
    jitterStrategy: JitterStrategy.EQUAL
  });

  // Decorrelated jitter (AWS style)
  const decorrelatedJitter = new RetryManager({
    name: 'decorrelated-jitter',
    maxAttempts: 1,
    policy: RetryPolicy.EXPONENTIAL,
    baseDelay: baseDelay,
    jitterStrategy: JitterStrategy.DECORRELATED
  });

  console.log('Jitter Configuration:');
  console.log(`Full Jitter:        ${JSON.stringify(fullJitter.jitterCalculator.getStrategyInfo())}`);
  console.log(`Equal Jitter:       ${JSON.stringify(equalJitter.jitterCalculator.getStrategyInfo())}`);
  console.log(`Decorrelated Jitter: ${JSON.stringify(decorrelatedJitter.jitterCalculator.getStrategyInfo())}`);

  // Simulate jitter application
  console.log('\nJitter Application (base delay = 1000ms):');
  const fullDelays = Array(5).fill().map(() => fullJitter.jitterCalculator.apply(baseDelay));
  const equalDelays = Array(5).fill().map(() => equalJitter.jitterCalculator.apply(baseDelay));
  const decorrelatedDelays = Array(5).fill().map(() => decorrelatedJitter.jitterCalculator.apply(baseDelay));

  console.log(`Full Jitter delays:        [${fullDelays.join(', ')}]`);
  console.log(`Equal Jitter delays:       [${equalDelays.join(', ')}]`);
  console.log(`Decorrelated Jitter delays: [${decorrelatedDelays.join(', ')}]`);
}, 5000);

// ============================================================================
// EXAMPLE 5: Backoff Policies Comparison
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 5: Backoff Policies Comparison ===\n');

  const exponentialRetry = new RetryManager({
    policy: RetryPolicy.EXPONENTIAL,
    baseDelay: 100
  });

  const linearRetry = new RetryManager({
    policy: RetryPolicy.LINEAR,
    baseDelay: 100
  });

  const fibonacciRetry = new RetryManager({
    policy: RetryPolicy.FIBONACCI,
    baseDelay: 100
  });

  const fixedRetry = new RetryManager({
    policy: RetryPolicy.FIXED,
    baseDelay: 100
  });

  console.log('Backoff Timeline (5 attempts):');
  console.log('Attempt | Exponential | Linear | Fibonacci | Fixed');
  console.log('--------|-------------|--------|-----------|-------');

  for (let i = 0; i < 5; i++) {
    const exp = exponentialRetry.backoffGenerator.getDelay(i);
    const lin = linearRetry.backoffGenerator.getDelay(i);
    const fib = fibonacciRetry.backoffGenerator.getDelay(i);
    const fix = fixedRetry.backoffGenerator.getDelay(i);
    console.log(`   ${i}    |     ${exp}ms    |  ${lin}ms   |    ${fib}ms     |  ${fix}ms`);
  }
}, 6000);

// ============================================================================
// EXAMPLE 6: Custom Retry Conditions
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 6: Custom Retry Conditions ===\n');

  const selectiveRetry = new RetryManager({
    name: 'selective-retry',
    maxAttempts: 4,
    shouldRetry: (error, attempt) => {
      // Retry only on specific error codes
      const retryableErrors = ['TIMEOUT', 'TEMPORARILY_UNAVAILABLE', 'SERVICE_BUSY'];
      const isRetryable = retryableErrors.includes(error.code);
      
      if (isRetryable && attempt < 4) {
        console.log(`  ↻ Retrying (${error.code})`);
        return true;
      } else if (!isRetryable) {
        console.log(`  ✗ Not retrying (${error.code})`);
        return false;
      } else {
        console.log(`  ✗ Max attempts reached`);
        return false;
      }
    }
  });

  let callCount = 0;
  async function selectiveFailure(shouldSucceed) {
    callCount++;
    if (!shouldSucceed) {
      const error = new Error('Temporary unavailable');
      error.code = 'TEMPORARILY_UNAVAILABLE';
      throw error;
    }
    return { success: true, attempt: callCount };
  }

  (async function exampleSelective() {
    console.log('Scenario 1: Transient error (should retry)');
    callCount = 0;
    try {
      await selectiveRetry.execute(() => selectiveFailure(callCount >= 2));
      console.log(`✓ Success after ${callCount} attempts\n`);
    } catch (error) {
      console.error(`✗ Failed: ${error.message}\n`);
    }

    console.log('Scenario 2: Permanent error (should not retry)');
    callCount = 0;
    try {
      const permanentRetry = new RetryManager({
        shouldRetry: (error) => {
          if (error.code === 'PERMANENT_ERROR') {
            return false;
          }
          return true;
        }
      });

      await permanentRetry.execute(async () => {
        const error = new Error('Permanent failure');
        error.code = 'PERMANENT_ERROR';
        throw error;
      });
    } catch (error) {
      console.log(`✗ Failed immediately (permanent error, no retry)\n`);
    }
  })();
}, 7000);

// ============================================================================
// EXAMPLE 7: Retry Statistics and Monitoring
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 7: Retry Statistics and Monitoring ===\n');

  const monitoredRetry = new RetryManager({
    name: 'monitored-operation',
    maxAttempts: 3,
    baseDelay: 50,
    onRetry: (error, attempt, summary) => {
      console.log(`  [${new Date().toISOString()}] Retry ${attempt}/${summary.totalAttempts}`);
    }
  });

  let operationCount = 0;
  async function operation() {
    operationCount++;
    if (operationCount <= 1) {
      throw new Error('Transient failure');
    }
    return { result: 'success' };
  }

  (async function exampleMonitoring() {
    operationCount = 0;
    try {
      await monitoredRetry.execute(() => operation());
      const stats = monitoredRetry.getStatistics();
      const config = monitoredRetry.getConfiguration();
      const history = monitoredRetry.getLastAttemptHistory();

      console.log('\nStatistics:');
      console.log(`  Total Executions: ${stats.totalExecutions}`);
      console.log(`  Successful: ${stats.successfulExecutions}`);
      console.log(`  Failed: ${stats.failedExecutions}`);
      console.log(`  Total Retries: ${stats.totalRetries}`);
      console.log(`  Success Rate: ${stats.successRate}`);
      console.log(`  Avg Retries/Execution: ${stats.averageRetriesPerExecution}`);

      console.log('\nConfiguration:');
      console.log(`  Name: ${config.name}`);
      console.log(`  Backoff Policy: ${config.backoff.policy}`);
      console.log(`  Jitter Strategy: ${config.jitter.strategy}`);
    } catch (error) {
      console.error('Error:', error.message);
    }
  })();
}, 8000);

console.log('✓ All examples scheduled');

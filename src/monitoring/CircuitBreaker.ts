/**
 * Circuit Breaker Pattern Implementation
 * Prevents cascading failures by monitoring provider health
 * States: Closed (normal) -> Open (failing) -> Half-Open (recovery test) -> Closed
 */

import {
  CircuitBreakerState,
  CircuitBreakerConfig,
  CircuitBreakerStateChange,
  HealthCheckResult
} from './types';

export class CircuitBreaker {
  private state: CircuitBreakerState = CircuitBreakerState.CLOSED;
  private failureCount: number = 0;
  private successCount: number = 0;
  private lastFailureTime: number = 0;
  private nextRetryTime: number = 0;
  private backoffMultiplier: number = 1;
  private readonly config: CircuitBreakerConfig;
  private stateChangeListeners: ((change: CircuitBreakerStateChange) => void)[] = [];

  constructor(
    private readonly providerId: string,
    config: Partial<CircuitBreakerConfig> = {}
  ) {
    this.config = {
      failureThreshold: config.failureThreshold ?? 5,
      successThreshold: config.successThreshold ?? 3,
      timeout: config.timeout ?? 60000,
      initialBackoff: config.initialBackoff ?? 1000,
      maxBackoff: config.maxBackoff ?? 60000
    };
  }

  /**
   * Record a health check result and update circuit breaker state
   */
  recordResult(result: HealthCheckResult): void {
    if (result.status === 'HEALTHY') {
      this.recordSuccess();
    } else {
      this.recordFailure(result.error || 'Unknown error');
    }
  }

  /**
   * Record a successful check
   */
  recordSuccess(): void {
    this.failureCount = 0;

    if (this.state === CircuitBreakerState.HALF_OPEN) {
      this.successCount++;
      if (this.successCount >= this.config.successThreshold) {
        this.transitionTo(CircuitBreakerState.CLOSED, 'Recovery successful');
        this.successCount = 0;
        this.backoffMultiplier = 1;
      }
    } else if (this.state === CircuitBreakerState.CLOSED) {
      this.successCount = 0;
    }
  }

  /**
   * Record a failed check
   */
  recordFailure(reason: string): void {
    this.lastFailureTime = Date.now();
    this.failureCount++;
    this.successCount = 0;

    if (this.state === CircuitBreakerState.HALF_OPEN) {
      this.transitionTo(CircuitBreakerState.OPEN, `Recovery attempt failed: ${reason}`);
      this.scheduleNextRetry();
    } else if (
      this.state === CircuitBreakerState.CLOSED &&
      this.failureCount >= this.config.failureThreshold
    ) {
      this.transitionTo(CircuitBreakerState.OPEN, `Failure threshold exceeded: ${reason}`);
      this.scheduleNextRetry();
    }
  }

  /**
   * Check if the circuit breaker allows requests
   */
  canExecute(): boolean {
    if (this.state === CircuitBreakerState.CLOSED) {
      return true;
    }

    if (this.state === CircuitBreakerState.OPEN) {
      if (Date.now() >= this.nextRetryTime) {
        this.transitionTo(CircuitBreakerState.HALF_OPEN, 'Attempting recovery');
        return true;
      }
      return false;
    }

    // HALF_OPEN: allow request to test recovery
    return true;
  }

  /**
   * Schedule the next retry attempt with exponential backoff
   */
  private scheduleNextRetry(): void {
    const backoff = Math.min(
      this.config.initialBackoff * this.backoffMultiplier,
      this.config.maxBackoff
    );
    this.nextRetryTime = Date.now() + backoff;
    this.backoffMultiplier = Math.min(this.backoffMultiplier * 2, 32);
  }

  /**
   * Transition to a new circuit breaker state
   */
  private transitionTo(newState: CircuitBreakerState, reason: string): void {
    if (newState === this.state) {
      return;
    }

    const change: CircuitBreakerStateChange = {
      providerId: this.providerId,
      oldState: this.state,
      newState,
      timestamp: Date.now(),
      reason
    };

    this.state = newState;
    this.notifyStateChange(change);
  }

  /**
   * Get current state
   */
  getState(): CircuitBreakerState {
    return this.state;
  }

  /**
   * Reset the circuit breaker
   */
  reset(): void {
    this.transitionTo(CircuitBreakerState.CLOSED, 'Manual reset');
    this.failureCount = 0;
    this.successCount = 0;
    this.backoffMultiplier = 1;
    this.nextRetryTime = 0;
  }

  /**
   * Get remaining time until next retry (in milliseconds)
   */
  getTimeUntilRetry(): number {
    if (this.state === CircuitBreakerState.CLOSED) {
      return 0;
    }
    return Math.max(0, this.nextRetryTime - Date.now());
  }

  /**
   * Subscribe to state change events
   */
  onStateChange(listener: (change: CircuitBreakerStateChange) => void): void {
    this.stateChangeListeners.push(listener);
  }

  /**
   * Notify all listeners of state change
   */
  private notifyStateChange(change: CircuitBreakerStateChange): void {
    for (const listener of this.stateChangeListeners) {
      listener(change);
    }
  }

  /**
   * Get detailed state information
   */
  getDetailedState(): {
    state: CircuitBreakerState;
    failureCount: number;
    successCount: number;
    lastFailureTime: number;
    nextRetryTime: number;
    timeUntilRetry: number;
    backoffMultiplier: number;
  } {
    return {
      state: this.state,
      failureCount: this.failureCount,
      successCount: this.successCount,
      lastFailureTime: this.lastFailureTime,
      nextRetryTime: this.nextRetryTime,
      timeUntilRetry: this.getTimeUntilRetry(),
      backoffMultiplier: this.backoffMultiplier
    };
  }
}

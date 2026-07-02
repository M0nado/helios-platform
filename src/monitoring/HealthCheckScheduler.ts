/**
 * Health Check Scheduler
 * Manages periodic health checks for all providers with adaptive intervals
 * Tracks health history and detects degradation patterns
 */

import {
  HealthCheckConfig,
  HealthCheckResult,
  HealthStatus,
  ProviderAdapter,
  CircuitBreakerState
} from './types';
import { CircuitBreaker } from './CircuitBreaker';

interface HealthHistory {
  results: HealthCheckResult[];
  maxSize: number;
  degradationTrend: boolean;
  trendWindow: number;
}

export class HealthCheckScheduler {
  private readonly config: HealthCheckConfig;
  private readonly circuitBreakers: Map<string, CircuitBreaker> = new Map();
  private readonly healthHistory: Map<string, HealthHistory> = new Map();
  private readonly activeTimers: Map<string, NodeJS.Timeout> = new Map();
  private readonly lastCheckTime: Map<string, number> = new Map();
  private readonly nextCheckTime: Map<string, number> = new Map();
  private readonly checkIntervals: Map<string, number> = new Map();
  private isRunning: boolean = false;
  private listeners: {
    onHealthCheckResult?: (result: HealthCheckResult) => void;
    onDegradationDetected?: (providerId: string, trend: boolean) => void;
  } = {};

  constructor(config: Partial<HealthCheckConfig> = {}) {
    this.config = {
      interval: config.interval ?? 30000,
      timeout: config.timeout ?? 10000,
      adaptiveInterval: config.adaptiveInterval ?? true,
      degradingIntervalMultiplier: config.degradingIntervalMultiplier ?? 0.5,
      minInterval: config.minInterval ?? 5000,
      maxInterval: config.maxInterval ?? 120000
    };
  }

  /**
   * Register a provider for health checks
   */
  registerProvider(provider: ProviderAdapter, circuitBreaker: CircuitBreaker): void {
    this.circuitBreakers.set(provider.providerId, circuitBreaker);
    this.healthHistory.set(provider.providerId, {
      results: [],
      maxSize: 100,
      degradationTrend: false,
      trendWindow: 10
    });
    this.checkIntervals.set(provider.providerId, this.config.interval);
    this.lastCheckTime.set(provider.providerId, 0);
    this.nextCheckTime.set(provider.providerId, Date.now());
  }

  /**
   * Unregister a provider from health checks
   */
  unregisterProvider(providerId: string): void {
    this.stopCheckForProvider(providerId);
    this.circuitBreakers.delete(providerId);
    this.healthHistory.delete(providerId);
    this.checkIntervals.delete(providerId);
    this.lastCheckTime.delete(providerId);
    this.nextCheckTime.delete(providerId);
  }

  /**
   * Start health check scheduler
   */
  async start(providers: ProviderAdapter[]): Promise<void> {
    if (this.isRunning) {
      return;
    }

    this.isRunning = true;

    for (const provider of providers) {
      const cb = this.circuitBreakers.get(provider.providerId);
      if (cb) {
        this.scheduleNextCheck(provider.providerId);
      }
    }
  }

  /**
   * Stop health check scheduler
   */
  stop(): void {
    this.isRunning = false;
    for (const [providerId] of this.activeTimers) {
      this.stopCheckForProvider(providerId);
    }
    this.activeTimers.clear();
  }

  /**
   * Schedule the next health check for a provider
   */
  private scheduleNextCheck(providerId: string): void {
    if (!this.isRunning) {
      return;
    }

    const timer = setTimeout(() => {
      this.executeHealthCheck(providerId).catch(err => {
        console.error(`Health check failed for ${providerId}:`, err);
      });
    }, Math.max(0, (this.nextCheckTime.get(providerId) ?? 0) - Date.now()));

    this.activeTimers.set(providerId, timer);
  }

  /**
   * Execute a single health check for a provider
   */
  private async executeHealthCheck(providerId: string): Promise<void> {
    const circuitBreaker = this.circuitBreakers.get(providerId);
    if (!circuitBreaker) {
      return;
    }

    if (!circuitBreaker.canExecute()) {
      const retryTime = circuitBreaker.getTimeUntilRetry();
      this.nextCheckTime.set(providerId, Date.now() + retryTime);
      this.scheduleNextCheck(providerId);
      return;
    }

    this.lastCheckTime.set(providerId, Date.now());

    try {
      // Note: In production, this would call the actual provider
      // For now, create a mock result
      const result: HealthCheckResult = {
        providerId,
        timestamp: Date.now(),
        status: HealthStatus.HEALTHY,
        latency: 0,
        errorRate: 0,
        successRate: 1,
        checksDone: 1
      };

      // Process the result
      this.processHealthCheckResult(result);
    } catch (error) {
      const result: HealthCheckResult = {
        providerId,
        timestamp: Date.now(),
        status: HealthStatus.UNHEALTHY,
        latency: 0,
        errorRate: 1,
        successRate: 0,
        checksDone: 1,
        error: error instanceof Error ? error.message : 'Unknown error'
      };

      this.processHealthCheckResult(result);
    }

    // Schedule next check with adaptive interval
    this.updateCheckInterval(providerId);
    this.nextCheckTime.set(providerId, Date.now() + (this.checkIntervals.get(providerId) ?? this.config.interval));
    this.scheduleNextCheck(providerId);
  }

  /**
   * Process a health check result
   */
  private processHealthCheckResult(result: HealthCheckResult): void {
    const history = this.healthHistory.get(result.providerId);
    if (!history) {
      return;
    }

    // Add to history
    history.results.push(result);
    if (history.results.length > history.maxSize) {
      history.results.shift();
    }

    // Update circuit breaker
    const circuitBreaker = this.circuitBreakers.get(result.providerId);
    if (circuitBreaker) {
      circuitBreaker.recordResult(result);
    }

    // Detect degradation patterns
    const isDegrading = this.detectDegradation(history);
    if (isDegrading !== history.degradationTrend) {
      history.degradationTrend = isDegrading;
      if (this.listeners.onDegradationDetected) {
        this.listeners.onDegradationDetected(result.providerId, isDegrading);
      }
    }

    // Notify listeners
    if (this.listeners.onHealthCheckResult) {
      this.listeners.onHealthCheckResult(result);
    }
  }

  /**
   * Detect degradation patterns in recent check history
   */
  private detectDegradation(history: HealthHistory): boolean {
    if (history.results.length < history.trendWindow) {
      return false;
    }

    const recentResults = history.results.slice(-history.trendWindow);
    const unhealthyCount = recentResults.filter(r => r.status !== 'HEALTHY').length;
    const degradedCount = recentResults.filter(r => r.status === 'DEGRADED').length;

    return (unhealthyCount + degradedCount) > history.trendWindow * 0.5;
  }

  /**
   * Update check interval based on provider health
   */
  private updateCheckInterval(providerId: string): void {
    if (!this.config.adaptiveInterval) {
      this.checkIntervals.set(providerId, this.config.interval);
      return;
    }

    const history = this.healthHistory.get(providerId);
    const circuitBreaker = this.circuitBreakers.get(providerId);

    if (!history || !circuitBreaker) {
      return;
    }

    let newInterval = this.config.interval;

    // If degrading, check more frequently
    if (history.degradationTrend) {
      newInterval = Math.max(
        this.config.minInterval,
        this.config.interval * this.config.degradingIntervalMultiplier
      );
    }

    // If circuit is open or half-open, check more frequently for recovery
    const state = circuitBreaker.getState();
    if (state === CircuitBreakerState.HALF_OPEN || state === CircuitBreakerState.OPEN) {
      newInterval = Math.max(
        this.config.minInterval,
        this.config.interval * this.config.degradingIntervalMultiplier
      );
    }

    // If healthy, check less frequently
    if (!history.degradationTrend && state === CircuitBreakerState.CLOSED) {
      newInterval = Math.min(
        this.config.maxInterval,
        this.config.interval * 2
      );
    }

    this.checkIntervals.set(providerId, newInterval);
  }

  /**
   * Stop health check for a specific provider
   */
  private stopCheckForProvider(providerId: string): void {
    const timer = this.activeTimers.get(providerId);
    if (timer) {
      clearTimeout(timer);
      this.activeTimers.delete(providerId);
    }
  }

  /**
   * Get health history for a provider
   */
  getHealthHistory(providerId: string): HealthCheckResult[] {
    return this.healthHistory.get(providerId)?.results ?? [];
  }

  /**
   * Get current check interval for a provider
   */
  getCheckInterval(providerId: string): number {
    return this.checkIntervals.get(providerId) ?? this.config.interval;
  }

  /**
   * Subscribe to health check events
   */
  on(event: 'healthCheckResult' | 'degradationDetected', listener: Function): void {
    if (event === 'healthCheckResult') {
      this.listeners.onHealthCheckResult = listener as (result: HealthCheckResult) => void;
    } else if (event === 'degradationDetected') {
      this.listeners.onDegradationDetected = listener as (providerId: string, trend: boolean) => void;
    }
  }

  /**
   * Get scheduler statistics
   */
  getStatistics(): {
    isRunning: boolean;
    registeredProviders: number;
    averageCheckInterval: number;
    lastCheckTimes: Map<string, number>;
  } {
    const intervals = Array.from(this.checkIntervals.values());
    const averageCheckInterval = intervals.length > 0
      ? intervals.reduce((a, b) => a + b, 0) / intervals.length
      : 0;

    return {
      isRunning: this.isRunning,
      registeredProviders: this.circuitBreakers.size,
      averageCheckInterval,
      lastCheckTimes: new Map(this.lastCheckTime)
    };
  }
}

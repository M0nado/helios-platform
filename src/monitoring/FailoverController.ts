/**
 * Failover Controller
 * Automatically detects provider failures and triggers failover
 * Implements graceful degradation and rate limiting
 */

import {
  FailoverConfig,
  FailoverEvent,
  ProviderAdapter,
  ProviderHealth,
  HealthStatus,
  CircuitBreakerState
} from './types';
import { CircuitBreaker } from './CircuitBreaker';

export class FailoverController {
  private readonly config: FailoverConfig;
  private readonly circuitBreakers: Map<string, CircuitBreaker> = new Map();
  private readonly failoverTimestamps: Map<string, number> = new Map();
  private activeFailovers: number = 0;
  private lastCascadePreventionTime: number = 0;
  private failoverEvents: FailoverEvent[] = [];
  private listeners: ((event: FailoverEvent) => void)[] = [];
  private providers: Map<string, ProviderAdapter> = new Map();

  constructor(config: Partial<FailoverConfig> = {}) {
    this.config = {
      enabled: config.enabled ?? true,
      maxConcurrentFailovers: config.maxConcurrentFailovers ?? 3,
      failoverDelay: config.failoverDelay ?? 1000,
      cascadePreventionDelay: config.cascadePreventionDelay ?? 5000
    };
  }

  /**
   * Register providers and circuit breakers
   */
  registerProviders(
    providers: ProviderAdapter[],
    circuitBreakers: Map<string, CircuitBreaker>
  ): void {
    for (const provider of providers) {
      this.providers.set(provider.providerId, provider);
      const cb = circuitBreakers.get(provider.providerId);
      if (cb) {
        this.circuitBreakers.set(provider.providerId, cb);
      }
    }
  }

  /**
   * Check if failover should be triggered based on provider health
   */
  shouldTriggerFailover(healthData: ProviderHealth): boolean {
    if (!this.config.enabled) {
      return false;
    }

    // Don't failover if provider is still healthy
    if (healthData.currentStatus === HealthStatus.HEALTHY) {
      return false;
    }

    // Check if circuit breaker is open
    const circuitBreaker = this.circuitBreakers.get(healthData.providerId);
    if (!circuitBreaker || circuitBreaker.getState() !== CircuitBreakerState.OPEN) {
      return false;
    }

    // Check concurrent failover limit
    if (this.activeFailovers >= this.config.maxConcurrentFailovers) {
      return false;
    }

    // Check cascade prevention delay
    const timeSinceLastCascade = Date.now() - this.lastCascadePreventionTime;
    if (timeSinceLastCascade < this.config.cascadePreventionDelay) {
      return false;
    }

    return true;
  }

  /**
   * Execute failover from one provider to another
   */
  async executeFailover(
    fromProvider: ProviderAdapter,
    toProvider: ProviderAdapter | null,
    reason: string
  ): Promise<boolean> {
    if (!this.config.enabled) {
      return false;
    }

    if (this.activeFailovers >= this.config.maxConcurrentFailovers) {
      return false;
    }

    try {
      this.activeFailovers++;
      this.lastCascadePreventionTime = Date.now();

      // Add delay to prevent immediate cascade
      await this.delay(this.config.failoverDelay);

      const targetProviderId = toProvider?.providerId ?? 'NONE';

      // Check if target provider is healthy
      if (toProvider) {
        const canExecute = this.circuitBreakers
          .get(toProvider.providerId)
          ?.canExecute() ?? false;

        if (!canExecute) {
          return false;
        }
      }

      const event: FailoverEvent = {
        timestamp: Date.now(),
        fromProviderId: fromProvider.providerId,
        toProviderId: targetProviderId,
        reason,
        success: true
      };

      this.failoverTimestamps.set(fromProvider.providerId, Date.now());
      this.failoverEvents.push(event);

      // Keep only recent events
      if (this.failoverEvents.length > 1000) {
        this.failoverEvents.shift();
      }

      this.notifyFailover(event);
      return true;
    } catch (error) {
      return false;
    } finally {
      this.activeFailovers--;
    }
  }

  /**
   * Get the next healthy provider
   */
  getNextHealthyProvider(
    currentProvider: ProviderAdapter,
    allProviders: ProviderAdapter[]
  ): ProviderAdapter | null {
    for (const provider of allProviders) {
      if (provider.providerId === currentProvider.providerId) {
        continue;
      }

      const circuitBreaker = this.circuitBreakers.get(provider.providerId);
      if (circuitBreaker && circuitBreaker.canExecute()) {
        return provider;
      }
    }

    return null;
  }

  /**
   * Manually trigger a failover
   */
  async manualFailover(
    fromProviderId: string,
    toProviderId: string | null,
    reason: string
  ): Promise<boolean> {
    const fromProvider = this.providers.get(fromProviderId);
    const toProvider = toProviderId ? this.providers.get(toProviderId) : null;

    if (!fromProvider) {
      return false;
    }

    return this.executeFailover(fromProvider, toProvider ?? null, reason);
  }

  /**
   * Check if a provider recently failed over
   */
  hasRecentlyFailedOver(providerId: string, windowMs: number = 30000): boolean {
    const lastFailoverTime = this.failoverTimestamps.get(providerId);
    if (!lastFailoverTime) {
      return false;
    }

    return Date.now() - lastFailoverTime < windowMs;
  }

  /**
   * Get recent failover events
   */
  getRecentFailovers(maxAge: number = 300000): FailoverEvent[] {
    const cutoffTime = Date.now() - maxAge;
    return this.failoverEvents.filter(event => event.timestamp >= cutoffTime);
  }

  /**
   * Get failover statistics
   */
  getFailoverStats(): {
    totalFailovers: number;
    activeFailovers: number;
    recentFailovers: number;
    failoversByProvider: Map<string, number>;
  } {
    const oneHourAgo = Date.now() - 3600000;
    const recentFailovers = this.failoverEvents.filter(e => e.timestamp >= oneHourAgo);

    const failoversByProvider = new Map<string, number>();
    for (const event of this.failoverEvents) {
      const count = failoversByProvider.get(event.fromProviderId) ?? 0;
      failoversByProvider.set(event.fromProviderId, count + 1);
    }

    return {
      totalFailovers: this.failoverEvents.length,
      activeFailovers: this.activeFailovers,
      recentFailovers: recentFailovers.length,
      failoversByProvider
    };
  }

  /**
   * Reset failover statistics for a provider
   */
  resetFailoverStats(providerId: string): void {
    this.failoverTimestamps.delete(providerId);
    this.failoverEvents = this.failoverEvents.filter(
      event => event.fromProviderId !== providerId
    );
  }

  /**
   * Subscribe to failover events
   */
  onFailover(listener: (event: FailoverEvent) => void): void {
    this.listeners.push(listener);
  }

  /**
   * Notify all listeners of failover event
   */
  private notifyFailover(event: FailoverEvent): void {
    for (const listener of this.listeners) {
      listener(event);
    }
  }

  /**
   * Helper to add delay
   */
  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * Get current failover state
   */
  getState(): {
    enabled: boolean;
    activeFailovers: number;
    maxConcurrentFailovers: number;
    lastCascadePreventionTime: number;
    totalEvents: number;
  } {
    return {
      enabled: this.config.enabled,
      activeFailovers: this.activeFailovers,
      maxConcurrentFailovers: this.config.maxConcurrentFailovers,
      lastCascadePreventionTime: this.lastCascadePreventionTime,
      totalEvents: this.failoverEvents.length
    };
  }
}

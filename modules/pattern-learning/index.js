/**
 * Pattern Learning Module - ENHANCED (Monado Engine)
 * Workload analysis, profile generation, resource optimization
 * Features: Caching, Validation, Error Handling, Logging
 * v7.0
 */

const { Logger, Validator, Cache, EventEmitter } = require('../utils');

class PatternLearning extends EventEmitter {
  constructor(config = {}) {
    super();
    this.config = config;
    this.logger = new Logger('PatternLearning');
    this.patterns = new Map();
    this.profiles = new Map();
    this.history = [];
    this.profileCache = new Cache(3600000); // 1 hour cache
    this.logger.info('Pattern Learning module initialized');
  }

  learnPattern(workload) {
    try {
      Validator.validateObject(workload, 'workload');
      
      const pattern = {
        id: `pattern_${Date.now()}`,
        type: workload.type || 'unknown',
        cpu: Validator.validateNumber(workload.cpu || 0, 'cpu', 0, 100),
        memory: Validator.validateNumber(workload.memory || 0, 'memory', 0, 100),
        disk: Validator.validateNumber(workload.disk || 0, 'disk', 0, 100),
        network: Validator.validateNumber(workload.network || 0, 'network', 0, 100),
        timestamp: new Date(),
      };

      this.patterns.set(pattern.id, pattern);
      this.history.push(pattern);
      
      this.logger.info(`Pattern learned: ${pattern.type}`, {
        cpu: pattern.cpu,
        memory: pattern.memory,
      });
      this.emit('pattern-learned', pattern);
      return pattern;
    } catch (error) {
      this.logger.error('Failed to learn pattern', { error: error.message });
      this.emit('error', { action: 'learnPattern', error });
      throw error;
    }
  }

  generateProfile(workloadType) {
    try {
      const type = Validator.validateString(workloadType, 'workloadType');
      
      // Check cache first
      const cached = this.profileCache.get(type);
      if (cached) {
        this.logger.debug(`Profile cache hit: ${type}`);
        return cached;
      }

      // Analyze learned patterns to build profile
      const relevantPatterns = Array.from(this.patterns.values())
        .filter(p => p.type === type);

      let profile;
      if (relevantPatterns.length > 0) {
        const avg = {
          cpu: relevantPatterns.reduce((sum, p) => sum + p.cpu, 0) / relevantPatterns.length,
          memory: relevantPatterns.reduce((sum, p) => sum + p.memory, 0) / relevantPatterns.length,
          disk: relevantPatterns.reduce((sum, p) => sum + p.disk, 0) / relevantPatterns.length,
          network: relevantPatterns.reduce((sum, p) => sum + p.network, 0) / relevantPatterns.length,
        };

        profile = {
          type,
          cpu: Math.round(avg.cpu),
          memory: Math.round(avg.memory),
          disk: Math.round(avg.disk),
          network: Math.round(avg.network),
          basedOnPatterns: relevantPatterns.length,
          recommendations: this._generateRecommendations(avg),
          generated: new Date(),
        };
      } else {
        profile = {
          type,
          cpu: 50,
          memory: 50,
          disk: 40,
          network: 25,
          basedOnPatterns: 0,
          recommendations: [
            'Allocate balanced resources initially',
            'Monitor workload patterns to optimize',
            'Enable adaptive scaling if available',
          ],
          generated: new Date(),
        };
      }

      this.profiles.set(type, profile);
      this.profileCache.set(type, profile);
      
      this.logger.info(`Profile generated for ${type}`, { patterns: profile.basedOnPatterns });
      this.emit('profile-generated', profile);
      return profile;
    } catch (error) {
      this.logger.error('Failed to generate profile', { error: error.message });
      this.emit('error', { action: 'generateProfile', error });
      throw error;
    }
  }

  _generateRecommendations(metrics) {
    const recommendations = [];

    if (metrics.cpu > 80) {
      recommendations.push('Allocate HIGH CPU resources (8+ cores)');
    } else if (metrics.cpu > 60) {
      recommendations.push('Allocate MODERATE CPU resources (4+ cores)');
    }

    if (metrics.memory > 80) {
      recommendations.push('Reserve HIGH memory (16+ GB)');
    } else if (metrics.memory > 60) {
      recommendations.push('Reserve MODERATE memory (8+ GB)');
    }

    if (metrics.disk > 70) {
      recommendations.push('Use NVMe SSD for optimal disk performance');
    }

    if (metrics.network > 70) {
      recommendations.push('Configure high-bandwidth network (1Gbps+)');
    } else {
      recommendations.push('Standard network configuration sufficient');
    }

    if (recommendations.length === 0) {
      recommendations.push('Balanced resource allocation recommended');
    }

    return recommendations;
  }

  classifyWorkload(workload) {
    try {
      Validator.validateObject(workload, 'workload');
      
      const cpu = workload.cpu || 0;
      const memory = workload.memory || 0;
      const disk = workload.disk || 0;

      if (cpu > 80 && memory > 70) {
        return 'compute_intensive';
      }
      if (disk > 70 || (cpu > 60 && disk > 50)) {
        return 'io_intensive';
      }
      if (cpu > 70 && memory < 50) {
        return 'cpu_bound';
      }
      if (memory > 75 && cpu < 50) {
        return 'memory_intensive';
      }
      return 'balanced';
    } catch (error) {
      this.logger.error('Failed to classify workload', { error: error.message });
      throw error;
    }
  }

  recommendResources(workload) {
    try {
      const classification = this.classifyWorkload(workload);
      
      const recommendations = {
        compute_intensive: {
          cpu: 'HIGH (8+ cores recommended)',
          memory: 'HIGH (16+ GB)',
          gpu: 'OPTIONAL (beneficial)',
          disk: 'SSD/NVMe',
        },
        io_intensive: {
          cpu: 'MODERATE (4-6 cores)',
          disk: 'NVMe SSD (fast I/O)',
          memory: 'MODERATE (8+ GB)',
          network: 'HIGH_BANDWIDTH',
        },
        cpu_bound: {
          cpu: 'HIGH (8+ cores)',
          memory: 'MODERATE (8 GB)',
          disk: 'Standard',
        },
        memory_intensive: {
          memory: 'HIGH (32+ GB)',
          cpu: 'MODERATE (4+ cores)',
          disk: 'NVMe SSD for swapping',
        },
        balanced: {
          cpu: 'MODERATE (4 cores)',
          memory: 'MODERATE (8 GB)',
          disk: 'SSD',
        },
      };

      const result = recommendations[classification];
      this.logger.info(`Resources recommended for ${classification}`, result);
      this.emit('resources-recommended', { classification, recommendations: result });
      return result;
    } catch (error) {
      this.logger.error('Failed to recommend resources', { error: error.message });
      throw error;
    }
  }

  getPatternHistory(limit = 100) {
    return this.history.slice(-limit);
  }

  getProfileByType(type) {
    try {
      const typeStr = Validator.validateString(type, 'type');
      return this.profiles.get(typeStr) || null;
    } catch (error) {
      this.logger.error('Failed to retrieve profile', { error: error.message });
      throw error;
    }
  }

  clearCache() {
    const size = this.profileCache.getSize();
    this.profileCache.clear();
    this.logger.info(`Profile cache cleared (${size} entries removed)`);
    return size;
  }

  getMetrics() {
    return {
      module: 'pattern-learning',
      patternsLearned: this.patterns.size,
      profilesGenerated: this.profiles.size,
      historySize: this.history.length,
      cacheSize: this.profileCache.getSize(),
      logCount: this.logger.getLogs().length,
      timestamp: Date.now(),
    };
  }
}

module.exports = { PatternLearning };

/**
 * HELIOS v4.0 Cache Manager Module
 * @module mod-cache
 */

const { CacheManager, TTLManager, EvictionPolicy, DistributedCache } = require('./implementation');

module.exports = {
  CacheManager,
  TTLManager,
  EvictionPolicy,
  DistributedCache,
};

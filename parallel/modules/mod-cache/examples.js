/**
 * HELIOS v4.0 Cache Manager - Usage Examples
 */

const { CacheManager } = require('./index');

// ============================================================================
// Example 1: Basic Cache Operations
// ============================================================================
console.log('=== Example 1: Basic Cache Operations ===\n');

const basicCache = new CacheManager({
  maxSize: 1000,
  defaultTTL: 60000, // 1 minute
  evictionPolicy: 'LRU'
});

basicCache.set('user:1001', { name: 'Alice', role: 'admin' });
basicCache.set('user:1002', { name: 'Bob', role: 'user' });

console.log('Retrieved user:1001:', basicCache.get('user:1001'));
console.log('Cache has user:1002:', basicCache.has('user:1002'));
console.log('Cache size:', basicCache.size());

// ============================================================================
// Example 2: TTL Expiration
// ============================================================================
console.log('\n=== Example 2: TTL Expiration ===\n');

const ttlCache = new CacheManager({
  defaultTTL: 2000 // 2 seconds
});

ttlCache.set('session:abc123', { userId: 1001, token: 'xyz789' });
console.log('Session exists:', ttlCache.has('session:abc123'));

// Wait for expiration
setTimeout(() => {
  console.log('Session after 3 seconds:', ttlCache.get('session:abc123')); // undefined
}, 3000);

// ============================================================================
// Example 3: Custom TTL per Entry
// ============================================================================
console.log('\n=== Example 3: Custom TTL per Entry ===\n');

const customTTLCache = new CacheManager();

customTTLCache.set('temp:data1', 'short-lived', 1000); // 1 second
customTTLCache.set('perm:data2', 'long-lived', 3600000); // 1 hour

console.log('Temp data:', customTTLCache.get('temp:data1'));
console.log('Perm data:', customTTLCache.get('perm:data2'));

// ============================================================================
// Example 4: Eviction Policies
// ============================================================================
console.log('\n=== Example 4: Eviction Policies ===\n');

// LRU (Least Recently Used)
const lruCache = new CacheManager({
  maxSize: 3,
  evictionPolicy: 'LRU'
});

lruCache.set('a', 1);
lruCache.set('b', 2);
lruCache.set('c', 3);
console.log('LRU cache before eviction:', lruCache.keys());

lruCache.set('d', 4); // 'a' gets evicted
console.log('LRU cache after eviction:', lruCache.keys());

// LFU (Least Frequently Used)
const lfuCache = new CacheManager({
  maxSize: 3,
  evictionPolicy: 'LFU'
});

lfuCache.set('x', 1);
lfuCache.get('x'); // Increase frequency
lfuCache.get('x'); // Increase frequency
lfuCache.set('y', 2);
lfuCache.set('z', 3);
console.log('LFU cache before eviction:', lfuCache.keys());

lfuCache.set('w', 4); // 'y' gets evicted (least frequent)
console.log('LFU cache after eviction:', lfuCache.keys());

// FIFO (First In First Out)
const fifoCache = new CacheManager({
  maxSize: 3,
  evictionPolicy: 'FIFO'
});

fifoCache.set('first', 1);
fifoCache.set('second', 2);
fifoCache.set('third', 3);
console.log('FIFO cache before eviction:', fifoCache.keys());

fifoCache.set('fourth', 4); // 'first' gets evicted
console.log('FIFO cache after eviction:', fifoCache.keys());

// ============================================================================
// Example 5: Cache Statistics and Monitoring
// ============================================================================
console.log('\n=== Example 5: Cache Statistics ===\n');

const statsCache = new CacheManager();

statsCache.set('key1', 'value1');
statsCache.set('key2', 'value2');

// Record some hits and misses
statsCache.get('key1'); // hit
statsCache.get('key1'); // hit
statsCache.get('key3'); // miss
statsCache.get('key4'); // miss

const stats = statsCache.getStats();
console.log('Cache Statistics:');
console.log(`  Hits: ${stats.hits}`);
console.log(`  Misses: ${stats.misses}`);
console.log(`  Hit Rate: ${stats.hitRate}`);
console.log(`  Size: ${stats.size}/${stats.maxSize}`);
console.log(`  Evictions: ${stats.evictions}`);

// ============================================================================
// Example 6: Cache Warmup
// ============================================================================
console.log('\n=== Example 6: Cache Warmup ===\n');

const warmupCache = new CacheManager();

const initialData = {
  'config:api.url': 'https://api.example.com',
  'config:api.key': 'sk_prod_abc123',
  'config:timeout': 30000,
  'config:retries': 3
};

warmupCache.warmup(initialData, 3600000); // 1 hour TTL
console.log('Cache loaded with', warmupCache.size(), 'config entries');
console.log('API URL:', warmupCache.get('config:api.url'));
console.log('API Timeout:', warmupCache.get('config:timeout'));

// ============================================================================
// Example 7: Distributed Caching
// ============================================================================
console.log('\n=== Example 7: Distributed Caching ===\n');

const cache1 = new CacheManager({
  distributed: true,
  nodeId: 'node-1'
});

const cache2 = new CacheManager({
  distributed: true,
  nodeId: 'node-2'
});

// Listen for sync events from other nodes
cache1.onDistributedSync((syncEvent) => {
  console.log(`Node 1 received sync from ${syncEvent.nodeId}: ${syncEvent.operation} ${syncEvent.key}`);
});

cache2.onDistributedSync((syncEvent) => {
  console.log(`Node 2 received sync from ${syncEvent.nodeId}: ${syncEvent.operation} ${syncEvent.key}`);
});

// Node 1 sets a value
cache1.set('shared:data', { value: 'important' });

// Simulate network sync
const syncEvents = [
  {
    nodeId: 'node-1',
    operation: 'set',
    key: 'shared:data',
    value: { value: 'important' },
    timestamp: Date.now()
  }
];

// Node 2 syncs with Node 1
cache2.syncWithPeers(syncEvents);
console.log('Node 2 synced. Data:', cache2.get('shared:data'));

// ============================================================================
// Example 8: Wildcard Key Patterns
// ============================================================================
console.log('\n=== Example 8: Wildcard Key Patterns ===\n');

const appCache = new CacheManager();

appCache.set('user:1001:profile', { name: 'Alice' });
appCache.set('user:1001:settings', { theme: 'dark' });
appCache.set('user:1002:profile', { name: 'Bob' });
appCache.set('post:5001:data', { title: 'Hello World' });

const userKeys = appCache.keys().filter(k => k.startsWith('user:'));
console.log('User-related keys:', userKeys);

const user1001Keys = appCache.keys().filter(k => k.startsWith('user:1001:'));
console.log('User 1001 keys:', user1001Keys);

// ============================================================================
// Example 9: Cache Invalidation Patterns
// ============================================================================
console.log('\n=== Example 9: Cache Invalidation ===\n');

const invalidationCache = new CacheManager();

// Prime cache with user data
invalidationCache.set('user:1001:friends', ['Alice', 'Charlie']);
invalidationCache.set('user:1001:profile', { name: 'Bob', age: 30 });

console.log('Initial state:');
console.log('  Friends:', invalidationCache.get('user:1001:friends'));

// User adds a friend - invalidate related cache
invalidationCache.delete('user:1001:friends');
console.log('After adding friend (cache invalidated):');
console.log('  Friends:', invalidationCache.get('user:1001:friends')); // undefined

// Clear all user data for ID 1001
invalidationCache.keys().forEach(key => {
  if (key.startsWith('user:1001:')) {
    invalidationCache.delete(key);
  }
});
console.log('After clearing all user:1001 keys, cache size:', invalidationCache.size());

// ============================================================================
// Example 10: Memory-Efficient Cache for Large Datasets
// ============================================================================
console.log('\n=== Example 10: Large Dataset Caching ===\n');

const largeCache = new CacheManager({
  maxSize: 10000,
  evictionPolicy: 'LFU',
  defaultTTL: 3600000
});

// Simulate loading product data
const products = [];
for (let i = 0; i < 100; i++) {
  const productId = `product:${i}`;
  const productData = {
    id: i,
    name: `Product ${i}`,
    price: Math.random() * 1000,
    inventory: Math.floor(Math.random() * 500)
  };
  largeCache.set(productId, productData);
  products.push(productId);
}

console.log('Loaded', largeCache.size(), 'products into cache');

// Access some products (increase their frequency)
for (let i = 0; i < 10; i++) {
  largeCache.get(`product:${i}`);
}

// Load more products (will trigger LFU eviction for less-used items)
for (let i = 100; i < 10050; i++) {
  const productId = `product:${i}`;
  largeCache.set(productId, { id: i, name: `Product ${i}`, price: Math.random() * 1000 });
}

const finalStats = largeCache.getStats();
console.log('Final cache state:');
console.log(`  Size: ${finalStats.size}/${finalStats.maxSize}`);
console.log(`  Evictions: ${finalStats.evictions}`);

// ============================================================================
// Example 11: Real-World: API Response Caching
// ============================================================================
console.log('\n=== Example 11: API Response Caching ===\n');

const apiCache = new CacheManager({
  maxSize: 500,
  defaultTTL: 300000, // 5 minutes
  evictionPolicy: 'LRU'
});

async function fetchUserWithCache(userId) {
  const cacheKey = `api:user:${userId}`;

  // Check cache first
  let user = apiCache.get(cacheKey);
  if (user) {
    console.log(`  ✓ Cache hit for user ${userId}`);
    return user;
  }

  // Simulate API call
  console.log(`  ✗ Cache miss, fetching user ${userId} from API...`);
  await new Promise(r => setTimeout(r, 100)); // Simulate network delay

  user = {
    id: userId,
    name: `User ${userId}`,
    email: `user${userId}@example.com`,
    fetchedAt: new Date().toISOString()
  };

  // Cache response
  apiCache.set(cacheKey, user, 300000); // 5 minute TTL

  return user;
}

// Demonstrate caching
(async () => {
  console.log('First request for user 1:');
  await fetchUserWithCache(1);

  console.log('Second request for user 1:');
  await fetchUserWithCache(1);

  console.log('Request for user 2:');
  await fetchUserWithCache(2);
})();

// ============================================================================
// Example 12: Cache with Error Handling
// ============================================================================
console.log('\n=== Example 12: Cache Error Handling ===\n');

const safeCache = new CacheManager({
  maxSize: 100
});

function safeGet(cache, key, defaultValue = null) {
  try {
    const value = cache.get(key);
    return value !== undefined ? value : defaultValue;
  } catch (error) {
    console.error(`Error retrieving key "${key}":`, error.message);
    return defaultValue;
  }
}

function safeSet(cache, key, value, ttl = null) {
  try {
    cache.set(key, value, ttl);
    return true;
  } catch (error) {
    console.error(`Error setting key "${key}":`, error.message);
    return false;
  }
}

safeSet(safeCache, 'config:timeout', 5000);
console.log('Config timeout:', safeGet(safeCache, 'config:timeout', 3000));
console.log('Missing key:', safeGet(safeCache, 'config:missing', 'default-value'));

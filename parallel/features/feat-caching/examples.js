/**
 * feat-caching - Usage Examples
 * Demonstrates real-world usage patterns for HTTP caching
 * @example
 */

const {
  HTTPCache,
  ETagManager,
  CacheControl,
  ConditionalRequest
} = require('./implementation');

// ============================================================================
// Example 1: Cache Hit/Miss Pattern
// ============================================================================
console.log('\n=== Example 1: Cache Hit/Miss ===');
{
  const cache = new HTTPCache();
  const url = 'https://api.example.com/users/123';

  // First request - cache miss
  let cached = cache.get(url);
  console.log('First request:', cached); // null

  // Store in cache
  cache.set(url, {
    data: { id: 123, name: 'Alice Johnson', email: 'alice@example.com' },
    headers: { 'content-type': 'application/json' },
    status: 200
  }, { ttl: 5 * 60 * 1000 }); // 5 minutes

  // Second request - cache hit
  cached = cache.get(url);
  console.log('Second request:', cached.data); // User object
  console.log('Cache stats:', cache.getStats()); // { hits: 1, misses: 1, ... }
}

// ============================================================================
// Example 2: ETag Validation
// ============================================================================
console.log('\n=== Example 2: ETag Validation ===');
{
  const manager = new ETagManager();
  const userData = JSON.stringify({ id: 123, name: 'Alice', lastUpdate: '2024-01-15' });
  
  // Generate ETag for response
  const responseETag = manager.generate(userData);
  console.log('Generated ETag:', responseETag);

  // Simulate client storing ETag and making second request
  const clientStoredETag = responseETag;
  const currentData = userData; // Same content
  const currentETag = manager.generate(currentData);

  // Check if content changed
  if (manager.matches(clientStoredETag, currentETag)) {
    console.log('Content unchanged - would return 304 Not Modified');
  }

  // Simulate content change
  const updatedData = JSON.stringify({ id: 123, name: 'Alice', lastUpdate: '2024-01-16' });
  const updatedETag = manager.generate(updatedData);

  if (!manager.matches(clientStoredETag, updatedETag)) {
    console.log('Content changed - return 200 with new ETag');
  }

  // Weak ETag support
  const weakManager = new ETagManager({ weak: true });
  const weakETag = weakManager.generate(userData);
  console.log('Weak ETag:', weakETag);
}

// ============================================================================
// Example 3: Conditional Request Handling
// ============================================================================
console.log('\n=== Example 3: Conditional Requests ===');
{
  const conditional = new ConditionalRequest();
  const currentETag = '"v2-abc123"';
  const lastModified = new Date('2024-01-15');

  // Scenario 1: Client checks If-None-Match
  const clientETag = '"v2-abc123"'; // Same ETag
  if (conditional.checkIfNoneMatch(clientETag, currentETag)) {
    const response = conditional.buildResponse(
      { ifNoneMatch: true, ifModifiedSince: false },
      currentETag,
      lastModified.toUTCString()
    );
    console.log('Scenario 1 - If-None-Match match:', response.status); // 304
  }

  // Scenario 2: Client checks If-Modified-Since
  const clientIfModifiedSince = new Date('2024-01-15');
  if (conditional.checkIfModifiedSince(clientIfModifiedSince, lastModified)) {
    console.log('Scenario 2 - Not modified since check passed - return 304');
  }

  // Scenario 3: Conditional update with If-Match
  if (conditional.checkIfMatch('"v2-abc123"', currentETag)) {
    console.log('Scenario 3 - Precondition met, allow update');
  } else {
    console.log('Scenario 3 - Precondition failed, return 412');
  }

  // Scenario 4: Conditional update with If-Unmodified-Since
  const clientIfUnmodifiedSince = new Date('2024-01-20'); // Future date
  if (conditional.checkIfUnmodifiedSince(clientIfUnmodifiedSince, lastModified)) {
    console.log('Scenario 4 - Resource unmodified since check passed, allow update');
  }
}

// ============================================================================
// Example 4: Cache Control Headers
// ============================================================================
console.log('\n=== Example 4: Cache Control Headers ===');
{
  const cc = new CacheControl();

  // Parse incoming Cache-Control header
  const incomingHeader = 'public, max-age=3600, must-revalidate, s-maxage=86400';
  const directives = cc.parse(incomingHeader);
  console.log('Parsed directives:', directives);

  // Check if cacheable
  if (cc.isCacheable(directives, 200)) {
    console.log('Response is cacheable for 1 hour (client) and 1 day (proxy)');
  }

  // Check if needs revalidation
  if (cc.needsRevalidation(directives)) {
    console.log('Must revalidate with origin server');
  }

  // Generate outgoing Cache-Control header
  const responseDirectives = {
    public: true,
    'max-age': 3600,
    's-maxage': 86400,
    'must-revalidate': true
  };
  const outgoingHeader = cc.generate(responseDirectives);
  console.log('Generated header:', outgoingHeader);

  // Different caching strategies
  const strategies = {
    immutable: cc.generate({ public: true, 'max-age': 31536000, immutable: true }),
    shortLived: cc.generate({ public: true, 'max-age': 60 }),
    private: cc.generate({ private: true, 'max-age': 3600 }),
    noCache: cc.generate({ 'no-cache': true })
  };

  console.log('Cache strategies:');
  console.log('  Immutable (1 year):', strategies.immutable);
  console.log('  Short-lived (1 min):', strategies.shortLived);
  console.log('  Private (1 hour):', strategies.private);
  console.log('  No-cache:', strategies.noCache);
}

// ============================================================================
// Example 5: Cache Invalidation Patterns
// ============================================================================
console.log('\n=== Example 5: Cache Invalidation ===');
{
  const cache = new HTTPCache();

  // Populate cache with multiple resources
  cache.set('api/users/1', { data: { name: 'Alice' }, status: 200 });
  cache.set('api/users/2', { data: { name: 'Bob' }, status: 200 });
  cache.set('api/users/3', { data: { name: 'Charlie' }, status: 200 });
  cache.set('api/posts/1', { data: { title: 'First Post' }, status: 200 });
  cache.set('api/posts/2', { data: { title: 'Second Post' }, status: 200 });

  console.log('Cache entries before invalidation:', cache.cache.size);

  // Invalidate all user-related entries
  const userInvalidated = cache.invalidateByPattern(/^api\/users\//);
  console.log(`Invalidated ${userInvalidated} user entries`);
  console.log('Cache entries after invalidation:', cache.cache.size);

  // Verify posts still cached
  console.log('Posts still cached:', cache.get('api/posts/1') !== null);

  // Invalidate everything
  cache.clear();
  console.log('Cache cleared, entries:', cache.cache.size);
}

// ============================================================================
// Example 6: Cache Statistics and Hit Ratio
// ============================================================================
console.log('\n=== Example 6: Cache Statistics ===');
{
  const cache = new HTTPCache({ maxSize: 1048576, maxEntries: 100 });

  const data = {
    data: { content: 'x'.repeat(1000) },
    headers: { 'content-type': 'text/plain' },
    status: 200
  };

  // Simulate cache activity
  cache.set('url1', data);
  cache.set('url2', data);
  cache.set('url3', data);

  cache.get('url1'); // Hit
  cache.get('url1'); // Hit
  cache.get('url2'); // Hit
  cache.get('url4'); // Miss
  cache.get('url5'); // Miss
  cache.get('url1'); // Hit

  const stats = cache.getStats();
  const ratio = cache.getHitRatio();

  console.log('Cache Statistics:');
  console.log(`  Entries: ${stats.hits + stats.misses}`);
  console.log(`  Hits: ${stats.hits}`);
  console.log(`  Misses: ${stats.misses}`);
  console.log(`  Hit Ratio: ${(ratio * 100).toFixed(1)}%`);
  console.log(`  Size: ${stats.size} bytes`);
  console.log(`  Evictions: ${stats.evictions}`);
}

// ============================================================================
// Example 7: Practical HTTP Middleware Pattern
// ============================================================================
console.log('\n=== Example 7: HTTP Middleware Pattern ===');
{
  class HTTPCacheMiddleware {
    constructor() {
      this.cache = new HTTPCache();
      this.etag = new ETagManager();
      this.cacheControl = new CacheControl();
      this.conditional = new ConditionalRequest();
    }

    async handleRequest(request) {
      const url = request.url;

      // Check cache
      const cached = this.cache.get(url);
      if (cached && !this.cache.isStale(url)) {
        // Check conditional headers
        if (request.headers['if-none-match']) {
          if (this.conditional.checkIfNoneMatch(
            request.headers['if-none-match'],
            cached.etag
          )) {
            return {
              status: 304,
              headers: { etag: cached.etag }
            };
          }
        }
        return cached;
      }

      // Fetch from origin
      const response = await this.fetchFromOrigin(url);

      // Generate ETag
      const etag = this.etag.generate(JSON.stringify(response.data));

      // Parse Cache-Control
      const cacheDirectives = this.cacheControl.parse(
        response.headers['cache-control'] || 'public, max-age=3600'
      );

      // Store in cache if cacheable
      if (this.cacheControl.isCacheable(cacheDirectives, response.status)) {
        const ttl = (cacheDirectives['max-age'] || 3600) * 1000;
        response.etag = etag;
        this.cache.set(url, response, { ttl });
      }

      return {
        ...response,
        headers: {
          ...response.headers,
          etag
        }
      };
    }

    async fetchFromOrigin(url) {
      // Simulate origin fetch
      return {
        data: { message: 'Data from origin' },
        headers: { 'cache-control': 'public, max-age=3600' },
        status: 200
      };
    }
  }

  const middleware = new HTTPCacheMiddleware();

  // Simulate request
  (async () => {
    const response = await middleware.handleRequest({
      url: 'https://api.example.com/data',
      headers: {}
    });
    console.log('Response:', response.status, response.data);

    // Second request with If-None-Match
    const response2 = await middleware.handleRequest({
      url: 'https://api.example.com/data',
      headers: { 'if-none-match': response.headers.etag }
    });
    console.log('Second response:', response2.status); // 304
  })();
}

// ============================================================================
// Example 8: Cache Freshness Lifecycle
// ============================================================================
console.log('\n=== Example 8: Cache Freshness Lifecycle ===');
{
  const cache = new HTTPCache();
  const url = 'api/data';

  // Store with short TTL
  cache.set(url, { data: 'fresh', status: 200 }, { ttl: 100 }); // 100ms

  console.log('Just cached - Fresh:', cache.isFresh(url)); // true
  console.log('Just cached - Stale:', cache.isStale(url)); // false

  // Wait for expiration
  setTimeout(() => {
    console.log('After 150ms - Fresh:', cache.isFresh(url)); // false
    console.log('After 150ms - Stale:', cache.isStale(url)); // true
    console.log('Expired entry:', cache.get(url)); // null
  }, 150);
}

console.log('\n=== All Examples Complete ===\n');

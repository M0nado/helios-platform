# feat-caching - HTTP Response Caching Module

Production-ready HTTP caching implementation with ETag support, cache-control headers, and conditional request handling. Fully compliant with RFC 7232 and RFC 7234.

## Features

- **HTTPCache**: In-memory HTTP response caching with configurable TTL and size limits
- **ETagManager**: Generate and validate HTTP entity tags (ETags) for cache validation
- **CacheControl**: Parse and generate RFC 7234 Cache-Control headers
- **ConditionalRequest**: Handle conditional requests (If-None-Match, If-Modified-Since, etc.)
- **100% JSDoc**: Full API documentation on every function
- **Production Ready**: Comprehensive error handling and validation

## Installation

```bash
npm install @helios/feat-caching
```

## Quick Start

```javascript
const { HTTPCache, ETagManager, CacheControl, ConditionalRequest } = require('@helios/feat-caching');

// Create cache instance
const cache = new HTTPCache({ maxSize: 104857600, maxEntries: 10000 });

// Store response in cache
cache.set('https://api.example.com/users', {
  data: { users: [...] },
  headers: { 'content-type': 'application/json' },
  status: 200
}, { ttl: 3600000 }); // 1 hour

// Retrieve from cache
const cached = cache.get('https://api.example.com/users');
if (cached && !cache.isStale('https://api.example.com/users')) {
  console.log('Cache hit:', cached.data);
}

// Generate ETag
const etag = new ETagManager().generate(JSON.stringify(userData));

// Handle conditional requests
const conditional = new ConditionalRequest();
if (conditional.checkIfNoneMatch(ifNoneMatchHeader, currentETag)) {
  // Return 304 Not Modified
}
```

## API Reference

### HTTPCache

Core HTTP cache implementation with LRU eviction and TTL support.

#### Constructor

```javascript
new HTTPCache(options)
```

**Options:**
- `maxSize` (number): Maximum cache size in bytes. Default: 104857600 (100MB)
- `maxEntries` (number): Maximum number of entries. Default: 10000
- `defaultTTL` (number): Default time-to-live in milliseconds. Default: 3600000 (1 hour)

**Performance:** O(1) average cache operations, O(n) for pattern-based invalidation

#### Methods

**set(key, value, options) → boolean**

Store a response in cache.

```javascript
const success = cache.set(
  'https://api.example.com/data',
  { data: responseBody, headers: responseHeaders, status: 200 },
  { ttl: 3600000, isPrivate: false }
);
```

**get(key) → Object|null**

Retrieve cached response. Returns null if expired or missing.

```javascript
const cached = cache.get('https://api.example.com/data');
if (cached) {
  console.log(cached.data);
  console.log(cached.headers);
  console.log(cached.status);
}
```

**isFresh(key) → boolean**

Check if entry exists and is not expired.

```javascript
if (cache.isFresh('https://api.example.com/data')) {
  console.log('Cache is fresh');
}
```

**isStale(key) → boolean**

Check if entry exists but is expired.

```javascript
if (cache.isStale('https://api.example.com/data')) {
  console.log('Cache needs revalidation');
}
```

**invalidate(key) → boolean**

Remove specific cache entry.

```javascript
cache.invalidate('https://api.example.com/data');
```

**invalidateByPattern(pattern) → number**

Invalidate all entries matching a regex or string pattern.

```javascript
const count = cache.invalidateByPattern(/^https:\/\/api\.example\.com\//);
console.log(`Invalidated ${count} entries`);
```

**clear() → void**

Clear entire cache.

```javascript
cache.clear();
```

**getStats() → Object**

Get cache statistics.

```javascript
const stats = cache.getStats();
// { hits: 1000, misses: 200, evictions: 50, size: 5242880 }
```

**getHitRatio() → number**

Calculate hit ratio (0-1).

```javascript
const ratio = cache.getHitRatio(); // 0.83 (83% hit rate)
```

### ETagManager

HTTP ETag generation and validation.

#### Constructor

```javascript
new ETagManager(options)
```

**Options:**
- `weak` (boolean): Generate weak ETags. Default: false

**Performance:** O(1) ETag operations

#### Methods

**generate(content) → string**

Generate ETag from content.

```javascript
const etag = new ETagManager().generate(JSON.stringify(data));
// Returns: "123abc456" or W/"123abc456" (weak)
```

**matches(etagA, etagB) → boolean**

Check if ETags match (strong comparison).

```javascript
const manager = new ETagManager();
if (manager.matches('"abc123"', '"abc123"')) {
  console.log('ETags match exactly');
}
```

**weakMatches(etagA, etagB) → boolean**

Check if ETags match (weak comparison, ignores W/ prefix).

```javascript
if (manager.weakMatches('W/"abc123"', '"abc123"')) {
  console.log('ETags match weakly');
}
```

**isWeak(etag) → boolean**

Check if ETag is weak.

```javascript
if (manager.isWeak('W/"abc123"')) {
  console.log('Weak ETag');
}
```

### CacheControl

Parse and generate Cache-Control headers.

#### Constructor

```javascript
new CacheControl()
```

#### Methods

**parse(headerValue) → Object**

Parse Cache-Control header value.

```javascript
const cc = new CacheControl();
const directives = cc.parse('public, max-age=3600, must-revalidate');
// { public: true, 'max-age': 3600, 'must-revalidate': true }
```

**generate(config) → string**

Generate Cache-Control header value.

```javascript
const cc = new CacheControl();
const header = cc.generate({
  public: true,
  'max-age': 3600,
  'must-revalidate': true
});
// Returns: "public, max-age=3600, must-revalidate"
```

**isCacheable(cacheControl, status) → boolean**

Check if response is cacheable based on directives and status.

```javascript
const cc = new CacheControl();
const directives = cc.parse('public, max-age=3600');
if (cc.isCacheable(directives, 200)) {
  console.log('Response is cacheable');
}
```

**needsRevalidation(cacheControl) → boolean**

Check if cache entry needs revalidation.

```javascript
const directives = cc.parse('must-revalidate');
if (cc.needsRevalidation(directives)) {
  console.log('Must revalidate with server');
}
```

### ConditionalRequest

Handle conditional HTTP requests per RFC 7232.

#### Constructor

```javascript
new ConditionalRequest()
```

#### Methods

**checkIfNoneMatch(etagHeader, currentETag) → boolean**

Check If-None-Match condition (304 if true).

```javascript
const conditional = new ConditionalRequest();
if (conditional.checkIfNoneMatch(ifNoneMatchHeader, currentETag)) {
  return { status: 304, headers: { etag: currentETag } };
}
```

**checkIfModifiedSince(ifModifiedSince, lastModified) → boolean**

Check If-Modified-Since condition (304 if true).

```javascript
if (conditional.checkIfModifiedSince(ifModifiedSinceDate, lastModifiedDate)) {
  return { status: 304 };
}
```

**checkIfMatch(etagHeader, currentETag) → boolean**

Check If-Match condition for updates/deletes.

```javascript
if (!conditional.checkIfMatch(ifMatchHeader, currentETag)) {
  return { status: 412, body: 'Precondition Failed' };
}
```

**checkIfUnmodifiedSince(ifUnmodifiedSince, lastModified) → boolean**

Check If-Unmodified-Since condition for updates/deletes.

```javascript
if (!conditional.checkIfUnmodifiedSince(ifUnmodifiedSinceDate, lastModifiedDate)) {
  return { status: 412 };
}
```

**buildResponse(conditions, etag, lastModified) → Object**

Build complete response for conditional request.

```javascript
const response = conditional.buildResponse(
  {
    ifNoneMatch: true,
    ifModifiedSince: false
  },
  currentETag,
  lastModifiedDate
);
// Returns: { status: 304, headers: {...}, body: null }
```

## Usage Examples

### Cache Hit/Miss Example

```javascript
const cache = new HTTPCache();

// First request - cache miss
const data1 = cache.get('user:123');
console.log(data1); // null

// Store in cache
cache.set('user:123', {
  data: { id: 123, name: 'John' },
  headers: { 'content-type': 'application/json' },
  status: 200
});

// Second request - cache hit
const data2 = cache.get('user:123');
console.log(data2.data); // { id: 123, name: 'John' }
console.log(cache.getStats()); // { hits: 1, misses: 1, ... }
```

### ETag Validation Example

```javascript
const manager = new ETagManager();
const userData = JSON.stringify({ id: 123, name: 'John' });
const etag = manager.generate(userData);

// Client sends If-None-Match with stored ETag
const clientETag = '"abc123"';
const currentETag = manager.generate(userData);

if (manager.matches(clientETag, currentETag)) {
  // Return 304 Not Modified
  console.log('Content unchanged');
} else {
  // Return full response with new ETag
  console.log('Content updated, send new version');
}
```

### Conditional Request Example

```javascript
const conditional = new ConditionalRequest();
const currentETag = '"v2-abc123"';
const lastModified = new Date('2024-01-15');

// Client requests with If-None-Match
const response = conditional.buildResponse(
  {
    ifNoneMatch: conditional.checkIfNoneMatch(currentETag, currentETag),
    ifModifiedSince: conditional.checkIfModifiedSince(
      new Date('2024-01-15'),
      lastModified
    )
  },
  currentETag,
  lastModified.toUTCString()
);

console.log(response.status); // 304
console.log(response.body); // null
```

### Cache Invalidation Example

```javascript
const cache = new HTTPCache();

// Populate cache with multiple URLs
cache.set('api/users/1', { data: { name: 'Alice' }, status: 200 });
cache.set('api/users/2', { data: { name: 'Bob' }, status: 200 });
cache.set('api/posts/1', { data: { title: 'Post 1' }, status: 200 });

// Invalidate all user-related entries
const invalidated = cache.invalidateByPattern(/^api\/users\//);
console.log(`Invalidated ${invalidated} entries`); // 2

// Verify posts still cached
console.log(cache.get('api/posts/1').data); // { title: 'Post 1' }
```

### Cache Control Header Example

```javascript
const cc = new CacheControl();

// Parse incoming header
const directives = cc.parse('public, max-age=3600, must-revalidate');

// Check if cacheable
if (cc.isCacheable(directives, 200)) {
  console.log('Cacheable response');
}

// Generate outgoing header
const header = cc.generate({
  public: true,
  'max-age': 3600,
  'must-revalidate': true
});
console.log(header); // "public, max-age=3600, must-revalidate"
```

## Performance Characteristics

- **Cache Hit**: O(1) average time
- **Cache Miss**: O(1) average time
- **ETag Generation**: O(n) where n = content size (hash computation)
- **ETag Comparison**: O(1) constant time
- **Invalidation**: O(1) for single entry, O(n) for pattern matching
- **Memory Usage**: ~100 bytes per cache entry + content size

## Error Handling

All methods include comprehensive error handling:

```javascript
try {
  const cache = new HTTPCache({ maxSize: -1 }); // Invalid
} catch (error) {
  console.error('Configuration error:', error.message);
}

try {
  cache.set(123, { data: 'value' }); // Key must be string
} catch (error) {
  console.error('Type error:', error.message);
}

try {
  const etag = manager.generate(null); // Invalid content
} catch (error) {
  console.error('Validation error:', error.message);
}
```

## License

MIT

/**
 * API Response Optimizer for HELIOS v4.0
 * Implements response compression, pagination, field selection, caching headers, and streaming
 * Performance Target: <300ms p99 latency (from 500ms)
 */

const zlib = require('zlib');
const { Transform } = require('stream');

class ResponseOptimizer {
  constructor(config = {}) {
    this.config = {
      gzipThreshold: config.gzipThreshold || 1024, // Compress if > 1KB
      defaultPageSize: config.defaultPageSize || 20,
      maxPageSize: config.maxPageSize || 100,
      compressionLevel: config.compressionLevel || 6,
      cacheDuration: config.cacheDuration || 3600,
      ...config,
    };
    this.metrics = {
      responsesCompressed: 0,
      bytesBeforeCompression: 0,
      bytesAfterCompression: 0,
      responsesPaginated: 0,
      fieldSelectionsUsed: 0,
      streamingResponses: 0,
    };
  }

  /**
   * Gzip Compression
   * Compresses response body for responses > 1KB
   */
  async compressResponse(data, headers = {}) {
    const serialized = typeof data === 'string' ? data : JSON.stringify(data);
    const buffer = Buffer.from(serialized, 'utf-8');

    // Check if client accepts gzip
    const acceptEncoding = headers['accept-encoding'] || '';
    const shouldCompress = (!acceptEncoding || acceptEncoding.includes('gzip')) && buffer.length > this.config.gzipThreshold;

    if (!shouldCompress) {
      return {
        body: buffer,
        headers: { 'Content-Type': 'application/json' },
        compressed: false,
        originalSize: buffer.length,
      };
    }

    return new Promise((resolve, reject) => {
      zlib.gzip(buffer, { level: this.config.compressionLevel }, (err, compressed) => {
        if (err) reject(err);

        this.metrics.responsesCompressed++;
        this.metrics.bytesBeforeCompression += buffer.length;
        this.metrics.bytesAfterCompression += compressed.length;

        resolve({
          body: compressed,
          headers: {
            'Content-Type': 'application/json',
            'Content-Encoding': 'gzip',
            'Content-Length': compressed.length,
          },
          compressed: true,
          originalSize: buffer.length,
          compressedSize: compressed.length,
          compressionRatio: (100 * (1 - compressed.length / buffer.length)).toFixed(2),
        });
      });
    });
  }

  /**
   * Response Pagination
   * Splits large datasets into manageable pages
   */
  paginate(data, query = {}) {
    const page = Math.max(1, parseInt(query.page || 1, 10));
    const limit = Math.min(
      parseInt(query.limit || this.config.defaultPageSize, 10),
      this.config.maxPageSize
    );

    const offset = (page - 1) * limit;
    const total = data.length;
    const items = data.slice(offset, offset + limit);
    const totalPages = Math.ceil(total / limit);
    const hasNextPage = page < totalPages;
    const hasPrevPage = page > 1;

    this.metrics.responsesPaginated++;

    return {
      data: items,
      pagination: {
        page,
        limit,
        offset,
        total,
        totalPages,
        hasNextPage,
        hasPrevPage,
      },
      links: {
        self: `/api/resource?page=${page}&limit=${limit}`,
        first: `/api/resource?page=1&limit=${limit}`,
        last: `/api/resource?page=${totalPages}&limit=${limit}`,
        next: hasNextPage ? `/api/resource?page=${page + 1}&limit=${limit}` : null,
        prev: hasPrevPage ? `/api/resource?page=${page - 1}&limit=${limit}` : null,
      },
    };
  }

  /**
   * Field Selection / Projection
   * Allows clients to request only needed fields
   */
  selectFields(data, fields = null) {
    if (!fields) return data;

    this.metrics.fieldSelectionsUsed++;

    const fieldList = fields.split(',').map(f => f.trim());
    const isArray = Array.isArray(data);
    const items = isArray ? data : [data];

    const projected = items.map(item => {
      const result = {};
      fieldList.forEach(field => {
        if (field in item) result[field] = item[field];
      });
      return result;
    });

    return isArray ? projected : projected[0];
  }

  /**
   * Response Caching Headers
   * Sets appropriate Cache-Control headers for different response types
   */
  getCacheHeaders(responseType = 'dynamic', ttl = null) {
    const duration = ttl || this.config.cacheDuration;
    
    const strategies = {
      static: {
        'Cache-Control': 'public, max-age=31536000, immutable',
        'Expires': new Date(Date.now() + 31536000000).toUTCString(),
      },
      dynamic: {
        'Cache-Control': `public, max-age=${duration}, s-maxage=${Math.floor(duration * 1.5)}`,
        'Expires': new Date(Date.now() + duration * 1000).toUTCString(),
      },
      private: {
        'Cache-Control': `private, max-age=${Math.floor(duration / 2)}, must-revalidate`,
      },
      noCache: {
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Pragma': 'no-cache',
        'Expires': '0',
      },
      conditional: {
        'Cache-Control': `public, max-age=0, must-revalidate, proxy-revalidate`,
        'ETag': this._generateETag(),
      },
    };

    return strategies[responseType] || strategies.dynamic;
  }

  /**
   * Streaming Responses
   * Streams large datasets using NDJSON (newline-delimited JSON)
   */
  createStreamingResponse(data, options = {}) {
    const self = this;

    const transformStream = new Transform({
      transform(chunk, encoding, callback) {
        try {
          const item = JSON.parse(chunk.toString());
          // Optionally apply field selection
          const projected = options.fields 
            ? self.selectFields(item, options.fields)
            : item;
          
          this.push(JSON.stringify(projected) + '\n');
          callback();
        } catch (err) {
          callback(err);
        }
      },
    });

    this.metrics.streamingResponses++;

    return {
      stream: transformStream,
      headers: {
        'Content-Type': 'application/x-ndjson',
        'Transfer-Encoding': 'chunked',
        'Cache-Control': 'no-cache, no-store, must-revalidate',
      },
    };
  }

  /**
   * Build complete response with optimization
   */
  async buildOptimizedResponse(data, options = {}) {
    const {
      compress = true,
      paginate = false,
      fields = null,
      cacheType = 'dynamic',
      stream = false,
      pagination: paginationQuery = {},
      headers = {},
    } = options;

    let response = data;

    // Apply pagination
    if (paginate) {
      response = this.paginate(response, paginationQuery);
    }

    // Apply field selection
    if (fields) {
      response.data = this.selectFields(response.data || response, fields);
    }

    // For streaming
    if (stream && Array.isArray(response)) {
      return this.createStreamingResponse(response, { fields });
    }

    // Compress response
    let compressed = response;
    if (compress) {
      compressed = await this.compressResponse(response, headers);
    }

    // Add cache headers
    const cacheHeaders = this.getCacheHeaders(cacheType);

    return {
      body: compressed.body || compressed,
      headers: {
        ...(typeof compressed === 'object' ? compressed.headers : { 'Content-Type': 'application/json' }),
        ...cacheHeaders,
        'X-Response-Time': `${Date.now()}ms`,
      },
      metadata: {
        compressed: compressed.compressed || false,
        originalSize: compressed.originalSize,
        compressedSize: compressed.compressedSize,
        compressionRatio: compressed.compressionRatio,
      },
    };
  }

  /**
   * Performance Metrics
   */
  getMetrics() {
    const compressionSavings = this.metrics.bytesBeforeCompression > 0
      ? (100 * (1 - this.metrics.bytesAfterCompression / this.metrics.bytesBeforeCompression)).toFixed(2)
      : 0;

    return {
      responsesCompressed: this.metrics.responsesCompressed,
      totalBytesBeforeCompression: this.metrics.bytesBeforeCompression,
      totalBytesAfterCompression: this.metrics.bytesAfterCompression,
      averageCompressionSavings: `${compressionSavings}%`,
      responsesPaginated: this.metrics.responsesPaginated,
      fieldSelectionsUsed: this.metrics.fieldSelectionsUsed,
      streamingResponses: this.metrics.streamingResponses,
    };
  }

  _generateETag() {
    return `"${Math.random().toString(36).substr(2, 9)}"`;
  }
}

module.exports = ResponseOptimizer;

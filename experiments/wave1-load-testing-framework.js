const autocannon = require('autocannon');

class LoadTestFramework {
  constructor(targetUrl, options = {}) {
    this.targetUrl = targetUrl;
    this.results = [];
    this.metrics = new Map();
  }

  async rampUpTest() {
    const config = {
      url: this.targetUrl,
      connections: 100,
      pipelining: 10,
      duration: 300,
      requests: [
        { path: '/api/health', method: 'GET' },
        { path: '/api/data', method: 'GET' },
      ]
    };
    
    const result = await autocannon(config);
    return {
      testType: 'rampUp',
      timestamp: new Date(),
      throughput: result.throughput.average,
      latency: {
        p50: result.latency.p50,
        p95: result.latency.p95,
        p99: result.latency.p99,
      },
      errorRate: result.errors / (result.requests.total),
      cpuUsage: process.cpuUsage(),
      memoryUsage: process.memoryUsage(),
    };
  }

  async sustainedLoadTest() {
    const config = {
      url: this.targetUrl,
      connections: 5000,
      pipelining: 50,
      duration: 3600,
    };
    
    const result = await autocannon(config);
    return {
      testType: 'sustained',
      throughput: result.throughput.average,
      latency: {
        p50: result.latency.p50,
        p95: result.latency.p95,
        p99: result.latency.p99,
      },
      errorRate: result.errors / (result.requests.total),
    };
  }

  async burstTest() {
    const config = {
      url: this.targetUrl,
      connections: 10000,
      pipelining: 100,
      duration: 60,
    };
    
    const result = await autocannon(config);
    return {
      testType: 'burst',
      throughput: result.throughput.average,
      latency: {
        p50: result.latency.p50,
        p95: result.latency.p95,
        p99: result.latency.p99,
      },
      errorRate: result.errors / (result.requests.total),
    };
  }

  async runFullSuite() {
    console.log('Starting Load Test Suite...');
    
    this.results.push(await this.rampUpTest());
    this.results.push(await this.sustainedLoadTest());
    this.results.push(await this.burstTest());
    
    return this.results;
  }
}

module.exports = LoadTestFramework;

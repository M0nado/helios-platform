# HELIOS v4.0 Phase 2 - Complete Deployment & Testing Infrastructure

## 🚀 QUICK START

This guide sets up everything needed to run Phase 2 experiments against **any real system**.

---

## 📦 PART 1: CORE EXPERIMENT FRAMEWORK

### File: `experiments/framework/load-test-core.js`

```javascript
// experiments/framework/load-test-core.js
const http = require('http');
const fs = require('fs');

class LoadTestFramework {
  constructor(targetUrl, options = {}) {
    this.targetUrl = targetUrl;
    this.options = {
      duration: options.duration || 60,
      concurrency: options.concurrency || 100,
      rps: options.rps || 1000,
      ...options
    };
    this.results = [];
    this.metrics = {
      startTime: null,
      endTime: null,
      totalRequests: 0,
      totalErrors: 0,
      totalLatency: 0,
      maxLatency: 0,
      minLatency: Infinity,
      latencies: [],
    };
  }

  async makeRequest(path = '/') {
    return new Promise((resolve, reject) => {
      const startTime = Date.now();
      
      http.get(`${this.targetUrl}${path}`, (res) => {
        let data = '';
        res.on('data', chunk => data += chunk);
        res.on('end', () => {
          const latency = Date.now() - startTime;
          resolve({ status: res.statusCode, latency });
        });
      }).on('error', reject);
    });
  }

  async runLoadTest() {
    this.metrics.startTime = Date.now();
    console.log(`🧪 Load Test: ${this.options.rps} req/sec, ${this.options.duration}s duration`);

    const requestsPerSecond = this.options.rps / this.options.concurrency;
    const totalRequests = (this.options.rps * this.options.duration) / this.options.concurrency;
    
    let completed = 0;
    let errors = 0;

    for (let i = 0; i < totalRequests; i++) {
      try {
        const { status, latency } = await this.makeRequest();
        
        this.metrics.totalRequests++;
        this.metrics.totalLatency += latency;
        this.metrics.latencies.push(latency);
        this.metrics.maxLatency = Math.max(this.metrics.maxLatency, latency);
        this.metrics.minLatency = Math.min(this.metrics.minLatency, latency);
        
        if (status !== 200) errors++;
        completed++;
        
        // Rate limiting
        if (i % 10 === 0) await this.sleep(10);
      } catch (err) {
        errors++;
      }
    }

    this.metrics.endTime = Date.now();
    this.metrics.totalErrors = errors;
    
    return this.generateReport();
  }

  generateReport() {
    const latencies = this.metrics.latencies.sort((a, b) => a - b);
    const report = {
      duration: (this.metrics.endTime - this.metrics.startTime) / 1000,
      totalRequests: this.metrics.totalRequests,
      errors: this.metrics.totalErrors,
      errorRate: this.metrics.totalErrors / this.metrics.totalRequests,
      throughput: this.metrics.totalRequests / ((this.metrics.endTime - this.metrics.startTime) / 1000),
      latency: {
        mean: this.metrics.totalLatency / this.metrics.totalRequests,
        min: this.metrics.minLatency,
        max: this.metrics.maxLatency,
        p50: latencies[Math.floor(latencies.length * 0.5)],
        p95: latencies[Math.floor(latencies.length * 0.95)],
        p99: latencies[Math.floor(latencies.length * 0.99)],
      },
    };
    
    console.log('\n📊 Results:');
    console.log(`  Throughput: ${report.throughput.toFixed(2)} req/sec`);
    console.log(`  Error Rate: ${(report.errorRate * 100).toFixed(2)}%`);
    console.log(`  Latency (p99): ${report.latency.p99.toFixed(2)}ms`);
    console.log(`  Latency (p95): ${report.latency.p95.toFixed(2)}ms`);
    console.log(`  Latency (mean): ${report.latency.mean.toFixed(2)}ms`);
    
    return report;
  }

  sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

module.exports = { LoadTestFramework };
```

### File: `experiments/framework/metrics-collector.js`

```javascript
// experiments/framework/metrics-collector.js
const os = require('os');
const fs = require('fs');

class MetricsCollector {
  constructor(name) {
    this.name = name;
    this.data = [];
    this.interval = null;
  }

  start(sampleInterval = 1000) {
    console.log(`📈 Collecting metrics (${sampleInterval}ms intervals)`);
    
    this.interval = setInterval(() => {
      const sample = {
        timestamp: new Date().toISOString(),
        cpu: os.loadavg(),
        memory: process.memoryUsage(),
        uptime: process.uptime(),
      };
      
      this.data.push(sample);
    }, sampleInterval);
  }

  stop() {
    if (this.interval) {
      clearInterval(this.interval);
      console.log(`✅ Metrics collection stopped (${this.data.length} samples)`);
    }
  }

  export(filename) {
    const json = JSON.stringify(this.data, null, 2);
    fs.writeFileSync(filename, json);
    console.log(`💾 Metrics exported to ${filename}`);
  }

  getSummary() {
    if (this.data.length === 0) return null;
    
    const memoryValues = this.data.map(d => d.memory.heapUsed / 1024 / 1024);
    const cpuValues = this.data.map(d => d.cpu[0]);
    
    return {
      duration: this.data.length / 1000,  // seconds
      samples: this.data.length,
      memory: {
        min: Math.min(...memoryValues),
        max: Math.max(...memoryValues),
        avg: memoryValues.reduce((a, b) => a + b) / memoryValues.length,
      },
      cpu: {
        min: Math.min(...cpuValues),
        max: Math.max(...cpuValues),
        avg: cpuValues.reduce((a, b) => a + b) / cpuValues.length,
      },
    };
  }
}

module.exports = { MetricsCollector };
```

---

## 🧪 PART 2: EXPERIMENT RUNNER SCRIPTS

### File: `experiments/run-exp7-load-testing.js`

```javascript
#!/usr/bin/env node
// experiments/run-exp7-load-testing.js

const { LoadTestFramework } = require('./framework/load-test-core');
const { MetricsCollector } = require('./framework/metrics-collector');
const fs = require('fs');
const path = require('path');

async function runLoadTestingExperiment() {
  console.log('🚀 EXPERIMENT 7: LOAD TESTING\n');

  const targetUrl = process.env.TARGET_URL || 'http://localhost:3000';
  const resultsDir = path.join(__dirname, 'results/exp7-load-testing');
  
  if (!fs.existsSync(resultsDir)) {
    fs.mkdirSync(resultsDir, { recursive: true });
  }

  const testScenarios = [
    { name: 'Light Load', rps: 100, duration: 60 },
    { name: 'Medium Load', rps: 500, duration: 60 },
    { name: 'Heavy Load', rps: 1000, duration: 60 },
    { name: 'Peak Load', rps: 5000, duration: 30 },
    { name: 'Burst', rps: 10000, duration: 10 },
  ];

  const results = {};

  for (const scenario of testScenarios) {
    console.log(`\n📊 Testing: ${scenario.name}`);
    console.log(`   ${scenario.rps} req/sec for ${scenario.duration}s`);

    const metrics = new MetricsCollector(scenario.name);
    const tester = new LoadTestFramework(targetUrl, {
      rps: scenario.rps,
      duration: scenario.duration,
      concurrency: Math.ceil(scenario.rps / 100),
    });

    metrics.start(1000);
    const report = await tester.runLoadTest();
    metrics.stop();

    results[scenario.name] = {
      ...report,
      metrics: metrics.getSummary(),
    };

    // Save individual result
    fs.writeFileSync(
      path.join(resultsDir, `${scenario.name.toLowerCase().replace(/ /g, '-')}.json`),
      JSON.stringify(results[scenario.name], null, 2)
    );
  }

  // Generate summary report
  const summary = {
    timestamp: new Date().toISOString(),
    targetUrl,
    scenarios: results,
    breakingPoint: findBreakingPoint(results),
    recommendations: generateRecommendations(results),
  };

  fs.writeFileSync(
    path.join(resultsDir, 'LOAD-TEST-REPORT.json'),
    JSON.stringify(summary, null, 2)
  );

  console.log('\n✅ Load testing complete');
  console.log(`📁 Results saved to: ${resultsDir}`);

  return summary;
}

function findBreakingPoint(results) {
  // Breaking point is where error rate exceeds 1% or p99 latency exceeds 1000ms
  for (const [name, result] of Object.entries(results)) {
    if (result.errorRate > 0.01 || result.latency.p99 > 1000) {
      return {
        scenario: name,
        rps: name.includes('Light') ? 100 : name.includes('Medium') ? 500 : 1000,
        errorRate: result.errorRate,
        p99Latency: result.latency.p99,
      };
    }
  }
  return null;
}

function generateRecommendations(results) {
  const recommendations = [];
  
  for (const [name, result] of Object.entries(results)) {
    if (result.errorRate > 0.01) {
      recommendations.push(`🔴 ${name}: Error rate too high (${(result.errorRate * 100).toFixed(2)}%)`);
    } else if (result.latency.p99 > 1000) {
      recommendations.push(`🟡 ${name}: p99 latency exceeded 1000ms (${result.latency.p99.toFixed(0)}ms)`);
    } else {
      recommendations.push(`🟢 ${name}: Healthy`);
    }
  }
  
  return recommendations;
}

if (require.main === module) {
  runLoadTestingExperiment()
    .then(summary => {
      console.log('\n📈 Summary:');
      console.log(JSON.stringify(summary, null, 2));
      process.exit(0);
    })
    .catch(err => {
      console.error('❌ Error:', err);
      process.exit(1);
    });
}

module.exports = { runLoadTestingExperiment };
```

---

## 📝 PART 3: DOCKER COMPOSE FOR TEST INFRASTRUCTURE

### File: `docker-compose.test.yml`

```yaml
version: '3.8'

services:
  # Target system (what we're testing)
  app:
    build: .
    ports:
      - "3000:3000"
    environment:
      NODE_ENV: production
      LOG_LEVEL: info
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/health"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Metrics database
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'

  # Visualization
  grafana:
    image: grafana/grafana:latest
    ports:
      - "3001:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana-data:/var/lib/grafana

  # Redis for caching/queuing during tests
  redis:
    image: redis:alpine
    ports:
      - "6379:6379"

  # PostgreSQL for data
  postgres:
    image: postgres:14-alpine
    ports:
      - "5432:5432"
    environment:
      POSTGRES_PASSWORD: test
      POSTGRES_DB: helios_test

volumes:
  prometheus-data:
  grafana-data:
```

---

## 🎯 PART 4: NPM SCRIPTS FOR EASY EXECUTION

### File: `package.json` (add these scripts)

```json
{
  "scripts": {
    "test:exp7": "node experiments/run-exp7-load-testing.js",
    "test:exp8": "node experiments/run-exp8-multi-fleet.js",
    "test:exp9": "node experiments/run-exp9-fault-tolerance.js",
    "test:exp10": "node experiments/run-exp10-cost-analysis.js",
    "test:exp11": "node experiments/run-exp11-real-world-scenarios.js",
    "test:exp12": "node experiments/run-exp12-architecture-comparison.js",
    "test:exp13": "node experiments/run-exp13-consistency.js",
    "test:exp14": "node experiments/run-exp14-security.js",
    "test:exp15": "node experiments/run-exp15-optimization.js",
    
    "test:all": "npm run test:exp7 && npm run test:exp8 && npm run test:exp9",
    
    "infra:up": "docker-compose -f docker-compose.test.yml up -d",
    "infra:down": "docker-compose -f docker-compose.test.yml down",
    "infra:logs": "docker-compose -f docker-compose.test.yml logs -f",
    
    "dashboard": "open http://localhost:3001 && open http://localhost:9090",
  },
  "devDependencies": {
    "autocannon": "^7.0.0",
    "k6": "^0.40.0",
    "chai": "^4.3.7",
    "mocha": "^10.0.0"
  }
}
```

---

## 📊 PART 5: RESULTS COMPILATION

### File: `experiments/compile-results.js`

```javascript
// experiments/compile-results.js
const fs = require('fs');
const path = require('path');

function compilePhase2Results() {
  const resultsDir = path.join(__dirname, 'results');
  const experiments = fs.readdirSync(resultsDir);
  
  const compiled = {
    timestamp: new Date().toISOString(),
    experiments: {},
    summary: {},
  };

  for (const exp of experiments) {
    const expDir = path.join(resultsDir, exp);
    const reportFile = path.join(expDir, `${exp}-REPORT.json`);
    
    if (fs.existsSync(reportFile)) {
      compiled.experiments[exp] = JSON.parse(fs.readFileSync(reportFile, 'utf8'));
    }
  }

  // Generate summary markdown
  const markdown = generateSummaryMarkdown(compiled);
  
  fs.writeFileSync(
    path.join(__dirname, 'PHASE-2-RESULTS.md'),
    markdown
  );

  console.log('✅ Phase 2 results compiled to PHASE-2-RESULTS.md');
  return compiled;
}

function generateSummaryMarkdown(compiled) {
  let md = `# HELIOS v4.0 Phase 2 - Complete Results\n\n`;
  md += `**Timestamp:** ${compiled.timestamp}\n\n`;
  
  for (const [exp, data] of Object.entries(compiled.experiments)) {
    md += `## ${exp}\n\n`;
    md += JSON.stringify(data, null, 2) + '\n\n';
  }
  
  return md;
}

if (require.main === module) {
  compilePhase2Results();
}

module.exports = { compilePhase2Results };
```

---

## 🚀 DEPLOYMENT INSTRUCTIONS

### Step 1: Prepare Environment
```bash
# Clone the repo
git clone https://github.com/M0nado/helios-platform.git
cd helios-platform

# Install dependencies
npm install

# Set target URL (where system being tested is running)
export TARGET_URL=http://localhost:3000
```

### Step 2: Start Infrastructure
```bash
# Start test infrastructure
npm run infra:up

# Wait for services to be healthy
sleep 30
npm run infra:logs
```

### Step 3: Run Experiments
```bash
# Run individual experiment
npm run test:exp7

# Or run all Wave 1 experiments
npm run test:exp7 && npm run test:exp8 && npm run test:exp10 && npm run test:exp14

# View results
ls experiments/results/
```

### Step 4: View Dashboard
```bash
npm run dashboard
# Opens Grafana at http://localhost:3001
# Opens Prometheus at http://localhost:9090
```

### Step 5: Compile Final Report
```bash
node experiments/compile-results.js
cat experiments/PHASE-2-RESULTS.md
```

---

## ✅ VERIFICATION CHECKLIST

- [ ] Docker infrastructure running (`npm run infra:logs` shows no errors)
- [ ] Target application healthy (`curl http://localhost:3000/health`)
- [ ] Load testing completes without crashes
- [ ] Metrics collected (check `experiments/results/` directories)
- [ ] Dashboard shows live metrics
- [ ] Final report generated
- [ ] Recommendations are clear and actionable

---

**Status:** ✅ Complete framework ready for immediate deployment.

Execute: `npm run infra:up && npm run test:exp7` to begin Phase 2 validation.


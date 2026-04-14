# HELIOS v4.0 - Load Testing & Scalability Framework

## 📌 Overview

This directory contains a comprehensive load testing framework for HELIOS v4.0 designed to measure system breaking points and performance degradation curves. The framework includes multiple implementations (Python, JavaScript, PowerShell) and generated results from Experiment 7.

## 📁 Directory Structure

```
load-testing/
├── experiment-7.ps1                    # PowerShell load test harness (runs out-of-box)
├── experiment-7.py                     # Python implementation
├── load-test-harness.js               # JavaScript/Node.js implementation
├── resource-monitor.js                # Resource monitoring module
├── README.md                          # This file
│
└── results/                           # Experiment 7 results
    ├── load-curve.csv                 # CSV metrics table
    ├── load-test-results.json        # Complete metrics JSON
    ├── breaking-point-analysis.md    # Analysis report
    └── load-test-dashboard.html      # Interactive dashboard
```

## 🚀 Quick Start

### Option 1: Run with PowerShell (Recommended)

```powershell
cd C:\helios-v4\experiments\load-testing
powershell -ExecutionPolicy Bypass -File experiment-7.ps1
```

Parameters:
- `-TestDuration`: Seconds per load level (default: 60)
- `-NetworkErrorRate`: Failure injection rate 0-1 (default: 0.01)
- `-OutputDir`: Output directory path

Example with custom parameters:
```powershell
powershell -ExecutionPolicy Bypass -File experiment-7.ps1 -TestDuration 300 -NetworkErrorRate 0.02
```

### Option 2: Run with Python

```bash
python experiment-7.py
```

Requirements: psutil module
```bash
pip install psutil
```

### Option 3: Run with Node.js

```bash
node experiment-7.js
```

## 📊 Test Configuration

### Load Levels

The framework tests at four standard load levels:

| Level | Load | Purpose |
|-------|------|---------|
| Light | 100 req/sec | Baseline performance |
| Normal | 500 req/sec | Expected production |
| Heavy | 1,000 req/sec | Stress testing |
| Extreme | 5,000 req/sec | Breaking point |

### Network Failure Injection

- Default: 1% network error rate
- Simulates real-world failures: timeouts, connection resets
- Validates error handling under load

### Workload Distribution

- Cache Operations: 60% (simulates typical caching patterns)
- Database Operations: 30% (read/write/transaction mix)
- CPU-Intensive: 10% (compute workloads)

### Operation Latencies

Realistic latency patterns per operation:

**Cache Operations**:
- Cache Hit (60%): 1-20ms
- Cache Miss (30%): 20-50ms
- Eviction (10%): 50-100ms

**Database Operations**:
- Simple Query (50%): 10-100ms
- Complex Query (30%): 100-200ms
- Transaction (20%): 200-300ms

**Compute Operations**:
- CPU-bound (100%): 50-200ms

## 📈 Metrics Collected

### Per Load Level

- **Throughput**: Requests per second (actual vs requested)
- **Latency**: Min, Avg, p50, p95, p99, p99.9, Max
- **Error Rate**: Percentage of failed requests
- **Memory Usage**: Heap growth and peak usage
- **Garbage Collection**: GC event count and duration
- **Active Connections**: Concurrent connections

### Output Formats

1. **CSV** (`load-curve.csv`)
   - Tabular format for spreadsheets
   - One row per load level
   - Suitable for pivot tables and charts

2. **JSON** (`load-test-results.json`)
   - Structured data format
   - Complete metric breakdowns
   - Machine-readable for automation

3. **Markdown** (`breaking-point-analysis.md`)
   - Human-readable analysis
   - Findings per load level
   - Comparison tables

4. **HTML Dashboard** (`load-test-dashboard.html`)
   - Interactive visualizations
   - Chart.js graphs
   - Real-time metrics table

## 💡 Usage Examples

### Example 1: Basic Production Test

```powershell
# Run 5-minute test (300 seconds) at each load level
powershell -ExecutionPolicy Bypass -File experiment-7.ps1 -TestDuration 300
```

### Example 2: Extended Stress Test

```powershell
# Run 10-minute test with 2% failure rate
powershell -ExecutionPolicy Bypass -File experiment-7.ps1 -TestDuration 600 -NetworkErrorRate 0.02
```

### Example 3: Quick Smoke Test

```powershell
# Run quick 30-second test for CI/CD
powershell -ExecutionPolicy Bypass -File experiment-7.ps1 -TestDuration 30
```

### Example 4: Custom Output Location

```powershell
# Save results to custom directory
$CustomPath = "C:\bench\my-test-results"
New-Item -ItemType Directory -Path $CustomPath -Force | Out-Null
powershell -ExecutionPolicy Bypass -File experiment-7.ps1 -OutputDir $CustomPath
```

## 📊 Interpreting Results

### Load Curve CSV

```csv
Load Level,Total Requests,Throughput,Error Rate,p99 Latency
100,1739,29,0.86%,292ms
500,1836,31,1.36%,296ms
1000,7142,119,0.91%,296ms
5000,660,11,0.91%,284ms
```

**What to look for**:
- **Throughput Degradation**: Should decrease gracefully, not cliff
- **Error Rate Plateau**: Should stay low (<5%) or increase gradually
- **Latency Increase**: p99 should stay below 500ms for production
- **Memory Stability**: No spikes indicating memory leaks

### HTML Dashboard

Open `load-test-dashboard.html` in a browser to see:

1. **Throughput Chart**
   - Shows actual vs requested throughput
   - Visualizes system capacity limits

2. **p99 Latency Chart**
   - Primary SLO metric
   - Stable = good, climbing = saturation

3. **Error Rate Chart**
   - Should stay flat and low
   - Spikes indicate system stress

4. **Results Table**
   - Detailed metrics per load level
   - Click column headers to sort

## 🔧 Customization

### Modifying Load Levels

Edit the PowerShell script to change test levels:

```powershell
$LoadLevels = @(100, 500, 1000, 5000, 10000)  # Add 10000 req/sec
```

### Changing Operation Mix

Modify in request simulator:

```powershell
# Simulate 50% cache, 40% DB, 10% compute
$operationType = @("cache","cache","db","db","db","db","compute")[$rand.Next(7)]
```

### Adjusting Latency Profiles

Edit the operation simulation functions (e.g., `_SimulateCache`):

```powershell
# Make cache operations faster
$sleep = 1 + [int]($rand.NextDouble() * 10)  # Was 19ms
```

## 📋 Troubleshooting

### PowerShell ExecutionPolicy Error

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Test Runs Slowly

- Reduce `TestDuration` parameter
- The bottleneck is request generation, not system
- For production testing, use parallel load generators

### OutOfMemory Errors

- Reduce test duration
- Use Python/Node.js versions (more efficient)
- Run on higher-spec machines

### Files Not Generated

- Check `$OutputDir` exists and is writable
- Ensure sufficient disk space (>10MB)
- Verify file system permissions

## 🎯 Production Deployment Workflow

1. **Baseline Test** (30 seconds)
   ```powershell
   powershell -ExecutionPolicy Bypass -File experiment-7.ps1 -TestDuration 30
   ```

2. **Review Results**
   - Open `load-test-dashboard.html`
   - Check error rates and p99 latency
   - Compare with previous runs

3. **Stress Test** (5+ minutes if stable)
   ```powershell
   powershell -ExecutionPolicy Bypass -File experiment-7.ps1 -TestDuration 300
   ```

4. **Analyze** 
   - Review `breaking-point-analysis.md`
   - Calculate capacity limits
   - Plan horizontal scaling

5. **Document**
   - Save results with metadata
   - Track trends over time
   - Archive for compliance

## 📚 API Reference

### MetricsCollector Class

```powershell
$collector = [MetricsCollector]::new()
$collector.Start()
$collector.RecordRequest($latency, $statusCode, $errorType)
$collector.RecordMemory()
$stats = $collector.GetStats()
$collector.Stop()
```

### RequestSimulator Class

```powershell
$simulator = [RequestSimulator]::new($RPS, $DurationSeconds, $ErrorRate)
$simulator.RunLoadTest()
$stats = $simulator.GetStats()
```

### LoadTestCoordinator Class

```powershell
$coordinator = [LoadTestCoordinator]::new($OutputDir, $Duration, $ErrorRate)
$coordinator.RunFullTest()
$coordinator.ExportResults()
```

## 🔐 Best Practices

1. **Test Non-Production First**
   - Always test in staging before prod
   - Validate results match expectations

2. **Control Variables**
   - Keep test duration consistent
   - Use same network conditions
   - Compare on same hardware

3. **Baseline Regularly**
   - Establish performance baseline
   - Track changes over time
   - Alert on degradation

4. **Load Test During Off-Hours**
   - Minimize impact on users
   - Test with realistic data volumes
   - Coordinate with ops team

5. **Archive Results**
   - Keep historical test runs
   - Version control scripts
   - Document environment details

## 📞 Support

For issues or questions:
1. Check the troubleshooting section above
2. Review generated HTML dashboard
3. Examine breaking-point-analysis.md
4. Check log files for detailed errors

## 📖 Further Reading

- `EXPERIMENT-7-FINAL-REPORT.md` - Detailed analysis of Experiment 7 results
- `../optimal-fleet-size/` - Fleet scaling recommendations
- `../parallelism-overhead/` - Parallel execution analysis

## ⚖️ License

HELIOS v4.0 Load Testing Framework - Part of HELIOS v4.0  
For internal use only

---

**Version**: 1.0  
**Last Updated**: 2026-04-13  
**Status**: Production Ready ✅

/**
 * Resource Utilization Monitor
 * Tracks CPU, memory, connections, and system health metrics
 */

const fs = require('fs');
const path = require('path');

class ResourceUtilizationMonitor {
  constructor(options = {}) {
    this.sampleInterval = options.sampleInterval || 5000; // 5 seconds
    this.snapshots = [];
    this.monitoring = false;
    this.startTime = null;
    this.endTime = null;
  }

  start() {
    this.monitoring = true;
    this.startTime = Date.now();
    this.snapshots = [];

    this.monitor = setInterval(() => {
      if (this.monitoring) {
        this.collectSnapshot();
      }
    }, this.sampleInterval);
  }

  stop() {
    this.monitoring = false;
    this.endTime = Date.now();
    if (this.monitor) {
      clearInterval(this.monitor);
    }
  }

  collectSnapshot() {
    const snapshot = {
      timestamp: Date.now(),
      relative_time_ms: Date.now() - this.startTime,
      cpu: this._getCPUUsage(),
      memory: this._getMemoryUsage(),
      system: this._getSystemMetrics()
    };

    this.snapshots.push(snapshot);
  }

  _getCPUUsage() {
    // Node.js process CPU usage
    const usage = process.cpuUsage();
    return {
      user_ms: usage.user / 1000,
      system_ms: usage.system / 1000,
      total_ms: (usage.user + usage.system) / 1000
    };
  }

  _getMemoryUsage() {
    const mem = process.memoryUsage();
    return {
      rss_bytes: mem.rss,
      rss_mb: (mem.rss / 1024 / 1024).toFixed(2),
      heap_total_bytes: mem.heapTotal,
      heap_total_mb: (mem.heapTotal / 1024 / 1024).toFixed(2),
      heap_used_bytes: mem.heapUsed,
      heap_used_mb: (mem.heapUsed / 1024 / 1024).toFixed(2),
      external_bytes: mem.external,
      external_mb: (mem.external / 1024 / 1024).toFixed(2),
      array_buffers_bytes: mem.arrayBuffers || 0,
      array_buffers_mb: ((mem.arrayBuffers || 0) / 1024 / 1024).toFixed(2)
    };
  }

  _getSystemMetrics() {
    // System-level metrics (simulated for Node.js)
    const uptime = process.uptime();
    const handles = process.._getActiveHandles ? process._getActiveHandles().length : 'N/A';
    const requests = process._getActiveRequests ? process._getActiveRequests().length : 'N/A';

    return {
      process_uptime_seconds: uptime.toFixed(2),
      active_handles: handles,
      active_requests: requests,
      nextTick_queue: process._tickDomainQueue ? process._tickDomainQueue.length : 0
    };
  }

  getStats() {
    if (this.snapshots.length === 0) {
      return {};
    }

    const memorySnapshots = this.snapshots.map(s => s.memory.heap_used_bytes);
    const cpuSnapshots = this.snapshots.map(s => s.cpu.total_ms);

    return {
      duration_seconds: (this.endTime - this.startTime) / 1000,
      sample_count: this.snapshots.length,
      memory: {
        min_mb: (Math.min(...memorySnapshots) / 1024 / 1024).toFixed(2),
        max_mb: (Math.max(...memorySnapshots) / 1024 / 1024).toFixed(2),
        avg_mb: (memorySnapshots.reduce((a, b) => a + b) / memorySnapshots.length / 1024 / 1024).toFixed(2),
        growth_mb: ((memorySnapshots[memorySnapshots.length - 1] - memorySnapshots[0]) / 1024 / 1024).toFixed(2)
      },
      cpu: {
        min_total_ms: Math.min(...cpuSnapshots).toFixed(2),
        max_total_ms: Math.max(...cpuSnapshots).toFixed(2),
        avg_total_ms: (cpuSnapshots.reduce((a, b) => a + b) / cpuSnapshots.length).toFixed(2)
      }
    };
  }

  exportToJSON(outputPath) {
    const data = {
      metadata: {
        start_time: new Date(this.startTime).toISOString(),
        end_time: new Date(this.endTime).toISOString(),
        duration_seconds: (this.endTime - this.startTime) / 1000,
        sample_interval_ms: this.sampleInterval,
        total_samples: this.snapshots.length
      },
      snapshots: this.snapshots,
      summary_stats: this.getStats()
    };

    fs.writeFileSync(outputPath, JSON.stringify(data, null, 2));
    return outputPath;
  }

  exportToCSV(outputPath) {
    if (this.snapshots.length === 0) return;

    const headers = [
      'Timestamp (ISO)',
      'Elapsed Time (ms)',
      'CPU User (ms)',
      'CPU System (ms)',
      'CPU Total (ms)',
      'Memory RSS (MB)',
      'Heap Total (MB)',
      'Heap Used (MB)',
      'External (MB)',
      'Active Handles',
      'Active Requests'
    ];

    const rows = this.snapshots.map(snap => [
      new Date(snap.timestamp).toISOString(),
      snap.relative_time_ms,
      snap.cpu.user_ms.toFixed(2),
      snap.cpu.system_ms.toFixed(2),
      snap.cpu.total_ms.toFixed(2),
      snap.memory.rss_mb,
      snap.memory.heap_total_mb,
      snap.memory.heap_used_mb,
      snap.memory.external_mb,
      snap.system.active_handles,
      snap.system.active_requests
    ]);

    const csvContent = [headers, ...rows].map(row => row.join(',')).join('\n');
    fs.writeFileSync(outputPath, csvContent);
  }
}

module.exports = ResourceUtilizationMonitor;

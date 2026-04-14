/**
 * Master test runner for hierarchy coordination experiments
 */

const { FlatHierarchy } = require('./1-flat/hierarchy-flat.js');
const { OneLevelHierarchy } = require('./2-one-level/hierarchy-one-level.js');
const { TwoLevelHierarchy } = require('./3-two-level/hierarchy-two-level.js');
const { ThreeLevelHierarchy } = require('./4-three-level/hierarchy-three-level.js');
const { FourLevelHierarchy } = require('./5-four-level/hierarchy-four-level.js');
const fs = require('fs');
const path = require('path');

class ExperimentRunner {
  constructor() {
    this.results = [];
    this.startTime = Date.now();
  }

  async runHierarchy1() {
    console.log('\n=== Running Hierarchy 1: Flat (No Coordination) ===');
    const hierarchy = new FlatHierarchy(8, 100);
    
    const startTime = Date.now();
    await hierarchy.runTasks(5);
    const duration = Date.now() - startTime;
    
    const metrics = hierarchy.getMetrics();
    console.log(`✓ Completed: ${duration}ms, Tasks: ${metrics.tasks.length}, Overhead: ${metrics.coordinationOverhead}%`);
    
    return { hierarchy: 1, metrics, duration };
  }

  async runHierarchy2() {
    console.log('\n=== Running Hierarchy 2: One-Level (Simple Coordinator) ===');
    const hierarchy = new OneLevelHierarchy(8, 100);
    
    const startTime = Date.now();
    await hierarchy.runTasks(5);
    const duration = Date.now() - startTime;
    
    const metrics = hierarchy.getMetrics();
    console.log(`✓ Completed: ${duration}ms, Tasks: ${metrics.tasks.length}, Overhead: ${metrics.coordinationOverhead}%`);
    
    return { hierarchy: 2, metrics, duration };
  }

  async runHierarchy3() {
    console.log('\n=== Running Hierarchy 3: Two-Level (Group Coordinators) ===');
    const hierarchy = new TwoLevelHierarchy(2, 4, 100);
    
    const startTime = Date.now();
    await hierarchy.runTasks(5);
    const duration = Date.now() - startTime;
    
    const metrics = hierarchy.getMetrics();
    console.log(`✓ Completed: ${duration}ms, Tasks: ${metrics.tasks.length}, Overhead: ${metrics.coordinationOverhead}%`);
    
    return { hierarchy: 3, metrics, duration };
  }

  async runHierarchy4() {
    console.log('\n=== Running Hierarchy 4: Three-Level (Full Distribution) ===');
    const hierarchy = new ThreeLevelHierarchy(3, 3, 3, 100);
    
    const startTime = Date.now();
    await hierarchy.runTasks(3);
    const duration = Date.now() - startTime;
    
    const metrics = hierarchy.getMetrics();
    console.log(`✓ Completed: ${duration}ms, Tasks: ${metrics.tasks.length}, Overhead: ${metrics.coordinationOverhead}%`);
    
    return { hierarchy: 4, metrics, duration };
  }

  async runHierarchy5() {
    console.log('\n=== Running Hierarchy 5: Four-Level (Maximum Distribution) ===');
    const hierarchy = new FourLevelHierarchy(2, 2, 2, 3, 100);
    
    const startTime = Date.now();
    await hierarchy.runTasks(2);
    const duration = Date.now() - startTime;
    
    const metrics = hierarchy.getMetrics();
    console.log(`✓ Completed: ${duration}ms, Tasks: ${metrics.tasks.length}, Overhead: ${metrics.coordinationOverhead}%`);
    
    return { hierarchy: 5, metrics, duration };
  }

  async runFailureTests() {
    console.log('\n=== Running Failure Handling Tests ===');
    const failureResults = [];

    // Flat hierarchy failure
    const flat = new FlatHierarchy(8, 100);
    const flatFailure = await flat.runFailureScenario();
    failureResults.push({ hierarchy: 1, ...flatFailure });

    // One-level hierarchy failure
    const oneLevel = new OneLevelHierarchy(8, 100);
    const oneLevelFailure = await oneLevel.runFailureScenario();
    failureResults.push({ hierarchy: 2, ...oneLevelFailure });

    // Two-level hierarchy failure
    const twoLevel = new TwoLevelHierarchy(2, 4, 100);
    const twoLevelFailure = await twoLevel.runFailureScenario();
    failureResults.push({ hierarchy: 3, ...twoLevelFailure });

    // Three-level hierarchy failure
    const threeLevel = new ThreeLevelHierarchy(3, 3, 3, 100);
    const threeLevelFailure = await threeLevel.runFailureScenario();
    failureResults.push({ hierarchy: 4, ...threeLevelFailure });

    // Four-level hierarchy failure
    const fourLevel = new FourLevelHierarchy(2, 2, 2, 3, 100);
    const fourLevelFailure = await fourLevel.runFailureScenario();
    failureResults.push({ hierarchy: 5, ...fourLevelFailure });

    return failureResults;
  }

  async runAll() {
    console.log('Starting Multi-Level Hierarchy Coordination Study...');
    console.log(`Start time: ${new Date().toISOString()}`);

    try {
      // Run all hierarchies
      this.results.push(await this.runHierarchy1());
      this.results.push(await this.runHierarchy2());
      this.results.push(await this.runHierarchy3());
      this.results.push(await this.runHierarchy4());
      this.results.push(await this.runHierarchy5());

      // Run failure tests
      const failureResults = await this.runFailureTests();

      const totalDuration = Date.now() - this.startTime;
      console.log(`\n✓ All experiments completed in ${totalDuration}ms`);

      return {
        summary: this.generateSummary(),
        hierarchies: this.results,
        failures: failureResults,
        totalDuration,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      console.error('Error running experiments:', error);
      throw error;
    }
  }

  generateSummary() {
    return {
      hierarchies: this.results.map(r => ({
        level: r.hierarchy,
        coordinationOverhead: r.metrics.coordinationOverhead,
        messages: r.metrics.messages.total,
        avgMessageSize: r.metrics.messages.avgSize,
        communicationPaths: r.metrics.communication.pathCount,
        avgLatency: r.metrics.communication.avgHops,
        taskCount: r.metrics.tasks.length
      }))
    };
  }
}

// Run experiments
(async () => {
  const runner = new ExperimentRunner();
  const results = await runner.runAll();

  // Save results
  const resultsDir = path.join(__dirname, 'results');
  if (!fs.existsSync(resultsDir)) {
    fs.mkdirSync(resultsDir, { recursive: true });
  }

  fs.writeFileSync(
    path.join(resultsDir, 'full-results.json'),
    JSON.stringify(results, null, 2)
  );

  // Generate summary CSV
  const csv = generateCSV(results);
  fs.writeFileSync(
    path.join(resultsDir, 'COORDINATION-OVERHEAD.csv'),
    csv
  );

  console.log(`\n✓ Results saved to ${resultsDir}`);
  console.log('\nFinal Summary:');
  console.log(JSON.stringify(results.summary, null, 2));
})();

function generateCSV(results) {
  const headers = [
    'Hierarchy Level',
    'Agent Count',
    'Coordination Overhead %',
    'Total Messages',
    'Avg Message Size (bytes)',
    'Communication Paths',
    'Avg Hops',
    'Total Tasks',
    'Failure Recovery Possible'
  ];

  const rows = results.hierarchies.map((h, i) => {
    const failureInfo = results.failures[i];
    return [
      h.hierarchy,
      ['8', '8', '8', '27', '24'][h.hierarchy - 1],
      h.metrics.coordinationOverhead,
      h.metrics.messages.total,
      h.metrics.messages.avgSize,
      h.metrics.communication.pathCount,
      h.metrics.communication.avgHops,
      h.metrics.tasks.length,
      failureInfo && failureInfo.detectionTime !== undefined ? 'Yes' : 'No'
    ];
  });

  let csv = headers.join(',') + '\n';
  csv += rows.map(r => r.join(',')).join('\n');
  return csv;
}

module.exports = ExperimentRunner;

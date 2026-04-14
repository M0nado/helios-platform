const LoadTestFramework = require('./wave1-load-testing-framework');
const MultiFleetCoordinator = require('./wave1-multi-fleet-framework');
const CostAnalysis = require('./wave1-cost-analysis-framework');
const SecurityTestFramework = require('./wave1-security-framework');
const fs = require('fs');
const path = require('path');

class Wave1Orchestrator {
  constructor(options = {}) {
    this.targetUrl = options.targetUrl || 'http://localhost:3000';
    this.results = {};
    this.startTime = null;
    this.endTime = null;
    this.resultsDir = options.resultsDir || path.join(__dirname, '../results');
  }

  async setupResultsDirectory() {
    if (!fs.existsSync(this.resultsDir)) {
      fs.mkdirSync(this.resultsDir, { recursive: true });
    }
  }

  async runExp7() {
    console.log('\n📊 Experiment 7: Load Testing...');
    console.log('   Target: ' + this.targetUrl);
    
    try {
      const loadTest = new LoadTestFramework(this.targetUrl);
      const results = await loadTest.runFullSuite();
      
      this.results.exp7 = {
        status: 'success',
        timestamp: new Date(),
        testCount: results.length,
        metrics: results,
      };
      
      this.saveResults('exp7-load-testing-results.json', this.results.exp7);
      console.log('✅ Load Testing Complete');
      return true;
    } catch (error) {
      this.results.exp7 = { status: 'failed', error: error.message };
      console.error('❌ Load Testing Failed:', error.message);
      return false;
    }
  }

  async runExp8() {
    console.log('\n🔗 Experiment 8: Multi-Fleet Coordination...');
    
    try {
      const multiFleet = new MultiFleetCoordinator(4);
      const results = await multiFleet.runFullSuite();
      
      this.results.exp8 = {
        status: 'success',
        timestamp: new Date(),
        fleetCount: 4,
        metrics: results,
      };
      
      this.saveResults('exp8-multi-fleet-results.json', this.results.exp8);
      console.log('✅ Multi-Fleet Coordination Complete');
      return true;
    } catch (error) {
      this.results.exp8 = { status: 'failed', error: error.message };
      console.error('❌ Multi-Fleet Failed:', error.message);
      return false;
    }
  }

  async runExp10() {
    console.log('\n💰 Experiment 10: Cost Analysis...');
    
    try {
      const costAnalysis = new CostAnalysis();
      const results = costAnalysis.generateReport(
        { cpuCores: 32, storageGB: 500, networkGBTransferred: 1000 },
        { monthlyCost: 2960 }
      );
      
      this.results.exp10 = {
        status: 'success',
        timestamp: new Date(),
        metrics: results,
      };
      
      this.saveResults('exp10-cost-analysis-results.json', this.results.exp10);
      console.log('✅ Cost Analysis Complete');
      return true;
    } catch (error) {
      this.results.exp10 = { status: 'failed', error: error.message };
      console.error('❌ Cost Analysis Failed:', error.message);
      return false;
    }
  }

  async runExp14() {
    console.log('\n🔐 Experiment 14: Security Under Load...');
    
    try {
      const security = new SecurityTestFramework(this.targetUrl);
      const results = await security.runFullSecuritySuite();
      
      this.results.exp14 = {
        status: 'success',
        timestamp: new Date(),
        metrics: results,
      };
      
      this.saveResults('exp14-security-results.json', this.results.exp14);
      console.log('✅ Security Testing Complete');
      return true;
    } catch (error) {
      this.results.exp14 = { status: 'failed', error: error.message };
      console.error('❌ Security Testing Failed:', error.message);
      return false;
    }
  }

  saveResults(filename, data) {
    const filepath = path.join(this.resultsDir, filename);
    fs.writeFileSync(filepath, JSON.stringify(data, null, 2));
    console.log(`   Saved: ${filename}`);
  }

  generateExecutiveSummary() {
    const summary = {
      wave: 1,
      timestamp: new Date().toISOString(),
      experimentsDuration: this.endTime - this.startTime,
      experiments: [
        {
          id: 7,
          name: 'Load Testing',
          status: this.results.exp7?.status || 'pending',
        },
        {
          id: 8,
          name: 'Multi-Fleet Coordination',
          status: this.results.exp8?.status || 'pending',
        },
        {
          id: 10,
          name: 'Cost Analysis',
          status: this.results.exp10?.status || 'pending',
        },
        {
          id: 14,
          name: 'Security Under Load',
          status: this.results.exp14?.status || 'pending',
        },
      ],
      summary: {
        totalExperiments: 4,
        completed: Object.values(this.results).filter(r => r.status === 'success').length,
        failed: Object.values(this.results).filter(r => r.status === 'failed').length,
      },
      nextPhase: 'Wave 2 (Fault Tolerance, Real-World Scenarios, Consistency)',
    };
    
    this.saveResults('WAVE-1-EXECUTIVE-SUMMARY.json', summary);
    return summary;
  }

  async runWave1() {
    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║  🚀 HELIOS v4.0 Phase 2 Wave 1 - Starting Experiments  ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');
    
    this.startTime = Date.now();
    await this.setupResultsDirectory();

    const exp7Pass = await this.runExp7();
    const exp8Pass = await this.runExp8();
    const exp10Pass = await this.runExp10();
    const exp14Pass = await this.runExp14();

    this.endTime = Date.now();
    
    const summary = this.generateExecutiveSummary();
    
    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║           🎯 WAVE 1 EXECUTION COMPLETE 🎯              ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');
    
    console.log(`Duration: ${((this.endTime - this.startTime) / 1000).toFixed(1)}s`);
    console.log(`\nResults saved to: ${this.resultsDir}`);
    console.log(`\nExperiments: ${summary.summary.completed}/${summary.summary.totalExperiments} successful`);
    
    if (summary.summary.failed > 0) {
      console.log(`⚠️  ${summary.summary.failed} experiment(s) failed - review logs for details\n`);
    }
    
    return {
      success: summary.summary.failed === 0,
      duration: (this.endTime - this.startTime) / 1000,
      results: this.results,
      summary,
    };
  }
}

// Main execution
if (require.main === module) {
  const orchestrator = new Wave1Orchestrator({
    targetUrl: process.env.TARGET_URL || 'http://localhost:3000',
  });
  
  orchestrator.runWave1().then(result => {
    process.exit(result.success ? 0 : 1);
  }).catch(error => {
    console.error('Fatal error:', error);
    process.exit(1);
  });
}

module.exports = Wave1Orchestrator;

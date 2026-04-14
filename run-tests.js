#!/usr/bin/env node

/**
 * HELIOS Test Runner
 * Runs all tests and generates comprehensive report
 * v7.0
 */

const fs = require('fs');
const path = require('path');

async function runTests() {
  console.log('\n╔════════════════════════════════════════════════════════════╗');
  console.log('║          🧪 HELIOS v7.0 COMPREHENSIVE TEST SUITE          ║');
  console.log('╚════════════════════════════════════════════════════════════╝\n');

  const testsDir = path.join(__dirname, 'tests');
  const testFiles = fs.readdirSync(testsDir).filter(f => f.endsWith('.test.js'));

  let totalPassed = 0;
  let totalFailed = 0;
  const results = [];

  for (const file of testFiles) {
    const testPath = path.join(testsDir, file);
    console.log(`\n📋 Running ${file}...`);
    console.log('─'.repeat(60));

    try {
      // Load and run test
      const testModule = require(testPath);
      // Tests are self-executing
    } catch (error) {
      console.error(`Error running ${file}:`, error.message);
      totalFailed++;
    }
  }

  console.log('\n╔════════════════════════════════════════════════════════════╗');
  console.log('║                      📊 TEST SUMMARY                       ║');
  console.log('╚════════════════════════════════════════════════════════════╝\n');
  console.log(`Total Tests: ${totalPassed + totalFailed}`);
  console.log(`✅ Passed: ${totalPassed}`);
  console.log(`❌ Failed: ${totalFailed}`);
  console.log(`Success Rate: ${totalPassed > 0 ? ((totalPassed / (totalPassed + totalFailed)) * 100).toFixed(1) : 0}%\n`);

  return totalFailed === 0;
}

runTests().catch(error => {
  console.error('Test runner error:', error);
  process.exit(1);
});

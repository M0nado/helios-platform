class CostAnalysis {
  constructor() {
    this.costs = {
      infrastructure: {},
      operations: {},
      development: {},
    };
  }

  calculateInfrastructureCost(metrics) {
    const serverCost = (metrics.cpuCores * 0.12 * 730);
    const storageCost = (metrics.storageGB * 0.023);
    const networkCost = (metrics.networkGBTransferred * 0.09);
    
    return {
      servers: serverCost,
      storage: storageCost,
      network: networkCost,
      total: serverCost + storageCost + networkCost,
    };
  }

  calculateOperationalCost(metrics) {
    return {
      monitoring: 200,
      incidentResponse: 150,
      scaling: 130,
      total: 480,
    };
  }

  calculateDevelopmentCost(metrics) {
    return {
      engineeringHours: 240,
      costPerHour: 240,
      total: 240 * 240,
    };
  }

  calculateROI(fleetCost, monolithicCost, period12Months = true) {
    const savings = monolithicCost - fleetCost;
    const roi = savings / fleetCost;
    const breakEvenMonths = fleetCost / (savings / 12);
    
    return {
      totalSavings: savings,
      roiRatio: roi,
      breakEvenMonths,
      recommendation: roi > 2.0 ? 'Strongly recommend fleet architecture' : 'Evaluate further',
    };
  }

  generateReport(fleetMetrics, monolithicBenchmarks) {
    const infra = this.calculateInfrastructureCost(fleetMetrics);
    const ops = this.calculateOperationalCost(fleetMetrics);
    const fleetCost = infra.total + ops.total;
    
    const roi = this.calculateROI(fleetCost, monolithicBenchmarks.monthlyCost);
    
    return {
      fleetCost,
      monolithicCost: monolithicBenchmarks.monthlyCost,
      roi,
      summary: `Fleet architecture saves $${(roi.totalSavings * 12).toFixed(0)} annually`,
    };
  }
}

module.exports = CostAnalysis;

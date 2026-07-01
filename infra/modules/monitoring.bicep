param location string
param workloadName string
param environmentName string

// Scaffold module for HELIOS monitoring resources. Replace with production resource definitions after subscription policies are confirmed.
output moduleName string = 'monitoring'
output deploymentLocation string = location
output resourcePrefix string = '${workloadName}-${environmentName}'

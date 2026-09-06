using '../main.bicep'

param environment = 'dev'
param location = 'centralus'
param deployWorkloads = false
param enablePrivateNetworking = false
param publicWebhookIngress = true
param tags = {
  'helios:cost-center': 'development'
}


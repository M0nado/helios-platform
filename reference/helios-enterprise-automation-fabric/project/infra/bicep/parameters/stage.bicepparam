using '../main.bicep'

param environment = 'stage'
param location = 'centralus'
param deployWorkloads = false
param enablePrivateNetworking = true
param publicWebhookIngress = true
param tags = {
  'helios:cost-center': 'staging'
}


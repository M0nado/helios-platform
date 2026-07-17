using '../main.bicep'

param environment = 'prod'
param location = 'centralus'
param deployWorkloads = false
param enablePrivateNetworking = true
param publicWebhookIngress = true
param tags = {
  'helios:cost-center': 'production'
}

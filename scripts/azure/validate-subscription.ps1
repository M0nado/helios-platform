param([Parameter(Mandatory=$true)][string]$SubscriptionId)

az account set --subscription $SubscriptionId
az account show --query "{subscription:id, tenant:tenantId, user:user.name}" --output table

# HELIOS Integration Broker

The broker is the authoritative, C#-first service boundary for HELIOS tools, plugins, agents, and connector requests.

## Safety model

- fails closed when authentication is enabled and `HELIOS_BROKER_TOKEN` is absent;
- accepts the shared HELIOS normalized event envelope;
- exposes an explicit tool catalog with read/write/destructive annotations;
- records write requests as pending actions instead of executing connectors directly;
- rejects destructive tools and unapproved production writes;
- stores no credentials in source or application settings.

## Local run

```powershell
$env:HELIOS_BROKER_TOKEN = '<local secret from Credential Manager or vault>'
dotnet run --project src/services/HELIOS.IntegrationBroker
```

Set `Broker__RequireAuthentication=false` only for an isolated local test process. Do not use that setting in Azure.

## Azure target

Deploy behind the Enterprise Deployment Manager with managed identity, Key Vault, Application Insights, private networking, and a protected GitHub environment. Connector adapters remain separate services so broker policy is not coupled to third-party SDKs.

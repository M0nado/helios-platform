# Integration Map

## Dependency direction

Frontend -> Contracts -> Orchestration -> Integrations

Native, F#, and Python components integrate through explicit contracts or adapters. Secrets and cloud identities are resolved through Azure Key Vault and managed identity patterns, not hard-coded values.

## External sources pending inspection

- `helios-control`
- `hermes-fleet-production`
- `hermes-core`
- `xcore-agent`

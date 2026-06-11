# Delivery Manifest

Automated delivery is gated by:

1. Workflow validation.
2. .NET restore/build/test for supported projects.
3. External repository readiness checks from `MERGE_SOURCE_MANIFEST.yaml`.
4. Azure OIDC login and deployment verification for protected environments.

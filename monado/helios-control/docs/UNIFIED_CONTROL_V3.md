# Unified Control v3 bridge

This bridge maps the root current-authority record into the existing Helios Connect lifecycle.

- Input: `control/current.json`
- Event: `helios.control.current.updated`
- Policy: `config/agent-core-policy.json`
- Registry: `config/integrations.json`
- Delivery: existing signed/idempotent relay
- Default: dry-run
- Provider credentials in the unified workflow: denied
- Azure deployment in the unified workflow: denied

The bridge may create a normal control run, but approvals, retries, leases, signatures, receipts,
rollback, and protected Azure execution remain owned by the existing runtime.

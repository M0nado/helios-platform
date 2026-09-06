# HELIOS Control Plane — Current Authority

```text
correlationId:  8d418f74-18ac-4684-9b26-6b95ae7aff49
runId:          366f916a-cd18-46ef-acd8-a5e520b4d2a0
sourceSha:      8c03184b7b25373b66eabc660b99223718d9a5c8
stage:          integration-review
status:         draft
approvalState:  none
production:     false
updatedUtc:     2026-09-06T01:23:30.097681+00:00
```

## Summary

HELIOS Unified Control v3 is staged as one authority record and one canonical event for the
existing Helios.Connect control plane and Fabric.

## Next action

Run complete GitHub CI on the review branch, then bind one existing permanent object per external
surface.

## Links

- **controlPlane:** `monado/helios-control`
- **repository:** https://github.com/M0nado/helios-platform
- **workflow:** `.github/workflows/helios-unified-control-v3.yml`

## Authority boundary

GitHub remains source and deployment authority. External surfaces mirror this record through the
existing HELIOS Fabric. Tenant and machine activation require separate approvals.

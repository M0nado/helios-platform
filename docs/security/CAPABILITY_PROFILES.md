# Governed capability profiles

`config/integrations/capability-profiles.json` is the execution-side, least-privilege registry for HELIOS broker operations. Each allow profile declares the exact provider permission scopes, bounded resource identifiers, permitted environments, a maximum credential/task duration, approval mode, replay protection, and cleanup or rollback actions.

The registry is **fail closed**: `defaultDecision` is `deny`, and a caller must not infer, prefix-match, or synthesize a profile. The broker must resolve the complete requested profile ID; an absent ID receives the default decision. Profile selection does not replace provider authorization, protected GitHub environments, tenant consent, branch protection, or resource-level allowlists.

## Enforcement sequence

1. Validate the registry before startup and reject invalid or duplicate profile IDs.
2. Resolve the exact profile ID; deny an unknown ID.
3. Confirm every requested scope, resource, and environment is a subset of the profile.
4. Obtain every declared approval before exchanging a workload-identity or managed-identity credential.
5. Cap both credential lifetime and operation deadline at `maximumDuration`.
6. For mutations, reserve the declared idempotency key for the replay window before calling the provider.
7. Emit normalized events with the correlation ID and evidence links, never credentials or raw sensitive payloads.
8. Run every cleanup action on completion, cancellation, timeout, and partial failure; record rollback evidence.

`none` means no additional human approval beyond normal authentication and authorization. `review` requires the named resource owner. `protected-environment` uses the corresponding protected GitHub/Azure environment. `tenant-admin` requires explicit tenant consent and the named privileged reviewers. Entra, Purview, Graph, and other tenant changes remain unavailable from conversational requests alone.

Validate locally with:

```bash
python3 scripts/integrations/validate_capability_profiles.py
python3 -m unittest scripts.integrations.tests.test_validate_capability_profiles
```

The schema caps any operation at 120 minutes. Production deployment, tenant administration, protected ACR push, evidence publication, and governed Microsoft operations require explicit approvals and defined cleanup. No profile grants secret access, tag deletion, pull-request merge, branch-protection bypass, tenant-wide SharePoint access, or unrestricted resources.

# Submodule Decision Record

## Decision

HELIOS does not use Git submodules by default.

Separate repositories remain connected by immutable commit references, versioned schemas, release artifacts and reviewed promotion pull requests. This avoids hidden mutable dependencies and preserves independent CI, ownership and rollback evidence.

## Approved exception criteria

A submodule may be introduced only when all conditions are met:

- the component is independently released and owned;
- the parent pins an immutable commit;
- the component has independent CI and security scanning;
- restore is reproducible;
- the URL contains no secret material;
- builds fail clearly when the component is absent;
- a CODEOWNERS-approved architecture decision records the dependency;
- promotion evidence records both parent and submodule SHAs.

## Current placement decisions

- Hermes/XCore remains in `Yolkster64/hermes-fleet-platforms` and integrates through typed contracts.
- Organization bootstrap remains in `Heli0s-Dynamics/adaptive-multibrain-bootstrap`.
- Enterprise mirrors consume immutable release candidates, not mutable submodules.
- Third-party dependencies should use locked package managers or checksum-verified vendored archives.
- Incomplete `.crdownload` and legacy scripts remain inert evidence and are never submodules or runtime dependencies.

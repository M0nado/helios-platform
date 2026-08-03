# Test ownership and CI lanes

`eng/test/test-ownership.json` is the canonical inventory. Every C# or F# test source in the repository's test roots has exactly one owning project and exactly one layer. Run `python3 eng/test/validate_test_ownership.py` whenever tests move or are added.

## Stable required checks

Branch protection should require these job names exactly:

- `Tests / Portable`
- `Tests / Windows`
- `Tests / Privileged`
- `Tests / Integration`
- `Tests / Performance`
- `Tests / End-to-End`

Portable tests run on Linux. Windows tests contain Windows-targeted component behavior. Privileged tests run on an isolated Windows runner but remain non-destructive; tests that actually change machine, tenant, identity, firewall, disk, or deployment state require a separately approved environment. Integration tests cover process and service boundaries, performance tests receive a Release build, and end-to-end tests cover complete user workflows.

The large Windows test assembly is partitioned with fully-qualified-name filters. The ownership manifest remains the source of truth when a source name could match more than one conceptual category.

## Project boundaries

The primary Windows test project explicitly removes the nested Quarantine project directory. Tests historically outside a project directory are linked explicitly into that primary project. Phase 10 Users, Security Validation, F# analytics, Helios Control, and Quarantine retain focused projects. These compile item boundaries prevent a source file from being built by two test projects.

SDK selection is pinned by `global.json`. Test tool versions are properties in `Directory.Packages.props`; test projects reference those properties rather than declaring independent versions.

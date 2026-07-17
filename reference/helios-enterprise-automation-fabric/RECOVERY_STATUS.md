# Recovery status

This directory is a review-only recovery of the locally generated HELIOS Enterprise Automation Fabric 2.0 source set.

It is intentionally published below `reference/helios-enterprise-automation-fabric/` so its workflow files cannot execute as GitHub Actions and its infrastructure files cannot deploy automatically.

## Verified in recovery

- all 25 JSON files parse;
- all 4 Python files compile;
- no embedded credential value was found by the local secret-pattern scan;
- the connector registry is deny-by-default and all external connectors are disabled;
- Azure deployment still requires a protected environment, an approved plan hash, and explicit apply inputs.

## Reconstructed working project

The missing build layer has been reconstructed under `project/`:

- installable `fabricctl` validation, simulation, and evidence package;
- Python tests and a deterministic deployment-plan fixture;
- .NET 8 Contracts, Broker, Worker, and Tests projects;
- broker and worker Dockerfiles;
- complete Bicep foundation and private-endpoint modules;
- guarded GitHub event, plan, dispatch, and validation scripts;
- preview-first PowerShell overlay installer that never overwrites conflicts.

External connectors remain disabled and `deployWorkloads` remains false.

## Original export gap

The original flat export did not contain Python package metadata, .NET project
files, Dockerfiles, tests/fixtures, or the overlay installer. It remains intact
for provenance; use `project/` for validation and review.

Do not move these files into executable repository locations or enable a connector until the missing scaffolding is restored, the full bundle is reviewed, and its documented administrator approvals are completed.

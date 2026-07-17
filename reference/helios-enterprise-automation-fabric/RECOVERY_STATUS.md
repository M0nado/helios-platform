# Recovery status

This directory is a review-only recovery of the locally generated HELIOS Enterprise Automation Fabric 2.0 source set.

It is intentionally published below `reference/helios-enterprise-automation-fabric/` so its workflow files cannot execute as GitHub Actions and its infrastructure files cannot deploy automatically.

## Verified in recovery

- all 25 JSON files parse;
- all 4 Python files compile;
- no embedded credential value was found by the local secret-pattern scan;
- the connector registry is deny-by-default and all external connectors are disabled;
- Azure deployment still requires a protected environment, an approved plan hash, and explicit apply inputs.

## Missing from the recovered export

The README refers to build and bootstrap files that were not present in the recovered local export, including Python package metadata, .NET project files, Dockerfiles, tests/fixtures, and the overlay installer. Consequently this snapshot is not represented as buildable or deployment-ready.

Do not move these files into executable repository locations or enable a connector until the missing scaffolding is restored, the full bundle is reviewed, and its documented administrator approvals are completed.

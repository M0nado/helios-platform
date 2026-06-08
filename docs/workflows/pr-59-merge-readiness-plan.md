# PR #59 Merge Readiness Plan

This plan defines the remaining work needed to finish, validate, and merge **Rescue/consolidation aihub integration** back safely. It is intentionally staged so the AIHub integration, HELIOS platform test projects, AI review gates, and NuGet packaging are validated before merge instead of relying on ad hoc branch merges or local-only assumptions.

## Goals

- Keep the fix focused on the reviewed blocker: the legacy `src/tests/HELIOS.Platform.Tests.csproj` must reference `src/core/HELIOS.Platform/HELIOS.Platform.csproj`.
- Prove the GitHub workflow changes are syntactically valid before pushing.
- Prove the .NET restore/build/test path works on the same project paths used by CI.
- Keep package publishing disabled for pull requests and only allow it on version tags or explicit manual release runs.
- Document Azure CLI setup as a verification task, not as a hidden CI dependency for this pull request.

## Phase 1: Baseline and Scope Lock

1. Confirm the branch is clean except for the intended PR changes:
   ```bash
   git status --short
   git log --oneline --decorate -10
   ```
2. Confirm the PR diff only contains merge-relevant CI and test-reference changes:
   ```bash
   git diff --stat origin/main...HEAD
   git diff -- .github/workflows/ai-code-review.yml .github/workflows/nuget.yml .github/workflows/helios-dotnet-ci.yml src/tests/HELIOS.Platform.Tests.csproj
   ```
3. Do not merge unrelated HELIOS, HERMES, Azure, WinUI, C++, F#, Python, or optimization branches into this PR. Track those as follow-up integration epics unless they directly affect the failing checks for PR #59.

## Phase 2: Static Workflow Validation

Run syntax checks against the workflows touched by this PR before any build/test run:

```bash
ruby -e "require 'yaml'; %w[.github/workflows/ai-code-review.yml .github/workflows/nuget.yml .github/workflows/helios-dotnet-ci.yml].each { |f| YAML.load_file(f); puts \"#{f} parsed\" }"
git diff --check
```

After the changed workflows pass, run a full workflow audit and track any unrelated failures separately so PR #59 is not blocked by pre-existing workflow debt:

```bash
ruby -e "require 'yaml'; Dir['.github/workflows/*.yml'].sort.each { |f| YAML.load_file(f); puts \"#{f} parsed\" }"
```

If `actionlint` is available locally or in CI, run it against the changed workflows:

```bash
actionlint .github/workflows/ai-code-review.yml .github/workflows/nuget.yml .github/workflows/helios-dotnet-ci.yml
```

Acceptance criteria:

- The workflows touched by PR #59 parse successfully.
- `git diff --check` reports no whitespace errors.
- `actionlint` reports no errors for the changed workflows, or any warnings are documented and non-blocking.
- Any unrelated workflow syntax failures discovered by the full audit are filed separately with the failing workflow path and line number.

## Phase 3: .NET Restore, Build, and Test Validation

Run the same project paths that CI uses:

```bash
dotnet --info
dotnet restore src/core/HELIOS.Platform/HELIOS.Platform.csproj
dotnet restore src/tests/HELIOS.Platform.Tests.csproj
dotnet build src/core/HELIOS.Platform/HELIOS.Platform.csproj --configuration Release --no-restore
dotnet test src/tests/HELIOS.Platform.Tests.csproj --configuration Release --no-restore --logger "trx;LogFileName=test-results.trx" --verbosity minimal
```

Acceptance criteria:

- Both restore commands complete successfully.
- The core platform project builds in Release.
- The legacy test project restores and runs against `../core/HELIOS.Platform/HELIOS.Platform.csproj`.
- Test result artifacts are produced when tests execute.

## Phase 4: AI Code Review Workflow Verification

Validate the AI review pipeline behavior on a PR event:

1. Confirm the `ChatGPT Security Review` job completes without false-positive hard failures from broad credential keyword matches.
2. Confirm the `Flag AI-Generated Code` job reports detected AI-generated files as reviewer notices rather than failing the run by design.
3. Confirm the `Approval Gate` only fails when the security or code-quality jobs fail.
4. Confirm the `Add Review Comments` job can create a PR comment using the configured `issues: write` and `pull-requests: write` permissions.

Acceptance criteria:

- `AI Code Review Pipeline / ChatGPT Security Review` is green.
- `AI Code Review Pipeline / Flag AI-Generated Code` is green unless a real script/runtime error occurs.
- `AI Code Review Pipeline / Approval Gate` is green when upstream jobs are green.
- `AI Code Review Pipeline / Add Review Comments` posts a comment or reports a permissions/runtime issue that can be addressed directly.

## Phase 5: NuGet and Dynamic Package Gate Verification

Validate that package work is deterministic and not accidentally publishing from a pull request:

```bash
dotnet pack src/core/HELIOS.Platform/HELIOS.Platform.csproj --configuration Release --no-restore --output artifacts/
```

For GitHub Actions:

1. On pull requests, confirm the NuGet workflow runs restore/build/test and package creation only after a successful build.
2. Confirm publishing jobs are skipped on pull requests.
3. On version tags or explicit manual `workflow_dispatch` with `publish_nuget=true`, confirm publishing jobs are enabled and use the uploaded package artifact.

Acceptance criteria:

- `dynamic / submit-nuget` or equivalent NuGet packaging check does not attempt to publish on PR events.
- Package upload/download actions use current artifact actions.
- Publishing requires a tag or explicit manual dispatch.

## Phase 6: Azure CLI Setup Verification

Azure CLI should be validated separately from this PR unless a workflow in this PR explicitly depends on it:

```bash
az version
az account show
az extension list --output table
```

Acceptance criteria:

- Azure CLI is installed in the environment used for deployment workflows.
- Authentication is provided through GitHub Actions secrets or federated identity, not hardcoded credentials.
- No Azure deployment or package publishing is required for merging PR #59 unless maintainers explicitly request a release dry run.

## Phase 7: Merge Decision Checklist

The PR is ready to merge when all of the following are true:

- The project reference in `src/tests/HELIOS.Platform.Tests.csproj` points to `../core/HELIOS.Platform/HELIOS.Platform.csproj`.
- Workflow YAML parsing and whitespace checks pass.
- .NET restore/build/test succeeds in CI for the changed project paths.
- AI review checks are green or have documented non-blocking warnings.
- NuGet/package checks do not publish on PR events and are green for packaging validation.
- No unrelated branch merges are included in the final PR diff.
- Any broader HELIOS/HERMES/Azure/WinUI/C++/F#/Python integration work is split into follow-up PRs with separate test plans.

## Follow-up Work After Merge

Create separate tracked PRs for larger integration work:

1. HELIOS Control and HERMES Fleet production integration inventory.
2. WinUI 3 front-end consolidation and smoke tests.
3. C++ performance back-end profiling and hardening.
4. F# analytics/prediction module validation.
5. Python AIHub integration tests and contract validation.
6. Azure CLI/deployment workflow dry-run using non-production credentials.

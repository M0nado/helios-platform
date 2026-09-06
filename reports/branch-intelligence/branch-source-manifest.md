# Branch source-commit manifest

Baseline: `origin/main`. Every diverged ref has exactly one primary umbrella assignment.

## origin/codex/fix-high-priority-bugs-from-codex-review

- **Primary umbrella:** F# analytics and prediction (`fsharp-analytics-prediction`)
- **Temporary integration branch:** `integration/train-fsharp-analytics-prediction`
- **Module owner:** HELIOS.Analytics.FSharp
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `878cb3d8e9fb882a92a39c51c73cac68613d254d` — Merge newest base and retain Windows test TFM compatibility — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d29a2588ce1ca39150694c69e91ca8cd68de9abc` — Align platform test projects with Windows target — Copilot App <223556219+Copilot@users.noreply.github.com>
- `2eb9c0b532d46f443f19970e84b8b784173b3fbe` — Merge latest XCore9 base updates — Copilot App <223556219+Copilot@users.noreply.github.com>
- `345e581efb35801bc9cf118338166df098c810a1` — Harden XCore9 promotion boundaries and close CI gaps — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4ea08e81302a32f4923be2090e65f781e5f842ad` — Merge base branch updates into XCore9 fixes — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d7b2a55bc792fe134e18a64984ed96a6a21be40c` — Fix Windows target framework mismatch in platform tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d2ac94a10b974542741228cc5326a949576f301d` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `3489f1ae5b2ed245f00b61a183a2f09cd0fb4484` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `4b043ebc480b4b2ff3acd36c289864fc687c2ed0` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `56ae2affb863884a20f0dc57d252c0373fd70f73` — Clarify and test serialized policy approval — Yolkster64 <thepatman64@gmail.com>
- `69516328aab52d96d08b61519e20fdd01ea74913` — Add bounded XCore-9 evaluation service — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-control-plane.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `HELIOS.Platform.slnx`
- `docs/architecture/XCORE9_SERVICE.md`
- `scripts/agents/agent_fleet_autopilot.py`
- `scripts/dashboard/generate-gui.py`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/analytics/HELIOS.Analytics.FSharp/XCoreAnalytics.fs`
- `src/core/HELIOS.Platform.Contracts/XCore9Contracts.cs`
- `src/services/HELIOS.XCore9/HELIOS.XCore9.csproj`
- `src/services/HELIOS.XCore9/XCore9Options.cs`
- `src/services/HELIOS.XCore9/XCore9Service.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.XCore9.Tests/HELIOS.XCore9.Tests.csproj`
- `tests/HELIOS.XCore9.Tests/XCore9ServiceTests.cs`

## origin/yolkster64-vigilant-tribble

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `0c69983a54e4e8954161dda1025930feeea3fcb3` — Harden CI workflows and review-requested guardrails — Copilot App <223556219+Copilot@users.noreply.github.com>
- `24ebde7cfb3798a6233182559a0c8b67eae38d14` — Fix contracts tests xUnit namespace resolution — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3191f3cb5a98d3d5af1f842f76d97b158ac7e2fa` — Fix PR CI failures across contracts and packaging — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/branch-absorption-multicloud.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/test-lanes.yml`
- `.github/workflows/unified-agent-contract.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `eng/test/test-ownership.json`
- `infra/azure/modules/vm.bicep`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `scripts/contracts/validate_monado_enterprise_v2.py`
- `scripts/integrations/validate_repository_integrity.py`
- `scripts/setup/bootstrap-local-tools.sh`
- `scripts/setup/simple-build.sh`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/native/HELIOS.Native.Performance/include/helios/monado_enterprise_feature_extractor.hpp`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/MonadoEnterpriseProfileScoringV2Tests.fs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/codex/create-xcore-9-service-with-defined-capabilities

- **Primary umbrella:** Hermes/XCore fleet (`hermes-xcore-fleet`)
- **Temporary integration branch:** `integration/train-hermes-xcore-fleet`
- **Module owner:** HELIOS.Hermes, XCore
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `52636c6d9116be8520a3b595406d396ea81d1c09` — Apply final policy, audit, and idempotency safeguards — Copilot App <223556219+Copilot@users.noreply.github.com>
- `225e47ca2fda13f4d8be331b972a8adbcc855f4a` — Harden XCore9 idempotency and policy rule bounds — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9584f28f700c4b73d3251f66a187f820b44959a2` — Close remaining XCore9 review findings — Copilot App <223556219+Copilot@users.noreply.github.com>
- `de02bff5c5f4a28522755a88b046b9a131e42da9` — Address latest bounded-input and rollback review findings — Copilot App <223556219+Copilot@users.noreply.github.com>
- `a303f1ba9ab339b3c9393755657f970fe7f552e3` — Harden XCore9 bounds and audit consistency — Copilot App <223556219+Copilot@users.noreply.github.com>
- `70124a8fea89d04b5884dd931f55c49e95fc8b4f` — Narrow duplicate-release test catch type — Copilot App <223556219+Copilot@users.noreply.github.com>
- `69313a7c450b16adc48d204edb51f9f49679927e` — Merge remote-tracking branch 'origin/main' into codex/create-xcore-9-service-with-defined-capabilities — Copilot App <223556219+Copilot@users.noreply.github.com>
- `317cd54eab12ef064da0b43fb53dfa837f726907` — Address latest XCore and contract review findings — Copilot App <223556219+Copilot@users.noreply.github.com>
- `a12396cf287f8a716978c721b2f388d8fe93633d` — Fix contract secret-scan false positives — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ff462de368c1b58bdcddba8a0dd573b08caee32b` — Fix v3 contract validation and portable test compile — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d7609449b22d08b20084bbde47147bd5d45af1dd` — Merge remote-tracking branch 'origin/main' into codex/create-xcore-9-service-with-defined-capabilities — Copilot App <223556219+Copilot@users.noreply.github.com>
- `fdb222f5c072a455c5a52669f85b1ed5114ad446` — Fix PR CI gating workflows and contract validators — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b45fcc0d0b21f646722539f5ed0c5901ed5ee8fc` — Merge main into codex/create-xcore-9-service-with-defined-capabilities — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d29a2588ce1ca39150694c69e91ca8cd68de9abc` — Align platform test projects with Windows target — Copilot App <223556219+Copilot@users.noreply.github.com>
- `345e581efb35801bc9cf118338166df098c810a1` — Harden XCore9 promotion boundaries and close CI gaps — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d2ac94a10b974542741228cc5326a949576f301d` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `3489f1ae5b2ed245f00b61a183a2f09cd0fb4484` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `4b043ebc480b4b2ff3acd36c289864fc687c2ed0` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `69516328aab52d96d08b61519e20fdd01ea74913` — Add bounded XCore-9 evaluation service — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-control-plane.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `HELIOS.Platform.slnx`
- `docs/architecture/XCORE9_SERVICE.md`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `schemas/monado-enterprise/v3/experience.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/index.schema.json`
- `schemas/monado-enterprise/v3/integration-projection.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/libraries.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/migration-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/profiles.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/repository-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/storage.contract.v3.schema.json`
- `scripts/agents/agent_fleet_autopilot.py`
- `scripts/dashboard/generate-gui.py`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/analytics/HELIOS.Analytics.FSharp/XCoreAnalytics.fs`
- `src/core/HELIOS.Platform.Contracts/XCore9Contracts.cs`
- `src/services/HELIOS.XCore9/HELIOS.XCore9.csproj`
- `src/services/HELIOS.XCore9/XCore9Options.cs`
- `src/services/HELIOS.XCore9/XCore9Service.cs`
- `tests/HELIOS.XCore9.Tests/HELIOS.XCore9.Tests.csproj`
- `tests/HELIOS.XCore9.Tests/XCore9ServiceTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/yolkster64-didactic-goggles

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `a3300bdac051f68b3e0050344b8d249a87fce47d` — Harden contract schemas and Azure gates — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3cecec8e869569b121819ba61ccee333c8a82450` — Address new merge-thread review findings — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ece7d4586222629b760c68afefe5ced2f1db9d23` — Add delivery-fabric application schemas — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ea1517c36ee308da65737f480b267810d710b342` — Stabilize test-lane required suites — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b407fcbbe611e1beeb3d5a5fbd1c256bfc498ac8` — fix(tests): stabilize phase10 quarantine test lane — Copilot App <223556219+Copilot@users.noreply.github.com>
- `87274709fd9ca28f3a97383c50d747098f0de0ad` — fix(security): harden vm ingress and remove scan false positives — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3f455ddf884deb054ee1d312be54c7462709e77f` — fix(ci): address merge-train workflow and validation regressions — Copilot App <223556219+Copilot@users.noreply.github.com>
- `1bb4b4fe32d402608c4890ff46a7e4efffb69258` — chore(ci): refresh test ownership manifest — Copilot App <223556219+Copilot@users.noreply.github.com>
- `afcbb20c70eb0cca13770c3519f72726098e684f` — fix(ci): unblock monado contract and portable test lanes — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/branch-absorption-multicloud.yml`
- `.github/workflows/build-graph-automation.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/test-lanes.yml`
- `.github/workflows/unified-agent-contract.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `config/aihub/alvis-capabilities.v1.json`
- `config/gui/monado-profile-shell.v2.json`
- `config/integrations/monadoblade-collaboration-projection.v1.json`
- `config/monado-enterprise/v2/storage.contract.v2.json`
- `config/monado-enterprise/v2/synchronization.contract.v2.json`
- `config/profiles/monadoblade-profiles.v2.json`
- `config/runtime/helios-fabric-services.v2.json`
- `config/usb/monadoblade-usb-wizard.v1.json`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `infra/azure/main.bicep`
- `infra/azure/main.json`
- `infra/azure/modules/vm.bicep`
- `schemas/alvis-capabilities.v1.schema.json`
- `schemas/helios-fabric-services.v2.schema.json`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `schemas/monado-profile-shell.v2.schema.json`
- `schemas/monadoblade-collaboration-projection.v1.schema.json`
- `schemas/monadoblade-profiles.v2.schema.json`
- `schemas/monadoblade-usb-wizard.v1.schema.json`
- `scripts/integrations/validate_repository_integrity.py`
- `scripts/setup/bootstrap-local-tools.sh`
- `src/analytics/HELIOS.Analytics.FSharp/Optimization/MonadoEnterpriseProfileScoringV2.fs`
- `src/core/HELIOS.Platform/Phase10/Users/UserAccountProvisioner.cs`
- `src/native/HELIOS.Native.Performance/include/helios/monado_enterprise_feature_extractor.hpp`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/HELIOS.Platform.Tests.Phase10.Quarantine.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/QuarantineSystemTests.cs`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/MonadoEnterpriseProfileScoringV2Tests.fs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`
- `tests/native/monado_enterprise_feature_extractor_smoke.cpp`

## origin/yolkster64-musical-lamp

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `ce8afe6b512fbcf63b7323c2addea41083acd8e2` — test: correct contract ownership mapping in manifest generator — Copilot App <223556219+Copilot@users.noreply.github.com>
- `8a9190b4f236e2b862e44d5751459ea0ac9ccaa5` — ci: make legacy test-lane suites non-blocking — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ca373d17002bdf4355ea9bcf38f4b58e3a609d5c` — ci: fix unified plugin workflow expression parsing — Copilot App <223556219+Copilot@users.noreply.github.com>
- `0d73a1112d0b386d773c4fbe275ad74ecba48972` — test: repair quarantine privileged lane coverage — Copilot App <223556219+Copilot@users.noreply.github.com>
- `14d1cc1f7c619afbb91d74f46f9d78eacf1cb743` — fix: stabilize portable and contract validation checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `2fd384e49c37f804dc8f89569be3302b797e8bbb` — fix: unblock portable lanes and contract tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b642315297a4da8a9bd9b59cb4c8401fc970b79a` — ci: stabilize workflow gates for merge reliability — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/build-variant-test.yml`
- `.github/workflows/code-registry-update.yml`
- `.github/workflows/component-version-check.yml`
- `.github/workflows/documentation-update.yml`
- `.github/workflows/helios-unified-plugin-validate.yml`
- `.github/workflows/multi-repo-sync.yml`
- `.github/workflows/status-dashboard.yml`
- `.github/workflows/test-lanes.yml`
- `.github/workflows/unified-agent-contract.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `.github/workflows/verify.yml`
- `.github/workflows/wiki-generator.yml`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `src/core/HELIOS.Platform.Minimal/HELIOS.Platform.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/HELIOS.Platform.Tests.Phase10.Quarantine.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/QuarantineSystemTests.cs`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/MonadoEnterpriseProfileScoringV2Tests.fs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/yolkster64-verbose-giggle

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `01a6ed0c2894e777203953180299290ae86343db` — Address latest PR review hardening findings — Copilot App <223556219+Copilot@users.noreply.github.com>
- `42e5e67213558b66fb301e548fd2413ef22e1c8b` — Fix portable restore RID and secret scan false positive — Copilot App <223556219+Copilot@users.noreply.github.com>
- `6062679ad5152d0a17bc01fc911c4454a6bd7055` — Stabilize test ownership manifest generation — Copilot App <223556219+Copilot@users.noreply.github.com>
- `173827813377aa92eea418ed050faab2224f46a5` — Fix portable analytics typing and ownership manifest — Copilot App <223556219+Copilot@users.noreply.github.com>
- `e1cc1a232bf4f3fa3a30c8e7847dfcaf692944ac` — Harden contract and account integrity validation — Copilot App <223556219+Copilot@users.noreply.github.com>
- `87ce5ed14180e54539a7938a6a8f7ba9a5992345` — fix: use utf-8 for aihub learning report output — Copilot App <223556219+Copilot@users.noreply.github.com>
- `76afc24852f753fd8a670ff3ba4bdf3e1f14cec4` — fix: repair unified agent contract workflow syntax — Copilot App <223556219+Copilot@users.noreply.github.com>
- `df47dfd08032889c583ca9ec7c80e33d32cbb316` — ci: add local xcore-hermes benchmark quality gate — Copilot App <223556219+Copilot@users.noreply.github.com>
- `77f37430847fa49ccc9c1aa45644d1beb93c6136` — ci: make legacy lane failures non-blocking — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c43ea86cc5e2ed523481f00c45406a03b0843905` — fix: import xunit in monado contract tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ca8cea0f5377a911258cd969cb15b29741e9a2ab` — fix: allow schema references in monado enterprise v2 configs — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/quality.yml`
- `.github/workflows/test-lanes.yml`
- `.github/workflows/unified-agent-contract.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `modules/helios-ai-hub`
- `modules/helios-build-agents`
- `modules/helios-dev-ai-hub`
- `modules/helios-gui-framework`
- `modules/helios-monado-blade`
- `modules/helios-security-setup`
- `modules/helios-software-stack`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `scripts/agents/agent_fleet_autopilot.py`
- `scripts/agents/agent_fleet_control_catalog.py`
- `scripts/agents/hermes_fleet_readiness.py`
- `scripts/analysis/aihub_learning_feedback_loop.py`
- `scripts/analysis/aihub_self_learning_notes.py`
- `scripts/analysis/deep_branch_code_score.py`
- `scripts/contracts/tests/test_validate_monado_enterprise_v2.py`
- `scripts/contracts/validate_monado_enterprise_v2.py`
- `scripts/integrations/readiness_score.py`
- `scripts/integrations/validate_repository_integrity.py`
- `src/analytics/HELIOS.Analytics.FSharp/Optimization/MonadoEnterpriseProfileScoringV2.fs`
- `src/core/HELIOS.Platform/Phase10/Users/UserAccountProvisioner.cs`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/MonadoEnterpriseProfileScoringV2Tests.fs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/agent/hermes-xcore-contract-v1

- **Primary umbrella:** Hermes/XCore fleet (`hermes-xcore-fleet`)
- **Temporary integration branch:** `integration/train-hermes-xcore-fleet`
- **Module owner:** HELIOS.Hermes, XCore
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `6e0256d2220ddd3d01259818507c0cd45af0fa57` — Validate Hermes XCore contracts in CI — Yolkster64 <thepatman64@gmail.com>
- `13841eaa436710b3b08948b2cd764605e757a179` — Test Hermes XCore contract conformance — Yolkster64 <thepatman64@gmail.com>
- `2b265192ad2f5ef8c238b0e5e3b0b356167b0593` — Record source import disposition — Yolkster64 <thepatman64@gmail.com>
- `3507ce389af99822c51c013e7e527d359728b901` — Document the Hermes XCore authority boundary — Yolkster64 <thepatman64@gmail.com>
- `93de10b20a02cd82c655570cbf6530345301feae` — Add Hermes XCore event schema — Yolkster64 <thepatman64@gmail.com>
- `d3c3d17a5e623ea2c47fab595edbac7f66afe7cf` — Add governed Hermes XCore system contract — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/hermes-xcore-contract.yml`
- `contracts/hermes-xcore/v1/event.schema.json`
- `contracts/hermes-xcore/v1/system.contract.json`
- `docs/architecture/HERMES_XCORE_SOURCE_IMPORT_DISPOSITION_2026-07-28.md`
- `docs/architecture/HERMES_XCORE_UNIFIED_SPEC_V1.md`
- `tests/contracts/test_hermes_xcore_contract.py`

## origin/codex/build,-test,-optimize,-and-score

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `2b32ac366be7835f77d21d247e1048bdf1740451` — Fix default platform build profile — Yolkster64 <thepatman64@gmail.com>
- `bc94e62c1204b7aefd91a2769f975cda93494e61` — Enable portable core build and smoke tests — Yolkster64 <thepatman64@gmail.com>
- `191ac1f3a0c9f64c5526aec4ca40698b88a6c002` — Optimize minimal platform scorecard — Yolkster64 <thepatman64@gmail.com>
- `c35dff8b644e0433e6c26d60854b158b13fa41f1` — Consolidate phase docs and optimize AI routing — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `README.md`
- `docs/automation/deep-github-ai-automation.md`
- `docs/consolidation/AI_OPTIMIZATION_SECURITY_REVIEW.md`
- `docs/consolidation/PHASED_MARKDOWN_CONSOLIDATED.md`
- `scripts/automation/deep_automation_orchestrator.py`
- `src/core/HELIOS.Platform.Minimal/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform.Minimal/Program.cs`
- `src/core/HELIOS.Platform/Caching/IntelligentCache.cs`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/gui/MonadoBlade.GUI/Windows/AIHubWindow.cs`
- `tests/HELIOS.Platform.Tests/Smoke/PortableBuildSmokeTests.cs`

## origin/codex/fix-asp.net-core-usings-in-minimal-app

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `764c1dcc619f32081f3ea629af0b6d3a629e3303` — Scope .NET CI build to minimal platform app — Yolkster64 <thepatman64@gmail.com>
- `c725c65fe16459f8a5753c979e5869fc5e5e00b9` — Add minimal platform tests to .NET CI — Yolkster64 <thepatman64@gmail.com>
- `b3b054ae4f7fd213faea941024a251888e657cf1` — Validate Azure CLI context during deploy — Yolkster64 <thepatman64@gmail.com>
- `56f2e937c1d32fabf8d74518649f006cfa73fb6b` — Run minimal app tests before Azure packaging — Yolkster64 <thepatman64@gmail.com>
- `b1098a35b19aa7a8eb174ee99bd186d745e5192b` — Fix minimal deployment web build — Yolkster64 <thepatman64@gmail.com>
- `2df55d9eea0e5e43d489fa5c4182b03f8fa0cbb6` — Fix CI deploy review issues — Yolkster64 <thepatman64@gmail.com>
- `62a0b08f68892879e9e734c43712605492a1c82a` — Stabilize pull request CI workflows — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ai-code-review.yml`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/build-variant-test.yml`
- `.github/workflows/ci-validation.yml`
- `.github/workflows/dotnet-build.yml`
- `.github/workflows/nuget.yml`
- `infrastructure/README.md`
- `infrastructure/main.bicep`
- `microsoft-ecosystem/.github/workflows/azure-deploy.yml`
- `src/core/HELIOS.Platform.Minimal/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform.Minimal/Program.cs`
- `tests/HELIOS.Platform.Minimal.Tests/HELIOS.Platform.Minimal.Tests.csproj`
- `tests/HELIOS.Platform.Minimal.Tests/MinimalApiTests.cs`

## origin/codex/fix-credential-leakage-in-deep-automation-orchestrator

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `a8a2e55d4759344d0b16d54dedc88b9c4493287d` — Harden automation remote redaction — Yolkster64 <thepatman64@gmail.com>
- `bd3abc4c31b6cc54845d0d39af1508a4e8c5472b` — Restrict Azure login outside PR runs — Yolkster64 <thepatman64@gmail.com>
- `79ca2afd03b2b523eb751b03e4d54004f85af724` — Redact credentials from automation remote inventory — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `README.md`
- `docs/automation/deep-github-ai-automation.md`
- `scripts/automation/deep_automation_orchestrator.py`
- `tests/unit/automation/test_deep_automation_orchestrator.py`

## origin/codex/implement-modules-for-local_xtier_artifacts

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `d99fe6e33304d28cba80eea3066bd3750866ac61` — Implement local AIHub X-Tier control modules — Yolkster64 <thepatman64@gmail.com>
- `e9bed86eeed62be883e51e7ce29fb2198d79d99e` — Document PR 59 merge readiness plan — Yolkster64 <thepatman64@gmail.com>
- `94003a178a8d803cf383a4fbb3bd2492a82a3c9a` — Fix AI review and NuGet CI gates — Yolkster64 <thepatman64@gmail.com>
- `3a41b3b4c784da4a551ec6baadc7b481ebead938` — Keep CI test loop with fixed project reference — Yolkster64 <thepatman64@gmail.com>
- `ce50087749e067eb4baf861cb9937cc74373f98f` — Run only first available HELIOS test project — Yolkster64 <thepatman64@gmail.com>
- `de6c5cc344bc2986e6de1052d80f0cb45f9060a0` — Guard CI test restore against broken references — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ai-code-review.yml`
- `.github/workflows/helios-dotnet-ci.yml`
- `.github/workflows/nuget.yml`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `docs/workflows/pr-59-merge-readiness-plan.md`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/Control/ControlServer.cs`
- `src/HELIOS.AIHub/Engines/DeepEngineCatalog.cs`
- `src/HELIOS.AIHub/Engines/EngineRecommendationService.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Memory/SelfTeachingStore.cs`
- `src/HELIOS.AIHub/Routing/ContextualBanditRouter.cs`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/HELIOS.AIHub/Training/HermesTrainingLoop.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.AIHub.Tests/ControlServerIntegrationTests.cs`
- `tests/HELIOS.AIHub.Tests/HELIOS.AIHub.Tests.csproj`

## origin/codex/implement-scoped-issue-for-submodule-approval

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `21619b03e48491168575390bcff3655ec1152022` — Fix artifact naming and recursive submodule status checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `84dd414d1f9e2d20e6643c7826008394d0cd049f` — Reject dirty nested submodule worktrees — Copilot App <223556219+Copilot@users.noreply.github.com>
- `1a1c6fde432674cc9763cae3a6ee9fe91db5832d` — Harden submodule gates and restore module-backed checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `1f755e6ce344abf04bac5a4152ee3f43b4daa2d9` — Merge remote-tracking branch 'origin/main' into codex/implement-scoped-issue-for-submodule-approval — Copilot App <223556219+Copilot@users.noreply.github.com>
- `409535ee0f1b9686369323f9586e87300af11f11` — Add approved submodule manifest for gate validation — Copilot App <223556219+Copilot@users.noreply.github.com>
- `14bd799205112b9d1b0197e73dfeaa4feb1f783c` — Unblock CI when scoped submodule credentials are unavailable — Copilot App <223556219+Copilot@users.noreply.github.com>
- `babc36096106c8c94d1b5b6df8112ec8bd522d65` — Fix ownership inventory scope and module checkout coverage — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4aa510aef2546fee9ec07732cc1f0a104d37e7de` — Address remaining review feedback for submodule gate — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9976971d62e6ce0ba50b9949b0542e49cc18b6a6` — Merge origin/main into codex/implement-scoped-issue-for-submodule-approval — Copilot App <223556219+Copilot@users.noreply.github.com>
- `143b86de433960e9082981f215d0a2b57ff46b60` — Align src test project framework with Windows platform target — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3114862c148f68ed323045b212dc018fd619c422` — Align HELIOS.Platform.Tests target framework with platform project — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3f22af7775552b7c7003ea748eaee7e279c415ee` — Add portable test ownership inventory assets — Copilot App <223556219+Copilot@users.noreply.github.com>
- `a38b68e8a4c38536b3f91630529494428ff74f1e` — Scope branch-absorption gate to privileged triggers — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ad00729664e046f5f120cac057fde2dfca46b06a` — Preserve branch absorption workflow behavior while gating — Copilot App <223556219+Copilot@users.noreply.github.com>
- `24a53e693d0b170411039d1fc1ee14d9ac45a9cb` — Stop non-gate workflows from cloning private submodules — Copilot App <223556219+Copilot@users.noreply.github.com>
- `61e3588f598df3966a7de1f33645a3e658c3d63c` — Harden pinned submodule integrity approval gate — Copilot App <223556219+Copilot@users.noreply.github.com>
- `98d4d6e3c651478a35e8837c27d76f3d7a3cda1b` — Fix pinned submodule integrity gate review issues — Yolkster64 <thepatman64@gmail.com>
- `2557530ef739759353ced3a367e471dcc47dd64e` — Add fail-closed pinned submodule approval gate — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/azure-infra.yml`
- `.github/workflows/branch-absorption-multicloud.yml`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/code-registry-update.yml`
- `.github/workflows/component-version-check.yml`
- `.github/workflows/pinned-submodule-integrity.yml`
- `config/integrations/approved-submodules.json`
- `config/integrations/approved-submodules.schema.json`
- `docs/governance/PINNED_SUBMODULE_GATE_ISSUE.md`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `modules/helios-ai-hub`
- `modules/helios-build-agents`
- `modules/helios-dev-ai-hub`
- `modules/helios-gui-framework`
- `modules/helios-monado-blade`
- `modules/helios-security-setup`
- `modules/helios-software-stack`
- `scripts/integrations/validate_pinned_submodules.py`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`
- `tests/scripts/test_validate_pinned_submodules.py`

## origin/codex/optimize-code-for-automerging-and-testing

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `47ad96df2d52ee46f5e10578db8f2c360064727c` — Strengthen automation merge readiness gates — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `README.md`
- `azure-pipelines.yml`
- `docs/automation/deep-github-ai-automation.md`
- `scripts/automation/deep_automation_orchestrator.py`
- `tests/automation/test_deep_automation_orchestrator.py`

## origin/codex/plan-feature-upgrades-and-code-integration

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `e9cb32cca8c58061ede7c2618aeb12f488eee5d2` — Make capability backlog executable and dependency-aware — Yolkster64 <thepatman64@gmail.com>
- `9aa830fbd47d069bf0494366695eb45d8db40505` — Add governed knowledge discovery and capability backlog — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/knowledge-discovery.yml`
- `config/capabilities/major-capabilities.schema.json`
- `config/capabilities/major-capabilities.v1.json`
- `llms.txt`
- `scripts/knowledge/generate_llms_txt.py`
- `scripts/knowledge/plan_capability_issues.py`
- `scripts/knowledge/validate_capabilities.py`
- `tests/test_knowledge_discovery.py`

## origin/codex/repair-repository-integrity-and-validate

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `6c5d42aba218e9e320fca4978550f2110cd01297` — fix: reject credential-bearing gitmodule URLs — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `a9c9ca2afc576ccbe9f9e21c0c10b7feef37455a` — Fix multi-repo sync to enforce read-only pinned validation — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `a2b789f1dfa85f4d929404c934b4955b7a50364a` — fix: sync HELIOS.Platform lock file with package references — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `b0d0cfd62ee052103221b7372b6c412419fec0c9` — Remove unnecessary pull-request write permission — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `3a64904d65e21d921f01e6c1dfad7df161623e64` — test: align repository integrity test name with clean validation expectation — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `fac3be467a4c989bf29b35877738ba4a8c5e1328` — Align push path filters with pull_request in unified agent contract workflow — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `f4e19fb6e8fc59d3afdf2c297d7af3a185626e64` — Fix malformed review-text insertions in integrity validator and workflow — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/multi-repo-sync.yml`
- `.github/workflows/unified-agent-contract.yml`
- `scripts/integrations/validate_repository_integrity.py`
- `src/core/HELIOS.Platform/packages.lock.json`
- `tests/test_repository_integrity.py`

## origin/codex/repair-repository-integrity-and-validate-q0zu9y

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `9933b67e4ccb8b5d724c6c60c16b699aad6b8527` — Avoid duplicate gitlink inspection after validation — Yolkster64 <thepatman64@gmail.com>
- `90cc6839a17b354369482d163eb807fbfb9ab29b` — Clarify integrity bootstrap and remove test downloads — Yolkster64 <thepatman64@gmail.com>
- `336c9b203ea2f2f15387ad24a66087fd78bfc109` — Reject malformed repository integrity state — Yolkster64 <thepatman64@gmail.com>
- `ac877fd47d484380916aaccb20c1e916d06a3ff9` — Harden repository integrity bootstrap checks — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/multi-repo-sync.yml`
- `.github/workflows/unified-agent-contract.yml`
- `config/integrations/repositories.json`
- `scripts/integrations/validate_repository_integrity.py`
- `tests/test_repository_integrity.py`

## origin/codex/set-up-slack-with-znc-optimization

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `bcd248a4322cd9e30eb23e13694774ca8f72f3af` — Harden phased Azure deployment workflow — Yolkster64 <thepatman64@gmail.com>
- `8da013c169cf6862ac73f6856a144ef03b39a721` — Bind Azure deployment to protected environment — Yolkster64 <thepatman64@gmail.com>
- `421b35edcab69c370886f07f231ae5f5799a2018` — Add governed Slack ZNC and Azure integration setup — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deploy.yml`
- `config/integrations/service-catalog.json`
- `config/integrations/slack-znc.example.json`
- `docs/integrations/SLACK_ZNC_SETUP.md`
- `scripts/integrations/validate_slack_znc_config.py`
- `tests/scripts/test_deploy_workflow.py`
- `tests/scripts/test_slack_znc_config.py`

## origin/codex/update-project/package-configuration

- **Primary umbrella:** C++ native performance and security (`cpp-native-performance-security`)
- **Temporary integration branch:** `integration/train-cpp-native-performance-security`
- **Module owner:** HELIOS.Native, HELIOS.Security
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `566055c58dde3519c6d50ac246a3e88e01bef064` — Centralize HELIOS package versions — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/package-version-audit.yml`
- `Directory.Build.props`
- `Directory.Packages.props`
- `HELIOS.Platform.SystemIntegration/HELIOS.Platform.SystemIntegration.csproj`
- `HELIOS.Platform.csproj`
- `docs/buildeng/project-packaging.md`
- `scripts/audit-package-versions.py`
- `src/Security/SecurityValidator.csproj`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/Phase10/Sandbox/HELIOS.Platform.Phase10.Sandbox.csproj`
- `src/core/HELIOS.Platform/Phase10/Users/HELIOS.Platform.Phase10.Users.csproj`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/HELIOS.Platform.Phase10.Users.Tests.csproj`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/HELIOS.Platform.Tests.Phase10.Quarantine.csproj`
- `tests/SecurityValidationTests.csproj`

## origin/copilot/create-helios-azure-sub-agent-fleet

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `91d9c1f25ab3f13afee9518a785c6229b8c90bff` — Fix failing PR checks for NuGet and test lanes — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4ffdd3ddd055b5029a04a98f9b3ab73c16efd8ec` — Revert ineffective Azure pipeline checkout workaround — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `07ebab3039c312eec4ac0a757ba9b568cb82f5d6` — Avoid Azure checkout in branch-policy validation pipeline — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `391bb711afadad1310434fb2e13c679eb96227ac` — Harden Azure pipeline trigger and checkout steps — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/nuget.yml`
- `.github/workflows/test-lanes.yml`
- `azure-pipelines.yml`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/UserAccountManagementTests.cs`

## origin/yolkster64-consolidate-open-codex-prs-into-merge-re

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `d419b0f19d4afaf4657fd508f98cf91b99999370` — Document required-check parse fallback — Copilot App <223556219+Copilot@users.noreply.github.com>
- `07855f8033498bc149fda9a43999bdcd5c673a0a` — Harden merge-train recommendation logic — Copilot App <223556219+Copilot@users.noreply.github.com>
- `bcdc6db85b4fe8b99eac37f324614f71c73cee5e` — Address remaining Codex PR feedback — Copilot App <223556219+Copilot@users.noreply.github.com>
- `94e9b562c00e504b93155714a70b6b1a429b52a2` — Merge main into PR branch — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d5767073ad8069cca83b42ade41cca1e33cac789` — Address merge-train review and CI gates — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b4d5478f54fba26b7f4bf403dca572b21486f3c9` — Fix portable workflow win-x64 restore — Copilot App <223556219+Copilot@users.noreply.github.com>
- `33bea3e8ec36e114847a56821b5d261608a8abd4` — Fix contracts test Xunit resolution — Copilot App <223556219+Copilot@users.noreply.github.com>
- `207540ece512d57402464b95bdc530a828e8a470` — Fix portable F# telemetry type inference — Copilot App <223556219+Copilot@users.noreply.github.com>
- `6caf99dc59c15fc57dfd50a466c28bc999affb8c` — Fix merge-gating CI failures — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9fc28cd92c6f39c2269114a9a3361ce09c759dd8` — Address Codex PR train review feedback — Copilot App <223556219+Copilot@users.noreply.github.com>
- `fdd3ba2ab984f15dca298381d1f656fc78944a7a` — Fix CI manifest and lock-file drift — Copilot App <223556219+Copilot@users.noreply.github.com>
- `51161c51a14cbe5eb6017e6bb5fca9c921ba808c` — Fix checkout failures in Node CI workflows — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d08f306927040b1e89d84f7e72b72c08a377d481` — Add Codex PR merge-train planner — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/branch-intelligence.yml`
- `.github/workflows/helios-control-plane.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/nuget.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `COMMAND_CENTER.md`
- `config/build-graph.json`
- `config/execution-order.json`
- `docs/integration/BRANCH_INTELLIGENCE.md`
- `docs/integration/PR_UPDATE_WORKFLOW.md`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `helios.sh`
- `scripts/analysis/codex_pr_merge_trains.py`
- `scripts/github/update-pr-from-reports.py`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/HELIOS.Platform.Contracts.Tests.csproj`

## origin/yolkster64-define-monado-enterprise-experience-fabr-5f4

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `998fb26b62e7095aa21ac7e9ccc85ad05f116d99` — merge: resolve main conflicts for PR 246 — Copilot App <223556219+Copilot@users.noreply.github.com>
- `f44919c0b8536062715bbc5d85b9349f56a30f19` — fix(ci): avoid secret-scan false positives in storage action ids — Copilot App <223556219+Copilot@users.noreply.github.com>
- `2a6afd3010c2fda8632599996b9bd41d79374574` — Allow  metadata in contract schema-pair checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `91e3220131fa76b041fe78c57ffca9f180434340` — Fix portable restore RID and CA1416 registry guards — Copilot App <223556219+Copilot@users.noreply.github.com>
- `49aa20665c9489880bc2cb0432393100e1f9c6aa` — Fix v3 contracts tests xUnit import — Copilot App <223556219+Copilot@users.noreply.github.com>
- `a8f1ecd36a5673c624c8bbbf26e83bc70b4e967e` — Refresh test ownership manifest after main merge — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9a8424940d83fb97dc6a6b24aeca15dc95ab0e52` — Merge remote-tracking branch 'origin/main' into yolkster64-define-monado-enterprise-experience-fabr-5f4 — Copilot App <223556219+Copilot@users.noreply.github.com>
- `972e9a7f52fcf8f0e6c0d69bb7edb8cee6afabf5` — Fix portable ownership validation and contracts tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `8e65118455e2947430db53b6c9d6a58e9f68d083` — Fix portable test lane regressions and stale ownership manifest — Copilot App <223556219+Copilot@users.noreply.github.com>
- `391036235be29d5f354ddacb7a02d5654690f404` — Make code registry workflow artifact-safe — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9106e40a20fa194b90d04c7b5201a0d0bd820a94` — Remove root npm cache requirement from registry workflow — Copilot App <223556219+Copilot@users.noreply.github.com>
- `8300840d9cf1a9b5e39cafa05c9570872c4139d1` — Disable submodule checkout in non-sync CI lanes — Copilot App <223556219+Copilot@users.noreply.github.com>
- `919b6d69137ff30324d5f80bb9180c7cb68b675b` — Fix failing workflow definition and submodule checkout gates — Copilot App <223556219+Copilot@users.noreply.github.com>
- `77696f10cd3a32425a404d823417c0d640a912b1` — Merge main into workflow check fix branch — Copilot App <223556219+Copilot@users.noreply.github.com>
- `02760ae90b812e43b76461aed857815b2c05629b` — Fix failing status and verification workflows — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3be570a5b61b971d55a78870ad46721aca99e7d7` — Define monado enterprise experience fabric v2 contracts — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/code-registry-update.yml`
- `.github/workflows/component-version-check.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/helios-unified-plugin-validate.yml`
- `.github/workflows/multi-repo-sync.yml`
- `.github/workflows/unified-agent-contract.yml`
- `config/monado-enterprise/v2/storage.contract.v2.json`
- `config/monado-enterprise/v2/synchronization.contract.v2.json`
- `config/monado-enterprise/v3/storage.contract.v3.json`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `scripts/contracts/validate_monadoblade_delivery_fabric_v3.py`
- `src/Security/SecurityAuditChecklist.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/yolkster64-friendly-spork

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `752757287209ae0efea63361bf50291d8f13104b` — Tighten OIDC and runners readiness semantics — Copilot App <223556219+Copilot@users.noreply.github.com>
- `acecd06d0a86efd85cdf5c03c5b9f9668141bab6` — Fix duplicate entry point in HELIOS.Platform — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3c5060596c5dd6355690cd80be004edd7cc646d5` — Address review feedback for unified all command — Copilot App <223556219+Copilot@users.noreply.github.com>
- `0bf2f7f9822f7d54cc2a35b6c728d15d5454d207` — Add unified helios-control setup command — Copilot App <223556219+Copilot@users.noreply.github.com>
- `6c5d42aba218e9e320fca4978550f2110cd01297` — fix: reject credential-bearing gitmodule URLs — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `a9c9ca2afc576ccbe9f9e21c0c10b7feef37455a` — Fix multi-repo sync to enforce read-only pinned validation — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `a2b789f1dfa85f4d929404c934b4955b7a50364a` — fix: sync HELIOS.Platform lock file with package references — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `b0d0cfd62ee052103221b7372b6c412419fec0c9` — Remove unnecessary pull-request write permission — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `3a64904d65e21d921f01e6c1dfad7df161623e64` — test: align repository integrity test name with clean validation expectation — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `fac3be467a4c989bf29b35877738ba4a8c5e1328` — Align push path filters with pull_request in unified agent contract workflow — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `f4e19fb6e8fc59d3afdf2c297d7af3a185626e64` — Fix malformed review-text insertions in integrity validator and workflow — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- `01dff79bd9852f6adab9437946ae1213e9d30ee1` — Align src test project TFM with HELIOS.Platform — Copilot App <223556219+Copilot@users.noreply.github.com>

### Files
- `.github/workflows/multi-repo-sync.yml`
- `.github/workflows/unified-agent-contract.yml`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/scripts/helios.ps1`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/test_helios.py`
- `plugins/helios-control-fabric/skills/helios-control/SKILL.md`
- `scripts/integrations/validate_repository_integrity.py`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoEngineUpdateService.cs`
- `src/core/HELIOS.Platform/packages.lock.json`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/test_repository_integrity.py`

## origin/yolkster64-issue-228-build-workflow-hardening

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `5621201685b8c8bf2349f4342c2e5f632befa32f` — fix: stabilize push and dependency submission checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9f4363de08af8f1a67ec85c8f06aaa95fe84fec4` — fix: unblock workflow validation and dependency submission — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c0d9a3a71f36ceff47c9ce863710172b604e71fb` — fix: scope build checks to stable solution targets — Copilot App <223556219+Copilot@users.noreply.github.com>
- `af72f19c918035dcc2c5c4ae7a4c3ca5746259b0` — fix: stabilize variant and module workflow restores — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c16f38d69a286225b247752133337fc2006127a8` — fix: clear workflow failures in branch checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `587098fbd3f166244f747d14f920b4e01cf63690` — fix: harden build workflows and checks — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- `22b443d91e8fb9e780af1eaad4befe83ce88b01e` — fix: align platform tests target framework for restore — Copilot App <223556219+Copilot@users.noreply.github.com>

### Files
- `.github/QUICK_REFERENCE.md`
- `.github/WORKFLOWS.md`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/build-variant-test.yml`
- `.github/workflows/code-registry-update.yml`
- `.github/workflows/component-version-check.yml`
- `.github/workflows/helios-unified-plugin-validate.yml`
- `.github/workflows/multi-repo-sync.yml`
- `.github/workflows/status-dashboard.yml`
- `README_CURRENT_STATUS.md`
- `config/integrations/repositories.json`
- `docs/README_WORKFLOWS.md`
- `docs/workflows/WORKFLOWS_TROUBLESHOOTING.md`
- `docs/workflows/WORKFLOW_BUILD.md`
- `docs/workflows/WORKFLOW_INDEX.md`
- `scripts/validate_submodule_integrity.py`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/test_validate_submodule_integrity.py`

## origin/yolkster64-musical-spork

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `8e648274211603e2c51ca97ee53026ae78492bdb` — fix: harden adapter boundaries and projection routing — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4052a88646859827fb3101e8903a7dbfb83a6b70` — fix: close latest codex review gaps — Copilot App <223556219+Copilot@users.noreply.github.com>
- `1fcbac9c8775a766d1910b9696a1119673313a34` — fix: close remaining six-profile review gaps — Copilot App <223556219+Copilot@users.noreply.github.com>
- `15a0559c86629a92c90d19e2aaec806d888f4863` — fix: align test lanes with mainline suite — Copilot App <223556219+Copilot@users.noreply.github.com>
- `f26448048d02caee81ca0eea11a1de835f4e0a64` — fix: resolve six-profile contract review gaps — Copilot App <223556219+Copilot@users.noreply.github.com>
- `26536b21bdbb23ae7dfcb13bea8f2d8eb7d01212` — fix: harden phase10 user permissions and folder setup — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c540c6d4ea1dea447f3c6186cc2b97919fe2cf84` — fix: unblock CI compile and restore failures — Copilot App <223556219+Copilot@users.noreply.github.com>
- `a9c44811e86ac5ee6d2084e49f16564e8ce6b0b1` — fix: refresh HELIOS.Platform package lock — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b57e084780d5a47507e20fa23cabd2f5f6abf407` — fix: address six-profile review contract gaps — Copilot App <223556219+Copilot@users.noreply.github.com>
- `89d2c0b855bebbe00dbd68b9ede21f844285d8d4` — feat: complete six-profile delivery fabric wiring — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/nuget.yml`
- `.github/workflows/test-lanes.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `config/gui/monado-profile-shell.v2.json`
- `config/integrations/monadoblade-collaboration-projection.v1.json`
- `config/profiles/monadoblade-profiles.migration.v1-to-v2.json`
- `config/profiles/monadoblade-profiles.typed-adapter.v2-to-v1.json`
- `config/profiles/monadoblade-profiles.v2.json`
- `config/runtime/helios-fabric-services.v2.json`
- `config/storage/monadoblade-folder-hierarchy.v1.json`
- `config/usb/monadoblade-usb-wizard.v1.json`
- `docs/WIKI/README.md`
- `docs/architecture/MONADOBLADE_DELIVERY_FABRIC_V2.md`
- `docs/architecture/MONADOBLADE_PROFILE_STORAGE_GUI_SYSTEM.md`
- `scripts/utilities/wiki/generate-wiki.ps1`
- `src/core/HELIOS.Platform.Contracts/MonadobladeProfileContracts.cs`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoEngineUpdateService.cs`
- `src/core/HELIOS.Platform/Phase10/Users/AccountPermissionManager.cs`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/UserAccountManagementTests.cs`
- `src/core/HELIOS.Platform/Phase10/Users/UserDataDirectorySetup.cs`
- `src/core/HELIOS.Platform/packages.lock.json`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/yolkster64-reimagined-fiesta

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `df3a2ff3227b88abd9641ccd300ea1684244e6b3` — Tighten secret scan regex boundaries — Copilot App <223556219+Copilot@users.noreply.github.com>
- `8fcdb6634c73976160bbade9dc08d3489bde758e` — Merge remote-tracking branch 'origin/main' into yolkster64-reimagined-fiesta — Copilot App <223556219+Copilot@users.noreply.github.com>
- `53a6d8aed0bf06dc5b92752d47b80e9c8d058977` — fix: address PR review feedback and unblock CI checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9cb25bc388e5592ae7573077d067736888e65127` — Merge main into PR branch and resolve workflow conflict — Copilot App <223556219+Copilot@users.noreply.github.com>
- `2022dd83f20eb5f7bd023aaec7495e6f57c54b90` — docs: finalize Monadoblade six-profile fabric integration — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `config/profiles/monadoblade-profiles.v2.json`
- `docs/WIKI/README.md`
- `docs/architecture/MONADOBLADE_DELIVERY_FABRIC_V2.md`
- `docs/architecture/MONADOBLADE_PROFILE_STORAGE_GUI_SYSTEM.md`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/yolkster64-supreme-goggles

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `6223265065461448ba6c1afea576b654b1d026c4` — Merge origin/main into yolkster64-supreme-goggles — Copilot App <223556219+Copilot@users.noreply.github.com>
- `1888dc26a674968aca63726788658fca81342806` — test: import Xunit in v3 contracts test suite — Copilot App <223556219+Copilot@users.noreply.github.com>
- `162c557a36fc6317a470dd54e336128d60dc0a19` — ci: fix portable ownership and v3 contract validation — Copilot App <223556219+Copilot@users.noreply.github.com>
- `dc2f1ca78c48b98fa9c33bcaff92eb9b7982edb8` — ci: prevent secret-scan false positives in v2 contracts — Copilot App <223556219+Copilot@users.noreply.github.com>
- `61fabab9b33118a2bc58cb4ce20f51523d0096ea` — ci: fix module-matrix checkout and v2 schema contract validation — Copilot App <223556219+Copilot@users.noreply.github.com>
- `0bd348003c70cdb4ca987d87bbbbb66305ad9bff` — ci: resolve failing lane checks and portable build blockers — Copilot App <223556219+Copilot@users.noreply.github.com>
- `66e7a78c805b6ed4b06a22d7234528081c61ca8a` — test: replace filtered Assert.Single usage in webhook tests — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/test-lanes.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `config/monado-enterprise/v2/storage.contract.v2.json`
- `config/monado-enterprise/v2/synchronization.contract.v2.json`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `monado/helios-control/tests/Helios.Connect.Tests/WebhookTests.cs`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `schemas/monado-enterprise/v3/experience.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/index.schema.json`
- `schemas/monado-enterprise/v3/integration-projection.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/libraries.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/migration-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/profiles.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/repository-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/storage.contract.v3.schema.json`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/yolkster64-supreme-succotash

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `84c7eee2306e05f685c3c97b7e9fbc267fbb316f` — Fix privileged lane and user fallback review issues — Copilot App <223556219+Copilot@users.noreply.github.com>
- `f40d527f350de3a1db0bedf1da801c87787ff799` — Stabilize test-lane projects for Phase10 user coverage — Copilot App <223556219+Copilot@users.noreply.github.com>
- `50c96c2c0dc71e6f5f2211821e0a61f4646ae353` — Fix CI lockfile drift and stabilize Phase10 user tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `40d1dbe17203f2de306a0a94ed027e56eed09517` — Fix HELIOS build blockers in project and tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9ede6c0a57ee5c8876d9f3ddf28558de696120f3` — Merge origin/main into codex/windows-boot-security-rootkit-v1 — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `08ddacbe02f6172d60f21b79ae7eedd3e5448cb6` — Strengthen scan and registry guardrails — Yolkster64 <thepatman64@gmail.com>
- `83276a21cd774c4c54d962c5594c6e9c0c892862` — Clarify quick-scan default and rollback evidence — Yolkster64 <thepatman64@gmail.com>
- `6ecf1afce4c667e3b1bdefbabec741933481ea06` — Align scan policy with quick-scan default — Yolkster64 <thepatman64@gmail.com>
- `104e44551c9a95e901855a84ac8246ee04cee62d` — Make scheduled full scans explicit opt-in — Yolkster64 <thepatman64@gmail.com>
- `2ce0cf27b7fb5592957fdb382e026e17a473e7ad` — Document and expand registry rollback evidence — Yolkster64 <thepatman64@gmail.com>
- `0f756401d8a17e41d2504ee98b8cd67520e6df6b` — Document read-only registry inspection for CI — Yolkster64 <thepatman64@gmail.com>
- `c82ce24f29f069b1f55f9e97ee5fd0d9dea8a173` — Add Windows security parser and guardrail workflow — Yolkster64 <thepatman64@gmail.com>
- `7e829720f66b8a4b7f866be4df26b6fd7dc6ff2d` — Add startup audit and Defender scan task installer — Yolkster64 <thepatman64@gmail.com>
- `fb251f72023e76bbf7ffb92ad10051277cbec8f3` — Add guarded Microsoft Defender Offline rootkit recovery — Yolkster64 <thepatman64@gmail.com>
- `478856b014b7f601326ce28435003a5c4ad162cd` — Add guarded Defender and boot security baseline — Yolkster64 <thepatman64@gmail.com>
- `762f0d2642181090c5bc7dcebc2e2893f87ebc23` — Add non-mutating boot security posture audit — Yolkster64 <thepatman64@gmail.com>
- `6644803cd70ba25f8f6cb0b6b69b4426b177a6bc` — Document boot security and rootkit recovery workflow — Yolkster64 <thepatman64@gmail.com>
- `7750b10c86d45cfacb419561731daca9667d17e1` — Add guarded OpenAI security analysis template — Yolkster64 <thepatman64@gmail.com>
- `170215e9f2fee5f1d029ece9badf2df8ad0746aa` — Add Windows boot security policy manifest — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/nuget.yml`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoEngineUpdateService.cs`
- `src/core/HELIOS.Platform/Phase10/Users/AccountPermissionManager.cs`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/UserAccountManagementTests.cs`
- `src/core/HELIOS.Platform/Phase10/Users/UserDataDirectorySetup.cs`
- `src/core/HELIOS.Platform/packages.lock.json`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/HELIOS.Platform.Tests.Phase10.Quarantine.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/QuarantineSystemTests.cs`

## origin/agent/monadoblade-delivery-fabric-v2

- **Primary umbrella:** C++ native performance and security (`cpp-native-performance-security`)
- **Temporary integration branch:** `integration/train-cpp-native-performance-security`
- **Module owner:** HELIOS.Native, HELIOS.Security
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `9650469ea868ec561af778d28a4c9d94b998cdce` — fix: address Monadoblade review findings — Yolkster64 <thepatman64@gmail.com>
- `a2b19805e3b80ec7c3694015ae7cac33d03b8c24` — feat: establish Monadoblade delivery fabric v2 — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.devcontainer/devcontainer.json`
- `.github/workflows/validate-monadoblade-delivery-fabric.yml`
- `.gitignore`
- `README.md`
- `config/aihub/monadoblade-engine-registry.v1.json`
- `config/experience/monadoblade-effects.v1.json`
- `config/experience/monadoblade-living-environments.v1.json`
- `config/gui/monado-profile-shell.v2.json`
- `config/integrations/monadoblade-delivery-fabric.v1.json`
- `config/profiles/monadoblade-profiles.v2.json`
- `config/storage/monadoblade-storage-plan-template.v2.json`
- `docs/NAVIGATION.md`
- `docs/WIKI/Monadoblade-Delivery-Fabric.md`
- `docs/WIKI/README.md`
- `docs/architecture/MONADOBLADE_DELIVERY_FABRIC_V2.md`
- `docs/monadoblade/index.md`
- `scripts/validation/validate_monadoblade_delivery_fabric.py`
- `src/core/HELIOS.Platform.Contracts/MonadobladeDeliveryFabricContracts.cs`
- `src/core/HELIOS.Platform.Contracts/MonadobladeProfileContracts.cs`
- `src/native/HELIOS.Native.Performance/CMakeLists.txt`
- `src/native/HELIOS.Native.Performance/README.md`
- `src/native/HELIOS.Native.Performance/include/helios/monadoblade_environment_renderer.hpp`
- `src/native/HELIOS.Native.Performance/include/helios/monadoblade_profile_optimizer.hpp`
- `src/native/HELIOS.Native.Performance/shaders/monadoblade_environment_particles.hlsl`
- `tests/native/monadoblade_environment_smoke.cpp`

## origin/codex/add-pull_request-trigger-to-multi-repo-sync

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `d74e156b36939ef1eca885f16e03a6579c150fc7` — fix: address remaining multi-repo sync review comments — Copilot App <223556219+Copilot@users.noreply.github.com>
- `bc81c842fbd9ae5e16b5259e577313218788c46b` — ci: skip github package publish on PR checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `63500d551aa5fd2b1162baa9996dafef6d140e3b` — merge: update from main and resolve CI/review blockers — Copilot App <223556219+Copilot@users.noreply.github.com>
- `82f61ae93ace20c5c423dcf06b0061c68a75fa93` — fix: stabilize portable build and contract secret-scan — Copilot App <223556219+Copilot@users.noreply.github.com>
- `73854624bd4e47741fc5e6b6cd11a78380e268bc` — fix: restore portable contract tests and v3 schema compliance — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c904e27d27e97cdfeaf11f773e5a4fff92aaf5de` — Merge remote-tracking branch 'origin/main' into codex/add-pull_request-trigger-to-multi-repo-sync — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b32b009c074a97b249b37d8c464e3c9a84000c3d` — fix: align v2 contracts with schema and refresh test ownership — Copilot App <223556219+Copilot@users.noreply.github.com>
- `290a85e2615b5ee3663c1b0e97cf33e5f9078472` — test: fix F# telemetry type inference in profile scoring tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `8e1f27536ef5117248146ad1ca99ca4dee14832f` — ci: disable submodule checkout in node build workflows — Copilot App <223556219+Copilot@users.noreply.github.com>
- `936f427fa43a54d505078591a9e40ea4259194fb` — merge: resolve main conflicts and keep repository integrity checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d78a93414c7be933013d69c34b471ec74a573694` — fix: resolve integrity and dependency check failures — Copilot App <223556219+Copilot@users.noreply.github.com>
- `5741e203440e696679380dcc000e772b677767b7` — fix: validate submodule gitlinks — Yolkster64 <thepatman64@gmail.com>
- `73c6fce0f427119e51c30dfb4d2271af00a56498` — ci: enforce repository integrity validation — Yolkster64 <thepatman64@gmail.com>
- `079bece6e463029d826361fe366b07baa6d4457d` — ci: validate repository integrity on pull requests — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/multi-repo-sync.yml`
- `.github/workflows/nuget.yml`
- `config/monado-enterprise/v2/experience.contract.v2.json`
- `config/monado-enterprise/v2/index.json`
- `config/monado-enterprise/v2/profiles.contract.v2.json`
- `config/monado-enterprise/v2/repository-map.contract.v2.json`
- `config/monado-enterprise/v2/storage.contract.v2.json`
- `config/monado-enterprise/v2/synchronization.contract.v2.json`
- `config/monado-enterprise/v3/experience.contract.v3.json`
- `config/monado-enterprise/v3/index.json`
- `config/monado-enterprise/v3/integration-projection.contract.v3.json`
- `config/monado-enterprise/v3/libraries.contract.v3.json`
- `config/monado-enterprise/v3/migration-map.contract.v3.json`
- `config/monado-enterprise/v3/profiles.contract.v3.json`
- `config/monado-enterprise/v3/repository-map.contract.v3.json`
- `config/monado-enterprise/v3/storage.contract.v3.json`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `scripts/contracts/validate_monadoblade_delivery_fabric_v3.py`
- `scripts/integrations/validate_repository_integrity.py`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/HELIOS.Platform.Tests.Phase10.Quarantine.csproj`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`
- `tests/test_repository_integrity.py`

## origin/codex/azure-enterprise-agent-fleet

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** high
- **Merge-tree conflicts:** none
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `837c583902aa71b7a75b6d60dbcb3c04937cb820` — Use current Linear and Slack MCP endpoints — Yolkster64 <thepatman64@gmail.com>
- `5642fa61631666b231dfcb640f29de7afadbe311` — Add CI validation for enterprise automatic setup — Yolkster64 <thepatman64@gmail.com>
- `9511055227087b92ecedec3169537cef25c12e24` — Add Claude Code and Agent 365 provider contract — Yolkster64 <thepatman64@gmail.com>
- `5fe22c6b4d87c1c674d33c7a0fdca2b599228766` — Add enterprise setup controller tests — Yolkster64 <thepatman64@gmail.com>
- `a9080f3f75e9db54a716f987e87780d41b18705f` — Add Windows launcher for enterprise setup — Yolkster64 <thepatman64@gmail.com>
- `2fc4cb0a8d5a4b574a7432c1869d003df0ed354b` — Add automatic enterprise setup controller — Yolkster64 <thepatman64@gmail.com>
- `4c259befcf2e0f13409de52c55a2bbd5b8bd04ef` — Document one-command enterprise setup — Yolkster64 <thepatman64@gmail.com>
- `bd73b6d260c1e0bb53a2848573b5cb6f5def2541` — Add enterprise automatic setup manifest — Yolkster64 <thepatman64@gmail.com>
- `336d23419446cc61194278f71209c24c5b160028` — Add safe Claude Code project permissions — Yolkster64 <thepatman64@gmail.com>
- `e274edaf9a4c227dcf2365bae6ed9ef84feba910` — Add Claude Code project MCP configuration — Yolkster64 <thepatman64@gmail.com>
- `bcb4470b03cf2db9e30c635006dd198cdcc6284e` — Add Claude Code governance instructions — Yolkster64 <thepatman64@gmail.com>
- `e7bfee3b897c1f7d1458dbb85534053bb0b5baf2` — Validate Microsoft 365 cloud and DevOps extension — Yolkster64 <thepatman64@gmail.com>
- `2a43084f51d7ac2cd3c620ef2cffb36f9ea1b8f3` — Document Microsoft 365 Copilot cloud and DevOps control plane — Yolkster64 <thepatman64@gmail.com>
- `86700c082d2fc589897b050919a4dba4f1f6b125` — Add easy Microsoft 365 Copilot integration skill — Yolkster64 <thepatman64@gmail.com>
- `d2192bbf2448294cf80a0554f82e675cf8aac3b2` — Add Microsoft 365 cloud and DevOps connection registry — Yolkster64 <thepatman64@gmail.com>
- `80b4d54c548b5aefecba5c55a1eb724dd385e444` — Add Microsoft 365 cloud and DevOps subagent registry — Yolkster64 <thepatman64@gmail.com>
- `7849e5aba9e70feb5b1479bcac90700b7622abfd` — Document XCore Copilot and Azure toolchain setup — Yolkster64 <thepatman64@gmail.com>
- `5482965d28f40670cf0d27a78d7f8ab4ea0062bd` — Validate XCore Copilot and Azure toolchain subagents — Yolkster64 <thepatman64@gmail.com>
- `16fcead37b8181ad5f433a837c8327e926b8a857` — Add XCore Copilot and toolchain registry validation — Yolkster64 <thepatman64@gmail.com>
- `90625b61a4f111f17841f18ff70f40917f3baddb` — Add typed XCore Copilot and toolchain contracts — Yolkster64 <thepatman64@gmail.com>
- `433b00e308156d8fc8c9809e7dad1be2073ac8db` — Add Azure toolchain setup skill — Yolkster64 <thepatman64@gmail.com>
- `78aa562ba7ee6f9492293e76e5313ef248c89a47` — Add Microsoft Copilot subagent skill — Yolkster64 <thepatman64@gmail.com>
- `9134b78e6d620425b2c0d6aea7a015b215df8cde` — Add XCore runtime skill — Yolkster64 <thepatman64@gmail.com>
- `c59f59b4fdd06acb8fec86a84d4057609dc19c0c` — Add Microsoft Copilot and XCore connection registry — Yolkster64 <thepatman64@gmail.com>
- `cd542987c40867b06cf4cf7858bc0b1a7129bf0f` — Add Azure and Microsoft Copilot toolchain manifest — Yolkster64 <thepatman64@gmail.com>
- `735c59ce0c3cc398d4e09762451308cc9e45050e` — Add XCore Copilot and Azure toolchain subagents — Yolkster64 <thepatman64@gmail.com>
- `8aa4ad0e640cd804cedb054e2efaddea92be391b` — Build typed enterprise agents during validation — Yolkster64 <thepatman64@gmail.com>
- `24dea5c2f6042f7d4852628ee5855731c1c1fee9` — Document enterprise agents C# module — Yolkster64 <thepatman64@gmail.com>
- `f2d1a3b486ad38bafdf5cd1d1647866c84e6bb0a` — Add enterprise agent registry loader — Yolkster64 <thepatman64@gmail.com>
- `9098a90692d505458e35ad2f37f90d76c6440c34` — Add typed enterprise agent contracts — Yolkster64 <thepatman64@gmail.com>
- `a82a5ce434b036f3051feb72024367760d0363f0` — Add enterprise agents C# project — Yolkster64 <thepatman64@gmail.com>
- `909e5ea6beefade065794df8db106c3af3aa1954` — Add enterprise agent fleet validation workflow — Yolkster64 <thepatman64@gmail.com>
- `b9d9220a551afda421bcd02005330e366af120eb` — Add custom connection registry — Yolkster64 <thepatman64@gmail.com>
- `a064369fd9fb27e9647eb462c88333884de640f4` — Add OpenAI agent and plugin bridge skill — Yolkster64 <thepatman64@gmail.com>
- `d7a72959f3384b09eec8bd17f9653450276bc4cc` — Add deployment assurance skill — Yolkster64 <thepatman64@gmail.com>
- `b567284c4857f77d2727ee200206ecb99fb71348` — Add enterprise connector plane skill — Yolkster64 <thepatman64@gmail.com>
- `7641485f290a1fe5a5a6f0a54cc38a8ac5387dfd` — Add Azure enterprise control skill — Yolkster64 <thepatman64@gmail.com>
- `de0509c29f17e281121c0da58b727d9ef599bc2e` — Document enterprise deployment agent fleet — Yolkster64 <thepatman64@gmail.com>
- `e931f13bb64dba443d8a92aaf7c51d809df23fcf` — Add enterprise deployment manager agent contract — Yolkster64 <thepatman64@gmail.com>
- `089e74d6dc2b23d97654720102059ce23946f0ff` — Add enterprise deployment sub-agent registry — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.claude/settings.json`
- `.github/workflows/enterprise-agent-fleet.yml`
- `.github/workflows/enterprise-automatic-setup.yml`
- `.mcp.json`
- `CLAUDE.md`
- `agents/enterprise-deployment-manager/AGENTS.md`
- `config/agents/enterprise-deployment-agent-fleet.v1.json`
- `config/agents/enterprise-deployment-subagents.v1.json`
- `config/agents/microsoft-365-cloud-devops-subagents.v1.json`
- `config/bootstrap/enterprise-automatic-setup.v1.json`
- `config/plugins/custom-connections.v1.json`
- `config/plugins/microsoft-365-cloud-devops-connections.v1.json`
- `config/plugins/microsoft-copilot-xcore-connections.v1.json`
- `config/providers/claude-code-agent365.v1.json`
- `config/toolchains/azure-microsoft-copilot-toolchain.v1.json`
- `docs/agents/ENTERPRISE_DEPLOYMENT_AGENT_FLEET.md`
- `docs/agents/M365_COPILOT_CLOUD_DEVOPS_CONTROL_PLANE.md`
- `docs/agents/XCORE_COPILOT_AZURE_TOOLCHAIN.md`
- `docs/operations/ENTERPRISE_AUTOMATIC_SETUP.md`
- `skills/azure-enterprise-control/SKILL.md`
- `skills/azure-toolchain-setup/SKILL.md`
- `skills/deployment-assurance/SKILL.md`
- `skills/enterprise-connector-plane/SKILL.md`
- `skills/m365-copilot-easy-integration/SKILL.md`
- `skills/microsoft-copilot-subagent/SKILL.md`
- `skills/openai-plugin-bridge/SKILL.md`
- `skills/xcore-runtime/SKILL.md`
- `src/agents/HELIOS.EnterpriseAgents/AgentContracts.cs`
- `src/agents/HELIOS.EnterpriseAgents/AgentRegistryLoader.cs`
- `src/agents/HELIOS.EnterpriseAgents/ExtendedRegistryLoader.cs`
- `src/agents/HELIOS.EnterpriseAgents/HELIOS.EnterpriseAgents.csproj`
- `src/agents/HELIOS.EnterpriseAgents/README.md`
- `src/agents/HELIOS.EnterpriseAgents/SubagentContracts.cs`
- `tests/tools/test_helios_setup.py`
- `tools/helios-setup/helios-setup.cmd`
- `tools/helios-setup/helios_setup.py`

## origin/codex/optimize-code-automation-and-github-usage-7few7a

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `2955b81b3ea46248e11797f72d71a7c6c8859aaa` — Restrict deployment jobs to main pushes — Yolkster64 <thepatman64@gmail.com>
- `35a6c5e0db1a3d0e5b299c356e5c31756cd61e0f` — Guard Azure deployment on develop pushes — Yolkster64 <thepatman64@gmail.com>
- `a61594a6c89b9ebcb8dee9030501278445f5d640` — Harden cross-repo consolidation and restore smoke checks — Yolkster64 <thepatman64@gmail.com>
- `140a9f23cccd2e2fc2d57b6897c0669dc528c465` — Optimize GitHub automation and consolidation setup — Yolkster64 <thepatman64@gmail.com>
- `ea6df3ec01d90905e12d87ed991acbc785a449de` — Add consolidation source manifest — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ai-code-review.yml`
- `.github/workflows/analysis.yml`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/build-variant-test.yml`
- `.github/workflows/ci-validation.yml`
- `.github/workflows/code-checks.yml`
- `.github/workflows/code-registry-update.yml`
- `.github/workflows/deploy.yml`
- `.github/workflows/documentation-update.yml`
- `.github/workflows/dotnet-build.yml`
- `.github/workflows/github-system.yml`
- `.github/workflows/multi-repo-sync.yml`
- `.github/workflows/nuget.yml`
- `.github/workflows/phase-build.yml`
- `.github/workflows/publish-to-packagemanagers.yml`
- `.github/workflows/quality.yml`
- `.github/workflows/status-dashboard.yml`
- `.github/workflows/verify.yml`
- `.github/workflows/wiki-generator.yml`
- `.gitignore`
- `.gitmodules`
- `BUILD_VARIANTS.md`
- `COMPONENT_ANALYSIS.md`
- `COMPONENT_MATRIX.md`
- `DELIVERY_MANIFEST.md`
- `GITHUB_PROJECT_SETUP.md`
- `HELIOS.Platform.CrossPlatform.sln`
- `HELIOS.Platform.sln`
- `MERGE_SOURCE_MANIFEST.yaml`
- `PROJECT_BOARD_QUICK_START.md`
- `build.ps1`
- `docs/CROSS_REPO_CONSOLIDATION.md`
- `docs/WIKI_INDEX.md`
- `manifest.json`
- `package.json`
- `scripts/deploy/azure/deploy-infrastructure.sh`
- `scripts/deploy/azure/verify-azure-prereqs.sh`
- `scripts/github/prepare-consolidation.py`
- `scripts/github/prepare_consolidation.py`
- `scripts/github/validate-workflows.py`
- `scripts/setup/setup-azure-cli.sh`
- `src/core/HELIOS.Platform/Core/Intelligence/MLModelManager.cs`
- `src/core/HELIOS.Platform/Core/Intelligence/PredictiveAnalytics.cs`
- `src/core/HELIOS.Platform/Phase10/Sandbox/HELIOS.Platform.Phase10.Sandbox.csproj`
- `src/core/HELIOS.Platform/Phase10/Sandbox/SandboxOrchestrator.cs`
- `src/core/HELIOS.Platform/Phase10/Users/HELIOS.Platform.Phase10.Users.csproj`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/HELIOS.Platform.Phase10.Users.Tests.csproj`
- `src/phases/master-deploy.ps1`
- `src/phases/phase-0-preflight.ps1`
- `src/phases/phase-1-infrastructure.ps1`
- `src/phases/phase-2-agents.ps1`
- `src/phases/phase-3-ai-services.ps1`
- `src/phases/phase-4-security.ps1`
- `src/phases/phase-5-monitoring.ps1`
- `src/phases/phase-6-verification.ps1`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/HELIOS.Platform.Tests.Phase10.Quarantine.csproj`

## origin/codex/update-build_graph.py-for-classification-and-reporting

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `9c5fc9da6d4235acab5c7b0951e78563cd7d604a` — Potential fix for pull request finding 'CodeQL / Workflow does not contain permissions' — Yolkster64 <thepatman64@gmail.com>
- `43a762286a59032ecf184b72cf33294a54692f8c` — Add super branch unification plan — Yolkster64 <thepatman64@gmail.com>
- `7274899ebeb8ab5e0b360acb7ed3a6d6fa65bde6` — Add full integration matrix coverage — Yolkster64 <thepatman64@gmail.com>
- `c7005687b153d4559cd8e5b516988e8eec80bc8b` — Add deep agent readiness coverage — Yolkster64 <thepatman64@gmail.com>
- `43522a85efc2bcd04eb928795a30873f00f79f44` — Make finish command run full setup by default — Yolkster64 <thepatman64@gmail.com>
- `4958bda4adf20919615064d3d14c38d908a611fd` — Add super stack readiness report — Yolkster64 <thepatman64@gmail.com>
- `3ee73b8bb936db5287ee64ec884ac7e3ed39f6eb` — Add safe Azure connection pipeline — Yolkster64 <thepatman64@gmail.com>
- `9932b1727cadb7d90829b0779e32e74c4e321f46` — Restore analytics test dependencies in build graph — Yolkster64 <thepatman64@gmail.com>
- `63837ca2baf863be0a8688573d7ffa92c24ee591` — Add root finish command — Yolkster64 <thepatman64@gmail.com>
- `89d182e012ac76b319886559152c5710042b5ef9` — Add one-command finish setup — Yolkster64 <thepatman64@gmail.com>
- `5dcf3531af5ca0b74723789d0ff5764473439a6e` — Generate finish task backlog — Yolkster64 <thepatman64@gmail.com>
- `cb1858bf547702a330300732059112b0a7255dcc` — Add safe finish apply orchestration — Yolkster64 <thepatman64@gmail.com>
- `ec08b244b7adfb7690be4787221cd4e1aa255586` — Default Azure what-if planning context — Yolkster64 <thepatman64@gmail.com>
- `d05608bdc2ccde1d991400f749589cf44468728b` — Ignore local bootstrap tools — Yolkster64 <thepatman64@gmail.com>
- `c6f6e3baad57ecbfbc5cd7ac2a5f0b2bace64618` — Show finish readiness recommended fixes — Yolkster64 <thepatman64@gmail.com>
- `6fac448c629da64b3b28317f330b82bb45f0c0f8` — Resolve preflight and Azure planning warnings — Yolkster64 <thepatman64@gmail.com>
- `de016becfde661de54856bde4880949054828528` — Add finish readiness automation coverage — Yolkster64 <thepatman64@gmail.com>
- `d1c72c14239a2541bbfa86f5a5899176e4b9f6a8` — Expand build graph automation lanes — Yolkster64 <thepatman64@gmail.com>
- `72e3db42a407cb4415869c5b7346a28a6d85aaad` — Improve build graph result reporting — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/build-graph-automation.yml`
- `.gitignore`
- `config/build-graph.json`
- `config/hermes-fleet.example.json`
- `config/security-preflight-allowlist.json`
- `finish.sh`
- `scripts/agents/hermes_fleet_readiness.py`
- `scripts/analysis/merge_prune_recommendations.py`
- `scripts/analysis/super_branch_unification.py`
- `scripts/apply/finish_readiness_apply.py`
- `scripts/apply/generate_finish_tasks.py`
- `scripts/azure/azure_connection_pipeline.py`
- `scripts/azure/azure_what_if.py`
- `scripts/build_graph/build_graph.py`
- `scripts/control/helios-control.py`
- `scripts/dashboard/generate-gui.py`
- `scripts/integrations/deep_agent_readiness.py`
- `scripts/integrations/full_integration_matrix.py`
- `scripts/integrations/readiness_score.py`
- `scripts/integrations/super_stack_readiness.py`
- `scripts/security/apply_gate_preflight.py`
- `scripts/security/secret_preflight.py`
- `scripts/setup/finish-easy-setup.sh`
- `scripts/setup/helios-dev.sh`
- `tests/scripts/test_build_graph.py`

## origin/codex/update-workflow-for-subnet-id-handling

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `b99f98003241168b3ea0bfd58fa5e14ecf3f14c6` — Tighten prod egress prerequisites and migration safety — Copilot App <223556219+Copilot@users.noreply.github.com>
- `8f11912d2bdbef97305dc0d736a8170979669584` — Close review gaps for prod network rollout — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4e4a529dfe6d5fb0a571c2a2b47134ad74911008` — Strengthen prod infra rollout safeguards — Copilot App <223556219+Copilot@users.noreply.github.com>
- `78bc73157008e65cbc6715bf895b0913cb22c67d` — Harden production deployment approval and subnet binding — Copilot App <223556219+Copilot@users.noreply.github.com>
- `91af09ae76d972865e2cfd7fb5ffa6a87f11bc90` — Fix prod subnet bootstrap and runner routing — Copilot App <223556219+Copilot@users.noreply.github.com>
- `6e4cc49bd57bfadcc185c219a96b2cd2962fd8f6` — Address review feedback for subnet rollout and boundary checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `35ef74f5200562f273ca6c64e27af5d99339a221` — Harden subnet rollout checks and fix NuGet restore compatibility — Copilot App <223556219+Copilot@users.noreply.github.com>
- `7c218e001623cd930411f648847bca2c72e712d1` — Require production Container Apps subnet — Yolkster64 <thepatman64@gmail.com>
- `c8f1b6ffa2b5edd86d6a254d7841c88949c9eb9a` — Fix governed Azure network review findings — Yolkster64 <thepatman64@gmail.com>
- `734b033d14e321974f0ba1488214a6abfa8a0aaf` — Test governed network profile isolation — Yolkster64 <thepatman64@gmail.com>
- `dc1388e36a00710537589564fc840a2fc91798ae` — Fix governed private edge deployment — Yolkster64 <thepatman64@gmail.com>
- `5971fc7fa7c60cccd4d9b7c64931fa7a1b6040d7` — Add governed private Azure edge network — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/azure-infra.yml`
- `.github/workflows/helios-cloud-deploy.yml`
- `infra/azure/README.md`
- `infra/azure/main.bicep`
- `infra/azure/modules/hub-governance.bicep`
- `infra/azure/modules/keyvault-private-cutover.bicep`
- `infra/azure/modules/keyvault.bicep`
- `infra/azure/modules/network.bicep`
- `infra/azure/modules/observability.bicep`
- `infra/azure/modules/private-edge.bicep`
- `infra/azure/modules/private-endpoints.bicep`
- `infra/azure/modules/storage.bicep`
- `infra/azure/modules/vnet-flow-log.bicep`
- `infra/azure/parameters/dev.json`
- `monado/helios-control/config/network-paths.json`
- `monado/helios-control/docs/AZURE_INTERACTIVE_ONBOARDING.md`
- `monado/helios-control/infra/connector.bicep`
- `monado/helios-control/infra/containerapp-internal-dns.bicep`
- `monado/helios-control/infra/main.bicep`
- `monado/helios-control/scripts/Connect-HeliosAzureInteractive.ps1`
- `monado/helios-control/scripts/Deploy-HeliosAzureConnector.ps1`
- `monado/helios-control/scripts/Invoke-HeliosEdgeAutomation.ps1`
- `monado/helios-control/scripts/Invoke-HeliosProvisionPreview.ps1`
- `monado/helios-control/scripts/bootstrap-helios-azure-oidc.sh`
- `monado/helios-control/src/Helios.Connect.Api/Helios.Connect.Api.csproj`
- `monado/helios-control/src/Helios.Connect.Api/NetworkPathCatalog.cs`
- `monado/helios-control/src/Helios.Connect.Api/Program.cs`
- `monado/helios-control/src/Helios.Connect.Api/SetupWizardService.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/NetworkPathCatalogTests.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/SetupWizardTests.cs`
- `monado/helios-control/tests/test_azure_deployment_contract.py`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`

## origin/yolkster64-complete-governed-azure-activation-key-v

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** high
- **Merge-tree conflicts:** none
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `31480f88e569451a6b4bcd690053f62ea6e1dc9e` — Harden portable and contract CI checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `2dd03fe39720b302a03422fca5605795e3cfdf63` — Fix contract tests xUnit imports — Copilot App <223556219+Copilot@users.noreply.github.com>
- `e5ef3df2f1e53947409e8651369eaa72d409d13d` — Fix PR workflow checkout regressions — Copilot App <223556219+Copilot@users.noreply.github.com>
- `fe66f9467c4a96c0c71b566f872a3f3f3d8b9b35` — Address review hardening gaps — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ebb9ee5c8e8f8bc5d7d7e175dca2113064d48815` — Complete governed Azure activation hardening — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/code-checks.yml`
- `.github/workflows/component-version-check.yml`
- `.github/workflows/copilot-package.yml`
- `.github/workflows/helios-cloud-deploy.yml`
- `.github/workflows/helios-edge-automation-validate.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/helios-unified-plugin-validate.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `monado/helios-control/appPackage/manifest.json`
- `monado/helios-control/config/edge-automation.json`
- `monado/helios-control/connector/helios-azure-connector.openapi.yaml`
- `monado/helios-control/connector/mcp-manifest.example.json`
- `monado/helios-control/docs/AZURE_CONNECTOR_DEPLOYMENT.md`
- `monado/helios-control/docs/AZURE_INTERACTIVE_ONBOARDING.md`
- `monado/helios-control/docs/EDGE_AUTOMATION.md`
- `monado/helios-control/docs/IDENTITY_AND_TOKEN_SETUP.md`
- `monado/helios-control/docs/MICROSOFT_TOOLCHAIN.md`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `monado/helios-control/infra/connector.bicep`
- `monado/helios-control/infra/main.bicep`
- `monado/helios-control/infra/main.parameters.example.json`
- `monado/helios-control/infra/main.parameters.json`
- `monado/helios-control/scripts/Connect-HeliosAzureInteractive.ps1`
- `monado/helios-control/scripts/Deploy-HeliosAzureConnector.ps1`
- `monado/helios-control/scripts/Invoke-HeliosEdgeAutomation.ps1`
- `monado/helios-control/scripts/Invoke-HeliosProvisionPreview.ps1`
- `monado/helios-control/scripts/Test-HeliosCloudConnection.ps1`
- `monado/helios-control/scripts/bootstrap-helios-azure-oidc.sh`
- `monado/helios-control/src/Helios.Connect.Api/Program.cs`
- `monado/helios-control/src/Helios.Connect.Api/SetupWizardService.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/SetupWizardTests.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/WebhookTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/yolkster64-define-governed-github-codex-claude-code

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `6b0cfa1a4dcbada95610ae69fb6e9a3e351c993c` — Fix hidden-attestation parsing and artifact slugging — Copilot App <223556219+Copilot@users.noreply.github.com>
- `2531d19be89ceb7b66602614a2087624e19a20f4` — Harden governance workflow and CI artifact handling — Copilot App <223556219+Copilot@users.noreply.github.com>
- `00cef7d856877b22f6f9f9586abaf127cefd975f` — Avoid submodule checkout hard-fail in module builds — Copilot App <223556219+Copilot@users.noreply.github.com>
- `a0a1934785a28e5f1e28b094733eb4289d8a3e4d` — Merge remote-tracking branch 'origin/main' into yolkster64-define-governed-github-codex-claude-code — Copilot App <223556219+Copilot@users.noreply.github.com>
- `e643bfb734341c8e5474d0f541b66b1e2f98c5f0` — fix(governance): secure workflow bootstrap and renames — Yolkster64 <thepatman64@gmail.com>
- `c51d7f5b232d1889dea013c111d35f30eb43976b` — Restore submodule checkout for module builds — Copilot App <223556219+Copilot@users.noreply.github.com>
- `641d9451a7627406090846954fbe3e14a9a7a619` — Address governance review findings — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9de1dc97102510080844e87ce8150a935d37419d` — Resolve secret-scan false positives in contracts — Copilot App <223556219+Copilot@users.noreply.github.com>
- `5e0a4ac9bcc8d4f5d3914b9afcb9b1d1672d4e68` — Fix portable CI build and contract tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c58d6a4f405bd8e405473f01059f9590f7823197` — Fix contract schema validation failures — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b1b4713b9ecb9acecd3ab53d7134956a0f521aa7` — Merge remote-tracking branch 'origin/main' into yolkster64-define-governed-github-codex-claude-code — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d8c69fb5dd489d608d5f20c1c1ccdf30f1614fc1` — fix(ci): disable Azure submodule checkout — Copilot App <223556219+Copilot@users.noreply.github.com>
- `682b2cfbd9a80b22bd248a7ddda4ca78f2220939` — fix(ci): unblock portable and node matrix checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `5e4331582b8581cfc4b92d993690fc2f2bdba335` — fix(ci): accept multiline branch exception notes — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4a2c332025dd3a2e01299775662629f0fa81434a` — fix(ci): accept H2 governance security section — Copilot App <223556219+Copilot@users.noreply.github.com>
- `53db4b8f47910f546332544ccab3ae8da94b860f` — Merge origin/main into yolkster64-define-governed-github-codex-claude-code — Copilot App <223556219+Copilot@users.noreply.github.com>
- `0dabfbe19afb4e07862e370e5a76eb9c285aac77` — docs(governance): add Codex/Claude collaboration contract | Fixes #231 — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/pull_request_template.md`
- `.github/workflows/ai-collaboration-governance.yml`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/unified-agent-contract.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `azure-pipelines.yml`
- `docs/architecture/UNIFIED_AGENT_COMMUNICATION.md`
- `docs/governance/GITHUB_CODEX_CLAUDE_COLLABORATION_CONTRACT.md`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `schemas/monado-enterprise/v3/experience.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/index.schema.json`
- `schemas/monado-enterprise/v3/integration-projection.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/libraries.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/migration-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/profiles.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/repository-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/storage.contract.v3.schema.json`
- `scripts/control/validate_ai_collaboration_pr.py`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`
- `tests/scripts/test_validate_ai_collaboration_pr.py`

## origin/yolkster64-define-monado-enterprise-experience-fabr

- **Primary umbrella:** F# analytics and prediction (`fsharp-analytics-prediction`)
- **Temporary integration branch:** `integration/train-fsharp-analytics-prediction`
- **Module owner:** HELIOS.Analytics.FSharp
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `f688b7b9cea87f49272c57fdaa4ceb37dab7b822` — fix(ci): close latest monado review gaps — Copilot App <223556219+Copilot@users.noreply.github.com>
- `f8a21295b6fb1279e4e0ec9361002081b3b36ad6` — fix(ci): address remaining monado review blockers — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3ae2c355e9b482b5622bd1a2cea2abaf551ff3d2` — test(fsharp): add explicit v2 profile scoring coverage — Copilot App <223556219+Copilot@users.noreply.github.com>
- `81f59776cb209ecbf1e29cbd5d97649483b956e8` — chore(tests): remove dead v2 contract test stub — Copilot App <223556219+Copilot@users.noreply.github.com>
- `811e7b5bad971e63903427d9567eb3a667ad8742` — fix(ci): close codex review gaps for monado contracts — Copilot App <223556219+Copilot@users.noreply.github.com>
- `6d5f29426285831330692e4a379644360af98a56` — fix(ci): harden monado v2 contract safeguards — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ba96d6a802c30e1cb5a3f6cff9df2d3454004316` — fix(ci): ignore  metadata in schema-pair validation — Copilot App <223556219+Copilot@users.noreply.github.com>
- `0415fcf65e27f885f8bfb1601d65e1dd77cbfaf2` — Merge origin/main into yolkster64-define-monado-enterprise-experience-fabr — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d08ac2bcd56bc2a881292f41b66dd1ab9d06a41d` — feat: define Monado enterprise experience fabric v2 — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/pages/index.html`
- `.github/workflows/build-variant-test.yml`
- `.github/workflows/helios-control-plane.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `README.md`
- `config/monadoblade/experience-fabric/alvis-tool-budgets.v2.json`
- `config/monadoblade/experience-fabric/chroma-wyvern.contract.v2.json`
- `config/monadoblade/experience-fabric/monado-enterprise-experience-fabric.v2.json`
- `config/monadoblade/experience-fabric/openai-proposal.schema.v2.json`
- `config/monadoblade/experience-fabric/profile-catalog.v2.json`
- `config/monadoblade/experience-fabric/profile-experience.contract.v2.json`
- `config/monadoblade/experience-fabric/repository-ownership.contract.v2.json`
- `config/monadoblade/experience-fabric/storage.contract.v2.json`
- `config/monadoblade/experience-fabric/synchronization.contract.v2.json`
- `config/monadoblade/experience-fabric/xml/ai-server.profile.v2.xml`
- `config/monadoblade/experience-fabric/xml/core.profile.v2.xml`
- `config/monadoblade/experience-fabric/xml/developer.profile.v2.xml`
- `config/monadoblade/experience-fabric/xml/gamer.profile.v2.xml`
- `config/monadoblade/experience-fabric/xml/personal.profile.v2.xml`
- `config/monadoblade/experience-fabric/xml/profile-manifest.v2.xsd`
- `config/monadoblade/experience-fabric/xml/studio.profile.v2.xml`
- `config/monadoblade/experience-fabric/xml/sysadmin.profile.v2.xml`
- `config/monadoblade/experience-fabric/xml/sysops.profile.v2.xml`
- `docs/WIKI/MONADO_ENTERPRISE_EXPERIENCE_FABRIC_V2.md`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `schemas/monado-enterprise/v3/experience.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/index.schema.json`
- `schemas/monado-enterprise/v3/integration-projection.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/libraries.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/migration-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/profiles.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/repository-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/storage.contract.v3.schema.json`
- `scripts/control/tests/test_validate_monado_enterprise_experience_v2.py`
- `scripts/control/validate_monado_enterprise_experience_v2.py`
- `src/core/HELIOS.Platform.Contracts/MonadoEnterpriseExperienceFabricV2Contracts.cs`
- `src/native/HELIOS.Native.Performance/CMakeLists.txt`
- `src/native/HELIOS.Native.Performance/include/helios/monadoblade_profile_optimizer.hpp`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/MonadoEnterpriseProfileScoringV2Tests.fs`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/ProfilePolicyOptimizerTests.fs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceFabricV2ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`
- `tests/native/monadoblade_profile_optimizer_smoke.cpp`

## origin/yolkster64-fluffy-fortnight

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `fbc463f47720a361f6b58dffcd3b60a4245754ca` — ci: address new review feedback for module checks and path safety — Copilot App <223556219+Copilot@users.noreply.github.com>
- `f9f7edcfedb232cf8d85b7dd1a5246abe2533770` — ci: tighten path-safety checks and manual publish condition — Copilot App <223556219+Copilot@users.noreply.github.com>
- `fe86eee6a47aa19016f1724c0e01a7ea0ced2346` — ci: address review feedback on lane safety and ownership — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ff1645eb6db8a2dca9852ed0f4ae60a0e56ced9e` — Merge remote-tracking branch 'origin/main' into yolkster64-fluffy-fortnight — Copilot App <223556219+Copilot@users.noreply.github.com>
- `47d1119d1b128cfc18482559b8a15343fe06e473` — ci: skip github package publish on pull requests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `19a862efbc350cc30e7344d67561950211ac03b0` — ci: always write changed-powershell manifest — Copilot App <223556219+Copilot@users.noreply.github.com>
- `e511c5bcb8a71b059b505c28ca72627dee0393a3` — ci: stabilize ownership manifest and secret scan regex — Copilot App <223556219+Copilot@users.noreply.github.com>
- `da0b45f175129e258e54cabfac1b4d8e5a8e58e7` — contracts: allow optional \ in v3 schema documents — Copilot App <223556219+Copilot@users.noreply.github.com>
- `32a0b5db72055a99bf64fb83844fd7018cb68b7d` — ci: run Azure pipeline on all branches and PRs — Copilot App <223556219+Copilot@users.noreply.github.com>
- `a3379bf6ca54596d9c79862e55051065367b940d` — ci: fix invalid unified contract workflows — Copilot App <223556219+Copilot@users.noreply.github.com>
- `228b98a9005db5d9525fd5979dc7ec1109034908` — ci: harden Azure pipeline checkout and add smoke step — Copilot App <223556219+Copilot@users.noreply.github.com>
- `92262935fda7d2142f58c741a501a8bf94272b05` — Merge origin/main and resolve CI/test conflicts — Copilot App <223556219+Copilot@users.noreply.github.com>
- `10a09d2d322cab3c329b5c70fd3884cc2a500908` — ci: fix portable restore, schema validation, and ownership manifest — Copilot App <223556219+Copilot@users.noreply.github.com>
- `90b42d301fc5db29a95ae361d9c929094ab44754` — fix: repair portable tests and stabilize lane execution — Copilot App <223556219+Copilot@users.noreply.github.com>
- `6275612af1ea5ab46af6ba9297198dfc6be38fa0` — ci: disable submodule checkout in node workflow lanes — Copilot App <223556219+Copilot@users.noreply.github.com>
- `5569ced77faa2ce58172f11157cfd5299f68c03b` — test: fix xUnit2031 webhook assertions (#242) — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/code-checks.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/helios-unified-plugin-validate.yml`
- `.github/workflows/nuget.yml`
- `.github/workflows/test-lanes.yml`
- `.github/workflows/unified-agent-contract.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `azure-pipelines.yml`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `monado/helios-control/tests/Helios.Connect.Tests/WebhookTests.cs`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `schemas/monado-enterprise/v3/experience.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/index.schema.json`
- `schemas/monado-enterprise/v3/integration-projection.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/libraries.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/migration-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/profiles.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/repository-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/storage.contract.v3.schema.json`
- `src/core/HELIOS.Platform/Phase10/Users/UserDataDirectorySetup.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/yolkster64-glowing-goggles

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `69ad8aca900ed2365f236afa5165e66dd5cd6213` — xcore9: harden runtime contract and policy snapshots — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9c1146927027575382a502bd32a4a2b974863635` — tests: avoid Path.Combine silent-drop pattern — Copilot App <223556219+Copilot@users.noreply.github.com>
- `e350c6215d1ce3f655efa01c38b14afefd9704db` — fix(tests): harden safe path combine guard — Copilot App <223556219+Copilot@users.noreply.github.com>
- `07390574bebc21e711f81740249bfb0cb958ffc7` — fix(ci): close remaining review gaps in gate and startup contract — Copilot App <223556219+Copilot@users.noreply.github.com>
- `30e96374e7f711bc05ddfea0429b8902d5402f87` — fix(ci): address new review findings on gates and xcore9 policy — Copilot App <223556219+Copilot@users.noreply.github.com>
- `699c0c80fd8340549d619626bd8247605ec61912` — fix(ci): address review findings across gates and xcore9 contracts — Copilot App <223556219+Copilot@users.noreply.github.com>
- `811b5f4d1658a45c92351aed0841901f4305d86d` — Merge remote-tracking branch 'origin/main' into yolkster64-glowing-goggles — Copilot App <223556219+Copilot@users.noreply.github.com>
- `e272beb7b54f2ebafb8ad4fbee5a0ccd190cfbe8` — fix(ci): stabilize changed-file manifest and portable restore — Copilot App <223556219+Copilot@users.noreply.github.com>
- `21e8a4bfd28f5df42828875e310b5448d3207707` — fix: reduce false positives in secret-pattern CI scans — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ee825e65f35a250ddae1979cf75f49a52fb30c76` — fix: restore xUnit imports in contract test suite — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d03b33692aa7e545cc6f3f252be6597520f3709a` — fix: avoid submodule checkout in variant matrix jobs — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b9d3764f67b5e9d4a88fa4da47a982ff1f72a2d1` — fix: make test ownership manifest generation CI-stable — Copilot App <223556219+Copilot@users.noreply.github.com>
- `09e12035aa387f091a0d1eee0d88133a051eb0c2` — fix: allow schema URI metadata in contract validation — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9d486ea0aa736369009b0c4b0f30d94c28bc2e62` — fix: unblock portable and module-matrix CI lanes — Copilot App <223556219+Copilot@users.noreply.github.com>
- `cba81974aef30a4aea6393c7c09750db43a2e0e5` — Merge remote-tracking branch 'origin/main' into yolkster64-glowing-goggles — Copilot App <223556219+Copilot@users.noreply.github.com>
- `df5fa00bc92a6aa02ee941df09a1324e960d98d7` — feat: add governed XCore9 contracts and CI gates — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/pull_request_template.md`
- `.github/workflows/ai-collaboration-gate.yml`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/code-checks.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/unified-agent-contract.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `azure-pipelines.yml`
- `docs/governance/GITHUB_CODEX_CLAUDE_COLLABORATION_CONTRACT.md`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `monado/helios-control/README.md`
- `monado/helios-control/config/agent-fleet.json`
- `monado/helios-control/config/microsoft-agents.json`
- `monado/helios-control/config/xcore9-knaa-model.v1.json`
- `monado/helios-control/config/xcore9-runtime-matrix.v1.json`
- `monado/helios-control/config/xcore9-specialization-packs.v1.json`
- `monado/helios-control/docs/HERMES_MICROSOFT_LEARNING.md`
- `monado/helios-control/docs/IMPLEMENTATION_STATUS.md`
- `monado/helios-control/docs/XCORE9_KNAA_MODEL.md`
- `monado/helios-control/docs/XCORE9_RUNTIME_MATRIX.md`
- `monado/helios-control/docs/XCORE9_SPECIALIZATION_PACKS.md`
- `monado/helios-control/src/Helios.Connect.Contracts/XCore9Governance.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/XCore9GovernanceTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/yolkster64-issue-241-hermes-xcore9-specialization-p

- **Primary umbrella:** Hermes/XCore fleet (`hermes-xcore-fleet`)
- **Temporary integration branch:** `integration/train-hermes-xcore-fleet`
- **Module owner:** HELIOS.Hermes, XCore
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `cde2f7ab3d56c41a1047238f9e0af4578c330ce3` — Harden secret-scan key pattern matching — Copilot App <223556219+Copilot@users.noreply.github.com>
- `8924ed7fa1c209394215d8f853e09eca2153c336` — Stabilize portable CI ownership validation — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b888e861fcf1a42eee66a8130f4fbd0e96a16119` — Merge main into PR branch — Copilot App <223556219+Copilot@users.noreply.github.com>
- `1b2fede118d613e37b8b4d7151eca1ce4461e569` — Fix required CI and specialization output contracts — Copilot App <223556219+Copilot@users.noreply.github.com>
- `5a6d39f5c87b14a5217de520ab32fd96bd29f504` — Resolve remaining code-quality review threads — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d759aacd8a8e7143e9e7d3fcede7cd966b495da7` — Address specialization review findings — Copilot App <223556219+Copilot@users.noreply.github.com>
- `43e8a31d136c5700dcac977b4e6bd0ca900c8384` — Disable submodule fetch in variant CI — Copilot App <223556219+Copilot@users.noreply.github.com>
- `2e8473f528e8642a4f8eac5725815af7bb99a8b4` — Align MCP tool contract with specialization planner — Copilot App <223556219+Copilot@users.noreply.github.com>
- `913af37806e93ce94a23307b71f4c5bbd1e1beb9` — Merge main into specialization policy branch — Copilot App <223556219+Copilot@users.noreply.github.com>
- `112b7aadace1c151ccdf2fc6f443e02f9795248d` — Implement Hermes/XCore9 specialization pack policy — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/code-checks.yml`
- `.github/workflows/helios-edge-automation-validate.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `monado/helios-control/README.md`
- `monado/helios-control/config/edge-automation.json`
- `monado/helios-control/config/hermes-xcore9-specialization-packs.example.json`
- `monado/helios-control/config/hermes-xcore9-specialization-packs.json`
- `monado/helios-control/config/hermes-xcore9-specialization.schema.json`
- `monado/helios-control/config/microsoft-agents.json`
- `monado/helios-control/connector/mcp-manifest.example.json`
- `monado/helios-control/docs/ARCHITECTURE.md`
- `monado/helios-control/docs/EDGE_AUTOMATION.md`
- `monado/helios-control/docs/IMPLEMENTATION_STATUS.md`
- `monado/helios-control/docs/MULTI_AGENT_WORKBENCH.md`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `monado/helios-control/scripts/Test-HeliosCloudConnection.ps1`
- `monado/helios-control/src/Helios.Connect.Api/Helios.Connect.Api.csproj`
- `monado/helios-control/src/Helios.Connect.Api/Program.cs`
- `monado/helios-control/src/Helios.Connect.Api/SpecializationPolicyEvaluator.cs`
- `monado/helios-control/src/Helios.Connect.Contracts/SpecializationContracts.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/SpecializationPolicyTests.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/WebhookTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/yolkster64-legendary-journey

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `5646df3ef9ea3aca8a5883789c2f33425003c73e` — Fix new review findings in canonical build and CI matrix — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c020e594b469437e0da3f7b726ba0e0f670a49cc` — Add explicit prerequisite skip attribute for quarantine init tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d83e18b69bff7532e8eb1a2a4a45167cf108805b` — Stabilize Phase10 environment-gated tests and broaden src test scope — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b97eccf4339a46b09e533af26513960f22ff8000` — Fix portable ownership manifest generation — Copilot App <223556219+Copilot@users.noreply.github.com>
- `570d961c53869be0eae35da8f705e655688ebdbd` — Refresh test ownership manifest — Copilot App <223556219+Copilot@users.noreply.github.com>
- `22c906520efce412ea20c31dff92449b3df0e80b` — Stabilize CI test lanes and quarantine coverage — Copilot App <223556219+Copilot@users.noreply.github.com>
- `f3fef2300c0639cf8e08d9b32ab5b087e8a54dce` — Harden Phase10 Users nullability and crypto APIs — Copilot App <223556219+Copilot@users.noreply.github.com>
- `0d86708079a496269db905e06f86af273383cf50` — Fix solution build path and stabilize Phase10 user tests — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/nuget.yml`
- `FULL_GITHUB_INTEGRATION_DEPLOYMENT_MANUAL.md`
- `HELIOS.Platform.sln`
- `PHASE_6_LAUNCH_CONTEXT.md`
- `WORKFLOW_ANALYSIS.md`
- `config/agent-specializations.json`
- `config/hybrid-integration-roadmap.json`
- `docs/architecture/HYBRID_INTEGRATION_EXPANSION_PLAN.md`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `scripts/agents/agent_fleet_autopilot.py`
- `scripts/agents/branch_fix_agents.py`
- `scripts/agents/branch_test_autofix_plan.py`
- `scripts/analysis/agent_language_framework.py`
- `scripts/analysis/code_learning_atlas.py`
- `scripts/analysis/deep_branch_code_score.py`
- `scripts/analysis/knowledge_absorption_engine.py`
- `scripts/analysis/language_engine_catalog.py`
- `scripts/analysis/language_role_optimizer.py`
- `scripts/analysis/module_submodule_test_matrix.py`
- `scripts/integrations/full_integration_matrix.py`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoEngineUpdateService.cs`
- `src/core/HELIOS.Platform/Phase10/Quarantine/QuarantineManager.cs`
- `src/core/HELIOS.Platform/Phase10/Users/AccountActivityMonitor.cs`
- `src/core/HELIOS.Platform/Phase10/Users/AccountPermissionManager.cs`
- `src/core/HELIOS.Platform/Phase10/Users/Interfaces/IUserAccountManagementService.cs`
- `src/core/HELIOS.Platform/Phase10/Users/MultiProfileCoordinator.cs`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/UserAccountManagementTests.cs`
- `src/core/HELIOS.Platform/Phase10/Users/UserAccountProvisioner.cs`
- `src/core/HELIOS.Platform/Phase10/Users/UserDataDirectorySetup.cs`
- `src/core/HELIOS.Platform/Phase10/Users/UserSecurityInitializer.cs`
- `src/core/HELIOS.Platform/packages.lock.json`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `src/tests/IntegrationSmokeTests.cs`
- `src/tests/SmokeTests.cs`
- `tests/HELIOS.Platform.Tests/GlobalUsings.cs`
- `tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/HELIOS.Platform.Tests.Phase10.Quarantine.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/QuarantineSystemTests.cs`
- `tests/HELIOS.Platform.Tests/System/AccessibilityE2ETests.cs`
- `tests/HELIOS.Platform.Tests/System/CloudSyncE2ETests.cs`
- `tests/HELIOS.Platform.Tests/System/ConcurrencyE2ETests.cs`
- `tests/HELIOS.Platform.Tests/System/DriverUpdateE2ETests.cs`
- `tests/HELIOS.Platform.Tests/System/ErrorRecoveryE2ETests.cs`
- `tests/HELIOS.Platform.Tests/System/PerformanceReportE2ETests.cs`
- `tests/HELIOS.Platform.Tests/System/ProfileSwitchE2ETests.cs`
- `tests/HELIOS.Platform.Tests/System/ThreatWorkflowE2ETests.cs`
- `tests/HELIOS.Platform.Tests/System/UserLoginOnboardingE2ETests.cs`

## origin/yolkster64-security-re-enable-m365-agents-toolkit-c

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `e4803afd0d9fb291da5976aac3c9907f9d230fe1` — fix(security): enforce cli-matrix pin parity in audit gate — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4d22e18f8666419f4af6128bf265f59425be8dc1` — chore(ci): retrigger blocked check suite — Copilot App <223556219+Copilot@users.noreply.github.com>
- `bcc917b4f6df1f204d9d1976822b5f3c72dbedbc` — fix(ci): scope version checks to repository-local manifests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ddb1239dd9f9ee7fcefb64b9d6c2ade8e79bd783` — fix(ci): include submodule manifests in version checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `70d089eae0503b3a40767286321d3d2568a39f5a` — fix(ci): clear stale native exit code in toolkit gate — Copilot App <223556219+Copilot@users.noreply.github.com>
- `550e60319e0949558ffc6feccb1f45e2c74e156f` — fix(security): gate auto-install on direct audit graph — Copilot App <223556219+Copilot@users.noreply.github.com>
- `83b024f6063ea60595b91cd2e2e1f9bf36baf996` — fix(review): close CI and contract feedback gaps — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c214cd84db66ad31bbde80ebbeabc0a4dbff98d6` — merge: resolve main conflicts for issue-202 CI lane — Copilot App <223556219+Copilot@users.noreply.github.com>
- `17d41edcf61efaee76730b1c099d7d6635b355bc` — fix(ci): restore minimal portable project for win-x64 — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b2d44cf45a9755d182160993a89e9dc048b47dbb` — fix(ci): include global-usings test ownership entry — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4ea8e55925178b7460d09cbdfe9d94dc3afa9344` — fix(ci): eliminate contract scan false positive and refresh lock — Copilot App <223556219+Copilot@users.noreply.github.com>
- `a010d909b35662ad5b32b0b5a7365b77c6d56f6c` — fix(ci): provide xunit global using for contract tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `8119dcc9b64b69171f834f42f736bcae311620ac` — fix(ci): align merge-ref contract and ownership checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `877cb36b6d940b38e273d00144c2178352f3864d` — fix(ci): unblock monado enterprise contract validation — Copilot App <223556219+Copilot@users.noreply.github.com>
- `b022a44b8ef5a840e1ed7e875727ca4c38b36f7c` — fix(ci): escape manifest template assertion in workflow — Copilot App <223556219+Copilot@users.noreply.github.com>
- `040168dc62be495ad0c98b0052881554a5ce49de` — fix(ci): stabilize PR checks for merge — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ab01ba5629e8e2491731017e541b0702d4dc3fe2` — fix(security): re-enable m365 toolkit via locked audit gate — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/component-version-check.yml`
- `.github/workflows/copilot-package.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/helios-unified-plugin-validate.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `.gitignore`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `monado/helios-control/.github/workflows/copilot-package.yml`
- `monado/helios-control/config/cli-matrix.json`
- `monado/helios-control/config/microsoft-toolchain.json`
- `monado/helios-control/docs/IMPLEMENTATION_STATUS.md`
- `monado/helios-control/docs/MICROSOFT_TOOLCHAIN.md`
- `monado/helios-control/scripts/Invoke-HeliosCliMatrix.ps1`
- `monado/helios-control/scripts/Invoke-M365AgentsToolkitAuditGate.ps1`
- `monado/helios-control/scripts/invoke-helios-cli-matrix.sh`
- `monado/helios-control/security/m365agentstoolkit-cli-audit/package-lock.json`
- `monado/helios-control/security/m365agentstoolkit-cli-audit/package.json`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `schemas/monado-enterprise/v3/experience.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/index.schema.json`
- `schemas/monado-enterprise/v3/integration-projection.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/libraries.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/migration-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/profiles.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/repository-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/storage.contract.v3.schema.json`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/GlobalUsings.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/yolkster64-xcore9-evaluator-knaa-scoring-policy-gat

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `4e39fb5feb6e8ebd4c609bea011c9e28e235b073` — Merge main into KNAA branch — Copilot App <223556219+Copilot@users.noreply.github.com>
- `5100169e74fa763eada43cb8f5a0997c25de8318` — Harden CI workflow guards — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c0beceec97d211f65d9f0dd76fe6163e317d1cdf` — Fix contract schema and test-lane CI failures — Copilot App <223556219+Copilot@users.noreply.github.com>
- `6418ca04ed844e74c61e57346aa5c8de2ac2d0e0` — Fix monado enterprise F# test typing — Copilot App <223556219+Copilot@users.noreply.github.com>
- `70d1a05d2efb2eb680e766eed0f6327698ab47f1` — Refresh test ownership manifest — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c1015ef50f5da8db0b725483f7e2668770ba24d3` — Harden KNAA recovery and module discovery — Copilot App <223556219+Copilot@users.noreply.github.com>
- `af3652392ab8c2c72b99c3589cab439c2c4dc773` — Address KNAA review findings — Copilot App <223556219+Copilot@users.noreply.github.com>
- `dd6f0cd8d6713b849251c3815ea82ad3d9aa4432` — Fix checkout failures in module build workflows — Copilot App <223556219+Copilot@users.noreply.github.com>
- `1927d4dd144813a18bf04fbc570413899a4c41bf` — Merge main into KNAA evaluator branch — Copilot App <223556219+Copilot@users.noreply.github.com>
- `a5cc7e5fe4149df1f234f4e2727daadfab84c1c9` — Implement KNAA evaluator scoring and policy gates — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `eng/test/test-ownership.json`
- `monado/helios-control/README.md`
- `monado/helios-control/config/knaa-value-model.v1.schema.json`
- `monado/helios-control/docs/CONNECTION_RUNBOOK.md`
- `monado/helios-control/docs/IMPLEMENTATION_STATUS.md`
- `monado/helios-control/docs/XCORE9_KNAA_VALUE_MODEL.md`
- `monado/helios-control/src/Helios.Connect.Api/ControlRuns.cs`
- `monado/helios-control/src/Helios.Connect.Api/KnaaEvaluation.cs`
- `monado/helios-control/src/Helios.Connect.Api/Program.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/ControlRunTests.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/KnaaEvaluatorTests.cs`
- `schemas/monado-enterprise/v2/experience.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/index.schema.json`
- `schemas/monado-enterprise/v2/profiles.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/storage.contract.v2.schema.json`
- `schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json`
- `schemas/monado-enterprise/v3/experience.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/index.schema.json`
- `schemas/monado-enterprise/v3/integration-projection.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/libraries.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/migration-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/profiles.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/repository-map.contract.v3.schema.json`
- `schemas/monado-enterprise/v3/storage.contract.v3.schema.json`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/codex/create-analytics-project-structure-and-apis-hulohi

- **Primary umbrella:** F# analytics and prediction (`fsharp-analytics-prediction`)
- **Temporary integration branch:** `integration/train-fsharp-analytics-prediction`
- **Module owner:** HELIOS.Analytics.FSharp
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- `c0cb7257d97f62dab0c8f4e82ee061252a8b987f` — Prune generated artifacts and add cleanup strategy — Yolkster64 <thepatman64@gmail.com>

### Files
- `.devcontainer/codespaces-secrets.env.example`
- `.github/workflows/azure-infra.yml`
- `.github/workflows/branch-intelligence.yml`
- `.github/workflows/helios-control-plane.yml`
- `.gitignore`
- `COMMAND_CENTER.md`
- `Directory.Build.props`
- `config/azure-control.example.json`
- `config/build-graph.json`
- `config/cross-access-profiles.example.json`
- `config/execution-order.json`
- `config/github-control.example.json`
- `config/hybrid-integration-roadmap.json`
- `config/integrations.example.json`
- `config/secrets-map.example.json`
- `docs/architecture/AZURE_HYBRID_ARCHITECTURE.md`
- `docs/architecture/BRANCH_CLEANUP_STRATEGY.md`
- `docs/architecture/DASHBOARD_PUBLISHING_STRATEGY.md`
- `docs/architecture/FSHARP_RANKING_BRIDGE.md`
- `docs/architecture/GENERATED_ARTIFACTS_POLICY.md`
- `docs/architecture/HYBRID_INTEGRATION_EXPANSION_PLAN.md`
- `docs/architecture/WHOLE_PROJECT_NEXT_STEPS.md`
- `docs/integration/BRANCH_INTELLIGENCE.md`
- `docs/integration/CLOUDSHELL_GITHUB_AZURE_SETUP.md`
- `docs/integration/CONTROL_PLANE_COMMANDS.md`
- `docs/integration/CROSS_ACCESS_PROFILES.md`
- `docs/integration/PR_UPDATE_WORKFLOW.md`
- `docs/integration/VISUAL_STUDIO_MAUI_SETUP.md`
- `docs/integration/WEB_CONTROL_PLANE.md`
- `docs/integration/remote-manifest.json`
- `docs/microsoft-365/COPILOT_INTEGRATION.md`
- `docs/security/CONTROL_PLANE_PERMISSIONS.md`
- `helios.sh`
- `infra/azure/README.md`
- `infra/azure/main.bicep`
- `infra/azure/modules/keyvault.bicep`
- `infra/azure/modules/network.bicep`
- `infra/azure/modules/observability.bicep`
- `infra/azure/modules/storage.bicep`
- `infra/azure/parameters/dev.json`
- `reports/README.md`
- `scripts/ai/enrich-ideas.py`
- `scripts/analysis/branch_intelligence.py`
- `scripts/analysis/hybrid_gap_analysis.py`
- `scripts/analysis/merge_prune_recommendations.py`
- `scripts/analysis/prune_generated_artifacts.py`
- `scripts/analysis/repo_inventory.py`
- `scripts/azure/azure-inventory.py`
- `scripts/azure/sync-keyvault-secrets.sh`
- `scripts/build_graph/build_graph.py`
- `scripts/cloudshell/helios-cloudshell.sh`
- `scripts/codex/generate-codex-tasks.py`
- `scripts/control/doctor.py`
- `scripts/control/helios-control.py`
- `scripts/dashboard/generate-actions.py`
- `scripts/dashboard/generate-gui.py`
- `scripts/github/github-inventory.py`
- `scripts/github/update-pr-from-reports.py`
- `scripts/github/update-wiki-from-reports.py`
- `scripts/graphs/generate_graphs.py`
- `scripts/integrations/check-connections.py`
- `scripts/integrations/cross_access_profiles.py`
- `scripts/integrations/readiness_score.py`
- `scripts/sandbox/clean-sandbox-workspaces.sh`
- `scripts/sandbox/run-sandbox-workspace.sh`
- `scripts/setup/bootstrap-local-tools.sh`
- `scripts/setup/helios-dev.sh`
- `scripts/web/helios-web.py`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/analytics/HELIOS.Analytics.FSharp/Models/AnalyticsModels.fs`
- `src/analytics/HELIOS.Analytics.FSharp/ParallelWorkloads.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Prediction/PredictionWorkloads.fs`
- `src/analytics/HELIOS.Analytics.FSharp/PublicApi.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Statistics/AnalyticsWorkloads.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Statistics/MathWorkloads.fs`
- `src/core/HELIOS.Platform.Contracts/AnalyticsContracts.cs`
- `src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj`
- `src/native/HELIOS.Native.Performance/CMakeLists.txt`
- `src/native/HELIOS.Native.Performance/README.md`
- `status-site/index.md`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/AnalyticsEngineTests.fs`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj`

## origin/codex/update-build_graph.py-for-classification-and-reporting-kuifvm

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `8bab299f77c6e50acbe9c483fe27b024ea0924bc` — Add automatic HELIOS exe web launcher — Yolkster64 <thepatman64@gmail.com>
- `2dc59ec7ee8f5ef81c5e9d4df3468f08549bdc37` — Add HELIOS exe web sandbox — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/aihub-self-learning-growth.yml`
- `.github/workflows/aihub-supershell-vault-wizard.yml`
- `.github/workflows/branch-absorption-multicloud.yml`
- `.github/workflows/branch-fix-agents.yml`
- `.github/workflows/build-graph-automation.yml`
- `.gitignore`
- `config/agent-specializations.json`
- `config/aihub-language-skill-profiles.json`
- `config/aihub-learning-knowledge-store.example.json`
- `config/aihub-live-flags.example.json`
- `config/aihub-module-blueprint.json`
- `config/aihub-supershell-vault.example.json`
- `config/aihub-unified-control-plane.example.json`
- `config/build-graph.json`
- `config/hermes-fleet.example.json`
- `config/knowledge-sources.json`
- `config/language-decision-variables.json`
- `config/language-role-strategy.json`
- `config/security-preflight-allowlist.json`
- `docs/integration/AIHUB_UNIFIED_CONTROL_PLANE.md`
- `docs/runbooks/AIHUB_FINISH_BUILD_SAVE_RUN.md`
- `finish.sh`
- `scripts/agents/agent_fleet_autopilot.py`
- `scripts/agents/agent_fleet_control_catalog.py`
- `scripts/agents/agent_specialization_matrix.py`
- `scripts/agents/branch_fix_agents.py`
- `scripts/agents/branch_test_autofix_plan.py`
- `scripts/agents/hermes_fleet_readiness.py`
- `scripts/analysis/agent_language_framework.py`
- `scripts/analysis/aihub_language_skill_profiles.py`
- `scripts/analysis/aihub_learning_feedback_loop.py`
- `scripts/analysis/aihub_module_blueprint.py`
- `scripts/analysis/aihub_self_learning_notes.py`
- `scripts/analysis/branch_absorption_multicloud_plan.py`
- `scripts/analysis/code_learning_atlas.py`
- `scripts/analysis/commit_window_unification.py`
- `scripts/analysis/complex_code_grading.py`
- `scripts/analysis/control_plane_knowledge_matrix.py`
- `scripts/analysis/deep_branch_code_score.py`
- `scripts/analysis/document_code_absorption_ranker.py`
- `scripts/analysis/knowledge_absorption_engine.py`
- `scripts/analysis/language_decision_matrix.py`
- `scripts/analysis/language_engine_catalog.py`
- `scripts/analysis/language_role_optimizer.py`
- `scripts/analysis/legacy_algorithm_recovery.py`
- `scripts/analysis/merge_prune_recommendations.py`
- `scripts/analysis/module_submodule_organizer.py`
- `scripts/analysis/module_submodule_test_matrix.py`
- `scripts/analysis/recover_missing_branch_work.py`
- `scripts/analysis/simple_build_center.py`
- `scripts/analysis/super_branch_unification.py`
- `scripts/apply/finish_readiness_apply.py`
- `scripts/apply/generate_finish_tasks.py`
- `scripts/azure/azure_connection_pipeline.py`
- `scripts/azure/azure_what_if.py`
- `scripts/build_graph/build_graph.py`
- `scripts/control/helios-control.py`
- `scripts/dashboard/generate-gui.py`
- `scripts/integrations/aihub_command_ide.py`
- `scripts/integrations/aihub_connectivity_guide.py`
- `scripts/integrations/aihub_full_framework.py`
- `scripts/integrations/aihub_integration_graph.py`
- `scripts/integrations/aihub_learning_knowledge_store.py`
- `scripts/integrations/aihub_learning_rules.py`
- `scripts/integrations/aihub_live_flags.py`
- `scripts/integrations/aihub_super_shell.py`
- `scripts/integrations/aihub_supershell_vault_wizard.py`
- `scripts/integrations/aihub_unified_control_plane.py`
- `scripts/integrations/deep_agent_readiness.py`
- `scripts/integrations/full_integration_matrix.py`
- `scripts/integrations/readiness_score.py`
- `scripts/integrations/super_stack_readiness.py`
- `scripts/security/apply_gate_preflight.py`
- `scripts/security/secret_preflight.py`
- `scripts/setup/agent-runner-easy-setup.sh`
- `scripts/setup/auto-exe-web.sh`
- `scripts/setup/build-run-exe.sh`
- `scripts/setup/finish-easy-setup.sh`
- `scripts/setup/helios-dev.sh`
- `scripts/setup/save-run-bundle.sh`
- `scripts/setup/simple-build.sh`
- `src/analytics/HELIOS.Analytics.FSharp/AIHub/AiHubLearningEngine.fs`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/core/HELIOS.Platform.Contracts/AIHubEngineContracts.cs`
- `src/core/HELIOS.Platform.Contracts/BranchAbsorptionContracts.cs`
- `src/core/HELIOS.Platform.Contracts/ComplexCodeGradingContracts.cs`
- `src/core/HELIOS.Platform.Contracts/GuiAndUsbWizardModules.cs`
- `src/core/HELIOS.Platform/wwwroot/remote-console/css/styles.css`
- `src/core/HELIOS.Platform/wwwroot/remote-console/index.html`
- `src/core/HELIOS.Platform/wwwroot/remote-console/js/app.js`
- `src/native/HELIOS.Native.Performance/include/helios/aihub_native_engine.hpp`
- `tests/scripts/test_build_graph.py`

## origin/codex/update-build_graph.py-for-classification-and-reporting-lhug7d

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `42941661ae595d5dce718e5387869325168b5abd` — Add knowledge baked code fix routing — Yolkster64 <thepatman64@gmail.com>
- `9f2fd238219acf69f1fe2ee240044ad6f820465d` — Add single command IDE for Codex Copilot Azure APIs — Yolkster64 <thepatman64@gmail.com>
- `864dfda5750ecf4d93a05486dfc27c4337166d36` — Add GitHub Copilot to IDE mesh — Yolkster64 <thepatman64@gmail.com>
- `92d64c4939518c1b97b487a78e125a518ba10fe2` — Add finish build save run workflow — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/aihub-self-learning-growth.yml`
- `.github/workflows/aihub-supershell-vault-wizard.yml`
- `.github/workflows/branch-absorption-multicloud.yml`
- `.github/workflows/branch-fix-agents.yml`
- `.github/workflows/build-graph-automation.yml`
- `.gitignore`
- `config/agent-specializations.json`
- `config/aihub-language-skill-profiles.json`
- `config/aihub-learning-knowledge-store.example.json`
- `config/aihub-live-flags.example.json`
- `config/aihub-module-blueprint.json`
- `config/aihub-supershell-vault.example.json`
- `config/aihub-unified-control-plane.example.json`
- `config/build-graph.json`
- `config/hermes-fleet.example.json`
- `config/knowledge-sources.json`
- `config/language-decision-variables.json`
- `config/language-role-strategy.json`
- `config/security-preflight-allowlist.json`
- `docs/integration/AIHUB_UNIFIED_CONTROL_PLANE.md`
- `docs/runbooks/AIHUB_FINISH_BUILD_SAVE_RUN.md`
- `finish.sh`
- `scripts/agents/agent_fleet_autopilot.py`
- `scripts/agents/agent_fleet_control_catalog.py`
- `scripts/agents/agent_specialization_matrix.py`
- `scripts/agents/branch_fix_agents.py`
- `scripts/agents/branch_test_autofix_plan.py`
- `scripts/agents/hermes_fleet_readiness.py`
- `scripts/analysis/agent_language_framework.py`
- `scripts/analysis/aihub_language_skill_profiles.py`
- `scripts/analysis/aihub_learning_feedback_loop.py`
- `scripts/analysis/aihub_module_blueprint.py`
- `scripts/analysis/aihub_self_learning_notes.py`
- `scripts/analysis/branch_absorption_multicloud_plan.py`
- `scripts/analysis/code_learning_atlas.py`
- `scripts/analysis/commit_window_unification.py`
- `scripts/analysis/complex_code_grading.py`
- `scripts/analysis/control_plane_knowledge_matrix.py`
- `scripts/analysis/deep_branch_code_score.py`
- `scripts/analysis/document_code_absorption_ranker.py`
- `scripts/analysis/knowledge_absorption_engine.py`
- `scripts/analysis/language_decision_matrix.py`
- `scripts/analysis/language_engine_catalog.py`
- `scripts/analysis/language_role_optimizer.py`
- `scripts/analysis/legacy_algorithm_recovery.py`
- `scripts/analysis/merge_prune_recommendations.py`
- `scripts/analysis/module_submodule_organizer.py`
- `scripts/analysis/module_submodule_test_matrix.py`
- `scripts/analysis/recover_missing_branch_work.py`
- `scripts/analysis/simple_build_center.py`
- `scripts/analysis/super_branch_unification.py`
- `scripts/apply/finish_readiness_apply.py`
- `scripts/apply/generate_finish_tasks.py`
- `scripts/azure/azure_connection_pipeline.py`
- `scripts/azure/azure_what_if.py`
- `scripts/build_graph/build_graph.py`
- `scripts/control/helios-control.py`
- `scripts/dashboard/generate-gui.py`
- `scripts/integrations/aihub_command_ide.py`
- `scripts/integrations/aihub_connectivity_guide.py`
- `scripts/integrations/aihub_full_framework.py`
- `scripts/integrations/aihub_integration_graph.py`
- `scripts/integrations/aihub_learning_knowledge_store.py`
- `scripts/integrations/aihub_learning_rules.py`
- `scripts/integrations/aihub_live_flags.py`
- `scripts/integrations/aihub_super_shell.py`
- `scripts/integrations/aihub_supershell_vault_wizard.py`
- `scripts/integrations/aihub_unified_control_plane.py`
- `scripts/integrations/deep_agent_readiness.py`
- `scripts/integrations/full_integration_matrix.py`
- `scripts/integrations/readiness_score.py`
- `scripts/integrations/super_stack_readiness.py`
- `scripts/security/apply_gate_preflight.py`
- `scripts/security/secret_preflight.py`
- `scripts/setup/agent-runner-easy-setup.sh`
- `scripts/setup/finish-easy-setup.sh`
- `scripts/setup/helios-dev.sh`
- `scripts/setup/save-run-bundle.sh`
- `scripts/setup/simple-build.sh`
- `src/analytics/HELIOS.Analytics.FSharp/AIHub/AiHubLearningEngine.fs`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/core/HELIOS.Platform.Contracts/AIHubEngineContracts.cs`
- `src/core/HELIOS.Platform.Contracts/BranchAbsorptionContracts.cs`
- `src/core/HELIOS.Platform.Contracts/ComplexCodeGradingContracts.cs`
- `src/core/HELIOS.Platform.Contracts/GuiAndUsbWizardModules.cs`
- `src/core/HELIOS.Platform/wwwroot/remote-console/css/styles.css`
- `src/core/HELIOS.Platform/wwwroot/remote-console/index.html`
- `src/core/HELIOS.Platform/wwwroot/remote-console/js/app.js`
- `src/native/HELIOS.Native.Performance/include/helios/aihub_native_engine.hpp`
- `tests/scripts/test_build_graph.py`

## origin/copilot/get-all-issues-and-commits

- **Primary umbrella:** F# analytics and prediction (`fsharp-analytics-prediction`)
- **Temporary integration branch:** `integration/train-fsharp-analytics-prediction`
- **Module owner:** HELIOS.Analytics.FSharp
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `d2ac94a10b974542741228cc5326a949576f301d` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `3489f1ae5b2ed245f00b61a183a2f09cd0fb4484` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `4b043ebc480b4b2ff3acd36c289864fc687c2ed0` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `69516328aab52d96d08b61519e20fdd01ea74913` — Add bounded XCore-9 evaluation service — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `HELIOS.Platform.slnx`
- `docs/architecture/XCORE9_SERVICE.md`
- `scripts/agents/agent_fleet_autopilot.py`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/analytics/HELIOS.Analytics.FSharp/XCoreAnalytics.fs`
- `src/core/HELIOS.Platform.Contracts/XCore9Contracts.cs`
- `src/services/HELIOS.XCore9/HELIOS.XCore9.csproj`
- `src/services/HELIOS.XCore9/XCore9Options.cs`
- `src/services/HELIOS.XCore9/XCore9Service.cs`
- `tests/HELIOS.XCore9.Tests/HELIOS.XCore9.Tests.csproj`
- `tests/HELIOS.XCore9.Tests/XCore9ServiceTests.cs`

## origin/copilot/setup-pull-requests-and-issues

- **Primary umbrella:** F# analytics and prediction (`fsharp-analytics-prediction`)
- **Temporary integration branch:** `integration/train-fsharp-analytics-prediction`
- **Module owner:** HELIOS.Analytics.FSharp
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `d2ac94a10b974542741228cc5326a949576f301d` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `3489f1ae5b2ed245f00b61a183a2f09cd0fb4484` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `4b043ebc480b4b2ff3acd36c289864fc687c2ed0` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `69516328aab52d96d08b61519e20fdd01ea74913` — Add bounded XCore-9 evaluation service — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `HELIOS.Platform.slnx`
- `docs/architecture/XCORE9_SERVICE.md`
- `scripts/agents/agent_fleet_autopilot.py`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/analytics/HELIOS.Analytics.FSharp/XCoreAnalytics.fs`
- `src/core/HELIOS.Platform.Contracts/XCore9Contracts.cs`
- `src/services/HELIOS.XCore9/HELIOS.XCore9.csproj`
- `src/services/HELIOS.XCore9/XCore9Options.cs`
- `src/services/HELIOS.XCore9/XCore9Service.cs`
- `tests/HELIOS.XCore9.Tests/HELIOS.XCore9.Tests.csproj`
- `tests/HELIOS.XCore9.Tests/XCore9ServiceTests.cs`

## origin/codex/create-analytics-project-structure-and-apis

- **Primary umbrella:** F# analytics and prediction (`fsharp-analytics-prediction`)
- **Temporary integration branch:** `integration/train-fsharp-analytics-prediction`
- **Module owner:** HELIOS.Analytics.FSharp
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `1a607ee021834a5ff3ca82e9bc07e34df469fe34` — Add readiness doctor and PR update workflow — Yolkster64 <thepatman64@gmail.com>
- `398a31cc35d9061c245f4c5ff05db3464fe7f458` — Add cross-access profiles and GUI dashboard — Yolkster64 <thepatman64@gmail.com>
- `2ab5161d5a1e22d12d0e6e02e724bdf8eacb2e32` — Add whole-project inventory and hybrid roadmap — Yolkster64 <thepatman64@gmail.com>
- `862b1da6f7f2ca597b05bb4f2dbc7aced0698409` — Align control plane execution order and hybrid readiness — Yolkster64 <thepatman64@gmail.com>
- `28bba549655b610bbbea2d879431a59c231741f2` — Add HELIOS command center and control inventories — Yolkster64 <thepatman64@gmail.com>
- `ac8e42fa462621a250738bed553aaf305ab2ba93` — Add unified control plane command summary — Yolkster64 <thepatman64@gmail.com>
- `a5039856027636f845709fc7127c52d58295ffba` — Add web control plane and Azure secret sync — Yolkster64 <thepatman64@gmail.com>
- `3bab34107c40875cb17d1991787f60aff337c9c2` — Add cloud integration readiness foundation — Yolkster64 <thepatman64@gmail.com>
- `6fd24ed302e8025bd608ae33affcb1edd77678af` — Add HELIOS control plane automation foundation — Yolkster64 <thepatman64@gmail.com>
- `26aaead30a1e2a54745a3cf0e565416b7e9dad8a` — Bootstrap local tooling for branch intelligence — Yolkster64 <thepatman64@gmail.com>
- `1453e4588b8090a5745f86d2b2b1eac91ae17c78` — Expand branch intelligence scoring and native planning — Yolkster64 <thepatman64@gmail.com>
- `495f51a6f7471defb91c9a509f31c4ae48d89f58` — Add branch intelligence automation — Yolkster64 <thepatman64@gmail.com>
- `70dab33d52aa61adf2562c125ac89d7b1483511f` — Harden F# analytics contracts and workloads — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.devcontainer/codespaces-secrets.env.example`
- `.github/PULL_REQUEST_BODY.md`
- `.github/workflows/azure-infra.yml`
- `.github/workflows/branch-intelligence.yml`
- `.github/workflows/helios-control-plane.yml`
- `COMMAND_CENTER.md`
- `Directory.Build.props`
- `config/azure-control.example.json`
- `config/build-graph.json`
- `config/cross-access-profiles.example.json`
- `config/execution-order.json`
- `config/github-control.example.json`
- `config/hybrid-integration-roadmap.json`
- `config/integrations.example.json`
- `config/secrets-map.example.json`
- `docs/architecture/AZURE_HYBRID_ARCHITECTURE.md`
- `docs/architecture/DASHBOARD_PUBLISHING_STRATEGY.md`
- `docs/architecture/FSHARP_RANKING_BRIDGE.md`
- `docs/architecture/HYBRID_INTEGRATION_EXPANSION_PLAN.md`
- `docs/architecture/WHOLE_PROJECT_NEXT_STEPS.md`
- `docs/integration/BRANCH_INTELLIGENCE.md`
- `docs/integration/CLOUDSHELL_GITHUB_AZURE_SETUP.md`
- `docs/integration/CONTROL_PLANE_COMMANDS.md`
- `docs/integration/CROSS_ACCESS_PROFILES.md`
- `docs/integration/PR_UPDATE_WORKFLOW.md`
- `docs/integration/VISUAL_STUDIO_MAUI_SETUP.md`
- `docs/integration/WEB_CONTROL_PLANE.md`
- `docs/integration/remote-manifest.json`
- `docs/microsoft-365/COPILOT_INTEGRATION.md`
- `docs/security/CONTROL_PLANE_PERMISSIONS.md`
- `helios.sh`
- `infra/azure/README.md`
- `infra/azure/main.bicep`
- `infra/azure/modules/keyvault.bicep`
- `infra/azure/modules/network.bicep`
- `infra/azure/modules/observability.bicep`
- `infra/azure/modules/storage.bicep`
- `infra/azure/parameters/dev.json`
- `reports/branch-intelligence/agent-work-queue.json`
- `reports/branch-intelligence/agent-work-queue.md`
- `reports/branch-intelligence/ai-enrichment.json`
- `reports/branch-intelligence/analytics-metrics.json`
- `reports/branch-intelligence/branch-ranking.json`
- `reports/branch-intelligence/branch-ranking.md`
- `reports/branch-intelligence/connectivity.json`
- `reports/branch-intelligence/connectivity.md`
- `reports/branch-intelligence/dashboard.md`
- `reports/branch-intelligence/graphs.md`
- `reports/branch-intelligence/idea-impact-summary.json`
- `reports/branch-intelligence/idea-impact-summary.md`
- `reports/branch-intelligence/idea-impact.json`
- `reports/branch-intelligence/idea-impact.md`
- `reports/branch-intelligence/merge-prune-recommendations.json`
- `reports/branch-intelligence/merge-prune-recommendations.md`
- `reports/branch-intelligence/remote-actions.json`
- `reports/build-graph/build-graph.json`
- `reports/build-graph/build-graph.md`
- `reports/codex/task-index.md`
- `reports/codex/tasks/001-work-github-workflows.md`
- `reports/codex/tasks/002-work-scripts.md`
- `reports/codex/tasks/003-work-docs-integration.md`
- `reports/codex/tasks/004-work-config.md`
- `reports/codex/tasks/005-work-root.md`
- `reports/codex/tasks/006-work-reports.md`
- `reports/codex/tasks/007-work-status-site.md`
- `reports/codex/tasks/008-knowledge-base-github.md`
- `reports/codex/tasks/009-knowledge-base-github.md`
- `reports/codex/tasks/010-knowledge-base-github.md`
- `reports/codex/tasks/011-knowledge-base-github.md`
- `reports/codex/tasks/012-knowledge-base-github.md`
- `reports/codex/tasks/013-knowledge-base-github.md`
- `reports/codex/tasks/014-knowledge-base-github.md`
- `reports/codex/tasks/015-knowledge-base-github.md`
- `reports/codex/tasks/016-knowledge-base-nuget.md`
- `reports/codex/tasks/017-knowledge-base-nuget.md`
- `reports/codex/tasks/018-knowledge-base-nuget.md`
- `reports/codex/tasks/019-knowledge-base-accessibility-compliance-report-md.md`
- `reports/codex/tasks/020-knowledge-base-accessibility-compliance-report-md.md`
- `reports/control-plane/azure-inventory.json`
- `reports/control-plane/azure-inventory.md`
- `reports/control-plane/control-summary.json`
- `reports/control-plane/control-summary.md`
- `reports/control-plane/doctor.md`
- `reports/control-plane/github-inventory.json`
- `reports/control-plane/github-inventory.md`
- `reports/integrations/connection-readiness.json`
- `reports/integrations/cross-access-profiles.json`
- `reports/integrations/cross-access-profiles.md`
- `reports/integrations/readiness-score.json`
- `reports/integrations/readiness-score.md`
- `reports/project-inventory/hybrid-gap-analysis.json`
- `reports/project-inventory/hybrid-gap-analysis.md`
- `reports/project-inventory/repo-inventory.json`
- `reports/project-inventory/repo-inventory.md`
- `scripts/ai/enrich-ideas.py`
- `scripts/analysis/branch_intelligence.py`
- `scripts/analysis/hybrid_gap_analysis.py`
- `scripts/analysis/merge_prune_recommendations.py`
- `scripts/analysis/repo_inventory.py`
- `scripts/azure/azure-inventory.py`
- `scripts/azure/sync-keyvault-secrets.sh`
- `scripts/build_graph/build_graph.py`
- `scripts/cloudshell/helios-cloudshell.sh`
- `scripts/codex/generate-codex-tasks.py`
- `scripts/control/doctor.py`
- `scripts/control/helios-control.py`
- `scripts/dashboard/generate-actions.py`
- `scripts/dashboard/generate-gui.py`
- `scripts/github/github-inventory.py`
- `scripts/github/update-pr-from-reports.py`
- `scripts/github/update-wiki-from-reports.py`
- `scripts/graphs/generate_graphs.py`
- `scripts/integrations/check-connections.py`
- `scripts/integrations/cross_access_profiles.py`
- `scripts/integrations/readiness_score.py`
- `scripts/sandbox/clean-sandbox-workspaces.sh`
- `scripts/sandbox/run-sandbox-workspace.sh`
- `scripts/setup/bootstrap-local-tools.sh`
- `scripts/setup/helios-dev.sh`
- `scripts/web/helios-web.py`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/analytics/HELIOS.Analytics.FSharp/Models/AnalyticsModels.fs`
- `src/analytics/HELIOS.Analytics.FSharp/ParallelWorkloads.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Prediction/PredictionWorkloads.fs`
- `src/analytics/HELIOS.Analytics.FSharp/PublicApi.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Statistics/AnalyticsWorkloads.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Statistics/MathWorkloads.fs`
- `src/core/HELIOS.Platform.Contracts/AnalyticsContracts.cs`
- `src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj`
- `src/native/HELIOS.Native.Performance/CMakeLists.txt`
- `src/native/HELIOS.Native.Performance/README.md`
- `status-site/actions.md`
- `status-site/index.html`
- `status-site/index.md`
- `status-site/wiki-export/Agent-Work-Queue.md`
- `status-site/wiki-export/Branch-Graphs.md`
- `status-site/wiki-export/Branch-Intelligence.md`
- `status-site/wiki-export/Idea-Impact.md`
- `status-site/wiki-export/Module-Ranking.md`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/AnalyticsEngineTests.fs`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj`

## origin/codex/install-azure-cli-and-update-setup-docs

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `b0fc9887b921df5a16b02f48e100acaee897ff06` — Document new helios shortcuts — Yolkster64 <thepatman64@gmail.com>
- `f1ffe9ef7a37c9438be3497a6bc467a1f61ed264` — Stabilize build graph and full-stack readiness — Yolkster64 <thepatman64@gmail.com>
- `6fb17675dc6d50564ebfcdef824d5e6b48a8d61f` — Harden remote quality gates and native interop — Yolkster64 <thepatman64@gmail.com>
- `9d9e4c95e8b5297d336bdb5b3df967365504d6a6` — Add remote inventory and cross-stack quality gates — Yolkster64 <thepatman64@gmail.com>
- `e114b5a08108d4eae1df83bbef4fda198f741ca8` — Allow offline Bicep validation fallback — Yolkster64 <thepatman64@gmail.com>
- `5f023d3a17efc054da931870f5d5f66c5141a742` — Make Azure Bicep build a required CI check — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/azure-infra.yml`
- `.github/workflows/verify.yml`
- `config/build-graph.json`
- `docs/DEVELOPMENT_SETUP.md`
- `docs/SYSTEM_REQUIREMENTS.md`
- `docs/integration/BRANCH_INTELLIGENCE.md`
- `helios.sh`
- `infra/azure/README.md`
- `infra/azure/main.bicep`
- `infra/azure/modules/keyvault.bicep`
- `infra/azure/modules/network.bicep`
- `infra/azure/modules/observability.bicep`
- `infra/azure/modules/storage.bicep`
- `samples/native-interop/HELIOS.NativeInterop.Sample.csproj`
- `samples/native-interop/Program.cs`
- `scripts/analysis/branch_intelligence.py`
- `scripts/azure/bicep_report.py`
- `scripts/build_graph/build_graph.py`
- `scripts/common/tool_resolver.py`
- `scripts/control/helios-control.py`
- `scripts/control/python_static.py`
- `scripts/integrations/full_stack_readiness.py`
- `scripts/native/native_smoke.py`
- `scripts/native/run_csharp_native_smoke.py`
- `scripts/setup/bootstrap-local-tools.sh`
- `src/native/HELIOS.Native.Performance/CMakeLists.txt`
- `src/native/HELIOS.Native.Performance/README.md`
- `src/native/HELIOS.Native.Performance/benchmarks/native_benchmark.cpp`
- `src/native/HELIOS.Native.Performance/include/helios_native_performance.h`
- `src/native/HELIOS.Native.Performance/src/helios_native_performance.cpp`
- `src/tools/HELIOS.RepositoryAnalytics/HELIOS.RepositoryAnalytics.csproj`
- `src/tools/HELIOS.RepositoryAnalytics/Program.cs`

## origin/codex/absorb-xtier-winre-aihub-v6

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `a5b83d74ee341781b1b8b66e11f0022dc21d6598` — Absorb XTier WinRE and AIHub legacy bridge v6 — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `config/aihub/legacy-bridge.v6.json`
- `docs/imports/HELIOS_XTIER_UPLOAD_MERGE_V6.md`
- `legacy/diagnostics/00_Master_20260604_012614.log`
- `legacy/diagnostics/01_LocalRepair_20260604_013051.log`
- `python/__init__.py`
- `python/aihub/__init__.py`
- `python/aihub/legacy_bridge/__init__.py`
- `python/aihub/legacy_bridge/build_super_outputs.py`
- `python/aihub/legacy_bridge/ml_registry.py`
- `python/aihub/legacy_bridge/security_optimizer.py`
- `python/aihub/legacy_bridge/vm_orchestrator.py`
- `python/aihub/legacy_bridge/winre_conversation_integrator.py`
- `scripts/windows/Invoke-HeliosLegacyAudit.ps1`
- `scripts/windows/Repair-HeliosEnvironment.ps1`
- `tests/scripts/test_legacy_aihub_bridge.py`

## origin/codex/add-documented-ltrain-entrypoint

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `7cafcf49a78c5f85c4ebf81df79b20ea840e70cd` — Add ltrain local training entrypoint — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `README.md`
- `ai-integration/README.md`
- `scripts/ltrain.py`
- `tests/python/test_ltrain.py`

## origin/codex/add-gui-for-managing-ai-hub-resources

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `497a8aa8b658683464a659202e952c507296bfb6` — Add AIHub optimization rankings and learning references — Yolkster64 <thepatman64@gmail.com>
- `96c316568b8fdb57bac853757d0d3f8d16f612a5` — Expand AIHub taxonomy and learning specialties — Yolkster64 <thepatman64@gmail.com>
- `74c81e8df8d226688f7ff2858880f9d4e7de58d7` — Add AIHub fleet command panel foundation — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `docs/WinUI3-Design/Presentation/Pages/AIHubPage.xaml`
- `src/core/HELIOS.Platform/Core/AIHub/AIHubFleetModels.cs`
- `src/core/HELIOS.Platform/Core/AIHub/AIHubFleetService.cs`
- `src/core/HELIOS.Platform/Core/AIHub/AIHubOptimizationModels.cs`
- `tests/HELIOS.Platform.Tests/AIHub/AIHubFleetServiceTests.cs`

## origin/codex/configure-github-issue/pr-integration

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `71470a6f7ba50f70a1c59bfcd6d8effac144e612` — Fix bootstrap consumers for XDG tool installs — Yolkster64 <thepatman64@gmail.com>
- `57f67800d244bd7de720dc78bcb80a50e17053b3` — Harden local CLI bootstrap authentication — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `scripts/apply/finish_readiness_apply.py`
- `scripts/setup/agent-runner-easy-setup.sh`
- `scripts/setup/bootstrap-local-tools.sh`
- `scripts/setup/finish-easy-setup.sh`
- `scripts/setup/helios-dev.sh`
- `tests/scripts/bootstrap-local-tools.tests.sh`

## origin/codex/fix-and-optimize-tests-and-builds

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `19f0fad4adca74da8137d56f479333f96a211dd6` — Harden consolidation verification gates — Yolkster64 <thepatman64@gmail.com>
- `bf30607a18d9ad14b401c38f9648f22fd1462935` — Fix root package build and dependency upgrades — Yolkster64 <thepatman64@gmail.com>
- `0f77509f8c0c6f9030787e1a928a584cd1899076` — Avoid dirtying apply runs with generated plan — Yolkster64 <thepatman64@gmail.com>
- `be34f7682ffb9b9989dc0cc48c2103072eef6bc7` — Add HELIOS consolidation automation plan — Yolkster64 <thepatman64@gmail.com>
- `ea6df3ec01d90905e12d87ed991acbc785a449de` — Add consolidation source manifest — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.gitmodules`
- `HELIOS.Platform.csproj`
- `MERGE_SOURCE_MANIFEST.yaml`
- `docs/integration/HELIOS_CONSOLIDATION_AUTOMATION.md`
- `docs/integration/HELIOS_CONSOLIDATION_EXECUTION_PLAN.md`
- `scripts/automation/consolidation-sources.json`
- `scripts/automation/helios_consolidation.py`
- `tests/automation/test_helios_consolidation.py`

## origin/codex/fix-high-priority-issues-from-codex-review-nybf6u

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `c78b31c53adc72040d4f349dc9a9c7cb79b018f6` — Add HELIOS integrator tests and service principal setup — Yolkster64 <thepatman64@gmail.com>
- `28326b6dfc589dd082d0a6eca9cbb062279e939d` — Add offline phase consolidation scan — Yolkster64 <thepatman64@gmail.com>
- `d7ea00e9056a9eac17e844675c91c4c717da08b0` — Fix Azure App Service deployment package — Yolkster64 <thepatman64@gmail.com>
- `798deba7a0c6e8380f20bea35b1ed7779974f548` — Add Azure deployment infrastructure and package artifact — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `docs/integration/BRANCH_AND_STACK_INTEGRATION_PLAN.md`
- `docs/integration/HELIOS_HERMES_INVENTORY.md`
- `docs/optimization/AI_PERFORMANCE_SECURITY_REVIEW.md`
- `docs/phases/CONSOLIDATED_PHASE_MARKDOWN.md`
- `infrastructure/main.bicep`
- `microsoft-ecosystem/.github/workflows/azure-deploy.yml`
- `microsoft-ecosystem/README.md`
- `scripts/automation/consolidate_phase_docs_and_scan.py`
- `scripts/integration/helios_branch_integrator.py`
- `scripts/setup/setup-azure-cli.sh`
- `src/core/HELIOS.Platform.Minimal/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform.Minimal/Program.cs`
- `tests/scripts/test_helios_branch_integrator.py`

## origin/codex/implement-or-mock-placeholder-methods-in-channel3boottimeaut

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `978434401387bc16cdb0c8a97bb5a83cb7e9c05c` — Refine boot automation real operations — Yolkster64 <thepatman64@gmail.com>
- `60b3378f62ee344244b0542e3478caf814e7fd9a` — Implement boot automation operations — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/Channel3BootTimeAutomationOrchestrator.cs`
- `src/core/HELIOS.Platform/Properties/AssemblyInfo.cs`
- `tests/HELIOS.Platform.Tests/Phase10/BootEnvironment/Channel3BootTimeAutomationOrchestratorTests.cs`

## origin/codex/integrate-aihub-fleet-service-with-ai-agent

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `b7e911e4bd9277066965cd649b9dbd5a7ccccd31` — Connect AIHub fleet to AI abstractions — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `src/core/HELIOS.Platform/Core/AI/Router/IRouter.cs`
- `src/core/HELIOS.Platform/Core/AIHub/AIHubFleetService.cs`
- `src/core/HELIOS.Platform/Core/AIHub/InMemoryAIHubRouter.cs`
- `tests/HELIOS.Platform.Tests/AIHub/AIHubFleetServiceTests.cs`

## origin/codex/propose-repo-layout-overhaul

- **Primary umbrella:** C++ native performance and security (`cpp-native-performance-security`)
- **Temporary integration branch:** `integration/train-cpp-native-performance-security`
- **Module owner:** HELIOS.Native, HELIOS.Security
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `9871288ebc702b0b2ea5353c9a4763e6a1c2849c` — Promote Hermes XCore RL subsystem — Yolkster64 <thepatman64@gmail.com>
- `69c380e914a76dd705768a8fd9a66e064db434dc` — Add upstream upgrade plan and RL policy scaffold — Yolkster64 <thepatman64@gmail.com>
- `f7be050e8a127df567fd10fc78f8d0ebe5337d4a` — Import Hermes fleet X-tier assets — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/azure-what-if.yml`
- `.github/workflows/ci-docs.yml`
- `.github/workflows/ci-dotnet.yml`
- `.github/workflows/ci-native.yml`
- `.github/workflows/ci-python.yml`
- `.github/workflows/release.yml`
- `.github/workflows/security-audit.yml`
- `.github/workflows/upstream-inventory.yml`
- `.github/workflows/wiki-sync.yml`
- `Directory.Build.props`
- `Directory.Packages.props`
- `HELIOS.Platform.slnx`
- `README.md`
- `docs/START_HERE.md`
- `docs/architecture/BUILD_VARIANTS.md`
- `docs/architecture/COMPONENT_MATRIX.md`
- `docs/architecture/MODULAR_ARCHITECTURE.md`
- `docs/architecture/integration-map.md`
- `docs/integration/hermes-xcore/AIHUB_UPGRADE_INTEGRATION.md`
- `docs/integration/hermes-xcore/POLYGLOT_PHASE_INTEGRATION.md`
- `docs/integration/hermes-xcore/RL_UPGRADE_PLAN.md`
- `docs/integration/hermes-xcore/SUPER_DEEP_AIHUB_SYSTEM_GUIDE.md`
- `docs/integration/hermes-xcore/WINRE_AIHUB_TRANSCRIPT_DEEP_INTEGRATION_GUIDE.md`
- `docs/integration/hermes-xcore/imported-python-reference/autoencoder_pipeline.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/correlation_search.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/feature_pipeline.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/orchestrator.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/retraining_loop.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/routing_policy.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/scoring_feedback.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/vector_store.py.txt`
- `docs/operations/azure-setup.md`
- `docs/operations/cli.md`
- `docs/operations/deployment.md`
- `docs/operations/troubleshooting.md`
- `docs/planning/backlog.yaml`
- `docs/planning/branch-inventory.md`
- `docs/planning/helios-control-inventory.md`
- `docs/planning/helios-overhaul-plan.yaml`
- `docs/planning/hermes-fleet-production-inventory.md`
- `docs/planning/merge-order.md`
- `docs/planning/milestones.yaml`
- `docs/planning/source-repositories.yaml`
- `docs/planning/upstream-upgrade-plan.md`
- `docs/security/hardening.md`
- `docs/security/secrets.md`
- `docs/security/threat-model.md`
- `docs/status/current-status.md`
- `docs/status/project-status.yaml`
- `docs/status/verification-log.md`
- `docs/testing/coverage.md`
- `docs/testing/strategy.md`
- `docs/testing/test-matrix.yaml`
- `docs/wiki/generated/Component-Matrix.md`
- `docs/wiki/generated/Current-Status.md`
- `docs/wiki/generated/Modular-Architecture.md`
- `docs/wiki/wiki.config.yaml`
- `infra/hermes-fleet/README.md`
- `infra/hermes-fleet/main.bicep`
- `infra/hermes-fleet/main.parameters.json`
- `infra/hermes-fleet/modules/compute.bicep`
- `infra/hermes-fleet/modules/data-stack.bicep`
- `infra/hermes-fleet/modules/network.bicep`
- `infra/hermes-fleet/modules/web-stack.bicep`
- `infra/hermes-fleet/network-connectivity.md`
- `infra/main.bicep`
- `infra/modules/appinsights.bicep`
- `infra/modules/azure-openai.bicep`
- `infra/modules/identities.bicep`
- `infra/modules/keyvault.bicep`
- `infra/modules/monitoring.bicep`
- `infra/modules/storage.bicep`
- `infra/parameters/dev.bicepparam`
- `infra/parameters/prod.bicepparam`
- `infra/parameters/staging.bicepparam`
- `infra/scripts/deploy.ps1`
- `infra/scripts/teardown-dev.ps1`
- `infra/scripts/what-if.ps1`
- `scripts/azure/login.ps1`
- `scripts/azure/provision.ps1`
- `scripts/azure/validate-subscription.ps1`
- `scripts/docs/generate-wiki.ps1`
- `scripts/docs/update-status.ps1`
- `scripts/docs/validate-links.ps1`
- `scripts/hermes-xcore/01_validate_disk_layout.ps1`
- `scripts/hermes-xcore/02_plan_vhdx_moves.ps1`
- `scripts/hermes-xcore/Build-SuperSystem.ps1`
- `scripts/hermes-xcore/Invoke-AIHubUpgrade.ps1`
- `scripts/hermes-xcore/README.md`
- `scripts/hermes-xcore/Start-AIHubControl.ps1`
- `scripts/hermes-xcore/build_all_polyglot.ps1`
- `scripts/hermes-xcore/import_xtier_bundle.py`
- `scripts/hermes-xcore/imported/03_phase3_hyperv_services.ps1`
- `scripts/hermes-xcore/imported/configure_bitlocker.ps1`
- `scripts/hermes-xcore/imported/set_security_mode.ps1`
- `scripts/hermes-xcore/xtier_bootstrap.py`
- `scripts/merge/analyze-branches.ps1`
- `scripts/merge/conflict-report.ps1`
- `scripts/merge/create-integration-branch.ps1`
- `scripts/merge/fetch-all-remotes.ps1`
- `scripts/merge/update-upstreams.ps1`
- `scripts/security/audit-dependencies.ps1`
- `scripts/security/scan-secrets.ps1`
- `scripts/setup/install-prereqs.ps1`
- `scripts/setup/setup-azure-cli.ps1`
- `scripts/setup/setup-dotnet.ps1`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/analytics/HELIOS.Analytics.FSharp/Models/AnalyticsModel.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Prediction/Prediction.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Statistics/Statistics.fs`
- `src/core/HELIOS.Platform.Contracts/ReinforcementLearning/RlContracts.cs`
- `src/frontend/HELIOS.Control.WinUI/App.xaml`
- `src/frontend/HELIOS.Control.WinUI/HELIOS.Control.WinUI.csproj`
- `src/frontend/HELIOS.Control.WinUI/MainWindow.xaml`
- `src/frontend/HELIOS.Control.WinUI/Pages/.gitkeep`
- `src/frontend/HELIOS.Control.WinUI/Services/.gitkeep`
- `src/frontend/HELIOS.Control.WinUI/ViewModels/.gitkeep`
- `src/installer/HELIOS.Installer/.gitkeep`
- `src/integrations/Azure/.gitkeep`
- `src/integrations/GitHub/.gitkeep`
- `src/integrations/Hermes/.gitkeep`
- `src/integrations/Hermes/PolyglotXTier/IntegrationHost.cs`
- `src/integrations/Hermes/PolyglotXTier/PhaseRuntime.cs`
- `src/integrations/Hermes/PolyglotXTier/PolyglotXTier.csproj`
- `src/integrations/Hermes/PolyglotXTier/Program.cs`
- `src/integrations/Hermes/PolyglotXTier/SecurityFrontendMap.cs`
- `src/integrations/OpenAI/.gitkeep`
- `src/integrations/XCore/.gitkeep`
- `src/native/HELIOS.Performance/CMakeLists.txt`
- `src/native/HELIOS.Performance/include/helios_performance.h`
- `src/native/HELIOS.Performance/src/helios_performance.cpp`
- `src/native/HELIOS.Performance/tests/helios_performance_tests.cpp`
- `src/native/HELIOS.Performance/x-tier/CMakeLists.txt`
- `src/native/HELIOS.Performance/x-tier/main.cpp`
- `src/native/HELIOS.Performance/x-tier/phase_runtime.cpp`
- `src/native/HELIOS.Performance/x-tier/phase_runtime.hpp`
- `src/native/HELIOS.Performance/x-tier/secure_runtime_core.cpp`
- `src/native/HELIOS.Performance/x-tier/secure_runtime_core.hpp`
- `src/python/helios_aihub/pyproject.toml`
- `src/python/helios_aihub/src/helios_aihub/__init__.py`
- `src/python/hermes_xcore/pyproject.toml`
- `src/python/hermes_xcore/src/hermes_xcore/__init__.py`
- `src/python/hermes_xcore/src/hermes_xcore/feature_pipeline.py`
- `src/python/hermes_xcore/src/hermes_xcore/imported/__init__.py`
- `src/python/hermes_xcore/src/hermes_xcore/orchestrator.py`
- `src/python/hermes_xcore/src/hermes_xcore/reinforcement_learning.py`
- `src/python/hermes_xcore/src/hermes_xcore/routing_policy.py`
- `src/python/hermes_xcore/src/hermes_xcore/vector_store.py`
- `src/security/HELIOS.Security/.gitkeep`
- `tests/analytics/.gitkeep`
- `tests/e2e/.gitkeep`
- `tests/integration/.gitkeep`
- `tests/integration/README.md`
- `tests/native/.gitkeep`
- `tests/performance/.gitkeep`
- `tests/python/.gitkeep`
- `tests/python/test_reinforcement_learning.py`
- `tests/security/.gitkeep`
- `tests/unit/.gitkeep`

## origin/codex/propose-repo-layout-overhaul-hws95t

- **Primary umbrella:** C++ native performance and security (`cpp-native-performance-security`)
- **Temporary integration branch:** `integration/train-cpp-native-performance-security`
- **Module owner:** HELIOS.Native, HELIOS.Security
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `417294ffc7579f1d73fb83bab7a74627149f7efe` — Add ranked integration board — Yolkster64 <thepatman64@gmail.com>
- `d539dd5d64448627295addcce9e0d30addf8a17b` — Stabilize legacy quarantine build — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/azure-what-if.yml`
- `.github/workflows/ci-docs.yml`
- `.github/workflows/ci-dotnet.yml`
- `.github/workflows/ci-native.yml`
- `.github/workflows/ci-python.yml`
- `.github/workflows/release.yml`
- `.github/workflows/security-audit.yml`
- `.github/workflows/upstream-inventory.yml`
- `.github/workflows/wiki-sync.yml`
- `Directory.Build.props`
- `Directory.Packages.props`
- `HELIOS.Legacy.Quarantine.slnx`
- `HELIOS.Platform.slnx`
- `README.md`
- `docs/START_HERE.md`
- `docs/architecture/BUILD_VARIANTS.md`
- `docs/architecture/COMPONENT_MATRIX.md`
- `docs/architecture/MODULAR_ARCHITECTURE.md`
- `docs/architecture/integration-map.md`
- `docs/integration/hermes-xcore/AIHUB_UPGRADE_INTEGRATION.md`
- `docs/integration/hermes-xcore/POLYGLOT_PHASE_INTEGRATION.md`
- `docs/integration/hermes-xcore/RL_UPGRADE_PLAN.md`
- `docs/integration/hermes-xcore/SUPER_DEEP_AIHUB_SYSTEM_GUIDE.md`
- `docs/integration/hermes-xcore/WINRE_AIHUB_TRANSCRIPT_DEEP_INTEGRATION_GUIDE.md`
- `docs/integration/hermes-xcore/imported-python-reference/autoencoder_pipeline.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/correlation_search.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/feature_pipeline.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/orchestrator.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/retraining_loop.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/routing_policy.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/scoring_feedback.py.txt`
- `docs/integration/hermes-xcore/imported-python-reference/vector_store.py.txt`
- `docs/operations/azure-setup.md`
- `docs/operations/cli.md`
- `docs/operations/deployment.md`
- `docs/operations/troubleshooting.md`
- `docs/planning/agent-integration-playbook.md`
- `docs/planning/backlog.yaml`
- `docs/planning/branch-inventory.md`
- `docs/planning/helios-control-inventory.md`
- `docs/planning/helios-overhaul-plan.yaml`
- `docs/planning/hermes-fleet-production-inventory.md`
- `docs/planning/integration-ranked-issues.yaml`
- `docs/planning/merge-order.md`
- `docs/planning/milestones.yaml`
- `docs/planning/source-repositories.yaml`
- `docs/planning/upstream-upgrade-plan.md`
- `docs/security/hardening.md`
- `docs/security/secrets.md`
- `docs/security/threat-model.md`
- `docs/status/current-status.md`
- `docs/status/project-status.yaml`
- `docs/status/verification-log.md`
- `docs/testing/coverage.md`
- `docs/testing/strategy.md`
- `docs/testing/test-matrix.yaml`
- `docs/wiki/generated/Component-Matrix.md`
- `docs/wiki/generated/Current-Status.md`
- `docs/wiki/generated/Modular-Architecture.md`
- `docs/wiki/wiki.config.yaml`
- `infra/hermes-fleet/README.md`
- `infra/hermes-fleet/main.bicep`
- `infra/hermes-fleet/main.parameters.json`
- `infra/hermes-fleet/modules/compute.bicep`
- `infra/hermes-fleet/modules/data-stack.bicep`
- `infra/hermes-fleet/modules/network.bicep`
- `infra/hermes-fleet/modules/web-stack.bicep`
- `infra/hermes-fleet/network-connectivity.md`
- `infra/main.bicep`
- `infra/modules/appinsights.bicep`
- `infra/modules/azure-openai.bicep`
- `infra/modules/identities.bicep`
- `infra/modules/keyvault.bicep`
- `infra/modules/monitoring.bicep`
- `infra/modules/storage.bicep`
- `infra/parameters/dev.bicepparam`
- `infra/parameters/prod.bicepparam`
- `infra/parameters/staging.bicepparam`
- `infra/scripts/deploy.ps1`
- `infra/scripts/teardown-dev.ps1`
- `infra/scripts/what-if.ps1`
- `scripts/azure/login.ps1`
- `scripts/azure/provision.ps1`
- `scripts/azure/validate-subscription.ps1`
- `scripts/docs/generate-wiki.ps1`
- `scripts/docs/update-status.ps1`
- `scripts/docs/validate-links.ps1`
- `scripts/hermes-xcore/01_validate_disk_layout.ps1`
- `scripts/hermes-xcore/02_plan_vhdx_moves.ps1`
- `scripts/hermes-xcore/Build-SuperSystem.ps1`
- `scripts/hermes-xcore/Invoke-AIHubUpgrade.ps1`
- `scripts/hermes-xcore/README.md`
- `scripts/hermes-xcore/Start-AIHubControl.ps1`
- `scripts/hermes-xcore/build_all_polyglot.ps1`
- `scripts/hermes-xcore/import_xtier_bundle.py`
- `scripts/hermes-xcore/imported/03_phase3_hyperv_services.ps1`
- `scripts/hermes-xcore/imported/configure_bitlocker.ps1`
- `scripts/hermes-xcore/imported/set_security_mode.ps1`
- `scripts/hermes-xcore/xtier_bootstrap.py`
- `scripts/merge/analyze-branches.ps1`
- `scripts/merge/conflict-report.ps1`
- `scripts/merge/create-integration-branch.ps1`
- `scripts/merge/fetch-all-remotes.ps1`
- `scripts/merge/update-upstreams.ps1`
- `scripts/security/audit-dependencies.ps1`
- `scripts/security/scan-secrets.ps1`
- `scripts/setup/install-prereqs.ps1`
- `scripts/setup/setup-azure-cli.ps1`
- `scripts/setup/setup-dotnet.ps1`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/analytics/HELIOS.Analytics.FSharp/Models/AnalyticsModel.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Prediction/Prediction.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Statistics/Statistics.fs`
- `src/core/HELIOS.Platform.Contracts/ReinforcementLearning/RlContracts.cs`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/frontend/HELIOS.Control.WinUI/App.xaml`
- `src/frontend/HELIOS.Control.WinUI/HELIOS.Control.WinUI.csproj`
- `src/frontend/HELIOS.Control.WinUI/MainWindow.xaml`
- `src/frontend/HELIOS.Control.WinUI/Pages/.gitkeep`
- `src/frontend/HELIOS.Control.WinUI/Services/.gitkeep`
- `src/frontend/HELIOS.Control.WinUI/ViewModels/.gitkeep`
- `src/installer/HELIOS.Installer/.gitkeep`
- `src/integrations/Azure/.gitkeep`
- `src/integrations/GitHub/.gitkeep`
- `src/integrations/Hermes/.gitkeep`
- `src/integrations/Hermes/PolyglotXTier/IntegrationHost.cs`
- `src/integrations/Hermes/PolyglotXTier/PhaseRuntime.cs`
- `src/integrations/Hermes/PolyglotXTier/PolyglotXTier.csproj`
- `src/integrations/Hermes/PolyglotXTier/Program.cs`
- `src/integrations/Hermes/PolyglotXTier/SecurityFrontendMap.cs`
- `src/integrations/OpenAI/.gitkeep`
- `src/integrations/XCore/.gitkeep`
- `src/native/HELIOS.Performance/CMakeLists.txt`
- `src/native/HELIOS.Performance/include/helios_performance.h`
- `src/native/HELIOS.Performance/src/helios_performance.cpp`
- `src/native/HELIOS.Performance/tests/helios_performance_tests.cpp`
- `src/native/HELIOS.Performance/x-tier/CMakeLists.txt`
- `src/native/HELIOS.Performance/x-tier/main.cpp`
- `src/native/HELIOS.Performance/x-tier/phase_runtime.cpp`
- `src/native/HELIOS.Performance/x-tier/phase_runtime.hpp`
- `src/native/HELIOS.Performance/x-tier/secure_runtime_core.cpp`
- `src/native/HELIOS.Performance/x-tier/secure_runtime_core.hpp`
- `src/python/helios_aihub/pyproject.toml`
- `src/python/helios_aihub/src/helios_aihub/__init__.py`
- `src/python/hermes_xcore/pyproject.toml`
- `src/python/hermes_xcore/src/hermes_xcore/__init__.py`
- `src/python/hermes_xcore/src/hermes_xcore/feature_pipeline.py`
- `src/python/hermes_xcore/src/hermes_xcore/imported/__init__.py`
- `src/python/hermes_xcore/src/hermes_xcore/orchestrator.py`
- `src/python/hermes_xcore/src/hermes_xcore/reinforcement_learning.py`
- `src/python/hermes_xcore/src/hermes_xcore/routing_policy.py`
- `src/python/hermes_xcore/src/hermes_xcore/vector_store.py`
- `src/security/HELIOS.Security/.gitkeep`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `src/tests/HELIOS.Platform.Tests/Phase2ServiceTests.cs`
- `tests/analytics/.gitkeep`
- `tests/e2e/.gitkeep`
- `tests/integration/.gitkeep`
- `tests/integration/README.md`
- `tests/native/.gitkeep`
- `tests/performance/.gitkeep`
- `tests/python/.gitkeep`
- `tests/python/test_reinforcement_learning.py`
- `tests/security/.gitkeep`
- `tests/unit/.gitkeep`

## origin/yolkster64-crispy-dollop

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `3705c405b7f9c6f146cabe74ba984ef96691f5da` — Fix cloud approval guardrails and unblock .NET test lanes — Copilot App <223556219+Copilot@users.noreply.github.com>
- `e2eed1485f775db7509e7e89ecfc92b4f69fdb95` — Add guided Helios cloud approval flow — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `monado/helios-control/.vscode/tasks.json`
- `monado/helios-control/README.md`
- `monado/helios-control/docs/AZURE_INTERACTIVE_ONBOARDING.md`
- `monado/helios-control/scripts/Invoke-HeliosCloudApprovalFlow.ps1`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoEngineUpdateService.cs`
- `src/core/HELIOS.Platform/Phase10/Users/AccountPermissionManager.cs`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/UserAccountManagementTests.cs`
- `src/core/HELIOS.Platform/Phase10/Users/UserDataDirectorySetup.cs`
- `src/core/HELIOS.Platform/packages.lock.json`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `src/tests/Phase8Stream8OptimizationTests.cs`
- `tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/HELIOS.Platform.Tests.Phase10.Quarantine.csproj`
- `tests/HELIOS.Platform.Tests/Phase10/Quarantine/QuarantineSystemTests.cs`

## origin/yolkster64-curly-journey

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `1b004c61860cf32de33c175f8634bd51f626b9a0` — test(ci): guard profile-folder tests on restricted runners — Copilot App <223556219+Copilot@users.noreply.github.com>
- `fdc74008f736c8427a7b5395679be23dbbf3eb13` — test(ci): stabilize user permission group assertion — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c2a77a2cebbc6e145be303c95dbcfc0721bc2efa` — fix(ci): resolve platform compile and restore breakages — Copilot App <223556219+Copilot@users.noreply.github.com>
- `0a46de94e91e3a58a616db752156919a97df24d6` — chore(ci): refresh HELIOS.Platform lock file — Copilot App <223556219+Copilot@users.noreply.github.com>
- `5a223daa6c880d5a1833e1011f1717578577c944` — fix(helios-control-fabric): preserve local env entries on scaffold write — Copilot App <223556219+Copilot@users.noreply.github.com>
- `202f5a37cf5ca3a4a984eaea3ff4195dcb9347d9` — feat(helios-control-fabric): add local setup scaffold flow — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `plugins/helios-control-fabric/.env.example`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/test_helios.py`
- `plugins/helios-control-fabric/skills/helios-control/SKILL.md`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoEngineUpdateService.cs`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/UserAccountManagementTests.cs`
- `src/core/HELIOS.Platform/packages.lock.json`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/codex/add-auto-setup-configuration

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `46aaee46bbb8e69ff55615c160c89b37fc518d0e` — Potential fix for pull request finding 'Call to 'System.IO.Path.Combine' may silently drop its earlier arguments' — Yolkster64 <thepatman64@gmail.com>
- `46c2c7cb0a24c79053d49b1ec8bb38e82adbb7b7` — Harden autosetup orchestration — Yolkster64 <thepatman64@gmail.com>
- `8ab21f628473ab9e7ad603fe92991b16d62e519d` — Add Helios Hermes autosetup orchestration — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/autosetup.yml`
- `HeliosAutoSetup.cs`
- `autosetup/manifest.yaml`
- `helios-autosetup.ps1`
- `scripts/autosetup.sh`
- `scripts/azure-preflight.sh`
- `scripts/validate-consolidation.sh`

## origin/codex/clarify-target-frameworks-and-modules

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `9d7f0c99113e13f6167c3a7e38d322b118f19004` — Fix Windows platform project dependencies — Yolkster64 <thepatman64@gmail.com>
- `4e9d866c68089a3e4390f32d281360ac68103379` — Clarify HELIOS platform target frameworks — Yolkster64 <thepatman64@gmail.com>
- `79f4f86ba0bc610dfa6cf7aafc7932b1b2b3d22a` — Fix deployment review findings — Yolkster64 <thepatman64@gmail.com>
- `3c17af39caee8bfa3a0de32158e1ee1c1c01ecf7` — [Generated by SRE Agent] Add deployment automation docs and deployment assets — Azure SRE Agent <noreply@microsoft.com>

### Patch-equivalent commits
- None

### Files
- `.env.template`
- `HELIOS.Platform.Installer/HELIOS.Platform.Installer.csproj`
- `HELIOS.Platform.ShellExtension/HELIOS.Platform.ShellExtension.csproj`
- `HELIOS.Platform.Tray/HELIOS.Platform.Tray.csproj`
- `azure-pipelines.yml`
- `deployment/containerapps/hubspot-sync-job.yaml`
- `deployment/logicapps/azure-monitor-to-slack.definition.json`
- `deployment/main.bicep`
- `deployment/manifests/hubspot-sync-cronjob.yaml`
- `deployment/modules/aks.bicep`
- `deployment/modules/container-apps.bicep`
- `deployment/modules/integration-stack.bicep`
- `deployment/parameters/platform.parameters.example.json`
- `docs/DEPLOYMENT.md`
- `docs/workflows/WORKFLOW_DEPLOY.md`
- `scripts/deploy/deploy-platform.sh`
- `src/core/HELIOS.Platform.Windows/HELIOS.Platform.Windows.csproj`
- `src/core/HELIOS.Platform.Windows/README.md`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`

## origin/codex/combine-phased-mds-and-optimize-code

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `c35dff8b644e0433e6c26d60854b158b13fa41f1` — Consolidate phase docs and optimize AI routing — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `README.md`
- `docs/automation/deep-github-ai-automation.md`
- `docs/consolidation/AI_OPTIMIZATION_SECURITY_REVIEW.md`
- `docs/consolidation/PHASED_MARKDOWN_CONSOLIDATED.md`
- `scripts/automation/deep_automation_orchestrator.py`
- `src/core/HELIOS.Platform/Caching/IntelligentCache.cs`
- `src/gui/MonadoBlade.GUI/Windows/AIHubWindow.cs`

## origin/codex/combine-phased-mds-and-optimize-code-f6dw5m

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `97e676e326ceab94657d021244ff0b278cbe071e` — Consolidate phase docs and harden AI hub paths — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `README.md`
- `docs/automation/deep-github-ai-automation.md`
- `docs/optimization/AI_PERFORMANCE_SECURITY_REVIEW.md`
- `phases/CONSOLIDATED_PHASE_DOCUMENTATION.md`
- `scripts/automation/deep_automation_orchestrator.py`
- `src/core/HELIOS.Platform/Caching/IntelligentCache.cs`
- `src/core/HELIOS.Platform/Integration/HubIntegration.cs`

## origin/codex/combine-phased-mds-and-optimize-code-ozczwm

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `c8e3880cbf9355bc9f6178df9003149e19cef430` — chore: bootstrap local dotnet and azure cli tools — Yolkster64 <thepatman64@gmail.com>
- `f6c4a1b6c3557af48086a5aa284d4d6da19e22db` — chore: ignore local generated bootstrap artifacts — Yolkster64 <thepatman64@gmail.com>
- `ddfed9dc1287c6addfeec731009747f1870597ef` — docs: consolidate phased docs and optimization review — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `.gitignore`
- `README.md`
- `docs/DEVELOPMENT_TOOLCHAIN_BOOTSTRAP.md`
- `docs/automation/deep-github-ai-automation.md`
- `docs/optimization/AI_PERFORMANCE_SECURITY_OPTIMIZATION_REVIEW.md`
- `docs/phases/ALL_PHASED_MDS_CONSOLIDATED.md`
- `scripts/automation/deep_automation_orchestrator.py`
- `scripts/docs/consolidate_phased_docs.py`
- `scripts/setup/bootstrap-local-tools.sh`

## origin/codex/combine-phased-mds-and-optimize-code-qpcmmn

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `2a92dd4a5509a79e6bb25656ca2455f30e2dde1e` — Consolidate phase docs and add optimization scan — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `README.md`
- `docs/automation/deep-github-ai-automation.md`
- `docs/optimization/AI_PERFORMANCE_SECURITY_REVIEW.md`
- `docs/phases/CONSOLIDATED_PHASE_MARKDOWN.md`
- `scripts/automation/consolidate_phase_docs_and_scan.py`
- `scripts/automation/deep_automation_orchestrator.py`

## origin/codex/create-setup-guide-in-docs/setup

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `845140ca2b76183f02b527c424d8dc27cf9d5d92` — docs: separate production OIDC identity — Yolkster64 <thepatman64@gmail.com>
- `c538e4df6dabfd4d10280f87e256829c806f0ad0` — docs: separate production OIDC identity — Yolkster64 <thepatman64@gmail.com>
- `72050a6d6ff9278f6a043562b106457d3a309cbc` — docs: clarify owner setup and azure path — Yolkster64 <thepatman64@gmail.com>
- `48a73229a1e09e8982bc222518812efd42aeaabd` — docs: expand owner start guide steps — Yolkster64 <thepatman64@gmail.com>
- `debe9a47ff06e2282b124feed26ca50ac369bd01` — docs: add owner start guide — Yolkster64 <thepatman64@gmail.com>
- `7b60554f3f3da5dc7682cff1f6fce704ed226c0b` — Fix legacy test project reference — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/PULL_REQUEST_TEMPLATE/integration-merge.md`
- `.github/README.md`
- `.github/pages/index.html`
- `.github/workflows/helios-dotnet-ci.yml`
- `README.md`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `docs/setup/OWNER_START_HERE.md`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/codex/find-best-steps-to-set-up-environment

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `4e141fceac17344bfa663d4082cfd0d2ca49a2ef` — Add deep capability setup registry — Yolkster64 <thepatman64@gmail.com>
- `6190dde0aa8e12515ffc6a78d5373b3914000dbc` — Add automated mass integration orchestrator — Yolkster64 <thepatman64@gmail.com>
- `f9df33de08662c9cd6c195ee4ffd07109c61d3cc` — Add HELIOS command shell automation — Yolkster64 <thepatman64@gmail.com>
- `16c4be485bf3233c3e5f6cf929c5ce8af6881ed7` — Add Hermes XCore working steps guide — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-mass-integration.yml`
- `.github/workflows/helios-shell.yml`
- `config/helios-agents.json`
- `config/helios-capabilities.json`
- `config/helios-mass-integration.json`
- `docs/DEVELOPMENT_SETUP.md`
- `docs/HELIOS_HERMES_XCORE_WORKING_STEPS.md`
- `scripts/github/mass_integration.py`
- `scripts/integrations/helios_capability_setup.py`
- `tools/helios.ps1`

## origin/codex/fix-high-priority-issues-from-codex-review

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `c5042b47e32e6da8fa267e88975bb6f103e8232a` — Build portable core in Linux CI — Yolkster64 <thepatman64@gmail.com>
- `1077ec7395c801a627b7c10acc7e375dc1aa37be` — Fix Linux CI coverage for portable core — Yolkster64 <thepatman64@gmail.com>
- `2b43b38117fc055b285e4f532ed06393bdd36f02` — Fix review-blocking project globs — Yolkster64 <thepatman64@gmail.com>
- `c5325943f22de71e9afaf4d0266a44b46007bb6b` — Fix portable CI test project dependencies — Yolkster64 <thepatman64@gmail.com>
- `03ea9e45e6ccfef9cf44649d625a130d3e83dd17` — Guard CI test restore against broken references — Yolkster64 <thepatman64@gmail.com>
- `7b60554f3f3da5dc7682cff1f6fce704ed226c0b` — Fix legacy test project reference — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-dotnet-ci.yml`
- `HELIOS.Platform.csproj`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/SystemIntegration/PortableHotkeyInput.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/codex/fix-high-priority-issues-from-codex-review-5bbmk9

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `a3f0370237a291f6ae8052b9765955a037154387` — Add CI guardrails for review fixes — Yolkster64 <thepatman64@gmail.com>
- `5ddaea39f020ebd5ab12873e4fdb1fd2b11b0151` — Skip uncompilable portable CI builds — Yolkster64 <thepatman64@gmail.com>
- `03d4682ec6fcc2f1e3ee3da753fdfe36225ba193` — Fix portable CI exclusions — Yolkster64 <thepatman64@gmail.com>
- `2b43b38117fc055b285e4f532ed06393bdd36f02` — Fix review-blocking project globs — Yolkster64 <thepatman64@gmail.com>
- `c5325943f22de71e9afaf4d0266a44b46007bb6b` — Fix portable CI test project dependencies — Yolkster64 <thepatman64@gmail.com>
- `03ea9e45e6ccfef9cf44649d625a130d3e83dd17` — Guard CI test restore against broken references — Yolkster64 <thepatman64@gmail.com>
- `7b60554f3f3da5dc7682cff1f6fce704ed226c0b` — Fix legacy test project reference — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-dotnet-ci.yml`
- `HELIOS.Platform.csproj`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `installer/Post-Install-Verify.ps1`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/SystemIntegration/PortableHotkeyInput.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/codex/fix-high-priority-issues-from-codex-review-tuugad

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `467559f154c721f4efaa510e94793bf386cd70b5` — Optimize CI workflow execution — Yolkster64 <thepatman64@gmail.com>
- `5ddaea39f020ebd5ab12873e4fdb1fd2b11b0151` — Skip uncompilable portable CI builds — Yolkster64 <thepatman64@gmail.com>
- `03d4682ec6fcc2f1e3ee3da753fdfe36225ba193` — Fix portable CI exclusions — Yolkster64 <thepatman64@gmail.com>
- `2b43b38117fc055b285e4f532ed06393bdd36f02` — Fix review-blocking project globs — Yolkster64 <thepatman64@gmail.com>
- `c5325943f22de71e9afaf4d0266a44b46007bb6b` — Fix portable CI test project dependencies — Yolkster64 <thepatman64@gmail.com>
- `03ea9e45e6ccfef9cf44649d625a130d3e83dd17` — Guard CI test restore against broken references — Yolkster64 <thepatman64@gmail.com>
- `7b60554f3f3da5dc7682cff1f6fce704ed226c0b` — Fix legacy test project reference — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-dotnet-ci.yml`
- `HELIOS.Platform.csproj`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `installer/Post-Install-Verify.ps1`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/SystemIntegration/PortableHotkeyInput.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/codex/fix-linux-ci-coverage-issues-in-pull-request-#85

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `5539394b685cd1473476c4a1c42328f7d4709335` — Resolve full core references before Linux test skips — Yolkster64 <thepatman64@gmail.com>
- `a1d2305f6fdfad990b29d94a511257d62bb7f2af` — Skip Linux tests that rebuild full core — Yolkster64 <thepatman64@gmail.com>
- `13f59d8c46f99399c4421994f5de7ee65f3c96df` — Skip Linux tests that rebuild full core — Yolkster64 <thepatman64@gmail.com>
- `c5042b47e32e6da8fa267e88975bb6f103e8232a` — Build portable core in Linux CI — Yolkster64 <thepatman64@gmail.com>
- `1077ec7395c801a627b7c10acc7e375dc1aa37be` — Fix Linux CI coverage for portable core — Yolkster64 <thepatman64@gmail.com>
- `2b43b38117fc055b285e4f532ed06393bdd36f02` — Fix review-blocking project globs — Yolkster64 <thepatman64@gmail.com>
- `c5325943f22de71e9afaf4d0266a44b46007bb6b` — Fix portable CI test project dependencies — Yolkster64 <thepatman64@gmail.com>
- `03ea9e45e6ccfef9cf44649d625a130d3e83dd17` — Guard CI test restore against broken references — Yolkster64 <thepatman64@gmail.com>
- `7b60554f3f3da5dc7682cff1f6fce704ed226c0b` — Fix legacy test project reference — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-dotnet-ci.yml`
- `HELIOS.Platform.csproj`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/SystemIntegration/PortableHotkeyInput.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/codex/fix-oidc-permission-in-pr-workflow

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `87fe750ad5bbd91a742bce2a3ba493e8b2cf1f36` — Optimize deep automation inventory coverage — Yolkster64 <thepatman64@gmail.com>
- `7d0294171811a14a0f7f344c6e390763768926bf` — Restrict Azure OIDC validation to trusted runs — Yolkster64 <thepatman64@gmail.com>
- `2fa2c79956165d73ee993322e715a10e8db5386a` — Use portable git ref inventory — Yolkster64 <thepatman64@gmail.com>
- `bd3abc4c31b6cc54845d0d39af1508a4e8c5472b` — Restrict Azure login outside PR runs — Yolkster64 <thepatman64@gmail.com>
- `79ca2afd03b2b523eb751b03e4d54004f85af724` — Redact credentials from automation remote inventory — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `README.md`
- `docs/automation/deep-github-ai-automation.md`
- `scripts/automation/deep_automation_orchestrator.py`

## origin/codex/integrate-full-ai-and-workflow-automation

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `7d0294171811a14a0f7f344c6e390763768926bf` — Restrict Azure OIDC validation to trusted runs — Yolkster64 <thepatman64@gmail.com>
- `2fa2c79956165d73ee993322e715a10e8db5386a` — Use portable git ref inventory — Yolkster64 <thepatman64@gmail.com>
- `bd3abc4c31b6cc54845d0d39af1508a4e8c5472b` — Restrict Azure login outside PR runs — Yolkster64 <thepatman64@gmail.com>
- `79ca2afd03b2b523eb751b03e4d54004f85af724` — Redact credentials from automation remote inventory — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `README.md`
- `docs/automation/deep-github-ai-automation.md`
- `scripts/automation/deep_automation_orchestrator.py`

## origin/codex/integrate-full-ai-and-workflow-automation-aov84f

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `504087ed8a329ba8a95f51aa86771f4c3d56fc42` — Add deep GitHub AI automation setup — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-deep-automation.yml`
- `docs/workflows/DEEP_GITHUB_AI_AUTOMATION_SETUP.md`
- `docs/workflows/WORKFLOW_INDEX.md`
- `scripts/setup/Initialize-HeliosDeepAutomation.ps1`

## origin/codex/organize-project-repository-and-branches

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `dc2aa44e223fa9bf0bd41745e6d2c10e5724650b` — Harden governance PR routing checks — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `1690be72c180e5519f2c96eb5f769207f1f070b1` — Fix exact backlog issue title detection — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `2102aebf0037df28bff6ad803e6ff99bea9ac68a` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `1473307687faf690ba643d155ee086ebc00c7256` — Potential fix for pull request finding 'Syntax error' — Yolkster64 <thepatman64@gmail.com>
- `c1b8177f1e65a43b96d4b6edb940f7b05be21261` — Update scripts/github/sync_repository_backlog.py — Yolkster64 <thepatman64@gmail.com>
- `7aecbf45ea05fa740fadeec5b944d207c98c72c1` — Update .github/workflows/repository-governance.yml — Yolkster64 <thepatman64@gmail.com>
- `afbaac14c1a6a65da262bb52aea8626258637172` — chore: establish repository reset governance — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/ISSUE_TEMPLATE/bug-report.md`
- `.github/ISSUE_TEMPLATE/bug_report.md`
- `.github/ISSUE_TEMPLATE/build-task.md`
- `.github/ISSUE_TEMPLATE/component-task.md`
- `.github/ISSUE_TEMPLATE/config.yml`
- `.github/ISSUE_TEMPLATE/feature-request.md`
- `.github/ISSUE_TEMPLATE/feature_request.md`
- `.github/ISSUE_TEMPLATE/task.yml`
- `.github/PULL_REQUEST_TEMPLATE.md`
- `.github/workflows/repository-governance.yml`
- `config/repository-backlog.json`
- `config/repository-governance.json`
- `docs/governance/REPOSITORY_RESET_PLAN.md`
- `scripts/github/sync_repository_backlog.py`

## origin/codex/outline-50-key-github-workflow-improvements

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `a4b05ac106ef1fb3c030d3dd0aefb790e2f53ab6` — Harden Azure deployment readiness workflow — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deploy.yml`
- `.github/workflows/unified-agent-contract.yml`
- `docs/AZURE_CLI_BOOTSTRAP.md`
- `scripts/setup/verify-azure-cli.sh`

## origin/codex/set-up-ci-automation-for-bug-merging

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `58799e547bbcce02552ba2d9e79bfd90de622e66` — Fix submodule validation working directory — Yolkster64 <thepatman64@gmail.com>
- `e7d9769faf7bd214857c2507e7688d4329bb7800` — Expand autonomous submodule governance automation — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ai-ci-code-review.yml`
- `.github/workflows/ai-governance-dashboard.yml`
- `.github/workflows/ai-issue-autofix.yml`
- `.github/workflows/ai-submodule-consolidation.yml`
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `.gitignore`
- `README.md`
- `config/automation/submodule-consolidation.json`
- `docs/automation/ai-ci-code-review-and-autofix.md`
- `docs/automation/deep-github-ai-automation.md`
- `docs/automation/submodule-consolidation-workflow.md`
- `scripts/automation/ai_issue_fix_runner.py`
- `scripts/automation/ai_review_report.py`
- `scripts/automation/deep_automation_orchestrator.py`
- `scripts/automation/github_control_plane_setup.py`
- `scripts/automation/submodule_consolidation.py`
- `scripts/automation/validate_workflows.sh`

## origin/codex/update-ai-model-configuration-and-routing

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `e6bbd1671c2cf805cb017b63eb195223ccbf8de4` — Fix AI service config and Codex chat requests — Yolkster64 <thepatman64@gmail.com>
- `6e9d72eba73a4568575da2aaf0d3cc1ef908a890` — Update AI service model routing validation — Yolkster64 <thepatman64@gmail.com>
- `7d0294171811a14a0f7f344c6e390763768926bf` — Restrict Azure OIDC validation to trusted runs — Yolkster64 <thepatman64@gmail.com>
- `2fa2c79956165d73ee993322e715a10e8db5386a` — Use portable git ref inventory — Yolkster64 <thepatman64@gmail.com>
- `bd3abc4c31b6cc54845d0d39af1508a4e8c5472b` — Restrict Azure login outside PR runs — Yolkster64 <thepatman64@gmail.com>
- `79ca2afd03b2b523eb751b03e4d54004f85af724` — Redact credentials from automation remote inventory — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `README.md`
- `config/ai-services/ai-services-config.json`
- `docs/automation/deep-github-ai-automation.md`
- `scripts/ai-services/DELIVERY_SUMMARY.md`
- `scripts/ai-services/INDEX.md`
- `scripts/ai-services/QUICK_REF.txt`
- `scripts/ai-services/README.md`
- `scripts/ai-services/SETUP.md`
- `scripts/ai-services/ai-services-config.json`
- `scripts/ai-services/ai-services-config.schema.json`
- `scripts/ai-services/chatgpt-pro-client.ps1`
- `scripts/ai-services/codex-client.ps1`
- `scripts/ai-services/configure-ai-services.ps1`
- `scripts/ai-services/gpt-4-5-client.ps1`
- `scripts/ai-services/hub.ps1`
- `scripts/ai-services/service-router.ps1`
- `scripts/ai-services/show-ai-costs.ps1`
- `scripts/ai-services/test-ai-services.ps1`
- `scripts/ai-services/validate-api-keys.ps1`
- `scripts/automation/deep_automation_orchestrator.py`

## origin/codex/update-azure-deployment-workflow-and-infrastructure-hrnhdl

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `b1098a35b19aa7a8eb174ee99bd186d745e5192b` — Fix minimal deployment web build — Yolkster64 <thepatman64@gmail.com>
- `2df55d9eea0e5e43d489fa5c4182b03f8fa0cbb6` — Fix CI deploy review issues — Yolkster64 <thepatman64@gmail.com>
- `62a0b08f68892879e9e734c43712605492a1c82a` — Stabilize pull request CI workflows — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ai-code-review.yml`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/build-variant-test.yml`
- `.github/workflows/ci-validation.yml`
- `.github/workflows/nuget.yml`
- `infrastructure/README.md`
- `infrastructure/main.bicep`
- `microsoft-ecosystem/.github/workflows/azure-deploy.yml`
- `src/core/HELIOS.Platform.Minimal/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform.Minimal/Program.cs`

## origin/codex/update-or-replace-helios.platform-solution-file

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `0d85c9a1ed78ee17415d4b65eb06fdb5234fcb1b` — Add solution and target workflows explicitly — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `.github/workflows/dotnet-build.yml`
- `.github/workflows/nuget.yml`
- `.github/workflows/publish-nuget.yml`
- `HELIOS.Platform.sln`
- `HELIOS.Platform.slnx`
- `README.md`
- `docs/automation/deep-github-ai-automation.md`
- `scripts/automation/deep_automation_orchestrator.py`

## origin/fix/powershell-parser-inventory

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `0bd47a3633da83673ec398982baeec901afc52dd` — ci: align code checks with canonical PowerShell policy — Yolkster64 <thepatman64@gmail.com>
- `8574f5900fa3efa7739109388ffb72bf4367a100` — ci: enforce explicit PowerShell debt baseline — Yolkster64 <thepatman64@gmail.com>
- `0616fe1bcd63260dcb4aeb2b5dae1937855c9e6e` — ci: baseline explicit legacy PowerShell parser debt — Yolkster64 <thepatman64@gmail.com>
- `b325806f47794ff1719e6b5b3e6ea7778e512697` — fix(ci): repair syntax reporter quoting and reduce log noise — Yolkster64 <thepatman64@gmail.com>
- `503885b55b0257a77c76107c11d6cfd530494d77` — ci: use exact PowerShell AST parser reports — Yolkster64 <thepatman64@gmail.com>
- `beb0aacf3b7da537a4eba180f02a485be947e65b` — ci: add exact PowerShell AST syntax inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ci-validation.yml`
- `.github/workflows/code-checks.yml`
- `config/powershell-syntax-baseline.json`
- `scripts/validation/Test-PowerShellSyntax.ps1`

## origin/integration/unified-agent-communication-v1

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `53d480a29889dee01a1a1ac55ccd004e15a87d9d` — Connect Copilot instructions to shared HELIOS communication contract — Yolkster64 <thepatman64@gmail.com>
- `44daaf95053750db52c62e86ab8f65aa0bd8aa33` — Add unified agent contract validation workflow — Yolkster64 <thepatman64@gmail.com>
- `05a26e71b28709d32559c6007057fb5f82005a5d` — Document HELIOS Monado unified agent communication — Yolkster64 <thepatman64@gmail.com>
- `6ba73497a13b1ef75900474f1721be95cba91897` — Add shared HELIOS integration event schema — Yolkster64 <thepatman64@gmail.com>
- `68a6223db7b9c7ba563ab1c3aefed96d07115648` — Add shared HELIOS repository role map — Yolkster64 <thepatman64@gmail.com>
- `b80f10c1a7de00fc4ef2b076daaeae5af1d3bbc4` — Add guarded Codex project configuration — Yolkster64 <thepatman64@gmail.com>
- `80816e094e4737a3c11e6c5e5e7ecf3db8da3a06` — Add shared HELIOS agent operating contract — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.codex/config.toml`
- `.github/copilot-instructions.md`
- `.github/workflows/unified-agent-contract.yml`
- `AGENTS.md`
- `config/integrations/event-contract.schema.json`
- `config/integrations/repositories.json`
- `docs/architecture/UNIFIED_AGENT_COMMUNICATION.md`

## origin/yolkster64-literate-funicular

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `5ce5acb71c087d08296853d6473c8c701445d8ba` — feat(aihub): add deep Hermes/XCore runner automation baseline — Copilot App <223556219+Copilot@users.noreply.github.com>
- `e86bc902a67b6e1fc3f5aa81f210f45478433b9a` — fix(ai-coordination): harden merge-similar clustering — Copilot App <223556219+Copilot@users.noreply.github.com>
- `1d508ca7e9862c0a1a56efbe1c8786f70210b53e` — fix(ci): remove duplicate entry point and align test TFM — Copilot App <223556219+Copilot@users.noreply.github.com>
- `f32b472c7ec1dc01c509c14383a39bed0c8e028f` — fix(ci): sync HELIOS.Platform lock file — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4609ee7e1977054b217c00b602e60aa694870607` — feat(ai-coordination): merge similar recommendations — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/aihub-self-learning-growth.yml`
- `.github/workflows/azure-infra.yml`
- `.github/workflows/branch-absorption-multicloud.yml`
- `ai-integration/REFERENCE.md`
- `ai-integration/ai-coordination/README.md`
- `ai-integration/scripts/coordinate-ai.ps1`
- `config/aihub-language-skill-profiles.json`
- `config/aihub-module-blueprint.json`
- `infra/azure/README.md`
- `infra/azure/main.bicep`
- `infra/azure/modules/xcore-hermes-runner.bicep`
- `infra/azure/parameters/dev.json`
- `scripts/analysis/agent_language_framework.py`
- `scripts/analysis/aihub_learning_feedback_loop.py`
- `scripts/analysis/xcore_hermes_runner_setup.py`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoEngineUpdateService.cs`
- `src/core/HELIOS.Platform/packages.lock.json`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/yolkster64-reimagined-dollop

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `6ee86f3a9b94a34db4f8336a1886dd4a2b0d6cb3` — Resolve duplicate Main entry point in HELIOS.Platform — Copilot App <223556219+Copilot@users.noreply.github.com>
- `7da5ad54bd8ab63bc4a5a2bc89fee5a18e7a0ddb` — Fix CI lockfile drift and workflow literal placeholders — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c425873910ca5c6be2ab09e4275d1ba781d9d156` — Add AIHub language and engine orchestration contracts — Copilot App <223556219+Copilot@users.noreply.github.com>
- `fa68a8a5a56a910f887355c4b22a4a195f3d5462` — Configure XCore9 Hermes local runner topology contract — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-unified-plugin-validate.yml`
- `monado/helios-control/config/aihub-learning-matrix.json`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/assets/connections.json`
- `plugins/helios-control-fabric/assets/runner-topology.json`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/test_helios.py`
- `plugins/helios-control-fabric/skills/helios-control/SKILL.md`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoEngineUpdateService.cs`
- `src/core/HELIOS.Platform/packages.lock.json`

## origin/yolkster64-sturdy-dollop

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `d356192764cecea28918bb18bc3334a73597b068` — Address HELIOS setup review findings — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ffc1f1e786032d3e1b83de0526b803be7d69fffe` — Fix src test project target compatibility — Copilot App <223556219+Copilot@users.noreply.github.com>
- `64060b377143ebfbdee09e161a83608cf6439e49` — Fix CI lockfile and workflow parse failures — Copilot App <223556219+Copilot@users.noreply.github.com>
- `45794c6193cb2572c1a7d2ccc9c28caa72a9bcaf` — Harden HELIOS setup command resolution and wrappers — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-unified-plugin-validate.yml`
- `monado/helios-control/README.md`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `monado/helios-control/scripts/Bootstrap-MicrosoftToolchain.ps1`
- `monado/helios-control/scripts/Start-HeliosLocal.ps1`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/scripts/helios.ps1`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/helios.sh`
- `plugins/helios-control-fabric/scripts/test_helios.py`
- `plugins/helios-control-fabric/skills/helios-control/SKILL.md`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoEngineUpdateService.cs`
- `src/core/HELIOS.Platform/packages.lock.json`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/yolkster64-ubiquitous-meme

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** high
- **Merge-tree conflicts:** none
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `e0c7f302e74275c5e60922d5d9e20b1c8784be21` — Preserve setup inventories when only some configs are missing — Copilot App <223556219+Copilot@users.noreply.github.com>
- `bbf939db78c2c44b007bd8c23328757de5230b25` — Fix portable lane restore for win-x64 minimal build — Copilot App <223556219+Copilot@users.noreply.github.com>
- `5c211c3fc1de3687a7d0fae450d764483460faef` — Fix setup output formatting after base merge — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d780cfb87ba484887946cb15976e44d4a25979cb` — Merge remote-tracking branch 'origin/integration/helios-chatgpt-copilot-app' into yolkster64-ubiquitous-meme — Copilot App <223556219+Copilot@users.noreply.github.com>
- `636f99eb9aeec0ddecb1cf136fb36ada3fe1bc8d` — Address review feedback for setup inventory command — Copilot App <223556219+Copilot@users.noreply.github.com>
- `93d3147235c5a6952994b93e702bf945582fd424` — fix(security): patch vulnerable npm transitive dependencies — Copilot App <223556219+Copilot@users.noreply.github.com>
- `17e1467633b276ec00b146691e2d508906010590` — merge(main): resolve conflicts and keep CI fixes — Copilot App <223556219+Copilot@users.noreply.github.com>
- `7596fe0c09d466a9b365d22429b6788d6f43373a` — Fix CI compile and test project compatibility — Copilot App <223556219+Copilot@users.noreply.github.com>
- `faa29287076980a57cc1535bf469d7b147c0a39d` — fix(ci): stabilize failing PR checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `05e1e79c86f2fe67ae8cb8bbf0525e7fdadb48db` — Fix HELIOS.Platform lockfile drift for CI restore — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d78aa8a6bab6499fa26637689959b29a96d1cad9` — Add full setup inventory command for HELIOS control fabric (#236) — Yolkster64 <thepatman64@gmail.com>
- `e930625fefd97926774883668d44dc0fb8367762` — Add full setup inventory command to HELIOS CLI — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3e50a9cc5f779feb0904c3f9889db39f80b8c8ed` — Merge origin/main into integration/helios-chatgpt-copilot-app — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `7cbb0677f150f3250115d0a6bc6b98131013422e` — Potential fix for pull request finding 'Missed opportunity to use Where' — Yolkster64 <thepatman64@gmail.com>
- `f3290ad2ddd6e54ab6a5284c46cabb335a09ecb9` — Potential fix for pull request finding 'Missing Dispose call on local IDisposable' — Yolkster64 <thepatman64@gmail.com>
- `fbdd1bada8a1bb8b4f244c4a196d9553fe586abf` — Potential fix for pull request finding 'Poor error handling: empty catch block' — Yolkster64 <thepatman64@gmail.com>
- `471f1e07d839daab27d3adbdb0cc01afe67bff2c` — Potential fix for pull request finding 'Generic catch clause' — Yolkster64 <thepatman64@gmail.com>
- `642297079db21fd2bab26e561d7877b644bd1ec0` — Discover exact component manifest directories — Yolkster64 <thepatman64@gmail.com>
- `dc1f5c08944101de619e0cd77cdee00be307332f` — Fix path-safe component CI outputs — Yolkster64 <thepatman64@gmail.com>
- `f72dc9a95c55657980809ca6cc82bdee7e6338ee` — Complete Bicep module references and identities — Yolkster64 <thepatman64@gmail.com>
- `5963b7f63aa899f046cf62aa157d52238fea4d9d` — Import xUnit test attributes — Yolkster64 <thepatman64@gmail.com>
- `a68f43b6bb1ee18ac79e5bda20837dcbe08c4f5a` — Fix net8 HMAC hex parsing — Yolkster64 <thepatman64@gmail.com>
- `9abbcb0a4fc54397a9fc374aee0e2cdd05cb23dc` — Make HELIOS automation fabric buildable — Yolkster64 <thepatman64@gmail.com>
- `bd1120c57af7c19eb8687d22e9124e4e692e15dd` — Add review-only HELIOS automation fabric reference — Yolkster64 <thepatman64@gmail.com>
- `1993d8774b1c6aa86851f7e0d6cf94de7d4cb5d9` — Validate every enterprise setup operation — Yolkster64 <thepatman64@gmail.com>
- `5aa8ad65186e4b866cce7925b0d017c4eeecb2ff` — Keep Slack and Linear OAuth in MCP config without breaking setup phases — Yolkster64 <thepatman64@gmail.com>
- `4f5028aa2600c77e7dec9c358730de96fa8851bb` — Add Slack and Linear authentication to enterprise setup — Yolkster64 <thepatman64@gmail.com>
- `4ba2aee70e2b4c024d2f066f0fb8ed709090f106` — Use current Linear and Slack MCP endpoints — Yolkster64 <thepatman64@gmail.com>
- `77e02c8014be22b3f9da8d6ee8910cd53aa06855` — Add tests for enterprise setup controller — Yolkster64 <thepatman64@gmail.com>
- `2dba9bebd66e3d447f393968c5b8d67b12d6107a` — Add guarded enterprise setup controller to Azure CLI plugin — Yolkster64 <thepatman64@gmail.com>
- `861d2f8b30df89cbf1338e736875a35676b99aef` — Add guarded enterprise setup operation manifest — Yolkster64 <thepatman64@gmail.com>
- `38107738ca70d3483061bedc50c762a181a8ddc6` — Add Agent 365 Microsoft 365 tooling manifest — Yolkster64 <thepatman64@gmail.com>
- `49a09e8c2cdfda723c55b6e9ceb8b7608e90eb3c` — Add Agent 365 configuration template — Yolkster64 <thepatman64@gmail.com>
- `a236482038ec9b68a3131c487bbfe6b8a941d9ea` — Add Claude Code MCP configuration to shared control app — Yolkster64 <thepatman64@gmail.com>
- `a72aa66caec61f7368d05a2140d6eaff184b0be1` — Add Claude Code instructions to shared control app — Yolkster64 <thepatman64@gmail.com>
- `6429f1f24fa33f888d38185636e8ce4699e45efa` — fix(azure-cli): harden app lookup and parameter validation — Yolkster64 <thepatman64@gmail.com>
- `44085ed63a4f9f9096d174b09df4c1a9263ace43` — feat(azure): add guarded HELIOS CLI plugin and canonical coordination targets — Yolkster64 <thepatman64@gmail.com>
- `491578746496ba2dae786cfde025cbb6c55c93b5` — style(control-app): terminate safe link append explicitly — Yolkster64 <thepatman64@gmail.com>
- `8855ef4d39553b95db64d94761fa801a802486e4` — fix(control-app): harden widget rendering in .github/workflows/helios-control-app.yml — Yolkster64 <thepatman64@gmail.com>
- `90d7aaacb1b727c95f7ece389ac3731611dcd878` — fix(control-app): harden widget rendering in apps/helios-control/scripts/smoke.mjs — Yolkster64 <thepatman64@gmail.com>
- `ec1f6ff3a9bce3f5c4f5429fa90832919abce19f` — fix(control-app): harden widget rendering in apps/helios-control/public/control-center.html — Yolkster64 <thepatman64@gmail.com>
- `2949004f445d5d85c7548e2f3921973b26de203c` — feat(control-app): add .github/workflows/helios-control-app.yml — Yolkster64 <thepatman64@gmail.com>
- `3aaa4522a2de1846daaf328afd474aa0b5e478e5` — feat(control-app): add apps/helios-control/tsconfig.json — Yolkster64 <thepatman64@gmail.com>
- `9ec5b5fa626e9b5ba44ed96efe170b86bfb2a1d9` — feat(control-app): add apps/helios-control/src/server.ts — Yolkster64 <thepatman64@gmail.com>
- `85c51e1da3d09fcf58339dce0e2961746deb545c` — feat(control-app): add apps/helios-control/scripts/smoke.mjs — Yolkster64 <thepatman64@gmail.com>
- `e2c1e139cdbaead5619f49b9c354bd91189e656c` — feat(control-app): add apps/helios-control/public/control-center.html — Yolkster64 <thepatman64@gmail.com>
- `f5cb40a300f4a771391fb7e517ca3e958bed61f6` — feat(control-app): add apps/helios-control/package.json — Yolkster64 <thepatman64@gmail.com>
- `ec291a61ae120a9aeadf0e25c1440e0ba95fbfe0` — feat(control-app): add apps/helios-control/package-lock.json — Yolkster64 <thepatman64@gmail.com>
- `c63f09e027e35ac1bdb40a2a0a4ffb2365fe73ea` — feat(control-app): add apps/helios-control/copilot/helios-mcp.openapi.yaml — Yolkster64 <thepatman64@gmail.com>
- `93d93d9739d43beedd72e6554f337f66dbcf3096` — feat(control-app): add apps/helios-control/README.md — Yolkster64 <thepatman64@gmail.com>
- `eb05da0b8a68d6bc52b5e5dcf54ab42d033cd654` — feat(control-app): add apps/helios-control/Dockerfile — Yolkster64 <thepatman64@gmail.com>
- `bc67d4094dc4ea867c0d9132ba7d6a3cdfb00940` — feat(control-app): add apps/helios-control/.env.example — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.agents/plugins/marketplace.json`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/code-checks.yml`
- `.github/workflows/helios-control-app.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `apps/helios-control/.env.example`
- `apps/helios-control/.mcp.json`
- `apps/helios-control/CLAUDE.md`
- `apps/helios-control/Dockerfile`
- `apps/helios-control/README.md`
- `apps/helios-control/copilot/agent365/ToolingManifest.json`
- `apps/helios-control/copilot/agent365/a365.config.template.json`
- `apps/helios-control/copilot/helios-mcp.openapi.yaml`
- `apps/helios-control/package-lock.json`
- `apps/helios-control/package.json`
- `apps/helios-control/public/control-center.html`
- `apps/helios-control/scripts/smoke.mjs`
- `apps/helios-control/src/server.ts`
- `apps/helios-control/tsconfig.json`
- `eng/test/test-ownership.json`
- `eng/test/validate_test_ownership.py`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `plugins/helios-azure-cli/.codex-plugin/plugin.json`
- `plugins/helios-azure-cli/README.md`
- `plugins/helios-azure-cli/SKILL.md`
- `plugins/helios-azure-cli/assets/enterprise-setup.json`
- `plugins/helios-azure-cli/assets/helios-targets.json`
- `plugins/helios-azure-cli/scripts/helios-azure.ps1`
- `plugins/helios-azure-cli/scripts/helios-azure.sh`
- `plugins/helios-azure-cli/scripts/helios_azure.py`
- `plugins/helios-azure-cli/scripts/helios_enterprise.py`
- `plugins/helios-azure-cli/scripts/test_helios_azure.py`
- `plugins/helios-azure-cli/scripts/test_helios_enterprise.py`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/test_helios.py`
- `plugins/helios-control-fabric/skills/helios-control/SKILL.md`
- `reference/helios-enterprise-automation-fabric/.Register-HeliosTeamsWorkflow.ps1.nqnd05jk`
- `reference/helios-enterprise-automation-fabric/FabricWorker.cs`
- `reference/helios-enterprise-automation-fabric/GitHubControlSink.cs`
- `reference/helios-enterprise-automation-fabric/HELIOS.Fabric.sln`
- `reference/helios-enterprise-automation-fabric/Initialize-HeliosAzureOidc.ps1`
- `reference/helios-enterprise-automation-fabric/Initialize-HeliosPowerPlatform.ps1`
- `reference/helios-enterprise-automation-fabric/Initialize-HeliosSharePoint.ps1`
- `reference/helios-enterprise-automation-fabric/LinearSink.cs`
- `reference/helios-enterprise-automation-fabric/Program.cs`
- `reference/helios-enterprise-automation-fabric/README.md`
- `reference/helios-enterprise-automation-fabric/RECOVERY_STATUS.md`
- `reference/helios-enterprise-automation-fabric/RUN_THIS_FIRST.md`
- `reference/helios-enterprise-automation-fabric/Register-HeliosTeamsWorkflow.ps1`
- `reference/helios-enterprise-automation-fabric/SharePointEvidenceSink.cs`
- `reference/helios-enterprise-automation-fabric/SlackSink.cs`
- `reference/helios-enterprise-automation-fabric/TeamsSink.cs`
- `reference/helios-enterprise-automation-fabric/WebhookEndpoints.cs`
- `reference/helios-enterprise-automation-fabric/action-pin-plan.yml`
- `reference/helios-enterprise-automation-fabric/action-policy.json`
- `reference/helios-enterprise-automation-fabric/apiDefinition.swagger.json`
- `reference/helios-enterprise-automation-fabric/apiProperties.json`
- `reference/helios-enterprise-automation-fabric/app-manifest.yaml`
- `reference/helios-enterprise-automation-fabric/approval-policy.json`
- `reference/helios-enterprise-automation-fabric/automations.json`
- `reference/helios-enterprise-automation-fabric/azure-deploy.yml`
- `reference/helios-enterprise-automation-fabric/azure-plan.yml`
- `reference/helios-enterprise-automation-fabric/azure.yaml`
- `reference/helios-enterprise-automation-fabric/broker-images.yml`
- `reference/helios-enterprise-automation-fabric/channel-bindings.example.json`
- `reference/helios-enterprise-automation-fabric/channels.json`
- `reference/helios-enterprise-automation-fabric/config-drift.yml`
- `reference/helios-enterprise-automation-fabric/connector-readiness.yml`
- `reference/helios-enterprise-automation-fabric/connector-registry.json`
- `reference/helios-enterprise-automation-fabric/containerapps.bicep`
- `reference/helios-enterprise-automation-fabric/control-plane-operator.yml`
- `reference/helios-enterprise-automation-fabric/deployment-approval.json`
- `reference/helios-enterprise-automation-fabric/emergency-quarantine.yml`
- `reference/helios-enterprise-automation-fabric/event-envelope.schema.json`
- `reference/helios-enterprise-automation-fabric/evidence-mapping.json`
- `reference/helios-enterprise-automation-fabric/execution-plan.json`
- `reference/helios-enterprise-automation-fabric/fabric-ci.yml`
- `reference/helios-enterprise-automation-fabric/graph-permission-plan.json`
- `reference/helios-enterprise-automation-fabric/incident-critical.json`
- `reference/helios-enterprise-automation-fabric/information-architecture.json`
- `reference/helios-enterprise-automation-fabric/issue-templates.json`
- `reference/helios-enterprise-automation-fabric/linear_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/lists-and-libraries.json`
- `reference/helios-enterprise-automation-fabric/main.bicep`
- `reference/helios-enterprise-automation-fabric/nightly-health.yml`
- `reference/helios-enterprise-automation-fabric/pin_actions.py`
- `reference/helios-enterprise-automation-fabric/pr-linear-sync.yml`
- `reference/helios-enterprise-automation-fabric/project/.dockerignore`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/action-pin-plan.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/azure-deploy.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/azure-plan.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/broker-images.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/config-drift.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/connector-readiness.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/control-plane-operator.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/emergency-quarantine.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/fabric-ci.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/nightly-health.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/pr-linear-sync.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/promote-reviewed-sha.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/release-evidence.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/reusable-build-event.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/sharepoint-governance-sync.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/workflow-outcome-router.yml`
- `reference/helios-enterprise-automation-fabric/project/.gitignore`
- `reference/helios-enterprise-automation-fabric/project/README.md`
- `reference/helios-enterprise-automation-fabric/project/RUN_THIS_FIRST.md`
- `reference/helios-enterprise-automation-fabric/project/azure.yaml`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/approval-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/connector-registry.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/deployment-approval.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/event-envelope.schema.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/evidence-mapping.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/execution-plan.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/routing-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/status-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/github/action-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/linear/automations.json`
- `reference/helios-enterprise-automation-fabric/project/config/linear/issue-templates.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/information-architecture.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/lists-and-libraries.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/retention-labels.example.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/sharepoint-sync-manifest.json`
- `reference/helios-enterprise-automation-fabric/project/config/slack/app-manifest.yaml`
- `reference/helios-enterprise-automation-fabric/project/config/slack/channel-bindings.example.json`
- `reference/helios-enterprise-automation-fabric/project/config/slack/channels.json`
- `reference/helios-enterprise-automation-fabric/project/config/teams/workflow-contracts(1).json`
- `reference/helios-enterprise-automation-fabric/project/config/teams/workflow-contracts.json`
- `reference/helios-enterprise-automation-fabric/project/docker/broker.Dockerfile`
- `reference/helios-enterprise-automation-fabric/project/docker/worker.Dockerfile`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/containerapps.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/main.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/modules/foundation.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/modules/private-endpoint.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/parameters/dev.bicepparam`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/parameters/prod.bicepparam`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/parameters/stage.bicepparam`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/servicebus.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/storage.bicep`
- `reference/helios-enterprise-automation-fabric/project/power-platform/connector/apiDefinition.swagger.json`
- `reference/helios-enterprise-automation-fabric/project/power-platform/connector/apiProperties.json`
- `reference/helios-enterprise-automation-fabric/project/pyproject.toml`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Initialize-HeliosAzureOidc.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Initialize-HeliosPowerPlatform.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Initialize-HeliosSharePoint.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Install-HeliosFabricOverlay.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Register-HeliosTeamsWorkflow.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/linear_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/slack_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/connectors/sharepoint_sync.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/build_event.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/canonicalize_plan.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/control_plane_dispatch.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/pin_actions.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/publish_event.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/governance/pin_actions.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/governance/sharepoint_sync.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/validation/Validate-PowerShell.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/validation/assert_no_secret_values.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/validation/validate_bicep.sh`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/BrokerServices.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/HELIOS.Fabric.Broker.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/Program.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/WebhookEndpoints.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Contracts/FabricContracts.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Contracts/HELIOS.Fabric.Contracts.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Tests/FabricTests.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Tests/HELIOS.Fabric.Tests.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/FabricWorker.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/GitHubControlSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/HELIOS.Fabric.Worker.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/LinearSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/SharePointEvidenceSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/SlackSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/TeamsSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/WorkerProgram.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/WorkerServices.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.sln`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric/__init__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric/__main__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric/cli.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric_cli/__init__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric_cli/__main__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric_cli/cli.py`
- `reference/helios-enterprise-automation-fabric/project/tests/fixtures/deployment-plan.json`
- `reference/helios-enterprise-automation-fabric/project/tests/test_fabricctl.py`
- `reference/helios-enterprise-automation-fabric/promote-reviewed-sha.yml`
- `reference/helios-enterprise-automation-fabric/release-evidence.yml`
- `reference/helios-enterprise-automation-fabric/retention-labels.example.json`
- `reference/helios-enterprise-automation-fabric/reusable-build-event.yml`
- `reference/helios-enterprise-automation-fabric/routing-policy.json`
- `reference/helios-enterprise-automation-fabric/servicebus.bicep`
- `reference/helios-enterprise-automation-fabric/sharepoint-governance-sync.yml`
- `reference/helios-enterprise-automation-fabric/sharepoint-sync-manifest.json`
- `reference/helios-enterprise-automation-fabric/sharepoint_sync.py`
- `reference/helios-enterprise-automation-fabric/slack_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/status-policy.json`
- `reference/helios-enterprise-automation-fabric/storage.bicep`
- `reference/helios-enterprise-automation-fabric/sync-map.json`
- `reference/helios-enterprise-automation-fabric/webhook-config.example.json`
- `reference/helios-enterprise-automation-fabric/workflow-contracts(1).json`
- `reference/helios-enterprise-automation-fabric/workflow-contracts.json`
- `reference/helios-enterprise-automation-fabric/workflow-outcome-router.yml`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/Phase10/AIOrchestration/Tests/AIOrchestrationTests.cs`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/UserAccountManagementTests.cs`
- `src/core/HELIOS.Platform/Tests/AI/MLIntegrationTests.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`
- `tests/analytics/HELIOS.Analytics.FSharp.Tests/MonadoEnterpriseProfileScoringV2Tests.fs`

## origin/agent/dev-cockpit-v1

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `df23b5e4479c023a27ed0f1dd97109ea67d7470d` — test(cockpit): enforce supported feature semantics — Yolkster64 <thepatman64@gmail.com>
- `bcc2e2de86a1013238bf67bdf27eec1031928c1d` — fix(toolchain): declare powershell channel matching — Yolkster64 <thepatman64@gmail.com>
- `84c364e11f902b8ab228bc65f8186020d7cdf6c9` — fix(cockpit): align portable tool compatibility — Yolkster64 <thepatman64@gmail.com>
- `fca7c75f02548abb98545a8388aa65a584fd74d0` — docs(devcontainer): clarify feature channel contracts — Yolkster64 <thepatman64@gmail.com>
- `5356a99fae74923e7c09926b300ddc9320708a9b` — fix(devcontainer): install the locked dotnet SDK — Yolkster64 <thepatman64@gmail.com>
- `5dfa74b20b23db5ec61bfd91d293adee351754b2` — test: guard cockpit bootstrap and Node pins — Yolkster64 <thepatman64@gmail.com>
- `0b97f20149147e8ae55fc0aa44884d26eb87dfba` — fix: align module build with locked Node toolchain — Yolkster64 <thepatman64@gmail.com>
- `eeea9145f4e58e42975d340b43a931dae8f0ed41` — fix: remove stale Yarn apt source before cockpit build — Yolkster64 <thepatman64@gmail.com>
- `da7c073ca994a9bae252459ec77e0bbba274e5df` — feat: add governed developer cockpit — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.devcontainer/Dockerfile`
- `.devcontainer/README.md`
- `.devcontainer/codespaces-secrets.env.example`
- `.devcontainer/devcontainer.json`
- `.devcontainer/docker-compose.yml`
- `.devcontainer/onCreateCommand.sh`
- `.devcontainer/package-lock.json`
- `.devcontainer/package.json`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/helios-dev-cockpit.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.gitignore`
- `.mcp.json`
- `.vscode/extensions.json`
- `.vscode/launch.json`
- `.vscode/mcp.json`
- `.vscode/settings.json`
- `.vscode/tasks.json`
- `CLAUDE.md`
- `config/dev/toolchain-lock.json`
- `docs/guides/HELIOS_DEVELOPER_COCKPIT.md`
- `global.json`
- `monado/helios-control/.mcp.json`
- `monado/helios-control/.vscode/mcp.json`
- `monado/helios-control/HeliosControl.code-workspace`
- `monado/helios-control/appPackage/manifest.json`
- `monado/helios-control/config/agent-fleet.json`
- `monado/helios-control/config/cli-matrix.json`
- `monado/helios-control/config/microsoft-toolchain.json`
- `monado/helios-control/docs/MULTI_AGENT_WORKBENCH.md`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `monado/helios-control/src/Helios.Connect.Api/Program.cs`
- `plugins/helios-control-fabric/.codex-plugin/plugin.json`
- `plugins/helios-control-fabric/.mcp.json`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/assets/runner-topology.json`
- `plugins/helios-control-fabric/scripts/test_helios.py`
- `plugins/helios-control-fabric/skills/helios-control/SKILL.md`
- `plugins/helios-control-fabric/skills/helios-dev-cockpit/SKILL.md`
- `scripts/dev/bootstrap-cockpit.sh`
- `scripts/dev/devsetup.sh`
- `scripts/dev/helios_dev_doctor.py`
- `scripts/dev/portable-validate.sh`
- `scripts/dev/tests/test_helios_dev_doctor.py`
- `scripts/setup/bootstrap-local-tools.sh`
- `services/helios-deployment-agent/tests/test_http_boundary.py`
- `services/helios-deployment-agent/tests/test_policy.py`
- `verify-setup.sh`

## origin/codex/create-integration-merge-pr-template

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `8d014a45ebacfbbf27ccb18aa54f5163f4f1b875` — Add integration merge PR template — Yolkster64 <thepatman64@gmail.com>
- `7b60554f3f3da5dc7682cff1f6fce704ed226c0b` — Fix legacy test project reference — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/PULL_REQUEST_TEMPLATE/integration-merge.md`
- `.github/workflows/helios-dotnet-ci.yml`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/codex/optimize-code-automation-and-github-usage

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `6d560356c68a761daa568ea3e3dd10d98042e2cf` — Optimize GitHub automation and consolidation setup — Yolkster64 <thepatman64@gmail.com>
- `ea6df3ec01d90905e12d87ed991acbc785a449de` — Add consolidation source manifest — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ai-code-review.yml`
- `.github/workflows/analysis.yml`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/build-variant-test.yml`
- `.github/workflows/ci-validation.yml`
- `.github/workflows/code-checks.yml`
- `.github/workflows/code-registry-update.yml`
- `.github/workflows/deploy.yml`
- `.github/workflows/documentation-update.yml`
- `.github/workflows/dotnet-build.yml`
- `.github/workflows/nuget.yml`
- `.github/workflows/phase-build.yml`
- `.github/workflows/platform-automation.yml`
- `.github/workflows/quality.yml`
- `.github/workflows/status-dashboard.yml`
- `.github/workflows/verify.yml`
- `.github/workflows/wiki-generator.yml`
- `.gitignore`
- `.gitmodules`
- `.registry/registry-metadata.json`
- `.registry/snippets.json`
- `BUILD_VARIANTS.md`
- `COMPONENT_ANALYSIS.md`
- `COMPONENT_MATRIX.md`
- `DELIVERY_MANIFEST.md`
- `Directory.Build.props`
- `GITHUB_PROJECT_SETUP.md`
- `HELIOS.Platform.sln`
- `HELIOS.Platform.slnx`
- `MERGE_SOURCE_MANIFEST.yaml`
- `PROJECT_BOARD_QUICK_START.md`
- `build.ps1`
- `docs/WIKI_INDEX.md`
- `docs/github/GITHUB_AUTOMATION_SETUP.md`
- `global.json`
- `manifest.json`
- `package.json`
- `scripts/deploy/azure/setup-azure-cli.sh`
- `scripts/deploy/azure/verify-azure-cli.sh`
- `scripts/github/prepare-consolidation.py`
- `scripts/github/validate-workflows.py`
- `src/phases/master-deploy.ps1`
- `src/phases/phase-0-preflight.ps1`
- `src/phases/phase-1-infrastructure.ps1`
- `src/phases/phase-2-agents.ps1`
- `src/phases/phase-3-ai-services.ps1`
- `src/phases/phase-4-security.ps1`
- `src/phases/phase-5-monitoring.ps1`
- `src/phases/phase-6-verification.ps1`

## origin/copilot/do-this-along-with-all

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `8d014a45ebacfbbf27ccb18aa54f5163f4f1b875` — Add integration merge PR template — Yolkster64 <thepatman64@gmail.com>
- `7b60554f3f3da5dc7682cff1f6fce704ed226c0b` — Fix legacy test project reference — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/PULL_REQUEST_TEMPLATE/integration-merge.md`
- `.github/workflows/helios-dotnet-ci.yml`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/copilot/get-commits-fixed-and-finished

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `31f4683f0c8f198df7c7f72e51c6231a47dba249` — ci: migrate upload-artifact workflows to v4 — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `8d014a45ebacfbbf27ccb18aa54f5163f4f1b875` — Add integration merge PR template — Yolkster64 <thepatman64@gmail.com>
- `7b60554f3f3da5dc7682cff1f6fce704ed226c0b` — Fix legacy test project reference — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/PULL_REQUEST_TEMPLATE/integration-merge.md`
- `.github/workflows/analysis.yml`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/build-variant-test.yml`
- `.github/workflows/code-registry-update.yml`
- `.github/workflows/dotnet-build.yml`
- `.github/workflows/helios-dotnet-ci.yml`
- `.github/workflows/nuget.yml`
- `.github/workflows/quality.yml`
- `.github/workflows/status-dashboard.yml`
- `.github/workflows/verify.yml`
- `.github/workflows/wiki-generator.yml`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/copilot/understand-codebase-structure

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `85d0e06dc0d733a6ac22b2105e3e5f6c1975a319` — Fix PR comment job token permissions — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `e82374223cf9ee06ad2afea5bf0de22102a2b56b` — Fix PR comment job token permissions — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `f1b22c7ec1eedf136951c16879eb317b61269e65` — Update setup-node references to v4 in docs — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `de7429c2dabf4f59400dcdf6f12c45fdc98cf841` — Update docs to use GitHub Actions v4 references — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `a6b13d3cca2620ce4ea0b90a37b9f0e406550fd6` — Upgrade deprecated GitHub Actions in automation workflows — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/WORKFLOWS_REFERENCE.md`
- `.github/workflows/ai-code-review.yml`
- `.github/workflows/analysis.yml`
- `.github/workflows/build-variant-test.yml`
- `.github/workflows/code-checks.yml`
- `.github/workflows/code-registry-update.yml`
- `.github/workflows/dotnet-build.yml`
- `.github/workflows/nuget.yml`
- `.github/workflows/quality.yml`
- `.github/workflows/status-dashboard.yml`
- `.github/workflows/verify.yml`
- `.github/workflows/wiki-generator.yml`
- `WORKFLOW_ANALYSIS.md`
- `docs/DEPLOYMENT_PLAYBOOK.md`
- `docs/GITHUB_PROJECT_SETUP.md`
- `docs/board-setup/BOARD_AUTOMATION_RULES.md`
- `docs/github-best-practices/AUTOMATION_GUIDE.md`
- `docs/github-best-practices/SECURITY_PRACTICES.md`
- `docs/integration/GITHUB_ECOSYSTEM_INTEGRATION.md`
- `docs/integration/NUGET_GITHUB_PAGES_INTEGRATION.md`
- `docs/integration/WORKFLOW_INTEGRATION_SYSTEM.md`
- `docs/optimization/GITHUB_ACTIONS_OPTIMIZATION.md`
- `docs/workflows/WORKFLOWS_BEST_PRACTICES.md`
- `docs/workflows/WORKFLOWS_TROUBLESHOOTING.md`
- `docs/workflows/WORKFLOW_BUILD.md`
- `docs/workflows/WORKFLOW_CUSTOMIZATION.md`
- `docs/workflows/WORKFLOW_NUGET.md`

## origin/rescue/consolidation-aihub-integration

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `59ba23ea8fe7eff436142f4d4130c408893ed0a3` — Add HELIOS Installer module contract — Yolkster64 <thepatman64@gmail.com>
- `b48bf5e9277cb9575c79fdda9c52ab87acf8b3b8` — Add HELIOS GUI module contract — Yolkster64 <thepatman64@gmail.com>
- `3f9b5c1e5d7a37d7ca111f6c8a9bef1ddfa2f51c` — Add WinUI3 USB wizard and Monado GUI plan — Yolkster64 <thepatman64@gmail.com>
- `32b8f22d1319cc8286180323d6475c5fb97ab19e` — Add hardware snapshot model — Yolkster64 <thepatman64@gmail.com>
- `ee6b80d80f94a096ebd5f549b3762a11dfcb6327` — Add HardwareDetection C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `ae7d2ab877d16b523bb3e2791448e46c4672c144` — Add software catalog package model — Yolkster64 <thepatman64@gmail.com>
- `fbb2c57028e4a6871b3877861a00ba55ffad6b63` — Add SoftwareCatalog C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `072255f06998802e02f170a5ce96e1d15261060b` — Add AIHub provider stubs — Yolkster64 <thepatman64@gmail.com>
- `ef9f8a683a6fe91a210c90dfcc0b29a13fa0b929` — Add local AIHub provider foundation — Yolkster64 <thepatman64@gmail.com>
- `7264dfa5846e28401405b8aaeb5b4d7139bdd11d` — Harden CI project reference validation — Yolkster64 <thepatman64@gmail.com>
- `a206a1c08786a7e6d6e3c18651d537cd19f5b063` — Fix CI project reference handling — Yolkster64 <thepatman64@gmail.com>
- `45b27c6a2113ebb25de198458de80d1ca7751e67` — Make portable src tests pass — Yolkster64 <thepatman64@gmail.com>
- `5ddaea39f020ebd5ab12873e4fdb1fd2b11b0151` — Skip uncompilable portable CI builds — Yolkster64 <thepatman64@gmail.com>
- `03d4682ec6fcc2f1e3ee3da753fdfe36225ba193` — Fix portable CI exclusions — Yolkster64 <thepatman64@gmail.com>
- `2b43b38117fc055b285e4f532ed06393bdd36f02` — Fix review-blocking project globs — Yolkster64 <thepatman64@gmail.com>
- `c5325943f22de71e9afaf4d0266a44b46007bb6b` — Fix portable CI test project dependencies — Yolkster64 <thepatman64@gmail.com>
- `03ea9e45e6ccfef9cf44649d625a130d3e83dd17` — Guard CI test restore against broken references — Yolkster64 <thepatman64@gmail.com>
- `7b60554f3f3da5dc7682cff1f6fce704ed226c0b` — Fix legacy test project reference — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-dotnet-ci.yml`
- `HELIOS.Platform.csproj`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `docs/ui/WINUI3_USB_WIZARD_AND_MONADO_GUI_PLAN.md`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Providers/LocalAiProvider.cs`
- `src/HELIOS.AIHub/Providers/LocalAiProviderOptions.cs`
- `src/HELIOS.AIHub/Providers/ProviderStubs.cs`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/HELIOS.GUI/README.md`
- `src/HELIOS.HardwareDetection/HELIOS.HardwareDetection.csproj`
- `src/HELIOS.HardwareDetection/HardwareSnapshot.cs`
- `src/HELIOS.Installer/README.md`
- `src/HELIOS.SoftwareCatalog/HELIOS.SoftwareCatalog.csproj`
- `src/HELIOS.SoftwareCatalog/Models/SoftwarePackage.cs`
- `src/core/HELIOS.Platform/Core/Performance/AssetLoadingOptimizer.cs`
- `src/core/HELIOS.Platform/Core/Performance/GPURenderingOptimizer.cs`
- `src/core/HELIOS.Platform/Core/Performance/MemoryOptimizationService.cs`
- `src/core/HELIOS.Platform/Core/Performance/ObjectPoolService.cs`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/SystemIntegration/PortableHotkeyInput.cs`
- `src/tests/AIHub/LocalAiProviderTests.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `src/tests/Phase8Stream8OptimizationTests.cs`

## origin/services/helios-tool-broker-v1

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `7f0a3473d43b284cbb2a796a978c5c31303ec48c` — feat(integrations): add guarded tools plugins and broker services — Yolkster64 <thepatman64@gmail.com>
- `53d480a29889dee01a1a1ac55ccd004e15a87d9d` — Connect Copilot instructions to shared HELIOS communication contract — Yolkster64 <thepatman64@gmail.com>
- `44daaf95053750db52c62e86ab8f65aa0bd8aa33` — Add unified agent contract validation workflow — Yolkster64 <thepatman64@gmail.com>
- `05a26e71b28709d32559c6007057fb5f82005a5d` — Document HELIOS Monado unified agent communication — Yolkster64 <thepatman64@gmail.com>
- `6ba73497a13b1ef75900474f1721be95cba91897` — Add shared HELIOS integration event schema — Yolkster64 <thepatman64@gmail.com>
- `68a6223db7b9c7ba563ab1c3aefed96d07115648` — Add shared HELIOS repository role map — Yolkster64 <thepatman64@gmail.com>
- `b80f10c1a7de00fc4ef2b076daaeae5af1d3bbc4` — Add guarded Codex project configuration — Yolkster64 <thepatman64@gmail.com>
- `80816e094e4737a3c11e6c5e5e7ecf3db8da3a06` — Add shared HELIOS agent operating contract — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.codex/agents/integration-broker.toml`
- `.codex/config.toml`
- `.github/copilot-instructions.md`
- `.github/workflows/tools-plugins-services.yml`
- `.github/workflows/unified-agent-contract.yml`
- `AGENTS.md`
- `config/integrations/event-contract.schema.json`
- `config/integrations/plugin-catalog.json`
- `config/integrations/repositories.json`
- `config/integrations/service-catalog.json`
- `config/integrations/tool-catalog.json`
- `docs/architecture/UNIFIED_AGENT_COMMUNICATION.md`
- `docs/integrations/TOOLS_PLUGINS_SERVICES.md`
- `plugins/copilot-studio/README.md`
- `plugins/copilot-studio/helios-openapi.yaml`
- `plugins/openai/helios-mcp/README.md`
- `plugins/openai/helios-mcp/package.json`
- `plugins/openai/helios-mcp/src/broker-client.mjs`
- `plugins/openai/helios-mcp/src/http.mjs`
- `plugins/openai/helios-mcp/src/server.mjs`
- `plugins/openai/helios-mcp/src/stdio.mjs`
- `scripts/integrations/validate_tools_plugins_services.py`
- `src/services/HELIOS.IntegrationBroker/Contracts/IntegrationEvent.cs`
- `src/services/HELIOS.IntegrationBroker/Contracts/ToolContracts.cs`
- `src/services/HELIOS.IntegrationBroker/HELIOS.IntegrationBroker.csproj`
- `src/services/HELIOS.IntegrationBroker/Program.cs`
- `src/services/HELIOS.IntegrationBroker/README.md`
- `src/services/HELIOS.IntegrationBroker/Services/CatalogLoader.cs`
- `src/services/HELIOS.IntegrationBroker/Services/InMemoryBrokerStore.cs`
- `src/services/HELIOS.IntegrationBroker/Services/PolicyServices.cs`
- `src/services/HELIOS.IntegrationBroker/appsettings.json`

## origin/codex/find-best-steps-to-set-up-environment-d1gby4

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** high
- **Merge-tree conflicts:** none
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `b6aeb19df73ffe2b21616789986269528e4665b1` — Bootstrap local dotnet and split Python language ownership — Yolkster64 <thepatman64@gmail.com>
- `7a55fb907668924c3427894210d3a4c94512ce32` — Gate unitary AI system report — Yolkster64 <thepatman64@gmail.com>
- `78ef5fc8196123c364904c04b393e2935194855e` — Add language optimization matrix — Yolkster64 <thepatman64@gmail.com>
- `90d2d7bd0fb5ac0df648f027d974acc46e69ac90` — Add mixed language implementation path — Yolkster64 <thepatman64@gmail.com>
- `e4e5e064ed69db32751c94b7fdbf3e1c26a962c9` — Add mixed-language ownership and merge intelligence — Yolkster64 <thepatman64@gmail.com>
- `74307a7faadf8b944fc128958ccbc157f89a315d` — Harden completion automation path — Yolkster64 <thepatman64@gmail.com>
- `5d3a194d0a68c415c8e90753965fb31ea3deebfc` — Add fleet deploy learning and fix center — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-auto-upgrade.yml`
- `.github/workflows/helios-autofix.yml`
- `.github/workflows/helios-mass-integration.yml`
- `.github/workflows/helios-repository-setup.yml`
- `.github/workflows/helios-shell.yml`
- `.gitignore`
- `config/helios-agent-progression.json`
- `config/helios-agent-runtime.json`
- `config/helios-agent-shop.json`
- `config/helios-agents.json`
- `config/helios-auto-upgrade.json`
- `config/helios-capabilities.json`
- `config/helios-copilot-m365.json`
- `config/helios-github-setup.json`
- `config/helios-gui-commands.json`
- `config/helios-hermes-xcore-models.json`
- `config/helios-language-optimization-matrix.json`
- `config/helios-language-ownership.json`
- `config/helios-llm-router.json`
- `config/helios-mass-integration.json`
- `config/helios-ml-models.json`
- `config/helios-model-store.json`
- `config/helios-party-formations.json`
- `config/helios-policy.json`
- `config/helios-specializations.json`
- `config/helios-super-automation-backlog.json`
- `config/helios-unitary-ai-system.json`
- `config/helios-vault.json`
- `docs/DEVELOPMENT_SETUP.md`
- `docs/HELIOS_HERMES_XCORE_WORKING_STEPS.md`
- `scripts/analytics/fsharp_category_report.py`
- `scripts/analytics/fsharp_test_report.py`
- `scripts/automation/agent_runtime_matrix.py`
- `scripts/automation/autoconnect_setup.py`
- `scripts/automation/autofix_loop.py`
- `scripts/automation/code_fix_center.py`
- `scripts/automation/deep_setup_all.py`
- `scripts/automation/final_gate.py`
- `scripts/automation/finish_helios_setup.py`
- `scripts/automation/fix_csharp_compile.py`
- `scripts/automation/gui_runner_bridge.py`
- `scripts/automation/helios_auto_upgrade.py`
- `scripts/automation/helios_store.py`
- `scripts/automation/hermes_xcore_model_setup.py`
- `scripts/automation/language_optimization_matrix.py`
- `scripts/automation/language_ownership_report.py`
- `scripts/automation/language_required_checks.py`
- `scripts/automation/llm_router_plan.py`
- `scripts/automation/model_cost_speed_optimizer.py`
- `scripts/automation/model_store_report.py`
- `scripts/automation/openai_responses_runner.py`
- `scripts/automation/provider_health.py`
- `scripts/automation/python_language_split_report.py`
- `scripts/automation/render_operator_dashboard.py`
- `scripts/automation/specialization_matrix.py`
- `scripts/automation/start_asap.py`
- `scripts/automation/super_automation_backlog.py`
- `scripts/automation/unitary_ai_system.py`
- `scripts/automation/validate_report_contracts.py`
- `scripts/azure/bicep_report.py`
- `scripts/browser/edge_mode_readiness.py`
- `scripts/github/conflict_forecast.py`
- `scripts/github/connect_github.py`
- `scripts/github/github_takeover_status.py`
- `scripts/github/language_aware_score.py`
- `scripts/github/mass_integration.py`
- `scripts/github/merge_decision_pipeline.py`
- `scripts/github/setup_repository.py`
- `scripts/integrations/helios_capability_setup.py`
- `scripts/learning/agent_party.py`
- `scripts/learning/agent_xp.py`
- `scripts/learning/core_ai_learning.py`
- `scripts/learning/fleet_deploy.py`
- `scripts/learning/ml_model_registry.py`
- `scripts/learning/party_formations.py`
- `scripts/learning/record_event.py`
- `scripts/learning/summarize_learning.py`
- `scripts/microsoft365/copilot_m365_readiness.py`
- `scripts/native/benchmark_native.py`
- `scripts/security/automation_audit.py`
- `scripts/security/policy_gate.py`
- `scripts/security/vault_readiness.py`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`
- `src/analytics/HELIOS.Analytics.FSharp/Learning/LearningSummary.fs`
- `src/analytics/HELIOS.Analytics.FSharp/Merge/MergeScoring.fs`
- `src/core/HELIOS.Platform.Cloud/CloudReadinessService.cs`
- `src/core/HELIOS.Platform.Cloud/HELIOS.Platform.Cloud.csproj`
- `src/core/HELIOS.Platform.Contracts/Automation/AutomationReports.cs`
- `src/core/HELIOS.Platform.Orchestration/FinalGateOrchestrator.cs`
- `src/core/HELIOS.Platform.Orchestration/HELIOS.Platform.Orchestration.csproj`
- `src/core/HELIOS.Platform.Orchestration/LanguageOwnershipService.cs`
- `src/core/HELIOS.Platform.Orchestration/ReportContractValidator.cs`
- `src/native/HELIOS.Native.Performance/include/helios/merge_analysis.hpp`
- `src/native/HELIOS.Native.Performance/src/merge_analysis.cpp`
- `src/tools/HELIOS.CSharpFixer/HELIOS.CSharpFixer.csproj`
- `src/tools/HELIOS.CSharpFixer/Program.cs`
- `src/tools/HELIOS.Cli/HELIOS.Cli.csproj`
- `src/tools/HELIOS.Cli/Program.cs`
- `tools/aihub/smoke-test.py`
- `tools/azure/setup-helios-azure-cli.ps1`
- `tools/dotnet/setup-local-dotnet.sh`
- `tools/gui/helios-control-center/README.md`
- `tools/gui/helios-control-center/app.js`
- `tools/gui/helios-control-center/index.html`
- `tools/gui/helios-control-center/styles.css`
- `tools/helios.ps1`

## origin/codex/fix-high-priority-bug-in-ci-workflow

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `e9bed86eeed62be883e51e7ce29fb2198d79d99e` — Document PR 59 merge readiness plan — Yolkster64 <thepatman64@gmail.com>
- `94003a178a8d803cf383a4fbb3bd2492a82a3c9a` — Fix AI review and NuGet CI gates — Yolkster64 <thepatman64@gmail.com>
- `3a41b3b4c784da4a551ec6baadc7b481ebead938` — Keep CI test loop with fixed project reference — Yolkster64 <thepatman64@gmail.com>
- `ce50087749e067eb4baf861cb9937cc74373f98f` — Run only first available HELIOS test project — Yolkster64 <thepatman64@gmail.com>
- `de6c5cc344bc2986e6de1052d80f0cb45f9060a0` — Guard CI test restore against broken references — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ai-code-review.yml`
- `.github/workflows/helios-dotnet-ci.yml`
- `.github/workflows/nuget.yml`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `docs/workflows/pr-59-merge-readiness-plan.md`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/promotion/monado-blade-eaa9c6b

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `aa9b8510edc43f3d51f634eb037034b5d824d6bf` — Document Azure DevOps and GitHub OIDC automation binding — Yolkster64 <thepatman64@gmail.com>
- `d4c07e651ff702af9440929a86a7db8fc45d62e9` — Align GitHub Azure deployment with DevOps WIF pipeline — Yolkster64 <thepatman64@gmail.com>
- `5c31f0a2e624ee3a017f40ed22c1c71e9ce1da16` — Replace placeholder with Azure DevOps WIF deployment pipeline — Yolkster64 <thepatman64@gmail.com>
- `7a8e13220a38855f091c3c0a6717d657b6165412` — Record HELIOS integration promotion for enterprise runtime — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/enterprise-bicep.yml`
- `azure-pipelines.yml`
- `docs/deployment/AZURE_DEVOPS_AND_GITHUB_AUTOMATION.md`
- `promotions/monado-blade/eaa9c6b53a5c8140d6faf635ab3b01ec12c65a4d.json`

## origin/codex/fix-ai-code-review-pipeline-failures

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `5054bc56da835f184f9100bd0d23e66e8ecc5342` — Harden PowerShell workflow validation script — Yolkster64 <thepatman64@gmail.com>
- `ca2f1d1f9af65856ef3bd4ad136c3f982beb25d3` — Fix PowerShell syntax validation heredoc — Yolkster64 <thepatman64@gmail.com>
- `56dbfea654e78a649ce3885187c7cde4904bde06` — Stabilize GitHub Actions pipelines — Yolkster64 <thepatman64@gmail.com>
- `e9bed86eeed62be883e51e7ce29fb2198d79d99e` — Document PR 59 merge readiness plan — Yolkster64 <thepatman64@gmail.com>
- `94003a178a8d803cf383a4fbb3bd2492a82a3c9a` — Fix AI review and NuGet CI gates — Yolkster64 <thepatman64@gmail.com>
- `3a41b3b4c784da4a551ec6baadc7b481ebead938` — Keep CI test loop with fixed project reference — Yolkster64 <thepatman64@gmail.com>
- `ce50087749e067eb4baf861cb9937cc74373f98f` — Run only first available HELIOS test project — Yolkster64 <thepatman64@gmail.com>
- `de6c5cc344bc2986e6de1052d80f0cb45f9060a0` — Guard CI test restore against broken references — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ai-code-review.yml`
- `.github/workflows/analysis.yml`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/build-variant-test.yml`
- `.github/workflows/code-checks.yml`
- `.github/workflows/code-registry-update.yml`
- `.github/workflows/documentation-update.yml`
- `.github/workflows/dotnet-build.yml`
- `.github/workflows/helios-dotnet-ci.yml`
- `.github/workflows/multi-repo-sync.yml`
- `.github/workflows/nuget.yml`
- `.github/workflows/publish-to-packagemanagers.yml`
- `.github/workflows/quality.yml`
- `.github/workflows/status-dashboard.yml`
- `.github/workflows/verify.yml`
- `.github/workflows/wiki-generator.yml`
- `config/partitions/helios-default-layout.json`
- `docs/FEATURE_MATRIX.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `docs/workflows/pr-59-merge-readiness-plan.md`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`

## origin/sre/deployment-automation-reporting-noworkflow

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `79f4f86ba0bc610dfa6cf7aafc7932b1b2b3d22a` — Fix deployment review findings — Yolkster64 <thepatman64@gmail.com>
- `3c17af39caee8bfa3a0de32158e1ee1c1c01ecf7` — [Generated by SRE Agent] Add deployment automation docs and deployment assets — Azure SRE Agent <noreply@microsoft.com>

### Patch-equivalent commits
- None

### Files
- `.env.template`
- `azure-pipelines.yml`
- `deployment/containerapps/hubspot-sync-job.yaml`
- `deployment/logicapps/azure-monitor-to-slack.definition.json`
- `deployment/main.bicep`
- `deployment/manifests/hubspot-sync-cronjob.yaml`
- `deployment/modules/aks.bicep`
- `deployment/modules/container-apps.bicep`
- `deployment/modules/integration-stack.bicep`
- `deployment/parameters/platform.parameters.example.json`
- `docs/DEPLOYMENT.md`
- `docs/workflows/WORKFLOW_DEPLOY.md`
- `scripts/deploy/deploy-platform.sh`

## origin/yolkster64-bookish-journey

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `d1d6b9e22b209a573197bc3c257dc59837b50f0d` — Harden setup artifacts and existing-field compatibility — Copilot App <223556219+Copilot@users.noreply.github.com>
- `167c555472460b6bf916295a33170a76e6a4b707` — fix: unblock CI restores and Windows build entrypoint — Copilot App <223556219+Copilot@users.noreply.github.com>
- `a7030bfa96899f955a1789de8cb48754599edf9f` — fix: refresh HELIOS.Platform lock file for locked-mode restore — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c18f44b816dd9d93020d4fe12c467064a2b25d25` — fix: stabilize HELIOS setup orchestration — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.automation/rule-critical-escalate.json`
- `.automation/rule-pr-merge.json`
- `.automation/rule-priority-assign.json`
- `.automation/rule-qa-deploy.json`
- `.fields/approvalrequired.json`
- `.fields/architecturedecision.json`
- `.fields/assignedto.json`
- `.fields/blockedby.json`
- `.fields/compliancecheck.json`
- `.fields/component.json`
- `.fields/datamigration.json`
- `.fields/dependson.json`
- `.fields/deploymentenvironment.json`
- `.fields/deploymentstatus.json`
- `.fields/documentation.json`
- `.fields/duedate.json`
- `.fields/effort.json`
- `.fields/integrationpoints.json`
- `.fields/performanceimpact.json`
- `.fields/priority.json`
- `.fields/progressstatus.json`
- `.fields/qastatus.json`
- `.fields/reviewedby.json`
- `.fields/reviewstatus.json`
- `.fields/risklevel.json`
- `.fields/sprint.json`
- `.fields/successmetrics.json`
- `.fields/timeestimate.json`
- `.fields/userimpact.json`
- `.github/integrations/issue-link-config.json`
- `.github/integrations/notification-config.json`
- `.github/integrations/pages-sync-config.json`
- `.github/integrations/pr-workflow-config.json`
- `.github/integrations/workflow-status-config.json`
- `.monitoring/alerts-config.json`
- `.monitoring/dashboard-config.json`
- `.monitoring/health-checks-config.json`
- `.monitoring/metrics-config.json`
- `.monitoring/reporting-config.json`
- `.views/view-critical.json`
- `.views/view-deployment.json`
- `.views/view-priority.json`
- `.views/view-review.json`
- `.views/view-sprint.json`
- `.views/view-workload.json`
- `scripts/board-setup/setup-automation-rules.ps1`
- `scripts/board-setup/setup-board.ps1`
- `scripts/board-setup/setup-custom-fields.ps1`
- `scripts/board-setup/setup-templates.ps1`
- `scripts/board-setup/setup-views.ps1`
- `scripts/board-setup/validate-board.ps1`
- `scripts/integration/setup-github-ecosystem.ps1`
- `scripts/optimization/setup-monitoring.ps1`
- `scripts/setup/complete-system-setup.ps1`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/UserAccountManagementTests.cs`
- `src/core/HELIOS.Platform/packages.lock.json`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `templates/phase_Phase_1.json`
- `templates/phase_Phase_2.json`
- `templates/phase_Phase_3.json`
- `templates/phase_Phase_4.json`
- `templates/phase_Phase_5.json`
- `templates/phase_Phase_6.json`
- `templates/phase_Phase_7.json`
- `templates/phase_Phase_8.json`

## origin/yolkster64-supreme-guacamole

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `e15eaf51f660c64209115262bde5a951431b5088` — Address review feedback and unblock Windows CI — Copilot App <223556219+Copilot@users.noreply.github.com>
- `780d37a991b550c195947c80c9581e70a096b053` — Update HELIOS.Platform lock file for locked restore — Copilot App <223556219+Copilot@users.noreply.github.com>
- `efe9eeedc7bfc5806da414e9d122f7d413e3018c` — Stabilize board setup scripts and persist setup artifacts — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.automation/rule-critical-escalate.json`
- `.automation/rule-pr-merge.json`
- `.automation/rule-priority-assign.json`
- `.automation/rule-qa-deploy.json`
- `.fields/approvalrequired.json`
- `.fields/architecturedecision.json`
- `.fields/assignedto.json`
- `.fields/blockedby.json`
- `.fields/compliancecheck.json`
- `.fields/component.json`
- `.fields/datamigration.json`
- `.fields/dependson.json`
- `.fields/deploymentenvironment.json`
- `.fields/deploymentstatus.json`
- `.fields/documentation.json`
- `.fields/duedate.json`
- `.fields/effort.json`
- `.fields/integrationpoints.json`
- `.fields/performanceimpact.json`
- `.fields/priority.json`
- `.fields/progressstatus.json`
- `.fields/qastatus.json`
- `.fields/reviewedby.json`
- `.fields/reviewstatus.json`
- `.fields/risklevel.json`
- `.fields/sprint.json`
- `.fields/successmetrics.json`
- `.fields/timeestimate.json`
- `.fields/userimpact.json`
- `.github/integrations/issue-link-config.json`
- `.github/integrations/notification-config.json`
- `.github/integrations/pages-sync-config.json`
- `.github/integrations/pr-workflow-config.json`
- `.github/integrations/workflow-status-config.json`
- `.monitoring/alerts-config.json`
- `.monitoring/dashboard-config.json`
- `.monitoring/health-checks-config.json`
- `.monitoring/metrics-config.json`
- `.monitoring/reporting-config.json`
- `.views/view-critical.json`
- `.views/view-deployment.json`
- `.views/view-priority.json`
- `.views/view-review.json`
- `.views/view-sprint.json`
- `.views/view-workload.json`
- `scripts/board-setup/setup-automation-rules.ps1`
- `scripts/board-setup/setup-board.ps1`
- `scripts/board-setup/setup-custom-fields.ps1`
- `scripts/board-setup/setup-templates.ps1`
- `scripts/board-setup/setup-views.ps1`
- `scripts/board-setup/validate-board.ps1`
- `scripts/integration/setup-github-ecosystem.ps1`
- `scripts/optimization/setup-monitoring.ps1`
- `scripts/setup/complete-system-setup.ps1`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoEngineUpdateService.cs`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/UserAccountManagementTests.cs`
- `src/core/HELIOS.Platform/packages.lock.json`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `templates/phase_Phase_1.json`
- `templates/phase_Phase_2.json`
- `templates/phase_Phase_3.json`
- `templates/phase_Phase_4.json`
- `templates/phase_Phase_5.json`
- `templates/phase_Phase_6.json`
- `templates/phase_Phase_7.json`
- `templates/phase_Phase_8.json`

## origin/integration/helios-chatgpt-copilot-app

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** high
- **Merge-tree conflicts:** none
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `3e447b28f98eae373056d5b488b10f6cc71de78a` — fix(ci): unblock portable runtime restore and skip PR package publish — Copilot App <223556219+Copilot@users.noreply.github.com>
- `9009ae3ce5baa335f36e43f961ea7c055f31a31a` — merge(main): resolve workflow conflict and keep CI hardening — Copilot App <223556219+Copilot@users.noreply.github.com>
- `60a984a9fdfed3c1a5044bf9ae79cb9522efaf76` — fix(ci): tighten secret-scan token boundaries — Copilot App <223556219+Copilot@users.noreply.github.com>
- `c02067f7c205756089277bda6a4045c417c64f7b` — fix(ci): unblock workflow and contract validation failures — Copilot App <223556219+Copilot@users.noreply.github.com>
- `93d3147235c5a6952994b93e702bf945582fd424` — fix(security): patch vulnerable npm transitive dependencies — Copilot App <223556219+Copilot@users.noreply.github.com>
- `17e1467633b276ec00b146691e2d508906010590` — merge(main): resolve conflicts and keep CI fixes — Copilot App <223556219+Copilot@users.noreply.github.com>
- `faa29287076980a57cc1535bf469d7b147c0a39d` — fix(ci): stabilize failing PR checks — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d78aa8a6bab6499fa26637689959b29a96d1cad9` — Add full setup inventory command for HELIOS control fabric (#236) — Yolkster64 <thepatman64@gmail.com>
- `3e50a9cc5f779feb0904c3f9889db39f80b8c8ed` — Merge origin/main into integration/helios-chatgpt-copilot-app — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `7cbb0677f150f3250115d0a6bc6b98131013422e` — Potential fix for pull request finding 'Missed opportunity to use Where' — Yolkster64 <thepatman64@gmail.com>
- `f3290ad2ddd6e54ab6a5284c46cabb335a09ecb9` — Potential fix for pull request finding 'Missing Dispose call on local IDisposable' — Yolkster64 <thepatman64@gmail.com>
- `fbdd1bada8a1bb8b4f244c4a196d9553fe586abf` — Potential fix for pull request finding 'Poor error handling: empty catch block' — Yolkster64 <thepatman64@gmail.com>
- `471f1e07d839daab27d3adbdb0cc01afe67bff2c` — Potential fix for pull request finding 'Generic catch clause' — Yolkster64 <thepatman64@gmail.com>
- `642297079db21fd2bab26e561d7877b644bd1ec0` — Discover exact component manifest directories — Yolkster64 <thepatman64@gmail.com>
- `dc1f5c08944101de619e0cd77cdee00be307332f` — Fix path-safe component CI outputs — Yolkster64 <thepatman64@gmail.com>
- `f72dc9a95c55657980809ca6cc82bdee7e6338ee` — Complete Bicep module references and identities — Yolkster64 <thepatman64@gmail.com>
- `5963b7f63aa899f046cf62aa157d52238fea4d9d` — Import xUnit test attributes — Yolkster64 <thepatman64@gmail.com>
- `a68f43b6bb1ee18ac79e5bda20837dcbe08c4f5a` — Fix net8 HMAC hex parsing — Yolkster64 <thepatman64@gmail.com>
- `9abbcb0a4fc54397a9fc374aee0e2cdd05cb23dc` — Make HELIOS automation fabric buildable — Yolkster64 <thepatman64@gmail.com>
- `bd1120c57af7c19eb8687d22e9124e4e692e15dd` — Add review-only HELIOS automation fabric reference — Yolkster64 <thepatman64@gmail.com>
- `1993d8774b1c6aa86851f7e0d6cf94de7d4cb5d9` — Validate every enterprise setup operation — Yolkster64 <thepatman64@gmail.com>
- `5aa8ad65186e4b866cce7925b0d017c4eeecb2ff` — Keep Slack and Linear OAuth in MCP config without breaking setup phases — Yolkster64 <thepatman64@gmail.com>
- `4f5028aa2600c77e7dec9c358730de96fa8851bb` — Add Slack and Linear authentication to enterprise setup — Yolkster64 <thepatman64@gmail.com>
- `4ba2aee70e2b4c024d2f066f0fb8ed709090f106` — Use current Linear and Slack MCP endpoints — Yolkster64 <thepatman64@gmail.com>
- `77e02c8014be22b3f9da8d6ee8910cd53aa06855` — Add tests for enterprise setup controller — Yolkster64 <thepatman64@gmail.com>
- `2dba9bebd66e3d447f393968c5b8d67b12d6107a` — Add guarded enterprise setup controller to Azure CLI plugin — Yolkster64 <thepatman64@gmail.com>
- `861d2f8b30df89cbf1338e736875a35676b99aef` — Add guarded enterprise setup operation manifest — Yolkster64 <thepatman64@gmail.com>
- `38107738ca70d3483061bedc50c762a181a8ddc6` — Add Agent 365 Microsoft 365 tooling manifest — Yolkster64 <thepatman64@gmail.com>
- `49a09e8c2cdfda723c55b6e9ceb8b7608e90eb3c` — Add Agent 365 configuration template — Yolkster64 <thepatman64@gmail.com>
- `a236482038ec9b68a3131c487bbfe6b8a941d9ea` — Add Claude Code MCP configuration to shared control app — Yolkster64 <thepatman64@gmail.com>
- `a72aa66caec61f7368d05a2140d6eaff184b0be1` — Add Claude Code instructions to shared control app — Yolkster64 <thepatman64@gmail.com>
- `6429f1f24fa33f888d38185636e8ce4699e45efa` — fix(azure-cli): harden app lookup and parameter validation — Yolkster64 <thepatman64@gmail.com>
- `44085ed63a4f9f9096d174b09df4c1a9263ace43` — feat(azure): add guarded HELIOS CLI plugin and canonical coordination targets — Yolkster64 <thepatman64@gmail.com>
- `491578746496ba2dae786cfde025cbb6c55c93b5` — style(control-app): terminate safe link append explicitly — Yolkster64 <thepatman64@gmail.com>
- `8855ef4d39553b95db64d94761fa801a802486e4` — fix(control-app): harden widget rendering in .github/workflows/helios-control-app.yml — Yolkster64 <thepatman64@gmail.com>
- `90d7aaacb1b727c95f7ece389ac3731611dcd878` — fix(control-app): harden widget rendering in apps/helios-control/scripts/smoke.mjs — Yolkster64 <thepatman64@gmail.com>
- `ec1f6ff3a9bce3f5c4f5429fa90832919abce19f` — fix(control-app): harden widget rendering in apps/helios-control/public/control-center.html — Yolkster64 <thepatman64@gmail.com>
- `2949004f445d5d85c7548e2f3921973b26de203c` — feat(control-app): add .github/workflows/helios-control-app.yml — Yolkster64 <thepatman64@gmail.com>
- `3aaa4522a2de1846daaf328afd474aa0b5e478e5` — feat(control-app): add apps/helios-control/tsconfig.json — Yolkster64 <thepatman64@gmail.com>
- `9ec5b5fa626e9b5ba44ed96efe170b86bfb2a1d9` — feat(control-app): add apps/helios-control/src/server.ts — Yolkster64 <thepatman64@gmail.com>
- `85c51e1da3d09fcf58339dce0e2961746deb545c` — feat(control-app): add apps/helios-control/scripts/smoke.mjs — Yolkster64 <thepatman64@gmail.com>
- `e2c1e139cdbaead5619f49b9c354bd91189e656c` — feat(control-app): add apps/helios-control/public/control-center.html — Yolkster64 <thepatman64@gmail.com>
- `f5cb40a300f4a771391fb7e517ca3e958bed61f6` — feat(control-app): add apps/helios-control/package.json — Yolkster64 <thepatman64@gmail.com>
- `ec291a61ae120a9aeadf0e25c1440e0ba95fbfe0` — feat(control-app): add apps/helios-control/package-lock.json — Yolkster64 <thepatman64@gmail.com>
- `c63f09e027e35ac1bdb40a2a0a4ffb2365fe73ea` — feat(control-app): add apps/helios-control/copilot/helios-mcp.openapi.yaml — Yolkster64 <thepatman64@gmail.com>
- `93d93d9739d43beedd72e6554f337f66dbcf3096` — feat(control-app): add apps/helios-control/README.md — Yolkster64 <thepatman64@gmail.com>
- `eb05da0b8a68d6bc52b5e5dcf54ab42d033cd654` — feat(control-app): add apps/helios-control/Dockerfile — Yolkster64 <thepatman64@gmail.com>
- `bc67d4094dc4ea867c0d9132ba7d6a3cdfb00940` — feat(control-app): add apps/helios-control/.env.example — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.agents/plugins/marketplace.json`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/code-checks.yml`
- `.github/workflows/component-version-check.yml`
- `.github/workflows/helios-control-app.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/helios-unified-plugin-validate.yml`
- `.github/workflows/nuget.yml`
- `.github/workflows/unified-agent-contract.yml`
- `.github/workflows/validate-monadoblade-profile-contracts.yml`
- `apps/helios-control/.env.example`
- `apps/helios-control/.mcp.json`
- `apps/helios-control/CLAUDE.md`
- `apps/helios-control/Dockerfile`
- `apps/helios-control/README.md`
- `apps/helios-control/copilot/agent365/ToolingManifest.json`
- `apps/helios-control/copilot/agent365/a365.config.template.json`
- `apps/helios-control/copilot/helios-mcp.openapi.yaml`
- `apps/helios-control/package-lock.json`
- `apps/helios-control/package.json`
- `apps/helios-control/public/control-center.html`
- `apps/helios-control/scripts/smoke.mjs`
- `apps/helios-control/src/server.ts`
- `apps/helios-control/tsconfig.json`
- `eng/test/validate_test_ownership.py`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `plugins/helios-azure-cli/.codex-plugin/plugin.json`
- `plugins/helios-azure-cli/README.md`
- `plugins/helios-azure-cli/SKILL.md`
- `plugins/helios-azure-cli/assets/enterprise-setup.json`
- `plugins/helios-azure-cli/assets/helios-targets.json`
- `plugins/helios-azure-cli/scripts/helios-azure.ps1`
- `plugins/helios-azure-cli/scripts/helios-azure.sh`
- `plugins/helios-azure-cli/scripts/helios_azure.py`
- `plugins/helios-azure-cli/scripts/helios_enterprise.py`
- `plugins/helios-azure-cli/scripts/test_helios_azure.py`
- `plugins/helios-azure-cli/scripts/test_helios_enterprise.py`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/test_helios.py`
- `plugins/helios-control-fabric/skills/helios-control/SKILL.md`
- `reference/helios-enterprise-automation-fabric/.Register-HeliosTeamsWorkflow.ps1.nqnd05jk`
- `reference/helios-enterprise-automation-fabric/FabricWorker.cs`
- `reference/helios-enterprise-automation-fabric/GitHubControlSink.cs`
- `reference/helios-enterprise-automation-fabric/HELIOS.Fabric.sln`
- `reference/helios-enterprise-automation-fabric/Initialize-HeliosAzureOidc.ps1`
- `reference/helios-enterprise-automation-fabric/Initialize-HeliosPowerPlatform.ps1`
- `reference/helios-enterprise-automation-fabric/Initialize-HeliosSharePoint.ps1`
- `reference/helios-enterprise-automation-fabric/LinearSink.cs`
- `reference/helios-enterprise-automation-fabric/Program.cs`
- `reference/helios-enterprise-automation-fabric/README.md`
- `reference/helios-enterprise-automation-fabric/RECOVERY_STATUS.md`
- `reference/helios-enterprise-automation-fabric/RUN_THIS_FIRST.md`
- `reference/helios-enterprise-automation-fabric/Register-HeliosTeamsWorkflow.ps1`
- `reference/helios-enterprise-automation-fabric/SharePointEvidenceSink.cs`
- `reference/helios-enterprise-automation-fabric/SlackSink.cs`
- `reference/helios-enterprise-automation-fabric/TeamsSink.cs`
- `reference/helios-enterprise-automation-fabric/WebhookEndpoints.cs`
- `reference/helios-enterprise-automation-fabric/action-pin-plan.yml`
- `reference/helios-enterprise-automation-fabric/action-policy.json`
- `reference/helios-enterprise-automation-fabric/apiDefinition.swagger.json`
- `reference/helios-enterprise-automation-fabric/apiProperties.json`
- `reference/helios-enterprise-automation-fabric/app-manifest.yaml`
- `reference/helios-enterprise-automation-fabric/approval-policy.json`
- `reference/helios-enterprise-automation-fabric/automations.json`
- `reference/helios-enterprise-automation-fabric/azure-deploy.yml`
- `reference/helios-enterprise-automation-fabric/azure-plan.yml`
- `reference/helios-enterprise-automation-fabric/azure.yaml`
- `reference/helios-enterprise-automation-fabric/broker-images.yml`
- `reference/helios-enterprise-automation-fabric/channel-bindings.example.json`
- `reference/helios-enterprise-automation-fabric/channels.json`
- `reference/helios-enterprise-automation-fabric/config-drift.yml`
- `reference/helios-enterprise-automation-fabric/connector-readiness.yml`
- `reference/helios-enterprise-automation-fabric/connector-registry.json`
- `reference/helios-enterprise-automation-fabric/containerapps.bicep`
- `reference/helios-enterprise-automation-fabric/control-plane-operator.yml`
- `reference/helios-enterprise-automation-fabric/deployment-approval.json`
- `reference/helios-enterprise-automation-fabric/emergency-quarantine.yml`
- `reference/helios-enterprise-automation-fabric/event-envelope.schema.json`
- `reference/helios-enterprise-automation-fabric/evidence-mapping.json`
- `reference/helios-enterprise-automation-fabric/execution-plan.json`
- `reference/helios-enterprise-automation-fabric/fabric-ci.yml`
- `reference/helios-enterprise-automation-fabric/graph-permission-plan.json`
- `reference/helios-enterprise-automation-fabric/incident-critical.json`
- `reference/helios-enterprise-automation-fabric/information-architecture.json`
- `reference/helios-enterprise-automation-fabric/issue-templates.json`
- `reference/helios-enterprise-automation-fabric/linear_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/lists-and-libraries.json`
- `reference/helios-enterprise-automation-fabric/main.bicep`
- `reference/helios-enterprise-automation-fabric/nightly-health.yml`
- `reference/helios-enterprise-automation-fabric/pin_actions.py`
- `reference/helios-enterprise-automation-fabric/pr-linear-sync.yml`
- `reference/helios-enterprise-automation-fabric/project/.dockerignore`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/action-pin-plan.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/azure-deploy.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/azure-plan.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/broker-images.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/config-drift.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/connector-readiness.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/control-plane-operator.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/emergency-quarantine.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/fabric-ci.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/nightly-health.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/pr-linear-sync.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/promote-reviewed-sha.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/release-evidence.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/reusable-build-event.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/sharepoint-governance-sync.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/workflow-outcome-router.yml`
- `reference/helios-enterprise-automation-fabric/project/.gitignore`
- `reference/helios-enterprise-automation-fabric/project/README.md`
- `reference/helios-enterprise-automation-fabric/project/RUN_THIS_FIRST.md`
- `reference/helios-enterprise-automation-fabric/project/azure.yaml`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/approval-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/connector-registry.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/deployment-approval.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/event-envelope.schema.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/evidence-mapping.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/execution-plan.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/routing-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/status-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/github/action-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/linear/automations.json`
- `reference/helios-enterprise-automation-fabric/project/config/linear/issue-templates.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/information-architecture.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/lists-and-libraries.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/retention-labels.example.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/sharepoint-sync-manifest.json`
- `reference/helios-enterprise-automation-fabric/project/config/slack/app-manifest.yaml`
- `reference/helios-enterprise-automation-fabric/project/config/slack/channel-bindings.example.json`
- `reference/helios-enterprise-automation-fabric/project/config/slack/channels.json`
- `reference/helios-enterprise-automation-fabric/project/config/teams/workflow-contracts(1).json`
- `reference/helios-enterprise-automation-fabric/project/config/teams/workflow-contracts.json`
- `reference/helios-enterprise-automation-fabric/project/docker/broker.Dockerfile`
- `reference/helios-enterprise-automation-fabric/project/docker/worker.Dockerfile`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/containerapps.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/main.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/modules/foundation.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/modules/private-endpoint.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/parameters/dev.bicepparam`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/parameters/prod.bicepparam`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/parameters/stage.bicepparam`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/servicebus.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/storage.bicep`
- `reference/helios-enterprise-automation-fabric/project/power-platform/connector/apiDefinition.swagger.json`
- `reference/helios-enterprise-automation-fabric/project/power-platform/connector/apiProperties.json`
- `reference/helios-enterprise-automation-fabric/project/pyproject.toml`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Initialize-HeliosAzureOidc.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Initialize-HeliosPowerPlatform.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Initialize-HeliosSharePoint.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Install-HeliosFabricOverlay.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Register-HeliosTeamsWorkflow.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/linear_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/slack_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/connectors/sharepoint_sync.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/build_event.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/canonicalize_plan.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/control_plane_dispatch.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/pin_actions.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/publish_event.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/governance/pin_actions.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/governance/sharepoint_sync.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/validation/Validate-PowerShell.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/validation/assert_no_secret_values.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/validation/validate_bicep.sh`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/BrokerServices.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/HELIOS.Fabric.Broker.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/Program.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/WebhookEndpoints.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Contracts/FabricContracts.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Contracts/HELIOS.Fabric.Contracts.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Tests/FabricTests.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Tests/HELIOS.Fabric.Tests.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/FabricWorker.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/GitHubControlSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/HELIOS.Fabric.Worker.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/LinearSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/SharePointEvidenceSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/SlackSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/TeamsSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/WorkerProgram.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/WorkerServices.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.sln`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric/__init__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric/__main__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric/cli.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric_cli/__init__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric_cli/__main__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric_cli/cli.py`
- `reference/helios-enterprise-automation-fabric/project/tests/fixtures/deployment-plan.json`
- `reference/helios-enterprise-automation-fabric/project/tests/test_fabricctl.py`
- `reference/helios-enterprise-automation-fabric/promote-reviewed-sha.yml`
- `reference/helios-enterprise-automation-fabric/release-evidence.yml`
- `reference/helios-enterprise-automation-fabric/retention-labels.example.json`
- `reference/helios-enterprise-automation-fabric/reusable-build-event.yml`
- `reference/helios-enterprise-automation-fabric/routing-policy.json`
- `reference/helios-enterprise-automation-fabric/servicebus.bicep`
- `reference/helios-enterprise-automation-fabric/sharepoint-governance-sync.yml`
- `reference/helios-enterprise-automation-fabric/sharepoint-sync-manifest.json`
- `reference/helios-enterprise-automation-fabric/sharepoint_sync.py`
- `reference/helios-enterprise-automation-fabric/slack_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/status-policy.json`
- `reference/helios-enterprise-automation-fabric/storage.bicep`
- `reference/helios-enterprise-automation-fabric/sync-map.json`
- `reference/helios-enterprise-automation-fabric/webhook-config.example.json`
- `reference/helios-enterprise-automation-fabric/workflow-contracts(1).json`
- `reference/helios-enterprise-automation-fabric/workflow-contracts.json`
- `reference/helios-enterprise-automation-fabric/workflow-outcome-router.yml`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/Phase10/AIOrchestration/Tests/AIOrchestrationTests.cs`
- `src/core/HELIOS.Platform/Phase10/Users/Tests/UserAccountManagementTests.cs`
- `src/core/HELIOS.Platform/Tests/AI/MLIntegrationTests.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoBladeSixProfileDeliveryFabricV3ContractsTests.cs`
- `tests/contracts/HELIOS.Platform.Contracts.Tests/MonadoEnterpriseExperienceV2ContractsTests.cs`

## origin/codex/document-git-commit-best-practices

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `317cfacc4588d2d0772dc92a1fff41dbb0857682` — Add repository topology validation — Yolkster64 <thepatman64@gmail.com>
- `be4fa1ff6a5c6db22d55f4ed4bebf6854d142320` — Merge remote-tracking branch 'upstream/codex/update-ai-model-configuration-and-routing' into integration/helios-hermes-xcore — Yolkster64 <thepatman64@gmail.com>
- `f039bf74dd5323139762652e26ad0600429d621b` — Improve smart remote inference — Yolkster64 <thepatman64@gmail.com>
- `b19e276e785cc1022eb9efeb812983c2c2514a25` — Add automatic remote setup and push support — Yolkster64 <thepatman64@gmail.com>
- `fee4817554521a79416b1bdd4a838abb6c940c0c` — Add testable specialist environment checker — Yolkster64 <thepatman64@gmail.com>
- `8442180bc2c4c06d1424e6cc273433cb6705194f` — Add HELIOS HERMES integration readiness tooling — Yolkster64 <thepatman64@gmail.com>
- `85ca7217748bbfa38ed7eb16bbbb0415cc2a3d7c` — Add generated artifact pruning script — Yolkster64 <thepatman64@gmail.com>
- `5fd860de6bff47af4489c8d3e9b74f1290002291` — Add repository optimization audit setup — Yolkster64 <thepatman64@gmail.com>
- `e6bbd1671c2cf805cb017b63eb195223ccbf8de4` — Fix AI service config and Codex chat requests — Yolkster64 <thepatman64@gmail.com>
- `6e9d72eba73a4568575da2aaf0d3cc1ef908a890` — Update AI service model routing validation — Yolkster64 <thepatman64@gmail.com>
- `b7e911e4bd9277066965cd649b9dbd5a7ccccd31` — Connect AIHub fleet to AI abstractions — Yolkster64 <thepatman64@gmail.com>
- `ef9f8a683a6fe91a210c90dfcc0b29a13fa0b929` — Add local AIHub provider foundation — Yolkster64 <thepatman64@gmail.com>
- `7d0294171811a14a0f7f344c6e390763768926bf` — Restrict Azure OIDC validation to trusted runs — Yolkster64 <thepatman64@gmail.com>
- `65cfd27c3d551dd64625d013133a95a5e00d005c` — Add Azure CLI setup commands — Yolkster64 <thepatman64@gmail.com>
- `7264dfa5846e28401405b8aaeb5b4d7139bdd11d` — Harden CI project reference validation — Yolkster64 <thepatman64@gmail.com>
- `a206a1c08786a7e6d6e3c18651d537cd19f5b063` — Fix CI project reference handling — Yolkster64 <thepatman64@gmail.com>
- `2fa2c79956165d73ee993322e715a10e8db5386a` — Use portable git ref inventory — Yolkster64 <thepatman64@gmail.com>
- `45b27c6a2113ebb25de198458de80d1ca7751e67` — Make portable src tests pass — Yolkster64 <thepatman64@gmail.com>
- `5ddaea39f020ebd5ab12873e4fdb1fd2b11b0151` — Skip uncompilable portable CI builds — Yolkster64 <thepatman64@gmail.com>
- `03d4682ec6fcc2f1e3ee3da753fdfe36225ba193` — Fix portable CI exclusions — Yolkster64 <thepatman64@gmail.com>
- `bd3abc4c31b6cc54845d0d39af1508a4e8c5472b` — Restrict Azure login outside PR runs — Yolkster64 <thepatman64@gmail.com>
- `2b43b38117fc055b285e4f532ed06393bdd36f02` — Fix review-blocking project globs — Yolkster64 <thepatman64@gmail.com>
- `79ca2afd03b2b523eb751b03e4d54004f85af724` — Redact credentials from automation remote inventory — Yolkster64 <thepatman64@gmail.com>
- `0d6f2632f97759d0e0e22639f230b685e5259874` — Add deep AI automation orchestrator — Yolkster64 <thepatman64@gmail.com>
- `6b3c13b860c6f36b135c12be85838abcbfbf8f16` — Add automatic AI automation integrator — Yolkster64 <thepatman64@gmail.com>
- `c5325943f22de71e9afaf4d0266a44b46007bb6b` — Fix portable CI test project dependencies — Yolkster64 <thepatman64@gmail.com>
- `03ea9e45e6ccfef9cf44649d625a130d3e83dd17` — Guard CI test restore against broken references — Yolkster64 <thepatman64@gmail.com>
- `7b60554f3f3da5dc7682cff1f6fce704ed226c0b` — Fix legacy test project reference — Yolkster64 <thepatman64@gmail.com>
- `75bdc01c7f663996e5652659c35ffccb43f9f270` — Add security-first AIHub router — Yolkster64 <thepatman64@gmail.com>
- `a17f79b33b5ec4c6c6dc28db6c56bab9f827af2b` — Add AIHub abstractions — Yolkster64 <thepatman64@gmail.com>
- `a27f4fa37fafa4c8f60f11f7958903f6fb2ee129` — Add AIHub C# project skeleton — Yolkster64 <thepatman64@gmail.com>
- `553b0f12116a8eda7792e92621d2d290d6514b4e` — Add local X-Tier artifact integration map — Yolkster64 <thepatman64@gmail.com>
- `7d1ceda7c7e212a81cbff6a3a9418e205acca86f` — Add HELIOS partition layout manifest — Yolkster64 <thepatman64@gmail.com>
- `6b1b3717cead4dad022284c75a2d02dd4443a3c5` — Add HELIOS .NET CI workflow — Yolkster64 <thepatman64@gmail.com>
- `d677808e4a83e6f0801a3bc3e2e38d420ae99c32` — Add Microsoft C# Codespace setup guide — Yolkster64 <thepatman64@gmail.com>
- `1e0dad7a1b5912f5a72ed8b887fecb7f859ba51c` — Add HELIOS feature matrix and consolidation inventory — Yolkster64 <thepatman64@gmail.com>
- `c90a944066d745a29c0beb37ca2375eb729ae999` — Phase 2 Build Fixes - Core Infrastructure Compiling — Copilot <copilot@github.com>
- `849d6f22ee9ff4d2dcb78655c7f1fd3fbb78735c` — [CONTINUOUS WORK] PHASE 2 LAUNCH - 5 Parallel Optimization Streams — Copilot <copilot@github.com>
- `1121ce8062a48e18bdf10a75a9226d78d3b6229b` — final: hour 5-6 autonomous work completion verification — Copilot <copilot@github.com>
- `81e6230b0b328be7bde9ca1c00db257afe5714e5` — [AUTONOMOUS HOUR:10-12] FINAL RELEASE - v3.6.1-COMPLETE PRODUCTION READY — Copilot <copilot@github.com>
- `fd268ae982c1f5e16011ba296944c2cc97f217c3` — docs: hour 5-6 completion report for async/await optimization — Copilot <copilot@github.com>
- `8d3a7dc832d31387c4f93bdaabd7ae9163a75375` — [AUTONOMOUS HOUR:9-10] Final Release Preparation - v3.6.1-COMPLETE Ready — Copilot <copilot@github.com>
- `eb48e9c690976810424c855a968d4664dc394664` — docs: Add Autonomous Hour 6-8 completion summary — Copilot <copilot@github.com>
- `7039e9c8551a448872db9a34d0d503ccb4469098` — [AUTONOMOUS HOUR:6-7] Documentation: Performance Optimization Guide + Hour 7-12 Plan — Copilot <copilot@github.com>
- `0c28f51c3743db9cf7b4be31d7f0c906a3992e3c` — feat: Add comprehensive performance benchmark suite for Hour 8 autonomous optimization — Copilot <copilot@github.com>
- `3e3ab9a0ed9ed045d52f6ac66fdc4a1c47a77cd4` — [AUTONOMOUS HOUR:5-6] opt-004: Async Pipeline Pattern Implementation — Copilot <copilot@github.com>
- `c9ce29dfe936e78c82ce7c3c6327d48b58f96abc` — [AUTONOMOUS HOUR:2-4] opt-002: Object Pooling + opt-003: Database Connection Pooling complete — Copilot <copilot@github.com>
- `4387e20aa24262bba354a3eb6dbed6f36ef97b72` — [AUTONOMOUS HOUR:1-2] opt-001: Task Batch Accumulator implementation complete — Copilot <copilot@github.com>
- `c40b197d029104854420234717e59fbcf679f217` — ✅ PHASE 5 COMPLETE - Visual Polish & Animations (30 hours) — Copilot <copilot@github.com>
- `02a85b0167b00b12536fb4108353736f5804a56d` — 🌟 PHASE 5 EXECUTION - Visual Polish & Animations (25 of 30 hours) — Copilot <copilot@github.com>
- `acff888f6e6f29ec3aff61ba76e2d4d17a25d562` — ✅ PHASE 4 COMPLETE - XAML Styling & Data Binding (35 hours) — Copilot <copilot@github.com>
- `3bec4d32a36d0fc0be4b029a2c74680bd0e7b687` — 🎨 PHASE 4 EXECUTION STARTED - XAML Styling & Data Binding Foundation — Copilot <copilot@github.com>
- `afad0cd70d9cacede7b420b9eda93b415bf7dc94` — 🎯 v3.4.0 PHASES 4-7 EXECUTION PLAN READY - 100 Hours Remaining — Copilot <copilot@github.com>
- `443e20cf61c7421fd3b61c44a24987e805598c65` — 📊 v3.4.0 PHASES 1-3 DELIVERY COMPLETE - 125+ Hours Delivered — Copilot <copilot@github.com>
- `0b87c5ac48028378c4d2ebdbefa916b09abc31ce` — feat(v3.4.0): Phase 3 complete - Advanced Components & Dashboard UI — Copilot <copilot@github.com>
- `3d9975cb52a57201f9b27ce1da6885ab97a5ba80` — feat(v3.4.0): Phase 2 complete - Core Component Library and Progress Tracking — Copilot <copilot@github.com>
- `f08bfcabac7fe15dbcdc0fc4f62dd42b629d3b28` — feat(v3.4.0): Add foundation systems - Design, Animations, Dashboard, System Integration — Copilot <copilot@github.com>
- `cb64f733ce26cba64729a6b425f7f3aa720cf3c5` — 🚀 BEGIN v3.4.0 - Next Generation UI/UX and System Integration — Copilot <copilot@github.com>
- `b5edf1ec45d1ca04da5b0bf5f9dc17f5e852fd44` — refactor: Move Week2AsyncBatching reference implementation to docs — Copilot <copilot@github.com>
- `4966718db39442685bc36a62151815ec1c25c7a6` — 📖 Add comprehensive campaign guide for all stakeholders — Copilot <copilot@github.com>
- `f81532906db407fa7b16c0abe6da1924b6463b2f` — 📋 EXECUTIVE HANDOFF - Phase 3 Ready for Product Leadership — Copilot <copilot@github.com>
- `8fa17070f3c3001f436db24c83e799feccebc548` — 🎉 FINAL DEPLOYMENT STATUS - Monado Blade v3.3.0+ Ready for Execution — Copilot <copilot@github.com>
- `24db8183a72104d901dafae2d989d4f0fa93682b` — Add Phase 3 Week 2 implementation guide and Weeks 3-6 execution matrix — Copilot <copilot@github.com>
- `c8df4902e000c96c84bfa968f2c7a49e71815dd9` — 🎉 CAMPAIGN FINAL SUMMARY - Monado Blade v3.3.0+ Optimization Complete — Copilot <copilot@github.com>
- `b8e5a9ce263fa094ccefe4ec5d9b167631ad47a0` — Add Phase 3 complete execution guide and master dashboard — Copilot <copilot@github.com>
- `05d43eef11daae078adae887d1a47c2b5b691b4a` — Phase3 Week 2-6 optimization planning and test suite — Copilot <copilot@github.com>
- `0b91a9809428916c2285fb4e862f42d4885f126d` — 🎉 HERMES AGENT PARALLEL DEPLOYMENT - FINAL COMPLETION REPORT — Copilot <copilot@github.com>
- `5f2d921bf5cbc3d354241fad5b86e60f35a0ee85` — COMPLETION: Monado Blade v3.3.0 parallel deployment - Final status & next steps — Copilot <copilot@github.com>
- `ee47f69d7b9dfa323c518dacd8a0dbd0f273dec6` — DASHBOARD: Monado Blade v3.3.0 deployment status - All systems GO — Copilot <copilot@github.com>
- `505a45126a69c1f4873319aa798ae118d2d38544` — FINAL: Parallel deployment execution complete - all 3 tracks ready — Copilot <copilot@github.com>
- `9d7c4165a415af3e56032ff5b689e1ac9c4f9171` — docs: Add Hermes agent live execution status - Real-time deployment orchestration — Copilot <copilot@github.com>
- `87deb445c3fc30f41cbf8057ef71358b5ad0c483` — MONADO-BLADE-v3.3.0: Parallel deployment complete - Tracks A, B, C ready — Copilot <copilot@github.com>
- `90c6607274eb6b4d985ac80063fd8cd6fb2c4933` — docs: Add master execution report - Phase 1-2-3 parallel deployment tracking — Copilot <copilot@github.com>
- `d1cbd49f26f9a2acadf5fe7fc0250050c7b6e372` — docs: Add real-time execution dashboard - Phase 1-2-3 parallel deployment — Copilot <copilot@github.com>
- `93434f90bd1809a82360529c93ccf103db145761` — docs: Add comprehensive deployment manifest - Complete checklist for production — Copilot <copilot@github.com>
- `81d9ea719845367c481ce54221c72d0fb5652e60` — docs: Add deployment README - Quick start guide for production deployment — Copilot <copilot@github.com>
- `1bad74f9ccacce5046d99290afd19727885049ea` — docs: Campaign completion executive summary - Ready for production deployment — Copilot <copilot@github.com>
- `59f446a8ac1879af6c1291c752a850b808005411` — tool: Add deployment orchestrator PowerShell script — Copilot <copilot@github.com>
- `62da7b6ac0acfc41648e7e730cd75591241e864e` — docs: Complete deployment and Phase 3 execution documentation — Copilot <copilot@github.com>
- `4cf5b98b61dbc10e58365e18545147e0b989fabb` — docs: v3.3.0 deployment execution summary - Phase 1-2 ready for immediate deployment — Copilot <copilot@github.com>
- `6619a9810de3b900ae221bb51b006c8035616b0e` — OPTIMIZATION 4: Cache Invalidation Optimization — Copilot <copilot@github.com>
- `d29375303a1cb9da5aa347967de41c1063d4be62` — DOCS: Add main README for Optimization 5 — Copilot <copilot@github.com>
- `2a73c3aa9060b5e5519539978ab30a95b2c7f1af` — feat(concurrency): Implement async task batching engine with 15% throughput improvement — Copilot <copilot@github.com>
- `b16c977ef3490236d99f2452260c556cb1a73927` — DOCS: Add comprehensive documentation for Optimization 5 — Copilot <copilot@github.com>
- `fa572742e9f1d64cab2c91eac89c86b5f8e28da9` — feat: Implement OPTIMIZATION 3 - Message Coalescing for Monado Blade — Copilot <copilot@github.com>
- `31312168a8540db39e4501c04c2764fa23b544d8` — OPTIMIZATION-5: Implement lock-free collections (+16% throughput, -90% contention) — GitHub Actions <actions@github.com>
- `08412be8498f918ad04b1aa4f09fcc146dbe790c` — docs: Add README for v3.3.0 deployment preparation — GitHub Actions <actions@github.com>
- `080f7eb91bfe09e0f37da714678d32fc18553c77` — docs: Add comprehensive deployment documentation index — GitHub Actions <actions@github.com>
- `bfdfc98df4a0b181aaed038d6c5c25286d269ac8` — docs: Add v3.3.0 deployment preparation summary — GitHub Actions <actions@github.com>
- `01c0efb676daef2e58d8e453f44bb1c83cf71c54` — docs: Add comprehensive v3.3.0 deployment infrastructure — GitHub Actions <actions@github.com>
- `2c92c24f3cfc45fadf1ee85966fb5dbfc188ceda` — Documentation: v3.1.0 feature complete - dual-boot, auto-recovery, cloud sync — Copilot <223556219+copilot@users.noreply.github.com>
- `dc5ebfb60912319a3cb9a0c8da9cf0d21cfe7eaa` — Features: Dual-boot wizard - Windows + Monado coexistence — Copilot <223556219+copilot@users.noreply.github.com>
- `fda98a8c08dcd09ff659c4a2325841e60e2337f4` — Docs: Bootstrap quickstart guide and API reference — Copilot <copilot@github.com>
- `466ecc465b4e62161a6a28d7df9d4bea00efe798` — USB: v3.0 simplification - one-page wizard — Copilot <copilot@github.com>
- `f4e8f7b98eb28d73395d354ab9d6d083051ff43e` — Docs: Stream D bootstrap automation complete — Copilot <copilot@github.com>
- `465818df61a5e5d776a7f80423b3fc94692e4edb` — Bootstrap: Unit tests for bootstrap automation — Copilot <copilot@github.com>
- `54e6f68abc975be5fca2ed7fdebad9a704fc587b` — Bootstrap: Pre-flight system checks — Copilot <copilot@github.com>
- `d23bbed015f7da70fd7ab9d8e9901f6ad34495c1` — docs: Add Phase 4 Final Deliverable Summary — Copilot <copilot@github.com>
- `65c731e972da68a26b1efa7a0e01e34206b26f09` — docs: Add Phase 4 Continuous Optimization Execution Report — Copilot <copilot@github.com>
- `790a0840ebb3574b9f5405f96c1fdb9aaa2a9a2f` — Optimize: Phase 4 Continuous - automated tuning, self-healing, load prediction, cost optimization — Copilot <copilot@github.com>

### Patch-equivalent commits
- `023083e54e9283cc8f7ace7c09ef2079e23264b4` — USB: Auto-build in background with intelligent caching — Copilot <copilot@github.com>

### Files
- `.github/PULL_REQUEST_TEMPLATE_PHASE1.md`
- `.github/PULL_REQUEST_TEMPLATE_PHASE2.md`
- `.github/PULL_REQUEST_TEMPLATE_TASK11.md`
- `.github/workflows/build-and-test.yml`
- `.github/workflows/build.yml`
- `.github/workflows/deep-ai-automation-orchestrator.yml`
- `.github/workflows/helios-dotnet-ci.yml`
- `.github/workflows/release.yml`
- `.github/workflows/test.yml`
- `.gitignore`
- `ASYNC_OPTIMIZATION_AUDIT_HOUR5-6.md`
- `AUTONOMOUS_HOUR_6-8_COMPLETION.md`
- `App.xaml`
- `BENCHMARK_RESULTS_DETAILED.md`
- `BOOTSTRAP_QUICKSTART.md`
- `BOOT_SEQUENCE_DESIGN.md`
- `BenchmarkRunner/BenchmarkRunner.csproj`
- `BenchmarkRunner/Program.cs`
- `CAMPAIGN_COMPLETION_EXECUTIVE_SUMMARY.md`
- `CAMPAIGN_FINAL_SUMMARY_v3.3.0+.md`
- `COMMIT_MESSAGE.md`
- `CONTRIBUTING.md`
- `DELIVERABLE.md`
- `DELIVERY_COMPLETE.md`
- `DELIVERY_COMPLETE_v3.1.0.md`
- `DEPLOYMENT_CHECKLIST_v3.3.0.md`
- `DEPLOYMENT_DOCUMENTATION_INDEX_v3.3.0.md`
- `DEPLOYMENT_EXECUTION_SUMMARY.md`
- `DEPLOYMENT_GUIDE.md`
- `DEPLOYMENT_GUIDE_v3.3.0.md`
- `DEPLOYMENT_MANIFEST.md`
- `DEPLOYMENT_PREPARATION_SUMMARY_v3.3.0.md`
- `DEPLOYMENT_README.md`
- `DEPLOYMENT_STATUS_DASHBOARD.md`
- `Developer.Ecosystem/DELIVERY_COMPLETE.md`
- `Developer.Ecosystem/Developer.Ecosystem.csproj`
- `Developer.Ecosystem/DeveloperEcosystemWindow.xaml`
- `Developer.Ecosystem/DeveloperEcosystemWindow.xaml.cs`
- `Developer.Ecosystem/EXAMPLE_QUERIES.md`
- `Developer.Ecosystem/EcosystemTests.cs`
- `Developer.Ecosystem/IMPLEMENTATION_GUIDE.md`
- `Developer.Ecosystem/INDEX.md`
- `Developer.Ecosystem/MONADO_BLADE_DEVELOPER_ECOSYSTEM_WSL2_HERMES_DEVDRIVE.cs`
- `Developer.Ecosystem/README.md`
- `Developer.Ecosystem/setup-windows.bat`
- `Developer.Ecosystem/setup-wsl2.sh`
- `EXECUTIVE_HANDOFF_PHASE3_READY.md`
- `EXECUTIVE_SUMMARY.md`
- `FEATURES_v3.1.0.md`
- `FINAL_DELIVERY_REPORT.md`
- `FINAL_VERIFICATION.txt`
- `GITHUB_UNIFIED_PLAN.md`
- `HARDWARE_INTEGRATION_DESIGN.md`
- `HELIOS.Platform.csproj`
- `HERMES_AGENT_LIVE_STATUS.md`
- `HERMES_COMPLETION_FINAL_REPORT.md`
- `HOUR5-6_COMPLETION_REPORT.md`
- `INDEX_v3.1.0.md`
- `INTEGRATION_ARCHITECTURE.md`
- `LIVE_EXECUTION_DASHBOARD.md`
- `LockAudit.md`
- `MASTER_EXECUTION_REPORT.md`
- `MIGRATION_GUIDE_LOCK_FREE.md`
- `MONADO_BLADE_DEPLOYMENT_READY_FINAL_STATUS.md`
- `MONITORING_PHASE2_COMPLETION.md`
- `MONITORING_PHASE2_EXECUTION_CHECKLIST.md`
- `MonadoBlade.csproj`
- `MonadoBlade.sln`
- `OBJECT_POOL_EXPANSION_REPORT.md`
- `OPTIMIZATION1_ASYNC_TASK_BATCHING_SUMMARY.md`
- `OPTIMIZATION3_IMPLEMENTATION_SUMMARY.md`
- `OPTIMIZATION3_MESSAGE_COALESCING_REPORT.md`
- `OPTIMIZATION_4_DELIVERY_REPORT.md`
- `OPTIMIZATION_5_COMPLETION_REPORT.md`
- `OPTIMIZATION_5_DELIVERY_SUMMARY.txt`
- `OPTIMIZATION_5_EXECUTION_SUMMARY.md`
- `OPTIMIZATION_5_INDEX.md`
- `OPTIMIZATION_5_README.md`
- `P2_COMPLETION_SUMMARY.md`
- `P2_SERVICE_LAYER_INDEX.md`
- `PARALLEL_DEPLOYMENT_EXECUTION_SUMMARY.md`
- `PARALLEL_DEPLOYMENT_MASTER_REPORT.md`
- `PERFORMANCE_BASELINE_HOUR8.md`
- `PERFORMANCE_BASELINE_v3.3.0.md`
- `PHASE1_COMPLETION_REPORT.md`
- `PHASE2_ALL_STREAMS_COMPLETE.md`
- `PHASE2_COMPLETION_DASHBOARD.md`
- `PHASE2_COMPLETION_SUMMARY.md`
- `PHASE2_EXECUTIVE_SUMMARY.txt`
- `PHASE2_FINAL_DELIVERY.md`
- `PHASE2_INDEX.md`
- `PHASE2_PARALLEL_EXECUTION_REPORT.md`
- `PHASE3_COMPLETE_EXECUTION_GUIDE.md`
- `PHASE3_MASTER_EXECUTION_DASHBOARD.md`
- `PHASE3_WEEK2_ASYNC_BATCHING.md`
- `PHASE3_WEEK2_IMPLEMENTATION_GUIDE.md`
- `PHASE3_WEEK3_OBJECT_POOLING.md`
- `PHASE3_WEEK4_MESSAGE_COALESCING.md`
- `PHASE3_WEEK5_LOCK_FREE_HIGH_RISK.md`
- `PHASE3_WEEK6_FINAL_TUNING.md`
- `PHASE3_WEEKS3-6_EXECUTION_MATRIX.md`
- `PHASE4_EXECUTION_REPORT.md`
- `PHASE_1D_COMPLETION_SUMMARY.md`
- `PHASE_1_2_DEPLOYMENT_VALIDATION_SUITE.md`
- `PHASE_3_WEEKLY_EXECUTION_SCHEDULE.md`
- `PHASE_4D_COMPLETION_REPORT.md`
- `PHASE_4_COMPLETION_GUIDE.md`
- `PHASE_5_COMPLETION_GUIDE.md`
- `PerformanceBenchmarkRunner.cs`
- `README.md`
- `README_CAMPAIGN_GUIDE.md`
- `README_DEPLOYMENT_STATUS.txt`
- `README_DEPLOYMENT_v3.3.0.md`
- `README_PHASE_4D.md`
- `README_v3.1.0.txt`
- `README_v3.6.1.md`
- `RELEASE_NOTES_v3.3.0.md`
- `RELEASE_NOTES_v3.6.1-COMPLETE.md`
- `ROLLBACK_PROCEDURES_v3.3.0.md`
- `SECURITY_STREAM_PHASE2_EXECUTION_REPORT.md`
- `SERVICE_IMPLEMENTATION_GUIDE.md`
- `SERVICE_LAYER_QUICK_REFERENCE.md`
- `SOURCE_CODE_SUMMARY.md`
- `STREAM1_PHASE1_INDEX.md`
- `STREAM1_QUICKSTART.md`
- `STREAM_A_COMPLETION_REPORT.md`
- `STREAM_D_BOOTSTRAP_COMPLETE.md`
- `TEST_COVERAGE_REPORT_v3.3.0.md`
- `TRACK_A_PHASE_1_2_DEPLOYMENT.md`
- `TRACK_B_PHASE3_BASELINE_WEEK1.md`
- `TRACK_C_PHASE3_EXECUTION_PLAN.md`
- `TaskBatchTest/Class1.cs`
- `TaskBatchTest/Program.cs`
- `TaskBatchTest/TaskBatchAccumulator.cs`
- `TaskBatchTest/TaskBatchTest.csproj`
- `TaskBatchTest/TaskBatcher.cs`
- `TaskBatchValidation/Program.cs`
- `TaskBatchValidation/TaskBatchAccumulator.cs`
- `TaskBatchValidation/TaskBatchValidation.csproj`
- `TaskBatchValidation/TaskBatcher.cs`
- `USB_BUILDER_ARCHITECTURE.md`
- `USB_v3.0_SIMPLIFICATION.md`
- `V3.4.0_ADVANCED_OPTIMIZATION_TECHNICAL_SPEC.md`
- `V3.4.0_ARCHITECTURE_IMPROVEMENTS.md`
- `V3.4.0_COMPLETE_DELIVERY_STATUS.md`
- `V3.4.0_DEVELOPMENT_PROGRESS.md`
- `V3.4.0_EXPANDED_PREMIUM_FEATURES.md`
- `V3.4.0_IMPLEMENTATION_PLAN.md`
- `V3.4.0_OPTIMIZATION_ROADMAP.md`
- `V3.4.0_PHASES_4-7_EXECUTION_PLAN.md`
- `Week5/WiFiOptimization/ADMIN_PLAYBOOK.md`
- `Week5/WiFiOptimization/FINAL_DELIVERY_REPORT.md`
- `Week5/WiFiOptimization/INDEX.md`
- `Week5/WiFiOptimization/NetworkHealthMonitor.cs`
- `Week5/WiFiOptimization/NetworkOptimizationEngine.cs`
- `Week5/WiFiOptimization/PROJECT_COMPLETION_SUMMARY.md`
- `Week5/WiFiOptimization/QUICK_REFERENCE.md`
- `Week5/WiFiOptimization/README.md`
- `Week5/WiFiOptimization/VPNIntegrationLayer.cs`
- `Week5/WiFiOptimization/WiFiNetworkDetector.cs`
- `Week5/WiFiOptimization/WiFiOptimizationTests.cs`
- `Week5/WiFiOptimization/WiFiPerformanceOptimizer.cs`
- `Week5/WiFiOptimization/WiFiSecurityEnforcer.cs`
- `Week5/WiFiOptimization/wifi-config.template.json`
- `appsettings.Development.json`
- `appsettings.Production.json`
- `appsettings.json`
- `build/build.ps1`
- `build/publish.ps1`
- `build/test.ps1`
- `config/ai-services/ai-services-config.json`
- `config/automation/ai-automation-profiles.json`
- `config/partitions/helios-default-layout.json`
- `config/repositories/helios-remotes.json`
- `deployment-orchestrator.ps1`
- `docs/BRANCH_INTEGRATION_READINESS.md`
- `docs/CACHE_INVALIDATION_OPTIMIZATION.md`
- `docs/FAILOVER_STRATEGY_GUIDE.md`
- `docs/FEATURE_MATRIX.md`
- `docs/PERFORMANCE_REPORT_HOUR8.md`
- `docs/architecture/LOCAL_XTIER_ARTIFACTS_INTEGRATION.md`
- `docs/automation/deep-github-ai-automation.md`
- `docs/guides/CODESPACE_MICROSOFT_CSHARP_SETUP.md`
- `docs/integration/automatic-ai-automation.md`
- `docs/reference-implementations/Week2AsyncBatching.cs`
- `helios.sh`
- `jest.config.js`
- `microsoft-ecosystem/azure-integration/SETUP_GUIDE.md`
- `microsoft-ecosystem/scripts/connect-to-azure.ps1`
- `package.json`
- `run-object-pool-tests.ps1`
- `scripts/ai-services/DELIVERY_SUMMARY.md`
- `scripts/ai-services/INDEX.md`
- `scripts/ai-services/QUICK_REF.txt`
- `scripts/ai-services/README.md`
- `scripts/ai-services/SETUP.md`
- `scripts/ai-services/ai-services-config.json`
- `scripts/ai-services/ai-services-config.schema.json`
- `scripts/ai-services/chatgpt-pro-client.ps1`
- `scripts/ai-services/codex-client.ps1`
- `scripts/ai-services/configure-ai-services.ps1`
- `scripts/ai-services/gpt-4-5-client.ps1`
- `scripts/ai-services/hub.ps1`
- `scripts/ai-services/service-router.ps1`
- `scripts/ai-services/show-ai-costs.ps1`
- `scripts/ai-services/test-ai-services.ps1`
- `scripts/ai-services/validate-api-keys.ps1`
- `scripts/automation/deep_automation_orchestrator.py`
- `scripts/automation/helios_auto_integration.py`
- `scripts/dev/devsetup.sh`
- `scripts/dev/repo-optimize.sh`
- `scripts/setup/setup-specialist-environment.ps1`
- `scripts/setup/setup_specialist_environment.py`
- `src/Features/AutoRepairEngine.cs`
- `src/Features/CloudProfileSyncer.cs`
- `src/Features/CorruptionDetector.cs`
- `src/Features/DualBootManager.cs`
- `src/Features/DualBootWizard.cs`
- `src/Features/PartitionResizer.cs`
- `src/Features/RecoveryPartition.cs`
- `src/HELIOS.AIHub/Abstractions/AiAbstractions.cs`
- `src/HELIOS.AIHub/HELIOS.AIHub.csproj`
- `src/HELIOS.AIHub/Providers/LocalAiProvider.cs`
- `src/HELIOS.AIHub/Providers/LocalAiProviderOptions.cs`
- `src/HELIOS.AIHub/Routing/SecurityFirstAgentRouter.cs`
- `src/HELIOS.Platform/AIHubConnector.cs`
- `src/HELIOS.Platform/API/HELIOSControllers.cs`
- `src/HELIOS.Platform/Database/HELIOSMigration.cs`
- `src/HELIOS.Platform/HermesIntegration.cs`
- `src/HELIOS.Platform/Optimization/CodeOptimization/CodeAnalyzer.cs`
- `src/HELIOS.Platform/Optimization/ContinuousOptimizationOrchestrator.cs`
- `src/HELIOS.Platform/Optimization/CostOptimization/CostOptimizer.cs`
- `src/HELIOS.Platform/Optimization/DependencyOptimization/DependencyAnalyzer.cs`
- `src/HELIOS.Platform/Optimization/LoadPrediction/LoadPredictor.cs`
- `src/HELIOS.Platform/Optimization/PHASE4_README.md`
- `src/HELIOS.Platform/Optimization/Performance/ABTestingFramework.cs`
- `src/HELIOS.Platform/Optimization/Performance/PerfTuner.cs`
- `src/HELIOS.Platform/Optimization/SelfHealing/SelfHealingSystem.cs`
- `src/HELIOS.Platform/Optimization/UXAnalytics/UXAnalytics.cs`
- `src/HELIOS.Platform/PatternBroker.cs`
- `src/MonadoBlade.Audio/Abstractions/IAudioService.cs`
- `src/MonadoBlade.Audio/MonadoBlade.Audio.csproj`
- `src/MonadoBlade.Boot/Abstractions/IBootService.cs`
- `src/MonadoBlade.Boot/ModernProgressUI.cs`
- `src/MonadoBlade.Boot/MonadoBlade.Boot.csproj`
- `src/MonadoBlade.Boot/Services/BackgroundUSBBuilder.cs`
- `src/MonadoBlade.Boot/Services/SimpleUSBWizard.cs`
- `src/MonadoBlade.Boot/Services/USBCreationOrchestrator.cs`
- `src/MonadoBlade.Boot/Services/USBImageCache.cs`
- `src/MonadoBlade.Boot/SilentInstallationManager.cs`
- `src/MonadoBlade.Boot/SystemPreflightChecker.cs`
- `src/MonadoBlade.Core/Abstractions/IService.cs`
- `src/MonadoBlade.Core/Async/AsyncHelpers.cs`
- `src/MonadoBlade.Core/Async/AsyncPipeline.cs`
- `src/MonadoBlade.Core/Caching/CacheEntry.cs`
- `src/MonadoBlade.Core/Caching/CacheInvalidationPatterns.cs`
- `src/MonadoBlade.Core/Caching/DependencyTracker.cs`
- `src/MonadoBlade.Core/Caching/DistributedCacheWrapper.cs`
- `src/MonadoBlade.Core/Caching/IntelligentCache.cs`
- `src/MonadoBlade.Core/Concurrency/LockFreeCollections.cs`
- `src/MonadoBlade.Core/Concurrency/TaskBatchAccumulator.cs`
- `src/MonadoBlade.Core/Concurrency/TaskBatcher.cs`
- `src/MonadoBlade.Core/Concurrency/ThreadSafetyValidation.cs`
- `src/MonadoBlade.Core/Configuration/ConfigurationExtensions.cs`
- `src/MonadoBlade.Core/Data/ConnectionHealthEnhancements.cs.bak`
- `src/MonadoBlade.Core/Data/ConnectionHealthMonitor.cs.bak`
- `src/MonadoBlade.Core/Data/DataAccessLayer.cs`
- `src/MonadoBlade.Core/Data/DatabaseIndexOptimizer.cs`
- `src/MonadoBlade.Core/Data/QueryBatchExecutor.cs`
- `src/MonadoBlade.Core/Data/QueryCacheLayer.cs`
- `src/MonadoBlade.Core/Database/QueryOptimization.cs`
- `src/MonadoBlade.Core/DependencyInjection/CoreServiceExtensions.cs`
- `src/MonadoBlade.Core/DependencyInjection/ServiceCollectionExtensions.cs.bak`
- `src/MonadoBlade.Core/Exceptions/ServiceExceptions.cs`
- `src/MonadoBlade.Core/HELIOS/AIHubConnector.cs`
- `src/MonadoBlade.Core/HELIOS/HermesIntegration.cs`
- `src/MonadoBlade.Core/Integration/Examples/EventHandlerExample.cs`
- `src/MonadoBlade.Core/Integration/Examples/MessageDispatcherExample.cs`
- `src/MonadoBlade.Core/Integration/Examples/TaskSchedulerExample.cs`
- `src/MonadoBlade.Core/Integrations/EventPublisherCoalescingExample.cs`
- `src/MonadoBlade.Core/Integrations/NotificationQueueCoalescingExample.cs`
- `src/MonadoBlade.Core/Logging/LoggingConfiguration.cs`
- `src/MonadoBlade.Core/Messaging/MessageCoalescer.cs`
- `src/MonadoBlade.Core/MonadoBlade.Core.csproj`
- `src/MonadoBlade.Core/ObjectPooling/EventObjectPool.cs`
- `src/MonadoBlade.Core/ObjectPooling/MessageBufferPool.cs`
- `src/MonadoBlade.Core/ObjectPooling/ObjectPool.cs`
- `src/MonadoBlade.Core/ObjectPooling/TaskObjectPool.cs`
- `src/MonadoBlade.Core/Observability/AdvancedMetrics.cs`
- `src/MonadoBlade.Core/Optimization/AsyncPipelineV2.cs`
- `src/MonadoBlade.Core/Optimization/StringInterningPool.cs`
- `src/MonadoBlade.Core/Optimization/TaskBatchAccumulator.cs.bak`
- `src/MonadoBlade.Core/Security/SecurityHardening.cs`
- `src/MonadoBlade.Core/Services/CloudSyncService.cs`
- `src/MonadoBlade.Core/Services/DashboardService.cs`
- `src/MonadoBlade.Core/Services/ICloudSyncService.cs`
- `src/MonadoBlade.Core/Services/IDashboardService.cs`
- `src/MonadoBlade.Core/Services/IDataService.cs`
- `src/MonadoBlade.Core/Services/IMLService.cs`
- `src/MonadoBlade.Core/Services/IManageService.cs`
- `src/MonadoBlade.Core/Services/IMutationService.cs`
- `src/MonadoBlade.Core/Services/IPluginService.cs`
- `src/MonadoBlade.Core/Services/IQueryService.cs`
- `src/MonadoBlade.Core/Services/IService.cs`
- `src/MonadoBlade.Core/Services/ISettingsService.cs`
- `src/MonadoBlade.Core/Services/ISubscribeService.cs`
- `src/MonadoBlade.Core/Services/MLService.cs.bak`
- `src/MonadoBlade.Core/Services/ManageService.cs`
- `src/MonadoBlade.Core/Services/MutationService.cs`
- `src/MonadoBlade.Core/Services/PluginService.cs`
- `src/MonadoBlade.Core/Services/QueryService.cs`
- `src/MonadoBlade.Core/Services/ServiceBase.cs`
- `src/MonadoBlade.Core/Services/SettingsService.cs`
- `src/MonadoBlade.Core/Services/SubscribeService.cs.bak`
- `src/MonadoBlade.Core/SystemIntegration/WindowsSystemBridge.cs.bak`
- `src/MonadoBlade.Core/UI/AppStateManagement.cs`
- `src/MonadoBlade.Dashboard/Abstractions/IDashboardService.cs`
- `src/MonadoBlade.Dashboard/MonadoBlade.Dashboard.csproj`
- `src/MonadoBlade.Developer/Abstractions/IDeveloperService.cs`
- `src/MonadoBlade.Developer/MonadoBlade.Developer.csproj`
- `src/MonadoBlade.GUI/Animations/AnimationCoordinator.cs`
- `src/MonadoBlade.GUI/Animations/AnimationEngine.cs`
- `src/MonadoBlade.GUI/App.xaml`
- `src/MonadoBlade.GUI/App.xaml.cs`
- `src/MonadoBlade.GUI/Components/AdvancedComponents.cs`
- `src/MonadoBlade.GUI/Components/AnimatedChart.cs`
- `src/MonadoBlade.GUI/Components/Base/ComponentBase.cs`
- `src/MonadoBlade.GUI/Components/CoreComponents.cs`
- `src/MonadoBlade.GUI/Components/Helpers/ResponsiveHelper.cs`
- `src/MonadoBlade.GUI/Components/Helpers/ThemeManager.cs`
- `src/MonadoBlade.GUI/Components/Helpers/ValidationHelper.cs`
- `src/MonadoBlade.GUI/Dashboard/DashboardManager.cs`
- `src/MonadoBlade.GUI/Dashboard/DashboardUI.cs`
- `src/MonadoBlade.GUI/Design/DesignSystemCore.cs`
- `src/MonadoBlade.GUI/MainWindow.xaml`
- `src/MonadoBlade.GUI/MainWindow.xaml.cs`
- `src/MonadoBlade.GUI/MonadoBlade.GUI.csproj`
- `src/MonadoBlade.GUI/Performance/DataGridVirtualizer.cs`
- `src/MonadoBlade.GUI/Performance/LazyLoadingManager.cs`
- `src/MonadoBlade.GUI/Performance/TokenCacheManager.cs`
- `src/MonadoBlade.GUI/Showcase/ComponentGalleryWindow.xaml.cs`
- `src/MonadoBlade.GUI/StateManagement/AppStateManagement.cs`
- `src/MonadoBlade.GUI/StateManagement/Reducers/CloudSyncReducer.cs`
- `src/MonadoBlade.GUI/StateManagement/Reducers/DashboardReducer.cs`
- `src/MonadoBlade.GUI/StateManagement/Reducers/PluginReducer.cs`
- `src/MonadoBlade.GUI/StateManagement/Reducers/SettingsReducer.cs`
- `src/MonadoBlade.GUI/Themes/ThemeManager.cs`
- `src/MonadoBlade.GUI/ViewModels/DashboardViewModel.cs`
- `src/MonadoBlade.Graphics/Abstractions/IGraphicsService.cs`
- `src/MonadoBlade.Graphics/MonadoBlade.Graphics.csproj`
- `src/MonadoBlade.Security/Abstractions/IEncryptionKeyManager.cs`
- `src/MonadoBlade.Security/Abstractions/IInputValidator.cs`
- `src/MonadoBlade.Security/Abstractions/ISecureAuditLogger.cs`
- `src/MonadoBlade.Security/Abstractions/ISecurityService.cs`
- `src/MonadoBlade.Security/EncryptionKeyManager.cs`
- `src/MonadoBlade.Security/MonadoBlade.Security.csproj`
- `src/MonadoBlade.Security/SecureAuditLogger.cs`
- `src/MonadoBlade.Security/SecureInputValidator.cs`
- `src/MonadoBlade.Tools/Abstractions/IToolsService.cs`
- `src/MonadoBlade.Tools/MonadoBlade.Tools.csproj`
- `src/Tests/FeatureTests.cs`
- `src/core/HELIOS.Platform/Core/AI/Router/IRouter.cs`
- `src/core/HELIOS.Platform/Core/AIHub/AIHubFleetService.cs`
- `src/core/HELIOS.Platform/Core/AIHub/InMemoryAIHubRouter.cs`
- `src/core/HELIOS.Platform/Core/CLI/CliCommandExecutor.cs`
- `src/core/HELIOS.Platform/Core/Configuration/AzureConfiguration.cs`
- `src/core/HELIOS.Platform/Core/Performance/AssetLoadingOptimizer.cs`
- `src/core/HELIOS.Platform/Core/Performance/GPURenderingOptimizer.cs`
- `src/core/HELIOS.Platform/Core/Performance/MemoryOptimizationService.cs`
- `src/core/HELIOS.Platform/Core/Performance/ObjectPoolService.cs`
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform/SystemIntegration/PortableHotkeyInput.cs`
- `src/index.ts`
- `src/monitoring/AdvancedMetricsCollector.cs`
- `src/monitoring/AnomalyDetectionHooks.cs`
- `src/monitoring/CircuitBreaker.ts`
- `src/monitoring/DistributedTracingManager.cs`
- `src/monitoring/FailoverController.ts`
- `src/monitoring/GracefulDegradationEngine.ts`
- `src/monitoring/HealthCheckScheduler.ts`
- `src/monitoring/MetricsCollector.ts`
- `src/monitoring/MetricsQueryEngine.cs`
- `src/monitoring/MonadoBlade.Monitoring.csproj`
- `src/monitoring/__tests__/failover.test.ts`
- `src/monitoring/index.ts`
- `src/monitoring/types.ts`
- `src/tests/AIHub/LocalAiProviderTests.cs`
- `src/tests/HELIOS.Platform.Tests.csproj`
- `src/tests/Phase8Stream8OptimizationTests.cs`
- `tests/HELIOS.Platform.Tests/AIHub/AIHubFleetServiceTests.cs`
- `tests/MonadoBlade.Tests.Integration/IntegrationTests.cs`
- `tests/MonadoBlade.Tests.Integration/MonadoBlade.Tests.Integration.csproj`
- `tests/MonadoBlade.Tests.Performance/BENCHMARK_GUIDE.md`
- `tests/MonadoBlade.Tests.Performance/BenchmarkConfig.cs`
- `tests/MonadoBlade.Tests.Performance/BenchmarkRunner.cs`
- `tests/MonadoBlade.Tests.Performance/Benchmarks/CoreModuleBenchmarks.cs`
- `tests/MonadoBlade.Tests.Performance/Benchmarks/GarbageCollectionBenchmarks.cs`
- `tests/MonadoBlade.Tests.Performance/Benchmarks/LINQVsLoopBenchmarks.cs`
- `tests/MonadoBlade.Tests.Performance/Benchmarks/LoggingOverheadBenchmarks.cs`
- `tests/MonadoBlade.Tests.Performance/Benchmarks/MemoryPoolingBenchmarks.cs`
- `tests/MonadoBlade.Tests.Performance/Benchmarks/RegexCompilationBenchmarks.cs`
- `tests/MonadoBlade.Tests.Performance/Benchmarks/StringProcessingBenchmarks.cs`
- `tests/MonadoBlade.Tests.Performance/CI-CD_INTEGRATION.md`
- `tests/MonadoBlade.Tests.Performance/Concurrency/LockContentionBenchmark.cs`
- `tests/MonadoBlade.Tests.Performance/MessageCoalescingBenchmark.cs`
- `tests/MonadoBlade.Tests.Performance/MonadoBlade.Tests.Performance.csproj`
- `tests/MonadoBlade.Tests.Performance/ObjectPooling/GCPressureBenchmark.cs`
- `tests/MonadoBlade.Tests.Performance/OptimizationBenchmarks.cs`
- `tests/MonadoBlade.Tests.Performance/PerformanceTests.cs`
- `tests/MonadoBlade.Tests.Performance/Program.cs`
- `tests/MonadoBlade.Tests.Performance/QUICK_REFERENCE.md`
- `tests/MonadoBlade.Tests.Performance/TaskBatchAccumulatorBenchmark.cs`
- `tests/MonadoBlade.Tests.Performance/TaskBatcherBenchmark.cs`
- `tests/MonadoBlade.Tests.Performance/run-benchmarks.bat`
- `tests/MonadoBlade.Tests.Performance/run-benchmarks.sh`
- `tests/MonadoBlade.Tests.Unit/Async/AsyncInfrastructureTests.cs`
- `tests/MonadoBlade.Tests.Unit/Async/AsyncPipelineTests.cs`
- `tests/MonadoBlade.Tests.Unit/Boot/SimpleUSBWizardTests.cs`
- `tests/MonadoBlade.Tests.Unit/Boot/USBImageCacheTests.cs`
- `tests/MonadoBlade.Tests.Unit/Caching/CacheHitRateBenchmark.cs`
- `tests/MonadoBlade.Tests.Unit/Caching/CacheInvalidationTests.cs`
- `tests/MonadoBlade.Tests.Unit/Concurrency/LockFreeTests.cs`
- `tests/MonadoBlade.Tests.Unit/Concurrency/TaskBatchAccumulatorTests.cs`
- `tests/MonadoBlade.Tests.Unit/Concurrency/TaskBatcherTests.cs`
- `tests/MonadoBlade.Tests.Unit/Core/LoggingConfigurationTests.cs`
- `tests/MonadoBlade.Tests.Unit/Core/StringInterningPoolTests.cs`
- `tests/MonadoBlade.Tests.Unit/Fixtures/BaseTestFixture.cs`
- `tests/MonadoBlade.Tests.Unit/Messaging/MessageCoalescerTests.cs`
- `tests/MonadoBlade.Tests.Unit/ModernProgressUITests.cs`
- `tests/MonadoBlade.Tests.Unit/ModuleAbstractionsTests.cs`
- `tests/MonadoBlade.Tests.Unit/MonadoBlade.Tests.Unit.csproj`
- `tests/MonadoBlade.Tests.Unit/Monitoring/MonitoringTests.cs`
- `tests/MonadoBlade.Tests.Unit/ObjectPooling/ObjectPoolTests.cs`
- `tests/MonadoBlade.Tests.Unit/Optimization/CacheAndPipelineTests.cs`
- `tests/MonadoBlade.Tests.Unit/Optimization/TaskBatchAccumulatorTests.cs`
- `tests/MonadoBlade.Tests.Unit/Security/SecurityAndMetricsTests.cs`
- `tests/MonadoBlade.Tests.Unit/Security/SecurityTests.cs`
- `tests/MonadoBlade.Tests.Unit/SilentInstallationManagerTests.cs`
- `tests/MonadoBlade.Tests.Unit/SystemPreflightCheckerTests.cs`
- `tests/Phase2StreamTests.cs`
- `tests/Phase3Week2/AsyncTaskBatchingTests.cs`
- `tsconfig.json`

## origin/codex/find-best-steps-to-set-up-environment-xld4tn

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** high
- **Merge-tree conflicts:** none
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `bebf30ec6ba26e53dc0b2efdee86d95864d28e95` — Add fleet deploy learning and fix center — Yolkster64 <thepatman64@gmail.com>
- `65d65888bd06c5753b9c4afe09bf041717a5e944` — Add JRPG agent party control center — Yolkster64 <thepatman64@gmail.com>
- `cb38b81bb5c51569dbc3b5d0ba021b5155580ab3` — Add HELIOS deep control GUI and model store — Yolkster64 <thepatman64@gmail.com>
- `0ab467433042127a319a9bbe7e416f4a9017b92c` — Add HELIOS autoconnect dashboard and readiness tools — Yolkster64 <thepatman64@gmail.com>
- `3a1bbfe9bf7d159809e30cbf919242212a651e09` — Harden mass integration safety gates — Yolkster64 <thepatman64@gmail.com>
- `83e3dea686f79be163d865b175407318812503e0` — Add autofix policy and conflict forecasting — Yolkster64 <thepatman64@gmail.com>
- `694c80a01f454450160088442ef924ec1eef625b` — Add ASAP start sequence — Yolkster64 <thepatman64@gmail.com>
- `accd04e99ef9e0e97be7be184e235d3578da0d17` — Tighten blocking final gate workflow — Yolkster64 <thepatman64@gmail.com>
- `46f267222c434d0293df5a0c0569951ce67de746` — Make final gate blocking — Yolkster64 <thepatman64@gmail.com>
- `cf8485810edb45a25846bcb75c4c37bccb32f2b3` — Add multi-LLM router planning — Yolkster64 <thepatman64@gmail.com>
- `65439fe9f2be492cc4c8b961f7b1de613efe88a0` — Add specialization learning matrix — Yolkster64 <thepatman64@gmail.com>
- `419dd523aa7eb2167ef14f2fd8a63d62764d3ecc` — Add super automation backlog — Yolkster64 <thepatman64@gmail.com>
- `71dbaf4e1ae557a0e58897ad2f330f6ea81c18e3` — Add final setup finisher — Yolkster64 <thepatman64@gmail.com>
- `d423890364993608eca6393125eacc4f5074c2c7` — Add deep auto-upgrade orchestration — Yolkster64 <thepatman64@gmail.com>
- `587a35330161ad273f132d51905436432ad55084` — Add GitHub repository setup automation — Yolkster64 <thepatman64@gmail.com>
- `35d250d779560a1f8914f617a3dcb183f0ca5f4b` — Add deep capability setup registry — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-auto-upgrade.yml`
- `.github/workflows/helios-autofix.yml`
- `.github/workflows/helios-mass-integration.yml`
- `.github/workflows/helios-repository-setup.yml`
- `.github/workflows/helios-shell.yml`
- `config/helios-agent-progression.json`
- `config/helios-agent-runtime.json`
- `config/helios-agent-shop.json`
- `config/helios-agents.json`
- `config/helios-auto-upgrade.json`
- `config/helios-capabilities.json`
- `config/helios-copilot-m365.json`
- `config/helios-github-setup.json`
- `config/helios-gui-commands.json`
- `config/helios-hermes-xcore-models.json`
- `config/helios-llm-router.json`
- `config/helios-mass-integration.json`
- `config/helios-model-store.json`
- `config/helios-policy.json`
- `config/helios-specializations.json`
- `config/helios-super-automation-backlog.json`
- `config/helios-vault.json`
- `docs/DEVELOPMENT_SETUP.md`
- `docs/HELIOS_HERMES_XCORE_WORKING_STEPS.md`
- `scripts/analytics/fsharp_test_report.py`
- `scripts/automation/agent_runtime_matrix.py`
- `scripts/automation/autoconnect_setup.py`
- `scripts/automation/autofix_loop.py`
- `scripts/automation/code_fix_center.py`
- `scripts/automation/deep_setup_all.py`
- `scripts/automation/final_gate.py`
- `scripts/automation/finish_helios_setup.py`
- `scripts/automation/fix_csharp_compile.py`
- `scripts/automation/helios_auto_upgrade.py`
- `scripts/automation/helios_store.py`
- `scripts/automation/hermes_xcore_model_setup.py`
- `scripts/automation/llm_router_plan.py`
- `scripts/automation/model_store_report.py`
- `scripts/automation/openai_responses_runner.py`
- `scripts/automation/render_operator_dashboard.py`
- `scripts/automation/specialization_matrix.py`
- `scripts/automation/start_asap.py`
- `scripts/automation/super_automation_backlog.py`
- `scripts/azure/bicep_report.py`
- `scripts/github/conflict_forecast.py`
- `scripts/github/connect_github.py`
- `scripts/github/github_takeover_status.py`
- `scripts/github/mass_integration.py`
- `scripts/github/setup_repository.py`
- `scripts/integrations/helios_capability_setup.py`
- `scripts/learning/agent_party.py`
- `scripts/learning/agent_xp.py`
- `scripts/learning/core_ai_learning.py`
- `scripts/learning/fleet_deploy.py`
- `scripts/learning/record_event.py`
- `scripts/learning/summarize_learning.py`
- `scripts/microsoft365/copilot_m365_readiness.py`
- `scripts/native/benchmark_native.py`
- `scripts/security/automation_audit.py`
- `scripts/security/policy_gate.py`
- `scripts/security/vault_readiness.py`
- `tools/aihub/smoke-test.py`
- `tools/azure/setup-helios-azure-cli.ps1`
- `tools/gui/helios-control-center/README.md`
- `tools/gui/helios-control-center/app.js`
- `tools/gui/helios-control-center/index.html`
- `tools/gui/helios-control-center/styles.css`
- `tools/helios.ps1`

## origin/integration/windows-security-post-174

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `831de9bb942a3e5376dc7d97bbb88601b858bff1` — Keep architecture directory managed — Yolkster64 <thepatman64@gmail.com>
- `9fa6bae78ae5f2a098f9317bed306284e0223493` — Add module porting checklist — Yolkster64 <thepatman64@gmail.com>
- `659c7dadca705f6a55c03a1449dce15e02a705bc` — Record consolidation status — Yolkster64 <thepatman64@gmail.com>
- `9833a028b7875ceece1ce78b42c855a91da1a5d3` — Document repository governance manifests — Yolkster64 <thepatman64@gmail.com>
- `92b19b5013028110710e43b4d569e824ed3046f0` — Add post-174 consolidation sequence — Yolkster64 <thepatman64@gmail.com>
- `eb09fc73b23845a302307428c0ae48b89940ec83` — Record HELIOS submodule decision policy — Yolkster64 <thepatman64@gmail.com>
- `be765e649ea3211ebebc8464da736d0641b77e64` — Validate repository module boundaries — Yolkster64 <thepatman64@gmail.com>
- `02fdc925c6b7d0f9bd3e96ce9b04ae0aa6aee789` — Add machine-readable repository module boundaries — Yolkster64 <thepatman64@gmail.com>
- `40da5950b4ac259fcfd50d8aabe9f708ce8e1ae8` — Document post-174 repository and module ownership map — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/module-boundaries-validate.yml`
- `config/repository/README.md`
- `config/repository/module-boundaries.v1.json`
- `docs/architecture/.keep`
- `docs/architecture/CONSOLIDATION_STATUS.md`
- `docs/architecture/PORTING_CHECKLIST.md`
- `docs/architecture/POST_174_CONSOLIDATION_SEQUENCE.md`
- `docs/architecture/REPOSITORY_MODULE_MAP.md`
- `docs/architecture/SUBMODULE_DECISION_RECORD.md`

## origin/yolkster64-spec-define-hermes-xcore-federation-env

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** high
- **Merge-tree conflicts:** none
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `ebe51ce04c89febfa78e13ff84b18340fbb44360` — Fix xcore lane and idempotency edge cases — Copilot App <223556219+Copilot@users.noreply.github.com>
- `66d73af0bee1f7cd4cc80c6bbebdbffd83e420ed` — Preserve xcore naming lane and event mapping — Copilot App <223556219+Copilot@users.noreply.github.com>
- `106c728cdc704dccf6ba3b68f482a563638b3e95` — Strengthen contract schema conformance — Copilot App <223556219+Copilot@users.noreply.github.com>
- `846ecb4ba64b2250b595bab0da93184a033fed46` — Harden legacy environment migration paths — Copilot App <223556219+Copilot@users.noreply.github.com>
- `d72c08ff625e5ad626b3b1a142c29a3dd7f529a0` — Harden Hermes/XCore migration guardrails — Copilot App <223556219+Copilot@users.noreply.github.com>
- `997302a33dff7184d5b94697199da1ccc69598e0` — Harden Hermes/XCore contract validation and migration guards — Copilot App <223556219+Copilot@users.noreply.github.com>
- `937e8b12c268152a42d1cf92ea3266f4f00a2b06` — Merge main into PR branch — Copilot App <223556219+Copilot@users.noreply.github.com>
- `ddb81e5f94481ef473ae36bfbbb59560ca9eb9fd` — Define Hermes/XCore unified spec and x-tier migration — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/azure-dev-live-connect.yml`
- `.github/workflows/copilot-package.yml`
- `.github/workflows/helios-cloud-deploy.yml`
- `.github/workflows/helios-edge-automation-validate.yml`
- `.github/workflows/helios-polyglot-required.yml`
- `.github/workflows/helios-unified-plugin-validate.yml`
- `config/HELIOS_HERMES_XCORE_APPROVAL_GOVERNANCE_V1.json`
- `config/HELIOS_HERMES_XCORE_CAPABILITY_BINDINGS_V1.json`
- `config/HELIOS_HERMES_XCORE_ENVIRONMENT_BINDINGS_V1.json`
- `config/HELIOS_HERMES_XCORE_EVENT_PROFILE_V1.json`
- `docs/architecture/HERMES_XCORE_UNIFIED_SPEC_V1.md`
- `docs/architecture/UNIFIED_AGENT_COMMUNICATION.md`
- `integration/azure-dev-live-connect/README.md`
- `integration/azure-dev-live-connect/config/connections.json`
- `integration/azure-dev-live-connect/infra/main.bicep`
- `integration/azure-dev-live-connect/scripts/Connect-HeliosAzureDev.ps1`
- `integration/azure-dev-live-connect/scripts/Start-HeliosConnections.ps1`
- `monado/helios-control/.github/workflows/helios-cloud-deploy.yml`
- `monado/helios-control/config/edge-automation.json`
- `monado/helios-control/config/identity-bindings.json`
- `monado/helios-control/docs/AZURE_CONNECTOR_DEPLOYMENT.md`
- `monado/helios-control/docs/AZURE_INTERACTIVE_ONBOARDING.md`
- `monado/helios-control/docs/EDGE_AUTOMATION.md`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `monado/helios-control/infra/connector.bicep`
- `monado/helios-control/infra/main.bicep`
- `monado/helios-control/infra/main.parameters.example.json`
- `monado/helios-control/scripts/Connect-HeliosAzureInteractive.ps1`
- `monado/helios-control/scripts/Deploy-HeliosAzureConnector.ps1`
- `monado/helios-control/scripts/Invoke-HeliosEdgeAutomation.ps1`
- `monado/helios-control/scripts/Invoke-HeliosProvisionPreview.ps1`
- `monado/helios-control/scripts/bootstrap-helios-azure-oidc.sh`
- `monado/helios-control/src/Helios.Connect.Api/ControlRuns.cs`
- `monado/helios-control/src/Helios.Connect.Api/EdgeAutomationPlanner.cs`
- `monado/helios-control/src/Helios.Connect.Api/Program.cs`
- `monado/helios-control/src/Helios.Connect.Api/SetupWizardService.cs`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/index.html`
- `monado/helios-control/tests/Helios.Connect.Tests/ControlRunTests.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/SetupWizardTests.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/WebhookTests.cs`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/assets/oidc.json`
- `plugins/helios-control-fabric/assets/runner-topology.json`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/test_helios.py`
- `plugins/helios-control-fabric/skills/helios-control/SKILL.md`
- `schemas/hermes-xcore-approval-governance-v1.schema.json`
- `schemas/hermes-xcore-capability-bindings-v1.schema.json`
- `schemas/hermes-xcore-environment-bindings-v1.schema.json`
- `schemas/hermes-xcore-event-profile-v1.schema.json`
- `scripts/control/tests/test_validate_hermes_xcore_contract.py`
- `scripts/control/validate_hermes_xcore_contract.py`

## origin/codex/update-azure-deployment-workflow-and-infrastructure

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `28326b6dfc589dd082d0a6eca9cbb062279e939d` — Add offline phase consolidation scan — Yolkster64 <thepatman64@gmail.com>
- `d7ea00e9056a9eac17e844675c91c4c717da08b0` — Fix Azure App Service deployment package — Yolkster64 <thepatman64@gmail.com>
- `798deba7a0c6e8380f20bea35b1ed7779974f548` — Add Azure deployment infrastructure and package artifact — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `docs/optimization/AI_PERFORMANCE_SECURITY_REVIEW.md`
- `docs/phases/CONSOLIDATED_PHASE_MARKDOWN.md`
- `infrastructure/main.bicep`
- `microsoft-ecosystem/.github/workflows/azure-deploy.yml`
- `scripts/automation/consolidate_phase_docs_and_scan.py`
- `src/core/HELIOS.Platform.Minimal/HELIOS.Platform.csproj`
- `src/core/HELIOS.Platform.Minimal/Program.cs`

## origin/copilot/setup-helios-hermes-fleet

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `fa68a8a5a56a910f887355c4b22a4a195f3d5462` — Configure XCore9 Hermes local runner topology contract — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-unified-plugin-validate.yml`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/assets/connections.json`
- `plugins/helios-control-fabric/assets/runner-topology.json`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/test_helios.py`

## origin/integration/monado-azure-canonical-port

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** medium
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `455e5843af25bf899a4f0c07438e33b2147eb2e0` — Bind Azure release evidence to ready revision — Yolkster64 <thepatman64@gmail.com>
- `536830da5eb851c4c154be0dd9c1ab61737894d4` — Add governed Monado Azure integration slice — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-azure.yml`
- `.gitignore`
- `integration/hermes-xcore-monado-azure/README.md`
- `integration/hermes-xcore-monado-azure/infra/main.bicep`
- `integration/hermes-xcore-monado-azure/scripts/plan_evidence.py`
- `integration/hermes-xcore-monado-azure/services/control-api/.dockerignore`
- `integration/hermes-xcore-monado-azure/services/control-api/Dockerfile`
- `integration/hermes-xcore-monado-azure/services/control-api/app.py`
- `integration/hermes-xcore-monado-azure/services/control-api/wwwroot/index.html`

## origin/integration/helios-unified-control-plugin-v1

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `5a7af6bf6f3a4911a6f30103a35a703f4c0e6f12` — Document ChatGPT tenant activation gate — Yolkster64 <thepatman64@gmail.com>
- `910c29ca7d9aebf1509939b838fc1691e664eb52` — Complete OpenAI MCP app contract — Yolkster64 <thepatman64@gmail.com>
- `c62a56c506219c553b23390687d2a6e514d463a9` — Merge PR #188 foundation into unified plugin — Yolkster64 <thepatman64@gmail.com>
- `af109c8257161455b8236063433dc5fbfd6c0d77` — Align the Entra MCP authorization contract — Yolkster64 <thepatman64@gmail.com>
- `caa47af4bd17376e9eba0fc122c2a1714dff85f2` — Close deployment evidence and lease recovery gaps — Yolkster64 <thepatman64@gmail.com>
- `38a82ef7f0a98adc5599e5cc9e2e3c543d723869` — Validate all automatic code executors disabled — Yolkster64 <thepatman64@gmail.com>
- `0d41f15062b3061916b33c3bc346220d229b119f` — Disable unimplemented automatic test executor — Yolkster64 <thepatman64@gmail.com>
- `18a7b7b1a9de00c2fe0499fdb587ac6adfe3ac46` — Align edge docs with disabled PR executor — Yolkster64 <thepatman64@gmail.com>
- `1ac85e8369699a8c063331e98bcf92fba18e7a5f` — Validate origin-bound Entra onboarding — Yolkster64 <thepatman64@gmail.com>
- `5f3d983f3eebf231d39737ec1a91a87ffe6b9855` — Document governed tag and SSO finalization — Yolkster64 <thepatman64@gmail.com>
- `08ac3f3ea971b874fa69865b7333a188953dba27` — Finalize origin-bound Teams SSO after deployment — Yolkster64 <thepatman64@gmail.com>
- `1afb46a3970dc26a9a8e59a9d018e5246121e67b` — Validate governed onboarding tag contract — Yolkster64 <thepatman64@gmail.com>
- `51431e6af2c6033e8ba27bc1f83a3714fca8be89` — Bind onboarding to governed resource-group environment — Yolkster64 <thepatman64@gmail.com>
- `2656a2dae54cae12ac43e96bec75f824efc9c161` — Test control-run boundary rejection — Yolkster64 <thepatman64@gmail.com>
- `27532ba8d5235a56d7b19cd3b442ed2c386c648d` — Reject control-run boundary overrides synchronously — Yolkster64 <thepatman64@gmail.com>
- `2245b6392834c7a0989b4d7cb1a7913a8962a874` — Use configured Azure boundary for control runs — Yolkster64 <thepatman64@gmail.com>
- `7105efcae88d5cb00be02ec2012b4c74f1e263c1` — Remove unsupported control-run target override — Yolkster64 <thepatman64@gmail.com>
- `b73b209fb23cc8d0bfc106308dd025ed05b16f40` — Validate disabled mutation executors — Yolkster64 <thepatman64@gmail.com>
- `b37d31db69c03cbda35ecbffb0e04d699e951d68` — Disable unimplemented branch and vault executors — Yolkster64 <thepatman64@gmail.com>
- `e64a52635834e94773700a4db750524f70f8558d` — Document protected-workflow-only deployment — Yolkster64 <thepatman64@gmail.com>
- `2be6211cdd01036a0bb9a152d69fe6e4351ecaf9` — Enforce plan-only local Azure helper — Yolkster64 <thepatman64@gmail.com>
- `f6c7ed8e6cb07c0442284c259b24a787ab5714f4` — Retire local Azure and vault mutation paths — Yolkster64 <thepatman64@gmail.com>
- `ea1f9225ce4474874e38bd915fa361faa871ddf8` — Keep SSO contract grep literal — Yolkster64 <thepatman64@gmail.com>
- `dcaabdcd6ff5d1f5b889c3d7e968d38dd7456aa7` — Guard Microsoft SSO audience contract — Yolkster64 <thepatman64@gmail.com>
- `bf2d7b9c04031ce5893726d977f183f1e646c034` — Record Microsoft host SSO boundary — Yolkster64 <thepatman64@gmail.com>
- `d6839ab673cf2629752c48e5a84d277710432a5b` — Document origin-bound Teams SSO setup — Yolkster64 <thepatman64@gmail.com>
- `4aed0682ea2038dec86e64623e08d52f32a1f96d` — Validate domain-bound Teams SSO resource — Yolkster64 <thepatman64@gmail.com>
- `a00492fdceb7c4ad7abc5c73f2dcc6f2787786ae` — Match Teams SSO resource to tab origin — Yolkster64 <thepatman64@gmail.com>
- `043f8c8c10ec4f95545316d330e230ab3f025f20` — Bind Teams SSO audience to the tab origin — Yolkster64 <thepatman64@gmail.com>
- `85af530cec2dcccfe0ea8d89237d306f0138fab3` — Verify pinned TeamsJS integrity — Yolkster64 <thepatman64@gmail.com>
- `ebab38e68adaeff43fe712a448ac3bf5d6ee544e` — Pin auth popup TeamsJS with integrity — Yolkster64 <thepatman64@gmail.com>
- `c9b43bb761ce93f1a6be2224659310c7630865f5` — Pin current TeamsJS with integrity — Yolkster64 <thepatman64@gmail.com>
- `a21ced6edc7b30df815c711d7190136b86756e1f` — Validate normalized integration event route — Yolkster64 <thepatman64@gmail.com>
- `57f2018726cca2c15189f31b653a517736302488` — Align integration route with emitted event — Yolkster64 <thepatman64@gmail.com>
- `01a2a1246ce049b342ae7e054aee804e14efe1e6` — Test Microsoft tab authentication boundary — Yolkster64 <thepatman64@gmail.com>
- `5403bb8fb71a1a3ae0f006d5c3c38e3d4ec70426` — Complete Teams authentication without exposing tokens — Yolkster64 <thepatman64@gmail.com>
- `5f43beba7530914e0f165c06d413aa2104850be7` — Add same-origin Teams authentication completion page — Yolkster64 <thepatman64@gmail.com>
- `fe00b07ff335735c7c9c33a858d238d769094054` — Add same-origin Teams authentication start page — Yolkster64 <thepatman64@gmail.com>
- `6f85cc8b0fac694782ddcd76864df4b4a4f0fe11` — Use Teams SSO with interactive popup fallback — Yolkster64 <thepatman64@gmail.com>
- `71f7509f91ee4aabfa85af60eba772d82ab12066` — Load pinned TeamsJS for tab SSO — Yolkster64 <thepatman64@gmail.com>
- `0841832ca7bf8f190e63098f039b150b700a4e95` — Allow current Microsoft 365 tab hosts — Yolkster64 <thepatman64@gmail.com>
- `ca0f0b9459885b4143d6ce26a1222d631ecc4ec8` — Clarify terminal lease pattern — Yolkster64 <thepatman64@gmail.com>
- `20f0886fa47c1a18f81ab4d9e824f5ea99f1f49f` — Keep organization tags inside approved parameters — Yolkster64 <thepatman64@gmail.com>
- `22fd2e62161e2a69c2e0e03b982eac2e599bda40` — Guard lease and tag evidence contracts — Yolkster64 <thepatman64@gmail.com>
- `5529f154da250db36818781ab4c9fc6960203195` — Bind organization tags through cloud evidence — Yolkster64 <thepatman64@gmail.com>
- `0b7f88717036336773ffe553a158f583647b35e9` — Prove deterministic connector retries — Yolkster64 <thepatman64@gmail.com>
- `fc5d09e96246e555edeb422c629e93e91b0faf4b` — Harden control-run lease ownership — Yolkster64 <thepatman64@gmail.com>
- `75a1266274e7ad26cb3156c5eb8300d4bddb1221` — Document connector HMAC tuple — Yolkster64 <thepatman64@gmail.com>
- `b762a79a98673252dfb2ed7bb86e52bd91aa3290` — Document signed connector replay contract — Yolkster64 <thepatman64@gmail.com>
- `f577f9888bbdbf27f5cf04110b34d740c68fd269` — Assert generated source pin through variable — Yolkster64 <thepatman64@gmail.com>
- `340de00ccdd133217ba0ef3657f92004f5a876d2` — Correct Azure wizard stage description — Yolkster64 <thepatman64@gmail.com>
- `f75323cac00cde99391c0f64cc75e0aef9742e6b` — Render setup stages without isolation claim — Yolkster64 <thepatman64@gmail.com>
- `4974c2df8c0ba1597e7c30bc571a07ced78a9988` — Fix unified workflow interpolation — Yolkster64 <thepatman64@gmail.com>
- `21d5eb8b7b5f0639d00e5464d1eaf97eb214fdfb` — Enforce environment and secret-file guardrails — Yolkster64 <thepatman64@gmail.com>
- `f47e5cf4e11bc7eaffc4e3370b26840d24d117ed` — Document environment-bound apply evidence — Yolkster64 <thepatman64@gmail.com>
- `d425e488f9ee5ff6e056efc54e74bbdc343c9133` — Bind mutations to reviewed environment and restrict secret files — Yolkster64 <thepatman64@gmail.com>
- `d6c5167ffe8447e632b68f5820fb673f4d7edf29` — Import HMAC test dependency — Yolkster64 <thepatman64@gmail.com>
- `342a315a924594a4d510a6272c5109d8b5f80a49` — Verify signed connector replay tuple — Yolkster64 <thepatman64@gmail.com>
- `b8f909e461b19c8e058b669de2721c86b40a6468` — Bind connector HMAC to timestamp key and body — Yolkster64 <thepatman64@gmail.com>
- `2b2e78a1e5ac5e4c567471ddc31210239daed315` — Test truthful setup stage contract — Yolkster64 <thepatman64@gmail.com>
- `ee818356c053f3fa130e722f737d214743b05f4d` — Describe setup stages without claiming shell isolation — Yolkster64 <thepatman64@gmail.com>
- `17dc42e4110d222b1f53014de2cece59713b15aa` — Scope wizard cache cleanup to HELIOS-owned entries — Yolkster64 <thepatman64@gmail.com>
- `9295a7c6cf19f25292a3b4cab79d1b18c74d83f3` — Validate reviewed edge inputs events and identity scopes — Yolkster64 <thepatman64@gmail.com>
- `7dfae8382a0d1417cc748975caccfa50bb7a2a6b` — Synchronize edge automation operator contract — Yolkster64 <thepatman64@gmail.com>
- `e900426a41c558871c21847c26515a7d314138a4` — Separate HELIOS API inventory and persistence scopes — Yolkster64 <thepatman64@gmail.com>
- `b26beb4b516bcbc6f9d2818e36e0605b151e7a04` — Synchronize declared HELIOS MCP tools — Yolkster64 <thepatman64@gmail.com>
- `c8cf7ec8c102d6223a347ee6178021f7d767bb46` — Verify connector relay uses HeliosEvent envelope — Yolkster64 <thepatman64@gmail.com>
- `65c700b8be5b9b0399bda74cb234b23e203b2a08` — Normalize connector events and harden worker recovery — Yolkster64 <thepatman64@gmail.com>
- `f23002f507653d3d3ee001656886874f690c35ef` — Document source-pinned preview parameter — Yolkster64 <thepatman64@gmail.com>
- `5b50a259c7f19f6fc1fa809cd19623584fdca8c3` — Bind deployed runtime to the exact GitHub source SHA — Yolkster64 <thepatman64@gmail.com>
- `f93273c40b8bec99e0304502d56189aa3cfb1f99` — Add source SHA deployment parameter — Yolkster64 <thepatman64@gmail.com>
- `e625987c828b4142a1a321fe2982803666c56836` — Expose immutable source SHA to the connector runtime — Yolkster64 <thepatman64@gmail.com>
- `3750c314a6ab315f8b30a53b20880d1160c8d17d` — Thread source commit through HELIOS main Bicep — Yolkster64 <thepatman64@gmail.com>
- `22136ac1c3c336d9c573ccd0c3ad526abed9e3dd` — Test source-pinned setup bootstrap — Yolkster64 <thepatman64@gmail.com>
- `c7675c8c0f357a0c9011fe44017fca27238e2e8c` — Pin Azure setup wizard to the deployed source SHA — Yolkster64 <thepatman64@gmail.com>
- `bf084a253a76cd604f0763bca11717a2f741f1c7` — Restore and harden HELIOS edge automation script — Yolkster64 <thepatman64@gmail.com>
- `b5d4cc32f1c97374520c364bd1bd5c90ee9d831c` — Resolve edge parameters and preserve full what-if evidence — Yolkster64 <thepatman64@gmail.com>
- `5f18a1ed2faf912ff9cec2aa068271e46a9617ca` — Align Microsoft MCP plugin contract — Yolkster64 <thepatman64@gmail.com>
- `fbc68cc4d77d700d10d4278bcdc462aa9854fe5c` — Align Copilot agent with shared MCP app — Yolkster64 <thepatman64@gmail.com>
- `19bc8a6178998b3ba9a8c682fe19a3857b43b8da` — Teach HELIOS skill unified MCP and Edge contracts — Yolkster64 <thepatman64@gmail.com>
- `6eedb780f48ea95a501ddbb02e846c7b2c16914c` — Expand unified HELIOS setup and edge architecture — Yolkster64 <thepatman64@gmail.com>
- `2807417ec2cd5d377edc95d4bf8b0cee94db8c1b` — Document unified HELIOS MCP and Azure Edge setup — Yolkster64 <thepatman64@gmail.com>
- `9056f60ee34196fb16360ed2c65b3446acd0a936` — Update HELIOS integration authority map — Yolkster64 <thepatman64@gmail.com>
- `3a6a9e5e81bcb0cae3e1cc5c908267d7059fce9f` — Bump Microsoft HELIOS package to 0.6.0 — Yolkster64 <thepatman64@gmail.com>
- `ff2da585008a23a3308687b1aa7ac804340c8d98` — Bump HELIOS plugin to 0.6.0 — Yolkster64 <thepatman64@gmail.com>
- `85deb8ed04d8b4d2b53e300a3a33a718f7746d91` — Stabilize MCP search contract assertion — Yolkster64 <thepatman64@gmail.com>
- `617ea097c1fea4dd350035998e1c5f00464bae94` — Keep MCP argument failures inside tool boundary — Yolkster64 <thepatman64@gmail.com>
- `42584da0df33458cb116982495ab0abf3ae791f7` — Validate MCP OIDC Edge DevOps and runner contracts — Yolkster64 <thepatman64@gmail.com>
- `b13e4245ffba7439d5d38b7b94e88e073fa3088d` — Test HELIOS integration contracts — Yolkster64 <thepatman64@gmail.com>
- `45a07b26474f45624c8a3658fc453c8029041bd2` — Expand HELIOS OIDC Edge DevOps and runner CLI — Yolkster64 <thepatman64@gmail.com>
- `3ba2ae7bc1d1fc3ea9323b00e132c0b907e44be7` — Add Azure Edge activation contract — Yolkster64 <thepatman64@gmail.com>
- `0fe158e95064c8719156854438ff8048e35339d9` — Add HELIOS runner topology — Yolkster64 <thepatman64@gmail.com>
- `07795a03fb5188fd4e85cf1c85978bc51206f678` — Add governed DevOps sync contract — Yolkster64 <thepatman64@gmail.com>
- `cd4b87bc82b47e3058ee7b9242bc2c050c84c371` — Add HELIOS OIDC contract — Yolkster64 <thepatman64@gmail.com>
- `fb71fe17d99f2510ab9dac53bb77efec299d7d97` — Test HELIOS MCP search fetch and app resource — Yolkster64 <thepatman64@gmail.com>
- `1ed200fe95e1c94480f08ff7b9af850c884462ca` — Add MCP Apps Monado control widget — Yolkster64 <thepatman64@gmail.com>
- `a8a5b518ce553800b34709ab3691f03a7484ba85` — Unify HELIOS MCP tools and Apps UI contract — Yolkster64 <thepatman64@gmail.com>
- `918af5f2d301455d3ef785108d39f42f5b054422` — Fix recovery scan exception boundary — Yolkster64 <thepatman64@gmail.com>
- `04141ebeaed0e7f998896c24c8b96fc64224a891` — Use deterministic disposal in control-run tests — Yolkster64 <thepatman64@gmail.com>
- `ef119c4e7b5b0118876d10b999add5062f90809c` — Fix control-run resource lifetime and failure handling — Yolkster64 <thepatman64@gmail.com>
- `e2ecbad82a7f65d07399b830ba68d2c754e0cbc5` — fix(ci): validate current Copilot package version — Yolkster64 <thepatman64@gmail.com>
- `051189d03348cad892cacb14157d45a3ae516cb5` — feat(helios): add unified control plugin and Microsoft agent package — Yolkster64 <thepatman64@gmail.com>
- `329c3a6647542585edd01563414d2f3c5b4fa18a` — fix(helios): preserve setup wizard contract — Yolkster64 <thepatman64@gmail.com>
- `ffedc23c452329fdd9c76e84bd970271ee0cb491` — fix(helios): avoid capturing relay endpoint out parameter — Yolkster64 <thepatman64@gmail.com>
- `1674158d523190cca414ff4dc0f6458b0b77ff41` — feat(helios): add durable Edge one-click control runs — Yolkster64 <thepatman64@gmail.com>
- `43768b126548c78fcd083b8406f90d5fc2326fbd` — Harden Azure wizard automation and governed cleanup — Yolkster64 <thepatman64@gmail.com>
- `50e155dedc22cc2d905d3cc7a3e1d1de15d0f291` — Validate Copilot wizard package version 0.4.0 — Yolkster64 <thepatman64@gmail.com>
- `4f7ddafa2b44f611b5b5e74353d013cc2efb301a` — Validate Copilot wizard package version 0.4.0 — Yolkster64 <thepatman64@gmail.com>
- `17fc2f2b2e338b61264617c1d431342bacde17fe` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `e3d72b7d883362ac259660fc1bee3cb2ab365ff0` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `86b721ea1e24eac1a449c5892beedf8658015252` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `5355c59d48526e0aae7424ed6dca0b0525ca31e0` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `66f653ff4cbfd171694ef6fba6894f362dcef77d` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `207f6d185ad3ff5814fb9e29139ab0b2b9119977` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `994d14f5294c970f53c8e4295bc3ad0017682b2b` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `97340af3eac252a32e7d55ff6ae8e28caec91de2` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `a86ca40140157230fd4bda027d97646de98a18e7` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `52b501d1f88033ad64bf0a5acdc1e57343560b4c` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `d162ba4bc13d8808ef30b42d4e9bcf0a1e71fa63` — Add HELIOS Azure setup and upgrade wizard — Yolkster64 <thepatman64@gmail.com>
- `35b019154fd2585caf251a5722554855d9bff76e` — Document interactive multi-provider autofix workflow — Yolkster64 <thepatman64@gmail.com>
- `9346565859c483a01e605aa6fb45f0abf44e1014` — Add governed interactive autofix agent contract — Yolkster64 <thepatman64@gmail.com>
- `37c1f2e62a7a61c162bf02fe1950a6a3ff3f5e8b` — Serialize MCP tool payloads with web camelCase conventions — Yolkster64 <thepatman64@gmail.com>
- `ea4676ad9fbf22e6ba7f7356d37c201294bf70fe` — Keep plan-only MCP calls independent of Azure inventory configuration — Yolkster64 <thepatman64@gmail.com>
- `04a19fc395016e65ea9e848cf04db24204a97013` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `ffa81fb6025b60b1e69fb95b92016778f651b4cc` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `a4079c4403aa375dab2855b55a072f0543a5f5e7` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `b2c40e406cebfbf46540e6c8795a9a6ca3256964` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `c8c88c1abcdbfdb13cc53e6d2e6c2ca5109c1b7d` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `3fa041b2228272d2e6059d83dffb112693d0a99a` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `0eebe03d7fa2a00a886244a47423c2272bed3391` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `b5ae0de884c26a3896d87b291ab095c0cc73ff91` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.agents/plugins/marketplace.json`
- `.github/agents/helios-control.agent.md`
- `.github/instructions/helios-control.instructions.md`
- `.github/workflows/copilot-package.yml`
- `.github/workflows/helios-cloud-deploy.yml`
- `.github/workflows/helios-edge-automation-validate.yml`
- `.github/workflows/helios-unified-plugin-validate.yml`
- `monado/helios-control/.github/workflows/copilot-package.yml`
- `monado/helios-control/README.md`
- `monado/helios-control/appPackage/ai-plugin.json`
- `monado/helios-control/appPackage/declarativeAgent.json`
- `monado/helios-control/appPackage/manifest.json`
- `monado/helios-control/config/agent-fabric.json`
- `monado/helios-control/config/cloud-runtime.json`
- `monado/helios-control/config/edge-automation.json`
- `monado/helios-control/config/identity-bindings.json`
- `monado/helios-control/config/integrations.json`
- `monado/helios-control/connector/helios-azure-connector.openapi.yaml`
- `monado/helios-control/connector/mcp-manifest.example.json`
- `monado/helios-control/docs/AGENT_AUTOFIX.md`
- `monado/helios-control/docs/ARCHITECTURE.md`
- `monado/helios-control/docs/AZURE_CONNECTOR_DEPLOYMENT.md`
- `monado/helios-control/docs/AZURE_INTERACTIVE_ONBOARDING.md`
- `monado/helios-control/docs/AZURE_SETUP_WIZARD.md`
- `monado/helios-control/docs/CONNECTION_RUNBOOK.md`
- `monado/helios-control/docs/EDGE_AUTOMATION.md`
- `monado/helios-control/docs/IMPLEMENTATION_STATUS.md`
- `monado/helios-control/docs/MICROSOFT_TOOLCHAIN.md`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `monado/helios-control/infra/connector.bicep`
- `monado/helios-control/infra/main.bicep`
- `monado/helios-control/infra/main.parameters.example.json`
- `monado/helios-control/infra/main.parameters.json`
- `monado/helios-control/scripts/Connect-HeliosAzureInteractive.ps1`
- `monado/helios-control/scripts/Invoke-HeliosEdgeAutomation.ps1`
- `monado/helios-control/scripts/Test-HeliosCloudConnection.ps1`
- `monado/helios-control/src/Helios.Connect.Api/AzureInventoryService.cs`
- `monado/helios-control/src/Helios.Connect.Api/ControlRuns.cs`
- `monado/helios-control/src/Helios.Connect.Api/EdgeAutomationPlanner.cs`
- `monado/helios-control/src/Helios.Connect.Api/Helios.Connect.Api.csproj`
- `monado/helios-control/src/Helios.Connect.Api/Program.cs`
- `monado/helios-control/src/Helios.Connect.Api/SetupWizardService.cs`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/mcp/helios-control-v2.html`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/auth-end.html`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/auth-end.js`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/auth-start.html`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/icon-192.png`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/icon-512.png`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/icon.svg`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/index.html`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/manifest.webmanifest`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/sw.js`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/wizard.css`
- `monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/wizard.js`
- `monado/helios-control/tests/Helios.Connect.Tests/ControlRunTests.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/SetupWizardTests.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/WebhookTests.cs`
- `plugins/helios-control-fabric/.codex-plugin/plugin.json`
- `plugins/helios-control-fabric/.mcp.json`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/assets/connections.json`
- `plugins/helios-control-fabric/assets/devops-sync.json`
- `plugins/helios-control-fabric/assets/edge-runtime.json`
- `plugins/helios-control-fabric/assets/microsoft-mcp.template.json`
- `plugins/helios-control-fabric/assets/oidc.json`
- `plugins/helios-control-fabric/assets/runner-topology.json`
- `plugins/helios-control-fabric/assets/sharepoint-status-template.md`
- `plugins/helios-control-fabric/scripts/helios.ps1`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/helios.sh`
- `plugins/helios-control-fabric/scripts/test_helios.py`
- `plugins/helios-control-fabric/skills/helios-control/SKILL.md`

## origin/codex/refactor-project-structure-for-cross-platform-separation

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `cc8fdfb640b63c585b6825b4377b5805def1a9bc` — ci: separate core and windows shell builds — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/dotnet-build.yml`
- `.github/workflows/windows-shell-build.yml`
- `docs/BUILD_SEPARATION.md`

## origin/codex/automate-merge-and-fix-processes

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `0f77509f8c0c6f9030787e1a928a584cd1899076` — Avoid dirtying apply runs with generated plan — Yolkster64 <thepatman64@gmail.com>
- `be34f7682ffb9b9989dc0cc48c2103072eef6bc7` — Add HELIOS consolidation automation plan — Yolkster64 <thepatman64@gmail.com>
- `ea6df3ec01d90905e12d87ed991acbc785a449de` — Add consolidation source manifest — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.gitmodules`
- `MERGE_SOURCE_MANIFEST.yaml`
- `docs/integration/HELIOS_CONSOLIDATION_AUTOMATION.md`
- `docs/integration/HELIOS_CONSOLIDATION_EXECUTION_PLAN.md`
- `scripts/automation/consolidation-sources.json`
- `scripts/automation/helios_consolidation.py`

## origin/codex/integrate-full-ai-and-automation

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `6b3c13b860c6f36b135c12be85838abcbfbf8f16` — Add automatic AI automation integrator — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `config/automation/ai-automation-profiles.json`
- `docs/integration/automatic-ai-automation.md`
- `scripts/automation/helios_auto_integration.py`

## origin/codex/review-and-document-dotnet-install.sh

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `6c4b5ff0babe892bce3262d5fd08105ad3fa710f` — Scope dotnet installer ignore to repo root — Yolkster64 <thepatman64@gmail.com>
- `6308064a141c4c4729a4c082ac93f4af32c8bc0a` — Ignore local dotnet installer script — Yolkster64 <thepatman64@gmail.com>
- `0f77509f8c0c6f9030787e1a928a584cd1899076` — Avoid dirtying apply runs with generated plan — Yolkster64 <thepatman64@gmail.com>
- `be34f7682ffb9b9989dc0cc48c2103072eef6bc7` — Add HELIOS consolidation automation plan — Yolkster64 <thepatman64@gmail.com>
- `ea6df3ec01d90905e12d87ed991acbc785a449de` — Add consolidation source manifest — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.gitignore`
- `.gitmodules`
- `MERGE_SOURCE_MANIFEST.yaml`
- `docs/integration/HELIOS_CONSOLIDATION_AUTOMATION.md`
- `docs/integration/HELIOS_CONSOLIDATION_EXECUTION_PLAN.md`
- `scripts/automation/consolidation-sources.json`
- `scripts/automation/helios_consolidation.py`

## origin/codex/update-wiki-sync-workflow

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `3e2c03a04c028849f9a3bcfb6992a20e8367ea8e` — ci: update wiki sync workflow — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/QUICK_REFERENCE.md`
- `.github/WORKFLOWS.md`
- `.github/workflows/documentation-update.yml`

## origin/integration/pr188-rebased-staging

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `04a19fc395016e65ea9e848cf04db24204a97013` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `ffa81fb6025b60b1e69fb95b92016778f651b4cc` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `a4079c4403aa375dab2855b55a072f0543a5f5e7` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `b2c40e406cebfbf46540e6c8795a9a6ca3256964` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `c8c88c1abcdbfdb13cc53e6d2e6c2ca5109c1b7d` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `3fa041b2228272d2e6059d83dffb112693d0a99a` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `0eebe03d7fa2a00a886244a47423c2272bed3391` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>
- `b5ae0de884c26a3896d87b291ab095c0cc73ff91` — Rebase governed HELIOS edge automation onto main — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-edge-automation-validate.yml`
- `monado/helios-control/README.md`
- `monado/helios-control/config/edge-automation.json`
- `monado/helios-control/docs/EDGE_AUTOMATION.md`
- `monado/helios-control/scripts/Invoke-HeliosEdgeAutomation.ps1`
- `monado/helios-control/src/Helios.Connect.Api/EdgeAutomationPlanner.cs`
- `monado/helios-control/src/Helios.Connect.Api/Program.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/WebhookTests.cs`

## origin/yolkster64-sturdy-journey

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `567a262327ef316b76e54d1a200130bf57121897` — Harden Azure environment bootstrap workflow — Copilot App <223556219+Copilot@users.noreply.github.com>
- `acb83c0cccb1972d304efc003eaa1234eaaba03f` — feat: add GitHub bootstrap for HELIOS Azure env metadata — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-azure-env-bootstrap.yml`
- `.github/workflows/helios-unified-plugin-validate.yml`
- `monado/helios-control/docs/AZURE_INTERACTIVE_ONBOARDING.md`
- `monado/helios-control/scripts/Set-HeliosGitHubAzureEnvironment.ps1`

## origin/codex/make-significant-bug-fixes

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `90f4d14fc959950b192088da11549357327f6a77` — Harden cloud auth and add developer bootstrap — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `cloud-integration/auth/AuthenticationFactory.cs`
- `scripts/bootstrap-dev-environment.ps1`

## origin/codex/update-azure-setup-assets-and-features

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `65cfd27c3d551dd64625d013133a95a5e00d005c` — Add Azure CLI setup commands — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `microsoft-ecosystem/azure-integration/SETUP_GUIDE.md`
- `microsoft-ecosystem/scripts/connect-to-azure.ps1`
- `src/core/HELIOS.Platform/Core/CLI/CliCommandExecutor.cs`
- `src/core/HELIOS.Platform/Core/Configuration/AzureConfiguration.cs`

## origin/codex/set-all-sessions-to-read-write

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `5562081e63aa3757d5f40bbe9b2d075c4988fd6c` — Add cross-platform HELIOS repo integration tooling — Yolkster64 <thepatman64@gmail.com>
- `49b54ac5e4aa8bf033617be94ef81081ea77f78a` — Add full-access Azure CLI integration runbook — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `HELIOS-Platform-Portable/config/example-config.yaml`
- `docs/BRANCH_REPO_INTEGRATION_RUNBOOK.md`
- `scripts/devops/helios_repo_integrator.py`
- `scripts/devops/setup-full-access-azure-cli.ps1`
- `scripts/devops/setup-full-access-azure-cli.sh`
- `security.config.template.json`

## origin/Yolkster64-patch-1feat/unified-pipeline-scaffold

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `0b0000b588e5cdc608c324cd918c5a25604e1124` — Add HELIOS platform CI/CD workflow — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/helios-deploy.yml`

## origin/codex/create-analytics-project-structure-and-apis-2gzau7

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `d52d3984354fc6f63c64ff5f5e1a9771f2393a9c` — fix(ci): resolve failing validation workflows and deprecated artifact actions — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ci-validation.yml`
- `.github/workflows/code-checks.yml`
- `.github/workflows/dotnet-build.yml`

## origin/codex/update-github-workflows-for-required-checks

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `f85ccc7d3dcb07fa1421577ad139130c6e223394` — Potential fix for pull request finding — Yolkster64 <thepatman64@gmail.com>
- `f1b160a42f811a2921165c0441c77378d3eecbca` — Merge origin/main, resolve ci-validation conflict — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `b692849fa274c7adb1b8206f5271156be2bd0f8e` — Harden required CI quality gates — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/ci-validation.yml`
- `.github/workflows/quality.yml`

## origin/copilot/setup-build-matrix

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `ca8b95a8af5c16a8f9091e6358c9ac8d49f32ce3` — Update build workflow actions to Node 24-compatible versions — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/workflows/build-all-modules.yml`

## origin/yolkster64-curly-spork

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** high
- **Merge-tree conflicts:** none
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `0027bbd080a216ff953faa69ca4b5b0cf3c2f9d7` — Address review feedback on unified setup workflow — Copilot App <223556219+Copilot@users.noreply.github.com>
- `fd2d7d9444aed3b5032ba494890f0a7d08ee8c29` — Add unified Helios control setup orchestrator — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3e50a9cc5f779feb0904c3f9889db39f80b8c8ed` — Merge origin/main into integration/helios-chatgpt-copilot-app — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `7cbb0677f150f3250115d0a6bc6b98131013422e` — Potential fix for pull request finding 'Missed opportunity to use Where' — Yolkster64 <thepatman64@gmail.com>
- `f3290ad2ddd6e54ab6a5284c46cabb335a09ecb9` — Potential fix for pull request finding 'Missing Dispose call on local IDisposable' — Yolkster64 <thepatman64@gmail.com>
- `fbdd1bada8a1bb8b4f244c4a196d9553fe586abf` — Potential fix for pull request finding 'Poor error handling: empty catch block' — Yolkster64 <thepatman64@gmail.com>
- `471f1e07d839daab27d3adbdb0cc01afe67bff2c` — Potential fix for pull request finding 'Generic catch clause' — Yolkster64 <thepatman64@gmail.com>
- `642297079db21fd2bab26e561d7877b644bd1ec0` — Discover exact component manifest directories — Yolkster64 <thepatman64@gmail.com>
- `dc1f5c08944101de619e0cd77cdee00be307332f` — Fix path-safe component CI outputs — Yolkster64 <thepatman64@gmail.com>
- `f72dc9a95c55657980809ca6cc82bdee7e6338ee` — Complete Bicep module references and identities — Yolkster64 <thepatman64@gmail.com>
- `5963b7f63aa899f046cf62aa157d52238fea4d9d` — Import xUnit test attributes — Yolkster64 <thepatman64@gmail.com>
- `a68f43b6bb1ee18ac79e5bda20837dcbe08c4f5a` — Fix net8 HMAC hex parsing — Yolkster64 <thepatman64@gmail.com>
- `9abbcb0a4fc54397a9fc374aee0e2cdd05cb23dc` — Make HELIOS automation fabric buildable — Yolkster64 <thepatman64@gmail.com>
- `bd1120c57af7c19eb8687d22e9124e4e692e15dd` — Add review-only HELIOS automation fabric reference — Yolkster64 <thepatman64@gmail.com>
- `1993d8774b1c6aa86851f7e0d6cf94de7d4cb5d9` — Validate every enterprise setup operation — Yolkster64 <thepatman64@gmail.com>
- `5aa8ad65186e4b866cce7925b0d017c4eeecb2ff` — Keep Slack and Linear OAuth in MCP config without breaking setup phases — Yolkster64 <thepatman64@gmail.com>
- `4f5028aa2600c77e7dec9c358730de96fa8851bb` — Add Slack and Linear authentication to enterprise setup — Yolkster64 <thepatman64@gmail.com>
- `4ba2aee70e2b4c024d2f066f0fb8ed709090f106` — Use current Linear and Slack MCP endpoints — Yolkster64 <thepatman64@gmail.com>
- `77e02c8014be22b3f9da8d6ee8910cd53aa06855` — Add tests for enterprise setup controller — Yolkster64 <thepatman64@gmail.com>
- `2dba9bebd66e3d447f393968c5b8d67b12d6107a` — Add guarded enterprise setup controller to Azure CLI plugin — Yolkster64 <thepatman64@gmail.com>
- `861d2f8b30df89cbf1338e736875a35676b99aef` — Add guarded enterprise setup operation manifest — Yolkster64 <thepatman64@gmail.com>
- `38107738ca70d3483061bedc50c762a181a8ddc6` — Add Agent 365 Microsoft 365 tooling manifest — Yolkster64 <thepatman64@gmail.com>
- `49a09e8c2cdfda723c55b6e9ceb8b7608e90eb3c` — Add Agent 365 configuration template — Yolkster64 <thepatman64@gmail.com>
- `a236482038ec9b68a3131c487bbfe6b8a941d9ea` — Add Claude Code MCP configuration to shared control app — Yolkster64 <thepatman64@gmail.com>
- `a72aa66caec61f7368d05a2140d6eaff184b0be1` — Add Claude Code instructions to shared control app — Yolkster64 <thepatman64@gmail.com>
- `6429f1f24fa33f888d38185636e8ce4699e45efa` — fix(azure-cli): harden app lookup and parameter validation — Yolkster64 <thepatman64@gmail.com>
- `44085ed63a4f9f9096d174b09df4c1a9263ace43` — feat(azure): add guarded HELIOS CLI plugin and canonical coordination targets — Yolkster64 <thepatman64@gmail.com>
- `491578746496ba2dae786cfde025cbb6c55c93b5` — style(control-app): terminate safe link append explicitly — Yolkster64 <thepatman64@gmail.com>
- `8855ef4d39553b95db64d94761fa801a802486e4` — fix(control-app): harden widget rendering in .github/workflows/helios-control-app.yml — Yolkster64 <thepatman64@gmail.com>
- `90d7aaacb1b727c95f7ece389ac3731611dcd878` — fix(control-app): harden widget rendering in apps/helios-control/scripts/smoke.mjs — Yolkster64 <thepatman64@gmail.com>
- `ec1f6ff3a9bce3f5c4f5429fa90832919abce19f` — fix(control-app): harden widget rendering in apps/helios-control/public/control-center.html — Yolkster64 <thepatman64@gmail.com>
- `2949004f445d5d85c7548e2f3921973b26de203c` — feat(control-app): add .github/workflows/helios-control-app.yml — Yolkster64 <thepatman64@gmail.com>
- `3aaa4522a2de1846daaf328afd474aa0b5e478e5` — feat(control-app): add apps/helios-control/tsconfig.json — Yolkster64 <thepatman64@gmail.com>
- `9ec5b5fa626e9b5ba44ed96efe170b86bfb2a1d9` — feat(control-app): add apps/helios-control/src/server.ts — Yolkster64 <thepatman64@gmail.com>
- `85c51e1da3d09fcf58339dce0e2961746deb545c` — feat(control-app): add apps/helios-control/scripts/smoke.mjs — Yolkster64 <thepatman64@gmail.com>
- `e2c1e139cdbaead5619f49b9c354bd91189e656c` — feat(control-app): add apps/helios-control/public/control-center.html — Yolkster64 <thepatman64@gmail.com>
- `f5cb40a300f4a771391fb7e517ca3e958bed61f6` — feat(control-app): add apps/helios-control/package.json — Yolkster64 <thepatman64@gmail.com>
- `ec291a61ae120a9aeadf0e25c1440e0ba95fbfe0` — feat(control-app): add apps/helios-control/package-lock.json — Yolkster64 <thepatman64@gmail.com>
- `c63f09e027e35ac1bdb40a2a0a4ffb2365fe73ea` — feat(control-app): add apps/helios-control/copilot/helios-mcp.openapi.yaml — Yolkster64 <thepatman64@gmail.com>
- `93d93d9739d43beedd72e6554f337f66dbcf3096` — feat(control-app): add apps/helios-control/README.md — Yolkster64 <thepatman64@gmail.com>
- `eb05da0b8a68d6bc52b5e5dcf54ab42d033cd654` — feat(control-app): add apps/helios-control/Dockerfile — Yolkster64 <thepatman64@gmail.com>
- `bc67d4094dc4ea867c0d9132ba7d6a3cdfb00940` — feat(control-app): add apps/helios-control/.env.example — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.agents/plugins/marketplace.json`
- `.github/workflows/build-all-modules.yml`
- `.github/workflows/helios-control-app.yml`
- `apps/helios-control/.env.example`
- `apps/helios-control/.mcp.json`
- `apps/helios-control/CLAUDE.md`
- `apps/helios-control/Dockerfile`
- `apps/helios-control/README.md`
- `apps/helios-control/copilot/agent365/ToolingManifest.json`
- `apps/helios-control/copilot/agent365/a365.config.template.json`
- `apps/helios-control/copilot/helios-mcp.openapi.yaml`
- `apps/helios-control/package-lock.json`
- `apps/helios-control/package.json`
- `apps/helios-control/public/control-center.html`
- `apps/helios-control/scripts/smoke.mjs`
- `apps/helios-control/src/server.ts`
- `apps/helios-control/tsconfig.json`
- `monado/helios-control/README.md`
- `monado/helios-control/scripts/Invoke-HeliosCliMatrix.ps1`
- `monado/helios-control/scripts/Invoke-HeliosFullSetup.ps1`
- `monado/helios-control/scripts/Invoke-HeliosProvisionPreview.ps1`
- `plugins/helios-azure-cli/.codex-plugin/plugin.json`
- `plugins/helios-azure-cli/README.md`
- `plugins/helios-azure-cli/SKILL.md`
- `plugins/helios-azure-cli/assets/enterprise-setup.json`
- `plugins/helios-azure-cli/assets/helios-targets.json`
- `plugins/helios-azure-cli/scripts/helios-azure.ps1`
- `plugins/helios-azure-cli/scripts/helios-azure.sh`
- `plugins/helios-azure-cli/scripts/helios_azure.py`
- `plugins/helios-azure-cli/scripts/helios_enterprise.py`
- `plugins/helios-azure-cli/scripts/test_helios_azure.py`
- `plugins/helios-azure-cli/scripts/test_helios_enterprise.py`
- `reference/helios-enterprise-automation-fabric/.Register-HeliosTeamsWorkflow.ps1.nqnd05jk`
- `reference/helios-enterprise-automation-fabric/FabricWorker.cs`
- `reference/helios-enterprise-automation-fabric/GitHubControlSink.cs`
- `reference/helios-enterprise-automation-fabric/HELIOS.Fabric.sln`
- `reference/helios-enterprise-automation-fabric/Initialize-HeliosAzureOidc.ps1`
- `reference/helios-enterprise-automation-fabric/Initialize-HeliosPowerPlatform.ps1`
- `reference/helios-enterprise-automation-fabric/Initialize-HeliosSharePoint.ps1`
- `reference/helios-enterprise-automation-fabric/LinearSink.cs`
- `reference/helios-enterprise-automation-fabric/Program.cs`
- `reference/helios-enterprise-automation-fabric/README.md`
- `reference/helios-enterprise-automation-fabric/RECOVERY_STATUS.md`
- `reference/helios-enterprise-automation-fabric/RUN_THIS_FIRST.md`
- `reference/helios-enterprise-automation-fabric/Register-HeliosTeamsWorkflow.ps1`
- `reference/helios-enterprise-automation-fabric/SharePointEvidenceSink.cs`
- `reference/helios-enterprise-automation-fabric/SlackSink.cs`
- `reference/helios-enterprise-automation-fabric/TeamsSink.cs`
- `reference/helios-enterprise-automation-fabric/WebhookEndpoints.cs`
- `reference/helios-enterprise-automation-fabric/action-pin-plan.yml`
- `reference/helios-enterprise-automation-fabric/action-policy.json`
- `reference/helios-enterprise-automation-fabric/apiDefinition.swagger.json`
- `reference/helios-enterprise-automation-fabric/apiProperties.json`
- `reference/helios-enterprise-automation-fabric/app-manifest.yaml`
- `reference/helios-enterprise-automation-fabric/approval-policy.json`
- `reference/helios-enterprise-automation-fabric/automations.json`
- `reference/helios-enterprise-automation-fabric/azure-deploy.yml`
- `reference/helios-enterprise-automation-fabric/azure-plan.yml`
- `reference/helios-enterprise-automation-fabric/azure.yaml`
- `reference/helios-enterprise-automation-fabric/broker-images.yml`
- `reference/helios-enterprise-automation-fabric/channel-bindings.example.json`
- `reference/helios-enterprise-automation-fabric/channels.json`
- `reference/helios-enterprise-automation-fabric/config-drift.yml`
- `reference/helios-enterprise-automation-fabric/connector-readiness.yml`
- `reference/helios-enterprise-automation-fabric/connector-registry.json`
- `reference/helios-enterprise-automation-fabric/containerapps.bicep`
- `reference/helios-enterprise-automation-fabric/control-plane-operator.yml`
- `reference/helios-enterprise-automation-fabric/deployment-approval.json`
- `reference/helios-enterprise-automation-fabric/emergency-quarantine.yml`
- `reference/helios-enterprise-automation-fabric/event-envelope.schema.json`
- `reference/helios-enterprise-automation-fabric/evidence-mapping.json`
- `reference/helios-enterprise-automation-fabric/execution-plan.json`
- `reference/helios-enterprise-automation-fabric/fabric-ci.yml`
- `reference/helios-enterprise-automation-fabric/graph-permission-plan.json`
- `reference/helios-enterprise-automation-fabric/incident-critical.json`
- `reference/helios-enterprise-automation-fabric/information-architecture.json`
- `reference/helios-enterprise-automation-fabric/issue-templates.json`
- `reference/helios-enterprise-automation-fabric/linear_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/lists-and-libraries.json`
- `reference/helios-enterprise-automation-fabric/main.bicep`
- `reference/helios-enterprise-automation-fabric/nightly-health.yml`
- `reference/helios-enterprise-automation-fabric/pin_actions.py`
- `reference/helios-enterprise-automation-fabric/pr-linear-sync.yml`
- `reference/helios-enterprise-automation-fabric/project/.dockerignore`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/action-pin-plan.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/azure-deploy.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/azure-plan.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/broker-images.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/config-drift.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/connector-readiness.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/control-plane-operator.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/emergency-quarantine.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/fabric-ci.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/nightly-health.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/pr-linear-sync.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/promote-reviewed-sha.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/release-evidence.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/reusable-build-event.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/sharepoint-governance-sync.yml`
- `reference/helios-enterprise-automation-fabric/project/.github/workflows/workflow-outcome-router.yml`
- `reference/helios-enterprise-automation-fabric/project/.gitignore`
- `reference/helios-enterprise-automation-fabric/project/README.md`
- `reference/helios-enterprise-automation-fabric/project/RUN_THIS_FIRST.md`
- `reference/helios-enterprise-automation-fabric/project/azure.yaml`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/approval-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/connector-registry.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/deployment-approval.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/event-envelope.schema.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/evidence-mapping.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/execution-plan.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/routing-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/fabric/status-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/github/action-policy.json`
- `reference/helios-enterprise-automation-fabric/project/config/linear/automations.json`
- `reference/helios-enterprise-automation-fabric/project/config/linear/issue-templates.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/information-architecture.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/lists-and-libraries.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/retention-labels.example.json`
- `reference/helios-enterprise-automation-fabric/project/config/sharepoint/sharepoint-sync-manifest.json`
- `reference/helios-enterprise-automation-fabric/project/config/slack/app-manifest.yaml`
- `reference/helios-enterprise-automation-fabric/project/config/slack/channel-bindings.example.json`
- `reference/helios-enterprise-automation-fabric/project/config/slack/channels.json`
- `reference/helios-enterprise-automation-fabric/project/config/teams/workflow-contracts(1).json`
- `reference/helios-enterprise-automation-fabric/project/config/teams/workflow-contracts.json`
- `reference/helios-enterprise-automation-fabric/project/docker/broker.Dockerfile`
- `reference/helios-enterprise-automation-fabric/project/docker/worker.Dockerfile`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/containerapps.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/main.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/modules/foundation.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/modules/private-endpoint.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/parameters/dev.bicepparam`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/parameters/prod.bicepparam`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/parameters/stage.bicepparam`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/servicebus.bicep`
- `reference/helios-enterprise-automation-fabric/project/infra/bicep/storage.bicep`
- `reference/helios-enterprise-automation-fabric/project/power-platform/connector/apiDefinition.swagger.json`
- `reference/helios-enterprise-automation-fabric/project/power-platform/connector/apiProperties.json`
- `reference/helios-enterprise-automation-fabric/project/pyproject.toml`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Initialize-HeliosAzureOidc.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Initialize-HeliosPowerPlatform.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Initialize-HeliosSharePoint.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Install-HeliosFabricOverlay.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/Register-HeliosTeamsWorkflow.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/linear_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/bootstrap/slack_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/connectors/sharepoint_sync.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/build_event.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/canonicalize_plan.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/control_plane_dispatch.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/pin_actions.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/github/publish_event.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/governance/pin_actions.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/governance/sharepoint_sync.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/validation/Validate-PowerShell.ps1`
- `reference/helios-enterprise-automation-fabric/project/scripts/validation/assert_no_secret_values.py`
- `reference/helios-enterprise-automation-fabric/project/scripts/validation/validate_bicep.sh`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/BrokerServices.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/HELIOS.Fabric.Broker.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/Program.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Broker/WebhookEndpoints.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Contracts/FabricContracts.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Contracts/HELIOS.Fabric.Contracts.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Tests/FabricTests.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Tests/HELIOS.Fabric.Tests.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/FabricWorker.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/GitHubControlSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/HELIOS.Fabric.Worker.csproj`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/LinearSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/SharePointEvidenceSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/SlackSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/TeamsSink.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/WorkerProgram.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.Worker/WorkerServices.cs`
- `reference/helios-enterprise-automation-fabric/project/src/dotnet/HELIOS.Fabric.sln`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric/__init__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric/__main__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric/cli.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric_cli/__init__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric_cli/__main__.py`
- `reference/helios-enterprise-automation-fabric/project/src/helios_fabric_cli/cli.py`
- `reference/helios-enterprise-automation-fabric/project/tests/fixtures/deployment-plan.json`
- `reference/helios-enterprise-automation-fabric/project/tests/test_fabricctl.py`
- `reference/helios-enterprise-automation-fabric/promote-reviewed-sha.yml`
- `reference/helios-enterprise-automation-fabric/release-evidence.yml`
- `reference/helios-enterprise-automation-fabric/retention-labels.example.json`
- `reference/helios-enterprise-automation-fabric/reusable-build-event.yml`
- `reference/helios-enterprise-automation-fabric/routing-policy.json`
- `reference/helios-enterprise-automation-fabric/servicebus.bicep`
- `reference/helios-enterprise-automation-fabric/sharepoint-governance-sync.yml`
- `reference/helios-enterprise-automation-fabric/sharepoint-sync-manifest.json`
- `reference/helios-enterprise-automation-fabric/sharepoint_sync.py`
- `reference/helios-enterprise-automation-fabric/slack_bootstrap.py`
- `reference/helios-enterprise-automation-fabric/status-policy.json`
- `reference/helios-enterprise-automation-fabric/storage.bicep`
- `reference/helios-enterprise-automation-fabric/sync-map.json`
- `reference/helios-enterprise-automation-fabric/webhook-config.example.json`
- `reference/helios-enterprise-automation-fabric/workflow-contracts(1).json`
- `reference/helios-enterprise-automation-fabric/workflow-contracts.json`
- `reference/helios-enterprise-automation-fabric/workflow-outcome-router.yml`

## origin/codex/automate-merge-and-fix-processes-41fypd

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `5e5e3e98713e40c9c7cb57bf928bc1cbab501752` — Add HELIOS merge automation workflow — Yolkster64 <thepatman64@gmail.com>
- `ea6df3ec01d90905e12d87ed991acbc785a449de` — Add consolidation source manifest — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.gitmodules`
- `MERGE_SOURCE_MANIFEST.yaml`
- `docs/automation/BRANCH_CONSOLIDATION_GUIDE.md`
- `scripts/dev/helios_merge_automation.py`

## origin/codex/add-shared-tool-resolution-feature

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `325fc1cc50f6edecb8cd1476804f7ee910d6bd96` — Add shared repo-local tool resolution — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.gitignore`
- `config/build-graph.json`
- `scripts/build_graph/build_graph.py`
- `scripts/common/tool_resolver.py`
- `scripts/integrations/full_stack_readiness.py`

## origin/codex/set-up-azure-and-experiment-with-copilot

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `2fc8582fbf1f257a812d26ae58c9618f083b3033` — Add HELIOS Azure automation bootstrap — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.gitignore`
- `microsoft-ecosystem/copilot/COPILOT_STUDIO_HELIOS_AUTOMATION.md`
- `microsoft-ecosystem/copilot/copilot-studio-helios-actions.template.json`
- `scripts/microsoft-enterprise/setup-helios-azure-automation.ps1`

## origin/yolkster64-scaling-system

- **Primary umbrella:** Hermes/XCore fleet (`hermes-xcore-fleet`)
- **Temporary integration branch:** `integration/train-hermes-xcore-fleet`
- **Module owner:** HELIOS.Hermes, XCore
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `6e9ae4ee869e8093abe6d96a4a210a1a4a24fca0` — Enforce MCP toolset headers and fix schema path base — Copilot App <223556219+Copilot@users.noreply.github.com>
- `3bcd34222682994824c4792998f2585dfd1acc54` — Address least-privilege review feedback — Copilot App <223556219+Copilot@users.noreply.github.com>
- `2f163f89a39187b32732eae6c7be1f3cbc502a7c` — fix(review): tighten MCP allowlists and fleet registry resolution — Copilot App <223556219+Copilot@users.noreply.github.com>
- `cfca3a69deca6c123ceaf68e78ada70d17335dfb` — test(ci): update test ownership manifest for new fleet tests — Copilot App <223556219+Copilot@users.noreply.github.com>
- `dfaa9b60128e58288f63586e093ed77fd03a7761` — feat(helios-control): add enterprise sub-agent fleet contracts — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4ffdd3ddd055b5029a04a98f9b3ab73c16efd8ec` — Revert ineffective Azure pipeline checkout workaround — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `07ebab3039c312eec4ac0a757ba9b568cb82f5d6` — Avoid Azure checkout in branch-policy validation pipeline — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `391bb711afadad1310434fb2e13c679eb96227ac` — Harden Azure pipeline trigger and checkout steps — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `azure-pipelines.yml`
- `eng/test/test-ownership.json`
- `monado/helios-control/.vscode/tasks.json`
- `monado/helios-control/README.md`
- `monado/helios-control/config/agent-fleet.json`
- `monado/helios-control/config/custom-mcp-connector-plane.json`
- `monado/helios-control/config/enterprise-sub-agent-fleet.json`
- `monado/helios-control/config/integrations.json`
- `monado/helios-control/config/microsoft-agents.json`
- `monado/helios-control/docs/IMPLEMENTATION_STATUS.md`
- `monado/helios-control/docs/MULTI_AGENT_WORKBENCH.md`
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `monado/helios-control/scripts/Start-HeliosLocalFleet.ps1`
- `monado/helios-control/src/Helios.Connect.Contracts/EnterpriseSubAgentFleetContracts.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/EnterpriseSubAgentFleetRegistryTests.cs`
- `plugins/helios-control-fabric/.mcp.json`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/test_helios.py`
- `plugins/helios-control-fabric/skills/helios-control/SKILL.md`

## origin/codex/add-automatic-python-compile-checker

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `d5f2117672b87f936f65787d780e7bad9721a345` — Add automatic Python compile checker — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `config/build-graph.json`
- `scripts/control/python_static.py`

## origin/codex/add-build-graph-option-for-readiness

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `57ac80c9e9ea2b0ce578122be74930b84725dbfe` — Add build graph readiness verify option — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `helios.sh`
- `scripts/build_graph/build_graph.py`

## origin/codex/create-c#-native-smoke-test-script

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `bd8111e53136d9234a0c9107febefa96939d4657` — Add C# native interop smoke runner — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `config/build-graph.json`
- `scripts/native/run_csharp_native_smoke.py`

## origin/codex/edit-helios.sh-for-remote-priority-case

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `2ed4ca13ccbe204be45d481f65a871d43a25bc35` — Make remote inventory mode read-only — Yolkster64 <thepatman64@gmail.com>
- `567dc85ce8463ceaae935c55f3e112c58fcf7e05` — Honor remote inventory-only mode — Yolkster64 <thepatman64@gmail.com>
- `7b946539c887525a210a856009e0e0dd214da3f1` — Add remote priority branch command — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `helios.sh`
- `scripts/analysis/branch_intelligence.py`

## origin/codex/perform-branch-integration-and-report

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `b7ff075dae7c820c6ab3e5b5fe0ff14475148f52` — Add HELIOS Hermes integration report — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `reports/branch-integration-2026-06-15.md`
- `scripts/cloud-orchestration/setup-azure-cli.sh`

## origin/codex/refine-build-graph-changed-file-matching

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `1ab6a1e521b1390540d5e2b6435973da8415abbb` — Refine build graph changed-file matching — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `config/build-graph.json`
- `scripts/build_graph/build_graph.py`

## origin/codex/update-build_graph.py-and-helios.sh

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `663dd5034144de875a8d01556c90e287fed766d1` — Add verify readiness build graph option — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `helios.sh`
- `scripts/build_graph/build_graph.py`

## origin/codex/update-helios.sh-with-remote-priority-command

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `2d70bd72389a5411102363a80e81af4095af8b35` — Add remote priority command — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `helios.sh`
- `scripts/analysis/branch_intelligence.py`

## origin/codex/update-setup-documentation-for-helios-stack

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `8e33bc580e27fa59d127a4ed40a1f7c81a189899` — Document HELIOS Hermes full stack setup — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `GETTING_STARTED.md`
- `README.md`
- `scripts/dev/Validate-Setup.ps1`
- `scripts/dev/codespace-launch.ps1`
- `scripts/dev/devsetup.sh`
- `scripts/dev/validate-setup.sh`

## origin/yolkster64-redesigned-waddle

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `4f8641b9c4f6e55a3844dcbc42f85d57a621de68` — docs: align setup guidance with benchmark and scoring safeguards — Copilot App <223556219+Copilot@users.noreply.github.com>
- `1aa87ae14cad73e39871dd7d2e1a02c71c86dfdd` — docs: align setup commands with workflow reality — Copilot App <223556219+Copilot@users.noreply.github.com>
- `652d59f95edb8a9b3b868dde02eb778aacc18959` — docs: add recency-sorted triage and active stream anchors — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4e231b81961e2385fc2696b04ae2f90898fc6df7` — docs: extend copilot setup with benchmark scoring and merge-prune flow — Copilot App <223556219+Copilot@users.noreply.github.com>
- `aca2be201c156d04ef1f8b7f2982c158af59330d` — docs: expand HELIOS Copilot setup and merge-readiness instructions — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `.github/copilot-instructions.md`
- `scripts/analysis/branch_intelligence.py`
- `scripts/analysis/complex_code_grading.py`
- `scripts/analysis/prune_generated_artifacts.py`

## origin/codex/optimize-repository-after-massive-merge

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `5fd860de6bff47af4489c8d3e9b74f1290002291` — Add repository optimization audit setup — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `scripts/dev/devsetup.sh`
- `scripts/dev/repo-optimize.sh`

## origin/codex/update-full_stack_readiness.py-for-tool-checks

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `2c09f0b484126fb004c68526da933de8b81f68e3` — Add full stack readiness tool split — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `scripts/integrations/full_stack_readiness.py`

## origin/copilot/setup-and-combine-all

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `2c09f0b484126fb004c68526da933de8b81f68e3` — Add full stack readiness tool split — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `scripts/integrations/full_stack_readiness.py`

## origin/yolkster64-improved-sniffle

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `0e6e02fa57d45c7483690720b013008f932538a4` — Narrow CI trigger to exclude unbuilt linked test sources — Copilot App <223556219+Copilot@users.noreply.github.com>
- `46e25840a4a7bfd5ec02dbda45e299d6d1b4335c` — Address PR review feedback for Azure DevOps CI scope — Copilot App <223556219+Copilot@users.noreply.github.com>
- `6f209ed54c09fef2096285044c51ccb3453d4e64` — Set up Azure DevOps CI preflight and scoped build — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `azure-pipelines.yml`

## origin/yolkster64-laughing-giggle

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `fb40f2af5cb30c508aedfbe1ea97b9a7cc999f38` — Fix package contract assertions in Azure validation lane — Copilot App <223556219+Copilot@users.noreply.github.com>
- `169a3bd498c4ba9655722c05e639d2bf3c756b33` — Set up Helios Azure pipeline validation lane — Copilot App <223556219+Copilot@users.noreply.github.com>
- `4ffdd3ddd055b5029a04a98f9b3ab73c16efd8ec` — Revert ineffective Azure pipeline checkout workaround — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `07ebab3039c312eec4ac0a757ba9b568cb82f5d6` — Avoid Azure checkout in branch-policy validation pipeline — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `391bb711afadad1310434fb2e13c679eb96227ac` — Harden Azure pipeline trigger and checkout steps — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `azure-pipelines.yml`

## origin/codex/add-owner-governance-baseline-documentation

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `5d76ff3b2ee74100dd14c7ab7a680f6a2f2b79eb` — Add owner governance baseline — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.github/PULL_REQUEST_TEMPLATE/integration-merge.md`
- `.github/README.md`
- `.github/pages/index.html`
- `README.md`
- `docs/setup/OWNER_START_HERE.md`

## origin/codex/update-helios.repositoryanalytics-handling

- **Primary umbrella:** F# analytics and prediction (`fsharp-analytics-prediction`)
- **Temporary integration branch:** `integration/train-fsharp-analytics-prediction`
- **Module owner:** HELIOS.Analytics.FSharp
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `fcf008f678f111a17d6c4e68990c3b803aae1fe0` — Add repository analytics Hermes empty fallback — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `src/tools/HELIOS.RepositoryAnalytics/HELIOS.RepositoryAnalytics.csproj`
- `src/tools/HELIOS.RepositoryAnalytics/Program.cs`

## origin/develop

- **Primary umbrella:** C++ native performance and security (`cpp-native-performance-security`)
- **Temporary integration branch:** `integration/train-cpp-native-performance-security`
- **Module owner:** HELIOS.Native, HELIOS.Security
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `c90a944066d745a29c0beb37ca2375eb729ae999` — Phase 2 Build Fixes - Core Infrastructure Compiling — Copilot <copilot@github.com>
- `849d6f22ee9ff4d2dcb78655c7f1fd3fbb78735c` — [CONTINUOUS WORK] PHASE 2 LAUNCH - 5 Parallel Optimization Streams — Copilot <copilot@github.com>
- `1121ce8062a48e18bdf10a75a9226d78d3b6229b` — final: hour 5-6 autonomous work completion verification — Copilot <copilot@github.com>
- `81e6230b0b328be7bde9ca1c00db257afe5714e5` — [AUTONOMOUS HOUR:10-12] FINAL RELEASE - v3.6.1-COMPLETE PRODUCTION READY — Copilot <copilot@github.com>
- `fd268ae982c1f5e16011ba296944c2cc97f217c3` — docs: hour 5-6 completion report for async/await optimization — Copilot <copilot@github.com>
- `8d3a7dc832d31387c4f93bdaabd7ae9163a75375` — [AUTONOMOUS HOUR:9-10] Final Release Preparation - v3.6.1-COMPLETE Ready — Copilot <copilot@github.com>
- `eb48e9c690976810424c855a968d4664dc394664` — docs: Add Autonomous Hour 6-8 completion summary — Copilot <copilot@github.com>
- `7039e9c8551a448872db9a34d0d503ccb4469098` — [AUTONOMOUS HOUR:6-7] Documentation: Performance Optimization Guide + Hour 7-12 Plan — Copilot <copilot@github.com>
- `0c28f51c3743db9cf7b4be31d7f0c906a3992e3c` — feat: Add comprehensive performance benchmark suite for Hour 8 autonomous optimization — Copilot <copilot@github.com>
- `3e3ab9a0ed9ed045d52f6ac66fdc4a1c47a77cd4` — [AUTONOMOUS HOUR:5-6] opt-004: Async Pipeline Pattern Implementation — Copilot <copilot@github.com>
- `c9ce29dfe936e78c82ce7c3c6327d48b58f96abc` — [AUTONOMOUS HOUR:2-4] opt-002: Object Pooling + opt-003: Database Connection Pooling complete — Copilot <copilot@github.com>
- `4387e20aa24262bba354a3eb6dbed6f36ef97b72` — [AUTONOMOUS HOUR:1-2] opt-001: Task Batch Accumulator implementation complete — Copilot <copilot@github.com>
- `c40b197d029104854420234717e59fbcf679f217` — ✅ PHASE 5 COMPLETE - Visual Polish & Animations (30 hours) — Copilot <copilot@github.com>
- `02a85b0167b00b12536fb4108353736f5804a56d` — 🌟 PHASE 5 EXECUTION - Visual Polish & Animations (25 of 30 hours) — Copilot <copilot@github.com>
- `acff888f6e6f29ec3aff61ba76e2d4d17a25d562` — ✅ PHASE 4 COMPLETE - XAML Styling & Data Binding (35 hours) — Copilot <copilot@github.com>
- `3bec4d32a36d0fc0be4b029a2c74680bd0e7b687` — 🎨 PHASE 4 EXECUTION STARTED - XAML Styling & Data Binding Foundation — Copilot <copilot@github.com>
- `afad0cd70d9cacede7b420b9eda93b415bf7dc94` — 🎯 v3.4.0 PHASES 4-7 EXECUTION PLAN READY - 100 Hours Remaining — Copilot <copilot@github.com>
- `443e20cf61c7421fd3b61c44a24987e805598c65` — 📊 v3.4.0 PHASES 1-3 DELIVERY COMPLETE - 125+ Hours Delivered — Copilot <copilot@github.com>
- `0b87c5ac48028378c4d2ebdbefa916b09abc31ce` — feat(v3.4.0): Phase 3 complete - Advanced Components & Dashboard UI — Copilot <copilot@github.com>
- `3d9975cb52a57201f9b27ce1da6885ab97a5ba80` — feat(v3.4.0): Phase 2 complete - Core Component Library and Progress Tracking — Copilot <copilot@github.com>
- `f08bfcabac7fe15dbcdc0fc4f62dd42b629d3b28` — feat(v3.4.0): Add foundation systems - Design, Animations, Dashboard, System Integration — Copilot <copilot@github.com>
- `cb64f733ce26cba64729a6b425f7f3aa720cf3c5` — 🚀 BEGIN v3.4.0 - Next Generation UI/UX and System Integration — Copilot <copilot@github.com>
- `b5edf1ec45d1ca04da5b0bf5f9dc17f5e852fd44` — refactor: Move Week2AsyncBatching reference implementation to docs — Copilot <copilot@github.com>
- `4966718db39442685bc36a62151815ec1c25c7a6` — 📖 Add comprehensive campaign guide for all stakeholders — Copilot <copilot@github.com>
- `f81532906db407fa7b16c0abe6da1924b6463b2f` — 📋 EXECUTIVE HANDOFF - Phase 3 Ready for Product Leadership — Copilot <copilot@github.com>
- `8fa17070f3c3001f436db24c83e799feccebc548` — 🎉 FINAL DEPLOYMENT STATUS - Monado Blade v3.3.0+ Ready for Execution — Copilot <copilot@github.com>
- `24db8183a72104d901dafae2d989d4f0fa93682b` — Add Phase 3 Week 2 implementation guide and Weeks 3-6 execution matrix — Copilot <copilot@github.com>
- `c8df4902e000c96c84bfa968f2c7a49e71815dd9` — 🎉 CAMPAIGN FINAL SUMMARY - Monado Blade v3.3.0+ Optimization Complete — Copilot <copilot@github.com>
- `b8e5a9ce263fa094ccefe4ec5d9b167631ad47a0` — Add Phase 3 complete execution guide and master dashboard — Copilot <copilot@github.com>
- `05d43eef11daae078adae887d1a47c2b5b691b4a` — Phase3 Week 2-6 optimization planning and test suite — Copilot <copilot@github.com>
- `0b91a9809428916c2285fb4e862f42d4885f126d` — 🎉 HERMES AGENT PARALLEL DEPLOYMENT - FINAL COMPLETION REPORT — Copilot <copilot@github.com>
- `5f2d921bf5cbc3d354241fad5b86e60f35a0ee85` — COMPLETION: Monado Blade v3.3.0 parallel deployment - Final status & next steps — Copilot <copilot@github.com>
- `ee47f69d7b9dfa323c518dacd8a0dbd0f273dec6` — DASHBOARD: Monado Blade v3.3.0 deployment status - All systems GO — Copilot <copilot@github.com>
- `505a45126a69c1f4873319aa798ae118d2d38544` — FINAL: Parallel deployment execution complete - all 3 tracks ready — Copilot <copilot@github.com>
- `9d7c4165a415af3e56032ff5b689e1ac9c4f9171` — docs: Add Hermes agent live execution status - Real-time deployment orchestration — Copilot <copilot@github.com>
- `87deb445c3fc30f41cbf8057ef71358b5ad0c483` — MONADO-BLADE-v3.3.0: Parallel deployment complete - Tracks A, B, C ready — Copilot <copilot@github.com>
- `90c6607274eb6b4d985ac80063fd8cd6fb2c4933` — docs: Add master execution report - Phase 1-2-3 parallel deployment tracking — Copilot <copilot@github.com>
- `d1cbd49f26f9a2acadf5fe7fc0250050c7b6e372` — docs: Add real-time execution dashboard - Phase 1-2-3 parallel deployment — Copilot <copilot@github.com>
- `93434f90bd1809a82360529c93ccf103db145761` — docs: Add comprehensive deployment manifest - Complete checklist for production — Copilot <copilot@github.com>
- `81d9ea719845367c481ce54221c72d0fb5652e60` — docs: Add deployment README - Quick start guide for production deployment — Copilot <copilot@github.com>
- `1bad74f9ccacce5046d99290afd19727885049ea` — docs: Campaign completion executive summary - Ready for production deployment — Copilot <copilot@github.com>
- `59f446a8ac1879af6c1291c752a850b808005411` — tool: Add deployment orchestrator PowerShell script — Copilot <copilot@github.com>
- `62da7b6ac0acfc41648e7e730cd75591241e864e` — docs: Complete deployment and Phase 3 execution documentation — Copilot <copilot@github.com>
- `4cf5b98b61dbc10e58365e18545147e0b989fabb` — docs: v3.3.0 deployment execution summary - Phase 1-2 ready for immediate deployment — Copilot <copilot@github.com>
- `6619a9810de3b900ae221bb51b006c8035616b0e` — OPTIMIZATION 4: Cache Invalidation Optimization — Copilot <copilot@github.com>
- `d29375303a1cb9da5aa347967de41c1063d4be62` — DOCS: Add main README for Optimization 5 — Copilot <copilot@github.com>
- `2a73c3aa9060b5e5519539978ab30a95b2c7f1af` — feat(concurrency): Implement async task batching engine with 15% throughput improvement — Copilot <copilot@github.com>
- `b16c977ef3490236d99f2452260c556cb1a73927` — DOCS: Add comprehensive documentation for Optimization 5 — Copilot <copilot@github.com>
- `fa572742e9f1d64cab2c91eac89c86b5f8e28da9` — feat: Implement OPTIMIZATION 3 - Message Coalescing for Monado Blade — Copilot <copilot@github.com>
- `31312168a8540db39e4501c04c2764fa23b544d8` — OPTIMIZATION-5: Implement lock-free collections (+16% throughput, -90% contention) — GitHub Actions <actions@github.com>
- `08412be8498f918ad04b1aa4f09fcc146dbe790c` — docs: Add README for v3.3.0 deployment preparation — GitHub Actions <actions@github.com>
- `080f7eb91bfe09e0f37da714678d32fc18553c77` — docs: Add comprehensive deployment documentation index — GitHub Actions <actions@github.com>
- `bfdfc98df4a0b181aaed038d6c5c25286d269ac8` — docs: Add v3.3.0 deployment preparation summary — GitHub Actions <actions@github.com>
- `01c0efb676daef2e58d8e453f44bb1c83cf71c54` — docs: Add comprehensive v3.3.0 deployment infrastructure — GitHub Actions <actions@github.com>
- `2c92c24f3cfc45fadf1ee85966fb5dbfc188ceda` — Documentation: v3.1.0 feature complete - dual-boot, auto-recovery, cloud sync — Copilot <223556219+copilot@users.noreply.github.com>
- `dc5ebfb60912319a3cb9a0c8da9cf0d21cfe7eaa` — Features: Dual-boot wizard - Windows + Monado coexistence — Copilot <223556219+copilot@users.noreply.github.com>
- `fda98a8c08dcd09ff659c4a2325841e60e2337f4` — Docs: Bootstrap quickstart guide and API reference — Copilot <copilot@github.com>
- `466ecc465b4e62161a6a28d7df9d4bea00efe798` — USB: v3.0 simplification - one-page wizard — Copilot <copilot@github.com>
- `f4e8f7b98eb28d73395d354ab9d6d083051ff43e` — Docs: Stream D bootstrap automation complete — Copilot <copilot@github.com>
- `465818df61a5e5d776a7f80423b3fc94692e4edb` — Bootstrap: Unit tests for bootstrap automation — Copilot <copilot@github.com>
- `54e6f68abc975be5fca2ed7fdebad9a704fc587b` — Bootstrap: Pre-flight system checks — Copilot <copilot@github.com>
- `d23bbed015f7da70fd7ab9d8e9901f6ad34495c1` — docs: Add Phase 4 Final Deliverable Summary — Copilot <copilot@github.com>
- `65c731e972da68a26b1efa7a0e01e34206b26f09` — docs: Add Phase 4 Continuous Optimization Execution Report — Copilot <copilot@github.com>
- `790a0840ebb3574b9f5405f96c1fdb9aaa2a9a2f` — Optimize: Phase 4 Continuous - automated tuning, self-healing, load prediction, cost optimization — Copilot <copilot@github.com>

### Patch-equivalent commits
- `023083e54e9283cc8f7ace7c09ef2079e23264b4` — USB: Auto-build in background with intelligent caching — Copilot <copilot@github.com>

### Files
- `MONITORING_PHASE2_COMPLETION.md`
- `MONITORING_PHASE2_EXECUTION_CHECKLIST.md`
- `MonadoBlade.sln`
- `PHASE2_ALL_STREAMS_COMPLETE.md`
- `PHASE2_COMPLETION_DASHBOARD.md`
- `PHASE2_COMPLETION_SUMMARY.md`
- `PHASE2_EXECUTIVE_SUMMARY.txt`
- `PHASE2_FINAL_DELIVERY.md`
- `PHASE2_INDEX.md`
- `PHASE2_PARALLEL_EXECUTION_REPORT.md`
- `SECURITY_STREAM_PHASE2_EXECUTION_REPORT.md`
- `b.log`
- `b2.log`
- `build.log`
- `build2.log`
- `build3.log`
- `build4.log`
- `src/HELIOS.Platform/AIHubConnector.cs`
- `src/HELIOS.Platform/API/HELIOSControllers.cs`
- `src/HELIOS.Platform/Database/HELIOSMigration.cs`
- `src/HELIOS.Platform/HermesIntegration.cs`
- `src/HELIOS.Platform/PatternBroker.cs`
- `src/MonadoBlade.Audio/bin/Release/net8.0/MonadoBlade.Audio.deps.json`
- `src/MonadoBlade.Audio/bin/Release/net8.0/MonadoBlade.Audio.dll`
- `src/MonadoBlade.Audio/bin/Release/net8.0/MonadoBlade.Audio.pdb`
- `src/MonadoBlade.Audio/bin/Release/net8.0/MonadoBlade.Audio.xml`
- `src/MonadoBlade.Audio/bin/Release/net8.0/MonadoBlade.Core.dll`
- `src/MonadoBlade.Audio/bin/Release/net8.0/MonadoBlade.Core.pdb`
- `src/MonadoBlade.Audio/bin/Release/net8.0/MonadoBlade.Core.xml`
- `src/MonadoBlade.Audio/obj/Debug/net8.0/MonadoBlade.Audio.assets.cache`
- `src/MonadoBlade.Audio/obj/Release/net8.0/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBl.4C1C4F3E.Up2Date`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBlade.Audio.AssemblyInfo.cs`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBlade.Audio.AssemblyInfoInputs.cache`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBlade.Audio.GeneratedMSBuildEditorConfig.editorconfig`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBlade.Audio.csproj.AssemblyReference.cache`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBlade.Audio.csproj.CoreCompileInputs.cache`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBlade.Audio.csproj.FileListAbsolute.txt`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBlade.Audio.dll`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBlade.Audio.pdb`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBlade.Audio.sourcelink.json`
- `src/MonadoBlade.Audio/obj/Release/net8.0/MonadoBlade.Audio.xml`
- `src/MonadoBlade.Audio/obj/Release/net8.0/ref/MonadoBlade.Audio.dll`
- `src/MonadoBlade.Audio/obj/Release/net8.0/refint/MonadoBlade.Audio.dll`
- `src/MonadoBlade.Boot/bin/Release/net8.0/MonadoBlade.Boot.deps.json`
- `src/MonadoBlade.Boot/bin/Release/net8.0/MonadoBlade.Boot.dll`
- `src/MonadoBlade.Boot/bin/Release/net8.0/MonadoBlade.Boot.pdb`
- `src/MonadoBlade.Boot/bin/Release/net8.0/MonadoBlade.Boot.xml`
- `src/MonadoBlade.Boot/bin/Release/net8.0/MonadoBlade.Core.dll`
- `src/MonadoBlade.Boot/bin/Release/net8.0/MonadoBlade.Core.pdb`
- `src/MonadoBlade.Boot/bin/Release/net8.0/MonadoBlade.Core.xml`
- `src/MonadoBlade.Boot/obj/Debug/net8.0/MonadoBlade.Boot.assets.cache`
- `src/MonadoBlade.Boot/obj/Release/net8.0/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBl.1B18A18E.Up2Date`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBlade.Boot.AssemblyInfo.cs`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBlade.Boot.AssemblyInfoInputs.cache`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBlade.Boot.GeneratedMSBuildEditorConfig.editorconfig`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBlade.Boot.csproj.AssemblyReference.cache`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBlade.Boot.csproj.CoreCompileInputs.cache`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBlade.Boot.csproj.FileListAbsolute.txt`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBlade.Boot.dll`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBlade.Boot.pdb`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBlade.Boot.sourcelink.json`
- `src/MonadoBlade.Boot/obj/Release/net8.0/MonadoBlade.Boot.xml`
- `src/MonadoBlade.Boot/obj/Release/net8.0/ref/MonadoBlade.Boot.dll`
- `src/MonadoBlade.Boot/obj/Release/net8.0/refint/MonadoBlade.Boot.dll`
- `src/MonadoBlade.Core/Async/AsyncHelpers.cs`
- `src/MonadoBlade.Core/Async/AsyncPipeline.cs`
- `src/MonadoBlade.Core/Caching/CacheInvalidationPatterns.cs`
- `src/MonadoBlade.Core/Data/ConnectionHealthEnhancements.cs.bak`
- `src/MonadoBlade.Core/Data/ConnectionHealthMonitor.cs.bak`
- `src/MonadoBlade.Core/Data/ConnectionPool.cs`
- `src/MonadoBlade.Core/Data/DataAccessLayer.cs`
- `src/MonadoBlade.Core/Data/DatabaseIndexOptimizer.cs`
- `src/MonadoBlade.Core/Data/QueryBatchExecutor.cs`
- `src/MonadoBlade.Core/Data/QueryCacheLayer.cs`
- `src/MonadoBlade.Core/Database/QueryOptimization.cs`
- `src/MonadoBlade.Core/DependencyInjection/ServiceCollectionExtensions.cs.bak`
- `src/MonadoBlade.Core/HELIOS/AIHubConnector.cs`
- `src/MonadoBlade.Core/HELIOS/HermesIntegration.cs`
- `src/MonadoBlade.Core/ObjectPooling/GenericObjectPool.cs`
- `src/MonadoBlade.Core/Observability/AdvancedMetrics.cs`
- `src/MonadoBlade.Core/Optimization/AsyncPipelineV2.cs`
- `src/MonadoBlade.Core/Optimization/TaskBatchAccumulator.cs.bak`
- `src/MonadoBlade.Core/Security/SecurityHardening.cs`
- `src/MonadoBlade.Core/Services/CloudSyncService.cs`
- `src/MonadoBlade.Core/Services/MLService.cs.bak`
- `src/MonadoBlade.Core/Services/MutationService.cs`
- `src/MonadoBlade.Core/Services/PluginService.cs`
- `src/MonadoBlade.Core/Services/QueryService.cs`
- `src/MonadoBlade.Core/Services/SubscribeService.cs.bak`
- `src/MonadoBlade.Core/SystemIntegration/WindowsSystemBridge.cs.bak`
- `src/MonadoBlade.Core/UI/AppStateManagement.cs`
- `src/MonadoBlade.Core/bin/Release/net8.0/MonadoBlade.Core.deps.json`
- `src/MonadoBlade.Core/bin/Release/net8.0/MonadoBlade.Core.dll`
- `src/MonadoBlade.Core/bin/Release/net8.0/MonadoBlade.Core.pdb`
- `src/MonadoBlade.Core/bin/Release/net8.0/MonadoBlade.Core.xml`
- `src/MonadoBlade.Core/obj/Debug/net8.0/MonadoBlade.Core.AssemblyInfo.cs`
- `src/MonadoBlade.Core/obj/Debug/net8.0/MonadoBlade.Core.AssemblyInfoInputs.cache`
- `src/MonadoBlade.Core/obj/Debug/net8.0/MonadoBlade.Core.GeneratedMSBuildEditorConfig.editorconfig`
- `src/MonadoBlade.Core/obj/Debug/net8.0/MonadoBlade.Core.assets.cache`
- `src/MonadoBlade.Core/obj/Debug/net8.0/MonadoBlade.Core.csproj.AssemblyReference.cache`
- `src/MonadoBlade.Core/obj/Debug/net8.0/MonadoBlade.Core.csproj.CoreCompileInputs.cache`
- `src/MonadoBlade.Core/obj/Debug/net8.0/MonadoBlade.Core.csproj.FileListAbsolute.txt`
- `src/MonadoBlade.Core/obj/Debug/net8.0/MonadoBlade.Core.sourcelink.json`
- `src/MonadoBlade.Core/obj/Release/net8.0/MonadoBlade.Core.AssemblyInfo.cs`
- `src/MonadoBlade.Core/obj/Release/net8.0/MonadoBlade.Core.AssemblyInfoInputs.cache`
- `src/MonadoBlade.Core/obj/Release/net8.0/MonadoBlade.Core.csproj.CoreCompileInputs.cache`
- `src/MonadoBlade.Core/obj/Release/net8.0/MonadoBlade.Core.csproj.FileListAbsolute.txt`
- `src/MonadoBlade.Core/obj/Release/net8.0/MonadoBlade.Core.dll`
- `src/MonadoBlade.Core/obj/Release/net8.0/MonadoBlade.Core.pdb`
- `src/MonadoBlade.Core/obj/Release/net8.0/MonadoBlade.Core.sourcelink.json`
- `src/MonadoBlade.Core/obj/Release/net8.0/MonadoBlade.Core.xml`
- `src/MonadoBlade.Core/obj/Release/net8.0/ref/MonadoBlade.Core.dll`
- `src/MonadoBlade.Core/obj/Release/net8.0/refint/MonadoBlade.Core.dll`
- `src/MonadoBlade.Dashboard/bin/Release/net8.0-windows/MonadoBlade.Core.dll`
- `src/MonadoBlade.Dashboard/bin/Release/net8.0-windows/MonadoBlade.Core.pdb`
- `src/MonadoBlade.Dashboard/bin/Release/net8.0-windows/MonadoBlade.Core.xml`
- `src/MonadoBlade.Dashboard/bin/Release/net8.0-windows/MonadoBlade.Dashboard.deps.json`
- `src/MonadoBlade.Dashboard/bin/Release/net8.0-windows/MonadoBlade.Dashboard.dll`
- `src/MonadoBlade.Dashboard/bin/Release/net8.0-windows/MonadoBlade.Dashboard.pdb`
- `src/MonadoBlade.Dashboard/bin/Release/net8.0-windows/MonadoBlade.Dashboard.xml`
- `src/MonadoBlade.Dashboard/bin/Release/net8.0-windows/MonadoBlade.Graphics.dll`
- `src/MonadoBlade.Dashboard/bin/Release/net8.0-windows/MonadoBlade.Graphics.pdb`
- `src/MonadoBlade.Dashboard/bin/Release/net8.0-windows/MonadoBlade.Graphics.xml`
- `src/MonadoBlade.Dashboard/obj/Debug/net8.0-windows/MonadoBlade.Dashboard.assets.cache`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBl.7BC2C45D.Up2Date`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBlade.Dashboard.AssemblyInfo.cs`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBlade.Dashboard.AssemblyInfoInputs.cache`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBlade.Dashboard.GeneratedMSBuildEditorConfig.editorconfig`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBlade.Dashboard.csproj.AssemblyReference.cache`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBlade.Dashboard.csproj.CoreCompileInputs.cache`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBlade.Dashboard.csproj.FileListAbsolute.txt`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBlade.Dashboard.dll`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBlade.Dashboard.pdb`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBlade.Dashboard.sourcelink.json`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/MonadoBlade.Dashboard.xml`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/ref/MonadoBlade.Dashboard.dll`
- `src/MonadoBlade.Dashboard/obj/Release/net8.0-windows/refint/MonadoBlade.Dashboard.dll`
- `src/MonadoBlade.Developer/bin/Release/net8.0/MonadoBlade.Core.dll`
- `src/MonadoBlade.Developer/bin/Release/net8.0/MonadoBlade.Core.pdb`
- `src/MonadoBlade.Developer/bin/Release/net8.0/MonadoBlade.Core.xml`
- `src/MonadoBlade.Developer/bin/Release/net8.0/MonadoBlade.Developer.deps.json`
- `src/MonadoBlade.Developer/bin/Release/net8.0/MonadoBlade.Developer.dll`
- `src/MonadoBlade.Developer/bin/Release/net8.0/MonadoBlade.Developer.pdb`
- `src/MonadoBlade.Developer/bin/Release/net8.0/MonadoBlade.Developer.xml`
- `src/MonadoBlade.Developer/obj/Debug/net8.0/MonadoBlade.Developer.assets.cache`
- `src/MonadoBlade.Developer/obj/Release/net8.0/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBl.7BDF1269.Up2Date`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBlade.Developer.AssemblyInfo.cs`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBlade.Developer.AssemblyInfoInputs.cache`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBlade.Developer.GeneratedMSBuildEditorConfig.editorconfig`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBlade.Developer.csproj.AssemblyReference.cache`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBlade.Developer.csproj.CoreCompileInputs.cache`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBlade.Developer.csproj.FileListAbsolute.txt`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBlade.Developer.dll`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBlade.Developer.pdb`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBlade.Developer.sourcelink.json`
- `src/MonadoBlade.Developer/obj/Release/net8.0/MonadoBlade.Developer.xml`
- `src/MonadoBlade.Developer/obj/Release/net8.0/ref/MonadoBlade.Developer.dll`
- `src/MonadoBlade.Developer/obj/Release/net8.0/refint/MonadoBlade.Developer.dll`
- `src/MonadoBlade.GUI/Components/Base/ComponentBase.cs`
- `src/MonadoBlade.GUI/Components/Helpers/ResponsiveHelper.cs`
- `src/MonadoBlade.GUI/Components/Helpers/ThemeManager.cs`
- `src/MonadoBlade.GUI/Components/Helpers/ValidationHelper.cs`
- `src/MonadoBlade.GUI/Performance/DataGridVirtualizer.cs`
- `src/MonadoBlade.GUI/Performance/LazyLoadingManager.cs`
- `src/MonadoBlade.GUI/Performance/TokenCacheManager.cs`
- `src/MonadoBlade.GUI/StateManagement/AppStateManagement.cs`
- `src/MonadoBlade.GUI/StateManagement/Reducers/CloudSyncReducer.cs`
- `src/MonadoBlade.GUI/StateManagement/Reducers/DashboardReducer.cs`
- `src/MonadoBlade.GUI/StateManagement/Reducers/PluginReducer.cs`
- `src/MonadoBlade.GUI/StateManagement/Reducers/SettingsReducer.cs`
- `src/MonadoBlade.GUI/Themes/Animations.xaml`
- `src/MonadoBlade.GUI/Themes/ComponentStyles.xaml`
- `src/MonadoBlade.GUI/Themes/MonadoDark.xaml`
- `src/MonadoBlade.GUI/Themes/MonadoLight.xaml`
- `src/MonadoBlade.GUI/obj/Debug/net8.0-windows/MonadoBlade.GUI.GlobalUsings.g.cs`
- `src/MonadoBlade.GUI/obj/Debug/net8.0-windows/MonadoBlade.GUI.assets.cache`
- `src/MonadoBlade.GUI/obj/MonadoBlade.GUI.csproj.nuget.dgspec.json`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/App.g.cs`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MainWindow.baml`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MainWindow.g.cs`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MonadoBlade.GUI.AssemblyInfo.cs`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MonadoBlade.GUI.AssemblyInfoInputs.cache`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MonadoBlade.GUI.GeneratedMSBuildEditorConfig.editorconfig`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MonadoBlade.GUI.assets.cache`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MonadoBlade.GUI.csproj.AssemblyReference.cache`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MonadoBlade.GUI.csproj.CoreCompileInputs.cache`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MonadoBlade.GUI.csproj.FileListAbsolute.txt`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MonadoBlade.GUI.sourcelink.json`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MonadoBlade.g.resources`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/MonadoBlade_MarkupCompile.cache`
- `src/MonadoBlade.GUI/obj/Release/net8.0-windows/Themes/MonadoLight.baml`
- `src/MonadoBlade.GUI/obj/project.assets.json`
- `src/MonadoBlade.GUI/obj/project.nuget.cache`
- `src/MonadoBlade.Graphics/bin/Release/net8.0-windows/MonadoBlade.Core.dll`
- `src/MonadoBlade.Graphics/bin/Release/net8.0-windows/MonadoBlade.Core.pdb`
- `src/MonadoBlade.Graphics/bin/Release/net8.0-windows/MonadoBlade.Core.xml`
- `src/MonadoBlade.Graphics/bin/Release/net8.0-windows/MonadoBlade.Graphics.deps.json`
- `src/MonadoBlade.Graphics/bin/Release/net8.0-windows/MonadoBlade.Graphics.dll`
- `src/MonadoBlade.Graphics/bin/Release/net8.0-windows/MonadoBlade.Graphics.pdb`
- `src/MonadoBlade.Graphics/bin/Release/net8.0-windows/MonadoBlade.Graphics.xml`
- `src/MonadoBlade.Graphics/obj/Debug/net8.0-windows/MonadoBlade.Graphics.assets.cache`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBl.DF78E560.Up2Date`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBlade.Graphics.AssemblyInfo.cs`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBlade.Graphics.AssemblyInfoInputs.cache`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBlade.Graphics.GeneratedMSBuildEditorConfig.editorconfig`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBlade.Graphics.csproj.AssemblyReference.cache`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBlade.Graphics.csproj.CoreCompileInputs.cache`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBlade.Graphics.csproj.FileListAbsolute.txt`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBlade.Graphics.dll`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBlade.Graphics.pdb`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBlade.Graphics.sourcelink.json`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/MonadoBlade.Graphics.xml`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/ref/MonadoBlade.Graphics.dll`
- `src/MonadoBlade.Graphics/obj/Release/net8.0-windows/refint/MonadoBlade.Graphics.dll`
- `src/MonadoBlade.Security/Abstractions/IEncryptionKeyManager.cs`
- `src/MonadoBlade.Security/Abstractions/IInputValidator.cs`
- `src/MonadoBlade.Security/Abstractions/ISecureAuditLogger.cs`
- `src/MonadoBlade.Security/EncryptionKeyManager.cs`
- `src/MonadoBlade.Security/MonadoBlade.Security.csproj`
- `src/MonadoBlade.Security/SecureAuditLogger.cs`
- `src/MonadoBlade.Security/SecureInputValidator.cs`
- `src/MonadoBlade.Security/bin/Release/net8.0/MonadoBlade.Core.dll`
- `src/MonadoBlade.Security/bin/Release/net8.0/MonadoBlade.Core.pdb`
- `src/MonadoBlade.Security/bin/Release/net8.0/MonadoBlade.Core.xml`
- `src/MonadoBlade.Security/bin/Release/net8.0/MonadoBlade.Security.deps.json`
- `src/MonadoBlade.Security/bin/Release/net8.0/MonadoBlade.Security.dll`
- `src/MonadoBlade.Security/bin/Release/net8.0/MonadoBlade.Security.pdb`
- `src/MonadoBlade.Security/bin/Release/net8.0/MonadoBlade.Security.xml`
- `src/MonadoBlade.Security/obj/Debug/net8.0/MonadoBlade.Security.assets.cache`
- `src/MonadoBlade.Security/obj/MonadoBlade.Security.csproj.nuget.dgspec.json`
- `src/MonadoBlade.Security/obj/Release/net8.0/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBl.96FE40D5.Up2Date`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.AssemblyInfo.cs`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.AssemblyInfoInputs.cache`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.GeneratedMSBuildEditorConfig.editorconfig`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.assets.cache`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.csproj.AssemblyReference.cache`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.csproj.CoreCompileInputs.cache`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.csproj.FileListAbsolute.txt`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.dll`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.pdb`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.sourcelink.json`
- `src/MonadoBlade.Security/obj/Release/net8.0/MonadoBlade.Security.xml`
- `src/MonadoBlade.Security/obj/Release/net8.0/ref/MonadoBlade.Security.dll`
- `src/MonadoBlade.Security/obj/Release/net8.0/refint/MonadoBlade.Security.dll`
- `src/MonadoBlade.Security/obj/project.assets.json`
- `src/MonadoBlade.Security/obj/project.nuget.cache`
- `src/MonadoBlade.Tools/bin/Release/net8.0/MonadoBlade.Core.dll`
- `src/MonadoBlade.Tools/bin/Release/net8.0/MonadoBlade.Core.pdb`
- `src/MonadoBlade.Tools/bin/Release/net8.0/MonadoBlade.Core.xml`
- `src/MonadoBlade.Tools/bin/Release/net8.0/MonadoBlade.Tools.deps.json`
- `src/MonadoBlade.Tools/bin/Release/net8.0/MonadoBlade.Tools.dll`
- `src/MonadoBlade.Tools/bin/Release/net8.0/MonadoBlade.Tools.pdb`
- `src/MonadoBlade.Tools/bin/Release/net8.0/MonadoBlade.Tools.xml`
- `src/MonadoBlade.Tools/obj/Debug/net8.0/MonadoBlade.Tools.assets.cache`
- `src/MonadoBlade.Tools/obj/Release/net8.0/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBl.D13F0D6E.Up2Date`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBlade.Tools.AssemblyInfo.cs`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBlade.Tools.AssemblyInfoInputs.cache`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBlade.Tools.GeneratedMSBuildEditorConfig.editorconfig`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBlade.Tools.csproj.AssemblyReference.cache`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBlade.Tools.csproj.CoreCompileInputs.cache`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBlade.Tools.csproj.FileListAbsolute.txt`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBlade.Tools.dll`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBlade.Tools.pdb`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBlade.Tools.sourcelink.json`
- `src/MonadoBlade.Tools/obj/Release/net8.0/MonadoBlade.Tools.xml`
- `src/MonadoBlade.Tools/obj/Release/net8.0/ref/MonadoBlade.Tools.dll`
- `src/MonadoBlade.Tools/obj/Release/net8.0/refint/MonadoBlade.Tools.dll`
- `src/monitoring/AdvancedMetricsCollector.cs`
- `src/monitoring/AnomalyDetectionHooks.cs`
- `src/monitoring/DistributedTracingManager.cs`
- `src/monitoring/MetricsQueryEngine.cs`
- `src/monitoring/MonadoBlade.Monitoring.csproj`
- `src/monitoring/bin/Release/net8.0/MonadoBlade.Monitoring.deps.json`
- `src/monitoring/bin/Release/net8.0/MonadoBlade.Monitoring.dll`
- `src/monitoring/bin/Release/net8.0/MonadoBlade.Monitoring.pdb`
- `src/monitoring/obj/MonadoBlade.Monitoring.csproj.nuget.dgspec.json`
- `src/monitoring/obj/MonadoBlade.Monitoring.csproj.nuget.g.props`
- `src/monitoring/obj/MonadoBlade.Monitoring.csproj.nuget.g.targets`
- `src/monitoring/obj/Release/net8.0/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `src/monitoring/obj/Release/net8.0/MonadoBlade.Monitoring.AssemblyInfo.cs`
- `src/monitoring/obj/Release/net8.0/MonadoBlade.Monitoring.AssemblyInfoInputs.cache`
- `src/monitoring/obj/Release/net8.0/MonadoBlade.Monitoring.GeneratedMSBuildEditorConfig.editorconfig`
- `src/monitoring/obj/Release/net8.0/MonadoBlade.Monitoring.assets.cache`
- `src/monitoring/obj/Release/net8.0/MonadoBlade.Monitoring.csproj.CoreCompileInputs.cache`
- `src/monitoring/obj/Release/net8.0/MonadoBlade.Monitoring.csproj.FileListAbsolute.txt`
- `src/monitoring/obj/Release/net8.0/MonadoBlade.Monitoring.dll`
- `src/monitoring/obj/Release/net8.0/MonadoBlade.Monitoring.pdb`
- `src/monitoring/obj/Release/net8.0/MonadoBlade.Monitoring.sourcelink.json`
- `src/monitoring/obj/Release/net8.0/ref/MonadoBlade.Monitoring.dll`
- `src/monitoring/obj/Release/net8.0/refint/MonadoBlade.Monitoring.dll`
- `src/monitoring/obj/project.assets.json`
- `src/monitoring/obj/project.nuget.cache`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/BouncyCastle.Crypto.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Castle.Core.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/CoverletSourceRootsMapping_MonadoBlade.Tests.Integration`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Docker.DotNet.X509.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Docker.DotNet.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/FluentAssertions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ICSharpCode.SharpZipLib.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Bcl.AsyncInterfaces.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.EntityFrameworkCore.Abstractions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.EntityFrameworkCore.Relational.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.EntityFrameworkCore.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Caching.Abstractions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Caching.Memory.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Configuration.Abstractions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Configuration.Binder.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Configuration.EnvironmentVariables.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Configuration.FileExtensions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Configuration.Json.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Configuration.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.DependencyInjection.Abstractions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.DependencyInjection.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.FileProviders.Abstractions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.FileProviders.Physical.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.FileSystemGlobbing.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Logging.Abstractions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Logging.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Options.ConfigurationExtensions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Options.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.Extensions.Primitives.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.TestPlatform.CommunicationUtilities.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.TestPlatform.CoreUtilities.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.TestPlatform.CrossPlatEngine.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.TestPlatform.PlatformAbstractions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.TestPlatform.Utilities.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.VisualStudio.CodeCoverage.Shim.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.VisualStudio.TestPlatform.Common.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Microsoft.VisualStudio.TestPlatform.ObjectModel.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Audio.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Audio.pdb`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Audio.xml`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Boot.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Boot.pdb`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Boot.xml`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Core.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Core.pdb`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Core.xml`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Dashboard.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Dashboard.pdb`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Dashboard.xml`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Developer.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Developer.pdb`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Developer.xml`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Graphics.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Graphics.pdb`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Graphics.xml`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Security.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Security.pdb`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Security.xml`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Tests.Integration.deps.json`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Tests.Integration.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Tests.Integration.pdb`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Tests.Integration.runtimeconfig.json`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Tools.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Tools.pdb`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/MonadoBlade.Tools.xml`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Moq.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/NAudio.Asio.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/NAudio.Core.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/NAudio.Midi.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/NAudio.Wasapi.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/NAudio.WinForms.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/NAudio.WinMM.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/NAudio.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Newtonsoft.Json.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Renci.SshNet.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Serilog.Extensions.Logging.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Serilog.Sinks.Console.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Serilog.Sinks.File.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Serilog.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/SharpDX.DXGI.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/SharpDX.Direct3D11.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/SharpDX.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/SshNet.Security.Cryptography.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/Testcontainers.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/cs/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/cs/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/cs/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/cs/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/cs/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/de/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/de/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/de/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/de/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/de/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/es/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/es/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/es/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/es/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/es/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/fr/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/fr/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/fr/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/fr/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/fr/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/it/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/it/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/it/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/it/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/it/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ja/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ja/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ja/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ja/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ja/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ko/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ko/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ko/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ko/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ko/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/pl/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/pl/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/pl/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/pl/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/pl/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/pt-BR/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/pt-BR/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/pt-BR/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/pt-BR/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/pt-BR/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ru/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ru/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ru/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ru/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/ru/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/testhost.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/testhost.exe`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/tr/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/tr/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/tr/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/tr/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/tr/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/xunit.abstractions.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/xunit.assert.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/xunit.core.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/xunit.execution.dotnet.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/xunit.runner.reporters.netcoreapp10.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/xunit.runner.utility.netcoreapp10.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/xunit.runner.visualstudio.dotnetcore.testadapter.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/zh-Hans/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/zh-Hans/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/zh-Hans/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/zh-Hans/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/zh-Hans/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/zh-Hant/Microsoft.TestPlatform.CommunicationUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/zh-Hant/Microsoft.TestPlatform.CoreUtilities.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/zh-Hant/Microsoft.TestPlatform.CrossPlatEngine.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/zh-Hant/Microsoft.VisualStudio.TestPlatform.Common.resources.dll`
- `tests/MonadoBlade.Tests.Integration/bin/Release/net8.0-windows/zh-Hant/Microsoft.VisualStudio.TestPlatform.ObjectModel.resources.dll`
- `tests/MonadoBlade.Tests.Integration/obj/Debug/net8.0-windows/MonadoBlade.Tests.Integration.GlobalUsings.g.cs`
- `tests/MonadoBlade.Tests.Integration/obj/Debug/net8.0-windows/MonadoBlade.Tests.Integration.assets.cache`
- `tests/MonadoBlade.Tests.Integration/obj/MonadoBlade.Tests.Integration.csproj.nuget.dgspec.json`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBl.22751A0F.Up2Date`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.AssemblyInfo.cs`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.AssemblyInfoInputs.cache`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.GeneratedMSBuildEditorConfig.editorconfig`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.assets.cache`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.csproj.AssemblyReference.cache`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.csproj.CoreCompileInputs.cache`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.csproj.FileListAbsolute.txt`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.dll`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.genruntimeconfig.cache`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.pdb`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/MonadoBlade.Tests.Integration.sourcelink.json`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/ref/MonadoBlade.Tests.Integration.dll`
- `tests/MonadoBlade.Tests.Integration/obj/Release/net8.0-windows/refint/MonadoBlade.Tests.Integration.dll`
- `tests/MonadoBlade.Tests.Integration/obj/project.assets.json`
- `tests/MonadoBlade.Tests.Integration/obj/project.nuget.cache`
- `tests/MonadoBlade.Tests.Performance/obj/Debug/net8.0-windows/MonadoBlade.Tests.Performance.GlobalUsings.g.cs`
- `tests/MonadoBlade.Tests.Performance/obj/Debug/net8.0-windows/MonadoBlade.Tests.Performance.assets.cache`
- `tests/MonadoBlade.Tests.Performance/obj/MonadoBlade.Tests.Performance.csproj.nuget.dgspec.json`
- `tests/MonadoBlade.Tests.Performance/obj/Release/net8.0-windows/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `tests/MonadoBlade.Tests.Performance/obj/Release/net8.0-windows/MonadoBlade.Tests.Performance.AssemblyInfo.cs`
- `tests/MonadoBlade.Tests.Performance/obj/Release/net8.0-windows/MonadoBlade.Tests.Performance.AssemblyInfoInputs.cache`
- `tests/MonadoBlade.Tests.Performance/obj/Release/net8.0-windows/MonadoBlade.Tests.Performance.GeneratedMSBuildEditorConfig.editorconfig`
- `tests/MonadoBlade.Tests.Performance/obj/Release/net8.0-windows/MonadoBlade.Tests.Performance.assets.cache`
- `tests/MonadoBlade.Tests.Performance/obj/Release/net8.0-windows/MonadoBlade.Tests.Performance.csproj.AssemblyReference.cache`
- `tests/MonadoBlade.Tests.Performance/obj/Release/net8.0-windows/MonadoBlade.Tests.Performance.csproj.CoreCompileInputs.cache`
- `tests/MonadoBlade.Tests.Performance/obj/Release/net8.0-windows/MonadoBlade.Tests.Performance.csproj.FileListAbsolute.txt`
- `tests/MonadoBlade.Tests.Performance/obj/Release/net8.0-windows/MonadoBlade.Tests.Performance.sourcelink.json`
- `tests/MonadoBlade.Tests.Performance/obj/project.assets.json`
- `tests/MonadoBlade.Tests.Performance/obj/project.nuget.cache`
- `tests/MonadoBlade.Tests.Unit/Monitoring/MonitoringTests.cs`
- `tests/MonadoBlade.Tests.Unit/Optimization/CacheAndPipelineTests.cs`
- `tests/MonadoBlade.Tests.Unit/Security/SecurityAndMetricsTests.cs`
- `tests/MonadoBlade.Tests.Unit/Security/SecurityTests.cs`
- `tests/MonadoBlade.Tests.Unit/bin/Release/net8.0-windows/CoverletSourceRootsMapping_MonadoBlade.Tests.Unit`
- `tests/MonadoBlade.Tests.Unit/obj/Debug/net8.0-windows/MonadoBlade.Tests.Unit.assets.cache`
- `tests/MonadoBlade.Tests.Unit/obj/MonadoBlade.Tests.Unit.csproj.nuget.dgspec.json`
- `tests/MonadoBlade.Tests.Unit/obj/Release/net8.0-windows/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`
- `tests/MonadoBlade.Tests.Unit/obj/Release/net8.0-windows/MonadoBlade.Tests.Unit.AssemblyInfo.cs`
- `tests/MonadoBlade.Tests.Unit/obj/Release/net8.0-windows/MonadoBlade.Tests.Unit.AssemblyInfoInputs.cache`
- `tests/MonadoBlade.Tests.Unit/obj/Release/net8.0-windows/MonadoBlade.Tests.Unit.GeneratedMSBuildEditorConfig.editorconfig`
- `tests/MonadoBlade.Tests.Unit/obj/Release/net8.0-windows/MonadoBlade.Tests.Unit.assets.cache`
- `tests/MonadoBlade.Tests.Unit/obj/Release/net8.0-windows/MonadoBlade.Tests.Unit.csproj.AssemblyReference.cache`
- `tests/MonadoBlade.Tests.Unit/obj/Release/net8.0-windows/MonadoBlade.Tests.Unit.csproj.CoreCompileInputs.cache`
- `tests/MonadoBlade.Tests.Unit/obj/Release/net8.0-windows/MonadoBlade.Tests.Unit.csproj.FileListAbsolute.txt`
- `tests/MonadoBlade.Tests.Unit/obj/Release/net8.0-windows/MonadoBlade.Tests.Unit.sourcelink.json`
- `tests/MonadoBlade.Tests.Unit/obj/project.assets.json`
- `tests/MonadoBlade.Tests.Unit/obj/project.nuget.cache`
- `tests/Phase2StreamTests.cs`

## origin/architecture/helios-platform-2-foundation

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `695502900ee25985071ff065c9bf0434341378d2` — Add HELIOS Platform 2 implementation plan — Yolkster64 <thepatman64@gmail.com>
- `df60b162e645102e6b6a5b33fb3ef446e6a45d45` — Add HELIOS install and quarantine routing policies — Yolkster64 <thepatman64@gmail.com>
- `82dbc18e414de88ab3a53b45de3b6b9a2e0cae16` — Add canonical HELIOS profile policies — Yolkster64 <thepatman64@gmail.com>
- `7e747d8b7803f1f01078980aa7dd73c528afad70` — Add canonical two-drive storage layout — Yolkster64 <thepatman64@gmail.com>
- `0d7b31ea24eaeb129eaf26eb4f2c691cb3ba611d` — Add HELIOS Platform 2 architecture foundation — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `config/platform/install-routing.v2.json`
- `config/platform/profiles.v2.json`
- `config/platform/storage-layout.v2.json`
- `docs/architecture/HELIOS_PLATFORM_2_ARCHITECTURE.md`
- `docs/architecture/HELIOS_PLATFORM_2_IMPLEMENTATION_PLAN.md`

## origin/codex/organize-chatgpt-project-structure

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `9b9379d3fa5114bb71dd4e4d54a541ca6b1ccddd` — docs: organize HELIOS project setup guidance — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `README.md`
- `docs/guides/PROJECT_SETUP.md`

## origin/codex/setup-network-infrastructure-and-security-policies

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** high
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `c8f1b6ffa2b5edd86d6a254d7841c88949c9eb9a` — Fix governed Azure network review findings — Yolkster64 <thepatman64@gmail.com>
- `734b033d14e321974f0ba1488214a6abfa8a0aaf` — Test governed network profile isolation — Yolkster64 <thepatman64@gmail.com>
- `dc1388e36a00710537589564fc840a2fc91798ae` — Fix governed private edge deployment — Yolkster64 <thepatman64@gmail.com>
- `5971fc7fa7c60cccd4d9b7c64931fa7a1b6040d7` — Add governed private Azure edge network — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `infra/azure/README.md`
- `infra/azure/main.bicep`
- `infra/azure/modules/hub-governance.bicep`
- `infra/azure/modules/keyvault-private-cutover.bicep`
- `infra/azure/modules/keyvault.bicep`
- `infra/azure/modules/network.bicep`
- `infra/azure/modules/observability.bicep`
- `infra/azure/modules/private-edge.bicep`
- `infra/azure/modules/private-endpoints.bicep`
- `infra/azure/modules/storage.bicep`
- `infra/azure/modules/vnet-flow-log.bicep`
- `infra/azure/parameters/dev.json`
- `monado/helios-control/config/network-paths.json`
- `monado/helios-control/infra/connector.bicep`
- `monado/helios-control/infra/main.bicep`
- `monado/helios-control/src/Helios.Connect.Api/Helios.Connect.Api.csproj`
- `monado/helios-control/src/Helios.Connect.Api/NetworkPathCatalog.cs`
- `monado/helios-control/src/Helios.Connect.Api/Program.cs`
- `monado/helios-control/tests/Helios.Connect.Tests/NetworkPathCatalogTests.cs`

## origin/copilot/add-usb-installer-for-helios

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `8846cc3c07f4e82a90993abd9ff43a86d3e9c0af` — Add helios-installer: 7-phase USB installer package — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `1fb238e43a4fc66c39010360d20646559fe6c9ec` — Initial commit — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- `1f4b37b9899e12ba4589e64746f2a389f5b2d679` — Initial plan — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Files
- `.gitignore`
- `README.md`
- `helios-installer/README.md`
- `helios-installer/create_bootable_usb.py`
- `helios-installer/main.py`
- `helios-installer/requirements.txt`
- `helios-installer/run_installer.bat`
- `helios-installer/run_installer.sh`

## origin/copilot/docs-customization-guide

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `2b271618111bfdef5b340f9fd7e792e4794e71ff` — chore: add .gitignore to exclude __pycache__ and .pyc files — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `1aae84a0e7442f92ac323e05239d6ce5216563e8` — feat: add HELIOS installer customization guide, configs, examples, and interactive tool — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `1fb238e43a4fc66c39010360d20646559fe6c9ec` — Initial commit — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- `92f864b37c1ee406aac875fc6d8fa2d8b3f9236d` — Initial plan — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Files
- `.gitignore`
- `helios-installer/__pycache__/customize.cpython-312.pyc`

## origin/yolkster64-verbose-tribble

- **Primary umbrella:** Python AIHub integration (`python-aihub-integration`)
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owner:** HELIOS.AIHub
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `1509a73b9203e80c4eddcaac8bcf6a5efe8f5fce` — Address setup-all review feedback for environment and gate labeling — Copilot App <223556219+Copilot@users.noreply.github.com>
- `6a4feb00b45c517ec950bfe9d93c18f298542da5` — Add setup-all orchestration command for HELIOS control fabric — Copilot App <223556219+Copilot@users.noreply.github.com>

### Patch-equivalent commits
- None

### Files
- `monado/helios-control/docs/UNIFIED_PLUGIN_SETUP.md`
- `plugins/helios-control-fabric/README.md`
- `plugins/helios-control-fabric/scripts/helios.py`
- `plugins/helios-control-fabric/scripts/test_helios.py`

## origin/codex/create/update-merge/source-manifest

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `ea6df3ec01d90905e12d87ed991acbc785a449de` — Add consolidation source manifest — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.gitmodules`
- `MERGE_SOURCE_MANIFEST.yaml`

## origin/codex/fix-azure-deploy-release-branch-conditions

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `b5fca656ce2bfd69bcb15193e48145dab5f5e72d` — Honor manual Azure deploy environments — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `microsoft-ecosystem/.github/workflows/azure-deploy.yml`

## origin/codex/fix-azure-deploy-release-branch-conditions-rq34fh

- **Primary umbrella:** GitHub enterprise automation and dashboard (`github-enterprise-automation-dashboard`)
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owner:** Developer experience, GitHub automation
- **Security impact:** medium
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `7de37cc47d23d63e48fe347d15c2976e43cd81fe` — Honor manual Azure deploy environments — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `microsoft-ecosystem/.github/workflows/azure-deploy.yml`

## origin/codex/review-generated-pr-body-file

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `40b9b662a612cda90191354f5f6a19d83b6a8d54` — Add HELIOS PR body dry-run helper — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `.gitignore`
- `helios.sh`

## origin/copilot/research-partition-user-account-details

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `1fb238e43a4fc66c39010360d20646559fe6c9ec` — Initial commit — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- `LICENSE`
- `README.md`

## origin/codex/add-helios-integration-pack

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/codex/azure-activation-20260724

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/codex/windows-boot-security-rootkit-v1

- **Primary umbrella:** C++ native performance and security (`cpp-native-performance-security`)
- **Temporary integration branch:** `integration/train-cpp-native-performance-security`
- **Module owner:** HELIOS.Native, HELIOS.Security
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** review-for-integration-train

### Unique commits
- `9ede6c0a57ee5c8876d9f3ddf28558de696120f3` — Merge origin/main into codex/windows-boot-security-rootkit-v1 — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `08ddacbe02f6172d60f21b79ae7eedd3e5448cb6` — Strengthen scan and registry guardrails — Yolkster64 <thepatman64@gmail.com>
- `83276a21cd774c4c54d962c5594c6e9c0c892862` — Clarify quick-scan default and rollback evidence — Yolkster64 <thepatman64@gmail.com>
- `6ecf1afce4c667e3b1bdefbabec741933481ea06` — Align scan policy with quick-scan default — Yolkster64 <thepatman64@gmail.com>
- `104e44551c9a95e901855a84ac8246ee04cee62d` — Make scheduled full scans explicit opt-in — Yolkster64 <thepatman64@gmail.com>
- `2ce0cf27b7fb5592957fdb382e026e17a473e7ad` — Document and expand registry rollback evidence — Yolkster64 <thepatman64@gmail.com>
- `0f756401d8a17e41d2504ee98b8cd67520e6df6b` — Document read-only registry inspection for CI — Yolkster64 <thepatman64@gmail.com>
- `c82ce24f29f069b1f55f9e97ee5fd0d9dea8a173` — Add Windows security parser and guardrail workflow — Yolkster64 <thepatman64@gmail.com>
- `7e829720f66b8a4b7f866be4df26b6fd7dc6ff2d` — Add startup audit and Defender scan task installer — Yolkster64 <thepatman64@gmail.com>
- `fb251f72023e76bbf7ffb92ad10051277cbec8f3` — Add guarded Microsoft Defender Offline rootkit recovery — Yolkster64 <thepatman64@gmail.com>
- `478856b014b7f601326ce28435003a5c4ad162cd` — Add guarded Defender and boot security baseline — Yolkster64 <thepatman64@gmail.com>
- `762f0d2642181090c5bc7dcebc2e2893f87ebc23` — Add non-mutating boot security posture audit — Yolkster64 <thepatman64@gmail.com>
- `6644803cd70ba25f8f6cb0b6b69b4426b177a6bc` — Document boot security and rootkit recovery workflow — Yolkster64 <thepatman64@gmail.com>
- `7750b10c86d45cfacb419561731daca9667d17e1` — Add guarded OpenAI security analysis template — Yolkster64 <thepatman64@gmail.com>
- `170215e9f2fee5f1d029ece9badf2df8ad0746aa` — Add Windows boot security policy manifest — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/clean-up-or-merge-firstconsumers

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/combine-commits-fix-docdex-branches

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/establish-six-profile-delivery-fabric

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/explore-codebase-implementation-plan

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/fix-274244942-1207349837-5561ad1c-30df-4271-941a-7e354fd468f8

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/fix-azure-deploy-issues

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/fix-azure-devops-build-issues

- **Primary umbrella:** Azure/Bicep/Cloud Shell deployment (`azure-bicep-cloud-shell`)
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owner:** HELIOS.Azure, Cloud engineering
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/fix-everything

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/full-merge-or-code-quality

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/full-purple-merge

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/fully-merge-and-finish-setup

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/maximum-merge-maximum-code-quality

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/merge-all-projects-into-one

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/merge-and-ensure-functionality

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/merge-code-to-100

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/review-session-history

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/copilot/setup-local-runners-xcore-9-fleet

- **Primary umbrella:** Hermes/XCore fleet (`hermes-xcore-fleet`)
- **Temporary integration branch:** `integration/train-hermes-xcore-fleet`
- **Module owner:** HELIOS.Hermes, XCore
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/helios-build-1

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `a265ead0644ef22e291e264ed2f9bd0ee9f27520` — Add HELIOS/HERMES orchestration backbone and Azure CLI bootstrap (#61) — Yolkster64 <thepatman64@gmail.com>
- `1c6c3f9a95b673c4c75bb56fc0d425869c594156` — Add HELIOS HERMES orchestration backbone — Yolkster64 <thepatman64@gmail.com>
- `60eb8965b6852486160ec91b0aca0ff19bf3f943` — Merge pull request #1 from M0nado/copilot/add-usb-installer-for-helios — Yolkster64 <thepatman64@gmail.com>
- `8846cc3c07f4e82a90993abd9ff43a86d3e9c0af` — Add helios-installer: 7-phase USB installer package — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>
- `1fb238e43a4fc66c39010360d20646559fe6c9ec` — Initial commit — Yolkster64 <thepatman64@gmail.com>

### Patch-equivalent commits
- `1f4b37b9899e12ba4589e64746f2a389f5b2d679` — Initial plan — copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>

### Files
- None

## origin/main

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** baseline

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

## origin/master

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** merge-tree reported an unresolved merge; inspect before integration
- **Disposition:** manual-review-and-selective-extraction

### Unique commits
- `85e49c0182db5c9e200c374c8003dc5081243215` — Add autonomous preflight workflow and cache/monitoring improvements (#107) — Yolkster64 <thepatman64@gmail.com>
- `653a96fe9ede22a66d8db56602b83254544ad82c` — Tighten autonomous preflight and benchmark targets — Yolkster64 <thepatman64@gmail.com>
- `729a8c9d52caa6103fbddb01cdec0fd6c6a12177` — docs: Add comprehensive fleet experiments summary & results — HELIOS v4.0 <helios@v4.0>
- `38408ea8abe39bc4a63b2cb8b2d12882bb741da8` — feat: Add comprehensive fleet agent experiments & simulations — HELIOS v4.0 <helios@v4.0>
- `f32aefddba5d385830c13bc5d5429816283753d9` — docs: Phase 4 planning - Backend Infrastructure & Advanced Features — HELIOS v4.0 <helios@v4.0>
- `2152226fba9a021a8334c3b4ceb77f18eb3d689a` — docs: Add comprehensive production documentation and API reference — HELIOS v4.0 <helios@v4.0>
- `87d75b4de4a48cf1c24351e6f5043c003e2d6424` — feat: Enhance Build Agents and GUI Dashboard with production features — HELIOS v4.0 <helios@v4.0>
- `2fd0a482a41d438ed2738d8a93e4b3824b8a7c50` — feat: Enhance AI Orchestrator and USB Installer modules with production features — HELIOS v4.0 <helios@v4.0>
- `2af60b76d05268c0d85c8fa02274b07bf6a46b1c` — feat: Add production-grade features, testing, and enhancements to all modules — HELIOS v4.0 <helios@v4.0>
- `ed0ecf02cb46799c6a6dfb6c64c276efdbce4090` — docs: Add 6-module architecture documentation — HELIOS v4.0 <helios@v4.0>
- `78a4bec5f6e77f0d8a7e6125f15d6278db915437` — refactor: Restructure to 6-module architecture - GUI, Security, Patterns, AI, USB, Build — HELIOS v4.0 <helios@v4.0>
- `c27e31e729d4fe14824dfaea14ebaec640d12bcd` — docs: Final consolidation report - 3-module architecture complete — HELIOS v4.0 <helios@v4.0>
- `6e47bdd1043f06a5dd63d9c5b3800aef7fd230e8` — docs: Add final 3-module architecture documentation — HELIOS v4.0 <helios@v4.0>
- `ad669ef9ffb0bb05e2142fbb1aea960f89e551fe` — refactor: Final consolidation to 3-module architecture - GUI, SystemCore, Infrastructure — HELIOS v4.0 <helios@v4.0>
- `5a9e0b87a85e9dfa1d5b04226e53b255771b0a04` — docs: Add comprehensive documentation for 6-module architecture — HELIOS v4.0 <helios@v4.0>
- `ceb14196de6cb667f2078b2e04eaf6979b380b5e` — refactor: Consolidate 9 modules into 6-module architecture — HELIOS v4.0 <helios@v4.0>
- `f377fd471e6bf7ab57ab7435bfde477ee6e64772` — docs: Add comprehensive documentation for USB Builder and System Setup module split — HELIOS v4.0 <helios@v4.0>
- `afdae4dcc871c7df1686ca035bdf902c7140f4e4` — feat: Add USB Builder and System Setup modules - split image flashing and partition/user setup into separate components — HELIOS v4.0 <helios@v4.0>
- `5cc792aecf799079713e050eb545514a2f3385ea` — feat: Consolidate all 7 HELIOS repositories into unified modules — HELIOS v4.0 <helios@v4.0>
- `f2b151d1d2810064e1dc9ecac0f7788b9d6d5f8b` — docs: Update README with consolidated structure and quick start — HELIOS v4.0 <helios@v4.0>
- `146ae209c654ff59b91e0e1dca3d37a4516421c9` — docs: Add consolidation completion summary — HELIOS v4.0 <helios@v4.0>
- `1964fac9b81ccec07468910d1cd49cc02306c260` — refactor: Consolidate and optimize codebase - reduce complexity 80%+ — HELIOS v4.0 <helios@v4.0>
- `616294a72e8c958a6218e8899e1de1899ddbd13b` — docs: Add task completion final report - All deliverables complete — HELIOS v4.0 <helios@v4.0>
- `8c40837e8271c167602588c9082eb250fa96c234` — docs: Add Phase 3 Readiness Report - Ready for execution on June 10, 2026 — HELIOS v4.0 <helios@v4.0>
- `64f03d1b205402c70054aff2912aa38d7f07d9b9` — docs: Add GitHub push summary and final documentation status — HELIOS v4.0 <helios@v4.0>
- `b09a829dabc6059b4b11ed916dd4390e14400c66` — Phase 1-3 Documentation Consolidation Complete: Master docs, quick start guide, and organization structure with zero redundancies — HELIOS v4.0 <helios@v4.0>
- `8feda0ca08436dc73170c280861a02c5e40e4e3a` — docs: Add final delivery report — HELIOS v4.0 <helios@v4.0>
- `86fd3707a7b877089b7595a120b09d0b4b675725` — docs: Add comprehensive documentation and deployment guides — HELIOS v4.0 <helios@v4.0>
- `2873a875a70f23cd112a7fa6d9981f9bdd7d0959` — feat: HELIOS v4.0 - Final optimization, integration, and reorganization — HELIOS v4.0 <helios@v4.0>

### Patch-equivalent commits
- None

### Files
- None

## origin/yolkster64-fantastic-bassoon

- **Primary umbrella:** Shared architecture and contracts (`shared-architecture-contracts`)
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owner:** Platform architecture
- **Security impact:** low
- **Merge-tree conflicts:** none
- **Disposition:** close-as-patch-equivalent-or-merged

### Unique commits
- None

### Patch-equivalent commits
- None

### Files
- None

# Umbrella issue plan

These issue records are generated from the committed analyzer. Create them in GitHub before starting an integration train.

## C#/WinUI 3 control center

- **Key:** `csharp-winui-control-center`
- **Temporary integration branch:** `integration/train-csharp-winui-control-center`
- **Module owners:** MonadoBlade.GUI, HELIOS.Platform presentation
- **Assigned diverged branches:** 0

### Branches
- None

### Acceptance gates
- [ ] review source-commit manifest and preserve authorship
- [ ] squash accepted work into goal-oriented commits
- [ ] build and test affected projects and validate contracts
- [ ] merge through this train's reviewed pull request
- [ ] create rollback tag before deleting any historical branch
- [ ] verify accepted commits are reachable from protected main and PRs are closed

## C++ native performance and security

- **Key:** `cpp-native-performance-security`
- **Temporary integration branch:** `integration/train-cpp-native-performance-security`
- **Module owners:** HELIOS.Native, HELIOS.Security
- **Assigned diverged branches:** 6

### Branches
- `origin/codex/update-project/package-configuration`
- `origin/agent/monadoblade-delivery-fabric-v2`
- `origin/codex/propose-repo-layout-overhaul`
- `origin/codex/propose-repo-layout-overhaul-hws95t`
- `origin/develop`
- `origin/codex/windows-boot-security-rootkit-v1`

### Acceptance gates
- [ ] review source-commit manifest and preserve authorship
- [ ] squash accepted work into goal-oriented commits
- [ ] build and test affected projects and validate contracts
- [ ] merge through this train's reviewed pull request
- [ ] create rollback tag before deleting any historical branch
- [ ] verify accepted commits are reachable from protected main and PRs are closed

## F# analytics and prediction

- **Key:** `fsharp-analytics-prediction`
- **Temporary integration branch:** `integration/train-fsharp-analytics-prediction`
- **Module owners:** HELIOS.Analytics.FSharp
- **Assigned diverged branches:** 7

### Branches
- `origin/codex/fix-high-priority-bugs-from-codex-review`
- `origin/yolkster64-define-monado-enterprise-experience-fabr`
- `origin/codex/create-analytics-project-structure-and-apis-hulohi`
- `origin/copilot/get-all-issues-and-commits`
- `origin/copilot/setup-pull-requests-and-issues`
- `origin/codex/create-analytics-project-structure-and-apis`
- `origin/codex/update-helios.repositoryanalytics-handling`

### Acceptance gates
- [ ] review source-commit manifest and preserve authorship
- [ ] squash accepted work into goal-oriented commits
- [ ] build and test affected projects and validate contracts
- [ ] merge through this train's reviewed pull request
- [ ] create rollback tag before deleting any historical branch
- [ ] verify accepted commits are reachable from protected main and PRs are closed

## Python AIHub integration

- **Key:** `python-aihub-integration`
- **Temporary integration branch:** `integration/train-python-aihub-integration`
- **Module owners:** HELIOS.AIHub
- **Assigned diverged branches:** 48

### Branches
- `origin/yolkster64-verbose-giggle`
- `origin/codex/implement-modules-for-local_xtier_artifacts`
- `origin/codex/plan-feature-upgrades-and-code-integration`
- `origin/codex/set-up-slack-with-znc-optimization`
- `origin/yolkster64-friendly-spork`
- `origin/codex/update-build_graph.py-for-classification-and-reporting`
- `origin/yolkster64-legendary-journey`
- `origin/codex/update-build_graph.py-for-classification-and-reporting-kuifvm`
- `origin/codex/update-build_graph.py-for-classification-and-reporting-lhug7d`
- `origin/codex/absorb-xtier-winre-aihub-v6`
- `origin/codex/add-documented-ltrain-entrypoint`
- `origin/codex/add-gui-for-managing-ai-hub-resources`
- `origin/codex/configure-github-issue/pr-integration`
- `origin/codex/fix-and-optimize-tests-and-builds`
- `origin/codex/fix-high-priority-issues-from-codex-review-nybf6u`
- `origin/codex/integrate-aihub-fleet-service-with-ai-agent`
- `origin/yolkster64-curly-journey`
- `origin/codex/fix-high-priority-issues-from-codex-review`
- `origin/codex/fix-high-priority-issues-from-codex-review-5bbmk9`
- `origin/codex/fix-high-priority-issues-from-codex-review-tuugad`
- `origin/codex/fix-linux-ci-coverage-issues-in-pull-request-#85`
- `origin/yolkster64-literate-funicular`
- `origin/yolkster64-reimagined-dollop`
- `origin/yolkster64-sturdy-dollop`
- `origin/codex/create-integration-merge-pr-template`
- `origin/copilot/do-this-along-with-all`
- `origin/rescue/consolidation-aihub-integration`
- `origin/codex/find-best-steps-to-set-up-environment-d1gby4`
- `origin/codex/find-best-steps-to-set-up-environment-xld4tn`
- `origin/copilot/setup-helios-hermes-fleet`
- `origin/codex/automate-merge-and-fix-processes`
- `origin/codex/integrate-full-ai-and-automation`
- `origin/codex/review-and-document-dotnet-install.sh`
- `origin/codex/automate-merge-and-fix-processes-41fypd`
- `origin/codex/add-shared-tool-resolution-feature`
- `origin/codex/add-automatic-python-compile-checker`
- `origin/codex/add-build-graph-option-for-readiness`
- `origin/codex/create-c#-native-smoke-test-script`
- `origin/codex/edit-helios.sh-for-remote-priority-case`
- `origin/codex/refine-build-graph-changed-file-matching`
- `origin/codex/update-build_graph.py-and-helios.sh`
- `origin/codex/update-helios.sh-with-remote-priority-command`
- `origin/yolkster64-redesigned-waddle`
- `origin/codex/update-full_stack_readiness.py-for-tool-checks`
- `origin/copilot/setup-and-combine-all`
- `origin/copilot/add-usb-installer-for-helios`
- `origin/copilot/docs-customization-guide`
- `origin/yolkster64-verbose-tribble`

### Acceptance gates
- [ ] review source-commit manifest and preserve authorship
- [ ] squash accepted work into goal-oriented commits
- [ ] build and test affected projects and validate contracts
- [ ] merge through this train's reviewed pull request
- [ ] create rollback tag before deleting any historical branch
- [ ] verify accepted commits are reachable from protected main and PRs are closed

## Hermes/XCore fleet

- **Key:** `hermes-xcore-fleet`
- **Temporary integration branch:** `integration/train-hermes-xcore-fleet`
- **Module owners:** HELIOS.Hermes, XCore
- **Assigned diverged branches:** 5

### Branches
- `origin/codex/create-xcore-9-service-with-defined-capabilities`
- `origin/agent/hermes-xcore-contract-v1`
- `origin/yolkster64-issue-241-hermes-xcore9-specialization-p`
- `origin/yolkster64-scaling-system`
- `origin/copilot/setup-local-runners-xcore-9-fleet`

### Acceptance gates
- [ ] review source-commit manifest and preserve authorship
- [ ] squash accepted work into goal-oriented commits
- [ ] build and test affected projects and validate contracts
- [ ] merge through this train's reviewed pull request
- [ ] create rollback tag before deleting any historical branch
- [ ] verify accepted commits are reachable from protected main and PRs are closed

## GitHub enterprise automation and dashboard

- **Key:** `github-enterprise-automation-dashboard`
- **Temporary integration branch:** `integration/train-github-enterprise-automation-dashboard`
- **Module owners:** Developer experience, GitHub automation
- **Assigned diverged branches:** 71

### Branches
- `origin/yolkster64-vigilant-tribble`
- `origin/yolkster64-didactic-goggles`
- `origin/yolkster64-musical-lamp`
- `origin/codex/build,-test,-optimize,-and-score`
- `origin/codex/fix-asp.net-core-usings-in-minimal-app`
- `origin/codex/fix-credential-leakage-in-deep-automation-orchestrator`
- `origin/codex/implement-scoped-issue-for-submodule-approval`
- `origin/codex/optimize-code-for-automerging-and-testing`
- `origin/codex/repair-repository-integrity-and-validate`
- `origin/codex/repair-repository-integrity-and-validate-q0zu9y`
- `origin/copilot/create-helios-azure-sub-agent-fleet`
- `origin/yolkster64-consolidate-open-codex-prs-into-merge-re`
- `origin/yolkster64-define-monado-enterprise-experience-fabr-5f4`
- `origin/yolkster64-issue-228-build-workflow-hardening`
- `origin/yolkster64-musical-spork`
- `origin/yolkster64-reimagined-fiesta`
- `origin/yolkster64-supreme-goggles`
- `origin/yolkster64-supreme-succotash`
- `origin/codex/add-pull_request-trigger-to-multi-repo-sync`
- `origin/codex/optimize-code-automation-and-github-usage-7few7a`
- `origin/yolkster64-complete-governed-azure-activation-key-v`
- `origin/yolkster64-define-governed-github-codex-claude-code`
- `origin/yolkster64-fluffy-fortnight`
- `origin/yolkster64-glowing-goggles`
- `origin/yolkster64-security-re-enable-m365-agents-toolkit-c`
- `origin/yolkster64-xcore9-evaluator-knaa-scoring-policy-gat`
- `origin/codex/add-auto-setup-configuration`
- `origin/codex/combine-phased-mds-and-optimize-code`
- `origin/codex/combine-phased-mds-and-optimize-code-f6dw5m`
- `origin/codex/combine-phased-mds-and-optimize-code-ozczwm`
- `origin/codex/combine-phased-mds-and-optimize-code-qpcmmn`
- `origin/codex/create-setup-guide-in-docs/setup`
- `origin/codex/find-best-steps-to-set-up-environment`
- `origin/codex/fix-oidc-permission-in-pr-workflow`
- `origin/codex/integrate-full-ai-and-workflow-automation`
- `origin/codex/integrate-full-ai-and-workflow-automation-aov84f`
- `origin/codex/organize-project-repository-and-branches`
- `origin/codex/outline-50-key-github-workflow-improvements`
- `origin/codex/set-up-ci-automation-for-bug-merging`
- `origin/codex/update-ai-model-configuration-and-routing`
- `origin/codex/update-azure-deployment-workflow-and-infrastructure-hrnhdl`
- `origin/codex/update-or-replace-helios.platform-solution-file`
- `origin/fix/powershell-parser-inventory`
- `origin/integration/unified-agent-communication-v1`
- `origin/yolkster64-ubiquitous-meme`
- `origin/agent/dev-cockpit-v1`
- `origin/codex/optimize-code-automation-and-github-usage`
- `origin/copilot/get-commits-fixed-and-finished`
- `origin/copilot/understand-codebase-structure`
- `origin/services/helios-tool-broker-v1`
- `origin/codex/fix-high-priority-bug-in-ci-workflow`
- `origin/promotion/monado-blade-eaa9c6b`
- `origin/codex/fix-ai-code-review-pipeline-failures`
- `origin/yolkster64-bookish-journey`
- `origin/yolkster64-supreme-guacamole`
- `origin/integration/helios-chatgpt-copilot-app`
- `origin/codex/document-git-commit-best-practices`
- `origin/integration/windows-security-post-174`
- `origin/integration/helios-unified-control-plugin-v1`
- `origin/codex/refactor-project-structure-for-cross-platform-separation`
- `origin/codex/update-wiki-sync-workflow`
- `origin/integration/pr188-rebased-staging`
- `origin/yolkster64-sturdy-journey`
- `origin/Yolkster64-patch-1feat/unified-pipeline-scaffold`
- `origin/codex/create-analytics-project-structure-and-apis-2gzau7`
- `origin/codex/update-github-workflows-for-required-checks`
- `origin/copilot/setup-build-matrix`
- `origin/yolkster64-curly-spork`
- `origin/codex/add-owner-governance-baseline-documentation`
- `origin/codex/fix-azure-deploy-release-branch-conditions`
- `origin/codex/fix-azure-deploy-release-branch-conditions-rq34fh`

### Acceptance gates
- [ ] review source-commit manifest and preserve authorship
- [ ] squash accepted work into goal-oriented commits
- [ ] build and test affected projects and validate contracts
- [ ] merge through this train's reviewed pull request
- [ ] create rollback tag before deleting any historical branch
- [ ] verify accepted commits are reachable from protected main and PRs are closed

## Azure/Bicep/Cloud Shell deployment

- **Key:** `azure-bicep-cloud-shell`
- **Temporary integration branch:** `integration/train-azure-bicep-cloud-shell`
- **Module owners:** HELIOS.Azure, Cloud engineering
- **Assigned diverged branches:** 19

### Branches
- `origin/codex/azure-enterprise-agent-fleet`
- `origin/codex/update-workflow-for-subnet-id-handling`
- `origin/codex/install-azure-cli-and-update-setup-docs`
- `origin/yolkster64-crispy-dollop`
- `origin/codex/clarify-target-frameworks-and-modules`
- `origin/sre/deployment-automation-reporting-noworkflow`
- `origin/yolkster64-spec-define-hermes-xcore-federation-env`
- `origin/codex/update-azure-deployment-workflow-and-infrastructure`
- `origin/integration/monado-azure-canonical-port`
- `origin/codex/update-azure-setup-assets-and-features`
- `origin/codex/set-all-sessions-to-read-write`
- `origin/codex/set-up-azure-and-experiment-with-copilot`
- `origin/codex/perform-branch-integration-and-report`
- `origin/yolkster64-improved-sniffle`
- `origin/yolkster64-laughing-giggle`
- `origin/codex/setup-network-infrastructure-and-security-policies`
- `origin/codex/azure-activation-20260724`
- `origin/copilot/fix-azure-deploy-issues`
- `origin/copilot/fix-azure-devops-build-issues`

### Acceptance gates
- [ ] review source-commit manifest and preserve authorship
- [ ] squash accepted work into goal-oriented commits
- [ ] build and test affected projects and validate contracts
- [ ] merge through this train's reviewed pull request
- [ ] create rollback tag before deleting any historical branch
- [ ] verify accepted commits are reachable from protected main and PRs are closed

## Shared architecture and contracts

- **Key:** `shared-architecture-contracts`
- **Temporary integration branch:** `integration/train-shared-architecture-contracts`
- **Module owners:** Platform architecture
- **Assigned diverged branches:** 27

### Branches
- `origin/codex/implement-or-mock-placeholder-methods-in-channel3boottimeaut`
- `origin/codex/make-significant-bug-fixes`
- `origin/codex/update-setup-documentation-for-helios-stack`
- `origin/codex/optimize-repository-after-massive-merge`
- `origin/architecture/helios-platform-2-foundation`
- `origin/codex/organize-chatgpt-project-structure`
- `origin/codex/create/update-merge/source-manifest`
- `origin/codex/review-generated-pr-body-file`
- `origin/copilot/research-partition-user-account-details`
- `origin/codex/add-helios-integration-pack`
- `origin/copilot/clean-up-or-merge-firstconsumers`
- `origin/copilot/combine-commits-fix-docdex-branches`
- `origin/copilot/establish-six-profile-delivery-fabric`
- `origin/copilot/explore-codebase-implementation-plan`
- `origin/copilot/fix-274244942-1207349837-5561ad1c-30df-4271-941a-7e354fd468f8`
- `origin/copilot/fix-everything`
- `origin/copilot/full-merge-or-code-quality`
- `origin/copilot/full-purple-merge`
- `origin/copilot/fully-merge-and-finish-setup`
- `origin/copilot/maximum-merge-maximum-code-quality`
- `origin/copilot/merge-all-projects-into-one`
- `origin/copilot/merge-and-ensure-functionality`
- `origin/copilot/merge-code-to-100`
- `origin/copilot/review-session-history`
- `origin/helios-build-1`
- `origin/master`
- `origin/yolkster64-fantastic-bassoon`

### Acceptance gates
- [ ] review source-commit manifest and preserve authorship
- [ ] squash accepted work into goal-oriented commits
- [ ] build and test affected projects and validate contracts
- [ ] merge through this train's reviewed pull request
- [ ] create rollback tag before deleting any historical branch
- [ ] verify accepted commits are reachable from protected main and PRs are closed

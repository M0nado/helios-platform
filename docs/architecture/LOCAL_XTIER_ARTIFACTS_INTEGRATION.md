# Local X-Tier / AIHub Artifact Integration Map

This document captures local computer project artifacts that were surfaced during consolidation. These artifacts should not be lost. They are treated as prototype/reference inputs that should be ported into the C# HELIOS architecture where appropriate.

## Rule

C# remains the production spine. Python artifacts are preserved as prototypes, reference implementations, or local diagnostic tools until their concepts are ported into C# modules.

## Artifacts discovered

### `security_optimizer.py`

Purpose:

- Defines `SecurityOptimizationPlan`.
- Supports balanced and paranoid profiles.
- Captures CPU, memory, egress, and training policy.

C# target:

- `HELIOS.Security.Optimization.SecurityOptimizationPlan`
- `HELIOS.Security.Optimization.SecurityProfilePlanner`

Feature matrix additions:

- balanced/paranoid security tuning
- egress allowlist modes
- signed-artifacts-only training policy
- checkpoint + drift guard training policy

### `vm_orchestrator.py`

Purpose:

- Defines a default topology across Docker, WSL2, and Hyper-V.
- Roles include gateway/API, GUI/control, trainer, and security isolation.

C# target:

- `HELIOS.Orchestration.RuntimeTopology`
- `HELIOS.AIHub.Wsl2Trainer`
- `HELIOS.Security.HyperVIsolation`
- `HELIOS.ServerHub.Gateway`

Default topology to preserve:

```text
Docker: gateway+api, GPU enabled, 8 GB
Docker: gui+control, 4 GB
WSL2: trainer, GPU enabled, 16 GB
Hyper-V: security-isolation, 4 GB
```

### `ml_registry.py`

Purpose:

- Seeds AI/ML model profiles and objectives.
- Includes contextual bandit routing, autoencoder guard, drift detector, security anomaly core, integration policy host, regression, graph attention, recurrent anomaly, chaos/evolutionary/bayesian optimizers, memory pressure optimizer, mesh consensus engine.

C# target:

- `HELIOS.AIHub.ModelRegistry`
- `HELIOS.AIHub.ModelProfile`
- `config/aihub/model-registry.seed.json`

Preserve families:

- bandit
- autoencoder
- statistical drift
- heuristic security anomaly
- rules/policy host
- regression
- mesh
- graph attention
- sequence anomaly
- exploration
- evolutionary
- bayesian optimization
- memory pressure
- consensus

### `deep_engine_fabric.py`

Purpose:

- Defines a deep engine catalog with regression, symbolic, geometry, vision, clustering, mesh, graph, sequence, routing, compression, governance, exploration, evolutionary, optimization, analytics, retrieval, security, and memory pressure engines.
- Captures parallelization types, specialization tools, hybridization strategies, and GitHub learning sources.

C# target:

- `HELIOS.AIHub.DeepEngineCatalog`
- `HELIOS.AIHub.EngineRecommendationService`
- `config/aihub/deep-engine-catalog.seed.json`

Must preserve:

- task/data/pipeline/tensor/model/fleet/multi-LLM/subagent/hybrid-mesh/async-event parallelization types
- chaos + bandit hybrid strategy
- natural selection + bayesian hybrid strategy
- graph attention + mesh consensus hybrid strategy
- autoencoder + vector retrieval hybrid strategy
- security anomaly + drift detector hybrid strategy

### `aihub_control_server.py`

Purpose:

- Local HTTP control server prototype.
- Exposes health, report, conversation report, docker status, fleet live, security live, Hyper-V phase, knowledge summary, engine catalog, engine recommendation, setup tracker, autoboot plan, tasks, agents, teams, memory search, and training trigger endpoints.

C# target:

- `HELIOS.ServerHub.LocalDashboard`
- `HELIOS.AIHub.ControlServer`
- `HELIOS.AIHub.MemorySearch`
- `HELIOS.AIHub.TrainingController`
- `HELIOS.ServerHub.ApiGateway`

Important endpoints to preserve:

```text
GET  /health
GET  /meta
GET  /memory/search
GET  /agents/list
GET  /teams
GET  /api/fleet/live
GET  /api/security/live
GET  /api/hyperv/phase1
GET  /api/knowledge/summary
GET  /api/engines/catalog
GET  /api/engines/recommend
GET  /api/setup/tracker
GET  /api/setup/autoboot-plan
POST /tasks/create
POST /api/train/trigger
```

### `ai.py`

Purpose:

- Thin AIHub API CLI.
- Supports task, train, status, agents, teams, logs, and optimize commands.

C# target:

- `HELIOS.Cli`
- `HELIOS.AIHub.Client`

Commands to preserve:

```text
helios ai task
helios ai train
helios ai status
helios ai agents
helios ai teams
helios ai logs
helios ai optimize
```

### `hermes_xcore_training_loop.py`

Purpose:

- Local self-teaching loop prototype.
- Uses contextual bandit routing, task generation, self-teaching store, vector memory, RL memory, scoring, reflection notes, and routing dispatch.

C# target:

- `HELIOS.AIHub.HermesTrainingLoop`
- `HELIOS.AIHub.Routing.ContextualBanditRouter`
- `HELIOS.AIHub.Memory.SelfTeachingStore`
- `HELIOS.AIHub.TaskGeneration.WeaknessAwareTaskGenerator`

Preserve concepts:

- weakness-aware task generation
- contextual bandit router
- vector memory
- RL memory
- reflection notes
- quality + latency scoring
- route feedback loops

### `hermes_xcore_training_loop_pseudo.py`

Purpose:

- Minimal pseudo-runner for Hermes/XCore.
- Registers inference, feature engineering, and training nodes.

C# target:

- `HELIOS.AIHub.HermesTrainingLoop.Tests`
- simple integration test fixture

### `winre_conversation_integrator.py`

Purpose:

- Converts WinRE/Copilot conversation transcripts into integration artifacts.
- Classifies requirements by winre, vhdx, security, networking, AI/ML, runtime, optimization.
- Produces requirements, milestones, unique requirements, component coverage, and integration map.

C# target:

- `HELIOS.Requirements.TranscriptIntegrator`
- `HELIOS.Requirements.ComponentClassifier`
- `HELIOS.Requirements.FeatureMatrixUpdater`

Preserve tags:

- winre
- vhdx
- security
- networking
- ai_ml
- runtime
- optimization

### `build_super_outputs.py`

Purpose:

- Seeds AIHub registry, security optimization plan, and VM topology artifacts.

C# target:

- `HELIOS.DevTools.GenerateSeedArtifacts`

### Repair logs

Files surfaced:

- `00_Master_20260604_012614.log`
- `01_LocalRepair_20260604_013051.log`
- `.reboot_needed`

Do not commit raw logs by default. They include local machine/user path details. Preserve lessons instead.

Lessons:

- X-Tier repair scripts must detect admin rights before execution.
- SFC/DISM/winmgmt/netsh/pnputil calls failed when commands were unavailable in the current execution environment.
- Admin elevation is required for PSWindowsUpdate module installation.
- Critical service checks for EventLog, Winmgmt, RpcSs, DcomLaunch, Schedule, CryptSvc, Dhcp, PlugPlay, Power were useful.
- Reboot pending detection should be part of the repair/reporting flow.

C# target:

- `HELIOS.Repair.LocalRepairRunner`
- `HELIOS.Repair.AdminPreflight`
- `HELIOS.Repair.CommandAvailabilityCheck`
- `HELIOS.Repair.CriticalServicesAudit`
- `HELIOS.Repair.PendingRebootDetector`

## Repo organization plan

Prototype Python code can be preserved under:

```text
docs/archive/local-x-tier-python/
```

Production C# ports should land under:

```text
src/HELIOS.AIHub/
src/HELIOS.ServerHub/
src/HELIOS.Orchestration/
src/HELIOS.Security/
src/HELIOS.Repair/
src/HELIOS.Cli/
```

## Next implementation issues

1. Port `security_optimizer.py` into C# security profile planner.
2. Port `vm_orchestrator.py` into runtime topology service.
3. Convert `ml_registry.py` into JSON seed plus C# model registry.
4. Convert `deep_engine_fabric.py` into AIHub deep-engine catalog.
5. Convert `aihub_control_server.py` into ASP.NET minimal API or local Kestrel service.
6. Convert `ai.py` into `helios ai` CLI commands.
7. Convert `winre_conversation_integrator.py` into requirement ingestion utility.
8. Add repair preflight that refuses to run SFC/DISM/netsh/pnputil until command availability and admin elevation are verified.

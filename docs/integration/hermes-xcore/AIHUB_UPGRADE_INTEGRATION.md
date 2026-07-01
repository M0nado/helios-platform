# AIHub Upgrade + Deep Integration

This layer connects optimization/security orchestration with AIHub training/state pipelines.

## New Components

- Control config: `config/x-tier/aihub-control.json`
- Local control app: `python/x-tier/aihub_control_server.py`
- Startup wrapper: `scripts/x-tier/Start-AIHubControl.ps1`
- Upgrade orchestrator: `scripts/x-tier/Invoke-AIHubUpgrade.ps1`
- Existing polyglot pipeline: `scripts/x-tier/build_all_polyglot.ps1`

## What it enables

1. Rebuild tri-language analytics (Python + C# + C++) from latest source docs.
2. Serve a local AIHub web app (`http://127.0.0.1:8787`) with:
   - health endpoint
   - merged report endpoint
   - training trigger endpoint
3. Run upgrade orchestration that chains:
   - polyglot rebuild
   - disk layout validation
   - optional optimization planning
   - optional imported security + BitLocker scripts
   - optional Hermes Docker stack bring-up (api/gateway/gui/trainer/aihub-control)

## Usage

```powershell
pwsh -File scripts/x-tier/Invoke-AIHubUpgrade.ps1 -SourcePath docs/x-tier/imported/paste-1780416864960.txt
pwsh -File scripts/x-tier/Start-AIHubControl.ps1 -Rebuild
pwsh -File scripts/x-tier/Invoke-AIHubUpgrade.ps1 -SourcePath docs/x-tier/imported/paste-1780416864960.txt -RunDockerStack
```

Docker-exposed merge surface:

- Hermes gateway: `http://127.0.0.1:8788`
- Hermes GUI (Streamlit): `http://127.0.0.1:8501`
- AIHub control merge API/UI: `http://127.0.0.1:8789`

GUI merge note:

- `runtime/hermes/apps/gui_control_center.py` now includes integrated controls for:
  - AIHub health
  - Docker stack status view
  - training trigger
  - embedded merged report view
  - LLM token/cost/power/speed dashboard
  - Hermes/XCore fleet live metrics
  - security + optimization live posture
  - knowledge mesh rollup from transcript + polyglot artifacts

Control API extensions:

- `GET /api/knowledge/summary` - unified token/cost/perf/power/security/fleet/training metrics.
- `GET /api/fleet/live` - Hermes/XCore live deployment stats.
- `GET /api/security/live` - live security posture rollup.
- `GET /api/conversation/report` - transcript-derived knowledge map.
- `GET /api/engines/catalog` - deep engine catalog (30 integrated classes, CUDA-aware).
- `GET /api/engines/recommend` - meta-learning engine mix recommendation (security + optimization + fleet-size aware).
- `GET /api/setup/tracker` - full merged setup tracker with copyable commands, difficulty labels, and upgrade paths.
- `GET /api/setup/autoboot-plan` - autoboot/self-heal phase details and forward stubs.
- `GET /api/hyperv/phase1` - Hyper-V phase1 status/capacity panel data with runtime profile linkage.

C++ source-of-truth integration:

- `cpp/x-tier/secure_runtime_core.*` now emits CPU/RAM/GPU/network optimization kernel metrics and 10 major parallelization types.
- `artifacts/polyglot/super_integrated_report.json` now includes `cpp_source_of_truth` and is used by AIHub summary scoring + GUI metrics.
- C++ now also emits security watch plans, quarantine thresholds, alert channels, and folder permission/compression governance data.
- C# frontend contract output (`csharp_security_frontend_map.json`) is generated from C++ report data and exposed through AIHub security endpoints for fleetwide UI consistency.
- Runtime profile and capacity defaults are now centralized in `config/x-tier/aihub-control.json` (`runtime_profile`, `hyperv_phase1`) and consumed by both API and GUI.

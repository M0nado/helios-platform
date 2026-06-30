# HELIOS / Monado Blade Feature Matrix

This is the no-loss inventory for consolidating HELIOS Platform, Monado Blade, AIHub, Hermes, installer, security, optimizer, GUI, drivers, software bundles, and Microsoft/Azure workflows.

## Status legend

- Implemented: code exists and must be preserved.
- Partial: code/docs exist but need integration, cleanup, or tests.
- Design: idea exists and needs implementation.
- Archive: keep as reference, not active product path.

## Core product rule

HELIOS is a C#/.NET Windows control plane. PowerShell is an external adapter. Python is archive/reference unless explicitly promoted.

## C# platform

| Feature | Status | Target |
|---|---:|---|
| HELIOS core platform | Partial | `src/core/HELIOS.Platform` |
| Monado Blade architecture patterns | Partial | `src/HELIOS.Shared` / `src/HELIOS.Core` |
| Result/error patterns | Partial | Shared primitives |
| Async retry/caching patterns | Partial | Shared primitives |
| Dependency injection host | Design | Root app host |

## Secure USB installer and recovery creator

| Feature | Status | Target |
|---|---:|---|
| USB flasher | Implemented | `src/HELIOS.Installer` |
| Boot diagnostics | Implemented | `src/HELIOS.Installer.Boot` |
| Monado USB management GUI | Implemented | GUI installer flow |
| Channel 3 boot orchestrator | Partial | Refactor into C# state machine |
| Windows recovery creator | Design | `src/HELIOS.Installer.Recovery` |
| WinRE/WinPE media generation | Design | C# + external PowerShell templates |
| Offline rootkit scan gate | Design | Defender Offline + Secure Boot/TPM checks |
| Locked-down first user | Design | `src/HELIOS.Profiles.Accounts` |
| No-outside-interference install mode | Design | staged trust gate |

## Security, vault, sandbox, quarantine

| Feature | Status | Target |
|---|---:|---|
| Vault encryption manager | Implemented | `src/HELIOS.Security.Vault` |
| Vault initializer/tests | Implemented | `src/HELIOS.Security.Vault` |
| Sandbox launcher/monitor/orchestrator | Implemented | `src/HELIOS.Security.Sandbox` |
| Quarantine service | Implemented | `src/HELIOS.Security.Quarantine` |
| Threat analyzer | Implemented | `src/HELIOS.Security.Threats` |
| Malware interfaces | Partial | `src/HELIOS.Security.Malware` |
| WDAC policy generator | Design | `src/HELIOS.Security.ApplicationControl` |
| AppLocker policy generator | Design | `src/HELIOS.Security.ApplicationControl` |
| Defender/firewall/audit baseline | Design | `src/HELIOS.Security.Baseline` |
| USB lock mode | Design | `src/HELIOS.Security.UsbLock` |

## Profiles, folders, partitions

| Feature | Status | Target |
|---|---:|---|
| Profile manager/switcher/detector | Implemented | `src/HELIOS.Profiles` |
| Gaming profile | Implemented | `src/HELIOS.Profiles` |
| Secure profile | Implemented | `src/HELIOS.Profiles` |
| Custom user folders | Design | `config/folders` |
| Core/common/cross software folders | Design | `config/folders` |
| DevDrive layout | Design | `config/partitions` |
| AI model/cache partition | Design | `I:/AIHub` |
| BitLocker/VHDX vault partition | Design | `K:/Vault` |
| Logs/sandbox/export partition | Design | `L:/HELIOS-Logs` |

## Driver and software optimizer

| Feature | Status | Target |
|---|---:|---|
| Driver installer/updater/repository | Implemented | `src/HELIOS.Drivers` |
| GPU optimizer | Implemented | `src/HELIOS.Optimizer` |
| Network optimizer | Implemented | `src/HELIOS.Optimizer` |
| Power profiler | Implemented | `src/HELIOS.Optimizer` |
| Performance tuner | Implemented | `src/HELIOS.Optimizer` |
| Software catalog service | Design | `src/HELIOS.SoftwareCatalog` |
| Bundle creator | Design | `src/HELIOS.SoftwareBundler` |
| Restore point before driver changes | Design | `src/HELIOS.Recovery.Checkpoints` |

## AIHub, Hermes, Copilot, Codex, ChatGPT, Azure Foundry

| Feature | Status | Target |
|---|---:|---|
| AIHub window | Implemented | GUI AIHub tab |
| Hermes optimization docs | Partial | `docs/aihub` |
| AI provider abstraction | Design | `src/HELIOS.AIHub` |
| Hermes agent mesh | Design | `config/aihub/hermes-agent-mesh.json` |
| Local LLM manager | Design | Ollama/LM Studio/llama.cpp adapters |
| WSL2 AI workspace | Design | `installer/hardware-integration/wsl2` + AIHub setup |
| Codex branch executor | Design | controlled branch workflows |
| GitHub Copilot instruction branch | Design | `.github` prompts/workflow |
| ChatGPT architecture reviewer lane | Design | AIHub provider policy |
| Azure AI Foundry provider | Design | Azure AIHub provider |

## Monado / Xenoblade GUI

| Feature | Status | Target |
|---|---:|---|
| MonadoMainWindow | Implemented | GUI shell |
| MonadoLoadingScreen | Implemented | Boot/loading experience |
| MonadoBlade components | Implemented | visual identity layer |
| XenobladeMondo | Implemented | visual identity layer |
| BladeAnimationController | Implemented | animation system |
| KanjiAnimationController | Implemented | symbol/glow system |
| MonadoColorPalette | Implemented | theme root |
| XenobladeThemeSystem | Implemented | theme system |
| Razer Chroma integration | Design | `HELIOS.GUI.Chroma` |
| WebView2 tabs | Design | docs/local dashboard/browser panels |
| Simple UX + advanced cockpit | Partial | default/simple, opt-in advanced |

## Codespaces and GitHub automation

| Feature | Status | Target |
|---|---:|---|
| Single devcontainer | Implemented | `.devcontainer/devcontainer.json` |
| Root C# solution | Design | `HELIOS.Platform.sln` |
| Real build/test CI | Design | `.github/workflows/helios-dotnet-ci.yml` |
| AIHub validation CI | Design | `.github/workflows/aihub-copilot-validation.yml` |
| Copilot prompt branch | Design | `copilot/aihub-hermes-codex-foundry` |
| Codex automation guide | Design | `docs/guides/CODEX_AUTOMATION.md` |

## Rule before deletion

Before anything is deleted, moved, or archived, map it to this matrix. If it maps to a feature, preserve it in code, config, docs, or archive.

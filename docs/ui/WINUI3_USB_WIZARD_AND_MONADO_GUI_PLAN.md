# WinUI 3 USB Wizard and Monado GUI Plan

This document preserves the GUI direction for HELIOS / Monado Blade while the branch consolidates AIHub, software catalogs, hardware detection, and Codespaces automation.

## Decision

The long-term user-facing shell should be WinUI 3 / Windows App SDK.

Existing WPF Monado/Xenoblade animation assets are not discarded. They remain the premium visual prototype layer until migrated, wrapped, or replaced by a higher-performance rendering layer.

## Main surfaces

### 1. Monado Boot / USB Wizard

Purpose:

- Create and verify HELIOS USB media.
- Preview Windows recovery / WinRE plan.
- Detect hardware and driver needs.
- Preview partition layout.
- Select profiles and software bundles.
- Apply security baseline in staged/dry-run-first mode.
- Produce audit evidence before any destructive action.

Tabs / steps:

1. Welcome and trust warning
2. Hardware scan
3. USB media selection
4. Recovery media plan
5. Partition plan
6. Profile selection
7. Security baseline
8. Driver and software bundles
9. Azure / AIHub / WSL2 options
10. Final review
11. Execute / export plan
12. Evidence and rollback instructions

Guardrails:

- No formatting without exact disk identity.
- No destructive actions without SysAdmin approval and confirmation phrase.
- No BitLocker changes without recovery-key evidence.
- No Azure deployment from the local wizard.
- No secret display or export.

### 2. Monado Control Center

Purpose:

- Daily GUI for profiles, system health, AIHub, security, dev workflows, and diagnostics.

Primary tabs:

```text
Dashboard
Profiles
Partitions
AIHub
GitHub / Codex
Azure
Security
Quarantine
DevOps
Studio
Gaming
Server Hub
Backups
Settings
```

### 3. AIHub cockpit

Purpose:

- Local/cloud provider status.
- Hermes/XCore routing.
- Model registry.
- Training loop status.
- Agent health.
- Memory/search status.
- Cost/performance/security posture.

Must preserve:

- contextual-bandit routing
- route -> execute -> score -> reflect -> memory
- local-first provider preference
- policy-gated cloud providers
- no secret data exposure

## Visual identity

Preserve these existing concepts:

- Monado loading screen
- glowing blade
- profile-colored blade states
- kanji/glyph orbitals
- spinning rings/circles
- hologram panels
- laser/glow accents
- sound cues
- Razer Chroma sync direction
- reduced-motion mode
- simple mode / advanced cockpit mode

## UX rule

Simple mode is default.

Advanced cockpit mode is opt-in.

The machine should feel powerful without making normal navigation feel like filing taxes inside a thunderstorm.

## Technical approach

### WinUI 3 shell

Target project:

```text
src/HELIOS.GUI/
```

Planned modules:

```text
HELIOS.GUI.Shell
HELIOS.GUI.Wizard
HELIOS.GUI.Effects
HELIOS.GUI.ProfileChrome
HELIOS.GUI.AIHub
HELIOS.GUI.Security
HELIOS.GUI.ServerHub
HELIOS.GUI.WebView
```

### Existing WPF assets

Existing WPF animation assets should be referenced in documentation and migrated carefully:

```text
MonadoMainWindow.cs
MonadoLoadingScreen.cs
AIHubWindow.cs
MonadoBlade.cs
MonadoBladeAdvanced.cs
XenobladeMondo.cs
BladeAnimationController.cs
KanjiAnimationController.cs
MonadoColorPalette.xaml
XenobladeThemeSystem.xaml
```

### Rendering direction

1. Start with WinUI 3 controls and clean layout.
2. Preserve WPF visuals as reference/prototype.
3. Add reusable animation/effect services.
4. Add performance toggles.
5. Add Chroma integration after profile system stabilizes.

## USB wizard service boundaries

The wizard should call C# services, not raw scripts directly.

```text
HELIOS.Installer.Boot
HELIOS.Installer.Recovery
HELIOS.Installer.Partitioning
HELIOS.Installer.Software
HELIOS.Installer.Drivers
HELIOS.Security.Baseline
HELIOS.Security.Vault
HELIOS.HardwareDetection
HELIOS.SoftwareCatalog
```

PowerShell scripts remain external adapters under:

```text
scripts/windows/templates/
```

C# should load, hash-verify, parameterize, dry-run, execute, and log those scripts.

## Build and test requirements

- WinUI 3 shell builds on Windows runner.
- Core service libraries build on Linux and Windows.
- Wizard plan generation can be unit-tested without admin rights.
- Destructive execution paths require integration tests with mocked runners.
- UI animation effects have reduced-motion and low-power modes.

## Immediate PR follow-up tasks

1. Create `src/HELIOS.GUI/README.md` with WinUI 3 shell contract.
2. Create `src/HELIOS.Installer/README.md` with USB wizard service contract.
3. Add Windows runner workflow for GUI shell once csproj is added.
4. Extract WPF asset inventory into `docs/ui/WPF_MONADO_ASSET_INVENTORY.md`.
5. Add simple UX wireframe for USB wizard.

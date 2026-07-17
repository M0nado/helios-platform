# HELIOS.GUI

Future WinUI 3 / Windows App SDK shell for HELIOS and Monado Blade.

## Purpose

This project will become the user-facing Control Center and USB Wizard host.

## Surfaces

- Monado Control Center
- Monado Boot / USB Wizard
- AIHub cockpit
- Security and quarantine dashboard
- Profile switcher
- Server and internet hub
- GitHub/Codex/Azure panels
- Studio/Gaming/Dev/SysAdmin profile views

## GUI direction

- WinUI 3 shell for long-term Windows UX.
- Existing WPF Monado/Xenoblade visuals preserved as prototype/reference until migrated.
- Simple mode by default.
- Advanced cockpit mode opt-in.
- Reduced-motion and low-power modes required.

## Visual identity

Preserve:

- glowing blade
- kanji/glyph orbitals
- spinning rings
- hologram panels
- profile colors
- Razer Chroma direction
- sound cues
- Monado loading screen

## Safety

The GUI must never directly format disks, change BitLocker, deploy Azure, grant tenant permissions, or run elevated scripts. It should call typed C# services that support dry-run, confirmation, audit evidence, and rollback notes.

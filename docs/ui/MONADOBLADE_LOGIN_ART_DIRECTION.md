# Monadoblade Login and Profile Wheel Art Direction

## Visual premise

The login experience is an original, high-performance WinUI 3 scene built around a dormant energy blade, a circular motorized profile selector, interactive grass, and profile-specific holographic telemetry. It is inspired by the feeling of a living control artifact, but must not reproduce an existing game logo, character, trademark, or exact prop.

## Scene composition

- Cinematic 16:9 desktop composition
- Windswept grass field at twilight
- Foreground grass blades extend toward the viewer and bend subtly toward pointer motion
- Volumetric mist and restrained aurora lighting
- Centered vertical Monadoblade with a circular aperture
- Twelve-segment outer mechanical core with counter-rotation
- Seven profile sectors around the blade
- Abstract kanji-inspired glyph in the center aperture
- Glass telemetry panels held outside the main silhouette

## Interaction sequence

1. **Dormant:** quiet grass, low mist, blade nearly dark.
2. **Pointer proximity:** nearby grass bends, particles drift toward the cursor, center aperture wakes.
3. **Wheel open:** seven profile sectors rise as holographic glyphs.
4. **Profile rotation:** the outer core rotates, profile color and sound crossfade, dashboard preview updates.
5. **Readiness:** the blade grows to 70%, storage, services, policy, security, AIHub, VM, and container checks appear.
6. **Activation:** the blade reaches full length, the center glyph glows, the outer core accelerates, and the selected shell opens.
7. **Blocked:** the ring stops, color returns to neutral, and the exact policy dependency is shown without dramatic failure noise.

## Profile visual language

| Profile | Primary color | Background motif | UI density | Sound character |
|---|---|---|---|---|
| Developer | cyan/teal | code grid through grass and aurora | advanced | glass-code rise |
| SysAdmin | amber/gold | sealed vault at night | focused | heavy sealed mechanism |
| SysOps | blue/amber | service constellation and network arcs | cockpit | network pulse |
| Gamer | electric green | low-latency energy field | minimal | fast charge |
| Studio | violet/magenta | waveform grass and spatial light | creative | harmonic bloom |
| Personal | silver/blue | calm grass and soft sky | simple | soft chime |
| Server/Background | graphite | service mesh and quiet status lights | operations | low sub-bass pulse |

## Performance constraints

- UI thread never waits for cloud services.
- The scene starts from cached shaders and local assets.
- Cinematic, balanced, lightweight, and reduced-motion modes are mandatory.
- Reduced-motion mode uses a static wheel, no particle attraction, and minimal grass movement.
- Quality can degrade automatically based on battery, thermal state, GPU pressure, remote session, or accessibility settings.
- Profile selection and policy readiness remain usable even if all decorative rendering is disabled.

## Asset production lanes

- Adobe Firefly or Photoshop concept frames for composition and lighting exploration
- Blender or Unreal for high-fidelity blade, ring, grass, and environment asset studies
- WinUI 3, Windows Composition, and Win2D for the production shell
- Direct2D/Direct3D only where profiling proves a measurable need
- Razer Chroma through a separately permissioned adapter
- Original glyph set created as vector assets; do not ship third-party franchise symbols

## Reproducible concept prompt

> Original high-end WinUI 3 desktop login and profile-selection concept for a futuristic system called Monadoblade. A luminous energy sword rises vertically from the center of a circular mechanical profile wheel. The outer ring has layered motorized segments, holographic glyphs and kanji-inspired abstract symbols, with a glowing center aperture. Seven selectable profile sectors surround the wheel: Developer, SysAdmin, SysOps, Gamer, Studio, Personal, Server. The background is a fully rendered windswept grass field at twilight with blades of grass extending into the foreground and reacting toward the cursor position, subtle volumetric mist, blue-green aurora light, dark brushed metal UI panels, glassmorphism telemetry, clean enterprise dashboard detail. Show light trails, subtle sound-wave visualization, profile color transitions, performance/security/AI/VM metrics, but keep the interface elegant, lightweight and readable. Cinematic widescreen composition, premium game-engine render, original design, no existing franchise logo or character.

## Deliverables

- Login wide concept
- Profile wheel close-up
- Seven profile theme frames
- Reduced-motion and lightweight frames
- Glyph vector sheet
- Blade and ring material sheet
- Grass interaction study
- Control Center compact quick-switch overlay
- DevHub dashboard and project-creation frame

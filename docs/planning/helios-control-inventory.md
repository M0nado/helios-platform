# helios-control Inventory

Status: remote URL configured as `https://github.com/M0nado/helios-control.git`, but fetch failed in this environment because GitHub requested credentials. Inspection remains blocked until credentials or a local path are available.

## Required inspection areas

- WinUI 3 frontend and app shell.
- C# control surface.
- `App.xaml` / `MainWindow.xaml`.
- Pages, ViewModels, and Services.
- Project files and package references.
- Existing UI architecture and direct service dependencies.

## Expected target mapping

| Source concern | Target path |
|---|---|
| WinUI app shell, pages, view models, UI services | `src/frontend/HELIOS.Control.WinUI/` |
| Shared UI-facing DTOs and interfaces | `src/core/HELIOS.Platform.Contracts/` |
| UI workflow routing and service orchestration | `src/core/HELIOS.Platform.Orchestration/` |

## Merge decision log

Record every accepted, rejected, or deferred subsystem here after inspection.

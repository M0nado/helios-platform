# Helios.Control

C# WinUI 3 operator front-end for HELIOS. This project owns the workstation
control surface only: secure status views, installer telemetry, fleet health,
Azure identity state, and AI Hub controls. Performance-sensitive work should be
kept behind native or service interfaces rather than embedded in the UI thread.

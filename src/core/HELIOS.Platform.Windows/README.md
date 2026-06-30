# HELIOS.Platform.Windows

`HELIOS.Platform.Windows` contains implementations that require Windows-specific APIs and therefore targets `net8.0-windows`.

The plain `HELIOS.Platform` project remains on `net8.0` and should hold platform-neutral abstractions, DTOs, orchestration code, cloud integrations, AI/ML services, and other services that can run on Linux, macOS, or Windows.

## Windows-only API boundary

Keep implementation code that uses these APIs in this project:

- `System.Diagnostics.EventLog`
- `System.Diagnostics.PerformanceCounter`
- `System.ServiceProcess.ServiceController`
- `System.Management` / WMI (`ManagementObjectSearcher`, `Win32_*` queries)
- `Microsoft.Win32.Registry`
- `System.Drawing.Common` and Windows Forms/WPF dependent drawing or UI helpers

Cross-platform callers should depend on abstractions from `HELIOS.Platform` and resolve Windows implementations from this project only in Windows application hosts or integration layers.

# Component Matrix

| Area | Source of truth | Primary workflow |
| --- | --- | --- |
| C#/WinUI control plane | `helios-control` | Windows UI build lane |
| C++ performance backend | `helios-monado-blade` | Native backend build lane |
| F# analytics and prediction | `hermes-fleet-production` | Analytics test lane |
| Python AI Hub | `helios-ai-hub` | Python lint/type/test lane |
| Azure/GitHub automation | `helios-build-agents` | Platform automation lane |

# Monadoblade Profile, Storage, GUI, and AIHub System

## Purpose

Monadoblade is a local-first, Azure-extended Windows platform built around seven isolated operating profiles, strict SSD partitioning, deterministic folder hierarchies, governed cross-partition transfers, and an AIHub control brain. The system must remain useful when cloud services are unavailable and must never make administrator, security, disk, identity, or production changes without an explicit policy gate.

## Primary applications

### HELIOS Control Center

The Control Center is the normal operating shell. It is implemented with C#/.NET and WinUI 3, with Windows Composition, Win2D or Direct2D for high-performance rendering and WebView2 for local documentation and approved dashboards.

It exposes:

- Profile switching and profile health
- AIHub, Hermes, XCore, and model routing
- CPU, GPU, RAM, storage, network, VM, container, model, build, security, and energy metrics
- CrossPartition transfer queue
- Quarantine and software trust status
- Docker, WSL2, Hyper-V, Azure, GitHub, Linear, and Slack status
- Backup, vault, recovery, and audit evidence

### HELIOS DevHub

DevHub is the Developer-profile workspace for building new applications and AI services. It provides project templates, AI/provider selection, solution generation, tests, CI, Docker files, Azure/Bicep assets, local model configuration, and governed promotion workflows.

DevHub supports project templates for:

- WinUI 3 desktop applications
- WPF compatibility applications
- ASP.NET Core APIs
- .NET Worker Services
- CLI tools
- C++ native libraries and optimized services
- F# analytical, optimization, and machine-learning modules
- Python training, evaluation, and experimentation services
- Docker Compose stacks
- Azure Functions and Container Apps
- MCP servers, ChatGPT Apps, Copilot agents, and bounded automation workers

## Profile model

The supported profiles are:

1. Developer
2. SysAdmin
3. SysOps
4. Gamer
5. Studio
6. Personal
7. ServerBackground

Each profile owns a policy object containing:

- User identity and elevation policy
- Allowed partitions and folder roots
- Allowed software bundles
- Startup applications and background services
- CPU, GPU, memory, disk, network, VM, Docker, and model budgets
- Firewall and egress rules
- AIHub objectives, telemetry detail, and learning scope
- UI theme, background, lighting, animation, sound, and dashboard widgets
- CrossPartition import and export permissions

Developer is powerful but not an administrator. SysAdmin is the only interactive administrator profile. ServerBackground is a non-interactive service identity.

## SysAdmin security boundary

SysAdmin must be local-only, hidden from normal profile switching, and disabled by default. Activation requires physical presence and an approved local factor such as:

- FIDO2 or hardware security key
- Encrypted USB authorization token
- Windows Hello plus a local approval secret
- Recovery authorization stored offline

High-risk operations require two gates: an active SysAdmin session and a separate physical/local authorization. Examples include disk layout changes, driver promotion, Defender or firewall policy changes, vault recovery, certificate trust changes, TPM or Secure Boot operations, Azure production deployment, Entra/RBAC changes, and removal of quarantined evidence.

Cloud identity, chat commands, AI recommendations, and remote API requests may never activate SysAdmin by themselves.

## SSD layout

The default workstation design assumes two approximately 2 TB NVMe SSDs. Exact sizes are calculated at installation time and must preserve OEM recovery requirements.

### Disk 0: operating system and recovery

- EFI System Partition: existing/OEM-sized
- Microsoft Reserved Partition: existing/OEM-sized
- C: `MONADO_SYSTEM`, NTFS, Windows, boot-critical drivers, Program Files, Visual Studio core, SDK foundations
- R: `MONADO_RECOVERY`, NTFS, recovery assets, WinRE customization, signed repair packages
- V: `MONADO_VAULT_CTRL`, NTFS/BitLocker, small local authorization broker and policy evidence; not a general secret dump

Disk 0 is intentionally conservative. Source trees, model caches, Docker data, WSL distributions, large media, games, and general build outputs do not belong on C:.

### Disk 1: workload isolation

- D: `MONADO_DEV`, ReFS Dev Drive, developer source, package caches, build outputs, toolchains, Docker/WSL data
- M: `MONADO_AI_MODELS`, NTFS or ReFS according to model-runtime compatibility, models, embeddings, vector stores, evaluation data
- S: `MONADO_SECURE`, NTFS/BitLocker, protected profile data and encrypted VHDX vaults
- Q: `MONADO_QUARANTINE`, NTFS, no-execute by policy where practical, suspicious files, drivers, scripts, models, and forensic captures
- B: `MONADO_BACKUP`, NTFS, local snapshots, system images, manifests, export evidence

The installer must compute sizes from available capacity using policy percentages and minimum/maximum bounds. It must emit a plan and require explicit confirmation before any destructive action.

## Strict folder hierarchy

Every managed volume contains a signed `MONADO_VOLUME.json` manifest and a versioned root layout. Applications must use the manifest instead of guessing drive letters.

Canonical roots include:

```text
D:\
  00_SystemIndex\
  01_Projects\
  02_Developer\
  03_AIHub\
  04_ProfileWorkspaces\
  05_CommonCore\
  06_CrossPartition\
  07_SoftwareCatalog\
  08_ContainersVMs\
  09_Telemetry\
  10_TempBuild\

M:\
  00_ModelIndex\
  01_Providers\
  02_Models\
  03_Embeddings\
  04_VectorStores\
  05_Training\
  06_Evaluations\
  07_RoutingMemory\
  08_ApprovedDatasets\

S:\
  00_SecureIndex\
  01_Vaults\
  02_ProfileData\
  03_IdentityEvidence\
  04_RecoveryKeys\
  05_ComplianceExports\

Q:\
  00_QuarantineIndex\
  01_Incoming\
  02_Files\
  03_Drivers\
  04_Scripts\
  05_Installers\
  06_Repositories\
  07_Models\
  08_NetworkCaptures\
  09_SandboxResults\
  10_ColdEvidence\

B:\
  00_BackupIndex\
  01_Daily\
  02_Weekly\
  03_Monthly\
  04_SystemImages\
  05_RepoBundles\
  06_RecoveryMedia\
  07_AuditEvidence\
```

## Common Core software

Common Core is the small approved software baseline shared by non-admin interactive profiles:

- Windows Terminal and PowerShell 7
- Microsoft Edge
- GitHub CLI and Git Credential Manager
- .NET desktop/runtime dependencies required by HELIOS
- Defender, Firewall, Windows Security, Event Viewer, and approved diagnostics
- 7-Zip or equivalent approved archive tooling
- HELIOS Control Center, Tray Agent, Profile Broker, Telemetry Agent, CrossPartition Broker, and Quarantine Client
- Approved audio/video codecs and hardware control utilities

Common Core must remain deliberately small. Profile-specific software is layered through catalog bundles and cannot silently modify another profile.

## Profile software bundles

### Developer

Visual Studio, VS Code, GitHub Desktop, GitHub Copilot, Azure CLI, Azure Developer CLI, Bicep, Azure Functions Core Tools, Docker Desktop, WSL2, .NET SDKs, C++ toolchain, CMake, Ninja, F# tooling, Python, Node.js LTS, package managers, database clients, local model runtimes, and approved SDKs.

Developer optimization prioritizes compilation throughput, Dev Drive caches, container startup, local model inference, parallel tests, and incremental builds while reserving resources for the UI and security services.

### SysAdmin

Only signed system administration, recovery, firmware, security, disk, driver, certificate, identity, and policy tools. Browsers, mail, games, media libraries, and everyday development tools are excluded unless required for a reviewed repair procedure.

### SysOps

Broad security and performance operations: observability, event correlation, Defender and firewall dashboards, network diagnostics, performance tracing, VM/container health, Azure Monitor, Application Insights, Log Analytics, GitHub Actions, Azure DevOps, incident evidence, backup status, service lifecycle, and policy drift.

SysOps may operate bounded services and development environments but cannot obtain unrestricted administrator rights.

### Gamer

Approved launchers, games, controller/peripheral utilities, capture tools, overlays, shader caches, and safe mod managers. AIHub optimizes latency, frame pacing, GPU scheduling, background throttling, storage queues, and network paths while preserving the firewall and security floor.

### Studio

DAWs, audio interfaces and ASIO tools, plugins, sample managers, video editors, Blender, Unreal, visual tools, color management, media codecs, and rendering utilities. AIHub prioritizes low audio latency, stable CPU scheduling, disk streaming, GPU render queues, and plugin quarantine checks.

### Personal

Office/productivity, communication, browsing, document, finance, legal, photo, and note applications with quiet telemetry and conservative automation.

### ServerBackground

.NET Worker Services, Windows services, Docker services, reverse proxy, databases, message bus, AIHub workers, Hermes/XCore workers, monitoring, backup, and scheduled security services. No interactive desktop dependency is required.

## CrossPartition and air-gap workflow

Untrusted content may not enter trusted profile roots directly.

```text
External source
  -> Q:\01_Incoming
  -> hash, signature, type, archive, dependency, SBOM, Defender, YARA/Sigma, driver/model checks
  -> isolated sandbox or disposable VM
  -> Q:\09_SandboxResults
  -> policy decision
  -> D:\06_CrossPartition\PendingApproval
  -> local approval when required
  -> D:\06_CrossPartition\Sanitized
  -> destination profile import
```

Every transfer has a correlation ID, source, destination, hashes, scanner versions, findings, approving identity, timestamps, and rollback/removal instructions. High-risk drivers, kernel components, unsigned scripts, unknown models, and executables require SysAdmin physical approval.

## AIHub main brain

AIHub is a governed control and learning plane, not an unrestricted autonomous administrator. It receives events from the GUI, DevHub, Hermes, XCore, security services, VM/container monitors, GitHub, Azure, and the script runner.

AIHub responsibilities:

- Model and agent routing
- Resource optimization by profile
- CPU/GPU/RAM/storage/network and power forecasting
- VM, WSL2, Hyper-V, Docker, and container monitoring
- Build/test failure learning
- Security anomaly scoring and quarantine recommendations
- Software and driver health
- Profile-specific UX and dashboard recommendations
- Backup, recovery, and policy-drift reporting
- Model evaluation, cost, latency, privacy, and quality metrics

Learning data is separated into public, internal, sensitive, and local-only classes. Raw secrets, private chat content, authentication material, and quarantined evidence are never used for training without a separate approved policy.

## Automatic communication fabric

Components communicate through typed commands, events, health probes, and evidence records. The initial implementation may use local HTTP/gRPC and durable JSON/SQLite for development; the enterprise implementation maps the same contracts to Azure Service Bus, Event Hubs, SignalR, Application Insights, and managed identities.

Required services include:

- Profile Broker
- Storage and Partition Catalog
- CrossPartition Broker
- Quarantine Scanner
- Software Catalog
- Driver Trust Service
- AIHub Router and Memory
- Hermes/XCore Fleet Coordinator
- VM and Container Watcher
- Metrics Aggregator
- Script Execution Broker
- GitHub/Linear/Slack Integration Broker
- Azure/Foundry Adapter
- Evidence and Rollback Service

All writes are idempotent, correlation-aware, policy checked, logged, and bounded by explicit tool contracts.

## Monado profile login experience

The custom shell runs after Windows authentication; it does not replace Windows credential validation.

1. A fully rendered windswept grass environment appears with depth, lighting, fog, and grass blades that bend subtly toward cursor movement.
2. The dormant Monadoblade rests in the center. Cursor proximity activates a low-energy glow and lightweight particle response.
3. Selection opens a circular profile wheel. Seven holographic glyph sectors represent Developer, SysAdmin, SysOps, Gamer, Studio, Personal, and ServerBackground.
4. Rotating the wheel changes profile color, ambience, sound cue, telemetry preview, and blade material response.
5. The blade extends as the selected profile prepares. A central abstract kanji-inspired glyph illuminates through the core aperture while the outer mechanical ring spins in counter-rotating segments.
6. The system performs a fast policy, storage, services, and security readiness check.
7. On success the selected profile shell opens. On failure it remains in a safe neutral shell and explains the exact blocked dependency.

The animation must degrade gracefully. Low-power mode uses static or reduced-motion backgrounds, cached shaders, fewer particles, and no blocking startup network calls. Profile switching must be fast and should not require logoff unless the Windows security boundary requires a different user identity.

## Profile UI differentiation

- Developer: cyan/teal code-grid overlays, build graph, model router, repository and container metrics
- SysAdmin: muted red/gold vault interface, minimal network surface, integrity and recovery evidence
- SysOps: blue/amber operations cockpit, service graph, security posture, latency, VM/container and network metrics
- Gamer: electric green/blue low-latency overlay, frame time, GPU, CPU, storage, network and background-throttle metrics
- Studio: violet/magenta waveform and spatial-grid UI, ASIO, buffer, plugin, disk-stream and render metrics
- Personal: calm silver/blue environment, low-noise notifications, privacy and backup status
- ServerBackground: dark graphite daemon view, uptime, queues, services, firewall, health, power and model-worker metrics

## Azure boundary

Azure extends local capabilities through managed identity and private networking where practical:

- Azure AI Foundry and approved model providers
- Key Vault secret references
- Container Apps and Functions
- ACR
- Service Bus, Event Hubs, and SignalR
- Cosmos DB or PostgreSQL for approved metadata
- AI Search for approved indexed knowledge
- Storage/Data Lake for approved artifacts and evidence
- Azure Monitor, Log Analytics, and Application Insights

GitHub Actions uses OIDC. Production deployment, Entra, Graph consent, RBAC, Key Vault writes, and tenant publication require protected environment approval. The local system must continue operating when these services are unavailable.

## Implementation phases

1. Contracts and validation: profile, partition, hierarchy, software, GUI, AIHub, and service manifests
2. C# shared contracts and profile/storage services
3. WinUI 3 Control Center shell and profile switcher
4. DevHub templates and project generator
5. AIHub, Hermes, XCore, VM/container, memory, and metrics integration
6. CrossPartition, quarantine, software, and driver trust workflows
7. Docker local stack and Azure staging
8. Custom rendered login/profile wheel and profile-specific UX
9. Hardware authorization and SysAdmin activation
10. Recovery, installer, signed release, and enterprise promotion

No phase may silently execute destructive disk, security, identity, or production operations.
# HELIOS Platform 2 Implementation Plan

## Objective

Normalize the existing HELIOS, MONADO, Control Center, integration apps/drivers, AIHub, Hermes/XCore, Docker, Azure, Microsoft 365, GitHub, and enterprise automation work into one governed implementation without discarding working components.

## Phase 0 — Architecture and contracts

Deliverables:

- canonical storage layout contract;
- canonical profile contract;
- install-routing and quarantine contract;
- typed volume resolution contract;
- profile-to-service activation contract;
- destructive-operation approval contract;
- compatibility map for existing applications and services.

Exit criteria:

- all configuration files validate;
- no hard-coded drive letters outside storage adapters;
- no daily profile has local administrator membership;
- Disk 0 Dev Drive is native ReFS, not VHDX;
- Vault and Quarantine VHDX behavior is explicit and testable.

## Phase 1 — Storage and profile foundation

Implement:

- `HELIOS.Storage` volume resolver;
- `HELIOS.Profiles` policy engine;
- folder and ACL planner;
- profile activation receipts;
- MONADO profile-switch integration;
- Control Center storage/profile panels;
- OneDrive folder redirection for Personal and Work;
- server/background service identities.

No disk mutation occurs in CI or automatic setup. A local planner produces an exact review artifact before any apply operation.

## Phase 2 — Security, vault, quarantine, and recovery

Implement:

- BitLocker-backed Vault VHDX broker;
- Azure Key Vault reference mapping;
- read-only quarantine base VHDX;
- disposable differencing quarantine sessions;
- Defender and Malwarebytes policy adapters;
- no-execute quarantine enforcement;
- scan, classification, approval, and release receipts;
- WinRE and Defender Offline readiness checks;
- TPM 2.0, Secure Boot, BitLocker, and recovery-evidence preflight;
- absolute-path Windows repair tool invocation.

## Phase 3 — Driver and software lifecycle

Integrate the existing hardware detection and software catalog applications.

Implement:

- hardware inventory and stable hardware IDs;
- signed-driver catalog;
- USB/offline driver staging;
- hardware-match verification;
- vendor package provenance and hash verification;
- Razer Synapse/Chroma/THX core installation;
- Intel chipset/network/Bluetooth/graphics lifecycle;
- NVIDIA Game Ready versus Studio recommendation and guarded replacement;
- software bundle selection by profile;
- high-throughput parallel download with bounded concurrency and integrity verification;
- install routing into CORE, DEVDRIVE, CROSS, or Disk 1 domains.

## Phase 4 — Control Center and MONADO UX

Control Center modules:

- Mission Control
- Profiles
- Storage
- Install Router
- Security
- Vault
- Quarantine
- Drivers and Firmware
- Recovery and USB
- Performance
- Gaming
- Studio
- Developer
- AIHub and Hermes
- Server and Networking
- Azure and Microsoft 365
- GitHub and DevOps
- Audit and Evidence

MONADO profile switching changes theme, dynamic background, startup applications, power policy, GPU policy, audio policy, network policy, security posture, storage shortcuts, and AIHub mode.

## Phase 5 — AIHub, Hermes/XCore, Docker, WSL2, and Hyper-V

Retain and harden existing AIHub and trainer behavior.

Canonical topology:

- Docker gateway/API;
- Docker GUI/control service;
- WSL2 GPU trainer;
- Hyper-V security-isolation VM;
- Windows C# control plane;
- Python learning and experimentation sidecar;
- C++ security/performance hot paths;
- F# policy scoring and recommendation.

All AI and agent writes pass through the HELIOS policy and approval layer. Training memory is redacted before cloud synchronization.

## Phase 6 — Azure, Entra, Intune, Purview, and Microsoft 365

Implement plan-first infrastructure for:

- Azure VNet and private endpoints;
- Key Vault;
- Container Apps and Functions;
- Service Bus and Event Hubs;
- Storage and Data Lake;
- Cosmos DB and AI Search;
- Azure AI Foundry;
- Log Analytics and Application Insights;
- Entra workload identities;
- GitHub OIDC;
- Intune enrollment and configuration readiness;
- Purview data classification and compliance readiness;
- Microsoft 365 Apps, Teams, SharePoint, OneDrive, Power Platform, Power BI, and Copilot integration.

Production remains protected by environment approval, immutable source SHA, reviewed what-if output, and separate apply authorization.

## Phase 7 — SOLGATE clean-install and recovery wizard

Build one USB experience with two selectable lanes:

- Security and recovery;
- Performance and software provisioning.

The wizard performs:

- signed-media verification;
- TPM/Secure Boot/UEFI checks;
- storage-controller and NVMe detection;
- signed driver matching and injection;
- offline malware/rootkit scan readiness;
- Windows installation;
- partition-plan generation;
- HELIOS profile and folder provisioning;
- approved software download and installation;
- security baseline staging;
- recovery-media generation;
- final evidence report.

## Phase 8 — Repository normalization

Target structure:

```text
apps/
src/
services/
config/
infra/
scripts/
tests/
docs/
legacy/
```

Rules:

- working code is moved only through reviewed PRs;
- prototypes remain preserved under `legacy/` until replaced and verified;
- C# is the control-plane spine;
- PowerShell is a signed and bounded system adapter;
- Python is the AI/data sidecar;
- Bicep is the Azure infrastructure source;
- JSON schemas define platform contracts;
- every dangerous operation has a plan, confirmation, receipt, and rollback path.

## Immediate next PRs

1. Add JSON schemas and validators for the new platform contracts.
2. Implement typed C# models for storage, profiles, routing, and approvals.
3. Add storage/profile contract tests.
4. Connect MONADO profile UI to the profile service.
5. Connect Control Center to the same state store.
6. Import the hardware-detection and software-catalog apps behind interfaces.
7. Implement vault and quarantine brokers in plan-only mode.
8. Add SOLGATE preflight and driver-catalog contracts.

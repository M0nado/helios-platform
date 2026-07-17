# HELIOS Platform 2 Architecture

## Status
Authoritative architecture foundation for the next HELIOS implementation phase.

This document preserves the approved two-drive design and profile model while reorganizing the platform into explicit system domains, policy contracts, applications, services, and infrastructure.

## Core design

HELIOS Platform 2 is a local-first, profile-aware Windows workstation and server control plane with cloud extension through Azure and Microsoft 365.

The operating model is:

```text
Profile selects behavior
Policy grants capability
Storage domain selects location
Install router classifies software
Security broker controls trust
Control Center exposes state and approvals
Azure extends approved capabilities
```

## Canonical repository domains

```text
apps/
  HELIOS.ControlCenter/
  MONADO.ProfileShell/
  LUMEN.Setup/
  SOLGATE.UsbWizard/

src/
  HELIOS.Core/
  HELIOS.Profiles/
  HELIOS.Storage/
  HELIOS.InstallRouting/
  HELIOS.Security/
  HELIOS.Quarantine/
  HELIOS.Vault/
  HELIOS.Recovery/
  HELIOS.Drivers/
  HELIOS.Performance/
  HELIOS.AIHub/
  HELIOS.Cloud/
  HELIOS.Fabric/

services/
  HELIOS.ProfileService/
  HELIOS.InstallWatch/
  HELIOS.SecurityService/
  HELIOS.ServerNode/
  HELIOS.AIHubWorker/

config/
  platform/
  storage/
  profiles/
  security/
  drivers/
  software/
  cloud/

infra/
  azure/
  github/
  m365/

scripts/
  windows/
  recovery/
  usb/
  azure/

tests/
  contracts/
  integration/
  safety/
```

## Two-drive architecture

The approved workstation model uses two nominal 2 TB NVMe drives, with approximately 1.0 TB actively allocated on Disk 0 and approximately 1.9 TB available on Disk 1.

### Disk 0: trusted low-latency platform disk

Disk 0 carries the operating system, native Dev Drive, shared cross-profile platform runtimes, recovery controls, and the encrypted vault container.

```text
EFI / MSR / WinRE       Windows-managed boot and recovery partitions
C: CORE                 320 GB NTFS
P: DEVDRIVE             500 GB ReFS Dev Drive
X: CROSS                140 GB NTFS
unallocated reserve     remaining capacity
```

The vault is not a physical partition. It is a dynamically expanding, BitLocker-protected VHDX stored under `X:\Security\Vaults\HELIOS-Vault.vhdx` and mounted as `V:` only after approved local authentication.

### Disk 1: flexible domain data disk

Disk 1 uses one large NTFS volume to avoid rigid capacity waste.

```text
D: DOMAINS              approximately 1.9 TB NTFS
```

Strict top-level hierarchy and ACLs provide separation:

```text
D:\
  Games\
  MusicStudio\
  Personal\
  OneDrive\
  Work\
  Media\
  AIData\
  ServerNode\
  Sandbox\
  Quarantine\
  Backups\
  Archives\
```

The quarantine container is a dynamically expanding differencing VHDX chain stored under `D:\Quarantine\Containers\`, mounted only for inspection. The base image remains read-only; session differencing disks are disposable.

## Storage-domain semantics

### CORE

`C:` is boot-critical and installation-critical.

Allowed:
- Windows
- signed drivers and firmware utilities
- Microsoft 365 desktop applications
- Razer Synapse, Chroma, THX Spatial Audio
- NVIDIA driver package and control components
- Defender, Malwarebytes, Control Center shell
- Visual Studio core and Windows SDK foundations when required by installers

Not allowed:
- repositories
- games
- media libraries
- model weights
- raw downloads
- virtual machine disks

### DEVDRIVE

`P:` is a native ReFS Dev Drive, never a VHDX.

```text
P:\
  Repos\
  Worktrees\
  Builds\
  Artifacts\
  Caches\NuGet\
  Caches\npm\
  Caches\pip\
  Caches\cargo\
  Caches\vcpkg\
  Environments\Python\
  Environments\Node\
  Containers\
  WSL\
  Tools\
```

This placement maximizes repository, dependency, build, container, and WSL throughput while minimizing contention with Windows and large media workloads.

### CROSS

`X:` contains only capabilities genuinely shared by multiple profiles.

```text
X:\
  Platform\
    Config\
    Policies\
    Manifests\
    Logs\
    Telemetry\
  Compute\
    CUDA\
    ONNX\
    DirectML\
    TensorRT\
    OpenVINO\
  Services\
    DockerShared\
    HyperVShared\
    NetworkControl\
    ServerControl\
  Security\
    Vaults\
    Certificates\
    KeyReferences\
    Audit\
  Recovery\
    Drivers\
    WinRE\
    UsbWizard\
    RepairPackages\
```

There is no generic Common partition. Shared Microsoft 365 applications remain installed in CORE; shared configuration and integration state belongs in CROSS. User documents and OneDrive remain on Disk 1.

## Performance expectations

Disk 0 separates three dominant I/O patterns:

- `C:` handles Windows, applications, drivers, paging, updates, and boot activity.
- `P:` handles high-file-count developer operations, builds, package extraction, repositories, and container/WSL workloads.
- `X:` handles shared compute runtimes, platform state, security containers, and recovery assets.

Expected effects:

- lower build and package-cache contention than placing development data on `C:`;
- no VHDX overhead for Dev Drive;
- predictable OS free space and servicing behavior;
- faster profile switching because common runtimes are installed once;
- better space utilization than creating separate physical Game, Music, Personal, Work, Server, Sandbox, and Quarantine partitions;
- easier backup and expansion because large data remains inside one Disk 1 volume.

The architecture does not claim that partitions increase raw NVMe throughput. The benefit is workload isolation, free-space governance, predictable scanning, simplified routing, and reduced duplicate installations.

## Profiles

Canonical interactive profiles:

- Personal
- Developer
- Gamer
- Studio
- Work
- SysOps
- SysAdmin

Canonical non-interactive profile:

- Server / Background

All daily profiles are standard users. SysAdmin is the only full local administrator and is disabled or locked when not in use.

SysAdmin activation requires approved local proof such as:

- FIDO2 security key or approved USB authorization token;
- strong local passphrase;
- Windows Hello confirmation;
- optional two-factor combination for destructive operations.

## Driver and software policy

Core device software is installed once in CORE and profile behavior is changed through policy rather than duplicate installation.

Shared core stack:

- Razer Synapse
- Razer Chroma
- THX Spatial Audio
- Intel chipset, network, Bluetooth, graphics, and firmware packages
- NVIDIA driver and NVIDIA application/control layer
- Microsoft 365 Apps
- Defender and Malwarebytes

NVIDIA policy uses one installed driver branch at a time. Control Center may recommend Game Ready or Studio based on workload, but switching requires a guarded driver replacement and reboot; profiles do not attempt to keep both kernel driver branches active simultaneously.

## Clean-install and USB trust chain

SOLGATE USB Wizard owns clean installation and recovery media creation.

Stages:

1. Verify ISO, package, and driver signatures.
2. Verify UEFI, Secure Boot, TPM 2.0, NVMe visibility, and WinRE readiness.
3. Run offline malware/rootkit scanning where supported.
4. Preserve required BitLocker recovery evidence.
5. Plan partitions without applying changes.
6. Require exact disk identity and physical confirmation.
7. Install Windows from known-good media.
8. Inject only signed, hardware-matched drivers.
9. Run Windows Update and vendor verification.
10. Create profiles, folders, ACLs, vault, quarantine container, and Control Center state.
11. Download approved software through catalog manifests with hash and signature verification.
12. Apply security baseline in audit-first mode, then approved enforcement.

No software can guarantee removal of every possible firmware compromise. HELIOS therefore distinguishes OS-level clean installation, offline scanning, boot-chain verification, and separately managed firmware trust.

## Cloud and enterprise integration

Azure and Microsoft 365 extend the local platform through:

- Entra ID
- Intune
- Purview
- Azure Key Vault
- Azure Virtual Network and private endpoints
- Azure AI Foundry
- Azure Monitor and Application Insights
- Container Apps, Functions, Service Bus, Storage, Cosmos DB, and AI Search where approved
- Microsoft 365 Apps, Teams, SharePoint, OneDrive, Power Platform, Power BI, and Copilot

Local vault values are never mirrored blindly. The vault broker maps approved local secret references to Azure Key Vault secret identifiers and supports one-way or policy-approved synchronization.

## Existing application integration

The existing Control Center, MONADO profile GUI, integration apps/drivers pack, AIHub server, Hermes/XCore trainer, Docker topology, and enterprise automation fabric are retained as implementation inputs.

They are integrated through typed contracts rather than copied into one process:

```text
MONADO profile shell
  -> HELIOS Profile Service
  -> HELIOS policy engine
  -> driver/performance/security adapters
  -> storage and install-routing services
  -> AIHub and Hermes runtime endpoints
  -> Azure/M365 integration broker
```

## Implementation rule

All destructive operations are plan-first and approval-gated. Repository changes, disk changes, driver changes, TPM/BitLocker changes, Azure deployment, Entra consent, and production promotion are separate approval boundaries.

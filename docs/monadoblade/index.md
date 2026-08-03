# Monadoblade

Monadoblade is HELIOS's governed six-identity Windows experience: one profile contract, one Monado energy/state model, distinct WinUI shells, a bounded C++ living environment, a separate ALVIS command surface, and a plan-only USB integration.

## Start here

- [Delivery Fabric v2 architecture](../architecture/MONADOBLADE_DELIVERY_FABRIC_V2.md)
- [Permanent identity contract](../../config/profiles/monadoblade-profiles.v2.json)
- [Shell and energy contract](../../config/gui/monado-profile-shell.v2.json)
- [Living environment contract](../../config/experience/monadoblade-living-environments.v1.json)
- [Wyvern, Chroma, and particle effects contract](../../config/experience/monadoblade-effects.v1.json)
- [Plan-only AIHub engine registry](../../config/aihub/monadoblade-engine-registry.v1.json)
- [Storage plan template](../../config/storage/monadoblade-storage-plan-template.v2.json)
- [Repository and ALVIS integration contract](../../config/integrations/monadoblade-delivery-fabric.v1.json)

## Permanent wheel

| Identity | Kanji | Role |
|---|---:|---|
| Core | 核 | Balanced daily center |
| Developer | 創 | Code, build, native, and test |
| Studio | 響 | Audio, media, and rendering |
| Gamer | 迅 | Low-latency play |
| AI/Server | 智 | Models, agents, queues, and services |
| Sysadmin | 統 | Hidden local/offline administration |

Personal and SysOps are overlays. Airgap, Recovery, and Quarantine are workflows. They do not add wheel sectors.

## Safety boundary

This lane does not replace Windows authentication, capture passwords, modify boot configuration, partition disks, deploy Azure, grant tenant permissions, or let ALVIS execute consequential actions. It defines versioned contracts and validation for later reviewed implementations.

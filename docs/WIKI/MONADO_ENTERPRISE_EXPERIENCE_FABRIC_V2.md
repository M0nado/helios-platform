# Monado enterprise experience fabric v2 (wiki source)

Canonical source: `docs/architecture/MONADO_ENTERPRISE_EXPERIENCE_FABRIC_V2.md`  
Contract root: `config/monadoblade/experience-fabric/monado-enterprise-experience-fabric.v2.json`

## Summary

- authoritative v2 contracts are machine-readable and execution-disabled by default;
- profile/storage/experience/ALVIS/chroma-wyvern/repository/synchronization contracts are split by concern;
- OpenAI actions are strict proposal records with approval, expiry, evidence, and rollback metadata;
- GUI and USB implementations remain in dedicated issue/module scopes and are linked, not duplicated.

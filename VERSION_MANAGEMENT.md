# Version Management

## Semantic Versioning

All HELIOS submodules use MAJOR.MINOR.PATCH versioning:

```
MAJOR.MINOR.PATCH (-PrereleaseTAG)

Examples:
  v1.0.0             Initial release
  v1.0.1             Bug fix
  v1.1.0             New feature, backward compatible
  v2.0.0             Breaking changes, new API
  v2.0.0-alpha       Alpha pre-release
  v2.0.0-beta.1      Beta pre-release
```

| Component | When Changed | Example |
|---|---|---|
| MAJOR | Breaking changes to API | 1.0.0 → 2.0.0 |
| MINOR | New features, backward compatible | 1.0.0 → 1.1.0 |
| PATCH | Bug fixes | 1.0.0 → 1.0.1 |

## Compatibility Matrix

HELIOS Platform v1.0.0 requires these submodule versions:

```
HELIOS v1.0.0 = Phase 0 + Phase 1 Complete

Phase 0 Submodules (v1.0+):
├─ PHASE-0-USB-Creator v1.0.0
├─ PHASE-0-Windows-Installer v1.0.0
├─ PHASE-0-Partition-Manager v1.0.0
└─ PHASE-0-System-Setup v1.0.0

Phase 1 Submodules (v1.0+):
├─ PHASE-1-AppLocker v1.0.0
├─ PHASE-1-Windows-Firewall v1.0.0
├─ PHASE-1-Credential-Vault v1.0.0
└─ PHASE-1-Malware-Quarantine v1.0.0

Version Range:
  ^1.0.0 = 1.0.0, 1.0.1, 1.0.2, ..., 1.9.9
           (compatible with 1.0.0, not with 2.0.0)
```

## Backward Compatibility

### Same Major Version = Backward Compatible

```
AppLocker v1.0.0 → v1.1.0 (COMPATIBLE)
  ✓ Old code still works
  ✓ New features available
  ✓ No migration needed

AppLocker v1.5.0 → v2.0.0 (INCOMPATIBLE)
  ✗ Old code may break
  ✓ Migration path provided
  ✓ Documentation updated
```

## Breaking Changes Policy

### Announce Early

```
4 weeks before: Announce breaking changes
  └─ What's changing
  └─ When it's changing
  └─ Migration path

2 weeks before: Beta release for testing
  └─ v2.0.0-beta available
  └─ Time to test migration

Release day: v2.0.0 stable
  └─ Full migration guide in release notes
```

### Deprecation Path

```
v1.5.0: Mark old API as deprecated
  ├─ Function still works
  └─ Warning shown to users
  
v1.6.0 onwards: Show warnings
  └─ Encourage migration

v2.0.0: Remove old API
  └─ Only new API available
```

## Release Checklist

Before releasing a new version:

```
1. Update version in STATUS.json
2. Update CHANGELOG.md with all changes
3. Run full test suite: Invoke-Pester tests/ -Verbose
4. All tests must pass
5. Code review completed
6. Create Git tag: git tag -a v1.1.0 -m "Release v1.1.0"
7. Push to repository: git push origin v1.1.0
8. Update documentation
9. Notify dependent teams
10. Archive previous version
```

## Version Support Timeline

```
v1.0.0 Lifecycle:
├─ Jan 28: Release
├─ Feb 11: v1.1.0 released (bug fixes in v1.0.0 stop)
├─ Mar 14: v1.2.0 released (v1.0.0 now outdated)
├─ Oct 15: v2.0.0 released (v1.2.0 is latest stable 1.x)
└─ Dec 1: v1.2.0 support ends (2 months after v2.0.0)

Support phases:
├─ Pre-release (0-4 weeks): Early access, frequent updates
├─ Stable (4-26 weeks): Production use, patches only
└─ LTS (26+ weeks): Critical fixes only
```

## Coordinated Phase Releases

All submodules in a phase release together with same version:

```
Phase 1 Release: All versions v1.0.0
├─ AppLocker v1.0.0 ────┐
├─ Firewall v1.0.0      ├─→ HELIOS Phase 1 v1.0.0
├─ Vault v1.0.0         │
└─ Quarantine v1.0.0 ───┘

Released: Jan 28, 2024
All tagged, all documented together
```

---

**Document Version**: 1.0  
**Last Updated**: 2024  
**Maintained By**: Release Team

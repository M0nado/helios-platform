# {{BUILD_NAME}} - Build Integrity Report

**Template Version:** 1.0  
**Report Date:** {{REPORT_DATE}}  
**Validation Status:** ✅ {{VALIDATION_STATUS}}

---

## ✅ Validation Results

### File Integrity

- **Files Checked:** {{FILES_CHECKED}}
- **Files Valid:** {{FILES_VALID}} ✓
- **Files Corrupted:** {{FILES_CORRUPTED}} ✗
- **Files Missing:** {{FILES_MISSING}} ⚠

**Overall:** {{OVERALL_STATUS}}

### Checksum Verification

| File | Expected | Actual | Status |
|------|----------|--------|--------|
| {{FILE_1}} | {{EXPECTED_1}} | {{ACTUAL_1}} | ✓ |
| {{FILE_2}} | {{EXPECTED_2}} | {{ACTUAL_2}} | ✓ |

### Dependency Validation

- All required dependencies: ✓ Present
- Version compatibility: ✓ Verified
- Conflicts detected: ✗ None

### Test Results

| Test Suite | Passed | Total | Pass Rate |
|-----------|--------|-------|-----------|
| Unit Tests | {{UNIT_PASSED}} | {{UNIT_TOTAL}} | {{UNIT_RATE}}% |
| Integration | {{INT_PASSED}} | {{INT_TOTAL}} | {{INT_RATE}}% |

---

**For Modification History:** [MODIFICATION_HISTORY.md](./MODIFICATION_HISTORY.md)  
**Generated from template version 1.0**

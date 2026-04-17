# {{BUILD_NAME}} - Configuration Guide

**Template Version:** 1.0  
**Last Updated:** {{LAST_UPDATED}}

---

## ⚙️ Configuration

### Configuration File

**Location:** `./config.json`

### Default Configuration

```json
{
  "build": {
    "name": "{{BUILD_NAME}}",
    "version": "{{BUILD_VERSION}}"
  },
  "{{CONFIG_SECTION_1}}": {
    "{{SETTING_1}}": {{VALUE_1}},
    "{{SETTING_2}}": "{{VALUE_2}}"
  }
}
```

### Environment Variables

| Variable | Default | Purpose |
|----------|---------|---------|
| {{ENV_VAR_1}} | {{DEFAULT_1}} | {{PURPOSE_1}} |
| {{ENV_VAR_2}} | {{DEFAULT_2}} | {{PURPOSE_2}} |

---

**See Also:** [README.md](./README.md#-configuration)  
**Generated from template version 1.0**

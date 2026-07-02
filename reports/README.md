# HELIOS Generated Reports

This directory is intentionally kept lightweight in git.

Run the command center to regenerate local reports:

```bash
./helios.sh all
./helios.sh dashboard
```

Generated JSON/Markdown reports are ignored by default and should be uploaded as CI artifacts or served through the local/status dashboard instead of being committed repeatedly.

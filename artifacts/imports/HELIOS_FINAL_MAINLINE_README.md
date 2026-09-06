# HELIOS final mainline artifacts

These checksum records identify externally preserved immutable packages used to generate and audit the final mainline overlay.

The binary packages are not interpreted merely because a matching filename exists. Before use, operators must:

1. obtain the package from the governed evidence location;
2. calculate SHA-256 locally;
3. compare it to the repository checksum record;
4. run the package validation suite;
5. generate a fresh file-level plan against current `origin/main`;
6. review every conflict before apply.

Neither artifact authorizes Azure deployment, tenant consent, secret creation, workstation mutation, or production activation.

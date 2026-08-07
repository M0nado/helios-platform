# Azure CLI bootstrap

HELIOS separates local tooling verification from authenticated deployment. The
bootstrap does not create credentials, change subscriptions, grant RBAC roles,
or deploy resources.

## Local setup

1. Install Azure CLI and Azure Developer CLI from Microsoft's supported packages.
2. Run `scripts/setup/verify-azure-cli.sh` to verify the commands, Azure CLI
   version, Bicep support, and current account context.
3. If needed, run `az login`, then explicitly select the intended subscription
   with `az account set --subscription <subscription-id>`.
4. Run the verifier again and review the tenant and subscription it prints.

The verifier is read-only and never installs software or mutates Azure state.

## GitHub Actions

Workflows must use Azure workload identity federation. Configure
`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` as protected
environment values; never configure `AZURE_CLIENT_SECRET`.

**HELIOS Deployment Readiness** validates contracts and compiles the primary
Bicep template but cannot deploy. **Azure Infrastructure** performs authenticated
validation, what-if, and an explicitly requested deployment. Production must use
a protected environment with reviewers and retain what-if evidence.

## Approval boundary

Entra/RBAC changes, Key Vault secret operations, production deployment,
firewall changes, and tenant-wide Graph consent require a scoped issue,
reviewable preview, explicit approval, rollback instructions, and correlation ID.
Microsoft Copilot, Hermes, XCore, and local scripts cannot bypass those gates.

import re
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = PROJECT_ROOT.parents[1]
WORKFLOW = REPOSITORY_ROOT / ".github/workflows/helios-cloud-deploy.yml"
SUBNET_PARAMETER = "containerAppsInfrastructureSubnetId"


class AzureDeploymentContractTests(unittest.TestCase):
    def test_every_reviewed_workflow_invocation_passes_subnet(self) -> None:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        invocations = re.findall(
            r"az deployment group (?:what-if|create) \\\n(?:(?!\n\s+- name:).)*?--output (?:json|json\)\")",
            workflow,
            flags=re.DOTALL,
        )

        self.assertEqual(3, len(invocations), "expected preview, drift-check, and deployment")
        for invocation in invocations:
            self.assertEqual(
                1,
                invocation.count(f'{SUBNET_PARAMETER}="${{HELIOS_CONTAINER_APPS_INFRASTRUCTURE_SUBNET_ID}}"'),
            )

        self.assertIn('"${{ inputs.targetEnvironment }}" == "azure-prod"', workflow)
        self.assertGreaterEqual(workflow.count("Production requires protected environment variable"), 2)

    def test_review_evidence_binds_subnet_parameter(self) -> None:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn(
            "containerAppsInfrastructureSubnetId: $containerAppsInfrastructureSubnetId",
            workflow,
        )
        self.assertIn(
            ".parameters == {location: $location, environmentName: $environmentName",
            workflow,
        )

    def test_workflow_blocks_in_place_subnet_migration(self) -> None:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        self.assertGreaterEqual(
            workflow.count(' --resource-type "Microsoft.App/managedEnvironments"'),
            2,
        )
        self.assertGreaterEqual(
            workflow.count('deploy a replacement environment and migrate with reviewed rollback evidence.'),
            4,
        )

    def test_private_ingress_verification_is_control_plane_aware(self) -> None:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn('if [[ "${ingress_external}" != "true" ]]; then', workflow)
        self.assertIn(' --resource-type "Microsoft.App/containerApps/authConfigs"', workflow)
        self.assertIn(
            "Private ingress prevents direct runner probes; validated readiness and fail-closed auth policy from ARM state.",
            workflow,
        )

    def test_connector_entry_point_cannot_be_external_in_production(self) -> None:
        connector = (PROJECT_ROOT / "infra/connector.bicep").read_text(encoding="utf-8")
        self.assertIn(
            "environmentName != 'prod' || !empty(containerAppsInfrastructureSubnetId)",
            connector,
        )
        self.assertIn(
            "fail('Production requires a delegated Container Apps infrastructure subnet.')",
            connector,
        )
        self.assertIn("external: empty(validatedContainerAppsInfrastructureSubnetId)", connector)
        self.assertNotIn("external: empty(containerAppsInfrastructureSubnetId)", connector)

    def test_direct_callers_forward_subnet_and_reject_empty_production(self) -> None:
        callers = [
            "Deploy-HeliosAzureConnector.ps1",
            "Invoke-HeliosProvisionPreview.ps1",
            "Invoke-HeliosEdgeAutomation.ps1",
            "Connect-HeliosAzureInteractive.ps1",
            "bootstrap-helios-azure-oidc.sh",
        ]
        for caller in callers:
            source = (PROJECT_ROOT / "scripts" / caller).read_text(encoding="utf-8")
            self.assertIn(SUBNET_PARAMETER, source, caller)
            self.assertRegex(source, r"prod.*(?:IsNullOrWhiteSpace|-z)", caller)

    def test_configure_validates_preview_before_persisting_subnet_binding(self) -> None:
        interactive = (PROJECT_ROOT / "scripts" / "Connect-HeliosAzureInteractive.ps1").read_text(encoding="utf-8")
        preview_call = re.search(r"\[void\] \(Invoke-BicepPreview", interactive)
        persist_call = re.search(
            r"Set-GitHubEnvironmentVariables\s*`\s*\n\s*-Values \$pendingGitHubEnvironmentValues",
            interactive,
        )
        self.assertIsNotNone(preview_call)
        self.assertIsNotNone(persist_call)
        self.assertLess(preview_call.start(), persist_call.start())


if __name__ == "__main__":
    unittest.main()

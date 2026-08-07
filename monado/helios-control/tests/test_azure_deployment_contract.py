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
            workflow.count('az resource show \\'),
            2,
        )
        self.assertGreaterEqual(
            workflow.count(' --resource-type "Microsoft.App/managedEnvironments"'),
            2,
        )
        self.assertNotIn('az resource list \\\n            --resource-group "${AZURE_RESOURCE_GROUP}" \\\n            --resource-type "Microsoft.App/managedEnvironments"', workflow)
        self.assertGreaterEqual(
            workflow.count('deploy a replacement environment and migrate with reviewed rollback evidence.'),
            4,
        )

    def test_private_production_requires_self_hosted_runner_for_boundary_checks(self) -> None:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn('Require self-hosted runner for private production boundary verification', workflow)
        self.assertIn('RUNNER_ENVIRONMENT: ${{ runner.environment }}', workflow)
        self.assertIn(
            "Production private-network validation requires a self-hosted runner with VNet and private DNS reachability to the internal Container Apps ingress.",
            workflow,
        )

    def test_verification_waits_for_ready_revision_and_immutable_image(self) -> None:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn('latest_revision="$(jq -r \'.properties.latestRevisionName // empty\' <<<"${app_state}")"', workflow)
        self.assertIn('latest_ready_revision="$(jq -r \'.properties.latestReadyRevisionName // empty\' <<<"${app_state}")"', workflow)
        self.assertIn('active_image="$(jq -r \'.properties.template.containers[0].image // empty\' <<<"${app_state}")"', workflow)
        self.assertIn('"${latest_revision}" == "${latest_ready_revision}"', workflow)
        self.assertIn('"${active_image}" == "${IMAGE_REFERENCE}"', workflow)

    def test_connector_entry_point_keeps_reachable_ingress(self) -> None:
        connector = (PROJECT_ROOT / "infra/connector.bicep").read_text(encoding="utf-8")
        self.assertIn(
            "environmentName != 'prod' || !empty(containerAppsInfrastructureSubnetId)",
            connector,
        )
        self.assertIn(
            "fail('Production requires a delegated Container Apps infrastructure subnet.')",
            connector,
        )
        self.assertIn("external: true", connector)
        self.assertNotIn("external: empty(validatedContainerAppsInfrastructureSubnetId)", connector)

    def test_connector_sets_user_defined_routing_for_production_subnet(self) -> None:
        connector = (PROJECT_ROOT / "infra/connector.bicep").read_text(encoding="utf-8")
        self.assertIn("outboundType: 'UserDefinedRouting'", connector)
        self.assertIn("outboundType: 'LoadBalancer'", connector)

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

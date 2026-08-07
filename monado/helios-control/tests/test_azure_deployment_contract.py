import re
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = PROJECT_ROOT.parents[1]
WORKFLOW = REPOSITORY_ROOT / ".github/workflows/helios-cloud-deploy.yml"
AZURE_INFRA_WORKFLOW = REPOSITORY_ROOT / ".github/workflows/azure-infra.yml"
AZURE_MAIN_BICEP = REPOSITORY_ROOT / "infra/azure/main.bicep"
NETWORK_MODULE = REPOSITORY_ROOT / "infra/azure/modules/network.bicep"
HUB_GOVERNANCE_MODULE = REPOSITORY_ROOT / "infra/azure/modules/hub-governance.bicep"
PRIVATE_EDGE_MODULE = REPOSITORY_ROOT / "infra/azure/modules/private-edge.bicep"
NETWORK_PATHS = PROJECT_ROOT / "config/network-paths.json"
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
        self.assertGreaterEqual(
            workflow.count('fromJSON(\'["self-hosted","linux","x64","helios-azure"]\')'),
            2,
        )
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
        internal_dns_module = (PROJECT_ROOT / "infra/containerapp-internal-dns.bicep").read_text(encoding="utf-8")
        self.assertIn("outboundType: 'UserDefinedRouting'", connector)
        self.assertIn("outboundType: 'LoadBalancer'", connector)
        self.assertIn("module containerAppsInternalDns 'containerapp-internal-dns.bicep'", connector)
        self.assertIn("zoneName: environment.properties.defaultDomain", connector)
        self.assertIn("virtualNetworkId: containerAppsVirtualNetwork.id", connector)
        self.assertIn("scopedContainerAppsInfrastructureSubnetId", connector)
        self.assertIn(
            "containerAppsInfrastructureSubnetId must target a subnet in the deployment subscription and resource group.",
            connector,
        )
        self.assertIn("resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01'", internal_dns_module)
        self.assertIn("resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01'", internal_dns_module)
        self.assertIn("resource wildcardRecord 'Microsoft.Network/privateDnsZones/A@2020-06-01'", internal_dns_module)

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

    def test_publish_requires_subnet_argument_to_match_protected_environment_binding(self) -> None:
        interactive = (PROJECT_ROOT / "scripts" / "Connect-HeliosAzureInteractive.ps1").read_text(encoding="utf-8")
        self.assertIn("Get-GitHubEnvironmentVariableValue", interactive)
        self.assertIn("HELIOS_CONTAINER_APPS_INFRASTRUCTURE_SUBNET_ID", interactive)
        self.assertRegex(
            interactive,
            r"if \(\$EnvironmentName -eq 'prod'\) \{\s+\$protectedSubnetBinding = Get-GitHubEnvironmentVariableValue",
        )
        self.assertIn(
            "Publish requires -ContainerAppsInfrastructureSubnetId to match protected environment binding HELIOS_CONTAINER_APPS_INFRASTRUCTURE_SUBNET_ID.",
            interactive,
        )

    def test_azure_infra_routes_prod_apply_through_protected_environment(self) -> None:
        workflow = AZURE_INFRA_WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("Restrict production apply to main branch", workflow)
        self.assertIn("Production deploy requests are restricted to refs/heads/main.", workflow)
        self.assertIn("Require reviewed rollback plan for production deploy", workflow)
        self.assertIn("Require reviewed rollback plan before production apply", workflow)
        self.assertIn("Deploy production after protected approval", workflow)
        self.assertIn("Capture reviewed production what-if", workflow)
        self.assertIn("Re-run production what-if after approval", workflow)
        self.assertIn("Fail on production what-if drift", workflow)
        self.assertIn("environment: azure-prod", workflow)
        self.assertIn("platform_address_space:", workflow)
        self.assertIn("cosmos_account_id:", workflow)
        self.assertIn("container_registry_id:", workflow)
        self.assertIn("edge_route_cutover_approved:", workflow)
        self.assertIn("key_vault_private_cutover_approved:", workflow)
        self.assertIn("enabled_egress_profiles:", workflow)
        self.assertIn("connector_relay_destinations:", workflow)
        self.assertIn("resolved_location=", workflow)
        self.assertIn("edgeRouteCutoverApproved=", workflow)
        self.assertIn("Input location '", workflow)
        self.assertIn("platformAddressSpace=", workflow)
        self.assertIn("cosmosAccountId=", workflow)
        self.assertIn("containerRegistryId=", workflow)
        self.assertIn("keyVaultPrivateCutoverApproved=", workflow)
        self.assertIn("enabledEgressProfiles=", workflow)
        self.assertIn("connectorRelayDestinations=", workflow)
        self.assertIn("Require reviewed connector public-origin rebinding before edge cutover", workflow)
        self.assertIn("HELIOS_CONNECTOR_PUBLIC_BASE_URL", workflow)
        self.assertIn("HELIOS_CONNECTOR_PUBLIC_BASE_URL must match the deployed Front Door endpoint origin", workflow)
        self.assertIn("HELIOS_PUBLIC_BASE_URL", workflow)
        self.assertIn("microsoftIdentityAzure for Cosmos readiness egress", workflow)
        self.assertIn("containerAppsPlatform for Container Apps platform egress", workflow)
        self.assertIn("containerRegistryDataPlane for ACR image-pull egress", workflow)
        self.assertIn(
            "if: ${{ github.event.inputs.deploy == 'true' && github.event.inputs.environment_name == 'prod' && github.ref == 'refs/heads/main' }}",
            workflow,
        )
        self.assertIn(
            "if: ${{ github.event.inputs.deploy == 'true' && github.event.inputs.environment_name != 'prod' }}",
            workflow,
        )

    def test_azure_infra_reads_free_form_inputs_from_environment_variables(self) -> None:
        workflow = AZURE_INFRA_WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("INPUT_ENABLED_EGRESS_PROFILES: ${{ github.event.inputs.enabled_egress_profiles }}", workflow)
        self.assertIn("INPUT_CONNECTOR_RELAY_DESTINATIONS: ${{ github.event.inputs.connector_relay_destinations }}", workflow)
        self.assertIn("INPUT_CONTAINER_REGISTRY_ID: ${{ github.event.inputs.container_registry_id }}", workflow)
        self.assertIn('enabled_egress_profiles_input="${INPUT_ENABLED_EGRESS_PROFILES}"', workflow)
        self.assertIn('connector_relay_destinations_input="${INPUT_CONNECTOR_RELAY_DESTINATIONS}"', workflow)
        self.assertNotIn("enabled_egress_profiles_input='${{ github.event.inputs.enabled_egress_profiles }}'", workflow)
        self.assertNotIn("connector_relay_destinations_input='${{ github.event.inputs.connector_relay_destinations }}'", workflow)

    def test_network_modules_support_non_overlapping_environment_address_plans(self) -> None:
        network_module = NETWORK_MODULE.read_text(encoding="utf-8")
        hub_governance_module = HUB_GOVERNANCE_MODULE.read_text(encoding="utf-8")
        self.assertIn("param platformAddressSpace string = ''", network_module)
        self.assertIn("environmentName == 'prod'", network_module)
        self.assertIn("? '10.44.0.0/16'", network_module)
        self.assertIn(": environmentName == 'test'", network_module)
        self.assertIn("? '10.43.0.0/16'", network_module)
        self.assertIn(": '10.42.0.0/16'", network_module)
        self.assertIn("'10.42.0.0/16'", network_module)
        self.assertIn("'10.43.0.0/16'", network_module)
        self.assertIn("'10.44.0.0/16'", network_module)
        self.assertIn("platformAddressSpace must be one of 10.42.0.0/16, 10.43.0.0/16, or 10.44.0.0/16.", network_module)
        self.assertIn("output platformAddressSpace string = resolvedPlatformAddressSpace", network_module)
        self.assertIn("param platformAddressSpace string", hub_governance_module)
        self.assertIn("sourceAddresses: [platformAddressSpace]", hub_governance_module)

    def test_reviewed_connector_workflow_propagates_public_base_url(self) -> None:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("HELIOS_CONNECTOR_PUBLIC_BASE_URL: ${{ vars.HELIOS_CONNECTOR_PUBLIC_BASE_URL }}", workflow)
        self.assertIn("HELIOS_CONTAINER_APPS_INFRASTRUCTURE_SUBNET_ID must be a full subnet resource ID.", workflow)
        self.assertIn(
            "HELIOS_CONTAINER_APPS_INFRASTRUCTURE_SUBNET_ID must target the protected deployment subscription/resource-group scope.",
            workflow,
        )
        self.assertIn(
            "HELIOS_CONNECTOR_PUBLIC_BASE_URL must be one HTTPS origin without path, query, or fragment.",
            workflow,
        )
        self.assertGreaterEqual(workflow.count('publicBaseUrl="${HELIOS_CONNECTOR_PUBLIC_BASE_URL}"'), 3)
        self.assertIn('--arg publicBaseUrl "${HELIOS_CONNECTOR_PUBLIC_BASE_URL}"', workflow)
        self.assertIn("publicBaseUrl: $publicBaseUrl", workflow)

    def test_production_edge_route_cutover_is_explicitly_gated(self) -> None:
        azure_main = AZURE_MAIN_BICEP.read_text(encoding="utf-8")
        private_edge = PRIVATE_EDGE_MODULE.read_text(encoding="utf-8")
        self.assertIn("param edgeRouteCutoverApproved bool = false", azure_main)
        self.assertIn("edgeRouteEnabled: environmentName != 'prod' || edgeRouteCutoverApproved", azure_main)
        self.assertIn("param edgeRouteEnabled bool = true", private_edge)
        self.assertIn("enabledState: edgeRouteEnabled ? 'Enabled' : 'Disabled'", private_edge)

    def test_network_path_catalog_includes_cosmos_firewall_destination(self) -> None:
        network_paths = NETWORK_PATHS.read_text(encoding="utf-8")
        self.assertIn("documents.azure.com", network_paths)
        self.assertIn("*.documents.azure.com", network_paths)
        self.assertIn("containerAppsPlatform", network_paths)
        self.assertIn("containerRegistryDataPlane", network_paths)
        self.assertIn("*.azurecr.io", network_paths)


if __name__ == "__main__":
    unittest.main()

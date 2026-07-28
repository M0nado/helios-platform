import json
import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
CONTRACT_PATH = ROOT / "contracts" / "hermes-xcore" / "v1" / "system.contract.json"
EVENT_SCHEMA_PATH = ROOT / "contracts" / "hermes-xcore" / "v1" / "event.schema.json"


class HermesXCoreContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.contract = json.loads(CONTRACT_PATH.read_text(encoding="utf-8"))
        cls.event_schema = json.loads(EVENT_SCHEMA_PATH.read_text(encoding="utf-8"))
        cls.environments = {
            item["id"]: item for item in cls.contract["environments"]
        }
        cls.agents = {item["id"]: item for item in cls.contract["agents"]}

    def test_versioned_contract_identity(self):
        self.assertEqual(self.contract["schemaVersion"], "1.0.0")
        self.assertEqual(self.contract["contractId"], "helios.hermes-xcore.system")

    def test_canonical_environments_only(self):
        self.assertEqual(
            set(self.environments),
            {"x-tier-dev", "x-tier-xcore", "x-tier-prod"},
        )
        self.assertNotIn("hotfix", self.environments)

    def test_hotfix_is_protected_workflow_not_environment(self):
        hotfix = self.contract["hotfixWorkflow"]
        self.assertFalse(hotfix["isEnvironment"])
        self.assertEqual(hotfix["targetEnvironment"], "x-tier-prod")
        self.assertFalse(hotfix["severityS0IsSufficientAuthority"])
        self.assertTrue(hotfix["requiresProtectedApproval"])
        self.assertEqual(hotfix["timeoutBehavior"], "deny")

    def test_workload_identity_is_distinct_per_environment(self):
        identities = [
            environment["workloadIdentityRef"]
            for environment in self.environments.values()
        ]
        self.assertEqual(len(identities), len(set(identities)))

    def test_chaos_is_only_enabled_in_xcore(self):
        enabled = {
            environment_id
            for environment_id, environment in self.environments.items()
            if environment["chaos"]["enabled"]
        }
        self.assertEqual(enabled, {"x-tier-xcore"})

    def test_xcore_experiments_are_bounded_and_reversible(self):
        chaos = self.environments["x-tier-xcore"]["chaos"]
        required_truths = [
            "isolatedResourcesRequired",
            "explicitOptInRequired",
            "ownerRequired",
            "seedRequired",
            "abortConditionsRequired",
            "cleanupRequired",
            "secretsMutationForbidden",
            "productionReachabilityForbidden",
        ]
        for field in required_truths:
            with self.subTest(field=field):
                self.assertTrue(chaos[field])
        self.assertGreater(chaos["maximumDurationMinutes"], 0)
        self.assertGreater(chaos["maximumCostUsd"], 0)
        self.assertGreater(chaos["maximumAffectedResources"], 0)

    def test_production_approval_is_human_artifact_bound_and_fail_closed(self):
        protection = self.environments["x-tier-prod"]["protection"]
        self.assertEqual(protection["surface"], "github-protected-environment")
        self.assertTrue(protection["humanApprovalRequired"])
        self.assertGreaterEqual(protection["minimumHumanApprovals"], 2)
        self.assertTrue(protection["separationOfDutiesRequired"])
        self.assertTrue(protection["bindExactArtifactDigest"])
        self.assertTrue(protection["bindDeploymentParameters"])
        self.assertTrue(protection["bindWhatIfEvidence"])
        self.assertEqual(protection["timeoutBehavior"], "deny")

    def test_hermes_cannot_self_approve_or_execute(self):
        hermes = self.agents["hermes"]
        forbidden = set(hermes["forbiddenCapabilities"])
        self.assertTrue(
            {
                "approve-own-request",
                "impersonate-human",
                "execute-deployment",
                "execute-rollback",
                "disable-safety",
            }.issubset(forbidden)
        )
        self.assertNotIn("approve-deployment", hermes["capabilities"])

    def test_notification_agent_cannot_approve_or_execute(self):
        notifier = self.agents["notification-agent"]
        forbidden = set(notifier["forbiddenCapabilities"])
        self.assertTrue(
            {
                "approve-deployment",
                "execute-deployment",
                "execute-rollback",
                "modify-policy",
            }.issubset(forbidden)
        )

    def test_codegen_has_no_production_access(self):
        codegen = self.agents["codegen-agent"]
        self.assertNotIn("x-tier-prod", codegen["environments"])
        self.assertTrue(codegen["productionWritesForbidden"])

    def test_runtime_is_idempotent_correlated_and_bounded(self):
        runtime = self.contract["runtime"]
        self.assertTrue(runtime["idempotencyRequired"])
        self.assertTrue(runtime["correlationRequired"])
        self.assertLessEqual(runtime["maximumHopCount"], 16)
        self.assertEqual(runtime["approvalTimeoutBehavior"], "FAILED")
        self.assertEqual(
            set(runtime["terminalStates"]),
            {"COMPLETE", "FAILED", "ROLLED_BACK"},
        )

    def test_promotion_evidence_is_artifact_and_approval_complete(self):
        evidence = set(self.contract["promotion"]["requiredEvidence"])
        self.assertTrue(
            {
                "immutable-source-revision",
                "artifact-sha256",
                "artifact-provenance",
                "test-results",
                "security-results",
                "deployment-plan",
                "what-if-output",
                "human-approval-record",
            }.issubset(evidence)
        )

    def test_prediction_is_advisory_and_exposes_unknown(self):
        policy = self.contract["promotion"]["predictionPolicy"]
        self.assertEqual(policy["mode"], "advisory-only")
        for field in (
            "unknownStateRequired",
            "unitsRequired",
            "windowRequired",
            "sampleSizeRequired",
            "modelVersionRequired",
            "minimumEvidenceRequired",
        ):
            with self.subTest(field=field):
                self.assertTrue(policy[field])

    def test_connector_authority_is_explicit(self):
        integrations = self.contract["integrations"]
        self.assertEqual(integrations["azureDevOps"]["accessMode"], "read-only")
        self.assertFalse(integrations["azureDevOps"]["sourceAuthority"])
        self.assertFalse(integrations["azureDevOps"]["releaseAuthority"])
        self.assertFalse(integrations["sharepoint"]["deploymentAuthority"])
        self.assertFalse(integrations["slack"]["approvalAuthority"])
        self.assertFalse(integrations["slack"]["deploymentAuthority"])
        self.assertFalse(integrations["linear"]["approvalAuthority"])
        self.assertFalse(integrations["linear"]["deploymentAuthority"])
        self.assertFalse(integrations["openai"]["modelOutputsAreAuthority"])

    def test_azure_and_openai_secret_policy(self):
        integrations = self.contract["integrations"]
        self.assertTrue(integrations["azure"]["clientSecretsForbidden"])
        self.assertEqual(
            integrations["azure"]["workloadRolePolicy"],
            "least-privilege-no-owner",
        )
        self.assertTrue(integrations["openai"]["keyMaterialInSourceForbidden"])
        self.assertEqual(
            integrations["openai"]["secretReference"],
            "azure-key-vault-managed-identity",
        )

    def test_event_schema_requires_delivery_safety_fields(self):
        required = set(self.event_schema["required"])
        self.assertTrue(
            {
                "eventId",
                "correlationId",
                "causationId",
                "idempotencyKey",
                "attempt",
                "hopCount",
                "dryRun",
            }.issubset(required)
        )
        self.assertEqual(
            self.event_schema["properties"]["hopCount"]["maximum"],
            self.contract["runtime"]["maximumHopCount"],
        )

    def test_event_artifact_is_immutable_and_provenanced(self):
        artifact = self.event_schema["properties"]["artifact"]
        self.assertEqual(
            set(artifact["required"]),
            {"repository", "revision", "digest", "provenanceRef"},
        )
        self.assertIn(
            "sha256:",
            self.event_schema["properties"]["approval"]["properties"][
                "boundArtifactDigest"
            ]["pattern"],
        )

    def test_contract_contains_no_raw_secret_fields(self):
        serialized = json.dumps(self.contract).lower()
        forbidden_field_names = [
            "\"password\"",
            "\"clientsecret\"",
            "\"privatekey\"",
            "\"accesstoken\"",
            "\"apikey\"",
        ]
        for name in forbidden_field_names:
            with self.subTest(name=name):
                self.assertNotIn(name, serialized)


if __name__ == "__main__":
    unittest.main()

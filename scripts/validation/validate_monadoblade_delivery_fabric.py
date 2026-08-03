#!/usr/bin/env python3
"""Validate the Monadoblade v2 delivery-fabric contracts without external dependencies."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any


IDENTITIES = ["core", "developer", "studio", "gamer", "ai-server", "sysadmin"]
KANJI = {
    "core": "核",
    "developer": "創",
    "studio": "響",
    "gamer": "迅",
    "ai-server": "智",
    "sysadmin": "統",
}
OVERLAYS = {"personal", "sysops"}
WORKFLOWS = {"standard", "airgap", "recovery", "quarantine"}
SHA256 = re.compile(r"^[0-9a-f]{64}$")
HEX_COLOR = re.compile(r"^#[0-9A-Fa-f]{6}$")


class ContractError(RuntimeError):
    """Raised when a delivery-fabric invariant is violated."""


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ContractError(message)


def load_json(root: Path, relative_path: str) -> dict[str, Any]:
    path = root / relative_path
    require(path.is_file(), f"missing contract: {relative_path}")
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ContractError(f"invalid JSON in {relative_path}: {exc}") from exc
    require(isinstance(value, dict), f"contract root must be an object: {relative_path}")
    return value


def validate_identities(data: dict[str, Any]) -> dict[str, dict[str, Any]]:
    identities = data.get("identities")
    require(isinstance(identities, list), "identities must be a list")
    ids = [entry.get("id") for entry in identities if isinstance(entry, dict)]
    require(ids == IDENTITIES, f"permanent identity order/set mismatch: {ids}")
    require(data.get("defaultIdentity") == "core", "Core must be the default identity")
    require(data.get("invariants", {}).get("permanentIdentityCount") == 6, "identity count invariant must be six")

    by_id = {entry["id"]: entry for entry in identities}
    colors: set[str] = set()
    kanji: set[str] = set()
    admins: list[str] = []
    for identity_id, entry in by_id.items():
        require(entry.get("kanji") == KANJI[identity_id], f"unexpected Kanji for {identity_id}")
        color = entry.get("color")
        require(isinstance(color, str) and HEX_COLOR.fullmatch(color) is not None, f"invalid color for {identity_id}")
        require(color.lower() not in colors, f"duplicate identity color: {color}")
        require(entry["kanji"] not in kanji, f"duplicate identity Kanji: {entry['kanji']}")
        colors.add(color.lower())
        kanji.add(entry["kanji"])
        require(entry.get("interactive") is True, f"identity {identity_id} must be interactive")
        if entry.get("administrator") is True:
            admins.append(identity_id)

    require(admins == ["sysadmin"], "Sysadmin must be the only administrator identity")
    sysadmin = by_id["sysadmin"]
    activation = sysadmin.get("activation", {})
    require(sysadmin.get("hidden") is True, "Sysadmin must remain hidden")
    require(sysadmin.get("enabledByDefault") is False, "Sysadmin must be disabled by default")
    require(sysadmin.get("networkMode") == "offline-local-only", "Sysadmin must be offline/local only")
    require(activation.get("physicalPresenceRequired") is True, "Sysadmin requires physical presence")
    require(activation.get("minimumFactors", 0) >= 2, "Sysadmin requires at least two local factors")
    for key in ("remoteActivationDenied", "cloudActivationDenied", "aiActivationDenied"):
        require(activation.get(key) is True, f"Sysadmin activation boundary missing {key}")

    overlays = data.get("capabilityOverlays")
    require(isinstance(overlays, list), "capabilityOverlays must be a list")
    require({entry.get("id") for entry in overlays} == OVERLAYS, "overlay set mismatch")
    for overlay in overlays:
        require(overlay.get("becomesIdentity") is False, f"overlay {overlay.get('id')} cannot become an identity")
        require("sysadmin" not in overlay.get("allowedIdentities", []), "overlays cannot alter Sysadmin")

    workflows = data.get("workflowStates")
    require(isinstance(workflows, list), "workflowStates must be a list")
    require({entry.get("id") for entry in workflows} == WORKFLOWS, "workflow state set mismatch")
    for workflow in workflows:
        require(workflow.get("selectableOnIdentityWheel") is False, f"workflow {workflow.get('id')} cannot appear on the wheel")
        if workflow.get("id") != "standard":
            require(workflow.get("requiresSysAdmin") is True, f"workflow {workflow.get('id')} requires Sysadmin")

    return by_id


def validate_shell(data: dict[str, Any], identities: dict[str, dict[str, Any]]) -> None:
    boundary = data.get("securityBoundary", {})
    require(boundary.get("runsAfterWindowsAuthentication") is True, "shell must run after Windows authentication")
    for key in ("replacesWindowsCredentialProvider", "capturesPasswords", "writesBootConfiguration"):
        require(boundary.get(key) is False, f"shell boundary must keep {key} false")

    monado = data.get("monado", {})
    require(monado.get("singleAperture") is True, "shell requires one physical aperture")
    require(monado.get("duplicateDecorativeWheelDenied") is True, "duplicate wheel must be denied")
    require(monado.get("bladeBodyRemainsCyanWhite") is True, "blade body must remain cyan-white")

    wheel = data.get("profileWheel", {})
    sectors = wheel.get("sectors")
    require(isinstance(sectors, list) and len(sectors) == 6, "wheel must contain six sectors")
    require([sector.get("identity") for sector in sectors] == IDENTITIES, "wheel identity order mismatch")
    for sector in sectors:
        identity = identities[sector["identity"]]
        require(sector.get("kanji") == identity.get("kanji"), f"wheel Kanji mismatch for {sector['identity']}")
        require(sector.get("color") == identity.get("color"), f"wheel color mismatch for {sector['identity']}")

    idle = data.get("login", {}).get("idle", {})
    require(idle.get("orbitalHologramsVisible") is False, "idle must collapse orbital holograms")
    require(data.get("boot", {}).get("persistentProfileHolograms") is False, "boot cannot retain profile holograms")
    require(data.get("boot", {}).get("bladeIsProgressIndicator") is True, "boot blade must indicate progress")

    energy_bus = data.get("energyBus", {})
    require(energy_bus.get("failureIsolation") is True, "energy bus consumers must be isolated")
    require(energy_bus.get("soundOrLightingFailureNeverBlocksIdentityActivation") is True, "audio/lighting cannot block activation")

    alvis = data.get("alvis", {})
    require(alvis.get("directExecutorToolsDenied") is True, "ALVIS direct executor tools must be denied")
    require(set(alvis.get("allowedToolPrefixes", [])) == {"search_", "fetch_", "plan_", "request_"}, "ALVIS prefixes mismatch")
    require(alvis.get("sysAdminMode") == "local-offline-read-plan", "ALVIS Sysadmin mode must remain local/offline")


def validate_environment(data: dict[str, Any]) -> None:
    presets = data.get("presets")
    require(isinstance(presets, list), "environment presets must be a list")
    require([preset.get("identity") for preset in presets] == IDENTITIES, "environment preset identity mismatch")

    renderer = data.get("renderer", {})
    draw_policy = renderer.get("drawPolicy", {})
    require(draw_policy.get("particleComputePassesPerUpdate") == 1, "particles require one bounded compute pass")
    require(draw_policy.get("particleInstancedDrawsPerFrame") == 1, "particles require one instanced draw")
    require(draw_policy.get("perParticleUiElementsDenied") is True, "per-particle UI elements are denied")
    require(draw_policy.get("suspendWhenMinimized") is True, "renderer must suspend when minimized")
    require(draw_policy.get("suspendWhenOccluded") is True, "renderer must suspend when occluded")

    weather = data.get("worldSignals", {}).get("weather", {})
    require(weather.get("requiresLocationConsent") is True, "live weather requires location consent")
    require(weather.get("neverBlocksShell") is True, "weather adapter cannot block the shell")

    tiers = data.get("performance", {}).get("qualityTiers", {})
    require(tiers.get("suspended", {}).get("particles") == 0, "suspended tier must spend zero particles")
    require(tiers.get("cinematic", {}).get("particles", 0) <= 8192, "cinematic particle pool exceeds cap")
    require(data.get("performance", {}).get("rendererFailureMode") == "static-background", "renderer requires a static fallback")


def validate_effects(data: dict[str, Any], identities: dict[str, dict[str, Any]]) -> None:
    transfer = data.get("energyTransfer", {})
    require(transfer.get("previewIsReversible") is True, "effect preview must remain reversible")
    require(transfer.get("identityColorIsAccentOnly") is True, "identity color must remain an accent")
    require(transfer.get("idleCollapsesHolograms") is True, "effects must collapse holograms while idle")

    wyvern = data.get("wyvern", {})
    require(wyvern.get("optional") is True, "Wyvern audio must remain optional")
    require(wyvern.get("maximumEventsPerSecond", 99) <= 8, "Wyvern event rate exceeds contract")
    require(wyvern.get("maximumSimultaneousVoices", 99) <= 6, "Wyvern polyphony exceeds contract")
    require(wyvern.get("masterGainMaximum", 99) <= 0.65, "Wyvern gain exceeds contract")
    require(wyvern.get("failureMode") == "silent", "Wyvern requires a silent failure mode")
    cues = wyvern.get("cues", [])
    require([cue.get("identity") for cue in cues] == IDENTITIES, "Wyvern cue identity mismatch")
    for cue in cues:
        identity = identities[cue["identity"]]
        require(cue.get("kanji") == identity.get("kanji"), f"Wyvern Kanji mismatch for {cue['identity']}")
        require(cue.get("color") == identity.get("color"), f"Wyvern color mismatch for {cue['identity']}")
        require(cue.get("cue") == identity.get("audioCue"), f"Wyvern cue mismatch for {cue['identity']}")

    chroma = data.get("chroma", {})
    require(chroma.get("optional") is True, "Chroma must remain optional")
    require(chroma.get("maximumFramesPerSecond", 99) <= 30, "Chroma rate exceeds contract")
    require(chroma.get("maximumBrightness", 99) <= 0.65, "Chroma brightness exceeds contract")
    require(chroma.get("deviceDiscoveryRequiresOptIn") is True, "Chroma discovery requires opt in")
    require(chroma.get("failureMode") == "screen-preview-only", "Chroma requires a screen-only fallback")
    patterns = chroma.get("patterns", [])
    require([pattern.get("identity") for pattern in patterns] == IDENTITIES, "Chroma pattern identity mismatch")
    for pattern in patterns:
        require(pattern.get("color") == identities[pattern["identity"]].get("color"), f"Chroma color mismatch for {pattern['identity']}")

    particles = data.get("particles", {})
    require(particles.get("fixedPool") is True, "effects require a fixed particle pool")
    require(particles.get("maximumCapacity", 0) <= 8192, "effects particle cap exceeds renderer cap")
    require(particles.get("computePassesPerUpdate") == 1, "effects require one particle compute pass")
    require(particles.get("instancedDrawsPerFrame") == 1, "effects require one particle draw")
    require(data.get("failureIsolation", {}).get("anyEffectMayFailWithoutBlockingShell") is True, "effects must be failure isolated")


def validate_engine_registry(data: dict[str, Any]) -> None:
    require(data.get("mode") == "evaluation-only", "engine registry must remain evaluation-only")
    training = data.get("training", {})
    for key in ("enabled", "autonomousPromotion", "productionDataAllowed", "secretOrCredentialDataAllowed", "quarantineEvidenceAllowed"):
        require(training.get(key) is False, f"engine training boundary must keep {key} false")
    require(0 < training.get("maximumLocalEvaluationCycles", 0) <= 3, "local evaluation cycle cap is invalid")
    require(str(training.get("artifactRoot", "")).startswith(".run/"), "engine artifacts must use the ignored .run root")
    require(training.get("artifactRootIsGitIgnored") is True, "engine artifact root must be declared ignored")

    engines = data.get("engines")
    require(isinstance(engines, list) and engines, "engine registry requires candidates")
    ids = [engine.get("id") for engine in engines]
    require(len(ids) == len(set(ids)), "engine IDs must be unique")
    allowed_languages = {"csharp", "fsharp", "cpp", "python"}
    denied_effects = {"execute", "apply", "deploy", "train", "promote"}
    denied_statuses = {"active", "production", "deployed", "promoted"}
    for engine in engines:
        require(engine.get("ownerLanguage") in allowed_languages, f"unknown engine language: {engine.get('id')}")
        require(engine.get("status") not in denied_statuses, f"engine is prematurely active: {engine.get('id')}")
        require(not denied_effects.intersection(engine.get("effects", [])), f"engine has a denied effect: {engine.get('id')}")
        require(set(engine.get("identities", [])).issubset(set(IDENTITIES)), f"engine has an unknown identity: {engine.get('id')}")

    alvis = data.get("alvis", {})
    require(alvis.get("exposedTool") == "plan_engine_evaluation", "ALVIS may expose only the evaluation planner")
    for key in ("directTrainingTool", "directPromotionTool", "directExecutionTool"):
        require(alvis.get(key) is None, f"engine registry must keep {key} null")


def validate_storage(data: dict[str, Any]) -> None:
    require(data.get("mode") == "plan-only", "storage contract must remain plan-only")
    apply = data.get("apply", {})
    require(apply.get("enabled") is False, "storage apply must be disabled")
    require(apply.get("availableFromAlvis") is False, "ALVIS cannot apply storage changes")
    require(apply.get("availableFromConversationalRequest") is False, "conversation cannot apply storage changes")
    selection = data.get("deviceSelection", {})
    require(selection.get("selectedDisk") is None, "template cannot preselect a disk")
    for key in ("requireExactModel", "requireSerial", "requireUniqueId", "requireCapacityMatch", "systemOrBootDiskDenied", "oemRecoveryDeletionDenied"):
        require(selection.get(key) is True, f"storage selection guardrail missing {key}")
    wizard = data.get("usbWizard", {})
    for key in ("inventoryIsReadOnly", "generatesPlanOnly", "directDiskCommandsDenied", "bootConfigurationWritesDenied"):
        require(wizard.get(key) is True, f"USB Wizard guardrail missing {key}")

    volumes = data.get("logicalVolumes")
    require(isinstance(volumes, list), "logicalVolumes must be a list")
    by_id = {volume.get("id"): volume for volume in volumes}
    require({"common-core", "developer", "studio", "ai-models", "secure", "quarantine", "backup"} == set(by_id), "logical volume set mismatch")
    require(by_id["quarantine"].get("workflowOnly") is True, "quarantine must remain a workflow volume")
    require(by_id["quarantine"].get("executeDenied") is True, "quarantine volume must deny execution")


def validate_delivery(data: dict[str, Any]) -> None:
    authority = data.get("authority", {})
    require(authority.get("codeAndRelease") == "M0nado/helios-platform", "platform authority mismatch")
    automation = data.get("githubAutomation", {})
    require(automation.get("directToMainDenied") is True, "direct-to-main writes must be denied")
    require(automation.get("forcePushDenied") is True, "force push must be denied")
    require(automation.get("cloudOrDeviceMutation") is False, "validation workflow cannot mutate cloud or devices")

    alvis = data.get("alvis", {})
    require(alvis.get("directExecutorToolsDenied") is True, "delivery contract must deny ALVIS executors")
    expected_prefixes = {"search_", "fetch_", "plan_", "request_"}
    require({entry.get("prefix") for entry in alvis.get("toolClasses", [])} == expected_prefixes, "delivery ALVIS prefix mismatch")
    request_tool = next(entry for entry in alvis["toolClasses"] if entry["prefix"] == "request_")
    require(request_tool.get("approval") == "human-required", "ALVIS requests require human approval")
    require(alvis.get("sysAdmin", {}).get("network") == "denied", "Sysadmin ALVIS network must be denied")

    projection = data.get("collaborationProjection", {})
    require(projection.get("github", {}).get("role") == "engineering-source-of-truth", "GitHub must remain engineering truth")
    for destination in ("linear", "slack", "sharepoint", "azureDevOps"):
        require(projection.get(destination, {}).get("mayTriggerExecution") is False, f"{destination} cannot trigger execution")

    intake = data.get("legacySourceIntake")
    require(isinstance(intake, list) and len(intake) == 16, "all sixteen supplied files require a disposition")
    names = [entry.get("name") for entry in intake]
    require(len(names) == len(set(names)), "legacy source names must be unique")
    for entry in intake:
        require(SHA256.fullmatch(str(entry.get("sha256", ""))) is not None, f"invalid source hash for {entry.get('name')}")
        require(entry.get("rawImport") is False, f"raw prototype import is denied: {entry.get('name')}")
        require(bool(entry.get("disposition")), f"source disposition missing: {entry.get('name')}")


def validate(root: Path) -> dict[str, Any]:
    profiles = load_json(root, "config/profiles/monadoblade-profiles.v2.json")
    identities = validate_identities(profiles)
    validate_shell(load_json(root, "config/gui/monado-profile-shell.v2.json"), identities)
    validate_environment(load_json(root, "config/experience/monadoblade-living-environments.v1.json"))
    validate_effects(load_json(root, "config/experience/monadoblade-effects.v1.json"), identities)
    validate_engine_registry(load_json(root, "config/aihub/monadoblade-engine-registry.v1.json"))
    validate_storage(load_json(root, "config/storage/monadoblade-storage-plan-template.v2.json"))
    validate_delivery(load_json(root, "config/integrations/monadoblade-delivery-fabric.v1.json"))
    require((root / "config/integrations/event-contract.schema.json").is_file(), "normalized integration event schema is missing")
    return {
        "status": "ok",
        "permanentIdentities": len(IDENTITIES),
        "capabilityOverlays": len(OVERLAYS),
        "workflowStates": len(WORKFLOWS),
        "rawLegacyImports": 0,
        "storageApplyEnabled": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    try:
        result = validate(args.root.resolve())
    except ContractError as exc:
        print(json.dumps({"status": "error", "message": str(exc)}, indent=2))
        return 1
    print(json.dumps(result, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

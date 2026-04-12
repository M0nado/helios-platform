#!/usr/bin/env python3
"""
HELIOS Installer – Interactive Customization Tool

Usage:
    python customize.py                    # Interactive menu
    python customize.py --load my.json     # Load and edit existing config
    python customize.py --preset minimal   # Start from a named preset
    python customize.py --dry-run          # Preview without saving
"""

import json
import os
import sys
import copy
import argparse
from pathlib import Path
from typing import Any

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
_HERE = Path(__file__).parent
CONFIG_DIR = _HERE / "config"
EXAMPLES_DIR = _HERE / "examples"
DEFAULT_CONFIG = CONFIG_DIR / "default_config.json"
PRESETS_FILE = CONFIG_DIR / "PRESETS.json"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _load_json(path: Path) -> dict:
    with open(path, "r", encoding="utf-8") as fh:
        return json.load(fh)


def _save_json(path: Path, data: dict) -> None:
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=2)
    print(f"\n✅  Saved to: {path}")


def _print_header(title: str) -> None:
    width = 60
    print("\n" + "═" * width)
    print(f"  {title}")
    print("═" * width)


def _ask(prompt: str, default: Any = None) -> str:
    suffix = f" [{default}]" if default is not None else ""
    try:
        value = input(f"{prompt}{suffix}: ").strip()
    except (EOFError, KeyboardInterrupt):
        print()
        return str(default) if default is not None else ""
    return value if value else (str(default) if default is not None else "")


def _ask_bool(prompt: str, default: bool = True) -> bool:
    default_str = "Y/n" if default else "y/N"
    try:
        raw = input(f"{prompt} [{default_str}]: ").strip().lower()
    except (EOFError, KeyboardInterrupt):
        print()
        return default
    if raw in ("y", "yes"):
        return True
    if raw in ("n", "no"):
        return False
    return default


def _ask_choice(prompt: str, choices: list, default: int = 0) -> str:
    print(f"\n{prompt}")
    for i, choice in enumerate(choices):
        marker = ">" if i == default else " "
        print(f"  {marker} {i + 1}. {choice}")
    while True:
        try:
            raw = input(f"Select [1-{len(choices)}] (default {default + 1}): ").strip()
        except (EOFError, KeyboardInterrupt):
            print()
            return choices[default]
        if not raw:
            return choices[default]
        if raw.isdigit() and 1 <= int(raw) <= len(choices):
            return choices[int(raw) - 1]
        print("  ⚠  Invalid choice, please try again.")


# ---------------------------------------------------------------------------
# Validation
# ---------------------------------------------------------------------------

class ValidationError(Exception):
    pass


def validate_config(cfg: dict) -> list:
    """Return a list of warning/error strings.  Empty list = valid."""
    issues = []

    # Partition sizes
    partitions = cfg.get("partitions", {})
    letters_seen = set()
    for letter, part in partitions.items():
        if not part.get("enabled", True):
            continue
        size = part.get("size_gb", 0)
        if size < 10:
            issues.append(f"Partition {letter}: size_gb must be >= 10 (got {size})")
        if letter in letters_seen:
            issues.append(f"Duplicate drive letter: {letter}")
        letters_seen.add(letter)

    # User names
    users = cfg.get("users", [])
    names_seen = set()
    for user in users:
        name = user.get("name", "")
        if not all(c.isalnum() or c in ("-", "_", ".") for c in name) or name.startswith("."):
            issues.append(f"User name '{name}' contains invalid characters (alphanumeric + underscore only)")
        if len(name) > 20:
            issues.append(f"User name '{name}' exceeds 20 characters")
        if name in names_seen:
            issues.append(f"Duplicate user name: {name}")
        names_seen.add(name)

    # BitLocker requires TPM
    security = cfg.get("security", {})
    bitlocker = security.get("bitlocker", {})
    if bitlocker.get("enabled") and not bitlocker.get("require_tpm", True):
        issues.append("BitLocker is enabled but require_tpm is false – a USB key will be required")

    # Phase dependencies
    phases = cfg.get("phases", {})
    enabled_phases = set(phases.get("enabled", range(1, 8)))
    features = cfg.get("features", {})
    if features.get("bitlocker") and 3 not in enabled_phases:
        issues.append("BitLocker is enabled but Phase 3 (Security) is not in the enabled phase list")

    return issues


# ---------------------------------------------------------------------------
# Menu sections
# ---------------------------------------------------------------------------

def _menu_installation(cfg: dict) -> None:
    _print_header("Installation Settings")
    inst = cfg.setdefault("installation", {})
    inst["name"] = _ask("Installation name", inst.get("name", "HELIOS Setup"))
    inst["auto_reboot"] = _ask_bool("Auto reboot after install?", inst.get("auto_reboot", False))
    inst["logging_level"] = _ask_choice(
        "Logging level",
        ["DEBUG", "INFO", "WARNING", "ERROR"],
        default=["DEBUG", "INFO", "WARNING", "ERROR"].index(inst.get("logging_level", "INFO")),
    )
    inst["dry_run"] = _ask_bool("Dry-run mode (preview only, no changes)?", inst.get("dry_run", False))


def _menu_phases(cfg: dict) -> None:
    _print_header("Phase Control")
    phases = cfg.setdefault("phases", {})

    current = phases.get("enabled", list(range(1, 8)))
    print(f"  Currently enabled phases: {current}")
    raw = _ask("Enter enabled phase numbers (comma-separated)", ",".join(str(p) for p in current))
    try:
        phases["enabled"] = sorted({int(x.strip()) for x in raw.split(",") if x.strip().isdigit()})
    except ValueError:
        print("  ⚠  Could not parse phases – keeping existing value.")

    phases["rollback_on_error"] = _ask_bool("Rollback on error?", phases.get("rollback_on_error", True))
    phases["continue_on_warning"] = _ask_bool("Continue on warning?", phases.get("continue_on_warning", True))


def _menu_partitions(cfg: dict) -> None:
    _print_header("Partition Configuration")
    partitions = cfg.setdefault("partitions", {})

    for letter, part in list(partitions.items()):
        print(f"\n  Drive {letter}:  {part.get('label', '')}  ({part.get('size_gb', '?')} GB)")
        part["enabled"] = _ask_bool(f"  Enable drive {letter}?", part.get("enabled", True))
        if part["enabled"]:
            raw = _ask(f"  Size in GB", part.get("size_gb", 100))
            try:
                size = int(raw)
                if size < 10:
                    print("  ⚠  Size must be at least 10 GB – keeping existing value.")
                else:
                    part["size_gb"] = size
            except ValueError:
                print("  ⚠  Invalid size – keeping existing value.")
            part["label"] = _ask(f"  Label", part.get("label", letter))
            part["encrypt"] = _ask_bool(f"  Encrypt with BitLocker?", part.get("encrypt", False))

    if _ask_bool("\nAdd a new partition?", False):
        letter = _ask("  New drive letter (single character)", "G").upper()
        new_part: dict = {
            "enabled": True,
            "size_gb": int(_ask("  Size in GB", 100)),
            "label": _ask("  Label", letter),
            "purpose": _ask_choice("  Purpose", ["os", "data", "backup", "dev", "games", "vhdx"]),
            "type": _ask_choice("  Type", ["basic", "vhdx"]),
            "encrypt": _ask_bool("  Encrypt?", False),
        }
        partitions[letter] = new_part


def _menu_security(cfg: dict) -> None:
    _print_header("Security Configuration")
    security = cfg.setdefault("security", {})

    # Defender
    defender = security.setdefault("defender", {})
    defender["enabled"] = _ask_bool("Enable Windows Defender?", defender.get("enabled", True))
    if defender["enabled"]:
        defender["realtime_protection"] = _ask_bool("  Real-time protection?", defender.get("realtime_protection", True))
        defender["cloud_protection"] = _ask_bool("  Cloud protection?", defender.get("cloud_protection", True))

    # BitLocker
    bitlocker = security.setdefault("bitlocker", {})
    bitlocker["enabled"] = _ask_bool("Enable BitLocker?", bitlocker.get("enabled", False))
    if bitlocker["enabled"]:
        bitlocker["algorithm"] = _ask_choice("  Algorithm", ["AES-128", "AES-256"], default=0)
        bitlocker["require_tpm"] = _ask_bool("  Require TPM?", bitlocker.get("require_tpm", True))

    # Firewall
    firewall = security.setdefault("firewall", {})
    firewall["enabled"] = _ask_bool("Enable Firewall?", firewall.get("enabled", True))
    if firewall["enabled"]:
        firewall["inbound_policy"] = _ask_choice("  Inbound policy", ["allow", "block"], default=0)
        firewall["outbound_policy"] = _ask_choice("  Outbound policy", ["allow", "block"], default=0)

    # AppLocker
    applocker = security.setdefault("applocker", {})
    applocker["enabled"] = _ask_bool("Enable AppLocker?", applocker.get("enabled", False))
    if applocker["enabled"]:
        applocker["mode"] = _ask_choice("  Mode", ["audit", "enforce", "disabled"], default=0)

    # Audit logging
    audit = security.setdefault("audit_logging", {})
    audit["enabled"] = _ask_bool("Enable audit logging?", audit.get("enabled", True))
    if audit["enabled"]:
        audit["level"] = _ask_choice("  Level", ["minimal", "standard", "verbose"], default=1)

    # USB
    usb = security.setdefault("usb", {})
    usb["restrict"] = _ask_bool("Restrict USB storage?", usb.get("restrict", False))
    if usb["restrict"]:
        usb["allow_hid"] = _ask_bool("  Allow HID devices (keyboard/mouse)?", usb.get("allow_hid", True))
        usb["allow_storage"] = False


def _menu_users(cfg: dict) -> None:
    _print_header("User Account Configuration")
    users = cfg.setdefault("users", [])

    for user in users:
        print(f"\n  User: {user.get('name')} ({user.get('type', 'standard')})")
        user["enabled"] = _ask_bool(f"  Keep user '{user['name']}'?", user.get("enabled", True))

    if _ask_bool("\nAdd a new user?", False):
        new_user: dict = {
            "name": _ask("  Username (alphanumeric + underscore, max 20 chars)", "NewUser"),
            "display_name": _ask("  Display name", "New User"),
            "type": _ask_choice("  Account type", ["standard", "administrator", "service"]),
            "enabled": True,
            "password_policy": _ask_choice("  Password policy", ["strong", "standard", "none"]),
            "groups": [],
        }
        users.append(new_user)


def _menu_optimization(cfg: dict) -> None:
    _print_header("Optimization Settings")
    opt = cfg.setdefault("optimization", {})

    opt["level"] = _ask_choice("Optimization level", ["minimal", "standard", "aggressive"], default=1)
    if opt["level"] != "minimal":
        power = opt.setdefault("power", {})
        power["plan"] = _ask_choice(
            "Power plan",
            ["power_saver", "balanced", "high_performance", "ultimate"],
            default=1,
        )


def _menu_features(cfg: dict) -> None:
    _print_header("Feature Toggles")
    features = cfg.setdefault("features", {})
    toggle_keys = [
        ("defender",         "Windows Defender"),
        ("bitlocker",        "BitLocker"),
        ("firewall",         "Firewall"),
        ("applocker",        "AppLocker"),
        ("audit_logging",    "Audit Logging"),
        ("usb_restrictions", "USB Restrictions"),
        ("seven_layer_setup","7-Layer Setup"),
        ("optimization",     "Optimization"),
    ]
    for key, label in toggle_keys:
        features[key] = _ask_bool(f"Enable {label}?", features.get(key, True))


def _menu_hooks(cfg: dict) -> None:
    _print_header("Custom Scripts / Hooks")
    hooks = cfg.setdefault("hooks", {})
    print("  Leave blank to skip a hook.\n")
    hooks["pre_install"]  = _ask("pre_install script path",  hooks.get("pre_install", ""))
    hooks["post_install"] = _ask("post_install script path", hooks.get("post_install", ""))
    hooks["on_error"]     = _ask("on_error script path",     hooks.get("on_error", ""))


# ---------------------------------------------------------------------------
# Main menu
# ---------------------------------------------------------------------------

MAIN_MENU_ITEMS = [
    ("installation",  "Installation Settings",  _menu_installation),
    ("phases",        "Phase Control",          _menu_phases),
    ("partitions",    "Partition Configuration", _menu_partitions),
    ("security",      "Security Configuration", _menu_security),
    ("users",         "User Accounts",          _menu_users),
    ("optimization",  "Optimization",           _menu_optimization),
    ("features",      "Feature Toggles",        _menu_features),
    ("hooks",         "Custom Scripts / Hooks", _menu_hooks),
]


def _preview(cfg: dict) -> None:
    _print_header("Configuration Preview")
    print(json.dumps(cfg, indent=2))


def _run_interactive(cfg: dict) -> dict:
    while True:
        _print_header("HELIOS Interactive Customization Tool")
        for i, (_, label, _) in enumerate(MAIN_MENU_ITEMS, start=1):
            print(f"  {i}. {label}")
        print(f"  {len(MAIN_MENU_ITEMS) + 1}. Preview configuration")
        print(f"  {len(MAIN_MENU_ITEMS) + 2}. Validate configuration")
        print(f"  {len(MAIN_MENU_ITEMS) + 3}. Save and exit")
        print(f"  0. Exit without saving")

        choice = _ask("\nYour choice", "3")
        try:
            choice_int = int(choice)
        except ValueError:
            continue

        if choice_int == 0:
            print("Exiting without saving.")
            sys.exit(0)
        elif 1 <= choice_int <= len(MAIN_MENU_ITEMS):
            _, _, handler = MAIN_MENU_ITEMS[choice_int - 1]
            handler(cfg)
        elif choice_int == len(MAIN_MENU_ITEMS) + 1:
            _preview(cfg)
        elif choice_int == len(MAIN_MENU_ITEMS) + 2:
            issues = validate_config(cfg)
            if issues:
                print("\n⚠  Validation issues found:")
                for issue in issues:
                    print(f"  • {issue}")
            else:
                print("\n✅  Configuration is valid!")
        elif choice_int == len(MAIN_MENU_ITEMS) + 3:
            issues = validate_config(cfg)
            if issues:
                print("\n⚠  Validation issues (review before saving):")
                for issue in issues:
                    print(f"  • {issue}")
                if not _ask_bool("Save anyway?", False):
                    continue
            return cfg

    return cfg


# ---------------------------------------------------------------------------
# CLI entry point
# ---------------------------------------------------------------------------

def _build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="HELIOS Installer – Interactive Customization Tool",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python customize.py                          # Start fresh from defaults
  python customize.py --preset developer       # Start from developer preset
  python customize.py --load my_config.json    # Edit an existing config
  python customize.py --preset minimal --dry-run  # Preview a preset
        """,
    )
    parser.add_argument(
        "--preset",
        choices=["minimal", "developer", "secure", "gaming", "enterprise", "custom"],
        help="Start from a named preset",
    )
    parser.add_argument(
        "--load",
        metavar="FILE",
        help="Load an existing configuration JSON file",
    )
    parser.add_argument(
        "--output",
        metavar="FILE",
        help="Output file path (default: custom_config.json)",
        default="custom_config.json",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Preview the configuration without saving",
    )
    parser.add_argument(
        "--validate",
        action="store_true",
        help="Validate a configuration file and exit",
    )
    return parser


def main() -> None:
    parser = _build_arg_parser()
    args = parser.parse_args()

    # Load base config
    if args.load:
        cfg = _load_json(Path(args.load))
        print(f"✅  Loaded config from: {args.load}")
    elif args.preset:
        presets = _load_json(PRESETS_FILE)
        cfg = copy.deepcopy(presets["presets"][args.preset]["config"])
        print(f"✅  Loaded preset: {args.preset}")
    else:
        cfg = _load_json(DEFAULT_CONFIG)
        print("✅  Loaded default configuration template")

    # Validate-only mode
    if args.validate:
        issues = validate_config(cfg)
        if issues:
            print("\n⚠  Validation issues:")
            for issue in issues:
                print(f"  • {issue}")
            sys.exit(1)
        else:
            print("\n✅  Configuration is valid!")
            sys.exit(0)

    # Dry-run: just preview
    if args.dry_run:
        _preview(cfg)
        issues = validate_config(cfg)
        if issues:
            print("\n⚠  Validation issues:")
            for issue in issues:
                print(f"  • {issue}")
        sys.exit(0)

    # Interactive customization
    cfg = _run_interactive(cfg)

    # Save
    output_path = Path(args.output)
    _save_json(output_path, cfg)


if __name__ == "__main__":
    main()

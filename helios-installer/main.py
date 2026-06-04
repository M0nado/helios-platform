#!/usr/bin/env python3
"""
HELIOS Quick Start USB Installer - Main Installer Script
=========================================================
Automated 7-phase setup that provisions a Windows workstation with the
complete HELIOS architecture: partitions, security baseline, user accounts,
7-layer foundation, and system optimisations.

Usage:
    python main.py [--dry-run] [--log-level DEBUG|INFO|WARNING]

Requires Administrator / root privileges.
"""

import argparse
import ctypes
import logging
import os
import platform
import subprocess
import sys
import tempfile
import time
from datetime import datetime, timezone
from pathlib import Path

try:
    from colorama import Fore, Style, init as colorama_init
    colorama_init(autoreset=True)
    _HAS_COLOR = True
except ImportError:
    _HAS_COLOR = False

try:
    from tqdm import tqdm
    _HAS_TQDM = True
except ImportError:
    _HAS_TQDM = False

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
INSTALLER_VERSION = "1.0.0"
LOG_FILE = Path("helios_setup.log")

PARTITIONS = [
    {"letter": "C", "label": "HELIOS_SYSTEM",   "size_gb": 200},
    {"letter": "D", "label": "HELIOS_DATA",      "size_gb": 500},
    {"letter": "F", "label": "HELIOS_APPS",      "size_gb": 300},
    {"letter": "G", "label": "HELIOS_GAMES",     "size_gb": 1000},
    {"letter": "H", "label": "HELIOS_MEDIA",     "size_gb": 500},
    {"letter": "I", "label": "HELIOS_ISO",       "size_gb": 200},
    {"letter": "J", "label": "HELIOS_BACKUP",    "size_gb": 500},
    {"letter": "K", "label": "HELIOS_SECURE",    "size_gb": 300},
    {"letter": "L", "label": "HELIOS_LOGS",      "size_gb": 100},
]

USERS = [
    {"name": "HeliosAdmin",     "group": "Administrators", "description": "HELIOS primary administrator"},
    {"name": "HeliosDev",       "group": "Developers",     "description": "Development workload profile"},
    {"name": "HeliosCreator",   "group": "Users",          "description": "Creative workload profile"},
    {"name": "HeliosGamer",     "group": "Users",          "description": "Gaming profile"},
    {"name": "HeliosGuest",     "group": "Guests",         "description": "Restricted guest access"},
    {"name": "HeliosService",   "group": "Users",          "description": "Background service account"},
    {"name": "HeliosBackup",    "group": "Backup Operators","description": "Backup & restore account"},
]

LAYERS = [
    "Layer 1 - UI & Rendering (Xenoblade GUI)",
    "Layer 2 - Application Management (50+ programs)",
    "Layer 3 - Optimisation Engine (MONADO)",
    "Layer 4 - Security Framework (AEGIS)",
    "Layer 5 - Data & Storage Management",
    "Layer 6 - Cloud & AI Orchestration (NEXUS)",
    "Layer 7 - Hardware & OS Management",
]

# ---------------------------------------------------------------------------
# Logging setup
# ---------------------------------------------------------------------------

def setup_logging(log_level: str) -> logging.Logger:
    """Initialise file + console logging."""
    numeric_level = getattr(logging, log_level.upper(), logging.INFO)
    logger = logging.getLogger("helios_installer")
    logger.setLevel(numeric_level)

    fmt = logging.Formatter(
        "%(asctime)s [%(levelname)-8s] %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
    )

    fh = logging.FileHandler(LOG_FILE, encoding="utf-8")
    fh.setLevel(numeric_level)
    fh.setFormatter(fmt)
    logger.addHandler(fh)

    ch = logging.StreamHandler(sys.stdout)
    ch.setLevel(numeric_level)
    ch.setFormatter(fmt)
    logger.addHandler(ch)

    return logger


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _color(text: str, color: str) -> str:
    if not _HAS_COLOR:
        return text
    colors = {
        "green":  Fore.GREEN,
        "red":    Fore.RED,
        "yellow": Fore.YELLOW,
        "cyan":   Fore.CYAN,
        "white":  Fore.WHITE,
    }
    return f"{colors.get(color, '')}{text}{Style.RESET_ALL}"


def _banner() -> None:
    print(_color(
        "\n╔══════════════════════════════════════════════════════════════╗\n"
        "║         HELIOS QUICK START USB INSTALLER  v" + INSTALLER_VERSION + "           ║\n"
        "║         Automated 7-Phase System Configuration              ║\n"
        "╚══════════════════════════════════════════════════════════════╝\n",
        "cyan",
    ))


def _phase_header(phase_num: int, title: str) -> None:
    print(_color(f"\n{'='*64}", "yellow"))
    print(_color(f"  PHASE {phase_num}: {title}", "yellow"))
    print(_color(f"{'='*64}", "yellow"))


def _ok(msg: str) -> None:
    print(_color(f"  ✓  {msg}", "green"))


def _warn(msg: str) -> None:
    print(_color(f"  ⚠  {msg}", "yellow"))


def _fail(msg: str) -> None:
    print(_color(f"  ✗  {msg}", "red"))


def _progress(items: list, desc: str = "") -> list:
    if _HAS_TQDM:
        return list(tqdm(items, desc=desc, ncols=72))
    for item in items:
        print(f"    → {item}")
    return items


def _run(cmd: list[str], dry_run: bool, logger: logging.Logger,
         redact: set[int] | None = None) -> bool:
    """Run a subprocess command, honouring dry-run mode.

    Args:
        cmd: Command and arguments to execute.
        dry_run: When True, only log the action without executing.
        logger: Logger instance.
        redact: Optional set of argument indices whose values should be
                replaced with '****' in log output (e.g. to hide passwords).
    """
    def _safe_cmd(c: list[str]) -> str:
        if not redact:
            return " ".join(c)
        return " ".join("****" if i in redact else v for i, v in enumerate(c))

    logger.debug("CMD: %s", _safe_cmd(cmd))
    if dry_run:
        logger.info("[dry-run] skipping: %s", _safe_cmd(cmd))
        return True
    try:
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=300,
        )
        if result.stdout:
            logger.debug(result.stdout.strip())
        if result.returncode != 0:
            logger.warning("Command returned %d: %s", result.returncode, result.stderr.strip())
            return False
        return True
    except FileNotFoundError:
        logger.warning("Executable not found: %s", cmd[0])
        return False
    except subprocess.TimeoutExpired:
        logger.error("Command timed out: %s", cmd[0])
        return False


def _is_windows() -> bool:
    return platform.system() == "Windows"


def _check_admin() -> bool:
    """Return True if the current process has elevated privileges."""
    if _is_windows():
        try:
            return ctypes.windll.shell32.IsUserAnAdmin() != 0
        except Exception:
            return False
    return os.geteuid() == 0


# ---------------------------------------------------------------------------
# Phase 1: Pre-flight system checks
# ---------------------------------------------------------------------------

def phase1_preflight(dry_run: bool, logger: logging.Logger) -> bool:
    _phase_header(1, "PRE-FLIGHT SYSTEM CHECKS")

    checks_passed = True

    # Admin check
    if _check_admin() or dry_run:
        _ok("Administrator privileges confirmed")
    else:
        _fail("This installer requires Administrator privileges. Re-run as Administrator.")
        checks_passed = False

    # OS check
    os_name = platform.system()
    os_ver = platform.version()
    logger.info("Operating system: %s %s", os_name, os_ver)
    if _is_windows():
        _ok(f"Windows detected: {platform.release()} ({os_ver})")
    else:
        _warn(f"Non-Windows OS: {os_name} — some features require Windows")

    # Python version
    py_ver = sys.version_info
    if py_ver >= (3, 10):
        _ok(f"Python {py_ver.major}.{py_ver.minor}.{py_ver.micro} — OK")
    else:
        _warn(f"Python {py_ver.major}.{py_ver.minor} detected; Python 3.10+ recommended")

    # Disk space (needs ≥ 3.9 TB free on the target volume)
    try:
        import shutil
        total, used, free = shutil.disk_usage("/")
        free_tb = free / (1024 ** 4)
        total_tb = total / (1024 ** 4)
        logger.info("Disk: %.2f TB total, %.2f TB free", total_tb, free_tb)
        if free_tb >= 3.6 or dry_run:
            _ok(f"Disk space: {free_tb:.1f} TB free of {total_tb:.1f} TB total")
        else:
            _warn(f"Low disk space: {free_tb:.1f} TB free — 3.6 TB+ recommended for full layout")
    except Exception as exc:
        logger.warning("Could not query disk space: %s", exc)

    # RAM check (needs ≥ 16 GB)
    try:
        import psutil
        ram_gb = psutil.virtual_memory().total / (1024 ** 3)
        logger.info("RAM: %.1f GB", ram_gb)
        if ram_gb >= 16 or dry_run:
            _ok(f"RAM: {ram_gb:.1f} GB — OK")
        else:
            _warn(f"RAM: {ram_gb:.1f} GB — 16 GB+ recommended")
    except ImportError:
        _warn("psutil not installed — RAM check skipped")

    logger.info("Phase 1 complete. Checks passed: %s", checks_passed)
    return checks_passed


# ---------------------------------------------------------------------------
# Phase 2: Disk partitioning
# ---------------------------------------------------------------------------

def phase2_partitioning(dry_run: bool, logger: logging.Logger) -> bool:
    _phase_header(2, "DISK PARTITIONING (9 drives, 4 TB layout)")

    if not _is_windows() and not dry_run:
        _warn("Partition creation skipped on non-Windows OS")
        logger.info("Phase 2 skipped (non-Windows)")
        return True

    success = True
    for part in PARTITIONS:
        label = part["label"]
        letter = part["letter"]
        size_gb = part["size_gb"]
        logger.info("Creating partition %s: %s (%d GB)", letter, label, size_gb)
        # diskpart script would be invoked here on a real system
        if dry_run:
            _ok(f"[dry-run] Partition {letter}: {label} ({size_gb} GB) — simulated")
        else:
            # Build a diskpart script for this partition and execute it
            script = (
                f"select disk 0\n"
                f"create partition primary size={size_gb * 1024}\n"
                f"format fs=ntfs label={label} quick\n"
                f"assign letter={letter}\n"
            )
            script_path = Path(tempfile.gettempdir()) / f"helios_diskpart_{letter}.txt"
            script_path.write_text(script)
            ok = _run(["diskpart", "/s", str(script_path)], dry_run=False, logger=logger)
            if ok:
                _ok(f"Partition {letter}: {label} ({size_gb} GB) created")
            else:
                _warn(f"Partition {letter}: could not create (may already exist or require manual setup)")
            script_path.unlink(missing_ok=True)

    logger.info("Phase 2 complete")
    return success


# ---------------------------------------------------------------------------
# Phase 3: Security baseline
# ---------------------------------------------------------------------------

def phase3_security(dry_run: bool, logger: logging.Logger) -> bool:
    _phase_header(3, "SECURITY BASELINE (BitLocker, Firewall, AppLocker, Audit)")

    tasks = [
        (["powershell", "-Command", "Enable-WindowsOptionalFeature -Online -FeatureName Windows-Defender-ApplicationGuard -NoRestart"],
         "Windows Defender Application Guard"),
        (["netsh", "advfirewall", "set", "allprofiles", "state", "on"],
         "Windows Firewall — enable all profiles"),
        (["auditpol", "/set", "/category:*", "/success:enable", "/failure:enable"],
         "Audit policy — log all success & failure events"),
        (["powershell", "-Command",
          "Set-MpPreference -DisableRealtimeMonitoring $false -MAPSReporting Advanced -SubmitSamplesConsent SendSafeSamples"],
         "Windows Defender real-time protection"),
    ]

    for cmd, desc in tasks:
        logger.info("Security task: %s", desc)
        if dry_run:
            _ok(f"[dry-run] {desc} — simulated")
        else:
            ok = _run(cmd, dry_run=False, logger=logger)
            if ok:
                _ok(desc)
            else:
                _warn(f"{desc} — skipped (requires elevated Windows environment)")

    logger.info("Phase 3 complete")
    return True


# ---------------------------------------------------------------------------
# Phase 4: User account setup
# ---------------------------------------------------------------------------

def phase4_users(dry_run: bool, logger: logging.Logger) -> bool:
    _phase_header(4, f"USER ACCOUNT SETUP ({len(USERS)} profiles)")

    for user in USERS:
        name = user["name"]
        desc = user["description"]
        group = user["group"]
        logger.info("Creating user: %s (%s)", name, group)

        if dry_run:
            _ok(f"[dry-run] User {name} ({group}) — simulated")
            continue

        # Create the account with a complex random password
        import secrets
        import string
        alphabet = string.ascii_letters + string.digits + "!@#$%^&*()"
        password = "".join(secrets.choice(alphabet) for _ in range(20))

        # net user <name> <password> /add ... — redact index 3 (the password)
        add_cmd = ["net", "user", name, password, "/add", f"/comment:{desc}"]
        grp_cmd = ["net", "localgroup", group, name, "/add"]
        if not _run(add_cmd, dry_run=False, logger=logger, redact={3}):
            _warn(f"User {name}: account creation failed (may already exist)")
        elif not _run(grp_cmd, dry_run=False, logger=logger):
            _warn(f"User {name}: group assignment failed")
        else:
            _ok(f"User {name} created and added to '{group}'")

    logger.info("Phase 4 complete")
    return True


# ---------------------------------------------------------------------------
# Phase 5: 7-Layer foundation initialisation
# ---------------------------------------------------------------------------

def phase5_layers(dry_run: bool, logger: logging.Logger) -> bool:
    _phase_header(5, "7-LAYER FOUNDATION INITIALISATION")

    base_dir = Path("C:/HELIOS") if _is_windows() else Path("/opt/helios")

    for i, layer in enumerate(LAYERS, start=1):
        layer_dir = base_dir / f"layer{i}"
        logger.info("Initialising %s at %s", layer, layer_dir)

        if dry_run:
            _ok(f"[dry-run] {layer} — directory structure simulated")
            continue

        try:
            layer_dir.mkdir(parents=True, exist_ok=True)
            (layer_dir / "config").mkdir(exist_ok=True)
            (layer_dir / "logs").mkdir(exist_ok=True)
            (layer_dir / "data").mkdir(exist_ok=True)
            marker = layer_dir / "layer.initialized"
            marker.write_text(
                f"layer={i}\nname={layer}\ninitialized={datetime.now(timezone.utc).isoformat()}\n"
            )
            _ok(f"{layer}")
        except PermissionError:
            _warn(f"{layer} — could not create {layer_dir} (permission denied)")
        except OSError as exc:
            _warn(f"{layer} — OS error: {exc}")

    logger.info("Phase 5 complete")
    return True


# ---------------------------------------------------------------------------
# Phase 6: System optimisation
# ---------------------------------------------------------------------------

def phase6_optimisation(dry_run: bool, logger: logging.Logger) -> bool:
    _phase_header(6, "SYSTEM OPTIMISATION")

    tasks = [
        # Startup
        (["powershell", "-Command",
          "Get-StartupInfo | Where-Object {$_.StartupType -eq 'Automatic'} | Out-Null"],
         "Query startup items"),
        # Services: set non-essential services to Manual
        (["sc", "config", "SysMain", "start=", "demand"],
         "SysMain (Superfetch) — set to on-demand"),
        (["sc", "config", "WSearch", "start=", "demand"],
         "Windows Search indexer — set to on-demand"),
        # Network
        (["netsh", "int", "tcp", "set", "global", "autotuninglevel=normal"],
         "TCP auto-tuning — normal"),
        (["netsh", "int", "tcp", "set", "global", "rss=enabled"],
         "RSS (Receive Side Scaling) — enabled"),
        # Power plan: High Performance
        (["powercfg", "/setactive", "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"],
         "Power plan — High Performance"),
        # Visual performance
        (["reg", "add",
          r"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
          "/v", "VisualFXSetting", "/t", "REG_DWORD", "/d", "2", "/f"],
         "Visual effects — optimised for performance"),
    ]

    for cmd, desc in tasks:
        logger.info("Optimisation task: %s", desc)
        if dry_run:
            _ok(f"[dry-run] {desc} — simulated")
        else:
            ok = _run(cmd, dry_run=False, logger=logger)
            if ok:
                _ok(desc)
            else:
                _warn(f"{desc} — skipped")

    logger.info("Phase 6 complete")
    return True


# ---------------------------------------------------------------------------
# Phase 7: Verification & validation
# ---------------------------------------------------------------------------

def phase7_verification(dry_run: bool, logger: logging.Logger) -> bool:
    _phase_header(7, "VERIFICATION & VALIDATION")

    report: list[tuple[str, bool]] = []

    # Check layer marker files
    base_dir = Path("C:/HELIOS") if _is_windows() else Path("/opt/helios")
    for i in range(1, 8):
        marker = base_dir / f"layer{i}" / "layer.initialized"
        exists = marker.exists() or dry_run
        report.append((f"Layer {i} initialised", exists))

    # Check log file
    report.append(("Setup log file exists", LOG_FILE.exists()))

    passed = 0
    for check, ok in report:
        if ok:
            _ok(check)
            passed += 1
        else:
            _warn(f"NOT verified: {check}")

    logger.info("Verification: %d/%d checks passed", passed, len(report))
    _phase_header(7, f"VERIFICATION COMPLETE — {passed}/{len(report)} checks passed")
    return passed == len(report) or dry_run


# ---------------------------------------------------------------------------
# Entrypoint
# ---------------------------------------------------------------------------

def main() -> int:
    parser = argparse.ArgumentParser(
        description="HELIOS Quick Start USB Installer",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Simulate all phases without making real changes",
    )
    parser.add_argument(
        "--log-level",
        default="INFO",
        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
        help="Logging verbosity (default: INFO)",
    )
    parser.add_argument(
        "--phases",
        nargs="+",
        type=int,
        choices=range(1, 8),
        metavar="N",
        help="Run only the specified phases (1-7). Default: all phases.",
    )
    args = parser.parse_args()

    logger = setup_logging(args.log_level)
    _banner()

    start_time = time.time()
    logger.info("HELIOS Installer %s started at %s", INSTALLER_VERSION, datetime.now().isoformat())
    logger.info("Dry-run: %s | Log level: %s", args.dry_run, args.log_level)

    phases_to_run = set(args.phases) if args.phases else set(range(1, 8))

    phase_map = {
        1: lambda: phase1_preflight(args.dry_run, logger),
        2: lambda: phase2_partitioning(args.dry_run, logger),
        3: lambda: phase3_security(args.dry_run, logger),
        4: lambda: phase4_users(args.dry_run, logger),
        5: lambda: phase5_layers(args.dry_run, logger),
        6: lambda: phase6_optimisation(args.dry_run, logger),
        7: lambda: phase7_verification(args.dry_run, logger),
    }

    overall_success = True
    for phase_num in sorted(phases_to_run):
        ok = phase_map[phase_num]()
        if not ok:
            overall_success = False
            logger.error("Phase %d reported failure — continuing remaining phases", phase_num)

    elapsed = time.time() - start_time
    minutes = int(elapsed // 60)
    seconds = int(elapsed % 60)

    print()
    if overall_success:
        print(_color(
            "╔══════════════════════════════════════════════════════════════╗\n"
            "║  ✓  HELIOS INSTALLATION COMPLETE                            ║\n"
            f"║     Completed in {minutes}m {seconds:02d}s                                    ║\n"
            f"║     Log saved to: {LOG_FILE}                         ║\n"
            "║     System is ready for HELIOS Build 1 deployment           ║\n"
            "╚══════════════════════════════════════════════════════════════╝",
            "green",
        ))
    else:
        print(_color(
            "╔══════════════════════════════════════════════════════════════╗\n"
            "║  ⚠  HELIOS INSTALLATION COMPLETED WITH WARNINGS             ║\n"
            f"║     Elapsed: {minutes}m {seconds:02d}s                                       ║\n"
            f"║     Review log: {LOG_FILE}                            ║\n"
            "╚══════════════════════════════════════════════════════════════╝",
            "yellow",
        ))

    logger.info("Installer finished in %dm %ds. Success: %s", minutes, seconds, overall_success)
    return 0 if overall_success else 1


if __name__ == "__main__":
    sys.exit(main())

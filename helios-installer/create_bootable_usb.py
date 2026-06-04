#!/usr/bin/env python3
"""
HELIOS Bootable USB Creation Tool
==================================
Writes the helios-installer package onto a USB drive so the installer
can be transported to the target machine and run without an internet
connection.

Usage (Windows – Administrator):
    python create_bootable_usb.py --drive E

Usage (Linux/macOS – root):
    sudo python3 create_bootable_usb.py --drive /dev/sdb

Options:
    --drive DRIVE     Target drive letter (Windows: E) or device path
                      (Linux: /dev/sdb).  REQUIRED.
    --source DIR      Directory containing the helios-installer package.
                      Defaults to the directory containing this script.
    --label LABEL     Volume label to apply to the USB partition.
                      Default: HELIOS_USB
    --dry-run         Show what would happen without writing anything.
    --force           Skip the "are you sure?" confirmation prompt.
"""

import argparse
import os
import platform
import shutil
import subprocess
import sys
from pathlib import Path


VOLUME_LABEL = "HELIOS_USB"
REQUIRED_FILES = [
    "main.py",
    "requirements.txt",
    "run_installer.bat",
    "run_installer.sh",
    "create_bootable_usb.py",
    "README.md",
]


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _is_windows() -> bool:
    return platform.system() == "Windows"


def _run(cmd: list[str], dry_run: bool) -> bool:
    if dry_run:
        print(f"  [dry-run] {' '.join(cmd)}")
        return True
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"  [WARN] Command failed: {' '.join(cmd)}")
        if result.stderr:
            print(f"         {result.stderr.strip()}")
        return False
    return True


def _confirm(prompt: str) -> bool:
    ans = input(f"{prompt} [y/N]: ").strip().lower()
    return ans in ("y", "yes")


def _validate_source(source_dir: Path) -> None:
    """Ensure all expected installer files are present in source_dir."""
    missing = [f for f in REQUIRED_FILES if not (source_dir / f).exists()]
    if missing:
        print(f"[ERROR] Source directory is missing files: {', '.join(missing)}")
        print(f"        Expected directory: {source_dir}")
        sys.exit(1)


# ---------------------------------------------------------------------------
# Windows: format & copy
# ---------------------------------------------------------------------------

def _format_windows(drive_letter: str, label: str, dry_run: bool) -> bool:
    """Format the USB drive on Windows using format.com."""
    drive = drive_letter.rstrip(":\\") + ":"
    print(f"  Formatting {drive} as FAT32 with label '{label}' ...")
    cmd = ["format", drive, "/FS:FAT32", f"/V:{label}", "/Q", "/Y"]
    return _run(cmd, dry_run)


def _copy_files_windows(source_dir: Path, drive_letter: str, dry_run: bool) -> None:
    dest = Path(f"{drive_letter.rstrip(':\\')}:\\HELIOS_INSTALLER")
    print(f"  Copying installer files to {dest} ...")
    if not dry_run:
        dest.mkdir(parents=True, exist_ok=True)
    for filename in REQUIRED_FILES:
        src = source_dir / filename
        dst = dest / filename
        if dry_run:
            print(f"  [dry-run] copy {src} → {dst}")
        else:
            shutil.copy2(src, dst)
            print(f"    ✓  {filename}")


# ---------------------------------------------------------------------------
# Linux/macOS: format & copy
# ---------------------------------------------------------------------------

def _format_linux(device: str, label: str, dry_run: bool) -> bool:
    """Format the USB device on Linux/macOS using mkfs.vfat."""
    print(f"  Formatting {device} as FAT32 with label '{label}' ...")
    cmd = ["mkfs.vfat", "-F", "32", "-n", label, device]
    return _run(cmd, dry_run)


def _mount_and_copy_linux(source_dir: Path, device: str, dry_run: bool) -> None:
    mount_point = Path("/mnt/helios_usb")
    print(f"  Mounting {device} at {mount_point} ...")
    if not dry_run:
        mount_point.mkdir(parents=True, exist_ok=True)
    _run(["mount", device, str(mount_point)], dry_run)

    dest = mount_point / "HELIOS_INSTALLER"
    print(f"  Copying installer files to {dest} ...")
    if not dry_run:
        dest.mkdir(parents=True, exist_ok=True)
    for filename in REQUIRED_FILES:
        src = source_dir / filename
        dst = dest / filename
        if dry_run:
            print(f"  [dry-run] copy {src} → {dst}")
        else:
            shutil.copy2(src, dst)
            print(f"    ✓  {filename}")

    print(f"  Unmounting {mount_point} ...")
    _run(["umount", str(mount_point)], dry_run)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> int:
    parser = argparse.ArgumentParser(
        description="HELIOS Bootable USB Creation Tool",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument(
        "--drive",
        required=True,
        metavar="DRIVE",
        help="Drive letter (Windows: E) or device path (Linux: /dev/sdb)",
    )
    parser.add_argument(
        "--source",
        default=str(Path(__file__).parent),
        metavar="DIR",
        help="Source directory containing installer files (default: this script's directory)",
    )
    parser.add_argument(
        "--label",
        default=VOLUME_LABEL,
        metavar="LABEL",
        help=f"Volume label for the USB partition (default: {VOLUME_LABEL})",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Simulate without writing to the drive",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Skip confirmation prompt",
    )
    args = parser.parse_args()

    source_dir = Path(args.source)
    drive = args.drive

    print()
    print(" ============================================================")
    print("  HELIOS Bootable USB Creation Tool")
    print(" ============================================================")
    print()
    print(f"  Source    : {source_dir}")
    print(f"  Target    : {drive}")
    print(f"  Label     : {args.label}")
    print(f"  Dry-run   : {args.dry_run}")
    print()

    _validate_source(source_dir)

    if not args.force and not args.dry_run:
        print(f"  ⚠  WARNING: ALL DATA ON '{drive}' WILL BE ERASED.")
        if not _confirm("  Continue?"):
            print("  Aborted.")
            return 0

    if _is_windows():
        if not _format_windows(drive, args.label, args.dry_run):
            print("[ERROR] Format failed. Aborting.")
            return 1
        _copy_files_windows(source_dir, drive, args.dry_run)
    else:
        if os.geteuid() != 0 and not args.dry_run:
            print("[ERROR] Root privileges required. Re-run with sudo.")
            return 1
        if not _format_linux(drive, args.label, args.dry_run):
            print("[ERROR] Format failed. Aborting.")
            return 1
        _mount_and_copy_linux(source_dir, drive, args.dry_run)

    print()
    print("  ✓  USB drive ready!")
    print(f"     Plug the drive into the target machine and run HELIOS_INSTALLER/run_installer.{'bat' if _is_windows() else 'sh'}")
    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())

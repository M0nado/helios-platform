#!/usr/bin/env python3
"""HELIOS local training (ltrain) entrypoint.

This command validates and launches a local-model training workflow.  It is
implemented as a Python CLI so it can run on Windows, Linux, Codespaces, and
CI without requiring PowerShell or a .NET SDK during configuration checks.
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import os
import shutil
import subprocess
import sys
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Sequence

DEFAULT_MIN_DISK_GB = 10.0
GPU_BACKENDS = ("cuda", "directml", "rocm")


class ValidationError(ValueError):
    """Raised when the ltrain configuration is unsafe or incomplete."""


@dataclass(frozen=True)
class LTrainConfig:
    dataset: Path
    model: Path
    output: Path
    device: str
    epochs: int
    batch_size: int
    learning_rate: float
    min_disk_gb: float
    trainer: str | None
    dry_run: bool


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="ltrain",
        description="Validate and launch a HELIOS local AI model training job.",
    )
    parser.add_argument("--dataset", required=True, type=Path, help="Training data file or directory.")
    parser.add_argument("--model", required=True, type=Path, help="Base model file or directory.")
    parser.add_argument("--output", required=True, type=Path, help="Directory for checkpoints and training logs.")
    parser.add_argument(
        "--device",
        default=os.environ.get("LTRAIN_DEVICE", "cpu"),
        choices=("cpu", *GPU_BACKENDS),
        help="Execution device. Defaults to LTRAIN_DEVICE or cpu.",
    )
    parser.add_argument("--epochs", type=int, default=int(os.environ.get("LTRAIN_EPOCHS", "1")))
    parser.add_argument("--batch-size", type=int, default=int(os.environ.get("LTRAIN_BATCH_SIZE", "1")))
    parser.add_argument("--learning-rate", type=float, default=float(os.environ.get("LTRAIN_LEARNING_RATE", "0.0001")))
    parser.add_argument("--min-disk-gb", type=float, default=float(os.environ.get("LTRAIN_MIN_DISK_GB", DEFAULT_MIN_DISK_GB)))
    parser.add_argument(
        "--trainer",
        default=os.environ.get("LTRAIN_TRAINER"),
        help="Optional external trainer executable or script. Defaults to validation-only dry run unless supplied.",
    )
    parser.add_argument("--dry-run", action="store_true", help="Validate configuration and print the resolved plan without training.")
    return parser


def parse_args(argv: Sequence[str] | None = None) -> LTrainConfig:
    ns = build_parser().parse_args(argv)
    return LTrainConfig(
        dataset=ns.dataset.expanduser(),
        model=ns.model.expanduser(),
        output=ns.output.expanduser(),
        device=ns.device,
        epochs=ns.epochs,
        batch_size=ns.batch_size,
        learning_rate=ns.learning_rate,
        min_disk_gb=ns.min_disk_gb,
        trainer=ns.trainer,
        dry_run=ns.dry_run,
    )


def validate_config(config: LTrainConfig) -> None:
    missing = [name for name, path in (("dataset", config.dataset), ("model", config.model)) if not path.exists()]
    if missing:
        raise ValidationError("Missing required path(s): " + ", ".join(missing))
    if config.epochs < 1:
        raise ValidationError("--epochs must be at least 1")
    if config.batch_size < 1:
        raise ValidationError("--batch-size must be at least 1")
    if not 0 < config.learning_rate <= 1:
        raise ValidationError("--learning-rate must be greater than 0 and less than or equal to 1")
    if config.min_disk_gb < 0:
        raise ValidationError("--min-disk-gb cannot be negative")

    config.output.mkdir(parents=True, exist_ok=True)
    free_gb = shutil.disk_usage(config.output).free / (1024**3)
    if free_gb < config.min_disk_gb:
        raise ValidationError(f"Insufficient disk space at {config.output}: {free_gb:.2f} GiB available, {config.min_disk_gb:.2f} GiB required")

    if config.device != "cpu":
        validate_gpu_backend(config.device)
    if config.trainer:
        validate_trainer(config.trainer)


def validate_gpu_backend(device: str) -> None:
    if device == "cuda":
        if shutil.which("nvidia-smi") is None and importlib.util.find_spec("torch") is None:
            raise ValidationError("CUDA mode requires nvidia-smi or a Python torch installation")
        return
    if device == "directml":
        if importlib.util.find_spec("torch_directml") is None:
            raise ValidationError("DirectML mode requires the torch-directml Python package")
        return
    if device == "rocm":
        if shutil.which("rocminfo") is None:
            raise ValidationError("ROCm mode requires rocminfo on PATH")
        return
    raise ValidationError(f"Unsupported device: {device}")


def validate_trainer(trainer: str) -> None:
    trainer_path = Path(trainer).expanduser()
    if trainer_path.exists():
        return
    if shutil.which(trainer) is None:
        raise ValidationError(f"Trainer dependency not found: {trainer}")


def training_command(config: LTrainConfig) -> list[str]:
    if not config.trainer:
        return []
    return [
        config.trainer,
        "--dataset", str(config.dataset),
        "--model", str(config.model),
        "--output", str(config.output),
        "--device", config.device,
        "--epochs", str(config.epochs),
        "--batch-size", str(config.batch_size),
        "--learning-rate", str(config.learning_rate),
    ]


def run(config: LTrainConfig) -> int:
    validate_config(config)
    plan = asdict(config)
    plan.update({"dataset": str(config.dataset), "model": str(config.model), "output": str(config.output)})
    print(json.dumps({"ltrain": "validated", "plan": plan}, indent=2))
    command = training_command(config)
    if config.dry_run or not command:
        return 0
    return subprocess.run(command, check=False).returncode


def main(argv: Sequence[str] | None = None) -> int:
    try:
        return run(parse_args(argv))
    except ValidationError as exc:
        print(f"ltrain validation failed: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

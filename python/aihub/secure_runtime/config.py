from __future__ import annotations

from dataclasses import dataclass
import ipaddress
import json
import os
from pathlib import Path
from typing import Any


class ConfigurationError(ValueError):
    """Raised when secure-runtime configuration violates a safety invariant."""


def _parse_positive_int(value: Any, *, name: str, default: int) -> int:
    if value is None:
        return default
    try:
        parsed = int(value)
    except (TypeError, ValueError) as exc:
        raise ConfigurationError(f"{name} must be an integer.") from exc
    if parsed <= 0:
        raise ConfigurationError(f"{name} must be greater than zero.")
    return parsed


def _is_loopback(host: str) -> bool:
    normalized = host.strip().lower()
    if normalized == "localhost":
        return True
    try:
        return ipaddress.ip_address(normalized).is_loopback
    except ValueError:
        return False


@dataclass(frozen=True, slots=True)
class SecureRuntimeConfig:
    host: str = "127.0.0.1"
    port: int = 8787
    token_environment_variable: str = "AIHUB_API_TOKEN"
    token_file: Path | None = None
    state_directory: Path = Path("artifacts/aihub/secure-runtime")
    max_request_bytes: int = 65_536
    requests_per_minute: int = 60
    service_name: str = "HELIOS Secure AIHub"

    def __post_init__(self) -> None:
        if not _is_loopback(self.host):
            raise ConfigurationError(
                "Secure AIHub refuses non-loopback binds. Use the governed C# "
                "integration broker for cloud ingress."
            )
        if not 0 <= self.port <= 65_535:
            raise ConfigurationError("port must be between 0 and 65535.")
        if not self.token_environment_variable.strip():
            raise ConfigurationError("token_environment_variable cannot be empty.")
        if self.max_request_bytes <= 0:
            raise ConfigurationError("max_request_bytes must be greater than zero.")
        if self.requests_per_minute <= 0:
            raise ConfigurationError("requests_per_minute must be greater than zero.")

    @classmethod
    def from_mapping(cls, value: dict[str, Any]) -> "SecureRuntimeConfig":
        token_file_value = value.get("token_file")
        return cls(
            host=str(value.get("host", "127.0.0.1")),
            port=_parse_positive_int(value.get("port"), name="port", default=8787),
            token_environment_variable=str(
                value.get("token_environment_variable", "AIHUB_API_TOKEN")
            ),
            token_file=Path(str(token_file_value)).expanduser() if token_file_value else None,
            state_directory=Path(
                str(value.get("state_directory", "artifacts/aihub/secure-runtime"))
            ).expanduser(),
            max_request_bytes=_parse_positive_int(
                value.get("max_request_bytes"),
                name="max_request_bytes",
                default=65_536,
            ),
            requests_per_minute=_parse_positive_int(
                value.get("requests_per_minute"),
                name="requests_per_minute",
                default=60,
            ),
            service_name=str(value.get("service_name", "HELIOS Secure AIHub")),
        )

    @classmethod
    def load(cls, path: Path | None = None) -> "SecureRuntimeConfig":
        data: dict[str, Any] = {}
        if path is not None:
            try:
                loaded = json.loads(path.read_text(encoding="utf-8"))
            except FileNotFoundError as exc:
                raise ConfigurationError(f"Configuration file not found: {path}") from exc
            except json.JSONDecodeError as exc:
                raise ConfigurationError(f"Invalid JSON configuration: {exc}") from exc
            if not isinstance(loaded, dict):
                raise ConfigurationError("Configuration root must be an object.")
            data = loaded

        overrides: dict[str, Any] = {}
        if os.getenv("AIHUB_CONTROL_HOST"):
            overrides["host"] = os.environ["AIHUB_CONTROL_HOST"]
        if os.getenv("AIHUB_CONTROL_PORT"):
            overrides["port"] = os.environ["AIHUB_CONTROL_PORT"]
        if os.getenv("AIHUB_STATE_DIRECTORY"):
            overrides["state_directory"] = os.environ["AIHUB_STATE_DIRECTORY"]
        if os.getenv("AIHUB_MAX_REQUEST_BYTES"):
            overrides["max_request_bytes"] = os.environ["AIHUB_MAX_REQUEST_BYTES"]
        if os.getenv("AIHUB_REQUESTS_PER_MINUTE"):
            overrides["requests_per_minute"] = os.environ[
                "AIHUB_REQUESTS_PER_MINUTE"
            ]

        data.update(overrides)
        return cls.from_mapping(data)

    def read_token(self) -> str:
        token = os.getenv(self.token_environment_variable, "").strip()
        if token:
            return token
        if self.token_file is not None:
            try:
                token = self.token_file.read_text(encoding="utf-8").strip()
            except FileNotFoundError as exc:
                raise ConfigurationError(
                    f"Token file does not exist: {self.token_file}"
                ) from exc
            if token:
                return token
        raise ConfigurationError(
            f"Set {self.token_environment_variable} or configure a non-empty token_file."
        )

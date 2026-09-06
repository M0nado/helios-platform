from __future__ import annotations

from dataclasses import dataclass
import ipaddress
import os
from pathlib import Path


DEFAULT_MAX_REQUEST_BYTES = 64 * 1024
DEFAULT_REQUESTS_PER_MINUTE = 60
DEFAULT_MAX_TASKS_RETURNED = 200


def _default_state_directory() -> Path:
    local_app_data = os.getenv("LOCALAPPDATA")
    if local_app_data:
        return Path(local_app_data) / "HELIOS" / "AIHub"
    return Path.home() / ".local" / "share" / "helios" / "aihub"


@dataclass(frozen=True, slots=True)
class RuntimeConfig:
    """Configuration for the guarded AIHub HTTP surface.

    Only loopback listeners are accepted. The API token is supplied at runtime
    through an environment variable or an explicitly selected local token file;
    it is never stored in repository configuration.
    """

    host: str = "127.0.0.1"
    port: int = 8787
    state_directory: Path = _default_state_directory()
    api_token: str = ""
    max_request_bytes: int = DEFAULT_MAX_REQUEST_BYTES
    requests_per_minute: int = DEFAULT_REQUESTS_PER_MINUTE
    max_tasks_returned: int = DEFAULT_MAX_TASKS_RETURNED

    @classmethod
    def from_environment(cls) -> "RuntimeConfig":
        token = os.getenv("AIHUB_API_KEY", "").strip()
        token_file = os.getenv("AIHUB_API_KEY_FILE", "").strip()
        if not token and token_file:
            path = Path(token_file).expanduser()
            token = path.read_text(encoding="utf-8").strip()

        config = cls(
            host=os.getenv("AIHUB_CONTROL_HOST", "127.0.0.1").strip(),
            port=int(os.getenv("AIHUB_CONTROL_PORT", "8787")),
            state_directory=Path(
                os.getenv("AIHUB_STATE_DIRECTORY", str(_default_state_directory()))
            ).expanduser(),
            api_token=token,
            max_request_bytes=int(
                os.getenv("AIHUB_MAX_REQUEST_BYTES", str(DEFAULT_MAX_REQUEST_BYTES))
            ),
            requests_per_minute=int(
                os.getenv(
                    "AIHUB_REQUESTS_PER_MINUTE",
                    str(DEFAULT_REQUESTS_PER_MINUTE),
                )
            ),
            max_tasks_returned=int(
                os.getenv("AIHUB_MAX_TASKS_RETURNED", str(DEFAULT_MAX_TASKS_RETURNED))
            ),
        )
        config.validate()
        return config

    def validate(self) -> None:
        host = self.host.strip().lower()
        if host == "localhost":
            pass
        else:
            try:
                address = ipaddress.ip_address(host)
            except ValueError as exc:
                raise ValueError(
                    "AIHub must bind to localhost or a literal loopback address."
                ) from exc
            if not address.is_loopback:
                raise ValueError("Non-loopback AIHub listeners are prohibited.")

        if not 0 <= self.port <= 65535:
            raise ValueError("port must be between 0 and 65535")
        if len(self.api_token) < 24:
            raise ValueError(
                "AIHUB_API_KEY must contain at least 24 characters and must be "
                "provided outside source control."
            )
        if not 1024 <= self.max_request_bytes <= 1024 * 1024:
            raise ValueError("max_request_bytes must be between 1 KiB and 1 MiB")
        if not 1 <= self.requests_per_minute <= 600:
            raise ValueError("requests_per_minute must be between 1 and 600")
        if not 1 <= self.max_tasks_returned <= 1000:
            raise ValueError("max_tasks_returned must be between 1 and 1000")

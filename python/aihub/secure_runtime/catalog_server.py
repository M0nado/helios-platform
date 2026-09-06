from __future__ import annotations

from dataclasses import asdict
from http import HTTPStatus
import logging
from pathlib import Path
from typing import Any
from urllib.parse import parse_qs, urlparse

from python.aihub.canonical_catalog import (
    build_engine_catalog,
    build_model_registry,
    build_security_plan,
    build_vm_topology,
    recommend_engine_mix,
)

from .config import RuntimeConfig
from .server import SecureAIHubHandler, SecureThreadingHTTPServer
from .storage import AtomicTaskStore


_LOG = logging.getLogger("helios.aihub.secure_runtime.catalog")
_CATALOG_ROUTES = frozenset(
    {
        "/api/security/plan",
        "/api/vm/topology",
        "/api/models/registry",
        "/api/engines/catalog",
        "/api/engines/recommend",
    }
)


def _parse_bool(value: str, *, default: bool) -> bool:
    normalized = value.strip().lower()
    if not normalized:
        return default
    if normalized in {"1", "true", "yes", "on"}:
        return True
    if normalized in {"0", "false", "no", "off"}:
        return False
    raise ValueError("boolean value must be true or false")


class CatalogAIHubHandler(SecureAIHubHandler):
    """Adds read-only, proposal-only catalog endpoints to the secure runtime."""

    def do_GET(self) -> None:  # noqa: N802
        parsed = urlparse(self.path)
        route = parsed.path
        if route not in _CATALOG_ROUTES:
            super().do_GET()
            return
        if not self._passes_rate_limit():
            return
        if not self._require_authorization():
            return

        query = parse_qs(parsed.query)
        try:
            if route == "/api/security/plan":
                profile = query.get("profile", ["balanced"])[0]
                self._json(
                    {
                        "plan": asdict(build_security_plan(profile)),
                        "proposalOnly": True,
                        "productionEnabled": False,
                    }
                )
                return

            if route == "/api/vm/topology":
                self._json(
                    {
                        "targets": [asdict(target) for target in build_vm_topology()],
                        "proposalOnly": True,
                        "productionEnabled": False,
                    }
                )
                return

            if route == "/api/models/registry":
                models = [asdict(model) for model in build_model_registry()]
                self._json(
                    {
                        "count": len(models),
                        "models": models,
                        "productionEnabled": False,
                    }
                )
                return

            cuda_enabled = _parse_bool(
                query.get("cuda", ["true"])[0],
                default=True,
            )
            if route == "/api/engines/catalog":
                self._json(build_engine_catalog(cuda_enabled=cuda_enabled))
                return

            security_profile = query.get("security_profile", ["balanced"])[0]
            optimization_pressure = float(
                query.get("optimization_pressure", ["0.5"])[0]
            )
            fleet_size = int(query.get("fleet_size", ["0"])[0])
            self._json(
                recommend_engine_mix(
                    cuda_enabled=cuda_enabled,
                    security_profile=security_profile,
                    optimization_pressure=optimization_pressure,
                    fleet_size=fleet_size,
                )
            )
        except (TypeError, ValueError) as exc:
            self._error(
                status=HTTPStatus.BAD_REQUEST,
                code="invalid_catalog_request",
                message=str(exc),
            )


def create_catalog_server(
    config: RuntimeConfig,
    *,
    task_store: AtomicTaskStore | None = None,
) -> SecureThreadingHTTPServer:
    config.validate()
    store = task_store or AtomicTaskStore(Path(config.state_directory) / "tasks.json")
    return SecureThreadingHTTPServer(
        (config.host, config.port),
        CatalogAIHubHandler,
        runtime_config=config,
        task_store=store,
    )


def run_catalog_server(config: RuntimeConfig | None = None) -> None:
    active_config = config or RuntimeConfig.from_environment()
    server = create_catalog_server(active_config)
    _LOG.info(
        "HELIOS AIHub catalog runtime listening on http://%s:%s",
        active_config.host,
        server.server_address[1],
    )
    try:
        server.serve_forever(poll_interval=0.25)
    except KeyboardInterrupt:
        pass
    finally:
        server.shutdown()
        server.server_close()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    run_catalog_server()

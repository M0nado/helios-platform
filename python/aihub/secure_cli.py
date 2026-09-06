from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import sys
from typing import Any
from urllib import error, parse, request


_ALLOWED_HOSTS = frozenset({"127.0.0.1", "::1", "localhost"})


def _load_token() -> str:
    token = os.getenv("AIHUB_API_KEY", "").strip()
    token_file = os.getenv("AIHUB_API_KEY_FILE", "").strip()
    if not token and token_file:
        token = Path(token_file).expanduser().read_text(encoding="utf-8").strip()
    if len(token) < 24:
        raise RuntimeError(
            "Set AIHUB_API_KEY or AIHUB_API_KEY_FILE to a local token of at least 24 characters."
        )
    return token


def _validate_base_url(value: str) -> str:
    parsed = parse.urlparse(value)
    if parsed.scheme != "http":
        raise ValueError("The local AIHub CLI accepts http:// loopback URLs only.")
    if parsed.hostname not in _ALLOWED_HOSTS:
        raise ValueError("The local AIHub CLI refuses non-loopback hosts.")
    if parsed.username or parsed.password:
        raise ValueError("Credentials must not be embedded in the URL.")
    if parsed.query or parsed.fragment:
        raise ValueError("The base URL cannot contain a query or fragment.")
    return value.rstrip("/")


def _request_json(
    *,
    base_url: str,
    path: str,
    method: str,
    token: str,
    payload: dict[str, Any] | None = None,
    timeout: float = 10.0,
) -> dict[str, Any]:
    url = f"{base_url}/{path.lstrip('/')}"
    body = None
    headers = {
        "Accept": "application/json",
        "Authorization": f"Bearer {token}",
        "User-Agent": "HELIOS-Secure-CLI/1",
    }
    if payload is not None:
        body = json.dumps(payload, allow_nan=False).encode("utf-8")
        headers["Content-Type"] = "application/json"

    command = request.Request(url=url, data=body, method=method, headers=headers)
    try:
        with request.urlopen(command, timeout=timeout) as response:
            raw = response.read().decode("utf-8")
            decoded = json.loads(raw) if raw else {}
            if not isinstance(decoded, dict):
                raise RuntimeError("AIHub returned a non-object JSON response.")
            return decoded
    except error.HTTPError as exc:
        raw = exc.read().decode("utf-8", errors="replace")
        try:
            decoded = json.loads(raw) if raw else {}
        except json.JSONDecodeError:
            decoded = {"error": {"code": "http_error", "message": raw[:500]}}
        raise RuntimeError(
            json.dumps(
                {
                    "status": exc.code,
                    "response": decoded,
                },
                sort_keys=True,
            )
        ) from exc
    except error.URLError as exc:
        raise RuntimeError(f"Could not connect to local AIHub: {exc.reason}") from exc


def _add_json_flag(parser: argparse.ArgumentParser) -> None:
    parser.add_argument(
        "--compact",
        action="store_true",
        help="Print compact JSON rather than indented JSON.",
    )


def _print(payload: dict[str, Any], *, compact: bool) -> None:
    print(json.dumps(payload, separators=(",", ":") if compact else None, indent=None if compact else 2, sort_keys=True))


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="helios-ai",
        description="Guarded loopback CLI for the HELIOS AIHub secure runtime.",
    )
    parser.add_argument(
        "--base-url",
        default=os.getenv("AIHUB_BASE_URL", "http://127.0.0.1:8787"),
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=float(os.getenv("AIHUB_TIMEOUT", "10")),
    )
    commands = parser.add_subparsers(dest="command", required=True)

    for name in ("status", "tasks", "topology", "models", "engines"):
        subparser = commands.add_parser(name)
        _add_json_flag(subparser)

    tasks = commands.choices["tasks"]
    tasks.add_argument("--limit", type=int, default=50)

    queue = commands.add_parser("queue")
    queue.add_argument("task_type")
    queue.add_argument("--priority", choices=("low", "normal", "high"), default="normal")
    queue.add_argument("--payload-json", default="{}")
    _add_json_flag(queue)

    training = commands.add_parser("training-proposal")
    training.add_argument("--cycles", type=int, default=1)
    _add_json_flag(training)

    security = commands.add_parser("security-plan")
    security.add_argument("--profile", choices=("balanced", "paranoid", "offline"), default="balanced")
    _add_json_flag(security)

    recommend = commands.add_parser("recommend")
    recommend.add_argument("--cuda", action=argparse.BooleanOptionalAction, default=True)
    recommend.add_argument("--security-profile", choices=("balanced", "paranoid", "offline"), default="balanced")
    recommend.add_argument("--optimization-pressure", type=float, default=0.5)
    recommend.add_argument("--fleet-size", type=int, default=0)
    _add_json_flag(recommend)
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    try:
        base_url = _validate_base_url(args.base_url)
        if not 0.1 <= args.timeout <= 60.0:
            raise ValueError("timeout must be between 0.1 and 60 seconds")
        token = _load_token()

        if args.command == "status":
            result = _request_json(base_url=base_url, path="/api/status", method="GET", token=token, timeout=args.timeout)
        elif args.command == "tasks":
            if not 1 <= args.limit <= 1000:
                raise ValueError("limit must be between 1 and 1000")
            result = _request_json(base_url=base_url, path=f"/api/tasks?limit={args.limit}", method="GET", token=token, timeout=args.timeout)
        elif args.command == "queue":
            payload = json.loads(args.payload_json)
            if not isinstance(payload, dict):
                raise ValueError("--payload-json must decode to a JSON object")
            result = _request_json(
                base_url=base_url,
                path="/api/tasks",
                method="POST",
                token=token,
                timeout=args.timeout,
                payload={"task_type": args.task_type, "priority": args.priority, "payload": payload},
            )
        elif args.command == "training-proposal":
            if not 1 <= args.cycles <= 100:
                raise ValueError("cycles must be between 1 and 100")
            result = _request_json(
                base_url=base_url,
                path="/api/train/trigger",
                method="POST",
                token=token,
                timeout=args.timeout,
                payload={"cycles": args.cycles},
            )
        elif args.command == "security-plan":
            result = _request_json(
                base_url=base_url,
                path=f"/api/security/plan?profile={parse.quote(args.profile)}",
                method="GET",
                token=token,
                timeout=args.timeout,
            )
        elif args.command == "topology":
            result = _request_json(base_url=base_url, path="/api/vm/topology", method="GET", token=token, timeout=args.timeout)
        elif args.command == "models":
            result = _request_json(base_url=base_url, path="/api/models/registry", method="GET", token=token, timeout=args.timeout)
        elif args.command == "engines":
            result = _request_json(base_url=base_url, path="/api/engines/catalog", method="GET", token=token, timeout=args.timeout)
        elif args.command == "recommend":
            query = parse.urlencode(
                {
                    "cuda": str(args.cuda).lower(),
                    "security_profile": args.security_profile,
                    "optimization_pressure": args.optimization_pressure,
                    "fleet_size": args.fleet_size,
                }
            )
            result = _request_json(base_url=base_url, path=f"/api/engines/recommend?{query}", method="GET", token=token, timeout=args.timeout)
        else:
            parser.error(f"Unsupported command: {args.command}")
            return 2

        _print(result, compact=args.compact)
        return 0
    except (json.JSONDecodeError, OSError, RuntimeError, ValueError) as exc:
        print(json.dumps({"error": str(exc)}, sort_keys=True), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

from __future__ import annotations

from dataclasses import asdict, dataclass
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import tempfile
import threading
from typing import Any
from uuid import uuid4


_TASK_TYPE = re.compile(r"^[a-z][a-z0-9_.-]{0,63}$")
_ALLOWED_PRIORITIES = frozenset({"low", "normal", "high"})


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


@dataclass(frozen=True, slots=True)
class TaskRecord:
    task_id: str
    task_type: str
    priority: str
    status: str
    created_at: str
    updated_at: str
    source: str
    payload: dict[str, Any]


class AtomicTaskStore:
    """Thread-safe JSON task queue with atomic replace semantics.

    This store records proposals and queued work only. It contains no executor,
    subprocess bridge, shell adapter, or privileged operation path.
    """

    def __init__(self, path: Path):
        self.path = Path(path)
        self._lock = threading.RLock()
        self.path.parent.mkdir(parents=True, exist_ok=True)
        if not self.path.exists():
            self._write_unlocked({"schemaVersion": 1, "tasks": []})

    def _load_unlocked(self) -> dict[str, Any]:
        try:
            data = json.loads(self.path.read_text(encoding="utf-8"))
        except FileNotFoundError:
            return {"schemaVersion": 1, "tasks": []}
        except (OSError, json.JSONDecodeError) as exc:
            raise RuntimeError(f"Task store is unreadable: {self.path}") from exc

        if not isinstance(data, dict) or not isinstance(data.get("tasks"), list):
            raise RuntimeError("Task store has an invalid schema.")
        return data

    def _write_unlocked(self, data: dict[str, Any]) -> None:
        encoded = (json.dumps(data, indent=2, sort_keys=True) + "\n").encode("utf-8")
        descriptor, temp_name = tempfile.mkstemp(
            prefix=f".{self.path.name}.",
            suffix=".tmp",
            dir=self.path.parent,
        )
        temp_path = Path(temp_name)
        try:
            with os.fdopen(descriptor, "wb") as handle:
                handle.write(encoded)
                handle.flush()
                os.fsync(handle.fileno())
            os.replace(temp_path, self.path)
        finally:
            temp_path.unlink(missing_ok=True)

    @staticmethod
    def _validate_payload(payload: dict[str, Any]) -> None:
        try:
            json.dumps(payload, allow_nan=False)
        except (TypeError, ValueError) as exc:
            raise ValueError("Task payload must be finite JSON data.") from exc

    def enqueue(
        self,
        *,
        task_type: str,
        priority: str,
        payload: dict[str, Any],
        source: str = "api",
    ) -> TaskRecord:
        normalized_type = task_type.strip().lower()
        normalized_priority = priority.strip().lower()
        if not _TASK_TYPE.fullmatch(normalized_type):
            raise ValueError("task_type must match ^[a-z][a-z0-9_.-]{0,63}$")
        if normalized_priority not in _ALLOWED_PRIORITIES:
            raise ValueError("priority must be low, normal, or high")
        if not isinstance(payload, dict):
            raise ValueError("payload must be a JSON object")
        self._validate_payload(payload)

        now = utc_now()
        record = TaskRecord(
            task_id=str(uuid4()),
            task_type=normalized_type,
            priority=normalized_priority,
            status="queued",
            created_at=now,
            updated_at=now,
            source=source,
            payload=payload,
        )

        with self._lock:
            data = self._load_unlocked()
            data["tasks"].append(asdict(record))
            self._write_unlocked(data)
        return record

    def list_tasks(self, *, limit: int = 200) -> list[dict[str, Any]]:
        bounded_limit = max(1, min(int(limit), 1000))
        with self._lock:
            tasks = self._load_unlocked()["tasks"]
            return list(reversed(tasks[-bounded_limit:]))

    def count(self) -> int:
        with self._lock:
            return len(self._load_unlocked()["tasks"])

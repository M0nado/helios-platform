from __future__ import annotations

import json
import os
from pathlib import Path
import tempfile
import threading
from typing import Any, Callable


class StateError(RuntimeError):
    """Raised when secure runtime state cannot be loaded or persisted safely."""


class AtomicJsonStore:
    """Thread-safe JSON object store using fsync and atomic replacement.

    The store never exposes a partially written queue or proposal document. A
    caller supplies an object-producing mutation callback, which runs while the
    process-local lock is held. Cross-process writers are intentionally not
    supported; the secure runtime owns its state directory exclusively.
    """

    def __init__(self, path: Path, *, default_factory: Callable[[], Any]) -> None:
        self.path = path
        self.default_factory = default_factory
        self._lock = threading.RLock()

    def read(self) -> Any:
        with self._lock:
            if not self.path.exists():
                return self.default_factory()
            try:
                return json.loads(self.path.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError) as exc:
                raise StateError(f"Unable to read valid JSON state from {self.path}") from exc

    def write(self, value: Any) -> None:
        serialized = json.dumps(
            value,
            indent=2,
            ensure_ascii=False,
            sort_keys=True,
        ) + "\n"

        with self._lock:
            self.path.parent.mkdir(parents=True, exist_ok=True)
            temporary_name: str | None = None
            try:
                with tempfile.NamedTemporaryFile(
                    mode="w",
                    encoding="utf-8",
                    newline="\n",
                    prefix=f".{self.path.name}.",
                    suffix=".tmp",
                    dir=self.path.parent,
                    delete=False,
                ) as handle:
                    temporary_name = handle.name
                    handle.write(serialized)
                    handle.flush()
                    os.fsync(handle.fileno())

                os.replace(temporary_name, self.path)
                temporary_name = None

                # Persist the directory entry on POSIX where directory fsync is
                # available. Windows guarantees ReplaceFile/MoveFileEx behavior
                # through os.replace; opening directories is not portable there.
                if os.name != "nt":
                    directory_fd = os.open(self.path.parent, os.O_RDONLY)
                    try:
                        os.fsync(directory_fd)
                    finally:
                        os.close(directory_fd)
            except OSError as exc:
                raise StateError(f"Unable to atomically write {self.path}") from exc
            finally:
                if temporary_name:
                    try:
                        Path(temporary_name).unlink(missing_ok=True)
                    except OSError:
                        pass

    def update(self, mutation: Callable[[Any], Any]) -> Any:
        with self._lock:
            current = self.read()
            updated = mutation(current)
            self.write(updated)
            return updated

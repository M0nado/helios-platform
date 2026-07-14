from __future__ import annotations

from dataclasses import dataclass


@dataclass
class VMTarget:
    backend: str  # wsl2 | hyperv | docker
    role: str
    gpu: bool
    memory_gb: int


class VMOrchestrator:
    def build_default_topology(self) -> list[VMTarget]:
        return [
            VMTarget("docker", "gateway+api", True, 8),
            VMTarget("docker", "gui+control", False, 4),
            VMTarget("wsl2", "trainer", True, 16),
            VMTarget("hyperv", "security-isolation", False, 4),
        ]

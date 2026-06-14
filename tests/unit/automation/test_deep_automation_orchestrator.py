#!/usr/bin/env python3
"""Tests for deep automation orchestrator secret redaction."""
from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "scripts" / "automation" / "deep_automation_orchestrator.py"
spec = importlib.util.spec_from_file_location("deep_automation_orchestrator", MODULE_PATH)
assert spec and spec.loader
orchestrator = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = orchestrator
spec.loader.exec_module(orchestrator)


class RemoteRedactionTests(unittest.TestCase):
    def test_redacts_authenticated_https_remote(self) -> None:
        line = "origin\thttps://user:ghp_secret123@github.com/org/repo.git (fetch)"

        sanitized = orchestrator.sanitize_remote_line(line)

        self.assertEqual(sanitized, "origin\thttps://[redacted]@github.com/org/repo.git (fetch)")
        self.assertNotIn("ghp_secret123", sanitized)
        self.assertNotIn("user:", sanitized)

    def test_redacts_query_secret_parameters(self) -> None:
        line = "origin\thttps://github.com/org/repo.git?token=abc123&ref=main (push)"

        sanitized = orchestrator.sanitize_remote_line(line)

        self.assertEqual(sanitized, "origin\thttps://github.com/org/repo.git?token=%5Bredacted%5D&ref=main (push)")
        self.assertNotIn("abc123", sanitized)

    def test_preserves_ssh_remote_without_credentials(self) -> None:
        line = "origin\tgit@github.com:org/repo.git (fetch)"

        self.assertEqual(orchestrator.sanitize_remote_line(line), line)


if __name__ == "__main__":
    unittest.main()

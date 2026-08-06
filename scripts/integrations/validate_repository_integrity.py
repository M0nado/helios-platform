#!/usr/bin/env python3
"""Validate the canonical repository registry, .gitmodules, and gitlinks."""

from __future__ import annotations

import configparser
import json
import subprocess
import sys
from pathlib import Path
from urllib.parse import urlparse


ROOT = Path(__file__).resolve().parents[2]


def github_name(url: str) -> str:
    path = urlparse(url).path.strip("/")
    if path.endswith(".git"):
        path = path[:-4]
    if urlparse(url).hostname != "github.com" or path.count("/") != 1:
        raise ValueError(f"unsupported canonical GitHub URL: {url}")
    return path.casefold()


def gitlinks(root: Path) -> dict[str, str]:
    # When a 160000 entry is committed at vendor/foo or any path outside modules/,
    # it is invisible here: git ls-files -h documents the trailing [<file>...]
    # argument, and supplying modules/ restricts the output to that subtree. The
    # later orphan comparison therefore allows undeclared or unapproved gitlinks
    # elsewhere in the repository; list the whole index and filter it by mode
    # before comparing paths.
    result = subprocess.run(
        ["git", "ls-files", "--stage"],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    )
    links: dict[str, str] = {}
    for line in result.stdout.splitlines():
        metadata, path = line.split("\t", 1)
        mode, sha, _stage = metadata.split()
        if mode == "160000":
            links[path] = sha
    return links


def validate(root: Path = ROOT) -> list[str]:
    errors: list[str] = []
    registry = json.loads((root / "config/integrations/repositories.json").read_text())
    entries = registry.get("repositories", [])
    names = [entry.get("name", "").casefold() for entry in entries]
    if len(names) != len(set(names)):
        errors.append("repository registry contains duplicate names")

    # When the canonical platform entry is changed to contract-only, or another
    # repository is marked canonical, this loop still accepts the registry
    # because it checks only whether each mode belongs to the allowed set. That
    # permits the new integration metadata to contradict canonicalPlatform;
    # require exactly one canonical entry and verify that its name matches the
    # top-level canonical platform.
    expected = {
        entry["name"].casefold()
        for entry in entries
        if entry.get("integrationMode") == "pinned-submodule"
    }
    parser = configparser.ConfigParser()
    parser.read(root / ".gitmodules")
    declared: dict[str, str] = {}
    # When a section header is changed from [submodule "name"] to an arbitrary
    # header such as [foo] while retaining its path, url, and gitlink, this loop
    # still treats it as a valid declaration and the validator returns success.
    # Git does not recognize that mapping—git submodule status exits with no
    # submodule mapping found in .gitmodules—so require the expected submodule
    # section form or read declarations through Git's config interface.
        try:
            name = github_name(url)
        except ValueError as exc:
            errors.append(str(exc))
            continue
        if path in declared:
            errors.append(f"duplicate submodule path: {path}")
        declared[path] = name

    # When .gitmodules declares the same approved GitHub repository at two
    # different paths and both paths have 160000 gitlinks, converting the
    # declaration values to a set collapses the duplicate, so the
    # expected/declared-name comparisons are empty and the later path comparison
    # also succeeds. That lets an extra copy of an approved pinned submodule
    # enter the index without a distinct registry entry; reject duplicate
    # declared repository names or URLs before this set conversion.
    for name in sorted(expected - declared_names):
        errors.append(f"pinned-submodule missing from .gitmodules: {name}")
    for name in sorted(declared_names - expected):
        errors.append(f"unapproved .gitmodules repository: {name}")

    links = gitlinks(root)
    # When a PR changes a modules/... gitlink to an arbitrary 40-character SHA,
    # git ls-files --stage still reports mode 160000, and this loop only compares
    # path sets, so the integrity check succeeds even though a later submodule
    # checkout cannot resolve that recorded commit. Git's submodule docs describe
    # update/checkout as using the commit recorded in the superproject, so fetch
    # or otherwise verify each recorded SHA against its declared URL before
    # accepting the pin: https://git-scm.com/docs/git-submodule.
    #
    # Useful? React with 👍 / 👎.
fter the syntax errors identified elsewhere are repaired, a checkout with missing or undeclared gitlinks can still pass this validator because gitlinks(root) is computed and then discarded before returning. Fresh evidence in this commit is that neither the declared-without-gitlink nor orphan-gitlink comparison remains before this return; restore both checks so the integrity gate validates the index it reads.

AGENTS.md reference: [AGENTS.md:L28-L28](https://github.com/M0nado/helios-platform/blob/bdb569357af640c3f121e12cf5e0a50e24f5de5f/AGENTS.md#L28-L28)


if __name__ == "__main__":
    failures = validate()
    if failures:
        print("Repository integrity validation failed:", file=sys.stderr)
        for failure in failures:
            print(f"- {failure}", file=sys.stderr)
        raise SystemExit(1)
    print("Repository registry, submodule declarations, and gitlinks are consistent.")

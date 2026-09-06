#!/usr/bin/env python3
"""Bounded, read-only HELIOS client checks. No token or process output is retained."""
from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass
from datetime import datetime, timezone
import json
import os
import shutil
import subprocess
from typing import Callable, Sequence


@dataclass(frozen=True)
class Check:
    id: str
    command: tuple[str, ...]
    kind: str


TOOLS = (
    Check('github-cli', ('gh', '--version'), 'tool'),
    Check('azure-cli', ('az', 'version'), 'tool'),
    Check('azure-developer-cli', ('azd', 'version'), 'tool'),
    Check('azure-devops-cli', ('az', 'extension', 'show', '--name', 'azure-devops', '--query', 'version', '--output', 'tsv'), 'tool'),
    Check('codex', ('codex', '--version'), 'tool'),
    Check('claude-code', ('claude', '--version'), 'tool'),
    Check('dotnet', ('dotnet', '--version'), 'tool'),
    Check('node', ('node', '--version'), 'tool'),
    Check('powershell', ('pwsh', '--version'), 'tool'),
)
AUTH = (
    Check('github-cli', ('gh', 'auth', 'status', '--hostname', 'github.com'), 'session'),
    Check('azure-cli', ('az', 'account', 'show', '--output', 'none'), 'session'),
    Check('azure-developer-cli', ('azd', 'auth', 'login', '--check-status'), 'session'),
    Check('codex', ('codex', 'login', 'status'), 'session'),
    Check('claude-code', ('claude', 'auth', 'status'), 'session'),
)
ALLOWED = frozenset(TOOLS + AUTH)


def probe(check: Check, timeout: float = 20.0, *,
          locate: Callable = shutil.which, run: Callable = subprocess.run) -> dict:
    if check not in ALLOWED:
        raise ValueError('Check is outside the fixed read-only allowlist')
    if not 1 <= timeout <= 60:
        raise ValueError('Timeout must be from 1 to 60 seconds')
    executable = locate(check.command[0])
    result = {'id': check.id, 'kind': check.kind, 'status': 'missing', 'exitCode': None}
    if not executable:
        return result
    env = dict(os.environ)
    env.update({'CI': 'true', 'GIT_TERMINAL_PROMPT': '0', 'GH_PROMPT_DISABLED': '1',
                'AZURE_EXTENSION_USE_DYNAMIC_INSTALL': 'no'})
    try:
        completed = run([executable, *check.command[1:]], stdin=subprocess.DEVNULL,
                        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                        timeout=timeout, check=False, shell=False, env=env)
        result['exitCode'] = completed.returncode
        # Local CLI credentials being present does not prove remote access.
        result['status'] = ('tool-available' if check.kind == 'tool' else 'session-check-passed') if completed.returncode == 0 else 'check-failed'
    except subprocess.TimeoutExpired:
        result['status'] = 'timeout'
    except OSError:
        result['status'] = 'execution-failed'
    return result


def report(*, check_sessions: bool = False, workers: int = 4, timeout: float = 20.0,
           perform: Callable = probe) -> dict:
    if not 1 <= workers <= 4:
        raise ValueError('Parallelism must be from 1 to 4')
    checks = TOOLS + (AUTH if check_sessions else ())
    with ThreadPoolExecutor(max_workers=workers) as pool:
        results = list(pool.map(lambda check: perform(check, timeout), checks))
    return {
        'schemaVersion': 1,
        'generatedAt': datetime.now(timezone.utc).isoformat(),
        'checks': results,
        'sessionChecksRequested': check_sessions,
        'cliChecksPassed': all(x['status'] in {'tool-available', 'session-check-passed'} for x in results),
        'endToEndVerified': False,
        'unverifiedSurfaces': ['chatgpt-mcp-oauth', 'github-browser-session',
                              'azure-devops-project-access', 'linear', 'slack',
                              'sharepoint', 'azure-what-if', 'provider-inference'],
        'externalWrites': 0,
        'credentialValuesRecorded': False,
    }


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check-sessions', action='store_true', help='Run read-only CLI session checks; these may contact providers.')
    parser.add_argument('--workers', type=int, choices=range(1, 5), default=4)
    parser.add_argument('--timeout', type=float, default=20)
    args = parser.parse_args(argv)
    if not 1 <= args.timeout <= 60:
        parser.error('--timeout must be from 1 to 60 seconds')
    result = report(check_sessions=args.check_sessions, workers=args.workers, timeout=args.timeout)
    print(json.dumps(result, indent=2))
    return 0 if result['cliChecksPassed'] else 2


if __name__ == '__main__':
    raise SystemExit(main())

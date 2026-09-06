import subprocess
import threading
import time
import unittest
from unittest.mock import Mock
from check_clients import AUTH, TOOLS, Check, probe, report


class ClientChecksTests(unittest.TestCase):
    def test_missing_executable(self):
        run = Mock()
        self.assertEqual(probe(TOOLS[0], locate=lambda _: None, run=run)['status'], 'missing')
        run.assert_not_called()

    def test_no_output_or_shell(self):
        run = Mock(return_value=subprocess.CompletedProcess([], 0))
        result = probe(AUTH[0], locate=lambda _: '/trusted/gh', run=run)
        self.assertEqual(result['status'], 'session-check-passed')
        kwargs = run.call_args.kwargs
        self.assertFalse(kwargs['shell'])
        self.assertEqual(kwargs['stdout'], subprocess.DEVNULL)
        self.assertEqual(kwargs['stderr'], subprocess.DEVNULL)
        self.assertEqual(kwargs['stdin'], subprocess.DEVNULL)
        self.assertEqual(kwargs['env']['AZURE_EXTENSION_USE_DYNAMIC_INSTALL'], 'no')

    def test_timeout_is_sanitized(self):
        run = Mock(side_effect=subprocess.TimeoutExpired('private-command', 1, output='private-output'))
        self.assertEqual(probe(TOOLS[0], locate=lambda _: '/trusted/gh', run=run)['status'], 'timeout')
        self.assertNotIn('private', str(probe(TOOLS[0], locate=lambda _: '/trusted/gh', run=run)))

    def test_unknown_commands_refused(self):
        with self.assertRaises(ValueError):
            probe(Check('github-cli', ('gh', 'auth', 'token'), 'session'))

    def test_tool_check_failure(self):
        run = Mock(return_value=subprocess.CompletedProcess([], 1))
        self.assertEqual(probe(TOOLS[0], locate=lambda _: '/trusted/gh', run=run)['status'], 'check-failed')

    def test_default_never_checks_sessions(self):
        performed = []
        def fake(c, t):
            performed.append(c)
            return {'status': 'tool-available'}
        result = report(perform=fake)
        self.assertEqual(set(performed), set(TOOLS))
        self.assertFalse(result['endToEndVerified'])
        self.assertFalse(result['sessionChecksRequested'])

    def test_all_clients_can_be_checked(self):
        result = report(check_sessions=True, perform=lambda c,t: {'id': c.id, 'status': 'session-check-passed'})
        self.assertEqual(len(result['checks']), len(TOOLS)+len(AUTH))
        self.assertTrue(result['cliChecksPassed'])
        self.assertFalse(result['endToEndVerified'])

    def test_worker_limit(self):
        for workers in [0,5]:
            with self.assertRaises(ValueError): report(workers=workers)

    def test_timeout_limit(self):
        for timeout in [0,61,float('nan')]:
            with self.assertRaises(ValueError): probe(TOOLS[0], timeout)

    def test_concurrency_is_bounded(self):
        lock = threading.Lock()
        active = peak = 0
        def fake(c,t):
            nonlocal active, peak
            with lock:
                active += 1
                peak = max(peak, active)
            time.sleep(.01)
            with lock: active -= 1
            return {'status': 'tool-available'}
        report(workers=2, perform=fake)
        self.assertLessEqual(peak,2)


if __name__ == '__main__': unittest.main()

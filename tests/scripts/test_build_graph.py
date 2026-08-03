from __future__ import annotations
import importlib.util
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
SPEC=importlib.util.spec_from_file_location('build_graph', ROOT/'scripts/build_graph/build_graph.py')
build_graph=importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(build_graph)

class BuildGraphTests(unittest.TestCase):
    def test_classifies_core_statuses(self):
        self.assertEqual(build_graph.classify_result({'exitCode':1,'tail':[]}), 'failed')
        self.assertEqual(build_graph.classify_result({'exitCode':0,'command':'tool --dry-run','tail':[]}), 'skipped/dry-run')
        self.assertEqual(build_graph.classify_result({'exitCode':0,'command':'tool','tail':['ok']}), 'passed')
        self.assertEqual(build_graph.classify_result({'exitCode':0,'status':'skipped/dependency','tail':[]}), 'skipped/dependency')

    def test_next_fixes_for_known_tools(self):
        nodes=[{'id':'dotnet','command':'dotnet --info'},{'id':'az','command':'az account show'},{'id':'cmake','command':'cmake --build .build/native'}]
        results=[
            {'id':'dotnet','command':'dotnet --info','exitCode':127,'status':'failed','tail':['dotnet: not found']},
            {'id':'az','command':'az account show','exitCode':1,'status':'failed','tail':['Please run az login']},
            {'id':'cmake','command':'cmake --build .build/native','exitCode':1,'status':'failed','tail':['CMake Error']},
        ]
        fixes=build_graph.next_fixes(nodes, results)
        self.assertEqual(fixes[0]['command'], 'scripts/setup/bootstrap-local-tools.sh')
        self.assertEqual(fixes[1]['command'], 'az login')
        self.assertEqual(fixes[2]['command'], 'cmake -S src/native/HELIOS.Native.Performance -B .build/native')

    def test_dependency_expansion_orders_prerequisites(self):
        nodes=[{'id':'a'},{'id':'b','dependsOn':['a']},{'id':'c','dependsOn':['b']}]
        self.assertEqual([n['id'] for n in build_graph.expand_dependencies([nodes[2]], nodes)], ['a','b','c'])

    def test_changed_only_selection_records_reasons(self):
        class Args:
            all=False; node=None; profile=None; tag=None; include_critical=True; changed_only=True; base='HEAD'
        nodes=[{'id':'critical','critical':True,'paths':['never/**']},{'id':'python','paths':['scripts/**/*.py']},{'id':'dotnet','paths':['src/**/*.cs']}]
        original=build_graph.changed_files
        try:
            build_graph.changed_files=lambda base: ['scripts/build_graph/build_graph.py']
            selected, changed, reasons=build_graph.select_nodes(nodes, Args())
        finally:
            build_graph.changed_files=original
        self.assertEqual(changed, ['scripts/build_graph/build_graph.py'])
        self.assertEqual({n['id'] for n in selected}, {'critical','python'})
        self.assertEqual(reasons['critical']['reason'], 'critical')
        self.assertEqual(reasons['python']['reason'], 'changed-path')
        self.assertEqual(reasons['dotnet']['reason'], 'skipped/no-change')

if __name__ == '__main__':
    unittest.main()

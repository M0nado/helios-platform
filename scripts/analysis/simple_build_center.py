#!/usr/bin/env python3
from __future__ import annotations
import json
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/build-graph/simple-build-center.json'
MD=ROOT/'reports/build-graph/simple-build-center.md'
COMMANDS=[
 {'id':'notes','title':'Self-learning notes','command':'scripts/setup/simple-build.sh notes','details':'Writes a reusable notebook of module memories, scaffold suggestions, fix/delete advice, help cards, and lessons from grading/organizing rounds.'},
 {'id':'absorb','title':'Branch absorption build','command':'scripts/setup/simple-build.sh absorb','details':'Generates branch absorption, branch autofix, document/code absorption, merge/prune recommendations, 30-variable code grading, simple build metadata, and dashboard reports.'},
 {'id':'organize-full','title':'Full organize + absorb/prune learning','command':'scripts/setup/simple-build.sh organize-full','details':'Runs grading, branch absorption, document absorption, merge/prune, branch tests, fleet selection, module placement, learning feedback, and GUI refresh as one report-first command.'},{'id':'organize-scaffold-plan','title':'Missing scaffold plan','command':'scripts/setup/simple-build.sh organize-scaffold-plan','details':'Reports empty or missing module/submodule scaffold files and where they should be found or laid out, without creating them.'},{'id':'organize-delete-plan','title':'Delete/fix candidate plan','command':'scripts/setup/simple-build.sh organize-delete-plan','details':'Marks bad files, merge-conflict files, placeholder files, and delete candidates in reports only; live deletion still requires an explicit env flag and review.'},{'id':'organize','title':'50-variable module/submodule organizer','command':'scripts/setup/simple-build.sh organize','details':'Builds the automatic project module/submodule tree with 50 variables, system awareness, keep/change/delete/fix advice, agent assignment, and report-only controls.'},
 {'id':'learn','title':'AIHub learning loop','command':'scripts/setup/simple-build.sh learn','details':'Runs complex grading, branch absorption, merge/prune, agent/fleet catalogs, language profiles, module organization, learning feedback, and dashboard refresh.'},
 {'id':'finish','title':'Finish readiness build','command':'scripts/setup/simple-build.sh finish','details':'Runs the full auto-connect branch test/prune/packet/30-variable grading/learning/finish chain and refreshes the GUI.'},
 {'id':'module','title':'Module blueprint build','command':'scripts/setup/simple-build.sh module','details':'Builds the GUI/USBWizard module contracts and refreshes the module blueprint report.'},
 {'id':'quick','title':'Quick changed build','command':'scripts/setup/simple-build.sh quick','details':'Runs changed quick checks and refreshes the GUI.'},
 {'id':'full','title':'Full report build','command':'scripts/setup/simple-build.sh full','details':'Runs the full build graph and refreshes the GUI.'},
 {'id':'clean','title':'Clean generated build output','command':'scripts/setup/simple-build.sh clean','details':'Removes local build folders and shows git status.'}
]
def main():
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'purpose':'Simple GUI-friendly build entry points so long validation commands become natural buttons instead of pasted walls of text.','commands':COMMANDS}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# Simple Build Center','',payload['purpose'],'']+[f"- **{c['title']}**: `{c['command']}` — {c['details']}" for c in COMMANDS]
    MD.write_text('\n'.join(lines)+'\n')
    print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()

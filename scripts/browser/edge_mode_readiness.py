#!/usr/bin/env python3
"""Report Microsoft Edge / IE Mode readiness without using retired Internet Explorer automation."""
from __future__ import annotations
import json, shutil, datetime as dt, os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/browser'
def main():
 edge=shutil.which('msedge') or shutil.which('microsoft-edge') or shutil.which('microsoft-edge-stable')
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'browser':'Microsoft Edge','edgeAvailable':bool(edge),'edgePath':edge,'internetExplorerAllowed':False,'legacyIeModePolicyExpected':True,'allowlistedUrls':os.getenv('HELIOS_LEGACY_BROWSER_URLS','').split(';') if os.getenv('HELIOS_LEGACY_BROWSER_URLS') else [],'note':'Use Microsoft Edge IE Mode by enterprise policy for legacy sites; do not automate retired Internet Explorer or read browser secrets.'}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'edge-mode-readiness.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'edge-mode-readiness.md').write_text('# Edge IE Mode Readiness\n\n'+json.dumps(payload,indent=2)+'\n')
 print(f"Wrote {(OUT/'edge-mode-readiness.md').relative_to(ROOT)}")
 return 0
if __name__=='__main__': raise SystemExit(main())

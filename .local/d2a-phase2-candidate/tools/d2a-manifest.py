"""Freeze or verify the complete D2-A candidate; evidence is separate."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / 'docs/stage3/d2a/candidate-manifest.json'
DIRS = ['src', 'tests', 'tools', 'docs/api', 'docs/source', 'docs/stage3/d2a']
IGNORE = {'bin', 'obj', 'node_modules', 'dist', 'TestResults', 'test-results',
          'playwright-report', '__pycache__', '.local', '.git', 'evidence', 'reports', 'closure'}

def entries():
    paths = {p for p in ROOT.iterdir() if p.is_file()}
    for directory in DIRS:
        paths.update(p for p in (ROOT / directory).rglob('*') if p.is_file()
                     and not (set(p.relative_to(ROOT).parts) & IGNORE) and p != MANIFEST)
    return [{'path': p.relative_to(ROOT).as_posix(),
             'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
            for p in sorted(paths)]

if sys.argv[1:] == ['--verify']:
    assert json.loads(MANIFEST.read_text(encoding='utf-8'))['entries'] == entries(), 'Candidate drift'
    print('Candidate content and file inventory verified')
elif not sys.argv[1:]:
    MANIFEST.write_text(json.dumps({'kind': 'D2-A frozen candidate, not human acceptance',
        'directories': DIRS, 'excludedDirectoryNames': sorted(IGNORE), 'entries': entries()}, indent=2) + '\n', encoding='utf-8')
else:
    raise SystemExit('Usage: python tools/d2a-manifest.py [--verify]')
print('SHA-256 ' + hashlib.sha256(MANIFEST.read_bytes()).hexdigest())

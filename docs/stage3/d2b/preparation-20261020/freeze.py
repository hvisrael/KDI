"""Freeze/verify the isolated D2-B source inventory; never writes accepted artifacts."""
import hashlib
import json
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3] / '.local/d2b-candidate'
MANIFEST = HERE / 'candidate-manifest.json'
EXCLUDED = {'bin', 'obj', 'node_modules', 'dist', 'wwwroot', 'TestResults',
            'test-results', 'playwright-report', '__pycache__', '.local', '.git'}

def entries():
    found = []
    for p in ROOT.rglob('*'):
        if not p.is_file():
            continue
        rel = p.relative_to(ROOT)
        if set(rel.parts) & EXCLUDED or rel.as_posix().startswith('docs/stage3/d2b/evidence/'):
            continue
        found.append({'path': rel.as_posix(), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()})
    return sorted(found, key=lambda x: x['path'])

if sys.argv[1:] == ['--verify']:
    assert entries() == json.loads(MANIFEST.read_text(encoding='utf-8'))['entries'], 'Candidate changed'
elif not sys.argv[1:]:
    MANIFEST.write_text(json.dumps({'kind': 'D2B-local-preparation-not-accepted',
        'root': '.local/d2b-candidate', 'excludedDirectoryNames': sorted(EXCLUDED),
        'excludedEvidence': 'docs/stage3/d2b/evidence/', 'entries': entries()}, indent=2) + '\n', encoding='utf-8')
else:
    raise SystemExit('Usage: freeze.py [--verify]')
print('SHA256 ' + hashlib.sha256(MANIFEST.read_bytes()).hexdigest())
print(str(len(json.loads(MANIFEST.read_text(encoding='utf-8'))['entries'])) + ' files; verification OK')

"""Create/verify a D1 source snapshot. Evidence and generated output stay outside it."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / 'docs/stage3/d1/implementation-manifest.json'
DIRECTORIES = ['src', 'tests', 'tools', 'docs/api', 'docs/source']
FILES = ['README.md', 'project.json', '.editorconfig', '.gitignore', 'global.json',
         'Directory.Build.props', 'Directory.Packages.props', 'NuGet.Config', 'Kulani.KDI.slnx',
         'docs/stage3/STATUS.md', 'docs/stage3/DECISIONS.md', 'docs/stage3/FUNCTIONAL-ANALYSIS.md',
         'docs/stage3/baseline.json', 'docs/stage3/task-input.json']
IGNORED = {'bin', 'obj', 'node_modules', 'dist', 'TestResults', 'test-results',
           'playwright-report', '__pycache__', '.local', '.git'}

def entries():
    paths = [ROOT / name for name in FILES]
    for name in DIRECTORIES:
        paths.extend(p for p in (ROOT / name).rglob('*') if p.is_file()
                     and not (set(p.relative_to(ROOT).parts) & IGNORED))
    return [{'path': p.relative_to(ROOT).as_posix(), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
            for p in sorted(paths, key=lambda p: p.relative_to(ROOT).as_posix())]

if sys.argv[1:] == ['--verify']:
    saved = json.loads(MANIFEST.read_text(encoding='utf-8'))
    assert saved['entries'] == entries(), 'Snapshot changed: content, added file, or removed file'
    print('PASS source snapshot verification')
elif not sys.argv[1:]:
    data = {'schemaVersion': '1.0', 'kind': 'd1-source-snapshot-not-accepted-release',
            'directories': DIRECTORIES, 'files': FILES, 'excludedDirectoryNames': sorted(IGNORED),
            'evidenceExcluded': 'docs/stage3/d1', 'entries': entries()}
    MANIFEST.write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')
else:
    raise SystemExit('Usage: python tools/content-manifest.py [--verify]')

print(f"SHA-256 {hashlib.sha256(MANIFEST.read_bytes()).hexdigest()}; {len(json.loads(MANIFEST.read_text())['entries'])} source files")

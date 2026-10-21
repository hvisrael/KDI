"""Read-only final closure verification; no SQL or mutations."""
from pathlib import Path
import hashlib, json, subprocess

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
C = HERE / 'candidate'
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
m = C / 'manifest.json'
expected_hash = (HERE / 'manifest.sha256').read_text().split()[0]
assert sha(m) == expected_hash, 'Manifest changed'
expected = json.loads(m.read_bytes())['entries']
actual = [{'path':p.relative_to(C).as_posix(),'sha256':sha(p)}
          for p in sorted(C.rglob('*')) if p.is_file() and p != m]
assert expected == actual, 'Frozen inventory/content changed; reviews invalid'
assert (ROOT/'.codex/agents/dba-engineer.toml').read_bytes() == (C/'effective-profile.toml').read_bytes()
assert (ROOT/'.codex/config.toml').read_bytes() == (C/'project-config.toml').read_bytes()
assert sha(ROOT/'docs/stage3/d2a/candidate-manifest.json') == '66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f'
subprocess.run(['python','tools/d2a-manifest.py','--verify'],cwd=ROOT,check=True)
print('Frozen onboarding candidate verified: '+expected_hash)
print('Integrity verified; this does not resolve retrospective AGENTS attribution or imply overall PASS')
